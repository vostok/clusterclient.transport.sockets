using System.Net;
using System.Net.Sockets;

namespace Vostok.Clusterclient.Transport.Sockets.Tests.Utilities
{
    internal static class FreeTcpPortFinder
    {
        public static int GetFreePort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            try
            {
                listener.Start();
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }
    }
}