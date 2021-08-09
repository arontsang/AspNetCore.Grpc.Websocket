using System;
using System.Threading;
using System.Threading.Tasks;

namespace ArtsTech.Grpc.WebSocket
{
    internal class CancellableTask : IAsyncDisposable
    {
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly Task _task;

        public CancellableTask(Func<CancellationToken, Task> task)
        {
            _task = task(_cancellationTokenSource.Token);
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                _cancellationTokenSource.Cancel();
                await _task;
            }
            catch (TaskCanceledException)
            {
                // Swallow TCE
            }
            finally
            {
                _cancellationTokenSource.Dispose();
            }
        }
    }
}