namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.Client.Interfaces;
    using LiteGraph.GraphRepositories;

    /// <summary>
    /// Chat settings methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public class ChatSettingsMethods : IChatSettingsMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphClient _Client = null;
        private GraphRepositoryBase _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Chat settings methods.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="repo">Graph repository.</param>
        public ChatSettingsMethods(LiteGraphClient client, GraphRepositoryBase repo)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<ChatSettings> ReadByTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.ChatSettings.ReadByTenant(tenantGuid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<ChatSettings> Upsert(ChatSettings settings, CancellationToken token = default)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            token.ThrowIfCancellationRequested();
            await _Client.ValidateTenantExists(settings.TenantGUID, token).ConfigureAwait(false);

            if (settings.DefaultCompletionEndpointGUID != null)
            {
                ChatEndpoint completion = await _Repo.ChatEndpoint.ReadByGuid(settings.TenantGUID, settings.DefaultCompletionEndpointGUID.Value, token).ConfigureAwait(false);
                if (completion == null) throw new ArgumentException("The specified default completion endpoint could not be found.");
                if (completion.EndpointType != ChatEndpointTypeEnum.Completion)
                    throw new ArgumentException("The specified default completion endpoint is not a completion endpoint.");
            }

            if (settings.DefaultEmbeddingEndpointGUID != null)
            {
                ChatEndpoint embedding = await _Repo.ChatEndpoint.ReadByGuid(settings.TenantGUID, settings.DefaultEmbeddingEndpointGUID.Value, token).ConfigureAwait(false);
                if (embedding == null) throw new ArgumentException("The specified default embedding endpoint could not be found.");
                if (embedding.EndpointType != ChatEndpointTypeEnum.Embedding)
                    throw new ArgumentException("The specified default embedding endpoint is not an embedding endpoint.");
            }

            ChatSettings upserted = await _Repo.ChatSettings.Upsert(settings, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "upserted chat settings for tenant " + upserted.TenantGUID);
            return upserted;
        }

        /// <inheritdoc />
        public async Task DeleteByTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ChatSettings.DeleteByTenant(tenantGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted chat settings for tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.ChatSettings.ExistsByTenant(tenantGuid, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
