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
    using PrettyId;

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
