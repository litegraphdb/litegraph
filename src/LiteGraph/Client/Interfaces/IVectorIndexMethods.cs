namespace LiteGraph.Client.Interfaces
{
    using System;
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
        /// <returns>Vector index configuration.</returns>
        VectorIndexConfiguration GetConfiguration(Guid tenantGuid, Guid graphGuid);

        /// <summary>
        /// Get the vector index statistics for a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <returns>Vector index statistics.</returns>
        VectorIndexStatistics GetStatistics(Guid tenantGuid, Guid graphGuid);

        /// <summary>
        /// Enable vector indexing on a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="configuration">Vector index configuration.</param>
        /// <returns>Task.</returns>
        Task EnableVectorIndexAsync(Guid tenantGuid, Guid graphGuid, VectorIndexConfiguration configuration);

        /// <summary>
        /// Rebuild the vector index for a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <returns>Task.</returns>
        Task RebuildVectorIndexAsync(Guid tenantGuid, Guid graphGuid);

        /// <summary>
        /// Delete (disable) the vector index for a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="deleteIndexFile">Whether to delete the persistent index file.</param>
        /// <returns>Task.</returns>
        Task DeleteVectorIndexAsync(Guid tenantGuid, Guid graphGuid, bool deleteIndexFile = false);
    }
}
