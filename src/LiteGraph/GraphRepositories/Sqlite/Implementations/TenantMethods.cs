namespace LiteGraph.GraphRepositories.Sqlite.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Runtime.Serialization.Json;
    using System.Text;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.GraphRepositories.Sqlite.Queries;
    using LiteGraph.Serialization;

    using LoggingSettings = LoggingSettings;

    /// <summary>
    /// Tenant methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public class TenantMethods : ITenantMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SqliteGraphRepository _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Tenant methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public TenantMethods(SqliteGraphRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public TenantMetadata Create(TenantMetadata tenant)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));
            string createQuery = TenantQueries.Insert(tenant);
            DataTable createResult = _Repo.ExecuteQuery(createQuery, true);
            TenantMetadata created = Converters.TenantFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public void DeleteByGuid(Guid guid, bool force = false)
        {
            _Repo.ExecuteQuery(TenantQueries.Delete(guid), true);
        }

        /// <inheritdoc />
        public bool ExistsByGuid(Guid tenantGuid)
        {
            return (ReadByGuid(tenantGuid) != null);
        }

        /// <inheritdoc />
        public TenantMetadata ReadByGuid(Guid guid)
        {
            DataTable result = _Repo.ExecuteQuery(TenantQueries.SelectByGuid(guid));
            if (result != null && result.Rows.Count == 1) return Converters.TenantFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public IEnumerable<TenantMetadata> ReadByGuids(List<Guid> guids)
        {
            if (guids == null || guids.Count < 1) yield break;
            DataTable result = _Repo.ExecuteQuery(TenantQueries.SelectByGuids(guids));

            if (result == null || result.Rows.Count < 1) yield break;

            for (int i = 0; i < result.Rows.Count; i++)
            {
                yield return Converters.TenantFromDataRow(result.Rows[i]);
            }
        }

        /// <inheritdoc />
        public IEnumerable<TenantMetadata> ReadMany(
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(TenantQueries.SelectMany(_Repo.SelectBatchSize, skip, order));
                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.TenantFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public EnumerationResult<TenantMetadata> Enumerate(EnumerationRequest query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            TenantMetadata marker = null;

            if (query.ContinuationToken != null)
            {
                marker = ReadByGuid(query.ContinuationToken.Value);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + query.ContinuationToken.Value + " could not be found.");
            }

            EnumerationResult<TenantMetadata> ret = new EnumerationResult<TenantMetadata>
            {
                MaxResults = query.MaxResults
            };

            ret.Timestamp.Start = DateTime.UtcNow;

            ret.TotalRecords = GetRecordCount(query.Ordering, query.ContinuationToken);

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
                Console.WriteLine("Yay");

                DataTable result = _Repo.ExecuteQuery(TenantQueries.GetRecordPage(
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
                    ret.Objects = Converters.TenantsFromDataTable(result);

                    TenantMetadata lastItem = ret.Objects.Last();

                    ret.RecordsRemaining = GetRecordCount(query.Ordering, lastItem.GUID);

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
        public int GetRecordCount(EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending, Guid? markerGuid = null)
        {
            TenantMetadata marker = null;
            if (markerGuid != null)
            {
                marker = ReadByGuid(markerGuid.Value);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + markerGuid.Value + " could not be found.");
            }

            DataTable result = _Repo.ExecuteQuery(TenantQueries.GetRecordCount(
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
        public TenantMetadata Update(TenantMetadata tenant)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));
            return Converters.TenantFromDataRow(_Repo.ExecuteQuery(TenantQueries.Update(tenant), true).Rows[0]);
        }

        /// <inheritdoc />
        public Dictionary<Guid, TenantStatistics> GetStatistics()
        {
            Dictionary<Guid, TenantStatistics> ret = new Dictionary<Guid, TenantStatistics>();
            DataTable table = _Repo.ExecuteQuery(TenantQueries.GetStatistics(null), true);
            if (table != null && table.Rows.Count > 0)
            {
                foreach (DataRow row in table.Rows)
                {
                    ret.Add(Guid.Parse(row["guid"].ToString()), Converters.TenantStatisticsFromDataRow(row));
                }
            }
            return ret;
        }

        /// <inheritdoc />
        public TenantStatistics GetStatistics(Guid tenantGuid)
        {
            DataTable table = _Repo.ExecuteQuery(TenantQueries.GetStatistics(tenantGuid), true);
            if (table != null && table.Rows.Count > 0) return Converters.TenantStatisticsFromDataRow(table.Rows[0]);
            return null;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
