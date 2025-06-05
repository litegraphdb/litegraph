namespace LiteGraph.Server.Classes
{
    using System;
    using System.Collections.Generic;

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
        /// Sqlite data repository filename.
        /// </summary>
        public string GraphRepositoryFilename
        {
            get
            {
                return _GraphRepositoryFilename;
            }
            set
            {
                if (String.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(GraphRepositoryFilename));
                _GraphRepositoryFilename = value;
            }
        }

        /// <summary>
        /// Maximum number of concurrent operations.
        /// For higher concurrency, use a lower number (e.g. 1).
        /// For lower concurrency, use a higher number (e.g. 10).
        /// This value dictates the maximum number of operations that may be operating in parallel at any one given time.
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
        public bool InMemory { get; set; } = false;

        #endregion

        #region Private-Members

        private string _AdminBearerToken = "litegraphadmin";
        private string _GraphRepositoryFilename = "litegraph.db";
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
