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
    internal abstract class ClusterClientHttpContent : HttpContent
    {
        protected readonly SendContext SendContext;
        protected readonly ILog Log;

        protected ClusterClientHttpContent(SendContext sendContext, ILog log)
        {
            this.SendContext = sendContext;
            Log = log;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            SendContext.Socket = SocketAccessor.GetSocket(stream, Log);
            return SerializeAsync(stream, context);
        }

        protected abstract Task SerializeAsync(Stream stream, TransportContext context);
    }

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
    
    internal class RequestByteArrayContent : ClusterClientHttpContent
    {
        private readonly Request request;
        private readonly CancellationToken cancellationToken;

        public RequestByteArrayContent(Request request, SendContext context, ILog log, CancellationToken cancellationToken) : base(context, log)
        {
            this.request = request;
            this.cancellationToken = cancellationToken;

            Headers.ContentLength = request.Content.Length;
        }

        protected override async Task SerializeAsync(Stream stream, TransportContext context)
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
            Log.Error(error, "Error in sending request body to " + uri.Authority);
        }
    }
}