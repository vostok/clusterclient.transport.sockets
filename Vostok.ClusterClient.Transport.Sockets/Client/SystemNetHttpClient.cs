using System.Net.Http;

namespace Vostok.Clusterclient.Transport.Sockets.Client
{
    internal class SystemNetHttpClient : HttpClient, IHttpClient
    {
        public SystemNetHttpClient()
        {
        }

        public SystemNetHttpClient(HttpMessageHandler handler)
            : base(handler)
        {
        }

        public SystemNetHttpClient(HttpMessageHandler handler, bool disposeHandler)
            : base(handler, disposeHandler)
        {
        }
    }
}