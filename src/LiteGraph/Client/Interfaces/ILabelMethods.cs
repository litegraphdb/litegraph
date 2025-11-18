namespace LiteGraph.Client.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;

    /// <summary>
    /// Interface for label methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public interface ILabelMethods
    {
        /// <summary>
        /// Create a label.
        /// </summary>
        /// <param name="label">Label.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Label.</returns>
        Task<LabelMetadata> Create(LabelMetadata label, CancellationToken token = default);

        /// <summary>
        /// Create multiple labels.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="labels">Labels.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Labels.</returns>
        Task<List<LabelMetadata>> CreateMany(Guid tenantGuid, List<LabelMetadata> labels, CancellationToken token = default);

        /// <summary>
        /// Read all labels in a given tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Labels.</returns>
        IAsyncEnumerable<LabelMetadata> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Read all labels in a given graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Labels.</returns>
        IAsyncEnumerable<LabelMetadata> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Read labels.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <param name="label">Label.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Labels.</returns>
        IAsyncEnumerable<LabelMetadata> ReadMany(
            Guid tenantGuid,
            Guid? graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
            string label,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Read graph labels.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Labels.</returns>
        IAsyncEnumerable<LabelMetadata> ReadManyGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Read node labels.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Labels.</returns>
        IAsyncEnumerable<LabelMetadata> ReadManyNode(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Read edge labels.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Labels.</returns>
        IAsyncEnumerable<LabelMetadata> ReadManyEdge(
            Guid tenantGuid,
            Guid graphGuid,
            Guid edgeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Read a label by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Label.</returns>
        Task<LabelMetadata> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Read labels by GUIDs.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guids">GUIDs.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Labels.</returns>
        IAsyncEnumerable<LabelMetadata> ReadByGuids(Guid tenantGuid, List<Guid> guids, CancellationToken token = default);

        /// <summary>
        /// Enumerate objects.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result.</returns>
        Task<EnumerationResult<LabelMetadata>> Enumerate(EnumerationRequest query = null, CancellationToken token = default);

        /// <summary>
        /// Update a label.
        /// </summary>
        /// <param name="label">Label.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Label.</returns>
        Task<LabelMetadata> Update(LabelMetadata label, CancellationToken token = default);

        /// <summary>
        /// Delete a label.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Delete labels.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuids">Node GUIDs.</param>
        /// <param name="edgeGuids">Edge GUIDs.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteMany(Guid tenantGuid, Guid? graphGuid, List<Guid> nodeGuids, List<Guid> edgeGuids, CancellationToken token = default);

        /// <summary>
        /// Delete labels.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guids">GUIDs.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteMany(Guid tenantGuid, List<Guid> guids, CancellationToken token = default);

        /// <summary>
        /// Delete all labels associated with a tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default);

        /// <summary>
        /// Delete all labels associated with a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteAllInGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default);

        /// <summary>
        /// Delete labels for the graph object itself, leaving subordinate node and edge labels in place.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteGraphLabels(Guid tenantGuid, Guid graphGuid, CancellationToken token = default);

        /// <summary>
        /// Delete node labels.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteNodeLabels(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default);

        /// <summary>
        /// Delete edge labels.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteEdgeLabels(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, CancellationToken token = default);

        /// <summary>
        /// Check if a label exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);
    }
}
