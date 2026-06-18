namespace LiteGraph.Server.Classes
{
    using System;

    /// <summary>
    /// Server-side graph transaction limits.
    /// </summary>
    public class TransactionSettings
    {
        #region Public-Members

        /// <summary>
        /// Maximum operations allowed in one graph transaction request.
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
        /// Maximum per-transaction timeout, in seconds.
        /// </summary>
        public int MaxTimeoutSeconds
        {
            get
            {
                return _MaxTimeoutSeconds;
            }
            set
            {
                if (value < 1 || value > 3600) throw new ArgumentOutOfRangeException(nameof(MaxTimeoutSeconds));
                _MaxTimeoutSeconds = value;
            }
        }

        #endregion

        #region Private-Members

        private int _MaxOperations = 1000;
        private int _MaxTimeoutSeconds = 60;

        #endregion
    }
}
