using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;
using Vostok.ClusterClient.Transport.Webrequest.Pool;
using Vostok.Logging.Abstractions;

namespace Vostok.ClusterClient.Transport.Sockets.Contents
{
    internal class RequestByteArrayContent : ClusterClientHttpContent
    {
        private readonly Request request;
        private readonly IPool<byte[]> pool;
        private readonly CancellationToken cancellationToken;

        public RequestByteArrayContent(
            Request request,
            SendContext context,
            IPool<byte[]> pool,
            ILog log,
            CancellationToken cancellationToken) : base(context, log)
        {
            this.request = request;
            this.pool = pool;
            this.cancellationToken = cancellationToken;

            Headers.ContentLength = request.Content.Length;
        }

        protected override async Task SerializeAsync(Stream stream, TransportContext context)
        {
            var content = request.Content;
            
            try
            {
                // (epeshk): avoid storing large buffers in Socket private fields.
                if (content.Buffer.Length < SocketsTransportConstants.LOHObjectSizeThreshold)
                {
                    await stream.WriteAsync(content.Buffer, content.Offset, content.Length, cancellationToken).ConfigureAwait(false);
                    return;
                }

                using (pool.AcquireHandle(out var buffer))
                {
                    var index = content.Offset;
                    var end = content.Offset + content.Length;
                    while (index < end)
                    {
                        var size = Math.Min(SocketsTransportConstants.PooledBufferSize, end - index);
                        Buffer.BlockCopy(content.Buffer, index, buffer, 0, size);
                        await stream.WriteAsync(buffer, 0, size, cancellationToken).ConfigureAwait(false);
                        index += size;
                    }
                }
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