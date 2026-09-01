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
    /// Chat feedback methods.
    /// Client implementations are responsible for input validation and cross-cutting logic.
    /// </summary>
    public class ChatFeedbackMethods : IChatFeedbackMethods
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private LiteGraphClient _Client = null;
        private GraphRepositoryBase _Repo = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Chat feedback methods.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="repo">Graph repository.</param>
        public ChatFeedbackMethods(LiteGraphClient client, GraphRepositoryBase repo)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<ChatFeedback> Create(ChatFeedback feedback, CancellationToken token = default)
        {
            if (feedback == null) throw new ArgumentNullException(nameof(feedback));
            token.ThrowIfCancellationRequested();
            await _Client.ValidateTenantExists(feedback.TenantGUID, token).ConfigureAwait(false);
            ChatTurn turn = await _Repo.ChatTurn.ReadByGuid(feedback.TenantGUID, feedback.TurnGUID, token).ConfigureAwait(false);
            if (turn == null) throw new KeyNotFoundException("The specified chat turn could not be found.");
            feedback.ThreadGUID = turn.ThreadGUID;
            ChatFeedback created = await _Repo.ChatFeedback.Create(feedback, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "created chat feedback " + created.GUID);
            return created;
        }

        /// <inheritdoc />
        public async Task<ChatFeedback> ReadByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.ChatFeedback.ReadByGuid(tenantGuid, guid, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async IAsyncEnumerable<ChatFeedback> ReadAllInTenant(
            Guid tenantGuid,
            ChatFeedbackRatingEnum? rating = null,
            Guid? threadGuid = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0,
            [EnumeratorCancellation] CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await foreach (ChatFeedback feedback in _Repo.ChatFeedback.ReadAllInTenant(tenantGuid, rating, threadGuid, order, skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                yield return feedback;
            }
        }

        /// <inheritdoc />
        public async Task<int> GetRecordCount(
            Guid? tenantGuid,
            ChatFeedbackRatingEnum? rating = null,
            CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.ChatFeedback.GetRecordCount(tenantGuid, rating, token).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task DeleteByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ChatFeedback.DeleteByGuid(tenantGuid, guid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted chat feedback " + guid);
        }

        /// <inheritdoc />
        public async Task DeleteByThread(Guid tenantGuid, Guid threadGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ChatFeedback.DeleteByThread(tenantGuid, threadGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted chat feedback in thread " + threadGuid);
        }

        /// <inheritdoc />
        public async Task DeleteAllInTenant(Guid tenantGuid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            await _Repo.ChatFeedback.DeleteAllInTenant(tenantGuid, token).ConfigureAwait(false);
            _Client.Logging.Log(SeverityEnum.Info, "deleted chat feedback in tenant " + tenantGuid);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsByGuid(Guid tenantGuid, Guid guid, CancellationToken token = default)
        {
            token.ThrowIfCancellationRequested();
            return await _Repo.ChatFeedback.ExistsByGuid(tenantGuid, guid, token).ConfigureAwait(false);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
