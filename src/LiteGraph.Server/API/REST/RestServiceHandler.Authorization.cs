namespace LiteGraph.Server.API.REST
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Server.Classes;
    using WatsonWebserver.Core;

    internal partial class RestServiceHandler
    {
        #region Authorization-Routes

        private async Task AuthorizationRoleCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            AuthorizationRole role = _Serializer.DeserializeJson<AuthorizationRole>(ctx.Request.DataAsString);
            NormalizeAuthorizationRoleForTenant(role, req.TenantGUID.Value, true);

            if (!ValidateAuthorizationRole(role, out string validationError))
            {
                await SendApiError(ctx, 400, ApiErrorEnum.BadRequest, validationError).ConfigureAwait(false);
                return;
            }

            AuthorizationRole created = await _LiteGraph.AuthorizationRoles.CreateRole(role, timeoutCts.Token).ConfigureAwait(false);
            ctx.Response.StatusCode = 201;
            await ctx.Response.Send(_Serializer.SerializeJson(created));
        }

        private async Task AuthorizationRoleReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            AuthorizationRoleSearchResult result = await SearchTenantVisibleRoles(req, timeoutCts.Token).ConfigureAwait(false);
            ctx.Response.StatusCode = 200;
            await ctx.Response.Send(_Serializer.SerializeJson(result));
        }

        private async Task AuthorizationRoleReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            Guid roleGuid = ParseRouteGuid(req, "roleGuid");
            AuthorizationRole role = await _LiteGraph.AuthorizationRoles.ReadRoleByGuid(roleGuid, timeoutCts.Token).ConfigureAwait(false);
            if (!RoleVisibleToTenant(role, req.TenantGUID.Value))
            {
                await SendApiError(ctx, 404, ApiErrorEnum.NotFound).ConfigureAwait(false);
                return;
            }

            ctx.Response.StatusCode = 200;
            await ctx.Response.Send(_Serializer.SerializeJson(role));
        }

        private async Task AuthorizationRoleUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            Guid roleGuid = ParseRouteGuid(req, "roleGuid");
            AuthorizationRole existing = await _LiteGraph.AuthorizationRoles.ReadRoleByGuid(roleGuid, timeoutCts.Token).ConfigureAwait(false);
            if (!RoleVisibleToTenant(existing, req.TenantGUID.Value))
            {
                await SendApiError(ctx, 404, ApiErrorEnum.NotFound).ConfigureAwait(false);
                return;
            }
            if (existing.BuiltIn || existing.TenantGUID == null)
            {
                await SendApiError(ctx, 409, ApiErrorEnum.Conflict, "Built-in authorization roles are immutable.").ConfigureAwait(false);
                return;
            }

            AuthorizationRole role = _Serializer.DeserializeJson<AuthorizationRole>(ctx.Request.DataAsString);
            role.GUID = roleGuid;
            role.CreatedUtc = existing.CreatedUtc;
            NormalizeAuthorizationRoleForTenant(role, req.TenantGUID.Value, true);

            if (!ValidateAuthorizationRole(role, out string validationError))
            {
                await SendApiError(ctx, 400, ApiErrorEnum.BadRequest, validationError).ConfigureAwait(false);
                return;
            }

            AuthorizationRole updated = await _LiteGraph.AuthorizationRoles.UpdateRole(role, timeoutCts.Token).ConfigureAwait(false);
            ctx.Response.StatusCode = 200;
            await ctx.Response.Send(_Serializer.SerializeJson(updated));
        }

        private async Task AuthorizationRoleDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            Guid roleGuid = ParseRouteGuid(req, "roleGuid");
            AuthorizationRole existing = await _LiteGraph.AuthorizationRoles.ReadRoleByGuid(roleGuid, timeoutCts.Token).ConfigureAwait(false);
            if (!RoleVisibleToTenant(existing, req.TenantGUID.Value))
            {
                await SendApiError(ctx, 404, ApiErrorEnum.NotFound).ConfigureAwait(false);
                return;
            }
            if (existing.BuiltIn || existing.TenantGUID == null)
            {
                await SendApiError(ctx, 409, ApiErrorEnum.Conflict, "Built-in authorization roles are immutable.").ConfigureAwait(false);
                return;
            }

            await _LiteGraph.AuthorizationRoles.DeleteRoleByGuid(roleGuid, timeoutCts.Token).ConfigureAwait(false);
            ctx.Response.StatusCode = 204;
            await ctx.Response.Send();
        }

        private async Task UserRoleAssignmentCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            UserRoleAssignment assignment = _Serializer.DeserializeJson<UserRoleAssignment>(ctx.Request.DataAsString);
            NormalizeUserRoleAssignment(assignment, req.TenantGUID.Value, req.UserGUID.Value, true);

            if (!await ValidateRoleReference(req.TenantGUID.Value, assignment.RoleGUID, assignment.RoleName, timeoutCts.Token).ConfigureAwait(false))
            {
                await SendApiError(ctx, 400, ApiErrorEnum.BadRequest, "A valid role GUID or role name is required.").ConfigureAwait(false);
                return;
            }

            UserRoleAssignment created = await _LiteGraph.AuthorizationRoles.CreateUserRole(assignment, timeoutCts.Token).ConfigureAwait(false);
            ctx.Response.StatusCode = 201;
            await ctx.Response.Send(_Serializer.SerializeJson(created));
        }

        private async Task UserRoleAssignmentReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            UserRoleAssignmentSearchRequest search = BuildUserRoleAssignmentSearch(req);
            UserRoleAssignmentSearchResult result = await _LiteGraph.AuthorizationRoles.SearchUserRoles(search, timeoutCts.Token).ConfigureAwait(false);
            ctx.Response.StatusCode = 200;
            await ctx.Response.Send(_Serializer.SerializeJson(result));
        }

        private async Task UserRoleAssignmentReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            Guid assignmentGuid = ParseRouteGuid(req, "assignmentGuid");
            UserRoleAssignment assignment = await _LiteGraph.AuthorizationRoles.ReadUserRoleByGuid(assignmentGuid, timeoutCts.Token).ConfigureAwait(false);
            if (!UserRoleAssignmentVisible(assignment, req.TenantGUID.Value, req.UserGUID.Value))
            {
                await SendApiError(ctx, 404, ApiErrorEnum.NotFound).ConfigureAwait(false);
                return;
            }

            ctx.Response.StatusCode = 200;
            await ctx.Response.Send(_Serializer.SerializeJson(assignment));
        }

        private async Task UserRoleAssignmentUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            Guid assignmentGuid = ParseRouteGuid(req, "assignmentGuid");
            UserRoleAssignment existing = await _LiteGraph.AuthorizationRoles.ReadUserRoleByGuid(assignmentGuid, timeoutCts.Token).ConfigureAwait(false);
            if (!UserRoleAssignmentVisible(existing, req.TenantGUID.Value, req.UserGUID.Value))
            {
                await SendApiError(ctx, 404, ApiErrorEnum.NotFound).ConfigureAwait(false);
                return;
            }

            UserRoleAssignment assignment = _Serializer.DeserializeJson<UserRoleAssignment>(ctx.Request.DataAsString);
            assignment.GUID = assignmentGuid;
            assignment.CreatedUtc = existing.CreatedUtc;
            NormalizeUserRoleAssignment(assignment, req.TenantGUID.Value, req.UserGUID.Value, false);

            if (!await ValidateRoleReference(req.TenantGUID.Value, assignment.RoleGUID, assignment.RoleName, timeoutCts.Token).ConfigureAwait(false))
            {
                await SendApiError(ctx, 400, ApiErrorEnum.BadRequest, "A valid role GUID or role name is required.").ConfigureAwait(false);
                return;
            }

            UserRoleAssignment updated = await _LiteGraph.AuthorizationRoles.UpdateUserRole(assignment, timeoutCts.Token).ConfigureAwait(false);
            ctx.Response.StatusCode = 200;
            await ctx.Response.Send(_Serializer.SerializeJson(updated));
        }

        private async Task UserRoleAssignmentDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            Guid assignmentGuid = ParseRouteGuid(req, "assignmentGuid");
            UserRoleAssignment existing = await _LiteGraph.AuthorizationRoles.ReadUserRoleByGuid(assignmentGuid, timeoutCts.Token).ConfigureAwait(false);
            if (!UserRoleAssignmentVisible(existing, req.TenantGUID.Value, req.UserGUID.Value))
            {
                await SendApiError(ctx, 404, ApiErrorEnum.NotFound).ConfigureAwait(false);
                return;
            }

            await _LiteGraph.AuthorizationRoles.DeleteUserRoleByGuid(assignmentGuid, timeoutCts.Token).ConfigureAwait(false);
            ctx.Response.StatusCode = 204;
            await ctx.Response.Send();
        }

        private async Task CredentialScopeAssignmentCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            CredentialScopeAssignment assignment = _Serializer.DeserializeJson<CredentialScopeAssignment>(ctx.Request.DataAsString);
            NormalizeCredentialScopeAssignment(assignment, req.TenantGUID.Value, req.CredentialGUID.Value, true);

            if (!await ValidateCredentialScopeAssignment(req.TenantGUID.Value, assignment, timeoutCts.Token).ConfigureAwait(false))
            {
                await SendApiError(ctx, 400, ApiErrorEnum.BadRequest, "A credential scope assignment requires a valid role reference or direct permissions and resource types.").ConfigureAwait(false);
                return;
            }

            CredentialScopeAssignment created = await _LiteGraph.AuthorizationRoles.CreateCredentialScope(assignment, timeoutCts.Token).ConfigureAwait(false);
            ctx.Response.StatusCode = 201;
            await ctx.Response.Send(_Serializer.SerializeJson(created));
        }

        private async Task CredentialScopeAssignmentReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            CredentialScopeAssignmentSearchRequest search = BuildCredentialScopeAssignmentSearch(req);
            CredentialScopeAssignmentSearchResult result = await _LiteGraph.AuthorizationRoles.SearchCredentialScopes(search, timeoutCts.Token).ConfigureAwait(false);
            ctx.Response.StatusCode = 200;
            await ctx.Response.Send(_Serializer.SerializeJson(result));
        }

        private async Task CredentialScopeAssignmentReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            Guid assignmentGuid = ParseRouteGuid(req, "assignmentGuid");
            CredentialScopeAssignment assignment = await _LiteGraph.AuthorizationRoles.ReadCredentialScopeByGuid(assignmentGuid, timeoutCts.Token).ConfigureAwait(false);
            if (!CredentialScopeAssignmentVisible(assignment, req.TenantGUID.Value, req.CredentialGUID.Value))
            {
                await SendApiError(ctx, 404, ApiErrorEnum.NotFound).ConfigureAwait(false);
                return;
            }

            ctx.Response.StatusCode = 200;
            await ctx.Response.Send(_Serializer.SerializeJson(assignment));
        }

        private async Task CredentialScopeAssignmentUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            Guid assignmentGuid = ParseRouteGuid(req, "assignmentGuid");
            CredentialScopeAssignment existing = await _LiteGraph.AuthorizationRoles.ReadCredentialScopeByGuid(assignmentGuid, timeoutCts.Token).ConfigureAwait(false);
            if (!CredentialScopeAssignmentVisible(existing, req.TenantGUID.Value, req.CredentialGUID.Value))
            {
                await SendApiError(ctx, 404, ApiErrorEnum.NotFound).ConfigureAwait(false);
                return;
            }

            CredentialScopeAssignment assignment = _Serializer.DeserializeJson<CredentialScopeAssignment>(ctx.Request.DataAsString);
            assignment.GUID = assignmentGuid;
            assignment.CreatedUtc = existing.CreatedUtc;
            NormalizeCredentialScopeAssignment(assignment, req.TenantGUID.Value, req.CredentialGUID.Value, false);

            if (!await ValidateCredentialScopeAssignment(req.TenantGUID.Value, assignment, timeoutCts.Token).ConfigureAwait(false))
            {
                await SendApiError(ctx, 400, ApiErrorEnum.BadRequest, "A credential scope assignment requires a valid role reference or direct permissions and resource types.").ConfigureAwait(false);
                return;
            }

            CredentialScopeAssignment updated = await _LiteGraph.AuthorizationRoles.UpdateCredentialScope(assignment, timeoutCts.Token).ConfigureAwait(false);
            ctx.Response.StatusCode = 200;
            await ctx.Response.Send(_Serializer.SerializeJson(updated));
        }

        private async Task CredentialScopeAssignmentDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            Guid assignmentGuid = ParseRouteGuid(req, "assignmentGuid");
            CredentialScopeAssignment existing = await _LiteGraph.AuthorizationRoles.ReadCredentialScopeByGuid(assignmentGuid, timeoutCts.Token).ConfigureAwait(false);
            if (!CredentialScopeAssignmentVisible(existing, req.TenantGUID.Value, req.CredentialGUID.Value))
            {
                await SendApiError(ctx, 404, ApiErrorEnum.NotFound).ConfigureAwait(false);
                return;
            }

            await _LiteGraph.AuthorizationRoles.DeleteCredentialScopeByGuid(assignmentGuid, timeoutCts.Token).ConfigureAwait(false);
            ctx.Response.StatusCode = 204;
            await ctx.Response.Send();
        }

        private async Task UserEffectivePermissionsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            Guid? graphGuid = QueryGuid(req.Query, "graphGuid");
            AuthorizationEffectivePermissionsResult result = new AuthorizationEffectivePermissionsResult
            {
                TenantGUID = req.TenantGUID.Value,
                UserGUID = req.UserGUID.Value,
                GraphGUID = graphGuid
            };

            UserRoleAssignmentSearchResult assignments = await _LiteGraph.AuthorizationRoles.SearchUserRoles(new UserRoleAssignmentSearchRequest
            {
                TenantGUID = req.TenantGUID.Value,
                UserGUID = req.UserGUID.Value,
                PageSize = 1000
            }, timeoutCts.Token).ConfigureAwait(false);

            result.UserRoleAssignments = assignments.Objects;
            foreach (UserRoleAssignment assignment in result.UserRoleAssignments)
            {
                AuthorizationRole role = await ResolveAuthorizationRole(req.TenantGUID.Value, assignment.RoleGUID, assignment.RoleName, timeoutCts.Token).ConfigureAwait(false);
                AddResolvedRole(result.Roles, role);
                result.Grants.Add(BuildEffectiveGrant("UserRole", assignment, role, graphGuid));
            }

            ctx.Response.StatusCode = 200;
            await ctx.Response.Send(_Serializer.SerializeJson(result));
        }

        private async Task CredentialEffectivePermissionsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            Guid? graphGuid = QueryGuid(req.Query, "graphGuid");
            AuthorizationEffectivePermissionsResult result = new AuthorizationEffectivePermissionsResult
            {
                TenantGUID = req.TenantGUID.Value,
                CredentialGUID = req.CredentialGUID.Value,
                GraphGUID = graphGuid
            };

            CredentialScopeAssignmentSearchResult assignments = await _LiteGraph.AuthorizationRoles.SearchCredentialScopes(new CredentialScopeAssignmentSearchRequest
            {
                TenantGUID = req.TenantGUID.Value,
                CredentialGUID = req.CredentialGUID.Value,
                PageSize = 1000
            }, timeoutCts.Token).ConfigureAwait(false);

            result.CredentialScopeAssignments = assignments.Objects;
            foreach (CredentialScopeAssignment assignment in result.CredentialScopeAssignments)
            {
                AuthorizationRole role = await ResolveAuthorizationRole(req.TenantGUID.Value, assignment.RoleGUID, assignment.RoleName, timeoutCts.Token).ConfigureAwait(false);
                AddResolvedRole(result.Roles, role);
                result.Grants.Add(BuildEffectiveGrant("CredentialScope", assignment, role, graphGuid));
            }

            ctx.Response.StatusCode = 200;
            await ctx.Response.Send(_Serializer.SerializeJson(result));
        }

        private AuthorizationRoleSearchRequest BuildAuthorizationRoleSearch(RequestContext req)
        {
            NameValueCollection q = req.Query;
            AuthorizationRoleSearchRequest search = new AuthorizationRoleSearchRequest();

            if (!String.IsNullOrEmpty(q?["name"])) search.Name = q["name"];
            if (!String.IsNullOrEmpty(q?["builtIn"]) && Boolean.TryParse(q["builtIn"], out bool builtIn)) search.BuiltIn = builtIn;
            if (!String.IsNullOrEmpty(q?["builtInRole"]) && Enum.TryParse(q["builtInRole"], true, out BuiltInRoleEnum builtInRole)) search.BuiltInRole = builtInRole;
            if (!String.IsNullOrEmpty(q?["resourceScope"]) && Enum.TryParse(q["resourceScope"], true, out AuthorizationResourceScopeEnum resourceScope)) search.ResourceScope = resourceScope;
            if (!String.IsNullOrEmpty(q?["permission"]) && Enum.TryParse(q["permission"], true, out AuthorizationPermissionEnum permission)) search.Permission = permission;
            if (!String.IsNullOrEmpty(q?["resourceType"]) && Enum.TryParse(q["resourceType"], true, out AuthorizationResourceTypeEnum resourceType)) search.ResourceType = resourceType;
            ApplyCommonSearchQuery(q, search);
            return search;
        }

        private UserRoleAssignmentSearchRequest BuildUserRoleAssignmentSearch(RequestContext req)
        {
            NameValueCollection q = req.Query;
            UserRoleAssignmentSearchRequest search = new UserRoleAssignmentSearchRequest
            {
                TenantGUID = req.TenantGUID.Value,
                UserGUID = req.UserGUID.Value
            };

            if (!String.IsNullOrEmpty(q?["roleGuid"]) && Guid.TryParse(q["roleGuid"], out Guid roleGuid)) search.RoleGUID = roleGuid;
            if (!String.IsNullOrEmpty(q?["roleName"])) search.RoleName = q["roleName"];
            if (!String.IsNullOrEmpty(q?["resourceScope"]) && Enum.TryParse(q["resourceScope"], true, out AuthorizationResourceScopeEnum resourceScope)) search.ResourceScope = resourceScope;
            if (!String.IsNullOrEmpty(q?["graphGuid"]) && Guid.TryParse(q["graphGuid"], out Guid graphGuid)) search.GraphGUID = graphGuid;
            ApplyCommonSearchQuery(q, search);
            return search;
        }

        private CredentialScopeAssignmentSearchRequest BuildCredentialScopeAssignmentSearch(RequestContext req)
        {
            NameValueCollection q = req.Query;
            CredentialScopeAssignmentSearchRequest search = new CredentialScopeAssignmentSearchRequest
            {
                TenantGUID = req.TenantGUID.Value,
                CredentialGUID = req.CredentialGUID.Value
            };

            if (!String.IsNullOrEmpty(q?["roleGuid"]) && Guid.TryParse(q["roleGuid"], out Guid roleGuid)) search.RoleGUID = roleGuid;
            if (!String.IsNullOrEmpty(q?["roleName"])) search.RoleName = q["roleName"];
            if (!String.IsNullOrEmpty(q?["resourceScope"]) && Enum.TryParse(q["resourceScope"], true, out AuthorizationResourceScopeEnum resourceScope)) search.ResourceScope = resourceScope;
            if (!String.IsNullOrEmpty(q?["graphGuid"]) && Guid.TryParse(q["graphGuid"], out Guid graphGuid)) search.GraphGUID = graphGuid;
            if (!String.IsNullOrEmpty(q?["permission"]) && Enum.TryParse(q["permission"], true, out AuthorizationPermissionEnum permission)) search.Permission = permission;
            if (!String.IsNullOrEmpty(q?["resourceType"]) && Enum.TryParse(q["resourceType"], true, out AuthorizationResourceTypeEnum resourceType)) search.ResourceType = resourceType;
            ApplyCommonSearchQuery(q, search);
            return search;
        }

        private async Task<AuthorizationRoleSearchResult> SearchTenantVisibleRoles(RequestContext req, CancellationToken token)
        {
            AuthorizationRoleSearchRequest query = BuildAuthorizationRoleSearch(req);
            bool includeBuiltIns = ParseBoolean(req.Query?["includeBuiltIns"], true);

            if (!includeBuiltIns)
            {
                query.TenantGUID = req.TenantGUID.Value;
                return await _LiteGraph.AuthorizationRoles.SearchRoles(query, token).ConfigureAwait(false);
            }

            int page = query.Page;
            int pageSize = query.PageSize;
            List<AuthorizationRole> roles = new List<AuthorizationRole>();

            if (query.BuiltIn != false)
            {
                AuthorizationRoleSearchRequest builtInSearch = CloneRoleSearch(query);
                builtInSearch.TenantGUID = null;
                builtInSearch.BuiltIn = true;
                builtInSearch.Page = 0;
                builtInSearch.PageSize = 1000;
                AuthorizationRoleSearchResult builtIns = await _LiteGraph.AuthorizationRoles.SearchRoles(builtInSearch, token).ConfigureAwait(false);
                roles.AddRange(builtIns.Objects);
            }

            if (query.BuiltIn != true)
            {
                AuthorizationRoleSearchRequest tenantSearch = CloneRoleSearch(query);
                tenantSearch.TenantGUID = req.TenantGUID.Value;
                tenantSearch.BuiltIn = false;
                tenantSearch.Page = 0;
                tenantSearch.PageSize = 1000;
                AuthorizationRoleSearchResult tenantRoles = await _LiteGraph.AuthorizationRoles.SearchRoles(tenantSearch, token).ConfigureAwait(false);
                roles.AddRange(tenantRoles.Objects);
            }

            roles = roles
                .OrderBy(role => role.BuiltIn ? 0 : 1)
                .ThenBy(role => role.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return new AuthorizationRoleSearchResult
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = roles.Count,
                TotalPages = (int)Math.Ceiling((double)roles.Count / pageSize),
                Objects = roles.Skip(page * pageSize).Take(pageSize).ToList()
            };
        }

        private static AuthorizationRoleSearchRequest CloneRoleSearch(AuthorizationRoleSearchRequest search)
        {
            return new AuthorizationRoleSearchRequest
            {
                TenantGUID = search.TenantGUID,
                Name = search.Name,
                BuiltIn = search.BuiltIn,
                BuiltInRole = search.BuiltInRole,
                ResourceScope = search.ResourceScope,
                Permission = search.Permission,
                ResourceType = search.ResourceType,
                FromUtc = search.FromUtc,
                ToUtc = search.ToUtc,
                Page = search.Page,
                PageSize = search.PageSize
            };
        }

        private static void ApplyCommonSearchQuery(NameValueCollection q, AuthorizationRoleSearchRequest search)
        {
            if (!String.IsNullOrEmpty(q?["fromUtc"]) && DateTime.TryParse(q["fromUtc"], null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime from)) search.FromUtc = from;
            if (!String.IsNullOrEmpty(q?["toUtc"]) && DateTime.TryParse(q["toUtc"], null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime to)) search.ToUtc = to;
            ApplyPaging(q, out int page, out int pageSize);
            search.Page = page;
            search.PageSize = pageSize;
        }

        private static void ApplyCommonSearchQuery(NameValueCollection q, UserRoleAssignmentSearchRequest search)
        {
            if (!String.IsNullOrEmpty(q?["fromUtc"]) && DateTime.TryParse(q["fromUtc"], null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime from)) search.FromUtc = from;
            if (!String.IsNullOrEmpty(q?["toUtc"]) && DateTime.TryParse(q["toUtc"], null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime to)) search.ToUtc = to;
            ApplyPaging(q, out int page, out int pageSize);
            search.Page = page;
            search.PageSize = pageSize;
        }

        private static void ApplyCommonSearchQuery(NameValueCollection q, CredentialScopeAssignmentSearchRequest search)
        {
            if (!String.IsNullOrEmpty(q?["fromUtc"]) && DateTime.TryParse(q["fromUtc"], null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime from)) search.FromUtc = from;
            if (!String.IsNullOrEmpty(q?["toUtc"]) && DateTime.TryParse(q["toUtc"], null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime to)) search.ToUtc = to;
            ApplyPaging(q, out int page, out int pageSize);
            search.Page = page;
            search.PageSize = pageSize;
        }

        private static void ApplyPaging(NameValueCollection q, out int page, out int pageSize)
        {
            page = 0;
            pageSize = 100;
            if (!String.IsNullOrEmpty(q?["page"]) && Int32.TryParse(q["page"], out int parsedPage) && parsedPage >= 0) page = parsedPage;
            if (!String.IsNullOrEmpty(q?["pageSize"]) && Int32.TryParse(q["pageSize"], out int parsedPageSize) && parsedPageSize >= 1 && parsedPageSize <= 1000) pageSize = parsedPageSize;
        }

        private static void NormalizeAuthorizationRoleForTenant(AuthorizationRole role, Guid tenantGuid, bool forceCustom)
        {
            if (role == null) throw new ArgumentNullException(nameof(role));
            if (role.GUID == Guid.Empty) role.GUID = Guid.NewGuid();
            role.TenantGUID = tenantGuid;
            if (forceCustom)
            {
                role.BuiltIn = false;
                role.BuiltInRole = BuiltInRoleEnum.Custom;
            }
            role.Permissions ??= new List<AuthorizationPermissionEnum>();
            role.ResourceTypes ??= new List<AuthorizationResourceTypeEnum>();
            role.LastUpdateUtc = DateTime.UtcNow;
        }

        private static bool ValidateAuthorizationRole(AuthorizationRole role, out string error)
        {
            error = null;
            if (role == null)
            {
                error = "Role is required.";
                return false;
            }
            if (String.IsNullOrWhiteSpace(role.Name))
            {
                error = "Role name is required.";
                return false;
            }
            if (role.Permissions == null || role.Permissions.Count < 1)
            {
                error = "At least one role permission is required.";
                return false;
            }
            if (role.ResourceTypes == null || role.ResourceTypes.Count < 1)
            {
                error = "At least one role resource type is required.";
                return false;
            }
            return true;
        }

        private static void NormalizeUserRoleAssignment(UserRoleAssignment assignment, Guid tenantGuid, Guid userGuid, bool ensureGuid)
        {
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));
            if (ensureGuid && assignment.GUID == Guid.Empty) assignment.GUID = Guid.NewGuid();
            assignment.TenantGUID = tenantGuid;
            assignment.UserGUID = userGuid;
            assignment.LastUpdateUtc = DateTime.UtcNow;
            if (assignment.ResourceScope == AuthorizationResourceScopeEnum.Tenant) assignment.GraphGUID = null;
        }

        private static void NormalizeCredentialScopeAssignment(CredentialScopeAssignment assignment, Guid tenantGuid, Guid credentialGuid, bool ensureGuid)
        {
            if (assignment == null) throw new ArgumentNullException(nameof(assignment));
            if (ensureGuid && assignment.GUID == Guid.Empty) assignment.GUID = Guid.NewGuid();
            assignment.TenantGUID = tenantGuid;
            assignment.CredentialGUID = credentialGuid;
            assignment.Permissions ??= new List<AuthorizationPermissionEnum>();
            assignment.ResourceTypes ??= new List<AuthorizationResourceTypeEnum>();
            assignment.LastUpdateUtc = DateTime.UtcNow;
            if (assignment.ResourceScope == AuthorizationResourceScopeEnum.Tenant) assignment.GraphGUID = null;
        }

        private async Task<bool> ValidateCredentialScopeAssignment(Guid tenantGuid, CredentialScopeAssignment assignment, CancellationToken token)
        {
            if (assignment == null) return false;
            bool hasRoleReference = await ValidateRoleReference(tenantGuid, assignment.RoleGUID, assignment.RoleName, token).ConfigureAwait(false);
            bool hasDirectGrant = assignment.Permissions != null
                && assignment.Permissions.Count > 0
                && assignment.ResourceTypes != null
                && assignment.ResourceTypes.Count > 0;

            return hasRoleReference || hasDirectGrant;
        }

        private async Task<bool> ValidateRoleReference(Guid tenantGuid, Guid? roleGuid, string roleName, CancellationToken token)
        {
            if (roleGuid.HasValue)
            {
                AuthorizationRole role = await _LiteGraph.AuthorizationRoles.ReadRoleByGuid(roleGuid.Value, token).ConfigureAwait(false);
                if (RoleVisibleToTenant(role, tenantGuid)) return true;
            }

            if (!String.IsNullOrWhiteSpace(roleName))
            {
                AuthorizationRole role = await ResolveAuthorizationRole(tenantGuid, null, roleName, token).ConfigureAwait(false);
                if (role != null) return true;
            }

            return false;
        }

        private async Task<AuthorizationRole> ResolveAuthorizationRole(Guid tenantGuid, Guid? roleGuid, string roleName, CancellationToken token)
        {
            if (roleGuid.HasValue)
            {
                AuthorizationRole role = await _LiteGraph.AuthorizationRoles.ReadRoleByGuid(roleGuid.Value, token).ConfigureAwait(false);
                if (RoleVisibleToTenant(role, tenantGuid)) return role;
            }

            if (!String.IsNullOrWhiteSpace(roleName))
            {
                AuthorizationRole tenantRole = await _LiteGraph.AuthorizationRoles.ReadRoleByName(tenantGuid, roleName, token).ConfigureAwait(false);
                if (tenantRole != null) return tenantRole;

                AuthorizationRole globalRole = await _LiteGraph.AuthorizationRoles.ReadRoleByName(null, roleName, token).ConfigureAwait(false);
                if (globalRole != null) return globalRole;

                RoleDefinition builtIn = AuthorizationPolicyDefinitions.GetBuiltInRole(roleName);
                if (builtIn != null) return AuthorizationRole.FromDefinition(builtIn, null);
            }

            return null;
        }

        private static AuthorizationEffectiveGrant BuildEffectiveGrant(string source, UserRoleAssignment assignment, AuthorizationRole role, Guid? requestedGraphGuid)
        {
            return new AuthorizationEffectiveGrant
            {
                Source = source,
                AssignmentGUID = assignment.GUID,
                RoleGUID = assignment.RoleGUID ?? role?.GUID,
                RoleName = assignment.RoleName ?? role?.Name,
                ResourceScope = assignment.ResourceScope,
                GraphGUID = assignment.GraphGUID,
                Permissions = role?.Permissions != null ? new List<AuthorizationPermissionEnum>(role.Permissions) : new List<AuthorizationPermissionEnum>(),
                ResourceTypes = role?.ResourceTypes != null ? new List<AuthorizationResourceTypeEnum>(role.ResourceTypes) : new List<AuthorizationResourceTypeEnum>(),
                InheritsToGraphs = role?.InheritsToGraphs ?? false,
                AppliesToRequestedGraph = GrantAppliesToRequestedGraph(assignment.ResourceScope, assignment.GraphGUID, role?.InheritsToGraphs ?? false, requestedGraphGuid)
            };
        }

        private static AuthorizationEffectiveGrant BuildEffectiveGrant(string source, CredentialScopeAssignment assignment, AuthorizationRole role, Guid? requestedGraphGuid)
        {
            List<AuthorizationPermissionEnum> permissions = role?.Permissions != null
                ? new List<AuthorizationPermissionEnum>(role.Permissions)
                : new List<AuthorizationPermissionEnum>();
            if (assignment.Permissions != null) permissions.AddRange(assignment.Permissions);

            List<AuthorizationResourceTypeEnum> resourceTypes = role?.ResourceTypes != null
                ? new List<AuthorizationResourceTypeEnum>(role.ResourceTypes)
                : new List<AuthorizationResourceTypeEnum>();
            if (assignment.ResourceTypes != null) resourceTypes.AddRange(assignment.ResourceTypes);

            return new AuthorizationEffectiveGrant
            {
                Source = source,
                AssignmentGUID = assignment.GUID,
                RoleGUID = assignment.RoleGUID ?? role?.GUID,
                RoleName = assignment.RoleName ?? role?.Name,
                ResourceScope = assignment.ResourceScope,
                GraphGUID = assignment.GraphGUID,
                Permissions = permissions.Distinct().ToList(),
                ResourceTypes = resourceTypes.Distinct().ToList(),
                InheritsToGraphs = role?.InheritsToGraphs ?? false,
                AppliesToRequestedGraph = GrantAppliesToRequestedGraph(assignment.ResourceScope, assignment.GraphGUID, role?.InheritsToGraphs ?? false, requestedGraphGuid)
            };
        }

        private static bool GrantAppliesToRequestedGraph(
            AuthorizationResourceScopeEnum scope,
            Guid? assignmentGraphGuid,
            bool inheritsToGraphs,
            Guid? requestedGraphGuid)
        {
            if (!requestedGraphGuid.HasValue) return true;
            if (scope == AuthorizationResourceScopeEnum.Tenant) return inheritsToGraphs;
            if (!assignmentGraphGuid.HasValue) return true;
            return assignmentGraphGuid.Value == requestedGraphGuid.Value;
        }

        private static void AddResolvedRole(List<AuthorizationRole> roles, AuthorizationRole role)
        {
            if (roles == null || role == null) return;
            if (role.GUID != Guid.Empty && roles.Any(existing => existing.GUID == role.GUID)) return;
            if (role.GUID == Guid.Empty && roles.Any(existing => String.Equals(existing.Name, role.Name, StringComparison.OrdinalIgnoreCase))) return;
            roles.Add(role);
        }

        private static bool RoleVisibleToTenant(AuthorizationRole role, Guid tenantGuid)
        {
            if (role == null) return false;
            if (role.TenantGUID.HasValue) return role.TenantGUID.Value == tenantGuid;
            return role.BuiltIn;
        }

        private static bool UserRoleAssignmentVisible(UserRoleAssignment assignment, Guid tenantGuid, Guid userGuid)
        {
            if (assignment == null) return false;
            return assignment.TenantGUID == tenantGuid && assignment.UserGUID == userGuid;
        }

        private static bool CredentialScopeAssignmentVisible(CredentialScopeAssignment assignment, Guid tenantGuid, Guid credentialGuid)
        {
            if (assignment == null) return false;
            return assignment.TenantGUID == tenantGuid && assignment.CredentialGUID == credentialGuid;
        }

        private static Guid ParseRouteGuid(RequestContext req, string parameterName)
        {
            string val = req.Http?.Request?.Url?.Parameters?.Get(parameterName);
            if (String.IsNullOrEmpty(val)) throw new ArgumentException(parameterName + " parameter is required.");
            return Guid.Parse(val);
        }

        private static Guid? QueryGuid(NameValueCollection q, string parameterName)
        {
            if (String.IsNullOrEmpty(q?[parameterName])) return null;
            if (Guid.TryParse(q[parameterName], out Guid guid)) return guid;
            return null;
        }

        private static bool ParseBoolean(string val, bool defaultValue)
        {
            if (String.IsNullOrEmpty(val)) return defaultValue;
            if (Boolean.TryParse(val, out bool parsed)) return parsed;
            return defaultValue;
        }

        private async Task SendApiError(HttpContextBase ctx, int statusCode, ApiErrorEnum error, string description = null)
        {
            ctx.Response.StatusCode = statusCode;
            await ctx.Response.Send(_Serializer.SerializeJson(new LiteGraph.Server.Classes.ApiErrorResponse(error, null, description)));
        }

        #endregion
    }
}
