namespace Vostok.Clusterclient.Transport.Sockets
{
    internal class SocketsTransportConstants
    {
        public const int PooledBufferSize = 64*1024;
        public const int PreferredReadSize = PooledBufferSize;
        public const int LOHObjectSizeThreshold = 85*1000;
    }
}