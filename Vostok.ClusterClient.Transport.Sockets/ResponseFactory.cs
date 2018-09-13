using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Vostok.ClusterClient.Core.Model;
using Vostok.Commons.Collections;
using Vostok.Logging.Abstractions;

namespace Vostok.ClusterClient.Transport.Sockets
{
    internal class ResponseFactory
    {
        private readonly SocketsTransportSettings settings;
        private readonly UnboundedObjectPool<byte[]> pool;
        private readonly ILog log;

        public ResponseFactory(SocketsTransportSettings settings, UnboundedObjectPool<byte[]> pool, ILog log)
        {
            this.settings = settings;
            this.pool = pool;
            this.log = log;
        }

        public async Task<Response> CreateAsync(HttpResponseMessage responseMessage, RequestState state)
        {
            if (state.Status == HttpActionStatus.UserStreamFailure)
                return new Response(ResponseCode.StreamInputFailure);
            if (state.Status == HttpActionStatus.RequestCanceled)
                return new Response(ResponseCode.Canceled);
            
            if (responseMessage.Content != null && responseMessage.Content.Headers.ContentLength > settings.MaxResponseBodySize)
                return new Response(ResponseCode.InsufficientStorage);
                        
            var responseCode = (ResponseCode) (int) responseMessage.StatusCode;
            var (content, stream) = await GetContentAsync(responseMessage).ConfigureAwait(false);

            var headers = Headers.Empty;
            
            foreach (var responseHeader in responseMessage.Headers)
                headers = headers.Set(responseHeader.Key, responseHeader.Value.FirstOrDefault());
            
            if (responseMessage.Content != null)
                foreach (var contentHeader in responseMessage.Content.Headers)
                    headers = headers.Set(contentHeader.Key, contentHeader.Value.FirstOrDefault());
            
            
            
            var response = new Response(responseCode, content, headers, stream);
            return response;
        }

        
        private bool ResponseBodyIsTooLarge(long length)
        {
            var limit = settings.MaxResponseBodySize ?? long.MaxValue;

            if (length > limit)
            {
                LogResponseBodyTooLarge(length, limit);
            }

            return length > limit;
        } 
        
        private void LogResponseBodyTooLarge(long size, long limit)
        {
            log.Error($"Response body size {size} is larger than configured limit of {limit} bytes.");
        }

        public async Task<(Content, Stream)> GetContentAsync(HttpResponseMessage message)
        {
            if (message.Content == null)
                return (null, null);
            
            if (UseStreaming(message))
            {
                var stream = await message.Content.ReadAsStreamAsync().ConfigureAwait(false);
                return (null, stream);
            }
            
            var bytes = await message.Content.ReadAsByteArrayAsync();
            return (new Content(bytes, 0, bytes.Length), null);
        }

        public bool UseStreaming(HttpResponseMessage message)
        {

            try
            {
                return settings.UseResponseStreaming(message.Content.Headers.ContentLength);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}