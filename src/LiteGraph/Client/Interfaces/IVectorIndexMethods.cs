namespace LiteGraph.Client.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Indexing.Vector;

    /// <summary>
    /// Interface for vector index methods in the client.
    /// Provides client-side validation and error handling for vector index operations.
    /// </summary>
    public interface IVectorIndexMethods
    {
        /// <summary>
        /// Get the vector index configuration for a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Vector index configuration.</returns>
        Task<VectorIndexConfiguration> GetConfiguration(Guid tenantGuid, Guid graphGuid, CancellationToken token = default);

        /// <summary>
        /// Get the vector index statistics for a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Vector index statistics.</returns>
        Task<VectorIndexStatistics> GetStatistics(Guid tenantGuid, Guid graphGuid, CancellationToken token = default);

        /// <summary>
        /// Enable vector indexing on a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="configuration">Vector index configuration.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task EnableVectorIndex(Guid tenantGuid, Guid graphGuid, VectorIndexConfiguration configuration, CancellationToken token = default);

        /// <summary>
        /// Rebuild the vector index for a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task RebuildVectorIndex(Guid tenantGuid, Guid graphGuid, CancellationToken token = default);

        /// <summary>
        /// Delete (disable) the vector index for a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="deleteIndexFile">Whether to delete the persistent index file.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteVectorIndex(Guid tenantGuid, Guid graphGuid, bool deleteIndexFile = false, CancellationToken token = default);
    }
}
