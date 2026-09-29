namespace LiteGraph.Server.Services.Cluster
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Coordination;

    /// <summary>
    /// Handle for a lock held in Clutch.  Release is idempotent; a lease that could not be renewed marks the handle lost.
    /// Thread safety: members may be read from any thread.
    /// </summary>
    internal sealed class ClutchLockHandle : ILockHandle
    {
        public string Key { get; }

        public LockModeEnum Mode { get; }

        public long FencingToken { get; }

        public DateTime AcquiredUtc { get; }

        public bool IsHeld { get { return Volatile.Read(ref _Held) == 1; } }

        public CancellationToken LostToken { get { return _Lost.Token; } }

        internal string HolderId { get; }

        internal string QualifiedKey { get; }

        private readonly ClutchLockProvider _Provider;
        private readonly CancellationTokenSource _Lost = new CancellationTokenSource();
        private int _Held = 1;

        internal ClutchLockHandle(ClutchLockProvider provider, string key, string qualifiedKey, LockModeEnum mode, string holderId, long fencingToken)
        {
            _Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Key = key;
            QualifiedKey = qualifiedKey;
            Mode = mode;
            HolderId = holderId;
            FencingToken = fencingToken;
            AcquiredUtc = DateTime.UtcNow;
        }

        internal void MarkLost()
        {
            if (Interlocked.Exchange(ref _Held, 0) == 0) return;
            try { _Lost.Cancel(); } catch (ObjectDisposedException) { }
            _Provider.Forget(this);
        }

        public void Dispose()
        {
            DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _Held, 0) == 0) return;
            try { _Lost.Cancel(); } catch (ObjectDisposedException) { }
            await _Provider.ReleaseAsync(this).ConfigureAwait(false);
        }
    }
}
