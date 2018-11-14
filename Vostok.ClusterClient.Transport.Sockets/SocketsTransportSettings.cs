using System;
using System.Net;
using System.Threading;
using JetBrains.Annotations;

namespace Vostok.Clusterclient.Transport.Sockets
{
    /// <summary>
    ///     A class that represents <see cref="SocketsTransport" /> settings.
    /// </summary>
    [PublicAPI]
    public class SocketsTransportSettings
    {
        /// <summary>
        ///     How much time connection will be alive after last usage. Note that if none other connections to endpoint is active,
        ///     ConnectionIdleTimeout will be divided by 4.
        /// </summary>
        public TimeSpan ConnectionIdleTimeout { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        ///     How much time client should wait for internal handler return after request cancellation.
        /// </summary>
        public TimeSpan RequestAbortTimeout { get; set; } = TimeSpan.FromMilliseconds(250);

        /// <summary>
        ///     Gets or sets a <see cref="IWebProxy" /> instance which will be used to send requests.
        /// </summary>
        public IWebProxy Proxy { get; set; }

        /// <summary>
        ///     Max connections count to single endpoint. When this limit is reached, request falls into queue and waits for free
        ///     connection.
        /// </summary>
        public int MaxConnectionsPerEndpoint { get; set; } = 10 * 1000;

        /// <summary>
        ///     Gets or sets the maximum response body size in bytes. This parameter doesn't affect content streaming.
        /// </summary>
        public long? MaxResponseBodySize { get; set; }

        /// <summary>
        ///     Gets or sets the maximum amount of data that can be drained from responses in bytes.
        /// </summary>
        public int? MaxResponseDrainSize { get; set; }

        /// <summary>
        ///     Gets or sets the delegate that decide use response streaming or not.
        /// </summary>
        public Predicate<long?> UseResponseStreaming { get; set; } = _ => false;

        /// <summary>
        ///     Gets or sets a value that indicates whether the transport should follow HTTP redirection responses.
        /// </summary>
        public bool AllowAutoRedirect { get; set; }

        /// <summary>
        ///     Gets or sets a maximum time to live of TCP connection.
        /// </summary>
        public TimeSpan ConnectionLifetime { get; set; } = Timeout.InfiniteTimeSpan;

        /// <summary>
        ///     Enables TCP keep alive.
        /// </summary>
        public bool TcpKeepAliveEnabled { get; set; }

        /// <summary>
        ///     Enables ARP cache warmup.
        /// </summary>
        public bool ArpCacheWarmupEnabled { get; set; }

        /// <summary>
        ///     Gets or sets the duration between two keepalive transmissions in idle condition.
        /// </summary>
        public TimeSpan TcpKeepAliveTime { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>
        ///     Gets ot sets the duration between two successive keepalive retransmissions, if acknowledgement to the previous
        ///     keepalive
        ///     transmission is not received.
        /// </summary>
        public TimeSpan TcpKeepAliveInterval { get; set; } = TimeSpan.FromSeconds(1);

        internal Func<int, byte[]> BufferFactory { get; set; } = size => new byte[size];

        internal SocketsTransportSettings Clone() => new SocketsTransportSettings
        {
            UseResponseStreaming = UseResponseStreaming,
            MaxResponseBodySize = MaxResponseBodySize,
            BufferFactory = BufferFactory,
            Proxy = Proxy,
            AllowAutoRedirect = AllowAutoRedirect,
            ConnectionIdleTimeout = ConnectionIdleTimeout,
            RequestAbortTimeout = RequestAbortTimeout,
            MaxConnectionsPerEndpoint = MaxConnectionsPerEndpoint,
            TcpKeepAliveEnabled = TcpKeepAliveEnabled,
            ConnectionLifetime = ConnectionLifetime,
            MaxResponseDrainSize = MaxResponseDrainSize,
            TcpKeepAliveInterval = TcpKeepAliveInterval,
            TcpKeepAliveTime = TcpKeepAliveTime
        };
    }
}