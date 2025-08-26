namespace LiteGraph.Client.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
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
        /// <returns>Graph.</returns>
        Graph Create(Graph graph);

        /// <summary>
        /// Read all graphs in a given tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <returns>Graphs.</returns>
        IEnumerable<Graph> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false);

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
        /// <returns>Graphs.</returns>
        IEnumerable<Graph> ReadMany(
            Guid tenantGuid,
            string name = null, 
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr graphFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false);

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
        /// <returns>Graph.</returns>
        Graph ReadFirst(
            Guid tenantGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr graphFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            bool includeData = false,
            bool includeSubordinates = false);

        /// <summary>
        /// Read a graph by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <returns>Graph.</returns>
        Graph ReadByGuid(
            Guid tenantGuid, 
            Guid guid,
            bool includeData = false,
            bool includeSubordinates = false);

        /// <summary>
        /// Read graphs by GUIDs.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guids">GUIDs.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <returns>Graphs.</returns>
        IEnumerable<Graph> ReadByGuids(
            Guid tenantGuid, 
            List<Guid> guids,
            bool includeData = false,
            bool includeSubordinates = false);

        /// <summary>
        /// Enumerate objects.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <returns>Enumeration result.</returns>
        EnumerationResult<Graph> Enumerate(EnumerationRequest query = null);

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
        /// <param name="force">True to force deletion of nodes and edges.</param>
        void DeleteByGuid(Guid tenantGuid, Guid guid, bool force = false);

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
    }
}
