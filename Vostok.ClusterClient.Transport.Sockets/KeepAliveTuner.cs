using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace Vostok.Clusterclient.Transport.Sockets
{
    internal static class KeepAliveTuner
    {
        public static byte[] GetKeepAliveValues(SocketsTransportSettings settings)
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

        public static void Tune(Socket socket, SocketsTransportSettings settings, byte[] keepAliveValues)
        {
            if (socket == null)
                return;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                socket.IOControl(IOControlCode.KeepAliveValues, keepAliveValues, null);
            }
            else
            {
                // see: https://github.com/dotnet/corefx/pull/29963
            }
        }
    }
}