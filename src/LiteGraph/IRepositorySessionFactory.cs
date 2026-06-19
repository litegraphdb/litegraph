namespace LiteGraph
{
    /// <summary>
    /// Factory for transaction-scoped repository sessions.
    /// </summary>
    public interface IRepositorySessionFactory
    {
        /// <summary>
        /// Create a repository session for one graph transaction execution.
        /// </summary>
        /// <param name="options">Transaction execution options.</param>
        /// <returns>Repository session.</returns>
        IRepositorySession CreateRepositorySession(TransactionExecutionOptions options);
    }
}
