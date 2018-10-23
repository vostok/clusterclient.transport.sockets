using System;
using System.Net.Http;

namespace Vostok.Clusterclient.Transport.Sockets.ClientProvider
{
    internal interface IHttpClientProvider : IDisposable
    {
        HttpClient GetClient(TimeSpan? connectionTimeout);
    }
}