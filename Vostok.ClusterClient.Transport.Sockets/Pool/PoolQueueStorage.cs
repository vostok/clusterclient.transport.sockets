using System.Collections.Concurrent;

namespace Vostok.ClusterClient.Transport.Webrequest.Pool
{
    internal class PoolQueueStorage<T> : IPoolStorage<T>
    {
        private readonly ConcurrentQueue<T> queue;

        public PoolQueueStorage() => queue = new ConcurrentQueue<T>();

        public int Count => queue.Count;

        public bool TryAcquire(out T resource) => queue.TryDequeue(out resource);

        public void Put(T resource) => queue.Enqueue(resource);
    }
}