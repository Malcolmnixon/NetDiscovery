using System.Text;
using NetDiscovery.Server;

namespace NetDiscovery.Provider
{
    internal class UdpDiscoveryServer : IDiscoveryServer
    {
        internal UdpDiscoveryServer(UdpDiscoveryProvider provider)
        {
            Provider = provider;
            Provider.UdpReceive += ProviderOnUdpReceive;
        }

        public string Identity { get; set; }

        internal UdpDiscoveryProvider Provider { get; }

        internal bool Running { get; set; }

        public void Dispose()
        {
            Stop();
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

        private void ProviderOnUdpReceive(object sender, UdpReceiveEventArgs e)
        {
            if (Running && e.Message == "?")
                e.Socket.SendTo(Encoding.ASCII.GetBytes(Identity), e.EndPoint);
        }
    }
}
