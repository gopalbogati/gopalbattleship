using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GopalBattleship.Utilities
{
   public static class Helpers
    {

        public static int GetNumberOfGames()
        {
            int numberInput = 0;
            do {
                int.TryParse(Console.ReadLine(), out numberInput);
                if(numberInput == 0 || numberInput > 10)
                {
                    Console.WriteLine("Invalid Number ! Must be a valid number between 1 and 10");
                }
            } while (numberInput == 0 || numberInput > 10);

            return numberInput;

        }
        public static string GetPlayerName(string playerOrder)
        {
            string playerName = null;
            do {
                Console.Write($"Enter the {playerOrder} player name: ");
                playerName = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(playerName))
                {
                    Console.WriteLine("You must enter the player name! ");
                }
            } while (string.IsNullOrWhiteSpace(playerName));

            return playerName;
           
        }
    }
}
