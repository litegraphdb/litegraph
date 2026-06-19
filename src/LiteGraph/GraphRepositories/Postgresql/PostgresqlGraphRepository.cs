namespace LiteGraph.GraphRepositories.Postgresql
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Postgresql.Implementations;
    using LiteGraph.GraphRepositories.Postgresql.Queries;
    using LiteGraph.Indexing.Vector;
    using Npgsql;

    /// <summary>
    /// PostgreSQL graph repository.
    /// </summary>
    public partial class PostgresqlGraphRepository : GraphRepositoryBase
    {
        private const string ProviderName = "Postgresql";

        /// <summary>
        /// Provider settings.
        /// </summary>
        public DatabaseSettings Settings { get; }

        /// <summary>
        /// PostgreSQL schema name.
        /// </summary>
        public string Schema { get; }

        /// <summary>
        /// Number of records to retrieve for object list retrieval.
        /// </summary>
        public int SelectBatchSize
        {
            get { return _SelectBatchSize; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(SelectBatchSize));
                _SelectBatchSize = value;
            }
        }

        /// <summary>
        /// Maximum supported statement length.
        /// </summary>
        public int MaxStatementLength
        {
            get { return _MaxStatementLength; }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(MaxStatementLength));
                _MaxStatementLength = value;
            }
        }

        /// <summary>
        /// Timestamp format.
        /// </summary>
        public string TimestampFormat
        {
            get { return _TimestampFormat; }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(TimestampFormat));
                _ = DateTime.UtcNow.ToString(value);
                _TimestampFormat = value;
            }
        }

        /// <inheritdoc />
        public override IAdminMethods Admin { get; }

        /// <inheritdoc />
        public override IBatchMethods Batch { get; }

        /// <inheritdoc />
        public override ICredentialMethods Credential { get; }

        /// <inheritdoc />
        public override IEdgeMethods Edge { get; }

        /// <inheritdoc />
        public override IGraphMethods Graph { get; }

        /// <inheritdoc />
        public override ILabelMethods Label { get; }

        /// <inheritdoc />
        public override INodeMethods Node { get; }

        /// <inheritdoc />
        public override ITagMethods Tag { get; }

        /// <inheritdoc />
        public override ITenantMethods Tenant { get; }

        /// <inheritdoc />
        public override IUserMethods User { get; }

        /// <inheritdoc />
        public override IVectorMethods Vector { get; }

        /// <inheritdoc />
        public override IVectorIndexMethods VectorIndex { get; }

        /// <inheritdoc />
        public override IRequestHistoryMethods RequestHistory { get; }

        /// <inheritdoc />
        public override IAuthorizationAuditMethods AuthorizationAudit { get; }

        /// <inheritdoc />
        public override IAuthorizationRoleMethods AuthorizationRoles { get; }

        /// <inheritdoc />
        public override bool GraphTransactionActive { get { return _Transaction != null; } }

        /// <inheritdoc />
        public override Guid? GraphTransactionTenantGUID { get { return _GraphTransactionTenantGUID; } }

        /// <inheritdoc />
        public override Guid? GraphTransactionGraphGUID { get { return _GraphTransactionGraphGUID; } }

        /// <summary>
        /// Vector index manager.
        /// </summary>
        public VectorIndexManager VectorIndexManager { get; private set; }

        private readonly object _QueryLock = new object();
        private readonly NpgsqlDataSource _DataSource;
        private readonly bool _OwnsDataSource;
        private NpgsqlConnection _TransactionConnection = null;
        private NpgsqlTransaction _Transaction = null;
        private Guid? _GraphTransactionTenantGUID = null;
        private Guid? _GraphTransactionGraphGUID = null;
        private bool _GraphTransactionVectorIndexFailed = false;
        private string _GraphTransactionVectorIndexDirtyReason = null;
        private readonly List<GraphTransactionVectorIndexMutation> _GraphTransactionVectorIndexMutations = new List<GraphTransactionVectorIndexMutation>();
        private bool _OwnsVectorIndexManager = true;
        private int _SelectBatchSize = 100;
        private int _MaxStatementLength = 1000000000;
        private string _TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Database settings.</param>
        public PostgresqlGraphRepository(DatabaseSettings settings)
            : this(settings, null, null, true, true)
        {
        }

        private PostgresqlGraphRepository(
            DatabaseSettings settings,
            NpgsqlDataSource dataSource,
            VectorIndexManager vectorIndexManager,
            bool ownsDataSource,
            bool ownsVectorIndexManager)
        {
            Settings = settings?.Clone() ?? throw new ArgumentNullException(nameof(settings));
            Settings.Type = DatabaseTypeEnum.Postgresql;
            Schema = NormalizeSchema(Settings.Schema);

            if (dataSource != null)
            {
                _DataSource = dataSource;
                _OwnsDataSource = ownsDataSource;
            }
            else
            {
                NpgsqlConnectionStringBuilder builder = BuildConnectionString(Settings);
                _DataSource = NpgsqlDataSource.Create(builder.ConnectionString);
                _OwnsDataSource = true;
            }

            Admin = new AdminMethods(this);
            Batch = new BatchMethods(this);
            Credential = new CredentialMethods(this);
            Edge = new EdgeMethods(this);
            Graph = new GraphMethods(this);
            Label = new LabelMethods(this);
            Node = new NodeMethods(this);
            Tag = new TagMethods(this);
            Tenant = new TenantMethods(this);
            User = new UserMethods(this);
            Vector = new VectorMethods(this);
            VectorIndex = new VectorIndexMethods(this);
            RequestHistory = new RequestHistoryMethods(this);
            AuthorizationAudit = new AuthorizationAuditMethods(this);
            AuthorizationRoles = new AuthorizationRoleMethods(this);

            if (vectorIndexManager != null)
            {
                VectorIndexManager = vectorIndexManager;
                _OwnsVectorIndexManager = ownsVectorIndexManager;
            }
            else
            {
                string indexDirectory = Path.Combine(".", "indexes", "postgresql", Schema);
                VectorIndexManager = new VectorIndexManager(indexDirectory);
                _OwnsVectorIndexManager = true;
            }
        }

        /// <inheritdoc />
        public override void InitializeRepository()
        {
            ThrowIfDisposed();
            ExecuteQuery("CREATE SCHEMA IF NOT EXISTS " + QuoteIdentifier(Schema) + ";", true);
            ExecuteQuery(SetupQueries.CreateTablesAndIndices(), true);
            EnsureRequestHistoryTransactionDiagnosticsColumn();
            EnsureBuiltInAuthorizationRoles();
        }

        /// <inheritdoc />
        public override async Task InitializeRepositoryAsync(CancellationToken token = default)
        {
            ThrowIfDisposed();
            token.ThrowIfCancellationRequested();
            await ExecuteQueryAsync("CREATE SCHEMA IF NOT EXISTS " + QuoteIdentifier(Schema) + ";", true, token).ConfigureAwait(false);
            await ExecuteQueryAsync(SetupQueries.CreateTablesAndIndices(), true, token).ConfigureAwait(false);
            await EnsureRequestHistoryTransactionDiagnosticsColumnAsync(token).ConfigureAwait(false);
            await EnsureBuiltInAuthorizationRolesAsync(token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override void Flush()
        {
            ThrowIfDisposed();
        }

        /// <inheritdoc />
        public override Task FlushAsync(CancellationToken token = default)
        {
            ThrowIfDisposed();
            token.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override GraphRepositoryBase CreateIsolatedTransactionRepository()
        {
            ThrowIfDisposed();

            PostgresqlGraphRepository clone = new PostgresqlGraphRepository(Settings.Clone(), _DataSource, VectorIndexManager, false, false)
            {
                Logging = Logging,
                Serializer = Serializer,
                SelectBatchSize = SelectBatchSize,
                MaxStatementLength = MaxStatementLength,
                TimestampFormat = TimestampFormat
            };

            return clone;
        }

        /// <inheritdoc />
        public override Task BeginGraphTransaction(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            return BeginGraphTransaction(tenantGuid, graphGuid, TransactionIsolationLevelEnum.Default, token);
        }

        /// <inheritdoc />
        public override Task BeginGraphTransaction(Guid tenantGuid, Guid graphGuid, TransactionIsolationLevelEnum isolationLevel, CancellationToken token = default)
        {
            ThrowIfDisposed();
            token.ThrowIfCancellationRequested();

            lock (_QueryLock)
            {
                if (_Transaction != null) throw new InvalidOperationException("A graph transaction is already active.");

                NpgsqlConnection conn = _DataSource.OpenConnection();
                _TransactionConnection = conn;
                _Transaction = isolationLevel == TransactionIsolationLevelEnum.Default
                    ? conn.BeginTransaction()
                    : conn.BeginTransaction(MapIsolationLevel(isolationLevel));
                _GraphTransactionTenantGUID = tenantGuid;
                _GraphTransactionGraphGUID = graphGuid;
                _GraphTransactionVectorIndexFailed = false;
                _GraphTransactionVectorIndexDirtyReason = null;
                _GraphTransactionVectorIndexMutations.Clear();
            }

            return Task.CompletedTask;
        }

        private static IsolationLevel MapIsolationLevel(TransactionIsolationLevelEnum isolationLevel)
        {
            switch (isolationLevel)
            {
                case TransactionIsolationLevelEnum.Default:
                    return IsolationLevel.Unspecified;
                case TransactionIsolationLevelEnum.ReadCommitted:
                    return IsolationLevel.ReadCommitted;
                case TransactionIsolationLevelEnum.RepeatableRead:
                    return IsolationLevel.RepeatableRead;
                case TransactionIsolationLevelEnum.Serializable:
                    return IsolationLevel.Serializable;
                default:
                    throw new NotSupportedException("PostgreSQL transaction isolation level '" + isolationLevel + "' is not supported.");
            }
        }

        /// <inheritdoc />
        public override async Task CommitGraphTransaction(CancellationToken token = default)
        {
            ThrowIfDisposed();
            token.ThrowIfCancellationRequested();

            Guid? tenantGuid = null;
            Guid? graphGuid = null;
            bool markDirty = false;
            string dirtyReason = null;
            Exception commitException = null;
            List<GraphTransactionVectorIndexMutation> stagedMutations = new List<GraphTransactionVectorIndexMutation>();

            lock (_QueryLock)
            {
                if (_Transaction == null) throw new InvalidOperationException("No graph transaction is active.");

                tenantGuid = _GraphTransactionTenantGUID;
                graphGuid = _GraphTransactionGraphGUID;
                markDirty = _GraphTransactionVectorIndexFailed;
                dirtyReason = _GraphTransactionVectorIndexDirtyReason;
                stagedMutations = _GraphTransactionVectorIndexMutations.ToList();

                try
                {
                    _Transaction.Commit();
                }
                catch (Exception e)
                {
                    commitException = e;
                    if (_GraphTransactionVectorIndexFailed)
                    {
                        markDirty = true;
                        dirtyReason = "Graph transaction commit failed after vector index failure: " + e.Message;
                    }
                }
                finally
                {
                    ClearGraphTransaction();
                }
            }

            if (commitException == null && stagedMutations.Count > 0)
            {
                string stagedFailure = await ApplyStagedVectorIndexMutationsAsync(stagedMutations).ConfigureAwait(false);
                if (!string.IsNullOrEmpty(stagedFailure))
                {
                    markDirty = true;
                    dirtyReason = stagedFailure;
                }
            }

            if (markDirty && tenantGuid.HasValue && graphGuid.HasValue)
                MarkVectorIndexDirtyAfterTransaction(tenantGuid.Value, graphGuid.Value, dirtyReason);

            if (commitException != null)
                ExceptionDispatchInfo.Capture(commitException).Throw();
        }

        /// <inheritdoc />
        public override Task RollbackGraphTransaction(CancellationToken token = default)
        {
            ThrowIfDisposed();
            token.ThrowIfCancellationRequested();

            Guid? tenantGuid = null;
            Guid? graphGuid = null;
            bool markDirty = false;
            string dirtyReason = null;
            Exception rollbackException = null;

            lock (_QueryLock)
            {
                if (_Transaction == null) throw new InvalidOperationException("No graph transaction is active.");

                tenantGuid = _GraphTransactionTenantGUID;
                graphGuid = _GraphTransactionGraphGUID;
                markDirty = _GraphTransactionVectorIndexFailed;
                dirtyReason = _GraphTransactionVectorIndexDirtyReason
                    ?? "Graph transaction rollback after vector index failure";

                try
                {
                    _Transaction.Rollback();
                }
                catch (Exception e)
                {
                    rollbackException = e;
                    if (_GraphTransactionVectorIndexFailed)
                    {
                        markDirty = true;
                        dirtyReason = "Graph transaction rollback failed after vector index failure: " + e.Message;
                    }
                }
                finally
                {
                    ClearGraphTransaction();
                }
            }

            if (markDirty && tenantGuid.HasValue && graphGuid.HasValue)
                MarkVectorIndexDirtyAfterTransaction(tenantGuid.Value, graphGuid.Value, dirtyReason);

            if (rollbackException != null)
                ExceptionDispatchInfo.Capture(rollbackException).Throw();

            return Task.CompletedTask;
        }
    }
}
