namespace LiteGraph.GraphRepositories.Interfaces
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using ExpressionTree;
    using LiteGraph;
    using LiteGraph.Serialization;
    using Microsoft.Data.Sqlite;

    /// <summary>
    /// Interface for vector methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public interface IVectorMethods
    {
        /// <summary>
        /// Create a vector.
        /// </summary>
        /// <param name="vector">Vector.</param>
        /// <returns>Vector.</returns>
        VectorMetadata Create(VectorMetadata vector);

        /// <summary>
        /// Create multiple vectors.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="vectors">Vectors.</param>
        /// <returns>Vectors.</returns>
        List<VectorMetadata> CreateMany(
            Guid tenantGuid,
            List<VectorMetadata> vectors);

        /// <summary>
        /// Read all vectors in a given tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <returns>Vectors.</returns>
        IEnumerable<VectorMetadata> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0);

        /// <summary>
        /// Read all vectors in a given graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <returns>Vectors.</returns>
        IEnumerable<VectorMetadata> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0);

        /// <summary>
        /// Read vectors.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <returns>Vectors.</returns>
        IEnumerable<VectorMetadata> ReadMany(
            Guid tenantGuid,
            Guid? graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0);

        /// <summary>
        /// Read graph vectors.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <returns>Vectors.</returns>
        IEnumerable<VectorMetadata> ReadManyGraph(
            Guid tenantGuid, 
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0);

        /// <summary>
        /// Read node vectors.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <returns>Vectors.</returns>
        IEnumerable<VectorMetadata> ReadManyNode(
            Guid tenantGuid, 
            Guid graphGuid, 
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0);

        /// <summary>
        /// Read edge vectors.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <returns>Vectors.</returns>
        IEnumerable<VectorMetadata> ReadManyEdge(
            Guid tenantGuid, 
            Guid graphGuid, 
            Guid edgeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0);

        /// <summary>
        /// Read a vector by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <returns>Vector.</returns>
        VectorMetadata ReadByGuid(Guid tenantGuid, Guid guid);

        /// <summary>
        /// Read vectors by GUIDs.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guids">GUIDs.</param>
        /// <returns>Vectors.</returns>
        IEnumerable<VectorMetadata> ReadByGuids(Guid tenantGuid, List<Guid> guids);

        /// <summary>
        /// Enumerate objects.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <returns>Enumeration result containing a page of objects.</returns>
        EnumerationResult<VectorMetadata> Enumerate(EnumerationRequest query);

        /// <summary>
        /// Get the record count.  Optionally supply a marker object GUID to indicate that only records from that marker record should be counted.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="markerGuid">Marker GUID.</param>
        /// <returns>Number of records.</returns>
        int GetRecordCount(
            Guid? tenantGuid,
            Guid? graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Guid? markerGuid = null);

        /// <summary>
        /// Update a vector.
        /// </summary>
        /// <param name="vector">Vector.</param>
        /// <returns>Vector.</returns>
        VectorMetadata Update(VectorMetadata vector);

        /// <summary>
        /// Delete a vector.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        void DeleteByGuid(Guid tenantGuid, Guid guid);

        /// <summary>
        /// Delete vectors.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuids">Node GUIDs.</param>
        /// <param name="edgeGuids">Edge GUIDs.</param>
        void DeleteMany(Guid tenantGuid, Guid? graphGuid, List<Guid> nodeGuids, List<Guid> edgeGuids);

        /// <summary>
        /// Delete vectors.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guids">GUIDs.</param>
        void DeleteMany(Guid tenantGuid, List<Guid> guids);

        /// <summary>
        /// Delete all vectors in a tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        void DeleteAllInTenant(Guid tenantGuid);

        /// <summary>
        /// Delete all vectors associated with a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        void DeleteAllInGraph(Guid tenantGuid, Guid graphGuid);

        /// <summary>
        /// Delete vectors for the graph object itself, leaving subordinate node and edge tags in place.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        void DeleteGraphVectors(Guid tenantGuid, Guid graphGuid);

        /// <summary>
        /// Delete vectors for a node object.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        void DeleteNodeVectors(Guid tenantGuid, Guid graphGuid, Guid nodeGuid);

        /// <summary>
        /// Delete vectors for an edge object.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        void DeleteEdgeVectors(Guid tenantGuid, Guid graphGuid, Guid edgeGuid);

        /// <summary>
        /// Check if a vector exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <returns>True if exists.</returns>
        bool ExistsByGuid(Guid tenantGuid, Guid guid);

        /// <summary>
        /// Search graph vectors.
        /// </summary>
        /// <param name="searchType">Vector search type.</param>
        /// <param name="vectors">Vectors.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="labels">Labels.</param>
        /// <param name="tags">Tags.</param>
        /// <param name="filter">Filter.</param>
        /// <returns>Vector search results containing graphs.</returns>
        IEnumerable<VectorSearchResult> SearchGraph(
            VectorSearchTypeEnum searchType,
            List<float> vectors,
            Guid tenantGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null);

        /// <summary>
        /// Search node vectors.
        /// </summary>
        /// <param name="searchType">Vector search type.</param>
        /// <param name="vectors">Vectors.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="labels">Labels.</param>
        /// <param name="tags">Tags.</param>
        /// <param name="filter">Filter.</param>
        /// <returns>Vector search results containing nodes.</returns>
        IEnumerable<VectorSearchResult> SearchNode(
            VectorSearchTypeEnum searchType,
            List<float> vectors,
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null);

        /// <summary>
        /// Search edge vectors.
        /// </summary>
        /// <param name="searchType">Vector search type.</param>
        /// <param name="vectors">Vectors.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="labels">Labels.</param>
        /// <param name="tags">Tags.</param>
        /// <param name="filter">Filter.</param>
        /// <returns>Vector search results containing edges.</returns>
        IEnumerable<VectorSearchResult> SearchEdge(
            VectorSearchTypeEnum searchType,
            List<float> vectors,
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null);
    }
}
