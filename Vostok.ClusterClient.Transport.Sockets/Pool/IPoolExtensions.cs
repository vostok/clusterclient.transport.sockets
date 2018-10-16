using System;
using System.Collections.Generic;

namespace Vostok.ClusterClient.Transport.Webrequest.Pool
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

        public static void Preallocate<T>(this IPool<T> pool, int count)
            where T : class
        {
            var resources = new List<T>();

            for (var i = 0; i < count; i++)
                resources.Add(pool.Acquire());

            foreach (var resource in resources)
                pool.Release(resource);
        }
    }
}