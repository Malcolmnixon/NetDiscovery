using System;

namespace NetDiscovery
{
    /// <summary>
    /// Discovery provider interface
    /// </summary>
    public interface IProvider : IDisposable
    {
        /// <summary>
        /// Create a discovery client
        /// </summary>
        /// <returns>New discovery client</returns>
        IClient CreateClient();

        /// <summary>
        /// Create a discovery server
        /// </summary>
        /// <returns>New discovery server</returns>
        IServer CreateServer();

        /// <summary>
        /// Start the provider
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the provider
        /// </summary>
        void Stop();
    }
}