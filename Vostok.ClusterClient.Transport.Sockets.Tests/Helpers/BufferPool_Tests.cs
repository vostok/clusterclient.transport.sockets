﻿using FluentAssertions;
using NUnit.Framework;
using Vostok.Clusterclient.Transport.Sockets.Helpers;

namespace Vostok.Clusterclient.Transport.Sockets.Tests.Helpers
{
    [TestFixture]
    internal class BufferPool_Tests
    {
        [Test]
        public void Should_give_out_buffers_exactly_of_needed_size()
        {
            using (BufferPool.Acquire(out var buffer))
            {
                buffer.Length.Should().Be(SocketsTransportConstants.PooledBufferSize);
            }
        }
    }
}