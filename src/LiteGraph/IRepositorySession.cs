namespace LiteGraph
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories;

    /// <summary>
    /// Repository state owned by one transaction execution.
    /// </summary>
    public interface IRepositorySession : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Repository instance used by operations executed inside the session.
        /// </summary>
        GraphRepositoryBase Repository { get; }

        /// <summary>
        /// Storage provider name.
        /// </summary>
        string Provider { get; }

        /// <summary>
        /// True if the session owns a repository instance distinct from the caller repository.
        /// </summary>
        bool IsIsolated { get; }

        /// <summary>
        /// True if callers must serialize access before using this session.
        /// </summary>
        bool RequiresSerializedExecution { get; }

        /// <summary>
        /// True if a provider transaction is active inside this session.
        /// </summary>
        bool Active { get; }

        /// <summary>
        /// Begin the provider transaction.
        /// </summary>
        /// <param name="options">Transaction execution options.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task BeginTransactionAsync(TransactionExecutionOptions options, CancellationToken token = default);

        /// <summary>
        /// Commit the provider transaction.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task CommitTransactionAsync(CancellationToken token = default);

        /// <summary>
        /// Roll back the provider transaction.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task RollbackTransactionAsync(CancellationToken token = default);
    }
}
