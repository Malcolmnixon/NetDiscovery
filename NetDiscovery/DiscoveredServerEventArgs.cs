using System;
using System.Net;

namespace NetDiscovery
{
    /// <summary>
    /// Discovered Server event arguments
    /// </summary>
    public class DiscoveredServerEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the DiscoveredServerEventArgs
        /// </summary>
        /// <param name="address">Server address</param>
        /// <param name="identity">Server identity</param>
        public DiscoveredServerEventArgs(IPAddress address, string identity)
        {
            Address = address;
            Identity = identity;
        }

        /// <summary>
        /// Gets the server address
        /// </summary>
        public IPAddress Address { get; }

        /// <summary>
        /// Gets the server identity
        /// </summary>
        public string Identity { get; }
    }
}