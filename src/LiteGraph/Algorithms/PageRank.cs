namespace LiteGraph.Algorithms
{
    using System;
    using System.Threading;

    /// <summary>
    /// PageRank over a directed graph adjacency.  Dangling nodes (no out-edges) distribute their rank uniformly across all nodes.
    /// </summary>
    public static class PageRank
    {
        #region Public-Members

        /// <summary>
        /// Default node-data property name used when writing PageRank results back.
        /// </summary>
        public const string DefaultProperty = "pagerank";

        #endregion

        #region Private-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Compute PageRank scores.
        /// </summary>
        /// <param name="adjacency">Graph adjacency.</param>
        /// <param name="dampingFactor">Damping factor (0.0 - 1.0).</param>
        /// <param name="maxIterations">Maximum iterations.</param>
        /// <param name="tolerance">Convergence tolerance (L1 delta across all nodes).</param>
        /// <param name="iterations">Number of iterations performed.</param>
        /// <param name="converged">Whether the computation converged within the iteration limit.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Score per node, indexed by dense node index.</returns>
        /// <exception cref="ArgumentNullException">Thrown when adjacency is null.</exception>
        public static double[] Compute(
            GraphAdjacency adjacency,
            double dampingFactor,
            int maxIterations,
            double tolerance,
            out int iterations,
            out bool converged,
            CancellationToken token = default)
        {
            if (adjacency == null) throw new ArgumentNullException(nameof(adjacency));

            int n = adjacency.NodeCount;
            iterations = 0;
            converged = true;

            if (n == 0) return Array.Empty<double>();

            double[] rank = new double[n];
            double[] next = new double[n];
            double initial = 1d / n;
            for (int i = 0; i < n; i++) rank[i] = initial;

            double teleport = (1d - dampingFactor) / n;

            for (int iter = 0; iter < maxIterations; iter++)
            {
                token.ThrowIfCancellationRequested();

                double danglingSum = 0d;
                for (int i = 0; i < n; i++)
                {
                    if (adjacency.OutDegree(i) == 0) danglingSum += rank[i];
                }
                double danglingContribution = dampingFactor * danglingSum / n;

                for (int i = 0; i < n; i++) next[i] = teleport + danglingContribution;

                for (int i = 0; i < n; i++)
                {
                    int outDegree = adjacency.OutDegree(i);
                    if (outDegree == 0) continue;

                    double share = dampingFactor * rank[i] / outDegree;
                    int start = adjacency.OutOffsets[i];
                    int end = adjacency.OutOffsets[i + 1];
                    for (int p = start; p < end; p++)
                    {
                        next[adjacency.OutTargets[p]] += share;
                    }
                }

                double delta = 0d;
                for (int i = 0; i < n; i++)
                {
                    delta += Math.Abs(next[i] - rank[i]);
                    rank[i] = next[i];
                }

                iterations = iter + 1;

                if (delta < tolerance)
                {
                    converged = true;
                    return rank;
                }
            }

            converged = false;
            return rank;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
