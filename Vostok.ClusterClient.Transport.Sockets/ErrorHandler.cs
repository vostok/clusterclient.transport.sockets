﻿using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using JetBrains.Annotations;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport.SystemNetHttp.Exceptions;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Transport.Sockets
{
    internal class ErrorHandler
    {
        private readonly ILog log;

        public ErrorHandler(ILog log)
        {
            this.log = log;
        }

        [CanBeNull]
        public Response TryHandle(Request request, Exception error, CancellationToken token)
        {
            if (token.IsCancellationRequested)
                return Responses.Canceled;

            switch (error)
            {
                case StreamAlreadyUsedException _:
                    return null;

                case UserStreamException _:
                    LogUserStreamFailure(error);
                    return Responses.StreamInputFailure;

                case BodySendException _:
                    log.Error(error);
                    return Responses.SendFailure;

                case HttpRequestException httpError:
                    if (IsConnectionFailure(httpError))
                    {
                        LogConnectionFailure(request, httpError);
                        return Responses.ConnectFailure;
                    }

                    break;
            }

            LogUnknownException(error);
            return Responses.UnknownFailure;
        }

        private static bool IsConnectionFailure(HttpRequestException error)
            => error.InnerException is SocketException socketError && IsConnectionFailure(socketError.SocketErrorCode) ||
               error.InnerException is OperationCanceledException;

        private static bool IsConnectionFailure(SocketError code)
        {
            switch (code)
            {
                case SocketError.HostDown:
                case SocketError.HostNotFound:
                case SocketError.HostUnreachable:

                case SocketError.NetworkDown:
                case SocketError.NetworkUnreachable:

                case SocketError.AddressNotAvailable:
                case SocketError.AddressAlreadyInUse:

                case SocketError.ConnectionRefused:
                case SocketError.ConnectionAborted:
                case SocketError.ConnectionReset:

                case SocketError.TimedOut:
                case SocketError.TryAgain:
                case SocketError.SystemNotReady:
                case SocketError.TooManyOpenSockets:
                case SocketError.NoBufferSpaceAvailable:
                case SocketError.DestinationAddressRequired:
                    return true;

                default:
                    return false;
            }
        }

        private void LogConnectionFailure(Request request, Exception error)
            => log.Warn(error, "Connection failure. Target = {Target}.", request.Url.Authority);

        private void LogUserStreamFailure(Exception error)
            => log.Error(error, "Failed to read from user-provided request body stream");

        private void LogUnknownException(Exception error)
            => log.Error(error, "Unknown exception in sending request.");
    }
}