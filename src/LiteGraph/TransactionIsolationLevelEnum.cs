namespace LiteGraph
{
    /// <summary>
    /// Provider-neutral transaction isolation level.
    /// </summary>
    public enum TransactionIsolationLevelEnum
    {
        /// <summary>
        /// Use the provider default isolation level.
        /// </summary>
        Default,

        /// <summary>
        /// Read committed isolation where supported by the provider.
        /// </summary>
        ReadCommitted,

        /// <summary>
        /// Repeatable read isolation where supported by the provider.
        /// </summary>
        RepeatableRead,

        /// <summary>
        /// Serializable isolation where supported by the provider.
        /// </summary>
        Serializable
    }
}
