namespace LiteGraph.Client.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Algorithms;

    /// <summary>
    /// Interface for graph algorithm methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public interface IAlgorithmMethods
    {
        /// <summary>
        /// Configuration and guardrails for algorithm execution.
        /// </summary>
        GraphAlgorithmConfiguration Configuration { get; set; }

        /// <summary>
        /// Run a graph algorithm over a single graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="request">Algorithm request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Algorithm result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the graph exceeds the configured node or edge ceiling.</exception>
        Task<GraphAlgorithmResult> Run(Guid tenantGuid, Guid graphGuid, GraphAlgorithmRequest request, CancellationToken token = default);
    }
}
