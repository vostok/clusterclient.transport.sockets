using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Vostok.ClusterClient.Core.Model;
using Vostok.Commons.Collections;
using Vostok.Logging.Abstractions;

namespace Vostok.ClusterClient.Transport.Sockets
{
    internal class RequestStreamContent : HttpContent
    {
        private readonly RequestState state;
        private readonly ILog log;
        private readonly UnboundedObjectPool<byte[]> arrayPool;

        public RequestStreamContent(
            RequestState state,
            UnboundedObjectPool<byte[]> arrayPool,
            ILog log)
        {
            this.state = state;
            this.arrayPool = arrayPool;
            this.log = log;

            Headers.ContentLength = state.Request.StreamContent.Length;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var streamContent = state.Request.StreamContent;
            var bodyStream = streamContent.Stream;
            var bytesToSend = streamContent.Length ?? long.MaxValue;
            var bytesSent = 0L;

            try
            {
                using (arrayPool.Acquire(out var buffer))
                {
                    while (bytesSent < bytesToSend)
                    {
                        var bytesToRead = (int) Math.Min(buffer.Length, bytesToSend - bytesSent);

                        int bytesRead;

                        try
                        {
                            bytesRead = await bodyStream.ReadAsync(buffer, 0, bytesToRead, state.CancellationToken).ConfigureAwait(false);
                        }
                        catch (StreamAlreadyUsedException)
                        {
                            throw;
                        }
                        catch (OperationCanceledException)
                        {
                            state.Status = HttpActionStatus.RequestCanceled;
                            return;
                        }
                        catch (Exception error)
                        {
                            LogUserStreamFailure(error);

                            state.Status = HttpActionStatus.UserStreamFailure;
                            return;
                        }

                        if (bytesRead == 0)
                            break;

                        await stream.WriteAsync(buffer, 0, bytesRead, state.CancellationToken).ConfigureAwait(false);

                        bytesSent += bytesRead;
                    }
                }

            }
            catch (OperationCanceledException)
            {
                state.Status = HttpActionStatus.RequestCanceled;
            }
            catch (StreamAlreadyUsedException)
            {
                throw;
            }
            catch (Exception e)
            {
                state.Status = HttpActionStatus.SendFailure;
                LogSendBodyFailure(state.Request.Url, e);
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            var streamContent = state.Request.StreamContent;
            length = streamContent.Length ?? 0;
            return streamContent.Length != null && streamContent.Length >= 0;
        }

        private void LogSendBodyFailure(Uri uri, Exception error)
        {
            log.Error("Error in sending request body to " + uri.Authority, error);
        }

        private void LogUserStreamFailure(Exception error)
        {
            log.Error("Failure in reading input stream while sending request body.", error);
        }
    }
}