using System;
using System.IO;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Transport.Sockets
{
    internal static class SocketAccessor
    {
        private static readonly Func<Stream, Socket> empty = _ => null;
        private static readonly object sync = new object();

        private static volatile Func<Stream, Socket> accessor;

        public static Socket GetSocket(Stream httpContentStream, ILog log)
        {
            if (httpContentStream == null)
                return null;
            EnsureInitialized(log);
            try
            {
                return accessor(httpContentStream);
            }
            catch (Exception e)
            {
                if (accessor != empty)
                    log.Warn(e, "Can't get Socket from HttpContentStream.");
                accessor = empty;
                return null;
            }
        }

        private static void EnsureInitialized(ILog log)
        {
            if (accessor == null)
            {
                lock (sync)
                {
                    if (accessor == null)
                        accessor = Build(log);
                }
            }
        }

        private static Func<Stream, Socket> Build(ILog log)
        {
            try
            {
                var parameterExpr = Expression.Parameter(typeof(Stream));
                var httpContentStreamType = typeof(HttpClient).Assembly.GetType("System.Net.Http.HttpContentStream");
                var httpContentStreamExpr = Expression.Convert(parameterExpr, httpContentStreamType);
                var connectionField = httpContentStreamType.GetField("_connection", BindingFlags.Instance | BindingFlags.NonPublic);
                var connectionFieldExpr = Expression.Field(httpContentStreamExpr, connectionField);
                var socketField = connectionField.FieldType.GetField("_socket", BindingFlags.Instance | BindingFlags.NonPublic);
                var socketFieldExpr = Expression.Field(connectionFieldExpr, socketField);
                var nullExpr = Expression.Constant(null, connectionField.FieldType);

                var condition = Expression.Condition(
                    Expression.Equal(connectionFieldExpr, nullExpr),
                    Expression.Constant(null, socketField.FieldType),
                    socketFieldExpr
                );
                return Expression.Lambda<Func<Stream, Socket>>(condition, parameterExpr).Compile();
            }
            catch (Exception e)
            {
                log.Warn(e, "Can't build Socket accessor.");
                return empty;
            }
        }
    }
}