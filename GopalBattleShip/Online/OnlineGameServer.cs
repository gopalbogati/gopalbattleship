using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using GopalBattleship.Entities;
using GopalBattleship.Entities.Boards;
using GopalBattleship.Entities.Games;
using GopalBattleship.Utilities;

namespace GopalBattleship.Online
{
    /// <summary>
    /// Lightweight HTTP listener that exposes Battleship game sessions over JSON endpoints.
    /// </summary>
    public class OnlineGameServer
    {
        private readonly HttpListener _listener;
        private readonly ConcurrentDictionary<string, Game> _games = new ConcurrentDictionary<string, Game>();
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();
        private bool _isRunning;

        public OnlineGameServer(params string[] prefixes)
        {
            if (prefixes == null || prefixes.Length == 0)
            {
                throw new ArgumentException("At least one prefix must be provided.", nameof(prefixes));
            }

            _listener = new HttpListener();

            foreach (var prefix in prefixes)
            {
                if (string.IsNullOrWhiteSpace(prefix))
                {
                    continue;
                }

                var normalized = prefix.EndsWith("/") ? prefix : prefix + "/";
                _listener.Prefixes.Add(normalized);
            }

            if (_listener.Prefixes.Count == 0)
            {
                throw new ArgumentException("At least one valid prefix must be provided.", nameof(prefixes));
            }
        }

        public void Start()
        {
            if (_isRunning)
            {
                return;
            }

            _listener.Start();
            _listener.BeginGetContext(ProcessRequest, null);
            _isRunning = true;
            Console.WriteLine("Battleship server started on " + string.Join(", ", _listener.Prefixes));
        }

        public void Stop()
        {
            if (!_isRunning)
            {
                return;
            }

            _isRunning = false;

            try
            {
                if (_listener.IsListening)
                {
                    _listener.Stop();
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (HttpListenerException)
            {
            }

            try
            {
                _listener.Close();
            }
            catch (ObjectDisposedException)
            {
            }
        }

        private void ProcessRequest(IAsyncResult ar)
        {
            HttpListenerContext context = null;

            try
            {
                if (!_listener.IsListening)
                {
                    return;
                }

                context = _listener.EndGetContext(ar);
            }
            catch (ObjectDisposedException)
            {
                return;
            }
            catch (HttpListenerException)
            {
                return;
            }
            finally
            {
                if (_listener.IsListening)
                {
                    _listener.BeginGetContext(ProcessRequest, null);
                }
            }

            if (context == null)
            {
                return;
            }

            var request = context.Request;
            var response = context.Response;

            response.Headers["Access-Control-Allow-Origin"] = "*";
            response.Headers["Access-Control-Allow-Methods"] = "GET,POST,DELETE,OPTIONS";
            response.Headers["Access-Control-Allow-Headers"] = "Content-Type";

            if (string.Equals(request.HttpMethod, "OPTIONS", StringComparison.OrdinalIgnoreCase))
            {
                response.StatusCode = (int)HttpStatusCode.OK;
                response.Close();
                return;
            }

            object result;
            var statusCode = HttpStatusCode.OK;
            var body = ReadRequestBody(request);

            try
            {
                result = HandleRequest(request, body);
            }
            catch (HttpErrorException ex)
            {
                statusCode = (HttpStatusCode)ex.StatusCode;
                result = new { error = ex.Message };
            }
            catch (ArgumentException ex)
            {
                statusCode = HttpStatusCode.BadRequest;
                result = new { error = ex.Message };
            }
            catch (InvalidOperationException ex)
            {
                statusCode = HttpStatusCode.BadRequest;
                result = new { error = ex.Message };
            }
            catch (Exception ex)
            {
                statusCode = HttpStatusCode.InternalServerError;
                result = new { error = ex.Message };
            }

            response.StatusCode = (int)statusCode;
            WriteJson(response, result);
        }

        private object HandleRequest(HttpListenerRequest request, string body)
        {
            var segments = request.Url.AbsolutePath.Trim('/')
                .Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            if (segments.Length == 0)
            {
                return new
                {
                    message = "Battleship server is running.",
                    activeGames = _games.Count
                };
            }

            if (!string.Equals(segments[0], "games", StringComparison.OrdinalIgnoreCase))
            {
                throw new HttpErrorException(404, "Unknown endpoint.");
            }

            if (segments.Length == 1)
            {
                if (string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    var games = _games.Select(kvp => BuildGameState(kvp.Key, kvp.Value))
                        .OrderBy(state => state.GameId)
                        .ToList();

                    return new { games };
                }

                EnsureMethod(request, "POST");

                var payload = DeserializeBody<StartGamePayload>(body) ?? new StartGamePayload
                {
                    Player1 = request.QueryString["player1"],
                    Player2 = request.QueryString["player2"]
                };

                if (string.IsNullOrWhiteSpace(payload.Player1) || string.IsNullOrWhiteSpace(payload.Player2))
                {
                    throw new HttpErrorException(400, "Both player names are required.");
                }

                var id = Guid.NewGuid().ToString("N");
                var game = new Game(payload.Player1.Trim(), payload.Player2.Trim());
                _games[id] = game;

                return new
                {
                    gameId = id,
                    game = BuildGameState(id, game)
                };
            }

            var gameId = segments[1];
            if (!_games.TryGetValue(gameId, out var existingGame))
            {
                throw new HttpErrorException(404, "Game not found.");
            }

            if (segments.Length == 2)
            {
                if (string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    return new { game = BuildGameState(gameId, existingGame) };
                }

                if (string.Equals(request.HttpMethod, "DELETE", StringComparison.OrdinalIgnoreCase))
                {
                    _games.TryRemove(gameId, out _);
                    return new { message = $"Game {gameId} removed." };
                }

                throw new HttpErrorException(405, "Method not allowed.");
            }

            var action = segments[2];

            if (string.Equals(action, "fire", StringComparison.OrdinalIgnoreCase))
            {
                EnsureMethod(request, "POST");
                var payload = DeserializeBody<FireShotPayload>(body) ?? FireShotPayload.FromQuery(request.QueryString);

                if (payload == null)
                {
                    throw new HttpErrorException(400, "Invalid request payload.");
                }

                if (string.IsNullOrWhiteSpace(payload.Player))
                {
                    throw new HttpErrorException(400, "Player is required.");
                }

                if (payload.Row < 1 || payload.ColumnValue < 1)
                {
                    throw new HttpErrorException(400, "Row and column must be between 1 and 10.");
                }

                var result = existingGame.FireShot(payload.Player.Trim(), new Coordinates(payload.Row, payload.ColumnValue));

                return new
                {
                    result = result.ToString(),
                    isHit = result == ShotResult.Hit,
                    game = BuildGameState(gameId, existingGame)
                };
            }

            if (string.Equals(action, "board", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    throw new HttpErrorException(405, "Method not allowed.");
                }

                var payload = DeserializeBody<PlayerRequest>(body);
                var playerName = payload?.Player ?? request.QueryString["player"];

                if (string.IsNullOrWhiteSpace(playerName))
                {
                    throw new HttpErrorException(400, "Player is required to view a board.");
                }

                var player = existingGame.GetPlayer(playerName.Trim());

                return new
                {
                    board = BuildBoardView(gameId, existingGame, player)
                };
            }

            throw new HttpErrorException(404, "Unknown endpoint.");
        }

        private GameStateDto BuildGameState(string gameId, Game game)
        {
            return new GameStateDto
            {
                GameId = gameId,
                Player1 = game.FirstPlayer.Name,
                Player2 = game.SecondPlayer.Name,
                CurrentPlayer = game.CurrentPlayer.Name,
                IsFinished = game.IsFinished,
                Winner = game.WinnerName,
                RemainingShips = new Dictionary<string, int>
                {
                    { game.FirstPlayer.Name, game.FirstPlayer.Ships.Count(ship => !ship.IsSunk) },
                    { game.SecondPlayer.Name, game.SecondPlayer.Ships.Count(ship => !ship.IsSunk) }
                }
            };
        }

        private BoardViewDto BuildBoardView(string gameId, Game game, Player player)
        {
            return new BoardViewDto
            {
                GameId = gameId,
                Player = player.Name,
                IsPlayersTurn = ReferenceEquals(player, game.CurrentPlayer),
                OwnBoard = BuildBoard(player.GameBoard),
                TargetingBoard = BuildBoard(player.FiringBoard),
                Legend = _legend
            };
        }

        private static string[][] BuildBoard(PlayBoard board)
        {
            var rows = new string[10][];

            for (var row = 1; row <= 10; row++)
            {
                var values = new string[10];
                for (var column = 1; column <= 10; column++)
                {
                    values[column - 1] = board.Panels.At(row, column).Status;
                }

                rows[row - 1] = values;
            }

            return rows;
        }

        private static void EnsureMethod(HttpListenerRequest request, string method)
        {
            if (!string.Equals(request.HttpMethod, method, StringComparison.OrdinalIgnoreCase))
            {
                throw new HttpErrorException(405, $"{request.HttpMethod} is not allowed. Expected {method}.");
            }
        }

        private string ReadRequestBody(HttpListenerRequest request)
        {
            if (!request.HasEntityBody)
            {
                return string.Empty;
            }

            using (var reader = new StreamReader(request.InputStream, request.ContentEncoding ?? Encoding.UTF8))
            {
                return reader.ReadToEnd();
            }
        }

        private T DeserializeBody<T>(string body) where T : class
        {
            if (string.IsNullOrWhiteSpace(body))
            {
                return null;
            }

            try
            {
                return _serializer.Deserialize<T>(body);
            }
            catch (InvalidOperationException)
            {
                throw new HttpErrorException(400, "Request body is not valid JSON.");
            }
        }

        private void WriteJson(HttpListenerResponse response, object data)
        {
            if (data == null)
            {
                response.ContentLength64 = 0;
                response.Close();
                return;
            }

            var payload = _serializer.Serialize(data);
            var buffer = Encoding.UTF8.GetBytes(payload);
            response.ContentType = "application/json";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
            response.Close();
        }

        private static readonly IDictionary<string, string> _legend = CreateLegend();

        private static IDictionary<string, string> CreateLegend()
        {
            return new Dictionary<string, string>
            {
                { nameof(EnumLabel.Empty), EnumLabel.Empty.GetAttributeOfType<DescriptionAttribute>().Description },
                { nameof(EnumLabel.Battleship), EnumLabel.Battleship.GetAttributeOfType<DescriptionAttribute>().Description },
                { nameof(EnumLabel.Hit), EnumLabel.Hit.GetAttributeOfType<DescriptionAttribute>().Description },
                { nameof(EnumLabel.Miss), EnumLabel.Miss.GetAttributeOfType<DescriptionAttribute>().Description }
            };
        }

        private class HttpErrorException : Exception
        {
            public int StatusCode { get; }

            public HttpErrorException(int statusCode, string message)
                : base(message)
            {
                StatusCode = statusCode;
            }
        }

        private class StartGamePayload
        {
            public string Player1 { get; set; }
            public string Player2 { get; set; }
        }

        private class PlayerRequest
        {
            public string Player { get; set; }
        }

        private class FireShotPayload
        {
            public string Player { get; set; }
            public int Row { get; set; }
            public int Column { get; set; }
            public int Col { get; set; }

            public int ColumnValue => Column != 0 ? Column : Col;

            public static FireShotPayload FromQuery(System.Collections.Specialized.NameValueCollection query)
            {
                if (query == null)
                {
                    return null;
                }

                var row = ParseInt(query["row"]);
                var column = ParseInt(query["column"], query["col"]);

                if (string.IsNullOrWhiteSpace(query["player"]) && row == 0 && column == 0)
                {
                    return null;
                }

                return new FireShotPayload
                {
                    Player = query["player"],
                    Row = row,
                    Column = column
                };
            }
        }

        private static int ParseInt(params string[] values)
        {
            foreach (var value in values)
            {
                if (int.TryParse(value, out var parsed))
                {
                    return parsed;
                }
            }

            return 0;
        }

        private class GameStateDto
        {
            public string GameId { get; set; }
            public string Player1 { get; set; }
            public string Player2 { get; set; }
            public string CurrentPlayer { get; set; }
            public bool IsFinished { get; set; }
            public string Winner { get; set; }
            public IDictionary<string, int> RemainingShips { get; set; }
        }

        private class BoardViewDto
        {
            public string GameId { get; set; }
            public string Player { get; set; }
            public bool IsPlayersTurn { get; set; }
            public string[][] OwnBoard { get; set; }
            public string[][] TargetingBoard { get; set; }
            public IDictionary<string, string> Legend { get; set; }
        }
    }
}
