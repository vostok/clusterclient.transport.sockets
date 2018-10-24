using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Transport;
using Vostok.Clusterclient.Transport.Sockets.Client;
using Vostok.Clusterclient.Transport.Sockets.ClientProvider;
using Vostok.Clusterclient.Transport.Sockets.Sender;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;

namespace Vostok.Clusterclient.Transport.Sockets.Tests
{
    internal class SocketsTransport_Tests
    {
        private ISocketsTransportRequestSender sender;
        private SocketsTransportSettings settings;
        private SocketsTransport transport;
        private IHttpClientProvider clientProvider;
        private ILog log;
        private TimeSpan timeout;

        private Request request;

        [SetUp]
        public void Setup()
        {
            sender = Substitute.For<ISocketsTransportRequestSender>();
            log = new ConsoleLog();
            settings = new SocketsTransportSettings();
            clientProvider = Substitute.For<IHttpClientProvider>();
            transport = new SocketsTransport(settings, log, sender, clientProvider);
            request = Request.Post("http://localhost/");
            timeout = 10.Seconds();
        }

        [Test]
        public void Should_support_request_streaming()
            => transport.Supports(TransportCapabilities.RequestStreaming).Should().BeTrue();
    
        [Test]
        public void Should_support_response_streaming()
            => transport.Supports(TransportCapabilities.ResponseStreaming).Should().BeTrue();

        [Test]
        public void Should_return_Cancelled_when_cancellation_token_is_cancelled()
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                
                transport
                    .SendAsync(request, null, timeout, cts.Token)
                    .GetAwaiter()
                    .GetResult()
                    .Code
                    .Should()
                    .Be(ResponseCode.Canceled);
            }
        }

        [TestCase(0)]
        [TestCase(0.1)]
        [TestCase(0.99)]
        public void Should_return_Timeout_when_provided_timeout_is_too_small(double timeoutMs)
            => transport
                .SendAsync(request, null, TimeSpan.FromTicks((long)(timeoutMs * 10000)), CancellationToken.None)
                .GetAwaiter().GetResult()
                .Code
                .Should()
                .Be(ResponseCode.RequestTimeout);

        [Test]
        public async Task Should_pass_connection_timeout_to_client_provider()
        {
            var connectionTimeout = 6.Seconds();
            await transport.SendAsync(request, connectionTimeout, timeout, CancellationToken.None);
            clientProvider.Received(1).GetClient(connectionTimeout);
        }
        
        [Test]
        public async Task Should_pass_null_connection_timeout_to_client_provider()
        {
            TimeSpan? connectionTimeout = null;
            await transport.SendAsync(request, connectionTimeout, timeout, CancellationToken.None);
            clientProvider.Received(1).GetClient(connectionTimeout);
        }

        [Test]
        public async Task Should_use_client_from_clientProvider_for_request_sending()
        {
            var client = Substitute.For<IHttpClient>();
            clientProvider.GetClient(null).ReturnsForAnyArgs(client);

            await transport.SendAsync(request, null, timeout, CancellationToken.None);

            sender.Received(1).SendAsync(client, request, Arg.Any<CancellationToken>());
        }

        [Test]
        public async Task Should_cancel_request_on_timeout()
        {
            CancellationToken token;
            sender.SendAsync(null, null, default)
                .ReturnsForAnyArgs(Task.Run(
                    async () =>
                    {
                        await Task.Yield();
                        await Task.Delay(-1);
                        return Responses.Ok;
                    }))
                .AndDoes(x => token = x.Arg<CancellationToken>());
            
            await transport.SendAsync(request, null, 2.Milliseconds(), CancellationToken.None);

            token.IsCancellationRequested.Should().BeTrue();
        }
    }
}