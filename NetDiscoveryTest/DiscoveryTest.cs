using System.Collections.Generic;
using System.Net;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetDiscovery.Client;
using NetDiscovery.Server;

namespace NetDiscoveryTest
{
    [TestClass]
    public class Discovery
    {
        [TestMethod]
        public void TestFindUdpServer()
        {
            // Build a discovery
            var found = new Dictionary<IPAddress, string>();

            using var server = new UdpDiscoveryServer(52148) {Identity = "TestServer"};
            server.Start();

            using var client = new UdpDiscoveryClient(52148);
            client.DiscoveredServer += (s, e) => { found[e.Address] = e.Identity; };
            client.Start();

            // Wait 5 seconds for discovery
            Thread.Sleep(5000);

            // Stop the client and server
            client.Stop();
            server.Stop();

            // Assert we found the test server
            Assert.IsTrue(found.ContainsValue("TestServer"));
        }
    }
}