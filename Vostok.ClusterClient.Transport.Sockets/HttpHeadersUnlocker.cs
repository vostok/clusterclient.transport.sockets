using System;
using System.Net.Http.Headers;
using System.Reflection;
using System.Reflection.Emit;
using Vostok.Logging.Abstractions;

namespace Vostok.ClusterClient.Transport.Sockets
{
    internal static class HttpHeadersUnlocker
    {
        private static readonly Action<HttpHeaders> unlocker;
        private static volatile bool canUnlock;

        static HttpHeadersUnlocker()
        {
            try
            {
                var allowLambda = CreateAssignment<HttpHeaders>("_allowedHeaderTypes", BindingFlags.Instance | BindingFlags.NonPublic, (int) (HttpHeaderType.Custom)                                          );
                var treatLambda = CreateAssignment<HttpHeaders>("_treatAsCustomHeaderTypes", BindingFlags.Instance | BindingFlags.NonPublic, (int) HttpHeaderType.All);

                unlocker = h =>
                {
                    allowLambda(h);
                    treatLambda(h);
                };
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
                unlocker = null;
            }

            canUnlock = unlocker != null;
        }

        private static Action<TType> CreateAssignment<TType>(string field, BindingFlags bindingFlags, int value)
        {
            var type = typeof(TType);
            
            var fieldInfo = type.GetField(field, bindingFlags);

            var dyn = new DynamicMethod($"Assign_{field}_{value}", null, new[]{typeof(TType)}, typeof(HttpHeadersUnlocker));

            var il = dyn.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldc_I4, value);
            il.Emit(OpCodes.Stfld, fieldInfo);
            il.Emit(OpCodes.Ret);

            return (Action<TType>) dyn.CreateDelegate(typeof(Action<TType>));
        }

        public static bool TryUnlockRestrictedHeaders(HttpHeaders headers, ILog log)
        {
            if (!canUnlock)
                return false;

            try
            {
                unlocker(headers);
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
                if (canUnlock)
                    log.Warn(error, "Failed to unlock HttpHeaders for unsafe assignment.");

                return canUnlock = false;
            }

            return true;
        }
    }
}