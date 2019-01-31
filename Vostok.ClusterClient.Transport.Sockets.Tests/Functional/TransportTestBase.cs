using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Commons.Threading;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;

namespace Vostok.Clusterclient.Transport.Sockets.Tests.Functional
{
    [TestFixture]
    internal class TransportTestsBase
    {
        private ILog log;
        private SocketsTransport transport;

        static TransportTestsBase()
        {
            ThreadPoolUtility.Setup();
        }

        [SetUp]
        public void SetUp()
        {
            log = new ConsoleLog();
            transport = new SocketsTransport(new SocketsTransportSettings(), log);
        }

        protected Task<Response> SendAsync(Request request, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return transport.SendAsync(request, null, timeout ?? 1.Minutes(), cancellationToken);
        }

        protected Response Send(Request request, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return transport.SendAsync(request, null, timeout ?? 1.Minutes(), cancellationToken).GetAwaiter().GetResult();
        }

        protected void SetSettings(Action<SocketsTransportSettings> update)
        {
            var settings = new SocketsTransportSettings();
            update(settings);
            transport = new SocketsTransport(settings, log);
        }
    }
}