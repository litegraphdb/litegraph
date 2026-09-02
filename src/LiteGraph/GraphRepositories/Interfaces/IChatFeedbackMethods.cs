namespace LiteGraph.GraphRepositories.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;

    /// <summary>
    /// Interface for chat feedback methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public interface IChatFeedbackMethods
    {
        /// <summary>
        /// Create a chat feedback record.
        /// </summary>
        /// <param name="feedback">Chat feedback.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat feedback.</returns>
        Task<ChatFeedback> Create(ChatFeedback feedback, CancellationToken token = default);

        /// <summary>
        /// Read a chat feedback record by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat feedback, or null if not found.</returns>
        Task<ChatFeedback> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Read all chat feedback in tenant, optionally filtered by rating or thread.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="rating">Rating filter.  Null returns all ratings.</param>
        /// <param name="threadGuid">Thread GUID filter.  Null returns all threads.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Async enumerable of chat feedback records.</returns>
        IAsyncEnumerable<ChatFeedback> ReadAllInTenant(
            Guid tenantGuid,
            ChatFeedbackRatingEnum? rating = null,
            Guid? threadGuid = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Enumerate chat feedback, optionally filtered by rating or thread.
        /// </summary>
        /// <param name="query">Enumeration query.  TenantGUID is required.</param>
        /// <param name="rating">Rating filter.  Null returns all ratings.</param>
        /// <param name="threadGuid">Thread GUID filter.  Null returns all threads.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing a page of objects.</returns>
        Task<EnumerationResult<ChatFeedback>> Enumerate(
            EnumerationRequest query,
            ChatFeedbackRatingEnum? rating = null,
            Guid? threadGuid = null,
            CancellationToken token = default);

        /// <summary>
        /// Get the record count.  Optionally supply a marker object GUID to indicate that only records from that marker record should be counted.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="rating">Rating filter.  Null counts all ratings.</param>
        /// <param name="threadGuid">Thread GUID filter.  Null counts all threads.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="markerGuid">Marker GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Number of records.</returns>
        Task<int> GetRecordCount(
            Guid? tenantGuid,
            ChatFeedbackRatingEnum? rating = null,
            Guid? threadGuid = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Guid? markerGuid = null,
            CancellationToken token = default);

        /// <summary>
        /// Delete a chat feedback record.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Delete all feedback for a thread.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="threadGuid">Thread GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByThread(Guid tenantGuid, Guid threadGuid, CancellationToken token = default);

        /// <summary>
        /// Delete all chat feedback in a tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default);

        /// <summary>
        /// Check if a chat feedback record exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);
    }
}
