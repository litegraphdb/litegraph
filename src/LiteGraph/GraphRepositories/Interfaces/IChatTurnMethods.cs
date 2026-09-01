namespace LiteGraph.GraphRepositories.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;

    /// <summary>
    /// Interface for chat turn methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public interface IChatTurnMethods
    {
        /// <summary>
        /// Create a chat turn.
        /// </summary>
        /// <param name="turn">Chat turn.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat turn.</returns>
        Task<ChatTurn> Create(ChatTurn turn, CancellationToken token = default);

        /// <summary>
        /// Read a chat turn by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat turn, or null if not found.</returns>
        Task<ChatTurn> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Read the turns of a thread in sequence order.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="threadGuid">Thread GUID.</param>
        /// <param name="ascending">True to read oldest-first; false to read newest-first.  Default is true.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Async enumerable of chat turns.</returns>
        IAsyncEnumerable<ChatTurn> ReadByThread(
            Guid tenantGuid,
            Guid threadGuid,
            bool ascending = true,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Read all chat turns in tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Async enumerable of chat turns.</returns>
        IAsyncEnumerable<ChatTurn> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Get the number of turns in a thread.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="threadGuid">Thread GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Number of turns.</returns>
        Task<int> GetCountByThread(Guid tenantGuid, Guid threadGuid, CancellationToken token = default);

        /// <summary>
        /// Get the highest sequence number in a thread.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="threadGuid">Thread GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Highest sequence number, or -1 when the thread has no turns.</returns>
        Task<int> GetMaxSequence(Guid tenantGuid, Guid threadGuid, CancellationToken token = default);

        /// <summary>
        /// Delete a chat turn.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Delete all turns in a thread.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="threadGuid">Thread GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByThread(Guid tenantGuid, Guid threadGuid, CancellationToken token = default);

        /// <summary>
        /// Delete all chat turns in a tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default);

        /// <summary>
        /// Delete turns created before a cutoff, for retention pruning.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="olderThanUtc">Cutoff timestamp, in UTC.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteOlderThan(Guid tenantGuid, DateTime olderThanUtc, CancellationToken token = default);

        /// <summary>
        /// Check if a chat turn exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);
    }
}
