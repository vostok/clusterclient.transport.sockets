using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Emit;
using Vostok.Logging.Abstractions;

namespace Vostok.ClusterClient.Transport.Sockets
{
    internal static class HttpHeadersUnlocker
    {
        private static readonly object sync = new object();
        private static Action<HttpHeaders> unlocker;
        private static volatile State state;

        public static bool TryUnlockRestrictedHeaders(HttpHeaders headers, ILog log)
        {
            EnsureInitialized(log);
            
            if (state == State.Broken)
                return false;

            return TryUnlockRestrictedHeadersInternal(headers, log);
        }

        private static void EnsureInitialized(ILog log)
        {
            if (state == State.NotInitialized)
            {
                lock (sync)
                {
                    if (state == State.NotInitialized)
                        BuildUnlocker(log);
                }
            }
        }

        private static void BuildUnlocker(ILog log)
        {
            try
            {
                var allowLambda = CreateAssignment<HttpHeaders>("_allowedHeaderTypes", BindingFlags.Instance | BindingFlags.NonPublic, (int) HttpHeaderType.Custom);
                var treatLambda = CreateAssignment<HttpHeaders>("_treatAsCustomHeaderTypes", BindingFlags.Instance | BindingFlags.NonPublic, (int) HttpHeaderType.All);

                unlocker = h =>
                {
                    allowLambda(h);
                    treatLambda(h);
                };
            }
            catch (Exception e)
            {
                log.ForContext(typeof(HttpHeadersUnlocker)).Error(e, "Can't unlock HttpHeaders");
                unlocker = null;
            }

            state = unlocker != null && Test(log)
                ? State.Ok
                : State.Broken;
        }

        private static Action<TType> CreateAssignment<TType>(string field, BindingFlags bindingFlags, int value)
        {
            var type = typeof(TType);

            var fieldInfo = type.GetField(field, bindingFlags);

            var dyn = new DynamicMethod($"Assign_{field}_{value}", null, new[] {typeof(TType)}, typeof(HttpHeadersUnlocker));

            var il = dyn.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, value);
            il.Emit(OpCodes.Stfld, fieldInfo);
            il.Emit(OpCodes.Ret);

            return (Action<TType>) dyn.CreateDelegate(typeof(Action<TType>));
        }

        private static bool TryUnlockRestrictedHeadersInternal(HttpHeaders headers, ILog log)
        {
            try
            {
                unlocker(headers);
            }
            catch (Exception error)
            {
                if (state == State.Ok)
                    log.ForContext(typeof(HttpHeadersUnlocker)).Warn(error, "Failed to unlock HttpHeaders for unsafe assignment.");

                state = State.Broken;
                return false;
            }

            return true;
        }

        private static bool Test(ILog log)
        {
            (string name, string value)[] tests =
            {
                ("Accept-Encoding", "gzip;q=1.0, identity; q=0.5, *;q=0"),
                ("Content-Range", "bytes 200-1000/67589"),
                ("Content-Language", "mi, en"),
                ("Referer", "whatever")
            };

            try
            {
                using (var request = new HttpRequestMessage())
                {
                    var headers = request.Headers;
                    unlocker(request.Headers);

                    foreach (var test in tests)
                    {
                        headers.Add(test.name, test.value);
                        if (!headers.TryGetValues(test.name, out var value))
                        {
                            log
                                .ForContext(typeof(HttpHeadersUnlocker))
                                .Warn($"Can't unlock HttpHeaders. Test failed on header {test.name}. Can't set header value.");
                            return false;
                        }

                        if (!string.Equals(value.FirstOrDefault(), test.value, StringComparison.Ordinal))
                        {
                            log
                                .ForContext(typeof(HttpHeadersUnlocker))
                                .Warn($"Can't unlock HttpHeaders. Test failed on header {test.name}. Expected value: '{test.value}', actual: '{value}'");
                            return false;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                log.ForContext(typeof(HttpHeadersUnlocker)).Warn(e, "Can't unlock HttpHeaders");
                return false;
            }

            return true;
        }

        private enum State
        {
            NotInitialized,
            Broken,
            Ok
        }
    }
}