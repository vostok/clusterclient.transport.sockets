## 0.1.6 (07.12.2019):

* Improved error logging (this fixes https://github.com/vostok/clusterclient.transport.sockets/issues/3)
* Improved detection of connection errors with following exception hierarchy: `HttpRequestException` --> `IOException` --> `SocketException`

## 0.1.5 (15-08-2019):

Fixed reception of Content-Length header without body in response to a HEAD request.

## 0.1.4 (14-08-2019):

Fixed a bug where a network error while reading content could cause the transport to return a response with headers or partial body.

## 0.1.3 (20-03-2019): 

SocketsTransportSettings: BufferFactory option is now public.

## 0.1.2 (03-03-2019): 

SocketsTransport now supports composite request bodies.

## 0.1.0 (04-02-2019): 

Initial prerelease.