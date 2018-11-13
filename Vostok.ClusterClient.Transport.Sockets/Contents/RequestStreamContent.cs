using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport.Sockets.Pool;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Transport.Sockets.Contents
{
    internal class RequestStreamContent : ClusterClientHttpContent
    {
        private readonly CancellationToken cancellationToken;
        private readonly IPool<byte[]> arrayPool;
        private readonly IStreamContent streamContent;

        public RequestStreamContent(
            Request request,
            SendContext sendContext,
            IPool<byte[]> arrayPool,
            ILog log,
            CancellationToken cancellationToken)
            : base(request, sendContext, log)
        {
            streamContent = request.StreamContent ?? throw new ArgumentNullException(nameof(request.StreamContent), "Bug in code: StreamContent is null.");
            this.arrayPool = arrayPool;
            this.cancellationToken = cancellationToken;

            Headers.ContentLength = request.StreamContent.Length;
        }

        protected override async Task SerializeAsync(Stream stream, TransportContext context)
        {
            var bodyStream = streamContent.Stream;
            var bytesToSend = streamContent.Length ?? long.MaxValue;
            var bytesSent = 0L;

            using (arrayPool.AcquireHandle(out var buffer))
            {
                while (bytesSent < bytesToSend)
                {
                    var bytesToRead = (int)Math.Min(buffer.Length, bytesToSend - bytesSent);

                    int bytesRead;

                    try
                    {
                        bytesRead = await bodyStream.ReadAsync(buffer, 0, bytesToRead, cancellationToken).ConfigureAwait(false);
                    }
                    catch (StreamAlreadyUsedException)
                    {
                        throw;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception error)
                    {
                        LogUserStreamFailure(error);
                        SendContext.Response = new Response(ResponseCode.StreamInputFailure);
                        return;
                    }

                    if (bytesRead == 0)
                        break;

                    await stream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);

                    bytesSent += bytesRead;
                }
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            length = streamContent.Length ?? 0;
            return streamContent.Length != null;
        }

        private void LogUserStreamFailure(Exception error)
        {
            Log.Warn(error, "Failure in reading input stream while sending request body.");
        }
    }
}