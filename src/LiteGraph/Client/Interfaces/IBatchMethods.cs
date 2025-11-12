namespace LiteGraph.Client.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;

    /// <summary>
    /// Interface for batch methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
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
