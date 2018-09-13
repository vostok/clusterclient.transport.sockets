using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Vostok.Logging.Abstractions;

namespace Vostok.ClusterClient.Transport.Sockets
{
    internal class RequestByteArrayContent : HttpContent
    {
        private readonly RequestState state;
        private readonly ILog log;

        public RequestByteArrayContent(RequestState state, ILog log)
        {
            this.state = state;
            this.log = log;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            try
            {
                var content = state.Request.Content;
                await stream.WriteAsync(content.Buffer, content.Offset, content.Length, state.CancellationToken).ConfigureAwait(false);
                await stream.FlushAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                state.Status = HttpActionStatus.RequestCanceled;
            }
            catch (Exception e)
            {
                state.Status = HttpActionStatus.SendFailure;
                LogSendBodyFailure(state.Request.Url, e);
            }
            
        }

        protected override bool TryComputeLength(out long length)
        {
            length = state.Request.Content.Length;
            return true;
        }

        private void LogSendBodyFailure(Uri uri, Exception error)
        {
            log.Error("Error in sending request body to " + uri.Authority, error);
        }

    }
}