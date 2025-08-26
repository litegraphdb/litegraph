namespace LiteGraph.GraphRepositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using ExpressionTree;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Helpers;
    using LiteGraph.Indexing.Vector;
    using LiteGraph.Serialization;
    using Timestamps;
    using LoggingSettings = LoggingSettings;

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
        public VectorMetadata Create(VectorMetadata vector)
        {
            if (vector == null) throw new ArgumentNullException(nameof(vector));
            if (string.IsNullOrEmpty(vector.Model)) throw new ArgumentException("The supplied vector model is null or empty.");
            if (vector.Dimensionality <= 0) throw new ArgumentException("The vector dimensionality must be greater than zero.");
            if (vector.Vectors == null || vector.Vectors.Count < 1) throw new ArgumentException("The supplied vector object must contain one or more vectors.");

            string createQuery = VectorQueries.Insert(vector);
            DataTable createResult = _Repo.ExecuteQuery(createQuery, true);
            VectorMetadata created = Converters.VectorFromDataRow(createResult.Rows[0]);
            
            // Update vector index asynchronously
            Task.Run(async () => await VectorMethodsIndexExtensions.UpdateIndexForCreateAsync(_Repo, created)).Wait();
            
            return created;
        }

        /// <inheritdoc />
        public List<VectorMetadata> CreateMany(Guid tenantGuid, List<VectorMetadata> vectors)
        {
            if (vectors == null || vectors.Count < 1) return new List<VectorMetadata>();
            foreach (VectorMetadata Vector in vectors)
            {
                Vector.TenantGUID = tenantGuid;
            }

            string insertQuery = VectorQueries.InsertMany(tenantGuid, vectors);
            string retrieveQuery = VectorQueries.SelectMany(tenantGuid, vectors.Select(n => n.GUID).ToList());

            // Execute the entire batch with BEGIN/COMMIT and multi-row INSERTs
            DataTable createResult = _Repo.ExecuteQuery(insertQuery, true);
            DataTable retrieveResult = _Repo.ExecuteQuery(retrieveQuery, true);
            List<VectorMetadata> created = Converters.VectorsFromDataTable(retrieveResult);
            
            // Update vector index asynchronously for batch
            Task.Run(async () => await VectorMethodsIndexExtensions.UpdateIndexForCreateManyAsync(_Repo, created)).Wait();
            
            return created;
        }

        /// <inheritdoc />
        public void DeleteByGuid(Guid tenantGuid, Guid guid)
        {
            VectorMetadata vector = ReadByGuid(tenantGuid, guid);
            if (vector != null)
            {
                _Repo.ExecuteQuery(VectorQueries.Delete(tenantGuid, guid), true);
                
                // Update vector index asynchronously
                Task.Run(async () => await VectorMethodsIndexExtensions.UpdateIndexForDeleteAsync(_Repo, tenantGuid, guid, vector.GraphGUID)).Wait();
            }
        }

        /// <inheritdoc />
        public void DeleteMany(Guid tenantGuid, Guid? graphGuid, List<Guid> nodeGuids, List<Guid> edgeGuids)
        {
            _Repo.ExecuteQuery(VectorQueries.DeleteMany(tenantGuid, graphGuid, nodeGuids, edgeGuids));
        }

        /// <inheritdoc />
        public void DeleteMany(Guid tenantGuid, List<Guid> guids)
        {
            _Repo.ExecuteQuery(VectorQueries.DeleteMany(tenantGuid, guids));
        }

        /// <inheritdoc />
        public void DeleteAllInTenant(Guid tenantGuid)
        {
            _Repo.ExecuteQuery(VectorQueries.DeleteAllInTenant(tenantGuid));
        }

        /// <inheritdoc />
        public void DeleteAllInGraph(Guid tenantGuid, Guid graphGuid)
        {
            _Repo.ExecuteQuery(VectorQueries.DeleteAllInGraph(tenantGuid, graphGuid));
        }

        /// <inheritdoc />
        public void DeleteGraphVectors(Guid tenantGuid, Guid graphGuid)
        {
            _Repo.ExecuteQuery(VectorQueries.DeleteGraph(tenantGuid, graphGuid));
        }

        /// <inheritdoc />
        public void DeleteNodeVectors(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            _Repo.Vector.DeleteMany(tenantGuid, graphGuid, new List<Guid> { nodeGuid }, null);
        }

        /// <inheritdoc />
        public void DeleteEdgeVectors(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            _Repo.Vector.DeleteMany(tenantGuid, graphGuid, null, new List<Guid> { edgeGuid });
        }

        /// <inheritdoc />
        public bool ExistsByGuid(Guid tenantGuid, Guid vectorGuid)
        {
            return (ReadByGuid(tenantGuid, vectorGuid) != null);
        }

        /// <inheritdoc />
        public IEnumerable<VectorMetadata> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(VectorQueries.SelectAllInTenant(tenantGuid, _Repo.SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    VectorMetadata vector = Converters.VectorFromDataRow(result.Rows[i]);
                    yield return vector;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public IEnumerable<VectorMetadata> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(VectorQueries.SelectAllInGraph(tenantGuid, graphGuid, _Repo.SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    VectorMetadata vector = Converters.VectorFromDataRow(result.Rows[i]);
                    yield return vector;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public VectorMetadata ReadByGuid(Guid tenantGuid, Guid guid)
        {
            DataTable result = _Repo.ExecuteQuery(VectorQueries.SelectByGuid(tenantGuid, guid));
            if (result != null && result.Rows.Count == 1) return Converters.VectorFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public IEnumerable<VectorMetadata> ReadByGuids(Guid tenantGuid, List<Guid> guids)
        {
            if (guids == null || guids.Count < 1) yield break;
            DataTable result = _Repo.ExecuteQuery(VectorQueries.SelectByGuids(tenantGuid, guids));

            if (result == null || result.Rows.Count < 1) yield break;

            for (int i = 0; i < result.Rows.Count; i++)
            {
                yield return Converters.VectorFromDataRow(result.Rows[i]);
            }
        }

        /// <inheritdoc />
        public IEnumerable<VectorMetadata> ReadMany(
            Guid tenantGuid,
            Guid? graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
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

                DataTable result = _Repo.ExecuteQuery(query);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.VectorFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public IEnumerable<VectorMetadata> ReadManyGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            while (true)
            {
                string query = VectorQueries.SelectGraph(
                    tenantGuid,
                    graphGuid,
                    _Repo.SelectBatchSize,
                    skip,
                    order);

                DataTable result = _Repo.ExecuteQuery(query);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.VectorFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public IEnumerable<VectorMetadata> ReadManyNode(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            while (true)
            {
                string query = VectorQueries.SelectNode(
                    tenantGuid,
                    graphGuid,
                    nodeGuid,
                    _Repo.SelectBatchSize,
                    skip,
                    order);

                DataTable result = _Repo.ExecuteQuery(query);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    VectorMetadata md = Converters.VectorFromDataRow(result.Rows[i]);
                    yield return md;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public IEnumerable<VectorMetadata> ReadManyEdge(
            Guid tenantGuid,
            Guid graphGuid,
            Guid edgeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            while (true)
            {
                string query = VectorQueries.SelectEdge(
                    tenantGuid,
                    graphGuid,
                    edgeGuid,
                    _Repo.SelectBatchSize,
                    skip,
                    order);

                DataTable result = _Repo.ExecuteQuery(query);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.VectorFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public EnumerationResult<VectorMetadata> Enumerate(EnumerationRequest query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            VectorMetadata marker = null;

            if (query.TenantGUID != null && query.ContinuationToken != null)
            {
                marker = ReadByGuid(query.TenantGUID.Value, query.ContinuationToken.Value);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + query.ContinuationToken.Value + " could not be found.");
            }

            EnumerationResult<VectorMetadata> ret = new EnumerationResult<VectorMetadata>
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
                DataTable result = _Repo.ExecuteQuery(VectorQueries.GetRecordPage(
                    query.TenantGUID,
                    query.GraphGUID,
                    query.MaxResults,
                    query.Skip,
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
                    ret.Objects = Converters.VectorsFromDataTable(result);

                    VectorMetadata lastItem = ret.Objects.Last();

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
        public int GetRecordCount(Guid? tenantGuid, Guid? graphGuid, EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, Guid? markerGuid = null)
        {
            VectorMetadata marker = null;
            if (tenantGuid != null && markerGuid != null)
            {
                marker = ReadByGuid(tenantGuid.Value, markerGuid.Value);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + markerGuid.Value + " could not be found.");
            }

            DataTable result = _Repo.ExecuteQuery(VectorQueries.GetRecordCount(
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
        public VectorMetadata Update(VectorMetadata vector)
        {
            if (vector == null) throw new ArgumentNullException(nameof(vector));
            if (string.IsNullOrEmpty(vector.Model)) throw new ArgumentException("The supplied vector model is null or empty.");
            if (vector.Dimensionality <= 0) throw new ArgumentException("The vector dimensionality must be greater than zero.");
            if (vector.Vectors == null || vector.Vectors.Count < 1) throw new ArgumentException("The supplied vector object must contain one or more vectors.");

            string updateQuery = VectorQueries.Update(vector);
            DataTable updateResult = _Repo.ExecuteQuery(updateQuery, true);
            VectorMetadata updated = Converters.VectorFromDataRow(updateResult.Rows[0]);
            
            // Update vector index asynchronously
            Task.Run(async () => await VectorMethodsIndexExtensions.UpdateIndexForUpdateAsync(_Repo, updated)).Wait();
            
            return updated;
        }

        /// <inheritdoc />
        public IEnumerable<VectorSearchResult> SearchGraph(
            VectorSearchTypeEnum searchType,
            List<float> vectors,
            Guid tenantGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null,
            int? topK = 100,
            float? minScore = 0.0f,
            float? maxDistance = 1.0f,
            float? minInnerProduct = 0.0f)
        {
            if (vectors == null || vectors.Count < 1) throw new ArgumentException("The supplied vector list must contain at least one vector.");
            if (topK != null && topK.Value < 1) throw new ArgumentOutOfRangeException(nameof(topK));

            // Step 1: Get all filtered vectors with a single query that includes all filtering
            List<VectorMetadata> candidateVectors = new List<VectorMetadata>();
            int skip = 0;

            while (true)
            {
                string query = VectorQueries.SelectGraphVectorsWithFilters(
                    tenantGuid,
                    labels,
                    tags,
                    filter,
                    _Repo.SelectBatchSize,
                    skip);

                DataTable result = _Repo.ExecuteQuery(query);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
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
            var sortedResults = bestResultsByGraph
                .OrderByDescending(x => x.Value.Score)
                .ThenBy(x => x.Value.Distance)
                .ThenByDescending(x => x.Value.InnerProduct)
                .Take(topK ?? int.MaxValue)
                .ToList();

            foreach (var kvp in sortedResults)
            {
                Graph graph = _Repo.Graph.ReadByGuid(tenantGuid, kvp.Key);
                if (graph != null)
                {
                    kvp.Value.Graph = graph;
                    // Optionally load vectors for the graph
                    graph.Vectors = _Repo.Vector.ReadManyGraph(tenantGuid, graph.GUID).ToList();
                    yield return kvp.Value;
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<VectorSearchResult> SearchNode(
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
            float? minInnerProduct = 0.0f)
        {
            if (vectors == null || vectors.Count < 1) throw new ArgumentException("The supplied vector list must contain at least one vector.");
            if (topK != null && topK.Value < 1) throw new ArgumentOutOfRangeException(nameof(topK));

            // Try to use HNSW index first if available and no complex filtering
            bool canUseIndex = (labels == null || labels.Count == 0) && 
                              (tags == null || tags.Count == 0) && 
                              filter == null;

            if (canUseIndex)
            {
                var graph = _Repo.Graph.ReadByGuid(tenantGuid, graphGuid);
                if (graph != null && graph.VectorIndexType.HasValue && graph.VectorIndexType != VectorIndexTypeEnum.None)
                {
                    // Use HNSW index for fast search
                    var indexedResults = Task.Run(async () => await VectorMethodsIndexExtensions.SearchWithIndexAsync(
                        _Repo, searchType, vectors, graph, topK ?? 100)).Result;

                    if (indexedResults != null)
                    {
                        // Convert indexed results to VectorSearchResult and get node info
                        foreach (var indexResult in indexedResults)
                        {
                            var node = _Repo.Node.ReadByGuid(tenantGuid, indexResult.Id);
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
                string query = VectorQueries.SelectNodeVectorsWithFilters(
                    tenantGuid,
                    graphGuid,
                    labels,
                    tags,
                    filter,
                    _Repo.SelectBatchSize,
                    skip);

                DataTable result = _Repo.ExecuteQuery(query);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
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
            var sortedResults = bestResultsByNode
                .OrderByDescending(x => x.Value.Score)
                .ThenBy(x => x.Value.Distance)
                .ThenByDescending(x => x.Value.InnerProduct)
                .Take(topK ?? int.MaxValue)
                .ToList();

            foreach (var kvp in sortedResults)
            {
                Node node = _Repo.Node.ReadByGuid(tenantGuid, kvp.Key);
                if (node != null)
                {
                    kvp.Value.Node = node;
                    // Optionally load vectors for the node
                    node.Vectors = _Repo.Vector.ReadManyNode(tenantGuid, node.GraphGUID, node.GUID).ToList();
                    yield return kvp.Value;
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<VectorSearchResult> SearchEdge(
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
            float? minInnerProduct = 0.0f)
        {
            if (vectors == null || vectors.Count < 1) throw new ArgumentException("The supplied vector list must contain at least one vector.");
            if (topK != null && topK.Value < 1) throw new ArgumentOutOfRangeException(nameof(topK));

            // Step 1: Get all filtered vectors with a single query that includes all filtering
            List<VectorMetadata> candidateVectors = new List<VectorMetadata>();
            int skip = 0;

            while (true)
            {
                string query = VectorQueries.SelectEdgeVectorsWithFilters(
                    tenantGuid,
                    graphGuid,
                    labels,
                    tags,
                    filter,
                    _Repo.SelectBatchSize,
                    skip);

                DataTable result = _Repo.ExecuteQuery(query);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
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
            var sortedResults = bestResultsByEdge
                .OrderByDescending(x => x.Value.Score)
                .ThenBy(x => x.Value.Distance)
                .ThenByDescending(x => x.Value.InnerProduct)
                .Take(topK ?? int.MaxValue)
                .ToList();

            foreach (var kvp in sortedResults)
            {
                Edge edge = _Repo.Edge.ReadByGuid(tenantGuid, kvp.Key);
                if (edge != null)
                {
                    kvp.Value.Edge = edge;
                    // Optionally load vectors for the edge
                    edge.Vectors = _Repo.Vector.ReadManyEdge(tenantGuid, edge.GraphGUID, edge.GUID).ToList();
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