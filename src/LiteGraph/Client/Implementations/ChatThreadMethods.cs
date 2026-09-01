namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.Client.Interfaces;
    using LiteGraph.GraphRepositories;

    /// <summary>
    /// Chat thread methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public class ChatThreadMethods : IChatThreadMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphClient _Client = null;
        private GraphRepositoryBase _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Chat thread methods.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="repo">Graph repository.</param>
        public ChatThreadMethods(LiteGraphClient client, GraphRepositoryBase repo)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<ChatThread> Create(ChatThread thread, CancellationToken token = default)
        {
            if (thread == null) throw new ArgumentNullException(nameof(thread));
            token.ThrowIfCancellationRequested();
            await _Client.ValidateTenantExists(thread.TenantGUID, token).ConfigureAwait(false);
            await _Client.ValidateUserExists(thread.TenantGUID, thread.UserGUID, token).ConfigureAwait(false);
            if (thread.GraphGUID != null) await _Client.ValidateGraphExists(thread.TenantGUID, thread.GraphGUID, token).ConfigureAwait(false);
            ChatThread created = await _Repo.ChatThread.Create(thread, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "created chat thread " + created.GUID);
            return created;
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<ChatThread> ReadAllInTenant(
            Guid tenantGuid,
            Guid? userGuid = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            _Client.Logging.Log(SeverityEnum.Debug, "retrieving chat threads");
            await foreach (ChatThread thread in _Repo.ChatThread.ReadAllInTenant(tenantGuid, userGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                yield return thread;
            }
        }

        /// <inheritdoc />
        public async Task<ChatThread> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.ChatThread.ReadByGuid(tenantGuid, guid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<ChatThread>> Enumerate(EnumerationRequest query, CancellationToken token = default)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            token.ThrowIfCancellationRequested();
            return await _Repo.ChatThread.Enumerate(query, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<int> GetRecordCount(
            Guid? tenantGuid,
            Guid? userGuid = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Guid? markerGuid = null,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.ChatThread.GetRecordCount(tenantGuid, userGuid, order, markerGuid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<ChatThread> Update(ChatThread thread, CancellationToken token = default)
        {
            if (thread == null) throw new ArgumentNullException(nameof(thread));
            token.ThrowIfCancellationRequested();
            if (thread.GraphGUID != null) await _Client.ValidateGraphExists(thread.TenantGUID, thread.GraphGUID, token).ConfigureAwait(false);
            ChatThread updated = await _Repo.ChatThread.Update(thread, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "updated chat thread " + updated.GUID);
            return updated;
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ChatFeedback.DeleteByThread(tenantGuid, guid, token).ConfigureAwait(false);
            await _Repo.ChatTurn.DeleteByThread(tenantGuid, guid, token).ConfigureAwait(false);
            await _Repo.ChatThread.DeleteByGuid(tenantGuid, guid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted chat thread " + guid + " and its turns and feedback");
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ChatFeedback.DeleteAllInTenant(tenantGuid, token).ConfigureAwait(false);
            await _Repo.ChatTurn.DeleteAllInTenant(tenantGuid, token).ConfigureAwait(false);
            await _Repo.ChatThread.DeleteAllInTenant(tenantGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted chat threads in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.ChatThread.ExistsByGuid(tenantGuid, guid, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
