using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using Vostok.Clusterclient.Transport.Sockets.Client;
using Vostok.Commons.Collections;

namespace Vostok.Clusterclient.Transport.Sockets.ClientProvider
{
    internal class HttpClientGlobalCache : IHttpClientGlobalCache
    {   
        private const int Capacity = 20;
        
        private readonly RecyclingBoundedCache<SettingsKey, Lazy<IHttpClient>> clients
            = new RecyclingBoundedCache<SettingsKey, Lazy<IHttpClient>>(Capacity);
     
        public static readonly HttpClientGlobalCache Instance = new HttpClientGlobalCache();
        
        public Lazy<IHttpClient> GetClient(SocketsTransportSettings settings, TimeSpan connectionTimeout)
        {
            var settingsKey = new SettingsKey(
                connectionTimeout,
                settings.ConnectionIdleTimeout,
                settings.ConnectionLifetime,
                settings.Proxy,
                settings.MaxConnectionsPerEndpoint,
                settings.MaxResponseDrainSize,
                settings.AllowAutoRedirect);
            
            return clients.Obtain(settingsKey, key => new Lazy<IHttpClient>(() => CreateClient(settingsKey)));
        }

        private static IHttpClient CreateClient(SettingsKey settings)
        {
            var handler = new SocketsHttpHandler
            {
                Proxy = settings.Proxy,
                ConnectTimeout = settings.ConnectionTimeout,
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

            return new SystemNetHttpClient(handler, true);
        }
    }
}