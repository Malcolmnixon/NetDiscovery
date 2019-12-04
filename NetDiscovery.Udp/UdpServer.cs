using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetDiscovery.Udp
{
    /// <summary>
    /// UDP server class
    /// </summary>
    internal sealed class UdpServer : IServer
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
        /// Initializes a new instance of the UdpServer class
        /// </summary>
        /// <param name="port">UDP port</param>
        public UdpServer(int port)
        {
            _port = port;
        }

        /// <summary>
        /// Gets or sets the server identity
        /// </summary>
        public string Identity { get; set; }

        /// <summary>
        /// Disposes of this UDP server
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Start the server
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
                _thread = new Thread(DiscoveryServer);
                _thread.Start();
            }
        }

        /// <summary>
        /// Stop the server
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
        /// Discovery server thread procedure
        /// </summary>
        private void DiscoveryServer()
        {
            // Dictionary of sockets by address
            var sockets = new Dictionary<IPAddress, Socket>();

            // Create link-local address
            var linkLocalV6 = IPAddress.Parse("ff02::1");

            // Create end-points
            var endPointV4 = new IPEndPoint(IPAddress.Broadcast, _port);
            var endPointV6 = new IPEndPoint(linkLocalV6, _port);

            // Loop until asked to cancel
            while (true)
            {
                // Wait for 3 seconds or a cancel request
                if (_cancel.Token.WaitHandle.WaitOne(3000))
                    break;

                // Get the addresses of all interfaces
                var addresses = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(nic => nic.OperationalStatus == OperationalStatus.Up ||
                                  nic.OperationalStatus == OperationalStatus.Unknown)
                    .SelectMany(nic => nic.GetIPProperties().UnicastAddresses
                        .Select(a => a.Address))
                    .ToList();

                // Find any addresses that have been added or removed
                var addedAddresses = addresses.Where(a => sockets.Keys.All(k => !k.Equals(a))).ToList();
                var removedAddresses = sockets.Keys.Where(k => addresses.All(a => !a.Equals(k))).ToList();

                // Discard sockets for removed addresses
                foreach (var address in removedAddresses)
                {
                    sockets[address].Dispose();
                    sockets.Remove(address);
                }

                // Add sockets for new IPv4 addresses
                foreach (var address in addedAddresses.Where(a => a.AddressFamily == AddressFamily.InterNetwork))
                {
                    // Create the socket
                    var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
                    {
                        EnableBroadcast = true,
                        ExclusiveAddressUse = false
                    };

                    // Allow address reuse
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                    // Bind to the address
                    socket.Bind(new IPEndPoint(address, _port));

                    // Save the socket
                    sockets[address] = socket;
                }

                // Add sockets for new IPv6 addresses
                foreach (var address in addedAddresses.Where(a => a.AddressFamily == AddressFamily.InterNetworkV6))
                {
                    // Create the socket
                    var socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp)
                    {
                        EnableBroadcast = true,
                        ExclusiveAddressUse = false
                    };

                    // Allow address reuse
                    socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                    // Join the link-local multi-cast group
                    socket.SetSocketOption(
                        SocketOptionLevel.IPv6,
                        SocketOptionName.AddMembership,
                        new IPv6MulticastOption(linkLocalV6));

                    // Bind to the address
                    socket.Bind(new IPEndPoint(address, _port));

                    // Save the socket
                    sockets[address] = socket;
                }

                // Get the identity bytes
                var identityBytes = Encoding.ASCII.GetBytes(Identity);

                // Send over IPv4 sockets
                foreach (var socket in sockets.Values.Where(s => s.AddressFamily == AddressFamily.InterNetwork))
                {
                    try
                    {
                        socket.SendTo(identityBytes, endPointV4);
                    }
                    catch (SocketException)
                    {
                        // Unable to send, ignore
                    }
                }

                // Send over IPv6 sockets
                foreach (var socket in sockets.Values.Where(s => s.AddressFamily == AddressFamily.InterNetworkV6))
                {
                    try
                    {
                        socket.SendTo(identityBytes, endPointV6);
                    }
                    catch (SocketException)
                    {
                        // Unable to send, ignore
                    }
                }
            }

            // Dispose of the sockets
            foreach (var socket in sockets.Values)
                socket.Dispose();
        }
    }
}
