namespace LiteGraph.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Local clustering coefficient over the undirected simple-graph view (self-loops and parallel edges removed, in- and out-neighbors combined).
    /// The score is the fraction of a node's neighbor pairs that are themselves connected: 2T / (k(k-1)), in [0, 1].
    /// </summary>
    public static class ClusteringCoefficient
    {
        #region Public-Members

        /// <summary>
        /// Default node-data property name used when writing clustering coefficient back.
        /// </summary>
        public const string DefaultProperty = "clustering";

        #endregion

        #region Private-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Compute the local clustering coefficient for every node.
        /// </summary>
        /// <param name="adjacency">Graph adjacency.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Clustering coefficient per node, indexed by dense node index.</returns>
        /// <exception cref="ArgumentNullException">Thrown when adjacency is null.</exception>
        public static double[] Compute(GraphAdjacency adjacency, CancellationToken token = default)
        {
            if (adjacency == null) throw new ArgumentNullException(nameof(adjacency));

            int n = adjacency.NodeCount;
            double[] scores = new double[n];
            if (n == 0) return scores;

            HashSet<int>[] neighbors = BuildSimpleNeighbors(adjacency);

            for (int v = 0; v < n; v++)
            {
                if ((v & 0x3FF) == 0) token.ThrowIfCancellationRequested();

                HashSet<int> vNeighbors = neighbors[v];
                int degree = vNeighbors.Count;
                if (degree < 2) continue;

                int links = 0;
                foreach (int a in vNeighbors)
                {
                    foreach (int b in vNeighbors)
                    {
                        if (b <= a) continue;
                        if (neighbors[a].Contains(b)) links++;
                    }
                }

                scores[v] = (2d * links) / ((double)degree * (degree - 1));
            }

            return scores;
        }

        #endregion

        #region Private-Methods

        private static HashSet<int>[] BuildSimpleNeighbors(GraphAdjacency adjacency)
        {
            int n = adjacency.NodeCount;
            HashSet<int>[] neighbors = new HashSet<int>[n];
            for (int i = 0; i < n; i++) neighbors[i] = new HashSet<int>();

            for (int v = 0; v < n; v++)
            {
                int outStart = adjacency.OutOffsets[v];
                int outEnd = adjacency.OutOffsets[v + 1];
                for (int p = outStart; p < outEnd; p++)
                {
                    int w = adjacency.OutTargets[p];
                    if (w != v) neighbors[v].Add(w);
                }

                int inStart = adjacency.InOffsets[v];
                int inEnd = adjacency.InOffsets[v + 1];
                for (int p = inStart; p < inEnd; p++)
                {
                    int w = adjacency.InSources[p];
                    if (w != v) neighbors[v].Add(w);
                }
            }

            return neighbors;
        }

        #endregion
    }
}
