using System;
using System.Net.Http;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Clusterclient.Transport.Sockets.Pool;

namespace Vostok.Clusterclient.Transport.Sockets.Tests.Contents
{
    internal abstract class RequestContent_Tests
    {
        [TestCase(1, 0, 1)]
        [TestCase(10000, 0, 10000)]
        [TestCase(100000, 0, 100000)]
        [TestCase(10000, 10, 1000)]
        [TestCase(100000, 10, 90001, 100)]
        [TestCase(100000, 10, 90001, 1000)]
        public void Should_contain_correct_content(int arrayLength, int offset, int length, int bufferSize=16484)
        {
            var bytes = new byte[arrayLength];
            new Random(42).NextBytes(bytes);

            var requestContent = CreateHttpContent(bytes, offset, length, new Pool<byte[]>(() => new byte[bufferSize]));
            
            var result = requestContent.ReadAsByteArrayAsync().GetAwaiter().GetResult();
            result.Should().BeEquivalentTo(new Span<byte>(bytes, offset, length).ToArray());
        }

        protected abstract HttpContent CreateHttpContent(byte[] data, int offset, int length, IPool<byte[]> pool);
    }
}