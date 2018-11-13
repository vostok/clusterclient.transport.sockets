using System.Net.Sockets;

namespace Vostok.Clusterclient.Transport.Sockets.Hacks
{
    internal interface ISocketTuner
    {
        void Tune(Socket socket);
    }
}