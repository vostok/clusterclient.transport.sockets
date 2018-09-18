using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.ClusterClient.Core.Model;
using Vostok.ClusterClient.Transport.Sockets.Tests.Utilities;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;

namespace Vostok.ClusterClient.Transport.Sockets.Tests.Functional
{
    [TestFixture]
    internal class TransportTestsBase
    {
        protected ILog log;
        protected SocketsTransport transport;

        static TransportTestsBase()
        {
            ThreadPoolUtility.SetUp();
        }

        [SetUp]
        public virtual void SetUp()
        {
            log = new ConsoleLog();
            transport = new SocketsTransport(new SocketsTransportSettings(), log);
        }

        protected Task<Response> SendAsync(Request request, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return transport.SendAsync(request, timeout ?? 1.Minutes(), cancellationToken);
        }

        protected Response Send(Request request, TimeSpan? timeout = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            return transport.SendAsync(request, timeout ?? 1.Minutes(), cancellationToken).GetAwaiter().GetResult();
        }

        protected void SetSettings(Action<SocketsTransportSettings> update)
        {
            var settings = new SocketsTransportSettings();
            update(settings);
            transport = new SocketsTransport(settings, log);
        }
    }
}