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
    /// Interface for user methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public interface IUserMethods
    {
        /// <summary>
        /// Create a user.
        /// </summary>
        /// <param name="user">User.</param>
        /// <returns>User.</returns>
        UserMaster Create(UserMaster user);

        /// <summary>
        /// Read all users in a given tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <returns>Users.</returns>
        IEnumerable<UserMaster> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0);

        /// <summary>
        /// Read users.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="email">Email.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <returns>Users.</returns>
        IEnumerable<UserMaster> ReadMany(
            Guid? tenantGuid,
            string email,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0);

        /// <summary>
        /// Read a user by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <returns>User.</returns>
        UserMaster ReadByGuid(Guid tenantGuid, Guid guid);

        /// <summary>
        /// Read tenants associated with a given email address.
        /// </summary>
        /// <param name="email">Email address.</param>
        /// <returns>List of tenants.</returns>
        List<TenantMetadata> ReadTenantsByEmail(string email);

        /// <summary>
        /// Read a user by email.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="email">Email.</param>
        /// <returns>User.</returns>
        UserMaster ReadByEmail(Guid tenantGuid, string email);

        /// <summary>
        /// Update a user.
        /// </summary>
        /// <param name="user">User.</param>
        /// <returns>User.</returns>
        UserMaster Update(UserMaster user);

        /// <summary>
        /// Delete a user.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        void DeleteByGuid(Guid tenantGuid, Guid guid);

        /// <summary>
        /// Delete users associated with a tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        void DeleteAllInTenant(Guid tenantGuid);

        /// <summary>
        /// Check if a user exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <returns>True if exists.</returns>
        bool ExistsByGuid(Guid tenantGuid, Guid guid);

        /// <summary>
        /// Check if a user exists by email.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="email">Email.</param>
        /// <returns>True if exists.</returns>
        bool ExistsByEmail(Guid tenantGuid, string email);
    }
}
