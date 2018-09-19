using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Vostok.ClusterClient.Core.Model;
using Vostok.ClusterClient.Core.Transport;
using Vostok.ClusterClient.Transport.Webrequest.Pool;
using Vostok.Commons.Helpers.Extensions;
using Vostok.Logging.Abstractions;

namespace Vostok.ClusterClient.Transport.Sockets
{
    public class SocketsTransport : ITransport, IDisposable
    {
        private const int BufferSize = 16*1024;
        private const int PreferredReadSize = 16*1024;
        private const int LOHObjectSizeThreshold = 85*1000;
        
        private readonly SocketsTransportSettings settings;
        private readonly ILog log;
        private readonly IPool<byte[]> pool;
        private readonly HttpClient client;
        private readonly HttpRequestMessageFactory requestFactory;

        /// <inheritdoc />
        public TransportCapabilities Capabilities { get; } = TransportCapabilities.RequestStreaming | TransportCapabilities.ResponseStreaming;

        /// <summary>
        /// Creates ClusterClient transport for .NET Core 2.1 and later based on <see cref="SocketsHttpHandler"/>
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        public SocketsTransport(SocketsTransportSettings settings, ILog log)
        {
            settings = settings.Clone();
            
            this.settings = settings;
            this.log = log;
            this.pool = new Pool<byte[]>(() => new byte[BufferSize]);
            var handler = new SocketsHttpHandler
            {
                Proxy = settings.Proxy,
                ConnectTimeout = settings.ConnectionTimeout ?? TimeSpan.FromMinutes(2),
                UseProxy = settings.Proxy != null,
                AllowAutoRedirect = settings.AllowAutoRedirect,
                PooledConnectionIdleTimeout = settings.ConnectionIdleTimeout,
                PooledConnectionLifetime = settings.ConnectionLifetime,
                MaxConnectionsPerServer = settings.MaxConnectionsPerEndpoint,
                AutomaticDecompression = DecompressionMethods.None,
                UseCookies = false
            };

            if (settings.MaxResponseDrainSize.HasValue)
                handler.MaxResponseDrainSize = settings.MaxResponseDrainSize.Value;
            
            settings.Tune?.Invoke(handler);
            
            client = new HttpClient(handler, true);

            requestFactory = new HttpRequestMessageFactory(pool, log);
        }

        /// <inheritdoc />
        public async Task<Response> SendAsync(Request request, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return Responses.Canceled;
            
            if (timeout.TotalMilliseconds < 1)
            {
                LogRequestTimeout(request, timeout);
                return Responses.Timeout;
            }

            using (var timeoutCancellation = new CancellationTokenSource())
            using (var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var timeoutTask = Task.Delay(timeout, timeoutCancellation.Token);
                var senderTask = SendInternalAsync(request, timeout, requestCancellation);
                var completedTask = await Task.WhenAny(timeoutTask, senderTask).ConfigureAwait(false);
                if (completedTask is Task<Response> taskWithResponse)
                {
                    timeoutCancellation.Cancel();
                    return taskWithResponse.GetAwaiter().GetResult();
                }

                // completedTask is timeout Task
                requestCancellation.Cancel();
                LogRequestTimeout(request, timeout);

                // wait for cancellation & dispose resources associated with Response object
                var senderTaskContinuation = senderTask.ContinueWith(
                    t =>
                    {
                        if (t.IsCompleted)
                            t.GetAwaiter().GetResult().Dispose();
                    });

                using (var abortCancellation = new CancellationTokenSource())
                {
                    var abortWaitingDelay = Task.Delay(settings.RequestAbortTimeout, abortCancellation.Token);

                    await Task.WhenAny(senderTaskContinuation, abortWaitingDelay).ConfigureAwait(false);
                    
                    abortCancellation.Cancel();
                }

                if (!senderTask.IsCompleted)
                    LogFailedToWaitForRequestAbort();

                return Responses.Timeout;
            }
        }

        private async Task<Response> SendInternalAsync(Request request, TimeSpan timeout, CancellationTokenSource cancellationTokenSource)
        {
            var sw = Stopwatch.StartNew();
            
            for (var i = 0; i < settings.ConnectionAttempts; i++)
            {
                if (cancellationTokenSource.IsCancellationRequested)
                    return Responses.Canceled;
                
                var attemptTimeout = timeout - sw.Elapsed;
                if (attemptTimeout.TotalMilliseconds < 1)
                {
                    LogRequestTimeout(request, timeout);
                    return new Response(ResponseCode.RequestTimeout);
                }

                try
                {
                    try
                    {
                        var response = await SendOnceAsync(request, attemptTimeout, cancellationTokenSource.Token).ConfigureAwait(false);
                        if (response != null)
                            return response;
                    }
                    catch (Exception)
                    {
                        cancellationTokenSource.Cancel();
                        throw;
                    }
                }
                catch (StreamAlreadyUsedException)
                {
                    throw;
                }
                catch (Exception e)
                {
                    LogUnknownException(e);
                    return Responses.UnknownFailure;
                }
            }

            return new Response(ResponseCode.ConnectFailure);
        }

        private async Task<Response> SendOnceAsync(Request request, TimeSpan timeout, CancellationToken cancellationToken)
        {
            using (var state = new RequestState(request))
            {
                // should create new HttpRequestMessage per attempt
                state.RequestMessage = requestFactory.Create(request, timeout, cancellationToken);

                try
                {
                    state.ResponseMessage = await client
                        .SendAsync(state.RequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (HttpRequestException e) when (IsConnectionTimeout(e))
                {
                    return null;
                }
                catch (OperationCanceledException)
                {
                    return Responses.Canceled;
                }
                catch (ResponseException e)
                {
                    return e.Response;
                }

                state.ResponseCode = (ResponseCode) (int) state.ResponseMessage.StatusCode;

                state.Headers = HeadersConverter.Create(state.ResponseMessage);

                var contentLength = state.ResponseMessage.Content.Headers.ContentLength;

                try
                {
                    if (NeedToStreamResponseBody(contentLength))
                    {
                        return await GetResponseWithStreamAsync(state).ConfigureAwait(false);
                    }

                    if (contentLength != null)
                    {
                        if (contentLength > settings.MaxResponseBodySize)
                            return new Response(ResponseCode.InsufficientStorage, headers: state.Headers);

                        return await GetResponseWithKnownContentLength(state, (int) contentLength, cancellationToken).ConfigureAwait(false);
                    }

                    return await GetResponseWithUnknownContentLength(state, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    return Responses.Canceled;
                }
                catch (ResponseException e)
                {
                    return e.Response;
                }
            }
        }

        private static bool IsConnectionTimeout(HttpRequestException e) => e.InnerException is SocketException se && IsConnectionFailure(se.SocketErrorCode) ||
                                                                           e.InnerException is TaskCanceledException;

        private async Task<Response> GetResponseWithUnknownContentLength(RequestState state, CancellationToken cancellationToken)
        {
            try
            {
                using (var stream = await state.ResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false))
                using (var memoryStream = new MemoryStream())
                using (pool.AcquireHandle(out var buffer))
                {
                    while (true)
                    {
                        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                        if (bytesRead == 0)
                            break;

                        memoryStream.Write(buffer, 0, bytesRead);

                        if (memoryStream.Length > settings.MaxResponseBodySize)
                            return new Response(ResponseCode.InsufficientStorage, headers: state.Headers);
                    }

                    var content = new Content(memoryStream.GetBuffer(), 0, (int) memoryStream.Length);
                    return new Response(state.ResponseCode, content, state.Headers);
                }
            }
            catch (Exception e)
            {
                LogReceiveBodyFailure(state.Request, e);
                return new Response(ResponseCode.ReceiveFailure, headers: state.Headers);
            }
        }

        private async Task<Response> GetResponseWithKnownContentLength(RequestState state, int contentLength, CancellationToken cancellationToken)
        {
            try
            {
                using (var stream = await state.ResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false))
                {
                    var array = settings.BufferFactory(contentLength);
                    
                    var totalBytesRead = 0;

                    // Reference to buffer used in ReadAsync will be stored in Socket instance. We want to avoid long-lived buffers in LOH
                    // gcroot sample output for buffer used in .ReadAsync:
                    // -> System.Net.Sockets.Socket
                    // -> System.Net.Sockets.Socket+CachedEventArgs
                    // -> System.Net.Sockets.Socket+AwaitableSocketAsyncEventArgs
                    // -> System.Byte[]
                    if (contentLength < LOHObjectSizeThreshold)
                    {
                        while (totalBytesRead < contentLength)
                        {
                            var bytesToRead = Math.Min(contentLength - totalBytesRead, PreferredReadSize);
                            var bytesRead = await stream.ReadAsync(array, totalBytesRead, bytesToRead, cancellationToken).ConfigureAwait(false);
                            if (bytesRead == 0)
                                break;

                            totalBytesRead += bytesRead;
                        }
                    }
                    else
                    {
                        using (pool.AcquireHandle(out var buffer))
                        {
                            while (totalBytesRead < contentLength)
                            {
                                var bytesToRead = Math.Min(contentLength - totalBytesRead, buffer.Length);
                                var bytesRead = await stream.ReadAsync(buffer, 0, bytesToRead, cancellationToken).ConfigureAwait(false);
                                if (bytesRead == 0)
                                    break;

                                Buffer.BlockCopy(buffer, 0, array, totalBytesRead, bytesRead);

                                totalBytesRead += bytesRead;
                            }
                        }
                    }

                    if (totalBytesRead < contentLength)
                        throw new EndOfStreamException($"Response stream ended prematurely. Read only {totalBytesRead} byte(s), but Content-Length specified {contentLength}.");

                    return new Response(state.ResponseCode, new Content(array, 0, contentLength), state.Headers);
                }
            }
            catch (Exception e)
            {
                LogReceiveBodyFailure(state.Request, e);
                return new Response(ResponseCode.ReceiveFailure, headers: state.Headers);
            }
        }

        private bool NeedToStreamResponseBody(long? length)
        {
            try
            {
                return settings.UseResponseStreaming(length);
            }
            catch (Exception error)
            {
                log.Error(error);
                return false;
            }
        }

        private async Task<Response> GetResponseWithStreamAsync(RequestState state)
        {
            var stream = await state.ResponseMessage.Content.ReadAsStreamAsync().ConfigureAwait(false);
            var wrappedStream = new ResponseStream(stream, state);
            state.PreventNextDispose();
            return new Response(state.ResponseCode, null, state.Headers, wrappedStream);
        }

        private void LogRequestTimeout(Request request, TimeSpan timeout)
        {
            log.Error($"Request timed out. Target = {request.Url.Authority}. Timeout = {timeout.ToPrettyString()}.");
        }

        private void LogUnknownException(Exception error)
        {
            log.Error(error, "Unknown error in sending request.");
        }

        private void LogReceiveBodyFailure(Request request, Exception error)
        {
            log.Error(error, "Error in receiving request body from " + request.Url.Authority);
        }

        private void LogFailedToWaitForRequestAbort()
        {
            log.Warn($"Timed out request was aborted but did not complete in {settings.RequestAbortTimeout.ToPrettyString()}.");
        }

        private static bool IsConnectionFailure(SocketError socketError)
        {
            switch (socketError)
            {
                case SocketError.HostNotFound:
                case SocketError.AddressNotAvailable:
                    return true;
                default:
                    return false;
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            client?.Dispose();
        }
    }
}