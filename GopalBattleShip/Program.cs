using System;
using GopalBattleship.Online;

namespace GopalBattleship
{
    class Program
    {
        static void Main(string[] args)
        {
            var prefix = GetPrefix(args);
            var server = new OnlineGameServer(prefix);
            server.Start();
            Console.WriteLine($"Battleship online server running at {prefix}");
            Console.WriteLine("Press Enter to stop the server.");
            Console.ReadLine();
            server.Stop();
        }

        private static string GetPrefix(string[] args)
        {
            var prefix = args.Length > 0
                ? args[0]
                : Environment.GetEnvironmentVariable("BATTLESHIP_PREFIX");

            if (string.IsNullOrWhiteSpace(prefix))
            {
                prefix = "http://localhost:5000/";
            }

            if (!prefix.EndsWith("/"))
            {
                prefix += "/";
            }

            return prefix;
        }
    }
}
