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
            DataTable result = _Repo.ExecuteQuery(TenantQueries.Select(guid));
            if (result != null && result.Rows.Count == 1) return Converters.TenantFromDataRow(result.Rows[0]);
            return null;
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
        public TenantMetadata Update(TenantMetadata tenant)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));
            return Converters.TenantFromDataRow(_Repo.ExecuteQuery(TenantQueries.Update(tenant), true).Rows[0]);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
