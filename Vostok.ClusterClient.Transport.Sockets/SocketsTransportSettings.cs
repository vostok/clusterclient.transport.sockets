using System;
using System.Net;
using System.Net.Http;

namespace Vostok.ClusterClient.Transport.Sockets
{
    public class SocketsTransportSettings
    {
        /// <summary>
        /// An attempts count to establish TCP connection with target host.
        /// </summary>
        public int ConnectionAttempts { get; set; } = 2;

        /// <summary>
        /// How much time connection will be alive after last usage. Note that if none other connections to endpoint is active, ConnectionIdleTimeout will be divided by 4.
        /// </summary>
        public TimeSpan ConnectionIdleTimeout { get; set; } = TimeSpan.FromMinutes(2);

        /// <summary>
        /// How much time client should wait for internal handler return after request cancellation.
        /// </summary>
        public TimeSpan RequestAbortTimeout { get; set; } = TimeSpan.FromMilliseconds(250);

        /// <summary>
        /// A <see cref="IWebProxy"/> instance which will be used to send requests.
        /// </summary>
        public IWebProxy Proxy { get; set; } = null;

        /// <summary>
        /// Max connections count to single endpoint. When this limit is reached, request falls into queue and wait for free connection.
        /// </summary>
        public int MaxConnectionsPerEndpoint { get; set; } = 10 * 1000;

        /// <summary>
        /// Max response body size in bytes. This parameter doesn't affect on content streaming.
        /// </summary>
        public long? MaxResponseBodySize { get; set; } = null;
        
        public int? MaxResponseDrainSize { get; set; } = null;

        public Predicate<long?> UseResponseStreaming { get; set; } = _ => false;

        public bool AllowAutoRedirect { get; set; } = false;

        public TimeSpan ConnectionLifetime { get; set; } = TimeSpan.FromSeconds(3);

        public Action<SocketsHttpHandler> Tune { get; set; } = null;
        
        internal Func<int, byte[]> BufferFactory { get; set; } = size => new byte[size];
        
        public bool TcpKeepAliveEnabled { get; set; } = false;
                
        public bool ArpCacheWarmupEnabled { get; set; } = false;

        /// <summary>
        /// The duration between two keepalive transmissions in idle condition.
        /// </summary>
        public TimeSpan TcpKeepAliveTime { get; set; } = TimeSpan.FromSeconds(3);

        /// <summary>
        /// The duration between two successive keepalive retransmissions, if acknowledgement to the previous keepalive transmission is not received.
        /// </summary>
        public TimeSpan TcpKeepAliveInterval { get; set; } = TimeSpan.FromSeconds(1);

        internal SocketsTransportSettings Clone()
        {
            return new SocketsTransportSettings
            {
                UseResponseStreaming = UseResponseStreaming,
                MaxResponseBodySize = MaxResponseBodySize,
                BufferFactory = BufferFactory,
                Proxy = Proxy,
                ConnectionAttempts = ConnectionAttempts,
                Tune = Tune,
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
}