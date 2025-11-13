namespace LiteGraph.GraphRepositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;

    using LoggingSettings = LoggingSettings;

    /// <summary>
    /// Label methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public class LabelMethods : ILabelMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SqliteGraphRepository _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Label methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public LabelMethods(SqliteGraphRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<LabelMetadata> Create(LabelMetadata label, CancellationToken token = default)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));
            if (string.IsNullOrEmpty(label.Label)) throw new ArgumentException("The supplied label is null or empty.");
            token.ThrowIfCancellationRequested();
            string createQuery = LabelQueries.Insert(label);
            DataTable createResult = await _Repo.ExecuteQueryAsync(createQuery, true, token).ConfigureAwait(false);
            LabelMetadata created = Converters.LabelFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public async Task<List<LabelMetadata>> CreateMany(Guid tenantGuid, List<LabelMetadata> labels, CancellationToken token = default)
        {
            if (labels == null || labels.Count < 1) return null;
            token.ThrowIfCancellationRequested();
            string createQuery = LabelQueries.InsertMany(tenantGuid, labels);
            string retrieveQuery = LabelQueries.SelectMany(tenantGuid, labels.Select(n => n.GUID).ToList());
            DataTable createResult = await _Repo.ExecuteQueryAsync(createQuery, true, token).ConfigureAwait(false);
            DataTable retrieveResult = await _Repo.ExecuteQueryAsync(retrieveQuery, true, token).ConfigureAwait(false);
            List<LabelMetadata> created = Converters.LabelsFromDataTable(retrieveResult);
            return created;
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(LabelQueries.Delete(tenantGuid, guid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteMany(Guid tenantGuid, Guid? graphGuid, List<Guid> nodeGuids, List<Guid> edgeGuids, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(LabelQueries.DeleteMany(tenantGuid, graphGuid, nodeGuids, edgeGuids), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteMany(Guid tenantGuid, List<Guid> guids, CancellationToken token = default)
        {
            if (guids == null || guids.Count < 1) return;
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(LabelQueries.DeleteMany(tenantGuid, guids), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(LabelQueries.DeleteAllInTenant(tenantGuid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(LabelQueries.DeleteAllInGraph(tenantGuid, graphGuid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteGraphLabels(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(LabelQueries.DeleteGraph(tenantGuid, graphGuid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteNodeLabels(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await DeleteMany(tenantGuid, graphGuid, new List<Guid> { nodeGuid }, null, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteEdgeLabels(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await DeleteMany(tenantGuid, graphGuid, null, new List<Guid> { edgeGuid }, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid labelGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            LabelMetadata label = await ReadByGuid(tenantGuid, labelGuid, token).ConfigureAwait(false);
            return label != null;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<LabelMetadata> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(LabelQueries.SelectAllInTenant(tenantGuid, _Repo.SelectBatchSize, skip, order), false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    LabelMetadata label = Converters.LabelFromDataRow(result.Rows[i]);
                    yield return label;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<LabelMetadata> ReadAllInGraph(
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
                DataTable result = await _Repo.ExecuteQueryAsync(LabelQueries.SelectAllInGraph(tenantGuid, graphGuid, _Repo.SelectBatchSize, skip, order), false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    LabelMetadata label = Converters.LabelFromDataRow(result.Rows[i]);
                    yield return label;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async Task<LabelMetadata> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(LabelQueries.SelectByGuid(tenantGuid, guid), false, token).ConfigureAwait(false);
            if (result != null && result.Rows.Count == 1) return Converters.LabelFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<LabelMetadata> ReadByGuids(Guid tenantGuid, List<Guid> guids, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (guids == null || guids.Count < 1) yield break;
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(LabelQueries.SelectByGuids(tenantGuid, guids), false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) yield break;

            for (int i = 0; i < result.Rows.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                yield return Converters.LabelFromDataRow(result.Rows[i]);
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<LabelMetadata> ReadMany(
            Guid tenantGuid,
            Guid? graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
            string label,
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
                    query = LabelQueries.SelectAllInTenant(tenantGuid, _Repo.SelectBatchSize, skip, order);
                }
                else
                {
                    if (edgeGuid != null)
                    {
                        query = LabelQueries.SelectEdge(
                            tenantGuid,
                            graphGuid.Value,
                            edgeGuid.Value,
                            label,
                            _Repo.SelectBatchSize,
                            skip,
                            order);
                    }
                    else if (nodeGuid != null)
                    {
                        query = LabelQueries.SelectNode(
                            tenantGuid,
                            graphGuid.Value,
                            nodeGuid.Value,
                            label,
                            _Repo.SelectBatchSize,
                            skip,
                            order);
                    }
                    else
                    {
                        query = LabelQueries.SelectGraph(
                            tenantGuid,
                            graphGuid.Value,
                            label,
                            _Repo.SelectBatchSize,
                            skip,
                            order);
                    }
                }

                DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    yield return Converters.LabelFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<LabelMetadata> ReadManyGraph(
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
                string query = LabelQueries.SelectGraph(
                    tenantGuid,
                    graphGuid,
                    null,
                    _Repo.SelectBatchSize,
                    skip,
                    order);

                DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    yield return Converters.LabelFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<LabelMetadata> ReadManyNode(
            Guid tenantGuid, 
            Guid graphGuid, 
            Guid nodeGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                token.ThrowIfCancellationRequested();
                string query = LabelQueries.SelectNode(
                    tenantGuid,
                    graphGuid,
                    nodeGuid,
                    null,
                    _Repo.SelectBatchSize,
                    skip,
                    order);

                DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    yield return Converters.LabelFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<LabelMetadata> ReadManyEdge(
            Guid tenantGuid, 
            Guid graphGuid, 
            Guid edgeGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, 
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                token.ThrowIfCancellationRequested();
                string query = LabelQueries.SelectEdge(
                    tenantGuid,
                    graphGuid,
                    edgeGuid,
                    null,
                    _Repo.SelectBatchSize,
                    skip,
                    order);

                DataTable result = await _Repo.ExecuteQueryAsync(query, token: token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    yield return Converters.LabelFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<LabelMetadata>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            token.ThrowIfCancellationRequested();

            LabelMetadata marker = null;

            if (query.TenantGUID != null && query.ContinuationToken != null && query.GraphGUID != null)
            {
                marker = await ReadByGuid(query.TenantGUID.Value, query.ContinuationToken.Value, token).ConfigureAwait(false);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + query.ContinuationToken.Value + " could not be found.");
            }

            EnumerationResult<LabelMetadata> ret = new EnumerationResult<LabelMetadata>
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
                DataTable result = await _Repo.ExecuteQueryAsync(LabelQueries.GetRecordPage(
                    query.TenantGUID,
                    query.GraphGUID,
                    query.MaxResults,
                    query.Skip,
                    query.Ordering,
                    marker), token: token).ConfigureAwait(false);

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
                    ret.Objects = Converters.LabelsFromDataTable(result);
                    LabelMetadata lastItem = ret.Objects.Last();

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
            LabelMetadata marker = null;
            if (tenantGuid != null && graphGuid != null && markerGuid != null)
            {
                marker = await ReadByGuid(tenantGuid.Value, markerGuid.Value, token).ConfigureAwait(false);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + markerGuid.Value + " could not be found.");
            }

            DataTable result = await _Repo.ExecuteQueryAsync(LabelQueries.GetRecordCount(
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
        public async Task<LabelMetadata> Update(LabelMetadata label, CancellationToken token = default)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));
            if (string.IsNullOrEmpty(label.Label)) throw new ArgumentException("The supplied label is null or empty.");
            token.ThrowIfCancellationRequested();

            string updateQuery = LabelQueries.Update(label);
            DataTable updateResult = await _Repo.ExecuteQueryAsync(updateQuery, true, token).ConfigureAwait(false);
            LabelMetadata updated = Converters.LabelFromDataRow(updateResult.Rows[0]);
            return updated;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
