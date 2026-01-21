using System;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using GopalBattleship.Entities.Boards;
using GopalBattleship.Entities.Games;

namespace GopalBattleship.Online
{
    /// <summary>
    /// Simple HTTP server to allow playing Battleship over the network.
    /// </summary>
    public class OnlineGameServer
    {
        private readonly HttpListener _listener;
        private readonly ConcurrentDictionary<string, Game> _games = new ConcurrentDictionary<string, Game>();

        public OnlineGameServer(string prefix)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(prefix);
        }

        public void Start()
        {
            _listener.Start();
            _listener.BeginGetContext(ProcessRequest, null);
            Console.WriteLine("Battleship server started on " + string.Join(", ", _listener.Prefixes));
        }

        public void Stop()
        {
            _listener.Stop();
        }

        private void ProcessRequest(IAsyncResult ar)
        {
            if (!_listener.IsListening) return;
            var context = _listener.EndGetContext(ar);
            _listener.BeginGetContext(ProcessRequest, null);

            var request = context.Request;
            var response = context.Response;
            string payload = string.Empty;

            try
            {
                if (request.Url.AbsolutePath == "/start")
                {
                    var player1 = request.QueryString["player1"];
                    var player2 = request.QueryString["player2"];
                    if (string.IsNullOrWhiteSpace(player1) || string.IsNullOrWhiteSpace(player2))
                    {
                        response.StatusCode = 400;
                        payload = "Both player names are required.";
                    }
                    else
                    {
                        var id = Guid.NewGuid().ToString();
                        _games[id] = new Game(player1, player2);
                        payload = id;
                    }
                }
                else if (request.Url.AbsolutePath == "/health")
                {
                    payload = "OK";
                }
                else if (request.Url.AbsolutePath == "/fire")
                {
                    var id = request.QueryString["gameId"];
                    var player = request.QueryString["player"];
                    var rowStr = request.QueryString["row"];
                    var colStr = request.QueryString["col"];

                    if (_games.TryGetValue(id, out var game) &&
                        int.TryParse(rowStr, out var row) &&
                        int.TryParse(colStr, out var col))
                    {
                        var result = game.FireShot(player, new Coordinates(row, col));
                        payload = result.ToString();
                    }
                    else
                    {
                        response.StatusCode = 400;
                        payload = "Invalid request";
                    }
                }
                else if (request.Url.AbsolutePath == "/board")
                {
                    var id = request.QueryString["gameId"];
                    var player = request.QueryString["player"];
                    if (_games.TryGetValue(id, out var game))
                    {
                        payload = game.GetBoards(player);
                    }
                    else
                    {
                        response.StatusCode = 404;
                        payload = "Game not found";
                    }
                }
                else
                {
                    response.StatusCode = 404;
                    payload = "Unknown endpoint";
                }
            }
            catch (ArgumentException ex)
            {
                response.StatusCode = 400;
                payload = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                response.StatusCode = 400;
                payload = ex.Message;
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                payload = ex.Message;
            }

            var buffer = Encoding.UTF8.GetBytes(payload);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }
}
