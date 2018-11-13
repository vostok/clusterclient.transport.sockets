using FluentAssertions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport.Sockets.Contents;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Transport.Sockets.Tests.Contents
{
    internal class RequestEmptyContent_Tests
    {
        [Test]
        public void Should_be_empty()
            => new RequestEmptyContent(Request.Get("http://localhost/"), new SendContext(), new SilentLog())
                .ReadAsByteArrayAsync()
                .GetAwaiter()
                .GetResult()
                .Should()
                .BeEmpty();
    }
}