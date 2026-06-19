namespace LiteGraph
{
    using System;

    /// <summary>
    /// Options used to create a graph transaction execution context.
    /// </summary>
    public class TransactionExecutionOptions
    {
        #region Public-Members

        /// <summary>
        /// Transaction identifier assigned by LiteGraph.
        /// </summary>
        public Guid TransactionId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid TenantGUID { get; set; } = Guid.Empty;

        /// <summary>
        /// Graph GUID.
        /// </summary>
        public Guid GraphGUID { get; set; } = Guid.Empty;

        /// <summary>
        /// Requested provider isolation level.
        /// </summary>
        public TransactionIsolationLevelEnum IsolationLevel { get; set; } = TransactionIsolationLevelEnum.Default;

        /// <summary>
        /// Number of operations in the transaction request.
        /// </summary>
        public int OperationCount { get; set; } = 0;

        /// <summary>
        /// Caller or subsystem creating the transaction context.
        /// </summary>
        public string Source { get; set; } = "transaction";

        #endregion

        #region Public-Methods

        /// <summary>
        /// Validate options.
        /// </summary>
        public void Validate()
        {
            if (TransactionId == Guid.Empty) throw new ArgumentException("TransactionId must not be empty.");
            if (TenantGUID == Guid.Empty) throw new ArgumentException("TenantGUID must not be empty.");
            if (GraphGUID == Guid.Empty) throw new ArgumentException("GraphGUID must not be empty.");
            if (OperationCount < 0) throw new ArgumentOutOfRangeException(nameof(OperationCount));
        }

        #endregion
    }
}
