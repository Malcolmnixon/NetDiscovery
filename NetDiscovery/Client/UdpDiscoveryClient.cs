using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NetDiscovery.Client
{
    /// <summary>
    /// UDP implementation of discovery client
    /// </summary>
    public class UdpDiscoveryClient : IDiscoveryClient
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
        /// Discovery cancellation token source
        /// </summary>
        private CancellationTokenSource _discoveryCancel;

        /// <summary>
        /// Discovery thread
        /// </summary>
        private Thread _discoveryThread;

        /// <summary>
        /// Initializes a new instance of the UdpDiscoveryClient
        /// </summary>
        /// <param name="port">UDP port number</param>
        public UdpDiscoveryClient(int port)
        {
            Port = port;
        }

        /// <summary>
        /// Discovery port
        /// </summary>
        public int Port { get; }

        /// <summary>
        /// Discovered Server event
        /// </summary>
        public event EventHandler<DiscoveredServerEventArgs> DiscoveredServer;

        /// <summary>
        /// Disposes of this objects resources
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Start discovery client
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
        /// Stop discovery client
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
        /// Discovery thread
        /// </summary>
        private void DiscoveryThread()
        {
            // Endpoint and message for broadcasting query
            var queryEndPoint = new IPEndPoint(IPAddress.Broadcast, Port);
            var queryMessage = Encoding.ASCII.GetBytes("?");

            // Dictionary of sockets by address
            var sockets = new Dictionary<IPAddress, Socket>();

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
                            .Where(i => i.Supports(NetworkInterfaceComponent.IPv4))
                            .SelectMany(nic => nic.GetIPProperties()
                                .UnicastAddresses.Select(a => a.Address)
                                .Where(a => a.AddressFamily == AddressFamily.InterNetwork))
                            .Distinct()
                            .ToList();

                        // Remove sockets associated with missing addresses
                        foreach (var address in sockets.Keys.Where(a => !addresses.Contains(a)))
                        {
                            sockets[address].Dispose();
                            sockets.Remove(address);
                        }

                        // Add sockets for new addresses
                        foreach (var address in addresses.Where(a => !sockets.Keys.Contains(a)))
                        {
                            var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
                            {
                                EnableBroadcast = true,
                                ExclusiveAddressUse = false,
                                ReceiveTimeout = SocketReadPeriodMs
                            };
                            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                            socket.Bind(new IPEndPoint(address, Port));
                            sockets[address] = socket;
                        }
                    }

                    // If no sockets then wait and again
                    if (sockets.Count == 0)
                    {
                        _discoveryCancel.Token.WaitHandle.WaitOne(InterfaceScanPeriodMs);
                        continue;
                    }

                    // Periodically send query broadcasts
                    if (elapsedMs >= sendQueryTimeoutMs)
                    {
                        sendQueryTimeoutMs = elapsedMs + QueryDiscoveryPeriodMs;
                        foreach (var socket in sockets.Values)
                            socket.SendTo(queryMessage, queryEndPoint);
                    }

                    // Wait for incoming requests
                    var readSockets = sockets.Values.ToList();
                    var errorSockets = sockets.Values.ToList();
                    Socket.Select(readSockets, null, errorSockets, SocketReadPeriodMs * 1000);

                    // Process all read requests
                    foreach (var socket in readSockets)
                    {
                        // Read from the socket
                        var buffer = new byte[1024];
                        EndPoint remoteEp = new IPEndPoint(0, 0);
                        var len = socket.ReceiveFrom(buffer, ref remoteEp);

                        // Verify we got a query response
                        var query = Encoding.ASCII.GetString(buffer, 0, len);
                        if (query == "?")
                            continue;

                        // Report the discovered server
                        //Trace.WriteLine($"Discovered server {query} at {remoteEp}");
                        DiscoveredServer?.Invoke(
                            this,
                            new DiscoveredServerEventArgs(
                                ((IPEndPoint) remoteEp).Address,
                                query));
                    }
                }
            }
            finally
            {
                // Dispose of all sockets
                foreach (var socket in sockets.Values)
                    socket.Dispose();
            }
        }
    }
}