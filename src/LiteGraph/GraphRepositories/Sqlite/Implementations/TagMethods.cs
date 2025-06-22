namespace LiteGraph.GraphRepositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Serialization;

    using LoggingSettings = LoggingSettings;

    /// <summary>
    /// Tag methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public class TagMethods : ITagMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SqliteGraphRepository _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Tag methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public TagMethods(SqliteGraphRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public TagMetadata Create(TagMetadata tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            if (string.IsNullOrEmpty(tag.Key)) throw new ArgumentException("The supplied tag key is null or empty.");
            string createQuery = TagQueries.Insert(tag);
            DataTable createResult = createResult = _Repo.ExecuteQuery(createQuery, true);
            TagMetadata created = Converters.TagFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public List<TagMetadata> CreateMany(Guid tenantGuid, List<TagMetadata> tags)
        {
            if (tags == null || tags.Count < 1) return new List<TagMetadata>();

            foreach (TagMetadata Tag in tags)
            {
                Tag.TenantGUID = tenantGuid;
            }

            string insertQuery = TagQueries.InsertMany(tenantGuid, tags);
            string retrieveQuery = TagQueries.SelectMany(tenantGuid, tags.Select(n => n.GUID).ToList());
            DataTable createResult = _Repo.ExecuteQuery(insertQuery, true);
            DataTable retrieveResult = _Repo.ExecuteQuery(retrieveQuery, true);
            List<TagMetadata> created = Converters.TagsFromDataTable(retrieveResult);
            return created;
        }

        /// <inheritdoc />
        public void DeleteByGuid(Guid tenantGuid, Guid guid)
        {
            _Repo.ExecuteQuery(TagQueries.Delete(tenantGuid, guid), true);
        }

        /// <inheritdoc />
        public void DeleteMany(Guid tenantGuid, Guid? graphGuid, List<Guid> nodeGuids, List<Guid> edgeGuids)
        {
            _Repo.ExecuteQuery(TagQueries.DeleteMany(tenantGuid, graphGuid, nodeGuids, edgeGuids));
        }

        /// <inheritdoc />
        public void DeleteMany(Guid tenantGuid, List<Guid> guids)
        {
            _Repo.ExecuteQuery(TagQueries.DeleteMany(tenantGuid, guids));
        }

        /// <inheritdoc />
        public void DeleteAllInTenant(Guid tenantGuid)
        {
            _Repo.ExecuteQuery(TagQueries.DeleteAllInTenant(tenantGuid));
        }
        
        /// <inheritdoc />
        public void DeleteAllInGraph(Guid tenantGuid, Guid graphGuid)
        {
            _Repo.ExecuteQuery(TagQueries.DeleteAllInGraph(tenantGuid, graphGuid));
        }

        /// <inheritdoc />
        public void DeleteGraphTags(Guid tenantGuid, Guid graphGuid)
        {
            _Repo.ExecuteQuery(TagQueries.DeleteGraph(tenantGuid, graphGuid));
        }

        /// <inheritdoc />
        public void DeleteNodeTags(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            _Repo.Tag.DeleteMany(tenantGuid, graphGuid, new List<Guid> { nodeGuid }, null);
        }

        /// <inheritdoc />
        public void DeleteEdgeTags(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            _Repo.Label.DeleteMany(tenantGuid, graphGuid, null, new List<Guid> { edgeGuid });
        }

        /// <inheritdoc />
        public bool ExistsByGuid(Guid tenantGuid, Guid tagGuid)
        {
            return (ReadByGuid(tenantGuid, tagGuid) != null);
        }

        /// <inheritdoc />
        public IEnumerable<TagMetadata> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(TagQueries.SelectAllInTenant(tenantGuid, _Repo.SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    TagMetadata tag = Converters.TagFromDataRow(result.Rows[i]);
                    yield return tag;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public IEnumerable<TagMetadata> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(TagQueries.SelectAllInGraph(tenantGuid, graphGuid, _Repo.SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    TagMetadata tag = Converters.TagFromDataRow(result.Rows[i]);
                    yield return tag;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public TagMetadata ReadByGuid(Guid tenantGuid, Guid guid)
        {
            DataTable result = _Repo.ExecuteQuery(TagQueries.Select(tenantGuid, guid));
            if (result != null && result.Rows.Count == 1) return Converters.TagFromDataRow(result.Rows[0]);
            return null;
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
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                string query = null;
                if (graphGuid == null)
                {
                    query = TagQueries.SelectTenant(tenantGuid, key, val, _Repo.SelectBatchSize, skip, order);
                }
                else
                {
                    if (edgeGuid != null) query = TagQueries.SelectEdge(tenantGuid, graphGuid.Value, edgeGuid.Value, key, val, _Repo.SelectBatchSize, skip, order);
                    else if (nodeGuid != null) query = TagQueries.SelectNode(tenantGuid, graphGuid.Value, nodeGuid.Value, key, val, _Repo.SelectBatchSize, skip, order);
                    else query = TagQueries.SelectGraph(tenantGuid, graphGuid.Value, key, val, _Repo.SelectBatchSize, skip, order);
                }

                DataTable result = _Repo.ExecuteQuery(query);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.TagFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public IEnumerable<TagMetadata> ReadManyGraph(Guid tenantGuid, Guid graphGuid, EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                string query = TagQueries.SelectGraph(tenantGuid, graphGuid, null, null, _Repo.SelectBatchSize, skip, order);
                DataTable result = _Repo.ExecuteQuery(query);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.TagFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public IEnumerable<TagMetadata> ReadManyNode(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                string query = TagQueries.SelectNode(tenantGuid, graphGuid, nodeGuid, null, null, _Repo.SelectBatchSize, skip, order);
                DataTable result = _Repo.ExecuteQuery(query);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.TagFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public IEnumerable<TagMetadata> ReadManyEdge(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                string query = TagQueries.SelectEdge(tenantGuid, graphGuid, edgeGuid, null, null, _Repo.SelectBatchSize, skip, order);
                DataTable result = _Repo.ExecuteQuery(query);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.TagFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public EnumerationResult<TagMetadata> Enumerate(EnumerationQuery query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            TagMetadata marker = null;

            if (query.TenantGUID != null && query.ContinuationToken != null)
            {
                marker = ReadByGuid(query.TenantGUID.Value, query.ContinuationToken.Value);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + query.ContinuationToken.Value + " could not be found.");
            }

            EnumerationResult<TagMetadata> ret = new EnumerationResult<TagMetadata>
            {
                MaxResults = query.MaxResults
            };

            ret.Timestamp.Start = DateTime.UtcNow;
            ret.TotalRecords = GetRecordCount(query.TenantGUID, query.GraphGUID, query.Ordering, query.ContinuationToken);

            if (ret.TotalRecords < 1)
            {
                ret.ContinuationToken = null;
                ret.EndOfResults = true;
                ret.RecordsRemaining = 0;
                ret.Timestamp.End = DateTime.UtcNow;
                return ret;
            }
            else
            {
                DataTable result = _Repo.ExecuteQuery(TagQueries.GetRecordPage(
                    query.TenantGUID,
                    query.GraphGUID,
                    query.MaxResults,
                    query.Ordering,
                    marker));

                if (result == null || result.Rows.Count < 1)
                {
                    ret.ContinuationToken = null;
                    ret.EndOfResults = true;
                    ret.RecordsRemaining = 0;
                    ret.Timestamp.End = DateTime.UtcNow;
                    return ret;
                }
                else
                {
                    ret.Objects = Converters.TagsFromDataTable(result);

                    TagMetadata lastItem = ret.Objects.Last();

                    ret.RecordsRemaining = GetRecordCount(query.TenantGUID, query.GraphGUID, query.Ordering, lastItem.GUID);

                    if (ret.RecordsRemaining > 0)
                    {
                        ret.ContinuationToken = lastItem.GUID;
                        ret.EndOfResults = false;
                        ret.Timestamp.End = DateTime.UtcNow;
                        return ret;
                    }
                    else
                    {
                        ret.ContinuationToken = null;
                        ret.EndOfResults = true;
                        ret.Timestamp.End = DateTime.UtcNow;
                        return ret;
                    }
                }
            }
        }

        /// <inheritdoc />
        public int GetRecordCount(
            Guid? tenantGuid,
            Guid? graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Guid? markerGuid = null)
        {
            TagMetadata marker = null;
            if (tenantGuid != null && graphGuid != null && markerGuid != null)
            {
                marker = ReadByGuid(tenantGuid.Value, markerGuid.Value);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + markerGuid.Value + " could not be found.");
            }

            DataTable result = _Repo.ExecuteQuery(TagQueries.GetRecordCount(
                tenantGuid,
                graphGuid,
                order,
                marker));

            if (result != null && result.Rows != null && result.Rows.Count > 0)
            {
                if (result.Columns.Contains("record_count"))
                {
                    return Convert.ToInt32(result.Rows[0]["record_count"]);
                }
            }
            return 0;
        }

        /// <inheritdoc />
        public TagMetadata Update(TagMetadata tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            if (string.IsNullOrEmpty(tag.Key)) throw new ArgumentException("The supplied tag key is null or empty.");

            string updateQuery = TagQueries.Update(tag);
            DataTable updateResult = _Repo.ExecuteQuery(updateQuery, true);
            TagMetadata updated = Converters.TagFromDataRow(updateResult.Rows[0]);
            return updated;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
