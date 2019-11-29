using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using NetDiscovery.Udp;

namespace NetDiscoveryTest
{
    [TestClass]
    public class UdpDiscoveryTest
    {
        [TestMethod]
        public void TestCreateClient()
        {
            using var provider = new UdpDiscoveryProvider(52148);
            using var client = provider.CreateClient();

            // Start components
            client.Start();
            provider.Start();

            Thread.Sleep(1000);

            // Stop components
            client.Stop();
            provider.Stop();
        }

        [TestMethod]
        public void TestCreateServer()
        {
            using var provider = new UdpDiscoveryProvider(52148);
            using var server = provider.CreateServer();
            server.Identity = "TestServer";

            // Start components
            server.Start();
            provider.Start();

            Thread.Sleep(1000);

            // Stop components
            server.Stop();
            provider.Stop();
        }

        [TestMethod]
        public void TestFindServer()
        {
            // Dictionary of found servers
            var found = new Dictionary<IPAddress, string>();

            using var provider = new UdpDiscoveryProvider(52148);
            using var client = provider.CreateClient();
            using var server = provider.CreateServer();
            client.DiscoveredServer += (s, e) => { found[e.Address] = e.Identity; };
            server.Identity = "TestServer";

            // Start components
            client.Start();
            server.Start();
            provider.Start();

            // Wait 5 seconds for discovery
            Thread.Sleep(5000);

            // Stop components
            client.Stop();
            server.Stop();
            provider.Stop();

            // Assert we found the test server
            Assert.IsTrue(found.ContainsValue("TestServer"));
        }
    }
}