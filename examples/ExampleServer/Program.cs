using System;
using NetDiscovery.Udp;

namespace ExampleServer
{
    class Program
    {
        static void Main(string[] args)
        {
            // Create server
            using var provider = new UdpDiscoveryProvider(52148);
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
