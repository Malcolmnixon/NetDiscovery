using System;

namespace NetDiscovery.Udp
{
    /// <summary>
    /// UDP implementation for discovery client
    /// </summary>
    internal class UdpDiscoveryClient : IDiscoveryClient
    {
        /// <summary>
        /// Initializes an instance of the UdpDiscoveryClient class
        /// </summary>
        /// <param name="provider">UDP discovery provider</param>
        internal UdpDiscoveryClient(UdpDiscoveryProvider provider)
        {
            Provider = provider;

            Provider.UdpSendQuery += ProviderOnUdpSendQuery;
            Provider.UdpReceive += ProviderOnUdpReceive;
        }

        /// <summary>
        /// Discovered server event
        /// </summary>
        public event EventHandler<DiscoveredServerEventArgs> DiscoveredServer;

        /// <summary>
        /// Gets the UDP discovery provider
        /// </summary>
        internal UdpDiscoveryProvider Provider { get; }

        /// <summary>
        /// Running flag
        /// </summary>
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
