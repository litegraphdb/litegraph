namespace LiteGraph
{
    using System;
    using ExpressionTree;
    using System.Collections.Generic;
    using System.Threading;
    using System.Linq;
    using LiteGraph.Gexf;
    using LiteGraph.GraphRepositories;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.Serialization;
    using System.Xml.Linq;
    using System.Collections.Specialized;
    using System.Reflection.Metadata.Ecma335;
    using SQLitePCL;
    using LiteGraph.Client.Interfaces;
    using LiteGraph.Client.Implementations;
    using Caching;

    /// <summary>
    /// LiteGraph client.
    /// The LiteGraph client leverages an underlying graph repository base class, which provides primitives.
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
                return _Repo.Logging;
            }
            set
            {
                if (value == null) value = new LoggingSettings();
                _Repo.Logging = value;
            }
        }

        /// <summary>
        /// Caching settings.
        /// </summary>
        public CachingSettings Caching
        {
            get
            {
                return _Caching;
            }
            set
            {
                if (value == null) value = new CachingSettings();
                _Caching = value;
            }
        }

        /// <summary>
        /// Storage settings.
        /// </summary>
        public StorageSettings Storage
        {
            get
            {
                return _Storage;
            }
            set
            {
                if (value == null) value = new StorageSettings();
                _Storage = value;
            }
        }

        /// <summary>
        /// Serialization helper.
        /// </summary>
        public Serializer Serializer
        {
            get
            {
                return _Repo.Serializer;
            }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(Serializer));
                _Repo.Serializer = value;
            }
        }

        /// <inheritdoc />
        public IAdminMethods Admin { get; }

        /// <inheritdoc />
        public IBatchMethods Batch { get; }

        /// <inheritdoc />
        public ICredentialMethods Credential { get; }

        /// <inheritdoc />
        public IEdgeMethods Edge { get; }

        /// <inheritdoc />
        public IGraphMethods Graph { get; }

        /// <inheritdoc />
        public ILabelMethods Label { get; }

        /// <inheritdoc />
        public INodeMethods Node { get; }

        /// <inheritdoc />
        public ITagMethods Tag { get; }

        /// <inheritdoc />
        public ITenantMethods Tenant { get; }

        /// <inheritdoc />
        public IUserMethods User { get; }

        /// <inheritdoc />
        public IVectorMethods Vector { get; }

        #endregion

        #region Private-Members

        private bool _Disposed = false;
        private CachingSettings _Caching = new CachingSettings();
        private StorageSettings _Storage = new StorageSettings();
        private GraphRepositoryBase _Repo = null;
        private GexfWriter _Gexf = new GexfWriter();

        private LRUCache<Guid, TenantMetadata> _TenantCache = null;
        private LRUCache<Guid, Graph> _GraphCache = null;
        private LRUCache<Guid, Node> _NodeCache = null;
        private LRUCache<Guid, Edge> _EdgeCache = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate LiteGraph client.
        /// </summary>
        /// <param name="repo">Graph repository driver.</param>
        /// <param name="logging">Logging.</param>
        /// <param name="caching">Caching settings.</param>
        /// <param name="storage">Storage settings.</param>
        public LiteGraphClient(
            GraphRepositoryBase repo,
            LoggingSettings logging = null,
            CachingSettings caching = null,
            StorageSettings storage = null)
        {
            if (repo == null) throw new ArgumentNullException(nameof(repo));

            _Repo = repo;

            if (logging != null) Logging = logging;
            else Logging = new LoggingSettings();

            if (caching != null) Caching = caching;
            else Caching = new CachingSettings();

            if (storage != null) Storage = storage;
            else Storage = new StorageSettings();

            if (Caching.Enable)
            {
                _TenantCache = new LRUCache<Guid, TenantMetadata>(Caching.Capacity, Caching.EvictCount);
                _GraphCache = new LRUCache<Guid, Graph>(Caching.Capacity, Caching.EvictCount);
                _NodeCache = new LRUCache<Guid, Node>(Caching.Capacity, Caching.EvictCount);
                _EdgeCache = new LRUCache<Guid, Edge>(Caching.Capacity, Caching.EvictCount);
            }

            Admin = new AdminMethods(this, _Repo, _Storage.BackupsDirectory);
            Batch = new BatchMethods(this, _Repo);
            Credential = new CredentialMethods(this, _Repo);
            Edge = new EdgeMethods(this, _Repo, _EdgeCache);
            Graph = new GraphMethods(this, _Repo, _GraphCache);
            Label = new LabelMethods(this, _Repo);
            Node = new NodeMethods(this, _Repo, _NodeCache);
            Tag = new TagMethods(this, _Repo);
            Tenant = new TenantMethods(this, _Repo, _TenantCache);
            User = new UserMethods(this, _Repo);
            Vector = new VectorMethods(this, _Repo);
        }

        #endregion

        #region Public-Methods

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
                _Repo = null;
                Logging = null;
            }

            _Disposed = true;
        }

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
            _Repo.InitializeRepository();
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
            return Serializer.DeserializeJson<T>(data.ToString());
        }

        /// <summary>
        /// Export graph to GEXF.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="filename">Filename.</param>
        /// <param name="includeData">True to include data.</param>
        public void ExportGraphToGexfFile(Guid tenantGuid, Guid graphGuid, string filename, bool includeData = false)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));
            _Gexf.ExportToFile(this, tenantGuid, graphGuid, filename, includeData);
        }

        /// <summary>
        /// Render a graph as GEXF.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="includeData">True to include data.</param>
        /// <returns>GEXF string.</returns>
        public string RenderGraphAsGexf(Guid tenantGuid, Guid graphGuid, bool includeData = false)
        {
            return _Gexf.RenderAsGexf(this, tenantGuid, graphGuid, includeData);
        }

        /// <summary>
        /// Flush the database to disk.  Only useful when using an in-memory LiteGraph instance.
        /// </summary>
        public void Flush()
        {
            _Repo.Flush();
        }

        #endregion

        #region Internal-Methods

        internal void ValidateLabels(List<string> labels)
        {
            if (labels == null) return;
            foreach (string label in labels)
                if (String.IsNullOrEmpty(label)) throw new ArgumentException("The supplied labels contains a null or empty label.");
        }

        internal void ValidateTags(NameValueCollection tags)
        {
            if (tags == null) return;
            foreach (string key in tags.AllKeys)
                if (String.IsNullOrEmpty(key)) throw new ArgumentException("The supplied tags contains a null or empty key.");
        }

        internal void ValidateVectors(List<VectorMetadata> vectors)
        {
            if (vectors == null || vectors.Count < 1) return;
            foreach (VectorMetadata vector in vectors)
            {
                if (String.IsNullOrEmpty(vector.Model)) throw new ArgumentException("The supplied vector object does not include a model.");
                if (vector.Dimensionality <= 0) throw new ArgumentException("The supplied vector object dimensionality must be greater than zero.");
                if (vector.Vectors == null || vector.Vectors.Count < 1) throw new ArgumentException("The supplied vector object does not include any vectors.");
                if (String.IsNullOrEmpty(vector.Content)) throw new ArgumentException("The supplied vector object does not contain any content.");
            }
        }
        
        internal void ValidateTenantExists(Guid tenantGuid)
        {
            if (TenantCacheTryGet(tenantGuid, out var _)) return;
            TenantMetadata tenant = Tenant.ReadByGuid(tenantGuid);
            if (tenant == null) throw new ArgumentException("No tenant with GUID '" + tenantGuid + "' exists.");
            TenantCacheAdd(tenant);
        }

        internal void ValidateUserExists(Guid tenantGuid, Guid userGuid)
        {
            if (!User.ExistsByGuid(tenantGuid, userGuid))
                throw new ArgumentException("No user with GUID '" + userGuid + "' exists.");
        }

        internal void ValidateGraphExists(Guid tenantGuid, Guid? graphGuid)
        {
            if (graphGuid == null) return;
            if (GraphCacheTryGet(graphGuid.Value, out var _)) return; 
            Graph graph = _Repo.Graph.ReadByGuid(tenantGuid, graphGuid.Value);
            if (graph == null) throw new ArgumentException("No graph with GUID '" + graphGuid.Value + "' exists.");
            GraphCacheAdd(graph);
        }

        internal void ValidateNodeExists(Guid tenantGuid, Guid? graphGuid, Guid? nodeGuid)
        {
            if (graphGuid == null) return;
            if (nodeGuid == null) return;
            if (NodeCacheTryGet(nodeGuid.Value, out var _)) return;
            Node node = _Repo.Node.ReadByGuid(tenantGuid, graphGuid.Value, nodeGuid.Value);
            if (node == null) throw new ArgumentException("No node with GUID '" + nodeGuid.Value + "' exists.");
            NodeCacheAdd(node);
        }

        internal void ValidateEdgeExists(Guid tenantGuid, Guid? graphGuid, Guid? edgeGuid)
        {
            if (graphGuid == null) return;
            if (edgeGuid == null) return;
            if (EdgeCacheTryGet(edgeGuid.Value, out var _)) return;
            Edge edge = _Repo.Edge.ReadByGuid(tenantGuid, graphGuid.Value, edgeGuid.Value);
            if (edge == null) throw new ArgumentException("No edge with GUID '" + edgeGuid.Value + "' exists.");
            EdgeCacheAdd(edge);
        }

        #endregion

        #region Private-Methods

        private void TenantCacheAdd(TenantMetadata obj)
        {
            if (_TenantCache != null)
            {
                _TenantCache.AddReplace(obj.GUID, obj);
            }
        }

        private bool TenantCacheTryGet(Guid guid, out TenantMetadata obj)
        {
            obj = null;
            if (_TenantCache != null) return _TenantCache.TryGet(guid, out obj);
            return false;
        }

        private void TenantCacheRemove(Guid guid)
        {
            if (_TenantCache != null) _TenantCache.TryRemove(guid);
        }

        private void GraphCacheAdd(Graph obj)
        {
            if (_GraphCache != null)
            {
                _GraphCache.AddReplace(obj.GUID, obj);
            }
        }

        private bool GraphCacheTryGet(Guid guid, out Graph obj)
        {
            obj = null;
            if (_GraphCache != null) return _GraphCache.TryGet(guid, out obj);
            return false;
        }

        private void GraphCacheRemove(Guid guid)
        {
            if (_GraphCache != null) _GraphCache.TryRemove(guid);
        }

        private void NodeCacheAdd(Node obj)
        {
            if (_NodeCache != null)
            {
                _NodeCache.AddReplace(obj.GUID, obj);
            }
        }

        private bool NodeCacheTryGet(Guid guid, out Node obj)
        {
            obj = null;
            if (_NodeCache != null) return _NodeCache.TryGet(guid, out obj);
            return false;
        }

        private void NodeCacheRemove(Guid guid)
        {
            if (_NodeCache != null) _NodeCache.TryRemove(guid);
        }

        private void EdgeCacheAdd(Edge obj)
        {
            if (_EdgeCache != null)
            {
                _EdgeCache.AddReplace(obj.GUID, obj);
            }
        }

        private bool EdgeCacheTryGet(Guid guid, out Edge obj)
        {
            obj = null;
            if (_EdgeCache != null) return _EdgeCache.TryGet(guid, out obj);
            return false;
        }

        private void EdgeCacheRemove(Guid guid)
        {
            if (_EdgeCache != null) _EdgeCache.TryRemove(guid);
        }

        #endregion
    }
}