namespace LiteGraph.Sdk.Interfaces
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ExpressionTree;
    using LiteGraph;

    /// <summary>
    /// Interface for node methods.
    /// </summary>
    public interface INodeMethods
    {
        /// <summary>
        /// Create a node.
        /// </summary>
        /// <param name="node">Node.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Node.</returns>
        Task<Node> Create(Node node, CancellationToken token = default);

        /// <summary>
        /// Create multiple nodes.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodes">Nodes.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Nodes.</returns>
        Task<List<Node>> CreateMany(Guid tenantGuid, Guid graphGuid, List<Node> nodes, CancellationToken token = default);

        /// <summary>
        /// Read nodes.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Nodes.</returns>
        Task<List<Node>> ReadMany(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default);

        /// <summary>
        /// Read node.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Node.</returns>
        Task<Node> ReadByGuid(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, bool includeData = false, bool includeSubordinates = false, CancellationToken token = default);

        /// <summary>
        /// Read nodes by GUIDs.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="guids">Node GUIDs.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List.</returns>
        Task<List<Node>> ReadByGuids(Guid tenantGuid, Guid graphGuid, List<Guid> guids, bool includeData = false, bool includeSubordinates = false, CancellationToken token = default);

        /// <summary>
        /// Update node.
        /// </summary>
        /// <param name="node">Node.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Node.</returns>
        Task<Node> Update(Node node, CancellationToken token = default);

        /// <summary>
        /// Delete a node and all associated edges.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByGuid(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default);

        /// <summary>
        /// Delete all nodes from a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteAllInGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default);

        /// <summary>
        /// Delete multiple nodes from a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuids">Node GUIDs.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteMany(Guid tenantGuid, Guid graphGuid, List<Guid> nodeGuids, CancellationToken token = default);

        /// <summary>
        /// Check existence of a node.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> ExistsByGuid(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default);

        /// <summary>
        /// Get nodes that have edges connecting to the specified node.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Nodes.</returns>
        Task<List<Node>> ReadParents(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Get nodes to which the specified node has connecting edges connecting.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Nodes.</returns>
        Task<List<Node>> ReadChildren(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Get neighbors for a given node.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Nodes.</returns>
        Task<List<Node>> ReadNeighbors(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Get routes between two nodes.
        /// </summary>
        /// <param name="searchType">Search type.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="fromNodeGuid">From node GUID.</param>
        /// <param name="toNodeGuid">To node GUID.</param>
        /// <param name="edgeFilter">
        /// Edge filter expression for Data JSON body.
        /// Expression left terms must follow the form of Sqlite JSON paths.
        /// For example, to retrieve the 'Name' property, use '$.Name', OperatorEnum.Equals, '[name here]'.</param>
        /// <param name="nodeFilter">
        /// Node filter expression for Data JSON body.
        /// Expression left terms must follow the form of Sqlite JSON paths.
        /// For example, to retrieve the 'Name' property, use '$.Name', OperatorEnum.Equals, '[name here]'.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Route details.</returns>
        Task<RouteResponse> ReadRoutes(
            SearchTypeEnum searchType,
            Guid tenantGuid,
            Guid graphGuid,
            Guid fromNodeGuid,
            Guid toNodeGuid,
            Expr edgeFilter = null,
            Expr nodeFilter = null,
            CancellationToken token = default);

        /// <summary>
        /// Search nodes.
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
        /// <returns>Node.</returns>
        Task<Node> ReadFirst(SearchRequest req, CancellationToken token = default);

        /// <summary>
        /// Enumerate.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result.</returns>
        Task<EnumerationResult<Node>> Enumerate(EnumerationRequest query, CancellationToken token = default);

        /// <summary>
        /// Read all nodes in a tenant across all graphs.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Nodes.</returns>
        Task<List<Node>> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default);

        /// <summary>
        /// Read all nodes in a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Nodes.</returns>
        Task<List<Node>> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default);

        /// <summary>
        /// Read most connected nodes in a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Nodes.</returns>
        Task<List<Node>> ReadMostConnected(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default);

        /// <summary>
        /// Read least connected nodes in a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Nodes.</returns>
        Task<List<Node>> ReadLeastConnected(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default);

        /// <summary>
        /// Delete all nodes in a tenant across all graphs.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default);
    }
}