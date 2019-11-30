using System.Net;

namespace NetDiscovery
{
    /// <summary>
    /// Discovery event arguments
    /// </summary>
    public sealed class DiscoveryEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the DiscoveryEventArgs class
        /// </summary>
        /// <param name="address">Server address</param>
        /// <param name="identity">Server identity</param>
        public DiscoveryEventArgs(IPAddress address, string identity)
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