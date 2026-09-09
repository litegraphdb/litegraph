namespace LiteGraph.Sdk.Implementations
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Sdk;
    using LiteGraph.Sdk.Interfaces;

    /// <summary>
    /// Graph algorithm SDK methods.
    /// </summary>
    public class AlgorithmMethods : IAlgorithmMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphSdk _Sdk = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Graph algorithm SDK methods.
        /// </summary>
        /// <param name="sdk">LiteGraph SDK.</param>
        public AlgorithmMethods(LiteGraphSdk sdk)
        {
            _Sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<GraphAlgorithmResult> Run(Guid tenantGuid, Guid graphGuid, GraphAlgorithmRequest request, CancellationToken token = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/algorithms";
            return await _Sdk.Post<GraphAlgorithmRequest, GraphAlgorithmResult>(url, request, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<byte[]> ExportGraph(
            Guid tenantGuid,
            Guid graphGuid,
            GraphExportFormatEnum format = GraphExportFormatEnum.NodeLinkJson,
            GraphExportAttributeLevelEnum attributeLevel = GraphExportAttributeLevelEnum.Meta,
            CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid
                + "/export/projection?format=" + format.ToString() + "&attributes=" + attributeLevel.ToString();
            return await _Sdk.Get(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<GraphAlgorithmImportResult> ImportResults(Guid tenantGuid, Guid graphGuid, GraphAlgorithmImportRequest request, CancellationToken token = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/algorithms/import";
            return await _Sdk.Post<GraphAlgorithmImportRequest, GraphAlgorithmImportResult>(url, request, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<GenerateEmbeddingsResult> GenerateEmbeddings(Guid tenantGuid, Guid graphGuid, GenerateEmbeddingsRequest request, CancellationToken token = default)
        {
            if (request == null) request = new GenerateEmbeddingsRequest();
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/algorithms/embeddings";
            return await _Sdk.Post<GenerateEmbeddingsRequest, GenerateEmbeddingsResult>(url, request, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
