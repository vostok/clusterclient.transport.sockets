using System.IO;
using System.Net;
using System.Threading.Tasks;
using Vostok.Logging.Abstractions;

namespace Vostok.ClusterClient.Transport.Sockets.Contents
{
    internal class RequestEmptyContent : ClusterClientHttpContent
    {
        public RequestEmptyContent(SendContext sendContext, ILog log)
            : base(sendContext, log) => Headers.ContentLength = 0;

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return true;
        }

        protected override Task SerializeAsync(Stream stream, TransportContext context) => Task.CompletedTask;
    }
}