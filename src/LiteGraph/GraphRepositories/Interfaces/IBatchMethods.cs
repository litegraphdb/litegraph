namespace LiteGraph.GraphRepositories.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;

    /// <summary>
    /// Interface for batch methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public interface IBatchMethods
    {
        /// <summary>
        /// Batch existence request.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="req">Existence request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Existence result.</returns>
        Task<ExistenceResult> Existence(Guid tenantGuid, Guid graphGuid, ExistenceRequest req, CancellationToken token = default);
    }
}
