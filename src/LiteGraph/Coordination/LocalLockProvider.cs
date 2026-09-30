namespace LiteGraph.Coordination
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Padlocks;

    /// <summary>
    /// In-process lock provider backed by Padlock.  Used for single-node deployments and embedded library use.
    /// Read locks are exclusive in this provider, which is correct if conservative.
    /// Thread safety: safe for concurrent use.
    /// </summary>
    public class LocalLockProvider : ILockProvider
    {
        #region Public-Members

        /// <inheritdoc />
        public string Name { get { return "Local"; } }

        /// <inheritdoc />
        public bool IsDistributed { get { return false; } }

        /// <inheritdoc />
        public bool IsAvailable { get { return !_Disposed; } }

        #endregion

        #region Private-Members

        private readonly Padlock<string> _Padlock = new Padlock<string>();
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public LocalLockProvider()
        {
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<ILockHandle> AcquireAsync(string key, LockModeEnum mode, LockAcquireOptions options = null, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            ThrowIfDisposed();
            if (options == null) options = new LockAcquireOptions();

            if (!options.Wait || options.TimeoutMs == 0)
            {
                ILockHandle immediate = await TryAcquireAsync(key, mode, token).ConfigureAwait(false);
                if (immediate == null) throw new LockNotAcquiredException(key, mode, "Denied");
                return immediate;
            }

            using (CancellationTokenSource timeout = new CancellationTokenSource(options.TimeoutMs))
            using (CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(timeout.Token, token))
            {
                try
                {
                    IDisposable inner = await _Padlock.LockAsync(key, linked.Token).ConfigureAwait(false);
                    return new LocalLockHandle(key, mode, inner);
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    throw new LockNotAcquiredException(key, mode, "Timeout");
                }
            }
        }

        /// <inheritdoc />
        public async Task<ILockHandle> TryAcquireAsync(string key, LockModeEnum mode, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            ThrowIfDisposed();
            token.ThrowIfCancellationRequested();

            if (_Padlock.IsLocked(key)) return null;

            using (CancellationTokenSource immediate = new CancellationTokenSource(TimeSpan.FromMilliseconds(50)))
            using (CancellationTokenSource linked = CancellationTokenSource.CreateLinkedTokenSource(immediate.Token, token))
            {
                try
                {
                    IDisposable inner = await _Padlock.LockAsync(key, linked.Token).ConfigureAwait(false);
                    return new LocalLockHandle(key, mode, inner);
                }
                catch (OperationCanceledException) when (!token.IsCancellationRequested)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Dispose.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed) return;
            _Disposed = true;
        }

        private void ThrowIfDisposed()
        {
            if (_Disposed) throw new ObjectDisposedException(nameof(LocalLockProvider));
        }

        #endregion
    }
}
