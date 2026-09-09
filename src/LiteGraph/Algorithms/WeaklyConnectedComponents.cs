namespace LiteGraph.Algorithms
{
    using System;
    using System.Threading;

    /// <summary>
    /// Weakly connected components using union-find.  Edges are treated as undirected for connectivity.
    /// </summary>
    public static class WeaklyConnectedComponents
    {
        #region Public-Members

        /// <summary>
        /// Default node-data property name used when writing the component identifier back.
        /// </summary>
        public const string DefaultProperty = "component";

        #endregion

        #region Private-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Compute weakly connected components.
        /// </summary>
        /// <param name="adjacency">Graph adjacency.</param>
        /// <param name="componentCount">Number of distinct components discovered.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Zero-based component identifier per node, indexed by dense node index.  Identifiers are assigned in ascending order of first-seen node index.</returns>
        /// <exception cref="ArgumentNullException">Thrown when adjacency is null.</exception>
        public static long[] Compute(
            GraphAdjacency adjacency,
            out int componentCount,
            CancellationToken token = default)
        {
            if (adjacency == null) throw new ArgumentNullException(nameof(adjacency));

            int n = adjacency.NodeCount;
            componentCount = 0;
            if (n == 0) return Array.Empty<long>();

            int[] parent = new int[n];
            int[] rank = new int[n];
            for (int i = 0; i < n; i++) parent[i] = i;

            for (int i = 0; i < n; i++)
            {
                if ((i & 0x3FFF) == 0) token.ThrowIfCancellationRequested();
                int start = adjacency.OutOffsets[i];
                int end = adjacency.OutOffsets[i + 1];
                for (int p = start; p < end; p++)
                {
                    Union(parent, rank, i, adjacency.OutTargets[p]);
                }
            }

            long[] labels = new long[n];
            long[] rootLabel = new long[n];
            for (int i = 0; i < n; i++) rootLabel[i] = -1L;

            long nextLabel = 0L;
            for (int i = 0; i < n; i++)
            {
                int root = Find(parent, i);
                if (rootLabel[root] < 0L)
                {
                    rootLabel[root] = nextLabel;
                    nextLabel++;
                }
                labels[i] = rootLabel[root];
            }

            componentCount = (int)nextLabel;
            return labels;
        }

        #endregion

        #region Private-Methods

        private static int Find(int[] parent, int x)
        {
            while (parent[x] != x)
            {
                parent[x] = parent[parent[x]];
                x = parent[x];
            }
            return x;
        }

        private static void Union(int[] parent, int[] rank, int a, int b)
        {
            int ra = Find(parent, a);
            int rb = Find(parent, b);
            if (ra == rb) return;

            if (rank[ra] < rank[rb])
            {
                parent[ra] = rb;
            }
            else if (rank[ra] > rank[rb])
            {
                parent[rb] = ra;
            }
            else
            {
                parent[rb] = ra;
                rank[ra]++;
            }
        }

        #endregion
    }
}
