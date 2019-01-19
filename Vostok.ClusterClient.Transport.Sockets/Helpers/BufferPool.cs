using System;
using System.Buffers;
using JetBrains.Annotations;

namespace Vostok.Clusterclient.Transport.Sockets.Helpers
{
    internal static class BufferPool
    {
        [NotNull]
        public static IDisposable Acquire(out byte[] buffer)
        {
            return new Releaser(buffer = ArrayPool<byte>.Shared.Rent(SocketsTransportConstants.PooledBufferSize));
        }

        private class Releaser : IDisposable
        {
            private readonly byte[] buffer;

            public Releaser(byte[] buffer)
            {
                this.buffer = buffer;
            }

            public void Dispose()
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }
}
