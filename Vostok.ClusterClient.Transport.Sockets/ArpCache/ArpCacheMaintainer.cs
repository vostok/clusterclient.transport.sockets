using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Vostok.Clusterclient.Transport.Sockets.ArpCache
{
    internal static unsafe class ArpCacheMaintainer
    {
        private static readonly ConcurrentDictionary<IPAddress, DateTime> ActiveAddresses;
        private static readonly TimeSpan ActiveAddressesTtl;
        private static readonly TimeSpan ActiveAddressesTtlHalf;
        private static readonly TimeSpan WarmupPeriod;
        private static volatile bool arpRequestsWork;

        static ArpCacheMaintainer()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            arpRequestsWork = true;
            ActiveAddresses = new ConcurrentDictionary<IPAddress, DateTime>();
            ActiveAddressesTtl = TimeSpan.FromDays(1);
            ActiveAddressesTtlHalf = ActiveAddressesTtl.Divide(2);
            WarmupPeriod = TimeSpan.FromSeconds(15);

            WarmupAndSchedule();
        }

        public static void ReportAddress(IPAddress address)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            if (ActiveAddresses.TryGetValue(address, out var timestamp))
            {
                // (iloktionov): Не будем каждый раз записывать timestamp в словарь, чтобы избежать лишнего lock contention.
                var currentTime = DateTime.UtcNow;
                if (currentTime - timestamp >= ActiveAddressesTtlHalf)
                {
                    ActiveAddresses[address] = currentTime;
                }
            }
            else
            {
                foreach (var network in NetworkHelper.LocalNetworks)
                {
                    if (network.Contains(address))
                    {
                        ActiveAddresses[address] = DateTime.UtcNow;
                        break;
                    }
                }
            }
        }

        private static void WarmupAndSchedule()
        {
            Warmup();

            Task.Delay(WarmupPeriod).ContinueWith(_ => WarmupAndSchedule());
        }

        private static void Warmup()
        {
            CleanupActiveAddresses();

            foreach (var address in NetworkHelper.GatewayAddresses.Concat(ActiveAddresses.Select(pair => pair.Key)))
            {
                SendARP(address);
            }
        }

        private static void CleanupActiveAddresses()
        {
            var currentTime = DateTime.UtcNow;

            foreach (var pair in ActiveAddresses)
            {
                if (currentTime - pair.Value >= ActiveAddressesTtl)
                {
                    ActiveAddresses.TryRemove(pair.Key, out _);
                }
            }
        }

        private static void SendARP(IPAddress address)
        {
            if (!arpRequestsWork)
                return;

            try
            {
                var ipAddressNumber = address.ToUInt32BigEndian();
                var macAddressBytes = stackalloc byte[6];
                var macAddressLength = 6;

                SendARP(ipAddressNumber, 0, macAddressBytes, ref macAddressLength);
            }
            catch
            {
                arpRequestsWork = false;
            }
        }

        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        private static extern int SendARP(uint dstIP, uint srcIP, byte* macAddr, ref int hwAddrLength);
    }
}