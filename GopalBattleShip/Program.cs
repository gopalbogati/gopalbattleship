using System;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using GopalBattleship.Online;

namespace GopalBattleship
{
    class Program
    {
        static void Main(string[] args)
        {
            var prefixEnv = Environment.GetEnvironmentVariable("BATTLESHIP_PREFIX");
            string[] prefixes;

            if (!string.IsNullOrWhiteSpace(prefixEnv))
            {
                prefixes = prefixEnv
                    .Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToArray();
            }
            else if (args.Length > 0)
            {
                prefixes = args;
            }
            else
            {
                prefixes = new[] { "http://+:5000/" };
            }

            prefixes = prefixes
                .Select(p => p.EndsWith("/") ? p : p + "/")
                .ToArray();

            var server = new OnlineGameServer(prefixes);
            var shutdownEvent = new ManualResetEventSlim(false);

            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                shutdownEvent.Set();
            };

            try
            {
                try
                {
                    server.Start();
                }
                catch (HttpListenerException ex)
                {
                    Console.Error.WriteLine("Failed to start the Battleship server: " + ex.Message);
                    Console.Error.WriteLine("Ensure the prefix is valid and accessible (try running as administrator or changing the port).");
                    return;
                }

                Console.WriteLine($"Battleship online server running at {string.Join(", ", prefixes)}");
                Console.WriteLine("Send HTTP requests to /games endpoints to create and play matches.");
                Console.WriteLine("Press Enter or use Ctrl+C to stop the server.");

                Task.Run(() =>
                {
                    Console.ReadLine();
                    shutdownEvent.Set();
                });

                shutdownEvent.Wait();
            }
            finally
            {
                shutdownEvent.Dispose();
                server.Stop();
                Console.WriteLine("Server stopped.");
            }
        }
    }
}
