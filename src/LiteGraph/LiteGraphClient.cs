namespace LiteGraph
{
    using System;
    using ExpressionTree;
    using System.Collections.Generic;
    using System.Threading;
    using System.Linq;
    using LiteGraph.Gexf;
    using LiteGraph.GraphRepositories;
    using LiteGraph.Serialization;
    using System.Xml.Linq;
    using System.Collections.Specialized;
    using System.Reflection.Metadata.Ecma335;
    using SQLitePCL;

    /// <summary>
    /// LiteGraph client.
    /// </summary>
    public class LiteGraphClient : IDisposable
    {
        #region Public-Members

        /// <summary>
        /// Logging settings.
        /// </summary>
        public LoggingSettings Logging
        {
            get
            {
                return _Repository.Logging;
            }
            set
            {
                if (value == null) value = new LoggingSettings();
                _Repository.Logging = value;
            }
        }

        /// <summary>
        /// Serialization helper.
        /// </summary>
        public SerializationHelper Serializer
        {
            get
            {
                return _Serializer;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Serializer));
                _Serializer = value;
            }
        }

        #endregion

        #region Private-Members

        private bool _Disposed = false;
        private GraphRepositoryBase _Repository = new SqliteGraphRepository();
        private SerializationHelper _Serializer = new SerializationHelper();
        private GexfWriter _Gexf = new GexfWriter();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate LiteGraph client.
        /// </summary>
        /// <param name="repo">Graph repository driver.</param>
        /// <param name="logging">Logging.</param>
        public LiteGraphClient(
            GraphRepositoryBase repo = null,
            LoggingSettings logging = null)
        {
            if (repo != null) _Repository = repo;

            if (logging != null) Logging = logging;
            else Logging = new LoggingSettings();
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Tear down the client and dispose of resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Initialize the repository.
        /// </summary>
        public void InitializeRepository()
        {
            _Repository.InitializeRepository();
        }

        /// <summary>
        /// Convert data associated with a graph, node, or edge to a specific type.
        /// </summary>
        /// <typeparam name="T">Type.</typeparam>
        /// <param name="data">Data.</param>
        /// <returns>Instance.</returns>
        public T ConvertData<T>(object data) where T : class, new()
        {
            if (data == null) return null;
            return _Serializer.DeserializeJson<T>(data.ToString());
        }

        #region Tenants

        /// <summary>
        /// Create a tenant.
        /// </summary>
        /// <param name="tenant">Tenant.</param>
        /// <returns>Tenant.</returns>
        public TenantMetadata CreateTenant(TenantMetadata tenant)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));

            TenantMetadata existing = _Repository.ReadTenant(tenant.GUID);
            if (existing != null) return existing;

            TenantMetadata created = _Repository.CreateTenant(tenant);
            Logging.Log(SeverityEnum.Info, "created tenant name " + created.Name + " GUID " + created.GUID);
            return created;
        }

        /// <summary>
        /// Create a tenant using a unique name.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="name">Unique name.</param>
        /// <returns>Tenant.</returns>
        public TenantMetadata CreateTenant(Guid guid, string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return CreateTenant(new TenantMetadata { GUID = guid, Name = name });
        }

        /// <summary>
        /// Read tenants.
        /// </summary>
        /// <param name="order">Enumeration order.</param>
        /// <returns>Tenants.</returns>
        public IEnumerable<TenantMetadata> ReadTenants(EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            Logging.Log(SeverityEnum.Debug, "retrieving tenants");

            foreach (TenantMetadata tenant in _Repository.ReadTenants(order))
            {
                yield return tenant;
            }
        }

        /// <summary>
        /// Read a tenant by GUID.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <returns>Tenant.</returns>
        public TenantMetadata ReadTenant(Guid guid)
        {
            Logging.Log(SeverityEnum.Debug, "retrieving tenant with GUID " + guid);

            return _Repository.ReadTenant(guid);
        }

        /// <summary>
        /// Update a tenant.
        /// </summary>
        /// <param name="tenant">Tenant.</param>
        /// <returns>Tenant.</returns>
        public TenantMetadata UpdateTenant(TenantMetadata tenant)
        {
            if (tenant == null) throw new ArgumentNullException(nameof(tenant));

            Logging.Log(SeverityEnum.Debug, "updating tenant with name " + tenant.Name + " GUID " + tenant.GUID);

            return _Repository.UpdateTenant(tenant);
        }

        /// <summary>
        /// Delete a tenant.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <param name="force">True to force deletion of subordinate objects.</param>
        public void DeleteTenant(Guid guid, bool force = false)
        {
            TenantMetadata tenant = ReadTenant(guid);
            if (tenant == null) return;

            Logging.Log(SeverityEnum.Info, "deleting tenant " + tenant.Name + " GUID " + tenant.GUID);

            _Repository.DeleteTenant(guid, force);
        }

        /// <summary>
        /// Check if a tenant exists by GUID.
        /// </summary>
        /// <param name="guid">GUID.</param>
        /// <returns>True if exists.</returns>
        public bool ExistsTenant(Guid guid)
        {
            return _Repository.ExistsTenant(guid);
        }

        #endregion

        #region Users

        /// <summary>
        /// Create a user.
        /// </summary>
        /// <param name="user">User.</param>
        /// <returns>User.</returns>
        public UserMaster CreateUser(UserMaster user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            UserMaster existing = _Repository.ReadUser(user.TenantGUID, user.GUID);
            if (existing != null) return existing;

            UserMaster created = _Repository.CreateUser(user);
            Logging.Log(SeverityEnum.Info, "created user email " + created.Email + " GUID " + created.GUID);
            return created;
        }

        /// <summary>
        /// Create a user using a unique name.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="firstName">First name.</param>
        /// <param name="lastName">Last name.</param>
        /// <param name="email">Email.</param>
        /// <param name="password">Password.</param>
        /// <returns>User.</returns>
        public UserMaster CreateUser(
            Guid tenantGuid, 
            Guid guid, 
            string firstName, 
            string lastName, 
            string email, 
            string password)
        {
            if (String.IsNullOrEmpty(firstName)) throw new ArgumentNullException(nameof(firstName));
            if (String.IsNullOrEmpty(lastName)) throw new ArgumentNullException(nameof(lastName));
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            if (String.IsNullOrEmpty(password)) throw new ArgumentNullException(nameof(password));
            return CreateUser(new UserMaster { GUID = guid, TenantGUID = tenantGuid, FirstName = firstName, LastName = lastName, Email = email, Password = password });
        }

        /// <summary>
        /// Read users.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="email">Email.</param>
        /// <param name="order">Enumeration order.</param>
        /// <returns>Users.</returns>
        public IEnumerable<UserMaster> ReadUsers(
            Guid tenantGuid, 
            string email = null, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            Logging.Log(SeverityEnum.Debug, "retrieving users");

            foreach (UserMaster user in _Repository.ReadUsers(tenantGuid, email, order))
            {
                yield return user;
            }
        }

        /// <summary>
        /// Read a user by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <returns>User.</returns>
        public UserMaster ReadUser(Guid tenantGuid, Guid guid)
        {
            Logging.Log(SeverityEnum.Debug, "retrieving user with GUID " + guid);

            return _Repository.ReadUser(tenantGuid, guid);
        }

        /// <summary>
        /// Read tenants associated with a given email.
        /// </summary>
        /// <param name="email">Email.</param>
        /// <returns>List of tenants.</returns>
        public List<TenantMetadata> ReadUserTenants(string email)
        {
            if (String.IsNullOrEmpty(email)) throw new ArgumentNullException(nameof(email));
            Logging.Log(SeverityEnum.Debug, "retrieving tenants associated with user " + email);
            return _Repository.ReadUserTenants(email);
        }

        /// <summary>
        /// Update a user.
        /// </summary>
        /// <param name="user">User.</param>
        /// <returns>User.</returns>
        public UserMaster UpdateUser(UserMaster user)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            Logging.Log(SeverityEnum.Debug, "updating user with email " + user.Email + " GUID " + user.GUID);

            return _Repository.UpdateUser(user);
        }

        /// <summary>
        /// Delete a user.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        public void DeleteUser(Guid tenantGuid, Guid guid)
        {
            UserMaster user = ReadUser(tenantGuid, guid);
            if (user == null) return;

            Logging.Log(SeverityEnum.Info, "deleting user with email " + user.Email + " GUID " + user.GUID);

            _Repository.DeleteUser(tenantGuid, guid);
            _Repository.DeleteUserCredentials(tenantGuid, guid);
        }

        /// <summary>
        /// Check if a user exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <returns>True if exists.</returns>
        public bool ExistsUser(Guid tenantGuid, Guid guid)
        {
            return _Repository.ExistsUser(tenantGuid, guid);
        }

        #endregion

        #region Credentials

        /// <summary>
        /// Create a credential.
        /// </summary>
        /// <param name="cred">Credential.</param>
        /// <returns>Credential.</returns>
        public Credential CreateCredential(Credential cred)
        {
            if (cred == null) throw new ArgumentNullException(nameof(cred));

            Credential existing = _Repository.ReadCredential(cred.TenantGUID, cred.GUID);
            if (existing != null) return existing;

            Credential created = _Repository.CreateCredential(cred);
            Logging.Log(SeverityEnum.Info, "created credential " + created.GUID);
            return created;
        }

        /// <summary>
        /// Create a credential using a unique name.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="userGuid">User GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="name">Name.</param>
        /// <returns>Credential.</returns>
        public Credential CreateCredential(Guid tenantGuid, Guid userGuid, Guid guid, string name)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));
            return CreateCredential(new Credential { GUID = guid, TenantGUID = tenantGuid, UserGUID = userGuid, Name = name });
        }

        /// <summary>
        /// Read credentials.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="userGuid">User GUID.</param>
        /// <param name="bearerToken">Bearer token.</param>
        /// <param name="order">Enumeration order.</param>
        /// <returns>Credentials.</returns>
        public IEnumerable<Credential> ReadCredentials(
            Guid? tenantGuid, 
            Guid? userGuid = null, 
            string bearerToken = null, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            Logging.Log(SeverityEnum.Debug, "retrieving credentials");

            foreach (Credential credential in _Repository.ReadCredentials(tenantGuid, userGuid, bearerToken, order))
            {
                yield return credential;
            }
        }

        /// <summary>
        /// Read a credential by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <returns>Credential.</returns>
        public Credential ReadCredential(Guid tenantGuid, Guid guid)
        {
            Logging.Log(SeverityEnum.Debug, "retrieving credential with GUID " + guid);

            return _Repository.ReadCredential(tenantGuid, guid);
        }

        /// <summary>
        /// Update a credential.
        /// </summary>
        /// <param name="credential">Credential.</param>
        /// <returns>Credential.</returns>
        public Credential UpdateCredential(Credential credential)
        {
            if (credential == null) throw new ArgumentNullException(nameof(credential));

            Logging.Log(SeverityEnum.Debug, "updating credential " + credential.Name + " GUID " + credential.GUID);

            return _Repository.UpdateCredential(credential);
        }

        /// <summary>
        /// Delete a credential.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        public void DeleteCredential(Guid tenantGuid, Guid guid)
        {
            Credential credential = ReadCredential(tenantGuid, guid);
            if (credential == null) return;

            Logging.Log(SeverityEnum.Info, "deleting credential " + credential.Name + " GUID " + credential.GUID);

            _Repository.DeleteCredential(tenantGuid, guid);
        }

        /// <summary>
        /// Check if a credential exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <returns>True if exists.</returns>
        public bool ExistsCredential(Guid tenantGuid, Guid guid)
        {
            return _Repository.ExistsCredential(tenantGuid, guid);
        }

        #endregion

        #region Labels

        /// <summary>
        /// Create a label.
        /// </summary>
        /// <param name="label">Label.</param>
        /// <returns>Label.</returns>
        public LabelMetadata CreateLabel(LabelMetadata label)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));
            
            LabelMetadata existing = _Repository.ReadLabel(label.TenantGUID, label.GUID);
            if (existing != null) return existing;

            LabelMetadata created = _Repository.CreateLabel(label);
            Logging.Log(SeverityEnum.Info, "created label " + created.GUID);
            return created;
        }

        /// <summary>
        /// Create a label using a unique name.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <param name="label">Label.</param>
        /// <returns>Label.</returns>
        public LabelMetadata CreateLabel(Guid tenantGuid, Guid graphGuid, Guid? nodeGuid, Guid? edgeGuid, string label)
        {
            if (String.IsNullOrEmpty(label)) throw new ArgumentNullException(nameof(label));
            return CreateLabel(new LabelMetadata { 
                GUID = Guid.NewGuid(), 
                TenantGUID = tenantGuid, 
                GraphGUID = graphGuid, 
                NodeGUID = nodeGuid, 
                EdgeGUID = edgeGuid, 
                Label = label 
            });
        }

        /// <summary>
        /// Read labels.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <param name="label">Label.</param>
        /// <param name="order">Enumeration order.</param>
        /// <returns>Labels.</returns>
        public IEnumerable<LabelMetadata> ReadLabels(
            Guid tenantGuid, 
            Guid? graphGuid, 
            Guid? nodeGuid, 
            Guid? edgeGuid, 
            string label,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            Logging.Log(SeverityEnum.Debug, "retrieving labels");

            foreach (LabelMetadata curr in _Repository.ReadLabels(tenantGuid, graphGuid, nodeGuid, edgeGuid, label, order))
            {
                yield return curr;
            }
        }

        /// <summary>
        /// Read a label by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <returns>Label.</returns>
        public LabelMetadata ReadLabel(Guid tenantGuid, Guid guid)
        {
            Logging.Log(SeverityEnum.Debug, "retrieving label with GUID " + guid);

            return _Repository.ReadLabel(tenantGuid, guid);
        }

        /// <summary>
        /// Update a label.
        /// </summary>
        /// <param name="label">LabelMetadata.</param>
        /// <returns>Label.</returns>
        public LabelMetadata UpdateLabel(LabelMetadata label)
        {
            if (label == null) throw new ArgumentNullException(nameof(label));

            Logging.Log(SeverityEnum.Debug, "updating label " + label.Label + " in GUID " + label.GUID);

            return _Repository.UpdateLabel(label);
        }

        /// <summary>
        /// Delete a label.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        public void DeleteLabel(Guid tenantGuid, Guid guid)
        {
            LabelMetadata label = ReadLabel(tenantGuid, guid);
            if (label == null) return;

            Logging.Log(SeverityEnum.Info, "deleting label " + label.Label + " in GUID " + label.GUID);

            _Repository.DeleteLabel(tenantGuid, guid);
        }

        /// <summary>
        /// Check if a label exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <returns>True if exists.</returns>
        public bool ExistsLabelMetadata(Guid tenantGuid, Guid guid)
        {
            return _Repository.ExistsLabel(tenantGuid, guid);
        }

        #endregion

        #region Tags

        /// <summary>
        /// Create a tag.
        /// </summary>
        /// <param name="tag">Tag.</param>
        /// <returns>Tag.</returns>
        public TagMetadata CreateTag(TagMetadata tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));

            TagMetadata existing = _Repository.ReadTag(tag.TenantGUID, tag.GUID);
            if (existing != null) return existing;

            TagMetadata created = _Repository.CreateTag(tag);
            Logging.Log(SeverityEnum.Info, "created tag " + created.GUID);
            return created;
        }

        /// <summary>
        /// Create a tag using a unique name.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <param name="key">Key.</param>
        /// <param name="val">Value.</param>
        /// <returns>Tag.</returns>
        public TagMetadata CreateTag(Guid tenantGuid, Guid graphGuid, Guid? nodeGuid, Guid? edgeGuid, string key, string val)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            return CreateTag(new TagMetadata { GUID = Guid.NewGuid(), TenantGUID = tenantGuid, GraphGUID = graphGuid, NodeGUID = nodeGuid, EdgeGUID = edgeGuid, Key = key, Value = val });
        }

        /// <summary>
        /// Read tags.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <param name="key">Key.</param>
        /// <param name="val">Value.</param>
        /// <param name="order">Enumeration order.</param>
        /// <returns>Tags.</returns>
        public IEnumerable<TagMetadata> ReadTags(
            Guid tenantGuid, 
            Guid? graphGuid, 
            Guid? nodeGuid, 
            Guid? edgeGuid, 
            string key, 
            string val, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            Logging.Log(SeverityEnum.Debug, "retrieving tags");

            foreach (TagMetadata tag in _Repository.ReadTags(tenantGuid, graphGuid, nodeGuid, edgeGuid, key, val, order))
            {
                yield return tag;
            }
        }

        /// <summary>
        /// Read a tag by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <returns>Tag.</returns>
        public TagMetadata ReadTag(Guid tenantGuid, Guid guid)
        {
            Logging.Log(SeverityEnum.Debug, "retrieving tag with GUID " + guid);

            return _Repository.ReadTag(tenantGuid, guid);
        }

        /// <summary>
        /// Update a tag.
        /// </summary>
        /// <param name="tag">TagMetadata.</param>
        /// <returns>Tag.</returns>
        public TagMetadata UpdateTag(TagMetadata tag)
        {
            if (tag == null) throw new ArgumentNullException(nameof(tag));

            Logging.Log(SeverityEnum.Debug, "updating tag " + tag.Key + " in GUID " + tag.GUID);

            return _Repository.UpdateTag(tag);
        }

        /// <summary>
        /// Delete a tag.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        public void DeleteTag(Guid tenantGuid, Guid guid)
        {
            TagMetadata tag = ReadTag(tenantGuid, guid);
            if (tag == null) return;

            Logging.Log(SeverityEnum.Info, "deleting tag " + tag.Key + " in GUID " + tag.GUID);

            _Repository.DeleteTag(tenantGuid, guid);
        }

        /// <summary>
        /// Check if a tag exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <returns>True if exists.</returns>
        public bool ExistsTagMetadata(Guid tenantGuid, Guid guid)
        {
            return _Repository.ExistsTag(tenantGuid, guid);
        }

        #endregion

        #region Vectors

        /// <summary>
        /// Create a vector.
        /// </summary>
        /// <param name="vector">Vector.</param>
        /// <returns>Vector.</returns>
        public VectorMetadata CreateVector(VectorMetadata vector)
        {
            if (vector == null) throw new ArgumentNullException(nameof(vector));

            VectorMetadata existing = _Repository.ReadVector(vector.TenantGUID, vector.GUID);
            if (existing != null) return existing;

            VectorMetadata created = _Repository.CreateVector(vector);
            Logging.Log(SeverityEnum.Info, "created vector " + created.GUID);
            return created;
        }

        /// <summary>
        /// Read vectors.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <returns>Vectors.</returns>
        public IEnumerable<VectorMetadata> ReadVectors(
            Guid tenantGuid, 
            Guid? graphGuid, 
            Guid? nodeGuid, 
            Guid? edgeGuid, 
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            Logging.Log(SeverityEnum.Debug, "retrieving vectors");

            IEnumerable<VectorMetadata> vectors;

            if (graphGuid != null) vectors = _Repository.ReadGraphVectors(tenantGuid, graphGuid.Value);
            else if (graphGuid != null && nodeGuid != null && edgeGuid == null) vectors = _Repository.ReadNodeVectors(tenantGuid, graphGuid.Value, nodeGuid.Value);
            else if (graphGuid != null && nodeGuid == null && edgeGuid != null) vectors = _Repository.ReadEdgeVectors(tenantGuid, graphGuid.Value, nodeGuid.Value);
            else vectors = _Repository.ReadVectors(tenantGuid, null, null, null);

            foreach (VectorMetadata vector in vectors)
            {
                yield return vector;
            }
        }

        /// <summary>
        /// Read a vector by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <returns>Vector.</returns>
        public VectorMetadata ReadVector(Guid tenantGuid, Guid guid)
        {
            Logging.Log(SeverityEnum.Debug, "retrieving vector with GUID " + guid);

            return _Repository.ReadVector(tenantGuid, guid);
        }

        /// <summary>
        /// Update a vector.
        /// </summary>
        /// <param name="vector">VectorMetadata.</param>
        /// <returns>Vector.</returns>
        public VectorMetadata UpdateVector(VectorMetadata vector)
        {
            if (vector == null) throw new ArgumentNullException(nameof(vector));

            Logging.Log(SeverityEnum.Debug, "updating vector GUID " + vector.GUID);

            return _Repository.UpdateVector(vector);
        }

        /// <summary>
        /// Delete a vector.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        public void DeleteVector(Guid tenantGuid, Guid guid)
        {
            VectorMetadata vector = ReadVector(tenantGuid, guid);
            if (vector == null) return;

            Logging.Log(SeverityEnum.Info, "deleting vector GUID " + vector.GUID);

            _Repository.DeleteVector(tenantGuid, guid);
        }

        /// <summary>
        /// Check if a vector exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <returns>True if exists.</returns>
        public bool ExistsVectorMetadata(Guid tenantGuid, Guid guid)
        {
            return _Repository.ExistsVector(tenantGuid, guid);
        }

        /// <summary>
        /// Search vectors.
        /// </summary>
        /// <param name="searchReq">Vector search request.</param>
        /// <returns>Vector search results.</returns>
        public IEnumerable<VectorSearchResult> SearchVectors(VectorSearchRequest searchReq)
        {
            if (searchReq == null) throw new ArgumentNullException(nameof(searchReq));
            return SearchVectors(
                searchReq.Domain,
                searchReq.SearchType,
                searchReq.Embeddings,
                searchReq.TenantGUID,
                searchReq.GraphGUID,
                searchReq.Labels,
                searchReq.Tags,
                searchReq.Expr);
        }

        /// <summary>
        /// Search vectors.
        /// </summary>
        /// <param name="domain">Vector search domain.</param>
        /// <param name="searchType">Vector search type.</param>
        /// <param name="vectors">Vectors.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="labels">Labels.</param>
        /// <param name="tags">Tags.</param>
        /// <param name="filter">Filter.</param>
        /// <returns></returns>
        public IEnumerable<VectorSearchResult> SearchVectors(
            VectorSearchDomainEnum domain,
            VectorSearchTypeEnum searchType,
            List<float> vectors,
            Guid tenantGuid,
            Guid? graphGuid = null,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr filter = null)
        {
            if (vectors == null || vectors.Count < 1) throw new ArgumentException("The supplied vector list must include at least one value.");
            
            if (domain == VectorSearchDomainEnum.Graph)
            {
                return _Repository.SearchGraphVectors(searchType, vectors, tenantGuid, labels, tags, filter);
            }
            else if (domain == VectorSearchDomainEnum.Node)
            {
                if (graphGuid == null) throw new ArgumentException("Graph GUID must be supplied when performing a node vector search.");
                return _Repository.SearchNodeVectors(searchType, vectors, tenantGuid, graphGuid.Value, labels, tags, filter);
            }
            else if (domain == VectorSearchDomainEnum.Edge)
            {
                if (graphGuid == null) throw new ArgumentException("Graph GUID must be supplied when performing an edge vector search.");
                return _Repository.SearchEdgeVectors(searchType, vectors, tenantGuid, graphGuid.Value, labels, tags, filter);
            }
            else
            {
                throw new ArgumentException("Unknown vector search domain '" + domain.ToString() + "'.");
            }
        }

        #endregion

        #region Graphs

        /// <summary>
        /// Create a graph using a unique name.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="name">Unique name.</param>
        /// <param name="labels">Labels.</param>
        /// <param name="tags">Tags.</param>
        /// <param name="data">Data.</param>
        /// <param name="vectors">Vectors.</param>
        /// <returns>Graph.</returns>
        public Graph CreateGraph(
            Guid tenantGuid, 
            Guid guid, 
            string name, 
            List<string> labels = null, 
            NameValueCollection tags = null, 
            List<VectorMetadata> vectors = null,
            object data = null)
        {
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            Graph existing = _Repository.ReadGraph(tenantGuid, guid);
            if (existing != null) return existing;

            Graph graph = _Repository.CreateGraph(tenantGuid, guid, name, data, labels, tags, vectors);
            Logging.Log(SeverityEnum.Info, "created graph name " + name + " GUID " + graph.GUID);
            return graph;
        }

        /// <summary>
        /// Read graphs.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="labels">Labels on which to filter results.</param>
        /// <param name="tags">Tags on which to filter results.</param>
        /// <param name="expr">
        /// Graph filter expression for Data JSON body.
        /// Expression left terms must follow the form of Sqlite JSON paths.
        /// For example, to retrieve the 'Name' property, use '$.Name', OperatorEnum.Equals, '[name here]'.</param>
        /// <param name="order">Enumeration order.</param>
        /// <returns>Graphs.</returns>
        public IEnumerable<Graph> ReadGraphs(
            Guid tenantGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr expr = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            if (order == EnumerationOrderEnum.CostAscending
                || order == EnumerationOrderEnum.CostDescending)
                throw new ArgumentException("Cost-based enumeration orders are only available to edge APIs.");

            Logging.Log(SeverityEnum.Debug, "retrieving graphs");

            foreach (Graph graph in _Repository.ReadGraphs(tenantGuid, labels, tags, expr, order))
            {
                yield return graph;
            }
        }

        /// <summary>
        /// Read a graph by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <returns>Graph.</returns>
        public Graph ReadGraph(Guid tenantGuid, Guid graphGuid)
        {
            Logging.Log(SeverityEnum.Debug, "retrieving graph with GUID " + graphGuid);

            return _Repository.ReadGraph(tenantGuid, graphGuid);
        }

        /// <summary>
        /// Update a graph.
        /// </summary>
        /// <param name="graph">Graph.</param>
        /// <returns>Graph.</returns>
        public Graph UpdateGraph(Graph graph)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));

            Logging.Log(SeverityEnum.Debug, "updating graph with name " + graph.Name + " GUID " + graph.GUID);

            return _Repository.UpdateGraph(graph);
        }

        /// <summary>
        /// Delete a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">GUID.</param>
        /// <param name="force">True to force deletion of nodes and edges.</param>
        public void DeleteGraph(Guid tenantGuid, Guid graphGuid, bool force = false)
        {
            Graph graph = ReadGraph(tenantGuid, graphGuid);
            if (graph == null) return;

            Logging.Log(SeverityEnum.Info, "deleting graph with name " + graph.Name + " GUID " + graph.GUID);

            if (force)
            {
                Logging.Log(SeverityEnum.Info, "deleting graph edges and nodes for graph GUID " + graph.GUID);

                _Repository.DeleteEdges(tenantGuid, graph.GUID);
                _Repository.DeleteNodes(tenantGuid, graph.GUID);
            }

            if (_Repository.ReadNodes(tenantGuid, graph.GUID).Any())
                throw new InvalidOperationException("The specified graph has dependent nodes or edges.");

            if (_Repository.ReadEdges(tenantGuid, graph.GUID).Any())
                throw new InvalidOperationException("The specified graph has dependent nodes or edges.");

            _Repository.DeleteGraph(tenantGuid, graph.GUID, force);
        }

        /// <summary>
        /// Check if a graph exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <returns>True if exists.</returns>
        public bool ExistsGraph(Guid tenantGuid, Guid guid)
        {
            return _Repository.ExistsGraph(tenantGuid, guid);
        }

        /// <summary>
        /// Export graph to GEXF.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid">GUID.</param>
        /// <param name="filename">Filename.</param>
        /// <param name="includeData">True to include data.</param>
        public void ExportGraphToGexfFile(Guid tenantGuid, Guid guid, string filename, bool includeData = false)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));
            _Gexf.ExportToFile(this, tenantGuid, guid, filename, includeData);
        }

        /// <summary>
        /// Render a graph as GEXF.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="guid"></param>
        /// <param name="includeData"></param>
        /// <returns></returns>
        public string RenderGraphAsGexf(Guid tenantGuid, Guid guid, bool includeData = false)
        {
            return _Gexf.RenderAsGexf(this, tenantGuid, guid, includeData);
        }

        #endregion

        #region Nodes

        /// <summary>
        /// Create a node.
        /// </summary>
        /// <param name="node">Node.</param>
        /// <returns>Node.</returns>
        public Node CreateNode(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            if (!_Repository.ExistsGraph(node.TenantGUID, node.GraphGUID)) throw new ArgumentException("No graph with GUID '" + node.GraphGUID + "' exists.");
            if (_Repository.ExistsNode(node.TenantGUID,node.GraphGUID, node.GUID)) throw new ArgumentException("A node with GUID '" + node.GUID + "' already exists in graph '" + node.GraphGUID + "'.");

            Node existing = _Repository.ReadNode(node.TenantGUID, node.GraphGUID, node.GUID);
            if (existing != null) return existing;

            Node created = _Repository.CreateNode(node);

            Logging.Log(SeverityEnum.Debug, "created node " + created.GUID + " in graph " + created.GraphGUID);

            return created;
        }

        /// <summary>
        /// Create nodes.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodes">Nodes.</param>
        /// <returns>Nodes.</returns>
        public List<Node> CreateNodes(Guid tenantGuid, Guid graphGuid, List<Node> nodes)
        {
            if (nodes == null) throw new ArgumentNullException(nameof(nodes));

            if (!_Repository.ExistsGraph(tenantGuid, graphGuid)) throw new ArgumentException("No graph with GUID '" + graphGuid + "' exists.");

            List<Node> created = _Repository.CreateMultipleNodes(tenantGuid, graphGuid, nodes);
            Logging.Log(SeverityEnum.Debug, "created " + created.Count + " node(s) in graph " + graphGuid);

            return created;
        }

        /// <summary>
        /// Read nodes.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="labels">Labels on which to filter results.</param>
        /// <param name="tags">Tags on which to filter results.</param>
        /// <param name="expr">
        /// Node filter expression for Data JSON body.
        /// Expression left terms must follow the form of Sqlite JSON paths.
        /// For example, to retrieve the 'Name' property, use '$.Name', OperatorEnum.Equals, '[name here]'.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">Number of records to skip.</param>
        /// <returns>Nodes.</returns>
        public IEnumerable<Node> ReadNodes(
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr expr = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            if (order == EnumerationOrderEnum.CostAscending
                || order == EnumerationOrderEnum.CostDescending)
                throw new ArgumentException("Cost-based enumeration orders are only available to edge APIs.");

            foreach (Node node in _Repository.ReadNodes(tenantGuid, graphGuid, labels, tags, expr, order, skip))
            {
                yield return node;
            }
        }

        /// <summary>
        /// Read node.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <returns>Node.</returns>
        public Node ReadNode(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            return _Repository.ReadNode(tenantGuid, graphGuid, nodeGuid);
        }

        /// <summary>
        /// Update node.
        /// </summary>
        /// <param name="node"></param>
        /// <returns>Node.</returns>
        public Node UpdateNode(Node node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));

            if (!_Repository.ExistsGraph(node.TenantGUID, node.GraphGUID)) throw new ArgumentException("No graph with GUID '" + node.GraphGUID + "' exists.");
            if (!_Repository.ExistsNode(node.TenantGUID, node.GraphGUID, node.GUID)) throw new ArgumentException("No node with GUID '" + node.GUID + "' exists in graph '" + node.GraphGUID + "'.");

            Node updated = _Repository.UpdateNode(node);

            Logging.Log(SeverityEnum.Debug, "updated node " + updated.GUID + " in graph " + updated.GraphGUID);

            return updated;
        }

        /// <summary>
        /// Delete a node and all associated edges.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        public void DeleteNode(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            Node node = _Repository.ReadNode(tenantGuid, graphGuid, nodeGuid);
            if (node != null)
            {
                Logging.Log(SeverityEnum.Info, "deleting edges connected to node " + nodeGuid + " in graph " + graphGuid);

                foreach (Edge edge in _Repository.GetConnectedEdges(tenantGuid, graphGuid, nodeGuid))
                    _Repository.DeleteEdge(tenantGuid, graphGuid, edge.GUID);

                Logging.Log(SeverityEnum.Info, "deleting node " + nodeGuid + " in graph " + graphGuid);

                _Repository.DeleteNode(tenantGuid, graphGuid, nodeGuid);
            }
        }

        /// <summary>
        /// Delete all nodes associated with a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        public void DeleteNodes(Guid tenantGuid, Guid graphGuid)
        {
            _Repository.DeleteNodes(tenantGuid, graphGuid);
        }

        /// <summary>
        /// Delete specific nodes associated with a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuids">Node GUIDs.</param>
        public void DeleteNodes(Guid tenantGuid, Guid graphGuid, List<Guid> nodeGuids)
        {
            _Repository.DeleteNodes(tenantGuid, graphGuid, nodeGuids);
        }

        /// <summary>
        /// Check existence of a node.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <returns>True if exists.</returns>
        public bool ExistsNode(Guid tenantGuid, Guid graphGuid, Guid nodeGuid)
        {
            return _Repository.ExistsNode(tenantGuid, graphGuid, nodeGuid);
        }

        #endregion

        #region Edges

        /// <summary>
        /// Create an edge between two nodes.
        /// </summary>
        /// <param name="edge">Edge.</param>
        /// <returns>Edge.</returns>
        public Edge CreateEdge(Edge edge)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));

            if (!_Repository.ExistsGraph(edge.TenantGUID, edge.GraphGUID)) throw new ArgumentException("No graph with GUID '" + edge.GraphGUID + "' exists.");
            if (_Repository.ExistsEdge(edge.TenantGUID, edge.GraphGUID, edge.GUID)) throw new ArgumentException("An edge with GUID '" + edge.GUID + "' already exists in graph '" + edge.GraphGUID + "'.");

            if (!_Repository.ExistsNode(edge.TenantGUID, edge.GraphGUID, edge.From)) throw new ArgumentException("No node with GUID '" + edge.From + "' exists in graph '" + edge.GraphGUID + "'");
            if (!_Repository.ExistsNode(edge.TenantGUID,edge.GraphGUID, edge.To)) throw new ArgumentException("No node with GUID '" + edge.To + "' exists in graph '" + edge.GraphGUID + "'");

            Edge existing = _Repository.ReadEdge(edge.TenantGUID, edge.GraphGUID, edge.GUID);
            if (existing != null) return existing;

            Edge created = _Repository.CreateEdge(edge);

            Logging.Log(SeverityEnum.Debug, "created edge " + created.GUID + " in graph " + created.GraphGUID);

            return created;
        }

        /// <summary>
        /// Create edges.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="edges">Edges.</param>
        /// <returns>Edges.</returns>
        public List<Edge> CreateEdges(Guid tenantGuid, Guid graphGuid, List<Edge> edges)
        {
            if (edges == null) throw new ArgumentNullException(nameof(edges));

            if (!_Repository.ExistsGraph(tenantGuid, graphGuid)) throw new ArgumentException("No graph with GUID '" + graphGuid + "' exists.");

            List<Edge> created = _Repository.CreateMultipleEdges(tenantGuid, graphGuid, edges);
            Logging.Log(SeverityEnum.Debug, "created " + created.Count + " edges(s) in graph " + graphGuid);

            return created;
        }

        /// <summary>
        /// Create an edge between two nodes.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="fromNode">From node.</param>
        /// <param name="toNode">To node.</param>
        /// <param name="name">Name.</param>
        /// <param name="cost">Cost.</param>
        /// <param name="labels">Labels.</param>
        /// <param name="tags">Tags.</param>
        /// <param name="data">Data.</param>
        /// <returns>Edge.</returns>
        public Edge CreateEdge(
            Guid tenantGuid,
            Guid graphGuid,
            Node fromNode,
            Node toNode,
            string name,
            int cost = 0,
            List<string> labels = null,
            NameValueCollection tags = null,
            object data = null)
        {
            if (fromNode == null) throw new ArgumentNullException(nameof(fromNode));
            if (toNode == null) throw new ArgumentNullException(nameof(toNode));
            if (String.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            Edge edge = new Edge
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                From = fromNode.GUID,
                Name = name,
                To = toNode.GUID,
                Cost = cost,
                Labels = labels,
                Tags = tags,
                Data = data
            };

            if (!_Repository.ExistsGraph(edge.TenantGUID, edge.GraphGUID)) throw new ArgumentException("No graph with GUID '" + edge.GraphGUID + "' exists.");
            if (_Repository.ExistsEdge(edge.TenantGUID, edge.GraphGUID, edge.GUID)) throw new ArgumentException("An edge with GUID '" + edge.GUID + "' already exists in graph '" + edge.GraphGUID + "'.");

            if (!_Repository.ExistsNode(edge.TenantGUID, edge.GraphGUID, edge.From)) throw new ArgumentException("No node with GUID '" + edge.From + "' exists in graph '" + edge.GraphGUID + "'");
            if (!_Repository.ExistsNode(edge.TenantGUID,edge.GraphGUID, edge.To)) throw new ArgumentException("No node with GUID '" + edge.To + "' exists in graph '" + edge.GraphGUID + "'");

            Edge created = _Repository.CreateEdge(edge);

            Logging.Log(SeverityEnum.Debug, "created edge " + created.GUID + " in graph " + created.GraphGUID);

            return created;
        }

        /// <summary>
        /// Read edges.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="labels">Labels on which to filter results.</param>
        /// <param name="tags">Tags on which to filter results.</param>
        /// <param name="expr">
        /// Edge filter expression for Data JSON body.
        /// Expression left terms must follow the form of Sqlite JSON paths.
        /// For example, to retrieve the 'Name' property, use '$.Name', OperatorEnum.Equals, '[name here]'.</param>
        /// <param name="order">Enumeration order.</param>
        /// <param name="skip">The number of records to skip.</param>
        /// <returns>Edges.</returns>
        public IEnumerable<Edge> ReadEdges(
            Guid tenantGuid,
            Guid graphGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr expr = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending,
            int skip = 0)
        {
            foreach (Edge edge in _Repository.ReadEdges(tenantGuid, graphGuid, labels, tags, expr, order, skip))
            {
                yield return edge;
            }
        }

        /// <summary>
        /// Read edge.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <returns>Edge.</returns>
        public Edge ReadEdge(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            return _Repository.ReadEdge(tenantGuid, graphGuid, edgeGuid);
        }

        /// <summary>
        /// Update edge.
        /// </summary>
        /// <param name="edge">Edge.</param>
        /// <returns>Edge.</returns>
        public Edge UpdateEdge(Edge edge)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));

            if (!_Repository.ExistsGraph(edge.TenantGUID, edge.GraphGUID)) throw new ArgumentException("No graph with GUID '" + edge.GraphGUID + "' exists.");
            if (!_Repository.ExistsEdge(edge.TenantGUID, edge.GraphGUID, edge.GUID)) throw new ArgumentException("No edge with GUID '" + edge.GUID + "' exists in graph '" + edge.GraphGUID + "'");

            if (!_Repository.ExistsNode(edge.TenantGUID, edge.GraphGUID, edge.From)) throw new ArgumentException("No node with GUID '" + edge.From + "' exists in graph '" + edge.GraphGUID + "'");
            if (!_Repository.ExistsNode(edge.TenantGUID, edge.GraphGUID, edge.To)) throw new ArgumentException("No node with GUID '" + edge.To + "' exists in graph '" + edge.GraphGUID + "'");

            Edge updated = _Repository.UpdateEdge(edge);

            Logging.Log(SeverityEnum.Debug, "updated edge " + updated.GUID + " in graph " + updated.GraphGUID);

            return updated;
        }

        /// <summary>
        /// Delete edge.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        public void DeleteEdge(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            Edge edge = _Repository.ReadEdge(tenantGuid, graphGuid, edgeGuid);
            if (edge != null)
            {
                _Repository.DeleteEdge(tenantGuid, graphGuid, edgeGuid);
                Logging.Log(SeverityEnum.Debug, "deleted edge " + edgeGuid + " in graph " + graphGuid);
            }
        }

        /// <summary>
        /// Delete all edges associated with a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        public void DeleteEdges(Guid tenantGuid, Guid graphGuid)
        {
            _Repository.DeleteEdges(tenantGuid, graphGuid);
        }

        /// <summary>
        /// Delete specific edges associated with a graph.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="edgeGuids">Edge GUIDs.</param>
        public void DeleteEdges(Guid tenantGuid, Guid graphGuid, List<Guid> edgeGuids)
        {
            _Repository.DeleteEdges(tenantGuid, graphGuid, edgeGuids);
        }

        /// <summary>
        /// Check if an edge exists by GUID.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="edgeGuid">Edge GUID.</param>
        /// <returns>True if exists.</returns>
        public bool ExistsEdge(Guid tenantGuid, Guid graphGuid, Guid edgeGuid)
        {
            return _Repository.ExistsEdge(tenantGuid, graphGuid, edgeGuid);
        }

        #endregion

        #region Batch

        /// <summary>
        /// Batch existence check.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="req">Existence request.</param>
        /// <returns>Existence result.</returns>
        public ExistenceResult BatchExistence(Guid tenantGuid, Guid graphGuid, ExistenceRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));
            if (!req.ContainsExistenceRequest()) throw new ArgumentException("Supplied existence request contains no valid existence filters.");
            return _Repository.BatchExistence(tenantGuid, graphGuid, req);
        }

        #endregion

        #region Routes-and-Traversal

        /// <summary>
        /// Get parents for a given node.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <returns>Nodes.</returns>
        public IEnumerable<Node> GetParents(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            if (order == EnumerationOrderEnum.CostAscending
                || order == EnumerationOrderEnum.CostDescending)
                throw new ArgumentException("Cost-based enumeration orders are only available to edge APIs.");

            foreach (Node node in _Repository.GetParents(tenantGuid, graphGuid, nodeGuid, order))
            {
                yield return node;
            }
        }

        /// <summary>
        /// Get children for a given node.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <returns>Nodes.</returns>
        public IEnumerable<Node> GetChildren(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            if (order == EnumerationOrderEnum.CostAscending
                || order == EnumerationOrderEnum.CostDescending)
                throw new ArgumentException("Cost-based enumeration orders are only available to edge APIs.");

            foreach (Node node in _Repository.GetChildren(tenantGuid, graphGuid, nodeGuid, order))
            {
                yield return node;
            }
        }

        /// <summary>
        /// Get neighbors for a given node.
        /// </summary>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="order">Enumeration order.</param>
        /// <returns>Nodes.</returns>
        public IEnumerable<Node> GetNeighbors(
            Guid tenantGuid,
            Guid graphGuid,
            Guid nodeGuid,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            if (order == EnumerationOrderEnum.CostAscending
                || order == EnumerationOrderEnum.CostDescending)
                throw new ArgumentException("Cost-based enumeration orders are only available to edge APIs.");

            foreach (Node node in _Repository.GetNeighbors(tenantGuid, graphGuid, nodeGuid, order))
            {
                yield return node;
            }
        }

        /// <summary>
        /// Get routes between two nodes.
        /// </summary>
        /// <param name="searchType">Search type.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="fromNodeGuid">From node GUID.</param>
        /// <param name="toNodeGuid">To node GUID.</param>
        /// <param name="edgeFilter">
        /// Edge filter expression for Data JSON body.
        /// Expression left terms must follow the form of Sqlite JSON paths.
        /// For example, to retrieve the 'Name' property, use '$.Name', OperatorEnum.Equals, '[name here]'.</param>
        /// <param name="nodeFilter">
        /// Node filter expression for Data JSON body.
        /// Expression left terms must follow the form of Sqlite JSON paths.
        /// For example, to retrieve the 'Name' property, use '$.Name', OperatorEnum.Equals, '[name here]'.</param>
        /// <returns>Route details.</returns>
        public IEnumerable<RouteDetail> GetRoutes(
            SearchTypeEnum searchType,
            Guid tenantGuid,
            Guid graphGuid,
            Guid fromNodeGuid,
            Guid toNodeGuid,
            Expr edgeFilter = null,
            Expr nodeFilter = null)
        {
            foreach (RouteDetail route in _Repository.GetRoutes(
                searchType, 
                tenantGuid,
                graphGuid,
                fromNodeGuid,
                toNodeGuid,
                edgeFilter,
                nodeFilter))
            {
                yield return route;
            }
        }

        /// <summary>
        /// Get edges from a given node.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="fromNodeGuid">From node GUID.</param>
        /// <param name="labels">Labels on which to filter edges.</param>
        /// <param name="tags">Tags on which to filter edges.</param>
        /// <param name="edgeFilter">
        /// Edge filter expression for Data JSON body.
        /// Expression left terms must follow the form of Sqlite JSON paths.
        /// For example, to retrieve the 'Name' property, use '$.Name', OperatorEnum.Equals, '[name here]'.</param>
        /// <param name="order">Enumeration order.</param>
        /// <returns>Edges.</returns>
        public IEnumerable<Edge> GetEdgesFrom(
            Guid tenantGuid,
            Guid graphGuid,
            Guid fromNodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            foreach (Edge edge in _Repository.GetEdgesFrom(tenantGuid, graphGuid, fromNodeGuid, labels, tags, edgeFilter, order))
            {
                yield return edge;
            }
        }

        /// <summary>
        /// Get edges to a given node.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="toNodeGuid">To node GUID.</param>
        /// <param name="labels">Labels on which to filter edges.</param>
        /// <param name="tags">Tags on which to filter edges.</param>
        /// <param name="edgeFilter">
        /// Edge filter expression for Data JSON body.
        /// Expression left terms must follow the form of Sqlite JSON paths.
        /// For example, to retrieve the 'Name' property, use '$.Name', OperatorEnum.Equals, '[name here]'.</param>
        /// <param name="order">Enumeration order.</param>
        /// <returns>Edges.</returns>
        public IEnumerable<Edge> GetEdgesTo(
            Guid tenantGuid,
            Guid graphGuid,
            Guid toNodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            return _Repository.GetEdgesTo(tenantGuid, graphGuid, toNodeGuid, labels, tags, edgeFilter, order);
        }

        /// <summary>
        /// Get edges between two nodes.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="fromNodeGuid">From node GUID.</param>
        /// <param name="toNodeGuid">To node GUID.</param>
        /// <param name="labels">Labels on which to filter edges.</param>
        /// <param name="tags">Tags on which to filter edges.</param>
        /// <param name="edgeFilter">
        /// Edge filter expression for Data JSON body.
        /// Expression left terms must follow the form of Sqlite JSON paths.
        /// For example, to retrieve the 'Name' property, use '$.Name', OperatorEnum.Equals, '[name here]'.</param>
        /// <param name="order">Enumeration order.</param>
        /// <returns>Edges.</returns>
        public IEnumerable<Edge> GetEdgesBetween(
            Guid tenantGuid,
            Guid graphGuid,
            Guid fromNodeGuid,
            Guid toNodeGuid,
            List<string> labels = null,
            NameValueCollection tags = null,
            Expr edgeFilter = null,
            EnumerationOrderEnum order = EnumerationOrderEnum.CreatedDescending)
        {
            foreach (Edge edge in _Repository.GetEdgesBetween(tenantGuid, graphGuid, fromNodeGuid, toNodeGuid, labels, tags, edgeFilter, order))
            {
                yield return edge;
            }
        }

        #endregion

        #endregion

        #region Private-Methods

        /// <summary>
        /// Dispose of the object.
        /// </summary>
        /// <param name="disposing">Disposing of resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (_Disposed)
            {
                return;
            }

            if (disposing)
            {
                _Repository = null;
                Logging = null;
            }

            _Disposed = true;
        }

        #endregion
    }
}