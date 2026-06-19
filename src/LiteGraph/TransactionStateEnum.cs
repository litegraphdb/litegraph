namespace LiteGraph
{
    /// <summary>
    /// Graph transaction lifecycle state.
    /// </summary>
    public enum TransactionStateEnum
    {
        /// <summary>
        /// Transaction result has been created but provider execution has not started.
        /// </summary>
        Created,

        /// <summary>
        /// Provider transaction is active.
        /// </summary>
        Active,

        /// <summary>
        /// Provider transaction is committing.
        /// </summary>
        Committing,

        /// <summary>
        /// Provider transaction committed.
        /// </summary>
        Committed,

        /// <summary>
        /// Provider transaction is rolling back.
        /// </summary>
        RollingBack,

        /// <summary>
        /// Provider transaction rolled back.
        /// </summary>
        RolledBack,

        /// <summary>
        /// Transaction failed before a committed or rolled-back final state was established.
        /// </summary>
        Faulted,

        /// <summary>
        /// Transaction-owned resources have been disposed.
        /// </summary>
        Disposed
    }
}
