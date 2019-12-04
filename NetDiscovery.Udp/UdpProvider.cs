namespace NetDiscovery.Udp
{
    /// <summary>
    /// UDP discovery provider
    /// </summary>
    public sealed class UdpProvider : IProvider
    {
        /// <summary>
        /// Discovery port
        /// </summary>
        private readonly int _port;

        /// <summary>
        /// Initializes a new instance of the UdpProvider
        /// </summary>
        /// <param name="port">Discovery port</param>
        public UdpProvider(int port)
        {
            _port = port;
        }

        /// <summary>
        /// Dispose of this provider
        /// </summary>
        public void Dispose()
        {
        }

        /// <summary>
        /// Create discovery client
        /// </summary>
        /// <returns>New discovery client</returns>
        public IClient CreateClient()
        {
            return new UdpClient(_port);
        }

        /// <summary>
        /// Create discovery server
        /// </summary>
        /// <returns>New discovery server</returns>
        public IServer CreateServer()
        {
            return new UdpServer(_port);
        }

        /// <summary>
        /// Start this provider
        /// </summary>
        public void Start()
        {
        }

        /// <summary>
        /// Stop this provider
        /// </summary>
        public void Stop()
        {
        }
    }
}
