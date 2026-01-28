namespace LiteGraph.Sdk.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using System.Numerics;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using ExpressionTree;
    using LiteGraph.Sdk.Interfaces;

    /// <summary>
    /// Vector methods.
    /// </summary>
    public class VectorMethods : IVectorMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphSdk _Sdk = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Vector methods.
        /// </summary>
        /// <param name="sdk">LiteGraph SDK.</param>
        public VectorMethods(LiteGraphSdk sdk)
        {
            _Sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<VectorMetadata>  Create(VectorMetadata vector, CancellationToken token = default)
        {
            if (vector == null) throw new ArgumentNullException(nameof(vector));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + vector.TenantGUID + "/vectors";
            return await _Sdk.PutCreate<VectorMetadata>(url, vector, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<VectorMetadata>> CreateMany(Guid tenantGuid, List<VectorMetadata> vectors, CancellationToken token = default)
        {
            if (vectors == null || vectors.Count < 1) throw new ArgumentNullException(nameof(vectors));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/vectors/bulk";
            return await _Sdk.PutCreate<List<VectorMetadata>>(url, vectors, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<VectorMetadata>> ReadMany(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0, 
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/vectors?skip=" + skip + "&order=" + order.ToString();
            return await _Sdk.GetMany<VectorMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<VectorMetadata> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/vectors/" + guid;
            return await _Sdk.Get<VectorMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<VectorMetadata>> ReadByGuids(Guid tenantGuid, List<Guid> guids, CancellationToken token = default)
        {
            if (guids == null || guids.Count < 1) throw new ArgumentNullException(nameof(guids));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/vectors?guids=" + string.Join(",", guids);
            return await _Sdk.Get<List<VectorMetadata>>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<VectorMetadata>  Update(VectorMetadata vector, CancellationToken token = default)
        {
            if (vector == null) throw new ArgumentNullException(nameof(vector));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + vector.TenantGUID + "/vectors/" + vector.GUID;
            return await _Sdk.PutUpdate<VectorMetadata>(url, vector, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/vectors/" + guid;
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteMany(Guid tenantGuid, List<Guid> guids, CancellationToken token = default)
        {
            if (guids == null || guids.Count < 1) throw new ArgumentNullException(nameof(guids));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/vectors/bulk";
            await _Sdk.Delete<List<Guid>>(url, guids, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/vectors/" + guid;
            return await _Sdk.Head(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<VectorSearchResult>> SearchVectors(
            Guid tenantGuid, 
            Guid? graphGuid, 
            VectorSearchRequest searchReq, 
            CancellationToken token = default)
        {
            if (searchReq == null) throw new ArgumentNullException(nameof(searchReq));

            searchReq.TenantGUID = tenantGuid;
            searchReq.GraphGUID = graphGuid;

            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/vectors";
            string json = Serializer.SerializeJson(searchReq, true);
            byte[] bytes = await _Sdk.PostRaw(url, Encoding.UTF8.GetBytes(json), "application/json", token).ConfigureAwait(false);

            if (bytes != null && bytes.Length > 0)
            {
                List<VectorSearchResult> result = Serializer.DeserializeJson<List<VectorSearchResult>>(Encoding.UTF8.GetString(bytes));
                return result;
            }

            return null;
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<VectorMetadata>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.TenantGUID == null) throw new ArgumentNullException(nameof(query.TenantGUID));
            string url = _Sdk.Endpoint + "v2.0/tenants/" + query.TenantGUID.Value + "/vectors";
            return await _Sdk.Post<EnumerationRequest, EnumerationResult<VectorMetadata>>(url, query, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<VectorMetadata>> ReadAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/vectors/all";
            return await _Sdk.GetMany<VectorMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<VectorMetadata>> ReadAllInGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/vectors/all";
            return await _Sdk.GetMany<VectorMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<VectorMetadata>> ReadManyGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/vectors";
            return await _Sdk.GetMany<VectorMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<VectorMetadata>> ReadManyNode(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/" + nodeGuid + "/vectors";
            return await _Sdk.GetMany<VectorMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<VectorMetadata>> ReadManyEdge(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/edges/" + edgeGuid + "/vectors";
            return await _Sdk.GetMany<VectorMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/vectors/all";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/vectors/all";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteGraphVectors(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/vectors";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteNodeVectors(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/" + nodeGuid + "/vectors";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteEdgeVectors(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/edges/" + edgeGuid + "/vectors";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
