using System;
using Vostok.Clusterclient.Transport.Sockets.Client;

namespace Vostok.Clusterclient.Transport.Sockets.ClientProvider
{
    internal interface IHttpClientGlobalCache
    {
        Lazy<IHttpClient> GetClient(SocketsTransportSettings settings, TimeSpan connectionTimeout);
    }
}