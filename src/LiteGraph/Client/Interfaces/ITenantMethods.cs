namespace LiteGraph.Client.Interfaces
{
    using System;
    using System.Collections.Generic;
    using LiteGraph;

    /// <summary>
    /// Interface for tenant methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public interface ITenantMethods
    {
        /// <summary>
        /// Create a tenant.
        /// </summary>
        /// <param name="tenant">Tenant.</param>
        /// <returns>Tenant.</returns>
        TenantMetadata Create(TenantMetadata tenant);

        /// <summary>
        /// Read tenants.
        /// </summary>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <returns>Tenants.</returns>
        IEnumerable<TenantMetadata> ReadMany(
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0);

        /// <summary>
        /// Read a tenant by GUID.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <returns>Tenant.</returns>
        TenantMetadata ReadByGuid(Guid guid);

        /// <summary>
        /// Enumerate objects.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <returns>Enumeration result.</returns>
        EnumerationResult<TenantMetadata> Enumerate(EnumerationQuery query = null);

        /// <summary>
        /// Update a tenant.
        /// </summary>
        /// <param name="tenant">Tenant.</param>
        /// <returns>Tenant.</returns>
        TenantMetadata Update(TenantMetadata tenant);

        /// <summary>
        /// Delete a tenant.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="force">True to force deletion of users and credentials.</param>
        void DeleteByGuid(Guid guid, bool force = false);

        /// <summary>
        /// Check if a tenant exists by GUID.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <returns>True if exists.</returns>
        bool ExistsByGuid(Guid guid);

        /// <summary>
        /// Retrieve tenant statistics.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <returns>Tenant statistics.</returns>
        TenantStatistics GetStatistics(Guid tenantGuid);

        /// <summary>
        /// Retrieve tenant statistics.
        /// </summary>
        /// <returns>Dictionary of tenant statistics.</returns>
        Dictionary<Guid, TenantStatistics> GetStatistics();
    }
}
