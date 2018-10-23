using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

namespace Vostok.Clusterclient.Transport.Sockets.ClientProvider
{
    internal class HttpClientProvider : IHttpClientProvider
    {
        private readonly SocketsTransportSettings settings;
        private ConcurrentDictionary<TimeSpan, Lazy<HttpClient>> clients;

        public HttpClientProvider(SocketsTransportSettings settings)
        {
            this.settings = settings;
            clients = new ConcurrentDictionary<TimeSpan, Lazy<HttpClient>>();
        }

        public HttpClient GetClient(TimeSpan? connectionTimeout)
            => clients
                .GetOrAdd(
                    connectionTimeout ?? Timeout.InfiniteTimeSpan,
                    t => new Lazy<HttpClient>(() => CreateClient(t)))
                .Value;
        

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

        public void Dispose()
        {
            foreach (var kvp in clients)
            {
                var client = kvp.Value.Value;

                client.CancelPendingRequests();
                client.Dispose();
            }
        }
    }
}