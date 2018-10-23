using System;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Clusterclient.Transport.Sockets.ClientProvider;

namespace Vostok.Clusterclient.Transport.Sockets.Tests
{
    internal class HttpClientProvider_Tests
    {   
        [TestCase(1)]
        [TestCase(2.5)]
        public void Should_return_client_with_given_connection_timeout(double seconds)
        {
            var timeout = TimeSpan.FromSeconds(seconds);
            var provider = new HttpClientProvider(new SocketsTransportSettings());
            var client = provider.GetClient(timeout);
            var handler = (SocketsHttpHandler) GetHandler(client);
            handler.ConnectTimeout.Should().Be(timeout);
        }
        
        [Test]
        public void Should_return_client_with_infinite_connection_timeout_if_given_timeout_is_null()
        {
            var provider = new HttpClientProvider(new SocketsTransportSettings());
            var client = provider.GetClient(null);
            var handler = (SocketsHttpHandler) GetHandler(client);
            handler.ConnectTimeout.Should().Be(Timeout.InfiniteTimeSpan);
        }
        
        private static HttpMessageHandler GetHandler(HttpClient client)
        {
            var field = typeof(HttpClient)
                .BaseType
                .GetField(
                    "_handler",
                    BindingFlags.Instance |
                    BindingFlags.NonPublic);
            
            return (HttpMessageHandler) field.GetValue(client);
        }
    }
}