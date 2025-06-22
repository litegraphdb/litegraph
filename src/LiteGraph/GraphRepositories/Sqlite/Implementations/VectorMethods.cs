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
    using LiteGraph.Serialization;

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
            DataTable createResult = _Repo.ExecuteQuery(insertQuery, true);
            DataTable retrieveResult = _Repo.ExecuteQuery(retrieveQuery, true);
            List<VectorMetadata> created = Converters.VectorsFromDataTable(retrieveResult);
            return created;
        }

        /// <inheritdoc />
        public void DeleteByGuid(Guid tenantGuid, Guid guid)
        {
            VectorMetadata vector = ReadByGuid(tenantGuid, guid);
            if (vector != null)
            {
                _Repo.ExecuteQuery(VectorQueries.Delete(tenantGuid, guid), true);
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
            DataTable result = _Repo.ExecuteQuery(VectorQueries.Select(tenantGuid, guid));
            if (result != null && result.Rows.Count == 1) return Converters.VectorFromDataRow(result.Rows[0]);
            return null;
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
        public EnumerationResult<VectorMetadata> Enumerate(EnumerationQuery query)
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
            return updated;
        }

        /// <inheritdoc />
        public IEnumerable<VectorSearchResult> SearchGraph(
            VectorSearchTypeEnum searchType,
            List<float> vectors,
            Guid tenantGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null)
        {
            if (vectors == null || vectors.Count < 1) throw new ArgumentException("The supplied vector list must contain at least one vector.");

            foreach (Graph graph in _Repo.Graph.ReadMany(tenantGuid, labels, tags, filter))
            {
                graph.Labels = LabelMetadata.ToListString(_Repo.Label.ReadMany(tenantGuid, graph.GUID, null, null, null).ToList());
                graph.Tags = TagMetadata.ToNameValueCollection(_Repo.Tag.ReadMany(tenantGuid, graph.GUID, null, null, null, null).ToList());
                graph.Vectors = _Repo.Vector.ReadManyGraph(tenantGuid, graph.GUID).ToList();

                foreach (VectorMetadata vmd in graph.Vectors)
                {
                    if (vmd.Vectors == null || vmd.Vectors.Count < 1) continue;
                    if (vmd.Vectors.Count != vectors.Count) continue;

                    float? score = null;
                    float? distance = null;
                    float? innerProduct = null;

                    CompareVectors(searchType, vectors, vmd.Vectors, out score, out distance, out innerProduct);

                    yield return new VectorSearchResult
                    {
                        Graph = graph,
                        Score = score,
                        Distance = distance,
                        InnerProduct = innerProduct,
                    };
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
            Expr filter = null)
        {
            if (vectors == null || vectors.Count < 1) throw new ArgumentException("The supplied vector list must contain at least one vector.");

            foreach (Node node in _Repo.Node.ReadMany(tenantGuid, graphGuid, labels, tags, filter))
            {
                node.Labels = LabelMetadata.ToListString(_Repo.Label.ReadMany(tenantGuid, node.GraphGUID, node.GUID, null, null).ToList());
                node.Tags = TagMetadata.ToNameValueCollection(_Repo.Tag.ReadMany(tenantGuid, node.GraphGUID, node.GUID, null, null, null).ToList());
                node.Vectors = _Repo.Vector.ReadManyNode(tenantGuid, node.GraphGUID, node.GUID).ToList();

                foreach (VectorMetadata vmd in node.Vectors)
                { 
                    if (vmd.Vectors == null || vmd.Vectors.Count < 1) continue;
                    if (vmd.Vectors.Count != vectors.Count) continue;

                    float? score = null;
                    float? distance = null;
                    float? innerProduct = null;

                    CompareVectors(searchType, vectors, vmd.Vectors, out score, out distance, out innerProduct);

                    yield return new VectorSearchResult
                    {
                        Node = node,
                        Score = score,
                        Distance = distance,
                        InnerProduct = innerProduct,
                    };
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
            Expr filter = null)
        {
            if (vectors == null || vectors.Count < 1) throw new ArgumentException("The supplied vector list must contain at least one vector.");

            foreach (Edge edge in _Repo.Edge.ReadMany(tenantGuid, graphGuid, labels, tags, filter))
            {
                edge.Labels = LabelMetadata.ToListString(_Repo.Label.ReadMany(tenantGuid, edge.GraphGUID, null, edge.GUID, null).ToList());
                edge.Tags = TagMetadata.ToNameValueCollection(_Repo.Tag.ReadMany(tenantGuid, edge.GraphGUID, null, edge.GUID, null, null).ToList());
                edge.Vectors = _Repo.Vector.ReadManyEdge(tenantGuid, edge.GraphGUID, edge.GUID).ToList();

                foreach (VectorMetadata vmd in edge.Vectors)
                {
                    if (vmd.Vectors == null || vmd.Vectors.Count < 1) continue;
                    if (vmd.Vectors.Count != vectors.Count) continue;

                    float? score = null;
                    float? distance = null;
                    float? innerProduct = null;

                    CompareVectors(searchType, vectors, vmd.Vectors, out score, out distance, out innerProduct);

                    yield return new VectorSearchResult
                    {
                        Edge = edge,
                        Score = score,
                        Distance = distance,
                        InnerProduct = innerProduct,
                    };
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

        #endregion
    }
}
