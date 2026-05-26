namespace LiteGraph.GraphRepositories.Sqlite
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.ExceptionServices;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite.Implementations;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Indexing.Vector;
    using Microsoft.Data.Sqlite;

    /// <summary>
    /// Sqlite graph repository.
    /// The graph repository base class is only responsible for primitives.
    /// Validation and cross-cutting functions should be performed in LiteGraphClient rather than in the graph repository base.
    /// </summary>
    public class SqliteGraphRepository : GraphRepositoryBase
    {
#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities

        // Helpful references for Sqlite JSON:
        // https://stackoverflow.com/questions/33432421/sqlite-json1-example-for-json-extract-set
        // https://www.sqlite.org/json1.html

        private const string ProviderName = "Sqlite";

        #region Public-Members

        /// <summary>
        /// Sqlite database filename.
        /// </summary>
        public string Filename
        {
            get
            {
                return _Filename;
            }
        }

        /// <summary>
        /// Maximum supported statement length.
        /// Default for Sqlite is 1,000,000,000 (see https://www.sqlite.org/limits.html).
        /// </summary>
        public int MaxStatementLength
        {
            get
            {
                return _MaxStatementLength;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(MaxStatementLength));
                _MaxStatementLength = value;
            }
        }

        /// <summary>
        /// Number of records to retrieve for object list retrieval.
        /// </summary>
        public int SelectBatchSize
        {
            get
            {
                return _SelectBatchSize;
            }
            set
            {
                if (value < 1) throw new ArgumentOutOfRangeException(nameof(SelectBatchSize));
                _SelectBatchSize = value;
            }
        }

        /// <summary>
        /// Timestamp format.
        /// </summary>
        public string TimestampFormat
        {
            get
            {
                return _TimestampFormat;
            }
            set
            {
                if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(TimestampFormat));
                string test = DateTime.UtcNow.ToString(value);
                _TimestampFormat = value;
            }
        }

        /// <summary>
        /// Admin methods.
        /// </summary>
        public override IAdminMethods Admin { get; }

        /// <summary>
        /// Batch methods.
        /// </summary>
        public override IBatchMethods Batch { get; }

        /// <summary>
        /// Credential methods.
        /// </summary>
        public override ICredentialMethods Credential { get; }

        /// <summary>
        /// Edge methods.
        /// </summary>
        public override IEdgeMethods Edge { get; }

        /// <summary>
        /// Graph methods.
        /// </summary>
        public override IGraphMethods Graph { get; }

        /// <summary>
        /// Label methods.
        /// </summary>
        public override ILabelMethods Label { get; }

        /// <summary>
        /// Node methods.
        /// </summary>
        public override INodeMethods Node { get; }

        /// <summary>
        /// Tag methods.
        /// </summary>
        public override ITagMethods Tag { get; }

        /// <summary>
        /// Tenant methods.
        /// </summary>
        public override ITenantMethods Tenant { get; }

        /// <summary>
        /// User methods.
        /// </summary>
        public override IUserMethods User { get; }

        /// <summary>
        /// Vector methods.
        /// </summary>
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
        public override bool GraphTransactionActive
        {
            get
            {
                return _Transaction != null;
            }
        }

        /// <inheritdoc />
        public override Guid? GraphTransactionTenantGUID
        {
            get
            {
                return _GraphTransactionTenantGUID;
            }
        }

        /// <inheritdoc />
        public override Guid? GraphTransactionGraphGUID
        {
            get
            {
                return _GraphTransactionGraphGUID;
            }
        }

        /// <summary>
        /// Vector index manager.
        /// </summary>
        public VectorIndexManager VectorIndexManager { get; private set; }

        #endregion

        #region Internal-Members

        #endregion

        #region Private-Members

        private string _Filename = "litegraph.db";
        private bool _InMemory = false;

        private readonly object _QueryLock = new object();
        private string _ConnectionString = "Data Source=litegraph.db;Mode=ReadWriteCreate;Cache=Shared;";
        private SqliteConnection _SqliteConnection = null;
        private SqliteConnection _TransactionConnection = null;
        private SqliteTransaction _Transaction = null;
        private Guid? _GraphTransactionTenantGUID = null;
        private Guid? _GraphTransactionGraphGUID = null;
        private bool _GraphTransactionVectorIndexTouched = false;
        private bool _GraphTransactionVectorIndexFailed = false;
        private string _GraphTransactionVectorIndexDirtyReason = null;

        private int _SelectBatchSize = 100;
        private int _MaxStatementLength = 1000000000; // https://www.sqlite.org/limits.html
        private string _TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="filename">Sqlite database filename.</param>
        /// <param name="inMemory">Boolean indicating whether or not the database should be held in-memory and flushed periodically to disk by user instruction.</param>
        public SqliteGraphRepository(string filename = "litegraph.db", bool inMemory = false)
        {
            if (string.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));

            _InMemory = inMemory;

            _Filename = filename;

            if (!_InMemory) _ConnectionString = "Data Source=" + filename + ";Pooling=true";
            else _ConnectionString = "Data Source=LiteGraphMemory;Mode=Memory;Cache=Shared";

            _SqliteConnection = new SqliteConnection(_ConnectionString);
            _SqliteConnection.Open();

            ApplyPerformanceSettings(_SqliteConnection);

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

            // Initialize vector index manager
            string indexDirectory = Path.Combine(Path.GetDirectoryName(_Filename) ?? ".", "indexes");
            VectorIndexManager = new VectorIndexManager(indexDirectory);
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public override void InitializeRepository()
        {
            ThrowIfDisposed();

            if (_InMemory && File.Exists(_Filename))
            {
                using (SqliteConnection diskDatabase = new SqliteConnection($"Data Source={_Filename};Pooling=false"))
                {
                    diskDatabase.Open();
                    diskDatabase.BackupDatabase(_SqliteConnection);
                }
            }

            ExecuteQuery(SetupQueries.CreateTablesAndIndices());
            EnsureCredentialScopeColumns();
            EnsureGraphVectorIndexConsistencyColumns();
            EnsureBuiltInAuthorizationRoles();
            EnsureRequestHistoryCorrelationColumns();
        }

        /// <summary>
        /// Saves the in-memory database back to disk file
        /// </summary>
        public override void Flush()
        {
            ThrowIfDisposed();

            if (_InMemory)
            {
                // Create a backup first for safety
                string backupPath = _Filename + ".backup";
                if (File.Exists(_Filename))
                {
                    File.Copy(_Filename, backupPath, true);
                }

                try
                {
                    using (SqliteConnection diskDatabase = new SqliteConnection($"Data Source={_Filename};Pooling=false"))
                    {
                        diskDatabase.Open();
                        // Backup from the instance connection to disk
                        _SqliteConnection.BackupDatabase(diskDatabase);
                    }

                    if (File.Exists(backupPath)) File.Delete(backupPath);
                }
                catch (Exception e)
                {
                    // Restore from backup on failure
                    if (File.Exists(backupPath))
                    {
                        File.Copy(backupPath, _Filename, true);
                        File.Delete(backupPath);
                    }
                    throw new Exception($"Failed to save database. Original file restored from backup. Error: {e.Message}", e);
                }
            }
        }

        /// <inheritdoc />
        public override Task BeginGraphTransaction(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            ThrowIfDisposed();
            token.ThrowIfCancellationRequested();

            lock (_QueryLock)
            {
                if (_Transaction != null) throw new InvalidOperationException("A graph transaction is already active.");

                SqliteConnection conn = _InMemory ? _SqliteConnection : new SqliteConnection(_ConnectionString);
                if (!_InMemory)
                {
                    try
                    {
                        conn.Open();
                    }
                    catch (Exception e) when (IsTransientOpenFailure(e))
                    {
                        LogPersistentConnectionFallback("begin graph transaction", e);
                        try { conn.Dispose(); } catch { }
                        EnsurePersistentConnectionOpen();
                        conn = _SqliteConnection;
                    }
                }

                _TransactionConnection = conn;
                _Transaction = conn.BeginTransaction();
                _GraphTransactionTenantGUID = tenantGuid;
                _GraphTransactionGraphGUID = graphGuid;
                _GraphTransactionVectorIndexTouched = false;
                _GraphTransactionVectorIndexFailed = false;
                _GraphTransactionVectorIndexDirtyReason = null;
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public override Task CommitGraphTransaction(CancellationToken token = default)
        {
            ThrowIfDisposed();
            token.ThrowIfCancellationRequested();

            Guid? tenantGuid = null;
            Guid? graphGuid = null;
            bool markDirty = false;
            string dirtyReason = null;
            Exception commitException = null;

            lock (_QueryLock)
            {
                if (_Transaction == null) throw new InvalidOperationException("No graph transaction is active.");

                tenantGuid = _GraphTransactionTenantGUID;
                graphGuid = _GraphTransactionGraphGUID;
                markDirty = _GraphTransactionVectorIndexFailed;
                dirtyReason = _GraphTransactionVectorIndexDirtyReason;

                try
                {
                    _Transaction.Commit();
                }
                catch (Exception e)
                {
                    commitException = e;
                    if (_GraphTransactionVectorIndexTouched || _GraphTransactionVectorIndexFailed)
                    {
                        markDirty = true;
                        dirtyReason = "Graph transaction commit failed after vector index mutation: " + e.Message;
                    }
                }
                finally
                {
                    ClearGraphTransaction();
                }
            }

            if (markDirty && tenantGuid.HasValue && graphGuid.HasValue)
                MarkVectorIndexDirtyAfterTransaction(tenantGuid.Value, graphGuid.Value, dirtyReason);

            if (commitException != null)
                ExceptionDispatchInfo.Capture(commitException).Throw();

            return Task.CompletedTask;
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
                markDirty = _GraphTransactionVectorIndexTouched || _GraphTransactionVectorIndexFailed;
                dirtyReason = _GraphTransactionVectorIndexDirtyReason
                    ?? "Graph transaction rollback after vector index mutation";

                try
                {
                    _Transaction.Rollback();
                }
                catch (Exception e)
                {
                    rollbackException = e;
                    if (_GraphTransactionVectorIndexTouched || _GraphTransactionVectorIndexFailed)
                    {
                        markDirty = true;
                        dirtyReason = "Graph transaction rollback failed after vector index mutation: " + e.Message;
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

        #endregion

        #region Internal-Methods

        internal void NoteVectorIndexMutation(Guid tenantGuid, Guid graphGuid, string reason)
        {
            lock (_QueryLock)
            {
                if (_Transaction == null) return;
                if (_GraphTransactionTenantGUID != tenantGuid || _GraphTransactionGraphGUID != graphGuid) return;

                _GraphTransactionVectorIndexTouched = true;
            }
        }

        internal void NoteVectorIndexFailure(Guid tenantGuid, Guid graphGuid, string reason)
        {
            lock (_QueryLock)
            {
                if (_Transaction == null) return;
                if (_GraphTransactionTenantGUID != tenantGuid || _GraphTransactionGraphGUID != graphGuid) return;

                _GraphTransactionVectorIndexFailed = true;
                _GraphTransactionVectorIndexDirtyReason = reason;
            }
        }

        internal DataTable ExecuteQuery(string query, bool isTransaction = false)
        {
            return ExecuteRepositoryOperation(
                ClassifySqlOperation(query, isTransaction),
                query,
                isTransaction,
                1,
                () => ExecuteQueryCore(query, isTransaction));
        }

        private DataTable ExecuteQueryCore(string query, bool isTransaction = false)
        {
            ThrowIfDisposed();

            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException(query);
            if (query.Length > MaxStatementLength) throw new ArgumentException("Query exceeds maximum statement length of " + MaxStatementLength + " characters.");

            DataTable result = new DataTable();

            if (isTransaction && _Transaction == null)
            {
                query = query.Trim();
                query = "BEGIN TRANSACTION; " + query + " END TRANSACTION;";
            }

            if (Logging.LogQueries) Logging.Log(SeverityEnum.Debug, "query: " + query);

            lock (_QueryLock)
            {
                if (_Transaction != null)
                {
                    try
                    {
                        using (SqliteCommand cmd = new SqliteCommand(query, _TransactionConnection))
                        {
                            cmd.Transaction = _Transaction;
                            using (SqliteDataReader rdr = cmd.ExecuteReader())
                            {
                                result.Load(rdr);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        e.Data.Add("IsTransaction", true);
                        e.Data.Add("Query", query);
                        throw;
                    }

                    if (Logging.LogResults) Logging.Log(SeverityEnum.Debug, "result: " + query + ": " + (result != null ? result.Rows.Count + " rows" : "(null)"));
                    return result;
                }
            }

            if (_InMemory)
            {
                lock (_QueryLock)
                {
                    try
                    {
                        using (SqliteCommand cmd = new SqliteCommand(query, _SqliteConnection))
                        {
                            using (SqliteDataReader rdr = cmd.ExecuteReader())
                            {
                                result.Load(rdr);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        if (isTransaction)
                        {
                            using (SqliteCommand cmd = new SqliteCommand("ROLLBACK;", _SqliteConnection))
                                cmd.ExecuteNonQuery();
                        }

                        e.Data.Add("IsTransaction", isTransaction);
                        e.Data.Add("Query", query);
                        throw;
                    }
                }
            }
            else
            {
                using (SqliteConnection conn = new SqliteConnection(_ConnectionString))
                {
                    try
                    {
                        conn.Open();

                        using (SqliteCommand cmd = new SqliteCommand(query, conn))
                        {
                            using (SqliteDataReader rdr = cmd.ExecuteReader())
                            {
                                result.Load(rdr);
                            }
                        }

                        conn.Close();
                    }
                    catch (Exception e)
                    {
                        if (IsTransientOpenFailure(e))
                        {
                            LogPersistentConnectionFallback("execute query", e);
                            return ExecuteQueryWithPersistentConnection(query, isTransaction);
                        }

                        if (isTransaction)
                        {
                            try
                            {
                                using (SqliteCommand cmd = new SqliteCommand("ROLLBACK;", conn))
                                    cmd.ExecuteNonQuery();
                            }
                            catch { }
                        }

                        e.Data.Add("IsTransaction", isTransaction);
                        e.Data.Add("Query", query);
                        throw;
                    }
                }
            }

            if (Logging.LogResults) Logging.Log(SeverityEnum.Debug, "result: " + query + ": " + (result != null ? result.Rows.Count + " rows" : "(null)"));
            return result;
        }

        internal async Task<DataTable> ExecuteQueryAsync(string query, bool isTransaction = false, CancellationToken token = default)
        {
            return await ExecuteRepositoryOperationAsync(
                ClassifySqlOperation(query, isTransaction),
                query,
                isTransaction,
                1,
                async () => await ExecuteQueryCoreAsync(query, isTransaction, token).ConfigureAwait(false)).ConfigureAwait(false);
        }

        private async Task<DataTable> ExecuteQueryCoreAsync(string query, bool isTransaction = false, CancellationToken token = default)
        {
            ThrowIfDisposed();

            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException(query);
            if (query.Length > MaxStatementLength) throw new ArgumentException("Query exceeds maximum statement length of " + MaxStatementLength + " characters.");

            token.ThrowIfCancellationRequested();

            DataTable result = new DataTable();

            if (isTransaction && _Transaction == null)
            {
                query = query.Trim();
                query = "BEGIN TRANSACTION; " + query + " END TRANSACTION;";
            }

            if (Logging.LogQueries) Logging.Log(SeverityEnum.Debug, "query: " + query);

            lock (_QueryLock)
            {
                if (_Transaction != null)
                {
                    try
                    {
                        using (SqliteCommand cmd = new SqliteCommand(query, _TransactionConnection))
                        {
                            cmd.Transaction = _Transaction;
                            token.ThrowIfCancellationRequested();
                            using (SqliteDataReader rdr = cmd.ExecuteReader())
                            {
                                result.Load(rdr);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        e.Data.Add("IsTransaction", true);
                        e.Data.Add("Query", query);
                        throw;
                    }

                    if (Logging.LogResults) Logging.Log(SeverityEnum.Debug, "result: " + query + ": " + (result != null ? result.Rows.Count + " rows" : "(null)"));
                    return result;
                }
            }

            if (_InMemory)
            {
                // Use the instance connection for in-memory operations
                try
                {
                    using (SqliteCommand cmd = new SqliteCommand(query, _SqliteConnection))
                    {
                        token.ThrowIfCancellationRequested();
                        using (SqliteDataReader rdr = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                        {
                            result.Load(rdr);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (isTransaction)
                    {
                        using (SqliteCommand cmd = new SqliteCommand("ROLLBACK;", _SqliteConnection))
                            await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                    }

                    e.Data.Add("IsTransaction", isTransaction);
                    e.Data.Add("Query", query);
                    throw;
                }
            }
            else
            {
                using (SqliteConnection conn = new SqliteConnection(_ConnectionString))
                {
                    try
                    {
                        await conn.OpenAsync(token).ConfigureAwait(false);

                        using (SqliteCommand cmd = new SqliteCommand(query, conn))
                        {
                            token.ThrowIfCancellationRequested();
                            using (SqliteDataReader rdr = await cmd.ExecuteReaderAsync(token).ConfigureAwait(false))
                            {
                                result.Load(rdr);
                            }
                        }

                        conn.Close();
                    }
                    catch (Exception e)
                    {
                        if (IsTransientOpenFailure(e))
                        {
                            LogPersistentConnectionFallback("execute query async", e);
                            token.ThrowIfCancellationRequested();
                            return ExecuteQueryWithPersistentConnection(query, isTransaction);
                        }

                        if (isTransaction)
                        {
                            try
                            {
                                using (SqliteCommand cmd = new SqliteCommand("ROLLBACK;", conn))
                                    await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
                            }
                            catch { }
                        }

                        e.Data.Add("IsTransaction", isTransaction);
                        e.Data.Add("Query", query);
                        throw;
                    }
                }
            }

            if (Logging.LogResults) Logging.Log(SeverityEnum.Debug, "result: " + query + ": " + (result != null ? result.Rows.Count + " rows" : "(null)"));
            return result;
        }

        internal DataTable ExecuteQueries(IEnumerable<string> queries, bool isTransaction = false)
        {
            List<string> queryList = queries?.ToList();
            return ExecuteRepositoryOperation(
                "batch",
                null,
                isTransaction,
                queryList?.Count(q => !String.IsNullOrEmpty(q)) ?? 0,
                () => ExecuteQueriesCore(queryList, isTransaction));
        }

        private DataTable ExecuteQueriesCore(IEnumerable<string> queries, bool isTransaction = false)
        {
            ThrowIfDisposed();

            if (queries == null || !queries.Any()) throw new ArgumentNullException(nameof(queries));

            DataTable result = new DataTable();

            lock (_QueryLock)
            {
                if (_Transaction != null)
                {
                    try
                    {
                        DataTable lastResult = null;

                        foreach (string query in queries.Where(q => !string.IsNullOrEmpty(q)))
                        {
                            if (query.Length > MaxStatementLength)
                                throw new ArgumentException($"Query exceeds maximum statement length of {MaxStatementLength} characters.");

                            if (Logging.LogQueries) Logging.Log(SeverityEnum.Debug, "query: " + query);

                            using (SqliteCommand cmd = new SqliteCommand(query, _TransactionConnection))
                            {
                                cmd.Transaction = _Transaction;

                                using (SqliteDataReader rdr = cmd.ExecuteReader())
                                {
                                    lastResult = new DataTable();
                                    lastResult.Load(rdr);
                                }

                                if (lastResult != null && lastResult.Rows.Count > 0)
                                {
                                    result = lastResult;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        e.Data.Add("IsTransaction", true);
                        e.Data.Add("Queries", string.Join("; ", queries));
                        throw;
                    }

                    if (Logging.LogResults) Logging.Log(SeverityEnum.Debug, "result: " + (result != null ? result.Rows.Count + " rows" : "(null)"));
                    return result;
                }
            }

            if (_InMemory)
            {
                lock (_QueryLock)
                {
                    SqliteTransaction transaction = null;

                    try
                    {
                        if (isTransaction)
                        {
                            transaction = _SqliteConnection.BeginTransaction();
                        }

                        DataTable lastResult = null;

                        foreach (string query in queries.Where(q => !string.IsNullOrEmpty(q)))
                        {
                            if (query.Length > MaxStatementLength)
                                throw new ArgumentException($"Query exceeds maximum statement length of {MaxStatementLength} characters.");

                            if (Logging.LogQueries) Logging.Log(SeverityEnum.Debug, "query: " + query);

                            using (SqliteCommand cmd = new SqliteCommand(query, _SqliteConnection))
                            {
                                if (transaction != null)
                                {
                                    cmd.Transaction = transaction;
                                }

                                using (SqliteDataReader rdr = cmd.ExecuteReader())
                                {
                                    lastResult = new DataTable();
                                    lastResult.Load(rdr);
                                }

                                if (lastResult != null && lastResult.Rows.Count > 0)
                                {
                                    result = lastResult;
                                }
                            }
                        }

                        transaction?.Commit();
                    }
                    catch (Exception e)
                    {
                        transaction?.Rollback();

                        e.Data.Add("IsTransaction", isTransaction);
                        e.Data.Add("Queries", string.Join("; ", queries));
                        throw;
                    }
                    finally
                    {
                        transaction?.Dispose();
                    }
                }
            }
            else
            {
                using (SqliteConnection conn = new SqliteConnection(_ConnectionString))
                {
                    SqliteTransaction transaction = null;

                    try
                    {
                        conn.Open();

                        if (isTransaction)
                        {
                            transaction = conn.BeginTransaction();
                        }

                        DataTable lastResult = null;

                        foreach (string query in queries.Where(q => !string.IsNullOrEmpty(q)))
                        {
                            if (query.Length > MaxStatementLength)
                                throw new ArgumentException($"Query exceeds maximum statement length of {MaxStatementLength} characters.");

                            if (Logging.LogQueries) Logging.Log(SeverityEnum.Debug, "query: " + query);

                            using (SqliteCommand cmd = new SqliteCommand(query, conn))
                            {
                                if (transaction != null)
                                {
                                    cmd.Transaction = transaction;
                                }

                                using (SqliteDataReader rdr = cmd.ExecuteReader())
                                {
                                    lastResult = new DataTable();
                                    lastResult.Load(rdr);
                                }

                                if (lastResult != null && lastResult.Rows.Count > 0)
                                {
                                    result = lastResult;
                                }
                            }
                        }

                        transaction?.Commit();
                    }
                    catch (Exception e)
                    {
                        if (IsTransientOpenFailure(e))
                        {
                            LogPersistentConnectionFallback("execute query batch", e);
                            return ExecuteQueriesWithPersistentConnection(queries, isTransaction);
                        }

                        try { transaction?.Rollback(); } catch { }

                        e.Data.Add("IsTransaction", isTransaction);
                        e.Data.Add("Queries", string.Join("; ", queries));
                        throw;
                    }
                    finally
                    {
                        transaction?.Dispose();
                        conn.Close();
                    }
                }
            }

            if (Logging.LogResults) Logging.Log(SeverityEnum.Debug, "result: " + (result != null ? result.Rows.Count + " rows" : "(null)"));
            return result;
        }

        #endregion

        #region Protected-Methods

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            if (Disposed) return;

            Exception disposalException = null;

            if (disposing)
            {
                lock (_QueryLock)
                {
                    try
                    {
                        if (_Transaction != null)
                        {
                            try { _Transaction.Rollback(); } catch { }
                            ClearGraphTransaction();
                        }

                        if (_InMemory && _SqliteConnection != null) Flush();
                    }
                    catch (Exception e)
                    {
                        disposalException = e;
                    }
                    finally
                    {
                        VectorIndexManager?.Dispose();
                        VectorIndexManager = null;

                        _SqliteConnection?.Close();
                        _SqliteConnection?.Dispose();
                        _SqliteConnection = null;
                    }
                }
            }

            base.Dispose(disposing);

            if (disposalException != null)
            {
                throw new InvalidOperationException("An error occurred while disposing the SQLite graph repository.", disposalException);
            }
        }

        #endregion

        #region Private-Methods

        private DataTable ExecuteRepositoryOperation(
            string operation,
            string query,
            bool isTransaction,
            int statementCount,
            Func<DataTable> action)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            using Activity activity = StartRepositoryOperationActivity(operation, isTransaction, statementCount);

            try
            {
                DataTable result = action();
                stopwatch.Stop();
                CompleteRepositoryOperation(activity, operation, isTransaction, statementCount, result?.Rows.Count ?? 0, stopwatch.Elapsed.TotalMilliseconds, true, null);
                return result;
            }
            catch (Exception e)
            {
                stopwatch.Stop();
                CompleteRepositoryOperation(activity, operation, isTransaction, statementCount, 0, stopwatch.Elapsed.TotalMilliseconds, false, e);
                throw;
            }
        }

        private async Task<DataTable> ExecuteRepositoryOperationAsync(
            string operation,
            string query,
            bool isTransaction,
            int statementCount,
            Func<Task<DataTable>> action)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            using Activity activity = StartRepositoryOperationActivity(operation, isTransaction, statementCount);

            try
            {
                DataTable result = await action().ConfigureAwait(false);
                stopwatch.Stop();
                CompleteRepositoryOperation(activity, operation, isTransaction, statementCount, result?.Rows.Count ?? 0, stopwatch.Elapsed.TotalMilliseconds, true, null);
                return result;
            }
            catch (Exception e)
            {
                stopwatch.Stop();
                CompleteRepositoryOperation(activity, operation, isTransaction, statementCount, 0, stopwatch.Elapsed.TotalMilliseconds, false, e);
                throw;
            }
        }

        private Activity StartRepositoryOperationActivity(string operation, bool isTransaction, int statementCount)
        {
            Activity activity = LiteGraphTelemetry.ActivitySource.StartActivity(LiteGraphTelemetry.RepositoryOperationActivityName, ActivityKind.Client);
            if (activity == null) return null;

            activity.SetTag("db.system", "sqlite");
            activity.SetTag("litegraph.repository.provider", ProviderName);
            activity.SetTag("litegraph.repository.operation", operation);
            activity.SetTag("litegraph.repository.transactional", isTransaction || _Transaction != null);
            activity.SetTag("litegraph.repository.statement_count", statementCount < 0 ? 0 : statementCount);
            return activity;
        }

        private void CompleteRepositoryOperation(
            Activity activity,
            string operation,
            bool isTransaction,
            int statementCount,
            int rowCount,
            double durationMs,
            bool success,
            Exception exception)
        {
            if (activity != null)
            {
                activity.SetTag("litegraph.repository.success", success);
                activity.SetTag("litegraph.repository.rows", rowCount);
                activity.SetTag("litegraph.repository.duration_ms", durationMs);

                if (success) LiteGraphTelemetry.SetActivityOk(activity);
                else LiteGraphTelemetry.SetActivityException(activity, exception);
            }

            LiteGraphTelemetry.RecordRepositoryOperation(new RepositoryOperationTelemetryEventArgs(
                ProviderName,
                operation,
                success,
                isTransaction || _Transaction != null,
                statementCount,
                rowCount,
                durationMs));
        }

        private static string ClassifySqlOperation(string query, bool isTransaction)
        {
            if (isTransaction) return "transaction";
            if (String.IsNullOrWhiteSpace(query)) return "unknown";

            string trimmed = query.TrimStart();
            int length = 0;
            while (length < trimmed.Length && !Char.IsWhiteSpace(trimmed[length]) && trimmed[length] != ';')
            {
                length++;
            }

            if (length < 1) return "unknown";

            string verb = trimmed.Substring(0, length).Trim().ToUpperInvariant();
            switch (verb)
            {
                case "SELECT":
                case "PRAGMA":
                case "WITH":
                    return "read";
                case "INSERT":
                case "UPDATE":
                case "DELETE":
                case "CREATE":
                case "DROP":
                case "ALTER":
                case "REPLACE":
                case "BEGIN":
                case "COMMIT":
                case "END":
                    return "write";
                default:
                    return verb.ToLowerInvariant();
            }
        }

        private void ApplyPerformanceSettings(SqliteConnection conn)
        {
            using (SqliteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText =
                    "PRAGMA journal_mode = WAL; " +
                    "PRAGMA synchronous = NORMAL; " +
                    "PRAGMA cache_size = -128000; " +
                    "PRAGMA temp_store = MEMORY; " +
                    "PRAGMA mmap_size = 536870912; ";
                cmd.ExecuteNonQuery();
            }
        }

        private bool IsTransientOpenFailure(Exception e)
        {
            return e is SqliteException sqliteException && sqliteException.SqliteErrorCode == 14;
        }

        private void LogPersistentConnectionFallback(string operation, Exception e)
        {
            Logging.Log(SeverityEnum.Warn, "sqlite " + operation + " falling back to the persistent WAL connection after a transient open failure: " + e.Message);
        }

        private void EnsurePersistentConnectionOpen()
        {
            if (_SqliteConnection != null && _SqliteConnection.State == ConnectionState.Open) return;

            try { _SqliteConnection?.Close(); } catch { }
            try { _SqliteConnection?.Dispose(); } catch { }

            _SqliteConnection = new SqliteConnection(_ConnectionString);
            _SqliteConnection.Open();
            ApplyPerformanceSettings(_SqliteConnection);
        }

        private DataTable ExecuteQueryWithPersistentConnection(string query, bool isTransaction)
        {
            DataTable result = new DataTable();

            lock (_QueryLock)
            {
                EnsurePersistentConnectionOpen();

                try
                {
                    using (SqliteCommand cmd = new SqliteCommand(query, _SqliteConnection))
                    {
                        using (SqliteDataReader rdr = cmd.ExecuteReader())
                        {
                            result.Load(rdr);
                        }
                    }
                }
                catch (Exception e)
                {
                    if (isTransaction)
                    {
                        try
                        {
                            using (SqliteCommand cmd = new SqliteCommand("ROLLBACK;", _SqliteConnection))
                                cmd.ExecuteNonQuery();
                        }
                        catch { }
                    }

                    e.Data.Add("IsTransaction", isTransaction);
                    e.Data.Add("Query", query);
                    throw;
                }
            }

            return result;
        }

        private DataTable ExecuteQueriesWithPersistentConnection(IEnumerable<string> queries, bool isTransaction)
        {
            DataTable result = new DataTable();

            lock (_QueryLock)
            {
                EnsurePersistentConnectionOpen();
                SqliteTransaction transaction = null;

                try
                {
                    if (isTransaction)
                    {
                        transaction = _SqliteConnection.BeginTransaction();
                    }

                    DataTable lastResult = null;

                    foreach (string query in queries.Where(q => !string.IsNullOrEmpty(q)))
                    {
                        if (query.Length > MaxStatementLength)
                            throw new ArgumentException($"Query exceeds maximum statement length of {MaxStatementLength} characters.");

                        if (Logging.LogQueries) Logging.Log(SeverityEnum.Debug, "query: " + query);

                        using (SqliteCommand cmd = new SqliteCommand(query, _SqliteConnection))
                        {
                            if (transaction != null)
                            {
                                cmd.Transaction = transaction;
                            }

                            using (SqliteDataReader rdr = cmd.ExecuteReader())
                            {
                                lastResult = new DataTable();
                                lastResult.Load(rdr);
                            }

                            if (lastResult != null && lastResult.Rows.Count > 0)
                            {
                                result = lastResult;
                            }
                        }
                    }

                    transaction?.Commit();
                }
                catch (Exception e)
                {
                    try { transaction?.Rollback(); } catch { }

                    e.Data.Add("IsTransaction", isTransaction);
                    e.Data.Add("Queries", string.Join("; ", queries));
                    throw;
                }
                finally
                {
                    transaction?.Dispose();
                }
            }

            return result;
        }

        private void ClearGraphTransaction()
        {
            SqliteTransaction transaction = _Transaction;
            SqliteConnection conn = _TransactionConnection;

            _Transaction = null;
            _TransactionConnection = null;
            _GraphTransactionTenantGUID = null;
            _GraphTransactionGraphGUID = null;
            _GraphTransactionVectorIndexTouched = false;
            _GraphTransactionVectorIndexFailed = false;
            _GraphTransactionVectorIndexDirtyReason = null;

            try { transaction?.Dispose(); } catch { }

            if (!_InMemory && conn != null && !ReferenceEquals(conn, _SqliteConnection))
            {
                try { conn.Close(); } catch { }
                try { conn.Dispose(); } catch { }
            }
        }

        private void MarkVectorIndexDirtyAfterTransaction(Guid tenantGuid, Guid graphGuid, string reason)
        {
            try
            {
                ExecuteQuery(GraphQueries.SetVectorIndexDirty(
                    tenantGuid,
                    graphGuid,
                    true,
                    reason ?? "Graph transaction completed with uncertain vector index state"), true);
            }
            catch (Exception e)
            {
                Logging.Log(SeverityEnum.Warn, "failed to mark vector index dirty after graph transaction: " + e.Message);
            }
        }

        private void EnsureCredentialScopeColumns()
        {
            DataTable tableInfo = ExecuteQuery("PRAGMA table_info('creds');");
            if (!ColumnExists(tableInfo, "scopes"))
            {
                ExecuteQuery("ALTER TABLE 'creds' ADD COLUMN scopes TEXT;");
            }

            if (!ColumnExists(tableInfo, "graphguids"))
            {
                ExecuteQuery("ALTER TABLE 'creds' ADD COLUMN graphguids TEXT;");
            }
        }

        private void EnsureGraphVectorIndexConsistencyColumns()
        {
            DataTable tableInfo = ExecuteQuery("SELECT name FROM pragma_table_info('graphs');");
            if (!ColumnExists(tableInfo, "vectorindexdirty"))
            {
                ExecuteQuery("ALTER TABLE 'graphs' ADD COLUMN vectorindexdirty INT NOT NULL DEFAULT 0;");
            }

            if (!ColumnExists(tableInfo, "vectorindexdirtyutc"))
            {
                ExecuteQuery("ALTER TABLE 'graphs' ADD COLUMN vectorindexdirtyutc VARCHAR(64);");
            }

            if (!ColumnExists(tableInfo, "vectorindexdirtyreason"))
            {
                ExecuteQuery("ALTER TABLE 'graphs' ADD COLUMN vectorindexdirtyreason TEXT;");
            }

            ExecuteQuery("CREATE INDEX IF NOT EXISTS 'idx_graphs_vectorindexdirty' ON 'graphs' (vectorindexdirty ASC);");
        }

        private void EnsureRequestHistoryCorrelationColumns()
        {
            DataTable tableInfo = ExecuteQuery("SELECT name FROM pragma_table_info('requesthistory');");
            if (!ColumnExists(tableInfo, "requestid"))
            {
                ExecuteQuery("ALTER TABLE 'requesthistory' ADD COLUMN requestid VARCHAR(128);");
            }

            if (!ColumnExists(tableInfo, "correlationid"))
            {
                ExecuteQuery("ALTER TABLE 'requesthistory' ADD COLUMN correlationid VARCHAR(128);");
            }

            if (!ColumnExists(tableInfo, "traceid"))
            {
                ExecuteQuery("ALTER TABLE 'requesthistory' ADD COLUMN traceid VARCHAR(128);");
            }

            ExecuteQuery("CREATE INDEX IF NOT EXISTS 'idx_requesthistory_requestid' ON 'requesthistory' (requestid ASC);");
            ExecuteQuery("CREATE INDEX IF NOT EXISTS 'idx_requesthistory_correlationid_createdutc' ON 'requesthistory' (correlationid ASC, createdutc DESC);");
            ExecuteQuery("CREATE INDEX IF NOT EXISTS 'idx_requesthistory_traceid_createdutc' ON 'requesthistory' (traceid ASC, createdutc DESC);");
        }

        private void EnsureBuiltInAuthorizationRoles()
        {
            bool changed = false;

            foreach (RoleDefinition definition in AuthorizationPolicyDefinitions.BuiltInRoles)
            {
                AuthorizationRole role = AuthorizationRole.FromDefinition(definition);
                DataTable existing = ExecuteQuery(AuthorizationRoleQueries.SelectRoleByName(null, role.Name));

                if (existing != null && existing.Rows.Count > 0)
                {
                    DataRow row = existing.Rows[0];
                    string guid = Converters.GetDataRowStringValue(row, "guid");
                    if (!String.IsNullOrEmpty(guid) && Guid.TryParse(guid, out Guid parsedGuid))
                        role.GUID = parsedGuid;

                    string created = Converters.GetDataRowStringValue(row, "createdutc");
                    if (!String.IsNullOrEmpty(created) && DateTime.TryParse(created, out DateTime parsedCreated))
                        role.CreatedUtc = DateTime.SpecifyKind(parsedCreated, DateTimeKind.Utc);

                    ExecuteQuery(AuthorizationRoleQueries.UpdateRole(role), true);
                    changed = true;
                }
                else
                {
                    ExecuteQuery(AuthorizationRoleQueries.InsertRole(role), true);
                    changed = true;
                }
            }

            if (changed) AuthorizationPolicyChangeTracker.SignalChanged();
        }

        private static bool ColumnExists(DataTable tableInfo, string columnName)
        {
            if (tableInfo == null || tableInfo.Rows == null) return false;

            foreach (DataRow row in tableInfo.Rows)
            {
                if (row.Table.Columns.Contains("name")
                    && row["name"] != null
                    && row["name"] != DBNull.Value
                    && String.Equals(row["name"].ToString(), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
    }
}
