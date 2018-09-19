using System;
using System.Reflection;

namespace Vostok.ClusterClient.Transport.Sockets.Utilities
{
    internal static class ClientIdentityCore
    {
        private static readonly Lazy<string> Identity = new Lazy<string>(ObtainIdentity);

        public static string Get() => Identity.Value;

        private static string ObtainIdentity()
        {
            try
            {
                return Assembly.GetEntryAssembly().GetName().Name;
            }
            catch (Exception)
            {
                return "unknown";
            }
        }
    }
}