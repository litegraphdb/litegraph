namespace LiteGraph.GraphRepositories
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ExpressionTree;
    using LiteGraph;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.Serialization;

    /// <summary>
    /// Graph repository base class.
    /// The graph repository base class is only responsible for primitives.
    /// Validation and cross-cutting functions should be performed in LiteGraphClient rather than in the graph repository base.
    /// </summary>
    public abstract class GraphRepositoryBase : IDisposable, IAsyncDisposable, IRepositorySessionFactory
    {
        #region Public-Members

        /// <summary>
        /// Logging.
        /// </summary>
        public LoggingSettings Logging
        {
            get
            {
                return _Logging;
            }
            set
            {
                if (value == null) value = new LoggingSettings();
                _Logging = value;
            }
        }

        /// <summary>
        /// Serializer.
        /// </summary>
        public Serializer Serializer
        {
            get
            {
                return _Serializer;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Serializer));
                _Serializer = value;
            }
        }

        /// <summary>
        /// Admin methods.
        /// </summary>
        public abstract IAdminMethods Admin { get; }

        /// <summary>
        /// Tenant methods.
        /// </summary>
        public abstract ITenantMethods Tenant { get; }

        /// <summary>
        /// User methods.
        /// </summary>
        public abstract IUserMethods User { get; }

        /// <summary>
        /// Credential methods.
        /// </summary>
        public abstract ICredentialMethods Credential { get; }

        /// <summary>
        /// Label methods.
        /// </summary>
        public abstract ILabelMethods Label { get; }

        /// <summary>
        /// Tag methods.
        /// </summary>
        public abstract ITagMethods Tag { get; }

        /// <summary>
        /// Vector methods.
        /// </summary>
        public abstract IVectorMethods Vector { get; }

        /// <summary>
        /// Graph methods.
        /// </summary>
        public abstract IGraphMethods Graph { get; }

        /// <summary>
        /// Node methods.
        /// </summary>
        public abstract INodeMethods Node { get; }

        /// <summary>
        /// Edge methods.
        /// </summary>
        public abstract IEdgeMethods Edge { get; }

        /// <summary>
        /// Batch methods.
        /// </summary>
        public abstract IBatchMethods Batch { get; }

        /// <summary>
        /// Vector index methods.
        /// </summary>
        public abstract IVectorIndexMethods VectorIndex { get; }

        /// <summary>
        /// Request history methods.
        /// </summary>
        public abstract IRequestHistoryMethods RequestHistory { get; }

        /// <summary>
        /// Authorization audit methods.
        /// </summary>
        public abstract IAuthorizationAuditMethods AuthorizationAudit { get; }

        /// <summary>
        /// Authorization role methods.
        /// </summary>
        public abstract IAuthorizationRoleMethods AuthorizationRoles { get; }

        /// <summary>
        /// Indicates whether a graph-scoped repository transaction is currently active.
        /// </summary>
        public virtual bool GraphTransactionActive
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Tenant GUID for the active graph transaction, if any.
        /// </summary>
        public virtual Guid? GraphTransactionTenantGUID
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Graph GUID for the active graph transaction, if any.
        /// </summary>
        public virtual Guid? GraphTransactionGraphGUID
        {
            get
            {
                return null;
            }
        }

        #endregion

        #region Private-Members

        private LoggingSettings _Logging = new LoggingSettings();
        private Serializer _Serializer = new Serializer();
        private bool _Disposed = false;

        #endregion

        #region Public-Methods

        /// <summary>
        /// Initialize the repository.
        /// </summary>
        public abstract void InitializeRepository();

        /// <summary>
        /// Initialize the repository asynchronously.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public virtual Task InitializeRepositoryAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            InitializeRepository();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Flush database contents to disk.  Only required if using an in-memory instance of a LiteGraph database.
        /// </summary>
        public abstract void Flush();

        /// <summary>
        /// Flush database contents asynchronously.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public virtual Task FlushAsync(CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Flush();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Create a repository instance that owns isolated transaction state for a single transaction execution.
        /// Providers that do not override this method continue to use the caller repository and the legacy transaction gate.
        /// </summary>
        /// <returns>Repository instance.</returns>
        public virtual GraphRepositoryBase CreateIsolatedTransactionRepository()
        {
            return this;
        }

        /// <summary>
        /// Create a repository session that owns transaction state for a single transaction execution.
        /// Providers that do not create an isolated repository return a session requiring serialized execution.
        /// </summary>
        /// <param name="options">Transaction execution options.</param>
        /// <returns>Repository session.</returns>
        public virtual IRepositorySession CreateRepositorySession(TransactionExecutionOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            options.Validate();

            GraphRepositoryBase repo = CreateIsolatedTransactionRepository();
            if (repo == null) throw new InvalidOperationException("Transaction repository factory returned null.");

            return new RepositoryTransactionSession(repo, !ReferenceEquals(repo, this));
        }

        /// <summary>
        /// Begin a graph-scoped transaction.  Transactions are limited to one tenant and one graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public virtual Task BeginGraphTransaction(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            throw new NotSupportedException(GetType().Name + " does not support graph-scoped transactions.");
        }

        /// <summary>
        /// Begin a graph-scoped transaction with a requested isolation level. Transactions are limited to one tenant and one graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="isolationLevel">Requested isolation level.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public virtual Task BeginGraphTransaction(Guid tenantGuid, Guid graphGuid, TransactionIsolationLevelEnum isolationLevel, CancellationToken token = default)
        {
            return BeginGraphTransaction(tenantGuid, graphGuid, token);
        }

        /// <summary>
        /// Commit the active graph-scoped transaction.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public virtual Task CommitGraphTransaction(CancellationToken token = default)
        {
            throw new NotSupportedException(GetType().Name + " does not support graph-scoped transactions.");
        }

        /// <summary>
        /// Roll back the active graph-scoped transaction.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public virtual Task RollbackGraphTransaction(CancellationToken token = default)
        {
            throw new NotSupportedException(GetType().Name + " does not support graph-scoped transactions.");
        }

        /// <summary>
        /// Dispose resources owned by the repository.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose resources owned by the repository asynchronously.
        /// </summary>
        /// <returns>Value task.</returns>
        public async ValueTask DisposeAsync()
        {
            await DisposeAsyncCore().ConfigureAwait(false);
            if (!Disposed) Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Protected-Methods

        /// <summary>
        /// Dispose resources owned by the repository.
        /// </summary>
        /// <param name="disposing">Disposing managed resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            _Disposed = true;
        }

        /// <summary>
        /// Dispose async resources owned by the repository.
        /// </summary>
        /// <returns>Value task.</returns>
        protected virtual ValueTask DisposeAsyncCore()
        {
            return ValueTask.CompletedTask;
        }

        /// <summary>
        /// Throw if the repository has been disposed.
        /// </summary>
        protected void ThrowIfDisposed()
        {
            if (_Disposed) throw new ObjectDisposedException(GetType().FullName);
        }

        /// <summary>
        /// Indicates whether the repository has been disposed.
        /// </summary>
        protected bool Disposed
        {
            get
            {
                return _Disposed;
            }
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
