using System;

namespace NetDiscovery
{
    /// <summary>
    /// Discovery client interface
    /// </summary>
    public interface IDiscoveryClient : IDisposable
    {
        /// <summary>
        /// Discovered Server event
        /// </summary>
        event EventHandler<DiscoveredServerEventArgs> DiscoveredServer;

        /// <summary>
        /// Start discovery client
        /// </summary>
        void Start();

        /// <summary>
        /// Stop discovery client
        /// </summary>
        void Stop();
    }
}