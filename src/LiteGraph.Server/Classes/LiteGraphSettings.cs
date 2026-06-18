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

        #endregion

        #region Private-Members

        private string _AdminBearerToken = "litegraphadmin";
        private DatabaseSettings _Database = new DatabaseSettings();
        private TransactionSettings _Transactions = new TransactionSettings();
        private int _MaxConcurrentOperations = 4;

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
