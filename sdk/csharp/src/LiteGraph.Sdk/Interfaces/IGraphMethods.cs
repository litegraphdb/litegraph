namespace LiteGraph.Sdk.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Sdk;

    /// <summary>
    /// Interface for graph methods.
    /// </summary>
    public interface IGraphMethods
    {
        /// <summary>
        /// Create a graph.
        /// </summary>
        /// <param name="graph">Graph.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Graph.</returns>
        Task<Graph> Create(Graph graph, CancellationToken token = default);

        /// <summary>
        /// Read graphs.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Graphs.</returns>
        Task<List<Graph>> ReadMany(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Read a graph by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Graph.</returns>
        Task<Graph> ReadByGuid(Guid tenantGuid, Guid guid, bool includeData = false, bool includeSubordinates = false, CancellationToken token = default);

        /// <summary>
        /// Read graphs by GUIDs.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guids">GUIDs.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List.</returns>
        Task<List<Graph>> ReadByGuids(Guid tenantGuid, List<Guid> guids, bool includeData = false, bool includeSubordinates = false, CancellationToken token = default);

        /// <summary>
        /// Update a graph.
        /// </summary>
        /// <param name="graph">Graph.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Graph.</returns>
        Task<Graph> Update(Graph graph, CancellationToken token = default);

        /// <summary>
        /// Delete a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="force">True to forcefully delete associated data.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByGuid(Guid tenantGuid, Guid guid, bool force = false, CancellationToken token = default);

        /// <summary>
        /// Check if a graph exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Search graphs.
        /// </summary>
        /// <param name="req">Search request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Search result.</returns>
        Task<SearchResult> Search(SearchRequest req, CancellationToken token = default);

        /// <summary>
        /// Read first.
        /// </summary>
        /// <param name="req">Search request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Graph.</returns>
        Task<Graph> ReadFirst(SearchRequest req, CancellationToken token = default);

        /// <summary>
        /// Enumerate.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result.</returns>
        Task<EnumerationResult<Graph>> Enumerate(EnumerationRequest query, CancellationToken token = default);

        /// <summary>
        /// Retrieve graph statistics.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">Graph GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Graph statistics.</returns>
        Task<GraphStatistics> GetStatistics(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Retrieve graph statistics.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <returns>Dictionary of graph statistics.</returns>
        /// <param name="token">Cancellation token.</param>
        Task<Dictionary<Guid, GraphStatistics>> GetStatistics(Guid tenantGuid, CancellationToken token = default);

        /// <summary>
        /// Enable vector indexing for a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="config">Vector index configuration.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task EnableVectorIndexing(Guid tenantGuid, Guid graphGuid, VectorIndexConfiguration config, CancellationToken token = default);

        /// <summary>
        /// Rebuild the vector index for a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task RebuildVectorIndex(Guid tenantGuid, Guid graphGuid, CancellationToken token = default);

        /// <summary>
        /// Delete the vector index for a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="deleteIndexFile">Whether to delete the index file.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteVectorIndex(Guid tenantGuid, Guid graphGuid, bool deleteIndexFile = false, CancellationToken token = default);

        /// <summary>
        /// Read the vector index configuration for a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Vector index configuration.</returns>
        Task<VectorIndexConfiguration> ReadVectorIndexConfig(Guid tenantGuid, Guid graphGuid, CancellationToken token = default);

        /// <summary>
        /// Get vector index statistics for a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Vector index statistics.</returns>
        Task<VectorIndexStatistics> GetVectorIndexStatistics(Guid tenantGuid, Guid graphGuid, CancellationToken token = default);

        /// <summary>
        /// Retrieve a subgraph starting from a specific node, traversing up to a specified depth.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Starting node GUID.</param>
        /// <param name="maxDepth">Maximum depth to traverse (0 = only the starting node, 1 = immediate neighbors, etc.).</param>
        /// <param name="maxNodes">Maximum number of nodes to retrieve (0 = unlimited).</param>
        /// <param name="maxEdges">Maximum number of edges to retrieve (0 = unlimited).</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Search result containing nodes and edges in the subgraph.</returns>
        Task<SearchResult> GetSubgraph(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            int maxDepth = 2,
            int maxNodes = 0,
            int maxEdges = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default);

        /// <summary>
        /// Get statistics for a subgraph starting from a specific node, traversing up to a specified depth.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Starting node GUID.</param>
        /// <param name="maxDepth">Maximum depth to traverse (0 = only the starting node, 1 = immediate neighbors, etc.).</param>
        /// <param name="maxNodes">Maximum number of nodes to retrieve (0 = unlimited).</param>
        /// <param name="maxEdges">Maximum number of edges to retrieve (0 = unlimited).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Graph statistics containing node and edge counts for the subgraph.</returns>
        Task<GraphStatistics> GetSubgraphStatistics(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            int maxDepth = 2,
            int maxNodes = 0,
            int maxEdges = 0,
            CancellationToken token = default);

        /// <summary>
        /// Export graph to GEXF format.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="includeData">True to include data.</param>
        /// <param name="includeSubordinates">True to include subordinates (labels, tags, vectors).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>GEXF XML string.</returns>
        Task<string> ExportGraphToGexf(
            Guid tenantGuid,
            Guid graphGuid,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default);

        /// <summary>
        /// Read all graphs in tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Graphs.</returns>
        Task<List<Graph>> ReadAllInTenant(Guid tenantGuid, CancellationToken token = default);

        /// <summary>
        /// Delete all graphs in tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default);
    }
}