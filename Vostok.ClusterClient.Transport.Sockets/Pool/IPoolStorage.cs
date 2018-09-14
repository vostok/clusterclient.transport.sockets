namespace Vostok.ClusterClient.Transport.Webrequest.Pool
{
    internal interface IPoolStorage<T>
    {
        int Count { get; }
        bool TryAcquire(out T resource);

        void Put(T resource);
    }
}