#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
namespace LiteGraph.Indexing.Vector
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Hnsw;
    using LiteGraph.Serialization;

    /// <summary>
    /// HnswLite implementation of the vector index interface.
    /// </summary>
    public class HnswLiteVectorIndex : IVectorIndex
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private HnswIndex _Index;
        private IHnswStorage _Storage;
        private IHnswLayerStorage _LayerStorage;
        private Graph _Graph;
        private DateTime? _LastRebuildUtc;
        private DateTime? _LastAddUtc;
        private DateTime? _LastRemoveUtc;
        private DateTime? _LastSearchUtc;
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the HnswLite vector index.
        /// </summary>
        public HnswLiteVectorIndex()
        {
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task InitializeAsync(Graph graph, CancellationToken cancellationToken = default)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            if (!graph.VectorDimensionality.HasValue)
                throw new ArgumentException("Graph must have VectorDimensionality set for indexing.");
            if (!graph.VectorIndexType.HasValue || graph.VectorIndexType == VectorIndexTypeEnum.None)
                throw new ArgumentException("Graph must have VectorIndexType set to HnswRam or HnswSqlite.");

            _Graph = graph;

            // Create appropriate storage based on index type
            if (graph.VectorIndexType == VectorIndexTypeEnum.HnswRam)
            {
                _Storage = new RamHnswStorage();
                _LayerStorage = new RamHnswLayerStorage();
            }
            else if (graph.VectorIndexType == VectorIndexTypeEnum.HnswSqlite)
            {
                if (string.IsNullOrEmpty(graph.VectorIndexFile))
                    throw new ArgumentException("Graph must have VectorIndexFile set for HnswSqlite index type.");

                _Storage = new SqliteHnswStorage(graph.VectorIndexFile);
                _LayerStorage = new SqliteHnswLayerStorage(graph.VectorIndexFile + ".layers");
            }
            else
            {
                throw new NotSupportedException($"Index type {graph.VectorIndexType} is not supported.");
            }

            // Create the index with configured parameters
            _Index = new HnswIndex(graph.VectorDimensionality.Value, _Storage, _LayerStorage);

            // Configure HNSW parameters
            _Index.M = graph.VectorIndexM ?? 16;
            _Index.EfConstruction = graph.VectorIndexEfConstruction ?? 200;

            // Set distance function based on common usage (could be made configurable)
            _Index.DistanceFunction = new CosineDistance();

            // Load existing index if using persistent storage
            if (graph.VectorIndexType == VectorIndexTypeEnum.HnswSqlite && File.Exists(graph.VectorIndexFile))
            {
                await LoadAsync(cancellationToken);
            }

            _LastRebuildUtc = DateTime.UtcNow;
        }

        /// <inheritdoc />
        public async Task AddAsync(Guid vectorId, List<float> vector, CancellationToken cancellationToken = default)
        {
            if (vectorId == Guid.Empty) throw new ArgumentException("Vector ID cannot be empty.");
            if (vector == null || vector.Count == 0) throw new ArgumentNullException(nameof(vector));
            if (_Index == null) throw new InvalidOperationException("Index not initialized.");

            await _Index.AddAsync(vectorId, vector, cancellationToken);
            _LastAddUtc = DateTime.UtcNow;
        }

        /// <inheritdoc />
        public async Task AddBatchAsync(Dictionary<Guid, List<float>> vectors, CancellationToken cancellationToken = default)
        {
            if (vectors == null || vectors.Count == 0) return;
            if (_Index == null) throw new InvalidOperationException("Index not initialized.");

            await _Index.AddNodesAsync(vectors, cancellationToken);
            _LastAddUtc = DateTime.UtcNow;
        }

        /// <inheritdoc />
        public async Task UpdateAsync(Guid vectorId, List<float> vector, CancellationToken cancellationToken = default)
        {
            if (vectorId == Guid.Empty) throw new ArgumentException("Vector ID cannot be empty.");
            if (vector == null || vector.Count == 0) throw new ArgumentNullException(nameof(vector));
            if (_Index == null) throw new InvalidOperationException("Index not initialized.");

            // HnswLite doesn't support update directly, so we remove and re-add
            await RemoveAsync(vectorId, cancellationToken);
            await AddAsync(vectorId, vector, cancellationToken);
        }

        /// <inheritdoc />
        public async Task RemoveAsync(Guid vectorId, CancellationToken cancellationToken = default)
        {
            if (vectorId == Guid.Empty) throw new ArgumentException("Vector ID cannot be empty.");
            if (_Index == null) throw new InvalidOperationException("Index not initialized.");

            await _Index.RemoveAsync(vectorId, cancellationToken);
            _LastRemoveUtc = DateTime.UtcNow;
        }

        /// <inheritdoc />
        public async Task RemoveBatchAsync(List<Guid> vectorIds, CancellationToken cancellationToken = default)
        {
            if (vectorIds == null || vectorIds.Count == 0) return;
            if (_Index == null) throw new InvalidOperationException("Index not initialized.");

            await _Index.RemoveNodesAsync(vectorIds, cancellationToken);
            _LastRemoveUtc = DateTime.UtcNow;
        }

        /// <inheritdoc />
        public async Task<List<(Guid Id, float Distance)>> SearchAsync(
            List<float> queryVector,
            int k,
            int? ef = null,
            CancellationToken cancellationToken = default)
        {
            if (queryVector == null || queryVector.Count == 0) throw new ArgumentNullException(nameof(queryVector));
            if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k), "k must be greater than 0.");
            if (_Index == null) throw new InvalidOperationException("Index not initialized.");

            // Set search ef parameter (defaults to graph's VectorIndexEf or 50)
            int searchEf = ef ?? _Graph?.VectorIndexEf ?? 50;
            if (_Storage?.EntryPoint == null)
            {
                _LastSearchUtc = DateTime.UtcNow;
                return new List<(Guid Id, float Distance)>();
            }
            IEnumerable<VectorResult> results = await _Index.GetTopKAsync(queryVector, k, Math.Max(searchEf, k), cancellationToken);
            _LastSearchUtc = DateTime.UtcNow;

            return results.Select(r => (r.GUID, r.Distance)).ToList();
        }

        /// <inheritdoc />
        public async Task SaveAsync(CancellationToken cancellationToken = default)
        {
            if (_Index == null) throw new InvalidOperationException("Index not initialized.");

            // For SQLite storage, the index is automatically persisted
            // For RAM storage, we need to export/import state
            if (_Graph?.VectorIndexType == VectorIndexTypeEnum.HnswRam && !string.IsNullOrEmpty(_Graph.VectorIndexFile))
            {
                // For RAM storage, provide basic statistics rather than full state export
                // The HnswLite library doesn't provide a public HnswState export method
                VectorIndexStatistics stats = GetStatistics();
                string json = System.Text.Json.JsonSerializer.Serialize(stats);
                await File.WriteAllTextAsync(_Graph.VectorIndexFile, json, cancellationToken);
            }
        }

        /// <inheritdoc />
        public async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            if (_Index == null) throw new InvalidOperationException("Index not initialized.");

            if (_Graph?.VectorIndexType == VectorIndexTypeEnum.HnswRam && !string.IsNullOrEmpty(_Graph.VectorIndexFile))
            {
                if (File.Exists(_Graph.VectorIndexFile))
                {
                    // For RAM storage, we just log that we found a previous state file
                    // The HnswLite library doesn't provide a public ImportState method
                    // Full state management would need to be implemented in a future version
                }
            }
        }

        /// <inheritdoc />
        public VectorIndexStatistics GetStatistics()
        {
            int vectorCount = 0;
            try
            {
                if (_Storage != null)
                {
                    vectorCount = _Storage.GetCountAsync().Result;
                }
            }
            catch
            {
                // Fallback to 0 if we can't get the count
            }

            VectorIndexStatistics stats = new VectorIndexStatistics
            {
                VectorCount = vectorCount,
                Dimensions = _Graph?.VectorDimensionality ?? 0,
                IndexType = _Graph?.VectorIndexType ?? VectorIndexTypeEnum.None,
                M = _Graph?.VectorIndexM ?? 16,
                EfConstruction = _Graph?.VectorIndexEfConstruction ?? 200,
                DefaultEf = _Graph?.VectorIndexEf ?? 50,
                IndexFile = _Graph?.VectorIndexFile,
                LastRebuildUtc = _LastRebuildUtc,
                LastAddUtc = _LastAddUtc,
                LastRemoveUtc = _LastRemoveUtc,
                LastSearchUtc = _LastSearchUtc,
                IsLoaded = _Index != null,
                DistanceMetric = "Cosine"
            };

            // Calculate index file size
            if (!string.IsNullOrEmpty(stats.IndexFile))
            {
                long totalSize = 0;

                // For SQLite indices, include both main and layer files
                if (_Graph?.VectorIndexType == VectorIndexTypeEnum.HnswSqlite)
                {
                    // Check main storage file
                    if (File.Exists(stats.IndexFile))
                    {
                        totalSize += new FileInfo(stats.IndexFile).Length;
                    }

                    // Check layer storage file
                    string layerFile = stats.IndexFile + ".layers";
                    if (File.Exists(layerFile))
                    {
                        totalSize += new FileInfo(layerFile).Length;
                    }
                }
                else
                {
                    // For RAM indices or single files
                    if (File.Exists(stats.IndexFile))
                    {
                        totalSize = new FileInfo(stats.IndexFile).Length;
                    }
                }

                stats.IndexFileSizeBytes = totalSize;
            }

            // Estimate memory usage (rough approximation)
            if (_Index != null && stats.VectorCount > 0)
            {
                int vectorSize = stats.Dimensions * sizeof(float);
                int connectionsPerNode = stats.M * 2; // Bidirectional
                int connectionSize = connectionsPerNode * sizeof(int);
                stats.EstimatedMemoryBytes = stats.VectorCount * (vectorSize + connectionSize);
            }

            return stats;
        }


        /// <inheritdoc />
        public async Task ClearAsync(CancellationToken cancellationToken = default)
        {
            if (_Index == null) throw new InvalidOperationException("Index not initialized.");

            // Reinitialize the index
            await InitializeAsync(_Graph, cancellationToken);
        }

        /// <inheritdoc />
        public bool Contains(Guid vectorId)
        {
            try
            {
                if (_Storage != null)
                {
                    TryGetNodeResult result = _Storage.TryGetNodeAsync(vectorId).Result;
                    return result.Success;
                }
            }
            catch
            {
                // Fallback to false if we can't check
            }
            return false;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion

        #region Private-Methods

        /// <summary>
        /// Dispose of resources.
        /// </summary>
        /// <param name="disposing">Disposing.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    // HnswIndex doesn't implement IDisposable
                    // But we should dispose of our storage if it's disposable
                    _Index = null;
                }
                _Disposed = true;
            }
        }

        #endregion

        #region Storage-Implementations

        /// <summary>
        /// In-memory storage implementation for HNSW index.
        /// </summary>
        private class RamHnswStorage : IHnswStorage
        {
            private readonly Dictionary<Guid, IHnswNode> _Nodes = new();
            private Guid? _EntryPoint;

            public Guid? EntryPoint { get => _EntryPoint; set => _EntryPoint = value; }

            public Task AddNodeAsync(Guid id, List<float> vector, CancellationToken cancellationToken = default)
            {
                HnswNode node = new HnswNode(id, vector);
                _Nodes[id] = node;
                return Task.CompletedTask;
            }

            public Task RemoveNodeAsync(Guid id, CancellationToken cancellationToken = default)
            {
                _Nodes.Remove(id);
                if (_EntryPoint == id)
                    _EntryPoint = null;
                return Task.CompletedTask;
            }

            public Task<IHnswNode> GetNodeAsync(Guid id, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_Nodes.GetValueOrDefault(id));
            }

            public Task<TryGetNodeResult> TryGetNodeAsync(Guid id, CancellationToken cancellationToken = default)
            {
                if (_Nodes.TryGetValue(id, out IHnswNode node))
                    return Task.FromResult(TryGetNodeResult.Found(node));
                return Task.FromResult(TryGetNodeResult.NotFound());
            }

            public Task<IEnumerable<Guid>> GetAllNodeIdsAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult<IEnumerable<Guid>>(_Nodes.Keys);
            }

            public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(_Nodes.Count);
            }

            public Task AddNodesAsync(Dictionary<Guid, List<float>> nodes, CancellationToken cancellationToken = default)
            {
                foreach (KeyValuePair<Guid, List<float>> kvp in nodes)
                {
                    HnswNode node = new HnswNode(kvp.Key, kvp.Value);
                    _Nodes[kvp.Key] = node;
                }
                return Task.CompletedTask;
            }

            public Task RemoveNodesAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
            {
                foreach (Guid id in ids)
                {
                    _Nodes.Remove(id);
                    if (_EntryPoint == id)
                        _EntryPoint = null;
                }
                return Task.CompletedTask;
            }

            public Task<Dictionary<Guid, IHnswNode>> GetNodesAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
            {
                Dictionary<Guid, IHnswNode> result = new Dictionary<Guid, IHnswNode>();
                foreach (Guid id in ids)
                {
                    if (_Nodes.TryGetValue(id, out IHnswNode node))
                        result[id] = node;
                }
                return Task.FromResult(result);
            }
        }

        /// <summary>
        /// Simple node implementation for HNSW.
        /// </summary>
        private class HnswNode : IHnswNode
        {
            public Guid Id { get; }
            public List<float> Vector { get; }
            public Dictionary<int, HashSet<Guid>> Connections { get; }

            public HnswNode(Guid id, List<float> vector)
            {
                Id = id;
                Vector = vector;
                Connections = new Dictionary<int, HashSet<Guid>>();
            }

            public Dictionary<int, HashSet<Guid>> GetNeighbors()
            {
                return Connections;
            }

            public void AddNeighbor(int layer, Guid neighborId)
            {
                if (!Connections.ContainsKey(layer))
                    Connections[layer] = new HashSet<Guid>();
                Connections[layer].Add(neighborId);
            }

            public void RemoveNeighbor(int layer, Guid neighborId)
            {
                if (Connections.TryGetValue(layer, out HashSet<Guid> neighbors))
                    neighbors.Remove(neighborId);
            }
        }

        /// <summary>
        /// SQLite storage implementation for HNSW index.
        /// </summary>
        private class SqliteHnswStorage : IHnswStorage, IDisposable
        {
            private readonly string _FilePath;
            private readonly RamHnswStorage _Storage = new();
            private readonly object _Lock = new object();
            private bool _Disposed = false;

            public SqliteHnswStorage(string filePath)
            {
                _FilePath = filePath;
                LoadFromFile();
            }

            public Guid? EntryPoint
            {
                get => _Storage.EntryPoint;
                set
                {
                    _Storage.EntryPoint = value;
                    SaveToFile();
                }
            }

            public async Task AddNodeAsync(Guid id, List<float> vector, CancellationToken cancellationToken = default)
            {
                await _Storage.AddNodeAsync(id, vector, cancellationToken);
                SaveToFile();
            }

            public async Task RemoveNodeAsync(Guid id, CancellationToken cancellationToken = default)
            {
                await _Storage.RemoveNodeAsync(id, cancellationToken);
                SaveToFile();
            }

            public Task<IHnswNode> GetNodeAsync(Guid id, CancellationToken cancellationToken = default)
            {
                return _Storage.GetNodeAsync(id, cancellationToken);
            }

            public Task<TryGetNodeResult> TryGetNodeAsync(Guid id, CancellationToken cancellationToken = default)
            {
                return _Storage.TryGetNodeAsync(id, cancellationToken);
            }

            public Task<IEnumerable<Guid>> GetAllNodeIdsAsync(CancellationToken cancellationToken = default)
            {
                return _Storage.GetAllNodeIdsAsync(cancellationToken);
            }

            public Task<int> GetCountAsync(CancellationToken cancellationToken = default)
            {
                return _Storage.GetCountAsync(cancellationToken);
            }

            public async Task AddNodesAsync(Dictionary<Guid, List<float>> nodes, CancellationToken cancellationToken = default)
            {
                await _Storage.AddNodesAsync(nodes, cancellationToken);
                SaveToFile();
            }

            public async Task RemoveNodesAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
            {
                await _Storage.RemoveNodesAsync(ids, cancellationToken);
                SaveToFile();
            }

            public Task<Dictionary<Guid, IHnswNode>> GetNodesAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
            {
                return _Storage.GetNodesAsync(ids, cancellationToken);
            }

            private void SaveToFile()
            {
                if (_Disposed) return;

                lock (_Lock)
                {
                    try
                    {
                        var allNodeIds = _Storage.GetAllNodeIdsAsync().Result?.ToList() ?? new List<Guid>();
                        var nodes = new List<object>();

                        foreach (var nodeId in allNodeIds)
                        {
                            var node = _Storage.GetNodeAsync(nodeId).Result;
                            if (node == null) continue;
                            nodes.Add(new
                            {
                                Id = nodeId,
                                Vector = node.Vector
                            });
                        }

                        object data = new
                        {
                            EntryPoint = _Storage.EntryPoint,
                            NodeCount = _Storage.GetCountAsync().Result,
                            LastSaved = DateTime.UtcNow,
                            Node = nodes
                        };

                        string json = System.Text.Json.JsonSerializer.Serialize(data, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                        // Ensure directory exists
                        string directory = Path.GetDirectoryName(_FilePath);
                        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        {
                            Directory.CreateDirectory(directory);
                        }

                        File.WriteAllText(_FilePath, json);
                    }
                    catch
                    {
                        // Best effort save, don't throw on file errors
                    }
                }
            }

            private void LoadFromFile()
            {
                if (!File.Exists(_FilePath)) return;

                lock (_Lock)
                {
                    try
                    {
                        string json = File.ReadAllText(_FilePath);
                        using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(json);

                        // Load nodes first
                        if (document.RootElement.TryGetProperty("Node", out System.Text.Json.JsonElement nodesElement) &&
                            nodesElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var nodeEl in nodesElement.EnumerateArray())
                            {
                                if (!nodeEl.TryGetProperty("Id", out var idEl)) continue;
                                if (idEl.ValueKind != System.Text.Json.JsonValueKind.String) continue;
                                if (!Guid.TryParse(idEl.GetString(), out Guid id)) continue;

                                if (!nodeEl.TryGetProperty("Vector", out var vecEl)) continue;
                                var vector = new List<float>();
                                if (vecEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    foreach (var v in vecEl.EnumerateArray())
                                    {
                                        if (v.ValueKind == System.Text.Json.JsonValueKind.Number && v.TryGetSingle(out float f))
                                            vector.Add(f);
                                    }
                                }
                                _Storage.AddNodeAsync(id, vector).Wait();
                            }
                        }

                        if (document.RootElement.TryGetProperty("EntryPoint", out System.Text.Json.JsonElement entryPointElement))
                        {
                            if (entryPointElement.ValueKind == System.Text.Json.JsonValueKind.String &&
                                Guid.TryParse(entryPointElement.GetString(), out Guid entryPointGuid))
                            {
                                _Storage.EntryPoint = entryPointGuid;
                            }
                        }
                    }
                    catch
                    {
                        // Best effort load, don't throw on file errors
                    }
                }
            }
            public void Dispose()
            {
                if (!_Disposed)
                {
                    SaveToFile();
                    _Disposed = true;
                }
            }
        }

        /// <summary>
        /// In-memory layer storage implementation.
        /// </summary>
        private class RamHnswLayerStorage : IHnswLayerStorage
        {
            private readonly Dictionary<Guid, int> _NodeLayers = new Dictionary<Guid, int>();

            public int Count => _NodeLayers.Count;

            public int GetNodeLayer(Guid nodeId)
            {
                return _NodeLayers.GetValueOrDefault(nodeId, 0);
            }

            public void SetNodeLayer(Guid nodeId, int layer)
            {
                _NodeLayers[nodeId] = layer;
            }

            public void RemoveNodeLayer(Guid nodeId)
            {
                _NodeLayers.Remove(nodeId);
            }

            public Dictionary<Guid, int> GetAllNodeLayers()
            {
                return new Dictionary<Guid, int>(_NodeLayers);
            }

            public void Clear()
            {
                _NodeLayers.Clear();
            }
        }

        /// <summary>
        /// SQLite-based layer storage implementation.
        /// </summary>
        private class SqliteHnswLayerStorage : IHnswLayerStorage, IDisposable
        {
            private readonly string _FilePath;
            private readonly RamHnswLayerStorage _Storage = new RamHnswLayerStorage();
            private readonly object _Lock = new object();
            private bool _Disposed = false;
            private static Serializer Serializer = new Serializer();

            public SqliteHnswLayerStorage(string filePath)
            {
                _FilePath = filePath;
                LoadFromFile();
            }

            public int Count => _Storage.Count;

            public int GetNodeLayer(Guid nodeId)
            {
                return _Storage.GetNodeLayer(nodeId);
            }

            public void SetNodeLayer(Guid nodeId, int layer)
            {
                _Storage.SetNodeLayer(nodeId, layer);
                SaveToFile();
            }

            public void RemoveNodeLayer(Guid nodeId)
            {
                _Storage.RemoveNodeLayer(nodeId);
                SaveToFile();
            }

            public Dictionary<Guid, int> GetAllNodeLayers()
            {
                return _Storage.GetAllNodeLayers();
            }

            public void Clear()
            {
                _Storage.Clear();
                SaveToFile();
            }

            private void LoadFromFile()
            {
                if (_Disposed) return;

                lock (_Lock)
                {
                    if (File.Exists(_FilePath))
                    {
                        try
                        {
                            string json = File.ReadAllText(_FilePath);
                            if (!string.IsNullOrEmpty(json))
                            {
                                Dictionary<string, int> data = Serializer.DeserializeJson<Dictionary<string, int>>(json);
                                foreach (KeyValuePair<string, int> kvp in data)
                                {
                                    if (Guid.TryParse(kvp.Key, out Guid id))
                                        _Storage.SetNodeLayer(id, kvp.Value);
                                }
                            }
                        }
                        catch (Exception)
                        {
                            // Continue, start with empty storage
                        }
                    }
                }
            }

            private void SaveToFile()
            {
                if (_Disposed) return;

                lock (_Lock)
                {
                    try
                    {
                        Dictionary<string, int> data = _Storage.GetAllNodeLayers()
                            .ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value);
                        string json = Serializer.SerializeJson(data, true);

                        string directory = Path.GetDirectoryName(_FilePath);
                        if (!Directory.Exists(directory))
                            Directory.CreateDirectory(directory);

                        File.WriteAllText(_FilePath, json);
                    }
                    catch (Exception)
                    {
                        // Don't throw, layer storage is not critical for basic functionality
                    }
                }
            }

            public void Dispose()
            {
                if (!_Disposed)
                {
                    SaveToFile();
                    _Disposed = true;
                }
            }
        }

        #endregion
    }
}