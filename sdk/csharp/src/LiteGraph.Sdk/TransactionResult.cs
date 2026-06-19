namespace LiteGraph.Sdk
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Graph-scoped transaction result.
    /// </summary>
    public class TransactionResult
    {
        /// <summary>
        /// True if the transaction committed.
        /// </summary>
        public bool Success { get; set; } = true;

        /// <summary>
        /// Transaction identifier assigned by LiteGraph.
        /// </summary>
        public Guid TransactionId { get; set; } = Guid.Empty;

        /// <summary>
        /// Final lifecycle state reached by the transaction.
        /// </summary>
        public string State { get; set; } = TransactionStateEnum.Created.ToString();

        /// <summary>
        /// True if the transaction rolled back.
        /// </summary>
        public bool RolledBack { get; set; } = false;

        /// <summary>
        /// True if request validation failed before a provider transaction started.
        /// </summary>
        public bool ValidationFailure { get; set; } = false;

        /// <summary>
        /// Index of failed operation, if any.
        /// </summary>
        public int? FailedOperationIndex { get; set; } = null;

        /// <summary>
        /// Error message, if the transaction failed.
        /// </summary>
        public string Error { get; set; } = null;

        /// <summary>
        /// Operation results.
        /// </summary>
        public List<TransactionOperationResult> Operations { get; set; } = new List<TransactionOperationResult>();

        /// <summary>
        /// Number of operations submitted with the transaction request.
        /// </summary>
        public int OperationCount { get; set; } = 0;

        /// <summary>
        /// UTC timestamp when transaction execution started.
        /// </summary>
        public DateTime StartedUtc { get; set; } = DateTime.MinValue;

        /// <summary>
        /// UTC timestamp when transaction execution completed.
        /// </summary>
        public DateTime CompletedUtc { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Transaction duration in milliseconds.
        /// </summary>
        public double DurationMs { get; set; } = 0;

        /// <summary>
        /// Commit duration in milliseconds.
        /// </summary>
        public double CommitDurationMs { get; set; } = 0;

        /// <summary>
        /// Rollback duration in milliseconds.
        /// </summary>
        public double RollbackDurationMs { get; set; } = 0;

        /// <summary>
        /// Provider name that executed the transaction.
        /// </summary>
        public string Provider { get; set; } = null;

        /// <summary>
        /// Provider transaction isolation level used for the transaction.
        /// </summary>
        public string IsolationLevel { get; set; } = "Default";

        /// <summary>
        /// True if execution used a transaction-local repository/session instead of the caller repository instance.
        /// </summary>
        public bool IsolatedRepository { get; set; } = false;

        /// <summary>
        /// True if execution used the legacy per-repository transaction gate.
        /// </summary>
        public bool SerializedByGate { get; set; } = false;

        /// <summary>
        /// Number of transaction retries attempted.
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// True if the failure is retryable.
        /// </summary>
        public bool Retryable { get; set; } = false;

        /// <summary>
        /// True if the failure represents a concurrency conflict.
        /// </summary>
        public bool ConcurrencyConflict { get; set; } = false;

        /// <summary>
        /// Provider-specific error code, if available.
        /// </summary>
        public string ProviderErrorCode { get; set; } = null;
    }
}
