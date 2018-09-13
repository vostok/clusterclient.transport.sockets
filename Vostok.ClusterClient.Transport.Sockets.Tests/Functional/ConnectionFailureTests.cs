using FluentAssertions;
using NUnit.Framework;
using Vostok.ClusterClient.Core.Model;
using Vostok.ClusterClient.Transport.Sockets.Tests.Utilities;

namespace Vostok.ClusterClient.Transport.Sockets.Tests.Functional
{
    internal class ConnectionFailureTests : TransportTestsBase
    {
        [Test]
        public void Should_return_ConnectFailure_code_when_request_url_is_wrong()
        {
            var response = Send(Request.Get("http://255.255.255.255/"));

            response.Code.Should().Be(ResponseCode.ConnectFailure);
        }

        [Test]
        public void Should_return_ConnectFailure_code_when_request_url_contains_unresolvable_hostname()
        {
            var response = Send(Request.Get("http://sdoghisguhodfgkdjfgdsfgj:7545/"));

            response.Code.Should().Be(ResponseCode.ConnectFailure);
        }

        [Test]
        public void Should_return_ConnectFailure_code_when_server_does_not_listen_on_needed_port()
        {
            var response = Send(Request.Get($"http://localhost:{FreeTcpPortFinder.GetFreePort()}/"));

            response.Code.Should().Be(ResponseCode.ConnectFailure);
        }
    }
}