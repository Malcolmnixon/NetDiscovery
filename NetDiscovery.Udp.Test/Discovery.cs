using System.Collections.Generic;
using System.Net;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace NetDiscovery.Udp.Test
{
    [TestClass]
    public class Discovery
    {
        [TestMethod]
        public void TestDiscover()
        {
            var servers = new Dictionary<string, IPAddress>();

            // Create the provider
            using var provider = new UdpProvider(12345);

            // Create the discovery client
            using var client = provider.CreateClient();
            client.Discovery += (s, e) =>
            {
                lock (servers)
                {
                    servers[e.Identity] = e.Address;
                }
            };

            // Create the discovery server
            using var server = provider.CreateServer();
            server.Identity = "TestDiscover";

            // Start discovery
            provider.Start();
            server.Start();
            client.Start();

            // Wait 5 seconds for discovery
            Thread.Sleep(5000);

            // Stop discovery
            provider.Stop();
            server.Stop();
            client.Stop();

            // Verify the server was discovered
            Assert.IsTrue(servers.ContainsKey("TestDiscover"));
        }
    }
}
