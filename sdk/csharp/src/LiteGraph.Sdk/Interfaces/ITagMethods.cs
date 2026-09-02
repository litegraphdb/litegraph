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
        /// Create multiple tags.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="tags">Tags.</param>
        /// <param name="returnMode">Bulk create response shape.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Tags.</returns>
        Task<List<TagMetadata>> CreateMany(Guid tenantGuid, List<TagMetadata> tags, BulkCreateReturnModeEnum returnMode, CancellationToken token = default);

        /// <summary>
        /// Read tags.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.  Minimum is 0.  Default is 0.</param>
        /// <param name="maxKeys">Maximum number of records to retrieve.  Minimum is 1, maximum is 1000.  Default is 1000.</param>
        /// <param name="continuationToken">Continuation token from a prior enumeration result, used to continue the enumeration.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing tags.</returns>
        Task<EnumerationResult<TagMetadata>> ReadMany(
            Guid tenantGuid,
            Guid? graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            int maxKeys = 1000,
            Guid? continuationToken = null,
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
        /// <returns>Enumeration result containing tags.</returns>
        Task<EnumerationResult<TagMetadata>> ReadByGuids(Guid tenantGuid, List<Guid> guids, CancellationToken token = default);

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
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.  Minimum is 0.  Default is 0.</param>
        /// <param name="maxKeys">Maximum number of records to retrieve.  Minimum is 1, maximum is 1000.  Default is 1000.</param>
        /// <param name="continuationToken">Continuation token from a prior enumeration result, used to continue the enumeration.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing tags.</returns>
        Task<EnumerationResult<TagMetadata>> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            int maxKeys = 1000,
            Guid? continuationToken = null,
            CancellationToken token = default);

        /// <summary>
        /// Read all tags in specific graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.  Minimum is 0.  Default is 0.</param>
        /// <param name="maxKeys">Maximum number of records to retrieve.  Minimum is 1, maximum is 1000.  Default is 1000.</param>
        /// <param name="continuationToken">Continuation token from a prior enumeration result, used to continue the enumeration.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing tags.</returns>
        Task<EnumerationResult<TagMetadata>> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            int maxKeys = 1000,
            Guid? continuationToken = null,
            CancellationToken token = default);

        /// <summary>
        /// Read tags attached to graph object.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.  Minimum is 0.  Default is 0.</param>
        /// <param name="maxKeys">Maximum number of records to retrieve.  Minimum is 1, maximum is 1000.  Default is 1000.</param>
        /// <param name="continuationToken">Continuation token from a prior enumeration result, used to continue the enumeration.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing tags.</returns>
        Task<EnumerationResult<TagMetadata>> ReadManyGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            int maxKeys = 1000,
            Guid? continuationToken = null,
            CancellationToken token = default);

        /// <summary>
        /// Read tags attached to specific node.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.  Minimum is 0.  Default is 0.</param>
        /// <param name="maxKeys">Maximum number of records to retrieve.  Minimum is 1, maximum is 1000.  Default is 1000.</param>
        /// <param name="continuationToken">Continuation token from a prior enumeration result, used to continue the enumeration.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing tags.</returns>
        Task<EnumerationResult<TagMetadata>> ReadManyNode(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            int maxKeys = 1000,
            Guid? continuationToken = null,
            CancellationToken token = default);

        /// <summary>
        /// Read tags attached to specific edge.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.  Minimum is 0.  Default is 0.</param>
        /// <param name="maxKeys">Maximum number of records to retrieve.  Minimum is 1, maximum is 1000.  Default is 1000.</param>
        /// <param name="continuationToken">Continuation token from a prior enumeration result, used to continue the enumeration.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing tags.</returns>
        Task<EnumerationResult<TagMetadata>> ReadManyEdge(
            Guid tenantGuid,
            Guid graphGuid,
            Guid edgeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            int maxKeys = 1000,
            Guid? continuationToken = null,
            CancellationToken token = default);

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
