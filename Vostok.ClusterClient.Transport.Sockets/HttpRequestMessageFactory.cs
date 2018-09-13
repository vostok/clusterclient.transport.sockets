using System;
using System.Net.Http;
using Vostok.ClusterClient.Core.Model;
using Vostok.Commons.Collections;
using Vostok.Logging.Abstractions;

namespace Vostok.ClusterClient.Transport.Sockets
{
    internal class HttpRequestMessageFactory
    {
        private readonly UnboundedObjectPool<byte[]> pool;
        private readonly ILog log;

        public HttpRequestMessageFactory(UnboundedObjectPool<byte[]> pool, ILog log)
        {
            this.pool = pool;
            this.log = log;
        }

        public HttpRequestMessage Create(Request request, RequestState state, TimeSpan timeout)
        {
            var method = TranslateRequestMethod(request.Method);
            var content = CreateContent(state);

            var message = new HttpRequestMessage(method, request.Url)
            {
                Content = content
            };

            HttpHeaderFiller.Fill(request, message, timeout, log);

            return message;
        }

        private HttpContent CreateContent(RequestState state)
        {
            var request = state.Request;

            var content = request.Content;
            var streamContent = request.StreamContent;
            
            if (content != null)
                return ByteArrayContent(state);
            if (streamContent != null)
                return HttpStreamContent(state);
            
            return null;
        }

        private HttpContent ByteArrayContent(RequestState state) => new RequestByteArrayContent(state, log);

        private HttpContent HttpStreamContent(RequestState state) => new RequestStreamContent(state, pool, log);

        private static HttpMethod TranslateRequestMethod(string httpMethod)
        {
            switch (httpMethod)
            {
                case RequestMethods.Get:
                    return HttpMethod.Get;
                case RequestMethods.Post:
                    return HttpMethod.Post;
                case RequestMethods.Put:
                    return HttpMethod.Put;
                case RequestMethods.Patch:
                    return HttpMethod.Patch;
                case RequestMethods.Delete:
                    return HttpMethod.Delete;
                case RequestMethods.Head:
                    return HttpMethod.Head;
                case RequestMethods.Options:
                    return HttpMethod.Options;
                case RequestMethods.Trace:
                    return HttpMethod.Trace;
                default:
                    return new HttpMethod(httpMethod);
            }
        }
    }
}