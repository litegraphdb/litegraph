namespace LiteGraph.Client.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
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
        /// <param name="token">Cancellation token.</param>
        /// <returns>User.</returns>
        Task<UserMaster> Create(UserMaster user, CancellationToken token = default);

        /// <summary>
        /// Read all users in a given tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Users.</returns>
        IAsyncEnumerable<UserMaster> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Read users.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="email">Email.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Users.</returns>
        IAsyncEnumerable<UserMaster> ReadMany(
            Guid? tenantGuid,
            string email,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Read a user by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>User.</returns>
        Task<UserMaster> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Read users by GUIDs.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guids">GUIDs.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Users.</returns>
        IAsyncEnumerable<UserMaster> ReadByGuids(Guid tenantGuid, List<Guid> guids, CancellationToken token = default);

        /// <summary>
        /// Read tenants associated with a given email address.
        /// </summary>
        /// <param name="email">Email address.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of tenants.</returns>
        Task<List<TenantMetadata>> ReadTenantsByEmail(string email, CancellationToken token = default);

        /// <summary>
        /// Read a user by email.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="email">Email.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>User.</returns>
        Task<UserMaster> ReadByEmail(Guid tenantGuid, string email, CancellationToken token = default);

        /// <summary>
        /// Enumerate objects.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result.</returns>
        Task<EnumerationResult<UserMaster>> Enumerate(EnumerationRequest query = null, CancellationToken token = default);

        /// <summary>
        /// Update a user.
        /// </summary>
        /// <param name="user">User.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>User.</returns>
        Task<UserMaster> Update(UserMaster user, CancellationToken token = default);

        /// <summary>
        /// Delete a user.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Delete users associated with a tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default);

        /// <summary>
        /// Check if a user exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Check if a user exists by email.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="email">Email.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> ExistsByEmail(Guid tenantGuid, string email, CancellationToken token = default);

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
