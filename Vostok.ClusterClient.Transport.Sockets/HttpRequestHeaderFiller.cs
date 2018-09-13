using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Vostok.ClusterClient.Core.Model;
using Vostok.Logging.Abstractions;

namespace Vostok.ClusterClient.Transport.Sockets
{
    internal static class HttpHeaderFiller
    {
        public static void Fill(Request request, HttpRequestMessage message, TimeSpan timeout, ILog log)
        {if (request.Headers != null)
            {
                var canAssignDirectly = HttpHeadersUnlocker.TryUnlockRestrictedHeaders(message.Headers, log);
                if (canAssignDirectly)
                {
                    AssignHeadersDirectly(request.Headers, message.Headers);
                }
                else
                {
                    //AssignHeadersThroughProperties(request.Headers, webRequest);
                }
            }

            SetRequestTimeoutHeader(message.Headers, timeout);

            TrySetHostExplicitly(request.Headers, message.Headers);
            //TrySetClientIdentityHeader(request, webRequest);
        }
        
        private static void AssignHeadersDirectly( Headers source,HttpHeaders target)
        {
            foreach (var header in source)
            {
                if (NeedToSkipHeader(header.Name))
                    continue;

                target.Add(header.Name, header.Value);
            }
        }

        private static void SetRequestTimeoutHeader(HttpHeaders headers, TimeSpan timeout)
        {
            headers.Add(HeaderNames.RequestTimeout, timeout.Ticks.ToString());
        }

        private static void TrySetHostExplicitly(Headers source, HttpRequestHeaders target)
        {
            var host = source?[HeaderNames.Host];
            if (host != null)
                target.Host = host;
        }
        
        // private static void TrySetClientIdentityHeader(Request request, HttpHeaders headers)
        // {
        //     if (request.Headers?[HeaderNames.ClientApplication] == null)
        //     {
        //         webRequest.Headers.Set(HeaderNames.ClientApplication, UrlEncodingHelper.UrlEncode(HttpClientIdentity.Get()));
        //     }
        // }
        
        private static bool NeedToSkipHeader(string name)
        {
            return
                name.Equals(HeaderNames.ContentLength) ||
                name.Equals(HeaderNames.Connection) ||
                name.Equals(HeaderNames.Host) ||
                name.Equals(HeaderNames.TransferEncoding);
        }
    }
}