namespace LiteGraph.Client.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;

    /// <summary>
    /// Interface for credential methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public interface ICredentialMethods
    {
        /// <summary>
        /// Create a credential.
        /// </summary>
        /// <param name="credential">Credential.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Credential.</returns>
        Task<Credential> Create(Credential credential, CancellationToken token = default);

        /// <summary>
        /// Read all credentials in a given tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Async enumerable of credentials.</returns>
        IAsyncEnumerable<Credential> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Read credentials.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="userGuid">User GUID.</param>
        /// <param name="bearerToken">Bearer token.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Async enumerable of credentials.</returns>
        IAsyncEnumerable<Credential> ReadMany(
            Guid? tenantGuid,
            Guid? userGuid,
            string bearerToken,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Read a credential by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Credential.</returns>
        Task<Credential> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Read credentials by GUIDs.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guids">GUIDs.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Async enumerable of credentials.</returns>
        IAsyncEnumerable<Credential> ReadByGuids(Guid tenantGuid, List<Guid> guids, CancellationToken token = default);

        /// <summary>
        /// Read a credential by bearer token.
        /// </summary>
        /// <param name="bearerToken">Bearer token.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Credential.</returns>
        Task<Credential> ReadByBearerToken(string bearerToken, CancellationToken token = default);

        /// <summary>
        /// Enumerate objects.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result.</returns>
        Task<EnumerationResult<Credential>> Enumerate(EnumerationRequest query = null, CancellationToken token = default);

        /// <summary>
        /// Update a credential.
        /// </summary>
        /// <param name="cred">Credential.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Credential.</returns>
        Task<Credential> Update(Credential cred, CancellationToken token = default);

        /// <summary>
        /// Delete credentials associated with a tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default);

        /// <summary>
        /// Delete a credential.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Delete credentials associated with a user.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="userGuid">User GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByUser(Guid tenantGuid, Guid userGuid, CancellationToken token = default);

        /// <summary>
        /// Check if a credential exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);
    }
}
