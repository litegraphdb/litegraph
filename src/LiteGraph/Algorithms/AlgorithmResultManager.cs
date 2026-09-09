namespace LiteGraph.Algorithms
{
    using System;
    using System.Collections.Concurrent;

    /// <summary>
    /// In-memory cache of graph algorithm results, keyed by graph and algorithm request signature.
    /// A cached result is returned only when the graph's node and edge counts match the counts captured at compute time, which auto-invalidates the entry on any node or edge addition or removal.
    /// Structural rewrites that preserve both counts are not detected automatically; call <see cref="Invalidate(System.Guid)"/> after such changes.
    /// This type is thread-safe.
    /// </summary>
    public class AlgorithmResultManager
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private readonly ConcurrentDictionary<string, CacheEntry> _Entries = new ConcurrentDictionary<string, CacheEntry>(StringComparer.Ordinal);

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public AlgorithmResultManager()
        {

        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Build a cache key for a graph and algorithm request.
        /// </summary>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="request">Algorithm request.</param>
        /// <returns>Cache key.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
        public static string BuildKey(Guid graphGuid, GraphAlgorithmRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            return graphGuid.ToString()
                + "|" + request.AlgorithmType.ToString()
                + "|d=" + request.DampingFactor.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + "|i=" + request.MaxIterations.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + "|t=" + request.Tolerance.ToString(System.Globalization.CultureInfo.InvariantCulture)
                + "|u=" + request.TreatAsUndirected.ToString()
                + "|m=" + (request.MaxResults.HasValue ? request.MaxResults.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) : "all");
        }

        /// <summary>
        /// Get a cached result when present and valid for the supplied graph node/edge counts.
        /// </summary>
        /// <param name="key">Cache key.</param>
        /// <param name="nodeCount">Current node count.</param>
        /// <param name="edgeCount">Current edge count.</param>
        /// <returns>The cached result, or null when absent or stale.</returns>
        public GraphAlgorithmResult TryGet(string key, int nodeCount, int edgeCount)
        {
            if (String.IsNullOrEmpty(key)) return null;
            if (!_Entries.TryGetValue(key, out CacheEntry entry)) return null;
            if (entry.NodeCount != nodeCount || entry.EdgeCount != edgeCount)
            {
                _Entries.TryRemove(key, out CacheEntry _);
                return null;
            }
            return entry.Result;
        }

        /// <summary>
        /// Store a result in the cache.
        /// </summary>
        /// <param name="key">Cache key.</param>
        /// <param name="result">Result to cache.</param>
        /// <param name="nodeCount">Node count captured at compute time.</param>
        /// <param name="edgeCount">Edge count captured at compute time.</param>
        public void Set(string key, GraphAlgorithmResult result, int nodeCount, int edgeCount)
        {
            if (String.IsNullOrEmpty(key) || result == null) return;
            _Entries[key] = new CacheEntry(result, nodeCount, edgeCount);
        }

        /// <summary>
        /// Invalidate all cached results for a graph.
        /// </summary>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <returns>Number of entries removed.</returns>
        public int Invalidate(Guid graphGuid)
        {
            string prefix = graphGuid.ToString() + "|";
            int removed = 0;
            foreach (string key in System.Linq.Enumerable.ToList(_Entries.Keys))
            {
                if (key.StartsWith(prefix, StringComparison.Ordinal) && _Entries.TryRemove(key, out CacheEntry _)) removed++;
            }
            return removed;
        }

        /// <summary>
        /// Remove all cached results.
        /// </summary>
        public void Clear()
        {
            _Entries.Clear();
        }

        #endregion

        #region Private-Methods

        #endregion

        private sealed class CacheEntry
        {
            internal GraphAlgorithmResult Result { get; }
            internal int NodeCount { get; }
            internal int EdgeCount { get; }

            internal CacheEntry(GraphAlgorithmResult result, int nodeCount, int edgeCount)
            {
                Result = result;
                NodeCount = nodeCount;
                EdgeCount = edgeCount;
            }
        }
    }
}
