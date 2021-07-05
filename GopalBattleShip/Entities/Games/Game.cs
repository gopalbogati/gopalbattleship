using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GopalBattleship.Entities.Games
{
    public class Game
    {
        public Player FirstPlayer { get; set; }
        public Player SecondPlayer { get; set; }

        public Game(string firstPlayer, string secondPlayer)
        {
            FirstPlayer = new Player(firstPlayer);
            SecondPlayer = new Player(secondPlayer);

            FirstPlayer.PlaceShips();
            SecondPlayer.PlaceShips();

            FirstPlayer.OutputBoards();
            SecondPlayer.OutputBoards();
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

            FirstPlayer.OutputBoards();
            SecondPlayer.OutputBoards();

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
