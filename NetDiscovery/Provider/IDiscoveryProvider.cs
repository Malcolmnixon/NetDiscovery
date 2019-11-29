using System;
using System.Collections.Generic;
using System.Text;
using NetDiscovery.Client;
using NetDiscovery.Server;

namespace NetDiscovery.Provider
{
    public interface IDiscoveryProvider : IDisposable
    {
        /// <summary>
        /// Create discovery client
        /// </summary>
        /// <returns></returns>
        IDiscoveryClient CreateClient();

        /// <summary>
        /// Create discovery server
        /// </summary>
        /// <returns></returns>
        IDiscoveryServer CreateServer();

        /// <summary>
        /// Start discovery provider
        /// </summary>
        void Start();

        /// <summary>
        /// Stop discovery provider
        /// </summary>
        void Stop();
    }
}
