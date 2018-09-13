using System;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Vostok.ClusterClient.Core;
using Vostok.ClusterClient.Core.Model;
using Vostok.ClusterClient.Core.Transport;
using Vostok.Commons.Collections;
using Vostok.Commons.Helpers.Extensions;
using Vostok.Logging.Abstractions;

namespace Vostok.ClusterClient.Transport.Sockets
{   
    public class SocketsTransport : ITransport, IDisposable
    {
        private readonly SocketsTransportSettings settings;
        private readonly ILog log;
        private readonly HttpClient client;
        private readonly HttpRequestMessageFactory requestFactory;
        private readonly ResponseFactory responseFactory;
        
        public SocketsTransport(SocketsTransportSettings settings, ILog log)
        {
            settings = settings.Clone();
            
            this.settings = settings;
            this.log = log;
            var handler = new SocketsHttpHandler
            {
                Proxy = settings.Proxy,
                ConnectTimeout = settings.ConnectionTimeout ?? TimeSpan.FromMinutes(2),
                UseProxy = settings.Proxy != null,
                AllowAutoRedirect = settings.AllowAutoRedirect,
                PooledConnectionIdleTimeout = settings.ConnectionIdleTimeout, //TODO
                PooledConnectionLifetime = settings.TcpKeepAliveTime
            };
            
            client = new HttpClient(handler, true);
            
            var pool = new UnboundedObjectPool<byte[]>(() => new byte[16 * 1024]);
            
            requestFactory = new HttpRequestMessageFactory(pool, log);
            responseFactory = new ResponseFactory(settings, pool, log);
        }

        public async Task<Response> SendAsync(Request request, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (timeout.TotalMilliseconds < 1)
            {
                LogRequestTimeout(request, timeout);
                return new Response(ResponseCode.RequestTimeout);
            }
            var sw = Stopwatch.StartNew();
            
            for (var i = 0; i < settings.ConnectionAttempts; i++)
            {
                var attemptTimeout = timeout - sw.Elapsed;
                if (attemptTimeout < TimeSpan.Zero)
                    return new Response(ResponseCode.RequestTimeout);

                using (var localCts = new CancellationTokenSource())
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, localCts.Token))
                {
                    var sendTask = SendInternalAsync(request, timeout, linkedCts);
                    var timeoutTask = Task.Delay(timeout, linkedCts.Token);
                    if (await Task.WhenAny(sendTask, timeoutTask) == timeoutTask)
                    {
                        localCts.Cancel();
                        continue;
                    }
                    localCts.Cancel();
                    var response = sendTask.GetAwaiter().GetResult();
                    if (response != null)
                        return response;
                }
            }

            return new Response(ResponseCode.ConnectFailure);
        }

        private async Task<Response> SendInternalAsync(Request request, TimeSpan timeout, CancellationTokenSource cancellationTokenSource)
        {
            var state = new RequestState(request, cancellationTokenSource);
                
            // should create new HttpRequestMessage per attempt
            var message = requestFactory.Create(request, state, timeout);

            try
            {
                var response = await client.SendAsync(message, state, HttpCompletionOption.ResponseHeadersRead, timeout, cancellationTokenSource.Token).ConfigureAwait(false);
                return await responseFactory.CreateAsync(response, state).ConfigureAwait(false);
            }
            catch (HttpRequestException e) when (e.InnerException is SocketException se && IsConnectionFailure(se.SocketErrorCode))
            {
            }
            catch (HttpRequestException e) when (e.InnerException is TaskCanceledException)
            {
            }
            catch (StreamAlreadyUsedException)
            {
                throw;
            }
            catch (Exception e)
            {
                return new Response(ResponseCode.UnknownFailure);
            }

            return null;
        }

        private void LogRequestTimeout(Request request, TimeSpan timeout)
        {
            log.Error($"Request timed out. Target = {request.Url.Authority}. Timeout = {timeout.ToPrettyString()}.");
        }

        private static bool IsConnectionFailure(SocketError socketError)
        {
            switch (socketError)
            {
                case SocketError.HostNotFound:
                case SocketError.AddressNotAvailable:
                    return true;
                default:
                    Console.WriteLine(socketError);
                    return false;
            }
        }

        public TransportCapabilities Capabilities { get; }
        internal SocketsTransportSettings Settings => settings;

        public void Dispose()
        {
            client?.Dispose();
        }
    }
}