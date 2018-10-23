using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;

namespace Vostok.Clusterclient.Transport.Sockets.Sender
{
    internal interface ISocketsTransportRequestSender
    {
        Task<Response> SendAsync(HttpClient client, Request request, CancellationToken cancellationToken);
    }
}