namespace LiteGraph.GraphRepositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using ExpressionTree;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
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

        private SqliteGraphRepository _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Vector methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public VectorMethods(SqliteGraphRepository repo)
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
            VectorMetadata created = Converters.VectorFromDataRow(createResult.Rows[0]);

            // Update vector index asynchronously
            await VectorMethodsIndexExtensions.UpdateIndexForCreateAsync(_Repo, created).ConfigureAwait(false);

            return created;
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
            List<VectorMetadata> created = Converters.VectorsFromDataTable(retrieveResult);

            // Update vector index asynchronously for batch
            await VectorMethodsIndexExtensions.UpdateIndexForCreateManyAsync(_Repo, created).ConfigureAwait(false);

            return created;
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            VectorMetadata vector = await ReadByGuid(tenantGuid, guid, token).ConfigureAwait(false);
            if (vector != null)
            {
                await _Repo.ExecuteQueryAsync(VectorQueries.Delete(tenantGuid, guid), true, token).ConfigureAwait(false);

                // Update vector index asynchronously
                await VectorMethodsIndexExtensions.UpdateIndexForDeleteAsync(_Repo, tenantGuid, guid, vector.GraphGUID).ConfigureAwait(false);
            }
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
                    if (edgeGuid != null) query = VectorQueries.SelectEdge(
                        tenantGuid,
                        graphGuid.Value,
                        edgeGuid.Value,
                        _Repo.SelectBatchSize,
                        skip,
                        order);
                    else if (nodeGuid != null) query = VectorQueries.SelectNode(
                        tenantGuid,
                        graphGuid.Value,
                        nodeGuid.Value,
                        _Repo.SelectBatchSize,
                        skip,
                        order);
                    else query = VectorQueries.SelectGraph(
                        tenantGuid,
                        graphGuid.Value,
                        _Repo.SelectBatchSize,
                        skip,
                        order);
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
            VectorMetadata updated = Converters.VectorFromDataRow(updateResult.Rows[0]);

            // Update vector index asynchronously
            await VectorMethodsIndexExtensions.UpdateIndexForUpdateAsync(_Repo, updated).ConfigureAwait(false);

            return updated;
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

            // Step 1: Get all filtered vectors with a single query that includes all filtering
            List<VectorMetadata> candidateVectors = new List<VectorMetadata>();
            int skip = 0;

            while (true)
            {
                token.ThrowIfCancellationRequested();
                string query = VectorQueries.SelectGraphVectorsWithFilters(
                    tenantGuid,
                    labels,
                    tags,
                    filter,
                    _Repo.SelectBatchSize,
                    skip);

                DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    VectorMetadata vmd = Converters.VectorFromDataRow(result.Rows[i]);
                    if (vmd.Vectors != null && vmd.Vectors.Count > 0 && vmd.Vectors.Count == vectors.Count)
                    {
                        candidateVectors.Add(vmd);
                    }
                }

                skip += result.Rows.Count;
                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }

            // Step 2: Compare vectors and collect matching results
            Dictionary<Guid, VectorSearchResult> bestResultsByGraph = new Dictionary<Guid, VectorSearchResult>();

            foreach (VectorMetadata vmd in candidateVectors)
            {
                token.ThrowIfCancellationRequested();
                float? score = null;
                float? distance = null;
                float? innerProduct = null;

                CompareVectors(searchType, vectors, vmd.Vectors, out score, out distance, out innerProduct);

                if (MeetsConstraints(score, distance, innerProduct, minScore, maxDistance, minInnerProduct))
                {
                    // Keep only the best result for each graph
                    if (!bestResultsByGraph.ContainsKey(vmd.GraphGUID))
                    {
                        bestResultsByGraph[vmd.GraphGUID] = new VectorSearchResult
                        {
                            Score = score,
                            Distance = distance,
                            InnerProduct = innerProduct
                        };
                    }
                    else
                    {
                        // Compare and keep the better result
                        VectorSearchResult existing = bestResultsByGraph[vmd.GraphGUID];
                        bool isBetter = false;

                        if (score != null && existing.Score != null)
                            isBetter = score.Value > existing.Score.Value;
                        else if (distance != null && existing.Distance != null)
                            isBetter = distance.Value < existing.Distance.Value;
                        else if (innerProduct != null && existing.InnerProduct != null)
                            isBetter = innerProduct.Value > existing.InnerProduct.Value;

                        if (isBetter)
                        {
                            bestResultsByGraph[vmd.GraphGUID] = new VectorSearchResult
                            {
                                Score = score,
                                Distance = distance,
                                InnerProduct = innerProduct
                            };
                        }
                    }
                }
            }

            // Step 3: Sort results and retrieve graphs
            List<KeyValuePair<Guid, VectorSearchResult>> sortedResults = bestResultsByGraph
                .OrderByDescending(x => x.Value.Score)
                .ThenBy(x => x.Value.Distance)
                .ThenByDescending(x => x.Value.InnerProduct)
                .Take(topK ?? int.MaxValue)
                .ToList();

            foreach (KeyValuePair<Guid, VectorSearchResult> kvp in sortedResults)
            {
                token.ThrowIfCancellationRequested();
                Graph graph = await _Repo.Graph.ReadByGuid(tenantGuid, kvp.Key, token).ConfigureAwait(false);
                if (graph != null)
                {
                    kvp.Value.Graph = graph;
                    // Optionally load vectors for the graph
                    List<VectorMetadata> graphVectors = new List<VectorMetadata>();
                    await foreach (VectorMetadata vector in _Repo.Vector.ReadManyGraph(tenantGuid, graph.GUID, token: token).WithCancellation(token).ConfigureAwait(false))
                    {
                        graphVectors.Add(vector);
                    }
                    graph.Vectors = graphVectors;
                    yield return kvp.Value;
                }
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

            // Try to use HNSW index first if available and no complex filtering
            bool canUseIndex = (labels == null || labels.Count == 0) &&
                              (tags == null || tags.Count == 0) &&
                              filter == null;

            if (canUseIndex)
            {
                Graph graph = await _Repo.Graph.ReadByGuid(tenantGuid, graphGuid, token).ConfigureAwait(false);
                if (graph != null && graph.VectorIndexType.HasValue && graph.VectorIndexType != VectorIndexTypeEnum.None)
                {
                    // Use HNSW index for fast search
                    IEnumerable<(Guid Id, float Score)> indexedResults = await VectorMethodsIndexExtensions.SearchWithIndexAsync(
                        _Repo, searchType, vectors, graph, topK ?? 100).ConfigureAwait(false);

                    if (indexedResults != null)
                    {
                        // Convert indexed results to VectorSearchResult and get node info
                        foreach ((Guid Id, float Score) indexResult in indexedResults)
                        {
                            token.ThrowIfCancellationRequested();
                            Node node = await _Repo.Node.ReadByGuid(tenantGuid, indexResult.Id, token).ConfigureAwait(false);
                            if (node != null)
                            {
                                yield return new VectorSearchResult
                                {
                                    Node = node,
                                    Graph = graph,
                                    Score = indexResult.Score,
                                    Distance = searchType == VectorSearchTypeEnum.CosineSimilarity ?
                                              (1.0f - indexResult.Score) : indexResult.Score
                                };
                            }
                        }
                        yield break; // Return indexed results, skip brute force
                    }
                }
            }

            // Fallback to brute force search (original implementation)
            // Step 1: Get all filtered vectors with a single query that includes all filtering
            List<VectorMetadata> candidateVectors = new List<VectorMetadata>();
            int skip = 0;

            while (true)
            {
                token.ThrowIfCancellationRequested();
                string query = VectorQueries.SelectNodeVectorsWithFilters(
                    tenantGuid,
                    graphGuid,
                    labels,
                    tags,
                    filter,
                    _Repo.SelectBatchSize,
                    skip);

                DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    VectorMetadata vmd = Converters.VectorFromDataRow(result.Rows[i]);
                    if (vmd.Vectors != null && vmd.Vectors.Count > 0 && vmd.Vectors.Count == vectors.Count)
                    {
                        candidateVectors.Add(vmd);
                    }
                }

                skip += result.Rows.Count;
                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }

            // Step 2: Compare vectors and collect matching results
            Dictionary<Guid, VectorSearchResult> bestResultsByNode = new Dictionary<Guid, VectorSearchResult>();

            foreach (VectorMetadata vmd in candidateVectors)
            {
                token.ThrowIfCancellationRequested();
                if (vmd.NodeGUID == null) continue;

                float? score = null;
                float? distance = null;
                float? innerProduct = null;

                CompareVectors(searchType, vectors, vmd.Vectors, out score, out distance, out innerProduct);

                if (MeetsConstraints(score, distance, innerProduct, minScore, maxDistance, minInnerProduct))
                {
                    // Keep only the best result for each node
                    if (!bestResultsByNode.ContainsKey(vmd.NodeGUID.Value))
                    {
                        bestResultsByNode[vmd.NodeGUID.Value] = new VectorSearchResult
                        {
                            Score = score,
                            Distance = distance,
                            InnerProduct = innerProduct
                        };
                    }
                    else
                    {
                        // Compare and keep the better result
                        VectorSearchResult existing = bestResultsByNode[vmd.NodeGUID.Value];
                        bool isBetter = false;

                        if (score != null && existing.Score != null)
                            isBetter = score.Value > existing.Score.Value;
                        else if (distance != null && existing.Distance != null)
                            isBetter = distance.Value < existing.Distance.Value;
                        else if (innerProduct != null && existing.InnerProduct != null)
                            isBetter = innerProduct.Value > existing.InnerProduct.Value;

                        if (isBetter)
                        {
                            bestResultsByNode[vmd.NodeGUID.Value] = new VectorSearchResult
                            {
                                Score = score,
                                Distance = distance,
                                InnerProduct = innerProduct
                            };
                        }
                    }
                }
            }

            // Step 3: Sort results and retrieve nodes
            List<KeyValuePair<Guid, VectorSearchResult>> sortedResults = bestResultsByNode
                .OrderByDescending(x => x.Value.Score)
                .ThenBy(x => x.Value.Distance)
                .ThenByDescending(x => x.Value.InnerProduct)
                .Take(topK ?? int.MaxValue)
                .ToList();

            foreach (KeyValuePair<Guid, VectorSearchResult> kvp in sortedResults)
            {
                token.ThrowIfCancellationRequested();
                Node node = await _Repo.Node.ReadByGuid(tenantGuid, kvp.Key, token).ConfigureAwait(false);
                if (node != null)
                {
                    kvp.Value.Node = node;
                    // Optionally load vectors for the node
                    List<VectorMetadata> nodeVectors = new List<VectorMetadata>();
                    await foreach (VectorMetadata vector in _Repo.Vector.ReadManyNode(tenantGuid, node.GraphGUID, node.GUID, token: token).WithCancellation(token).ConfigureAwait(false))
                    {
                        nodeVectors.Add(vector);
                    }
                    node.Vectors = nodeVectors;
                    yield return kvp.Value;
                }
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

            // Step 1: Get all filtered vectors with a single query that includes all filtering
            List<VectorMetadata> candidateVectors = new List<VectorMetadata>();
            int skip = 0;

            while (true)
            {
                token.ThrowIfCancellationRequested();
                string query = VectorQueries.SelectEdgeVectorsWithFilters(
                    tenantGuid,
                    graphGuid,
                    labels,
                    tags,
                    filter,
                    _Repo.SelectBatchSize,
                    skip);

                DataTable result = await _Repo.ExecuteQueryAsync(query, false, token).ConfigureAwait(false);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    token.ThrowIfCancellationRequested();
                    VectorMetadata vmd = Converters.VectorFromDataRow(result.Rows[i]);
                    if (vmd.Vectors != null && vmd.Vectors.Count > 0 && vmd.Vectors.Count == vectors.Count)
                    {
                        candidateVectors.Add(vmd);
                    }
                }

                skip += result.Rows.Count;
                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }

            // Step 2: Compare vectors and collect matching results
            Dictionary<Guid, VectorSearchResult> bestResultsByEdge = new Dictionary<Guid, VectorSearchResult>();

            foreach (VectorMetadata vmd in candidateVectors)
            {
                token.ThrowIfCancellationRequested();
                if (vmd.EdgeGUID == null) continue;

                float? score = null;
                float? distance = null;
                float? innerProduct = null;

                CompareVectors(searchType, vectors, vmd.Vectors, out score, out distance, out innerProduct);

                if (MeetsConstraints(score, distance, innerProduct, minScore, maxDistance, minInnerProduct))
                {
                    // Keep only the best result for each edge
                    if (!bestResultsByEdge.ContainsKey(vmd.EdgeGUID.Value))
                    {
                        bestResultsByEdge[vmd.EdgeGUID.Value] = new VectorSearchResult
                        {
                            Score = score,
                            Distance = distance,
                            InnerProduct = innerProduct
                        };
                    }
                    else
                    {
                        // Compare and keep the better result
                        VectorSearchResult existing = bestResultsByEdge[vmd.EdgeGUID.Value];
                        bool isBetter = false;

                        if (score != null && existing.Score != null)
                            isBetter = score.Value > existing.Score.Value;
                        else if (distance != null && existing.Distance != null)
                            isBetter = distance.Value < existing.Distance.Value;
                        else if (innerProduct != null && existing.InnerProduct != null)
                            isBetter = innerProduct.Value > existing.InnerProduct.Value;

                        if (isBetter)
                        {
                            bestResultsByEdge[vmd.EdgeGUID.Value] = new VectorSearchResult
                            {
                                Score = score,
                                Distance = distance,
                                InnerProduct = innerProduct
                            };
                        }
                    }
                }
            }

            // Step 3: Sort results and retrieve edges
            List<KeyValuePair<Guid, VectorSearchResult>> sortedResults = bestResultsByEdge
                .OrderByDescending(x => x.Value.Score)
                .ThenBy(x => x.Value.Distance)
                .ThenByDescending(x => x.Value.InnerProduct)
                .Take(topK ?? int.MaxValue)
                .ToList();

            foreach (KeyValuePair<Guid, VectorSearchResult> kvp in sortedResults)
            {
                token.ThrowIfCancellationRequested();
                Edge edge = await _Repo.Edge.ReadByGuid(tenantGuid, kvp.Key, token).ConfigureAwait(false);
                if (edge != null)
                {
                    kvp.Value.Edge = edge;
                    // Optionally load vectors for the edge
                    List<VectorMetadata> edgeVectors = new List<VectorMetadata>();
                    await foreach (VectorMetadata vector in _Repo.Vector.ReadManyEdge(tenantGuid, edge.GraphGUID, edge.GUID, token: token).WithCancellation(token).ConfigureAwait(false))
                    {
                        edgeVectors.Add(vector);
                    }
                    edge.Vectors = edgeVectors;
                    yield return kvp.Value;
                }
            }
        }

        #endregion

        #region Private-Methods

        private void CompareVectors(
            VectorSearchTypeEnum searchType,
            List<float> vectors1,
            List<float> vectors2,
            out float? score,
            out float? distance,
            out float? innerProduct)
        {
            score = null;
            distance = null;
            innerProduct = null;

            if (searchType == VectorSearchTypeEnum.CosineDistance)
                distance = VectorHelper.CalculateCosineDistance(vectors1, vectors2);
            else if (searchType == VectorSearchTypeEnum.CosineSimilarity)
                score = VectorHelper.CalculateCosineSimilarity(vectors1, vectors2);
            else if (searchType == VectorSearchTypeEnum.DotProduct)
                innerProduct = VectorHelper.CalculateInnerProduct(vectors1, vectors2);
            else if (searchType == VectorSearchTypeEnum.EuclidianDistance)
                distance = VectorHelper.CalculateEuclidianDistance(vectors1, vectors2);
            else if (searchType == VectorSearchTypeEnum.EuclidianSimilarity)
                score = VectorHelper.CalculateEuclidianSimilarity(vectors1, vectors2);
            else
            {
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