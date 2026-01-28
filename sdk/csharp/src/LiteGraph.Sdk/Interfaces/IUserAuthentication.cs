namespace LiteGraph.Sdk.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Sdk;

    /// <summary>
    /// Interface for user authentication methods.
    /// </summary>
    public interface IUserAuthentication
    {
        /// <summary>
        /// Get tenants for the email address associated with this SDK instance.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>List of tenants.</returns>
        Task<List<TenantMetadata>> GetTenantsForEmail(CancellationToken token = default);

        /// <summary>
        /// Get details for the current bearer token.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Authentication token details.</returns>
        Task<AuthenticationToken> GenerateToken(CancellationToken token = default);

        /// <summary>
        /// Get token details.
        /// </summary>
        /// <param name="authToken">Authentication token string.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Authentication token details.</returns>
        Task<AuthenticationToken> GetTokenDetails(string authToken, CancellationToken token = default);
    }
}

