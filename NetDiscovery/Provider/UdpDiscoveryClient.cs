using System;
using NetDiscovery.Client;

namespace NetDiscovery.Provider
{
    internal class UdpDiscoveryClient : IDiscoveryClient
    {
        internal UdpDiscoveryClient(UdpDiscoveryProvider provider)
        {
            Provider = provider;

            Provider.UdpSendQuery += ProviderOnUdpSendQuery;
            Provider.UdpReceive += ProviderOnUdpReceive;
        }

        public event EventHandler<DiscoveredServerEventArgs> DiscoveredServer;

        internal UdpDiscoveryProvider Provider { get; }

        internal bool Running { get; set; }

        public void Dispose()
        {
            Stop();
            Provider.UdpSendQuery -= ProviderOnUdpSendQuery;
            Provider.UdpReceive -= ProviderOnUdpReceive;
        }

        public void Start()
        {
            Running = true;
        }

        public void Stop()
        {
            Running = false;
        }
        private void ProviderOnUdpSendQuery(object sender, EventArgs e)
        {
            if (Running)
                Provider.UdpSend("?");
        }

        private void ProviderOnUdpReceive(object sender, UdpReceiveEventArgs e)
        {
            if (Running && e.Message != "?")
                DiscoveredServer?.Invoke(
                    this, 
                    new DiscoveredServerEventArgs(
                        e.EndPoint.Address,
                        e.Message));
        }
    }
}
