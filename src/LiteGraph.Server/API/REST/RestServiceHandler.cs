namespace LiteGraph.Server.API.REST
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection.PortableExecutable;
    using System.Text.Json;
    using System.Threading.Tasks;
    using LiteGraph.Serialization;
    using LiteGraph.Server.API.Agnostic;
    using LiteGraph.Server.Classes;
    using LiteGraph.Server.Services;
    using SyslogLogging;
    using WatsonWebserver;
    using WatsonWebserver.Core;

    internal class RestServiceHandler
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Internal-Members

        #endregion

        #region Private-Members

        private readonly string _Header = "[RestServiceHandler] ";
        static string _Hostname = Dns.GetHostName();
        private Settings _Settings = null;
        private LoggingModule _Logging = null;
        private LiteGraphClient _LiteGraph = null;
        private SerializationHelper _Serializer = null;
        private AuthenticationService _Authentication = null;
        private ServiceHandler _ServiceHandler = null;

        private Webserver _Webserver = null;

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
            SerializationHelper serializer,
            AuthenticationService auth,
            ServiceHandler service)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _LiteGraph = litegraph ?? throw new ArgumentNullException(nameof(litegraph));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _Authentication = auth ?? throw new ArgumentNullException(nameof(auth));
            _ServiceHandler = service ?? throw new ArgumentNullException(nameof(service));

            _Webserver = new Webserver(_Settings.Rest, DefaultRequestHandler);
            _Webserver.Routes.PreRouting = PreRoutingHandler;
            _Webserver.Routes.AuthenticateRequest = AuthenticateRequest;
            _Webserver.Routes.PostRouting = PostRoutingHandler;
            _Webserver.Routes.Preflight = OptionsHandler;

            InitializeRoutes();

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

        internal void InitializeRoutes()
        {
            _Webserver.Routes.PreAuthentication.Static.Add(HttpMethod.HEAD, "/", LoopbackRoute, ExceptionRoute);
            _Webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/", RootRoute, ExceptionRoute);
            _Webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/favicon.ico", FaviconRoute, ExceptionRoute);
            _Webserver.Routes.PreAuthentication.Static.Add(HttpMethod.GET, "/v1.0/token/tenants", TokenTenantsRoute, ExceptionRoute);

            #region Tokens

            _Webserver.Routes.PostAuthentication.Static.Add(HttpMethod.GET, "/v1.0/token", TokenCreateRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Static.Add(HttpMethod.GET, "/v1.0/token/details", TokenDetailsRoute, ExceptionRoute);
            
            #endregion

            #region Tenants

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants", TenantCreateRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants", TenantReadManyRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}", TenantReadRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}", TenantExistsRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}", TenantUpdateRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}", TenantDeleteRoute, ExceptionRoute);

            #endregion

            #region Users

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/users", UserCreateRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/users", UserReadManyRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/users/{userGuid}", UserReadRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/users/{userGuid}", UserExistsRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/users/{userGuid}", UserUpdateRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/users/{userGuid}", UserDeleteRoute, ExceptionRoute);

            #endregion

            #region Credentials

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/credentials", CredentialCreateRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/credentials", CredentialReadManyRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}", CredentialReadRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}", CredentialExistsRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}", CredentialUpdateRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}", CredentialDeleteRoute, ExceptionRoute);

            #endregion

            #region Labels

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/labels", LabelCreateRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/labels", LabelReadManyRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/labels/{labelGuid}", LabelReadRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/labels/{labelGuid}", LabelExistsRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/labels/{labelGuid}", LabelUpdateRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/labels/{labelGuid}", LabelDeleteRoute, ExceptionRoute);

            #endregion

            #region Tags

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/tags", TagCreateRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/tags", TagReadManyRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/tags/{tagGuid}", TagReadRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/tags/{tagGuid}", TagExistsRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/tags/{tagGuid}", TagUpdateRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/tags/{tagGuid}", TagDeleteRoute, ExceptionRoute);

            #endregion

            #region Vectors

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/vectors", VectorCreateRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/vectors", VectorSearchRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/vectors", VectorReadManyRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/vectors/{vectorGuid}", VectorReadRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/vectors/{vectorGuid}", VectorExistsRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/vectors/{vectorGuid}", VectorUpdateRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/vectors/{vectorGuid}", VectorDeleteRoute, ExceptionRoute);

            #endregion

            #region Graphs

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs", GraphCreateRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/search", GraphSearchRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}", GraphReadRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}", GraphExistsRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs", GraphReadManyRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}", GraphUpdateRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}", GraphDeleteRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/existence", GraphExistenceRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/export/gexf", GraphGexfExportRoute, ExceptionRoute);

            #endregion

            #region Nodes

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes", NodeCreateRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/multiple", NodeCreateMultipleRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/search", NodeSearchRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}", NodeReadRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}", NodeExistsRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes", NodeReadManyRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}", NodeUpdateRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/all", NodeDeleteAllRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/multiple", NodeDeleteMultipleRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}", NodeDeleteRoute, ExceptionRoute);

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/edges/from", EdgesFromNodeRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/edges/to", EdgesToNodeRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/edges", AllEdgesToNodeRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/neighbors", NodeNeighborsRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/parents", NodeParentsRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/children", NodeChildrenRoute, ExceptionRoute);

            #endregion

            #region Edges

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges", EdgeCreateRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/multiple", EdgeCreateMultipleRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/between", EdgesBetweenRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/search", EdgeSearchRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}", EdgeReadRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.HEAD, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}", EdgeExistsRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.GET, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges", EdgeReadManyRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.PUT, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}", EdgeUpdateRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/all", EdgeDeleteAllRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/multiple", EdgeDeleteMultipleRoute, ExceptionRoute);
            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.DELETE, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}", EdgeDeleteRoute, ExceptionRoute);

            #endregion

            #region Routes-and-Traversal

            _Webserver.Routes.PostAuthentication.Parameter.Add(HttpMethod.POST, "/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/routes", GetRoutesRoute, ExceptionRoute);

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
            responseHeaders.Add("Access-Control-Allow-Headers", "*, Content-Type, X-Requested-With, " + headers);
            responseHeaders.Add("Access-Control-Expose-Headers", "Content-Type, X-Requested-With, " + headers);
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
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, fe.Message), true));
                return;
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + ctx.Request.Source.IpAddress + " exception building request context" + Environment.NewLine + e.ToString());
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, "Unable to build request context."), true));
                return;
            }

            ctx.Metadata = req;
            if (_Settings.Debug.Requests)
                _Logging.Debug(_Serializer.SerializeJson(ctx.Request, true));
        }

        internal async Task AuthenticateRequest(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            _Authentication.AuthenticateAndAuthorize(req);

            switch (req.Authentication.Result)
            {
                case AuthenticationResultEnum.Success:
                    break;

                case AuthenticationResultEnum.NotFound:
                    ctx.Response.StatusCode = 401;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.AuthenticationFailed), true));
                    return;

                case AuthenticationResultEnum.Inactive:
                    ctx.Response.StatusCode = 401;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.Inactive), true));
                    return;

                case AuthenticationResultEnum.Invalid:
                    ctx.Response.StatusCode = 400;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest), true));
                    return;
            }

            switch (req.Authorization.Result)
            {
                case AuthorizationResultEnum.Conflict:
                    ctx.Response.StatusCode = 409;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.Conflict), true));
                    return;

                case AuthorizationResultEnum.Denied:
                    ctx.Response.StatusCode = 401;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.AuthorizationFailed), true));
                    return;

                case AuthorizationResultEnum.NotFound:
                    ctx.Response.StatusCode = 404;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound), true));
                    return;

                case AuthorizationResultEnum.Permitted:
                    break;
            }
        }

        internal async Task DefaultRequestHandler(HttpContextBase ctx)
        {
            _Logging.Warn(_Header + "unknown verb or endpoint: " + ctx.Request.Method + " " + ctx.Request.Url.RawWithQuery);
            ctx.Response.StatusCode = 400;
            await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest), true));
        }

        internal async Task PostRoutingHandler(HttpContextBase ctx)
        {
            string msg =
                _Header
                + ctx.Request.Method.ToString() + " " + ctx.Request.Url.RawWithQuery + " "
                + ctx.Response.StatusCode + " "
                + ctx.Timestamp.TotalMs + "ms";

            if (ctx.Response.StatusCode > 299 && _Settings.Debug.Requests)
                msg += Environment.NewLine + ctx.Response.DataAsString;

            ctx.Timestamp.End = DateTime.UtcNow;
            _Logging.Debug(msg);
        }

        #endregion

        #region Private-Route-Implementations

        #region Token

        private async Task TokenCreateRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            AuthenticationToken token = _Authentication.GenerateToken(req.Authentication.TenantGUID.Value, req.Authentication.UserGUID.Value);
            ctx.Response.StatusCode = 200;
            await ctx.Response.Send(_Serializer.SerializeJson(token, true));
            return;
        }

        private async Task TokenDetailsRoute(HttpContextBase ctx) 
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            
            if (String.IsNullOrEmpty(req.Authentication.SecurityToken))
            {
                _Logging.Warn(_Header + "no authentication token supplied from which to read details");
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, "No authentication token supplied."), true));
                return;
            }

            AuthenticationToken token = _Authentication.ReadToken(req.Authentication.SecurityToken);
            ctx.Response.StatusCode = 200;
            await ctx.Response.Send(_Serializer.SerializeJson(token, true));
            return;
        }

        private async Task TokenTenantsRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            if (String.IsNullOrEmpty(req.Authentication.Email))
            {
                _Logging.Warn(_Header + "no email supplied in headers for tenant retrieval");
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, "No email supplied in the request headers."), true));
                return;
            }

            ResponseContext resp = await _ServiceHandler.UserTenants(req);
            if (resp.Success)
            {
                ctx.Response.StatusCode = 200;
                await ctx.Response.Send(_Serializer.SerializeJson(resp.Data, true));
                return;
            }
            else
            {
                ctx.Response.StatusCode = 404;
                await ctx.Response.Send(_Serializer.SerializeJson(resp.Error, true));
            }
        }

        #endregion

        #region General

        private async Task ExceptionRoute(HttpContextBase ctx, Exception e)
        {
            if (_Settings.Debug.Exceptions) _Logging.Warn(_Header + "exception of type " + e.GetType() + ": " + e.ToString());

            if (e is JsonException)
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.DeserializationError), true));
            }
            else if (e is FormatException)
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, e.Message), true));
            }
            else if (e is InvalidOperationException)
            {
                ctx.Response.StatusCode = 409;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.Conflict, null, e.Message), true));
            }
            else if (e is ArgumentException)
            {
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, e.Message), true));
            }
            else
            {
                ctx.Response.StatusCode = 500;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError, null, e.Message), true));
                _Logging.Warn(_Header + "exception encountered for " + ctx.Request.Method.ToString() + " " + ctx.Request.Url.RawWithQuery + Environment.NewLine + e.ToString());
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

        private async Task FaviconRoute(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 200;
            ctx.Response.ContentType = Constants.FaviconContentType;
            await ctx.Response.Send(File.ReadAllBytes(Constants.FaviconFile));
        }

        private async Task NoRequestBody(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 400;
            await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest), true));
        }

        private async Task NotAdmin(HttpContextBase ctx)
        {
            ctx.Response.StatusCode = 401;
            await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.AuthorizationFailed), true));
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

        private async Task LabelReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.LabelReadMany);
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

        private async Task TagReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.TagReadMany);
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

        private async Task VectorReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.VectorReadMany);
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
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphUpdate);
        }

        private async Task GraphDeleteRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.GraphDelete);
        }

        private async Task GraphGexfExportRoute(HttpContextBase ctx)
        {
            try
            {
                RequestContext req = (RequestContext)ctx.Metadata;
                ResponseContext resp = await _ServiceHandler.GraphGexfExport(req);
                if (!resp.Success)
                {
                    ctx.Response.StatusCode = 500;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError), true));
                }
                else
                {
                    ctx.Response.ContentType = Constants.XmlContentType;
                    ctx.Response.StatusCode = 200;
                    await ctx.Response.Send(resp.Data.ToString());
                }
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "GEXF export error:" + Environment.NewLine + e.ToString());
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = Constants.JsonContentType;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError, null, e.Message), true));
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

        private async Task NodeCreateMultipleRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Nodes = _Serializer.DeserializeJson<List<Node>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeCreateMultiple);
        }

        private async Task NodeReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeReadMany);
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

        private async Task NodeDeleteMultipleRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.GUIDs = _Serializer.DeserializeJson<List<Guid>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.NodeDeleteMultiple);
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

        private async Task EdgeCreateMultipleRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.Edges = _Serializer.DeserializeJson<List<Edge>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeCreateMultiple);
        }

        private async Task EdgeReadManyRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeReadMany);
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

        private async Task EdgeDeleteMultipleRoute(HttpContextBase ctx)
        {
            RequestContext req = (RequestContext)ctx.Metadata;

            if (String.IsNullOrEmpty(ctx.Request.DataAsString))
            {
                await NoRequestBody(ctx);
                return;
            }

            req.GUIDs = _Serializer.DeserializeJson<List<Guid>>(ctx.Request.DataAsString);
            await WrappedRequestHandler(ctx, req, _ServiceHandler.EdgeDeleteMultiple);
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

        private string GetQueryValue(NameValueCollection nvc, string key)
        {
            if (nvc != null && nvc.AllKeys.Contains(key)) return nvc[key];
            return null;
        }

        private async Task WrappedRequestHandler(HttpContextBase ctx, RequestContext req, Func<RequestContext, Task<ResponseContext>> func)
        {
            try
            {
                ResponseContext resp = await func(req);
                if (resp == null)
                {
                    _Logging.Warn(_Header + "no response from agnostic handler");
                    ctx.Response.StatusCode = 500;
                    await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.InternalError), true));
                    return;
                }
                else if (resp.Success)
                {
                    ctx.Response.StatusCode = resp.StatusCode;
                    if (resp.Data != null) await ctx.Response.Send(_Serializer.SerializeJson(resp.Data, true));
                    else await ctx.Response.Send();
                    return;
                }
                else
                {
                    if (resp.Error != null) ctx.Response.StatusCode = resp.Error.StatusCode;
                    else ctx.Response.StatusCode = 418;
                    if (resp.Error != null && ctx.Request.Method != HttpMethod.HEAD) await ctx.Response.Send(_Serializer.SerializeJson(resp.Error, true));
                    else await ctx.Response.Send();
                    return;
                }
            }
            catch (JsonException je)
            {
                _Logging.Warn(_Header + "JSON exception: " + Environment.NewLine + je.ToString());
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.DeserializationError, null, je.Message), true));
            }
            catch (FormatException fe)
            {
                _Logging.Warn(_Header + "format exception: " + Environment.NewLine + fe.ToString());
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, fe.Message), true));
            }
            catch (InvalidOperationException ioe)
            {
                _Logging.Warn(_Header + "invalid operation exception: " + Environment.NewLine + ioe.ToString());
                ctx.Response.StatusCode = 409;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.Conflict, null, ioe.Message), true));
            }
            catch (KeyNotFoundException knfe)
            {
                _Logging.Warn(_Header + "invalid operation exception: " + Environment.NewLine + knfe.ToString());
                ctx.Response.StatusCode = 404;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.NotFound, null, knfe.Message), true));
                return;
            }
            catch (ArgumentException ae)
            {
                _Logging.Warn(_Header + "argument exception: " + Environment.NewLine + ae.ToString());
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, ae.Message), true));
                return;
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "exception: " + Environment.NewLine + e.ToString());
                ctx.Response.StatusCode = 400;
                await ctx.Response.Send(_Serializer.SerializeJson(new ApiErrorResponse(ApiErrorEnum.BadRequest, null, e.Message), true));
                return;
            }
        }

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
