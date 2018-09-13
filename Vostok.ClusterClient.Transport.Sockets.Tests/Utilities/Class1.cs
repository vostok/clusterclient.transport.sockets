using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Vostok.ClusterClient.Transport.Sockets.Tests.Utilities
{
    internal class FreeTcpPortFinder
    {
        public static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                listener.Start();
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }
    }
    
    internal static class ThreadPoolUtility
    {
        public const int MaximumThreads = short.MaxValue;

        public static void SetUp(int multiplier = 128)
        {
            if (multiplier <= 0)
                return;

            var minimumThreads = Math.Min(Environment.ProcessorCount*multiplier, MaximumThreads);

            ThreadPool.SetMaxThreads(MaximumThreads, MaximumThreads);
            ThreadPool.SetMinThreads(minimumThreads, minimumThreads);
            ThreadPool.GetMinThreads(out _, out _);
            ThreadPool.GetMaxThreads(out _, out _);
        }

        public static ThreadPoolState GetPoolState()
        {
            ThreadPool.GetMinThreads(out var minWorkerThreads, out var minIocpThreads);
            ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxIocpThreads);
            ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableIocpThread);

            return new ThreadPoolState(
                minWorkerThreads,
                maxWorkerThreads - availableWorkerThreads,
                minIocpThreads,
                maxIocpThreads - availableIocpThread);
        }
    }
    
    internal struct ThreadPoolState
    {
        public ThreadPoolState(int minWorkerThreads, int usedThreads, int minIocpThreads, int usedIocpThreads)
            : this()
        {
            MinWorkerThreads = minWorkerThreads;
            UsedThreads = usedThreads;
            MinIocpThreads = minIocpThreads;
            UsedIocpThreads = usedIocpThreads;
        }

        public int MinWorkerThreads { get; }
        public int UsedThreads { get; }
        public int MinIocpThreads { get; }
        public int UsedIocpThreads { get; }

        public override bool Equals(object obj) =>
            !ReferenceEquals(null, obj) &&
            obj is ThreadPoolState state && Equals(state);

        public bool Equals(ThreadPoolState other) =>
            MinWorkerThreads == other.MinWorkerThreads &&
            UsedThreads == other.UsedThreads &&
            MinIocpThreads == other.MinIocpThreads &&
            UsedIocpThreads == other.UsedIocpThreads;

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = MinWorkerThreads;
                hashCode = (hashCode * 397) ^ UsedThreads;
                hashCode = (hashCode * 397) ^ MinIocpThreads;
                hashCode = (hashCode * 397) ^ UsedIocpThreads;
                return hashCode;
            }
        }
    }
}