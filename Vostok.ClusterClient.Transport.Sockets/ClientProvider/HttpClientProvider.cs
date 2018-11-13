using System;
using System.Collections.Concurrent;
using System.Threading;
using Vostok.Clusterclient.Transport.Sockets.Client;

namespace Vostok.Clusterclient.Transport.Sockets.ClientProvider
{
    internal class HttpClientProvider : IHttpClientProvider
    {
        private readonly SocketsTransportSettings settings;
        private readonly ConcurrentDictionary<TimeSpan, Lazy<IHttpClient>> clients;
        private readonly IHttpClientGlobalCache globalCache = HttpClientGlobalCache.Instance;

        public HttpClientProvider(SocketsTransportSettings settings)
        {
            this.settings = settings;
            clients = new ConcurrentDictionary<TimeSpan, Lazy<IHttpClient>>();
        }

        public IHttpClient GetClient(TimeSpan? connectionTimeout)
            => clients
                .GetOrAdd(
                    connectionTimeout ?? Timeout.InfiniteTimeSpan,
                    t => globalCache.GetClient(settings, t))
                .Value;

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