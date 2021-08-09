using System;
using System.IO;
using System.IO.Pipelines;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;

namespace ArtsTech.Grpc.WebSocket
{
    public partial class GrpcWebSocketMiddleware
    {
                

        private class ResponseFeature
            : HttpResponseFeature
                , IHttpResponseBodyFeature
                , IHttpResponseTrailersFeature
                , IAsyncDisposable
        {
            private static readonly ReadOnlyMemory<byte> TrailerPrefix = new ReadOnlyMemory<byte>(new byte[] { 0x80 });

            private readonly Task _task;
            private bool _hasStarted;

            public ResponseFeature(System.Net.WebSockets.WebSocket ws)
            {
                var pipe = new Pipe();
                Writer = pipe.Writer;

                _task = WriteWebsocket(ws, pipe.Reader);
            }


            public override bool HasStarted => _hasStarted;

            public async ValueTask DisposeAsync()
            {
                await Writer.CompleteAsync();
                await _task;
            }

            public void DisableBuffering()
            {
            }

            public Task SendFileAsync(string path, long offset, long? count,
                CancellationToken cancellationToken = new())
            {
                throw new NotSupportedException();
            }

            public Task StartAsync(CancellationToken cancellationToken = new())
            {
                _hasStarted = true;
                return Task.CompletedTask;
            }

            public async Task CompleteAsync()
            {
                await Writer.CompleteAsync();
                await _task;
            }

            public Stream Stream
            {
                get => throw new NotSupportedException();
            }

            public PipeWriter Writer { get; }

            private async Task WriteWebsocket(System.Net.WebSockets.WebSocket ws, PipeReader writer)
            {
                try
                {
                    while (true)
                    {
                        var result = await writer.ReadAsync(CancellationToken.None);
                        var buffer = result.Buffer;


                        while (!buffer.IsEmpty)
                        {
                            await ws.SendAsync(buffer.First, WebSocketMessageType.Binary, false,
                                CancellationToken.None);
                            buffer = buffer.Slice(buffer.First.Length);
                        }

                        await ws.SendAsync(Memory<byte>.Empty, WebSocketMessageType.Binary, true,
                            CancellationToken.None);
                        writer.AdvanceTo(buffer.Start, buffer.End);

                        if (result.IsCompleted)
                            break;
                    }
                }
                finally
                {
                    // Send all trailers.
                    var trailers = GetTrailerString(Trailers);
                    var length = BitConverter.GetBytes(trailers.Length);
                    if (BitConverter.IsLittleEndian)
                        Array.Reverse(length);
                    await ws.SendAsync(TrailerPrefix, WebSocketMessageType.Binary, false, CancellationToken.None);
                    await ws.SendAsync(length, WebSocketMessageType.Binary, false, CancellationToken.None);
                    await ws.SendAsync(Encoding.ASCII.GetBytes(trailers), WebSocketMessageType.Binary, true,
                        CancellationToken.None);
                }
            }

            private string GetTrailerString(IHeaderDictionary headers)
            {
                var ret = new StringBuilder();
                foreach (var (key, values) in headers)
                {
                    ret.AppendLine($"{key}: {values}");
                }

                return ret.ToString();
            }

            public IHeaderDictionary Trailers { get; set; } = new HeaderDictionary();
        }
    }
}