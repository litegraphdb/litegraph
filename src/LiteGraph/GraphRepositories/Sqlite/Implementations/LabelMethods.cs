using SQLitePCL;
using SyslogLogging;

namespace LiteGraph.GraphRepositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection.Emit;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Serialization;

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
        public LabelMetadata Create(LabelMetadata label)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));
            if (string.IsNullOrEmpty(label.Label)) throw new ArgumentException("The supplied label is null or empty.");
            string createQuery = LabelQueries.Insert(label);
            DataTable createResult = _Repo.ExecuteQuery(createQuery, true);
            LabelMetadata created = Converters.LabelFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public List<LabelMetadata> CreateMany(Guid tenantGuid, List<LabelMetadata> labels)
        {
            if (labels == null || labels.Count < 1) return null;
            string createQuery = LabelQueries.InsertMany(tenantGuid, labels);
            string retrieveQuery = LabelQueries.SelectMany(tenantGuid, labels.Select(n => n.GUID).ToList());
            DataTable createResult = _Repo.ExecuteQuery(createQuery, true);
            DataTable retrieveResult = _Repo.ExecuteQuery(retrieveQuery, true);
            List<LabelMetadata> created = Converters.LabelsFromDataTable(retrieveResult);
            return created;
        }

        /// <inheritdoc />
        public void DeleteByGuid(Guid tenantGuid, Guid guid)
        {
            _Repo.ExecuteQuery(LabelQueries.Delete(tenantGuid, guid), true);
        }

        /// <inheritdoc />
        public void DeleteMany(Guid tenantGuid, Guid? graphGuid, List<Guid> nodeGuids, List<Guid> edgeGuids)
        {
            _Repo.ExecuteQuery(LabelQueries.DeleteMany(tenantGuid, graphGuid, nodeGuids, edgeGuids));
        }

        /// <inheritdoc />
        public void DeleteMany(Guid tenantGuid, List<Guid> guids)
        {
            if (guids == null || guids.Count < 1) return;
            _Repo.ExecuteQuery(LabelQueries.DeleteMany(tenantGuid, guids));
        }

        /// <inheritdoc />
        public void DeleteAllInTenant(Guid tenantGuid)
        {
            _Repo.ExecuteQuery(LabelQueries.DeleteAllInTenant(tenantGuid));
        }

        /// <inheritdoc />
        public void DeleteAllInGraph(Guid tenantGuid, Guid graphGuid)
        {
            _Repo.ExecuteQuery(LabelQueries.DeleteAllInGraph(tenantGuid, graphGuid));
        }

        /// <inheritdoc />
        public void DeleteGraphLabels(Guid tenantGuid, Guid graphGuid)
        {
            _Repo.ExecuteQuery(LabelQueries.DeleteGraph(tenantGuid, graphGuid));
        }

        /// <inheritdoc />
        public void DeleteNodeLabels(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            _Repo.Label.DeleteMany(tenantGuid, graphGuid, new List<Guid> { nodeGuid }, null);
        }

        /// <inheritdoc />
        public void DeleteEdgeLabels(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            _Repo.Label.DeleteMany(tenantGuid, graphGuid, null, new List<Guid> { edgeGuid });
        }

        /// <inheritdoc />
        public bool ExistsByGuid(Guid tenantGuid, Guid labelGuid)
        {
            return (ReadByGuid(tenantGuid, labelGuid) != null);
        }

        /// <inheritdoc />
        public IEnumerable<LabelMetadata> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(LabelQueries.SelectAllInTenant(tenantGuid, _Repo.SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    LabelMetadata label = Converters.LabelFromDataRow(result.Rows[i]);
                    yield return label;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public IEnumerable<LabelMetadata> ReadAllInGraph(
            Guid tenantGuid,
            Guid graphGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(LabelQueries.SelectAllInGraph(tenantGuid, graphGuid, _Repo.SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    LabelMetadata label = Converters.LabelFromDataRow(result.Rows[i]);
                    yield return label;
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public LabelMetadata ReadByGuid(Guid tenantGuid, Guid guid)
        {
            DataTable result = _Repo.ExecuteQuery(LabelQueries.Select(tenantGuid, guid));
            if (result != null && result.Rows.Count == 1) return Converters.LabelFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public IEnumerable<LabelMetadata> ReadMany(
            Guid tenantGuid,
            Guid? graphGuid,
            Guid? nodeGuid,
            Guid? edgeGuid,
            string label,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
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

                DataTable result = _Repo.ExecuteQuery(query);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.LabelFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public IEnumerable<LabelMetadata> ReadManyGraph(Guid tenantGuid, Guid graphGuid, EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                string query = LabelQueries.SelectGraph(
                    tenantGuid,
                    graphGuid,
                    null,
                    _Repo.SelectBatchSize,
                    skip,
                    order);

                DataTable result = _Repo.ExecuteQuery(query);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.LabelFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public IEnumerable<LabelMetadata> ReadManyNode(Guid tenantGuid, Guid graphGuid, Guid nodeGuid, EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                string query = LabelQueries.SelectNode(
                    tenantGuid,
                    graphGuid,
                    nodeGuid,
                    null,
                    _Repo.SelectBatchSize,
                    skip,
                    order);

                DataTable result = _Repo.ExecuteQuery(query);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.LabelFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public IEnumerable<LabelMetadata> ReadManyEdge(Guid tenantGuid, Guid graphGuid, Guid edgeGuid, EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                string query = LabelQueries.SelectEdge(
                    tenantGuid,
                    graphGuid,
                    edgeGuid,
                    null,
                    _Repo.SelectBatchSize,
                    skip,
                    order);

                DataTable result = _Repo.ExecuteQuery(query);
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.LabelFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public EnumerationResult<LabelMetadata> Enumerate(EnumerationQuery query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            LabelMetadata marker = null;

            if (query.TenantGUID != null && query.ContinuationToken != null && query.GraphGUID != null)
            {
                marker = ReadByGuid(query.TenantGUID.Value, query.ContinuationToken.Value);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + query.ContinuationToken.Value + " could not be found.");
            }

            EnumerationResult<LabelMetadata> ret = new EnumerationResult<LabelMetadata>
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
                DataTable result = _Repo.ExecuteQuery(LabelQueries.GetRecordPage(
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
                    ret.Objects = Converters.LabelsFromDataTable(result);
                    LabelMetadata lastItem = ret.Objects.Last();

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
            LabelMetadata marker = null;
            if (tenantGuid != null && graphGuid != null && markerGuid != null)
            {
                marker = ReadByGuid(tenantGuid.Value, markerGuid.Value);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + markerGuid.Value + " could not be found.");
            }

            DataTable result = _Repo.ExecuteQuery(LabelQueries.GetRecordCount(
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
        public LabelMetadata Update(LabelMetadata label)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));
            if (string.IsNullOrEmpty(label.Label)) throw new ArgumentException("The supplied label is null or empty.");

            string updateQuery = LabelQueries.Update(label);
            DataTable updateResult =  _Repo.ExecuteQuery(updateQuery, true);
            LabelMetadata updated = Converters.LabelFromDataRow(updateResult.Rows[0]);
            return updated;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
