using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport.Sockets.Pool;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Transport.Sockets.Tests
{
    internal class ResponseReader_Tests
    {
        private SocketsTransportSettings settings;
        private ResponseReader reader;
        
        [SetUp]
        public void Setup()
        {
            settings = new SocketsTransportSettings();
            reader = new ResponseReader(
                settings,
                new Pool<byte[]>(() => new byte[100]),
                new SilentLog());

        }

        [TestCase(null)]
        [TestCase(100)]
        public void Should_consider_UseResponseStreaming_setting(int? contentLength)
        {
            var bytes = new byte[contentLength ?? 100];
            var response = new HttpResponseMessage
            {
                Content = new ByteArrayContent(bytes, 0, bytes.Length) { Headers = { ContentLength = contentLength} }
            };
            
            ResponseReadResult Read() => reader.ReadResponseBodyAsync(response, CancellationToken.None).GetAwaiter().GetResult();

            settings.UseResponseStreaming = l => l != contentLength;
            Read().Content.Should().NotBeNull();

            settings.UseResponseStreaming = l => l == contentLength;
            Read().Stream.Should().NotBeNull();
        }
        
        [TestCase(1, 1)]
        [TestCase(10, 10)]
        [TestCase(100000, 100000)]
        [TestCase(1, null)]
        [TestCase(10, null)]
        [TestCase(100000, null)]
        public void Should_read_Content_from_response_body(int length, int? contentLength)
        {
            var bytes = new byte[length];
            new Random(42).NextBytes(bytes);

            var response = new HttpResponseMessage
            {
                Content = new ByteArrayContent(bytes, 0, length) {Headers = { ContentLength = contentLength}}
            };

            var result = reader.ReadResponseBodyAsync(response, CancellationToken.None).GetAwaiter().GetResult();

            result.Content.Should().NotBeNull();
            result.Stream.Should().BeNull();
            result.ErrorCode.Should().BeNull();

            new Span<byte>(result.Content.Buffer, result.Content.Offset, result.Content.Length)
                .ToArray()
                .Should()
                .BeEquivalentTo(bytes);
        }
        
        [TestCase(1, 1)]
        [TestCase(10, 10)]
        [TestCase(100000, 100000)]
        [TestCase(1, null)]
        [TestCase(10, null)]
        [TestCase(100000, null)]
        public void Should_read_Stream_from_response_body(int length, int? contentLength)
        {
            var bytes = new byte[length];
            new Random(42).NextBytes(bytes);

            var response = new HttpResponseMessage
            {
                Content = new ByteArrayContent(bytes, 0, length) {Headers = { ContentLength = contentLength}}
            };

            settings.UseResponseStreaming = _ => true;
            
            var result = reader.ReadResponseBodyAsync(response, CancellationToken.None).GetAwaiter().GetResult();

            result.Content.Should().BeNull();
            result.Stream.Should().NotBeNull();
            result.ErrorCode.Should().BeNull();

            var ms = new MemoryStream();
            result.Stream.CopyTo(ms);
            
            ms
                .ToArray()
                .Should()
                .BeEquivalentTo(bytes);
        }
        
        [TestCase(1, 2)]
        [TestCase(100000, 100001)]
        public void Should_save_ReceiveFailure_error_code_when_content_length_is_wrong(int length, int? contentLength)
        {
            var bytes = new byte[length];
            new Random(42).NextBytes(bytes);

            var response = new HttpResponseMessage
            {
                Content = new ByteArrayContent(bytes, 0, length) {Headers = { ContentLength = contentLength}}
            };

            var result = reader.ReadResponseBodyAsync(response, CancellationToken.None).GetAwaiter().GetResult();

            result.Content.Should().BeNull();
            result.Stream.Should().BeNull();
            result.ErrorCode.Should().Be(ResponseCode.ReceiveFailure);
        }
        
        [Test]        
        public void Should_not_dispose_underlying_response_content()
        {
            var content = new TestHttpContent();
            
            var response = new HttpResponseMessage
            {
                Content = content
            };

            settings.UseResponseStreaming = _ => true;
            
            reader.ReadResponseBodyAsync(response, CancellationToken.None).GetAwaiter().GetResult();

            content.IsDisposed.Should().BeFalse();
        }

        private class TestHttpContent : HttpContent
        {
            public bool IsDisposed;
            
            protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)=> Task.CompletedTask;

            protected override bool TryComputeLength(out long length)
            {
                length = 0;
                return true;
            }

            protected override void Dispose(bool disposing)
            {
                IsDisposed = true;
                base.Dispose(disposing);
            }
        }

    }
}