namespace LiteGraph.Indexing.Vector
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for vector index implementations.
    /// </summary>
    public interface IVectorIndex : IDisposable
    {
        /// <summary>
        /// Initialize the vector index.
        /// </summary>
        /// <param name="graph">Graph containing index configuration.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task InitializeAsync(Graph graph, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add a single vector to the index.
        /// </summary>
        /// <param name="vectorId">Vector identifier.</param>
        /// <param name="vector">Vector data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task AddAsync(Guid vectorId, List<float> vector, CancellationToken cancellationToken = default);

        /// <summary>
        /// Add multiple vectors to the index in batch.
        /// </summary>
        /// <param name="vectors">Dictionary of vector IDs to vector data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task AddBatchAsync(Dictionary<Guid, List<float>> vectors, CancellationToken cancellationToken = default);

        /// <summary>
        /// Update an existing vector in the index.
        /// </summary>
        /// <param name="vectorId">Vector identifier.</param>
        /// <param name="vector">New vector data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task UpdateAsync(Guid vectorId, List<float> vector, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove a vector from the index.
        /// </summary>
        /// <param name="vectorId">Vector identifier.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task RemoveAsync(Guid vectorId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove multiple vectors from the index.
        /// </summary>
        /// <param name="vectorIds">List of vector identifiers.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task RemoveBatchAsync(List<Guid> vectorIds, CancellationToken cancellationToken = default);

        /// <summary>
        /// Search for the k nearest neighbors to a query vector.
        /// </summary>
        /// <param name="queryVector">Query vector.</param>
        /// <param name="k">Number of neighbors to retrieve.</param>
        /// <param name="ef">Dynamic candidate list size for search. Higher values improve recall at the cost of speed.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>List of vector IDs and their distances.</returns>
        Task<List<(Guid Id, float Distance)>> SearchAsync(
            List<float> queryVector, 
            int k, 
            int? ef = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Save the index to persistent storage.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task SaveAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Load the index from persistent storage.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task LoadAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Get statistics about the index.
        /// </summary>
        /// <returns>Index statistics.</returns>
        VectorIndexStatistics GetStatistics();

        /// <summary>
        /// Clear all vectors from the index.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task ClearAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Check if a vector exists in the index.
        /// </summary>
        /// <param name="vectorId">Vector identifier.</param>
        /// <returns>True if the vector exists.</returns>
        bool Contains(Guid vectorId);
    }
}