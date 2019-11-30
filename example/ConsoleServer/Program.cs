using System;
using NetDiscovery.Udp;

namespace ConsoleServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // Create server
            using var provider = new UdpProvider(52148);
            using var server = provider.CreateServer();
            server.Identity = "TestServer";

            // Start components
            server.Start();
            provider.Start();

            // Wait for key
            Console.WriteLine("Press any key to terminate...");
            Console.ReadKey();
        }
    }
}
