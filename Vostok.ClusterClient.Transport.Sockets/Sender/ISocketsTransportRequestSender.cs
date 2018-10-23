using System;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Transport.Sockets.Sender
{
    internal interface ISocketsTransportRequestSender : IDisposable
    {
        Task<Response> SendAsync(Request request, TimeSpan connectionTimeout, CancellationToken cancellationToken);
    }
}