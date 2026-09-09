namespace LiteGraph.Algorithms
{
    using System;
    using System.Threading;

    /// <summary>
    /// Degree centrality: in-degree, out-degree, and total degree per node.
    /// </summary>
    public static class DegreeCentrality
    {
        #region Public-Members

        /// <summary>
        /// Default node-data property name used when writing total degree back.
        /// </summary>
        public const string DefaultProperty = "degree";

        #endregion

        #region Private-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Compute degree centrality.
        /// </summary>
        /// <param name="adjacency">Graph adjacency.</param>
        /// <param name="inDegrees">In-degree per node, indexed by dense node index.</param>
        /// <param name="outDegrees">Out-degree per node, indexed by dense node index.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Total degree per node, indexed by dense node index.</returns>
        /// <exception cref="ArgumentNullException">Thrown when adjacency is null.</exception>
        public static int[] Compute(
            GraphAdjacency adjacency,
            out int[] inDegrees,
            out int[] outDegrees,
            CancellationToken token = default)
        {
            if (adjacency == null) throw new ArgumentNullException(nameof(adjacency));

            int n = adjacency.NodeCount;
            int[] total = new int[n];
            inDegrees = new int[n];
            outDegrees = new int[n];

            for (int i = 0; i < n; i++)
            {
                if ((i & 0x3FFF) == 0) token.ThrowIfCancellationRequested();
                inDegrees[i] = adjacency.InDegree(i);
                outDegrees[i] = adjacency.OutDegree(i);
                total[i] = inDegrees[i] + outDegrees[i];
            }

            return total;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
