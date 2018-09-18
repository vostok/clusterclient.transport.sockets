using System;
using System.Threading;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.ClusterClient.Core.Model;
using Vostok.ClusterClient.Transport.Sockets.Tests.Functional.Helpers;

namespace Vostok.ClusterClient.Transport.Sockets.Tests.Functional
{
    internal class ConnectionTimeoutTests : TransportTestsBase
    {
        private readonly Uri dummyServerUrl = new Uri("http://9.0.0.1:10/");

        [Test]
        public void Should_timeout_on_connection_to_a_blackhole_by_connect_timeout()
        {
            SetSettings(
                s =>
                {
                    s.ConnectionAttempts = 3;
                    s.ConnectionTimeout = 250.Milliseconds();
                });

            var task = SendAsync(Request.Get(dummyServerUrl));

            task.Wait(5.Seconds()).Should().BeTrue();

            task.Result.Code.Should().Be(ResponseCode.ConnectFailure);
        }

        [Test]
        public void Should_timeout_on_connection_to_a_blackhole_by_full_timeout()
        {
            SetSettings(
                s =>
                {
                    s.ConnectionAttempts = 3;
                    s.ConnectionTimeout = 1.Seconds();
                });

            var task = SendAsync(Request.Get(dummyServerUrl), 500.Milliseconds());

            task.Wait(5.Seconds()).Should().BeTrue();

            task.Result.Code.Should().Be(ResponseCode.RequestTimeout);
        }

        [Test]
        public void Should_not_timeout_on_connection_when_server_is_just_slow()
        {
            using (var server = TestServer.StartNew(
                ctx =>
                {
                    Thread.Sleep(2.Seconds());
                    ctx.Response.StatusCode = 200;
                }))
            {
                SetSettings(
                    s =>
                    {
                        s.ConnectionAttempts = 3;
                        s.ConnectionTimeout = 250.Milliseconds();
                    });


                Send(Request.Get(server.Url)).Code.Should().Be(ResponseCode.Ok);
            }
        }
        
        [Test]
        public void Should_not_timeout_if_any_attempts_are_available()
        {
            SetSettings(
                s =>
                {
                    s.ConnectionAttempts = 3;
                    s.ConnectionTimeout = 1.Seconds();
                });

            var task = SendAsync(Request.Get(dummyServerUrl));

            task.Wait(2.Seconds());

            task.IsCompleted.Should().BeFalse();
        }
        
        [Test]
        public void Should_timeout_by_request_timeout_first()
        {
            SetSettings(
                s =>
                {
                    s.ConnectionAttempts = 1;
                    s.ConnectionTimeout = 10.Seconds();
                });

            var task = SendAsync(Request.Get(dummyServerUrl), 500.Milliseconds());

            task.Wait(1.Seconds());

            task.IsCompleted.Should().BeTrue();

            task.Result.Code.Should().Be(ResponseCode.RequestTimeout);
        }
    }
}