using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.ClusterClient.Core.Model;
using Vostok.ClusterClient.Transport.Sockets.Tests.Functional.Helpers;

namespace Vostok.ClusterClient.Transport.Sockets.Tests.Functional
{
    internal class MaxConnectionsPerEndpointTests : TransportTestsBase
    {
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(10)]
        public void Should_not_send_new_requests_to_server_on_per_server_connections_limit(int limit)
        {
            SetSettings(s => s.MaxConnectionsPerEndpoint = limit);
            using (var server = TestServer.StartNew(
                ctx =>
                {
                    Thread.Sleep(1.Seconds());
                    ctx.Response.StatusCode = 200;
                }))
            {
                var request = Request.Get(server.Url);
                var tasks = new List<Task>();
                for (var i = 0; i < limit; i++)
                {
                    tasks.Add(SendAsync(request, TimeSpan.FromSeconds(5)));
                }
                Task.Delay(100).GetAwaiter().GetResult();
                var lastTask = SendAsync(request);
                Task.WhenAll(tasks).GetAwaiter().GetResult();
                lastTask.IsCompleted.Should().BeFalse();
            }
        }
    }
}