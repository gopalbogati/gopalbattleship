using GopalBattleship.Entities.Games;
using GopalBattleship.Utilities;
using System;

namespace GopalBattleship
{
    class Program
    {
        static void Main(string[] args)
        {
            int isFirstPlayerWinner = 0, isSecondPlayerWinner = 0;

            Console.WriteLine("Specify the number of games you would like to play");
            var numGames = Helpers.GetNumberOfGames();
            Console.WriteLine($"Total number of games : {numGames}");
            //Two players created
            string firstPlayer = Helpers.GetPlayerName("first");
            string secondPlayer = Helpers.GetPlayerName("second");
            Console.Write("Enter any key to start :");
            Console.ReadKey();

            for (int i = 0; i < numGames; i++)
            {
                Game game1 = new Game(firstPlayer, secondPlayer);
                game1.ExecuteTheWholeGame();
                if(game1.FirstPlayer.HasLost)
                {
                    isSecondPlayerWinner++;
                }
                else
                {
                    isFirstPlayerWinner++;
                }
            }

            Console.WriteLine($"{firstPlayer} Wins: " + isFirstPlayerWinner.ToString());
            Console.WriteLine($"{secondPlayer} Wins: " + isSecondPlayerWinner.ToString());
            Console.ReadLine();
           
        }
    }
}
