using System.Net.Sockets;

namespace Vostok.Clusterclient.Transport.Sockets
{
    internal interface IKeepAliveTuner
    {
        void Tune(Socket socket);
    }
}