using System;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Vostok.Clusterclient.Core.Model;
using Vostok.Clusterclient.Transport.Sockets.Client;
using Vostok.Clusterclient.Transport.Sockets.Hacks;
using Vostok.Clusterclient.Transport.Sockets.Messages;
using Vostok.Clusterclient.Transport.Sockets.ResponseReading;
using Vostok.Logging.Abstractions;

namespace Vostok.Clusterclient.Transport.Sockets.Sender
{
    internal class SocketsTransportRequestSender : ISocketsTransportRequestSender
    {
        private readonly IHttpRequestMessageFactory requestFactory;
        private readonly IResponseReader responseReader;
        private readonly ISocketTuner socketTuner;
        private readonly ILog log;

        public SocketsTransportRequestSender(
            IHttpRequestMessageFactory requestFactory,
            IResponseReader responseReader,
            ISocketTuner socketTuner,
            ILog log)
        {
            this.requestFactory = requestFactory;
            this.responseReader = responseReader;
            this.socketTuner = socketTuner;
            this.log = log;
        }

        public async Task<Response> SendAsync(IHttpClient client, Request request, CancellationToken cancellationToken)
        {
            // TODO: await Task.Yield(); (configure-await-false check fails there on CI
            await Task.Delay(0).ConfigureAwait(false);

            try
            {
                using (var state = new RequestDisposableState())
                    return await SendInternalAsync(client, state, request, cancellationToken).ConfigureAwait(false);
            }
            catch (StreamAlreadyUsedException)
            {
                throw;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return Responses.Canceled;
            }
            catch (Exception e)
            {
                LogUnknownException(e);
                return Responses.UnknownFailure;
            }
        }

        private static bool IsConnectionFailure(HttpRequestException e, CancellationToken cancellationToken)
            => e.InnerException is SocketException se && IsConnectionFailure(se.SocketErrorCode) ||
               e.InnerException is TaskCanceledException && !cancellationToken.IsCancellationRequested;

        private static bool IsConnectionFailure(SocketError socketError)
        {
            switch (socketError)
            {
                case SocketError.HostNotFound:
                case SocketError.AddressNotAvailable:
                // seen on linux:
                case SocketError.ConnectionRefused:
                case SocketError.TryAgain:
                case SocketError.NetworkUnreachable:
                // other:
                case SocketError.NetworkDown:
                case SocketError.HostDown:
                case SocketError.HostUnreachable:
                    return true;
                default:
                    return false;
            }
        }

        private async Task<Response> SendInternalAsync(IHttpClient client, RequestDisposableState state, Request request, CancellationToken cancellationToken)
        {
            state.RequestMessage = requestFactory.Create(request, cancellationToken, out var sendContext);

            try
            {
                state.ResponseMessage = await client
                    .SendAsync(state.RequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (HttpRequestException e) when (IsConnectionFailure(e, cancellationToken))
            {
                log.Warn(e, "Connection failure. Target = {Target}.", request.Url.Authority);
                return Responses.ConnectFailure;
            }

            if (sendContext.Response != null)
                return sendContext.Response;

            socketTuner.Tune(sendContext.Socket);

            if (sendContext.Response != null)
                return sendContext.Response;

            var responseCode = (ResponseCode) (int) state.ResponseMessage.StatusCode;

            var headers = HeadersConverter.Create(state.ResponseMessage);

            var responseReadResult = await responseReader
                .ReadResponseBodyAsync(state.ResponseMessage, cancellationToken)
                .ConfigureAwait(false);

            if (responseReadResult.Content != null)
                return new Response(responseCode, responseReadResult.Content, headers);
            if (responseReadResult.ErrorCode != null)
                return new Response(responseReadResult.ErrorCode.Value, null, headers);
            if (responseReadResult.Stream == null)
                return new Response(responseCode, null, headers);

            state.PreventNextDispose();
            return new Response(responseCode, null, headers, new ResponseStream(responseReadResult.Stream, state));
        }
        private void LogUnknownException(Exception error)
        {
            log.Warn(error, "Unknown error in sending request.");
        }
    }
}