namespace LiteGraph.Client.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;

    /// <summary>
    /// Interface for chat settings methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public interface IChatSettingsMethods
    {
        /// <summary>
        /// Read the chat settings for a tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat settings, or null when the tenant has none.</returns>
        Task<ChatSettings> ReadByTenant(Guid tenantGuid, CancellationToken token = default);

        /// <summary>
        /// Create or replace the chat settings for a tenant.
        /// </summary>
        /// <param name="settings">Chat settings.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat settings.</returns>
        Task<ChatSettings> Upsert(ChatSettings settings, CancellationToken token = default);

        /// <summary>
        /// Delete the chat settings for a tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByTenant(Guid tenantGuid, CancellationToken token = default);

        /// <summary>
        /// Check if chat settings exist for a tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> ExistsByTenant(Guid tenantGuid, CancellationToken token = default);
    }
}
