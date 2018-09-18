using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Vostok.ClusterClient.Core.Model;
using Vostok.Logging.Abstractions;

namespace Vostok.ClusterClient.Transport.Sockets
{
    internal class RequestByteArrayContent : HttpContent
    {
        private readonly Request request;
        private readonly ILog log;
        private readonly CancellationToken cancellationToken;

        public RequestByteArrayContent(Request request, ILog log, CancellationToken cancellationToken)
        {
            this.request = request;
            this.log = log;
            this.cancellationToken = cancellationToken;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var content = request.Content;
            
            try
            {
                await stream.WriteAsync(content.Buffer, content.Offset, content.Length, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                LogSendBodyFailure(request.Url, e);
            }
            
        }

        protected override bool TryComputeLength(out long length)
        {
            length = request.Content.Length;
            return true;
        }

        private void LogSendBodyFailure(Uri uri, Exception error)
        {
            log.Error(error, "Error in sending request body to " + uri.Authority);
        }

    }
}