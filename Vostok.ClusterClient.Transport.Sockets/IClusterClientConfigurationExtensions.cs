using System;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core;

namespace Vostok.Clusterclient.Transport.Sockets
{
    [PublicAPI]
    [Obsolete("This module is now obsolete. Please use an equivalent extension from Vostok.ClusterClient.Transport library.")]
    public static class IClusterClientConfigurationExtensions
    {
        /// <summary>
        /// Initialiazes configuration transport with a <see cref="SocketsTransport"/> with given settings.
        /// </summary>
        public static void SetupSocketTransport(this IClusterClientConfiguration self, SocketsTransportSettings settings)
        {
            self.Transport = new SocketsTransport(settings, self.Log);
        }

        /// <summary>
        /// Initialiazes configuration transport with a <see cref="SocketsTransport"/> with default settings.
        /// </summary>
        public static void SetupSocketTransport(this IClusterClientConfiguration self)
        {
            self.Transport = new SocketsTransport(self.Log);
        }
    }
}