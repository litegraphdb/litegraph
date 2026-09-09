namespace LiteGraph.Sdk.Interfaces
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Interface for graph algorithm SDK methods.
    /// </summary>
    public interface IAlgorithmMethods
    {
        /// <summary>
        /// Run a graph algorithm over a single graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="request">Algorithm request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Algorithm result.</returns>
        Task<GraphAlgorithmResult> Run(Guid tenantGuid, Guid graphGuid, GraphAlgorithmRequest request, CancellationToken token = default);

        /// <summary>
        /// Export a graph as a portable projection for external computation (for example rustworkx or NetworkX).
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="format">Export format.</param>
        /// <param name="attributeLevel">Attribute detail level.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Exported projection bytes.</returns>
        Task<byte[]> ExportGraph(
            Guid tenantGuid,
            Guid graphGuid,
            GraphExportFormatEnum format = GraphExportFormatEnum.NodeLinkJson,
            GraphExportAttributeLevelEnum attributeLevel = GraphExportAttributeLevelEnum.Meta,
            CancellationToken token = default);

        /// <summary>
        /// Import externally computed per-node values back onto graph nodes.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="request">Import request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Import result.</returns>
        Task<GraphAlgorithmImportResult> ImportResults(Guid tenantGuid, Guid graphGuid, GraphAlgorithmImportRequest request, CancellationToken token = default);
    }
}
