using System;
using NetDiscovery.Udp;

namespace ConsoleClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create discovery client
            using var provider = new UdpProvider(52148);
            using var client = provider.CreateClient();
            client.Discovery += (s, e) => Console.WriteLine($"Discovered: {e.Address}: {e.Identity}");

            // Start components
            client.Start();
            provider.Start();

            // Wait for key
            Console.WriteLine("Press any key to terminate...");
            Console.ReadKey();
        }
    }
}
