using System.Threading;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport.Sockets.Client;

namespace Vostok.Clusterclient.Transport.Sockets.Sender
{
    internal interface IInternalTransport
    {
        Task<Response> SendAsync(IHttpClient client, Request request, CancellationToken cancellationToken);
    }
}