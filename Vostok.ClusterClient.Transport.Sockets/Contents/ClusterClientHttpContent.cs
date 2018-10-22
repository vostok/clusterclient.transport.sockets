using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Transport.Sockets.Contents
{
    internal abstract class ClusterClientHttpContent : HttpContent
    {
        private readonly Request request;
        protected readonly SendContext SendContext;
        protected readonly ILog Log;

        protected ClusterClientHttpContent(Request request, SendContext sendContext, ILog log)
        {
            this.request = request;
            SendContext = sendContext;
            Log = log;
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            SendContext.Socket = SocketAccessor.GetSocket(stream, Log);
            try
            {
                await SerializeAsync(stream, context);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (StreamAlreadyUsedException)
            {
                throw;
            }
            catch (Exception e)
            {
                LogSendBodyFailure(request.Url, e);
                SendContext.Response = new Response(ResponseCode.SendFailure);
            }
        }

        protected abstract Task SerializeAsync(Stream stream, TransportContext context);

        private void LogSendBodyFailure(Uri uri, Exception error)
        {
            Log.Error(error, "Error in sending request body to " + uri.Authority);
        }
    }
}