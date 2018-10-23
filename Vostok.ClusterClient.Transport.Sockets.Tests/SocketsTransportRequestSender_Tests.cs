using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport.Sockets.Hacks;
using Vostok.Clusterclient.Transport.Sockets.Messages;
using Vostok.Clusterclient.Transport.Sockets.ResponseReading;
using Vostok.Clusterclient.Transport.Sockets.Sender;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;

namespace Vostok.Clusterclient.Transport.Sockets.Tests
{
    public class SocketsTransportRequestSender_Tests
    {
        private SocketsTransportSettings settings;
        private IHttpRequestMessageFactory requestFactory;
        private IResponseReader responseReader;
        private SocketsTransportRequestSender sender;
        private ISocketTuner socketTuner;
        private ILog log;

        [SetUp]
        public void Setup()
        {
            settings = new SocketsTransportSettings();
            requestFactory = Substitute.For<IHttpRequestMessageFactory>();
            responseReader = Substitute.For<IResponseReader>();
            socketTuner = Substitute.For<ISocketTuner>();
            log = new ConsoleLog();
            
            sender = new SocketsTransportRequestSender(settings, requestFactory, responseReader, socketTuner, log);
        }

        [TearDown]
        public void Teardown() => ConsoleLog.Flush();
        
        [Test]
        public async Task Should_handle_errors()
        {
            requestFactory
                .Create(null, default, out _)
                .ThrowsForAnyArgs(new ArgumentException());
            var response = await sender.SendAsync(null, null, CancellationToken.None);
            response.Code.Should().Be(ResponseCode.UnknownFailure);
        }

        [Test]
        public async Task Should_return_cancelled_on_OperationCancelledException_when_cancellation_requested()
        {
            using (var cts = new CancellationTokenSource())
            {
                cts.Cancel();
                
                requestFactory
                    .Create(null, default, out _)
                    .ThrowsForAnyArgs(new OperationCanceledException());
                var response = await sender.SendAsync(null, null, cts.Token);
                response.Code.Should().Be(ResponseCode.Canceled);
            }
        }

        [Test]
        public void Should_not_catch_StreamAlreadyUsedException()
        {
            requestFactory
                .Create(null, default, out _)
                .ThrowsForAnyArgs(new StreamAlreadyUsedException(""));
            
            new Action(() => sender.SendAsync(null, null, default).GetAwaiter().GetResult())
                .Should().Throw<StreamAlreadyUsedException>();
        }
    }
}