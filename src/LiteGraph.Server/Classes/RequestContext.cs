namespace LiteGraph.Server.Classes
{
    using LiteGraph.Helpers;
    using LiteGraph.Serialization;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using WatsonWebserver.Core;

    /// <summary>
    /// Request context.
    /// </summary>
    public class RequestContext
    {
        #region Public-Members

        /// <summary>
        /// Request GUID.
        /// </summary>
        public Guid RequestGuid { get; set; } = Guid.NewGuid();

        /// <summary>
        /// API version.
        /// </summary>
        public ApiVersionEnum ApiVersion { get; set; } = ApiVersionEnum.Unknown;

        /// <summary>
        /// Request type.
        /// </summary>
        public RequestTypeEnum RequestType { get; set; } = RequestTypeEnum.Unknown;

        /// <summary>
        /// Requestor IP.
        /// </summary>
        public string Ip
        {
            get
            {
                if (_Http != null)
                    return _Http.Request.Source.IpAddress;

                return null;
            }
        }

        /// <summary>
        /// HTTP context.
        /// </summary>
        public HttpContextBase Http
        {
            get
            {
                return _Http;
            }
        }

        /// <summary>
        /// Authentication context.
        /// </summary>
        public AuthenticationContext Authentication
        {
            get
            {
                return _Authentication;
            }
        }

        /// <summary>
        /// Authorization context.
        /// </summary>
        public AuthorizationContext Authorization
        {
            get
            {
                return _Authorization;
            }
        }

        /// <summary>
        /// Content-type.
        /// </summary>
        public string ContentType { get; set; } = null;

        /// <summary>
        /// Content length.
        /// </summary>
        public long ContentLength
        {
            get
            {
                return _ContentLength;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(ContentLength));
                _ContentLength = value;
            }
        }

        /// <summary>
        /// Data.
        /// </summary>
        public byte[] Data { get; set; } = null;

        /// <summary>
        /// HTTP method.
        /// </summary>
        public HttpMethod Method
        {
            get
            {
                if (_Http != null) return _Http.Request.Method;
                return HttpMethod.UNKNOWN;
            }
        }

        /// <summary>
        /// URL.
        /// </summary>
        public string Url
        {
            get
            {
                if (_Http != null) return _Http.Request.Url.Full;
                return null;
            }
        }

        /// <summary>
        /// Querystring.
        /// </summary>
        public string Querystring
        {
            get
            {
                if (_Http != null) return _Http.Request.Query.Querystring;
                return null;
            }
        }

        /// <summary>
        /// Headers.
        /// </summary>
        public NameValueCollection Headers
        {
            get
            {
                if (_Http != null) return _Http.Request.Headers;
                return new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);
            }
        }

        /// <summary>
        /// Query.
        /// </summary>
        public NameValueCollection Query
        {
            get
            {
                if (_Http != null) return _Http.Request.Query.Elements;
                return new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);
            }
        }

        #region Objects

        /// <summary>
        /// Backup filename.
        /// </summary>
        public string BackupFilename { get; set; } = null;

        /// <summary>
        /// Backup request.
        /// </summary>
        public BackupRequest BackupRequest { get; set; } = null;

        /// <summary>
        /// Tenant GUID.
        /// </summary>
        public Guid? TenantGUID { get; set; } = null;

        /// <summary>
        /// Tenant.
        /// </summary>
        public TenantMetadata Tenant { get; set; } = null;

        /// <summary>
        /// Graph GUID.
        /// </summary>
        public Guid? GraphGUID { get; set; } = null;

        /// <summary>
        /// Graph.
        /// </summary>
        public Graph Graph { get; set; } = null;

        /// <summary>
        /// GUIDs.
        /// </summary>
        public List<Guid> GUIDs { get; set; } = null;

        /// <summary>
        /// Node GUID.
        /// </summary>
        public Guid? NodeGUID { get; set; } = null;

        /// <summary>
        /// Node.
        /// </summary>
        public Node Node { get; set; } = null;

        /// <summary>
        /// Nodes.
        /// </summary>
        public List<Node> Nodes { get; set; } = null;

        /// <summary>
        /// Edge GUID.
        /// </summary>
        public Guid? EdgeGUID { get; set; } = null;

        /// <summary>
        /// Edge.
        /// </summary>
        public Edge Edge { get; set; } = null;

        /// <summary>
        /// Edges.
        /// </summary>
        public List<Edge> Edges { get; set; } = null;

        /// <summary>
        /// Label GUID.
        /// </summary>
        public Guid? LabelGUID { get; set; } = null;

        /// <summary>
        /// Label.
        /// </summary>
        public LabelMetadata Label { get; set; } = null;

        /// <summary>
        /// Labels.
        /// </summary>
        public List<LabelMetadata> Labels { get; set; } = null;

        /// <summary>
        /// Tag GUID.
        /// </summary>
        public Guid? TagGUID { get; set; } = null;

        /// <summary>
        /// Tag.
        /// </summary>
        public TagMetadata Tag { get; set; } = null;

        /// <summary>
        /// Tags.
        /// </summary>
        public List<TagMetadata> Tags { get; set; } = null;

        /// <summary>
        /// Vector GUID.
        /// </summary>
        public Guid? VectorGUID { get; set; } = null;

        /// <summary>
        /// Vector.
        /// </summary>
        public VectorMetadata Vector { get; set; } = null;

        /// <summary>
        /// Vectors.
        /// </summary>
        public List<VectorMetadata> Vectors { get; set; } = null;

        /// <summary>
        /// Vector search request.
        /// </summary>
        public VectorSearchRequest VectorSearchRequest { get; set; } = null;

        /// <summary>
        /// User GUID.
        /// </summary>
        public Guid? UserGUID { get; set; } = null;

        /// <summary>
        /// User.
        /// </summary>
        public UserMaster User { get; set; } = null;

        /// <summary>
        /// Credential GUID.
        /// </summary>
        public Guid? CredentialGUID { get; set; } = null;

        /// <summary>
        /// Credential.
        /// </summary>
        public Credential Credential { get; set; } = null;

        /// <summary>
        /// Existence request.
        /// </summary>
        public ExistenceRequest ExistenceRequest { get; set; } = null;

        /// <summary>
        /// Search request.
        /// </summary>
        public SearchRequest SearchRequest { get; set; } = null;

        /// <summary>
        /// Enumeration query.
        /// </summary>
        public EnumerationRequest EnumerationQuery { get; set; } = null;

        /// <summary>
        /// Route request.
        /// </summary>
        public RouteRequest RouteRequest { get; set; } = null;

        #endregion

        #region Query

        /// <summary>
        /// Enumeration order.
        /// </summary>
        public EnumerationOrderEnum Order { get; set; } = EnumerationOrderEnum.CreatedDescending;

        /// <summary>
        /// Number of records to skip in enumeration.
        /// </summary>
        public int Skip
        {
            get
            {
                return _Skip;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(Skip));
                _Skip = value;
            }
        }

        /// <summary>
        /// Max keys.
        /// </summary>
        public int MaxKeys
        {
            get
            {
                return _MaxKeys;
            }
            set
            {
                if (value < 1 || value > 1000) throw new ArgumentOutOfRangeException(nameof(MaxKeys));
                _MaxKeys = value;
            }
        }

        /// <summary>
        /// Force deletion.
        /// </summary>
        public bool Force { get; set; } = false;

        /// <summary>
        /// Include data.
        /// </summary>
        public bool IncludeData { get; set; } = false;

        /// <summary>
        /// Include subordinates.
        /// </summary>
        public bool IncludeSubordinates { get; set; } = false;

        /// <summary>
        /// Max depth for subgraph traversal.
        /// </summary>
        public int MaxDepth { get; set; } = 2;

        /// <summary>
        /// Max nodes for subgraph retrieval.
        /// </summary>
        public int MaxNodes { get; set; } = 0;

        /// <summary>
        /// Max edges for subgraph retrieval.
        /// </summary>
        public int MaxEdges { get; set; } = 0;

        /// <summary>
        /// From GUID.
        /// </summary>
        public Guid? FromGUID { get; set; } = null;

        /// <summary>
        /// To GUID.
        /// </summary>
        public Guid? ToGUID { get; set; } = null;

        /// <summary>
        /// Continuation token.
        /// </summary>
        public string ContinuationToken { get; set; } = null;

        #endregion

        #endregion

        #region Private-Members

        private long _ContentLength = 0;
        private HttpContextBase _Http = null;
        private UrlContext _Url = null;
        private AuthenticationContext _Authentication = new AuthenticationContext();
        private AuthorizationContext _Authorization = new AuthorizationContext();

        private int _MaxKeys = 1000;
        private int _Skip = 0;

        private static Serializer _Serializer = new Serializer();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="ctx">HTTP context.</param>
        public RequestContext(HttpContextBase ctx)
        {
            _Http = ctx ?? throw new ArgumentNullException(nameof(ctx));
            _Url = new UrlContext(
                ctx.Request.Method,
                ctx.Request.Url.RawWithoutQuery,
                ctx.Request.Query.Elements,
                ctx.Request.Headers);

            RequestType = _Url.RequestType;

            SetApiVersion();
            SetAuthValues();
            SetRequestValues();

            Data = ctx.Request.DataAsBytes;
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        private void SetApiVersion()
        {
            if (_Http.Request.Url.Parameters.AllKeys.Contains("apiVersion"))
            {
                string apiVersion = _Http.Request.Url.Parameters.Get("apiVersion");
                if (!String.IsNullOrEmpty(apiVersion))
                {
                    if (apiVersion.Equals("v1.0")) ApiVersion = ApiVersionEnum.V1_0;
                }
            }
        }

        private void SetAuthValues()
        {
            if (_Http.Request.HeaderExists(Constants.AuthorizationHeader))
            {
                string authHeader = _Http.Request.RetrieveHeaderValue(Constants.AuthorizationHeader);
                if (authHeader.ToLower().StartsWith("bearer "))
                    _Authentication.BearerToken = authHeader.Substring(7);
            }

            if (_Http.Request.HeaderExists(Constants.EmailHeader))
                _Authentication.Email = _Http.Request.RetrieveHeaderValue(Constants.EmailHeader);

            if (_Http.Request.HeaderExists(Constants.PasswordHeader))
                _Authentication.Password = _Http.Request.RetrieveHeaderValue(Constants.PasswordHeader);

            if (_Http.Request.HeaderExists(Constants.TokenHeader))
                _Authentication.SecurityToken = _Http.Request.RetrieveHeaderValue(Constants.TokenHeader);

            if (_Http.Request.HeaderExists(Constants.TenantGuidHeader))
                _Authentication.TenantGUID = Guid.Parse(_Http.Request.RetrieveHeaderValue(Constants.TenantGuidHeader));
        }

        private void SetRequestValues()
        {
            if (_Url != null)
            {
                if (_Url.UrlParameters != null && _Url.UrlParameters.Count > 0)
                {
                    if (_Url.UrlParameters.AllKeys.Contains("backupFilename")) BackupFilename = _Url.GetParameter("backupFilename");
                    if (_Url.UrlParameters.AllKeys.Contains("tenantGuid")) TenantGUID = Guid.Parse(_Url.GetParameter("tenantGuid"));
                    if (_Url.UrlParameters.AllKeys.Contains("userGuid")) UserGUID = Guid.Parse(_Url.GetParameter("userGuid"));
                    if (_Url.UrlParameters.AllKeys.Contains("credentialGuid")) CredentialGUID = Guid.Parse(_Url.GetParameter("credentialGuid"));
                    if (_Url.UrlParameters.AllKeys.Contains("labelGuid")) LabelGUID = Guid.Parse(_Url.GetParameter("labelGuid"));
                    if (_Url.UrlParameters.AllKeys.Contains("tagGuid")) TagGUID = Guid.Parse(_Url.GetParameter("tagGuid"));
                    if (_Url.UrlParameters.AllKeys.Contains("vectorGuid")) VectorGUID = Guid.Parse(_Url.GetParameter("vectorGuid"));
                    if (_Url.UrlParameters.AllKeys.Contains("graphGuid")) GraphGUID = Guid.Parse(_Url.GetParameter("graphGuid"));
                    if (_Url.UrlParameters.AllKeys.Contains("nodeGuid")) NodeGUID = Guid.Parse(_Url.GetParameter("nodeGuid"));
                    if (_Url.UrlParameters.AllKeys.Contains("edgeGuid")) EdgeGUID = Guid.Parse(_Url.GetParameter("edgeGuid"));
                }

                if (_Url.QueryExists(Constants.SkipQuerystring))
                    if (Int32.TryParse(_Url.GetQueryValue(Constants.SkipQuerystring), out int skip)) Skip = skip;

                if (_Url.QueryExists(Constants.MaxKeysQuerystring))
                    if (Int32.TryParse(_Url.GetQueryValue(Constants.MaxKeysQuerystring), out int maxKeys)) MaxKeys = maxKeys;

                if (_Url.QueryExists(Constants.EnumerationOrderQuerystring))
                {
                    if (Enum.TryParse<EnumerationOrderEnum>(_Url.GetQueryValue(Constants.EnumerationOrderQuerystring), out EnumerationOrderEnum val))
                    {
                        Order = val;
                    }
                }

                if (_Url.QueryExists(Constants.ContinuationTokenQuerystring)) ContinuationToken = _Url.GetQueryValue(Constants.ContinuationTokenQuerystring);
                if (_Url.QueryExists(Constants.ForceQuerystring)) Force = true;
                if (_Url.QueryExists(Constants.IncludeDataQuerystring)) IncludeData = true;
                if (_Url.QueryExists(Constants.IncludeSubordinatesQuerystring)) IncludeSubordinates = true;

                if (_Url.QueryExists(Constants.MaxDepth))
                    if (int.TryParse(_Url.GetQueryValue(Constants.MaxDepth), out int maxDepth)) MaxDepth = maxDepth;

                if (_Url.QueryExists(Constants.MaxNodes))
                    if (int.TryParse(_Url.GetQueryValue(Constants.MaxNodes), out int maxNodes)) MaxNodes = maxNodes;

                if (_Url.QueryExists(Constants.MaxEdges))
                    if (int.TryParse(_Url.GetQueryValue(Constants.MaxEdges), out int maxEdges)) MaxEdges = maxEdges;

                if (_Url.QueryExists(Constants.FromGuidQuerystring)) FromGUID = Guid.Parse(_Url.GetQueryValue(Constants.FromGuidQuerystring));
                if (_Url.QueryExists(Constants.ToGuidQuerystring)) ToGUID = Guid.Parse(_Url.GetQueryValue(Constants.ToGuidQuerystring));
                if (_Url.QueryExists(Constants.GuidsQuerystring)) GUIDs = StringHelpers.StringToGuidList(_Url.GetQueryValue(Constants.GuidsQuerystring));
            }
        }

        #endregion
    }
}
