namespace LiteGraph.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Closeness centrality via multi-source breadth-first search.  Edges are treated as undirected (in- and out-neighbors combined).
    /// Uses the Wasserman-Faust normalization so scores are comparable across disconnected components: (reachable-1)^2 / ((N-1) * sumOfDistances).
    /// Complexity is O(V * (V + E)); intended for graphs within the configured in-memory ceiling.
    /// </summary>
    public static class ClosenessCentrality
    {
        #region Public-Members

        /// <summary>
        /// Default node-data property name used when writing closeness scores back.
        /// </summary>
        public const string DefaultProperty = "closeness";

        #endregion

        #region Private-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Compute closeness centrality.
        /// </summary>
        /// <param name="adjacency">Graph adjacency.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Closeness score per node, indexed by dense node index.</returns>
        /// <exception cref="ArgumentNullException">Thrown when adjacency is null.</exception>
        public static double[] Compute(GraphAdjacency adjacency, CancellationToken token = default)
        {
            if (adjacency == null) throw new ArgumentNullException(nameof(adjacency));

            int n = adjacency.NodeCount;
            double[] scores = new double[n];
            if (n <= 1) return scores;

            int[] distances = new int[n];
            int[] queue = new int[n];

            for (int source = 0; source < n; source++)
            {
                token.ThrowIfCancellationRequested();

                for (int i = 0; i < n; i++) distances[i] = -1;
                int head = 0;
                int tail = 0;
                distances[source] = 0;
                queue[tail++] = source;

                long sumDistances = 0;
                int reachable = 0;

                while (head < tail)
                {
                    int v = queue[head++];
                    int nextDistance = distances[v] + 1;

                    int outStart = adjacency.OutOffsets[v];
                    int outEnd = adjacency.OutOffsets[v + 1];
                    for (int p = outStart; p < outEnd; p++)
                    {
                        int w = adjacency.OutTargets[p];
                        if (distances[w] < 0)
                        {
                            distances[w] = nextDistance;
                            sumDistances += nextDistance;
                            reachable++;
                            queue[tail++] = w;
                        }
                    }

                    int inStart = adjacency.InOffsets[v];
                    int inEnd = adjacency.InOffsets[v + 1];
                    for (int p = inStart; p < inEnd; p++)
                    {
                        int w = adjacency.InSources[p];
                        if (distances[w] < 0)
                        {
                            distances[w] = nextDistance;
                            sumDistances += nextDistance;
                            reachable++;
                            queue[tail++] = w;
                        }
                    }
                }

                if (sumDistances > 0)
                {
                    double normalization = (double)reachable / (n - 1);
                    scores[source] = ((double)reachable / sumDistances) * normalization;
                }
            }

            return scores;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
