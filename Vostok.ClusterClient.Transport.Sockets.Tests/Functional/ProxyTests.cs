using System;
using System.Net;
using FluentAssertions;
using NUnit.Framework;
using Vostok.ClusterClient.Core.Model;
using Vostok.ClusterClient.Transport.Sockets.Tests.Functional.Helpers;

namespace Vostok.ClusterClient.Transport.Sockets.Tests.Functional
{
    internal class ProxyTests : TransportTestsBase
    {
        [Test]
        public void Should_send_request_to_proxy_if_setting_is_true()
        {
            using (var proxy = TestServer.StartNew(ctx => ctx.Response.StatusCode = 201))
            {
                SetSettings(s => s.Proxy = new WebProxy(proxy.Url, false));

                var expectedUrl = new Uri($"http://vostok:{proxy.Port}");
                
                var request = Request.Get("http://vostok");
                var response = Send(request);
                response.Code.Should().Be(ResponseCode.Created);
                proxy.LastRequest.Url.Should().Be(expectedUrl);
            }
        }
    }
}