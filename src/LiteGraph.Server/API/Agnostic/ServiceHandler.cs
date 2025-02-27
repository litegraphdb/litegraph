namespace LiteGraph.Server.API.Agnostic
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Reflection.PortableExecutable;
    using System.Text;
    using System.Threading.Tasks;
    using LiteGraph.Serialization;
    using LiteGraph.Server.Classes;
    using LiteGraph.Server.Services;
    using SyslogLogging;
    using WatsonWebserver.Core;

    internal class ServiceHandler
    {
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        #region Internal-Members

        #endregion

        #region Private-Members

        private readonly string _Header = "[ServiceHandler] ";
        static string _Hostname = Dns.GetHostName();
        private Settings _Settings = null;
        private LoggingModule _Logging = null;
        private LiteGraphClient _LiteGraph = null;
        private SerializationHelper _Serializer = null;
        private AuthenticationService _Authentication = null;

        #endregion

        #region Constructors-and-Factories

        internal ServiceHandler(
            Settings settings,
            LoggingModule logging,
            LiteGraphClient litegraph,
            SerializationHelper serializer,
            AuthenticationService auth)
        {
            _Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _Logging = logging ?? throw new ArgumentNullException(nameof(logging));
            _LiteGraph = litegraph ?? throw new ArgumentNullException(nameof(litegraph));
            _Serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            _Authentication = auth ?? throw new ArgumentNullException(nameof(auth));

            _Logging.Debug(_Header + "initialized service handler");
        }

        #endregion

        #region Internal-Methods

        #region Tenant-Routes

        internal async Task<ResponseContext> TenantCreate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Tenant == null) throw new ArgumentNullException(nameof(req.Tenant));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            TenantMetadata obj = _LiteGraph.CreateTenant(req.Tenant.GUID, req.Tenant.Name);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> TenantReadMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            List<TenantMetadata> objs = _LiteGraph.ReadTenants().ToList();
            if (objs == null) objs = new List<TenantMetadata>();
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> TenantRead(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            TenantMetadata obj = _LiteGraph.ReadTenant(req.TenantGUID.Value);
            if (obj != null) return new ResponseContext(req, obj);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> TenantExists(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (_LiteGraph.ExistsTenant(req.TenantGUID.Value)) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> TenantUpdate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Tenant == null) throw new ArgumentNullException(nameof(req.Tenant));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            req.Tenant.GUID = req.TenantGUID.Value;
            TenantMetadata obj = _LiteGraph.UpdateTenant(req.Tenant);
            if (obj == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> TenantDelete(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            _LiteGraph.DeleteTenant(req.TenantGUID.Value, req.Force);
            return new ResponseContext(req);
        }

        #endregion

        #region User-Routes

        internal async Task<ResponseContext> UserCreate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.User == null) throw new ArgumentNullException(nameof(req.User));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            req.User.TenantGUID = req.TenantGUID.Value;
            UserMaster obj = _LiteGraph.CreateUser(req.User);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> UserReadMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            List<UserMaster> objs = _LiteGraph.ReadUsers(req.TenantGUID.Value, null).ToList();
            if (objs == null) objs = new List<UserMaster>();
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> UserRead(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            UserMaster obj = _LiteGraph.ReadUser(req.TenantGUID.Value, req.UserGUID.Value);
            if (obj != null) return new ResponseContext(req, obj);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> UserExists(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (_LiteGraph.ExistsUser(req.TenantGUID.Value, req.UserGUID.Value)) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> UserUpdate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.User == null) throw new ArgumentNullException(nameof(req.User));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            req.User.TenantGUID = req.TenantGUID.Value;
            UserMaster obj = _LiteGraph.UpdateUser(req.User);
            if (obj == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> UserDelete(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            _LiteGraph.DeleteUser(req.TenantGUID.Value, req.UserGUID.Value);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> UserTenants(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            List<TenantMetadata> tenants = _LiteGraph.ReadUserTenants(req.Authentication.Email);
            if (tenants != null && tenants.Count > 0) return new ResponseContext(req, tenants);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        #endregion

        #region Credential-Routes

        internal async Task<ResponseContext> CredentialCreate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Credential == null) throw new ArgumentNullException(nameof(req.Credential));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            req.Credential.TenantGUID = req.TenantGUID.Value;
            Credential obj = _LiteGraph.CreateCredential(req.Credential);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> CredentialReadMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            List<Credential> objs = _LiteGraph.ReadCredentials(req.TenantGUID.Value, null, null).ToList();
            if (objs == null) objs = new List<Credential>();
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> CredentialRead(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            Credential obj = _LiteGraph.ReadCredential(req.TenantGUID.Value, req.CredentialGUID.Value);
            if (obj != null) return new ResponseContext(req, obj);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> CredentialExists(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            if (_LiteGraph.ExistsCredential(req.TenantGUID.Value, req.CredentialGUID.Value)) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> CredentialUpdate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Credential == null) throw new ArgumentNullException(nameof(req.Credential));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            req.Credential.TenantGUID = req.TenantGUID.Value;
            Credential obj = _LiteGraph.UpdateCredential(req.Credential);
            if (obj == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> CredentialDelete(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.Authentication.IsAdmin) return ResponseContext.FromError(req, ApiErrorEnum.AuthorizationFailed);
            _LiteGraph.DeleteCredential(req.TenantGUID.Value, req.CredentialGUID.Value);
            return new ResponseContext(req);
        }

        #endregion

        #region Label-Routes

        internal async Task<ResponseContext> LabelCreate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Label == null) throw new ArgumentNullException(nameof(req.Label));
            req.Label.TenantGUID = req.TenantGUID.Value;
            LabelMetadata obj = _LiteGraph.CreateLabel(req.Label);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> LabelReadMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            List<LabelMetadata> objs = _LiteGraph.ReadLabels(req.TenantGUID.Value, req.GraphGUID, req.NodeGUID, req.EdgeGUID, null).ToList();
            if (objs == null) objs = new List<LabelMetadata>();
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> LabelRead(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            LabelMetadata obj = _LiteGraph.ReadLabel(req.TenantGUID.Value, req.LabelGUID.Value);
            if (obj != null) return new ResponseContext(req, obj);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> LabelExists(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (_LiteGraph.ExistsLabelMetadata(req.TenantGUID.Value, req.LabelGUID.Value)) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> LabelUpdate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Label == null) throw new ArgumentNullException(nameof(req.Label));
            req.Label.TenantGUID = req.TenantGUID.Value;
            LabelMetadata obj = _LiteGraph.UpdateLabel(req.Label);
            if (obj == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> LabelDelete(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            _LiteGraph.DeleteLabel(req.TenantGUID.Value, req.LabelGUID.Value);
            return new ResponseContext(req);
        }

        #endregion

        #region Tag-Routes

        internal async Task<ResponseContext> TagCreate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Tag == null) throw new ArgumentNullException(nameof(req.Tag));
            req.Tag.TenantGUID = req.TenantGUID.Value;
            TagMetadata obj = _LiteGraph.CreateTag(req.Tag);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> TagReadMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            List<TagMetadata> objs = _LiteGraph.ReadTags(req.TenantGUID.Value, null, null, null, null, null).ToList();
            if (objs == null) objs = new List<TagMetadata>();
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> TagRead(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            TagMetadata obj = _LiteGraph.ReadTag(req.TenantGUID.Value, req.TagGUID.Value);
            if (obj != null) return new ResponseContext(req, obj);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> TagExists(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (_LiteGraph.ExistsTagMetadata(req.TenantGUID.Value, req.TagGUID.Value)) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> TagUpdate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Tag == null) throw new ArgumentNullException(nameof(req.Tag));
            req.Tag.TenantGUID = req.TenantGUID.Value;
            TagMetadata obj = _LiteGraph.UpdateTag(req.Tag);
            if (obj == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> TagDelete(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            _LiteGraph.DeleteTag(req.TenantGUID.Value, req.TagGUID.Value);
            return new ResponseContext(req);
        }

        #endregion

        #region Vector-Routes

        internal async Task<ResponseContext> VectorCreate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Vector == null) throw new ArgumentNullException(nameof(req.Vector));
            req.Vector.TenantGUID = req.TenantGUID.Value;
            VectorMetadata obj = _LiteGraph.CreateVector(req.Vector);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> VectorReadMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            List<VectorMetadata> objs = _LiteGraph.ReadVectors(req.TenantGUID.Value, null, null, null).ToList();
            if (objs == null) objs = new List<VectorMetadata>();
            return new ResponseContext(req, objs);
        }

        internal async Task<ResponseContext> VectorRead(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            VectorMetadata obj = _LiteGraph.ReadVector(req.TenantGUID.Value, req.VectorGUID.Value);
            if (obj != null) return new ResponseContext(req, obj);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> VectorExists(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (_LiteGraph.ExistsVectorMetadata(req.TenantGUID.Value, req.VectorGUID.Value)) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> VectorUpdate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Vector == null) throw new ArgumentNullException(nameof(req.Vector));
            req.Vector.TenantGUID = req.TenantGUID.Value;
            VectorMetadata obj = _LiteGraph.UpdateVector(req.Vector);
            if (obj == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            return new ResponseContext(req, obj);
        }

        internal async Task<ResponseContext> VectorDelete(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            _LiteGraph.DeleteVector(req.TenantGUID.Value, req.VectorGUID.Value);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> VectorSearch(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.VectorSearchRequest == null) throw new ArgumentNullException(nameof(req.VectorSearchRequest));
            if (req.GraphGUID != null && !_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            IEnumerable<VectorSearchResult> sresp = _LiteGraph.SearchVectors(
                req.VectorSearchRequest.Domain,
                req.VectorSearchRequest.SearchType,
                req.VectorSearchRequest.Embeddings,
                req.VectorSearchRequest.TenantGUID,
                req.VectorSearchRequest.GraphGUID,
                req.VectorSearchRequest.Labels,
                req.VectorSearchRequest.Tags,
                req.VectorSearchRequest.Expr);
            return new ResponseContext(req, sresp.ToList());
        }

        #endregion

        #region Graph-Routes

        internal async Task<ResponseContext> GraphCreate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Graph == null) throw new ArgumentNullException(nameof(req.Graph));
            req.Graph.TenantGUID = req.TenantGUID.Value;

            Graph graph = _LiteGraph.CreateGraph(
                req.TenantGUID.Value, 
                req.Graph.GUID, 
                req.Graph.Name, 
                req.Graph.Labels,
                req.Graph.Tags, 
                req.Graph.Vectors,
                req.Graph.Data);
            return new ResponseContext(req, graph);
        }

        internal async Task<ResponseContext> GraphReadMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            List<Graph> graphs = _LiteGraph.ReadGraphs(req.TenantGUID.Value).ToList();
            if (graphs == null) graphs = new List<Graph>();
            return new ResponseContext(req, graphs);
        }

        internal async Task<ResponseContext> GraphExistence(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.ExistenceRequest == null) throw new ArgumentNullException(nameof(req.ExistenceRequest));
            if (!req.ExistenceRequest.ContainsExistenceRequest()) return ResponseContext.FromError(req, ApiErrorEnum.BadRequest, null, "No valid existence filters are present in the request.");
            ExistenceResult resp = _LiteGraph.BatchExistence(req.TenantGUID.Value, req.GraphGUID.Value, req.ExistenceRequest);
            return new ResponseContext(req, resp);
        }

        internal async Task<ResponseContext> GraphSearch(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.SearchRequest == null) throw new ArgumentNullException(nameof(req.ExistenceRequest));
            SearchResult sresp = new SearchResult();
            sresp.Graphs = _LiteGraph.ReadGraphs(
                req.TenantGUID.Value, 
                req.SearchRequest.Labels,
                req.SearchRequest.Tags, 
                req.SearchRequest.Expr, 
                req.SearchRequest.Ordering).ToList();
            return new ResponseContext(req, sresp);
        }

        internal async Task<ResponseContext> GraphRead(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            Graph graph = _LiteGraph.ReadGraph(req.TenantGUID.Value, req.GraphGUID.Value);
            if (graph == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            else return new ResponseContext(req, graph);
        }

        internal async Task<ResponseContext> GraphExists(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            Graph graph = _LiteGraph.ReadGraph(req.TenantGUID.Value, req.GraphGUID.Value);
            if (graph == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            else return new ResponseContext(req);
        }

        internal async Task<ResponseContext> GraphUpdate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Graph == null) throw new ArgumentNullException(nameof(req.Graph));
            req.Graph.TenantGUID = req.TenantGUID.Value;
            req.Graph = _LiteGraph.UpdateGraph(req.Graph);
            if (req.Graph == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            else return new ResponseContext(req, req.Graph);
        }

        internal async Task<ResponseContext> GraphDelete(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);

            try
            {
                _LiteGraph.DeleteGraph(req.TenantGUID.Value, req.GraphGUID.Value, req.Force);
                return new ResponseContext(req);
            }
            catch (InvalidOperationException ioe)
            {
                return ResponseContext.FromError(req, ApiErrorEnum.Conflict, null, ioe.Message);
            }
            catch (ArgumentException e)
            {
                return ResponseContext.FromError(req, ApiErrorEnum.BadRequest, null, e.Message);
            }
        }

        internal async Task<ResponseContext> GraphGexfExport(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            try
            {
                string xml = _LiteGraph.RenderGraphAsGexf(
                    req.TenantGUID.Value,
                    req.GraphGUID.Value,
                    req.IncludeData);

                return new ResponseContext(req, xml);
            }
            catch (ArgumentException)
            {
                return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            }
            catch (Exception e)
            {
                _Logging.Warn(_Header + "GEXF export exception:" + Environment.NewLine + e.ToString());
                return ResponseContext.FromError(req, ApiErrorEnum.InternalError);
            }
        }

        #endregion

        #region Node-Routes

        internal async Task<ResponseContext> NodeCreate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Node == null) throw new ArgumentNullException(nameof(req.Node));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            req.Node.TenantGUID = req.TenantGUID.Value;
            req.Node.GraphGUID = req.GraphGUID.Value;
            req.Node = _LiteGraph.CreateNode(req.Node);
            return new ResponseContext(req, req.Node);
        }

        internal async Task<ResponseContext> NodeCreateMultiple(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Nodes == null) throw new ArgumentNullException(nameof(req.Nodes));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);

            try
            {
                req.Nodes = _LiteGraph.CreateNodes(req.TenantGUID.Value, req.GraphGUID.Value, req.Nodes);
                return new ResponseContext(req, req.Nodes);
            }
            catch (InvalidOperationException ioe)
            {
                return ResponseContext.FromError(req, ApiErrorEnum.Conflict, null, ioe.Message);
            }
        }

        internal async Task<ResponseContext> NodeReadMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            List<Node> nodes = _LiteGraph.ReadNodes(req.TenantGUID.Value, req.GraphGUID.Value).ToList();
            if (nodes == null) nodes = new List<Node>();
            return new ResponseContext(req, nodes);
        }

        internal async Task<ResponseContext> NodeSearch(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.SearchRequest == null) throw new ArgumentNullException(nameof(req.SearchRequest));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            SearchResult sresp = new SearchResult();
            sresp.Nodes = _LiteGraph.ReadNodes(
                req.TenantGUID.Value, 
                req.GraphGUID.Value, 
                req.SearchRequest.Labels,
                req.SearchRequest.Tags, 
                req.SearchRequest.Expr, 
                req.SearchRequest.Ordering,
                req.SearchRequest.Skip).ToList();
            if (sresp.Nodes == null) sresp.Nodes = new List<Node>();
            return new ResponseContext(req, sresp);
        }

        internal async Task<ResponseContext> NodeRead(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            Node node = _LiteGraph.ReadNode(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value);
            if (node == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            else return new ResponseContext(req, node);
        }

        internal async Task<ResponseContext> NodeExists(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            bool exists = _LiteGraph.ExistsNode(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value);
            if (exists) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> NodeUpdate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Node == null) throw new ArgumentNullException(nameof(req.Node));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            req.Node.TenantGUID = req.TenantGUID.Value;
            req.Node.GraphGUID = req.GraphGUID.Value;
            req.Node = _LiteGraph.UpdateNode(req.Node);
            if (req.Node != null) return new ResponseContext(req, req.Node);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> NodeDelete(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (!_LiteGraph.ExistsNode(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            _LiteGraph.DeleteNode(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> NodeDeleteAll(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            _LiteGraph.DeleteNodes(req.TenantGUID.Value, req.GraphGUID.Value);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> NodeDeleteMultiple(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.GUIDs == null) throw new ArgumentNullException(nameof(req.GUIDs));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            _LiteGraph.DeleteNodes(req.TenantGUID.Value, req.GraphGUID.Value, req.GUIDs);
            return new ResponseContext(req);
        }

        #endregion

        #region Edge-Routes

        internal async Task<ResponseContext> EdgeCreate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Edge == null) throw new ArgumentNullException(nameof(req.Edge));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            req.Edge.TenantGUID = req.TenantGUID.Value;
            req.Edge.GraphGUID = req.GraphGUID.Value;
            req.Edge = _LiteGraph.CreateEdge(req.Edge);
            return new ResponseContext(req, req.Edge);
        }

        internal async Task<ResponseContext> EdgeCreateMultiple(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Edges == null) throw new ArgumentNullException(nameof(req.Edges));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);

            try
            {
                req.Edges = _LiteGraph.CreateEdges(req.TenantGUID.Value, req.GraphGUID.Value, req.Edges);
                return new ResponseContext(req, req.Edges);
            }
            catch (KeyNotFoundException knfe)
            {
                return ResponseContext.FromError(req, ApiErrorEnum.NotFound, null, knfe.Message);
            }
            catch (InvalidOperationException ioe)
            {
                return ResponseContext.FromError(req, ApiErrorEnum.Conflict, null, ioe.Message);
            }
        }

        internal async Task<ResponseContext> EdgeReadMany(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            req.Edges = _LiteGraph.ReadEdges(req.TenantGUID.Value, req.GraphGUID.Value).ToList();
            if (req.Edges == null) req.Edges = new List<Edge>();
            return new ResponseContext(req, req.Edges);
        }

        internal async Task<ResponseContext> EdgesBetween(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (!_LiteGraph.ExistsNode(req.TenantGUID.Value, req.GraphGUID.Value, req.FromGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound, null, "The specified from node does not exist.");
            if (!_LiteGraph.ExistsNode(req.TenantGUID.Value, req.GraphGUID.Value, req.ToGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound, null, "The specified to node does not exist.");
            req.Edges = _LiteGraph.GetEdgesBetween(req.TenantGUID.Value, req.GraphGUID.Value, req.FromGUID.Value, req.ToGUID.Value).ToList();
            if (req.Edges == null) req.Edges = new List<Edge>();
            return new ResponseContext(req, req.Edges);
        }

        internal async Task<ResponseContext> EdgeSearch(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.SearchRequest == null) throw new ArgumentNullException(nameof(req.SearchRequest));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            SearchResult sresp = new SearchResult();
            sresp.Edges = _LiteGraph.ReadEdges(
                req.TenantGUID.Value, 
                req.GraphGUID.Value, 
                req.SearchRequest.Labels,
                req.SearchRequest.Tags, 
                req.SearchRequest.Expr, 
                req.SearchRequest.Ordering,
                req.SearchRequest.Skip).ToList();
            return new ResponseContext(req, sresp);
        }

        internal async Task<ResponseContext> EdgeRead(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            Edge edge = _LiteGraph.ReadEdge(req.TenantGUID.Value, req.GraphGUID.Value, req.EdgeGUID.Value);
            if (edge == null) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            return new ResponseContext(req, edge);
        }

        internal async Task<ResponseContext> EdgeExists(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (_LiteGraph.ExistsEdge(req.TenantGUID.Value, req.GraphGUID.Value, req.EdgeGUID.Value)) return new ResponseContext(req);
            else return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
        }

        internal async Task<ResponseContext> EdgeUpdate(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.Edge == null) throw new ArgumentNullException(nameof(req.Edge));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            req.Edge.TenantGUID = req.TenantGUID.Value;
            req.Edge.GraphGUID = req.GraphGUID.Value;
            req.Edge = _LiteGraph.UpdateEdge(req.Edge);
            return new ResponseContext(req, req.Edge);
        }

        internal async Task<ResponseContext> EdgeDelete(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (!_LiteGraph.ExistsEdge(req.TenantGUID.Value, req.GraphGUID.Value, req.EdgeGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            _LiteGraph.DeleteEdge(req.TenantGUID.Value, req.GraphGUID.Value, req.EdgeGUID.Value);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> EdgeDeleteAll(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            _LiteGraph.DeleteEdges(req.TenantGUID.Value, req.GraphGUID.Value);
            return new ResponseContext(req);
        }

        internal async Task<ResponseContext> EdgeDeleteMultiple(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.GUIDs == null) throw new ArgumentNullException(nameof(req.GUIDs));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            _LiteGraph.DeleteEdges(req.TenantGUID.Value, req.GraphGUID.Value, req.GUIDs);
            return new ResponseContext(req);
        }

        #endregion

        #region Routes-and-Traversal

        internal async Task<ResponseContext> EdgesFromNode(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (!_LiteGraph.ExistsNode(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            List<Edge> edgesFrom = _LiteGraph.GetEdgesFrom(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value).ToList();
            if (edgesFrom == null) edgesFrom = new List<Edge>();
            return new ResponseContext(req, edgesFrom);
        }

        internal async Task<ResponseContext> EdgesToNode(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (!_LiteGraph.ExistsNode(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            List<Edge> edgesTo = _LiteGraph.GetEdgesTo(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value).ToList();
            if (edgesTo == null) edgesTo = new List<Edge>();
            return new ResponseContext(req, edgesTo);
        }

        internal async Task<ResponseContext> AllEdgesToNode(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (!_LiteGraph.ExistsNode(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            List<Edge> edgesFrom = _LiteGraph.GetEdgesFrom(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value).ToList();
            if (edgesFrom == null) edgesFrom = new List<Edge>();
            List<Edge> edgesTo = _LiteGraph.GetEdgesTo(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value).ToList();
            if (edgesTo == null) edgesTo = new List<Edge>();
            List<Edge> edges = new List<Edge>();
            edges.AddRange(edgesFrom);
            edges.AddRange(edgesTo);
            edges = edges.DistinctBy(e => e.GUID).ToList();
            return new ResponseContext(req, edges);
        }

        internal async Task<ResponseContext> NodeChildren(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (!_LiteGraph.ExistsNode(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            List<Node> nodes = _LiteGraph.GetChildren(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value).ToList();
            if (nodes == null) nodes = new List<Node>();
            return new ResponseContext(req, nodes);
        }

        internal async Task<ResponseContext> NodeParents(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (!_LiteGraph.ExistsNode(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            List<Node> parents = _LiteGraph.GetParents(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value).ToList();
            if (parents == null) parents = new List<Node>();
            return new ResponseContext(req, parents);
        }

        internal async Task<ResponseContext> NodeNeighbors(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (!_LiteGraph.ExistsNode(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            List<Node> neighbors = _LiteGraph.GetNeighbors(req.TenantGUID.Value, req.GraphGUID.Value, req.NodeGUID.Value).ToList();
            if (neighbors == null) neighbors = new List<Node>();
            return new ResponseContext(req, neighbors);
        }

        internal async Task<ResponseContext> GetRoutes(RequestContext req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (req.RouteRequest == null) throw new ArgumentNullException(nameof(req.RouteRequest));
            if (!_LiteGraph.ExistsGraph(req.TenantGUID.Value, req.GraphGUID.Value)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (!_LiteGraph.ExistsNode(req.TenantGUID.Value, req.GraphGUID.Value, req.RouteRequest.From)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);
            if (!_LiteGraph.ExistsNode(req.TenantGUID.Value, req.GraphGUID.Value, req.RouteRequest.To)) return ResponseContext.FromError(req, ApiErrorEnum.NotFound);

            RouteResponse sresp = new RouteResponse();
            List<RouteDetail> routes = _LiteGraph.GetRoutes(
                SearchTypeEnum.DepthFirstSearch,
                req.TenantGUID.Value,
                req.GraphGUID.Value,
                req.RouteRequest.From,
                req.RouteRequest.To,
                req.RouteRequest.EdgeFilter,
                req.RouteRequest.NodeFilter).ToList();

            if (routes == null) routes = new List<RouteDetail>();
            routes = routes.OrderBy(r => r.TotalCost).ToList();
            sresp.Routes = routes;
            sresp.Timestamp.End = DateTime.UtcNow;
            return new ResponseContext(req, sresp);
        }

        #endregion

        #endregion

        #region Private-Methods

        #endregion

#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
    }
}
