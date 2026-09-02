namespace LiteGraph.Server.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.GraphRepositories;
    using LiteGraph.Server.Classes;
    using SyslogLogging;

    /// <summary>
    /// Central authorization policy evaluator.
    /// </summary>
    public class AuthorizationService
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private readonly string _Header = "[AuthorizationService] ";
        private readonly LoggingModule _Logging = null;
        private readonly GraphRepositoryBase _Repo = null;
        private readonly ConcurrentDictionary<string, AuthorizationEffectivePolicy> _EffectivePolicyCache = new ConcurrentDictionary<string, AuthorizationEffectivePolicy>(StringComparer.Ordinal);
        private readonly ConcurrentDictionary<string, RoleDefinition> _RoleCache = new ConcurrentDictionary<string, RoleDefinition>(StringComparer.Ordinal);
        private readonly object _CacheLock = new object();
        private long _ObservedPolicyVersion = AuthorizationPolicyChangeTracker.Version;
        private long _PolicyCacheHits = 0;
        private long _PolicyCacheMisses = 0;
        private long _PolicyCacheInvalidations = 0;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Authorization service.
        /// </summary>
        /// <param name="logging">Logging module.</param>
        /// <param name="repo">Graph repository.</param>
        public AuthorizationService(LoggingModule logging, GraphRepositoryBase repo = null)
        {
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _Repo = repo;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Get authorization cache statistics.
        /// </summary>
        /// <returns>Cache statistics.</returns>
        public AuthorizationCacheStatistics GetCacheStatistics()
        {
            EnsureAuthorizationCacheCurrent();

            return new AuthorizationCacheStatistics(
                _EffectivePolicyCache.Count,
                _RoleCache.Count,
                Interlocked.Read(ref _PolicyCacheHits),
                Interlocked.Read(ref _PolicyCacheMisses),
                Interlocked.Read(ref _PolicyCacheInvalidations),
                AuthorizationPolicyChangeTracker.Version);
        }

        /// <summary>
        /// Authorize a request.
        /// </summary>
        /// <param name="req">Request context.</param>
        /// <param name="token">Cancellation token.</param>
        public async Task Authorize(RequestContext req, CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            token.ThrowIfCancellationRequested();

            // System administrators (break-glass token or a user with the IsSystemAdmin flag) are server-wide superusers
            // and bypass tenant and scope checks entirely.
            if (req.Authentication.IsSystemAdmin)
            {
                await ResolveGraphGuidForAuthorization(req, token).ConfigureAwait(false);
                ApplyDecision(req, AuthorizationDecision.Permit(RequiredScope(req), AuthorizationDecisionReason.Permitted));
                return;
            }

            AuthorizationDecision tenantDecision = EvaluateTenantAccess(req.Authentication.IsSystemAdmin, req.TenantGUID, req.Authentication.TenantGUID);
            if (tenantDecision.Result != AuthorizationResultEnum.Permitted)
            {
                _Logging.Warn(_Header + "attempt to access tenant " + req.TenantGUID + " from tenant " + req.Authentication.TenantGUID);
                ApplyDecision(req, tenantDecision);
                return;
            }

            await ResolveGraphGuidForAuthorization(req, token).ConfigureAwait(false);

            // Tenant administrators have full rights within their own tenant.
            if (req.Authentication.IsTenantAdmin
                && req.TenantGUID.HasValue
                && req.Authentication.TenantGUID.HasValue
                && req.TenantGUID.Value.Equals(req.Authentication.TenantGUID.Value))
            {
                ApplyDecision(req, AuthorizationDecision.Permit(RequiredScope(req), AuthorizationDecisionReason.Permitted));
                return;
            }

            // v8 self-service: a user may read, check, or update their own user record even without an explicit RBAC role.
            if ((req.RequestType == RequestTypeEnum.UserRead
                    || req.RequestType == RequestTypeEnum.UserUpdate
                    || req.RequestType == RequestTypeEnum.UserExists)
                && req.UserGUID.HasValue
                && req.Authentication.UserGUID.HasValue
                && req.UserGUID.Value.Equals(req.Authentication.UserGUID.Value)
                && req.TenantGUID.HasValue
                && req.Authentication.TenantGUID.HasValue
                && req.TenantGUID.Value.Equals(req.Authentication.TenantGUID.Value))
            {
                ApplyDecision(req, AuthorizationDecision.Permit(RequiredScope(req), AuthorizationDecisionReason.Permitted));
                return;
            }

            AuthorizationDecision accessDecision = await EvaluateRequestAccess(req, RequiredScope(req), RequiredResourceType(req.RequestType), token).ConfigureAwait(false);
            if (accessDecision.Result != AuthorizationResultEnum.Permitted)
            {
                if (accessDecision.Reason == AuthorizationDecisionReason.GraphDenied && req.Authentication.Credential != null)
                {
                    _Logging.Warn(_Header + "credential " + req.Authentication.Credential.GUID + " attempted to access graph " + req.GraphGUID);
                }
                else if (accessDecision.Reason == AuthorizationDecisionReason.MissingScope && req.Authentication.Credential != null)
                {
                    _Logging.Warn(_Header + "credential " + req.Authentication.Credential.GUID + " missing required scope " + accessDecision.RequiredScope + " for " + req.RequestType);
                }

                ApplyDecision(req, accessDecision);
                return;
            }

            ApplyDecision(req, accessDecision);
        }

        /// <summary>
        /// Evaluate whether a request can access the requested tenant.
        /// </summary>
        /// <param name="isAdmin">True if the request uses administrator authentication.</param>
        /// <param name="requestedTenantGuid">Requested tenant GUID.</param>
        /// <param name="authenticatedTenantGuid">Authenticated tenant GUID.</param>
        /// <returns>Authorization decision.</returns>
        public AuthorizationDecision EvaluateTenantAccess(bool isAdmin, Guid? requestedTenantGuid, Guid? authenticatedTenantGuid)
        {
            if (isAdmin) return AuthorizationDecision.Permit(null, AuthorizationDecisionReason.Permitted);
            if (requestedTenantGuid == null) return AuthorizationDecision.Permit(null, AuthorizationDecisionReason.Permitted);
            if (requestedTenantGuid.Equals(authenticatedTenantGuid)) return AuthorizationDecision.Permit(null, AuthorizationDecisionReason.Permitted);
            return AuthorizationDecision.Deny(null, AuthorizationDecisionReason.TenantDenied);
        }

        /// <summary>
        /// Evaluate whether a credential can perform an operation against a graph.
        /// </summary>
        /// <param name="credential">Credential.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="requestType">Request type.</param>
        /// <returns>Authorization decision.</returns>
        public AuthorizationDecision EvaluateCredential(Credential credential, Guid? graphGuid, RequestTypeEnum requestType)
        {
            return EvaluateCredential(credential, graphGuid, RequiredScope(requestType));
        }

        /// <summary>
        /// Evaluate whether a credential can perform an operation against a graph.
        /// </summary>
        /// <param name="credential">Credential.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="requiredScope">Required scope.</param>
        /// <returns>Authorization decision.</returns>
        public AuthorizationDecision EvaluateCredential(Credential credential, Guid? graphGuid, string requiredScope)
        {
            if (credential == null) return AuthorizationDecision.Permit(requiredScope, AuthorizationDecisionReason.NoCredential);

            if (!credential.CanAccessGraph(graphGuid))
            {
                return AuthorizationDecision.Deny(requiredScope, AuthorizationDecisionReason.GraphDenied);
            }

            if (!credential.HasScope(requiredScope))
            {
                return AuthorizationDecision.Deny(requiredScope, AuthorizationDecisionReason.MissingScope);
            }

            return AuthorizationDecision.Permit(requiredScope, AuthorizationDecisionReason.Permitted);
        }

        /// <summary>
        /// Evaluate effective request access using legacy credential fields and stored user/credential assignments where present.
        /// </summary>
        /// <param name="req">Request context.</param>
        /// <param name="requiredScope">Required scope.</param>
        /// <param name="resourceType">Required resource type.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Authorization decision.</returns>
        public async Task<AuthorizationDecision> EvaluateRequestAccess(
            RequestContext req,
            string requiredScope,
            AuthorizationResourceTypeEnum resourceType,
            CancellationToken token = default)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            token.ThrowIfCancellationRequested();

            if (req.Authentication.Credential != null)
            {
                return await EvaluateCredentialEffectiveAccess(
                    req.Authentication.Credential,
                    req.GraphGUID,
                    requiredScope,
                    resourceType,
                    req.RequestType,
                    token).ConfigureAwait(false);
            }

            if (req.Authentication.TenantGUID.HasValue && req.Authentication.UserGUID.HasValue)
            {
                return await EvaluateUserEffectiveAccess(
                    req.Authentication.TenantGUID.Value,
                    req.Authentication.UserGUID.Value,
                    req.GraphGUID,
                    requiredScope,
                    resourceType,
                    req.RequestType,
                    token).ConfigureAwait(false);
            }

            return AuthorizationDecision.Permit(requiredScope, AuthorizationDecisionReason.NoCredential);
        }

        /// <summary>
        /// Evaluate effective credential access using legacy credential fields and stored credential-scope assignments where present.
        /// </summary>
        /// <param name="credential">Credential.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="requestType">Request type.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Authorization decision.</returns>
        public Task<AuthorizationDecision> EvaluateCredentialEffectiveAccess(
            Credential credential,
            Guid? graphGuid,
            RequestTypeEnum requestType,
            CancellationToken token = default)
        {
            return EvaluateCredentialEffectiveAccess(
                credential,
                graphGuid,
                RequiredScope(requestType),
                RequiredResourceType(requestType),
                requestType,
                token);
        }

        /// <summary>
        /// Evaluate effective credential access using legacy credential fields and stored credential-scope assignments where present.
        /// </summary>
        /// <param name="credential">Credential.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="requiredScope">Required scope.</param>
        /// <param name="resourceType">Required resource type.</param>
        /// <param name="requestType">Request type.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Authorization decision.</returns>
        public async Task<AuthorizationDecision> EvaluateCredentialEffectiveAccess(
            Credential credential,
            Guid? graphGuid,
            string requiredScope,
            AuthorizationResourceTypeEnum resourceType,
            RequestTypeEnum requestType,
            CancellationToken token = default)
        {
            AuthorizationDecision legacyDecision = EvaluateCredential(credential, graphGuid, requiredScope);
            if (legacyDecision.Result != AuthorizationResultEnum.Permitted) return legacyDecision;
            if (credential == null || _Repo == null) return legacyDecision;

            AuthorizationEffectivePolicy policy = await GetCredentialEffectivePolicy(credential.TenantGUID, credential.GUID, token).ConfigureAwait(false);
            if (policy.AssignmentCount < 1) return legacyDecision;

            AuthorizationPermissionEnum permission = RequiredPermission(requiredScope, requestType);
            AuthorizationDecision policyDecision = EvaluateCredentialPolicy(
                policy,
                graphGuid,
                requiredScope,
                permission,
                resourceType);

            if (policyDecision.Result != AuthorizationResultEnum.Permitted
                && IsMemberLevelChatRequest(permission, resourceType))
            {
                // Member-level chat operations remain available to any tenant member, mirroring the
                // no-assignment fallback: graph-scoped grants must not lock a principal out of chat.
                return AuthorizationDecision.Permit(requiredScope, AuthorizationDecisionReason.Permitted);
            }

            return policyDecision;
        }

        /// <summary>
        /// Evaluate effective user access using stored user-role assignments where present.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="userGuid">User GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="requestType">Request type.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Authorization decision.</returns>
        public Task<AuthorizationDecision> EvaluateUserEffectiveAccess(
            Guid tenantGuid,
            Guid userGuid,
            Guid? graphGuid,
            RequestTypeEnum requestType,
            CancellationToken token = default)
        {
            return EvaluateUserEffectiveAccess(
                tenantGuid,
                userGuid,
                graphGuid,
                RequiredScope(requestType),
                RequiredResourceType(requestType),
                requestType,
                token);
        }

        /// <summary>
        /// Evaluate effective user access using stored user-role assignments where present.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="userGuid">User GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="requiredScope">Required scope.</param>
        /// <param name="resourceType">Required resource type.</param>
        /// <param name="requestType">Request type.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Authorization decision.</returns>
        public async Task<AuthorizationDecision> EvaluateUserEffectiveAccess(
            Guid tenantGuid,
            Guid userGuid,
            Guid? graphGuid,
            string requiredScope,
            AuthorizationResourceTypeEnum resourceType,
            RequestTypeEnum requestType,
            CancellationToken token = default)
        {
            if (_Repo == null) return AuthorizationDecision.Permit(requiredScope, AuthorizationDecisionReason.NoCredential);

            AuthorizationEffectivePolicy policy = await GetUserEffectivePolicy(tenantGuid, userGuid, token).ConfigureAwait(false);
            if (policy.AssignmentCount < 1)
            {
                if (RequiredPermission(requiredScope, requestType) == AuthorizationPermissionEnum.Admin)
                    return AuthorizationDecision.Deny(requiredScope, AuthorizationDecisionReason.MissingScope);

                return AuthorizationDecision.Permit(requiredScope, AuthorizationDecisionReason.NoCredential);
            }

            AuthorizationPermissionEnum permission = RequiredPermission(requiredScope, requestType);
            AuthorizationDecision policyDecision = EvaluateUserPolicy(
                policy,
                graphGuid,
                requiredScope,
                permission,
                resourceType);

            if (policyDecision.Result != AuthorizationResultEnum.Permitted
                && IsMemberLevelChatRequest(permission, resourceType))
            {
                // Member-level chat operations remain available to any tenant member, mirroring the
                // no-assignment fallback: graph-scoped grants must not lock a principal out of chat.
                return AuthorizationDecision.Permit(requiredScope, AuthorizationDecisionReason.Permitted);
            }

            return policyDecision;
        }

        /// <summary>
        /// Determine whether the authenticated principal holds an effective grant for the supplied permission and resource type within its own tenant.
        /// Evaluates the same cached user-role and credential-scope grants used by the authorize path.  Graph-scoped grants do not qualify.
        /// System and tenant administrator flags are not considered; callers should check those flags separately.
        /// </summary>
        /// <param name="authentication">Authentication context.</param>
        /// <param name="permission">Required permission.</param>
        /// <param name="resourceType">Required resource type.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>True when an effective grant covers the permission and resource type.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the authentication context is null.</exception>
        public async Task<bool> HasEffectiveGrant(
            AuthenticationContext authentication,
            AuthorizationPermissionEnum permission,
            AuthorizationResourceTypeEnum resourceType,
            CancellationToken token = default)
        {
            if (authentication == null) throw new ArgumentNullException(nameof(authentication));
            token.ThrowIfCancellationRequested();

            if (_Repo == null) return false;
            if (!authentication.TenantGUID.HasValue) return false;

            Guid tenantGuid = authentication.TenantGUID.Value;

            if (authentication.UserGUID.HasValue)
            {
                AuthorizationEffectivePolicy userPolicy = await GetUserEffectivePolicy(tenantGuid, authentication.UserGUID.Value, token).ConfigureAwait(false);
                if (userPolicy.Grants.Any(grant =>
                    AssignmentAppliesToGraph(grant.ResourceScope, grant.GraphGUID, grant.Role, null)
                    && RoleGrants(grant.Role, permission, resourceType)))
                {
                    return true;
                }
            }

            if (authentication.Credential != null)
            {
                AuthorizationEffectivePolicy credentialPolicy = await GetCredentialEffectivePolicy(authentication.Credential.TenantGUID, authentication.Credential.GUID, token).ConfigureAwait(false);
                if (credentialPolicy.Grants.Any(grant =>
                    AssignmentAppliesToGraph(grant.ResourceScope, grant.GraphGUID, grant.Role, null)
                    && CachedCredentialScopeGrants(grant, permission, resourceType)))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determine the credential scope required for a request.
        /// </summary>
        /// <param name="req">Request context.</param>
        /// <returns>Required credential scope.</returns>
        public static string RequiredScope(RequestContext req)
        {
            if (req == null) return "read";
            return RequiredScope(req.RequestType);
        }

        /// <summary>
        /// Determine the credential scope required for a request type.
        /// </summary>
        /// <param name="requestType">Request type.</param>
        /// <returns>Required credential scope.</returns>
        public static string RequiredScope(RequestTypeEnum requestType)
        {
            if (IsAdministrativeRequest(requestType)) return "admin";

            switch (requestType)
            {
                case RequestTypeEnum.Backup:
                case RequestTypeEnum.BackupDelete:
                case RequestTypeEnum.FlushDatabase:
                case RequestTypeEnum.TenantCreate:
                case RequestTypeEnum.TenantDelete:
                case RequestTypeEnum.TenantUpdate:
                case RequestTypeEnum.UserCreate:
                case RequestTypeEnum.UserDelete:
                case RequestTypeEnum.UserUpdate:
                case RequestTypeEnum.CredentialCreate:
                case RequestTypeEnum.CredentialDelete:
                case RequestTypeEnum.CredentialDeleteAllInTenant:
                case RequestTypeEnum.CredentialDeleteByUser:
                case RequestTypeEnum.CredentialUpdate:
                case RequestTypeEnum.LabelCreate:
                case RequestTypeEnum.LabelCreateMany:
                case RequestTypeEnum.LabelDelete:
                case RequestTypeEnum.LabelDeleteMany:
                case RequestTypeEnum.LabelDeleteAllInTenant:
                case RequestTypeEnum.LabelDeleteAllInGraph:
                case RequestTypeEnum.LabelDeleteGraphLabels:
                case RequestTypeEnum.LabelDeleteNodeLabels:
                case RequestTypeEnum.LabelDeleteEdgeLabels:
                case RequestTypeEnum.LabelUpdate:
                case RequestTypeEnum.VectorCreate:
                case RequestTypeEnum.VectorCreateMany:
                case RequestTypeEnum.VectorDelete:
                case RequestTypeEnum.VectorDeleteMany:
                case RequestTypeEnum.VectorDeleteAllInTenant:
                case RequestTypeEnum.VectorDeleteAllInGraph:
                case RequestTypeEnum.VectorDeleteGraphVectors:
                case RequestTypeEnum.VectorDeleteNodeVectors:
                case RequestTypeEnum.VectorDeleteEdgeVectors:
                case RequestTypeEnum.VectorUpdate:
                case RequestTypeEnum.TagCreate:
                case RequestTypeEnum.TagCreateMany:
                case RequestTypeEnum.TagDelete:
                case RequestTypeEnum.TagDeleteMany:
                case RequestTypeEnum.TagDeleteAllInTenant:
                case RequestTypeEnum.TagDeleteAllInGraph:
                case RequestTypeEnum.TagDeleteGraphTags:
                case RequestTypeEnum.TagDeleteNodeTags:
                case RequestTypeEnum.TagDeleteEdgeTags:
                case RequestTypeEnum.TagUpdate:
                case RequestTypeEnum.GraphCreate:
                case RequestTypeEnum.GraphDelete:
                case RequestTypeEnum.GraphDeleteAllInTenant:
                case RequestTypeEnum.GraphTransaction:
                case RequestTypeEnum.GraphUpdate:
                case RequestTypeEnum.GraphImportJsonl:
                case RequestTypeEnum.GraphImportJsonlNew:
                case RequestTypeEnum.GraphVectorIndexDisable:
                case RequestTypeEnum.GraphVectorIndexEnable:
                case RequestTypeEnum.GraphVectorIndexRebuild:
                case RequestTypeEnum.NodeCreate:
                case RequestTypeEnum.NodeCreateMany:
                case RequestTypeEnum.NodeDelete:
                case RequestTypeEnum.NodeDeleteAll:
                case RequestTypeEnum.NodeDeleteAllInTenant:
                case RequestTypeEnum.NodeDeleteMany:
                case RequestTypeEnum.NodeUpdate:
                case RequestTypeEnum.EdgeCreate:
                case RequestTypeEnum.EdgeCreateMany:
                case RequestTypeEnum.EdgeDelete:
                case RequestTypeEnum.EdgeDeleteAll:
                case RequestTypeEnum.EdgeDeleteAllInTenant:
                case RequestTypeEnum.EdgeDeleteMany:
                case RequestTypeEnum.EdgeDeleteNodeEdges:
                case RequestTypeEnum.EdgeDeleteNodeEdgesMany:
                case RequestTypeEnum.EdgeUpdate:
                case RequestTypeEnum.ChatCompletion:
                case RequestTypeEnum.ChatGraphCompletionOpenAI:
                case RequestTypeEnum.ChatGraphCompletionOllama:
                case RequestTypeEnum.ChatEndpointPreload:
                case RequestTypeEnum.ChatThreadCreate:
                case RequestTypeEnum.ChatThreadUpdate:
                case RequestTypeEnum.ChatThreadDelete:
                case RequestTypeEnum.ChatFeedbackCreate:
                    return "write";
                default:
                    return "read";
            }
        }

        /// <summary>
        /// Determine the authorization permission required for a request type.
        /// </summary>
        /// <param name="requestType">Request type.</param>
        /// <returns>Authorization permission.</returns>
        public static AuthorizationPermissionEnum RequiredPermission(RequestTypeEnum requestType)
        {
            return RequiredPermission(RequiredScope(requestType), requestType);
        }

        /// <summary>
        /// Determine the authorization permission required for a scope/request pair.
        /// </summary>
        /// <param name="requiredScope">Required legacy scope.</param>
        /// <param name="requestType">Request type.</param>
        /// <returns>Authorization permission.</returns>
        public static AuthorizationPermissionEnum RequiredPermission(string requiredScope, RequestTypeEnum requestType)
        {
            if (String.Equals(requiredScope, "admin", StringComparison.OrdinalIgnoreCase)) return AuthorizationPermissionEnum.Admin;
            if (IsAdministrativeRequest(requestType)) return AuthorizationPermissionEnum.Admin;

            switch (requestType)
            {
                case RequestTypeEnum.Backup:
                case RequestTypeEnum.BackupDelete:
                case RequestTypeEnum.FlushDatabase:
                case RequestTypeEnum.TenantCreate:
                case RequestTypeEnum.TenantDelete:
                case RequestTypeEnum.TenantUpdate:
                case RequestTypeEnum.UserCreate:
                case RequestTypeEnum.UserDelete:
                case RequestTypeEnum.UserUpdate:
                case RequestTypeEnum.CredentialCreate:
                case RequestTypeEnum.CredentialDelete:
                case RequestTypeEnum.CredentialDeleteAllInTenant:
                case RequestTypeEnum.CredentialDeleteByUser:
                case RequestTypeEnum.CredentialUpdate:
                    return AuthorizationPermissionEnum.Admin;
                case RequestTypeEnum.GraphDelete:
                case RequestTypeEnum.GraphDeleteAllInTenant:
                case RequestTypeEnum.NodeDelete:
                case RequestTypeEnum.NodeDeleteAll:
                case RequestTypeEnum.NodeDeleteAllInTenant:
                case RequestTypeEnum.NodeDeleteMany:
                case RequestTypeEnum.EdgeDelete:
                case RequestTypeEnum.EdgeDeleteAll:
                case RequestTypeEnum.EdgeDeleteAllInTenant:
                case RequestTypeEnum.EdgeDeleteMany:
                case RequestTypeEnum.EdgeDeleteNodeEdges:
                case RequestTypeEnum.EdgeDeleteNodeEdgesMany:
                case RequestTypeEnum.LabelDelete:
                case RequestTypeEnum.LabelDeleteMany:
                case RequestTypeEnum.LabelDeleteAllInTenant:
                case RequestTypeEnum.LabelDeleteAllInGraph:
                case RequestTypeEnum.LabelDeleteGraphLabels:
                case RequestTypeEnum.LabelDeleteNodeLabels:
                case RequestTypeEnum.LabelDeleteEdgeLabels:
                case RequestTypeEnum.TagDelete:
                case RequestTypeEnum.TagDeleteMany:
                case RequestTypeEnum.TagDeleteAllInTenant:
                case RequestTypeEnum.TagDeleteAllInGraph:
                case RequestTypeEnum.TagDeleteGraphTags:
                case RequestTypeEnum.TagDeleteNodeTags:
                case RequestTypeEnum.TagDeleteEdgeTags:
                case RequestTypeEnum.VectorDelete:
                case RequestTypeEnum.VectorDeleteMany:
                case RequestTypeEnum.VectorDeleteAllInTenant:
                case RequestTypeEnum.VectorDeleteAllInGraph:
                case RequestTypeEnum.VectorDeleteGraphVectors:
                case RequestTypeEnum.VectorDeleteNodeVectors:
                case RequestTypeEnum.VectorDeleteEdgeVectors:
                    return AuthorizationPermissionEnum.Delete;
                default:
                    if (String.Equals(requiredScope, "write", StringComparison.OrdinalIgnoreCase)) return AuthorizationPermissionEnum.Write;
                    return AuthorizationPermissionEnum.Read;
            }
        }

        /// <summary>
        /// Determine the resource type required for a request type.
        /// </summary>
        /// <param name="requestType">Request type.</param>
        /// <returns>Authorization resource type.</returns>
        public static AuthorizationResourceTypeEnum RequiredResourceType(RequestTypeEnum requestType)
        {
            // Chat requests resolve to the Chat resource type even when they are administrative,
            // so a role granting [Admin] on [Chat] can delegate chat management without tenant-wide admin.
            switch (requestType)
            {
                case RequestTypeEnum.ChatCompletion:
                case RequestTypeEnum.ChatGraphCompletionOpenAI:
                case RequestTypeEnum.ChatGraphCompletionOllama:
                case RequestTypeEnum.ChatModelsReadAll:
                case RequestTypeEnum.ChatGraphModelsReadAll:
                case RequestTypeEnum.ChatEndpointCreate:
                case RequestTypeEnum.ChatEndpointReadAll:
                case RequestTypeEnum.ChatEndpointRead:
                case RequestTypeEnum.ChatEndpointExists:
                case RequestTypeEnum.ChatEndpointUpdate:
                case RequestTypeEnum.ChatEndpointDelete:
                case RequestTypeEnum.ChatEndpointTest:
                case RequestTypeEnum.ChatEndpointPreload:
                case RequestTypeEnum.ChatEndpointHealthReadAll:
                case RequestTypeEnum.ChatEndpointHealthRead:
                case RequestTypeEnum.ChatThreadCreate:
                case RequestTypeEnum.ChatThreadReadAll:
                case RequestTypeEnum.ChatThreadRead:
                case RequestTypeEnum.ChatThreadUpdate:
                case RequestTypeEnum.ChatThreadDelete:
                case RequestTypeEnum.ChatThreadTurnsRead:
                case RequestTypeEnum.ChatFeedbackCreate:
                case RequestTypeEnum.ChatFeedbackReadAll:
                case RequestTypeEnum.ChatFeedbackRead:
                case RequestTypeEnum.ChatFeedbackDelete:
                case RequestTypeEnum.ChatSettingsRead:
                case RequestTypeEnum.ChatSettingsUpdate:
                    return AuthorizationResourceTypeEnum.Chat;
            }

            if (IsAdministrativeRequest(requestType)) return AuthorizationResourceTypeEnum.Admin;

            switch (requestType)
            {
                case RequestTypeEnum.Backup:
                case RequestTypeEnum.BackupDelete:
                case RequestTypeEnum.FlushDatabase:
                case RequestTypeEnum.TenantCreate:
                case RequestTypeEnum.TenantDelete:
                case RequestTypeEnum.TenantUpdate:
                case RequestTypeEnum.UserCreate:
                case RequestTypeEnum.UserDelete:
                case RequestTypeEnum.UserUpdate:
                case RequestTypeEnum.CredentialCreate:
                case RequestTypeEnum.CredentialDelete:
                case RequestTypeEnum.CredentialDeleteAllInTenant:
                case RequestTypeEnum.CredentialDeleteByUser:
                case RequestTypeEnum.CredentialUpdate:
                    return AuthorizationResourceTypeEnum.Admin;
                case RequestTypeEnum.GraphTransaction:
                    return AuthorizationResourceTypeEnum.Transaction;
                case RequestTypeEnum.GraphQuery:
                    return AuthorizationResourceTypeEnum.Query;
                case RequestTypeEnum.GraphCreate:
                case RequestTypeEnum.GraphDelete:
                case RequestTypeEnum.GraphDeleteAllInTenant:
                case RequestTypeEnum.GraphRead:
                case RequestTypeEnum.GraphReadAllInTenant:
                case RequestTypeEnum.GraphReadFirst:
                case RequestTypeEnum.GraphUpdate:
                case RequestTypeEnum.GraphExists:
                case RequestTypeEnum.GraphExistence:
                case RequestTypeEnum.GraphStatistics:
                case RequestTypeEnum.GraphSearch:
                case RequestTypeEnum.GraphExportJsonl:
                case RequestTypeEnum.GraphExportSubgraphJsonl:
                case RequestTypeEnum.GraphImportJsonl:
                case RequestTypeEnum.GraphImportJsonlNew:
                case RequestTypeEnum.GraphVectorIndexDisable:
                case RequestTypeEnum.GraphVectorIndexEnable:
                case RequestTypeEnum.GraphVectorIndexRebuild:
                    return AuthorizationResourceTypeEnum.Graph;
                case RequestTypeEnum.NodeCreate:
                case RequestTypeEnum.NodeCreateMany:
                case RequestTypeEnum.NodeDelete:
                case RequestTypeEnum.NodeDeleteAll:
                case RequestTypeEnum.NodeDeleteAllInTenant:
                case RequestTypeEnum.NodeDeleteMany:
                case RequestTypeEnum.NodeRead:
                case RequestTypeEnum.NodeReadAll:
                case RequestTypeEnum.NodeReadAllInTenant:
                case RequestTypeEnum.NodeReadAllInGraph:
                case RequestTypeEnum.NodeEnumerate:
                case RequestTypeEnum.NodeReadFirst:
                case RequestTypeEnum.NodeUpdate:
                case RequestTypeEnum.NodeExists:
                case RequestTypeEnum.NodeSearch:
                case RequestTypeEnum.NodeParents:
                case RequestTypeEnum.NodeChildren:
                case RequestTypeEnum.NodeNeighbors:
                case RequestTypeEnum.NodeReadMostConnected:
                case RequestTypeEnum.NodeReadLeastConnected:
                case RequestTypeEnum.GetRoutes:
                    return AuthorizationResourceTypeEnum.Node;
                case RequestTypeEnum.EdgeCreate:
                case RequestTypeEnum.EdgeCreateMany:
                case RequestTypeEnum.EdgeDelete:
                case RequestTypeEnum.EdgeDeleteAll:
                case RequestTypeEnum.EdgeDeleteAllInTenant:
                case RequestTypeEnum.EdgeDeleteMany:
                case RequestTypeEnum.EdgeDeleteNodeEdges:
                case RequestTypeEnum.EdgeDeleteNodeEdgesMany:
                case RequestTypeEnum.EdgeRead:
                case RequestTypeEnum.EdgeReadAll:
                case RequestTypeEnum.EdgeReadAllInTenant:
                case RequestTypeEnum.EdgeReadAllInGraph:
                case RequestTypeEnum.EdgeEnumerate:
                case RequestTypeEnum.EdgeReadMany:
                case RequestTypeEnum.EdgesFromNode:
                case RequestTypeEnum.EdgesToNode:
                case RequestTypeEnum.AllEdgesToNode:
                case RequestTypeEnum.EdgeBetween:
                case RequestTypeEnum.EdgeUpdate:
                case RequestTypeEnum.EdgeExists:
                case RequestTypeEnum.EdgeSearch:
                    return AuthorizationResourceTypeEnum.Edge;
                case RequestTypeEnum.LabelCreate:
                case RequestTypeEnum.LabelCreateMany:
                case RequestTypeEnum.LabelDelete:
                case RequestTypeEnum.LabelDeleteMany:
                case RequestTypeEnum.LabelDeleteAllInTenant:
                case RequestTypeEnum.LabelDeleteAllInGraph:
                case RequestTypeEnum.LabelDeleteGraphLabels:
                case RequestTypeEnum.LabelDeleteNodeLabels:
                case RequestTypeEnum.LabelDeleteEdgeLabels:
                case RequestTypeEnum.LabelRead:
                case RequestTypeEnum.LabelReadAll:
                case RequestTypeEnum.LabelReadAllInTenant:
                case RequestTypeEnum.LabelReadAllInGraph:
                case RequestTypeEnum.LabelEnumerate:
                case RequestTypeEnum.LabelReadManyGraph:
                case RequestTypeEnum.LabelReadManyNode:
                case RequestTypeEnum.LabelReadManyEdge:
                case RequestTypeEnum.LabelUpdate:
                case RequestTypeEnum.LabelExists:
                    return AuthorizationResourceTypeEnum.Label;
                case RequestTypeEnum.TagCreate:
                case RequestTypeEnum.TagCreateMany:
                case RequestTypeEnum.TagDelete:
                case RequestTypeEnum.TagDeleteMany:
                case RequestTypeEnum.TagDeleteAllInTenant:
                case RequestTypeEnum.TagDeleteAllInGraph:
                case RequestTypeEnum.TagDeleteGraphTags:
                case RequestTypeEnum.TagDeleteNodeTags:
                case RequestTypeEnum.TagDeleteEdgeTags:
                case RequestTypeEnum.TagRead:
                case RequestTypeEnum.TagReadAll:
                case RequestTypeEnum.TagReadAllInTenant:
                case RequestTypeEnum.TagReadAllInGraph:
                case RequestTypeEnum.TagEnumerate:
                case RequestTypeEnum.TagReadManyGraph:
                case RequestTypeEnum.TagReadManyNode:
                case RequestTypeEnum.TagReadManyEdge:
                case RequestTypeEnum.TagUpdate:
                case RequestTypeEnum.TagExists:
                    return AuthorizationResourceTypeEnum.Tag;
                case RequestTypeEnum.VectorCreate:
                case RequestTypeEnum.VectorCreateMany:
                case RequestTypeEnum.VectorDelete:
                case RequestTypeEnum.VectorDeleteMany:
                case RequestTypeEnum.VectorDeleteAllInTenant:
                case RequestTypeEnum.VectorDeleteAllInGraph:
                case RequestTypeEnum.VectorDeleteGraphVectors:
                case RequestTypeEnum.VectorDeleteNodeVectors:
                case RequestTypeEnum.VectorDeleteEdgeVectors:
                case RequestTypeEnum.VectorRead:
                case RequestTypeEnum.VectorReadAll:
                case RequestTypeEnum.VectorReadAllInTenant:
                case RequestTypeEnum.VectorReadAllInGraph:
                case RequestTypeEnum.VectorEnumerate:
                case RequestTypeEnum.VectorReadManyGraph:
                case RequestTypeEnum.VectorReadManyNode:
                case RequestTypeEnum.VectorReadManyEdge:
                case RequestTypeEnum.VectorUpdate:
                case RequestTypeEnum.VectorExists:
                case RequestTypeEnum.VectorSearch:
                    return AuthorizationResourceTypeEnum.Vector;
                default:
                    return AuthorizationResourceTypeEnum.Admin;
            }
        }

        /// <summary>
        /// Determine if a request type is an administrative authorization operation.
        /// </summary>
        /// <param name="requestType">Request type.</param>
        /// <returns>True if administrative authorization is required.</returns>
        public static bool IsAdministrativeRequest(RequestTypeEnum requestType)
        {
            switch (requestType)
            {
                case RequestTypeEnum.Backup:
                case RequestTypeEnum.BackupDelete:
                case RequestTypeEnum.FlushDatabase:
                case RequestTypeEnum.TenantCreate:
                case RequestTypeEnum.TenantDelete:
                case RequestTypeEnum.TenantUpdate:
                case RequestTypeEnum.UserCreate:
                case RequestTypeEnum.UserDelete:
                case RequestTypeEnum.UserUpdate:
                case RequestTypeEnum.CredentialCreate:
                case RequestTypeEnum.CredentialDelete:
                case RequestTypeEnum.CredentialDeleteAllInTenant:
                case RequestTypeEnum.CredentialDeleteByUser:
                case RequestTypeEnum.CredentialUpdate:
                case RequestTypeEnum.AuthorizationRoleCreate:
                case RequestTypeEnum.AuthorizationRoleDelete:
                case RequestTypeEnum.AuthorizationRoleRead:
                case RequestTypeEnum.AuthorizationRoleReadAll:
                case RequestTypeEnum.AuthorizationRoleUpdate:
                case RequestTypeEnum.UserRoleAssignmentCreate:
                case RequestTypeEnum.UserRoleAssignmentDelete:
                case RequestTypeEnum.UserRoleAssignmentRead:
                case RequestTypeEnum.UserRoleAssignmentReadAll:
                case RequestTypeEnum.UserRoleAssignmentUpdate:
                case RequestTypeEnum.CredentialScopeAssignmentCreate:
                case RequestTypeEnum.CredentialScopeAssignmentDelete:
                case RequestTypeEnum.CredentialScopeAssignmentRead:
                case RequestTypeEnum.CredentialScopeAssignmentReadAll:
                case RequestTypeEnum.CredentialScopeAssignmentUpdate:
                case RequestTypeEnum.UserEffectivePermissionsRead:
                case RequestTypeEnum.CredentialEffectivePermissionsRead:
                case RequestTypeEnum.SettingsRead:
                case RequestTypeEnum.SettingsUpdate:
                case RequestTypeEnum.SettingsRestart:
                case RequestTypeEnum.ChatEndpointCreate:
                case RequestTypeEnum.ChatEndpointReadAll:
                case RequestTypeEnum.ChatEndpointRead:
                case RequestTypeEnum.ChatEndpointExists:
                case RequestTypeEnum.ChatEndpointUpdate:
                case RequestTypeEnum.ChatEndpointDelete:
                case RequestTypeEnum.ChatEndpointTest:
                case RequestTypeEnum.ChatEndpointHealthReadAll:
                case RequestTypeEnum.ChatEndpointHealthRead:
                case RequestTypeEnum.ChatSettingsUpdate:
                case RequestTypeEnum.ChatFeedbackReadAll:
                case RequestTypeEnum.ChatFeedbackRead:
                case RequestTypeEnum.ChatFeedbackDelete:
                    return true;
                default:
                    return false;
            }
        }

        #endregion

        #region Private-Methods

        private async Task<AuthorizationEffectivePolicy> GetCredentialEffectivePolicy(Guid tenantGuid, Guid credentialGuid, CancellationToken token)
        {
            return await GetEffectivePolicy(
                EffectivePolicyCacheKey("credential", tenantGuid, credentialGuid),
                async ct => await LoadCredentialEffectivePolicy(tenantGuid, credentialGuid, ct).ConfigureAwait(false),
                token).ConfigureAwait(false);
        }

        private async Task<AuthorizationEffectivePolicy> GetUserEffectivePolicy(Guid tenantGuid, Guid userGuid, CancellationToken token)
        {
            return await GetEffectivePolicy(
                EffectivePolicyCacheKey("user", tenantGuid, userGuid),
                async ct => await LoadUserEffectivePolicy(tenantGuid, userGuid, ct).ConfigureAwait(false),
                token).ConfigureAwait(false);
        }

        private async Task<AuthorizationEffectivePolicy> GetEffectivePolicy(
            string cacheKey,
            Func<CancellationToken, Task<AuthorizationEffectivePolicy>> loader,
            CancellationToken token)
        {
            if (String.IsNullOrEmpty(cacheKey)) throw new ArgumentNullException(nameof(cacheKey));
            if (loader == null) throw new ArgumentNullException(nameof(loader));

            while (true)
            {
                long version = EnsureAuthorizationCacheCurrent();
                if (_EffectivePolicyCache.TryGetValue(cacheKey, out AuthorizationEffectivePolicy cached))
                {
                    Interlocked.Increment(ref _PolicyCacheHits);
                    return cached;
                }

                Interlocked.Increment(ref _PolicyCacheMisses);
                AuthorizationEffectivePolicy loaded = await loader(token).ConfigureAwait(false);
                if (version == AuthorizationPolicyChangeTracker.Version)
                {
                    _EffectivePolicyCache[cacheKey] = loaded;
                    return loaded;
                }
            }
        }

        private async Task<AuthorizationEffectivePolicy> LoadCredentialEffectivePolicy(Guid tenantGuid, Guid credentialGuid, CancellationToken token)
        {
            List<CredentialScopeAssignment> assignments = await LoadCredentialScopes(tenantGuid, credentialGuid, token).ConfigureAwait(false);
            AuthorizationEffectivePolicy policy = new AuthorizationEffectivePolicy(assignments.Count);

            foreach (CredentialScopeAssignment assignment in assignments)
            {
                token.ThrowIfCancellationRequested();
                RoleDefinition role = await ResolveRole(tenantGuid, assignment.RoleGUID, assignment.RoleName, token).ConfigureAwait(false);
                policy.Grants.Add(CachedAuthorizationGrant.FromCredentialScope(assignment, role));
            }

            return policy;
        }

        private async Task<AuthorizationEffectivePolicy> LoadUserEffectivePolicy(Guid tenantGuid, Guid userGuid, CancellationToken token)
        {
            List<UserRoleAssignment> assignments = await LoadUserRoles(tenantGuid, userGuid, token).ConfigureAwait(false);
            AuthorizationEffectivePolicy policy = new AuthorizationEffectivePolicy(assignments.Count);

            foreach (UserRoleAssignment assignment in assignments)
            {
                token.ThrowIfCancellationRequested();
                RoleDefinition role = await ResolveRole(tenantGuid, assignment.RoleGUID, assignment.RoleName, token).ConfigureAwait(false);
                policy.Grants.Add(CachedAuthorizationGrant.FromUserRole(assignment, role));
            }

            return policy;
        }

        private long EnsureAuthorizationCacheCurrent()
        {
            long currentVersion = AuthorizationPolicyChangeTracker.Version;
            if (_ObservedPolicyVersion == currentVersion) return currentVersion;

            lock (_CacheLock)
            {
                currentVersion = AuthorizationPolicyChangeTracker.Version;
                if (_ObservedPolicyVersion != currentVersion)
                {
                    _EffectivePolicyCache.Clear();
                    _RoleCache.Clear();
                    _ObservedPolicyVersion = currentVersion;
                    Interlocked.Increment(ref _PolicyCacheInvalidations);
                }
            }

            return currentVersion;
        }

        private static string EffectivePolicyCacheKey(string subjectType, Guid tenantGuid, Guid subjectGuid)
        {
            return subjectType + ":" + tenantGuid.ToString("N") + ":" + subjectGuid.ToString("N");
        }

        private async Task<List<CredentialScopeAssignment>> LoadCredentialScopes(Guid tenantGuid, Guid credentialGuid, CancellationToken token)
        {
            CredentialScopeAssignmentSearchResult result = await _Repo.AuthorizationRoles.SearchCredentialScopes(new CredentialScopeAssignmentSearchRequest
            {
                TenantGUID = tenantGuid,
                CredentialGUID = credentialGuid,
                PageSize = 1000
            }, token).ConfigureAwait(false);

            return result?.Objects ?? new List<CredentialScopeAssignment>();
        }

        private async Task<List<UserRoleAssignment>> LoadUserRoles(Guid tenantGuid, Guid userGuid, CancellationToken token)
        {
            UserRoleAssignmentSearchResult result = await _Repo.AuthorizationRoles.SearchUserRoles(new UserRoleAssignmentSearchRequest
            {
                TenantGUID = tenantGuid,
                UserGUID = userGuid,
                PageSize = 1000
            }, token).ConfigureAwait(false);

            return result?.Objects ?? new List<UserRoleAssignment>();
        }

        private AuthorizationDecision EvaluateCredentialPolicy(
            AuthorizationEffectivePolicy policy,
            Guid? graphGuid,
            string requiredScope,
            AuthorizationPermissionEnum permission,
            AuthorizationResourceTypeEnum resourceType)
        {
            bool anyAppliesToGraph = false;

            foreach (CachedAuthorizationGrant grant in policy.Grants)
            {
                if (!AssignmentAppliesToGraph(grant.ResourceScope, grant.GraphGUID, grant.Role, graphGuid)) continue;

                anyAppliesToGraph = true;
                if (CachedCredentialScopeGrants(grant, permission, resourceType))
                    return AuthorizationDecision.Permit(requiredScope, AuthorizationDecisionReason.Permitted);
            }

            if (graphGuid.HasValue && !anyAppliesToGraph) return AuthorizationDecision.Deny(requiredScope, AuthorizationDecisionReason.GraphDenied);
            return AuthorizationDecision.Deny(requiredScope, AuthorizationDecisionReason.MissingScope);
        }

        private AuthorizationDecision EvaluateUserPolicy(
            AuthorizationEffectivePolicy policy,
            Guid? graphGuid,
            string requiredScope,
            AuthorizationPermissionEnum permission,
            AuthorizationResourceTypeEnum resourceType)
        {
            bool anyAppliesToGraph = false;

            foreach (CachedAuthorizationGrant grant in policy.Grants)
            {
                if (!AssignmentAppliesToGraph(grant.ResourceScope, grant.GraphGUID, grant.Role, graphGuid)) continue;

                anyAppliesToGraph = true;
                if (RoleGrants(grant.Role, permission, resourceType))
                    return AuthorizationDecision.Permit(requiredScope, AuthorizationDecisionReason.Permitted);
            }

            if (graphGuid.HasValue && !anyAppliesToGraph) return AuthorizationDecision.Deny(requiredScope, AuthorizationDecisionReason.GraphDenied);
            return AuthorizationDecision.Deny(requiredScope, AuthorizationDecisionReason.MissingScope);
        }

        private async Task<RoleDefinition> ResolveRole(Guid tenantGuid, Guid? roleGuid, string roleName, CancellationToken token)
        {
            if (_Repo == null) return AuthorizationPolicyDefinitions.GetBuiltInRole(roleName);

            if (roleGuid.HasValue)
            {
                RoleDefinition storedByGuid = await ResolveRoleByGuid(roleGuid.Value, token).ConfigureAwait(false);
                if (storedByGuid != null) return storedByGuid;
            }

            if (!String.IsNullOrWhiteSpace(roleName))
            {
                RoleDefinition storedByTenantName = await ResolveRoleByName(tenantGuid, roleName, token).ConfigureAwait(false);
                if (storedByTenantName != null) return storedByTenantName;

                RoleDefinition storedByGlobalName = await ResolveRoleByName(null, roleName, token).ConfigureAwait(false);
                if (storedByGlobalName != null) return storedByGlobalName;

                RoleDefinition builtIn = AuthorizationPolicyDefinitions.GetBuiltInRole(roleName);
                if (builtIn != null) return builtIn;
            }

            return null;
        }

        private async Task<RoleDefinition> ResolveRoleByGuid(Guid roleGuid, CancellationToken token)
        {
            return await GetRoleFromCache(
                "guid:" + roleGuid.ToString("N"),
                async ct =>
                {
                    AuthorizationRole stored = await _Repo.AuthorizationRoles.ReadRoleByGuid(roleGuid, ct).ConfigureAwait(false);
                    return stored?.ToDefinition();
                },
                token).ConfigureAwait(false);
        }

        private async Task<RoleDefinition> ResolveRoleByName(Guid? tenantGuid, string roleName, CancellationToken token)
        {
            return await GetRoleFromCache(
                RoleNameCacheKey(tenantGuid, roleName),
                async ct =>
                {
                    AuthorizationRole stored = await _Repo.AuthorizationRoles.ReadRoleByName(tenantGuid, roleName, ct).ConfigureAwait(false);
                    return stored?.ToDefinition();
                },
                token).ConfigureAwait(false);
        }

        private async Task<RoleDefinition> GetRoleFromCache(
            string cacheKey,
            Func<CancellationToken, Task<RoleDefinition>> loader,
            CancellationToken token)
        {
            if (String.IsNullOrEmpty(cacheKey)) throw new ArgumentNullException(nameof(cacheKey));
            if (loader == null) throw new ArgumentNullException(nameof(loader));

            while (true)
            {
                long version = EnsureAuthorizationCacheCurrent();
                if (_RoleCache.TryGetValue(cacheKey, out RoleDefinition cached)) return cached;

                RoleDefinition loaded = await loader(token).ConfigureAwait(false);
                if (version != AuthorizationPolicyChangeTracker.Version) continue;
                if (loaded == null) return null;
                _RoleCache[cacheKey] = loaded;
                return loaded;
            }
        }

        private static string RoleNameCacheKey(Guid? tenantGuid, string roleName)
        {
            return "name:" + (tenantGuid.HasValue ? tenantGuid.Value.ToString("N") : "global") + ":" + roleName.ToLowerInvariant();
        }

        private async Task ResolveGraphGuidForAuthorization(RequestContext req, CancellationToken token)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.GraphGUID.HasValue) return;
            if (!req.TenantGUID.HasValue) return;
            if (_Repo == null) return;

            Guid tenantGuid = req.TenantGUID.Value;

            switch (req.RequestType)
            {
                case RequestTypeEnum.LabelCreate:
                case RequestTypeEnum.LabelUpdate:
                    req.GraphGUID = ResolveGraphGuid(req.Label?.GraphGUID);
                    if (!req.GraphGUID.HasValue && req.LabelGUID.HasValue)
                    {
                        LabelMetadata label = await _Repo.Label.ReadByGuid(tenantGuid, req.LabelGUID.Value, token).ConfigureAwait(false);
                        req.GraphGUID = ResolveGraphGuid(label?.GraphGUID);
                    }
                    return;

                case RequestTypeEnum.LabelCreateMany:
                    req.GraphGUID = ResolveCommonGraphGuid(req.Labels?.Select(label => label.GraphGUID));
                    return;

                case RequestTypeEnum.LabelDeleteMany:
                    req.GraphGUID = await ResolveCommonLabelGraphGuid(tenantGuid, req.GUIDs, token).ConfigureAwait(false);
                    return;

                case RequestTypeEnum.LabelRead:
                case RequestTypeEnum.LabelDelete:
                case RequestTypeEnum.LabelExists:
                    if (req.LabelGUID.HasValue)
                    {
                        LabelMetadata label = await _Repo.Label.ReadByGuid(tenantGuid, req.LabelGUID.Value, token).ConfigureAwait(false);
                        req.GraphGUID = ResolveGraphGuid(label?.GraphGUID);
                    }
                    return;

                case RequestTypeEnum.TagCreate:
                case RequestTypeEnum.TagUpdate:
                    req.GraphGUID = ResolveGraphGuid(req.Tag?.GraphGUID);
                    if (!req.GraphGUID.HasValue && req.TagGUID.HasValue)
                    {
                        TagMetadata tag = await _Repo.Tag.ReadByGuid(tenantGuid, req.TagGUID.Value, token).ConfigureAwait(false);
                        req.GraphGUID = ResolveGraphGuid(tag?.GraphGUID);
                    }
                    return;

                case RequestTypeEnum.TagCreateMany:
                    req.GraphGUID = ResolveCommonGraphGuid(req.Tags?.Select(tag => tag.GraphGUID));
                    return;

                case RequestTypeEnum.TagDeleteMany:
                    req.GraphGUID = await ResolveCommonTagGraphGuid(tenantGuid, req.GUIDs, token).ConfigureAwait(false);
                    return;

                case RequestTypeEnum.TagRead:
                case RequestTypeEnum.TagDelete:
                case RequestTypeEnum.TagExists:
                    if (req.TagGUID.HasValue)
                    {
                        TagMetadata tag = await _Repo.Tag.ReadByGuid(tenantGuid, req.TagGUID.Value, token).ConfigureAwait(false);
                        req.GraphGUID = ResolveGraphGuid(tag?.GraphGUID);
                    }
                    return;

                case RequestTypeEnum.VectorCreate:
                case RequestTypeEnum.VectorUpdate:
                    req.GraphGUID = ResolveGraphGuid(req.Vector?.GraphGUID);
                    if (!req.GraphGUID.HasValue && req.VectorGUID.HasValue)
                    {
                        VectorMetadata vector = await _Repo.Vector.ReadByGuid(tenantGuid, req.VectorGUID.Value, token).ConfigureAwait(false);
                        req.GraphGUID = ResolveGraphGuid(vector?.GraphGUID);
                    }
                    return;

                case RequestTypeEnum.VectorCreateMany:
                    req.GraphGUID = ResolveCommonGraphGuid(req.Vectors?.Select(vector => vector.GraphGUID));
                    return;

                case RequestTypeEnum.VectorDeleteMany:
                    req.GraphGUID = await ResolveCommonVectorGraphGuid(tenantGuid, req.GUIDs, token).ConfigureAwait(false);
                    return;

                case RequestTypeEnum.VectorRead:
                case RequestTypeEnum.VectorDelete:
                case RequestTypeEnum.VectorExists:
                    if (req.VectorGUID.HasValue)
                    {
                        VectorMetadata vector = await _Repo.Vector.ReadByGuid(tenantGuid, req.VectorGUID.Value, token).ConfigureAwait(false);
                        req.GraphGUID = ResolveGraphGuid(vector?.GraphGUID);
                    }
                    return;
            }
        }

        private async Task<Guid?> ResolveCommonLabelGraphGuid(Guid tenantGuid, List<Guid> guids, CancellationToken token)
        {
            if (guids == null || guids.Count < 1) return null;

            List<Guid> graphGuids = new List<Guid>();
            foreach (Guid guid in guids)
            {
                LabelMetadata label = await _Repo.Label.ReadByGuid(tenantGuid, guid, token).ConfigureAwait(false);
                if (label != null) graphGuids.Add(label.GraphGUID);
            }

            return ResolveCommonGraphGuid(graphGuids);
        }

        private async Task<Guid?> ResolveCommonTagGraphGuid(Guid tenantGuid, List<Guid> guids, CancellationToken token)
        {
            if (guids == null || guids.Count < 1) return null;

            List<Guid> graphGuids = new List<Guid>();
            foreach (Guid guid in guids)
            {
                TagMetadata tag = await _Repo.Tag.ReadByGuid(tenantGuid, guid, token).ConfigureAwait(false);
                if (tag != null) graphGuids.Add(tag.GraphGUID);
            }

            return ResolveCommonGraphGuid(graphGuids);
        }

        private async Task<Guid?> ResolveCommonVectorGraphGuid(Guid tenantGuid, List<Guid> guids, CancellationToken token)
        {
            if (guids == null || guids.Count < 1) return null;

            List<Guid> graphGuids = new List<Guid>();
            foreach (Guid guid in guids)
            {
                VectorMetadata vector = await _Repo.Vector.ReadByGuid(tenantGuid, guid, token).ConfigureAwait(false);
                if (vector != null) graphGuids.Add(vector.GraphGUID);
            }

            return ResolveCommonGraphGuid(graphGuids);
        }

        private static Guid? ResolveGraphGuid(Guid? graphGuid)
        {
            if (!graphGuid.HasValue) return null;
            if (graphGuid.Value == default(Guid)) return null;
            return graphGuid.Value;
        }

        private static Guid? ResolveCommonGraphGuid(IEnumerable<Guid> graphGuids)
        {
            if (graphGuids == null) return null;

            List<Guid> distinct = graphGuids
                .Where(graphGuid => graphGuid != default(Guid))
                .Distinct()
                .Take(2)
                .ToList();

            if (distinct.Count == 0) return null;
            if (distinct.Count == 1) return distinct[0];
            return Guid.Empty;
        }

        private static bool AssignmentAppliesToGraph(
            AuthorizationResourceScopeEnum assignmentScope,
            Guid? assignmentGraphGuid,
            RoleDefinition role,
            Guid? requestedGraphGuid)
        {
            if (assignmentScope == AuthorizationResourceScopeEnum.Tenant)
            {
                if (!requestedGraphGuid.HasValue) return true;
                return role == null || role.InheritsToGraphs;
            }

            if (!requestedGraphGuid.HasValue) return false;
            if (!assignmentGraphGuid.HasValue) return true;
            return assignmentGraphGuid.Value == requestedGraphGuid.Value;
        }

        private static bool CachedCredentialScopeGrants(
            CachedAuthorizationGrant grant,
            AuthorizationPermissionEnum permission,
            AuthorizationResourceTypeEnum resourceType)
        {
            if (grant == null) return false;
            if (RoleGrants(grant.Role, permission, resourceType)) return true;

            bool permissionGranted = grant.DirectPermissions != null
                && (grant.DirectPermissions.Contains(permission) || grant.DirectPermissions.Contains(AuthorizationPermissionEnum.Admin));
            bool resourceGranted = grant.DirectResourceTypes != null
                && grant.DirectResourceTypes.Contains(resourceType);

            return permissionGranted && resourceGranted;
        }

        private static bool IsMemberLevelChatRequest(AuthorizationPermissionEnum permission, AuthorizationResourceTypeEnum resourceType)
        {
            return resourceType == AuthorizationResourceTypeEnum.Chat
                && permission != AuthorizationPermissionEnum.Admin;
        }

        private static bool RoleGrants(RoleDefinition role, AuthorizationPermissionEnum permission, AuthorizationResourceTypeEnum resourceType)
        {
            if (role == null) return false;

            bool permissionGranted = role.Permissions != null
                && (role.Permissions.Contains(permission) || role.Permissions.Contains(AuthorizationPermissionEnum.Admin));
            bool resourceGranted = role.ResourceTypes != null
                && role.ResourceTypes.Contains(resourceType);

            return permissionGranted && resourceGranted;
        }

        private static void ApplyDecision(RequestContext req, AuthorizationDecision decision)
        {
            req.Authorization.Result = decision.Result;
            req.Authorization.RequiredScope = decision.RequiredScope;
            req.Authorization.Reason = decision.Reason.ToString();
        }

        private sealed class AuthorizationEffectivePolicy
        {
            public int AssignmentCount { get; }

            public List<CachedAuthorizationGrant> Grants { get; } = new List<CachedAuthorizationGrant>();

            public AuthorizationEffectivePolicy(int assignmentCount)
            {
                AssignmentCount = assignmentCount;
            }
        }

        private sealed class CachedAuthorizationGrant
        {
            public AuthorizationResourceScopeEnum ResourceScope { get; set; } = AuthorizationResourceScopeEnum.Graph;

            public Guid? GraphGUID { get; set; } = null;

            public RoleDefinition Role { get; set; } = null;

            public List<AuthorizationPermissionEnum> DirectPermissions { get; set; } = new List<AuthorizationPermissionEnum>();

            public List<AuthorizationResourceTypeEnum> DirectResourceTypes { get; set; } = new List<AuthorizationResourceTypeEnum>();

            public static CachedAuthorizationGrant FromUserRole(UserRoleAssignment assignment, RoleDefinition role)
            {
                if (assignment == null) throw new ArgumentNullException(nameof(assignment));

                return new CachedAuthorizationGrant
                {
                    ResourceScope = assignment.ResourceScope,
                    GraphGUID = assignment.GraphGUID,
                    Role = role
                };
            }

            public static CachedAuthorizationGrant FromCredentialScope(CredentialScopeAssignment assignment, RoleDefinition role)
            {
                if (assignment == null) throw new ArgumentNullException(nameof(assignment));

                return new CachedAuthorizationGrant
                {
                    ResourceScope = assignment.ResourceScope,
                    GraphGUID = assignment.GraphGUID,
                    Role = role,
                    DirectPermissions = assignment.Permissions != null ? new List<AuthorizationPermissionEnum>(assignment.Permissions) : new List<AuthorizationPermissionEnum>(),
                    DirectResourceTypes = assignment.ResourceTypes != null ? new List<AuthorizationResourceTypeEnum>(assignment.ResourceTypes) : new List<AuthorizationResourceTypeEnum>()
                };
            }
        }

        #endregion
    }

    /// <summary>
    /// Authorization decision.
    /// </summary>
    public class AuthorizationDecision
    {
        /// <summary>
        /// Authorization result.
        /// </summary>
        public AuthorizationResultEnum Result { get; }

        /// <summary>
        /// Scope required by the request, when applicable.
        /// </summary>
        public string RequiredScope { get; }

        /// <summary>
        /// Decision reason.
        /// </summary>
        public AuthorizationDecisionReason Reason { get; }

        private AuthorizationDecision(AuthorizationResultEnum result, string requiredScope, AuthorizationDecisionReason reason)
        {
            Result = result;
            RequiredScope = requiredScope;
            Reason = reason;
        }

        /// <summary>
        /// Create a permitted decision.
        /// </summary>
        /// <param name="requiredScope">Required scope.</param>
        /// <param name="reason">Decision reason.</param>
        /// <returns>Authorization decision.</returns>
        public static AuthorizationDecision Permit(string requiredScope, AuthorizationDecisionReason reason)
        {
            return new AuthorizationDecision(AuthorizationResultEnum.Permitted, requiredScope, reason);
        }

        /// <summary>
        /// Create a denied decision.
        /// </summary>
        /// <param name="requiredScope">Required scope.</param>
        /// <param name="reason">Decision reason.</param>
        /// <returns>Authorization decision.</returns>
        public static AuthorizationDecision Deny(string requiredScope, AuthorizationDecisionReason reason)
        {
            return new AuthorizationDecision(AuthorizationResultEnum.Denied, requiredScope, reason);
        }
    }

    /// <summary>
    /// Authorization cache statistics.
    /// </summary>
    public class AuthorizationCacheStatistics
    {
        /// <summary>
        /// Number of cached effective policy entries.
        /// </summary>
        public int EffectivePolicyCacheEntries { get; }

        /// <summary>
        /// Number of cached role definitions.
        /// </summary>
        public int RoleCacheEntries { get; }

        /// <summary>
        /// Effective policy cache hits.
        /// </summary>
        public long PolicyCacheHits { get; }

        /// <summary>
        /// Effective policy cache misses.
        /// </summary>
        public long PolicyCacheMisses { get; }

        /// <summary>
        /// Cache invalidations observed by this service instance.
        /// </summary>
        public long PolicyCacheInvalidations { get; }

        /// <summary>
        /// Current authorization policy version.
        /// </summary>
        public long PolicyVersion { get; }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="effectivePolicyCacheEntries">Effective policy cache entries.</param>
        /// <param name="roleCacheEntries">Role cache entries.</param>
        /// <param name="policyCacheHits">Policy cache hits.</param>
        /// <param name="policyCacheMisses">Policy cache misses.</param>
        /// <param name="policyCacheInvalidations">Policy cache invalidations.</param>
        /// <param name="policyVersion">Current policy version.</param>
        public AuthorizationCacheStatistics(
            int effectivePolicyCacheEntries,
            int roleCacheEntries,
            long policyCacheHits,
            long policyCacheMisses,
            long policyCacheInvalidations,
            long policyVersion)
        {
            EffectivePolicyCacheEntries = effectivePolicyCacheEntries;
            RoleCacheEntries = roleCacheEntries;
            PolicyCacheHits = policyCacheHits;
            PolicyCacheMisses = policyCacheMisses;
            PolicyCacheInvalidations = policyCacheInvalidations;
            PolicyVersion = policyVersion;
        }
    }

    /// <summary>
    /// Authorization decision reason.
    /// </summary>
    public enum AuthorizationDecisionReason
    {
        /// <summary>
        /// The request is permitted.
        /// </summary>
        Permitted,
        /// <summary>
        /// No scoped credential was present.
        /// </summary>
        NoCredential,
        /// <summary>
        /// The requested tenant is not accessible.
        /// </summary>
        TenantDenied,
        /// <summary>
        /// The requested graph is not in the credential allow-list.
        /// </summary>
        GraphDenied,
        /// <summary>
        /// The credential is missing the required scope.
        /// </summary>
        MissingScope
    }
}
