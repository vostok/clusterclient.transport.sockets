using System;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;

namespace Vostok.ClusterClient.Transport.Sockets
{
    public class SocketsTransportSettings
    {
        public int ConnectionAttempts { get; set; } = 2;

        public TimeSpan? ConnectionTimeout { get; set; } = TimeSpan.FromMilliseconds(750);

        public TimeSpan ConnectionIdleTimeout { get; set; } = TimeSpan.FromMinutes(2);

        public TimeSpan RequestAbortTimeout { get; set; } = TimeSpan.FromMilliseconds(250);

        public IWebProxy Proxy { get; set; } = null;

        public int MaxConnectionsPerEndpoint { get; set; } = 10 * 1000;

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
                ConnectionTimeout = ConnectionTimeout,
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