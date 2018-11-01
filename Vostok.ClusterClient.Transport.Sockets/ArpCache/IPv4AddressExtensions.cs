using System;
using System.Net;
using System.Net.Sockets;

namespace Vostok.Clusterclient.Transport.Sockets.ArpCache
{
    internal static class IPv4AddressExtensions
    {
        public static uint ToUInt32(this IPAddress address)
        {
            if (address.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException("Address must be an IPv4 address.", nameof(address));

            var rawAddress = (uint)address.Address;

            return
                ((rawAddress & 0x000000FFU) << 24) |
                ((rawAddress & 0x0000FF00U) << 8) |
                ((rawAddress & 0x00FF0000U) >> 8) |
                ((rawAddress & 0xFF000000U) >> 24);
        }

        public static uint ToUInt32BigEndian(this IPAddress address)
        {
            if (address.AddressFamily != AddressFamily.InterNetwork)
                throw new ArgumentException("Address must be an IPv4 address.", nameof(address));

            return (uint)address.Address;
        }
    }
}