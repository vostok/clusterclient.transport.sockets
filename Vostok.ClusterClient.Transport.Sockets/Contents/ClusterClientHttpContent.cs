using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Transport.Sockets.Contents
{
    internal abstract class ClusterClientHttpContent : HttpContent
    {
        protected readonly SendContext SendContext;
        protected readonly ILog Log;

        protected ClusterClientHttpContent(SendContext sendContext, ILog log)
        {
            SendContext = sendContext;
            Log = log;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            SendContext.Socket = SocketAccessor.GetSocket(stream, Log);
            return SerializeAsync(stream, context);
        }

        protected abstract Task SerializeAsync(Stream stream, TransportContext context);
    }
}