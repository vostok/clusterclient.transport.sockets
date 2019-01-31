using System;
using System.Linq.Expressions;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Invoker = System.Func<System.Net.Http.SocketsHttpHandler, System.Net.Http.HttpRequestMessage, System.Threading.CancellationToken, System.Threading.Tasks.Task<System.Net.Http.HttpResponseMessage>>;

namespace Vostok.Clusterclient.Transport.Sockets
{
    internal class SocketsHandlerInvoker : HttpMessageHandler
    {
        private static readonly Invoker invoker;

        static SocketsHandlerInvoker()
        {
            try
            {
                invoker = BuildInvoker();

                CanInvokeDirectly = true;
            }
            catch
            {
                invoker = (handler, message, token) => new HttpClient(handler).SendAsync(message, HttpCompletionOption.ResponseHeadersRead, token);
            }
        }

        private SocketsHandlerInvoker()
        {
        }

        public static bool CanInvokeDirectly { get; }

        public static Task<HttpResponseMessage> Invoke(SocketsHttpHandler handler, HttpRequestMessage message, CancellationToken token)
            => invoker(handler, message, token);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage message, CancellationToken token)
            => throw new NotImplementedException();

        private static Invoker BuildInvoker()
        {
            var handlerParameter = Expression.Parameter(typeof(SocketsHttpHandler));
            var messageParameter = Expression.Parameter(typeof(HttpRequestMessage));
            var tokenParameter = Expression.Parameter(typeof(CancellationToken));

            var sendMethod = typeof(SocketsHttpHandler).GetMethod(nameof(SendAsync), BindingFlags.Instance | BindingFlags.NonPublic);
            var sendMethodCall = Expression.Call(handlerParameter, sendMethod, messageParameter, tokenParameter);

            return Expression.Lambda<Invoker>(sendMethodCall, handlerParameter, messageParameter, tokenParameter).Compile();
        }
    }
}
