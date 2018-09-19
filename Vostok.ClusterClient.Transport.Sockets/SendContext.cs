using System.Net.Sockets;
using Vostok.ClusterClient.Core.Model;

namespace Vostok.ClusterClient.Transport.Sockets
{
    internal class SendContext
    {
        public Socket Socket;
        public Response Response;
    }
}