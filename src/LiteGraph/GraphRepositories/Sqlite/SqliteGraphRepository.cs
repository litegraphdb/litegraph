namespace LiteGraph.GraphRepositories.Sqlite
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
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

        #region Public-Members

        /// <summary>
        /// Sqlite database filename.
        /// </summary>
        public string Filename
        {
            get
            {
                return Filename;
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

            if (!_InMemory) _ConnectionString = "Data Source=" + filename + ";Pooling=false";
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
            
            // Initialize vector index manager
            string indexDirectory = Path.Combine(Path.GetDirectoryName(_Filename) ?? ".", "indexes");
            VectorIndexManager = new VectorIndexManager(indexDirectory);
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public override void InitializeRepository()
        {
            if (_InMemory && File.Exists(_Filename))
            {
                using (SqliteConnection diskDatabase = new SqliteConnection($"Data Source={_Filename};Pooling=false"))
                {
                    diskDatabase.Open();
                    diskDatabase.BackupDatabase(_SqliteConnection);
                }
            }

            ExecuteQuery(SetupQueries.CreateTablesAndIndices());
        }

        /// <summary>
        /// Saves the in-memory database back to disk file
        /// </summary>
        public override void Flush()
        {
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

        #endregion

        #region Internal-Methods

        internal DataTable ExecuteQuery(string query, bool isTransaction = false)
        {
            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException(query);
            if (query.Length > MaxStatementLength) throw new ArgumentException("Query exceeds maximum statement length of " + MaxStatementLength + " characters.");

            DataTable result = new DataTable();

            if (isTransaction)
            {
                query = query.Trim();
                query = "BEGIN TRANSACTION; " + query + " END TRANSACTION;";
            }

            if (Logging.LogQueries) Logging.Log(SeverityEnum.Debug, "query: " + query);

            lock (_QueryLock)
            {
                if (_InMemory)
                {
                    // Use the instance connection for in-memory operations
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
                else
                {
                    // Original code for disk-based operations
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
                            if (isTransaction)
                            {
                                using (SqliteCommand cmd = new SqliteCommand("ROLLBACK;", conn))
                                    cmd.ExecuteNonQuery();
                            }

                            e.Data.Add("IsTransaction", isTransaction);
                            e.Data.Add("Query", query);
                            throw;
                        }
                    }
                }
            }

            if (Logging.LogResults) Logging.Log(SeverityEnum.Debug, "result: " + query + ": " + (result != null ? result.Rows.Count + " rows" : "(null)"));
            return result;
        }

        internal async Task<DataTable> ExecuteQueryAsync(string query, bool isTransaction = false, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(query)) throw new ArgumentNullException(query);
            if (query.Length > MaxStatementLength) throw new ArgumentException("Query exceeds maximum statement length of " + MaxStatementLength + " characters.");

            token.ThrowIfCancellationRequested();

            DataTable result = new DataTable();

            if (isTransaction)
            {
                query = query.Trim();
                query = "BEGIN TRANSACTION; " + query + " END TRANSACTION;";
            }

            if (Logging.LogQueries) Logging.Log(SeverityEnum.Debug, "query: " + query);

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
                // Original code for disk-based operations
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
                        if (isTransaction)
                        {
                            using (SqliteCommand cmd = new SqliteCommand("ROLLBACK;", conn))
                                await cmd.ExecuteNonQueryAsync(token).ConfigureAwait(false);
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
            if (queries == null || !queries.Any()) throw new ArgumentNullException(nameof(queries));

            DataTable result = new DataTable();

            lock (_QueryLock)
            {
                if (_InMemory)
                {
                    // Use the instance connection for in-memory operations
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

                                // We'll return the result of the last query that returns data
                                if (lastResult != null && lastResult.Rows.Count > 0)
                                {
                                    result = lastResult;
                                }
                            }
                        }

                        // Commit the transaction if we're using one
                        transaction?.Commit();
                    }
                    catch (Exception e)
                    {
                        // Roll back the transaction if an error occurs
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
                else
                {
                    // Original code for disk-based operations
                    using (SqliteConnection conn = new SqliteConnection(_ConnectionString))
                    {
                        conn.Open();
                        SqliteTransaction transaction = null;

                        try
                        {
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

                                    // We'll return the result of the last query that returns data
                                    if (lastResult != null && lastResult.Rows.Count > 0)
                                    {
                                        result = lastResult;
                                    }
                                }
                            }

                            // Commit the transaction if we're using one
                            transaction?.Commit();
                        }
                        catch (Exception e)
                        {
                            // Roll back the transaction if an error occurs
                            transaction?.Rollback();

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
            }

            if (Logging.LogResults) Logging.Log(SeverityEnum.Debug, "result: " + (result != null ? result.Rows.Count + " rows" : "(null)"));
            return result;
        }

        #endregion

        #region Private-Methods
        private void ApplyPerformanceSettings(SqliteConnection conn)
        {
            using (SqliteCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = 
                    // "PRAGMA journal_mode = WAL; " +
                    "PRAGMA synchronous = NORMAL; " +
                    "PRAGMA cache_size = -128000; " +
                    "PRAGMA temp_store = MEMORY; " +
                    "PRAGMA mmap_size = 536870912; ";
                cmd.ExecuteNonQuery();
            }
        }

        #endregion

#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
    }
}
