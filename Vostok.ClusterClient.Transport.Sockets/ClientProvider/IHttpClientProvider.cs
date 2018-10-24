using System;
using Vostok.Clusterclient.Transport.Sockets.Client;

namespace Vostok.Clusterclient.Transport.Sockets.ClientProvider
{
    internal interface IHttpClientProvider : IDisposable
    {
        IHttpClient GetClient(TimeSpan? connectionTimeout);
    }
}