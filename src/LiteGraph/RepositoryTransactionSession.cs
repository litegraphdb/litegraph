namespace LiteGraph
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories;

    /// <summary>
    /// Default repository-session adapter for graph transaction execution.
    /// </summary>
    public sealed class RepositoryTransactionSession : IRepositorySession
    {
        #region Public-Members

        /// <inheritdoc />
        public GraphRepositoryBase Repository { get; }

        /// <inheritdoc />
        public string Provider { get; }

        /// <inheritdoc />
        public bool IsIsolated { get; }

        /// <inheritdoc />
        public bool RequiresSerializedExecution
        {
            get
            {
                return !IsIsolated;
            }
        }

        /// <inheritdoc />
        public bool Active
        {
            get
            {
                return Repository.GraphTransactionActive;
            }
        }

        #endregion

        #region Private-Members

        private bool _Disposed = false;
        private bool _Started = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="repository">Repository instance.</param>
        /// <param name="isIsolated">True if the repository is isolated from the caller repository.</param>
        public RepositoryTransactionSession(GraphRepositoryBase repository, bool isIsolated)
        {
            Repository = repository ?? throw new ArgumentNullException(nameof(repository));
            IsIsolated = isIsolated;
            Provider = ResolveProviderName(repository);
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task BeginTransactionAsync(TransactionExecutionOptions options, CancellationToken token = default)
        {
            ThrowIfDisposed();
            if (options == null) throw new ArgumentNullException(nameof(options));
            options.Validate();
            if (Active) throw new InvalidOperationException("A graph transaction is already active on this repository session.");
            await Repository.BeginGraphTransaction(options.TenantGUID, options.GraphGUID, options.IsolationLevel, token).ConfigureAwait(false);
            _Started = true;
        }

        /// <inheritdoc />
        public async Task CommitTransactionAsync(CancellationToken token = default)
        {
            ThrowIfDisposed();
            if (!Active) throw new InvalidOperationException("No graph transaction is active on this repository session.");
            await Repository.CommitGraphTransaction(token).ConfigureAwait(false);
            _Started = false;
        }

        /// <inheritdoc />
        public async Task RollbackTransactionAsync(CancellationToken token = default)
        {
            ThrowIfDisposed();
            if (!Active) throw new InvalidOperationException("No graph transaction is active on this repository session.");
            await Repository.RollbackGraphTransaction(token).ConfigureAwait(false);
            _Started = false;
        }

        /// <summary>
        /// Dispose.
        /// </summary>
        public void Dispose()
        {
            if (_Disposed) return;
            _Disposed = true;

            if (_Started && Active)
            {
                try { Repository.RollbackGraphTransaction(CancellationToken.None).GetAwaiter().GetResult(); } catch { }
            }

            if (IsIsolated) Repository.Dispose();
        }

        /// <summary>
        /// Dispose asynchronously.
        /// </summary>
        /// <returns>Value task.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_Disposed) return;
            _Disposed = true;

            if (_Started && Active)
            {
                try { await Repository.RollbackGraphTransaction(CancellationToken.None).ConfigureAwait(false); } catch { }
            }

            if (IsIsolated) await Repository.DisposeAsync().ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        private void ThrowIfDisposed()
        {
            if (_Disposed) throw new ObjectDisposedException(nameof(RepositoryTransactionSession));
        }

        private static string ResolveProviderName(GraphRepositoryBase repo)
        {
            if (repo == null) return null;

            string typeName = repo.GetType().Name;
            const string suffix = "GraphRepository";
            if (typeName.EndsWith(suffix, StringComparison.Ordinal) && typeName.Length > suffix.Length)
                return typeName.Substring(0, typeName.Length - suffix.Length);

            return typeName;
        }

        #endregion
    }
}
