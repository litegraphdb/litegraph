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
    /// Interface for tenant methods.
    /// </summary>
    public interface ITenantMethods
    {
        /// <summary>
        /// Create a tenant.
        /// </summary>
        /// <param name="tenant">Tenant.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Tenant.</returns>
        Task<TenantMetadata> Create(TenantMetadata tenant, CancellationToken token = default);

        /// <summary>
        /// Read tenants.
        /// </summary>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Tenants.</returns>
        Task<List<TenantMetadata>> ReadMany(
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Read a tenant by GUID.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Tenant.</returns>
        Task<TenantMetadata> ReadByGuid(Guid guid, CancellationToken token = default);

        /// <summary>
        /// Read tenants by GUIDs.
        /// </summary>
        /// <param name="guids">GUIDs.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List.</returns>
        Task<List<TenantMetadata>> ReadByGuids(List<Guid> guids, CancellationToken token = default);

        /// <summary>
        /// Update a tenant.
        /// </summary>
        /// <param name="tenant">Tenant.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Tenant.</returns>
        Task<TenantMetadata> Update(TenantMetadata tenant, CancellationToken token = default);

        /// <summary>
        /// Delete a tenant.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="force">True to force deletion of users and credentials.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByGuid(Guid guid, bool force = false, CancellationToken token = default);

        /// <summary>
        /// Check if a tenant exists by GUID.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> ExistsByGuid(Guid guid, CancellationToken token = default);

        /// <summary>
        /// Enumerate.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result.</returns>
        Task<EnumerationResult<TenantMetadata>> Enumerate(EnumerationRequest query, CancellationToken token = default);

        /// <summary>
        /// Retrieve tenant statistics.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Tenant statistics.</returns>
        Task<TenantStatistics> GetStatistics(Guid tenantGuid, CancellationToken token = default);

        /// <summary>
        /// Retrieve tenant statistics.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Dictionary of tenant statistics.</returns>
        Task<Dictionary<Guid, TenantStatistics>> GetStatistics(CancellationToken token = default);
    }
}