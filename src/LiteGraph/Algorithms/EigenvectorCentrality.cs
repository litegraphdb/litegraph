namespace LiteGraph.Algorithms
{
    using System;
    using System.Threading;

    /// <summary>
    /// Eigenvector centrality via power iteration on the undirected adjacency (in- and out-neighbors combined), which guarantees a symmetric, non-negative operator with a well-defined dominant eigenvector.
    /// Scores are normalized to unit L2 norm.
    /// </summary>
    public static class EigenvectorCentrality
    {
        #region Public-Members

        /// <summary>
        /// Default node-data property name used when writing eigenvector scores back.
        /// </summary>
        public const string DefaultProperty = "eigenvector";

        #endregion

        #region Private-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Compute eigenvector centrality.
        /// </summary>
        /// <param name="adjacency">Graph adjacency.</param>
        /// <param name="maxIterations">Maximum iterations.</param>
        /// <param name="tolerance">Convergence tolerance (L1 delta across all nodes).</param>
        /// <param name="iterations">Number of iterations performed.</param>
        /// <param name="converged">Whether the computation converged within the iteration limit.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Eigenvector score per node, indexed by dense node index.</returns>
        /// <exception cref="ArgumentNullException">Thrown when adjacency is null.</exception>
        public static double[] Compute(
            GraphAdjacency adjacency,
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

            double[] score = new double[n];
            double[] next = new double[n];
            double initial = 1d / Math.Sqrt(n);
            for (int i = 0; i < n; i++) score[i] = initial;

            for (int iter = 0; iter < maxIterations; iter++)
            {
                token.ThrowIfCancellationRequested();

                for (int i = 0; i < n; i++) next[i] = 0d;

                for (int v = 0; v < n; v++)
                {
                    double sv = score[v];

                    int outStart = adjacency.OutOffsets[v];
                    int outEnd = adjacency.OutOffsets[v + 1];
                    for (int p = outStart; p < outEnd; p++) next[adjacency.OutTargets[p]] += sv;

                    int inStart = adjacency.InOffsets[v];
                    int inEnd = adjacency.InOffsets[v + 1];
                    for (int p = inStart; p < inEnd; p++) next[adjacency.InSources[p]] += sv;
                }

                double norm = 0d;
                for (int i = 0; i < n; i++) norm += next[i] * next[i];
                norm = Math.Sqrt(norm);

                if (norm <= 0d)
                {
                    iterations = iter + 1;
                    converged = true;
                    return score;
                }

                double delta = 0d;
                for (int i = 0; i < n; i++)
                {
                    next[i] /= norm;
                    delta += Math.Abs(next[i] - score[i]);
                    score[i] = next[i];
                }

                iterations = iter + 1;
                if (delta < tolerance)
                {
                    converged = true;
                    return score;
                }
            }

            converged = false;
            return score;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
