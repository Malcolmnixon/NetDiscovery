using System;

namespace NetDiscovery
{
    /// <summary>
    /// Discovery server interface
    /// </summary>
    public interface IServer : IDisposable
    {
        /// <summary>
        /// Gets or sets the server identity
        /// </summary>
        string Identity { get; set; }

        /// <summary>
        /// Start the server
        /// </summary>
        void Start();

        /// <summary>
        /// Stop the server
        /// </summary>
        void Stop();
    }
}