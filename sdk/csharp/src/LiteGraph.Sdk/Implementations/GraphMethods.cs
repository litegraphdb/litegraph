namespace LiteGraph.Sdk.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Sdk;
    using LiteGraph.Sdk.Interfaces;

    /// <summary>
    /// Graph methods.
    /// </summary>
    public class GraphMethods : IGraphMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphSdk _Sdk = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Graph methods.
        /// </summary>
        /// <param name="sdk">LiteGraph SDK.</param>
        public GraphMethods(LiteGraphSdk sdk)
        {
            _Sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<Graph> Create(Graph graph, CancellationToken token = default)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + graph.TenantGUID + "/graphs";
            return await _Sdk.PutCreate<Graph>(url, graph, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Graph>> ReadMany(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0, 
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs?skip=" + skip + "&order=" + order.ToString();
            return await _Sdk.GetMany<Graph>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Graph> ReadByGuid(Guid tenantGuid, Guid graphGuid, bool includeData = false, bool includeSubordinates = false, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid;
            if (includeData) url += "?incldata";
            if (includeSubordinates) url += (includeData ? "&" : "?") + "inclsub";
            return await _Sdk.Get<Graph>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Graph>> ReadByGuids(Guid tenantGuid, List<Guid> guids, bool includeData = false, bool includeSubordinates = false, CancellationToken token = default)
        {
            if (guids == null || guids.Count < 1) throw new ArgumentNullException(nameof(guids));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs?guids=" + string.Join(",", guids);
            if (includeData) url += "&incldata";
            if (includeSubordinates) url += "&inclsub";
            return await _Sdk.Get<List<Graph>>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Graph> Update(Graph graph, CancellationToken token = default)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + graph.TenantGUID + "/graphs/" + graph.GUID;
            return await _Sdk.PutUpdate<Graph>(url, graph, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid graphGuid, bool force = false, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid;
            if (force) url += "?force";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + guid;
            return await _Sdk.Head(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<SearchResult> Search(SearchRequest req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + req.TenantGUID + "/graphs/search";
            return await _Sdk.Post<SearchRequest, SearchResult>(url, req, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Graph> ReadFirst(SearchRequest req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + req.TenantGUID + "/graphs/first";
            return await _Sdk.Post<SearchRequest, Graph>(url, req, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<Graph>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.TenantGUID == null) throw new ArgumentNullException(nameof(query.TenantGUID));
            string url = _Sdk.Endpoint + "v2.0/tenants/" + query.TenantGUID.Value + "/graphs";
            return await _Sdk.Post<EnumerationRequest, EnumerationResult<Graph>>(url, query, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<GraphStatistics> GetStatistics(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + guid + "/stats";
            return await _Sdk.Get<GraphStatistics>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Dictionary<Guid, GraphStatistics>> GetStatistics(Guid tenantGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/stats";
            return await _Sdk.Get<Dictionary<Guid, GraphStatistics>>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task EnableVectorIndexing(Guid tenantGuid, Guid graphGuid, VectorIndexConfiguration config, CancellationToken token = default)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            string url = _Sdk.Endpoint + "v2.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/vectorindex/enable";
            await _Sdk.PutUpdate<VectorIndexConfiguration>(url, config, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task RebuildVectorIndex(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v2.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/vectorindex/rebuild";
            await _Sdk.Post<object, object>(url, new { }, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteVectorIndex(Guid tenantGuid, Guid graphGuid, bool deleteIndexFile = false, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/vectorindex";
            if (deleteIndexFile) url += "?deleteFile=true";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<VectorIndexConfiguration> ReadVectorIndexConfig(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/vectorindex/config";
            return await _Sdk.Get<VectorIndexConfiguration>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<VectorIndexStatistics> GetVectorIndexStatistics(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/vectorindex/stats";
            return await _Sdk.Get<VectorIndexStatistics>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<SearchResult> GetSubgraph(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            int maxDepth = 2,
            int maxNodes = 0,
            int maxEdges = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default)
        {
            if (maxDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDepth));
            if (maxNodes < 0) throw new ArgumentOutOfRangeException(nameof(maxNodes));
            if (maxEdges < 0) throw new ArgumentOutOfRangeException(nameof(maxEdges));

            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/" + nodeGuid + "/subgraph";
            url += "?maxDepth=" + maxDepth;
            url += "&maxNodes=" + maxNodes;
            url += "&maxEdges=" + maxEdges;
            if (includeData) url += "&incldata";
            if (includeSubordinates) url += "&inclsub";

            return await _Sdk.Get<SearchResult>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<GraphStatistics> GetSubgraphStatistics(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            int maxDepth = 2,
            int maxNodes = 0,
            int maxEdges = 0,
            CancellationToken token = default)
        {
            if (maxDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDepth));
            if (maxNodes < 0) throw new ArgumentOutOfRangeException(nameof(maxNodes));
            if (maxEdges < 0) throw new ArgumentOutOfRangeException(nameof(maxEdges));

            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/" + nodeGuid + "/subgraph/stats";
            url += "?maxDepth=" + maxDepth;
            url += "&maxNodes=" + maxNodes;
            url += "&maxEdges=" + maxEdges;

            return await _Sdk.Get<GraphStatistics>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<string> ExportGraphToGexf(
            Guid tenantGuid,
            Guid graphGuid,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/export/gexf";
            if (includeData) url += "?incldata";
            if (includeSubordinates) url += (includeData ? "&" : "?") + "inclsub";

            byte[] bytes = await _Sdk.Get(url, token).ConfigureAwait(false);
            if (bytes != null && bytes.Length > 0)
            {
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            return null;
        }

        /// <inheritdoc />
        public async Task<List<Graph>> ReadAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/all";
            return await _Sdk.GetMany<Graph>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/all";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}