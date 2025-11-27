namespace LiteGraph.Server.Classes
{
    using LiteGraph.Serialization;
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using UrlMatcher;
    using WatsonWebserver;
    using WatsonWebserver.Core;

    /// <summary>
    /// URL context.
    /// </summary>
    public class UrlContext
    {
        #region Public-Members

        /// <summary>
        /// Method to invoke to emit log messages.
        /// </summary>
        public Action<string> Logger { get; set; } = null;

        /// <summary>
        /// API version.
        /// </summary>
        public ApiVersionEnum ApiVersion
        {
            get
            {
                if (_UrlParameters != null)
                {
                    string apiVersion = GetParameter("ApiVersion");
                    return VersionStringToApiVersion(apiVersion);
                }

                return ApiVersionEnum.Unknown;
            }
        }

        /// <summary>
        /// Request type.
        /// </summary>
        public RequestTypeEnum RequestType
        {
            get
            {
                return _RequestType;
            }
        }

        /// <summary>
        /// HTTP method.
        /// </summary>
        public HttpMethod HttpMethod
        {
            get
            {
                return _HttpMethod;
            }
        }

        /// <summary>
        /// Query.
        /// </summary>
        public NameValueCollection Query
        {
            get
            {
                return _Query;
            }
        }

        /// <summary>
        /// Headers.
        /// </summary>
        public NameValueCollection Headers
        {
            get
            {
                return _Headers;
            }
        }

        /// <summary>
        /// URL parameters.
        /// </summary>
        public NameValueCollection UrlParameters
        {
            get
            {
                return _UrlParameters;
            }
        }

        #endregion

        #region Private-Members

        private string _Header = "[UrlContext] ";
        private HttpMethod _HttpMethod = HttpMethod.GET;
        private string _Url = null;
        private NameValueCollection _Query = null;
        private NameValueCollection _Headers = null;
        private NameValueCollection _UrlParameters = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);

        private RequestTypeEnum _RequestType = RequestTypeEnum.Unknown;

        private static Serializer _Serializer = new Serializer();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="method">HTTP method.</param>
        /// <param name="url">URL.</param>
        /// <param name="query">Query.</param>
        /// <param name="headers">Headers.</param>
        public UrlContext(
            HttpMethod method,
            string url,
            NameValueCollection query = null,
            NameValueCollection headers = null)
        {
            if (String.IsNullOrEmpty(url)) throw new ArgumentNullException(nameof(url));

            _HttpMethod = method;
            _Url = url;
            _Query = NormalizeNameValueCollection(query);
            _Headers = NormalizeNameValueCollection(headers);

            _RequestType = UrlAndMethodToRequestType();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Retrieve URL parameter.
        /// </summary>
        /// <param name="key">Parameter.</param>
        /// <returns>Value or null.</returns>
        public string GetParameter(string key)
        {
            if (string.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            string ret = null;
            if (_UrlParameters != null)
            {
                if (_UrlParameters.AllKeys.Contains(key, StringComparer.InvariantCultureIgnoreCase))
                {
                    ret = _UrlParameters[key];
                }
            }
            else
            {
                Log("no parameters");
            }

            Log("retrieving parameter " + key + ": " + (!String.IsNullOrEmpty(ret) ? ret : "(null)"));
            return ret;
        }

        /// <summary>
        /// Retrieve query value.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Value or null.</returns>
        public string GetQueryValue(string key)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            if (_Query != null)
            {
                foreach (string existingKey in _Query.Keys)
                {
                    if (existingKey != null && existingKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                        return _Query[existingKey];
                }

                return null;
            }

            return null;
        }

        /// <summary>
        /// Retrieve header value.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>Value or null.</returns>
        public string GetHeaderValue(string key)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));

            if (_Headers != null)
            {
                foreach (string existingKey in _Headers.Keys)
                {
                    if (existingKey != null && existingKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                        return _Headers[existingKey];
                }

                return null;
            }

            return null;
        }

        /// <summary>
        /// Check if a query key exists.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>True if exists.</returns>
        public bool QueryExists(string key)
        {
            if (_Query != null && !string.IsNullOrEmpty(key))
            {
                foreach (string existingKey in _Query.Keys)
                {
                    if (existingKey != null && existingKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

            return false;
        }

        /// <summary>
        /// Check if a header exists.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <returns>True if exists.</returns>
        public bool HeaderExists(string key)
        {
            if (_Headers != null && !string.IsNullOrEmpty(key))
            {
                foreach (string existingKey in _Headers.Keys)
                {
                    if (existingKey != null && existingKey.Equals(key, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }

                return false;
            }

            return false;
        }

        #endregion

        #region Private-Methods

        private RequestTypeEnum UrlAndMethodToRequestType()
        {
            _UrlParameters = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);

            try
            {
                Matcher matcher = new Matcher(_Url);

                if (_HttpMethod == HttpMethod.GET)
                {
                    #region GET

                    if (matcher.Match("/", out _UrlParameters)) return RequestTypeEnum.Root;
                    if (matcher.Match("/favicon.ico", out _UrlParameters)) return RequestTypeEnum.Favicon;

                    if (matcher.Match("/v1.0/backups", out _UrlParameters)) return RequestTypeEnum.BackupReadAll;
                    if (matcher.Match("/v1.0/backups/{backupFilename}", out _UrlParameters)) return RequestTypeEnum.BackupRead;

                    if (matcher.Match("/v1.0/tenants", out _UrlParameters)) return RequestTypeEnum.TenantReadAll;
                    if (matcher.Match("/v2.0/tenants", out _UrlParameters)) return RequestTypeEnum.TenantEnumerate;
                    if (matcher.Match("/v1.0/tenants/stats", out _UrlParameters)) return RequestTypeEnum.TenantStatistics;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}", out _UrlParameters)) return RequestTypeEnum.TenantRead;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/stats", out _UrlParameters)) return RequestTypeEnum.TenantStatistics;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/users", out _UrlParameters)) return RequestTypeEnum.UserReadAll;
                    if (matcher.Match("/v2.0/tenants/{tenantGuid}/users", out _UrlParameters)) return RequestTypeEnum.UserEnumerate;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/users/{userGuid}", out _UrlParameters)) return RequestTypeEnum.UserRead;

                    if (matcher.Match("/v1.0/credentials/bearer/{bearerToken}", out _UrlParameters)) return RequestTypeEnum.CredentialReadByBearerToken;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/credentials", out _UrlParameters)) return RequestTypeEnum.CredentialReadAll;
                    if (matcher.Match("/v2.0/tenants/{tenantGuid}/credentials", out _UrlParameters)) return RequestTypeEnum.CredentialEnumerate;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}", out _UrlParameters)) return RequestTypeEnum.CredentialRead;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/labels/all", out _UrlParameters)) return RequestTypeEnum.LabelReadAllInTenant;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/labels/all", out _UrlParameters)) return RequestTypeEnum.LabelReadAllInGraph;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/labels", out _UrlParameters)) return RequestTypeEnum.LabelReadManyGraph;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/labels", out _UrlParameters)) return RequestTypeEnum.LabelReadManyNode;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}/labels", out _UrlParameters)) return RequestTypeEnum.LabelReadManyEdge;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/labels", out _UrlParameters)) return RequestTypeEnum.LabelReadAll;
                    if (matcher.Match("/v2.0/tenants/{tenantGuid}/labels", out _UrlParameters)) return RequestTypeEnum.LabelEnumerate;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/labels/{labelGuid}", out _UrlParameters)) return RequestTypeEnum.LabelRead;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/tags/all", out _UrlParameters)) return RequestTypeEnum.TagReadAllInTenant;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/tags/all", out _UrlParameters)) return RequestTypeEnum.TagReadAllInGraph;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/tags", out _UrlParameters)) return RequestTypeEnum.TagReadManyGraph;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/tags", out _UrlParameters)) return RequestTypeEnum.TagReadManyNode;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}/tags", out _UrlParameters)) return RequestTypeEnum.TagReadManyEdge;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/tags", out _UrlParameters)) return RequestTypeEnum.TagReadAll;
                    if (matcher.Match("/v2.0/tenants/{tenantGuid}/tags", out _UrlParameters)) return RequestTypeEnum.TagEnumerate;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/tags/{tagGuid}", out _UrlParameters)) return RequestTypeEnum.TagRead;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/vectors/all", out _UrlParameters)) return RequestTypeEnum.VectorReadAllInTenant;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectors/all", out _UrlParameters)) return RequestTypeEnum.VectorReadAllInGraph;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectors", out _UrlParameters)) return RequestTypeEnum.VectorReadManyGraph;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/vectors", out _UrlParameters)) return RequestTypeEnum.VectorReadManyNode;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}/vectors", out _UrlParameters)) return RequestTypeEnum.VectorReadManyEdge;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/vectors", out _UrlParameters)) return RequestTypeEnum.VectorReadAll;
                    if (matcher.Match("/v2.0/tenants/{tenantGuid}/vectors", out _UrlParameters)) return RequestTypeEnum.VectorEnumerate;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/vectors/{vectorGuid}", out _UrlParameters)) return RequestTypeEnum.VectorRead;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/all", out _UrlParameters)) return RequestTypeEnum.GraphReadAllInTenant;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs", out _UrlParameters)) return RequestTypeEnum.GraphReadAll;
                    if (matcher.Match("/v2.0/tenants/{tenantGuid}/graphs", out _UrlParameters)) return RequestTypeEnum.GraphEnumerate;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/stats", out _UrlParameters)) return RequestTypeEnum.GraphStatistics;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}", out _UrlParameters)) return RequestTypeEnum.GraphRead;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/stats", out _UrlParameters)) return RequestTypeEnum.GraphStatistics;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/export/gexf", out _UrlParameters)) return RequestTypeEnum.GraphExport;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/config", out _UrlParameters)) return RequestTypeEnum.GraphVectorIndexConfig;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/stats", out _UrlParameters)) return RequestTypeEnum.GraphVectorIndexStats;
                    if (matcher.Match("/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/config", out _UrlParameters)) return RequestTypeEnum.GraphVectorIndexConfig;
                    if (matcher.Match("/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/stats", out _UrlParameters)) return RequestTypeEnum.GraphVectorIndexStats;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/nodes/all", out _UrlParameters)) return RequestTypeEnum.NodeReadAllInTenant;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/all", out _UrlParameters)) return RequestTypeEnum.NodeReadAllInGraph;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/mostconnected", out _UrlParameters)) return RequestTypeEnum.NodeReadMostConnected;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/leastconnected", out _UrlParameters)) return RequestTypeEnum.NodeReadLeastConnected;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes", out _UrlParameters)) return RequestTypeEnum.NodeReadAll;
                    if (matcher.Match("/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes", out _UrlParameters)) return RequestTypeEnum.NodeEnumerate;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}", out _UrlParameters)) return RequestTypeEnum.NodeRead;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/edges/from", out _UrlParameters)) return RequestTypeEnum.EdgesFromNode;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/edges/to", out _UrlParameters)) return RequestTypeEnum.EdgesToNode;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/edges", out _UrlParameters)) return RequestTypeEnum.AllEdgesToNode;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/neighbors", out _UrlParameters)) return RequestTypeEnum.NodeNeighbors;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/parents", out _UrlParameters)) return RequestTypeEnum.NodeParents;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/children", out _UrlParameters)) return RequestTypeEnum.NodeChildren;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/subgraph", out _UrlParameters)) return RequestTypeEnum.GraphSubgraph;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/subgraph/stats", out _UrlParameters)) return RequestTypeEnum.GraphSubgraphStatistics;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/edges/all", out _UrlParameters)) return RequestTypeEnum.EdgeReadAllInTenant;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/all", out _UrlParameters)) return RequestTypeEnum.EdgeReadAllInGraph;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges", out _UrlParameters)) return RequestTypeEnum.EdgeReadMany;
                    if (matcher.Match("/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges", out _UrlParameters)) return RequestTypeEnum.EdgeEnumerate;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/between", out _UrlParameters)) return RequestTypeEnum.EdgeBetween;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}", out _UrlParameters)) return RequestTypeEnum.EdgeReadMany;

                    #endregion
                }
                else if (_HttpMethod == HttpMethod.HEAD)
                {
                    #region HEAD

                    if (matcher.Match("/", out _UrlParameters)) return RequestTypeEnum.Loopback;

                    if (matcher.Match("/v1.0/backups/{backupFilename}", out _UrlParameters)) return RequestTypeEnum.BackupExists;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}", out _UrlParameters)) return RequestTypeEnum.TenantExists;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/users/{userGuid}", out _UrlParameters)) return RequestTypeEnum.UserExists;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}", out _UrlParameters)) return RequestTypeEnum.CredentialExists;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/labels/{labelGuid}", out _UrlParameters)) return RequestTypeEnum.LabelExists;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/tags/{tagGuid}", out _UrlParameters)) return RequestTypeEnum.TagExists;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/vectors/{vectorGuid}", out _UrlParameters)) return RequestTypeEnum.VectorExists;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}", out _UrlParameters)) return RequestTypeEnum.GraphExists;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}", out _UrlParameters)) return RequestTypeEnum.NodeExists;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}", out _UrlParameters)) return RequestTypeEnum.EdgeExists;

                    #endregion
                }
                else if (_HttpMethod == HttpMethod.PUT)
                {
                    #region PUT

                    if (matcher.Match("/v1.0/tenants", out _UrlParameters)) return RequestTypeEnum.TenantCreate;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}", out _UrlParameters)) return RequestTypeEnum.TenantUpdate;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/users", out _UrlParameters)) return RequestTypeEnum.UserCreate;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/users/{userGuid}", out _UrlParameters)) return RequestTypeEnum.UserUpdate;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/credentials", out _UrlParameters)) return RequestTypeEnum.CredentialCreate;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}", out _UrlParameters)) return RequestTypeEnum.CredentialUpdate;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/labels", out _UrlParameters)) return RequestTypeEnum.LabelCreate;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/labels/bulk", out _UrlParameters)) return RequestTypeEnum.LabelCreateMany;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/labels/{labelGuid}", out _UrlParameters)) return RequestTypeEnum.LabelUpdate;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/tags", out _UrlParameters)) return RequestTypeEnum.TagCreate;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/tags/bulk", out _UrlParameters)) return RequestTypeEnum.TagCreateMany;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/tags/{tagGuid}", out _UrlParameters)) return RequestTypeEnum.TagUpdate;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/vectors", out _UrlParameters)) return RequestTypeEnum.VectorCreate;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/vectors/bulk", out _UrlParameters)) return RequestTypeEnum.VectorCreateMany;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/vectors/{vectorGuid}", out _UrlParameters)) return RequestTypeEnum.VectorUpdate;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs", out _UrlParameters)) return RequestTypeEnum.GraphCreate;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}", out _UrlParameters)) return RequestTypeEnum.GraphUpdate;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/enable", out _UrlParameters)) return RequestTypeEnum.GraphVectorIndexEnable;
                    if (matcher.Match("/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/enable", out _UrlParameters)) return RequestTypeEnum.GraphVectorIndexEnable;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes", out _UrlParameters)) return RequestTypeEnum.NodeCreate;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/bulk", out _UrlParameters)) return RequestTypeEnum.NodeCreateMany;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}", out _UrlParameters)) return RequestTypeEnum.NodeUpdate;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges", out _UrlParameters)) return RequestTypeEnum.EdgeCreate;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/bulk", out _UrlParameters)) return RequestTypeEnum.EdgeCreateMany;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}", out _UrlParameters)) return RequestTypeEnum.EdgeUpdate;

                    #endregion
                }
                else if (_HttpMethod == HttpMethod.POST)
                {
                    #region POST

                    if (matcher.Match("/v1.0/backup", out _UrlParameters)) return RequestTypeEnum.Backup;

                    if (matcher.Match("/v1.0/flush", out _UrlParameters)) return RequestTypeEnum.FlushDatabase;

                    if (matcher.Match("/v2.0/tenants", out _UrlParameters)) return RequestTypeEnum.TenantEnumerate;
                    if (matcher.Match("/v2.0/tenants/{tenantGuid}/graphs", out _UrlParameters)) return RequestTypeEnum.GraphEnumerate;
                    if (matcher.Match("/v2.0/tenants/{tenantGuid}/users", out _UrlParameters)) return RequestTypeEnum.UserEnumerate;
                    if (matcher.Match("/v2.0/tenants/{tenantGuid}/credentials", out _UrlParameters)) return RequestTypeEnum.CredentialEnumerate;
                    if (matcher.Match("/v2.0/tenants/{tenantGuid}/labels", out _UrlParameters)) return RequestTypeEnum.LabelEnumerate;
                    if (matcher.Match("/v2.0/tenants/{tenantGuid}/tags", out _UrlParameters)) return RequestTypeEnum.TagEnumerate;
                    if (matcher.Match("/v2.0/tenants/{tenantGuid}/vectors", out _UrlParameters)) return RequestTypeEnum.VectorEnumerate;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/first", out _UrlParameters)) return RequestTypeEnum.GraphReadFirst;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/search", out _UrlParameters)) return RequestTypeEnum.GraphSearch;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/existence", out _UrlParameters)) return RequestTypeEnum.GraphExistence;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/rebuild", out _UrlParameters)) return RequestTypeEnum.GraphVectorIndexRebuild;
                    if (matcher.Match("/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/rebuild", out _UrlParameters)) return RequestTypeEnum.GraphVectorIndexRebuild;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/routes", out _UrlParameters)) return RequestTypeEnum.GetRoutes;

                    if (matcher.Match("/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes", out _UrlParameters)) return RequestTypeEnum.NodeEnumerate;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/first", out _UrlParameters)) return RequestTypeEnum.NodeReadFirst;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/search", out _UrlParameters)) return RequestTypeEnum.NodeSearch;

                    if (matcher.Match("/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges", out _UrlParameters)) return RequestTypeEnum.EdgeEnumerate;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/first", out _UrlParameters)) return RequestTypeEnum.EdgeReadAll;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/search", out _UrlParameters)) return RequestTypeEnum.EdgeSearch;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/vectors", out _UrlParameters)) return RequestTypeEnum.VectorSearch;

                    #endregion
                }
                else if (_HttpMethod == HttpMethod.DELETE)
                {
                    #region DELETE

                    if (matcher.Match("/v1.0/backups/{backupFilename}", out _UrlParameters)) return RequestTypeEnum.BackupDelete;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}", out _UrlParameters)) return RequestTypeEnum.TenantDelete;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/users/{userGuid}/credentials", out _UrlParameters)) return RequestTypeEnum.CredentialDeleteByUser;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/credentials/{credentialGuid}", out _UrlParameters)) return RequestTypeEnum.CredentialDelete;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/credentials", out _UrlParameters)) return RequestTypeEnum.CredentialDeleteAllInTenant;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/users/{userGuid}", out _UrlParameters)) return RequestTypeEnum.UserDelete;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/labels/all", out _UrlParameters)) return RequestTypeEnum.LabelDeleteAllInTenant;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/labels/all", out _UrlParameters)) return RequestTypeEnum.LabelDeleteAllInGraph;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/labels", out _UrlParameters)) return RequestTypeEnum.LabelDeleteGraphLabels;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/labels", out _UrlParameters)) return RequestTypeEnum.LabelDeleteNodeLabels;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}/labels", out _UrlParameters)) return RequestTypeEnum.LabelDeleteEdgeLabels;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/labels/bulk", out _UrlParameters)) return RequestTypeEnum.LabelDeleteMany;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/labels/{labelGuid}", out _UrlParameters)) return RequestTypeEnum.LabelDelete;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/tags/all", out _UrlParameters)) return RequestTypeEnum.TagDeleteAllInTenant;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/tags/all", out _UrlParameters)) return RequestTypeEnum.TagDeleteAllInGraph;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/tags", out _UrlParameters)) return RequestTypeEnum.TagDeleteGraphTags;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/tags", out _UrlParameters)) return RequestTypeEnum.TagDeleteNodeTags;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}/tags", out _UrlParameters)) return RequestTypeEnum.TagDeleteEdgeTags;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/tags/bulk", out _UrlParameters)) return RequestTypeEnum.TagDeleteMany;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/tags/{tagGuid}", out _UrlParameters)) return RequestTypeEnum.TagDelete;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/vectors/all", out _UrlParameters)) return RequestTypeEnum.VectorDeleteAllInTenant;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectors/all", out _UrlParameters)) return RequestTypeEnum.VectorDeleteAllInGraph;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectors", out _UrlParameters)) return RequestTypeEnum.VectorDeleteGraphVectors;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/vectors", out _UrlParameters)) return RequestTypeEnum.VectorDeleteNodeVectors;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}/vectors", out _UrlParameters)) return RequestTypeEnum.VectorDeleteEdgeVectors;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/vectors/bulk", out _UrlParameters)) return RequestTypeEnum.VectorDeleteMany;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/vectors/{vectorGuid}", out _UrlParameters)) return RequestTypeEnum.VectorDelete;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/all", out _UrlParameters)) return RequestTypeEnum.GraphDeleteAllInTenant;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}", out _UrlParameters)) return RequestTypeEnum.GraphDelete;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex", out _UrlParameters)) return RequestTypeEnum.GraphVectorIndexDisable;
                    if (matcher.Match("/v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex", out _UrlParameters)) return RequestTypeEnum.GraphVectorIndexDisable;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/nodes/all", out _UrlParameters)) return RequestTypeEnum.NodeDeleteAllInTenant;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/all", out _UrlParameters)) return RequestTypeEnum.NodeDeleteAll;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/bulk", out _UrlParameters)) return RequestTypeEnum.NodeDeleteMany;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}", out _UrlParameters)) return RequestTypeEnum.NodeDelete;

                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/edges/all", out _UrlParameters)) return RequestTypeEnum.EdgeDeleteAllInTenant;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/all", out _UrlParameters)) return RequestTypeEnum.EdgeDeleteAll;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/bulk", out _UrlParameters)) return RequestTypeEnum.EdgeDeleteMany;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}", out _UrlParameters)) return RequestTypeEnum.EdgeDelete;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/edges", out _UrlParameters)) return RequestTypeEnum.EdgeDeleteNodeEdges;
                    if (matcher.Match("/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/edges/bulk", out _UrlParameters)) return RequestTypeEnum.EdgeDeleteNodeEdgesMany;
                  
                    #endregion
                }

                return RequestTypeEnum.Unknown;
            }
            finally
            {
                // Console.WriteLine("URL parameters:" + Environment.NewLine + _Serializer.SerializeJson(_UrlParameters, true));
            }
        }

        private ApiVersionEnum VersionStringToApiVersion(string str)
        {
            if (String.IsNullOrEmpty(str)) str = "";
            str = str.ToLower().Trim();
            if (str.Equals("v1.0")) return ApiVersionEnum.V1_0;
            return ApiVersionEnum.Unknown;
        }

        private void Log(string msg)
        {
            if (String.IsNullOrEmpty(msg) && Logger != null)
                Logger(_Header + msg);
        }

        private NameValueCollection NormalizeNameValueCollection(NameValueCollection nvc)
        {
            NameValueCollection ret = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);

            if (nvc != null)
            {
                foreach (string key in nvc.AllKeys)
                {
                    ret.Add(key, nvc[key]);
                }
            }

            return ret;
        }

        #endregion
    }
}
