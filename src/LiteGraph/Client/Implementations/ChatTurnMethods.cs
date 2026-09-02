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
    /// Chat turn methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public class ChatTurnMethods : IChatTurnMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphClient _Client = null;
        private GraphRepositoryBase _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Chat turn methods.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="repo">Graph repository.</param>
        public ChatTurnMethods(LiteGraphClient client, GraphRepositoryBase repo)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<ChatTurn> Create(ChatTurn turn, CancellationToken token = default)
        {
            if (turn == null) throw new ArgumentNullException(nameof(turn));
            token.ThrowIfCancellationRequested();
            await _Client.ValidateTenantExists(turn.TenantGUID, token).ConfigureAwait(false);
            bool threadExists = await _Repo.ChatThread.ExistsByGuid(turn.TenantGUID, turn.ThreadGUID, token).ConfigureAwait(false);
            if (!threadExists) throw new KeyNotFoundException("The specified chat thread could not be found.");
            ChatTurn created = await _Repo.ChatTurn.Create(turn, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Debug, "created chat turn " + created.GUID);
            return created;
        }

        /// <inheritdoc />
        public async Task<ChatTurn> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.ChatTurn.ReadByGuid(tenantGuid, guid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<ChatTurn> ReadByThread(
            Guid tenantGuid,
            Guid threadGuid,
            bool ascending = true,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await foreach (ChatTurn turn in _Repo.ChatTurn.ReadByThread(tenantGuid, threadGuid, ascending, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                yield return turn;
            }
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<ChatTurn> ReadAllInTenant(
            Guid tenantGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await foreach (ChatTurn turn in _Repo.ChatTurn.ReadAllInTenant(tenantGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                yield return turn;
            }
        }

        /// <inheritdoc />
        public async Task<EnumerationResult<ChatTurn>> Enumerate(EnumerationRequest query, Guid threadGuid, CancellationToken token = default)
        {
            if (query == null) query = new EnumerationRequest();
            token.ThrowIfCancellationRequested();
            return await _Repo.ChatTurn.Enumerate(query, threadGuid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<int> GetRecordCount(
            Guid? tenantGuid,
            Guid? threadGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            Guid? markerGuid = null,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.ChatTurn.GetRecordCount(tenantGuid, threadGuid, order, markerGuid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<int> GetCountByThread(Guid tenantGuid, Guid threadGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.ChatTurn.GetCountByThread(tenantGuid, threadGuid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<int> GetMaxSequence(Guid tenantGuid, Guid threadGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.ChatTurn.GetMaxSequence(tenantGuid, threadGuid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ChatTurn.DeleteByGuid(tenantGuid, guid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted chat turn " + guid);
        }

        /// <inheritdoc />
        public async Task DeleteByThread(Guid tenantGuid, Guid threadGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ChatTurn.DeleteByThread(tenantGuid, threadGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted chat turns in thread " + threadGuid);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ChatTurn.DeleteAllInTenant(tenantGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted chat turns in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public async Task DeleteOlderThan(Guid tenantGuid, DateTime olderThanUtc, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ChatTurn.DeleteOlderThan(tenantGuid, olderThanUtc, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Debug, "pruned chat turns in tenant " + tenantGuid + " older than " + olderThanUtc.ToString("o"));
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.ChatTurn.ExistsByGuid(tenantGuid, guid, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
