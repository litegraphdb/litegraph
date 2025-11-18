namespace LiteGraph.GraphRepositories.Sqlite.Implementations
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
        public async Task<TagMetadata> Create(TagMetadata tag, CancellationToken token = default)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            if (string.IsNullOrEmpty(tag.Key)) throw new ArgumentException("The supplied tag key is null or empty.");
            token.ThrowIfCancellationRequested();
            string createQuery = TagQueries.Insert(tag);
            DataTable createResult = await _Repo.ExecuteQueryAsync(createQuery, true, token).ConfigureAwait(false);
            TagMetadata created = Converters.TagFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public async Task<List<TagMetadata>> CreateMany(Guid tenantGuid, List<TagMetadata> tags, CancellationToken token = default)
        {
            if (tags == null || tags.Count < 1) return new List<TagMetadata>();
            token.ThrowIfCancellationRequested();

            foreach (TagMetadata Tag in tags)
            {
                Tag.TenantGUID = tenantGuid;
            }

            string insertQuery = TagQueries.InsertMany(tenantGuid, tags);
            string retrieveQuery = TagQueries.SelectMany(tenantGuid, tags.Select(n => n.GUID).ToList());
            DataTable createResult = await _Repo.ExecuteQueryAsync(insertQuery, true, token).ConfigureAwait(false);
            DataTable retrieveResult = await _Repo.ExecuteQueryAsync(retrieveQuery, true, token).ConfigureAwait(false);
            List<TagMetadata> created = Converters.TagsFromDataTable(retrieveResult);
            return created;
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(TagQueries.Delete(tenantGuid, guid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteMany(Guid tenantGuid, Guid? graphGuid, List<Guid> nodeGuids, List<Guid> edgeGuids, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(TagQueries.DeleteMany(tenantGuid, graphGuid, nodeGuids, edgeGuids), false, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteMany(Guid tenantGuid, List<Guid> guids, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(TagQueries.DeleteMany(tenantGuid, guids), false, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(TagQueries.DeleteAllInTenant(tenantGuid), false, token).ConfigureAwait(false);
        }
        
        /// <inheritdoc />
        public async Task DeleteAllInGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(TagQueries.DeleteAllInGraph(tenantGuid, graphGuid), false, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteGraphTags(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(TagQueries.DeleteGraph(tenantGuid, graphGuid), false, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteNodeTags(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await DeleteMany(tenantGuid, graphGuid, new List<Guid> { nodeGuid }, null, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteEdgeTags(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await DeleteMany(tenantGuid, graphGuid, null, new List<Guid> { edgeGuid }, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid tagGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            TagMetadata tag = await ReadByGuid(tenantGuid, tagGuid, token).ConfigureAwait(false);
            return tag != null;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<TagMetadata> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(TagQueries.SelectAllInTenant(tenantGuid, _Repo.SelectBatchSize, skip, order), false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    TagMetadata tag = Converters.TagFromDataRow(result.Rows[i]);
                    yield return tag;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
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
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(TagQueries.SelectAllInGraph(tenantGuid, graphGuid, _Repo.SelectBatchSize, skip, order), false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    TagMetadata tag = Converters.TagFromDataRow(result.Rows[i]);
                    yield return tag;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async Task<TagMetadata> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(TagQueries.SelectByGuid(tenantGuid, guid), false, token).ConfigureAwait(false);
            if (result != null && result.Rows.Count == 1) return Converters.TagFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<TagMetadata> ReadByGuids(Guid tenantGuid, List<Guid> guids, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (guids == null || guids.Count < 1) yield break;
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(TagQueries.SelectByGuids(tenantGuid, guids), false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) yield break;

            for (int i = 0; i < result.Rows.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                yield return Converters.TagFromDataRow(result.Rows[i]);
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
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                token.ThrowIfCancellationRequested();
                string query = null;
                if (graphGuid == null)
                {
                    query = TagQueries.SelectTenant(tenantGuid, key, val, _Repo.SelectBatchSize, skip, order);
                }
                else
                {
                    if (edgeGuid != null)
                    {
                        query = TagQueries.SelectEdge(tenantGuid, graphGuid.Value, edgeGuid.Value, key, val, _Repo.SelectBatchSize, skip, order);
                    }
                    else if (nodeGuid != null)
                    {
                        query = TagQueries.SelectNode(tenantGuid, graphGuid.Value, nodeGuid.Value, key, val, _Repo.SelectBatchSize, skip, order);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(key) && string.IsNullOrEmpty(val))
                        {
                            query = TagQueries.SelectAllInGraph(tenantGuid, graphGuid.Value, _Repo.SelectBatchSize, skip, order);
                        }
                        else
                        {
                            query = TagQueries.SelectGraph(tenantGuid, graphGuid.Value, key, val, _Repo.SelectBatchSize, skip, order);
                        }
                    }
                }

                DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    yield return Converters.TagFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<TagMetadata> ReadManyGraph(Guid tenantGuid, Guid graphGuid, EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, int skip = 0, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                token.ThrowIfCancellationRequested();
                string query = TagQueries.SelectGraph(tenantGuid, graphGuid, null, null, _Repo.SelectBatchSize, skip, order);
                DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    yield return Converters.TagFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<TagMetadata> ReadManyNode(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, int skip = 0, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                token.ThrowIfCancellationRequested();
                string query = TagQueries.SelectNode(tenantGuid, graphGuid, nodeGuid, null, null, _Repo.SelectBatchSize, skip, order);
                DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    yield return Converters.TagFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<TagMetadata> ReadManyEdge(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, int skip = 0, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                token.ThrowIfCancellationRequested();
                string query = TagQueries.SelectEdge(tenantGuid, graphGuid, edgeGuid, null, null, _Repo.SelectBatchSize, skip, order);
                DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    yield return Converters.TagFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<TagMetadata>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            token.ThrowIfCancellationRequested();

            TagMetadata marker = null;

            if (query.TenantGUID != null && query.ContinuationToken != null)
            {
                marker = await ReadByGuid(query.TenantGUID.Value, query.ContinuationToken.Value, token).ConfigureAwait(false);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + query.ContinuationToken.Value + " could not be found.");
            }

            EnumerationResult<TagMetadata> ret = new EnumerationResult<TagMetadata>
            {
                MaxResults = query.MaxResults
            };

            ret.Timestamp.Start = DateTime.UtcNow;
            ret.TotalRecords = await GetRecordCount(query.TenantGUID, query.GraphGUID, query.Ordering, null, token).ConfigureAwait(false);

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
                DataTable result = await _Repo.ExecuteQueryAsync(TagQueries.GetRecordPage(
                    query.TenantGUID,
                    query.GraphGUID,
                    query.MaxResults,
                    query.Skip,
                    query.Ordering,
                    marker), false, token).ConfigureAwait(false);

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

                    ret.RecordsRemaining = await GetRecordCount(query.TenantGUID, query.GraphGUID, query.Ordering, lastItem.GUID, token).ConfigureAwait(false);

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
        public async Task<int> GetRecordCount(
            Guid? tenantGuid,
            Guid? graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Guid? markerGuid = null,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            TagMetadata marker = null;
            if (tenantGuid != null && graphGuid != null && markerGuid != null)
            {
                marker = await ReadByGuid(tenantGuid.Value, markerGuid.Value, token).ConfigureAwait(false);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + markerGuid.Value + " could not be found.");
            }

            DataTable result = await _Repo.ExecuteQueryAsync(TagQueries.GetRecordCount(
                tenantGuid,
                graphGuid,
                order,
                marker), false, token).ConfigureAwait(false);

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
        public async Task<TagMetadata> Update(TagMetadata tag, CancellationToken token = default)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));
            if (string.IsNullOrEmpty(tag.Key)) throw new ArgumentException("The supplied tag key is null or empty.");
            token.ThrowIfCancellationRequested();

            string updateQuery = TagQueries.Update(tag);
            DataTable updateResult = await _Repo.ExecuteQueryAsync(updateQuery, true, token).ConfigureAwait(false);
            TagMetadata updated = Converters.TagFromDataRow(updateResult.Rows[0]);
            return updated;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
