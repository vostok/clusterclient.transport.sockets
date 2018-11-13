using System;
using System.IO;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport.Sockets.Contents;
using Vostok.Clusterclient.Transport.Sockets.Messages;
using Vostok.Clusterclient.Transport.Sockets.Pool;
using Vostok.Logging.Console;

namespace Vostok.Clusterclient.Transport.Sockets.Tests
{
    internal class HttpRequestMessageFactory_Tests
    {
        private readonly HttpRequestMessageFactory factory = new HttpRequestMessageFactory(
            new Pool<byte[]>(() => new byte[1000]),
            new ConsoleLog());
        
        private Request request = Request.Post("http://localhost");
        
        [Test]
        public void Should_create_HttpRequestMessage_with_empty_content_when_content_is_not_specified()
        {
            var message = factory.Create(request, CancellationToken.None, out _);
            message.Content.Should().BeOfType<RequestEmptyContent>();
        }
        
        [Test]
        public void Should_create_HttpRequestMessage_with_empty_content_when_content_is_empty()
        {
            var message = factory.Create(request.WithContent(Content.Empty), CancellationToken.None, out _);
            message.Content.Should().BeOfType<RequestEmptyContent>();
        }
        
        [Test]
        public void Should_create_HttpRequestMessage_with_byte_array_content_when_Content_is_set()
        {
            var content = new Content(new byte[4], 0, 4);
            var message = factory.Create(request.WithContent(content), CancellationToken.None, out _);
            message.Content.Should().BeOfType<RequestByteArrayContent>();
        }
        
        [Test]
        public void Should_create_HttpRequestMessage_with_correct_byte_array_content()
        {
            var bytes = new byte[100000];
            var offset = 7;
            var length = 99000;
            new Random(42).NextBytes(bytes);
            var content = new Content(bytes, offset, length);
            var message = factory.Create(request.WithContent(content), CancellationToken.None, out _);
            
            message.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult().Should().BeEquivalentTo(new Span<byte>(bytes, offset, length).ToArray());
        }

        [Test]
        public void Should_create_HttpRequestMessage_with_stream_content_when_StreamContent_is_set()
        {
            var content = new StreamContent(new MemoryStream(new byte[4], 0, 4));
            var message = factory.Create(request.WithContent(content), CancellationToken.None, out _);
            message.Content.Should().BeOfType<RequestStreamContent>();
        }
        
        [Test]
        public void Should_create_HttpRequestMessage_with_correct_stream_content()
        {
            var bytes = new byte[100000];
            var offset = 7;
            var length = 99000;
            new Random(42).NextBytes(bytes);
            var content = new StreamContent(new MemoryStream(bytes, offset, length));
            var message = factory.Create(request.WithContent(content), CancellationToken.None, out _);
            
            message.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult().Should().BeEquivalentTo(new Span<byte>(bytes, offset, length).ToArray());
        }

        [Test]
        public void Should_create_HttpRequestMessage_with_stream_content_when_StreamContent_with_length_is_set()
        {
            var content = new StreamContent(new MemoryStream(new byte[4], 0, 4), 4);
            var message = factory.Create(request.WithContent(content), CancellationToken.None, out _);
            message.Content.Should().BeOfType<RequestStreamContent>();
        }
        
        [Test]
        public void Should_create_HttpRequestMessage_with_correct_stream_content_with_length()
        {
            var bytes = new byte[100000];
            var offset = 7;
            var length = 99000;
            new Random(42).NextBytes(bytes);
            var content = new StreamContent(new MemoryStream(bytes, offset, length), length);
            var message = factory.Create(request.WithContent(content), CancellationToken.None, out _);
            
            message.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult().Should().BeEquivalentTo(new Span<byte>(bytes, offset, length).ToArray());
        }
    }
}