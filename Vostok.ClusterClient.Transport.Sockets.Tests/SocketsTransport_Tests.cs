using System;
using System.Threading;
using FluentAssertions;
using FluentAssertions.Extensions;
using NSubstitute;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Core.Transport;
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
        private ILog log;

        private Request request;

        [SetUp]
        public void Setup()
        {
            sender = Substitute.For<ISocketsTransportRequestSender>();
            log = new ConsoleLog();
            settings = new SocketsTransportSettings();
            transport = new SocketsTransport(settings, log, sender);
            request = Request.Post("http://localhost/");
        }

        [TearDown]
        public void Teardown() => ConsoleLog.Flush();

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
                    .SendAsync(request, null, 1.Seconds(), cts.Token)
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
                .SendAsync(request, null, TimeSpan.FromMilliseconds(timeoutMs), CancellationToken.None)
                .GetAwaiter().GetResult()
                .Code
                .Should()
                .Be(ResponseCode.RequestTimeout);

        [Test]
        public void Should_pass_connection_timeout_to_sender()
        {
            var connectionTimeout = 6.Seconds();
            transport.SendAsync(request, connectionTimeout, 10.Seconds(), CancellationToken.None);
            sender.Received(1).SendAsync(Arg.Any<Request>(), connectionTimeout, Arg.Any<CancellationToken>());
        }
    }
}