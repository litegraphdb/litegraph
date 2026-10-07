namespace LiteGraph.Server.Classes
{
    using System;

    /// <summary>
    /// Transaction diagnostics recorded with a request history entry for a graph transaction request.
    /// Thread safety: not thread-safe; create one per entry.
    /// </summary>
    internal sealed class RequestHistoryTransactionDiagnostics
    {
        #region Public-Members

        /// <summary>
        /// Transaction ID.
        /// </summary>
        public Guid TransactionId { get; set; } = Guid.Empty;

        /// <summary>
        /// Final transaction state.
        /// </summary>
        public string State { get; set; } = null;

        /// <summary>
        /// True when the transaction committed.
        /// </summary>
        public bool Success { get; set; } = false;

        /// <summary>
        /// True when the transaction was rolled back.
        /// </summary>
        public bool RolledBack { get; set; } = false;

        /// <summary>
        /// True when an operation failed validation.
        /// </summary>
        public bool ValidationFailure { get; set; } = false;

        /// <summary>
        /// Index of the failed operation, or null.
        /// </summary>
        public int? FailedOperationIndex { get; set; } = null;

        /// <summary>
        /// Number of operations in the transaction.
        /// </summary>
        public int OperationCount { get; set; } = 0;

        /// <summary>
        /// Isolation level used.
        /// </summary>
        public string IsolationLevel { get; set; } = null;

        /// <summary>
        /// Storage provider.
        /// </summary>
        public string Provider { get; set; } = null;

        /// <summary>
        /// True when the transaction ran against an isolated repository.
        /// </summary>
        public bool IsolatedRepository { get; set; } = false;

        /// <summary>
        /// True when the transaction was serialized by the transaction gate.
        /// </summary>
        public bool SerializedByGate { get; set; } = false;

        /// <summary>
        /// Number of retries.
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// True when the failure can be retried.
        /// </summary>
        public bool Retryable { get; set; } = false;

        /// <summary>
        /// True when the failure was a concurrency conflict.
        /// </summary>
        public bool ConcurrencyConflict { get; set; } = false;

        /// <summary>
        /// Provider error code, or null.
        /// </summary>
        public string ProviderErrorCode { get; set; } = null;

        /// <summary>
        /// Time spent waiting for the transaction gate, in milliseconds.
        /// </summary>
        public double QueueWaitDurationMs { get; set; } = 0;

        /// <summary>
        /// Commit duration in milliseconds.
        /// </summary>
        public double CommitDurationMs { get; set; } = 0;

        /// <summary>
        /// Rollback duration in milliseconds.
        /// </summary>
        public double RollbackDurationMs { get; set; } = 0;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public RequestHistoryTransactionDiagnostics()
        {
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
