using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Vostok.ClusterClient.Core.Model;

namespace Vostok.ClusterClient.Transport.Sockets
{
    public class SocketsTransportSettings
    {
        public bool Pipelined { get; set; } = true;

        public bool FixThreadPoolProblems { get; set; } = true;

        public int ConnectionAttempts { get; set; } = 2;

        public TimeSpan? ConnectionTimeout { get; set; } = TimeSpan.FromMilliseconds(750);

        public TimeSpan ConnectionIdleTimeout { get; set; } = TimeSpan.FromMinutes(2);

        public TimeSpan RequestAbortTimeout { get; set; } = TimeSpan.FromMilliseconds(250);

        public IWebProxy Proxy { get; set; } = null;

        public int MaxConnectionsPerEndpoint { get; set; } = 10 * 1000;

        public long? MaxResponseBodySize { get; set; } = null;

        public Predicate<long?> UseResponseStreaming { get; set; } = _ => false;

        public string ConnectionGroupName { get; set; } = null;

        public bool AllowAutoRedirect { get; set; } = false;

        public bool TcpKeepAliveEnabled { get; set; } = false;

        public TimeSpan TcpKeepAliveTime { get; set; } = TimeSpan.FromSeconds(3);

        public TimeSpan TcpKeepAlivePeriod { get; set; } = TimeSpan.FromSeconds(1);

        public bool ArpCacheWarmupEnabled { get; set; } = false;

        public X509Certificate2[] ClientCertificates { get; set; } = null;

        public Action<SocketsHttpHandler> Tune { get; set; } = null;
        
        internal Func<int, byte[]> BufferFactory { get; set; } = size => new byte[size];

        internal bool FixNonAsciiHeaders { get; set; } = false;

        internal SocketsTransportSettings Clone()
        {
            return new SocketsTransportSettings
            {
                UseResponseStreaming = UseResponseStreaming,
                MaxResponseBodySize = MaxResponseBodySize,
                BufferFactory = BufferFactory,
                Proxy = Proxy,
                ConnectionAttempts = ConnectionAttempts,
                ClientCertificates = ClientCertificates,
                ConnectionTimeout = ConnectionTimeout,
                Pipelined = Pipelined,
                Tune = Tune,
                AllowAutoRedirect = AllowAutoRedirect,
                ConnectionGroupName = ConnectionGroupName,
                ConnectionIdleTimeout = ConnectionIdleTimeout,
                RequestAbortTimeout = RequestAbortTimeout,
                ArpCacheWarmupEnabled = ArpCacheWarmupEnabled,
                FixNonAsciiHeaders = FixNonAsciiHeaders,
                FixThreadPoolProblems = FixThreadPoolProblems,
                MaxConnectionsPerEndpoint = MaxConnectionsPerEndpoint,
                TcpKeepAliveEnabled = TcpKeepAliveEnabled,
                TcpKeepAlivePeriod = TcpKeepAlivePeriod,
                TcpKeepAliveTime = TcpKeepAliveTime
            };
        }
    }

    internal class RequestState
    {
        public RequestState(Request request, CancellationTokenSource cancellationTokenSource)
        {
            Request = request;
            this.cancellationTokenSource = cancellationTokenSource;
        }

        public Request Request;
        private readonly CancellationTokenSource cancellationTokenSource;
        public HttpActionStatus Status;
        public CancellationToken CancellationToken => cancellationTokenSource.Token;

        public void Cancel()
        {
            try
            {
                cancellationTokenSource.Cancel();
            }
            catch{ }
        }
    }

    internal enum HttpActionStatus
    {
        Success,
        ConnectionFailure,
        SendFailure,
        ReceiveFailure,
        Timeout,
        RequestCanceled,
        ProtocolError,
        UnknownFailure,
        InsufficientStorage,
        UserStreamFailure
    }
}