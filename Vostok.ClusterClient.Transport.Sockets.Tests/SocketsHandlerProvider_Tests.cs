using System.Net;
using System.Threading;
using FluentAssertions;
using FluentAssertions.Extensions;
using NSubstitute;
using NUnit.Framework;

namespace Vostok.Clusterclient.Transport.Sockets.Tests
{
    [TestFixture]
    internal class SocketsHandlerProvider_Tests
    {
        private SocketsTransportSettings settings;
        private SocketsHandlerProvider provider;

        [SetUp]
        public void TestSetup()
        {
            settings = new SocketsTransportSettings();
            provider = new SocketsHandlerProvider(settings);
        }

        [Test]
        public void Should_return_a_handler_with_proxy_from_settings()
        {
            var proxy = Substitute.For<IWebProxy>();

            settings.Proxy = proxy;

            provider.Obtain(null).Proxy.Should().BeSameAs(proxy);
        }

        [Test]
        public void Should_return_a_handler_with_given_connection_timeout()
        {
            provider.Obtain(1.Seconds()).ConnectTimeout.Should().Be(1.Seconds());
            provider.Obtain(2.Seconds()).ConnectTimeout.Should().Be(2.Seconds());
            provider.Obtain(3.Seconds()).ConnectTimeout.Should().Be(3.Seconds());

            provider.Obtain(null).ConnectTimeout.Should().Be(Timeout.InfiniteTimeSpan);
        }

        [Test]
        public void Should_return_a_handler_with_given_connection_idle_timeout()
        {
            settings.ConnectionIdleTimeout = 5.Seconds();

            provider.Obtain(1.Seconds()).PooledConnectionIdleTimeout.Should().Be(5.Seconds());
        }

        [Test]
        public void Should_return_a_handler_with_given_connection_lifetime()
        {
            settings.ConnectionLifetime = 10.Seconds();

            provider.Obtain(1.Seconds()).PooledConnectionLifetime.Should().Be(10.Seconds());
        }

        [Test]
        public void Should_return_a_handler_with_given_max_connections_per_endpoint()
        {
            settings.MaxConnectionsPerEndpoint = 123;

            provider.Obtain(1.Seconds()).MaxConnectionsPerServer.Should().Be(123);
        }

        [Test]
        public void Should_return_a_handler_with_given_auto_redirect_flag()
        {
            settings.AllowAutoRedirect = false;

            provider.Obtain(1.Seconds()).AllowAutoRedirect.Should().BeFalse();
        }

        [Test]
        public void Should_return_a_handler_with_given_custom_tuning_applied()
        {
            settings.CustomTuning = handler => handler.MaxResponseHeadersLength = 65242;

            provider.Obtain(1.Seconds()).MaxResponseHeadersLength.Should().Be(65242);
        }

        [Test]
        public void Should_cache_handler_in_one_instance()
        {
            var handler1 = provider.Obtain(null);
            var handler2 = provider.Obtain(null);

            handler2.Should().BeSameAs(handler1);
        }

        [Test]
        public void Should_cache_handler_between_multiple_instances()
        {
            var handler1 = provider.Obtain(null);
            var handler2 = new SocketsHandlerProvider(settings).Obtain(null);

            handler2.Should().BeSameAs(handler1);
        }

        [Test]
        public void Should_return_different_handlers_for_different_connection_timeouts()
        {
            var handler1 = provider.Obtain(null);
            var handler2 = provider.Obtain(1.Seconds());

            handler2.Should().NotBeSameAs(handler1);
        }

        [Test]
        public void Should_return_different_handlers_for_different_settings()
        {
            var settings2 = new SocketsTransportSettings { ConnectionLifetime = 5.Hours() };
            var provider2 = new SocketsHandlerProvider(settings2);

            var handler1 = provider.Obtain(null);
            var handler2 = provider2.Obtain(null);

            handler2.Should().NotBeSameAs(handler1);
        }
    }
}