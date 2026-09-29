namespace LiteGraph.Coordination
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Handle for a lock held by LocalLockProvider.
    /// Thread safety: members may be read from any thread; release is idempotent.
    /// </summary>
    internal sealed class LocalLockHandle : ILockHandle
    {
        public string Key { get; }

        public LockModeEnum Mode { get; }

        public long FencingToken { get { return 0; } }

        public DateTime AcquiredUtc { get; }

        public bool IsHeld { get { return Volatile.Read(ref _Held) == 1; } }

        public CancellationToken LostToken { get { return _Lost.Token; } }

        private readonly IDisposable _Inner;
        private readonly CancellationTokenSource _Lost = new CancellationTokenSource();
        private int _Held = 1;

        internal LocalLockHandle(string key, LockModeEnum mode, IDisposable inner)
        {
            Key = key;
            Mode = mode;
            AcquiredUtc = DateTime.UtcNow;
            _Inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _Held, 0) == 0) return;
            try { _Lost.Cancel(); } catch (ObjectDisposedException) { }
            _Inner.Dispose();
        }

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }
    }
}
