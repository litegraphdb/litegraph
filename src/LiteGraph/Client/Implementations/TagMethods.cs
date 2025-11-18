namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Client.Interfaces;
    using LiteGraph.GraphRepositories;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Serialization;

    using LoggingSettings = LoggingSettings;

    /// <summary>
    /// Tag methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public class TagMethods : ITagMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphClient _Client = null;
        private GraphRepositoryBase _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Tag methods.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="repo">Graph repository.</param>
        public TagMethods(LiteGraphClient client, GraphRepositoryBase repo)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<TagMetadata> Create(TagMetadata tag, CancellationToken token = default)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            token.ThrowIfCancellationRequested();

            await _Client.ValidateTenantExists(tag.TenantGUID, token).ConfigureAwait(false);
            await _Client.ValidateGraphExists(tag.TenantGUID, tag.GraphGUID, token).ConfigureAwait(false);
            if (tag.NodeGUID != null) await _Client.ValidateNodeExists(tag.TenantGUID, tag.NodeGUID.Value, token).ConfigureAwait(false);
            if (tag.EdgeGUID != null) await _Client.ValidateEdgeExists(tag.TenantGUID, tag.EdgeGUID.Value, token).ConfigureAwait(false);
            TagMetadata created = await _Repo.Tag.Create(tag, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "created tag " + created.GUID);
            return created;
        }

        /// <inheritdoc />
        public async Task<List<TagMetadata>> CreateMany(Guid tenantGuid, List<TagMetadata> tags, CancellationToken token = default)
        {
            if (tags == null || tags.Count < 1) return null;
            token.ThrowIfCancellationRequested();

            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);

            foreach (TagMetadata tag in tags)
            {
                await _Client.ValidateGraphExists(tenantGuid, tag.GraphGUID, token).ConfigureAwait(false);

                if (string.IsNullOrEmpty(tag.Key)) throw new ArgumentException("The supplied tag key is null or empty.");

                tag.TenantGUID = tenantGuid;
            }

            tags = await _Repo.Tag.CreateMany(tenantGuid, tags, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "created " + tags.Count + " tag(s) in tenant " + tenantGuid);
            return tags;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<TagMetadata> ReadAllInTenant(
            Guid tenantGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);

            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tags");

            await foreach (TagMetadata tag in _Repo.Tag.ReadAllInTenant(tenantGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return tag;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<TagMetadata> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tags");

            await foreach (TagMetadata tag in _Repo.Tag.ReadAllInGraph(tenantGuid, graphGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return tag;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<TagMetadata> ReadMany(
            Guid tenantGuid,
            Guid? graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
            string key,
            string val,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tags");

            await foreach (TagMetadata tag in _Repo.Tag.ReadMany(tenantGuid, graphGuid, nodeGuid, edgeGuid, key, val, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return tag;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<TagMetadata> ReadManyGraph(
            Guid tenantGuid, 
            Guid graphGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tags");

            await foreach (TagMetadata tag in _Repo.Tag.ReadManyGraph(tenantGuid, graphGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return tag;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<TagMetadata> ReadManyNode(
            Guid tenantGuid, 
            Guid graphGuid, 
            Guid nodeGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tags");

            await foreach (TagMetadata tag in _Repo.Tag.ReadManyNode(tenantGuid, graphGuid, nodeGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return tag;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<TagMetadata> ReadManyEdge(
            Guid tenantGuid, 
            Guid graphGuid, 
            Guid edgeGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tags");

            await foreach (TagMetadata tag in _Repo.Tag.ReadManyEdge(tenantGuid, graphGuid, edgeGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return tag;
            }
        }

        /// <inheritdoc />
        public async Task<TagMetadata> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tag with GUID " + guid);

            return await _Repo.Tag.ReadByGuid(tenantGuid, guid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<TagMetadata> ReadByGuids(Guid tenantGuid, List<Guid> guids, [EnumeratorCancellation] CancellationToken token = default)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tags");
            await foreach (TagMetadata obj in _Repo.Tag.ReadByGuids(tenantGuid, guids, token).WithCancellation(token).ConfigureAwait(false))
            {
                yield return obj;
            }
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<TagMetadata>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) query = new EnumerationRequest();
            return await _Repo.Tag.Enumerate(query, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<TagMetadata> Update(TagMetadata tag, CancellationToken token = default)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            token.ThrowIfCancellationRequested();

            await _Client.ValidateTenantExists(tag.TenantGUID, token).ConfigureAwait(false);
            await _Client.ValidateGraphExists(tag.TenantGUID, tag.GraphGUID, token).ConfigureAwait(false);
            if (tag.NodeGUID != null) await _Client.ValidateNodeExists(tag.TenantGUID, tag.NodeGUID.Value, token).ConfigureAwait(false);
            if (tag.EdgeGUID != null) await _Client.ValidateEdgeExists(tag.TenantGUID, tag.EdgeGUID.Value, token).ConfigureAwait(false);
            tag = await _Repo.Tag.Update(tag, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Debug, "updated tag " + tag.GUID);
            return tag;
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            TagMetadata tag = await ReadByGuid(tenantGuid, guid, token).ConfigureAwait(false);
            if (tag == null) return;
            await _Repo.Tag.DeleteByGuid(tenantGuid, guid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted tag " + tag.GUID);
        }

        /// <inheritdoc />
        public async Task DeleteMany(Guid tenantGuid, Guid? graphGuid, List<Guid> nodeGuids, List<Guid> edgeGuids, CancellationToken token = default)
        {
            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            await _Repo.Tag.DeleteMany(tenantGuid, graphGuid, nodeGuids, edgeGuids, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted tags in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public async Task DeleteMany(Guid tenantGuid, List<Guid> guids, CancellationToken token = default)
        {
            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            await _Repo.Tag.DeleteMany(tenantGuid, guids, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted tags in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            await _Client.ValidateTenantExists(tenantGuid, token).ConfigureAwait(false);
            await _Repo.Tag.DeleteAllInTenant(tenantGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted tags in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public async Task DeleteAllInGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            await _Repo.Tag.DeleteAllInGraph(tenantGuid, graphGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted tags in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public async Task DeleteGraphTags(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            await _Repo.Tag.DeleteGraphTags(tenantGuid, graphGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted tags in graph " + graphGuid);
        }

        /// <inheritdoc />
        public async Task DeleteNodeTags(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default)
        {
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            await _Client.ValidateNodeExists(tenantGuid, nodeGuid, token).ConfigureAwait(false);
            await _Repo.Tag.DeleteNodeTags(tenantGuid, graphGuid, nodeGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted tags for node " + nodeGuid);
        }

        /// <inheritdoc />
        public async Task DeleteEdgeTags(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, CancellationToken token = default)
        {
            await _Client.ValidateGraphExists(tenantGuid, graphGuid, token).ConfigureAwait(false);
            await _Client.ValidateEdgeExists(tenantGuid, edgeGuid, token).ConfigureAwait(false);
            await _Repo.Tag.DeleteEdgeTags(tenantGuid, graphGuid, edgeGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted tags for edge " + edgeGuid);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            return await _Repo.Tag.ExistsByGuid(tenantGuid, guid, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
