namespace LiteGraph.GraphRepositories.Interfaces
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
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
        /// <returns>Graph.</returns>
        Graph Create(Graph graph);

        /// <summary>
        /// Read all graphs in a given tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <returns>Graphs.</returns>
        IEnumerable<Graph> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0);

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
        /// <returns>Graphs.</returns>
        IEnumerable<Graph> ReadMany(
            Guid tenantGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr graphFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0);

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
        /// <returns>Graph.</returns>
        Graph ReadFirst(
            Guid tenantGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr graphFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending);

        /// <summary>
        /// Read a graph by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <returns>Graph.</returns>
        Graph ReadByGuid(Guid tenantGuid, Guid guid);

        /// <summary>
        /// Read graph by GUIDs.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guids">GUIDs.</param>
        /// <returns>Graphs.</returns>
        IEnumerable<Graph> ReadByGuids(Guid tenantGuid, List<Guid> guids);

        /// <summary>
        /// Enumerate objects.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <returns>Enumeration result containing a page of objects.</returns>
        EnumerationResult<Graph> Enumerate(EnumerationRequest query);

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
        /// <returns>Number of records.</returns>
        int GetRecordCount(
            Guid? tenantGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Guid? markerGuid = null);

        /// <summary>
        /// Update a graph.
        /// </summary>
        /// <param name="graph">Graph.</param>
        /// <returns>Graph.</returns>
        Graph Update(Graph graph);

        /// <summary>
        /// Delete a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        void DeleteByGuid(Guid tenantGuid, Guid guid);

        /// <summary>
        /// Delete graphs associated with a tenant.  Deletion is forceful.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        void DeleteAllInTenant(Guid tenantGuid);

        /// <summary>
        /// Check if a graph exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <returns>True if exists.</returns>
        bool ExistsByGuid(Guid tenantGuid, Guid guid);

        /// <summary>
        /// Retrieve graph statistics.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">Graph GUID.</param>
        /// <returns>Graph statistics.</returns>
        GraphStatistics GetStatistics(Guid tenantGuid, Guid guid);

        /// <summary>
        /// Retrieve graph statistics.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <returns>Dictionary of graph statistics.</returns>
        Dictionary<Guid, GraphStatistics> GetStatistics(Guid tenantGuid);

        /// <summary>
        /// Enable vector indexing for a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="configuration">Vector index configuration.</param>
        /// <returns>Task.</returns>
        Task EnableVectorIndexingAsync(
            Guid tenantGuid,
            Guid graphGuid,
            VectorIndexConfiguration configuration);

        /// <summary>
        /// Disable vector indexing for a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="deleteIndexFile">Whether to delete the index file.</param>
        /// <returns>Task.</returns>
        Task DisableVectorIndexingAsync(
            Guid tenantGuid,
            Guid graphGuid,
            bool deleteIndexFile = false);

        /// <summary>
        /// Rebuild the vector index for a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <returns>Task.</returns>
        Task RebuildVectorIndexAsync(
            Guid tenantGuid,
            Guid graphGuid);

        /// <summary>
        /// Get vector index statistics for a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <returns>Vector index statistics or null if no index exists.</returns>
        VectorIndexStatistics GetVectorIndexStatistics(
            Guid tenantGuid,
            Guid graphGuid);

        /// <summary>
        /// Retrieve a subgraph starting from a specific node, traversing up to a specified depth.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Starting node GUID.</param>
        /// <param name="maxDepth">Maximum depth to traverse (0 = only the starting node, 1 = immediate neighbors, etc.).</param>
        /// <param name="maxNodes">Maximum number of nodes to retrieve (0 = unlimited).</param>
        /// <param name="maxEdges">Maximum number of edges to retrieve (0 = unlimited).</param>
        /// <returns>Search result containing nodes and edges in the subgraph.</returns>
        SearchResult GetSubgraph(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            int maxDepth = 2,
            int maxNodes = 0,
            int maxEdges = 0);

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
        /// <returns>GraphStatistics with node/edge counts and label/tag/vector counts.</returns>
        GraphStatistics GetSubgraphStatistics(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            int maxDepth = 2,
            int maxNodes = 0,
            int maxEdges = 0);
    }
}
