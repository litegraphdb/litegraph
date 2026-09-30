namespace LiteGraph.Server.Services.Chat
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using WatsonWebserver.Core;

    /// <summary>
    /// Keeps a server-sent-event stream alive while the upstream model is silent, for example during tool calls or a slow
    /// first token.  When nothing has been written for the configured interval it writes an event carrying only a retry
    /// field ("retry: 3000"), which SSE clients accept without dispatching a message, so load balancers and proxies with
    /// idle timeouts do not cut the connection.  Every write to the stream must go through SendAsync so keepalive frames
    /// never interleave with events.
    /// Thread safety: safe for concurrent use; writes are serialized.
    /// </summary>
    internal sealed class SseKeepAlive : IAsyncDisposable
    {
        internal int KeepAlivesSent { get { return Volatile.Read(ref _KeepAlivesSent); } }

        private static readonly string _RetryMs = "3000";

        private readonly HttpContextBase _Ctx;
        private readonly TimeSpan _Interval;
        private readonly SemaphoreSlim _WriteLock = new SemaphoreSlim(1, 1);
        private readonly CancellationTokenSource _Cts = new CancellationTokenSource();
        private readonly Task _Loop;
        private long _LastWriteTicks = DateTime.UtcNow.Ticks;
        private int _KeepAlivesSent = 0;
        private int _Stopped = 0;

        internal SseKeepAlive(HttpContextBase ctx, int intervalSeconds)
        {
            _Ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
            if (intervalSeconds < 1) throw new ArgumentOutOfRangeException(nameof(intervalSeconds));
            _Interval = TimeSpan.FromSeconds(intervalSeconds);
            _Loop = Task.Run(() => LoopAsync(_Cts.Token));
        }

        internal async Task SendAsync(ServerSentEvent evt, bool final)
        {
            await _WriteLock.WaitAsync().ConfigureAwait(false);
            try
            {
                await _Ctx.Response.SendEvent(evt, final).ConfigureAwait(false);
                Interlocked.Exchange(ref _LastWriteTicks, DateTime.UtcNow.Ticks);
            }
            finally
            {
                _WriteLock.Release();
            }

            if (final) Stop();
        }

        public async ValueTask DisposeAsync()
        {
            Stop();
            try { await _Loop.ConfigureAwait(false); } catch (OperationCanceledException) { }
            _Cts.Dispose();
            _WriteLock.Dispose();
        }

        private void Stop()
        {
            if (Interlocked.Exchange(ref _Stopped, 1) == 1) return;
            try { _Cts.Cancel(); } catch (ObjectDisposedException) { }
        }

        private async Task LoopAsync(CancellationToken token)
        {
            TimeSpan check = TimeSpan.FromMilliseconds(Math.Min(1000, _Interval.TotalMilliseconds / 2));
            while (!token.IsCancellationRequested)
            {
                await Task.Delay(check, token).ConfigureAwait(false);
                if (DateTime.UtcNow.Ticks - Interlocked.Read(ref _LastWriteTicks) < _Interval.Ticks) continue;

                await _WriteLock.WaitAsync(token).ConfigureAwait(false);
                try
                {
                    if (token.IsCancellationRequested) return;
                    await _Ctx.Response.SendEvent(new ServerSentEvent { Retry = _RetryMs }, false).ConfigureAwait(false);
                    Interlocked.Exchange(ref _LastWriteTicks, DateTime.UtcNow.Ticks);
                    Interlocked.Increment(ref _KeepAlivesSent);
                }
                catch (Exception e) when (!(e is OperationCanceledException))
                {
                    // The client went away or the response completed; stop sending.
                    return;
                }
                finally
                {
                    _WriteLock.Release();
                }
            }
        }
    }
}
