namespace LiteGraph.GraphRepositories.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;

    /// <summary>
    /// Interface for chat thread methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public interface IChatThreadMethods
    {
        /// <summary>
        /// Create a chat thread.
        /// </summary>
        /// <param name="thread">Chat thread.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat thread.</returns>
        Task<ChatThread> Create(ChatThread thread, CancellationToken token = default);

        /// <summary>
        /// Read all chat threads in tenant, optionally filtered by owning user.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="userGuid">User GUID filter.  Null returns all users' threads.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Async enumerable of chat threads.</returns>
        IAsyncEnumerable<ChatThread> ReadAllInTenant(
            Guid tenantGuid,
            Guid? userGuid = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Read a chat thread by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat thread, or null if not found.</returns>
        Task<ChatThread> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Enumerate chat threads.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing a page of objects.</returns>
        Task<EnumerationResult<ChatThread>> Enumerate(EnumerationRequest query, CancellationToken token = default);

        /// <summary>
        /// Get the record count.  Optionally supply a marker object GUID to indicate that only records from that marker record should be counted.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="userGuid">User GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="markerGuid">Marker GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Number of records.</returns>
        Task<int> GetRecordCount(
            Guid? tenantGuid,
            Guid? userGuid = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Guid? markerGuid = null,
            CancellationToken token = default);

        /// <summary>
        /// Update a chat thread.
        /// </summary>
        /// <param name="thread">Chat thread.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat thread.</returns>
        Task<ChatThread> Update(ChatThread thread, CancellationToken token = default);

        /// <summary>
        /// Delete a chat thread.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Delete all chat threads in a tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default);

        /// <summary>
        /// Check if a chat thread exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);
    }
}
