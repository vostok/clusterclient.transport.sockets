using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport.Sockets.ArpCache;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Transport.Sockets.Sender
{
    internal class SocketsTransportRequestSender : ISocketsTransportRequestSender
    {
        private readonly SocketsTransportSettings settings;
        private readonly HttpRequestMessageFactory requestFactory;
        private readonly ResponseReader responseReader;
        private readonly byte[] keepAliveValues;
        private readonly ILog log;

        private readonly ConcurrentDictionary<TimeSpan, Lazy<HttpClient>> clients;

        public SocketsTransportRequestSender(
            SocketsTransportSettings settings,
            HttpRequestMessageFactory requestFactory,
            ResponseReader responseReader,
            byte[] keepAliveValues,
            ILog log)
        {
            this.settings = settings;
            this.requestFactory = requestFactory;
            this.responseReader = responseReader;
            this.keepAliveValues = keepAliveValues;
            this.log = log;

            clients = new ConcurrentDictionary<TimeSpan, Lazy<HttpClient>>();
        }

        public async Task<Response> SendAsync(Request request, TimeSpan connectionTimeout, CancellationToken cancellationToken)
        {
            try
            {
                using (var state = new RequestDisposableState())
                    return await SendInternalAsync(state, request, connectionTimeout, cancellationToken).ConfigureAwait(false);
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

        private async Task<Response> SendInternalAsync(RequestDisposableState state, Request request, TimeSpan connectionTimeout, CancellationToken cancellationToken)
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

        private void LogUnknownException(Exception error)
        {
            log.Warn(error, "Unknown error in sending request.");
        }
    }
}