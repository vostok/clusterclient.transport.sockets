using System.Net.Http;
using System.Threading;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport.Sockets.Contents;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Transport.Sockets.Tests.Contents
{
    [UsedImplicitly]
    internal class RequestByteArrayContent_Tests : RequestContent_Tests
    {
        private RequestByteArrayContent CreateRequestContent(Content content)
            => new RequestByteArrayContent(
                Request.Post("http://localhost").WithContent(content),
                new SendContext(),
                new SilentLog(),
                CancellationToken.None);

        protected override HttpContent CreateHttpContent(byte[] data, int offset, int length)
            => CreateRequestContent(new Content(data, offset, length));
    }
}