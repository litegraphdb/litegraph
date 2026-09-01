namespace LiteGraph.Server.API.Agnostic
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.Server.Classes;

    /// <summary>
    /// Agnostic chat handlers: endpoint, thread, turn, feedback, and settings operations.
    /// The chat completion flow itself is owned by the chat service, not this class.
    /// </summary>
    internal partial class ServiceHandler
    {
        #region Chat-Services

        /// <summary>
        /// Chat endpoint health service, notified when endpoints change.  Null until wired at startup.
        /// </summary>
        internal Services.ChatEndpointHealthService ChatHealth { get; set; } = null;

        /// <summary>
        /// Chat service, whose provider client cache is invalidated when endpoints change.  Null until wired at startup.
        /// </summary>
        internal Services.Chat.ChatService Chat { get; set; } = null;

        /// <summary>
        /// Observability service for chat metrics.  Null until wired at startup.
        /// </summary>
        internal Services.ObservabilityService Observability { get; set; } = null;

        #endregion

        #region Chat-Helpers

        private bool CanManageChat(RequestContext req)
        {
            // Managing chat endpoints, chat settings, feedback administration, and all-user history:
            // system admins, or tenant admins in their own tenant.
            return req.Authentication.IsSystemAdmin || (req.Authentication.IsTenantAdmin && IsOwnTenant(req));
        }

        private bool CanUseChat(RequestContext req)
        {
            // Any authenticated member of the tenant may chat and manage their own threads.
            return req.Authentication.IsSystemAdmin || IsOwnTenant(req);
        }

        private bool IsOwnChatPrincipal(RequestContext req, Guid ownerUserGuid)
        {
            return req.Authentication.UserGUID.HasValue
                && req.Authentication.UserGUID.Value.Equals(ownerUserGuid);
        }

        #endregion

        #region Chat-Endpoint-Routes

        internal async Task<ResponseContext> ChatEndpointCreate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.ChatEndpoint == null) throw new ArgumentNullException(nameof(req.ChatEndpoint));
            if (!CanManageChat(req)) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            req.ChatEndpoint.TenantGUID = req.TenantGUID.Value;
            ChatEndpoint obj = await _LiteGraph.ChatEndpoint.Create(req.ChatEndpoint, token).ConfigureAwait(false);
            ChatHealth?.OnEndpointCreatedOrUpdated(obj);
            return new ResponseContext(req, obj.Redact());
        }

        internal async Task<ResponseContext> ChatEndpointReadAll(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!CanManageChat(req)) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            List<ChatEndpoint> objs = new List<ChatEndpoint>();
            await foreach (ChatEndpoint endpoint in _LiteGraph.ChatEndpoint.ReadAllInTenant(req.TenantGUID.Value, req.ChatEndpointTypeFilter, req.Order, req.Skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                objs.Add(endpoint.Redact());
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> ChatEndpointRead(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!CanManageChat(req)) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            ChatEndpoint obj = await _LiteGraph.ChatEndpoint.ReadByGuid(req.TenantGUID.Value, req.ChatEndpointGUID.Value, token).ConfigureAwait(false);
            if (obj != null) return new ResponseContext(req, obj.Redact());
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> ChatEndpointExists(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!CanManageChat(req)) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (await _LiteGraph.ChatEndpoint.ExistsByGuid(req.TenantGUID.Value, req.ChatEndpointGUID.Value, token).ConfigureAwait(false)) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> ChatEndpointUpdate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.ChatEndpoint == null) throw new ArgumentNullException(nameof(req.ChatEndpoint));
            if (!CanManageChat(req)) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            req.ChatEndpoint.TenantGUID = req.TenantGUID.Value;
            req.ChatEndpoint.GUID = req.ChatEndpointGUID.Value;

            try
            {
                ChatEndpoint obj = await _LiteGraph.ChatEndpoint.Update(req.ChatEndpoint, token).ConfigureAwait(false);
                Chat?.InvalidateEndpoint(obj.GUID);
                ChatHealth?.OnEndpointCreatedOrUpdated(obj);
                return new ResponseContext(req, obj.Redact());
            }
            catch (KeyNotFoundException)
            {
                return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            }
        }

        internal async Task<ResponseContext> ChatEndpointDelete(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!CanManageChat(req)) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            ChatEndpoint existing = await _LiteGraph.ChatEndpoint.ReadByGuid(req.TenantGUID.Value, req.ChatEndpointGUID.Value, token).ConfigureAwait(false);
            if (existing == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            await _LiteGraph.ChatEndpoint.DeleteByGuid(req.TenantGUID.Value, req.ChatEndpointGUID.Value, token).ConfigureAwait(false);
            Chat?.InvalidateEndpoint(existing.GUID);
            ChatHealth?.OnEndpointDeleted(existing.TenantGUID, existing.GUID, existing.Name, existing.EndpointType);
            return new ResponseContext(req);
        }

        #endregion

        #region Chat-Thread-Routes

        internal async Task<ResponseContext> ChatThreadCreate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.ChatThread == null) throw new ArgumentNullException(nameof(req.ChatThread));
            if (!CanUseChat(req)) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (!req.Authentication.UserGUID.HasValue)
                return ResponseContext.FromError(req, ApiErrorEnum.BadRequest, null, "Chat threads require a user principal.");

            req.ChatThread.TenantGUID = req.TenantGUID.Value;
            req.ChatThread.UserGUID = req.Authentication.UserGUID.Value;

            try
            {
                ChatThread obj = await _LiteGraph.ChatThread.Create(req.ChatThread, token).ConfigureAwait(false);
                return new ResponseContext(req, obj);
            }
            catch (KeyNotFoundException knfe)
            {
                return ResponseContext.FromError(req, ApiErrorEnum.NotFound, null, knfe.Message);
            }
        }

        internal async Task<ResponseContext> ChatThreadReadAll(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!CanUseChat(req)) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            Guid? userFilter;

            if (req.AllUsers)
            {
                if (!CanManageChat(req)) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
                userFilter = null;
            }
            else
            {
                if (!req.Authentication.UserGUID.HasValue)
                {
                    if (!CanManageChat(req)) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
                    userFilter = null;
                }
                else
                {
                    userFilter = req.Authentication.UserGUID.Value;
                }
            }

            List<ChatThread> objs = new List<ChatThread>();
            await foreach (ChatThread thread in _LiteGraph.ChatThread.ReadAllInTenant(req.TenantGUID.Value, userFilter, req.Order, req.Skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                objs.Add(thread);
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> ChatThreadRead(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!CanUseChat(req)) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            ChatThread obj = await _LiteGraph.ChatThread.ReadByGuid(req.TenantGUID.Value, req.ChatThreadGUID.Value, token).ConfigureAwait(false);
            if (obj == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (!CanManageChat(req) && !IsOwnChatPrincipal(req, obj.UserGUID))
                return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> ChatThreadDelete(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!CanUseChat(req)) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            ChatThread obj = await _LiteGraph.ChatThread.ReadByGuid(req.TenantGUID.Value, req.ChatThreadGUID.Value, token).ConfigureAwait(false);
            if (obj == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (!CanManageChat(req) && !IsOwnChatPrincipal(req, obj.UserGUID))
                return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            await _LiteGraph.ChatThread.DeleteByGuid(req.TenantGUID.Value, req.ChatThreadGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> ChatThreadTurnsRead(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!CanUseChat(req)) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            ChatThread thread = await _LiteGraph.ChatThread.ReadByGuid(req.TenantGUID.Value, req.ChatThreadGUID.Value, token).ConfigureAwait(false);
            if (thread == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (!CanManageChat(req) && !IsOwnChatPrincipal(req, thread.UserGUID))
                return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            List<ChatTurn> objs = new List<ChatTurn>();
            await foreach (ChatTurn turn in _LiteGraph.ChatTurn.ReadByThread(req.TenantGUID.Value, req.ChatThreadGUID.Value, true, req.Skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                objs.Add(turn);
            }

            return new ResponseContext(req, objs);
        }

        #endregion

        #region Chat-Feedback-Routes

        internal async Task<ResponseContext> ChatFeedbackCreate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.ChatFeedback == null) throw new ArgumentNullException(nameof(req.ChatFeedback));
            if (!CanUseChat(req)) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            ChatTurn turn = await _LiteGraph.ChatTurn.ReadByGuid(req.TenantGUID.Value, req.ChatTurnGUID.Value, token).ConfigureAwait(false);
            if (turn == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);

            ChatThread thread = await _LiteGraph.ChatThread.ReadByGuid(req.TenantGUID.Value, turn.ThreadGUID, token).ConfigureAwait(false);
            if (thread != null && !CanManageChat(req) && !IsOwnChatPrincipal(req, thread.UserGUID))
                return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            req.ChatFeedback.TenantGUID = req.TenantGUID.Value;
            req.ChatFeedback.TurnGUID = req.ChatTurnGUID.Value;
            req.ChatFeedback.ThreadGUID = turn.ThreadGUID;
            if (req.Authentication.UserGUID.HasValue) req.ChatFeedback.UserGUID = req.Authentication.UserGUID.Value;

            ChatFeedback obj = await _LiteGraph.ChatFeedback.Create(req.ChatFeedback, token).ConfigureAwait(false);
            Observability?.RecordChatFeedback(obj.Rating);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> ChatFeedbackReadAll(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!CanManageChat(req)) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);

            List<ChatFeedback> objs = new List<ChatFeedback>();
            await foreach (ChatFeedback feedback in _LiteGraph.ChatFeedback.ReadAllInTenant(req.TenantGUID.Value, null, null, req.Order, req.Skip, token).WithCancellation(token).ConfigureAwait(false))
            {
                objs.Add(feedback);
            }

            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> ChatFeedbackRead(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!CanManageChat(req)) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            ChatFeedback obj = await _LiteGraph.ChatFeedback.ReadByGuid(req.TenantGUID.Value, req.ChatFeedbackGUID.Value, token).ConfigureAwait(false);
            if (obj != null) return new ResponseContext(req, obj);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> ChatFeedbackDelete(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!CanManageChat(req)) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (!await _LiteGraph.ChatFeedback.ExistsByGuid(req.TenantGUID.Value, req.ChatFeedbackGUID.Value, token).ConfigureAwait(false))
                return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            await _LiteGraph.ChatFeedback.DeleteByGuid(req.TenantGUID.Value, req.ChatFeedbackGUID.Value, token).ConfigureAwait(false);
            return new ResponseContext(req);
        }

        #endregion

        #region Chat-Settings-Routes

        internal async Task<ResponseContext> ChatSettingsRead(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!CanUseChat(req)) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            ChatSettings obj = await _LiteGraph.ChatSettings.ReadByTenant(req.TenantGUID.Value, token).ConfigureAwait(false);
            if (obj == null) obj = new ChatSettings { TenantGUID = req.TenantGUID.Value };

            // Surface effective defaults: when no explicit default endpoint is stored, report the
            // first active endpoint of each type, matching the orchestrator's own fallback.
            if (obj.DefaultCompletionEndpointGUID == null)
            {
                await foreach (ChatEndpoint candidate in _LiteGraph.ChatEndpoint.ReadAllInTenant(req.TenantGUID.Value, ChatEndpointTypeEnum.Completion, EnumerationOrderEnum.CreatedAscending, 0, token).ConfigureAwait(false))
                {
                    if (candidate.Active) { obj.DefaultCompletionEndpointGUID = candidate.GUID; break; }
                }
            }

            if (obj.DefaultEmbeddingEndpointGUID == null)
            {
                await foreach (ChatEndpoint candidate in _LiteGraph.ChatEndpoint.ReadAllInTenant(req.TenantGUID.Value, ChatEndpointTypeEnum.Embedding, EnumerationOrderEnum.CreatedAscending, 0, token).ConfigureAwait(false))
                {
                    if (candidate.Active) { obj.DefaultEmbeddingEndpointGUID = candidate.GUID; break; }
                }
            }

            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> ChatSettingsUpdate(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.ChatSettings == null) throw new ArgumentNullException(nameof(req.ChatSettings));
            if (!CanManageChat(req)) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            req.ChatSettings.TenantGUID = req.TenantGUID.Value;

            try
            {
                ChatSettings obj = await _LiteGraph.ChatSettings.Upsert(req.ChatSettings, token).ConfigureAwait(false);
                return new ResponseContext(req, obj);
            }
            catch (ArgumentException ae)
            {
                return ResponseContext.FromError(req, ApiErrorEnum.BadRequest, null, ae.Message);
            }
        }

        #endregion
    }
}
