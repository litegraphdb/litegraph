namespace LiteGraph.GraphRepositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using ExpressionTree;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Serialization;

    using LoggingSettings = LoggingSettings;

    /// <summary>
    /// Edge methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public class EdgeMethods : IEdgeMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SqliteGraphRepository _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Edge methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public EdgeMethods(SqliteGraphRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<Edge> Create(Edge edge, CancellationToken token = default)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));
            token.ThrowIfCancellationRequested();

            string insertQuery = EdgeQueries.Insert(edge);
            DataTable createResult = await _Repo.ExecuteQueryAsync(insertQuery, true, token).ConfigureAwait(false); 
            Edge created = Converters.EdgeFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public async Task<List<Edge>> CreateMany(Guid tenantGuid, Guid graphGuid, List<Edge> edges, CancellationToken token = default)
        {
            if (edges == null || edges.Count < 1) return new List<Edge>();
            token.ThrowIfCancellationRequested();

            List<Edge> created = new List<Edge>();

            foreach (Edge edge in edges)
            {
                edge.TenantGUID = tenantGuid;
                edge.GraphGUID = graphGuid;
            }

            string insertQuery = EdgeQueries.InsertMany(tenantGuid, edges);
            string retrieveQuery = EdgeQueries.SelectMany(tenantGuid, edges.Select(n => n.GUID).ToList());

            // Execute the entire batch with BEGIN/COMMIT and multi-row INSERTs
            DataTable createResult = await _Repo.ExecuteQueryAsync(insertQuery, true, token).ConfigureAwait(false);
            DataTable retrieveResult = await _Repo.ExecuteQueryAsync(retrieveQuery, true, token).ConfigureAwait(false);
            created = Converters.EdgesFromDataTable(retrieveResult);
            return created;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Edge> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(EdgeQueries.SelectAllInTenant(tenantGuid, _Repo.SelectBatchSize, skip, order), false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Edge edge = Converters.EdgeFromDataRow(result.Rows[i]);
                    yield return edge;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Edge> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(EdgeQueries.SelectAllInGraph(tenantGuid, graphGuid, _Repo.SelectBatchSize, skip, order), false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Edge edge = Converters.EdgeFromDataRow(result.Rows[i]);
                    yield return edge;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Edge> ReadMany(
            Guid tenantGuid,
            Guid graphGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            token.ThrowIfCancellationRequested();

            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(EdgeQueries.SelectMany(
                    tenantGuid, 
                    graphGuid, 
                    name,
                    labels, 
                    tags, 
                    edgeFilter, 
                    _Repo.SelectBatchSize, 
                    skip, 
                    order), false, token).ConfigureAwait(false);

                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Edge edge = Converters.EdgeFromDataRow(result.Rows[i]);
                    yield return edge;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async Task<Edge> ReadFirst(
            Guid tenantGuid,
            Guid graphGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            DataTable result = await _Repo.ExecuteQueryAsync(EdgeQueries.SelectMany(
                tenantGuid, 
                graphGuid, 
                name,
                labels, 
                tags, 
                edgeFilter, 
                1, 
                0, 
                order), false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) return null;

            if (result.Rows.Count > 0)
            {
                return Converters.EdgeFromDataRow(result.Rows[0]);
            }

            return null;
        }

        /// <inheritdoc />
        public async Task<Edge> ReadByGuid(Guid tenantGuid, Guid edgeGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            DataTable result = await _Repo.ExecuteQueryAsync(EdgeQueries.SelectByGuid(tenantGuid, edgeGuid), false, token).ConfigureAwait(false);
            if (result != null && result.Rows.Count == 1)
            {
                Edge edge = Converters.EdgeFromDataRow(result.Rows[0]);
                return edge;
            }
            return null;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Edge> ReadByGuids(Guid tenantGuid, List<Guid> guids, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (guids == null || guids.Count < 1) yield break;
            token.ThrowIfCancellationRequested();

            DataTable result = await _Repo.ExecuteQueryAsync(EdgeQueries.SelectByGuids(tenantGuid, guids), false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) yield break;

            for (int i = 0; i < result.Rows.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                yield return Converters.EdgeFromDataRow(result.Rows[i]);
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Edge> ReadNodeEdges(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            token.ThrowIfCancellationRequested();

            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(EdgeQueries.SelectConnected(tenantGuid, graphGuid, nodeGuid, labels, tags, edgeFilter, _Repo.SelectBatchSize, skip, order), false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Edge edge = Converters.EdgeFromDataRow(result.Rows[i]);
                    yield return edge;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Edge> ReadEdgesFromNode(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            token.ThrowIfCancellationRequested();

            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(EdgeQueries.SelectEdgesFrom(tenantGuid, graphGuid, nodeGuid, labels, tags, edgeFilter, _Repo.SelectBatchSize, skip, order), false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Edge edge = Converters.EdgeFromDataRow(result.Rows[i]);
                    yield return edge;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Edge> ReadEdgesToNode(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            token.ThrowIfCancellationRequested();

            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(EdgeQueries.SelectEdgesTo(tenantGuid, graphGuid, nodeGuid, labels, tags, edgeFilter, _Repo.SelectBatchSize, skip, order), false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Edge edge = Converters.EdgeFromDataRow(result.Rows[i]);
                    yield return edge;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<Edge> ReadEdgesBetweenNodes(
            Guid tenantGuid,
            Guid graphGuid,
            Guid fromNodeGuid,
            Guid toNodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));
            token.ThrowIfCancellationRequested();

            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(EdgeQueries.SelectEdgesBetween(tenantGuid, graphGuid, fromNodeGuid, toNodeGuid, labels, tags, edgeFilter, _Repo.SelectBatchSize, skip, order), false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Edge edge = Converters.EdgeFromDataRow(result.Rows[i]);
                    yield return edge;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<Edge>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            token.ThrowIfCancellationRequested();

            Edge marker = null;

            if (query.TenantGUID != null && query.ContinuationToken != null && query.GraphGUID != null)
            {
                marker = await ReadByGuid(query.TenantGUID.Value, query.ContinuationToken.Value, token).ConfigureAwait(false);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + query.ContinuationToken.Value + " could not be found.");
            }

            EnumerationResult<Edge> ret = new EnumerationResult<Edge>
            {
                MaxResults = query.MaxResults
            };

            ret.Timestamp.Start = DateTime.UtcNow;
            ret.TotalRecords = await GetRecordCount(query.TenantGUID, query.GraphGUID, query.Labels, query.Tags, query.Expr, query.Ordering, null, token).ConfigureAwait(false);

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
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(EdgeQueries.GetRecordPage(
                    query.TenantGUID,
                    query.GraphGUID,
                    query.Labels,
                    query.Tags,
                    query.Expr,
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
                    ret.Objects = Converters.EdgesFromDataTable(result);

                    Edge lastItem = ret.Objects.Last();

                    ret.RecordsRemaining = await GetRecordCount(query.TenantGUID, query.GraphGUID, query.Labels, query.Tags, query.Expr, query.Ordering, lastItem.GUID, token).ConfigureAwait(false);

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
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Guid? markerGuid = null,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();

            Edge marker = null;
            if (tenantGuid != null && graphGuid != null && markerGuid != null)
            {
                marker = await ReadByGuid(tenantGuid.Value, markerGuid.Value, token).ConfigureAwait(false);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + markerGuid.Value + " could not be found.");
            }

            DataTable result = await _Repo.ExecuteQueryAsync(EdgeQueries.GetRecordCount(
                tenantGuid,
                graphGuid,
                labels,
                tags,
                filter,
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
        public async Task<Edge> Update(Edge edge, CancellationToken token = default)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));
            token.ThrowIfCancellationRequested();

            DataTable result = await _Repo.ExecuteQueryAsync(EdgeQueries.Update(edge), true, token).ConfigureAwait(false);
            Edge updated = Converters.EdgeFromDataRow(result.Rows[0]);
            return updated;
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(EdgeQueries.Delete(tenantGuid, graphGuid, edgeGuid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(EdgeQueries.DeleteAllInTenant(tenantGuid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(EdgeQueries.DeleteAllInGraph(tenantGuid, graphGuid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteMany(Guid tenantGuid, Guid graphGuid, List<Guid> edgeGuids, CancellationToken token = default)
        {
            if (edgeGuids == null || edgeGuids.Count < 1) return;
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(EdgeQueries.DeleteMany(tenantGuid, graphGuid, edgeGuids), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteNodeEdges(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default)
        {
            await DeleteNodeEdges(tenantGuid, graphGuid, new List<Guid> { nodeGuid }, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteNodeEdges(Guid tenantGuid, Guid graphGuid, List<Guid> nodeGuids, CancellationToken token = default)
        {
            if (nodeGuids == null || nodeGuids.Count < 1) return;
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(EdgeQueries.DeleteNodeEdges(tenantGuid, graphGuid, nodeGuids), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid edgeGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            Edge edge = await ReadByGuid(tenantGuid, edgeGuid, token).ConfigureAwait(false);
            return (edge != null);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
