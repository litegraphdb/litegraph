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
    using ExpressionTree;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Serialization;

    using LoggingSettings = LoggingSettings;

    /// <summary>
    /// Graph methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public class GraphMethods : IGraphMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SqliteGraphRepository _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Graph methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public GraphMethods(SqliteGraphRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public Graph Create(Graph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            string createQuery = GraphQueries.Insert(graph);
            DataTable createResult = _Repo.ExecuteQuery(createQuery, true);
            Graph created = Converters.GraphFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public IEnumerable<Graph> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(GraphQueries.SelectAllInTenant(tenantGuid, _Repo.SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Graph graph = Converters.GraphFromDataRow(result.Rows[i]);
                    yield return graph;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public IEnumerable<Graph> ReadMany(
            Guid tenantGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr graphFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(GraphQueries.SelectMany(
                    tenantGuid,
                    labels,
                    tags,
                    graphFilter,
                    _Repo.SelectBatchSize,
                    skip,
                    order));

                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    Graph graph = Converters.GraphFromDataRow(result.Rows[i]);
                    yield return graph;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public Graph ReadFirst(
            Guid tenantGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr graphFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            DataTable result = _Repo.ExecuteQuery(GraphQueries.SelectMany(
                tenantGuid,
                labels,
                tags,
                graphFilter,
                1,
                0,
                order));

            if (result == null || result.Rows.Count < 1) return null;

            if (result.Rows.Count > 0)
            {
                return Converters.GraphFromDataRow(result.Rows[0]);
            }

            return null;
        }

        /// <inheritdoc />
        public Graph ReadByGuid(Guid tenantGuid, Guid guid)
        {
            DataTable result = _Repo.ExecuteQuery(GraphQueries.Select(tenantGuid, guid));
            if (result != null && result.Rows.Count == 1)
            {
                Graph graph = Converters.GraphFromDataRow(result.Rows[0]);
                return graph;
            }
            return null;
        }

        /// <inheritdoc />
        public EnumerationResult<Graph> Enumerate(EnumerationQuery query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            Graph marker = null;

            if (query.TenantGUID != null && query.ContinuationToken != null)
            {
                marker = ReadByGuid(query.TenantGUID.Value, query.ContinuationToken.Value);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + query.ContinuationToken.Value + " could not be found.");
            }

            EnumerationResult<Graph> ret = new EnumerationResult<Graph>
            {
                MaxResults = query.MaxResults
            };

            ret.Timestamp.Start = DateTime.UtcNow;
            ret.TotalRecords = GetRecordCount(query.TenantGUID, query.Labels, query.Tags, query.Expr, query.Ordering, query.ContinuationToken);

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
                DataTable result = _Repo.ExecuteQuery(GraphQueries.GetRecordPage(
                    query.TenantGUID,
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
                    ret.Objects = Converters.GraphsFromDataTable(result);

                    Graph lastItem = ret.Objects.Last();

                    ret.RecordsRemaining = GetRecordCount(query.TenantGUID, query.Labels, query.Tags, query.Expr, query.Ordering, lastItem.GUID);

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
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Guid? markerGuid = null)
        {
            Graph marker = null;
            if (tenantGuid != null && markerGuid != null)
            {
                marker = ReadByGuid(tenantGuid.Value, markerGuid.Value);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + markerGuid.Value + " could not be found.");
            }

            DataTable result = _Repo.ExecuteQuery(GraphQueries.GetRecordCount(
                tenantGuid,
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
        public Graph Update(Graph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            Graph updated = Converters.GraphFromDataRow(_Repo.ExecuteQuery(GraphQueries.Update(graph), true).Rows[0]);
            return updated;
        }

        /// <inheritdoc />
        public void DeleteAllInTenant(Guid tenantGuid)
        {
            _Repo.ExecuteQuery(GraphQueries.DeleteAllInTenant(tenantGuid), true);
        }

        /// <inheritdoc />
        public void DeleteByGuid(Guid tenantGuid, Guid graphGuid)
        {
            _Repo.ExecuteQuery(GraphQueries.Delete(tenantGuid, graphGuid), true);
        }

        /// <inheritdoc />
        public bool ExistsByGuid(Guid tenantGuid, Guid graphGuid)
        {
            return (ReadByGuid(tenantGuid, graphGuid) != null);
        }

        /// <inheritdoc />
        public Dictionary<Guid, GraphStatistics> GetStatistics(Guid tenantGuid)
        {
            Dictionary<Guid, GraphStatistics> ret = new Dictionary<Guid, GraphStatistics>();
            DataTable table = _Repo.ExecuteQuery(GraphQueries.GetStatistics(tenantGuid, null), true);
            if (table != null && table.Rows.Count > 0)
            {
                foreach (DataRow row in table.Rows)
                {
                    Guid graphGuid = Guid.Parse(row["guid"].ToString());

                    GraphStatistics stats = new GraphStatistics
                    {
                        Nodes = Convert.ToInt32(row["nodes"]),
                        Edges = Convert.ToInt32(row["edges"]),
                        Labels = Convert.ToInt32(row["labels"]),
                        Tags = Convert.ToInt32(row["tags"]),
                        Vectors = Convert.ToInt32(row["vectors"])
                    };

                    ret[graphGuid] = stats;
                }
            }
            return ret;
        }

        /// <inheritdoc />
        public GraphStatistics GetStatistics(Guid tenantGuid, Guid guid)
        {
            DataTable table = _Repo.ExecuteQuery(GraphQueries.GetStatistics(tenantGuid, guid), true);
            if (table != null && table.Rows.Count > 0) return Converters.GraphStatisticsFromDataRow(table.Rows[0]);
            return null;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
