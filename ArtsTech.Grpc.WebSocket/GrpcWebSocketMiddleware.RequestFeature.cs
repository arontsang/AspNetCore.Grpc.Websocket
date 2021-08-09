using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;

namespace ArtsTech.Grpc.WebSocket
{
    public partial class GrpcWebSocketMiddleware
    {
        private class RequestBodyFeature
            : IRequestBodyPipeFeature
                , IAsyncDisposable
                , IHttpRequestLifetimeFeature
        {
            public PipeReader Reader { get; }

            private readonly IAsyncDisposable _task;
            private readonly CancellationTokenSource _clientRequestCancellationSource = new();

            public RequestBodyFeature(System.Net.WebSockets.WebSocket ws)
            {
                var pipe = new Pipe();
                Reader = pipe.Reader;

                _task = new CancellableTask(cancel => ReadWebsocket(ws, pipe.Writer, cancel));
            }

            public ValueTask DisposeAsync()
            {
                return _task.DisposeAsync();
            }

            public void Abort()
            {
                _clientRequestCancellationSource.Cancel();
            }

            public CancellationToken RequestAborted { get; set; }

            private async Task ReadWebsocket(System.Net.WebSockets.WebSocket ws, PipeWriter writer,
                CancellationToken stoppingToken)
            {
                using (var buffer = MemoryPool<byte>.Shared.Rent())
                {
                    // TODO: Merge these headers with the `DefaultHttpContext`
                    var headers = await ws.ReceiveAsync(buffer.Memory, stoppingToken);
                }

                try
                {
                    using var buffer = MemoryPool<byte>.Shared.Rent();
                    while (!stoppingToken.IsCancellationRequested)
                    {
                        var prefixBuffer = buffer.Memory[..1];


                        var result = await ws.ReceiveAsync(prefixBuffer, stoppingToken);

                        if (result.MessageType == WebSocketMessageType.Close ||
                            (result.Count == 1 && prefixBuffer.Span[0] == 0x01))
                        {
                            break;
                        }

                        do
                        {
                            result = await ws.ReceiveAsync(buffer.Memory, stoppingToken);
                            var payloadPart = buffer.Memory[..result.Count];
                            await writer.WriteAsync(payloadPart, CancellationToken.None);
                        } while (!result.EndOfMessage);

                        await writer.FlushAsync(CancellationToken.None);
                    }
                }
                finally
                {
                    _clientRequestCancellationSource.Cancel();
                    await writer.CompleteAsync();
                }
            }
        }
    }
}