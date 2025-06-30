namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Runtime.Serialization.Json;
    using System.Text;
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
        public TagMetadata Create(TagMetadata tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));

            _Client.ValidateTenantExists(tag.TenantGUID);
            _Client.ValidateGraphExists(tag.TenantGUID, tag.GraphGUID);
            if (tag.NodeGUID != null) _Client.ValidateNodeExists(tag.TenantGUID, tag.NodeGUID.Value);
            if (tag.EdgeGUID != null) _Client.ValidateEdgeExists(tag.TenantGUID, tag.EdgeGUID.Value);
            TagMetadata created = _Repo.Tag.Create(tag);
            _Client.Logging.Log(SeverityEnum.Info, "created tag " + created.GUID);
            return created;
        }

        /// <inheritdoc />
        public List<TagMetadata> CreateMany(Guid tenantGuid, List<TagMetadata> tags)
        {
            if (tags == null || tags.Count < 1) return null;

            _Client.ValidateTenantExists(tenantGuid);

            foreach (TagMetadata tag in tags)
            {
                _Client.ValidateGraphExists(tenantGuid, tag.GraphGUID);

                if (string.IsNullOrEmpty(tag.Key)) throw new ArgumentException("The supplied tag key is null or empty.");

                tag.TenantGUID = tenantGuid;
            }

            tags = _Repo.Tag.CreateMany(tenantGuid, tags);
            _Client.Logging.Log(SeverityEnum.Info, "created " + tags.Count + " tag(s) in tenant " + tenantGuid);
            return tags;
        }

        /// <inheritdoc />
        public IEnumerable<TagMetadata> ReadAllInTenant(
            Guid tenantGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0)
        {
            _Client.ValidateTenantExists(tenantGuid);

            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tags");

            foreach (TagMetadata tag in _Repo.Tag.ReadAllInTenant(tenantGuid, order, skip))
            {
                yield return tag;
            }
        }

        /// <inheritdoc />
        public IEnumerable<TagMetadata> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tags");

            foreach (TagMetadata tag in _Repo.Tag.ReadAllInGraph(tenantGuid, graphGuid, order, skip))
            {
                yield return tag;
            }
        }

        /// <inheritdoc />
        public IEnumerable<TagMetadata> ReadMany(
            Guid tenantGuid,
            Guid? graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
            string key,
            string val,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tags");

            foreach (TagMetadata tag in _Repo.Tag.ReadMany(tenantGuid, graphGuid, nodeGuid, edgeGuid, key, val, order, skip))
            {
                yield return tag;
            }
        }

        /// <inheritdoc />
        public IEnumerable<TagMetadata> ReadManyGraph(
            Guid tenantGuid, 
            Guid graphGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tags");

            foreach (TagMetadata tag in _Repo.Tag.ReadManyGraph(tenantGuid, graphGuid, order, skip))
            {
                yield return tag;
            }
        }

        /// <inheritdoc />
        public IEnumerable<TagMetadata> ReadManyNode(
            Guid tenantGuid, 
            Guid graphGuid, 
            Guid nodeGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tags");

            foreach (TagMetadata tag in _Repo.Tag.ReadManyNode(tenantGuid, graphGuid, nodeGuid, order, skip))
            {
                yield return tag;
            }
        }

        /// <inheritdoc />
        public IEnumerable<TagMetadata> ReadManyEdge(
            Guid tenantGuid, 
            Guid graphGuid, 
            Guid edgeGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tags");

            foreach (TagMetadata tag in _Repo.Tag.ReadManyEdge(tenantGuid, graphGuid, edgeGuid, order, skip))
            {
                yield return tag;
            }
        }

        /// <inheritdoc />
        public TagMetadata ReadByGuid(Guid tenantGuid, Guid guid)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tag with GUID " + guid);

            return _Repo.Tag.ReadByGuid(tenantGuid, guid);
        }

        /// <inheritdoc />
        public IEnumerable<TagMetadata> ReadByGuids(Guid tenantGuid, List<Guid> guids)
        {
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving tags");
            foreach (TagMetadata obj in _Repo.Tag.ReadByGuids(tenantGuid, guids))
            {
                yield return obj;
            }
        }

        /// <inheritdoc />
        public EnumerationResult<TagMetadata> Enumerate(EnumerationQuery query)
        {
            if (query == null) query = new EnumerationQuery();
            return _Repo.Tag.Enumerate(query);
        }

        /// <inheritdoc />
        public TagMetadata Update(TagMetadata tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));

            _Client.ValidateTenantExists(tag.TenantGUID);
            _Client.ValidateGraphExists(tag.TenantGUID, tag.GraphGUID);
            if (tag.NodeGUID != null) _Client.ValidateNodeExists(tag.TenantGUID, tag.NodeGUID.Value);
            if (tag.EdgeGUID != null) _Client.ValidateEdgeExists(tag.TenantGUID, tag.EdgeGUID.Value);
            tag = _Repo.Tag.Update(tag);
            _Client.Logging.Log(SeverityEnum.Debug, "updated tag " + tag.GUID);
            return tag;
        }

        /// <inheritdoc />
        public void DeleteByGuid(Guid tenantGuid, Guid guid)
        {
            _Client.ValidateTenantExists(tenantGuid);
            TagMetadata tag = ReadByGuid(tenantGuid, guid);
            if (tag == null) return;
            _Repo.Tag.DeleteByGuid(tenantGuid, guid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted tag " + tag.GUID);
        }

        /// <inheritdoc />
        public void DeleteMany(Guid tenantGuid, Guid? graphGuid, List<Guid> nodeGuids, List<Guid> edgeGuids)
        {
            _Client.ValidateTenantExists(tenantGuid);
            _Repo.Tag.DeleteMany(tenantGuid, graphGuid, nodeGuids, edgeGuids);
            _Client.Logging.Log(SeverityEnum.Info, "deleted tags in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public void DeleteMany(Guid tenantGuid, List<Guid> guids)
        {
            _Client.ValidateTenantExists(tenantGuid);
            _Repo.Tag.DeleteMany(tenantGuid, guids);
            _Client.Logging.Log(SeverityEnum.Info, "deleted tags in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public void DeleteAllInTenant(Guid tenantGuid)
        {
            _Client.ValidateTenantExists(tenantGuid);
            _Repo.Tag.DeleteAllInTenant(tenantGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted tags in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public void DeleteAllInGraph(Guid tenantGuid, Guid graphGuid)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Repo.Tag.DeleteAllInGraph(tenantGuid, graphGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted tags in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public void DeleteGraphTags(Guid tenantGuid, Guid graphGuid)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Repo.Tag.DeleteGraphTags(tenantGuid, graphGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted tags in graph " + graphGuid);
        }

        /// <inheritdoc />
        public void DeleteNodeTags(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Client.ValidateNodeExists(tenantGuid, nodeGuid);
            _Repo.Tag.DeleteNodeTags(tenantGuid, graphGuid, nodeGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted tags for node " + nodeGuid);
        }

        /// <inheritdoc />
        public void DeleteEdgeTags(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            _Client.ValidateGraphExists(tenantGuid, graphGuid);
            _Client.ValidateEdgeExists(tenantGuid, edgeGuid);
            _Repo.Tag.DeleteEdgeTags(tenantGuid, graphGuid, edgeGuid);
            _Client.Logging.Log(SeverityEnum.Info, "deleted tags for edge " + edgeGuid);
        }

        /// <inheritdoc />
        public bool ExistsByGuid(Guid tenantGuid, Guid guid)
        {
            return _Repo.Tag.ExistsByGuid(tenantGuid, guid);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
