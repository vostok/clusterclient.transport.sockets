using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Vostok.Clusterclient.Transport.Sockets
{
    internal class KeepAliveTuner : IKeepAliveTuner
    {
        private readonly byte[] keepAliveValues;

        public KeepAliveTuner(SocketsTransportSettings settings)
            => keepAliveValues = GetKeepAliveValues(settings);

        public void Tune(Socket socket)
        {
            if (socket == null)
                return;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                socket.IOControl(IOControlCode.KeepAliveValues, keepAliveValues, null);
            }
        }

        private static byte[] GetKeepAliveValues(SocketsTransportSettings settings)
        {
            if (!settings.TcpKeepAliveEnabled || !RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return null;

            var tcpKeepAliveTime = (int) settings.TcpKeepAliveTime.TotalMilliseconds;
            var tcpKeepAliveInterval = (int) settings.TcpKeepAliveInterval.TotalMilliseconds;

            return new byte[]
            {
                1,
                0,
                0,
                0,
                (byte) (tcpKeepAliveTime & byte.MaxValue),
                (byte) ((tcpKeepAliveTime >> 8) & byte.MaxValue),
                (byte) ((tcpKeepAliveTime >> 16) & byte.MaxValue),
                (byte) ((tcpKeepAliveTime >> 24) & byte.MaxValue),
                (byte) (tcpKeepAliveInterval & byte.MaxValue),
                (byte) ((tcpKeepAliveInterval >> 8) & byte.MaxValue),
                (byte) ((tcpKeepAliveInterval >> 16) & byte.MaxValue),
                (byte) ((tcpKeepAliveInterval >> 24) & byte.MaxValue)
            };
        }
    }
}