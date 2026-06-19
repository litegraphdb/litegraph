namespace LiteGraph.Server.API.REST
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Indexing.Vector;
    using LiteGraph.Serialization;
    using LiteGraph.Server.API.Agnostic;
    using LiteGraph.Server.Classes;
    using LiteGraph.Server.Services;
    using ApiErrorResponse = LiteGraph.Server.Classes.ApiErrorResponse;
    using AuthenticationResultEnum = LiteGraph.Server.Classes.AuthenticationResultEnum;
    using AuthorizationResultEnum = LiteGraph.Server.Classes.AuthorizationResultEnum;
    using SyslogLogging;
    using WatsonWebserver;
    using WatsonWebserver.Core;
    using WatsonWebserver.Core.OpenApi;

    internal partial class RestServiceHandler : IDisposable
    {
        #region Internal-Members

        #endregion

        #region Private-Members

        private readonly string _Header = "[RestServiceHandler] ";
        static string _Hostname = Dns.GetHostName();
        private Settings _Settings = null;
        private LoggingModule _Logging = null;
        private LiteGraphClient _LiteGraph = null;
        private Serializer _Serializer = null;
        private AuthenticationService _Authentication = null;
        private ServiceHandler _ServiceHandler = null;
        private RequestHistoryService _RequestHistory = null;
        private ObservabilityService _Observability = null;

        private Webserver _Webserver = null;
        private bool _Disposed = false;

        private List<string> _Localhost = new List<string>
        {
            "127.0.0.1",
            "localhost",
            "::1"
        };

        #endregion

        #region Constructors-and-Factories

        internal RestServiceHandler(
            Settings settings,
            LoggingModule logging,
            LiteGraphClient litegraph,
            Serializer serializer,
            AuthenticationService auth,
            ServiceHandler service,
            RequestHistoryService requestHistory,
            ObservabilityService observability)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _LiteGraph = litegraph ?? throw new ArgumentNullException(nameof(litegraph));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _Authentication = auth ?? throw new ArgumentNullException(nameof(auth));
            _ServiceHandler = service ?? throw new ArgumentNullException(nameof(service));
            _RequestHistory = requestHistory ?? throw new ArgumentNullException(nameof(requestHistory));
            _Observability = observability ?? throw new ArgumentNullException(nameof(observability));

            _Webserver = new Webserver(_Settings.Rest, DefaultRoute);
            _Webserver.Routes.PreRouting = PreRoutingHandler;
            _Webserver.Routes.AuthenticateRequest = AuthenticateRequest;
            _Webserver.Routes.PostRouting = PostRoutingHandler;
            _Webserver.Routes.Preflight = OptionsHandler;
            _Webserver.Routes.Exception = ExceptionRoute;

            InitializeRoutes();

            _Webserver.UseOpenApi(openApi =>
            {
                openApi.Info.Title = "LiteGraph API";
                openApi.Info.Version = "v7.0.0";
                openApi.Info.Description = "LiteGraph is a lightweight graph database with vector search, multi-tenancy, and AI agent integration. This API provides full CRUD operations for graphs, nodes, edges, labels, tags, and vectors with built-in HNSW vector indexing.";
                openApi.Info.Contact = new OpenApiContact
                {
                    Name = "LiteGraph",
                    Url = "https://github.com/jchristn/LiteGraph"
                };
                openApi.Info.License = new OpenApiLicense
                {
                    Name = "MIT",
                    Url = "https://github.com/jchristn/LiteGraph/blob/main/LICENSE.md"
                };

                openApi.Tags.Add(new OpenApiTag { Name = "System", Description = "Health checks and system information" });
                openApi.Tags.Add(new OpenApiTag { Name = "Tokens", Description = "Authentication token management" });
                openApi.Tags.Add(new OpenApiTag { Name = "Admin", Description = "Administrative operations including backups, restore, and database flush" });
                openApi.Tags.Add(new OpenApiTag { Name = "Tenants", Description = "Multi-tenant management and statistics" });
                openApi.Tags.Add(new OpenApiTag { Name = "Users", Description = "User account management" });
                openApi.Tags.Add(new OpenApiTag { Name = "Credentials", Description = "Bearer token credential management" });
                openApi.Tags.Add(new OpenApiTag { Name = "Authorization", Description = "Roles, assignments, and effective permission inspection" });
                openApi.Tags.Add(new OpenApiTag { Name = "Graphs", Description = "Graph CRUD, search, statistics, and GEXF export" });
                openApi.Tags.Add(new OpenApiTag { Name = "VectorIndex", Description = "HNSW vector index configuration, rebuild, and statistics" });
                openApi.Tags.Add(new OpenApiTag { Name = "Nodes", Description = "Node CRUD, search, and batch operations" });
                openApi.Tags.Add(new OpenApiTag { Name = "NodeTraversal", Description = "Node relationship traversal: parents, children, neighbors, and edges" });
                openApi.Tags.Add(new OpenApiTag { Name = "Edges", Description = "Edge CRUD, search, and batch operations" });
                openApi.Tags.Add(new OpenApiTag { Name = "Routes", Description = "Route finding and graph traversal between nodes" });
                openApi.Tags.Add(new OpenApiTag { Name = "Labels", Description = "Label management for graphs, nodes, and edges" });
                openApi.Tags.Add(new OpenApiTag { Name = "Tags", Description = "Key-value tag management for graphs, nodes, and edges" });
                openApi.Tags.Add(new OpenApiTag { Name = "Vectors", Description = "Vector embedding storage, search, and management" });
                openApi.Tags.Add(new OpenApiTag { Name = "RequestHistory", Description = "HTTP request history capture, search, and retention" });

                openApi.SecuritySchemes["BearerToken"] = new OpenApiSecurityScheme
                {
                    Type = "http",
                    Scheme = "bearer",
                    Description = "Bearer token authentication. Use the admin token or a credential bearer token."
                };
                openApi.SecuritySchemes["EmailPassword"] = new OpenApiSecurityScheme
                {
                    Type = "apiKey",
                    Name = "x-email",
                    In = "header",
                    Description = "Email-based authentication. Requires x-email, x-password, and x-tenant-guid headers."
                };
                openApi.SecuritySchemes["SecurityToken"] = new OpenApiSecurityScheme
                {
                    Type = "apiKey",
                    Name = "x-token",
                    In = "header",
                    Description = "Temporal security token (24-hour expiry). Obtain via GET /v1.0/token."
                };

                openApi.IncludePreAuthRoutes = true;
                openApi.IncludePostAuthRoutes = true;
            });

            _Logging.Info(_Header + "starting REST server on " + _Settings.Rest.Prefix);
            _Webserver.Start();

            if (_Localhost.Contains(_Settings.Rest.Hostname))
            {
                _Logging.Alert(_Header + Environment.NewLine + Environment.NewLine
                    + "NOTICE" + Environment.NewLine
                    + "------" + Environment.NewLine
                    + "LiteGraph is configured to listen on localhost and will not be externally accessible." + Environment.NewLine
                    + "Modify " + Constants.SettingsFile + " to change the REST listener hostname to make externally accessible." + Environment.NewLine);
            }
        }

        #endregion

        #region Internal-Methods

        internal void Stop()
        {
            if (_Disposed) return;
            if (_Webserver == null) return;

            _Logging.Info(_Header + "stopping REST server");
            _Webserver.Stop();
        }

        public void Dispose()
        {
            if (_Disposed) return;

            try
            {
                Stop();
            }
            catch (Exception e)
            {
                _Logging?.Warn(_Header + "error while stopping REST server: " + e.Message);
            }
            finally
            {
                if (_Webserver is IDisposable disposableWebserver) disposableWebserver.Dispose();

                _Webserver = null;
                _Disposed = true;
                GC.SuppressFinalize(this);
            }
        }

        internal void InitializeRoutes()
        {
            _Webserver.Routes.PreAuthentication.Static.Add(HttpMethod.HEAD, "/", LoopbackRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Health check", "System"));
            _Webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/", RootRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Server information", "System"));
            _Webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/favicon.ico", FaviconRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Favicon", "System"));
            _Webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/token/tenants", TokenTenantsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List tenants for email", "Tokens"));
            if (_Settings.Observability.Enable && _Settings.Observability.EnablePrometheus)
            {
                _Webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, _Settings.Observability.MetricsPath, MetricsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Prometheus metrics", "System"));
            }

            #region Tokens

            _Webserver.Routes.PostAuthentication.Static.Add(HttpMethod.GET, "/v1.0/token", TokenCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Generate authentication token", "Tokens"));
            _Webserver.Routes.PostAuthentication.Static.Add(HttpMethod.GET, "/v1.0/token/details", TokenDetailsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get token details", "Tokens"));

            #endregion

            #region Admin

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/backups", BackupReadAllRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List all backups", "Admin"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/backups/{backupFilename}", BackupReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read backup file", "Admin"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/backups/{backupFilename}", BackupExistsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check if backup exists", "Admin"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/backups", BackupRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create backup", "Admin"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/backups/{backupFilename}", BackupDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete backup", "Admin"));

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/flush", FlushRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Flush database to disk", "Admin"));

            #endregion

            #region Tenants

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants", TenantCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create tenant", "Tenants"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants", TenantReadManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List tenants", "Tenants"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v2.0/tenants", TenantEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate tenants", "Tenants"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants", TenantEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate tenants (POST)", "Tenants"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/stats", TenantStatisticsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get all tenant statistics", "Tenants"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}", TenantReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read tenant", "Tenants"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/stats", TenantStatisticsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get tenant statistics", "Tenants"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}", TenantExistsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check if tenant exists", "Tenants"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}", TenantUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update tenant", "Tenants"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}", TenantDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete tenant", "Tenants"));

            #endregion

            #region Users

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/users", UserCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create user", "Users"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/users", UserReadManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List users", "Users"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v2.0/tenants/{tenantGuid}/users", UserEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate users", "Users"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants/{tenantGuid}/users", UserEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate users (POST)", "Users"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/users/{userGuid}", UserReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read user", "Users"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/users/{userGuid}", UserExistsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check if user exists", "Users"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/users/{userGuid}", UserUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update user", "Users"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/users/{userGuid}", UserDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete user", "Users"));

            #endregion

            #region Credentials

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/credentials", CredentialCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create credential", "Credentials"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/credentials", CredentialReadManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List credentials", "Credentials"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v2.0/tenants/{tenantGuid}/credentials", CredentialEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate credentials", "Credentials"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/credentials/bearer/{bearerToken}", CredentialReadByBearerTokenRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Lookup credential by bearer token", "Credentials"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants/{tenantGuid}/credentials", CredentialEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate credentials (POST)", "Credentials"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}", CredentialReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read credential", "Credentials"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}", CredentialExistsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check if credential exists", "Credentials"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}", CredentialUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update credential", "Credentials"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/users/{userGuid}/credentials", CredentialDeleteByUserRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete credentials by user", "Credentials"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/credentials", CredentialDeleteAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all credentials in tenant", "Credentials"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}", CredentialDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete credential", "Credentials"));

            #endregion

            #region Authorization

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/roles", AuthorizationRoleCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create authorization role", "Authorization"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/roles", AuthorizationRoleReadManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List authorization roles", "Authorization"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/roles/{roleGuid}", AuthorizationRoleReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read authorization role", "Authorization"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/roles/{roleGuid}", AuthorizationRoleUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update authorization role", "Authorization"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/roles/{roleGuid}", AuthorizationRoleDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete authorization role", "Authorization"));

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/users/{userGuid}/roles", UserRoleAssignmentCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Assign user role", "Authorization"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/users/{userGuid}/roles", UserRoleAssignmentReadManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List user role assignments", "Authorization"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/users/{userGuid}/roles/{assignmentGuid}", UserRoleAssignmentReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read user role assignment", "Authorization"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/users/{userGuid}/roles/{assignmentGuid}", UserRoleAssignmentUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update user role assignment", "Authorization"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/users/{userGuid}/roles/{assignmentGuid}", UserRoleAssignmentDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Revoke user role assignment", "Authorization"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/users/{userGuid}/permissions", UserEffectivePermissionsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List user effective permissions", "Authorization"));

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}/scopes", CredentialScopeAssignmentCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Assign credential scope", "Authorization"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}/scopes", CredentialScopeAssignmentReadManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List credential scope assignments", "Authorization"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}/scopes/{assignmentGuid}", CredentialScopeAssignmentReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read credential scope assignment", "Authorization"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}/scopes/{assignmentGuid}", CredentialScopeAssignmentUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update credential scope assignment", "Authorization"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}/scopes/{assignmentGuid}", CredentialScopeAssignmentDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Revoke credential scope assignment", "Authorization"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}/permissions", CredentialEffectivePermissionsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List credential effective permissions", "Authorization"));

            #endregion

            #region Labels

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/labels", LabelCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create label", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/labels/bulk", LabelCreateManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create multiple labels", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/labels", LabelReadManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List labels", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v2.0/tenants/{tenantGuid}/labels", LabelEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate labels", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants/{tenantGuid}/labels", LabelEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate labels (POST)", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/labels", LabelEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate graph labels (POST)", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/labels/all", LabelReadAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read all labels in tenant", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/labels/all", LabelReadAllInGraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read all labels in graph", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/labels", LabelReadManyGraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read graph labels", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/labels", LabelReadManyNodeRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read node labels", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}/labels", LabelReadManyEdgeRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read edge labels", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/labels/{labelGuid}", LabelReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read label", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/labels/{labelGuid}", LabelExistsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check if label exists", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/labels/{labelGuid}", LabelUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update label", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/labels/all", LabelDeleteAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all labels in tenant", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/labels/all", LabelDeleteAllInGraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all labels in graph", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/labels", LabelDeleteGraphLabelsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete graph labels", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/labels", LabelDeleteNodeLabelsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete node labels", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}/labels", LabelDeleteEdgeLabelsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete edge labels", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/labels/bulk", LabelDeleteManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete multiple labels", "Labels"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/labels/{labelGuid}", LabelDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete label", "Labels"));

            #endregion

            #region Tags

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/tags", TagCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create tag", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/tags/bulk", TagCreateManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create multiple tags", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/tags/all", TagReadAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read all tags in tenant", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/tags", TagReadManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List tags", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v2.0/tenants/{tenantGuid}/tags", TagEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate tags", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/tags/all", TagReadAllInGraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read all tags in graph", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/tags", TagReadManyGraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read graph tags", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/tags", TagReadManyNodeRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read node tags", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}/tags", TagReadManyEdgeRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read edge tags", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants/{tenantGuid}/tags", TagEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate tags (POST)", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/tags", TagEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate graph tags (POST)", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/tags/{tagGuid}", TagReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read tag", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/tags/{tagGuid}", TagExistsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check if tag exists", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/tags/{tagGuid}", TagUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update tag", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/tags/bulk", TagDeleteManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete multiple tags", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/tags/all", TagDeleteAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all tags in tenant", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/tags/all", TagDeleteAllInGraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all tags in graph", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/tags", TagDeleteGraphTagsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete graph tags", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/tags", TagDeleteNodeTagsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete node tags", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}/tags", TagDeleteEdgeTagsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete edge tags", "Tags"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/tags/{tagGuid}", TagDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete tag", "Tags"));

            #endregion

            #region Vectors

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/vectors", VectorCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create vector", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/vectors/bulk", VectorCreateManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create multiple vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/vectors", VectorSearchRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Search vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectors/search", VectorSearchRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Search graph vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/vectors", VectorReadManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v2.0/tenants/{tenantGuid}/vectors", VectorEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants/{tenantGuid}/vectors", VectorEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate vectors (POST)", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectors", VectorEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate graph vectors (POST)", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/vectors/all", VectorReadAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read all vectors in tenant", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectors/all", VectorReadAllInGraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read all vectors in graph", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectors", VectorReadManyGraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read graph vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/vectors", VectorReadManyNodeRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read node vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}/vectors", VectorReadManyEdgeRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read edge vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/vectors/{vectorGuid}", VectorReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read vector", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/vectors/{vectorGuid}", VectorExistsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check if vector exists", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/vectors/{vectorGuid}", VectorUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update vector", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/vectors/bulk", VectorDeleteManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete multiple vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/vectors/all", VectorDeleteAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all vectors in tenant", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectors/all", VectorDeleteAllInGraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all vectors in graph", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectors", VectorDeleteGraphVectorsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete graph vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/vectors", VectorDeleteNodeVectorsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete node vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}/vectors", VectorDeleteEdgeVectorsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete edge vectors", "Vectors"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/vectors/{vectorGuid}", VectorDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete vector", "Vectors"));

            #endregion

            #region Graphs

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs", GraphCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create graph", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/first", GraphReadFirstRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read first matching graph", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/search", GraphSearchRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Search graphs", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/stats", GraphStatisticsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get all graph statistics", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/all", GraphReadAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read all graphs in tenant", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}", GraphReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read graph", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/stats", GraphStatisticsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get graph statistics", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}", GraphExistsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check if graph exists", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs", GraphReadManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List graphs", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v2.0/tenants/{tenantGuid}/graphs", GraphEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate graphs", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants/{tenantGuid}/graphs", GraphEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate graphs (POST)", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}", GraphUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update graph", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/all", GraphDeleteAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all graphs in tenant", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}", GraphDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete graph", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/existence", GraphExistenceRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check batch existence", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/query", GraphQueryRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Execute native graph query", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/transaction", GraphTransactionRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Execute graph transaction", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/subgraph", GraphSubgraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get subgraph from node", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/subgraph/stats", GraphSubgraphStatisticsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get subgraph statistics", "Graphs"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/export/gexf", GraphGexfExportRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Export graph as GEXF", "Graphs"));

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/enable", GraphEnableVectorIndexRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enable vector indexing", "VectorIndex"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/config", GraphGetVectorIndexConfigRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get vector index configuration", "VectorIndex"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex", GraphDisableVectorIndexRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Disable vector indexing", "VectorIndex"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/rebuild", GraphRebuildVectorIndexRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Rebuild vector index", "VectorIndex"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/stats", GraphVectorIndexStatsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get vector index statistics", "VectorIndex"));

            // v2.0 versions for consistency
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/enable", GraphEnableVectorIndexRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enable vector indexing", "VectorIndex"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/config", GraphGetVectorIndexConfigRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get vector index configuration", "VectorIndex"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex", GraphDisableVectorIndexRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Disable vector indexing", "VectorIndex"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/rebuild", GraphRebuildVectorIndexRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Rebuild vector index", "VectorIndex"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/stats", GraphVectorIndexStatsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get vector index statistics", "VectorIndex"));

            #endregion

            #region Nodes

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes", NodeCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create node", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/bulk", NodeCreateManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create multiple nodes", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/first", NodeReadFirstRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read first matching node", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/search", NodeSearchRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Search nodes", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/all", NodeReadAllInGraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read all nodes in graph", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/mostconnected", NodeReadMostConnectedRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read most connected nodes", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/leastconnected", NodeReadLeastConnectedRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read least connected nodes", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}", NodeReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read node", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}", NodeExistsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check if node exists", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes", NodeReadManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List nodes", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes", NodeEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate nodes", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes", NodeEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate nodes (POST)", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}", NodeUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update node", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/all", NodeDeleteAllRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all nodes in graph", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/bulk", NodeDeleteManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete multiple nodes", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}", NodeDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete node", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/nodes/all", NodeReadAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read all nodes in tenant", "Nodes"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/nodes/all", NodeDeleteAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all nodes in tenant", "Nodes"));

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/edges/from", EdgesFromNodeRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get outgoing edges from node", "NodeTraversal"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/edges/to", EdgesToNodeRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get incoming edges to node", "NodeTraversal"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/edges", AllEdgesToNodeRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get all edges for node", "NodeTraversal"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/edges", AllEdgesToNodeRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get all edges for node with filters", "NodeTraversal"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/neighbors", NodeNeighborsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get node neighbors", "NodeTraversal"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/parents", NodeParentsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get parent nodes", "NodeTraversal"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/children", NodeChildrenRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get child nodes", "NodeTraversal"));

            #endregion

            #region Edges

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges", EdgeCreateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create edge", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/bulk", EdgeCreateManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Create multiple edges", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/between", EdgesBetweenRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get edges between nodes", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/first", EdgeReadFirstRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read first matching edge", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/search", EdgeSearchRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Search edges", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/edges/all", EdgeReadAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read all edges in tenant", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/all", EdgeReadAllInGraphRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read all edges in graph", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}", EdgeReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read edge", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}", EdgeExistsRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Check if edge exists", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges", EdgeReadManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List edges", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges", EdgeEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate edges", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges", EdgeEnumerateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Enumerate edges (POST)", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}", EdgeUpdateRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Update edge", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/all", EdgeDeleteAllRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all edges in graph", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/bulk", EdgeDeleteManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete multiple edges", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}", EdgeDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete edge", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/edges/all", EdgeDeleteAllInTenantRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all edges in tenant", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/edges", EdgeDeleteNodeEdgesRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete all edges for node", "Edges"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/edges/bulk", EdgeDeleteNodeEdgesManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete edges for multiple nodes", "Edges"));

            #endregion

            #region Routes-and-Traversal

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/routes", GetRoutesRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Find routes between nodes", "Routes"));

            #endregion

            #region Request-History

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/requesthistory", RequestHistoryListRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("List request history", "RequestHistory"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/requesthistory/summary", RequestHistorySummaryRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Get request history summary", "RequestHistory"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/requesthistory/{requestGuid}", RequestHistoryReadRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read request history entry", "RequestHistory"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/requesthistory/{requestGuid}/detail", RequestHistoryDetailRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Read request history detail", "RequestHistory"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/requesthistory/bulk", RequestHistoryDeleteManyRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Bulk delete request history", "RequestHistory"));
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/requesthistory/{requestGuid}", RequestHistoryDeleteRoute, ExceptionRoute, openApiMetadata: OpenApiRouteMetadata.Create("Delete request history entry", "RequestHistory"));

            #endregion
        }

        internal async Task OptionsHandler(HttpContextBase ctx)
        {
            NameValueCollection responseHeaders = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);

            string[] requestedHeaders = null;
            string headers = "";

            if (ctx.Request.Headers != null)
            {
                for (int i = 0; i < ctx.Request.Headers.Count; i++)
                {
                    string key = ctx.Request.Headers.GetKey(i);
                    string value = ctx.Request.Headers.Get(i);
                    if (String.IsNullOrEmpty(key)) continue;
                    if (String.IsNullOrEmpty(value)) continue;
                    if (String.Compare(key.ToLower(), "access-control-request-headers") == 0)
                    {
                        requestedHeaders = value.Split(',');
                        break;
                    }
                }
            }

            if (requestedHeaders != null)
            {
                foreach (string curr in requestedHeaders)
                {
                    headers += ", " + curr;
                }
            }

            responseHeaders.Add("Access-Control-Allow-Methods", "OPTIONS, HEAD, GET, PUT, POST, DELETE");
            responseHeaders.Add("Access-Control-Allow-Headers", "*, Content-Type, X-Requested-With, " + Constants.RequestIdHeader + ", " + Constants.CorrelationIdHeader + ", " + Constants.TraceparentHeader + headers);
            responseHeaders.Add("Access-Control-Expose-Headers", "Content-Type, X-Requested-With, " + Constants.RequestIdHeader + ", " + Constants.CorrelationIdHeader + headers);
            responseHeaders.Add("Access-Control-Allow-Origin", "*");
            responseHeaders.Add("Accept", "*/*");
            responseHeaders.Add("Accept-Language", "en-US, en");
            responseHeaders.Add("Accept-Charset", "ISO-8859-1, utf-8");
            responseHeaders.Add("Connection", "keep-alive");

            ctx.Response.StatusCode = 200;
            ctx.Response.Headers = responseHeaders;
            await ctx.Response.Send();
            return;
        }

        internal async Task PreRoutingHandler(HttpContextBase ctx)
        {
            RequestContext req = null;

            ctx.Response.Headers.Add(Constants.HostnameHeader, _Hostname);
            ctx.Response.ContentType = Constants.JsonContentType;

            try
            {
                req = new RequestContext(ctx);
            }
            catch (FormatException fe)
            {
                _Logging.Warn(_Header + "format exception building request context" + Environment.NewLine + fe.ToString());
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, fe.Message)));
                return;
            }
            catch (ArgumentOutOfRangeException aore)
            {
                _Logging.Warn(_Header + ctx.Request.Source.IpAddress + " argument out of range exception building request context" + Environment.NewLine + aore.ToString());
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, aore.Message)));
                return;
            }
            catch (ArgumentNullException ane)
            {
                _Logging.Warn(_Header + ctx.Request.Source.IpAddress + " argument null exception building request context" + Environment.NewLine + ane.ToString());
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, ane.Message)));
                return;
            }
            catch (ArgumentException ae)
            {
                _Logging.Warn(_Header + ctx.Request.Source.IpAddress + " argument exception building request context" + Environment.NewLine + ae.ToString());
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, ae.Message)));
                return;
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + ctx.Request.Source.IpAddress + " exception building request context" + Environment.NewLine + e.ToString());
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError, null, e.Message)));
                return;
            }

            ctx.Metadata = req;
            StartRequestActivity(ctx, req);
            ctx.Response.Headers.Add(Constants.RequestIdHeader, req.RequestId);
            ctx.Response.Headers.Add(Constants.CorrelationIdHeader, req.CorrelationId);
            if (_Settings.Debug.Requests)
                _Logging.Debug(BuildSanitizedRequestDebugLog(ctx));
        }

        internal async Task AuthenticateRequest(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            using (Activity activity = StartInternalActivity("litegraph.auth", req))
            using (CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource())
            {
                try
                {
                    await _Authentication.AuthenticateAndAuthorize(req, timeoutCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                {
                    ctx.Response.StatusCode = 408;
                    RecordActivityException(activity, new TimeoutException("Authentication timed out."), ctx.Response.StatusCode);
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.RequestTimeout)));
                    return;
                }

                _Observability.RecordAuthentication(req.Authentication.Result, req.Authorization.Result);
                SetAuthenticationActivityTags(activity, req);
            }

            switch (req.Authentication.Result)
            {
                case AuthenticationResultEnum.Success:
                    break;

                case AuthenticationResultEnum.NotFound:
                    ctx.Response.StatusCode = 401;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.AuthenticationFailed)));
                    return;

                case AuthenticationResultEnum.Inactive:
                    ctx.Response.StatusCode = 401;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.Inactive)));
                    return;

                case AuthenticationResultEnum.Invalid:
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest)));
                    return;
            }

            switch (req.Authorization.Result)
            {
                case AuthorizationResultEnum.Conflict:
                    ctx.Response.StatusCode = 409;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.Conflict)));
                    return;

                case AuthorizationResultEnum.Denied:
                    ctx.Response.StatusCode = 401;
                    await SendAuthorizationFailed(ctx, req).ConfigureAwait(false);
                    return;

                case AuthorizationResultEnum.NotFound:
                    ctx.Response.StatusCode = 404;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound)));
                    return;

                case AuthorizationResultEnum.Permitted:
                    break;
            }
        }

        internal async Task DefaultRoute(HttpContextBase ctx)
        {
            _Logging.Warn(_Header + "unknown verb or endpoint: " + ctx.Request.Method + " " + OperationalLogRedactor.RedactUrl(ctx.Request.Url.RawWithQuery));
            ctx.Response.StatusCode = 400;
            await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest)));
        }

        internal async Task PostRoutingHandler(HttpContextBase ctx)
        {
            ctx.Timestamp.End = DateTime.UtcNow;
            RequestContext req = ctx.Metadata as RequestContext;
            string msg = OperationalLogFormatter.FormatRequestCompletion(
                _Header,
                ctx.Request.Method.ToString(),
                OperationalLogRedactor.RedactUrl(ctx.Request.Url.RawWithQuery),
                ctx.Response.StatusCode,
                ctx.Timestamp.TotalMs,
                req?.RequestId,
                req?.CorrelationId,
                req?.TraceId,
                ctx.Response.DataAsString,
                ctx.Response.StatusCode > 299 && _Settings.Debug.Requests,
                _Settings.Logging.JsonLogOutput);

            _Logging.Debug(msg);

            _Observability.RecordHttpRequest(
                ctx.Request.Method.ToString(),
                OperationalLogRedactor.RedactUrl(ctx.Request.Url.RawWithoutQuery),
                ctx.Response.StatusCode,
                ctx.Timestamp.TotalMs ?? 0);

            CaptureRequestHistory(ctx);
            StopRequestActivity(ctx);
            await Task.CompletedTask;
        }

        private string BuildSanitizedRequestDebugLog(HttpContextBase ctx)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(_Header + "request:");
            sb.AppendLine("| Method         : " + ctx.Request.Method);
            sb.AppendLine("| URL            : " + OperationalLogRedactor.RedactUrl(ctx.Request.Url.RawWithQuery));
            sb.AppendLine("| Source IP      : " + ctx.Request.Source?.IpAddress);
            sb.AppendLine("| Content type   : " + ctx.Request.ContentType);
            sb.AppendLine("| Content length : " + (ctx.Request.DataAsBytes != null ? ctx.Request.DataAsBytes.LongLength : 0));

            Dictionary<string, string> headers = OperationalLogRedactor.RedactHeaders(ctx.Request.Headers);
            foreach (KeyValuePair<string, string> header in headers.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
            {
                sb.AppendLine("| Header " + header.Key + " : " + header.Value);
            }

            sb.Append("| Body           : <omitted>");
            return sb.ToString();
        }

        private void CaptureRequestHistory(HttpContextBase ctx)
        {
            try
            {
                if (_RequestHistory == null || !_Settings.RequestHistory.Enable) return;
                string path = ctx.Request.Url.RawWithoutQuery;
                string redactedPath = OperationalLogRedactor.RedactUrl(path);
                if (_RequestHistory.ShouldSkip(path)) return;

                RequestContext req = ctx.Metadata as RequestContext;

                int statusCode = ctx.Response.StatusCode;
                bool success = statusCode >= 100 && statusCode < 400;

                byte[] reqBytes = ctx.Request.DataAsBytes;
                long reqLen = reqBytes != null ? reqBytes.LongLength : 0L;
                string reqBody = _RequestHistory.CaptureBody(reqBytes, _Settings.RequestHistory.MaxRequestBodyBytes, out bool reqTrunc);

                string responseText = ctx.Response.DataAsString;
                string respBody = _RequestHistory.CaptureBody(responseText, _Settings.RequestHistory.MaxResponseBodyBytes, out bool respTrunc, out long respLen);

                Guid? tenantGuid = null;
                Guid? userGuid = null;
                if (req != null)
                {
                    if (req.Authentication != null)
                    {
                        tenantGuid = req.Authentication.TenantGUID;
                        userGuid = req.Authentication.UserGUID;
                    }
                    if (tenantGuid == null) tenantGuid = req.TenantGUID;
                }

                RequestHistoryDetail detail = new RequestHistoryDetail
                {
                    GUID = req?.RequestGuid ?? Guid.NewGuid(),
                    CreatedUtc = ctx.Timestamp.Start,
                    CompletedUtc = ctx.Timestamp.End,
                    Method = ctx.Request.Method.ToString(),
                    Path = redactedPath,
                    Url = OperationalLogRedactor.RedactUrl(ctx.Request.Url.RawWithQuery),
                    SourceIp = ctx.Request.Source?.IpAddress,
                    TenantGUID = tenantGuid,
                    UserGUID = userGuid,
                    StatusCode = statusCode,
                    Success = success,
                    ProcessingTimeMs = ctx.Timestamp.TotalMs ?? 0,
                    RequestBodyLength = reqLen,
                    ResponseBodyLength = respLen,
                    RequestBodyTruncated = reqTrunc,
                    ResponseBodyTruncated = respTrunc,
                    RequestContentType = ctx.Request.ContentType,
                    ResponseContentType = ctx.Response.ContentType,
                    RequestHeaders = _RequestHistory.RedactHeaders(ctx.Request.Headers),
                    ResponseHeaders = _RequestHistory.RedactHeaders(ctx.Response.Headers),
                    RequestId = req?.RequestId,
                    CorrelationId = req?.CorrelationId,
                    TraceId = req?.TraceId,
                    TransactionDiagnosticsJson = CaptureTransactionDiagnosticsJson(req, responseText),
                    RequestBody = reqBody,
                    ResponseBody = respBody
                };

                _RequestHistory.Capture(detail);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "request history capture error: " + e.Message);
            }
        }

        private string CaptureTransactionDiagnosticsJson(RequestContext req, string responseText)
        {
            if (req == null || req.RequestType != RequestTypeEnum.GraphTransaction) return null;
            if (String.IsNullOrWhiteSpace(responseText)) return null;

            try
            {
                TransactionResult result = _Serializer.DeserializeJson<TransactionResult>(responseText);
                if (result == null || result.TransactionId == Guid.Empty) return null;

                return _Serializer.SerializeJson(new RequestHistoryTransactionDiagnostics
                {
                    TransactionId = result.TransactionId,
                    State = result.State,
                    Success = result.Success,
                    RolledBack = result.RolledBack,
                    ValidationFailure = result.ValidationFailure,
                    FailedOperationIndex = result.FailedOperationIndex,
                    OperationCount = result.OperationCount,
                    IsolationLevel = result.IsolationLevel,
                    Provider = result.Provider,
                    IsolatedRepository = result.IsolatedRepository,
                    SerializedByGate = result.SerializedByGate,
                    RetryCount = result.RetryCount,
                    Retryable = result.Retryable,
                    ConcurrencyConflict = result.ConcurrencyConflict,
                    ProviderErrorCode = result.ProviderErrorCode,
                    CommitDurationMs = result.CommitDurationMs,
                    RollbackDurationMs = result.RollbackDurationMs
                }, false);
            }
            catch
            {
                return null;
            }
        }

        private sealed class RequestHistoryTransactionDiagnostics
        {
            public Guid TransactionId { get; set; } = Guid.Empty;
            public string State { get; set; } = null;
            public bool Success { get; set; } = false;
            public bool RolledBack { get; set; } = false;
            public bool ValidationFailure { get; set; } = false;
            public int? FailedOperationIndex { get; set; } = null;
            public int OperationCount { get; set; } = 0;
            public string IsolationLevel { get; set; } = null;
            public string Provider { get; set; } = null;
            public bool IsolatedRepository { get; set; } = false;
            public bool SerializedByGate { get; set; } = false;
            public int RetryCount { get; set; } = 0;
            public bool Retryable { get; set; } = false;
            public bool ConcurrencyConflict { get; set; } = false;
            public string ProviderErrorCode { get; set; } = null;
            public double CommitDurationMs { get; set; } = 0;
            public double RollbackDurationMs { get; set; } = 0;
        }

        internal async Task ExceptionHandler(HttpContextBase @base, Exception exception)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Private-Route-Implementations

        #region Token

        private async Task TokenCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            AuthenticationToken token = _Authentication.GenerateToken(req.Authentication.TenantGUID.Value, req.Authentication.UserGUID.Value);
            ctx.Response.StatusCode = 200;
            await ctx.Response.Send(_Serializer.SerializeJson(token));
            return;
        }

        private async Task TokenDetailsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(req.Authentication.SecurityToken))
            {
                _Logging.Warn(_Header + "no authentication token supplied from which to read details");
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, "No authentication token supplied.")));
                return;
            }

            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            try
            {
                AuthenticationToken token = await _Authentication.ReadToken(req.Authentication.SecurityToken, timeoutCts.Token).ConfigureAwait(false);
                ctx.Response.StatusCode = 200;
                await ctx.Response.Send(_Serializer.SerializeJson(token));
            }
            catch (OperationCanceledException oce) when (timeoutCts.IsCancellationRequested)
            {
                await SendRequestTimeout(ctx, "token detail read", oce).ConfigureAwait(false);
            }
            return;
        }

        private async Task TokenTenantsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(req.Authentication.Email))
            {
                _Logging.Warn(_Header + "no email supplied in headers for tenant retrieval");
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, "No email supplied in the request headers.")));
                return;
            }

            ResponseContext resp = await _ServiceHandler.UserTenants(req);
            if (resp.Success)
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.Send(_Serializer.SerializeJson(resp.Data));
                return;
            }
            else
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.Send(_Serializer.SerializeJson(resp.Error));
            }
        }

        #endregion

        #region Admin

        private async Task BackupRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.BackupRequest = _Serializer.DeserializeJson<BackupRequest>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.BackupExecute);
        }

        private async Task BackupReadAllRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            await WrappedRequestHandler(ctx, req, _ServiceHandler.BackupReadAll);
        }

        private async Task BackupReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            await WrappedRequestHandler(ctx, req, _ServiceHandler.BackupRead);
        }

        private async Task BackupExistsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            await WrappedRequestHandler(ctx, req, _ServiceHandler.BackupExists);
        }

        private async Task BackupDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            await WrappedRequestHandler(ctx, req, _ServiceHandler.BackupDelete);
        }

        private async Task FlushRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            await WrappedRequestHandler(ctx, req, _ServiceHandler.FlushDatabase);
        }

        #endregion

        #region General

        private async Task ExceptionRoute(HttpContextBase ctx, Exception e)
        {
            if (_Settings.Debug.Exceptions) _Logging.Warn(_Header + "exception of type " + e.GetType() + ": " + e.ToString());

            if (e is InvalidOperationException)
            {
                ctx.Response.StatusCode = 409;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.Conflict, null, e.Message)));
            }
            else if (
                e is ArgumentNullException
                || e is ArgumentOutOfRangeException
                || e is ArgumentException
                || e is FormatException
                || e is JsonException)
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, e.Message)));
            }
            else
            {
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError, null, e.Message)));
                _Logging.Warn(_Header + "exception encountered for " + ctx.Request.Method.ToString() + " " + OperationalLogRedactor.RedactUrl(ctx.Request.Url.RawWithQuery) + Environment.NewLine + e.ToString());
            }
        }

        private async Task LoopbackRoute(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 200;
            await ctx.Response.Send();
        }

        private async Task RootRoute(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.HtmlContentType;
            await ctx.Response.Send(Constants.DefaultHomepage);
        }

        private async Task MetricsRoute(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = "text/plain; version=0.0.4";
            await ctx.Response.Send(_Observability.RenderPrometheus());
        }

        private async Task FaviconRoute(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.FaviconContentType;
            await ctx.Response.Send(File.ReadAllBytes(Constants.FaviconFile));
        }

        private async Task NoRequestBody(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 400;
            await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest)));
        }

        private async Task NotAdmin(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 401;
            await SendAuthorizationFailed(ctx, ctx.Metadata as RequestContext, "AdminRequired", "admin", "Administrator authentication is required for this request.").ConfigureAwait(false);
        }

        private async Task SendAuthorizationFailed(
            HttpContextBase ctx,
            RequestContext req,
            string reason = null,
            string requiredScope = null,
            string description = null)
        {
            ApiErrorResponse response = AuthorizationFailedResponse(req, reason, requiredScope, description);
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            await AuditAuthorizationDenied(req, response, ctx.Response.StatusCode, timeoutCts.Token).ConfigureAwait(false);
            await ctx.Response.Send(_Serializer.SerializeJson(response)).ConfigureAwait(false);
        }

        private static ApiErrorResponse AuthorizationFailedResponse(
            RequestContext req,
            string reason = null,
            string requiredScope = null,
            string description = null)
        {
            reason ??= req?.Authorization?.Reason;
            requiredScope ??= req?.Authorization?.RequiredScope;

            Dictionary<string, string> context = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!String.IsNullOrEmpty(reason)) context["reason"] = reason;
            if (!String.IsNullOrEmpty(requiredScope)) context["requiredScope"] = requiredScope;
            if (req != null)
            {
                context["requestType"] = req.RequestType.ToString();
                if (req.TenantGUID != null) context["tenantGuid"] = req.TenantGUID.Value.ToString();
                if (req.GraphGUID != null) context["graphGuid"] = req.GraphGUID.Value.ToString();
            }

            return new ApiErrorResponse(
                ApiErrorEnum.AuthorizationFailed,
                context.Count > 0 ? context : null,
                description ?? "Authorization denied. Inspect the response context for the request type, reason, and required scope when available.");
        }

        private async Task AuditAuthorizationDenied(RequestContext req, ApiErrorResponse response, int statusCode, CancellationToken token)
        {
            if (req == null || response == null || _LiteGraph?.AuthorizationAudit == null) return;

            try
            {
                string reason = null;
                string requiredScope = null;
                if (response.Context is Dictionary<string, string> context)
                {
                    if (context.TryGetValue("reason", out string contextReason)) reason = contextReason;
                    if (context.TryGetValue("requiredScope", out string contextScope)) requiredScope = contextScope;
                }
                if (String.IsNullOrEmpty(reason)) reason = req.Authorization?.Reason;
                if (String.IsNullOrEmpty(requiredScope)) requiredScope = req.Authorization?.RequiredScope;

                AuthorizationAuditEntry entry = new AuthorizationAuditEntry
                {
                    RequestId = req.RequestId,
                    CorrelationId = req.CorrelationId,
                    TraceId = req.TraceId,
                    TenantGUID = req.TenantGUID ?? req.Authentication?.TenantGUID,
                    GraphGUID = req.GraphGUID,
                    UserGUID = req.Authentication?.UserGUID,
                    CredentialGUID = req.Authentication?.CredentialGUID,
                    RequestType = req.RequestType.ToString(),
                    Method = req.Http?.Request?.Method.ToString(),
                    Path = OperationalLogRedactor.RedactUrl(req.Http?.Request?.Url?.RawWithoutQuery),
                    SourceIp = req.Http?.Request?.Source?.IpAddress,
                    AuthenticationResult = req.Authentication?.Result.ToString(),
                    AuthorizationResult = AuthorizationResultEnum.Denied.ToString(),
                    Reason = reason,
                    RequiredScope = requiredScope,
                    IsAdmin = req.Authentication?.IsAdmin ?? false,
                    StatusCode = statusCode,
                    Description = response.Description
                };

                await _LiteGraph.AuthorizationAudit.Insert(entry, token).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "failed to write authorization audit entry:" + Environment.NewLine + e.ToString());
            }
        }

        #endregion

        #region Tenant-Routes

        private async Task TenantCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Tenant = _Serializer.DeserializeJson<TenantMetadata>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TenantCreate);
        }

        private async Task TenantReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TenantReadMany);
        }

        private async Task TenantEnumerateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            if (req.Data != null) req.EnumerationQuery = _Serializer.DeserializeJson<EnumerationRequest>(Encoding.UTF8.GetString(req.Data));
            else req.EnumerationQuery = BuildEnumerationQuery(req);

            await WrappedRequestHandler(ctx, req, _ServiceHandler.TenantEnumerate);
        }

        private async Task TenantReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TenantRead);
        }

        private async Task TenantStatisticsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin && req.TenantGUID == null)
            {
                await NotAdmin(ctx);
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TenantStatistics);
        }

        private async Task TenantExistsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TenantExists);
        }

        private async Task TenantUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Tenant = _Serializer.DeserializeJson<TenantMetadata>(ctx.Request.DataAsString);
            req.Tenant.GUID = req.TenantGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TenantUpdate);
        }

        private async Task TenantDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TenantDelete);
        }

        #endregion

        #region User-Routes

        private async Task UserCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.User = _Serializer.DeserializeJson<UserMaster>(ctx.Request.DataAsString);
            req.User.TenantGUID = req.TenantGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.UserCreate);
        }

        private async Task UserReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.UserReadMany);
        }

        private async Task UserEnumerateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            if (req.Data != null) req.EnumerationQuery = _Serializer.DeserializeJson<EnumerationRequest>(Encoding.UTF8.GetString(req.Data));
            else req.EnumerationQuery = BuildEnumerationQuery(req);

            await WrappedRequestHandler(ctx, req, _ServiceHandler.UserEnumerate);
        }

        private async Task UserReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.UserRead);
        }

        private async Task UserExistsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.UserExists);
        }

        private async Task UserUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.User = _Serializer.DeserializeJson<UserMaster>(ctx.Request.DataAsString);
            req.User.TenantGUID = req.TenantGUID.Value;
            req.User.GUID = req.UserGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.UserUpdate);
        }

        private async Task UserDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.UserDelete);
        }

        #endregion

        #region Credential-Routes

        private async Task CredentialCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Credential = _Serializer.DeserializeJson<Credential>(ctx.Request.DataAsString);
            req.Credential.TenantGUID = req.TenantGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.CredentialCreate);
        }

        private async Task CredentialReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.CredentialReadMany);
        }

        private async Task CredentialEnumerateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            if (req.Data != null) req.EnumerationQuery = _Serializer.DeserializeJson<EnumerationRequest>(Encoding.UTF8.GetString(req.Data));
            else req.EnumerationQuery = BuildEnumerationQuery(req);

            await WrappedRequestHandler(ctx, req, _ServiceHandler.CredentialEnumerate);
        }

        private async Task CredentialReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.CredentialRead);
        }

        private async Task CredentialExistsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.CredentialExists);
        }

        private async Task CredentialUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Credential = _Serializer.DeserializeJson<Credential>(ctx.Request.DataAsString);
            req.Credential.TenantGUID = req.TenantGUID.Value;
            req.Credential.GUID = req.CredentialGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.CredentialUpdate);
        }

        private async Task CredentialDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.CredentialDelete);
        }

        private async Task CredentialReadByBearerTokenRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.CredentialReadByBearerToken);
        }

        private async Task CredentialDeleteAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.CredentialDeleteAllInTenant);
        }

        private async Task CredentialDeleteByUserRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin)
            {
                await NotAdmin(ctx);
                return;
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.CredentialDeleteByUser);
        }

        #endregion

        #region Label-Routes

        private async Task LabelCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Label = _Serializer.DeserializeJson<LabelMetadata>(ctx.Request.DataAsString);
            req.Label.TenantGUID = req.TenantGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelCreate);
        }

        private async Task LabelCreateManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Labels = _Serializer.DeserializeJson<List<LabelMetadata>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelCreateMany);
        }

        private async Task LabelReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelReadMany);
        }

        private async Task LabelEnumerateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (req.Data != null) req.EnumerationQuery = _Serializer.DeserializeJson<EnumerationRequest>(Encoding.UTF8.GetString(req.Data));
            else req.EnumerationQuery = BuildEnumerationQuery(req);
            if (req.EnumerationQuery?.GraphGUID != null) req.GraphGUID = req.EnumerationQuery.GraphGUID;
            if (req.GraphGUID.HasValue) req.EnumerationQuery.GraphGUID = req.GraphGUID;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelEnumerate);
        }

        private async Task LabelReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelRead);
        }

        private async Task LabelExistsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelExists);
        }

        private async Task LabelUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Label = _Serializer.DeserializeJson<LabelMetadata>(ctx.Request.DataAsString);
            req.Label.TenantGUID = req.TenantGUID.Value;
            req.Label.GUID = req.LabelGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelUpdate);
        }

        private async Task LabelDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelDelete);
        }

        private async Task LabelDeleteManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.GUIDs = _Serializer.DeserializeJson<List<Guid>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelDeleteMany);
        }

        private async Task LabelReadAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelReadAllInTenant);
        }

        private async Task LabelReadAllInGraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelReadAllInGraph);
        }

        private async Task LabelReadManyGraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelReadManyGraph);
        }

        private async Task LabelReadManyNodeRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelReadManyNode);
        }

        private async Task LabelReadManyEdgeRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelReadManyEdge);
        }

        private async Task LabelDeleteAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelDeleteAllInTenant);
        }

        private async Task LabelDeleteAllInGraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelDeleteAllInGraph);
        }

        private async Task LabelDeleteGraphLabelsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelDeleteGraphLabels);
        }

        private async Task LabelDeleteNodeLabelsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelDeleteNodeLabels);
        }

        private async Task LabelDeleteEdgeLabelsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelDeleteEdgeLabels);
        }

        #endregion

        #region Tag-Routes

        private async Task TagCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Tag = _Serializer.DeserializeJson<TagMetadata>(ctx.Request.DataAsString);
            req.Tag.TenantGUID = req.TenantGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagCreate);
        }

        private async Task TagCreateManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Tags = _Serializer.DeserializeJson<List<TagMetadata>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagCreateMany);
        }

        private async Task TagReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagReadMany);
        }

        private async Task TagEnumerateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (req.Data != null) req.EnumerationQuery = _Serializer.DeserializeJson<EnumerationRequest>(Encoding.UTF8.GetString(req.Data));
            else req.EnumerationQuery = BuildEnumerationQuery(req);
            if (req.EnumerationQuery?.GraphGUID != null) req.GraphGUID = req.EnumerationQuery.GraphGUID;
            if (req.GraphGUID.HasValue) req.EnumerationQuery.GraphGUID = req.GraphGUID;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagEnumerate);
        }

        private async Task TagReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagRead);
        }

        private async Task TagExistsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagExists);
        }

        private async Task TagUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Tag = _Serializer.DeserializeJson<TagMetadata>(ctx.Request.DataAsString);
            req.Tag.TenantGUID = req.TenantGUID.Value;
            req.Tag.GUID = req.TagGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagUpdate);
        }

        private async Task TagDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagDelete);
        }

        private async Task TagDeleteManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.GUIDs = _Serializer.DeserializeJson<List<Guid>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagDeleteMany);
        }

        private async Task TagReadAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagReadAllInTenant);
        }

        private async Task TagReadAllInGraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagReadAllInGraph);
        }

        private async Task TagReadManyGraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagReadManyGraph);
        }

        private async Task TagReadManyNodeRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagReadManyNode);
        }

        private async Task TagReadManyEdgeRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagReadManyEdge);
        }

        private async Task TagDeleteAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagDeleteAllInTenant);
        }

        private async Task TagDeleteAllInGraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagDeleteAllInGraph);
        }

        private async Task TagDeleteGraphTagsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagDeleteGraphTags);
        }

        private async Task TagDeleteNodeTagsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagDeleteNodeTags);
        }

        private async Task TagDeleteEdgeTagsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagDeleteEdgeTags);
        }

        #endregion

        #region Vector-Routes

        private async Task VectorCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Vector = _Serializer.DeserializeJson<VectorMetadata>(ctx.Request.DataAsString);
            req.Vector.TenantGUID = req.TenantGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorCreate);
        }

        private async Task VectorCreateManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Vectors = _Serializer.DeserializeJson<List<VectorMetadata>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorCreateMany);
        }

        private async Task VectorReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorReadMany);
        }

        private async Task VectorEnumerateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (req.Data != null) req.EnumerationQuery = _Serializer.DeserializeJson<EnumerationRequest>(Encoding.UTF8.GetString(req.Data));
            else req.EnumerationQuery = BuildEnumerationQuery(req);
            if (req.EnumerationQuery?.GraphGUID != null) req.GraphGUID = req.EnumerationQuery.GraphGUID;
            if (req.GraphGUID.HasValue) req.EnumerationQuery.GraphGUID = req.GraphGUID;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorEnumerate);
        }

        private async Task VectorReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorRead);
        }

        private async Task VectorExistsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorExists);
        }

        private async Task VectorUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Vector = _Serializer.DeserializeJson<VectorMetadata>(ctx.Request.DataAsString);
            req.Vector.TenantGUID = req.TenantGUID.Value;
            req.Vector.GUID = req.VectorGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorUpdate);
        }

        private async Task VectorDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorDelete);
        }

        private async Task VectorDeleteManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.GUIDs = _Serializer.DeserializeJson<List<Guid>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorDeleteMany);
        }

        private async Task VectorReadAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorReadAllInTenant);
        }

        private async Task VectorReadAllInGraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorReadAllInGraph);
        }

        private async Task VectorReadManyGraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorReadManyGraph);
        }

        private async Task VectorReadManyNodeRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorReadManyNode);
        }

        private async Task VectorReadManyEdgeRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorReadManyEdge);
        }

        private async Task VectorDeleteAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorDeleteAllInTenant);
        }

        private async Task VectorDeleteAllInGraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorDeleteAllInGraph);
        }

        private async Task VectorDeleteGraphVectorsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorDeleteGraphVectors);
        }

        private async Task VectorDeleteNodeVectorsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorDeleteNodeVectors);
        }

        private async Task VectorDeleteEdgeVectorsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorDeleteEdgeVectors);
        }

        private async Task VectorSearchRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.VectorSearchRequest = _Serializer.DeserializeJson<VectorSearchRequest>(ctx.Request.DataAsString);
            req.VectorSearchRequest.TenantGUID = req.TenantGUID.Value;
            if (req.GraphGUID.HasValue) req.VectorSearchRequest.GraphGUID = req.GraphGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorSearch);
        }

        #endregion

        #region Graph-Routes

        private async Task GraphCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Graph = _Serializer.DeserializeJson<Graph>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphCreate);
        }

        private async Task GraphReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphReadMany);
        }

        private async Task GraphEnumerateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (req.Data != null) req.EnumerationQuery = _Serializer.DeserializeJson<EnumerationRequest>(Encoding.UTF8.GetString(req.Data));
            else req.EnumerationQuery = BuildEnumerationQuery(req);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphEnumerate);
        }

        private async Task GraphExistenceRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.ExistenceRequest = _Serializer.DeserializeJson<ExistenceRequest>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphExistence);
        }

        private async Task GraphQueryRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            GraphQueryRequest query = _Serializer.DeserializeJson<GraphQueryRequest>(ctx.Request.DataAsString);
            DateTime start = DateTime.UtcNow;
            string requiredScope = GraphQueryRequiredScope(query);
            Stopwatch authorizationStopwatch = new Stopwatch();
            using (Activity activity = StartInternalActivity("litegraph.graph.query", req))
            using (CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource())
            {
                activity?.SetTag("litegraph.query.required_scope", requiredScope);

                AuthorizationDecision queryAuthorization;
                try
                {
                    authorizationStopwatch.Start();
                    queryAuthorization = await _Authentication.AuthorizeRequestScope(
                        req,
                        requiredScope,
                        AuthorizationResourceTypeEnum.Query,
                        timeoutCts.Token).ConfigureAwait(false);
                    authorizationStopwatch.Stop();
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                {
                    authorizationStopwatch.Stop();
                    _Observability.RecordGraphQuery(String.Equals(requiredScope, "write", StringComparison.OrdinalIgnoreCase), false, (DateTime.UtcNow - start).TotalMilliseconds);
                    ctx.Response.StatusCode = 408;
                    RecordActivityException(activity, new TimeoutException("Graph query authorization timed out."), ctx.Response.StatusCode);
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.RequestTimeout)));
                    return;
                }

                if (queryAuthorization.Result != AuthorizationResultEnum.Permitted)
                {
                    bool writeDenied = String.Equals(requiredScope, "write", StringComparison.OrdinalIgnoreCase);
                    double deniedDurationMs = (DateTime.UtcNow - start).TotalMilliseconds;
                    _Observability.RecordGraphQuery(writeDenied, false, deniedDurationMs);
                    activity?.SetTag("litegraph.query.success", false);
                    activity?.SetTag("litegraph.query.mutated", writeDenied);
                    activity?.SetTag("litegraph.query.denied", true);
                    ctx.Response.StatusCode = 401;
                    await SendAuthorizationFailed(
                        ctx,
                        req,
                        queryAuthorization.Reason.ToString(),
                        queryAuthorization.RequiredScope,
                        "The authenticated principal is not authorized to execute this graph query.").ConfigureAwait(false);
                    return;
                }

                GraphQueryResult result;
                try
                {
                    result = await _LiteGraph.Query.Execute(req.TenantGUID.Value, req.GraphGUID.Value, query, timeoutCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                {
                    _Observability.RecordGraphQuery(String.Equals(requiredScope, "write", StringComparison.OrdinalIgnoreCase), false, (DateTime.UtcNow - start).TotalMilliseconds);
                    ctx.Response.StatusCode = 408;
                    RecordActivityException(activity, new TimeoutException("Graph query timed out."), ctx.Response.StatusCode);
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.RequestTimeout)));
                    return;
                }

                double durationMs = (DateTime.UtcNow - start).TotalMilliseconds;
                if (result.ExecutionProfile != null) result.ExecutionProfile.AuthorizationTimeMs = authorizationStopwatch.Elapsed.TotalMilliseconds;
                _Observability.RecordGraphQuery(result.Mutated, true, durationMs);
                activity?.SetTag("litegraph.query.success", true);
                activity?.SetTag("litegraph.query.mutated", result.Mutated);
                activity?.SetTag("litegraph.query.row_count", result.RowCount);
                if (result.Plan != null)
                {
                    activity?.SetTag("litegraph.query.kind", result.Plan.Kind.ToString());
                    activity?.SetTag("litegraph.query.uses_vector_search", result.Plan.UsesVectorSearch);
                }

                if (result.Plan != null && result.Plan.UsesVectorSearch)
                {
                    string domain = result.Plan.VectorDomain.HasValue ? result.Plan.VectorDomain.Value.ToString() : "unknown";
                    int resultCount = result.VectorSearchResults?.Count ?? result.RowCount;
                    _Observability.RecordVectorSearch(domain, true, resultCount, durationMs);
                    activity?.SetTag("litegraph.vector.domain", domain);
                    activity?.SetTag("litegraph.vector.result_count", resultCount);
                }

                ctx.Response.StatusCode = 200;
                string responseBody;
                if (result.ExecutionProfile != null)
                {
                    Stopwatch serializationStopwatch = Stopwatch.StartNew();
                    responseBody = _Serializer.SerializeJson(result);
                    serializationStopwatch.Stop();
                    result.ExecutionProfile.SerializationTimeMs = serializationStopwatch.Elapsed.TotalMilliseconds;
                    responseBody = _Serializer.SerializeJson(result);
                }
                else
                {
                    responseBody = _Serializer.SerializeJson(result);
                }

                await ctx.Response.Send(responseBody);
            }
        }

        private async Task GraphTransactionRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            TransactionRequest transaction = _Serializer.DeserializeJson<TransactionRequest>(ctx.Request.DataAsString);
            ApplyTransactionServerLimits(transaction, _Settings?.LiteGraph?.Transactions);
            DateTime start = DateTime.UtcNow;
            using (Activity activity = StartInternalActivity("litegraph.graph.transaction", req))
            using (CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource())
            {
                int operationCount = transaction?.Operations?.Count ?? 0;
                activity?.SetTag("litegraph.transaction.operation_count", operationCount);

                TransactionResult result;
                try
                {
                    result = await _LiteGraph.Transaction.Execute(req.TenantGUID.Value, req.GraphGUID.Value, transaction, timeoutCts.Token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
                {
                    _Observability.RecordGraphTransaction(false, true, false, operationCount, (DateTime.UtcNow - start).TotalMilliseconds);
                    ctx.Response.StatusCode = 408;
                    RecordActivityException(activity, new TimeoutException("Graph transaction timed out."), ctx.Response.StatusCode);
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.RequestTimeout)));
                    return;
                }

                _Observability.RecordGraphTransaction(result.Success, result.RolledBack, result.ValidationFailure, operationCount, (DateTime.UtcNow - start).TotalMilliseconds);
                activity?.SetTag("litegraph.transaction.id", result.TransactionId);
                activity?.SetTag("litegraph.transaction.state", result.State);
                activity?.SetTag("litegraph.transaction.success", result.Success);
                activity?.SetTag("litegraph.transaction.rolled_back", result.RolledBack);
                activity?.SetTag("litegraph.transaction.provider", result.Provider);
                activity?.SetTag("litegraph.transaction.isolation_level", result.IsolationLevel);
                activity?.SetTag("litegraph.transaction.isolated_repository", result.IsolatedRepository);
                activity?.SetTag("litegraph.transaction.serialized_by_gate", result.SerializedByGate);
                activity?.SetTag("litegraph.transaction.retry_count", result.RetryCount);
                activity?.SetTag("litegraph.transaction.retryable", result.Retryable);
                activity?.SetTag("litegraph.transaction.concurrency_conflict", result.ConcurrencyConflict);
                activity?.SetTag("litegraph.transaction.validation_failure", result.ValidationFailure);
                if (!String.IsNullOrEmpty(result.ProviderErrorCode)) activity?.SetTag("litegraph.transaction.provider_error_code", result.ProviderErrorCode);
                if (result.FailedOperationIndex != null) activity?.SetTag("litegraph.transaction.failed_operation_index", result.FailedOperationIndex.Value);

                ctx.Response.StatusCode = GraphTransactionStatusCode(result);
                await ctx.Response.Send(_Serializer.SerializeJson(result));
            }
        }

        private static void ApplyTransactionServerLimits(TransactionRequest transaction, TransactionSettings settings)
        {
            if (transaction == null || settings == null) return;
            if (transaction.MaxOperations > settings.MaxOperations) transaction.MaxOperations = settings.MaxOperations;
            if (transaction.TimeoutSeconds > settings.MaxTimeoutSeconds) transaction.TimeoutSeconds = settings.MaxTimeoutSeconds;
        }

        private static int GraphTransactionStatusCode(TransactionResult result)
        {
            if (result == null || result.Success) return 200;
            if (result.ValidationFailure) return 400;
            if (result.RolledBack || result.Retryable || result.ConcurrencyConflict) return 409;
            return 500;
        }

        private async Task GraphSubgraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphSubgraph);
        }

        private async Task GraphSubgraphStatisticsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphSubgraphStatistics);
        }

        private async Task GraphReadFirstRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await GraphReadManyRoute(ctx);
                return;
            }

            req.SearchRequest = _Serializer.DeserializeJson<SearchRequest>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphReadFirst);
        }

        private async Task GraphSearchRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await GraphReadManyRoute(ctx);
                return;
            }

            req.SearchRequest = _Serializer.DeserializeJson<SearchRequest>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphSearch);
        }

        private async Task GraphReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphRead);
        }

        private async Task GraphStatisticsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!req.Authentication.IsAdmin && req.TenantGUID == null)
            {
                await NotAdmin(ctx);
            }
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphStatistics);
        }

        private async Task GraphExistsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphExists);
        }

        private async Task GraphUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Graph = _Serializer.DeserializeJson<Graph>(ctx.Request.DataAsString);
            req.Graph.TenantGUID = req.TenantGUID.Value;
            req.Graph.GUID = req.GraphGUID.Value;

            // Get current graph to preserve vector index properties
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            Graph currentGraph;
            try
            {
                currentGraph = await _LiteGraph.Graph.ReadByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token: timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException oce) when (timeoutCts.IsCancellationRequested)
            {
                await SendRequestTimeout(ctx, "graph update read", oce).ConfigureAwait(false);
                return;
            }

            if (currentGraph != null)
            {
                // Preserve existing vector index properties - these should only be changed through vector index APIs
                req.Graph.VectorIndexType = currentGraph.VectorIndexType;
                req.Graph.VectorIndexFile = currentGraph.VectorIndexFile;
                req.Graph.VectorIndexThreshold = currentGraph.VectorIndexThreshold;
                req.Graph.VectorDimensionality = currentGraph.VectorDimensionality;
                req.Graph.VectorIndexM = currentGraph.VectorIndexM;
                req.Graph.VectorIndexEf = currentGraph.VectorIndexEf;
                req.Graph.VectorIndexEfConstruction = currentGraph.VectorIndexEfConstruction;
                req.Graph.VectorIndexDirty = currentGraph.VectorIndexDirty;
                req.Graph.VectorIndexDirtyUtc = currentGraph.VectorIndexDirtyUtc;
                req.Graph.VectorIndexDirtyReason = currentGraph.VectorIndexDirtyReason;
            }

            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphUpdate);
        }

        private async Task GraphDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphDelete);
        }

        private async Task GraphReadAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphReadAllInTenant);
        }

        private async Task GraphDeleteAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphDeleteAllInTenant);
        }

        private async Task GraphGexfExportRoute(HttpContextBase ctx)
        {
            try
            {
                RequestContext req = (RequestContext)ctx.Metadata;
                using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
                ResponseContext resp = await _ServiceHandler.GraphGexfExport(req, timeoutCts.Token).ConfigureAwait(false);
                if (!resp.Success)
                {
                    ctx.Response.StatusCode = 500;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError)));
                }
                else
                {
                    ctx.Response.ContentType = Constants.XmlContentType;
                    ctx.Response.StatusCode = 200;
                    await ctx.Response.Send(resp.Data.ToString());
                }
            }
            catch (OperationCanceledException oce)
            {
                await SendRequestTimeout(ctx, "GEXF export", oce).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "GEXF export error:" + Environment.NewLine + e.ToString());
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = Constants.JsonContentType;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError, null, e.Message)));
            }
        }

        private async Task GraphEnableVectorIndexRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();

            try
            {
                // Parse request body for index configuration
                string requestData = ctx.Request.DataAsString;
                VectorIndexConfiguration config;

                if (string.IsNullOrWhiteSpace(requestData))
                {
                    // Use default configuration if no body provided
                    config = new VectorIndexConfiguration();
                }
                else
                {
                    config = _Serializer.DeserializeJson<VectorIndexConfiguration>(requestData);
                    if (config == null)
                    {
                        ctx.Response.StatusCode = 400;
                        ctx.Response.ContentType = Constants.JsonContentType;
                        await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, "Invalid configuration format")));
                        return;
                    }
                }

                // Validate configuration
                if (!config.IsValid(out string errorMessage))
                {
                    ctx.Response.StatusCode = 400;
                    ctx.Response.ContentType = Constants.JsonContentType;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, errorMessage)));
                    return;
                }

                await _LiteGraph.Graph.EnableVectorIndexing(
                    req.TenantGUID.Value,
                    req.GraphGUID.Value,
                    config,
                    timeoutCts.Token).ConfigureAwait(false);

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = Constants.JsonContentType;
                await ctx.Response.Send(_Serializer.SerializeJson(config));
            }
            catch (OperationCanceledException oce) when (timeoutCts.IsCancellationRequested)
            {
                await SendRequestTimeout(ctx, "vector index enable", oce).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = Constants.JsonContentType;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError, null, e.Message)));
            }
        }

        private async Task GraphDisableVectorIndexRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();

            bool deleteFile = false;
            if (!string.IsNullOrEmpty(req.Query["deleteFile"]))
            {
                bool.TryParse(req.Query["deleteFile"], out deleteFile);
            }

            try
            {
                await _LiteGraph.Graph.DisableVectorIndexing(
                    req.TenantGUID.Value,
                    req.GraphGUID.Value,
                    deleteFile,
                    timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException oce) when (timeoutCts.IsCancellationRequested)
            {
                await SendRequestTimeout(ctx, "vector index disable", oce).ConfigureAwait(false);
                return;
            }

            ctx.Response.StatusCode = 200;
            await ctx.Response.Send();
        }

        private async Task GraphRebuildVectorIndexRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();

            try
            {
                await _LiteGraph.Graph.RebuildVectorIndex(
                    req.TenantGUID.Value,
                    req.GraphGUID.Value,
                    timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException oce) when (timeoutCts.IsCancellationRequested)
            {
                await SendRequestTimeout(ctx, "vector index rebuild", oce).ConfigureAwait(false);
                return;
            }

            ctx.Response.StatusCode = 200;
            await ctx.Response.Send();
        }

        private async Task GraphVectorIndexStatsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();

            VectorIndexStatistics stats;
            try
            {
                stats = await _LiteGraph.Graph.GetVectorIndexStatistics(
                    req.TenantGUID.Value,
                    req.GraphGUID.Value,
                    timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException oce) when (timeoutCts.IsCancellationRequested)
            {
                await SendRequestTimeout(ctx, "vector index statistics", oce).ConfigureAwait(false);
                return;
            }

            if (stats == null)
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound, null, "The specified graph has no configured vector index.")));
                return;
            }

            ctx.Response.StatusCode = 200;
            await ctx.Response.Send(_Serializer.SerializeJson(stats));
        }

        private async Task GraphGetVectorIndexConfigRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();

            try
            {
                Graph graph = await _LiteGraph.Graph.ReadByGuid(req.TenantGUID.Value, req.GraphGUID.Value, token: timeoutCts.Token).ConfigureAwait(false);
                if (graph == null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = Constants.JsonContentType;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound, null, "Graph not found")));
                    return;
                }

                // Check if vector indexing is enabled
                if (!graph.VectorIndexType.HasValue || graph.VectorIndexType == VectorIndexTypeEnum.None)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.ContentType = Constants.JsonContentType;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound, null, "The specified graph has no configured vector index.")));
                    return;
                }

                VectorIndexConfiguration config = new VectorIndexConfiguration(graph);

                ctx.Response.StatusCode = 200;
                ctx.Response.ContentType = Constants.JsonContentType;
                await ctx.Response.Send(_Serializer.SerializeJson(config));
            }
            catch (OperationCanceledException oce) when (timeoutCts.IsCancellationRequested)
            {
                await SendRequestTimeout(ctx, "vector index config read", oce).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = Constants.JsonContentType;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError, null, e.Message)));
            }
        }


        #endregion

        #region Node-Routes

        private async Task NodeCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Node = _Serializer.DeserializeJson<Node>(ctx.Request.DataAsString);
            req.Node.TenantGUID = req.TenantGUID.Value;
            req.Node.GraphGUID = req.GraphGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeCreate);
        }

        private async Task NodeCreateManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Nodes = _Serializer.DeserializeJson<List<Node>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeCreateMany);
        }

        private async Task NodeReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeReadMany);
        }

        private async Task NodeEnumerateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (req.Data != null) req.EnumerationQuery = _Serializer.DeserializeJson<EnumerationRequest>(Encoding.UTF8.GetString(req.Data));
            else req.EnumerationQuery = BuildEnumerationQuery(req);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeEnumerate);
        }

        private async Task NodeSearchRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NodeReadManyRoute(ctx);
                return;
            }

            req.SearchRequest = _Serializer.DeserializeJson<SearchRequest>(ctx.Request.DataAsString);
            req.SearchRequest.TenantGUID = req.TenantGUID.Value;
            req.SearchRequest.GraphGUID = req.GraphGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeSearch);
        }

        private async Task NodeReadFirstRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NodeReadManyRoute(ctx);
                return;
            }

            req.SearchRequest = _Serializer.DeserializeJson<SearchRequest>(ctx.Request.DataAsString);
            req.SearchRequest.TenantGUID = req.TenantGUID.Value;
            req.SearchRequest.GraphGUID = req.GraphGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeReadFirst);
        }

        private async Task NodeReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeRead);
        }

        private async Task NodeExistsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeExists);
        }

        private async Task NodeUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Node = _Serializer.DeserializeJson<Node>(ctx.Request.DataAsString);
            req.Node.TenantGUID = req.TenantGUID.Value;
            req.Node.GraphGUID = req.GraphGUID.Value;
            req.Node.GUID = req.NodeGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeUpdate);
        }

        private async Task NodeDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeDelete);
        }

        private async Task NodeDeleteAllRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeDeleteAll);
        }

        private async Task NodeDeleteManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.GUIDs = _Serializer.DeserializeJson<List<Guid>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeDeleteMany);
        }

        private async Task NodeReadAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeReadAllInTenant);
        }

        private async Task NodeReadAllInGraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeReadAllInGraph);
        }

        private async Task NodeReadMostConnectedRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeReadMostConnected);
        }

        private async Task NodeReadLeastConnectedRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeReadLeastConnected);
        }

        private async Task NodeDeleteAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeDeleteAllInTenant);
        }

        #endregion

        #region Edge-Routes

        private async Task EdgeCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Edge = _Serializer.DeserializeJson<Edge>(ctx.Request.DataAsString);
            req.Edge.TenantGUID = req.TenantGUID.Value;
            req.Edge.GraphGUID = req.GraphGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeCreate);
        }

        private async Task EdgeCreateManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Edges = _Serializer.DeserializeJson<List<Edge>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeCreateMany);
        }

        private async Task EdgeReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeReadMany);
        }

        private async Task EdgeEnumerateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (req.Data != null) req.EnumerationQuery = _Serializer.DeserializeJson<EnumerationRequest>(Encoding.UTF8.GetString(req.Data));
            else req.EnumerationQuery = BuildEnumerationQuery(req);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeEnumerate);
        }

        private async Task EdgesBetweenRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgesBetween);
        }

        private async Task EdgeSearchRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await EdgeReadManyRoute(ctx);
                return;
            }

            req.SearchRequest = _Serializer.DeserializeJson<SearchRequest>(ctx.Request.DataAsString);
            req.SearchRequest.TenantGUID = req.TenantGUID.Value;
            req.SearchRequest.GraphGUID = req.GraphGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeSearch);
        }

        private async Task EdgeReadFirstRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await EdgeReadManyRoute(ctx);
                return;
            }

            req.SearchRequest = _Serializer.DeserializeJson<SearchRequest>(ctx.Request.DataAsString);
            req.SearchRequest.TenantGUID = req.TenantGUID.Value;
            req.SearchRequest.GraphGUID = req.GraphGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeReadFirst);
        }

        private async Task EdgeReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeRead);
        }

        private async Task EdgeExistsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeExists);
        }

        private async Task EdgeUpdateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Edge = _Serializer.DeserializeJson<Edge>(ctx.Request.DataAsString);
            req.Edge.TenantGUID = req.TenantGUID.Value;
            req.Edge.GraphGUID = req.GraphGUID.Value;
            req.Edge.GUID = req.EdgeGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeUpdate);
        }

        private async Task EdgeDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeDelete);
        }

        private async Task EdgeDeleteAllRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeDeleteAll);
        }

        private async Task EdgeDeleteManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.GUIDs = _Serializer.DeserializeJson<List<Guid>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeDeleteMany);
        }

        private async Task EdgeReadAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeReadAllInTenant);
        }

        private async Task EdgeReadAllInGraphRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeReadAllInGraph);
        }

        private async Task EdgeDeleteAllInTenantRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeDeleteAllInTenant);
        }

        private async Task EdgeDeleteNodeEdgesRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeDeleteNodeEdges);
        }

        private async Task EdgeDeleteNodeEdgesManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.GUIDs = _Serializer.DeserializeJson<List<Guid>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeDeleteNodeEdgesMany);
        }

        #endregion

        #region Routes-and-Traversal

        private async Task EdgesFromNodeRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgesFromNode);
        }

        private async Task EdgesToNodeRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgesToNode);
        }

        private async Task AllEdgesToNodeRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (!String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                req.SearchRequest = _Serializer.DeserializeJson<SearchRequest>(ctx.Request.DataAsString);
                req.SearchRequest.TenantGUID = req.TenantGUID.Value;
                req.SearchRequest.GraphGUID = req.GraphGUID.Value;
            }

            await WrappedRequestHandler(ctx, req, _ServiceHandler.AllEdgesToNode);
        }

        private async Task NodeChildrenRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeChildren);
        }

        private async Task NodeParentsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeParents);
        }

        private async Task NodeNeighborsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeNeighbors);
        }

        private async Task GetRoutesRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.RouteRequest = _Serializer.DeserializeJson<RouteRequest>(ctx.Request.DataAsString);
            req.RouteRequest.TenantGUID = req.TenantGUID.Value;
            req.RouteRequest.GraphGUID = req.GraphGUID.Value;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GetRoutes);
        }

        #endregion

        #endregion

        #region Private-Methods

        private async Task WrappedRequestHandler(HttpContextBase ctx, RequestContext req, Func<RequestContext, CancellationToken, Task<ResponseContext>> func)
        {
            using (Activity activity = StartInternalActivity("litegraph.rest.handler", req))
            {
                await WrappedRequestHandlerInternal(ctx, req, func, activity).ConfigureAwait(false);
            }
        }

        private async Task WrappedRequestHandlerInternal(HttpContextBase ctx, RequestContext req, Func<RequestContext, CancellationToken, Task<ResponseContext>> func, Activity activity)
        {
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();

            try
            {
                ResponseContext resp = await func(req, timeoutCts.Token).ConfigureAwait(false);
                if (resp == null)
                {
                    _Logging.Warn(_Header + "no response from agnostic handler");
                    ctx.Response.StatusCode = 500;
                    activity?.SetTag("litegraph.handler.success", false);
                    activity?.SetTag("http.response.status_code", ctx.Response.StatusCode);
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError)));
                    return;
                }
                else if (resp.Success)
                {
                    ctx.Response.StatusCode = resp.StatusCode;
                    activity?.SetTag("litegraph.handler.success", true);
                    activity?.SetTag("http.response.status_code", ctx.Response.StatusCode);
                    RecordEntityCountsFromResponse(resp.Data);
                    if (resp.Data != null) await ctx.Response.Send(_Serializer.SerializeJson(resp.Data));
                    else await ctx.Response.Send();
                    return;
                }
                else
                {
                    if (resp.Error != null) ctx.Response.StatusCode = resp.Error.StatusCode;
                    else ctx.Response.StatusCode = 418;
                    activity?.SetTag("litegraph.handler.success", false);
                    activity?.SetTag("http.response.status_code", ctx.Response.StatusCode);
                    if (resp.Error != null && ctx.Request.Method != HttpMethod.HEAD) await ctx.Response.Send(_Serializer.SerializeJson(resp.Error));
                    else await ctx.Response.Send();
                    return;
                }
            }
            catch (OperationCanceledException oce) when (timeoutCts.IsCancellationRequested)
            {
                _Logging.Warn(_Header + "request timed out after " + _Settings.RequestTimeoutSeconds + " seconds");
                ctx.Response.StatusCode = 408;
                RecordActivityException(activity, oce, ctx.Response.StatusCode);
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.RequestTimeout)));
                return;
            }
            catch (JsonException je)
            {
                _Logging.Warn(_Header + "JSON exception: " + Environment.NewLine + je.ToString());
                ctx.Response.StatusCode = 400;
                RecordActivityException(activity, je, ctx.Response.StatusCode);
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.DeserializationError, null, je.Message)));
            }
            catch (FormatException fe)
            {
                _Logging.Warn(_Header + "format exception: " + Environment.NewLine + fe.ToString());
                ctx.Response.StatusCode = 400;
                RecordActivityException(activity, fe, ctx.Response.StatusCode);
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, fe.Message)));
            }
            catch (InvalidOperationException ioe)
            {
                _Logging.Warn(_Header + "invalid operation exception: " + Environment.NewLine + ioe.ToString());
                ctx.Response.StatusCode = 409;
                RecordActivityException(activity, ioe, ctx.Response.StatusCode);
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.Conflict, null, ioe.Message)));
            }
            catch (FileNotFoundException fnfe)
            {
                _Logging.Warn(_Header + "file not found exception: " + Environment.NewLine + fnfe.ToString());
                ctx.Response.StatusCode = 404;
                RecordActivityException(activity, fnfe, ctx.Response.StatusCode);
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound, null, fnfe.Message)));
                return;
            }
            catch (KeyNotFoundException knfe)
            {
                _Logging.Warn(_Header + "invalid operation exception: " + Environment.NewLine + knfe.ToString());
                ctx.Response.StatusCode = 404;
                RecordActivityException(activity, knfe, ctx.Response.StatusCode);
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound, null, knfe.Message)));
                return;
            }
            catch (ArgumentException ae)
            {
                _Logging.Warn(_Header + "argument exception: " + Environment.NewLine + ae.ToString());
                ctx.Response.StatusCode = 400;
                RecordActivityException(activity, ae, ctx.Response.StatusCode);
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, ae.Message)));
                return;
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception: " + Environment.NewLine + e.ToString());
                ctx.Response.StatusCode = 400;
                RecordActivityException(activity, e, ctx.Response.StatusCode);
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, e.Message)));
                return;
            }
        }

        private CancellationTokenSource CreateRequestTimeoutTokenSource()
        {
            int timeoutSeconds = _Settings?.RequestTimeoutSeconds ?? 60;
            if (timeoutSeconds < 1) timeoutSeconds = 60;
            return new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        }

        private async Task SendRequestTimeout(HttpContextBase ctx, string operation, OperationCanceledException exception, Activity activity = null)
        {
            _Logging.Warn(_Header + operation + " timed out after " + _Settings.RequestTimeoutSeconds + " seconds");
            ctx.Response.StatusCode = 408;
            RecordActivityException(activity, exception, ctx.Response.StatusCode);
            await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.RequestTimeout)));
        }

        private void RecordEntityCountsFromResponse(object data)
        {
            if (data is TenantStatistics tenantStats)
            {
                RecordTenantEntityCounts("tenant", tenantStats);
            }
            else if (data is Dictionary<Guid, TenantStatistics> tenantStatsByTenant)
            {
                RecordTenantEntityCounts("all_tenants", AggregateTenantStatistics(tenantStatsByTenant.Values));
            }
            else if (data is GraphStatistics graphStats)
            {
                RecordGraphEntityCounts("graph", graphStats);
            }
            else if (data is Dictionary<Guid, GraphStatistics> graphStatsByGraph)
            {
                RecordGraphEntityCounts("tenant_graphs", AggregateGraphStatistics(graphStatsByGraph.Values));
            }
        }

        private void RecordTenantEntityCounts(string scope, TenantStatistics stats)
        {
            if (stats == null) return;
            _Observability.RecordEntityCount(scope, "graphs", stats.Graphs);
            _Observability.RecordEntityCount(scope, "nodes", stats.Nodes);
            _Observability.RecordEntityCount(scope, "edges", stats.Edges);
            _Observability.RecordEntityCount(scope, "labels", stats.Labels);
            _Observability.RecordEntityCount(scope, "tags", stats.Tags);
            _Observability.RecordEntityCount(scope, "vectors", stats.Vectors);
        }

        private void RecordGraphEntityCounts(string scope, GraphStatistics stats)
        {
            if (stats == null) return;
            _Observability.RecordEntityCount(scope, "nodes", stats.Nodes);
            _Observability.RecordEntityCount(scope, "edges", stats.Edges);
            _Observability.RecordEntityCount(scope, "labels", stats.Labels);
            _Observability.RecordEntityCount(scope, "tags", stats.Tags);
            _Observability.RecordEntityCount(scope, "vectors", stats.Vectors);
        }

        private static TenantStatistics AggregateTenantStatistics(IEnumerable<TenantStatistics> stats)
        {
            TenantStatistics ret = new TenantStatistics();
            if (stats == null) return ret;

            foreach (TenantStatistics current in stats)
            {
                if (current == null) continue;
                ret.Graphs += current.Graphs;
                ret.Nodes += current.Nodes;
                ret.Edges += current.Edges;
                ret.Labels += current.Labels;
                ret.Tags += current.Tags;
                ret.Vectors += current.Vectors;
            }

            return ret;
        }

        private static GraphStatistics AggregateGraphStatistics(IEnumerable<GraphStatistics> stats)
        {
            GraphStatistics ret = new GraphStatistics();
            if (stats == null) return ret;

            foreach (GraphStatistics current in stats)
            {
                if (current == null) continue;
                ret.Nodes += current.Nodes;
                ret.Edges += current.Edges;
                ret.Labels += current.Labels;
                ret.Tags += current.Tags;
                ret.Vectors += current.Vectors;
            }

            return ret;
        }

        private void StartRequestActivity(HttpContextBase ctx, RequestContext req)
        {
            if (ctx == null || req == null) return;
            if (!_Settings.Observability.Enable || !_Settings.Observability.EnableOpenTelemetry) return;

            string traceparent = ctx.Request.HeaderExists(Constants.TraceparentHeader)
                ? ctx.Request.RetrieveHeaderValue(Constants.TraceparentHeader)
                : null;
            string tracestate = ctx.Request.HeaderExists(Constants.TracestateHeader)
                ? ctx.Request.RetrieveHeaderValue(Constants.TracestateHeader)
                : null;

            Activity activity;
            if (ObservabilityService.TryParseTraceContext(traceparent, tracestate, out ActivityContext parentContext))
            {
                activity = _Observability.StartActivity(ctx.Request.Method + " " + OperationalLogRedactor.RedactUrl(ctx.Request.Url.RawWithoutQuery), ActivityKind.Server, parentContext);
            }
            else
            {
                activity = _Observability.StartActivity(ctx.Request.Method + " " + OperationalLogRedactor.RedactUrl(ctx.Request.Url.RawWithoutQuery), ActivityKind.Server);
            }

            if (activity == null) return;

            activity.SetTag("http.request.method", ctx.Request.Method.ToString());
            activity.SetTag("url.path", OperationalLogRedactor.RedactUrl(ctx.Request.Url.RawWithoutQuery));
            activity.SetTag("url.full", OperationalLogRedactor.RedactUrl(ctx.Request.Url.RawWithQuery));
            activity.SetTag("server.address", _Settings.Rest.Hostname);
            activity.SetTag("server.port", _Settings.Rest.Port);
            activity.SetTag("client.address", ctx.Request.Source.IpAddress);
            activity.SetTag("litegraph.request.id", req.RequestId);
            activity.SetTag("litegraph.correlation.id", req.CorrelationId);
            activity.SetTag("litegraph.request.type", req.RequestType.ToString());
            if (req.TenantGUID != null) activity.SetTag("litegraph.tenant_guid", req.TenantGUID.Value.ToString());
            if (req.GraphGUID != null) activity.SetTag("litegraph.graph_guid", req.GraphGUID.Value.ToString());

            req.RequestActivity = activity;
            req.TraceId = activity.TraceId.ToString();
        }

        private Activity StartInternalActivity(string name, RequestContext req)
        {
            if (req == null || req.RequestActivity == null) return null;
            Activity activity = _Observability.StartActivity(name, ActivityKind.Internal, req.RequestActivity.Context);
            if (activity == null) return null;

            activity.SetTag("litegraph.request.id", req.RequestId);
            activity.SetTag("litegraph.correlation.id", req.CorrelationId);
            activity.SetTag("litegraph.request.type", req.RequestType.ToString());
            if (req.TenantGUID != null) activity.SetTag("litegraph.tenant_guid", req.TenantGUID.Value.ToString());
            if (req.GraphGUID != null) activity.SetTag("litegraph.graph_guid", req.GraphGUID.Value.ToString());
            return activity;
        }

        private static void SetAuthenticationActivityTags(Activity activity, RequestContext req)
        {
            if (activity == null || req == null) return;
            activity.SetTag("litegraph.authentication.result", req.Authentication.Result.ToString());
            activity.SetTag("litegraph.authorization.result", req.Authorization.Result.ToString());
            if (!String.IsNullOrEmpty(req.Authorization.Reason)) activity.SetTag("litegraph.authorization.reason", req.Authorization.Reason);
            if (!String.IsNullOrEmpty(req.Authorization.RequiredScope)) activity.SetTag("litegraph.authorization.required_scope", req.Authorization.RequiredScope);
            activity.SetTag("litegraph.authentication.is_admin", req.Authentication.IsAdmin);
            activity.SetTag("litegraph.authentication.has_credential", req.Authentication.CredentialGUID != null);
        }

        private static void RecordActivityException(Activity activity, Exception exception, int statusCode)
        {
            if (activity == null) return;
            activity.SetTag("litegraph.handler.success", false);
            activity.SetTag("http.response.status_code", statusCode);
            activity.SetTag("exception.type", exception.GetType().FullName);
            activity.SetTag("exception.message", exception.Message);
        }

        private static void StopRequestActivity(HttpContextBase ctx)
        {
            if (ctx?.Metadata is not RequestContext req) return;
            Activity activity = req.RequestActivity;
            if (activity == null) return;

            activity.SetTag("http.response.status_code", ctx.Response.StatusCode);
            if (ctx.Timestamp.TotalMs != null) activity.SetTag("litegraph.request.duration_ms", ctx.Timestamp.TotalMs.Value);
            activity.Stop();
            activity.Dispose();
            req.RequestActivity = null;
        }

        private EnumerationRequest BuildEnumerationQuery(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            return new EnumerationRequest
            {
                TenantGUID = req.TenantGUID,
                GraphGUID = req.GraphGUID,
                MaxResults = req.MaxKeys,
                Skip = req.Skip,
                IncludeData = req.IncludeData,
                IncludeSubordinates = req.IncludeSubordinates,
                ContinuationToken = (!String.IsNullOrEmpty(req.ContinuationToken) ? Guid.Parse(req.ContinuationToken) : null)
            };
        }

        private static string GraphQueryRequiredScope(GraphQueryRequest query)
        {
            if (query == null || String.IsNullOrWhiteSpace(query.Query)) return "read";

            try
            {
                LiteGraph.Query.Ast.GraphQueryAst ast = LiteGraph.Query.Parser.Parse(query.Query);
                return IsWriteQueryKind(ast.Kind) ? "write" : "read";
            }
            catch
            {
                return QueryContainsMutationKeyword(query.Query) ? "write" : "read";
            }
        }

        private static bool IsWriteQueryKind(LiteGraph.Query.GraphQueryKindEnum kind)
        {
            switch (kind)
            {
                case LiteGraph.Query.GraphQueryKindEnum.CreateNode:
                case LiteGraph.Query.GraphQueryKindEnum.CreateEdge:
                case LiteGraph.Query.GraphQueryKindEnum.CreateLabel:
                case LiteGraph.Query.GraphQueryKindEnum.CreateTag:
                case LiteGraph.Query.GraphQueryKindEnum.CreateVector:
                case LiteGraph.Query.GraphQueryKindEnum.UpdateNode:
                case LiteGraph.Query.GraphQueryKindEnum.UpdateEdge:
                case LiteGraph.Query.GraphQueryKindEnum.UpdateLabel:
                case LiteGraph.Query.GraphQueryKindEnum.UpdateTag:
                case LiteGraph.Query.GraphQueryKindEnum.UpdateVector:
                case LiteGraph.Query.GraphQueryKindEnum.DeleteNode:
                case LiteGraph.Query.GraphQueryKindEnum.DeleteEdge:
                case LiteGraph.Query.GraphQueryKindEnum.DeleteLabel:
                case LiteGraph.Query.GraphQueryKindEnum.DeleteTag:
                case LiteGraph.Query.GraphQueryKindEnum.DeleteVector:
                    return true;
                default:
                    return false;
            }
        }

        private static bool QueryContainsMutationKeyword(string query)
        {
            if (String.IsNullOrWhiteSpace(query)) return false;

            try
            {
                foreach (LiteGraph.Query.GraphQueryToken token in LiteGraph.Query.Lexer.Tokenize(query))
                {
                    if (token.Type == LiteGraph.Query.GraphQueryTokenTypeEnum.Identifier
                        && IsMutationKeyword(token.Text))
                    {
                        return true;
                    }
                }
            }
            catch
            {
                return query.IndexOf("CREATE", StringComparison.OrdinalIgnoreCase) >= 0
                    || query.IndexOf("MERGE", StringComparison.OrdinalIgnoreCase) >= 0
                    || query.IndexOf("SET", StringComparison.OrdinalIgnoreCase) >= 0
                    || query.IndexOf("DELETE", StringComparison.OrdinalIgnoreCase) >= 0
                    || query.IndexOf("REMOVE", StringComparison.OrdinalIgnoreCase) >= 0;
            }

            return false;
        }

        private static bool IsMutationKeyword(string text)
        {
            return String.Equals(text, "CREATE", StringComparison.OrdinalIgnoreCase)
                || String.Equals(text, "MERGE", StringComparison.OrdinalIgnoreCase)
                || String.Equals(text, "SET", StringComparison.OrdinalIgnoreCase)
                || String.Equals(text, "DELETE", StringComparison.OrdinalIgnoreCase)
                || String.Equals(text, "REMOVE", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Request-History-Routes

        private Guid? TenantScopeForRequestHistory(RequestContext req, NameValueCollection query)
        {
            if (req.Authentication.IsAdmin)
            {
                string q = query?["tenantGuid"];
                if (!string.IsNullOrEmpty(q) && Guid.TryParse(q, out Guid tg)) return tg;
                return null;
            }
            return req.Authentication.TenantGUID;
        }

        private RequestHistorySearchRequest BuildRequestHistorySearch(RequestContext req)
        {
            NameValueCollection q = req.Query;
            RequestHistorySearchRequest search = new RequestHistorySearchRequest();

            search.TenantGUID = TenantScopeForRequestHistory(req, q);

            if (!string.IsNullOrEmpty(q?["requestId"])) search.RequestId = q["requestId"];
            if (!string.IsNullOrEmpty(q?["correlationId"])) search.CorrelationId = q["correlationId"];
            if (!string.IsNullOrEmpty(q?["traceId"])) search.TraceId = q["traceId"];
            if (!string.IsNullOrEmpty(q?["method"])) search.Method = q["method"];
            if (!string.IsNullOrEmpty(q?["path"])) search.Path = q["path"];
            if (!string.IsNullOrEmpty(q?["sourceIp"])) search.SourceIp = q["sourceIp"];
            if (!string.IsNullOrEmpty(q?["transactionId"])) search.TransactionId = q["transactionId"];
            if (!string.IsNullOrEmpty(q?["statusCode"]) && int.TryParse(q["statusCode"], out int sc)) search.StatusCode = sc;
            if (!string.IsNullOrEmpty(q?["success"]) && bool.TryParse(q["success"], out bool success)) search.Success = success;
            if (!string.IsNullOrEmpty(q?["hasTransactionDiagnostics"]) && bool.TryParse(q["hasTransactionDiagnostics"], out bool hasTransactionDiagnostics)) search.HasTransactionDiagnostics = hasTransactionDiagnostics;
            if (!string.IsNullOrEmpty(q?["fromUtc"]) && DateTime.TryParse(q["fromUtc"], null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime from))
                search.FromUtc = from;
            if (!string.IsNullOrEmpty(q?["toUtc"]) && DateTime.TryParse(q["toUtc"], null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime to))
                search.ToUtc = to;
            if (!string.IsNullOrEmpty(q?["page"]) && int.TryParse(q["page"], out int page) && page >= 0) search.Page = page;
            if (!string.IsNullOrEmpty(q?["pageSize"]) && int.TryParse(q["pageSize"], out int pageSize) && pageSize >= 1 && pageSize <= 1000) search.PageSize = pageSize;

            return search;
        }

        private async Task RequestHistoryListRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            RequestHistorySearchRequest search = BuildRequestHistorySearch(req);
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            try
            {
                RequestHistorySearchResult result = await _LiteGraph.RequestHistory.Search(search, timeoutCts.Token).ConfigureAwait(false);
                ctx.Response.StatusCode = 200;
                await ctx.Response.Send(_Serializer.SerializeJson(result));
            }
            catch (OperationCanceledException oce) when (timeoutCts.IsCancellationRequested)
            {
                await SendRequestTimeout(ctx, "request history search", oce).ConfigureAwait(false);
            }
        }

        private async Task RequestHistorySummaryRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            NameValueCollection q = req.Query;

            string interval = q?["interval"];
            if (string.IsNullOrEmpty(interval)) interval = "hour";

            DateTime end = DateTime.UtcNow;
            DateTime start = end.AddDays(-1);
            if (!string.IsNullOrEmpty(q?["startUtc"]) && DateTime.TryParse(q["startUtc"], null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime s)) start = s;
            if (!string.IsNullOrEmpty(q?["endUtc"]) && DateTime.TryParse(q["endUtc"], null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime e)) end = e;

            Guid? tenantGuid = TenantScopeForRequestHistory(req, q);

            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            try
            {
                RequestHistorySummary summary = await _LiteGraph.RequestHistory.GetSummary(tenantGuid, interval, start, end, timeoutCts.Token).ConfigureAwait(false);
                ctx.Response.StatusCode = 200;
                await ctx.Response.Send(_Serializer.SerializeJson(summary));
            }
            catch (OperationCanceledException oce) when (timeoutCts.IsCancellationRequested)
            {
                await SendRequestTimeout(ctx, "request history summary", oce).ConfigureAwait(false);
            }
        }

        private async Task RequestHistoryReadRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            Guid id = ParseRequestHistoryGuid(req);
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            RequestHistoryEntry entry;
            try
            {
                entry = await _LiteGraph.RequestHistory.ReadByGuid(id, timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException oce) when (timeoutCts.IsCancellationRequested)
            {
                await SendRequestTimeout(ctx, "request history read", oce).ConfigureAwait(false);
                return;
            }

            if (entry == null)
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound)));
                return;
            }
            if (!CanViewRequestHistoryRow(req, entry.TenantGUID))
            {
                ctx.Response.StatusCode = 401;
                await SendAuthorizationFailed(ctx, req, "RequestHistoryTenantDenied", "read", "The request history row is outside the authenticated tenant.").ConfigureAwait(false);
                return;
            }
            ctx.Response.StatusCode = 200;
            await ctx.Response.Send(_Serializer.SerializeJson(entry));
        }

        private async Task RequestHistoryDetailRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            Guid id = ParseRequestHistoryGuid(req);
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            RequestHistoryDetail detail;
            try
            {
                detail = await _LiteGraph.RequestHistory.ReadDetailByGuid(id, timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException oce) when (timeoutCts.IsCancellationRequested)
            {
                await SendRequestTimeout(ctx, "request history detail read", oce).ConfigureAwait(false);
                return;
            }

            if (detail == null)
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound)));
                return;
            }
            if (!CanViewRequestHistoryRow(req, detail.TenantGUID))
            {
                ctx.Response.StatusCode = 401;
                await SendAuthorizationFailed(ctx, req, "RequestHistoryTenantDenied", "read", "The request history row is outside the authenticated tenant.").ConfigureAwait(false);
                return;
            }
            ctx.Response.StatusCode = 200;
            await ctx.Response.Send(_Serializer.SerializeJson(detail));
        }

        private async Task RequestHistoryDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            Guid id = ParseRequestHistoryGuid(req);
            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            RequestHistoryEntry entry;
            try
            {
                entry = await _LiteGraph.RequestHistory.ReadByGuid(id, timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException oce) when (timeoutCts.IsCancellationRequested)
            {
                await SendRequestTimeout(ctx, "request history delete read", oce).ConfigureAwait(false);
                return;
            }

            if (entry == null)
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound)));
                return;
            }
            if (!CanViewRequestHistoryRow(req, entry.TenantGUID))
            {
                ctx.Response.StatusCode = 401;
                await SendAuthorizationFailed(ctx, req, "RequestHistoryTenantDenied", "write", "The request history row is outside the authenticated tenant.").ConfigureAwait(false);
                return;
            }

            try
            {
                await _LiteGraph.RequestHistory.DeleteByGuid(id, timeoutCts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException oce) when (timeoutCts.IsCancellationRequested)
            {
                await SendRequestTimeout(ctx, "request history delete", oce).ConfigureAwait(false);
                return;
            }

            ctx.Response.StatusCode = 204;
            await ctx.Response.Send();
        }

        private async Task RequestHistoryDeleteManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            RequestHistorySearchRequest search = BuildRequestHistorySearch(req);

            // Non-admins can only bulk-delete within their own tenant scope.
            if (!req.Authentication.IsAdmin)
            {
                search.TenantGUID = req.Authentication.TenantGUID;
            }

            using CancellationTokenSource timeoutCts = CreateRequestTimeoutTokenSource();
            try
            {
                int deleted = await _LiteGraph.RequestHistory.DeleteMany(search, timeoutCts.Token).ConfigureAwait(false);
                ctx.Response.StatusCode = 200;
                await ctx.Response.Send(_Serializer.SerializeJson(new { Deleted = deleted }));
            }
            catch (OperationCanceledException oce) when (timeoutCts.IsCancellationRequested)
            {
                await SendRequestTimeout(ctx, "request history bulk delete", oce).ConfigureAwait(false);
            }
        }

        private Guid ParseRequestHistoryGuid(RequestContext req)
        {
            string val = req.Http?.Request?.Url?.Parameters?.Get("requestGuid");
            if (string.IsNullOrEmpty(val)) throw new ArgumentException("requestGuid parameter is required");
            return Guid.Parse(val);
        }

        private bool CanViewRequestHistoryRow(RequestContext req, Guid? rowTenant)
        {
            if (req.Authentication.IsAdmin) return true;
            if (rowTenant == null) return false;
            return rowTenant.Value == req.Authentication.TenantGUID.GetValueOrDefault();
        }

        #endregion

    }
}
