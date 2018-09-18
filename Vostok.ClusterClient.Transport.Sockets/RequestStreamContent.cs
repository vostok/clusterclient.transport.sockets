using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Vostok.ClusterClient.Core.Model;
using Vostok.ClusterClient.Transport.Webrequest.Pool;
using Vostok.Commons.Collections;
using Vostok.Logging.Abstractions;

namespace Vostok.ClusterClient.Transport.Sockets
{
    internal class ResponseException : Exception
    {
        public Response Response { get; }

        public ResponseException(Response response)
        {
            Response = response;
        }
    }
    
    internal class RequestStreamContent : HttpContent
    {
        private readonly ILog log;
        private readonly CancellationToken cancellationToken;
        private readonly Request request;
        private readonly IPool<byte[]> arrayPool;

        public RequestStreamContent(
            Request request,
            IPool<byte[]> arrayPool,
            ILog log,
             CancellationToken cancellationToken)
        {
            this.request = request;
            this.arrayPool = arrayPool;
            this.log = log;
            this.cancellationToken = cancellationToken;

            Headers.ContentLength = request.StreamContent.Length;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            var streamContent = request.StreamContent;
            var bodyStream = streamContent.Stream;
            var bytesToSend = streamContent.Length ?? long.MaxValue;
            var bytesSent = 0L;

            try
            {
                using (arrayPool.AcquireHandle(out var buffer))
                {
                    while (bytesSent < bytesToSend)
                    {
                        var bytesToRead = (int) Math.Min(buffer.Length, bytesToSend - bytesSent);

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
                            throw new ResponseException(new Response(ResponseCode.StreamInputFailure));
                        }

                        if (bytesRead == 0)
                            break;

                        await stream.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);

                        bytesSent += bytesRead;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (StreamAlreadyUsedException)
            {
                throw;
            }
            catch (ResponseException)
            {
                throw;
            }
            catch (Exception e)
            {
                LogSendBodyFailure(request.Url, e);
                throw new ResponseException(new Response(ResponseCode.SendFailure));
            }
        }

        protected override bool TryComputeLength(out long length)
        {
            var streamContent = request.StreamContent;
            length = streamContent.Length ?? 0;
            return streamContent.Length != null && streamContent.Length >= 0;
        }

        private void LogSendBodyFailure(Uri uri, Exception error)
        {
            log.Error(error, "Error in sending request body to " + uri.Authority);
        }

        private void LogUserStreamFailure(Exception error)
        {
            log.Error(error, "Failure in reading input stream while sending request body.");
        }
    }
}