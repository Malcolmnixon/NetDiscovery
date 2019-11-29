using System.Net;
using System.Net.Sockets;

namespace NetDiscovery.Udp
{
    internal class UdpReceiveEventArgs
    {
        internal UdpReceiveEventArgs(IPEndPoint endPoint, Socket socket, string message)
        {
            EndPoint = endPoint;
            Socket = socket;
            Message = message;
        }

        internal IPEndPoint EndPoint { get; }
        
        internal Socket Socket { get; }

        internal string Message { get; }
    }
}
