namespace LiteGraph.Client.Interfaces
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;

    /// <summary>
    /// Interface for chat endpoint methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public interface IChatEndpointMethods
    {
        /// <summary>
        /// Create a chat endpoint.
        /// </summary>
        /// <param name="endpoint">Chat endpoint.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat endpoint.</returns>
        Task<ChatEndpoint> Create(ChatEndpoint endpoint, CancellationToken token = default);

        /// <summary>
        /// Read all chat endpoints in tenant, optionally filtered by endpoint type.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="endpointType">Endpoint type filter.  Null returns all types.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Async enumerable of chat endpoints.</returns>
        IAsyncEnumerable<ChatEndpoint> ReadAllInTenant(
            Guid tenantGuid,
            ChatEndpointTypeEnum? endpointType = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            CancellationToken token = default);

        /// <summary>
        /// Read a chat endpoint by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat endpoint, or null if not found.</returns>
        Task<ChatEndpoint> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Enumerate chat endpoints, optionally filtered by endpoint type.
        /// </summary>
        /// <param name="query">Enumeration query.</param>
        /// <param name="endpointType">Endpoint type filter.  Null returns all types.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Enumeration result containing a page of objects.</returns>
        Task<EnumerationResult<ChatEndpoint>> Enumerate(EnumerationRequest query, ChatEndpointTypeEnum? endpointType = null, CancellationToken token = default);

        /// <summary>
        /// Get the record count.  Optionally supply a marker object GUID to indicate that only records from that marker record should be counted.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="endpointType">Endpoint type filter.  Null counts all types.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="markerGuid">Marker GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Number of records.</returns>
        Task<int> GetRecordCount(
            Guid? tenantGuid,
            ChatEndpointTypeEnum? endpointType = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Guid? markerGuid = null,
            CancellationToken token = default);

        /// <summary>
        /// Update a chat endpoint.
        /// </summary>
        /// <param name="endpoint">Chat endpoint.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Chat endpoint.</returns>
        Task<ChatEndpoint> Update(ChatEndpoint endpoint, CancellationToken token = default);

        /// <summary>
        /// Delete a chat endpoint.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);

        /// <summary>
        /// Delete all chat endpoints in a tenant.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="token">Cancellation token.</param>
        Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default);

        /// <summary>
        /// Check if a chat endpoint exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True if exists.</returns>
        Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default);
    }
}
