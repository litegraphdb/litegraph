namespace LiteGraph.GraphRepositories.Interfaces
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using ExpressionTree;
    using LiteGraph;
    using LiteGraph.Indexing.Vector;
    using LiteGraph.Serialization;
    using Microsoft.Data.Sqlite;

    /// <summary>
    /// Interface for graph methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
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
        /// <param name="token">Cancellation token.</param>
        /// <returns>Graphs.</returns>
        IAsyncEnumerable<Graph> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
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
        /// <param name="token">Cancellation token.</param>
        /// <returns>Graph.</returns>
        Task<Graph> ReadFirst(
            Guid tenantGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr graphFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            CancellationToken token = default);

        /// <summary>
        /// Read a graph by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Graph.</returns>
        Task<Graph> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Read graph by GUIDs.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guids">GUIDs.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Graphs.</returns>
        IAsyncEnumerable<Graph> ReadByGuids(Guid tenantGuid, List<Guid> guids, CancellationToken token = default);

        /// <summary>
        /// Enumerate objects.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing a page of objects.</returns>
        Task<EnumerationResult<Graph>> Enumerate(EnumerationRequest query, CancellationToken token = default);

        /// <summary>
        /// Get the record count.  Optionally supply a marker object GUID to indicate that only records from that marker record should be counted.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="labels">Labels.</param>
        /// <param name="tags">Tags upon which to filter.</param>
        /// <param name="filter">
        /// Edge filter expression for Data JSON body.
        /// Expression left terms must follow the form of Sqlite JSON paths.
        /// For example, to retrieve the 'Name' property, use '$.Name', OperatorEnum.Equals, '[name here]'.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="markerGuid">Marker GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Number of records.</returns>
        Task<int> GetRecordCount(
            Guid? tenantGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Guid? markerGuid = null,
            CancellationToken token = default);

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
        /// <param name="token">Cancellation token.</param>
        Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

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
        Task EnableVectorIndexingAsync(
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
        Task DisableVectorIndexingAsync(
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
        Task RebuildVectorIndexAsync(
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
        /// <param name="token">Cancellation token.</param>
        /// <returns>Search result containing nodes and edges in the subgraph.</returns>
        Task<SearchResult> GetSubgraph(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            int maxDepth = 2,
            int maxNodes = 0,
            int maxEdges = 0,
            CancellationToken token = default);

        /// <summary>
        /// Get statistics for a subgraph starting from a specific node, traversing up to a specified depth.
        /// This method performs lightweight traversal and returns complete statistics.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Starting node GUID.</param>
        /// <param name="maxDepth">Maximum depth to traverse (0 = only the starting node, 1 = immediate neighbors, etc.).</param>
        /// <param name="maxNodes">Maximum number of nodes to retrieve (0 = unlimited).</param>
        /// <param name="maxEdges">Maximum number of edges to retrieve (0 = unlimited).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>GraphStatistics with node/edge counts and label/tag/vector counts.</returns>
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
