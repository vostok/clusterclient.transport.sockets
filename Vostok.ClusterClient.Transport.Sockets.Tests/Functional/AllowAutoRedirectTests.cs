using System;
using System.Net;
using FluentAssertions;
using NUnit.Framework;
using Vostok.ClusterClient.Core.Model;
using Vostok.ClusterClient.Transport.Sockets.Tests.Functional.Helpers;

namespace Vostok.ClusterClient.Transport.Sockets.Tests.Functional
{
    internal class AllowAutoRedirectTests : TransportTestsBase
    {
        private void Handler(HttpListenerContext ctx)
        {
            switch (ctx.Request.Url.PathAndQuery)
            {
                case "/redirect":
                    ctx.Response.StatusCode = 302;
                    ctx.Response.RedirectLocation = "/target";
                    return;
                case "/target":
                    ctx.Response.StatusCode = 200;
                    return;
            }
        }
        
        [TestCase("GET")]
        [TestCase("POST")]
        public void Should_automatically_redirect_and_return_Ok_if_setting_is_true(string method)
        {
            SetAllowAutoRedirect(true);
            using (var server = TestServer.StartNew(Handler))
            {
                var response = Send(new Request(method, new Uri($"http://localhost:{server.Port}/redirect")));
                response.Code.Should().Be(ResponseCode.Ok);
                server.LastRequest.Url.Should().Be(new Uri($"http://localhost:{server.Port}/target"));
            }
        }
        
        [TestCase("GET")]
        [TestCase("POST")]
        public void Should_not_automatically_redirect_and_return_Found_if_setting_is_false(string method)
        {
            SetAllowAutoRedirect(false);
            using (var server = TestServer.StartNew(Handler))
            {
                var response = Send(new Request(method, new Uri($"http://localhost:{server.Port}/redirect")));
                response.Code.Should().Be(ResponseCode.Found);
                server.LastRequest.Url.Should().Be(new Uri($"http://localhost:{server.Port}/redirect"));
            }
        }

        private void SetAllowAutoRedirect(bool value)
        {
            SetSettings(s => s.AllowAutoRedirect = value);
        }
    }
}