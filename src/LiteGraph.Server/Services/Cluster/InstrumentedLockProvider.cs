namespace LiteGraph.Server.Services.Cluster
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Coordination;

    /// <summary>
    /// Lock provider decorator that records every acquisition in the observability service: its duration and outcome
    /// (acquired, denied, unavailable, cancelled), labeled by key class (the first key segment, never a GUID).
    /// Thread safety: safe for concurrent use; delegates to the wrapped provider.
    /// </summary>
    public class InstrumentedLockProvider : ILockProvider
    {
        #region Public-Members

        /// <inheritdoc />
        public string Name { get { return _Inner.Name; } }

        /// <inheritdoc />
        public bool IsDistributed { get { return _Inner.IsDistributed; } }

        /// <inheritdoc />
        public bool IsAvailable { get { return _Inner.IsAvailable; } }

        /// <summary>
        /// The wrapped provider.
        /// </summary>
        public ILockProvider Inner { get { return _Inner; } }

        /// <summary>
        /// Observability service that receives the measurements.  Null until wired at startup; while null nothing is recorded.
        /// </summary>
        public ObservabilityService Observability { get; set; } = null;

        #endregion

        #region Private-Members

        private readonly ILockProvider _Inner;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="inner">Provider to wrap.</param>
        /// <exception cref="ArgumentNullException">inner is null.</exception>
        public InstrumentedLockProvider(ILockProvider inner)
        {
            _Inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<ILockHandle> AcquireAsync(string key, LockModeEnum mode, LockAcquireOptions options = null, CancellationToken token = default)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            string outcome = "acquired";
            try
            {
                return await _Inner.AcquireAsync(key, mode, options, token).ConfigureAwait(false);
            }
            catch (LockNotAcquiredException)
            {
                outcome = "denied";
                throw;
            }
            catch (LockProviderUnavailableException)
            {
                outcome = "unavailable";
                throw;
            }
            catch (OperationCanceledException)
            {
                outcome = "cancelled";
                throw;
            }
            finally
            {
                Observability?.RecordLockAcquire(key, outcome, stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        /// <inheritdoc />
        public async Task<ILockHandle> TryAcquireAsync(string key, LockModeEnum mode, CancellationToken token = default)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            string outcome = "denied";
            try
            {
                ILockHandle handle = await _Inner.TryAcquireAsync(key, mode, token).ConfigureAwait(false);
                if (handle != null) outcome = "acquired";
                return handle;
            }
            catch (LockProviderUnavailableException)
            {
                outcome = "unavailable";
                throw;
            }
            catch (OperationCanceledException)
            {
                outcome = "cancelled";
                throw;
            }
            finally
            {
                Observability?.RecordLockAcquire(key, outcome, stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        /// <summary>
        /// Dispose the wrapped provider.
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
            if (disposing) _Inner.Dispose();
        }

        #endregion
    }
}
