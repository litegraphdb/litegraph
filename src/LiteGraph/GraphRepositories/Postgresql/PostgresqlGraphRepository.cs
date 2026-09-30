namespace LiteGraph.GraphRepositories.Postgresql
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Coordination;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Postgresql.Implementations;
    using LiteGraph.GraphRepositories.Postgresql.Queries;
    using Npgsql;
    using Pgvector.Npgsql;

    /// <summary>
    /// PostgreSQL graph repository.
    /// Vectors are stored in a pgvector column and searched in the database, so the repository holds no per-process
    /// vector index state and any number of processes can share one database.
    /// Requires the pgvector extension; InitializeRepository creates it when the connecting role is permitted to.
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

        /// <summary>
        /// Rows converted per batch when migrating legacy BYTEA embeddings to pgvector during InitializeRepository.
        /// Default is 1000.  Minimum is 1, maximum is 100000.
        /// </summary>
        public int EmbeddingMigrationBatchSize
        {
            get { return _EmbeddingMigrationBatchSize; }
            set
            {
                if (value < 1 || value > 100000) throw new ArgumentOutOfRangeException(nameof(EmbeddingMigrationBatchSize), "EmbeddingMigrationBatchSize must be between 1 and 100000.");
                _EmbeddingMigrationBatchSize = value;
            }
        }

        /// <summary>
        /// Maximum time in milliseconds to wait for the schema lock during InitializeRepository.
        /// Default is 600000 (10 minutes, enough for a large embedding migration running on another node).  Minimum is 1000, maximum is 3600000.
        /// </summary>
        public int SchemaLockTimeoutMs
        {
            get { return _SchemaLockTimeoutMs; }
            set
            {
                if (value < 1000 || value > 3600000) throw new ArgumentOutOfRangeException(nameof(SchemaLockTimeoutMs), "SchemaLockTimeoutMs must be between 1000 and 3600000.");
                _SchemaLockTimeoutMs = value;
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

        /// <summary>
        /// Chat endpoint methods.
        /// </summary>
        public override IChatEndpointMethods ChatEndpoint { get; }

        /// <summary>
        /// Chat thread methods.
        /// </summary>
        public override IChatThreadMethods ChatThread { get; }

        /// <summary>
        /// Chat turn methods.
        /// </summary>
        public override IChatTurnMethods ChatTurn { get; }

        /// <summary>
        /// Chat feedback methods.
        /// </summary>
        public override IChatFeedbackMethods ChatFeedback { get; }

        /// <summary>
        /// Chat settings methods.
        /// </summary>
        public override IChatSettingsMethods ChatSettings { get; }

        /// <inheritdoc />
        public override IAuthorizationAuditMethods AuthorizationAudit { get; }

        /// <inheritdoc />
        public override IAuthorizationRoleMethods AuthorizationRoles { get; }

        /// <inheritdoc />
        public override bool UsesFileBackedVectorIndexes { get { return false; } }

        /// <inheritdoc />
        public override bool GraphTransactionActive { get { return _Transaction != null; } }

        /// <inheritdoc />
        public override Guid? GraphTransactionTenantGUID { get { return _GraphTransactionTenantGUID; } }

        /// <inheritdoc />
        public override Guid? GraphTransactionGraphGUID { get { return _GraphTransactionGraphGUID; } }

        private readonly object _QueryLock = new object();
        private readonly SemaphoreSlim _TransactionSemaphore = new SemaphoreSlim(1, 1);
        private readonly NpgsqlDataSource _DataSource;
        private readonly bool _OwnsDataSource;
        private NpgsqlConnection _TransactionConnection = null;
        private NpgsqlTransaction _Transaction = null;
        private Guid? _GraphTransactionTenantGUID = null;
        private Guid? _GraphTransactionGraphGUID = null;
        private int _SelectBatchSize = 100;
        private int _EmbeddingMigrationBatchSize = 1000;
        private int _SchemaLockTimeoutMs = 600000;
        private int _MaxStatementLength = 1000000000;
        private string _TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="settings">Database settings.</param>
        public PostgresqlGraphRepository(DatabaseSettings settings)
            : this(settings, null, true)
        {
        }

        private PostgresqlGraphRepository(
            DatabaseSettings settings,
            NpgsqlDataSource dataSource,
            bool ownsDataSource)
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
                NpgsqlDataSourceBuilder dataSourceBuilder = new NpgsqlDataSourceBuilder(builder.ConnectionString);
                dataSourceBuilder.UseVector();
                _DataSource = dataSourceBuilder.Build();
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
            ChatEndpoint = new ChatEndpointMethods(this);
            ChatThread = new ChatThreadMethods(this);
            ChatTurn = new ChatTurnMethods(this);
            ChatFeedback = new ChatFeedbackMethods(this);
            ChatSettings = new ChatSettingsMethods(this);
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">The pgvector extension is not installed or cannot be created by the connecting role.</exception>
        /// <exception cref="LockNotAcquiredException">Another process held the schema lock for longer than SchemaLockTimeoutMs.</exception>
        public override void InitializeRepository()
        {
            Task.Run(() => InitializeRepositoryAsync()).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        /// <exception cref="InvalidOperationException">The pgvector extension is not installed or cannot be created by the connecting role.</exception>
        /// <exception cref="LockNotAcquiredException">Another process held the schema lock for longer than SchemaLockTimeoutMs.</exception>
        public override async Task InitializeRepositoryAsync(CancellationToken token = default)
        {
            ThrowIfDisposed();
            token.ThrowIfCancellationRequested();

            await using (ILockHandle schemaLock = await LockProvider.AcquireAsync(
                LockKeys.Schema,
                LockModeEnum.Exclusive,
                LockAcquireOptions.WaitUpTo(SchemaLockTimeoutMs),
                token).ConfigureAwait(false))
            {
                await EnsureVectorExtensionAsync(token).ConfigureAwait(false);
                await ExecuteQueryAsync("CREATE SCHEMA IF NOT EXISTS " + QuoteIdentifier(Schema) + ";", true, token).ConfigureAwait(false);
                await ExecuteQueryAsync(SetupQueries.CreateTablesAndIndices(), true, token).ConfigureAwait(false);
                await EnsureSchemaMigrationsTableAsync(token).ConfigureAwait(false);
                await EnsureRequestHistoryTransactionDiagnosticsColumnAsync(token).ConfigureAwait(false);
                await EnsureUserAdminFlagColumnsAsync(token).ConfigureAwait(false);
                await EnsureChatEndpointContextWindowColumnAsync(token).ConfigureAwait(false);
                await MigrateEmbeddingsToPgvectorAsync(token).ConfigureAwait(false);
                await EnsureIntegrityConstraintsAsync(token).ConfigureAwait(false);
                await EnsureBuiltInAuthorizationRolesAsync(token).ConfigureAwait(false);
            }
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

            PostgresqlGraphRepository clone = new PostgresqlGraphRepository(Settings.Clone(), _DataSource, false)
            {
                LockProvider = LockProvider,
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
        public override async Task BeginGraphTransaction(Guid tenantGuid, Guid graphGuid, TransactionIsolationLevelEnum isolationLevel, CancellationToken token = default)
        {
            ThrowIfDisposed();
            token.ThrowIfCancellationRequested();

            await _TransactionSemaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                if (_Transaction != null) throw new InvalidOperationException("A graph transaction is already active.");

                NpgsqlConnection conn = await _DataSource.OpenConnectionAsync(token).ConfigureAwait(false);
                NpgsqlTransaction transaction;
                try
                {
                    transaction = isolationLevel == TransactionIsolationLevelEnum.Default
                        ? await conn.BeginTransactionAsync(token).ConfigureAwait(false)
                        : await conn.BeginTransactionAsync(MapIsolationLevel(isolationLevel), token).ConfigureAwait(false);
                }
                catch
                {
                    await conn.DisposeAsync().ConfigureAwait(false);
                    throw;
                }

                lock (_QueryLock)
                {
                    _TransactionConnection = conn;
                    _Transaction = transaction;
                    _GraphTransactionTenantGUID = tenantGuid;
                    _GraphTransactionGraphGUID = graphGuid;
                }
            }
            finally
            {
                _TransactionSemaphore.Release();
            }
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

            Exception commitException = null;

            await _TransactionSemaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                NpgsqlTransaction transaction;
                lock (_QueryLock)
                {
                    if (_Transaction == null) throw new InvalidOperationException("No graph transaction is active.");
                    transaction = _Transaction;
                }

                try
                {
                    // Commit must run to completion once started so its outcome is never ambiguous to the caller.
                    await transaction.CommitAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    commitException = e;
                }
                finally
                {
                    await ClearGraphTransactionAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                _TransactionSemaphore.Release();
            }

            if (commitException != null)
                ExceptionDispatchInfo.Capture(commitException).Throw();
        }

        /// <inheritdoc />
        public override async Task RollbackGraphTransaction(CancellationToken token = default)
        {
            ThrowIfDisposed();
            token.ThrowIfCancellationRequested();

            Exception rollbackException = null;

            await _TransactionSemaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                NpgsqlTransaction transaction;
                lock (_QueryLock)
                {
                    if (_Transaction == null) throw new InvalidOperationException("No graph transaction is active.");
                    transaction = _Transaction;
                }

                try
                {
                    // Rollback must run to completion once started so the connection is not returned to the pool mid-transaction.
                    await transaction.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    rollbackException = e;
                }
                finally
                {
                    await ClearGraphTransactionAsync().ConfigureAwait(false);
                }
            }
            finally
            {
                _TransactionSemaphore.Release();
            }

            if (rollbackException != null)
                ExceptionDispatchInfo.Capture(rollbackException).Throw();
        }
    }
}
