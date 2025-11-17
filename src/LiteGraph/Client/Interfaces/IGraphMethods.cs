namespace LiteGraph.Client.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using ExpressionTree;
    using LiteGraph;
    using LiteGraph.Indexing.Vector;

    /// <summary>
    /// Interface for graph methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
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
        /// Read all graphs in a given tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Graphs.</returns>
        IAsyncEnumerable<Graph> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default);

        /// <summary>
        /// Read graphs.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="name">Name on which to search.</param>
        /// <param name="labels">Labels.</param>
        /// <param name="tags">Tags on which to match.</param>
        /// <param name="graphFilter">
        /// Graph filter expression for Data JSON body.
        /// Expression left terms must follow the form of Sqlite JSON paths.
        /// For example, to retrieve the 'Name' property, use '$.Name', OperatorEnum.Equals, '[name here]'.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Graphs.</returns>
        IAsyncEnumerable<Graph> ReadMany(
            Guid tenantGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr graphFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default);

        /// <summary>
        /// Read first.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="name">Name on which to search.</param>
        /// <param name="labels">Labels.</param>
        /// <param name="tags">Tags on which to match.</param>
        /// <param name="graphFilter">
        /// Graph filter expression for Data JSON body.
        /// Expression left terms must follow the form of Sqlite JSON paths.
        /// For example, to retrieve the 'Name' property, use '$.Name', OperatorEnum.Equals, '[name here]'.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Graph.</returns>
        Task<Graph> ReadFirst(
            Guid tenantGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr graphFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            bool includeData = false,
            bool includeSubordinates = false,
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
        Task<Graph> ReadByGuid(
            Guid tenantGuid,
            Guid guid,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default);

        /// <summary>
        /// Read graphs by GUIDs.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guids">GUIDs.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Graphs.</returns>
        IAsyncEnumerable<Graph> ReadByGuids(
            Guid tenantGuid,
            List<Guid> guids,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default);

        /// <summary>
        /// Enumerate objects.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result.</returns>
        Task<EnumerationResult<Graph>> Enumerate(EnumerationRequest query = null, CancellationToken token = default);

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
        /// <param name="force">True to force deletion of nodes and edges.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByGuid(Guid tenantGuid, Guid guid, bool force = false, CancellationToken token = default);

        /// <summary>
        /// Delete graphs associated with a tenant.  Deletion is forceful.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default);

        /// <summary>
        /// Check if a graph exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

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
        /// <param name="token">Cancellation token.</param>
        /// <returns>Dictionary of graph statistics.</returns>
        Task<Dictionary<Guid, GraphStatistics>> GetStatistics(Guid tenantGuid, CancellationToken token = default);

        /// <summary>
        /// Enable vector indexing for a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="configuration">Vector index configuration.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task EnableVectorIndexing(
            Guid tenantGuid,
            Guid graphGuid,
            VectorIndexConfiguration configuration,
            CancellationToken token = default);

        /// <summary>
        /// Disable vector indexing for a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="deleteIndexFile">Whether to delete the index file.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DisableVectorIndexing(
            Guid tenantGuid,
            Guid graphGuid,
            bool deleteIndexFile = false,
            CancellationToken token = default);

        /// <summary>
        /// Rebuild the vector index for a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task RebuildVectorIndex(
            Guid tenantGuid,
            Guid graphGuid,
            CancellationToken token = default);

        /// <summary>
        /// Get vector index statistics for a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Vector index statistics or null if no index exists.</returns>
        Task<VectorIndexStatistics> GetVectorIndexStatistics(
            Guid tenantGuid,
            Guid graphGuid,
            CancellationToken token = default);

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
    }
}
