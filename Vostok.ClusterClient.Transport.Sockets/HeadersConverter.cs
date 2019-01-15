using System;
using System.Net.Http;
using System.Net.Http.Headers;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport.Sockets.Hacks;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Transport.Sockets
{
    internal static class HeadersConverter
    {
        public static void Fill(Request request, HttpRequestMessage message, ILog log)
        {
            try
            {
                if (request.Headers != null)
                {
                    var canAssignDirectly = HttpHeadersUnlocker.TryUnlockRestrictedHeaders(message.Headers, log);
                    if (canAssignDirectly)
                    {
                        AssignHeadersDirectly(request.Headers, message.Headers);
                    }
                    else
                    {
                        AssignHeadersThroughProperties(request.Headers, message);
                    }
                }
            }
            catch (Exception e)
            {
                log.ForContext(typeof(HeadersConverter)).Error(e);
            }

            TrySetHostExplicitly(request.Headers, message.Headers);
        }

        public static Headers Create(HttpResponseMessage responseMessage)
        {
            var headers = Headers.Empty;

            foreach (var (key, value) in responseMessage.Headers)
                headers = headers.Set(key, string.Join(", ", value));

            if (responseMessage.Content != null)
            {
                foreach (var (key, value) in responseMessage.Content.Headers)
                    headers = headers.Set(key, string.Join(", ", value));
            }

            return headers;
        }

        private static void AssignHeadersDirectly(Headers source, HttpHeaders target)
        {
            foreach (var header in source)
            {
                if (NeedToSkipHeader(header.Name))
                    continue;

                target.Add(header.Name, header.Value);
            }
        }

        private static void AssignHeadersThroughProperties(Headers headers, HttpRequestMessage message)
        {
            foreach (var header in headers)
            {
                if (NeedToSkipHeader(header.Name))
                    continue;

                if (IsContentHeader(header.Name))
                {
                    message.Content.Headers.Add(header.Name, header.Value);
                    continue;
                }

                message.Headers.Add(header.Name, header.Value);
            }
        }

        private static void TrySetHostExplicitly(Headers source, HttpRequestHeaders target)
        {
            var host = source?[HeaderNames.Host];
            if (host != null)
                target.Host = host;
        }

        private static bool NeedToSkipHeader(string name) => name.Equals(HeaderNames.ContentLength) ||
                                                             name.Equals(HeaderNames.Connection) ||
                                                             name.Equals(HeaderNames.Host) ||
                                                             name.Equals(HeaderNames.TransferEncoding);

        private static bool IsContentHeader(string headerName)
        {
            switch (headerName)
            {
                case "Allow":
                case "Content-Disposition":
                case "Content-Encoding":
                case "Content-Language":
                case "Content-Length":
                case "Content-Location":
                case "Content-MD5":
                case "Content-Range":
                case "Content-Type":
                case "Expires":
                case "Last-Modified":
                    return true;
                default:
                    return false;
            }
        }
    }
}