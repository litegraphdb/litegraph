namespace LiteGraph
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories;

    /// <summary>
    /// Explicit lifecycle controller for one graph transaction.
    /// </summary>
    public sealed class GraphTransactionContext : ITransactionContext
    {
        #region Public-Members

        /// <inheritdoc />
        public TransactionExecutionOptions Options { get; }

        /// <inheritdoc />
        public GraphRepositoryBase Repository
        {
            get
            {
                return _Session.Repository;
            }
        }

        /// <inheritdoc />
        public TransactionStateEnum State { get; private set; } = TransactionStateEnum.Created;

        /// <inheritdoc />
        public string Provider
        {
            get
            {
                return _Session.Provider;
            }
        }

        /// <inheritdoc />
        public bool IsolatedRepository
        {
            get
            {
                return _Session.IsIsolated;
            }
        }

        /// <inheritdoc />
        public bool SerializedByGate { get; private set; } = false;

        /// <inheritdoc />
        public double QueueWaitDurationMs { get; private set; } = 0;

        /// <inheritdoc />
        public double CommitDurationMs { get; private set; } = 0;

        /// <inheritdoc />
        public double RollbackDurationMs { get; private set; } = 0;

        #endregion

        #region Private-Members

        private readonly IRepositorySession _Session;
        private readonly SemaphoreSlim _SerializedGate;
        private bool _GateAcquired = false;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="options">Transaction execution options.</param>
        /// <param name="session">Repository session.</param>
        /// <param name="serializedGate">Optional fallback serialization gate.</param>
        public GraphTransactionContext(TransactionExecutionOptions options, IRepositorySession session, SemaphoreSlim serializedGate = null)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            Options.Validate();
            _Session = session ?? throw new ArgumentNullException(nameof(session));
            _SerializedGate = serializedGate;
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task BeginAsync(CancellationToken token = default)
        {
            ThrowIfDisposed();
            if (State != TransactionStateEnum.Created) throw new InvalidOperationException("Transaction context has already been started.");

            if (_Session.RequiresSerializedExecution)
            {
                if (_SerializedGate == null) throw new InvalidOperationException("Repository session requires serialized execution but no transaction gate was supplied.");

                Stopwatch queueStopwatch = Stopwatch.StartNew();
                await _SerializedGate.WaitAsync(token).ConfigureAwait(false);
                queueStopwatch.Stop();
                _GateAcquired = true;
                SerializedByGate = true;
                QueueWaitDurationMs = queueStopwatch.Elapsed.TotalMilliseconds;
            }

            try
            {
                await _Session.BeginTransactionAsync(Options, token).ConfigureAwait(false);
                State = TransactionStateEnum.Active;
            }
            catch
            {
                ReleaseGate();
                throw;
            }
        }

        /// <inheritdoc />
        public async Task CommitAsync(CancellationToken token = default)
        {
            ThrowIfDisposed();
            if (State != TransactionStateEnum.Active) throw new InvalidOperationException("Only an active transaction can be committed.");

            Stopwatch stopwatch = Stopwatch.StartNew();
            State = TransactionStateEnum.Committing;
            try
            {
                await _Session.CommitTransactionAsync(token).ConfigureAwait(false);
                State = TransactionStateEnum.Committed;
            }
            catch
            {
                State = TransactionStateEnum.Faulted;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                CommitDurationMs = stopwatch.Elapsed.TotalMilliseconds;
            }
        }

        /// <inheritdoc />
        public async Task RollbackAsync(CancellationToken token = default)
        {
            ThrowIfDisposed();
            if (State != TransactionStateEnum.Active && State != TransactionStateEnum.Committing && State != TransactionStateEnum.Faulted)
                throw new InvalidOperationException("Only an active or faulted transaction can be rolled back.");

            Stopwatch stopwatch = Stopwatch.StartNew();
            State = TransactionStateEnum.RollingBack;
            try
            {
                if (_Session.Active) await _Session.RollbackTransactionAsync(token).ConfigureAwait(false);
                State = TransactionStateEnum.RolledBack;
            }
            catch
            {
                State = TransactionStateEnum.Faulted;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                RollbackDurationMs = stopwatch.Elapsed.TotalMilliseconds;
            }
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;
            _Disposed = true;

            if (_Session.Active)
            {
                try { _Session.RollbackTransactionAsync(CancellationToken.None).GetAwaiter().GetResult(); } catch { }
            }

            ReleaseGate();
            _Session.Dispose();
        }

        /// <summary>
        /// Dispose asynchronously.
        /// </summary>
        /// <returns>Value task.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_Disposed) return;
            _Disposed = true;

            if (_Session.Active)
            {
                try { await _Session.RollbackTransactionAsync(CancellationToken.None).ConfigureAwait(false); } catch { }
            }

            ReleaseGate();
            await _Session.DisposeAsync().ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private void ThrowIfDisposed()
        {
            if (_Disposed) throw new ObjectDisposedException(nameof(GraphTransactionContext));
        }

        private void ReleaseGate()
        {
            if (!_GateAcquired) return;
            _GateAcquired = false;
            _SerializedGate.Release();
        }

        #endregion
    }
}
