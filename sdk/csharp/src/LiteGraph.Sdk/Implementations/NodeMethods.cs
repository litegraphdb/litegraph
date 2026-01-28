namespace LiteGraph.Sdk.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using ExpressionTree;
    using LiteGraph.Sdk;
    using LiteGraph.Sdk.Interfaces;

    /// <summary>
    /// Node methods.
    /// </summary>
    public class NodeMethods : INodeMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphSdk _Sdk = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Node methods.
        /// </summary>
        /// <param name="sdk">LiteGraph SDK.</param>
        public NodeMethods(LiteGraphSdk sdk)
        {
            _Sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<Node> Create(Node node, CancellationToken token = default)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + node.TenantGUID + "/graphs/" + node.GraphGUID + "/nodes";
            return await _Sdk.PutCreate<Node>(url, node, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Node>> CreateMany(Guid tenantGuid, Guid graphGuid, List<Node> nodes, CancellationToken token = default)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));
            if (nodes.Count < 1) return new List<Node>();
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/bulk";
            return await _Sdk.PutCreate<List<Node>>(url, nodes, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Node>> ReadMany(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentNullException(nameof(skip));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes?skip=" + skip + "&order=" + order.ToString();
            if (includeData) url += "&incldata";
            if (includeSubordinates) url += "&inclsub";
            return await _Sdk.GetMany<Node>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Node> ReadByGuid(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, bool includeData = false, bool includeSubordinates = false, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/" + nodeGuid;
            if (includeData) url += "?incldata";
            if (includeSubordinates) url += (includeData ? "&" : "?") + "inclsub";
            return await _Sdk.Get<Node>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Node>> ReadByGuids(Guid tenantGuid, Guid graphGuid, List<Guid> guids, bool includeData = false, bool includeSubordinates = false, CancellationToken token = default)
        {
            if (guids == null || guids.Count < 1) throw new ArgumentNullException(nameof(guids));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes?guids=" + string.Join(",", guids);
            if (includeData) url += "&incldata";
            if (includeSubordinates) url += "&inclsub";
            return await _Sdk.Get<List<Node>>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Node> Update(Node node, CancellationToken token = default)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + node.TenantGUID + "/graphs/" + node.GraphGUID + "/nodes/" + node.GUID;
            return await _Sdk.PutUpdate<Node>(url, node, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/" + nodeGuid;
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/all";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteMany(Guid tenantGuid, Guid graphGuid, List<Guid> nodeGuids, CancellationToken token = default)
        {
            if (nodeGuids == null || nodeGuids.Count < 1) throw new ArgumentNullException(nameof(nodeGuids));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/bulk";
            await _Sdk.Delete<List<Guid>>(url, nodeGuids, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/" + nodeGuid;
            return await _Sdk.Head(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Node>> ReadParents(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0, 
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/" + nodeGuid + "/parents?skip=" + skip + "&order=" + order.ToString();
            return await _Sdk.GetMany<Node>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Node>> ReadChildren(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0, 
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/" + nodeGuid + "/children?skip=" + skip + "&order=" + order.ToString();
            return await _Sdk.GetMany<Node>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Node>> ReadNeighbors(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0, 
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/" + nodeGuid + "/neighbors?skip=" + skip + "&order=" + order.ToString();
            return await _Sdk.GetMany<Node>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<RouteResponse> ReadRoutes(
            SearchTypeEnum searchType,
            Guid tenantGuid,
            Guid graphGuid,
            Guid fromNodeGuid,
            Guid toNodeGuid,
            Expr edgeFilter = null,
            Expr nodeFilter = null, 
            CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/routes";

            RouteRequest req = new RouteRequest
            {
                Graph = graphGuid,
                From = fromNodeGuid,
                To = toNodeGuid
            };

            byte[] bytes = await _Sdk.PostRaw(url, Encoding.UTF8.GetBytes(Serializer.SerializeJson(req, true)), "application/json", token).ConfigureAwait(false);

            if (bytes != null && bytes.Length > 0)
            {
                RouteResponse resp = Serializer.DeserializeJson<RouteResponse>(Encoding.UTF8.GetString(bytes));
                return resp;
            }

            return null;
        }

        /// <inheritdoc />
        public async Task<SearchResult> Search(SearchRequest req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + req.TenantGUID + "/graphs/" + req.GraphGUID + "/nodes/search";
            return await _Sdk.Post<SearchRequest, SearchResult>(url, req, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<Node> ReadFirst(SearchRequest req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + req.TenantGUID + "/graphs/" + req.GraphGUID + "/nodes/first";
            return await _Sdk.Post<SearchRequest, Node>(url, req, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<Node>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.TenantGUID == null) throw new ArgumentNullException(nameof(query.TenantGUID));
            string url = _Sdk.Endpoint + "v2.0/tenants/" + query.TenantGUID.Value + "/graphs/" + query.GraphGUID.Value + "/nodes";
            return await _Sdk.Post<EnumerationRequest, EnumerationResult<Node>>(url, query, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Node>> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/nodes/all";

            bool hasQuery = false;
            if (skip > 0) { url += "?skip=" + skip; hasQuery = true; }
            if (order != EnumerationOrderEnum.CreatedDescending) { url += (hasQuery ? "&" : "?") + "order=" + order.ToString(); hasQuery = true; }
            if (includeData) { url += (hasQuery ? "&" : "?") + "incldata"; hasQuery = true; }
            if (includeSubordinates) url += (hasQuery ? "&" : "?") + "inclsub";

            return await _Sdk.GetMany<Node>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Node>> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/all";

            bool hasQuery = false;
            if (skip > 0) { url += "?skip=" + skip; hasQuery = true; }
            if (order != EnumerationOrderEnum.CreatedDescending) { url += (hasQuery ? "&" : "?") + "order=" + order.ToString(); hasQuery = true; }
            if (includeData) { url += (hasQuery ? "&" : "?") + "incldata"; hasQuery = true; }
            if (includeSubordinates) url += (hasQuery ? "&" : "?") + "inclsub";

            return await _Sdk.GetMany<Node>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Node>> ReadMostConnected(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/mostconnected";

            bool hasQuery = false;
            if (skip > 0) { url += "?skip=" + skip; hasQuery = true; }
            if (order != EnumerationOrderEnum.CreatedDescending) { url += (hasQuery ? "&" : "?") + "order=" + order.ToString(); hasQuery = true; }
            if (includeData) { url += (hasQuery ? "&" : "?") + "incldata"; hasQuery = true; }
            if (includeSubordinates) url += (hasQuery ? "&" : "?") + "inclsub";

            return await _Sdk.GetMany<Node>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<Node>> ReadLeastConnected(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            bool includeData = false,
            bool includeSubordinates = false,
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/leastconnected";

            bool hasQuery = false;
            if (skip > 0) { url += "?skip=" + skip; hasQuery = true; }
            if (order != EnumerationOrderEnum.CreatedDescending) { url += (hasQuery ? "&" : "?") + "order=" + order.ToString(); hasQuery = true; }
            if (includeData) { url += (hasQuery ? "&" : "?") + "incldata"; hasQuery = true; }
            if (includeSubordinates) url += (hasQuery ? "&" : "?") + "inclsub";

            return await _Sdk.GetMany<Node>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/nodes/all";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}