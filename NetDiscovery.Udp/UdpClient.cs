using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetDiscovery.Udp
{
    /// <summary>
    /// UDP client class
    /// </summary>
    internal sealed class UdpClient : IClient
    {
        /// <summary>
        /// Lock object
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// UDP port
        /// </summary>
        private readonly int _port;

        /// <summary>
        /// Cancellation token source
        /// </summary>
        private CancellationTokenSource _cancel;

        /// <summary>
        /// Worker thread
        /// </summary>
        private Thread _thread;

        /// <summary>
        /// Initializes a new instance of the UdpClient class
        /// </summary>
        /// <param name="port">UDP port</param>
        public UdpClient(int port)
        {
            _port = port;
        }

        /// <summary>
        /// Discovery event
        /// </summary>
        public event EventHandler<DiscoveryEventArgs> Discovery;

        /// <summary>
        /// Dispose of this UDP client
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Start the client
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                // Skip if started
                if (_thread != null)
                    return;

                // Start discovery
                _cancel = new CancellationTokenSource();
                _thread = new Thread(DiscoveryClient);
                _thread.Start();
            }
        }

        /// <summary>
        /// Stop the client
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                // Skip if stopped
                if (_thread == null)
                    return;

                // Stop discovery
                _cancel.Cancel();
                _thread.Join();
                _cancel = null;
                _thread = null;
            }
        }

        /// <summary>
        /// Discovery client thread procedure
        /// </summary>
        private void DiscoveryClient()
        {
            var sockets = new List<Socket>();

            // Detect if IPv4 is supported
            if (Socket.OSSupportsIPv4)
            {
                // Create the IPv4 socket
                var socketV4 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
                {
                    EnableBroadcast = true,
                    ExclusiveAddressUse = false
                };

                // Allow address reuse
                socketV4.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // Bind to the port
                socketV4.Bind(new IPEndPoint(IPAddress.Any, _port));

                // Add to the list of sockets
                sockets.Add(socketV4);
            }

            // Detect if IPv6 is supported
            if (Socket.OSSupportsIPv6)
            {
                // Create the IPv6 socket
                var socketV6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp)
                {
                    EnableBroadcast = true,
                    ExclusiveAddressUse = false
                };

                // Allow both sockets to reuse addresses
                socketV6.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                // Join the IPv6 socket to the local-link group
                socketV6.SetSocketOption(
                    SocketOptionLevel.IPv6,
                    SocketOptionName.AddMembership,
                    new IPv6MulticastOption(IPAddress.Parse("ff02::1")));

                // Bind to the port
                socketV6.Bind(new IPEndPoint(IPAddress.IPv6Any, _port));

                // Add to the list of sockets
                sockets.Add(socketV6);
            }

            // If no listener sockets then IP networking is unsupported
            if (sockets.Count == 0)
                return;

            // Read buffer
            var buffer = new byte[1024];

            // Loop until cancelled
            while (!_cancel.IsCancellationRequested)
            {
                // Wait for incoming data
                var checkRead = sockets.ToList();
                var checkError = new List<Socket>();
                Socket.Select(checkRead, null, checkError, 1000000);

                // Process all sockets with incoming data
                foreach (var socket in checkRead)
                {
                    // Read the next packet
                    var ep = socket.LocalEndPoint;
                    var len = socket.ReceiveFrom(buffer, ref ep);

                    // Get the address and identity
                    var address = ((IPEndPoint) ep).Address;
                    var identity = Encoding.ASCII.GetString(buffer, 0, len);

                    // Report the discovery
                    Discovery?.Invoke(this, new DiscoveryEventArgs(address, identity));
                }
            }

            // Dispose of the sockets
            foreach (var socket in sockets)
                socket.Dispose();
        }
    }
}
