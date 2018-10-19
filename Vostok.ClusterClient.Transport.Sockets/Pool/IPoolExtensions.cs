using System;

namespace Vostok.Clusterclient.Transport.Sockets.Pool
{
    internal static class IPoolExtensions
    {
        /// <summary>
        /// Acquires a resource from pool and wraps it into a disposable handle which releases resource on disposal.
        /// </summary>
        public static IDisposable AcquireHandle<T>(this IPool<T> pool, out T resource)
            where T : class
        {
            resource = pool.Acquire();
            return new PoolHandle<T>(pool, resource);
        }
    }
}