using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Session;
using NUnit.Framework;
using Vostok.ClusterClient.Core.Model;
using Vostok.ClusterClient.Transport.Sockets.Tests.Functional.Helpers;
using Vostok.Commons.Testing;

namespace Vostok.ClusterClient.Transport.Sockets.Tests.Functional
{
    [Explicit]
    internal class ConnectionIdleTimeoutTests : TransportTestsBase
    {
        [TestCase(10)]
        [TestCase(50)]
        public void Should_close_tcp_connection_by_connection_idle_timeout2(int connections)
        {
            SetSettings(s => s.ConnectionIdleTimeout = 1.Seconds());
                        
            using (var testServer = TestServer.StartNew(context =>
            {
                Thread.Sleep(1.Seconds());
                context.Response.StatusCode = 200;
            }))

            using (var sniffer = new DisconnectsSniffer(testServer.Port))
            {
                for (var i = 0; i < connections; ++i)
                    SendAsync(Request.Get(testServer.Url));
                
                new Action(() => sniffer.Disconnects.Should().Be(connections)).ShouldPassIn(8.Seconds());
            }
        }
        
        [TestCase(10)]
        [TestCase(50)]
        public void Should_not_close_tcp_connection_by_connection_idle_timeout(int connections)
        {
            SetSettings(s => s.ConnectionIdleTimeout = 30.Seconds());
            
            using (var testServer = TestServer.StartNew(context =>
            {
                Thread.Sleep(1.Seconds());
                context.Response.StatusCode = 200;
                context.Response.KeepAlive = true;
            }))
                
            using (var sniffer = new DisconnectsSniffer(testServer.Port))
            {
                for (var i = 0; i < connections; ++i)
                    SendAsync(Request.Get(testServer.Url).WithHeader("Connection", "keep-alive"));
                new Action(() => sniffer.Disconnects.Should().Be(0)).ShouldNotFailIn(8.Seconds());
            }
        }

        #region Sniffer
        private class DisconnectsSniffer : IDisposable
        {
            private TraceEventSession etwSession;
            private int disconnects;

            public int Disconnects => disconnects;

            public DisconnectsSniffer(int port)
            {                
                var etwSession = new TraceEventSession(nameof(DisconnectsSniffer));
                this.etwSession = etwSession;
                etwSession.EnableKernelProvider(KernelTraceEventParser.Keywords.NetworkTCPIP);
                etwSession.Source.Kernel.TcpIpDisconnect += data =>
                {
                    if (data.dport == port)
                        Interlocked.Increment(ref disconnects);
                };
                etwSession.Source.Kernel.TcpIpDisconnectIPV6 += data =>
                {
                    if (data.dport == port)
                        Interlocked.Increment(ref disconnects);
                };

                Task.Run(() =>etwSession.Source.Process());
            }

            public void Dispose()
            {
                etwSession?.Dispose();
            }
        }
        #endregion
    }
}