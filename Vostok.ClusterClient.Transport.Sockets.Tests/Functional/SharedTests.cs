using System;
using Vostok.ClusterClient.Core.Transport;
using Vostok.ClusterClient.Transport.Tests.Functional;
using Vostok.Logging.Abstractions;
using Vostok.Logging.Console;

namespace Vostok.ClusterClient.Transport.Sockets.Tests.Functional
{
    public class Config : ITransportTestConfig
    {
        public ILog CreateLog() => new ConsoleLog();

        public ITransport CreateTransport(TestTransportSettings settings, ILog log)
        {
            var transportSettings = new SocketsTransportSettings
            {
                UseResponseStreaming = settings.UseResponseStreaming,
                Proxy = settings.Proxy,
                BufferFactory = settings.BufferFactory,
                ConnectionAttempts = settings.ConnectionAttempts,
                ConnectionTimeout = settings.ConnectionTimeout,
                AllowAutoRedirect = settings.AllowAutoRedirect,
                MaxResponseBodySize = settings.MaxResponseBodySize,
                MaxConnectionsPerEndpoint = settings.MaxConnectionsPerEndpoint
            };
            return new SocketsTransport(transportSettings, log);
        }

        public TestTransportSettings CreateDefaultSettings() => new TestTransportSettings
        {
            MaxConnectionsPerEndpoint = 10 * 1000,
            ConnectionAttempts = 2,
            ConnectionTimeout = TimeSpan.FromMilliseconds(750),
            BufferFactory = size => new byte[size],
            UseResponseStreaming = _ => false
        };
    }

    internal class AllowAutoRedirectTests : AllowAutoRedirectTests<Config>
    {
    }
    internal class ClientTimeoutTests : ClientTimeoutTests<Config>
    {
    }
    internal class ConnectionFailureTests : ConnectionFailureTests<Config>
    {
    }
    internal class ConnectionTimeoutTests : ConnectionTimeoutTests<Config>
    {
    }
    internal class ContentReceivingTests : ContentReceivingTests<Config>
    {
    }
    internal class ContentSendingTests : ContentSendingTests<Config>
    {
    }
    internal class HeaderReceivingTests : HeaderReceivingTests<Config>
    {
    }
    internal class HeaderSendingTests : HeaderSendingTests<Config>
    {
    }
    internal class MaxConnectionsPerEndpointTests : MaxConnectionsPerEndpointTests<Config>
    {
    }
    internal class MethodSendingTests : MethodSendingTests<Config>
    {
    }
    internal class ProxyTests : ProxyTests<Config>
    {
    }
    internal class QuerySendingTests : QuerySendingTests<Config>
    {
    }
    internal class RequestCancellationTests : RequestCancellationTests<Config>
    {
    }
    internal class StatusCodeReceivingTests : StatusCodeReceivingTests<Config>
    {
    }
}