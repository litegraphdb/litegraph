namespace LiteGraph.Client.Interfaces
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
    /// Interface for label methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public interface ILabelMethods
    {
        /// <summary>
        /// Create a label.
        /// </summary>
        /// <param name="label">Label.</param>
        /// <returns>Label.</returns>
        LabelMetadata Create(LabelMetadata label);

        /// <summary>
        /// Create multiple labels.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="labels">Labels.</param>
        /// <returns>Labels.</returns>
        List<LabelMetadata> CreateMany(Guid tenantGuid, List<LabelMetadata> labels);

        /// <summary>
        /// Read all labels in a given tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <returns>Labels.</returns>
        IEnumerable<LabelMetadata> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0);

        /// <summary>
        /// Read all labels in a given graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <returns>Labels.</returns>
        IEnumerable<LabelMetadata> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0);

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
        /// <returns>Labels.</returns>
        IEnumerable<LabelMetadata> ReadMany(
            Guid tenantGuid,
            Guid? graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
            string label,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0);

        /// <summary>
        /// Read graph labels.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <returns>Labels.</returns>
        IEnumerable<LabelMetadata> ReadManyGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0);

        /// <summary>
        /// Read node labels.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <returns>Labels.</returns>
        IEnumerable<LabelMetadata> ReadManyNode(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0);

        /// <summary>
        /// Read edge labels.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <returns>Labels.</returns>
        IEnumerable<LabelMetadata> ReadManyEdge(
            Guid tenantGuid,
            Guid graphGuid,
            Guid edgeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0);

        /// <summary>
        /// Read a label by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <returns>Label.</returns>
        LabelMetadata ReadByGuid(Guid tenantGuid, Guid guid);

        /// <summary>
        /// Update a label.
        /// </summary>
        /// <param name="label">Label.</param>
        /// <returns>Label.</returns>
        LabelMetadata Update(LabelMetadata label);

        /// <summary>
        /// Delete a label.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        void DeleteByGuid(Guid tenantGuid, Guid guid);

        /// <summary>
        /// Delete labels.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuids">Node GUIDs.</param>
        /// <param name="edgeGuids">Edge GUIDs.</param>
        void DeleteMany(Guid tenantGuid, Guid? graphGuid, List<Guid> nodeGuids, List<Guid> edgeGuids);

        /// <summary>
        /// Delete labels.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guids">GUIDs.</param>
        void DeleteMany(Guid tenantGuid, List<Guid> guids);

        /// <summary>
        /// Delete all labels associated with a tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        void DeleteAllInTenant(Guid tenantGuid);

        /// <summary>
        /// Delete all labels associated with a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        void DeleteAllInGraph(Guid tenantGuid, Guid graphGuid);

        /// <summary>
        /// Delete labels for the graph object itself, leaving subordinate node and edge labels in place.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        void DeleteGraphLabels(Guid tenantGuid, Guid graphGuid);

        /// <summary>
        /// Delete node labels.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        void DeleteNodeLabels(Guid tenantGuid, Guid graphGuid, Guid nodeGuid);

        /// <summary>
        /// Delete edge labels.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        void DeleteEdgeLabels(Guid tenantGuid, Guid graphGuid, Guid edgeGuid);

        /// <summary>
        /// Check if a label exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <returns>True if exists.</returns>
        bool ExistsByGuid(Guid tenantGuid, Guid guid);
    }
}
