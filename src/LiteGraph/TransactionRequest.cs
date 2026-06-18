namespace LiteGraph
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Graph-scoped transaction request.
    /// </summary>
    public class TransactionRequest
    {
        #region Public-Members

        /// <summary>
        /// Operations to execute atomically.
        /// </summary>
        public List<TransactionOperation> Operations
        {
            get
            {
                return _Operations;
            }
            set
            {
                if (value == null) value = new List<TransactionOperation>();
                _Operations = value;
            }
        }

        /// <summary>
        /// Maximum number of operations permitted by this request.
        /// </summary>
        public int MaxOperations
        {
            get
            {
                return _MaxOperations;
            }
            set
            {
                if (value < 1 || value > 10000) throw new ArgumentOutOfRangeException(nameof(MaxOperations));
                _MaxOperations = value;
            }
        }

        /// <summary>
        /// Request timeout in seconds.
        /// </summary>
        public int TimeoutSeconds
        {
            get
            {
                return _TimeoutSeconds;
            }
            set
            {
                if (value < 1 || value > 3600) throw new ArgumentOutOfRangeException(nameof(TimeoutSeconds));
                _TimeoutSeconds = value;
            }
        }

        /// <summary>
        /// Requested provider transaction isolation level.
        /// </summary>
        public TransactionIsolationLevelEnum IsolationLevel { get; set; } = TransactionIsolationLevelEnum.Default;

        #endregion

        #region Private-Members

        private List<TransactionOperation> _Operations = new List<TransactionOperation>();
        private int _MaxOperations = 1000;
        private int _TimeoutSeconds = 60;

        #endregion
    }
}
