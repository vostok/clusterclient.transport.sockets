using FluentAssertions;
using NUnit.Framework;
using Vostok.ClusterClient.Core.Model;
using Vostok.ClusterClient.Transport.Sockets.Tests.Functional.Helpers;
using Vostok.Commons.Threading;

namespace Vostok.ClusterClient.Transport.Sockets.Tests.Functional
{
    internal class ContentReceivingTests : TransportTestsBase
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(10)]
        [TestCase(500)]
        [TestCase(4096)]
        [TestCase(1024 * 1024)]
        [TestCase(4 * 1024 * 1024)]
        public void Should_be_able_to_receive_content_of_given_size(int size)
        {
            var content = ThreadSafeRandom.NextBytes(size);

            using (var server = TestServer.StartNew(
                ctx =>
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentLength64 = content.Length;
                    ctx.Response.OutputStream.Write(content, 0, content.Length);
                }))
            {
                var response = Send(Request.Put(server.Url));

                response.Content.ToArraySegment().Should().Equal(content);
            }
        }

        [Test]
        public void Should_read_response_body_greater_than_64k_with_non_successful_code()
        {
            var content = ThreadSafeRandom.NextBytes(100 * Constants.Kilobytes);

            using (var server = TestServer.StartNew(
                ctx =>
                {
                    ctx.Response.StatusCode = 409;
                    ctx.Response.OutputStream.Write(content, 0, content.Length);
                }))
            {
                var response = Send(Request.Put(server.Url));

                response.Code.Should().Be(ResponseCode.Conflict);
                response.Content.ToArraySegment().Should().Equal(content);
            }
        }

        [Test]
        public void Should_read_response_body_without_content_length()
        {
            var content = ThreadSafeRandom.NextBytes(500 * Constants.Kilobytes);

            using (var server = TestServer.StartNew(
                ctx =>
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.OutputStream.Write(content, 0, content.Length);
                }))
            {
                var response = Send(Request.Put(server.Url));

                response.Content.ToArraySegment().Should().Equal(content);
            }
        }

        [Test]
        public void Should_return_http_517_when_response_body_size_is_larger_than_configured_limit_when_content_length_is_known()
        {
            SetSettings(s => s.MaxResponseBodySize = Constants.Kilobytes);

            var content = ThreadSafeRandom.NextBytes(Constants.Kilobytes + 1);

            using (var server = TestServer.StartNew(
                ctx =>
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentLength64 = content.Length;
                    ctx.Response.OutputStream.Write(content, 0, content.Length);
                }))
            {
                var response = Send(Request.Put(server.Url));

                response.Code.Should().Be(ResponseCode.InsufficientStorage);
                response.Content.Length.Should().Be(0);
            }
        }

        [Test]
        public void Should_return_http_517_when_response_body_size_is_larger_than_configured_limit_when_content_length_is_unknown()
        {
            SetSettings(s => s.MaxResponseBodySize = Constants.Kilobytes);

            var content = ThreadSafeRandom.NextBytes(Constants.Kilobytes + 1);

            using (var server = TestServer.StartNew(
                ctx =>
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.OutputStream.Write(content, 0, content.Length);
                }))
            {
                var response = Send(Request.Put(server.Url));

                response.Code.Should().Be(ResponseCode.InsufficientStorage);
                response.Content.Length.Should().Be(0);
            }
        }

        [Test]
        public void Should_return_response_with_correct_content_length_when_buffer_factory_is_overriden()
        {
            SetSettings(s => s.BufferFactory = size => new byte[size * 2]);
            var content = ThreadSafeRandom.NextBytes(1234);

            using (var server = TestServer.StartNew(
                ctx =>
                {
                    ctx.Response.StatusCode = 200;
                    ctx.Response.ContentLength64 = content.Length;
                    ctx.Response.OutputStream.Write(content, 0, content.Length);
                }))
            {
                var response = Send(Request.Put(server.Url));

                response.Code.Should().Be(ResponseCode.Ok);
                response.Content.Buffer.Length.Should().Be(2468);
                response.Content.Offset.Should().Be(0);
                response.Content.Length.Should().Be(1234);
            }
        }

        [TestCase(1024*1024, 10)]
        [TestCase(1024*1024, 100)]
        [TestCase(1024*1024, 1024)]
        public void Should_support_response_streaming(int chunkSize, int chunksCount)
        {
            SetSettings(s => s.UseResponseStreaming = _ => true);

            using (var server = TestServer.StartNew(
                ctx =>
                {
                    var buffer = new byte[chunkSize];
                    ctx.Response.StatusCode = 200;
                    ctx.Response.SendChunked = true;
                    for (var i = 0; i < chunksCount; ++i)
                    {
                        ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    }
                }))
            {
                using (var response = Send(Request.Put(server.Url)))
                {
                    response.Code.Should().Be(ResponseCode.Ok);
                    response.HasContent.Should().BeFalse();
                    response.HasStream.Should().BeTrue();
                    var bytesRead = 0L;
                    var buffer = new byte[16 * Constants.Kilobytes];
                    while (true)
                    {
                        var size = response.Stream.Read(buffer);
                        bytesRead += size;
                        if (size <= 0)
                            break;
                    }

                    bytesRead.Should().Be(chunkSize * (long) chunksCount);
                }
            }
        }
    }
}