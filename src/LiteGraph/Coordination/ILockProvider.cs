namespace LiteGraph.Coordination
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Keyed lock provider.  Implementations may be in-process (single node) or distributed (cluster).
    /// Thread safety: implementations must be safe for concurrent use.
    /// </summary>
    public interface ILockProvider : IDisposable
    {
        /// <summary>
        /// Provider name, for example Local or Clutch.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// True when the provider coordinates across processes.
        /// </summary>
        bool IsDistributed { get; }

        /// <summary>
        /// True when the provider is currently able to grant locks.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Acquire a lock, waiting according to the supplied options.
        /// </summary>
        /// <param name="key">Lock key.  See LockKeys.</param>
        /// <param name="mode">Lock mode.</param>
        /// <param name="options">Acquisition options; null uses defaults.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Held lock.  Dispose to release.</returns>
        /// <exception cref="ArgumentNullException">Key is null or empty.</exception>
        /// <exception cref="LockNotAcquiredException">The lock was not granted within the allowed time.</exception>
        /// <exception cref="LockProviderUnavailableException">The provider cannot currently grant locks.</exception>
        Task<ILockHandle> AcquireAsync(string key, LockModeEnum mode, LockAcquireOptions options = null, CancellationToken token = default);

        /// <summary>
        /// Try to acquire a lock without waiting.
        /// </summary>
        /// <param name="key">Lock key.  See LockKeys.</param>
        /// <param name="mode">Lock mode.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Held lock, or null if the lock is currently held elsewhere.</returns>
        /// <exception cref="ArgumentNullException">Key is null or empty.</exception>
        /// <exception cref="LockProviderUnavailableException">The provider cannot currently grant locks.</exception>
        Task<ILockHandle> TryAcquireAsync(string key, LockModeEnum mode, CancellationToken token = default);
    }
}
