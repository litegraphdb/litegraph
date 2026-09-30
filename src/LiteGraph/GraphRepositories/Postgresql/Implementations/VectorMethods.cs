namespace LiteGraph.GraphRepositories.Postgresql.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using ExpressionTree;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Postgresql;
    using LiteGraph.GraphRepositories.Postgresql.Queries;
    using LiteGraph.Helpers;
    using LiteGraph.Indexing.Vector;

    /// <summary>
    /// Vector methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public class VectorMethods : IVectorMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private PostgresqlGraphRepository _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Vector methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public VectorMethods(PostgresqlGraphRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<VectorMetadata> Create(VectorMetadata vector, CancellationToken token = default)
        {
            if (vector == null) throw new ArgumentNullException(nameof(vector));
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(vector.Model)) throw new ArgumentException("The supplied vector model is null or empty.");
            if (vector.Dimensionality <= 0) throw new ArgumentException("The vector dimensionality must be greater than zero.");
            if (vector.Vectors == null || vector.Vectors.Count < 1) throw new ArgumentException("The supplied vector object must contain one or more vectors.");

            string createQuery = VectorQueries.Insert(vector);
            DataTable createResult = await _Repo.ExecuteQueryAsync(createQuery, true, token).ConfigureAwait(false);
            return Converters.VectorFromDataRow(createResult.Rows[0]);
        }

        /// <inheritdoc />
        public async Task<List<VectorMetadata>> CreateMany(Guid tenantGuid, List<VectorMetadata> vectors, CancellationToken token = default)
        {
            if (vectors == null || vectors.Count < 1) return new List<VectorMetadata>();
            token.ThrowIfCancellationRequested();
            foreach (VectorMetadata Vector in vectors)
            {
                token.ThrowIfCancellationRequested();
                Vector.TenantGUID = tenantGuid;
            }

            string insertQuery = VectorQueries.InsertMany(tenantGuid, vectors);
            string retrieveQuery = VectorQueries.SelectMany(tenantGuid, vectors.Select(n => n.GUID).ToList());

            // Execute the entire batch with BEGIN/COMMIT and multi-row INSERTs
            DataTable createResult = await _Repo.ExecuteQueryAsync(insertQuery, true, token).ConfigureAwait(false);
            DataTable retrieveResult = await _Repo.ExecuteQueryAsync(retrieveQuery, true, token).ConfigureAwait(false);
            return Converters.VectorsFromDataTable(retrieveResult);
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(VectorQueries.Delete(tenantGuid, guid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteMany(Guid tenantGuid, Guid? graphGuid, List<Guid> nodeGuids, List<Guid> edgeGuids, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(VectorQueries.DeleteMany(tenantGuid, graphGuid, nodeGuids, edgeGuids), token: token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteMany(Guid tenantGuid, List<Guid> guids, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(VectorQueries.DeleteMany(tenantGuid, guids), false, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(VectorQueries.DeleteAllInTenant(tenantGuid), false, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteAllInGraph(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(VectorQueries.DeleteAllInGraph(tenantGuid, graphGuid), false, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteGraphVectors(Guid tenantGuid, Guid graphGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(VectorQueries.DeleteGraph(tenantGuid, graphGuid), false, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteNodeVectors(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.Vector.DeleteMany(tenantGuid, graphGuid, new List<Guid> { nodeGuid }, null, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteEdgeVectors(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.Vector.DeleteMany(tenantGuid, graphGuid, null, new List<Guid> { edgeGuid }, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid vectorGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return (await ReadByGuid(tenantGuid, vectorGuid, token).ConfigureAwait(false) != null);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<VectorMetadata> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(VectorQueries.SelectAllInTenant(tenantGuid, _Repo.SelectBatchSize, skip, order), false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    VectorMetadata vector = Converters.VectorFromDataRow(result.Rows[i]);
                    yield return vector;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<VectorMetadata> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                DataTable result = await _Repo.ExecuteQueryAsync(VectorQueries.SelectAllInGraph(tenantGuid, graphGuid, _Repo.SelectBatchSize, skip, order), false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    VectorMetadata vector = Converters.VectorFromDataRow(result.Rows[i]);
                    yield return vector;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async Task<VectorMetadata> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(VectorQueries.SelectByGuid(tenantGuid, guid), false, token).ConfigureAwait(false);
            if (result != null && result.Rows.Count == 1) return Converters.VectorFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<VectorMetadata> ReadByGuids(Guid tenantGuid, List<Guid> guids, [EnumeratorCancellation] CancellationToken token = default)
        {
            if (guids == null || guids.Count < 1) yield break;
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(VectorQueries.SelectByGuids(tenantGuid, guids), false, token).ConfigureAwait(false);

            if (result == null || result.Rows.Count < 1) yield break;

            for (int i = 0; i < result.Rows.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                yield return Converters.VectorFromDataRow(result.Rows[i]);
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<VectorMetadata> ReadMany(
            Guid tenantGuid,
            Guid? graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
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
                    query = VectorQueries.SelectTenant(tenantGuid, _Repo.SelectBatchSize, skip, order);
                }
                else
                {
                    if (edgeGuid != null)
                    {
                        query = VectorQueries.SelectEdge(
                            tenantGuid,
                            graphGuid.Value,
                            edgeGuid.Value,
                            _Repo.SelectBatchSize,
                            skip,
                            order);
                    }
                    else if (nodeGuid != null)
                    {
                        query = VectorQueries.SelectNode(
                            tenantGuid,
                            graphGuid.Value,
                            nodeGuid.Value,
                            _Repo.SelectBatchSize,
                            skip,
                            order);
                    }
                    else
                    {
                        query = VectorQueries.SelectAllInGraph(
                            tenantGuid,
                            graphGuid.Value,
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
                    yield return Converters.VectorFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<VectorMetadata> ReadManyGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                string query = VectorQueries.SelectGraph(
                    tenantGuid,
                    graphGuid,
                    _Repo.SelectBatchSize,
                    skip,
                    order);

                DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    yield return Converters.VectorFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<VectorMetadata> ReadManyNode(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                string query = VectorQueries.SelectNode(
                    tenantGuid,
                    graphGuid,
                    nodeGuid,
                    _Repo.SelectBatchSize,
                    skip,
                    order);

                DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    VectorMetadata md = Converters.VectorFromDataRow(result.Rows[i]);
                    yield return md;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<VectorMetadata> ReadManyEdge(
            Guid tenantGuid,
            Guid graphGuid,
            Guid edgeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                string query = VectorQueries.SelectEdge(
                    tenantGuid,
                    graphGuid,
                    edgeGuid,
                    _Repo.SelectBatchSize,
                    skip,
                    order);

                DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    yield return Converters.VectorFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<VectorMetadata> ReadManyForNodes(
            Guid tenantGuid,
            Guid graphGuid,
            List<Guid> nodeGuids,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (nodeGuids == null || nodeGuids.Count < 1) yield break;

            List<Guid> materialized = nodeGuids.Where(g => g != Guid.Empty).Distinct().ToList();
            for (int offset = 0; offset < materialized.Count; offset += _Repo.SelectBatchSize)
            {
                token.ThrowIfCancellationRequested();
                List<Guid> batch = materialized.Skip(offset).Take(_Repo.SelectBatchSize).ToList();
                string query = VectorQueries.SelectManyNodes(tenantGuid, graphGuid, batch);
                DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) continue;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    yield return Converters.VectorFromDataRow(result.Rows[i]);
                }
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<VectorMetadata> ReadManyForEdges(
            Guid tenantGuid,
            Guid graphGuid,
            List<Guid> edgeGuids,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (edgeGuids == null || edgeGuids.Count < 1) yield break;

            List<Guid> materialized = edgeGuids.Where(g => g != Guid.Empty).Distinct().ToList();
            for (int offset = 0; offset < materialized.Count; offset += _Repo.SelectBatchSize)
            {
                token.ThrowIfCancellationRequested();
                List<Guid> batch = materialized.Skip(offset).Take(_Repo.SelectBatchSize).ToList();
                string query = VectorQueries.SelectManyEdges(tenantGuid, graphGuid, batch);
                DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) continue;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    yield return Converters.VectorFromDataRow(result.Rows[i]);
                }
            }
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<VectorMetadata>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            token.ThrowIfCancellationRequested();

            VectorMetadata marker = null;

            if (query.TenantGUID != null && query.ContinuationToken != null)
            {
                marker = await ReadByGuid(query.TenantGUID.Value, query.ContinuationToken.Value, token).ConfigureAwait(false);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + query.ContinuationToken.Value + " could not be found.");
            }

            EnumerationResult<VectorMetadata> ret = new EnumerationResult<VectorMetadata>
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
                DataTable result = await _Repo.ExecuteQueryAsync(VectorQueries.GetRecordPage(
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
                    ret.Objects = Converters.VectorsFromDataTable(result);

                    VectorMetadata lastItem = ret.Objects.Last();

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
        public async Task<int> GetRecordCount(Guid? tenantGuid, Guid? graphGuid, EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, Guid? markerGuid = null, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            VectorMetadata marker = null;
            if (tenantGuid != null && markerGuid != null)
            {
                marker = await ReadByGuid(tenantGuid.Value, markerGuid.Value, token).ConfigureAwait(false);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + markerGuid.Value + " could not be found.");
            }

            DataTable result = await _Repo.ExecuteQueryAsync(VectorQueries.GetRecordCount(
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
        public async Task<VectorMetadata> Update(VectorMetadata vector, CancellationToken token = default)
        {
            if (vector == null) throw new ArgumentNullException(nameof(vector));
            token.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(vector.Model)) throw new ArgumentException("The supplied vector model is null or empty.");
            if (vector.Dimensionality <= 0) throw new ArgumentException("The vector dimensionality must be greater than zero.");
            if (vector.Vectors == null || vector.Vectors.Count < 1) throw new ArgumentException("The supplied vector object must contain one or more vectors.");

            string updateQuery = VectorQueries.Update(vector);
            DataTable updateResult = await _Repo.ExecuteQueryAsync(updateQuery, true, token).ConfigureAwait(false);
            return Converters.VectorFromDataRow(updateResult.Rows[0]);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<VectorSearchResult> SearchGraph(
            VectorSearchTypeEnum searchType,
            List<float> vectors,
            Guid tenantGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null,
            int? topK = 100,
            float? minScore = 0.0f,
            float? maxDistance = 1.0f,
            float? minInnerProduct = 0.0f,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (vectors == null || vectors.Count < 1) throw new ArgumentException("The supplied vector list must contain at least one vector.");
            if (topK != null && topK.Value < 1) throw new ArgumentOutOfRangeException(nameof(topK));
            token.ThrowIfCancellationRequested();

            List<KeyValuePair<Guid, VectorSearchResult>> matches = await SearchCoreAsync(
                "graph", searchType, vectors, tenantGuid, null, labels, tags, filter, topK, minScore, maxDistance, minInnerProduct, null, token).ConfigureAwait(false);

            List<Guid> graphGuids = matches.Select(m => m.Key).ToList();
            Dictionary<Guid, Graph> graphs = await ToDictionaryAsync(_Repo.Graph.ReadByGuids(tenantGuid, graphGuids, token), g => g.GUID, token).ConfigureAwait(false);
            Dictionary<Guid, List<VectorMetadata>> graphVectors = await ReadVectorsByOwnerAsync(VectorQueries.SelectManyGraphs(tenantGuid, graphGuids), v => v.GraphGUID, token).ConfigureAwait(false);

            foreach (KeyValuePair<Guid, VectorSearchResult> kvp in matches)
            {
                token.ThrowIfCancellationRequested();
                if (!graphs.TryGetValue(kvp.Key, out Graph graph)) continue;

                kvp.Value.Graph = graph;
                graph.Vectors = graphVectors.TryGetValue(graph.GUID, out List<VectorMetadata> list) ? list : new List<VectorMetadata>();
                yield return kvp.Value;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<VectorSearchResult> SearchNode(
            VectorSearchTypeEnum searchType,
            List<float> vectors,
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null,
            int? topK = 100,
            float? minScore = 0.0f,
            float? maxDistance = 1.0f,
            float? minInnerProduct = 0.0f,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (vectors == null || vectors.Count < 1) throw new ArgumentException("The supplied vector list must contain at least one vector.");
            if (topK != null && topK.Value < 1) throw new ArgumentOutOfRangeException(nameof(topK));
            token.ThrowIfCancellationRequested();

            Graph graph = await _Repo.Graph.ReadByGuid(tenantGuid, graphGuid, token).ConfigureAwait(false);

            List<KeyValuePair<Guid, VectorSearchResult>> matches = await SearchCoreAsync(
                "node", searchType, vectors, tenantGuid, graphGuid, labels, tags, filter, topK, minScore, maxDistance, minInnerProduct, graph, token).ConfigureAwait(false);

            List<Guid> nodeGuids = matches.Select(m => m.Key).ToList();
            Dictionary<Guid, Node> nodes = await ToDictionaryAsync(_Repo.Node.ReadByGuids(tenantGuid, nodeGuids, token), n => n.GUID, token).ConfigureAwait(false);
            Dictionary<Guid, List<VectorMetadata>> nodeVectors = await ReadVectorsByOwnerAsync(VectorQueries.SelectManyNodes(tenantGuid, graphGuid, nodeGuids), v => v.NodeGUID, token).ConfigureAwait(false);

            foreach (KeyValuePair<Guid, VectorSearchResult> kvp in matches)
            {
                token.ThrowIfCancellationRequested();
                if (!nodes.TryGetValue(kvp.Key, out Node node)) continue;

                kvp.Value.Node = node;
                kvp.Value.Graph = graph;
                node.Vectors = nodeVectors.TryGetValue(node.GUID, out List<VectorMetadata> list) ? list : new List<VectorMetadata>();
                yield return kvp.Value;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<VectorSearchResult> SearchEdge(
            VectorSearchTypeEnum searchType,
            List<float> vectors,
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null,
            int? topK = 100,
            float? minScore = 0.0f,
            float? maxDistance = 1.0f,
            float? minInnerProduct = 0.0f,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            if (vectors == null || vectors.Count < 1) throw new ArgumentException("The supplied vector list must contain at least one vector.");
            if (topK != null && topK.Value < 1) throw new ArgumentOutOfRangeException(nameof(topK));
            token.ThrowIfCancellationRequested();

            List<KeyValuePair<Guid, VectorSearchResult>> matches = await SearchCoreAsync(
                "edge", searchType, vectors, tenantGuid, graphGuid, labels, tags, filter, topK, minScore, maxDistance, minInnerProduct, null, token).ConfigureAwait(false);

            List<Guid> edgeGuids = matches.Select(m => m.Key).ToList();
            Dictionary<Guid, Edge> edges = await ToDictionaryAsync(_Repo.Edge.ReadByGuids(tenantGuid, edgeGuids, token), e => e.GUID, token).ConfigureAwait(false);
            Dictionary<Guid, List<VectorMetadata>> edgeVectors = await ReadVectorsByOwnerAsync(VectorQueries.SelectManyEdges(tenantGuid, graphGuid, edgeGuids), v => v.EdgeGUID, token).ConfigureAwait(false);

            foreach (KeyValuePair<Guid, VectorSearchResult> kvp in matches)
            {
                token.ThrowIfCancellationRequested();
                if (!edges.TryGetValue(kvp.Key, out Edge edge)) continue;

                kvp.Value.Edge = edge;
                edge.Vectors = edgeVectors.TryGetValue(edge.GUID, out List<VectorMetadata> list) ? list : new List<VectorMetadata>();
                yield return kvp.Value;
            }
        }

        #endregion

        #region Private-Methods

        private async Task<Dictionary<Guid, List<VectorMetadata>>> ReadVectorsByOwnerAsync(string query, Func<VectorMetadata, Guid?> owner, CancellationToken token)
        {
            // One query for every result's vectors, grouped by owner and ordered as ReadMany* orders them
            // (createdutc descending, then guid descending), instead of one query per result.
            Dictionary<Guid, List<VectorMetadata>> ret = new Dictionary<Guid, List<VectorMetadata>>();
            DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
            if (result == null) return ret;

            foreach (DataRow row in result.Rows)
            {
                VectorMetadata vector = Converters.VectorFromDataRow(row);
                Guid? id = owner(vector);
                if (id == null) continue;
                if (!ret.TryGetValue(id.Value, out List<VectorMetadata> list))
                {
                    list = new List<VectorMetadata>();
                    ret[id.Value] = list;
                }
                list.Add(vector);
            }

            foreach (List<VectorMetadata> list in ret.Values)
            {
                list.Sort((a, b) =>
                {
                    int byTime = b.CreatedUtc.CompareTo(a.CreatedUtc);
                    return byTime != 0 ? byTime : String.CompareOrdinal(b.GUID.ToString(), a.GUID.ToString());
                });
            }

            return ret;
        }

        private static async Task<Dictionary<Guid, T>> ToDictionaryAsync<T>(IAsyncEnumerable<T> items, Func<T, Guid> key, CancellationToken token)
        {
            Dictionary<Guid, T> ret = new Dictionary<Guid, T>();
            await foreach (T item in items.WithCancellation(token).ConfigureAwait(false))
            {
                if (item != null) ret[key(item)] = item;
            }
            return ret;
        }

        private async Task<List<KeyValuePair<Guid, VectorSearchResult>>> SearchCoreAsync(
            string scope,
            VectorSearchTypeEnum searchType,
            List<float> vectors,
            Guid tenantGuid,
            Guid? graphGuid,
            List<string> labels,
            NameValueCollection tags,
            Expr filter,
            int? topK,
            float? minScore,
            float? maxDistance,
            float? minInnerProduct,
            Graph graph,
            CancellationToken token)
        {
            using Activity activity = LiteGraphTelemetry.ActivitySource.StartActivity(LiteGraphTelemetry.VectorIndexSearchActivityName, ActivityKind.Internal);

            int limit = topK ?? Int32.MaxValue;
            bool useIndex = scope == "node"
                && graph != null
                && graph.VectorIndexType.HasValue
                && graph.VectorIndexType != VectorIndexTypeEnum.None
                && PgvectorQueries.IsCosine(searchType)
                && PgvectorQueries.IsIndexableDimensionality(vectors.Count);

            int candidateLimit = PgvectorQueries.MaxEfSearch;
            if (topK != null) candidateLimit = Math.Min(PgvectorQueries.MaxEfSearch, Math.Max(topK.Value * 4, 64));
            int efSearch = Math.Min(PgvectorQueries.MaxEfSearch, Math.Max(graph?.VectorIndexEf ?? 40, candidateLimit));

            activity?.SetTag("db.system", "postgresql");
            activity?.SetTag("litegraph.vector.provider", "pgvector");
            activity?.SetTag("litegraph.vector.search_scope", scope);
            activity?.SetTag("litegraph.vector.search_type", searchType.ToString());
            activity?.SetTag("litegraph.vector.dimensions", vectors.Count);
            activity?.SetTag("litegraph.vector.index.used", useIndex);

            string query = PgvectorQueries.Search(
                scope, tenantGuid, graphGuid, vectors, searchType, labels, tags, filter, limit, useIndex, candidateLimit, efSearch);

            DataTable result = await _Repo.ExecuteQueryAsync(query, useIndex, token).ConfigureAwait(false);

            List<KeyValuePair<Guid, VectorSearchResult>> matches = new List<KeyValuePair<Guid, VectorSearchResult>>();
            if (result == null || result.Rows.Count < 1)
            {
                LiteGraphTelemetry.SetActivityOk(activity);
                return matches;
            }

            foreach (DataRow row in result.Rows)
            {
                token.ThrowIfCancellationRequested();
                if (row["objguid"] == DBNull.Value || row["dist"] == DBNull.Value) continue;

                double raw = Convert.ToDouble(row["dist"], CultureInfo.InvariantCulture);
                if (Double.IsNaN(raw) || Double.IsInfinity(raw)) continue;

                VectorSearchResult match = ToSearchResult(searchType, (float)raw);
                if (!MeetsConstraints(match.Score, match.Distance, match.InnerProduct, minScore, maxDistance, minInnerProduct)) continue;

                matches.Add(new KeyValuePair<Guid, VectorSearchResult>(Guid.Parse(row["objguid"].ToString()), match));
            }

            activity?.SetTag("litegraph.vector.index.results", matches.Count);
            LiteGraphTelemetry.SetActivityOk(activity);
            return matches;
        }

        private static VectorSearchResult ToSearchResult(VectorSearchTypeEnum searchType, float pgDistance)
        {
            switch (searchType)
            {
                case VectorSearchTypeEnum.CosineDistance:
                    return new VectorSearchResult { Distance = pgDistance };
                case VectorSearchTypeEnum.CosineSimilarity:
                    return new VectorSearchResult { Score = 1.0f - pgDistance };
                case VectorSearchTypeEnum.EuclidianDistance:
                    return new VectorSearchResult { Distance = pgDistance };
                case VectorSearchTypeEnum.EuclidianSimilarity:
                    return new VectorSearchResult { Score = 1.0f / (1.0f + pgDistance) };
                case VectorSearchTypeEnum.DotProduct:
                    return new VectorSearchResult { InnerProduct = -pgDistance };
                default:
                    throw new ArgumentException("Unknown vector search type " + searchType.ToString() + ".");
            }
        }

        private bool MeetsConstraints(
            float? score,
            float? distance,
            float? innerProduct,
            float? minScore,
            float? maxDistance,
            float? minInnerProduct)
        {
            if (score != null && minScore != null && score.Value < minScore.Value) return false;
            if (distance != null && maxDistance != null && distance.Value > maxDistance.Value) return false;
            if (innerProduct != null && minInnerProduct != null && innerProduct.Value < minInnerProduct.Value) return false;
            return true;
        }

        #endregion
    }
}

