namespace LiteGraph
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories;

    /// <summary>
    /// Lifecycle controller for one graph transaction execution.
    /// </summary>
    public interface ITransactionContext : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Transaction execution options.
        /// </summary>
        TransactionExecutionOptions Options { get; }

        /// <summary>
        /// Repository used by operations executed inside this transaction.
        /// </summary>
        GraphRepositoryBase Repository { get; }

        /// <summary>
        /// Current transaction lifecycle state.
        /// </summary>
        TransactionStateEnum State { get; }

        /// <summary>
        /// Storage provider name.
        /// </summary>
        string Provider { get; }

        /// <summary>
        /// True if execution uses a transaction-local repository/session.
        /// </summary>
        bool IsolatedRepository { get; }

        /// <summary>
        /// True if execution waited behind the serialized fallback gate.
        /// </summary>
        bool SerializedByGate { get; }

        /// <summary>
        /// Time spent waiting to enter the serialized fallback gate.
        /// </summary>
        double QueueWaitDurationMs { get; }

        /// <summary>
        /// Time spent committing.
        /// </summary>
        double CommitDurationMs { get; }

        /// <summary>
        /// Time spent rolling back.
        /// </summary>
        double RollbackDurationMs { get; }

        /// <summary>
        /// Begin the transaction.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task BeginAsync(CancellationToken token = default);

        /// <summary>
        /// Commit the transaction.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task CommitAsync(CancellationToken token = default);

        /// <summary>
        /// Roll back the transaction.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task RollbackAsync(CancellationToken token = default);
    }
}
