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
    /// Interface for tag methods.
    /// </summary>
    public interface ITagMethods
    {
        /// <summary>
        /// Create a tag.
        /// </summary>
        /// <param name="tag">Tag.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Tag.</returns>
        Task<TagMetadata> Create(TagMetadata tag, CancellationToken token = default);

        /// <summary>
        /// Create multiple tags.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="tags">Tags.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Tags.</returns>
        Task<List<TagMetadata>> CreateMany(Guid tenantGuid, List<TagMetadata> tags, CancellationToken token = default);

        /// <summary>
        /// Read tags.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Tags.</returns>
        Task<List<TagMetadata>> ReadMany(
            Guid tenantGuid,
            Guid? graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Read a tag by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Tag.</returns>
        Task<TagMetadata> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Read tags by GUIDs.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guids">GUIDs.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List.</returns>
        Task<List<TagMetadata>> ReadByGuids(Guid tenantGuid, List<Guid> guids, CancellationToken token = default);

        /// <summary>
        /// Update a tag.
        /// </summary>
        /// <param name="tag">Tag.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Tag.</returns>
        Task<TagMetadata> Update(TagMetadata tag, CancellationToken token = default);

        /// <summary>
        /// Delete a tag.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Delete tags.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guids">GUIDs.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteMany(Guid tenantGuid, List<Guid> guids, CancellationToken token = default);

        /// <summary>
        /// Check if a tag exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Enumerate.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result.</returns>
        Task<EnumerationResult<TagMetadata>> Enumerate(EnumerationRequest query, CancellationToken token = default);

        /// <summary>
        /// Read all tags across tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Tags.</returns>
        Task<List<TagMetadata>> ReadAllInTenant(Guid tenantGuid, CancellationToken token = default);

        /// <summary>
        /// Read all tags in specific graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Tags.</returns>
        Task<List<TagMetadata>> ReadAllInGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default);

        /// <summary>
        /// Read tags attached to graph object.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Tags.</returns>
        Task<List<TagMetadata>> ReadManyGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default);

        /// <summary>
        /// Read tags attached to specific node.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Tags.</returns>
        Task<List<TagMetadata>> ReadManyNode(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default);

        /// <summary>
        /// Read tags attached to specific edge.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Tags.</returns>
        Task<List<TagMetadata>> ReadManyEdge(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, CancellationToken token = default);

        /// <summary>
        /// Delete all tags in tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default);

        /// <summary>
        /// Delete all tags in graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteAllInGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default);

        /// <summary>
        /// Delete graph-specific tags.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteGraphTags(Guid tenantGuid, Guid graphGuid, CancellationToken token = default);

        /// <summary>
        /// Delete node-specific tags.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteNodeTags(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default);

        /// <summary>
        /// Delete edge-specific tags.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteEdgeTags(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, CancellationToken token = default);
    }
}