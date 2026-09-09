namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Algorithms;
    using LiteGraph.GraphRepositories;

    /// <summary>
    /// Graph algorithm methods implementation for the client.
    /// Provides client-side validation, guardrails, and optional write-back for algorithm operations.
    /// </summary>
    public class AlgorithmMethods : Interfaces.IAlgorithmMethods
    {
        #region Public-Members

        /// <inheritdoc />
        public GraphAlgorithmConfiguration Configuration
        {
            get
            {
                return _Configuration;
            }
            set
            {
                _Configuration = value ?? new GraphAlgorithmConfiguration();
            }
        }

        #endregion

        #region Private-Members

        private readonly LiteGraphClient _Client;
        private GraphRepositoryBase _Repo = null;
        private GraphAlgorithmConfiguration _Configuration = new GraphAlgorithmConfiguration();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate AlgorithmMethods.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="repo">Graph repository.</param>
        public AlgorithmMethods(LiteGraphClient client, GraphRepositoryBase repo)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<GraphAlgorithmResult> Run(Guid tenantGuid, Guid graphGuid, GraphAlgorithmRequest request, CancellationToken token = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            token.ThrowIfCancellationRequested();

            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);

            GraphAlgorithmResult result = await GraphAlgorithmRunner.RunAsync(
                _Client,
                tenantGuid,
                graphGuid,
                request,
                _Configuration.MaxNodes,
                _Configuration.MaxEdges,
                token).ConfigureAwait(false);

            if (request.WriteBack)
            {
                string property = !string.IsNullOrEmpty(request.WriteBackProperty)
                    ? request.WriteBackProperty
                    : DefaultProperty(request.AlgorithmType);

                await WriteBackAsync(tenantGuid, graphGuid, result, property, token).ConfigureAwait(false);

                result.WrittenBack = true;
                result.WriteBackProperty = property;
            }

            return result;
        }

        /// <inheritdoc />
        public async Task ExportGraph(
            Guid tenantGuid,
            Guid graphGuid,
            GraphExportFormatEnum format,
            GraphExportAttributeLevelEnum attributeLevel,
            Stream stream,
            CancellationToken token = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            token.ThrowIfCancellationRequested();

            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);

            await GraphProjectionExporter.ExportAsync(_Client, tenantGuid, graphGuid, format, attributeLevel, stream, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<int> ImportResults(Guid tenantGuid, Guid graphGuid, GraphAlgorithmImportRequest request, CancellationToken token = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            token.ThrowIfCancellationRequested();

            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);

            int updated = 0;

            foreach (KeyValuePair<Guid, Dictionary<string, double>> entry in request.Values)
            {
                token.ThrowIfCancellationRequested();
                if (entry.Value == null || entry.Value.Count == 0) continue;

                Node node = await _Client.Node.ReadByGuid(tenantGuid, graphGuid, entry.Key, true, false, token).ConfigureAwait(false);
                if (node == null) continue;

                JsonObject dataObject = ToJsonObject(node.Data);
                foreach (KeyValuePair<string, double> property in entry.Value)
                {
                    dataObject[property.Key] = JsonValue.Create(property.Value);
                }

                node.Data = dataObject;
                await _Client.Node.Update(node, token).ConfigureAwait(false);
                updated++;
            }

            return updated;
        }

        #endregion

        #region Private-Methods

        private static string DefaultProperty(GraphAlgorithmTypeEnum algorithm)
        {
            switch (algorithm)
            {
                case GraphAlgorithmTypeEnum.PageRank:
                    return PageRank.DefaultProperty;
                case GraphAlgorithmTypeEnum.DegreeCentrality:
                    return DegreeCentrality.DefaultProperty;
                case GraphAlgorithmTypeEnum.WeaklyConnectedComponents:
                    return WeaklyConnectedComponents.DefaultProperty;
                case GraphAlgorithmTypeEnum.StronglyConnectedComponents:
                    return StronglyConnectedComponents.DefaultProperty;
                case GraphAlgorithmTypeEnum.LabelPropagation:
                    return LabelPropagation.DefaultProperty;
                case GraphAlgorithmTypeEnum.ClosenessCentrality:
                    return ClosenessCentrality.DefaultProperty;
                case GraphAlgorithmTypeEnum.EigenvectorCentrality:
                    return EigenvectorCentrality.DefaultProperty;
                case GraphAlgorithmTypeEnum.BetweennessCentrality:
                    return BetweennessCentrality.DefaultProperty;
                case GraphAlgorithmTypeEnum.Louvain:
                    return Louvain.DefaultProperty;
                case GraphAlgorithmTypeEnum.ClusteringCoefficient:
                    return ClusteringCoefficient.DefaultProperty;
                case GraphAlgorithmTypeEnum.KCore:
                    return KCore.DefaultProperty;
                default:
                    return "algorithm";
            }
        }

        private async Task WriteBackAsync(Guid tenantGuid, Guid graphGuid, GraphAlgorithmResult result, string property, CancellationToken token)
        {
            foreach (GraphAlgorithmNodeResult item in result.Nodes)
            {
                token.ThrowIfCancellationRequested();

                Node node = await _Client.Node.ReadByGuid(tenantGuid, graphGuid, item.NodeGUID, true, false, token).ConfigureAwait(false);
                if (node == null) continue;

                JsonObject dataObject = ToJsonObject(node.Data);

                if (item.Community != null) dataObject[property] = JsonValue.Create(item.Community.Value);
                else dataObject[property] = JsonValue.Create(item.Score);

                node.Data = dataObject;
                await _Client.Node.Update(node, token).ConfigureAwait(false);
            }
        }

        private static JsonObject ToJsonObject(object data)
        {
            if (data == null) return new JsonObject();

            try
            {
                JsonNode node = JsonSerializer.SerializeToNode(data);
                if (node is JsonObject existing) return existing;
            }
            catch (JsonException)
            {
                // Non-object or non-serializable data is replaced with a fresh object carrying only the algorithm result.
            }

            return new JsonObject();
        }

        #endregion
    }
}
