using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Transport;
using Vostok.Clusterclient.Transport.Sockets.ArpCache;
using Vostok.Clusterclient.Transport.Sockets.Pool;
using Vostok.Commons.Time;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Transport.Sockets
{
    /// <summary>
    ///     <para>ClusterClient HTTP transport for .NET Core 2.1 and later.</para>
    ///     <para>Internally uses <see cref="SocketsHttpHandler" />.</para>
    /// </summary>
    [PublicAPI]
    public class SocketsTransport : ITransport, IDisposable
    {
        private readonly SocketsTransportSettings settings;
        private readonly ILog log;
        private readonly IPool<byte[]> pool;
        private readonly ConcurrentDictionary<TimeSpan, Lazy<HttpClient>> clients;
        private readonly HttpRequestMessageFactory requestFactory;
        private readonly byte[] keepAliveValues;
        private readonly ResponseReader responseReader;

        /// <summary>
        ///     Creates ClusterClient transport for .NET Core 2.1 and later based on <see cref="SocketsHttpHandler" />
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        public SocketsTransport(SocketsTransportSettings settings, ILog log)
        {
            settings = settings.Clone();

            this.settings = settings;
            this.log = log;
            pool = new Pool<byte[]>(() => new byte[SocketsTransportConstants.PooledBufferSize]);

            requestFactory = new HttpRequestMessageFactory(pool, log);
            responseReader = new ResponseReader(settings, pool, log);

            keepAliveValues = KeepAliveTuner.GetKeepAliveValues(settings);

            clients = new ConcurrentDictionary<TimeSpan, Lazy<HttpClient>>();
        }

        /// <inheritdoc />
        public TransportCapabilities Capabilities { get; } = TransportCapabilities.RequestStreaming | TransportCapabilities.ResponseStreaming;

        /// <inheritdoc />
        public async Task<Response> SendAsync(Request request, TimeSpan? connectionTimeout, TimeSpan timeout, CancellationToken cancellationToken)
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
                var senderTask = SendInternalAsync(request, connectionTimeout ?? Timeout.InfiniteTimeSpan, requestCancellation.Token);
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

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (var kvp in clients)
            {
                var client = kvp.Value.Value;

                client.CancelPendingRequests();
                client.Dispose();
            }
        }

        private static bool IsConnectionFailure(HttpRequestException e, CancellationToken cancellationToken)
            => e.InnerException is SocketException se && IsConnectionFailure(se.SocketErrorCode) ||
               e.InnerException is TaskCanceledException && !cancellationToken.IsCancellationRequested;

        private static bool IsConnectionFailure(SocketError socketError)
        {
            switch (socketError)
            {
                case SocketError.HostNotFound:
                case SocketError.AddressNotAvailable:
                // seen on linux:
                case SocketError.ConnectionRefused:
                case SocketError.TryAgain:
                case SocketError.NetworkUnreachable:
                // other:
                case SocketError.NetworkDown:
                case SocketError.HostDown:
                case SocketError.HostUnreachable:
                    return true;
                default:
                    return false;
            }
        }

        private HttpClient CreateClient(TimeSpan connectionTimeout)
        {
            var handler = new SocketsHttpHandler
            {
                Proxy = settings.Proxy,
                ConnectTimeout = connectionTimeout,
                UseProxy = settings.Proxy != null,
                AllowAutoRedirect = settings.AllowAutoRedirect,
                PooledConnectionIdleTimeout = settings.ConnectionIdleTimeout,
                PooledConnectionLifetime = settings.ConnectionLifetime,
                MaxConnectionsPerServer = settings.MaxConnectionsPerEndpoint,
                AutomaticDecompression = DecompressionMethods.None,
                UseCookies = false,
                SslOptions =
                {
                    CertificateRevocationCheckMode = X509RevocationMode.NoCheck,
                    RemoteCertificateValidationCallback = (_, __, ___, ____) => true
                }
            };

            if (settings.MaxResponseDrainSize.HasValue)
                handler.MaxResponseDrainSize = settings.MaxResponseDrainSize.Value;

            settings.Tune?.Invoke(handler);

            return new HttpClient(handler, true);
        }

        private async Task<Response> SendInternalAsync(Request request, TimeSpan connectionTimeout, CancellationToken cancellationToken)
        {
            try
            {
                using (var state = new RequestDisposableState())
                    return await SendOnceAsync(state, request, connectionTimeout, cancellationToken).ConfigureAwait(false);
            }
            catch (StreamAlreadyUsedException)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                return Responses.Canceled;
            }
            catch (Exception e)
            {
                LogUnknownException(e);
                return Responses.UnknownFailure;
            }
        }

        private async Task<Response> SendOnceAsync(RequestDisposableState state, Request request, TimeSpan connectionTimeout, CancellationToken cancellationToken)
        {
            state.RequestMessage = requestFactory.Create(request, cancellationToken, out var sendContext);

            var client = clients.GetOrAdd(connectionTimeout, t => new Lazy<HttpClient>(() => CreateClient(t))).Value;

            try
            {
                state.ResponseMessage = await client
                    .SendAsync(state.RequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (HttpRequestException e) when (IsConnectionFailure(e, cancellationToken))
            {
                var message = $"Connection failure. Target = {request.Url.Authority}.";
                log.Warn(e, message);
                return Responses.ConnectFailure;
            }

            var socket = sendContext.Socket;
            if (socket != null)
            {
                if (sendContext.Response != null)
                    return sendContext.Response;

                if (settings.TcpKeepAliveEnabled)
                    KeepAliveTuner.Tune(socket, settings, keepAliveValues);

                if (settings.ArpCacheWarmupEnabled && socket.RemoteEndPoint is IPEndPoint ipEndPoint)
                    ArpCacheMaintainer.ReportAddress(ipEndPoint.Address);
            }

            if (sendContext.Response != null)
                return sendContext.Response;

            var responseCode = (ResponseCode) (int) state.ResponseMessage.StatusCode;

            var headers = HeadersConverter.Create(state.ResponseMessage);

            var responseReadResult = await responseReader
                .ReadResponseBodyAsync(state.ResponseMessage, cancellationToken)
                .ConfigureAwait(false);

            if (responseReadResult.Content != null)
                return new Response(responseCode, responseReadResult.Content, headers);
            if (responseReadResult.ErrorCode != null)
                return new Response(responseReadResult.ErrorCode.Value, null, headers);
            if (responseReadResult.Stream == null)
                return new Response(responseCode, null, headers);

            state.PreventNextDispose();
            return new Response(responseCode, null, headers, new ResponseStream(responseReadResult.Stream, state));
        }

        private void LogRequestTimeout(Request request, TimeSpan timeout)
        {
            log.Warn(
                "Request timed out. Target = {Target}. Timeout = {Timeout}.",
                request.Url.Authority,
                timeout.ToPrettyString());
        }

        private void LogUnknownException(Exception error)
        {
            log.Warn(error, "Unknown error in sending request.");
        }

        private void LogFailedToWaitForRequestAbort()
        {
            log.Warn(
                "Timed out request was aborted but did not complete in {RequestAbortTimeout}.",
                settings.RequestAbortTimeout.ToPrettyString());
        }
    }
}