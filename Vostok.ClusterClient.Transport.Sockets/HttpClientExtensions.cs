using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Vostok.ClusterClient.Transport.Sockets
{
    internal static class HttpClientExtensions
    {
        public static async Task<HttpResponseMessage> SendAsync(
            this HttpClient client,
            HttpRequestMessage requestMessage,
            RequestState state,
            HttpCompletionOption completionOption,
            TimeSpan timeout,
            CancellationToken cancellatonToken)
        {
            using (var timeoutCts = new CancellationTokenSource(timeout))
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(timeoutCts.Token, cancellatonToken))
            {
                try
                {
                    return await client.SendAsync(requestMessage, completionOption, linkedCts.Token);
                }
                catch (OperationCanceledException e) when (timeoutCts.IsCancellationRequested)
                {
                    return new HttpResponseMessage(HttpStatusCode.RequestTimeout);
                }
                catch (OperationCanceledException e) when (cancellatonToken.IsCancellationRequested)
                {
                    state.Status = HttpActionStatus.RequestCanceled;
                    return null;
                }
            }
        }
    }
}