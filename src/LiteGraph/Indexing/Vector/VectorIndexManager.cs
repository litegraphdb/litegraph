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
        private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _IndexLocks;
        private readonly string _StorageDirectory;
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
            _IndexLocks = new ConcurrentDictionary<Guid, SemaphoreSlim>();

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
            ThrowIfDisposed();

            if (graph == null) throw new ArgumentNullException(nameof(graph));
            SemaphoreSlim indexLock = GetIndexLock(graph.GUID);
            await indexLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                return await GetOrCreateIndexUnlockedAsync(graph, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                indexLock.Release();
            }
        }

        /// <summary>
        /// Execute an operation against a graph index while holding the graph's index lock.
        /// </summary>
        /// <param name="graph">Graph containing index configuration.</param>
        /// <param name="operation">Operation to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Operation result.</returns>
        public async Task<TResult> ExecuteWithIndexAsync<TResult>(
            Graph graph,
            Func<IVectorIndex, Task<TResult>> operation,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (graph == null) throw new ArgumentNullException(nameof(graph));
            if (operation == null) throw new ArgumentNullException(nameof(operation));

            SemaphoreSlim indexLock = GetIndexLock(graph.GUID);
            await indexLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                IVectorIndex index = await GetOrCreateIndexUnlockedAsync(graph, cancellationToken).ConfigureAwait(false);
                if (index == null) return default;
                return await operation(index).ConfigureAwait(false);
            }
            finally
            {
                indexLock.Release();
            }
        }

        /// <summary>
        /// Execute an operation against a graph index while holding the graph's index lock.
        /// </summary>
        /// <param name="graph">Graph containing index configuration.</param>
        /// <param name="operation">Operation to execute.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Task.</returns>
        public async Task ExecuteWithIndexAsync(
            Graph graph,
            Func<IVectorIndex, Task> operation,
            CancellationToken cancellationToken = default)
        {
            await ExecuteWithIndexAsync<object>(
                graph,
                async index =>
                {
                    await operation(index).ConfigureAwait(false);
                    return null;
                },
                cancellationToken).ConfigureAwait(false);
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
            ThrowIfDisposed();

            if (graph == null) throw new ArgumentNullException(nameof(graph));
            if (indexType == VectorIndexTypeEnum.None)
                throw new ArgumentException("Index type cannot be None.");
            if (!graph.VectorDimensionality.HasValue)
                throw new ArgumentException("Graph must have VectorDimensionality set before enabling indexing.");

            SemaphoreSlim indexLock = GetIndexLock(graph.GUID);
            await indexLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Remove existing index if any
                if (_Indexes.TryRemove(graph.GUID, out IVectorIndex existingIndex))
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
                    string extension = indexType == VectorIndexTypeEnum.HnswRam ? ".hnsw" : ".sqlite";
                    graph.VectorIndexFile = Path.Combine(_StorageDirectory, $"{graph.GUID}{extension}");
                }

                // Create and initialize new index
                HnswLiteVectorIndex index = new HnswLiteVectorIndex();
                await index.InitializeAsync(graph, cancellationToken).ConfigureAwait(false);

                _Indexes[graph.GUID] = index;
                return index;
            }
            finally
            {
                indexLock.Release();
            }
        }

        /// <summary>
        /// Disable indexing for a graph.
        /// </summary>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="deleteIndexFile">Whether to delete the index file.</param>
        /// <returns>Task.</returns>
        public async Task DisableIndexingAsync(Guid graphGuid, bool deleteIndexFile = false)
        {
            ThrowIfDisposed();

            SemaphoreSlim indexLock = GetIndexLock(graphGuid);
            await indexLock.WaitAsync().ConfigureAwait(false);
            try
            {
                if (_Indexes.TryRemove(graphGuid, out IVectorIndex index))
                {
                    VectorIndexStatistics stats = index.GetStatistics();
                    index.Dispose();

                    if (deleteIndexFile && !string.IsNullOrEmpty(stats.IndexFile))
                    {
                        try
                        {
                            if (File.Exists(stats.IndexFile))
                                File.Delete(stats.IndexFile);

                            // Also delete mapping file for RAM indexes
                            string mappingFile = stats.IndexFile + ".mapping";
                            if (File.Exists(mappingFile))
                                File.Delete(mappingFile);
                        }
                        catch
                        {
                            // Best effort deletion
                        }
                    }
                }
            }
            finally
            {
                indexLock.Release();
            }

            await Task.CompletedTask.ConfigureAwait(false);
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
            IEnumerable<VectorIndexEntry> entries = vectors?
                .Where(vector => vector != null && vector.Vectors != null && vector.Vectors.Count > 0 && vector.NodeGUID.HasValue)
                .Select(vector => VectorIndexEntry.FromVectorMetadata(vector, graph))
                .ToList();

            return await RebuildIndexAsync(graph, entries, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Rebuild the index for a graph from scratch using fully resolved index entries.
        /// </summary>
        /// <param name="graph">Graph to rebuild index for.</param>
        /// <param name="entries">All index entries to add to the index.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Rebuilt index.</returns>
        public async Task<IVectorIndex> RebuildIndexAsync(
            Graph graph,
            IEnumerable<VectorIndexEntry> entries,
            CancellationToken cancellationToken = default)
        {
            ThrowIfDisposed();

            if (graph == null) throw new ArgumentNullException(nameof(graph));
            if (!graph.VectorIndexType.HasValue || graph.VectorIndexType == VectorIndexTypeEnum.None)
                throw new ArgumentException("Graph must have indexing enabled.");

            SemaphoreSlim indexLock = GetIndexLock(graph.GUID);
            await indexLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                // Remove existing index
                if (_Indexes.TryRemove(graph.GUID, out IVectorIndex existingIndex))
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

                        string mappingFile = graph.VectorIndexFile + ".mapping";
                        if (File.Exists(mappingFile))
                            File.Delete(mappingFile);
                    }
                    catch
                    {
                        // Best effort deletion
                    }
                }

                // Create new index
                HnswLiteVectorIndex index = new HnswLiteVectorIndex();
                await index.InitializeAsync(graph, cancellationToken).ConfigureAwait(false);

                // Add all vectors in batches
                if (entries != null)
                {
                    List<VectorIndexEntry> batch = new List<VectorIndexEntry>();
                    const int batchSize = 1000;

                    foreach (VectorIndexEntry entry in entries)
                    {
                        if (entry == null || entry.Vector == null || entry.Vector.Count < 1) continue;

                        batch.Add(entry);
                        if (batch.Count >= batchSize)
                        {
                            await index.AddBatchAsync(batch, cancellationToken).ConfigureAwait(false);
                            batch.Clear();
                        }
                    }

                    // Add remaining vectors
                    if (batch.Count > 0)
                    {
                        await index.AddBatchAsync(batch, cancellationToken).ConfigureAwait(false);
                    }

                    // Save the index
                    await index.SaveAsync(cancellationToken).ConfigureAwait(false);
                }

                _Indexes[graph.GUID] = index;
                return index;
            }
            finally
            {
                indexLock.Release();
            }
        }

        /// <summary>
        /// Get statistics for a graph's index.
        /// </summary>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <returns>Index statistics or null if no index exists.</returns>
        public VectorIndexStatistics GetStatistics(Guid graphGuid)
        {
            ThrowIfDisposed();

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
            ThrowIfDisposed();

            return _Indexes.ContainsKey(graphGuid);
        }

        /// <summary>
        /// Get the index for a graph.
        /// </summary>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <returns>Vector index or null if not found.</returns>
        public IVectorIndex GetIndex(Guid graphGuid)
        {
            ThrowIfDisposed();

            _Indexes.TryGetValue(graphGuid, out IVectorIndex index);
            return index;
        }

        /// <summary>
        /// Remove an index from memory without deleting files.
        /// </summary>
        /// <param name="graphGuid">Graph GUID.</param>
        public void UnloadIndex(Guid graphGuid)
        {
            ThrowIfDisposed();

            if (_Indexes.TryRemove(graphGuid, out IVectorIndex index))
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
            ThrowIfDisposed();

            List<Task> tasks = new List<Task>();
            foreach (Guid graphGuid in _Indexes.Keys.ToList())
            {
                tasks.Add(SaveIndexAsync(graphGuid, cancellationToken));
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
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
                    foreach (IVectorIndex index in _Indexes.Values)
                    {
                        index?.Dispose();
                    }
                    _Indexes.Clear();

                    foreach (SemaphoreSlim indexLock in _IndexLocks.Values)
                    {
                        indexLock.Dispose();
                    }
                    _IndexLocks.Clear();
                }
                _Disposed = true;
            }
        }

        private async Task<IVectorIndex> GetOrCreateIndexUnlockedAsync(Graph graph, CancellationToken cancellationToken)
        {
            if (!graph.VectorIndexType.HasValue || graph.VectorIndexType == VectorIndexTypeEnum.None)
                return null;

            return await _Indexes.GetOrAddAsync<Guid, IVectorIndex>(graph.GUID, async (guid) =>
            {
                HnswLiteVectorIndex index = new HnswLiteVectorIndex();

                // Ensure index file path is set
                if (string.IsNullOrEmpty(graph.VectorIndexFile))
                {
                    graph.VectorIndexFile = Path.Combine(_StorageDirectory, $"{graph.GUID}.hnsw");
                }
                else if (!Path.IsPathRooted(graph.VectorIndexFile))
                {
                    graph.VectorIndexFile = Path.Combine(_StorageDirectory, graph.VectorIndexFile);
                }

                await index.InitializeAsync(graph, cancellationToken).ConfigureAwait(false);
                return index;
            }).ConfigureAwait(false);
        }

        private SemaphoreSlim GetIndexLock(Guid graphGuid)
        {
            return _IndexLocks.GetOrAdd(graphGuid, _ => new SemaphoreSlim(1, 1));
        }

        private async Task SaveIndexAsync(Guid graphGuid, CancellationToken cancellationToken)
        {
            SemaphoreSlim indexLock = GetIndexLock(graphGuid);
            await indexLock.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                if (_Indexes.TryGetValue(graphGuid, out IVectorIndex index))
                {
                    await index.SaveAsync(cancellationToken).ConfigureAwait(false);
                }
            }
            finally
            {
                indexLock.Release();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_Disposed) throw new ObjectDisposedException(nameof(VectorIndexManager));
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
            if (dictionary.TryGetValue(key, out TValue value))
                return value;

            value = await valueFactory(key);
            TValue result = dictionary.GetOrAdd(key, value);

            if (!EqualityComparer<TValue>.Default.Equals(result, value) && value is IDisposable disposable)
            {
                disposable.Dispose();
            }

            return result;
        }
    }
}
