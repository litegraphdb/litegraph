namespace LiteGraph.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Label propagation community detection.  Edges are treated as undirected (in- and out-neighbors combined).
    /// Updates are applied asynchronously in ascending node-index order and ties are broken toward the smallest label, so runs are deterministic for a given adjacency.
    /// </summary>
    public static class LabelPropagation
    {
        #region Public-Members

        /// <summary>
        /// Default node-data property name used when writing the community identifier back.
        /// </summary>
        public const string DefaultProperty = "community";

        #endregion

        #region Private-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Compute communities via label propagation.
        /// </summary>
        /// <param name="adjacency">Graph adjacency.</param>
        /// <param name="maxIterations">Maximum iterations.</param>
        /// <param name="communityCount">Number of distinct communities discovered.</param>
        /// <param name="iterations">Number of iterations performed.</param>
        /// <param name="converged">Whether propagation stabilized within the iteration limit.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Zero-based community identifier per node, indexed by dense node index.</returns>
        /// <exception cref="ArgumentNullException">Thrown when adjacency is null.</exception>
        public static long[] Compute(
            GraphAdjacency adjacency,
            int maxIterations,
            out int communityCount,
            out int iterations,
            out bool converged,
            CancellationToken token = default)
        {
            if (adjacency == null) throw new ArgumentNullException(nameof(adjacency));

            int n = adjacency.NodeCount;
            communityCount = 0;
            iterations = 0;
            converged = true;
            if (n == 0) return Array.Empty<long>();

            int[] labels = new int[n];
            for (int i = 0; i < n; i++) labels[i] = i;

            Dictionary<int, int> counts = new Dictionary<int, int>();

            for (int iter = 0; iter < maxIterations; iter++)
            {
                token.ThrowIfCancellationRequested();
                bool changed = false;

                for (int v = 0; v < n; v++)
                {
                    counts.Clear();

                    int outStart = adjacency.OutOffsets[v];
                    int outEnd = adjacency.OutOffsets[v + 1];
                    for (int p = outStart; p < outEnd; p++)
                    {
                        int label = labels[adjacency.OutTargets[p]];
                        counts.TryGetValue(label, out int c);
                        counts[label] = c + 1;
                    }

                    int inStart = adjacency.InOffsets[v];
                    int inEnd = adjacency.InOffsets[v + 1];
                    for (int p = inStart; p < inEnd; p++)
                    {
                        int label = labels[adjacency.InSources[p]];
                        counts.TryGetValue(label, out int c);
                        counts[label] = c + 1;
                    }

                    if (counts.Count == 0) continue;

                    int bestLabel = labels[v];
                    int bestCount = -1;
                    foreach (KeyValuePair<int, int> kvp in counts)
                    {
                        if (kvp.Value > bestCount || (kvp.Value == bestCount && kvp.Key < bestLabel))
                        {
                            bestCount = kvp.Value;
                            bestLabel = kvp.Key;
                        }
                    }

                    if (bestLabel != labels[v])
                    {
                        labels[v] = bestLabel;
                        changed = true;
                    }
                }

                iterations = iter + 1;
                if (!changed)
                {
                    converged = true;
                    return Compact(labels, out communityCount);
                }
            }

            converged = false;
            return Compact(labels, out communityCount);
        }

        #endregion

        #region Private-Methods

        private static long[] Compact(int[] labels, out int communityCount)
        {
            int n = labels.Length;
            long[] result = new long[n];
            Dictionary<int, long> remap = new Dictionary<int, long>();
            long next = 0L;

            for (int i = 0; i < n; i++)
            {
                if (!remap.TryGetValue(labels[i], out long mapped))
                {
                    mapped = next;
                    remap[labels[i]] = mapped;
                    next++;
                }
                result[i] = mapped;
            }

            communityCount = (int)next;
            return result;
        }

        #endregion
    }
}
