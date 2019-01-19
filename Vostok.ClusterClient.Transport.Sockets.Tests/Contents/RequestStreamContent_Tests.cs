using System.IO;
using System.Net.Http;
using System.Threading;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport.Sockets.Contents;
using Vostok.Logging.Abstractions;
using StreamContent = Vostok.Clusterclient.Core.Model.StreamContent;

namespace Vostok.Clusterclient.Transport.Sockets.Tests.Contents
{
    [UsedImplicitly]
    internal class RequestStreamContent_Tests : RequestContent_Tests
    {
        private RequestStreamContent CreateRequestContent(IStreamContent content)
            => new RequestStreamContent(
                Request.Get("http://localhost").WithContent(content),
                new SendContext(),
                new SilentLog(),
                CancellationToken.None);

        protected override HttpContent CreateHttpContent(byte[] data, int offset, int length)
            => CreateRequestContent(new StreamContent(new MemoryStream(data, offset, length)));
    }
}