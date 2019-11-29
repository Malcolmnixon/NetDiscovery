using System;

namespace NetDiscovery
{
    /// <summary>
    /// Discovery server interface
    /// </summary>
    public interface IDiscoveryServer : IDisposable
    {
        /// <summary>
        /// Gets or sets the identity to announce
        /// </summary>
        string Identity { get; set; }

        /// <summary>
        /// Start discovery server
        /// </summary>
        void Start();

        /// <summary>
        /// Stop discovery server
        /// </summary>
        void Stop();
    }
}