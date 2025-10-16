using System;
using GopalBattleship.Entities.Boards;
using GopalBattleship.Utilities;

namespace GopalBattleship.Entities.Games
{
    public class Game
    {
        public Player FirstPlayer { get; set; }
        public Player SecondPlayer { get; set; }
        public Player CurrentPlayer { get; private set; }
        public bool IsFinished
        {
            get
            {
                return FirstPlayer.HasLost || SecondPlayer.HasLost;
            }
        }

        public Player Winner
        {
            get
            {
                if (!IsFinished)
                {
                    return null;
                }

                if (FirstPlayer.HasLost)
                {
                    return SecondPlayer;
                }

                if (SecondPlayer.HasLost)
                {
                    return FirstPlayer;
                }

                return null;
            }
        }

        public string WinnerName
        {
            get
            {
                return Winner?.Name;
            }
        }

        public Game(string firstPlayer, string secondPlayer)
        {
            FirstPlayer = new Player(firstPlayer);
            SecondPlayer = new Player(secondPlayer);

            FirstPlayer.PlaceShips();
            SecondPlayer.PlaceShips();

            CurrentPlayer = FirstPlayer;
        }

        public ShotResult FireShot(string playerName, Coordinates coordinates)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                throw new ArgumentException("Player name is required", nameof(playerName));
            }

            if (IsFinished)
            {
                throw new InvalidOperationException("Game already finished");
            }

            var shootingPlayer = GetPlayer(playerName);
            var opponent = GetOpponent(playerName);

            if (shootingPlayer != CurrentPlayer)
            {
                throw new InvalidOperationException("It's not this player's turn");
            }

            ValidateCoordinates(coordinates);

            var firingPanel = shootingPlayer.FiringBoard.Panels.At(coordinates.Row, coordinates.Column);
            if (firingPanel.OccupationType == EnumLabel.Hit || firingPanel.OccupationType == EnumLabel.Miss)
            {
                throw new InvalidOperationException("This coordinate has already been targeted.");
            }

            var result = opponent.ProcessShot(coordinates);
            shootingPlayer.ProcessShotResult(coordinates, result);

            if (!opponent.HasLost)
            {
                CurrentPlayer = opponent;
            }

            return result;
        }

        public string GetBoards(string playerName)
        {
            return GetPlayer(playerName).GetBoardsString();
        }

        public Player GetPlayer(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName))
            {
                throw new ArgumentException("Player name is required", nameof(playerName));
            }

            if (string.Equals(FirstPlayer.Name, playerName, StringComparison.OrdinalIgnoreCase))
            {
                return FirstPlayer;
            }

            if (string.Equals(SecondPlayer.Name, playerName, StringComparison.OrdinalIgnoreCase))
            {
                return SecondPlayer;
            }

            throw new ArgumentException($"Unknown player '{playerName}'");
        }

        public Player GetOpponent(string playerName)
        {
            var player = GetPlayer(playerName);
            return ReferenceEquals(player, FirstPlayer) ? SecondPlayer : FirstPlayer;
        }

        private static void ValidateCoordinates(Coordinates coordinates)
        {
            if (coordinates == null)
            {
                throw new ArgumentNullException(nameof(coordinates));
            }

            if (coordinates.Row < 1 || coordinates.Row > 10 || coordinates.Column < 1 || coordinates.Column > 10)
            {
                throw new ArgumentOutOfRangeException(nameof(coordinates), "Coordinates must be between 1 and 10.");
            }
        }

        public void PlayTurn()
        {
            //Each exchange of shots is called a Round.
            //One round = Player 1 fires a shot, then Player 2 fires a shot.
            var coordinates = FirstPlayer.FireShot();
            var result = SecondPlayer.ProcessShot(coordinates);
            FirstPlayer.ProcessShotResult(coordinates, result);

            if (!SecondPlayer.HasLost) //If player 2 already lost, we can't let them take another turn.
            {
                coordinates = SecondPlayer.FireShot();
                result = FirstPlayer.ProcessShot(coordinates);
                SecondPlayer.ProcessShotResult(coordinates, result);
            }
        }

        public void ExecuteTheWholeGame()
        {
            while (!FirstPlayer.HasLost && !SecondPlayer.HasLost)
            {
                PlayTurn();
            }

            if (FirstPlayer.HasLost)
            {
                Console.WriteLine(SecondPlayer.Name + " has won the game!");
            }
            else if (SecondPlayer.HasLost)
            {
                Console.WriteLine(FirstPlayer.Name + " has won the game!");
            }
        }
    }
}
