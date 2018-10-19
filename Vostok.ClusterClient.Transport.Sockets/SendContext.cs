using System.Net.Sockets;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Transport.Sockets
{
    internal class SendContext
    {
        public Socket Socket;
        public Response Response;
    }
}