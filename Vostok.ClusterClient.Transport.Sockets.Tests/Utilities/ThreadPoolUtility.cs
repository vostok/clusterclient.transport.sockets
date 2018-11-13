using System;
using System.Threading;

namespace Vostok.Clusterclient.Transport.Sockets.Tests.Utilities
{
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
}