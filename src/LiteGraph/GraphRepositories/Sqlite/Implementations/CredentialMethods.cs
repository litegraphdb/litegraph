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
    using Timestamps;

    using LoggingSettings = LoggingSettings;

    /// <summary>
    /// Credential methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public class CredentialMethods : ICredentialMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private SqliteGraphRepository _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Credential methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public CredentialMethods(SqliteGraphRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public Credential Create(Credential cred)
        {
            if (cred == null) throw new ArgumentNullException(nameof(cred));
            string createQuery = CredentialQueries.Insert(cred);
            DataTable createResult = _Repo.ExecuteQuery(createQuery, true);
            Credential created = Converters.CredentialFromDataRow(createResult.Rows[0]);
            return created;
        }

        /// <inheritdoc />
        public void DeleteAllInTenant(Guid tenantGuid)
        {
            _Repo.ExecuteQuery(CredentialQueries.DeleteAllInTenant(tenantGuid), true);
        }

        /// <inheritdoc />
        public void DeleteByGuid(Guid tenantGuid, Guid guid)
        {
            _Repo.ExecuteQuery(CredentialQueries.Delete(tenantGuid, guid), true);
        }

        /// <inheritdoc />
        public void DeleteByUser(Guid tenantGuid, Guid userGuid)
        {
            _Repo.ExecuteQuery(CredentialQueries.DeleteUser(tenantGuid, userGuid), true);
        }

        /// <inheritdoc />
        public bool ExistsByGuid(Guid tenantGuid, Guid guid)
        {
            return (ReadByGuid(tenantGuid, guid) != null);
        }

        /// <inheritdoc />
        public IEnumerable<Credential> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(CredentialQueries.SelectAllInTenant(
                    tenantGuid,
                    _Repo.SelectBatchSize,
                    skip,
                    order));

                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.CredentialFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public Credential ReadByGuid(Guid tenantGuid, Guid guid)
        {
            DataTable result = _Repo.ExecuteQuery(CredentialQueries.SelectByGuid(tenantGuid, guid));
            if (result != null && result.Rows.Count == 1) return Converters.CredentialFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public Credential ReadByBearerToken(string bearerToken)
        {
            if (String.IsNullOrEmpty(bearerToken)) throw new ArgumentNullException(nameof(bearerToken));
            DataTable result = _Repo.ExecuteQuery(CredentialQueries.SelectByToken(bearerToken));
            if (result != null && result.Rows.Count == 1) return Converters.CredentialFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public IEnumerable<Credential> ReadMany(
            Guid? tenantGuid,
            Guid? userGuid,
            string bearerToken,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (skip < 0) throw new ArgumentOutOfRangeException(nameof(skip));

            while (true)
            {
                DataTable result = _Repo.ExecuteQuery(CredentialQueries.Select(
                    tenantGuid,
                    userGuid,
                    bearerToken,
                    _Repo.SelectBatchSize,
                    skip,
                    order));

                if (result == null || result.Rows.Count < 1) break;

                for (int i = 0; i < result.Rows.Count; i++)
                {
                    yield return Converters.CredentialFromDataRow(result.Rows[i]);
                    skip++;
                }

                if (result.Rows.Count < _Repo.SelectBatchSize) break;
            }
        }

        /// <inheritdoc />
        public EnumerationResult<Credential> Enumerate(EnumerationQuery query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));

            Credential marker = null;

            if (query.TenantGUID != null && query.ContinuationToken != null)
            {
                marker = ReadByGuid(query.TenantGUID.Value, query.ContinuationToken.Value);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + query.ContinuationToken + " could not be found.");
            }

            EnumerationResult<Credential> ret = new EnumerationResult<Credential>
            {
                MaxResults = query.MaxResults
            };

            ret.Timestamp.Start = DateTime.UtcNow;

            ret.TotalRecords = GetRecordCount(query.TenantGUID, query.UserGUID, query.Ordering, query.ContinuationToken);

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
                DataTable result = _Repo.ExecuteQuery(CredentialQueries.GetRecordPage(
                    query.TenantGUID,
                    query.UserGUID,
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
                    ret.Objects = Converters.CredentialFromDataTable(result);

                    Credential lastItem = ret.Objects.Last();

                    ret.RecordsRemaining = GetRecordCount(query.TenantGUID, query.UserGUID, query.Ordering, lastItem.GUID);
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
            Guid? userGuid, 
            EnumerationOrderEnum order, 
            Guid? markerGuid)
        {
            Credential marker = null;

            if (tenantGuid != null && markerGuid != null)
            {
                marker = ReadByGuid(tenantGuid.Value, markerGuid.Value);
                if (marker == null) throw new KeyNotFoundException("The object associated with the supplied marker GUID " + markerGuid.Value + " could not be found.");
            }

            DataTable result = _Repo.ExecuteQuery(CredentialQueries.GetRecordCount(
                tenantGuid,
                userGuid,
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
        public Credential Update(Credential cred)
        {
            if (cred == null) throw new ArgumentNullException(nameof(cred));
            return Converters.CredentialFromDataRow(_Repo.ExecuteQuery(CredentialQueries.Update(cred), true).Rows[0]);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
