namespace LiteGraph.GraphRepositories.Postgresql.Implementations
{
    using System;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories.Interfaces;
    using LiteGraph.GraphRepositories.Postgresql;
    using LiteGraph.GraphRepositories.Postgresql.Queries;

    /// <summary>
    /// Chat settings methods.
    /// Graph repository base methods are responsible only for primitives, not input validation or cross-cutting.
    /// </summary>
    public class ChatSettingsMethods : IChatSettingsMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private PostgresqlGraphRepository _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Chat settings methods.
        /// </summary>
        /// <param name="repo">Graph repository.</param>
        public ChatSettingsMethods(PostgresqlGraphRepository repo)
        {
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<ChatSettings> ReadByTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            DataTable result = await _Repo.ExecuteQueryAsync(ChatSettingsQueries.SelectByTenant(tenantGuid), false, token).ConfigureAwait(false);
            if (result != null && result.Rows.Count == 1) return Converters.ChatSettingsFromDataRow(result.Rows[0]);
            return null;
        }

        /// <inheritdoc />
        public async Task<ChatSettings> Upsert(ChatSettings settings, CancellationToken token = default)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            token.ThrowIfCancellationRequested();

            ChatSettings existing = await ReadByTenant(settings.TenantGUID, token).ConfigureAwait(false);
            DataTable result;

            if (existing == null)
            {
                result = await _Repo.ExecuteQueryAsync(ChatSettingsQueries.Insert(settings), true, token).ConfigureAwait(false);
            }
            else
            {
                result = await _Repo.ExecuteQueryAsync(ChatSettingsQueries.Update(settings), true, token).ConfigureAwait(false);
            }

            return Converters.ChatSettingsFromDataRow(result.Rows[0]);
        }

        /// <inheritdoc />
        public async Task DeleteByTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ExecuteQueryAsync(ChatSettingsQueries.Delete(tenantGuid), true, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            ChatSettings settings = await ReadByTenant(tenantGuid, token).ConfigureAwait(false);
            return (settings != null);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
