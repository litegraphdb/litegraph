namespace LiteGraph.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Sdk.Implementations;
    using LiteGraph.Sdk.Interfaces;
    using RestWrapper;

    /// <summary>
    /// LiteGraph SDK.
    /// </summary>
    public class LiteGraphSdk : SdkBase
    {
        #region Public-Members

        /// <summary>
        /// Admin methods.
        /// </summary>
        public IAdminMethods Admin { get; }

        /// <summary>
        /// Batch methods.
        /// </summary>
        public IBatchMethods Batch { get; }
        
        /// <summary>
        /// Chat methods.
        /// </summary>
        public IChatMethods Chat { get; }

        /// <summary>
        /// Credential methods.
        /// </summary>
        public ICredentialMethods Credential { get; }

        /// <summary>
        /// Edge methods.
        /// </summary>
        public IEdgeMethods Edge { get; }

        /// <summary>
        /// Graph methods.
        /// </summary>
        public IGraphMethods Graph { get; }

        /// <summary>
        /// Label methods.
        /// </summary>
        public ILabelMethods Label { get; }

        /// <summary>
        /// Node methods.
        /// </summary>
        public INodeMethods Node { get; }

        /// <summary>
        /// Tag methods.
        /// </summary>
        public ITagMethods Tag { get; }

        /// <summary>
        /// Tenant methods.
        /// </summary>
        public ITenantMethods Tenant { get; }

        /// <summary>
        /// Graph transaction methods.
        /// </summary>
        public ITransactionMethods Transaction { get; }

        /// <summary>
        /// User methods.
        /// </summary>
        public IUserMethods User { get; }

        /// <summary>
        /// Vector methods.
        /// </summary>
        public IVectorMethods Vector { get; }

        /// <summary>
        /// Graph algorithm methods.
        /// </summary>
        public IAlgorithmMethods Algorithm { get; }

        /// <summary>
        /// User authentication methods.
        /// </summary>
        public IUserAuthentication UserAuthentication { get; }

        /// <summary>
        /// User email
        /// </summary>
        public string Email { get; private set; }

        /// <summary>
        /// User password
        /// </summary>
        public string Password { get; private set; }

        /// <summary>
        /// Tenant GUID
        /// </summary>
        public Guid? TenantGuid { get; private set; }

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="endpoint">Endpoint URL.</param>
        /// <param name="bearerToken">Bearer token.</param>
        public LiteGraphSdk(
            string endpoint = "http://localhost:8701/",
            string bearerToken = "default") : base(endpoint, bearerToken)
        {
            Admin = new AdminMethods(this);
            Batch = new BatchMethods(this);
            Chat = new ChatMethods(this);
            Credential = new CredentialMethods(this);
            Edge = new EdgeMethods(this);
            Graph = new GraphMethods(this);
            Label = new LabelMethods(this);
            Node = new NodeMethods(this);
            Tag = new TagMethods(this);
            Tenant = new TenantMethods(this);
            Transaction = new TransactionMethods(this);
            User = new UserMethods(this);
            Vector = new VectorMethods(this);
            Algorithm = new AlgorithmMethods(this);
        }

        /// <summary>
        /// Instantiate with token-based authentication.
        /// This constructor will authenticate using email and password, generate a token, and use it for subsequent requests.
        /// </summary>
        /// <param name="email">User email.</param>
        /// <param name="password">User password.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="endpoint">Endpoint URL.</param>
        /// <param name="bearerToken">Bearer token.</param>
        public LiteGraphSdk(
            string email,
            string password,
            Guid tenantGuid,
            string endpoint = "http://localhost:8701",
            string bearerToken = null) : base(endpoint, bearerToken ?? "default")
        {
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            if (String.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));

            Email = email;
            Password = password;
            TenantGuid = tenantGuid;

            Admin = new AdminMethods(this);
            Batch = new BatchMethods(this);
            Chat = new ChatMethods(this);
            Credential = new CredentialMethods(this);
            Edge = new EdgeMethods(this);
            Graph = new GraphMethods(this);
            Label = new LabelMethods(this);
            Node = new NodeMethods(this);
            Tag = new TagMethods(this);
            Tenant = new TenantMethods(this);
            Transaction = new TransactionMethods(this);
            User = new UserMethods(this);
            Vector = new VectorMethods(this);
            Algorithm = new AlgorithmMethods(this);
            UserAuthentication = new UserAuthentication(this);
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Liveness check (GET /v1.0/health/live).  Needs no authentication.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Health response, or null if the server did not answer.</returns>
        public async Task<HealthResponse> HealthLive(CancellationToken token = default)
        {
            return await ReadHealth("v1.0/health/live", token).ConfigureAwait(false);
        }

        /// <summary>
        /// Readiness check (GET /v1.0/health/ready).  Needs no authentication.  Returns the body for both 200 and 503, so a
        /// node that is not ready reports why (see HealthResponse.Checks); a 503 is not retried.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Health response, or null if the server did not answer.</returns>
        public async Task<HealthResponse> HealthReady(CancellationToken token = default)
        {
            return await ReadHealth("v1.0/health/ready", token).ConfigureAwait(false);
        }

        /// <summary>
        /// Get tenants for an email address.
        /// This static method can be used to retrieve all tenants associated with an email before instantiating the SDK.
        /// </summary>
        /// <param name="email">Email address.</param>
        /// <param name="endpoint">Endpoint URL. Defaults to http://localhost:8701</param>
        /// <returns>Enumeration result containing tenant metadata.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the email or endpoint is null or empty.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the server returns a non-success status code or no response.</exception>
        public static EnumerationResult<TenantMetadata> GetTenantsForEmail(string email, string endpoint = "http://localhost:8701")
        {
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            if (String.IsNullOrEmpty(endpoint)) throw new ArgumentNullException(nameof(endpoint));

            if (!endpoint.EndsWith("/")) endpoint += "/";

            string url = endpoint + "v1.0/token/tenants";

            using (RestRequest req = new RestRequest(url))
            {
                req.TimeoutMilliseconds = 300000;
                req.Headers.Add("x-email", email);

                using (RestResponse resp = req.SendAsync(CancellationToken.None).GetAwaiter().GetResult())
                {
                    if (resp != null)
                    {
                        if (resp.StatusCode >= 200 && resp.StatusCode <= 299)
                        {
                            if (!String.IsNullOrEmpty(resp.DataAsString))
                            {
                                return Serializer.DeserializeJson<EnumerationResult<TenantMetadata>>(resp.DataAsString);
                            }
                            else
                            {
                                return new EnumerationResult<TenantMetadata>();
                            }
                        }
                        else
                        {
                            throw new InvalidOperationException($"Failed to retrieve tenants for email. Status code: {resp.StatusCode}");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("No response received when retrieving tenants for email.");
                    }
                }
            }
        }

        #endregion

        #region Private-Methods

        private async Task<HealthResponse> ReadHealth(string path, CancellationToken token)
        {
            using (SdkExchange exchange = await SendAsync(Endpoint + path, HttpMethod.Get, null, null, null, false, false, token).ConfigureAwait(false))
            {
                RestResponse resp = exchange.Response;
                if (resp == null || String.IsNullOrEmpty(resp.DataAsString)) return null;
                return Serializer.DeserializeJson<HealthResponse>(resp.DataAsString);
            }
        }

        #endregion
    }
}
