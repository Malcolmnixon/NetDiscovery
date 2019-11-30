using System;

namespace NetDiscovery.Udp
{
    /// <summary>
    /// UDP client class
    /// </summary>
    internal sealed class UdpClient : IClient
    {
        /// <summary>
        /// Initializes a new instance of the UdpClient class
        /// </summary>
        /// <param name="provider">Parent provider</param>
        public UdpClient(UdpProvider provider)
        {
            Provider = provider;
        }

        /// <summary>
        /// Gets the provider
        /// </summary>
        public UdpProvider Provider { get; }

        /// <summary>
        /// Gets or sets whether the client is active
        /// </summary>
        public bool Active { get; private set; }

        /// <summary>
        /// Discovery event
        /// </summary>
        public event EventHandler<DiscoveryEventArgs> Discovery;

        /// <summary>
        /// Dispose of this UDP client
        /// </summary>
        public void Dispose()
        {
            Active = false;
            Provider.Dispose(this);
        }

        /// <summary>
        /// Start the client
        /// </summary>
        public void Start()
        {
            Active = true;
        }

        /// <summary>
        /// Stop the client
        /// </summary>
        public void Stop()
        {
            Active = false;
        }

        /// <summary>
        /// Invoke the discovery event
        /// </summary>
        /// <param name="args">Discovery event arguments</param>
        public void InvokeDiscovery(DiscoveryEventArgs args)
        {
            Discovery?.Invoke(this, args);
        }
    }
}
