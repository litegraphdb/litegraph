namespace LiteGraph.Sdk
{
    /// <summary>
    /// Provider transaction isolation level.
    /// </summary>
    public enum TransactionIsolationLevelEnum
    {
        /// <summary>
        /// Use the provider default.
        /// </summary>
        Default,

        /// <summary>
        /// Read committed isolation.
        /// </summary>
        ReadCommitted,

        /// <summary>
        /// Repeatable read isolation.
        /// </summary>
        RepeatableRead,

        /// <summary>
        /// Serializable isolation.
        /// </summary>
        Serializable
    }
}
