using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GopalBattleship.Entities.Boards;

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
            if (IsFinished)
            {
                throw new InvalidOperationException("Game already finished");
            }

            Player shootingPlayer;
            Player opponent;

            if (FirstPlayer.Name == playerName)
            {
                shootingPlayer = FirstPlayer;
                opponent = SecondPlayer;
            }
            else if (SecondPlayer.Name == playerName)
            {
                shootingPlayer = SecondPlayer;
                opponent = FirstPlayer;
            }
            else
            {
                throw new ArgumentException("Unknown player");
            }

            if (shootingPlayer != CurrentPlayer)
            {
                throw new InvalidOperationException("It's not this player's turn");
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
            if (FirstPlayer.Name == playerName)
            {
                return FirstPlayer.GetBoardsString();
            }
            else if (SecondPlayer.Name == playerName)
            {
                return SecondPlayer.GetBoardsString();
            }

            throw new ArgumentException("Unknown player");
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
