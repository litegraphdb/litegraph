namespace LiteGraph.Client.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;

    /// <summary>
    /// Interface for user methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
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
        /// Read users by GUIDs.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guids">GUIDs.</param>
        /// <returns>Users.</returns>
        IEnumerable<UserMaster> ReadByGuids(Guid tenantGuid, List<Guid> guids);

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
        /// Enumerate objects.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <returns>Enumeration result.</returns>
        EnumerationResult<UserMaster> Enumerate(EnumerationRequest query = null);

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

        /// <summary>
        /// Create an authentication token using email, password, and tenant GUID.
        /// </summary>
        /// <param name="email">Email address for authentication.</param>
        /// <param name="password">Password for authentication.</param>
        /// <param name="tenantGuid">Tenant GUID for authentication.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Authentication token details.</returns>
        Task<AuthenticationToken> GenerateAuthenticationToken(string email, string password, Guid tenantGuid, CancellationToken token = default);

        /// <summary>
        /// Read authentication token details.
        /// </summary>
        /// <param name="authToken">Authentication token (security token).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Authentication token details.</returns>
        Task<AuthenticationToken> ReadAuthenticationToken(string authToken, CancellationToken token = default);
    }
}
