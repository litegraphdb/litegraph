namespace LiteGraph.Client.Interfaces
{
    using System;
    using System.IO;
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

        /// <summary>
        /// Export a graph to a stream as a portable projection for external computation (for example rustworkx or NetworkX).
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="format">Export format.</param>
        /// <param name="attributeLevel">Attribute detail level.</param>
        /// <param name="stream">Destination stream (left open).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
        Task ExportGraph(
            Guid tenantGuid,
            Guid graphGuid,
            GraphExportFormatEnum format,
            GraphExportAttributeLevelEnum attributeLevel,
            Stream stream,
            CancellationToken token = default);

        /// <summary>
        /// Import externally computed per-node values back onto graph nodes, writing them into node data.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="request">Import request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Number of nodes updated.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
        Task<int> ImportResults(Guid tenantGuid, Guid graphGuid, GraphAlgorithmImportRequest request, CancellationToken token = default);
    }
}
