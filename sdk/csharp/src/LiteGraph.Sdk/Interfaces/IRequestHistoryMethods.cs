namespace LiteGraph.Sdk.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for request history methods.  System administrators see every tenant and may filter by tenant; tenant
    /// administrators are scoped to their own tenant by the server.
    /// </summary>
    public interface IRequestHistoryMethods
    {
        /// <summary>
        /// Search request history, returning one page.
        /// </summary>
        /// <param name="request">Filters and paging, or null for the first page of all entries.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>One page of entries, newest first, or null if the request failed.</returns>
        Task<EnumerationResult<RequestHistoryEntry>> Search(RequestHistorySearchRequest request = null, CancellationToken token = default);

        /// <summary>
        /// Enumerate every request history entry matching the filters, following pages until the end of results.
        /// </summary>
        /// <param name="request">Filters, or null for all entries.  MaxKeys sets the page size; Skip sets the starting offset.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Matching entries, newest first.</returns>
        IAsyncEnumerable<RequestHistoryEntry> Enumerate(RequestHistorySearchRequest request = null, CancellationToken token = default);

        /// <summary>
        /// Read one request history entry.
        /// </summary>
        /// <param name="requestGuid">Entry GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The entry, or null if it does not exist.</returns>
        Task<RequestHistoryEntry> ReadByGuid(Guid requestGuid, CancellationToken token = default);

        /// <summary>
        /// Read one request history entry with its captured headers and bodies.
        /// </summary>
        /// <param name="requestGuid">Entry GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>The detail, or null if it does not exist.</returns>
        Task<RequestHistoryDetail> ReadDetail(Guid requestGuid, CancellationToken token = default);

        /// <summary>
        /// Read request counts over a time range, bucketed by interval.
        /// </summary>
        /// <param name="interval">Bucket interval: minute, 15minute, hour, 6hour, or day.  Null for hour.</param>
        /// <param name="startUtc">Range start (UTC).  Null for 24 hours before the end.</param>
        /// <param name="endUtc">Range end (UTC).  Null for now.</param>
        /// <param name="tenantGuid">Tenant filter for system administrators.  Null for all tenants.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Summary, or null if the request failed.</returns>
        Task<RequestHistorySummary> ReadSummary(string interval = null, DateTime? startUtc = null, DateTime? endUtc = null, Guid? tenantGuid = null, CancellationToken token = default);

        /// <summary>
        /// Delete one request history entry.
        /// </summary>
        /// <param name="requestGuid">Entry GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        Task DeleteByGuid(Guid requestGuid, CancellationToken token = default);

        /// <summary>
        /// Delete every request history entry matching the filters.  Paging values are ignored.
        /// </summary>
        /// <param name="request">Filters.  An empty filter deletes every entry the caller can see.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Number of entries deleted, or null if the request failed.</returns>
        /// <exception cref="ArgumentNullException">request is null.</exception>
        Task<RequestHistoryDeleteResult> DeleteMany(RequestHistorySearchRequest request, CancellationToken token = default);
    }
}
