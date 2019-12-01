using System.Net;
using System.Net.Sockets;

namespace NetDiscovery.Udp
{
    internal sealed class UdpDiscoverySocketV4 : UdpDiscoverySocket
    {
        private readonly IPEndPoint _broadcastEndPoint;

        public UdpDiscoverySocketV4(IPAddress address, int port)
        {
            // Create broadcast end-point
            _broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, port);

            // Save the address
            Address = address;

            // Create the socket
            Socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
            {
                EnableBroadcast = true,
                ExclusiveAddressUse = false,
                ReceiveTimeout = 1000
            };

            // Allow address reuse
            Socket.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.ReuseAddress,
                true);

            // Bind to the end-point
            Socket.Bind(new IPEndPoint(Address, port));
        }

        public override void Broadcast(byte[] message)
        {
            try
            {
                // Send to the broadcast address
                Socket.SendTo(message, _broadcastEndPoint);
            }
            catch (SocketException ex)
            {
                System.Console.WriteLine($"Socket Error : {ex.Message}");
            }
        }

        public override void Dispose()
        {
            // Skip if disposed
            if (Socket == null)
                return;

            // Dispose of socket
            Socket.Dispose();
            Socket = null;
        }
    }
}
