using System;
using System.Net.Http;
using System.Threading;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.ClusterClient.Transport.Sockets
{
    internal class RequestState : IDisposable
    {
        private int disposeBarrier;

        public RequestState(Request request)
        {
            Request = request;
        }

        public HttpRequestMessage RequestMessage { get; set; }
        public HttpResponseMessage ResponseMessage { get; set; }

        public Headers Headers { get; set; }

        public ResponseCode ResponseCode { get; set; }
        public Request Request { get; set; }

        public void PreventNextDispose()
        {
            Interlocked.Exchange(ref disposeBarrier, 1);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref disposeBarrier, 0) > 0)
                return;

            DisposeRequest();
            DisposeResponse();
        }

        private void DisposeRequest()
        {
            if (RequestMessage != null)
                try
                {
                    RequestMessage.Dispose();
                }
                catch
                {
                }
                finally
                {
                    RequestMessage = null;
                }
        }

        private void DisposeResponse()
        {
            if (ResponseMessage != null)
                try
                {
                    ResponseMessage.Dispose();
                }
                catch
                {
                }
                finally
                {
                    ResponseMessage = null;
                }
        }
    }
}