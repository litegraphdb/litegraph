namespace LiteGraph.Sdk.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using LiteGraph.Sdk.Interfaces;

    /// <summary>
    /// Tag methods.
    /// </summary>
    public class TagMethods : ITagMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphSdk _Sdk = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Tag methods.
        /// </summary>
        /// <param name="sdk">LiteGraph SDK.</param>
        public TagMethods(LiteGraphSdk sdk)
        {
            _Sdk = sdk ?? throw new ArgumentNullException(nameof(sdk));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<TagMetadata> Create(TagMetadata tag, CancellationToken token = default)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tag.TenantGUID + "/tags";
            return await _Sdk.PutCreate<TagMetadata>(url, tag, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<TagMetadata>> CreateMany(Guid tenantGuid, List<TagMetadata> tags, CancellationToken token = default)
        {
            if (tags == null || tags.Count < 1) throw new ArgumentNullException(nameof(tags));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/tags/bulk";
            return await _Sdk.PutCreate<List<TagMetadata>>(url, tags, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<TagMetadata>> ReadMany(
            Guid tenantGuid,
            Guid? graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0, 
            CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/tags?skip=" + skip + "&order=" + order.ToString();
            return await _Sdk.GetMany<TagMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<TagMetadata> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/tags/" + guid;
            return await _Sdk.Get<TagMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<TagMetadata>> ReadByGuids(Guid tenantGuid, List<Guid> guids, CancellationToken token = default)
        {
            if (guids == null || guids.Count < 1) throw new ArgumentNullException(nameof(guids));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/tags?guids=" + string.Join(",", guids);
            return await _Sdk.Get<List<TagMetadata>>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<TagMetadata> Update(TagMetadata tag, CancellationToken token = default)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tag.TenantGUID + "/tags/" + tag.GUID;
            return await _Sdk.PutUpdate<TagMetadata>(url, tag, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/tags/" + guid;
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteMany(Guid tenantGuid, List<Guid> guids, CancellationToken token = default)
        {
            if (guids == null || guids.Count < 1) throw new ArgumentNullException(nameof(guids));
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/tags/bulk";
            await _Sdk.Delete<List<Guid>>(url, guids, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/tags/" + guid;
            return await _Sdk.Head(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<TagMetadata>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            if (query.TenantGUID == null) throw new ArgumentNullException(nameof(query.TenantGUID));
            string url = _Sdk.Endpoint + "v2.0/tenants/" + query.TenantGUID.Value + "/tags";
            return await _Sdk.Post<EnumerationRequest, EnumerationResult<TagMetadata>>(url, query, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<TagMetadata>> ReadAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/tags/all";
            return await _Sdk.GetMany<TagMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<TagMetadata>> ReadAllInGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/tags/all";
            return await _Sdk.GetMany<TagMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<TagMetadata>> ReadManyGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/tags";
            return await _Sdk.GetMany<TagMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<TagMetadata>> ReadManyNode(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/" + nodeGuid + "/tags";
            return await _Sdk.GetMany<TagMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<List<TagMetadata>> ReadManyEdge(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/edges/" + edgeGuid + "/tags";
            return await _Sdk.GetMany<TagMetadata>(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/tags/all";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/tags/all";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteGraphTags(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/tags";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteNodeTags(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/nodes/" + nodeGuid + "/tags";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteEdgeTags(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, CancellationToken token = default)
        {
            string url = _Sdk.Endpoint + "v1.0/tenants/" + tenantGuid + "/graphs/" + graphGuid + "/edges/" + edgeGuid + "/tags";
            await _Sdk.Delete(url, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
