using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetDiscovery.Udp
{
    /// <summary>
    /// UDP discovery provider
    /// </summary>
    public class UdpDiscoveryProvider : IDiscoveryProvider
    {
        /// <summary>
        /// Interface scan period in milliseconds
        /// </summary>
        private const int InterfaceScanPeriodMs = 5000;

        /// <summary>
        /// Send query period in milliseconds
        /// </summary>
        private const int QueryDiscoveryPeriodMs = 3000;

        /// <summary>
        /// Socket read period in milliseconds
        /// </summary>
        private const int SocketReadPeriodMs = 1000;

        /// <summary>
        /// Lock object
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// Dictionary of active sockets
        /// </summary>
        private readonly Dictionary<IPAddress, Socket> _sockets = new Dictionary<IPAddress, Socket>();

        /// <summary>
        /// IPv4 broadcast end-point
        /// </summary>
        private readonly IPEndPoint _broadcastEndPoint;

        /// <summary>
        /// IPv6 link-local end-point
        /// </summary>
        private readonly IPEndPoint _linkLocalEndPoint;

        /// <summary>
        /// Discovery cancellation token source
        /// </summary>
        private CancellationTokenSource _discoveryCancel;

        /// <summary>
        /// Discovery thread
        /// </summary>
        private Thread _discoveryThread;

        /// <summary>
        /// Initializes a new instance of the UdpDiscoveryProvider
        /// </summary>
        /// <param name="port">UDP port number</param>
        public UdpDiscoveryProvider(int port)
        {
            _broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, port);
            _linkLocalEndPoint = new IPEndPoint(IPAddress.Parse("ff02::1"), port);
            Port = port;
        }

        /// <summary>
        /// Discovery port
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Send Query event
        /// </summary>
        internal event EventHandler UdpSendQuery;

        /// <summary>
        /// Receive event
        /// </summary>
        internal event EventHandler<UdpReceiveEventArgs> UdpReceive;

        public void Dispose()
        {
            Stop();
        }

        public IDiscoveryClient CreateClient()
        {
            return new UdpDiscoveryClient(this);
        }

        public IDiscoveryServer CreateServer()
        {
            return new UdpDiscoveryServer(this);
        }

        /// <summary>
        /// Start discovery provider
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                // If already started then do nothing
                if (_discoveryThread != null)
                    return;

                // Start the discovery server
                _discoveryCancel = new CancellationTokenSource();
                _discoveryThread = new Thread(DiscoveryThread);
                _discoveryThread.Start();
            }
        }

        /// <summary>
        /// Stop discovery provider
        /// </summary>
        public void Stop()
        {
            lock (_lock)
            {
                // If already stopped then do nothing
                if (_discoveryThread == null)
                    return;

                _discoveryCancel.Cancel();
                _discoveryThread.Join();
                _discoveryCancel = null;
                _discoveryThread = null;
            }
        }

        /// <summary>
        /// Send UDP message
        /// </summary>
        /// <param name="message">Message to send</param>
        internal void UdpSend(string message)
        {
            var messageBytes = Encoding.ASCII.GetBytes(message);

            foreach (var socket in _sockets.Values)
                if (socket.AddressFamily == AddressFamily.InterNetwork)
                    socket.SendTo(messageBytes, _broadcastEndPoint);
                else if (socket.AddressFamily == AddressFamily.InterNetworkV6)
                    socket.SendTo(messageBytes, _linkLocalEndPoint);
        }


        /// <summary>
        /// Discovery thread
        /// </summary>
        private void DiscoveryThread()
        {
            // Stopwatch for timing of periodic discovery actions
            var stopwatch = Stopwatch.StartNew();

            // Timeout for updating interfaces
            var updateInterfaceTimeoutMs = 0L;

            // Timeout for sending query
            var sendQueryTimeoutMs = 0L;

            try
            {
                // Loop until asked to quit
                while (!_discoveryCancel.IsCancellationRequested)
                {
                    // Get the elapsed milliseconds
                    var elapsedMs = stopwatch.ElapsedMilliseconds;

                    // Periodically scan for new network interfaces
                    if (elapsedMs >= updateInterfaceTimeoutMs)
                    {
                        // Schedule next update of interfaces
                        updateInterfaceTimeoutMs = elapsedMs + InterfaceScanPeriodMs;

                        // Get the list of distinct addresses
                        var addresses = NetworkInterface
                            .GetAllNetworkInterfaces()
                            .Where(i => i.OperationalStatus == OperationalStatus.Up)
                            .SelectMany(nic => nic.GetIPProperties()
                                .UnicastAddresses.Select(a => a.Address))
                            .Distinct()
                            .ToList();

                        // Remove sockets associated with missing addresses
                        foreach (var address in _sockets.Keys.Where(a => !addresses.Contains(a)))
                        {
                            _sockets[address].Dispose();
                            _sockets.Remove(address);
                        }

                        // Add sockets for new addresses
                        foreach (var address in addresses.Where(a => !_sockets.Keys.Contains(a)))
                        {
                            try
                            {
                                // Create the socket
                                var socket = new Socket(address.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
                                {
                                    EnableBroadcast = true,
                                    ExclusiveAddressUse = false,
                                    ReceiveTimeout = SocketReadPeriodMs
                                };

                                // Allow for reuse
                                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

                                // For IPv6 join the link-local scope for all nodes
                                if (address.AddressFamily == AddressFamily.InterNetworkV6)
                                {
                                    socket.SetSocketOption(
                                        SocketOptionLevel.IPv6,
                                        SocketOptionName.AddMembership,
                                        new IPv6MulticastOption(
                                            _linkLocalEndPoint.Address, address.ScopeId));
                                }

                                // Bind to address
                                socket.Bind(new IPEndPoint(address, Port));

                                // Save socket
                                _sockets[address] = socket;
                            }
                            catch (SocketException)
                            {
                                // Can happen in firewall situations
                            }
                        }
                    }

                    // If no sockets then wait and again
                    if (_sockets.Count == 0)
                    {
                        _discoveryCancel.Token.WaitHandle.WaitOne(InterfaceScanPeriodMs);
                        continue;
                    }

                    // Periodically send query broadcasts
                    if (elapsedMs >= sendQueryTimeoutMs)
                    {
                        sendQueryTimeoutMs = elapsedMs + QueryDiscoveryPeriodMs;
                        UdpSendQuery?.Invoke(this, EventArgs.Empty);
                    }

                    // Wait for incoming requests
                    var readSockets = _sockets.Values.ToList();
                    var errorSockets = _sockets.Values.ToList();
                    Socket.Select(readSockets, null, errorSockets, SocketReadPeriodMs * 1000);

                    // Process all read requests
                    foreach (var socket in readSockets)
                    {
                        // Read from the socket
                        var buffer = new byte[1024];
                        EndPoint remoteEp;
                        if (socket.AddressFamily == AddressFamily.InterNetwork)
                            remoteEp = new IPEndPoint(_broadcastEndPoint.Address, 0);
                        else
                            remoteEp = new IPEndPoint(_linkLocalEndPoint.Address, 0);
                        var len = socket.ReceiveFrom(buffer, ref remoteEp);

                        // Verify we got a query response
                        var message = Encoding.ASCII.GetString(buffer, 0, len);

                        // Dispatch received
                        UdpReceive?.Invoke(
                            this,
                            new UdpReceiveEventArgs(
                                (IPEndPoint)remoteEp,
                                socket,
                                message));
                    }
                }
            }
            finally
            {
                // Dispose of all sockets
                foreach (var socket in _sockets.Values)
                {
                    if (socket.AddressFamily == AddressFamily.InterNetworkV6)
                        socket.SetSocketOption(
                            SocketOptionLevel.IPv6,
                            SocketOptionName.DropMembership,
                            new IPv6MulticastOption(_linkLocalEndPoint.Address)); 
                    socket.Dispose();
                }

                // Clear the sockets
                _sockets.Clear();
            }
        }
    }
}
