using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading;
using Vostok.ClusterClient.Core.Model;

namespace Vostok.ClusterClient.Transport.Sockets
{
    internal class RequestState : IDisposable
    {
        private readonly TimeSpan timeout;
        private readonly CancellationTokenSource cancellationTokenSource;
        private readonly Stopwatch stopwatch;
        private int cancellationState;
        private int disposeBarrier;

        public RequestState(TimeSpan timeout, CancellationTokenSource cancellationTokenSource)
        {
            this.timeout = timeout;
            this.cancellationTokenSource = cancellationTokenSource;
            stopwatch = Stopwatch.StartNew();
        }

        public HttpRequestMessage RequestMessage { get; set; }
        public HttpResponseMessage ResponseMessage { get; set; }

        public Headers Headers { get; set; }

        public TimeSpan TimeRemaining
        {
            get
            {
                var remaining = timeout - stopwatch.Elapsed;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }
        public bool RequestCancelled => cancellationState > 0;
        
        public ResponseCode ResponseCode { get; set; }
        public Request Request { get; set; }

        public void CancelRequest()
        {
            Interlocked.Exchange(ref cancellationState, 1);

            CancelRequestAttempt();
        }

        public void CancelRequestAttempt()
        {
            if (RequestMessage != null)
                try
                {
                    cancellationTokenSource.Cancel();
                }
                catch
                {
                }
        }

        public void PreventDispose()
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

        public void DisposeRequest()
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

        public void DisposeResponse()
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