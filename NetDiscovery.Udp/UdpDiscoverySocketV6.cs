using System;
using System.Net;
using System.Net.Sockets;

namespace NetDiscovery.Udp
{
    internal sealed class UdpDiscoverySocketV6 : UdpDiscoverySocket
    {
        private static readonly IPAddress LinkLocalAddress = IPAddress.Parse("ff02::1");

        private readonly IPEndPoint _linkLocalEndPoint;

        public UdpDiscoverySocketV6(IPAddress address, int port)
        {
            // Create link local end-point
            _linkLocalEndPoint = new IPEndPoint(LinkLocalAddress, port);

            // Save the address
            Address = address;

            // Create socket
            Socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp)
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

            // Join the link-local multi-cast group
            Socket.SetSocketOption(
                SocketOptionLevel.IPv6,
                SocketOptionName.AddMembership,
                new IPv6MulticastOption(LinkLocalAddress, Address.ScopeId));

            // Bind to the end-point
            Socket.Bind(new IPEndPoint(Address, port));
        }

        public override void Broadcast(byte[] message)
        {
            try
            {
                // Send to the link-local group
                Socket.SendTo(message, _linkLocalEndPoint);
            }
            catch (SocketException)
            {
                // TODO: Investigate network errors for unreachable network
                // throw;
            }
        }

        public override void Dispose()
        {
            // Skip if disposed
            if (Socket == null)
                return;

            // Drop link-local group membership
            Socket.SetSocketOption(
                SocketOptionLevel.IPv6,
                SocketOptionName.DropMembership,
                new IPv6MulticastOption(LinkLocalAddress, Address.ScopeId));

            // Dispose of socket
            Socket.Dispose();
            Socket = null;
        }
    }
}
