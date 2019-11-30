using System;
using System.Collections.Generic;
using System.Text;

namespace NetDiscovery.Udp
{
    /// <summary>
    /// UDP server class
    /// </summary>
    internal sealed class UdpServer : IServer
    {
        /// <summary>
        /// Initializes a new instance of the UdpServer class
        /// </summary>
        /// <param name="provider">Parent provider</param>
        public UdpServer(UdpProvider provider)
        {
            Provider = provider;
        }

        /// <summary>
        /// Gets or sets the server identity
        /// </summary>
        public string Identity { get; set; }

        /// <summary>
        /// Gets the provider
        /// </summary>
        public UdpProvider Provider { get; }

        /// <summary>
        /// Gets or sets whether the server is active
        /// </summary>
        public bool Active { get; private set; }

        /// <summary>
        /// Disposes of this UDP server
        /// </summary>
        public void Dispose()
        {
            Active = false;
            Provider.Dispose(this);
        }

        /// <summary>
        /// Start the server
        /// </summary>
        public void Start()
        {
            Active = true;
        }

        /// <summary>
        /// Stop the server
        /// </summary>
        public void Stop()
        {
            Active = false;
        }
    }
}
