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

    /// <summary>
    /// Interface for vector methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public interface IVectorMethods
    {
        /// <summary>
        /// Create a vector.
        /// </summary>
        /// <param name="vector">Vector.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Vector.</returns>
        Task<VectorMetadata> Create(VectorMetadata vector, CancellationToken token = default);

        /// <summary>
        /// Create multiple vectors.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="vectors">Vectors.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Vectors.</returns>
        Task<List<VectorMetadata>> CreateMany(
            Guid tenantGuid,
            List<VectorMetadata> vectors,
            CancellationToken token = default);

        /// <summary>
        /// Read all vectors in a given tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Vectors.</returns>
        IAsyncEnumerable<VectorMetadata> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Read all vectors in a given graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Vectors.</returns>
        IAsyncEnumerable<VectorMetadata> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Read vectors.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Vectors.</returns>
        IAsyncEnumerable<VectorMetadata> ReadMany(
            Guid tenantGuid,
            Guid? graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Read graph vectors.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Vectors.</returns>
        IAsyncEnumerable<VectorMetadata> ReadManyGraph(
            Guid tenantGuid, 
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Read node vectors.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Vectors.</returns>
        IAsyncEnumerable<VectorMetadata> ReadManyNode(
            Guid tenantGuid, 
            Guid graphGuid, 
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Read edge vectors.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Vectors.</returns>
        IAsyncEnumerable<VectorMetadata> ReadManyEdge(
            Guid tenantGuid, 
            Guid graphGuid, 
            Guid edgeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Read a vector by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Vector.</returns>
        Task<VectorMetadata> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Read vectors by GUIDs.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guids">GUIDs.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Vectors.</returns>
        IAsyncEnumerable<VectorMetadata> ReadByGuids(Guid tenantGuid, List<Guid> guids, CancellationToken token = default);

        /// <summary>
        /// Enumerate objects.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result.</returns>
        Task<EnumerationResult<VectorMetadata>> Enumerate(EnumerationRequest query = null, CancellationToken token = default);

        /// <summary>
        /// Update a vector.
        /// </summary>
        /// <param name="vector">Vector.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Vector.</returns>
        Task<VectorMetadata> Update(VectorMetadata vector, CancellationToken token = default);

        /// <summary>
        /// Delete a vector.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Delete vectors.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuids">Node GUIDs.</param>
        /// <param name="edgeGuids">Edge GUIDs.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteMany(Guid tenantGuid, Guid? graphGuid, List<Guid> nodeGuids, List<Guid> edgeGuids, CancellationToken token = default);

        /// <summary>
        /// Delete vectors.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guids">GUIDs.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteMany(Guid tenantGuid, List<Guid> guids, CancellationToken token = default);

        /// <summary>
        /// Delete all vectors associated with a tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default);

        /// <summary>
        /// Delete all vectors associated with a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteAllInGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default);

        /// <summary>
        /// Delete vectors for the graph object itself, leaving subordinate node and edge tags in place.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteGraphVectors(Guid tenantGuid, Guid graphGuid, CancellationToken token = default);

        /// <summary>
        /// Delete vectors for a node object.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteNodeVectors(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default);

        /// <summary>
        /// Delete vectors for an edge object.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteEdgeVectors(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, CancellationToken token = default);

        /// <summary>
        /// Check if a vector exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Search vectors.
        /// </summary>
        /// <param name="req">Search request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Vector search results.</returns>
        IAsyncEnumerable<VectorSearchResult> Search(VectorSearchRequest req, CancellationToken token = default);
    }
}
