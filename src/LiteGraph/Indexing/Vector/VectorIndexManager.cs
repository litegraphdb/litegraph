namespace LiteGraph.Indexing.Vector
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Manages vector indexes for graphs.
    /// </summary>
    public class VectorIndexManager : IDisposable
    {
        #region Private-Members

        private readonly ConcurrentDictionary<Guid, IVectorIndex> _Indexes;
        private readonly string _StorageDirectory;
        private readonly object _Lock = new object();
        private bool _Disposed = false;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the vector index manager.
        /// </summary>
        /// <param name="storageDirectory">Directory for storing index files.</param>
        public VectorIndexManager(string storageDirectory = null)
        {
            _StorageDirectory = storageDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "indexes");
            _Indexes = new ConcurrentDictionary<Guid, IVectorIndex>();

            if (!Directory.Exists(_StorageDirectory))
            {
                Directory.CreateDirectory(_StorageDirectory);
            }
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Get or create an index for a graph.
        /// </summary>
        /// <param name="graph">Graph to get index for.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Vector index or null if indexing is not enabled.</returns>
        public async Task<IVectorIndex> GetOrCreateIndexAsync(Graph graph, CancellationToken cancellationToken = default)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            if (!graph.VectorIndexType.HasValue || graph.VectorIndexType == VectorIndexTypeEnum.None)
                return null;

            return await _Indexes.GetOrAddAsync<Guid, IVectorIndex>(graph.GUID, async (guid) =>
            {
                var index = new HnswLiteVectorIndex();
                
                // Ensure index file path is set
                if (string.IsNullOrEmpty(graph.VectorIndexFile))
                {
                    graph.VectorIndexFile = Path.Combine(_StorageDirectory, $"{graph.GUID}.hnsw");
                }
                else if (!Path.IsPathRooted(graph.VectorIndexFile))
                {
                    graph.VectorIndexFile = Path.Combine(_StorageDirectory, graph.VectorIndexFile);
                }

                await index.InitializeAsync(graph, cancellationToken);
                return index;
            });
        }

        /// <summary>
        /// Enable indexing for a graph.
        /// </summary>
        /// <param name="graph">Graph to enable indexing for.</param>
        /// <param name="indexType">Type of index to create.</param>
        /// <param name="indexFile">Optional index file path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Created index.</returns>
        public async Task<IVectorIndex> EnableIndexingAsync(
            Graph graph, 
            VectorIndexTypeEnum indexType,
            string indexFile = null,
            CancellationToken cancellationToken = default)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            if (indexType == VectorIndexTypeEnum.None)
                throw new ArgumentException("Index type cannot be None.");
            if (!graph.VectorDimensionality.HasValue)
                throw new ArgumentException("Graph must have VectorDimensionality set before enabling indexing.");

            // Remove existing index if any
            if (_Indexes.TryRemove(graph.GUID, out var existingIndex))
            {
                existingIndex.Dispose();
            }

            // Update graph properties
            graph.VectorIndexType = indexType;
            
            if (!string.IsNullOrEmpty(indexFile))
            {
                graph.VectorIndexFile = Path.IsPathRooted(indexFile) 
                    ? indexFile 
                    : Path.Combine(_StorageDirectory, indexFile);
            }
            else
            {
                var extension = indexType == VectorIndexTypeEnum.HnswRam ? ".hnsw" : ".sqlite";
                graph.VectorIndexFile = Path.Combine(_StorageDirectory, $"{graph.GUID}{extension}");
            }

            // Create and initialize new index
            var index = new HnswLiteVectorIndex();
            await index.InitializeAsync(graph, cancellationToken);
            
            _Indexes[graph.GUID] = index;
            return index;
        }

        /// <summary>
        /// Disable indexing for a graph.
        /// </summary>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="deleteIndexFile">Whether to delete the index file.</param>
        /// <returns>Task.</returns>
        public async Task DisableIndexingAsync(Guid graphGuid, bool deleteIndexFile = false)
        {
            if (_Indexes.TryRemove(graphGuid, out var index))
            {
                var stats = index.GetStatistics();
                index.Dispose();

                if (deleteIndexFile && !string.IsNullOrEmpty(stats.IndexFile))
                {
                    try
                    {
                        if (File.Exists(stats.IndexFile))
                            File.Delete(stats.IndexFile);
                        
                        // Also delete mapping file for RAM indexes
                        var mappingFile = stats.IndexFile + ".mapping";
                        if (File.Exists(mappingFile))
                            File.Delete(mappingFile);
                    }
                    catch
                    {
                        // Best effort deletion
                    }
                }
            }
            
            await Task.CompletedTask;
        }

        /// <summary>
        /// Rebuild the index for a graph from scratch.
        /// </summary>
        /// <param name="graph">Graph to rebuild index for.</param>
        /// <param name="vectors">All vectors to add to the index.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Rebuilt index.</returns>
        public async Task<IVectorIndex> RebuildIndexAsync(
            Graph graph,
            IEnumerable<VectorMetadata> vectors,
            CancellationToken cancellationToken = default)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            if (!graph.VectorIndexType.HasValue || graph.VectorIndexType == VectorIndexTypeEnum.None)
                throw new ArgumentException("Graph must have indexing enabled.");

            // Remove existing index
            if (_Indexes.TryRemove(graph.GUID, out var existingIndex))
            {
                existingIndex.Dispose();
            }

            // Delete existing index files
            if (!string.IsNullOrEmpty(graph.VectorIndexFile))
            {
                try
                {
                    if (File.Exists(graph.VectorIndexFile))
                        File.Delete(graph.VectorIndexFile);
                    
                    var mappingFile = graph.VectorIndexFile + ".mapping";
                    if (File.Exists(mappingFile))
                        File.Delete(mappingFile);
                }
                catch
                {
                    // Best effort deletion
                }
            }

            // Create new index
            var index = new HnswLiteVectorIndex();
            await index.InitializeAsync(graph, cancellationToken);

            // Add all vectors in batches
            if (vectors != null)
            {
                var batch = new Dictionary<Guid, List<float>>();
                const int batchSize = 1000;

                foreach (var vector in vectors)
                {
                    if (vector.Vectors != null && vector.Vectors.Count > 0 && vector.NodeGUID.HasValue)
                    {
                        // Use NodeGUID as the key, not vector.GUID, so search can find the node
                        batch[vector.NodeGUID.Value] = vector.Vectors;

                        if (batch.Count >= batchSize)
                        {
                            await index.AddBatchAsync(batch, cancellationToken);
                            batch.Clear();
                        }
                    }
                }

                // Add remaining vectors
                if (batch.Count > 0)
                {
                    await index.AddBatchAsync(batch, cancellationToken);
                }

                // Save the index
                await index.SaveAsync(cancellationToken);
            }

            _Indexes[graph.GUID] = index;
            return index;
        }

        /// <summary>
        /// Get statistics for a graph's index.
        /// </summary>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <returns>Index statistics or null if no index exists.</returns>
        public VectorIndexStatistics GetStatistics(Guid graphGuid)
        {
            if (_Indexes.TryGetValue(graphGuid, out var index))
            {
                return index.GetStatistics();
            }
            return null;
        }

        /// <summary>
        /// Check if a graph has an active index.
        /// </summary>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <returns>True if an index exists.</returns>
        public bool HasIndex(Guid graphGuid)
        {
            return _Indexes.ContainsKey(graphGuid);
        }

        /// <summary>
        /// Get the index for a graph.
        /// </summary>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <returns>Vector index or null if not found.</returns>
        public IVectorIndex GetIndex(Guid graphGuid)
        {
            _Indexes.TryGetValue(graphGuid, out var index);
            return index;
        }

        /// <summary>
        /// Remove an index from memory without deleting files.
        /// </summary>
        /// <param name="graphGuid">Graph GUID.</param>
        public void UnloadIndex(Guid graphGuid)
        {
            if (_Indexes.TryRemove(graphGuid, out var index))
            {
                index.Dispose();
            }
        }

        /// <summary>
        /// Save all indexes to persistent storage.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task SaveAllAsync(CancellationToken cancellationToken = default)
        {
            var tasks = _Indexes.Values.Select(index => index.SaveAsync(cancellationToken));
            await Task.WhenAll(tasks);
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
                    foreach (var index in _Indexes.Values)
                    {
                        index?.Dispose();
                    }
                    _Indexes.Clear();
                }
                _Disposed = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// Extension methods for ConcurrentDictionary to support async factory.
    /// </summary>
    internal static class ConcurrentDictionaryExtensions
    {
        /// <summary>
        /// Get or add with async factory.
        /// </summary>
        public static async Task<TValue> GetOrAddAsync<TKey, TValue>(
            this ConcurrentDictionary<TKey, TValue> dictionary,
            TKey key,
            Func<TKey, Task<TValue>> valueFactory)
        {
            if (dictionary.TryGetValue(key, out var value))
                return value;

            value = await valueFactory(key);
            return dictionary.GetOrAdd(key, value);
        }
    }
}