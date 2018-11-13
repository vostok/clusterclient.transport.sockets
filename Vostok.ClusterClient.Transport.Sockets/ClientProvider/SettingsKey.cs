using System;
using System.Net;

namespace Vostok.Clusterclient.Transport.Sockets.ClientProvider
{
    internal class SettingsKey : IEquatable<SettingsKey>
    {
        public SettingsKey(
            TimeSpan connectionTimeout,
            TimeSpan connectionIdleTimeout,
            TimeSpan connectionLifetime,
            IWebProxy proxy,
            int maxConnectionsPerEndpoint,
            int? maxResponseDrainSize,
            bool allowAutoRedirect)
        {
            ConnectionTimeout = connectionTimeout;
            ConnectionIdleTimeout = connectionIdleTimeout;
            ConnectionLifetime = connectionLifetime;
            Proxy = proxy;
            MaxConnectionsPerEndpoint = maxConnectionsPerEndpoint;
            MaxResponseDrainSize = maxResponseDrainSize;
            AllowAutoRedirect = allowAutoRedirect;
        }

        public readonly TimeSpan ConnectionTimeout;
        public readonly TimeSpan ConnectionIdleTimeout;
        public readonly TimeSpan ConnectionLifetime;
        public readonly IWebProxy Proxy;
        public readonly int MaxConnectionsPerEndpoint;
        public readonly int? MaxResponseDrainSize;
        public readonly bool AllowAutoRedirect;

        #region Equality
        
        public bool Equals(SettingsKey other)
        {
            return ConnectionTimeout.Equals(other.ConnectionTimeout) &&
                   ConnectionIdleTimeout.Equals(other.ConnectionIdleTimeout) &&
                   ReferenceEquals(Proxy, other.Proxy) &&
                   MaxConnectionsPerEndpoint == other.MaxConnectionsPerEndpoint &&
                   MaxResponseDrainSize == other.MaxResponseDrainSize &&
                   AllowAutoRedirect == other.AllowAutoRedirect;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is SettingsKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = ConnectionTimeout.GetHashCode();
                hashCode = (hashCode * 397) ^ ConnectionIdleTimeout.GetHashCode();
                hashCode = (hashCode * 397) ^ (Proxy != null ? Proxy.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ MaxConnectionsPerEndpoint;
                hashCode = (hashCode * 397) ^ MaxResponseDrainSize.GetHashCode();
                hashCode = (hashCode * 397) ^ AllowAutoRedirect.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator==(SettingsKey left, SettingsKey right) => left.Equals(right);

        public static bool operator!=(SettingsKey left, SettingsKey right) => !left.Equals(right);
        
        #endregion
    }
}