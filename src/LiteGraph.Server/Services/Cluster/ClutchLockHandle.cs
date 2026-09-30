namespace LiteGraph.Server.Services.Cluster
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Clutch.Sdk;
    using LiteGraph.Coordination;

    /// <summary>
    /// Handle for a lock held in Clutch over one lock connection.  Release is idempotent.  The handle is marked lost
    /// when its connection closes or its lease goes unrenewed for a full lease period.
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

        internal ClutchLockClient Client { get; }

        internal DateTime LastRenewedUtc { get { return new DateTime(Interlocked.Read(ref _LastRenewedTicks), DateTimeKind.Utc); } }

        private readonly ClutchLockProvider _Provider;
        private readonly CancellationTokenSource _Lost = new CancellationTokenSource();
        private long _LastRenewedTicks;
        private int _Held = 1;

        internal ClutchLockHandle(ClutchLockProvider provider, ClutchLockClient client, string key, LockModeEnum mode, AcquiredLock acquired)
        {
            _Provider = provider ?? throw new ArgumentNullException(nameof(provider));
            Client = client ?? throw new ArgumentNullException(nameof(client));
            if (acquired == null) throw new ArgumentNullException(nameof(acquired));
            Key = key;
            Mode = mode;
            HolderId = acquired.HolderId;
            FencingToken = acquired.FencingToken;
            AcquiredUtc = DateTime.UtcNow;
            _LastRenewedTicks = AcquiredUtc.Ticks;
        }

        internal void MarkRenewed()
        {
            Interlocked.Exchange(ref _LastRenewedTicks, DateTime.UtcNow.Ticks);
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
