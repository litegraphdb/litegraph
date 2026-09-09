namespace LiteGraph.Server.Classes
{
    using System;
    using System.Collections.Generic;
    using LiteGraph;

    /// <summary>
    /// LiteGraph settings.
    /// </summary>
    public class LiteGraphSettings
    {
        #region Public-Members

        /// <summary>
        /// Administrator bearer token.
        /// </summary>
        public string AdminBearerToken
        {
            get
            {
                return _AdminBearerToken;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(AdminBearerToken));
                _AdminBearerToken = value;
            }
        }

        /// <summary>
        /// Sqlite data repository filename.  Preserved for compatibility with existing configuration files.
        /// </summary>
        public string GraphRepositoryFilename
        {
            get
            {
                return _Database.Filename;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(GraphRepositoryFilename));
                _Database.Filename = value;
            }
        }

        /// <summary>
        /// Provider-neutral database settings.
        /// </summary>
        public DatabaseSettings Database
        {
            get
            {
                return _Database;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Database));
                _Database = value;
            }
        }

        /// <summary>
        /// Server-side graph transaction limits.
        /// </summary>
        public TransactionSettings Transactions
        {
            get
            {
                return _Transactions;
            }
            set
            {
                if (value == null) value = new TransactionSettings();
                _Transactions = value;
            }
        }

        /// <summary>
        /// Reserved concurrency limit setting.
        /// This value is preserved for compatibility with existing configuration files.
        /// The current server request pipeline does not actively enforce it.
        /// </summary>
        public int MaxConcurrentOperations
        {
            get
            {
                return _MaxConcurrentOperations;
            }
        }

        /// <summary>
        /// Boolean indicating if the database should be in-memory.
        /// </summary>
        public bool InMemory
        {
            get
            {
                return _Database.InMemory;
            }
            set
            {
                _Database.InMemory = value;
            }
        }

        /// <summary>
        /// Maximum number of nodes a graph algorithm will load into memory.  Graphs exceeding this are rejected rather than risking memory exhaustion.  Minimum 0 (0 means unlimited), default 1000000.
        /// </summary>
        public int MaxAlgorithmNodes
        {
            get
            {
                return _MaxAlgorithmNodes;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(MaxAlgorithmNodes));
                _MaxAlgorithmNodes = value;
            }
        }

        /// <summary>
        /// Maximum number of edges a graph algorithm will load into memory.  Graphs exceeding this are rejected rather than risking memory exhaustion.  Minimum 0 (0 means unlimited), default 10000000.
        /// </summary>
        public int MaxAlgorithmEdges
        {
            get
            {
                return _MaxAlgorithmEdges;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(MaxAlgorithmEdges));
                _MaxAlgorithmEdges = value;
            }
        }

        #endregion

        #region Private-Members

        private string _AdminBearerToken = "litegraphadmin";
        private DatabaseSettings _Database = new DatabaseSettings();
        private TransactionSettings _Transactions = new TransactionSettings();
        private int _MaxConcurrentOperations = 4;
        private int _MaxAlgorithmNodes = 1000000;
        private int _MaxAlgorithmEdges = 10000000;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public LiteGraphSettings()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
