using FluentAssertions;
using NUnit.Framework;

namespace Vostok.Clusterclient.Transport.Sockets.Tests
{
    [TestFixture]
    internal class SocketsHandlerInvoker_Tests
    {
        [Test]
        public void Should_be_able_to_invoke_SocketsHttpHandler_directly()
        {
            SocketsHandlerInvoker.CanInvokeDirectly.Should().BeTrue();
        }
    }
}