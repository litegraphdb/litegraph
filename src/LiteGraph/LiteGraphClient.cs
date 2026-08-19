namespace LiteGraph
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Threading;
    using System.Threading.Tasks;
    using System.IO;
    using Caching;
    using LiteGraph.Client.Implementations;
    using LiteGraph.Client.Interfaces;
    using LiteGraph.Gexf;
    using LiteGraph.GraphRepositories;
    using LiteGraph.Jsonl;
    using LiteGraph.Serialization;
    using LiteGraph.Subgraph;

    /// <summary>
    /// LiteGraph client.
    /// The LiteGraph client leverages an underlying graph repository base class, which provides primitives.
    /// </summary>
    public class LiteGraphClient : IDisposable, IAsyncDisposable
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

        /// <summary>
        /// Base URL of the LiteGraph server.
        /// </summary>
        public string Endpoint { get; private set; }

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

        /// <inheritdoc />
        public IVectorIndexMethods VectorIndex { get; }

        /// <inheritdoc />
        public IQueryMethods Query { get; }

        /// <inheritdoc />
        public ITransactionMethods Transaction { get; }

        /// <summary>
        /// Request history methods.
        /// </summary>
        public LiteGraph.GraphRepositories.Interfaces.IRequestHistoryMethods RequestHistory
        {
            get
            {
                return _Repo?.RequestHistory;
            }
        }

        /// <summary>
        /// Authorization audit methods.
        /// </summary>
        public LiteGraph.GraphRepositories.Interfaces.IAuthorizationAuditMethods AuthorizationAudit
        {
            get
            {
                return _Repo?.AuthorizationAudit;
            }
        }

        /// <summary>
        /// Authorization role methods.
        /// </summary>
        public LiteGraph.GraphRepositories.Interfaces.IAuthorizationRoleMethods AuthorizationRoles
        {
            get
            {
                return _Repo?.AuthorizationRoles;
            }
        }

        #endregion

        #region Private-Members

        private bool _Disposed = false;
        private CachingSettings _Caching = new CachingSettings();
        private StorageSettings _Storage = new StorageSettings();
        private GraphRepositoryBase _Repo = null;
        private bool _DisposeRepository = false;
        private GexfWriter _Gexf = new GexfWriter();
        private SubgraphExtractor _SubgraphExtractor = new SubgraphExtractor();
        private JsonlGraphWriter _JsonlWriter = new JsonlGraphWriter();
        private JsonlGraphReader _JsonlReader = new JsonlGraphReader();
        private JsonlGraphImporter _JsonlImporter = new JsonlGraphImporter();

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
            StorageSettings storage = null) : this(repo, logging, caching, storage, true)
        {
        }

        /// <summary>
        /// Instantiate LiteGraph client.
        /// </summary>
        /// <param name="repo">Graph repository driver.</param>
        /// <param name="logging">Logging.</param>
        /// <param name="caching">Caching settings.</param>
        /// <param name="storage">Storage settings.</param>
        /// <param name="disposeRepository">Dispose the repository when this client is disposed.</param>
        public LiteGraphClient(
            GraphRepositoryBase repo,
            LoggingSettings logging,
            CachingSettings caching,
            StorageSettings storage,
            bool disposeRepository)
        {
            if (repo == null) throw new ArgumentNullException(nameof(repo));

            _Repo = repo;
            _DisposeRepository = disposeRepository;

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
            VectorIndex = new VectorIndexMethods(this, _Repo);
            Query = new QueryMethods(this, _Repo);
            Transaction = new TransactionMethods(_Repo);
        }

        /// <summary>
        /// Instantiate LiteGraph client.
        /// </summary>
        /// <param name="endpoint">Base URL of the LiteGraph server.</param>
        public LiteGraphClient(string endpoint = "http://localhost:8000/")
        {
            if (string.IsNullOrEmpty(endpoint)) throw new ArgumentNullException(nameof(endpoint));
            Endpoint = endpoint.TrimEnd('/');
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
                Exception disposalException = null;

                try
                {
                    if (_Repo != null)
                    {
                        _Repo.Logging = null;
                        if (_DisposeRepository) _Repo.Dispose();
                    }
                }
                catch (Exception e)
                {
                    disposalException = e;
                }
                finally
                {
                    _TenantCache = null;
                    _GraphCache = null;
                    _NodeCache = null;
                    _EdgeCache = null;

                    _Repo = null;
                }

                _Disposed = true;

                if (disposalException != null)
                {
                    throw new InvalidOperationException("An error occurred while disposing the LiteGraph client.", disposalException);
                }
            }
            else
            {
                _Disposed = true;
            }
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
        /// Tear down the client and dispose of resources asynchronously.
        /// </summary>
        /// <returns>Value task.</returns>
        public async ValueTask DisposeAsync()
        {
            if (_Disposed) return;

            Exception disposalException = null;

            try
            {
                if (_Repo != null)
                {
                    _Repo.Logging = null;
                    if (_DisposeRepository) await _Repo.DisposeAsync().ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                disposalException = e;
            }
            finally
            {
                _TenantCache = null;
                _GraphCache = null;
                _NodeCache = null;
                _EdgeCache = null;

                _Repo = null;
                _Disposed = true;
            }

            GC.SuppressFinalize(this);

            if (disposalException != null)
                throw new InvalidOperationException("An error occurred while disposing the LiteGraph client.", disposalException);
        }

        /// <summary>
        /// Initialize the repository.
        /// </summary>
        public void InitializeRepository()
        {
            _Repo.InitializeRepository();
        }

        /// <summary>
        /// Initialize the repository asynchronously.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task InitializeRepositoryAsync(CancellationToken token = default)
        {
            await _Repo.InitializeRepositoryAsync(token).ConfigureAwait(false);
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
        /// <param name="includeSubordinates">True to include subordinates (labels, tags, vectors).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task ExportGraphToGexfFile(
            Guid tenantGuid,
            Guid graphGuid,
            string filename,
            bool includeData,
            bool includeSubordinates,
            CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));
            await _Gexf.ExportToFile(this, tenantGuid, graphGuid, filename, includeData, includeSubordinates, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Render a graph as GEXF.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="includeData">True to include data.</param>
        /// <param name="includeSubordinates">True to include subordinates (labels, tags, vectors).</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>GEXF string.</returns>
        public async Task<string> RenderGraphAsGexf(Guid tenantGuid, Guid graphGuid, bool includeData, bool includeSubordinates, CancellationToken token = default)
        {
            return await _Gexf.RenderAsGexf(this, tenantGuid, graphGuid, includeData, includeSubordinates, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Extract a filtered, directional subgraph as a materialized search result.
        /// </summary>
        /// <param name="request">Subgraph extraction request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Search result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the graph or a start node does not exist, or no start node is supplied.</exception>
        public async Task<SearchResult> ExtractSubgraph(SubgraphExtractionRequest request, CancellationToken token = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            return await _SubgraphExtractor.Extract(this, request, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Extract a filtered subgraph from a single start node as a materialized search result.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="startNodeGuid">Start node GUID.</param>
        /// <param name="maxDepth">Maximum traversal depth.  Default is 2.  Minimum is 0.</param>
        /// <param name="direction">Traversal direction.  Default is Both.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Search result.</returns>
        public async Task<SearchResult> ExtractSubgraph(
            Guid tenantGuid,
            Guid graphGuid,
            Guid startNodeGuid,
            int maxDepth = 2,
            GraphTraversalDirectionEnum direction = GraphTraversalDirectionEnum.Both,
            CancellationToken token = default)
        {
            SubgraphExtractionRequest request = new SubgraphExtractionRequest
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                StartNodeGUIDs = new List<Guid> { startNodeGuid },
                MaxDepth = maxDepth,
                Direction = direction,
                IncludeData = true,
                IncludeSubordinates = true
            };
            return await _SubgraphExtractor.Extract(this, request, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Export a filtered subgraph to a stream in JSONL format.
        /// </summary>
        /// <param name="request">Subgraph extraction request.</param>
        /// <param name="stream">Destination stream.  The stream is left open.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the request or stream is null.</exception>
        public async Task ExportSubgraphToJsonlStream(SubgraphExtractionRequest request, Stream stream, CancellationToken token = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            SearchResult result = await _SubgraphExtractor.Extract(this, request, token).ConfigureAwait(false);
            JsonlExportMetadata metadata = BuildSubgraphMetadata(request, result);
            await _JsonlWriter.WriteSearchResult(result, metadata, stream, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Export an already-extracted search result to a stream in JSONL format.
        /// </summary>
        /// <param name="result">Search result to export.</param>
        /// <param name="stream">Destination stream.  The stream is left open.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the result or stream is null.</exception>
        public async Task ExportSearchResultToJsonlStream(SearchResult result, Stream stream, CancellationToken token = default)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            JsonlExportMetadata metadata = new JsonlExportMetadata { Kind = "subgraph" };
            if (result.Graphs != null && result.Graphs.Count > 0)
            {
                metadata.SourceTenantGUID = result.Graphs[0].TenantGUID;
                metadata.SourceGraphGUID = result.Graphs[0].GUID;
                metadata.SourceGraphName = result.Graphs[0].Name;
            }

            await _JsonlWriter.WriteSearchResult(result, metadata, stream, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Export an entire graph to a stream in JSONL format.  Runs in constant memory; suitable as a provider-agnostic backup.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="includeData">True to include the data property of objects.</param>
        /// <param name="includeSubordinates">True to include labels, tags, and vectors.</param>
        /// <param name="stream">Destination stream.  The stream is left open.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the stream is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the graph does not exist.</exception>
        public async Task ExportGraphToJsonlStream(Guid tenantGuid, Guid graphGuid, bool includeData, bool includeSubordinates, Stream stream, CancellationToken token = default)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            await _JsonlWriter.WriteGraph(this, tenantGuid, graphGuid, null, includeData, includeSubordinates, stream, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Export a filtered subgraph to a file in JSONL format.
        /// </summary>
        /// <param name="request">Subgraph extraction request.</param>
        /// <param name="filename">Destination filename.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the request or filename is null.</exception>
        public async Task ExportSubgraphToJsonlFile(SubgraphExtractionRequest request, string filename, CancellationToken token = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));

            SearchResult result = await _SubgraphExtractor.Extract(this, request, token).ConfigureAwait(false);
            JsonlExportMetadata metadata = BuildSubgraphMetadata(request, result);
            await _JsonlWriter.WriteSearchResultToFile(result, metadata, filename, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Export an entire graph to a file in JSONL format.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="filename">Destination filename.</param>
        /// <param name="includeData">True to include the data property of objects.</param>
        /// <param name="includeSubordinates">True to include labels, tags, and vectors.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the filename is null.</exception>
        public async Task ExportGraphToJsonlFile(Guid tenantGuid, Guid graphGuid, string filename, bool includeData, bool includeSubordinates, CancellationToken token = default)
        {
            if (String.IsNullOrEmpty(filename)) throw new ArgumentNullException(nameof(filename));
            await _JsonlWriter.WriteGraphToFile(this, tenantGuid, graphGuid, null, includeData, includeSubordinates, filename, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Render an entire graph as a JSONL string.  Not for large graphs; use the streaming methods instead.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="includeData">True to include the data property of objects.</param>
        /// <param name="includeSubordinates">True to include labels, tags, and vectors.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>JSONL string.</returns>
        /// <exception cref="ArgumentException">Thrown when the graph does not exist.</exception>
        public async Task<string> RenderGraphAsJsonl(Guid tenantGuid, Guid graphGuid, bool includeData, bool includeSubordinates, CancellationToken token = default)
        {
            return await _JsonlWriter.RenderGraph(this, tenantGuid, graphGuid, null, includeData, includeSubordinates, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Import JSONL from a stream into a new or existing graph.
        /// </summary>
        /// <param name="tenantGuid">Target tenant GUID.</param>
        /// <param name="jsonl">Source JSONL stream.</param>
        /// <param name="request">Import request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Import result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the stream or request is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the request is inconsistent with its mode, or the target graph does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown on a GUID collision under the Preserve strategy.</exception>
        /// <exception cref="JsonlFormatException">Thrown on a malformed line when the error policy is Abort.</exception>
        public async Task<GraphImportResult> ImportGraphFromJsonlStream(Guid tenantGuid, Stream jsonl, GraphImportRequest request, CancellationToken token = default)
        {
            if (jsonl == null) throw new ArgumentNullException(nameof(jsonl));
            if (request == null) throw new ArgumentNullException(nameof(request));
            return await _JsonlImporter.Import(this, tenantGuid, jsonl, request, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Import JSONL from a string into a new or existing graph.
        /// </summary>
        /// <param name="tenantGuid">Target tenant GUID.</param>
        /// <param name="jsonl">Source JSONL content.</param>
        /// <param name="request">Import request.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Import result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the content or request is null.</exception>
        public async Task<GraphImportResult> ImportGraphFromJsonl(Guid tenantGuid, string jsonl, GraphImportRequest request, CancellationToken token = default)
        {
            if (jsonl == null) throw new ArgumentNullException(nameof(jsonl));
            if (request == null) throw new ArgumentNullException(nameof(request));

            using (MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(jsonl)))
            {
                return await _JsonlImporter.Import(this, tenantGuid, ms, request, token).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Flush the database to disk.  Only useful when using an in-memory LiteGraph instance.
        /// </summary>
        public void Flush()
        {
            _Repo.Flush();
        }

        /// <summary>
        /// Flush the database to disk asynchronously.  Only useful when using an in-memory LiteGraph instance.
        /// </summary>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task FlushAsync(CancellationToken token = default)
        {
            await _Repo.FlushAsync(token).ConfigureAwait(false);
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

        internal async Task ValidateTenantExists(Guid tenantGuid, CancellationToken token = default)
        {
            if (TenantCacheTryGet(tenantGuid, out TenantMetadata _)) return;
            TenantMetadata tenant = await Tenant.ReadByGuid(tenantGuid, token).ConfigureAwait(false);
            if (tenant == null) throw new ArgumentException("No tenant with GUID '" + tenantGuid + "' exists.");
            TenantCacheAdd(tenant);
        }

        internal async Task ValidateUserExists(Guid tenantGuid, Guid userGuid, CancellationToken token = default)
        {
            if (!await User.ExistsByGuid(tenantGuid, userGuid, token).ConfigureAwait(false))
                throw new ArgumentException("No user with GUID '" + userGuid + "' exists.");
        }

        internal async Task ValidateGraphExists(Guid tenantGuid, Guid? graphGuid, CancellationToken token = default)
        {
            if (graphGuid == null) return;
            if (GraphCacheTryGet(graphGuid.Value, out Graph _)) return;
            Graph graph = await _Repo.Graph.ReadByGuid(tenantGuid, graphGuid.Value, token).ConfigureAwait(false);
            if (graph == null) throw new ArgumentException("No graph with GUID '" + graphGuid.Value + "' exists.");
            GraphCacheAdd(graph);
        }

        internal async Task ValidateNodeExists(Guid tenantGuid, Guid? nodeGuid, CancellationToken token = default)
        {
            if (nodeGuid == null) return;
            if (NodeCacheTryGet(nodeGuid.Value, out Node _)) return;
            Node node = await _Repo.Node.ReadByGuid(tenantGuid, nodeGuid.Value, token).ConfigureAwait(false);
            if (node == null) throw new ArgumentException("No node with GUID '" + nodeGuid.Value + "' exists.");
            NodeCacheAdd(node);
        }

        internal async Task ValidateEdgeExists(Guid tenantGuid, Guid? edgeGuid, CancellationToken token = default)
        {
            if (edgeGuid == null) return;
            if (EdgeCacheTryGet(edgeGuid.Value, out Edge _)) return;
            Edge edge = await _Repo.Edge.ReadByGuid(tenantGuid, edgeGuid.Value, token).ConfigureAwait(false);
            if (edge == null) throw new ArgumentException("No edge with GUID '" + edgeGuid.Value + "' exists.");
            EdgeCacheAdd(edge);
        }

        #endregion

        #region Private-Methods

        private JsonlExportMetadata BuildSubgraphMetadata(SubgraphExtractionRequest request, SearchResult result)
        {
            string graphName = null;
            if (result != null && result.Graphs != null && result.Graphs.Count > 0) graphName = result.Graphs[0].Name;

            int graphCount = (result?.Graphs != null) ? result.Graphs.Count : 0;
            int nodeCount = (result?.Nodes != null) ? result.Nodes.Count : 0;
            int edgeCount = (result?.Edges != null) ? result.Edges.Count : 0;

            string selection =
                "start=" + String.Join("|", request.StartNodeGUIDs) +
                ",depth=" + request.MaxDepth +
                ",direction=" + request.Direction +
                ",maxNodes=" + request.MaxNodes +
                ",maxEdges=" + request.MaxEdges +
                ",counts(graphs=" + graphCount + ",nodes=" + nodeCount + ",edges=" + edgeCount + ")";

            return new JsonlExportMetadata
            {
                Kind = "subgraph",
                SourceTenantGUID = request.TenantGUID,
                SourceGraphGUID = request.GraphGUID,
                SourceGraphName = graphName,
                SelectionSummary = selection
            };
        }

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
            if (_TenantCache != null) _TenantCache.TryRemove(guid, out _);
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
            if (_GraphCache != null) _GraphCache.TryRemove(guid, out _);
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
            if (_NodeCache != null) _NodeCache.TryRemove(guid, out _);
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
            if (_EdgeCache != null) _EdgeCache.TryRemove(guid, out _);
        }

        #endregion
    }
}
