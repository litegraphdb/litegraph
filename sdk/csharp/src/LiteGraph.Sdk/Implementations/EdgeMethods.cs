namespace LiteGraph.Sdk.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using ExpressionTree;
    using LiteGraph.Sdk.Interfaces;

    /// <summary>
    /// Edge methods.
    /// </summary>
    public class EdgeMethods : IEdgeMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphSdk _Sdk = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Edge methods.
        /// </summary>
        /// <param name="sdk">LiteGraph SDK.</param>
        public EdgeMethods(LiteGraphSdk sdk)
        {
            _Sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<Edge> Create(Edge edge, CancellationToken token = default)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + edge.TenantGUID + "/graphs/" + edge.GraphGUID + "/edges";
            return await _Sdk.PutCreate<Edge>(url, edge, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Edge>> CreateMany(Guid tenantGuid, Guid graphGuid, List<Edge> edges, CancellationToken token = default)
        {
            if (edges == null) throw new ArgumentNullException(nameof(edges));
            if (edges.Count < 1) return new List<Edge>();
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/edges/bulk";
            return await _Sdk.PutCreate<List<Edge>>(url, edges, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Edge>> ReadMany(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0, CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentNullException(nameof(skip));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/edges?skip=" + skip + "&order=" + order.ToString();
            return await _Sdk.GetMany<Edge>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Edge> ReadByGuid(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, bool includeData = false, bool includeSubordinates = false, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/edges/" + edgeGuid;
            if (includeData) url += "?incldata";
            if (includeSubordinates) url += (includeData ? "&" : "?") + "inclsub";
            return await _Sdk.Get<Edge>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Edge>> ReadByGuids(Guid tenantGuid, Guid graphGuid, List<Guid> guids, bool includeData = false, bool includeSubordinates = false, CancellationToken token = default)
        {
            if (guids == null || guids.Count < 1) throw new ArgumentNullException(nameof(guids));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/edges?guids=" + string.Join(",", guids);
            if (includeData) url += "&incldata";
            if (includeSubordinates) url += "&inclsub";
            return await _Sdk.Get<List<Edge>>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Edge>> ReadNodeEdges(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/" + nodeGuid + "/edges";
            bool hasQuery = false;
            if (skip > 0) { url += "?skip=" + skip; hasQuery = true; }
            if (order != EnumerationOrderEnum.CreatedDescending) { url += (hasQuery ? "&" : "?") + "order=" + order.ToString(); hasQuery = true; }
            if (includeData) { url += (hasQuery ? "&" : "?") + "incldata"; hasQuery = true; }
            if (includeSubordinates) url += (hasQuery ? "&" : "?") + "inclsub";
            return await _Sdk.GetMany<Edge>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Edge>> ReadEdgesFromNode(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/" + nodeGuid + "/edges/from";
            bool hasQuery = false;
            if (skip > 0) { url += "?skip=" + skip; hasQuery = true; }
            if (order != EnumerationOrderEnum.CreatedDescending) { url += (hasQuery ? "&" : "?") + "order=" + order.ToString(); hasQuery = true; }
            if (includeData) { url += (hasQuery ? "&" : "?") + "incldata"; hasQuery = true; }
            if (includeSubordinates) url += (hasQuery ? "&" : "?") + "inclsub";
            return await _Sdk.GetMany<Edge>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Edge>> ReadEdgesToNode(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/" + nodeGuid + "/edges/to";
            bool hasQuery = false;
            if (skip > 0) { url += "?skip=" + skip; hasQuery = true; }
            if (order != EnumerationOrderEnum.CreatedDescending) { url += (hasQuery ? "&" : "?") + "order=" + order.ToString(); hasQuery = true; }
            if (includeData) { url += (hasQuery ? "&" : "?") + "incldata"; hasQuery = true; }
            if (includeSubordinates) url += (hasQuery ? "&" : "?") + "inclsub";
            return await _Sdk.GetMany<Edge>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Edge>> ReadEdgesBetweenNodes(
            Guid tenantGuid,
            Guid graphGuid,
            Guid fromNodeGuid,
            Guid toNodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0, 
            CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/edges/between?from=" + fromNodeGuid + "&to=" + toNodeGuid;
            return await _Sdk.GetMany<Edge>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Edge> Update(Edge edge, CancellationToken token = default)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + edge.TenantGUID + "/graphs/" + edge.GraphGUID + "/edges/" + edge.GUID;
            return await _Sdk.PutUpdate<Edge>(url, edge, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/edges/" + edgeGuid;
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteMany(Guid tenantGuid, Guid graphGuid, List<Guid> edgeGuids, CancellationToken token = default)
        {
            if (edgeGuids == null) throw new ArgumentNullException(nameof(edgeGuids));
            if (edgeGuids.Count < 1) return;
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/edges/bulk";
            await _Sdk.Delete<List<Guid>>(url, edgeGuids, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteNodeEdges(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/" + nodeGuid + "/edges";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/edges/all";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/edges/" + edgeGuid;
            return await _Sdk.Head(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<SearchResult> Search(SearchRequest req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + req.TenantGUID + "/graphs/" + req.GraphGUID + "/edges/search";
            return await _Sdk.Post<SearchRequest, SearchResult>(url, req, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Edge> ReadFirst(SearchRequest req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + req.TenantGUID + "/graphs/" + req.GraphGUID + "/edges/first";
            return await _Sdk.Post<SearchRequest, Edge>(url, req, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<Edge>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.TenantGUID == null) throw new ArgumentNullException(nameof(query.TenantGUID));
            string url = _Sdk.Endpoint + "v2.0/tenants/" + query.TenantGUID.Value + "/graphs/" + query.GraphGUID.Value + "/edges";
            return await _Sdk.Post<EnumerationRequest, EnumerationResult<Edge>>(url, query, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Edge>> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/edges/all";

            bool hasQuery = false;
            if (skip > 0) { url += "?skip=" + skip; hasQuery = true; }
            if (order != EnumerationOrderEnum.CreatedDescending) { url += (hasQuery ? "&" : "?") + "order=" + order.ToString(); hasQuery = true; }
            if (includeData) { url += (hasQuery ? "&" : "?") + "incldata"; hasQuery = true; }
            if (includeSubordinates) url += (hasQuery ? "&" : "?") + "inclsub";

            return await _Sdk.GetMany<Edge>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Edge>> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/edges/all";

            bool hasQuery = false;
            if (skip > 0) { url += "?skip=" + skip; hasQuery = true; }
            if (order != EnumerationOrderEnum.CreatedDescending) { url += (hasQuery ? "&" : "?") + "order=" + order.ToString(); hasQuery = true; }
            if (includeData) { url += (hasQuery ? "&" : "?") + "incldata"; hasQuery = true; }
            if (includeSubordinates) url += (hasQuery ? "&" : "?") + "inclsub";

            return await _Sdk.GetMany<Edge>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/edges/all";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteNodeEdgesMany(Guid tenantGuid, Guid graphGuid, List<Guid> nodeGuids, CancellationToken token = default)
        {
            if (nodeGuids == null) throw new ArgumentNullException(nameof(nodeGuids));
            if (nodeGuids.Count < 1) return;

            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/edges/bulk";
            await _Sdk.Delete<List<Guid>>(url, nodeGuids, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}