namespace LiteGraph.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// k-core decomposition: the core number of each node over the undirected simple-graph view (self-loops and parallel edges removed).
    /// A node's core number is the largest k such that it belongs to a maximal subgraph in which every node has degree at least k.
    /// Computed with the Batagelj-Zaversnik linear-time algorithm.
    /// </summary>
    public static class KCore
    {
        #region Public-Members

        /// <summary>
        /// Default node-data property name used when writing core numbers back.
        /// </summary>
        public const string DefaultProperty = "kcore";

        #endregion

        #region Private-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Compute the core number of every node.
        /// </summary>
        /// <param name="adjacency">Graph adjacency.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Core number per node, indexed by dense node index.</returns>
        /// <exception cref="ArgumentNullException">Thrown when adjacency is null.</exception>
        public static int[] Compute(GraphAdjacency adjacency, CancellationToken token = default)
        {
            if (adjacency == null) throw new ArgumentNullException(nameof(adjacency));

            int n = adjacency.NodeCount;
            int[] core = new int[n];
            if (n == 0) return core;

            List<int>[] neighbors = BuildSimpleNeighbors(adjacency);
            int[] degree = new int[n];
            int maxDegree = 0;
            for (int i = 0; i < n; i++)
            {
                degree[i] = neighbors[i].Count;
                if (degree[i] > maxDegree) maxDegree = degree[i];
            }

            // Bucket-sort vertices by current degree.
            int[] bin = new int[maxDegree + 2];
            for (int i = 0; i < n; i++) bin[degree[i]]++;

            int start = 0;
            for (int d = 0; d <= maxDegree; d++)
            {
                int count = bin[d];
                bin[d] = start;
                start += count;
            }

            int[] vert = new int[n];
            int[] pos = new int[n];
            for (int i = 0; i < n; i++)
            {
                pos[i] = bin[degree[i]];
                vert[pos[i]] = i;
                bin[degree[i]]++;
            }

            for (int d = maxDegree; d >= 1; d--) bin[d] = bin[d - 1];
            bin[0] = 0;

            for (int i = 0; i < n; i++)
            {
                if ((i & 0x3FFF) == 0) token.ThrowIfCancellationRequested();

                int v = vert[i];
                core[v] = degree[v];

                foreach (int u in neighbors[v])
                {
                    if (degree[u] > degree[v])
                    {
                        int du = degree[u];
                        int pu = pos[u];
                        int pw = bin[du];
                        int w = vert[pw];
                        if (u != w)
                        {
                            pos[u] = pw;
                            vert[pu] = w;
                            pos[w] = pu;
                            vert[pw] = u;
                        }
                        bin[du]++;
                        degree[u]--;
                    }
                }
            }

            return core;
        }

        #endregion

        #region Private-Methods

        private static List<int>[] BuildSimpleNeighbors(GraphAdjacency adjacency)
        {
            int n = adjacency.NodeCount;
            HashSet<int>[] sets = new HashSet<int>[n];
            for (int i = 0; i < n; i++) sets[i] = new HashSet<int>();

            for (int v = 0; v < n; v++)
            {
                int outStart = adjacency.OutOffsets[v];
                int outEnd = adjacency.OutOffsets[v + 1];
                for (int p = outStart; p < outEnd; p++)
                {
                    int w = adjacency.OutTargets[p];
                    if (w != v) sets[v].Add(w);
                }

                int inStart = adjacency.InOffsets[v];
                int inEnd = adjacency.InOffsets[v + 1];
                for (int p = inStart; p < inEnd; p++)
                {
                    int w = adjacency.InSources[p];
                    if (w != v) sets[v].Add(w);
                }
            }

            List<int>[] neighbors = new List<int>[n];
            for (int i = 0; i < n; i++) neighbors[i] = new List<int>(sets[i]);
            return neighbors;
        }

        #endregion
    }
}
