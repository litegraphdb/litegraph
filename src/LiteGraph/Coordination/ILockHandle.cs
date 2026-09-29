namespace LiteGraph.Coordination
{
    using System;
    using System.Threading;

    /// <summary>
    /// A held lock.  Dispose to release.
    /// Thread safety: members may be read from any thread; dispose exactly once.
    /// </summary>
    public interface ILockHandle : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Lock key.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// Lock mode.
        /// </summary>
        LockModeEnum Mode { get; }

        /// <summary>
        /// Monotonic fencing token issued with the grant.  Zero for in-process locks.
        /// </summary>
        long FencingToken { get; }

        /// <summary>
        /// UTC timestamp at which the lock was acquired.
        /// </summary>
        DateTime AcquiredUtc { get; }

        /// <summary>
        /// True while the lock is held.  Becomes false after release or if a distributed lease is lost.
        /// </summary>
        bool IsHeld { get; }

        /// <summary>
        /// Cancellation token cancelled when the lock is lost or released.
        /// Link it into work performed under the lock so lost leases stop the work.
        /// </summary>
        CancellationToken LostToken { get; }
    }
}
