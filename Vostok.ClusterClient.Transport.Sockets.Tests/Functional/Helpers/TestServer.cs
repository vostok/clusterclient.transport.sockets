using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Vostok.ClusterClient.Transport.Sockets.Tests.Utilities;

namespace Vostok.ClusterClient.Transport.Sockets.Tests.Functional.Helpers
{
    internal class TestServer : IDisposable
    {
        private readonly HttpListener listener;
        private readonly int port;
        private readonly string host;
        private volatile ReceivedRequest lastRequest;

        public TestServer()
        {
            port = FreeTcpPortFinder.GetFreePort();
            host = Dns.GetHostName();
            listener = new HttpListener();
            listener.Prefixes.Add($"http://+:{port}/");
        }

        public ReceivedRequest LastRequest => lastRequest;

        public Uri Url => new Uri($"http://{host}:{port}/");

        public string Host => host;

        public int Port => port;

        public bool BufferRequestBody { get; set; } = true;

        public static TestServer StartNew(Action<HttpListenerContext> handle)
        {
            var server = new TestServer();

            server.Start(handle);

            return server;
        }

        public void Start(Action<HttpListenerContext> handle)
        {
            listener.Start();

            Task.Run(
                async () =>
                {
                    while (true)
                    {
                        var context = await listener.GetContextAsync();

                        Task.Run(
                            () =>
                            {
                                Interlocked.Exchange(ref lastRequest, DescribeReceivedRequest(context.Request));

                                handle(context);

                                context.Response.Close();
                            });
                    }
                });
        }

        public void Dispose()
        {
            listener.Stop();
            listener.Close();
        }

        private ReceivedRequest DescribeReceivedRequest(HttpListenerRequest request)
        {
            var receivedRequest = new ReceivedRequest
            {
                Url = request.Url,
                Method = request.HttpMethod,
                Headers = request.Headers,
                Query = HttpUtility.ParseQueryString(request.Url.Query),
            };

            if (BufferRequestBody)
            {
                var bodyStream = new MemoryStream(Math.Max(4, (int) request.ContentLength64));

                request.InputStream.CopyTo(bodyStream);

                receivedRequest.Body = bodyStream.ToArray();
                receivedRequest.BodySize = bodyStream.Length;
            }
            else
            {
                try
                {
                    var buffer = new byte[16 * 1024];

                    while (true)
                    {
                        var bytesReceived = request.InputStream.Read(buffer, 0, buffer.Length);
                        if (bytesReceived == 0)
                            break;

                        receivedRequest.BodySize += bytesReceived;
                    }
                }
                catch (Exception error)
                {
                    Console.Out.WriteLine(error);
                }
            }

            return receivedRequest;
        }
    }
}