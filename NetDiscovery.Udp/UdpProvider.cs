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
    public sealed class UdpProvider : IProvider
    {
        /// <summary>
        /// Lock object
        /// </summary>
        private readonly object _lock = new object();

        /// <summary>
        /// Discovery port
        /// </summary>
        private readonly int _port;

        /// <summary>
        /// List of active clients
        /// </summary>
        private readonly List<UdpClient> _clients = new List<UdpClient>();

        /// <summary>
        /// List of active servers
        /// </summary>
        private readonly List<UdpServer> _servers = new List<UdpServer>();

        /// <summary>
        /// Cancellation token source
        /// </summary>
        private CancellationTokenSource _cancel;

        /// <summary>
        /// Worker thread
        /// </summary>
        private Thread _thread;

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
            Stop();
        }

        /// <summary>
        /// Create discovery client
        /// </summary>
        /// <returns>New discovery client</returns>
        public IClient CreateClient()
        {
            lock (_lock)
            {
                var client = new UdpClient(this);
                _clients.Add(client);
                return client;
            }
        }

        /// <summary>
        /// Create discovery server
        /// </summary>
        /// <returns>New discovery server</returns>
        public IServer CreateServer()
        {
            lock (_lock)
            {
                var server = new UdpServer(this);
                _servers.Add(server);
                return server;
            }
        }

        /// <summary>
        /// Start this provider
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
                _thread = new Thread(Discovery);
                _thread.Start();
            }
        }

        /// <summary>
        /// Stop this provider
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
        /// Dispose of a discovery client
        /// </summary>
        /// <param name="client">Client to dispose</param>
        internal void Dispose(UdpClient client)
        {
            lock (_lock)
            {
                _clients.Remove(client);
            }
        }

        /// <summary>
        /// Dispose of a discovery server
        /// </summary>
        /// <param name="server">Server to dispose</param>
        internal void Dispose(UdpServer server)
        {
            lock (_lock)
            {
                _servers.Remove(server);
            }
        }

        private void Discovery()
        {
            // Dictionary of discovery sockets
            var sockets = new Dictionary<IPAddress, UdpDiscoverySocket>();

            try
            {
                // Start a stopwatch
                var stopwatch = Stopwatch.StartNew();

                // Scan interfaces timeout in milliseconds
                var scanInterfacesTimeoutMs = 0L;

                // Send query timeout in milliseconds
                var sendQueryTimeoutMs = 0L;

                // Loop until asked to terminate
                while (!_cancel.IsCancellationRequested)
                {
                    // Get the elapsed milliseconds
                    var elapsedMs = stopwatch.ElapsedMilliseconds;

                    // Periodically scan for interface changes
                    if (elapsedMs >= scanInterfacesTimeoutMs)
                    {
                        // Reschedule in 10 seconds
                        scanInterfacesTimeoutMs = elapsedMs + 10000;

                        // Get the list of available interfaces
                        var addresses = NetworkInterface.GetAllNetworkInterfaces()
                            .Where(i => i.OperationalStatus == OperationalStatus.Up)
                            .SelectMany(i => i.GetIPProperties().UnicastAddresses
                                .Select(a => a.Address))
                            .ToList();

                        // Add new addresses
                        foreach (var address in addresses.Where(a => !sockets.ContainsKey(a)).ToList())
                        {
                            System.Console.WriteLine($"Self : {address}");
                            var socket = UdpDiscoverySocket.Create(address, _port);
                            if (socket != null)
                                sockets[address] = socket;
                        }

                        // Remove lost addresses
                        foreach (var address in sockets.Keys.Where(a => !addresses.Contains(a)).ToList())
                        {
                            System.Console.WriteLine($"Self Lost : {address}");
                            sockets[address].Dispose();
                            sockets.Remove(address);
                        }
                    }

                    // If we have any clients then send a query
                    if (_clients.Count > 0 && elapsedMs >= sendQueryTimeoutMs)
                    {
                        // Reschedule in 3 seconds
                        sendQueryTimeoutMs = elapsedMs + 3000;

                        // Broadcast a query message
                        var message = Encoding.ASCII.GetBytes("?");
                        foreach (var socket in sockets.Values)
                            socket.Broadcast(message);
                    }

                    // Build the socket list for read and error
                    var checkRead = sockets.Values.Select(s => s.Socket).ToList();
                    var checkError = checkRead.ToList();

                    // Wait for incoming data
                    Socket.Select(checkRead, null, checkError, 1000000);

                    // Process all sockets with data ready
                    foreach (var sock in checkRead)
                    {
                        var socket = sockets.Values.First(s => s.Socket == sock);
                        socket.Receive(out var message, out var sender);

                        // Inspect message
                        if (message == "?")
                        {
                            // Send identity of every server back to the sender
                            foreach (var identity in _servers.Select(server => Encoding.ASCII.GetBytes(server.Identity)))
                                socket.Broadcast(identity);
                        }
                        else
                        {
                            // Send announcement to all clients
                            var discovery = new DiscoveryEventArgs(sender.Address, message);
                            foreach (var client in _clients)
                                client.InvokeDiscovery(discovery);
                        }
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
