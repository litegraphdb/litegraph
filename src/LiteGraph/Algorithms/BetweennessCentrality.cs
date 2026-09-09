namespace LiteGraph.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Betweenness centrality using Brandes' algorithm on the undirected, unweighted view (in- and out-neighbors combined).
    /// Scores are normalized by the number of ordered node pairs (n-1)(n-2) so they fall in [0, 1].
    /// Complexity is O(V * (V + E)); intended for graphs within the configured in-memory ceiling.
    /// </summary>
    public static class BetweennessCentrality
    {
        #region Public-Members

        /// <summary>
        /// Default node-data property name used when writing betweenness scores back.
        /// </summary>
        public const string DefaultProperty = "betweenness";

        #endregion

        #region Private-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Compute betweenness centrality.
        /// </summary>
        /// <param name="adjacency">Graph adjacency.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Betweenness score per node, indexed by dense node index.</returns>
        /// <exception cref="ArgumentNullException">Thrown when adjacency is null.</exception>
        public static double[] Compute(GraphAdjacency adjacency, CancellationToken token = default)
        {
            if (adjacency == null) throw new ArgumentNullException(nameof(adjacency));

            int n = adjacency.NodeCount;
            double[] betweenness = new double[n];
            if (n <= 2) return betweenness;

            List<int>[] neighbors = BuildUndirectedNeighbors(adjacency);

            int[] sigma = new int[n];
            int[] distance = new int[n];
            double[] delta = new double[n];
            int[] queue = new int[n];
            int[] stack = new int[n];
            List<int>[] predecessors = new List<int>[n];
            for (int i = 0; i < n; i++) predecessors[i] = new List<int>();

            for (int source = 0; source < n; source++)
            {
                token.ThrowIfCancellationRequested();

                for (int i = 0; i < n; i++)
                {
                    sigma[i] = 0;
                    distance[i] = -1;
                    delta[i] = 0d;
                    predecessors[i].Clear();
                }

                sigma[source] = 1;
                distance[source] = 0;
                int head = 0;
                int tail = 0;
                int stackTop = 0;
                queue[tail++] = source;

                while (head < tail)
                {
                    int v = queue[head++];
                    stack[stackTop++] = v;

                    List<int> vNeighbors = neighbors[v];
                    for (int k = 0; k < vNeighbors.Count; k++)
                    {
                        int w = vNeighbors[k];
                        if (distance[w] < 0)
                        {
                            distance[w] = distance[v] + 1;
                            queue[tail++] = w;
                        }
                        if (distance[w] == distance[v] + 1)
                        {
                            sigma[w] += sigma[v];
                            predecessors[w].Add(v);
                        }
                    }
                }

                while (stackTop > 0)
                {
                    int w = stack[--stackTop];
                    List<int> wPredecessors = predecessors[w];
                    for (int k = 0; k < wPredecessors.Count; k++)
                    {
                        int v = wPredecessors[k];
                        delta[v] += ((double)sigma[v] / sigma[w]) * (1d + delta[w]);
                    }
                    if (w != source) betweenness[w] += delta[w];
                }
            }

            // Normalize by the number of ordered pairs (n-1)(n-2). The undirected double-counting
            // of each geodesic (once from each endpoint as source) is absorbed by this normalization,
            // matching NetworkX's normalized undirected betweenness (star center -> 1.0).
            double scale = 1d / ((double)(n - 1) * (n - 2));
            for (int i = 0; i < n; i++) betweenness[i] = betweenness[i] * scale;

            return betweenness;
        }

        #endregion

        #region Private-Methods

        private static List<int>[] BuildUndirectedNeighbors(GraphAdjacency adjacency)
        {
            int n = adjacency.NodeCount;
            List<int>[] neighbors = new List<int>[n];
            for (int i = 0; i < n; i++) neighbors[i] = new List<int>();

            for (int v = 0; v < n; v++)
            {
                int outStart = adjacency.OutOffsets[v];
                int outEnd = adjacency.OutOffsets[v + 1];
                for (int p = outStart; p < outEnd; p++) neighbors[v].Add(adjacency.OutTargets[p]);

                int inStart = adjacency.InOffsets[v];
                int inEnd = adjacency.InOffsets[v + 1];
                for (int p = inStart; p < inEnd; p++) neighbors[v].Add(adjacency.InSources[p]);
            }

            return neighbors;
        }

        #endregion
    }
}
