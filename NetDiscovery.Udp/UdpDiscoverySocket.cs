using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NetDiscovery.Udp
{
    internal abstract class UdpDiscoverySocket : IDisposable
    {
        public IPAddress Address { get; protected set; }

        public Socket Socket { get; protected set; }

        public static UdpDiscoverySocket Create(IPAddress address, int port)
        {
            try
            {
                // Create for IPv4 addresses
                if (address.AddressFamily == AddressFamily.InterNetwork)
                    return new UdpDiscoverySocketV4(address, port);

                // Create for IPv6 addresses
                if (address.AddressFamily == AddressFamily.InterNetworkV6)
                    return new UdpDiscoverySocketV6(address, port);
            }
            catch (SocketException ex)
            {
                System.Console.WriteLine($"Socket Error : {ex.Message}");
            }

            // Unsupported or error
            return null;
        }

        public abstract void Broadcast(byte[] message);

        public void Receive(out string message, out IPEndPoint sender)
        {
            var buffer = new byte[1024];
            EndPoint ep = new IPEndPoint(Address, 0);
            var len = Socket.ReceiveFrom(buffer, ref ep);

            message = Encoding.ASCII.GetString(buffer, 0, len);
            sender = (IPEndPoint)ep;
        }

        public abstract void Dispose();
    }
}
