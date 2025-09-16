using System;
using GopalBattleship.Online;

namespace GopalBattleship
{
    class Program
    {
        static void Main(string[] args)
        {
            var server = new OnlineGameServer("http://localhost:5000/");
            server.Start();
            Console.WriteLine("Battleship online server running at http://localhost:5000/");
            Console.WriteLine("Press Enter to stop the server.");
            Console.ReadLine();
            server.Stop();
        }
    }
}
