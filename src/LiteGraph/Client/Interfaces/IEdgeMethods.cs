﻿namespace LiteGraph.Client.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using ExpressionTree;
    using LiteGraph;

    /// <summary>
    /// Interface for edge methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public interface IEdgeMethods
    {
        /// <summary>
        /// Create an edge between two nodes.
        /// </summary>
        /// <param name="edge">Edge.</param>
        /// <returns>Edge.</returns>
        Edge Create(Edge edge);

        /// <summary>
        /// Create multiple edges.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="edges">Edges.</param>
        /// <returns>Edges.</returns>
        List<Edge> CreateMany(Guid tenantGuid, Guid graphGuid, List<Edge> edges);

        /// <summary>
        /// Read all edges in a given tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <returns>Edges.</returns>
        IEnumerable<Edge> ReadAllInTenant(
            Guid tenantGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false);

        /// <summary>
        /// Read all edges in a given graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <returns>Edges.</returns>
        IEnumerable<Edge> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false);

        /// <summary>
        /// Read edges.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="name">Name on which to search.</param>
        /// <param name="labels">Labels.</param>
        /// <param name="tags">Tags.</param>
        /// <param name="edgeFilter">
        /// Edge filter expression for Data JSON body.
        /// Expression left terms must follow the form of Sqlite JSON paths.
        /// For example, to retrieve the 'Name' property, use '$.Name', OperatorEnum.Equals, '[name here]'.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <returns>Edges.</returns>
        IEnumerable<Edge> ReadMany(
            Guid tenantGuid,
            Guid graphGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false);

        /// <summary>
        /// Read first.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="name">Name on which to search.</param>
        /// <param name="labels">Labels.</param>
        /// <param name="tags">Tags.</param>
        /// <param name="edgeFilter">
        /// Edge filter expression for Data JSON body.
        /// Expression left terms must follow the form of Sqlite JSON paths.
        /// For example, to retrieve the 'Name' property, use '$.Name', OperatorEnum.Equals, '[name here]'.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <returns>Edge.</returns>
        Edge ReadFirst(
            Guid tenantGuid,
            Guid graphGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            bool includeData = false,
            bool includeSubordinates = false);

        /// <summary>
        /// Read edge.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <returns>Edge.</returns>
        Edge ReadByGuid(
            Guid tenantGuid, 
            Guid graphGuid, 
            Guid edgeGuid,
            bool includeData = false,
            bool includeSubordinates = false);

        /// <summary>
        /// Read edges by GUIDs.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guids">GUIDs.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <returns>Edges.</returns>
        IEnumerable<Edge> ReadByGuids(
            Guid tenantGuid, 
            List<Guid> guids,
            bool includeData = false,
            bool includeSubordinates = false);

        /// <summary>
        /// Get edges connected to or initiated from a given node.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="labels">Labels.</param>
        /// <param name="tags">Tags upon which to filter edges.</param>
        /// <param name="edgeFilter">
        /// Edge filter expression for Data JSON body.
        /// Expression left terms must follow the form of Sqlite JSON paths.
        /// For example, to retrieve the 'Name' property, use '$.Name', OperatorEnum.Equals, '[name here]'.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <returns>Edges.</returns>
        IEnumerable<Edge> ReadNodeEdges(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false);

        /// <summary>
        /// Get edges from a given node.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="labels">Labels.</param>
        /// <param name="tags">Tags upon which to filter edges.</param>
        /// <param name="edgeFilter">
        /// Edge filter expression for Data JSON body.
        /// Expression left terms must follow the form of Sqlite JSON paths.
        /// For example, to retrieve the 'Name' property, use '$.Name', OperatorEnum.Equals, '[name here]'.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <returns>Edges.</returns>
        IEnumerable<Edge> ReadEdgesFromNode(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false);

        /// <summary>
        /// Get edges to a given node.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="labels">Labels.</param>
        /// <param name="tags">Tags upon which to filter edges.</param>
        /// <param name="edgeFilter">
        /// Edge filter expression for Data JSON body.
        /// Expression left terms must follow the form of Sqlite JSON paths.
        /// For example, to retrieve the 'Name' property, use '$.Name', OperatorEnum.Equals, '[name here]'.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <returns>Edges.</returns>
        IEnumerable<Edge> ReadEdgesToNode(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false);

        /// <summary>
        /// Get edges between two neighboring nodes.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="fromNodeGuid">From node GUID.</param>
        /// <param name="toNodeGuid">To node GUID.</param>
        /// <param name="labels">Labels.</param>
        /// <param name="tags">Tags upon which to filter edges.</param>
        /// <param name="edgeFilter">
        /// Edge filter expression for Data JSON body.
        /// Expression left terms must follow the form of Sqlite JSON paths.
        /// For example, to retrieve the 'Name' property, use '$.Name', OperatorEnum.Equals, '[name here]'.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="includeData">Boolean indicating whether the object's data property should be included.</param>
        /// <param name="includeSubordinates">Boolean indicating whether the object's subordinate properties (labels, tags, vectors) should be included.</param>
        /// <returns>Edges.</returns>
        IEnumerable<Edge> ReadEdgesBetweenNodes(
            Guid tenantGuid,
            Guid graphGuid,
            Guid fromNodeGuid,
            Guid toNodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false);

        /// <summary>
        /// Enumerate objects.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <returns>Enumeration result.</returns>
        EnumerationResult<Edge> Enumerate(EnumerationRequest query = null);

        /// <summary>
        /// Update edge.
        /// </summary>
        /// <param name="edge">Edge.</param>
        /// <returns>Edge.</returns>
        Edge Update(Edge edge);

        /// <summary>
        /// Delete edge.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        void DeleteByGuid(Guid tenantGuid, Guid graphGuid, Guid edgeGuid);

        /// <summary>
        /// Delete all edges from a tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        void DeleteAllInTenant(Guid tenantGuid);

        /// <summary>
        /// Delete all edges from a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        void DeleteAllInGraph(Guid tenantGuid, Guid graphGuid);

        /// <summary>
        /// Delete multiple edges from a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="edgeGuids">Edge GUIDs.</param>
        void DeleteMany(Guid tenantGuid, Guid graphGuid, List<Guid> edgeGuids);

        /// <summary>
        /// Delete all edges associated with a given node.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        void DeleteNodeEdges(Guid tenantGuid, Guid graphGuid, Guid nodeGuid);

        /// <summary>
        /// Delete all edges associated with a set of given nodes.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuids">Node GUIDs.</param>
        void DeleteNodeEdges(Guid tenantGuid, Guid graphGuid, List<Guid> nodeGuids);

        /// <summary>
        /// Check if an edge exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <returns>True if exists.</returns>
        bool ExistsByGuid(Guid tenantGuid, Guid graphGuid, Guid edgeGuid);
    }
}
