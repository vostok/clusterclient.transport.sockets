using System.Collections.Concurrent;

namespace Vostok.ClusterClient.Transport.Webrequest.Pool
{
    internal class PoolStackStorage<T> : IPoolStorage<T>
    {
        private readonly ConcurrentStack<T> stack;

        public PoolStackStorage() => stack = new ConcurrentStack<T>();

        public int Count => stack.Count;

        public bool TryAcquire(out T resource) => stack.TryPop(out resource);

        public void Put(T resource) => stack.Push(resource);
    }
}