namespace LiteGraph.GraphRepositories.Sqlite
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel.DataAnnotations;
    using System.Data;
    using System.Linq;
    using System.Reflection.Emit;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using System.Xml.Schema;
    using Caching;
    using ExpressionTree;
    using LiteGraph;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Implementations;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Helpers;
    using LiteGraph.Serialization;
    using Microsoft.Data.Sqlite;
    using PrettyId;

    using LoggingSettings = LiteGraph.LoggingSettings;

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
        /// Default for Sqlite is 1000000 (see https://www.sqlite.org/limits.html).
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

        #endregion

        #region Internal-Members

        internal string ConnectionString = "Data Source=litegraph.db;Pooling=false";
        internal readonly object QueryLock = new object();

        #endregion

        #region Private-Members

        private string _Filename = "litegraph.db";
        private int _SelectBatchSize = 100;
        private int _MaxStatementLength = 1000000; // https://www.sqlite.org/limits.html
        private string _TimestampFormat = "yyyy-MM-dd HH:mm:ss.ffffff";

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="filename">Sqlite database filename.</param>
        public SqliteGraphRepository(string filename = "litegraph.db")
        {
            if (string.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));

            _Filename = filename;

            ConnectionString = "Data Source=" + filename + ";Pooling=false";

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
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public override void InitializeRepository()
        {
            ExecuteQuery(SetupQueries.CreateTablesAndIndices());
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

            lock (QueryLock)
            {
                using (SqliteConnection conn = new SqliteConnection(ConnectionString))
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

            if (Logging.LogResults) Logging.Log(SeverityEnum.Debug, "result: " + query + ": " + (result != null ? result.Rows.Count + " rows" : "(null)"));
            return result;
        }

        internal DataTable ExecuteQueries(IEnumerable<string> queries, bool isTransaction = false)
        {
            if (queries == null || !queries.Any()) throw new ArgumentNullException(nameof(queries));

            DataTable result = new DataTable();

            lock (QueryLock)
            {
                using (SqliteConnection conn = new SqliteConnection(ConnectionString))
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

            if (Logging.LogResults) Logging.Log(SeverityEnum.Debug, "result: " + (result != null ? result.Rows.Count + " rows" : "(null)"));
            return result;
        }
        #endregion

        #region Private-Methods

        #endregion

#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities
    }
}
