using System;

namespace NetDiscovery
{
    /// <summary>
    /// Discovery client interface
    /// </summary>
    public interface IClient : IDisposable
    {
        /// <summary>
        /// Discovery event
        /// </summary>
        event EventHandler<DiscoveryEventArgs> Discovery;

        /// <summary>
        /// Start the client
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the client
        /// </summary>
        void Stop();
    }
}