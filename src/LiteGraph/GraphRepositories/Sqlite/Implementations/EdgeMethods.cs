namespace LiteGraph.GraphRepositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.Linq;
    using System.Reflection.Emit;
    using System.Runtime.Serialization.Json;
    using System.Text;
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
        public Edge Create(Edge edge)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));

            string insertQuery = EdgeQueries.Insert(edge);
            DataTable createResult = _Repo.ExecuteQuery(insertQuery, true); 
            Edge created = Converters.EdgeFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public List<Edge> CreateMany(Guid tenantGuid, Guid graphGuid, List<Edge> edges)
        {
            if (edges == null || edges.Count < 1) return new List<Edge>();
            List<Edge> created = new List<Edge>();

            foreach (Edge edge in edges)
            {
                edge.TenantGUID = tenantGuid;
                edge.GraphGUID = graphGuid;
            }

            string insertQuery = EdgeQueries.InsertMany(tenantGuid, edges);
            string retrieveQuery = EdgeQueries.SelectMany(tenantGuid, edges.Select(n => n.GUID).ToList());

            // Execute the entire batch with BEGIN/COMMIT and multi-row INSERTs
            DataTable createResult = _Repo.ExecuteQuery(insertQuery, true);
            DataTable retrieveResult = _Repo.ExecuteQuery(retrieveQuery, true);
            created = Converters.EdgesFromDataTable(retrieveResult);
            return created;
        }

        /// <inheritdoc />
        public IEnumerable<Edge> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(EdgeQueries.SelectAllInTenant(tenantGuid, _Repo.SelectBatchSize, skip, order));
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
        public IEnumerable<Edge> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(EdgeQueries.SelectAllInGraph(tenantGuid, graphGuid, _Repo.SelectBatchSize, skip, order));
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
        public IEnumerable<Edge> ReadMany(
            Guid tenantGuid,
            Guid graphGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(EdgeQueries.SelectMany(
                    tenantGuid, 
                    graphGuid, 
                    name,
                    labels, 
                    tags, 
                    edgeFilter, 
                    _Repo.SelectBatchSize, 
                    skip, 
                    order));

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
        public Edge ReadFirst(
            Guid tenantGuid,
            Guid graphGuid,
            string name = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            DataTable result = _Repo.ExecuteQuery(EdgeQueries.SelectMany(
                tenantGuid, 
                graphGuid, 
                name,
                labels, 
                tags, 
                edgeFilter, 
                1, 
                0, 
                order));

            if (result == null || result.Rows.Count < 1) return null;

            if (result.Rows.Count > 0)
            {
                return Converters.EdgeFromDataRow(result.Rows[0]);
            }

            return null;
        }

        /// <inheritdoc />
        public Edge ReadByGuid(Guid tenantGuid, Guid edgeGuid)
        {
            DataTable result = _Repo.ExecuteQuery(EdgeQueries.SelectByGuid(tenantGuid, edgeGuid));
            if (result != null && result.Rows.Count == 1)
            {
                Edge edge = Converters.EdgeFromDataRow(result.Rows[0]);
                return edge;
            }
            return null;
        }

        /// <inheritdoc />
        public IEnumerable<Edge> ReadByGuids(Guid tenantGuid, List<Guid> guids)
        {
            if (guids == null || guids.Count < 1) yield break;
            DataTable result = _Repo.ExecuteQuery(EdgeQueries.SelectByGuids(tenantGuid, guids));

            if (result == null || result.Rows.Count < 1) yield break;

            for (int i = 0; i < result.Rows.Count; i++)
            {
                yield return Converters.EdgeFromDataRow(result.Rows[i]);
            }
        }

        /// <inheritdoc />
        public IEnumerable<Edge> ReadNodeEdges(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(EdgeQueries.SelectConnected(tenantGuid, graphGuid, nodeGuid, labels, tags, edgeFilter, _Repo.SelectBatchSize, skip, order));
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
        public IEnumerable<Edge> ReadEdgesFromNode(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(EdgeQueries.SelectEdgesFrom(tenantGuid, graphGuid, nodeGuid, labels, tags, edgeFilter, _Repo.SelectBatchSize, skip, order));
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
        public IEnumerable<Edge> ReadEdgesToNode(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(EdgeQueries.SelectEdgesTo(tenantGuid, graphGuid, nodeGuid, labels, tags, edgeFilter, _Repo.SelectBatchSize, skip, order));
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
        public IEnumerable<Edge> ReadEdgesBetweenNodes(
            Guid tenantGuid,
            Guid graphGuid,
            Guid fromNodeGuid,
            Guid toNodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(EdgeQueries.SelectEdgesBetween(tenantGuid, graphGuid, fromNodeGuid, toNodeGuid, labels, tags, edgeFilter, _Repo.SelectBatchSize, skip, order));
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
        public EnumerationResult<Edge> Enumerate(EnumerationRequest query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            Edge marker = null;

            if (query.TenantGUID != null && query.ContinuationToken != null && query.GraphGUID != null)
            {
                marker = ReadByGuid(query.TenantGUID.Value, query.ContinuationToken.Value);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + query.ContinuationToken.Value + " could not be found.");
            }

            EnumerationResult<Edge> ret = new EnumerationResult<Edge>
            {
                MaxResults = query.MaxResults
            };

            ret.Timestamp.Start = DateTime.UtcNow;
            ret.TotalRecords = GetRecordCount(query.TenantGUID, query.GraphGUID, query.Labels, query.Tags, query.Expr, query.Ordering, null);

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
                DataTable result = _Repo.ExecuteQuery(EdgeQueries.GetRecordPage(
                    query.TenantGUID,
                    query.GraphGUID,
                    query.Labels,
                    query.Tags,
                    query.Expr,
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
                    ret.Objects = Converters.EdgesFromDataTable(result);

                    Edge lastItem = ret.Objects.Last();

                    ret.RecordsRemaining = GetRecordCount(query.TenantGUID, query.GraphGUID, query.Labels, query.Tags, query.Expr, query.Ordering, lastItem.GUID);

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
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Guid? markerGuid = null)
        {
            Edge marker = null;
            if (tenantGuid != null && graphGuid != null && markerGuid != null)
            {
                marker = ReadByGuid(tenantGuid.Value, markerGuid.Value);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + markerGuid.Value + " could not be found.");
            }

            DataTable result = _Repo.ExecuteQuery(EdgeQueries.GetRecordCount(
                tenantGuid,
                graphGuid,
                labels,
                tags,
                filter,
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
        public Edge Update(Edge edge)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));
            Edge updated = Converters.EdgeFromDataRow(_Repo.ExecuteQuery(EdgeQueries.Update(edge), true).Rows[0]);
            return updated;
        }

        /// <inheritdoc />
        public void DeleteByGuid(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            _Repo.ExecuteQuery(EdgeQueries.Delete(tenantGuid, graphGuid, edgeGuid), true);
        }

        /// <inheritdoc />
        public void DeleteAllInTenant(Guid tenantGuid)
        {
            _Repo.ExecuteQuery(EdgeQueries.DeleteAllInTenant(tenantGuid), true);
        }

        /// <inheritdoc />
        public void DeleteAllInGraph(Guid tenantGuid, Guid graphGuid)
        {
            _Repo.ExecuteQuery(EdgeQueries.DeleteAllInGraph(tenantGuid, graphGuid), true);
        }

        /// <inheritdoc />
        public void DeleteMany(Guid tenantGuid, Guid graphGuid, List<Guid> edgeGuids)
        {
            if (edgeGuids == null || edgeGuids.Count < 1) return;
            _Repo.ExecuteQuery(EdgeQueries.DeleteMany(tenantGuid, graphGuid, edgeGuids), true);
        }

        /// <inheritdoc />
        public void DeleteNodeEdges(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            DeleteNodeEdges(tenantGuid, graphGuid, new List<Guid> { nodeGuid });
        }

        /// <inheritdoc />
        public void DeleteNodeEdges(Guid tenantGuid, Guid graphGuid, List<Guid> nodeGuids)
        {
            if (nodeGuids == null || nodeGuids.Count < 1) return;
            _Repo.ExecuteQuery(EdgeQueries.DeleteNodeEdges(tenantGuid, graphGuid, nodeGuids), true);
        }

        /// <inheritdoc />
        public bool ExistsByGuid(Guid tenantGuid, Guid edgeGuid)
        {
            return (ReadByGuid(tenantGuid, edgeGuid) != null);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
