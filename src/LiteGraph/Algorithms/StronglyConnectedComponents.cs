namespace LiteGraph.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Strongly connected components using an iterative form of Tarjan's algorithm (directed).  The iterative form avoids stack overflow on deep graphs.
    /// </summary>
    public static class StronglyConnectedComponents
    {
        #region Public-Members

        /// <summary>
        /// Default node-data property name used when writing the component identifier back.
        /// </summary>
        public const string DefaultProperty = "scc";

        #endregion

        #region Private-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Compute strongly connected components.
        /// </summary>
        /// <param name="adjacency">Graph adjacency.</param>
        /// <param name="componentCount">Number of distinct components discovered.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Zero-based component identifier per node, indexed by dense node index.</returns>
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

            int[] index = new int[n];
            int[] lowLink = new int[n];
            bool[] onStack = new bool[n];
            bool[] visited = new bool[n];
            for (int i = 0; i < n; i++) index[i] = -1;

            long[] component = new long[n];
            int[] tarjanStack = new int[n];
            int tarjanTop = 0;

            int nextIndex = 0;
            long nextComponent = 0L;

            int[] frameNode = new int[n];
            int[] frameChildPointer = new int[n];

            for (int root = 0; root < n; root++)
            {
                if (visited[root]) continue;
                if ((root & 0x3FFF) == 0) token.ThrowIfCancellationRequested();

                int frameTop = 0;
                frameNode[0] = root;
                frameChildPointer[0] = adjacency.OutOffsets[root];
                visited[root] = true;
                index[root] = nextIndex;
                lowLink[root] = nextIndex;
                nextIndex++;
                tarjanStack[tarjanTop++] = root;
                onStack[root] = true;

                while (frameTop >= 0)
                {
                    int v = frameNode[frameTop];
                    int edgeEnd = adjacency.OutOffsets[v + 1];

                    if (frameChildPointer[frameTop] < edgeEnd)
                    {
                        int p = frameChildPointer[frameTop];
                        frameChildPointer[frameTop]++;
                        int w = adjacency.OutTargets[p];

                        if (!visited[w])
                        {
                            visited[w] = true;
                            index[w] = nextIndex;
                            lowLink[w] = nextIndex;
                            nextIndex++;
                            tarjanStack[tarjanTop++] = w;
                            onStack[w] = true;

                            frameTop++;
                            frameNode[frameTop] = w;
                            frameChildPointer[frameTop] = adjacency.OutOffsets[w];
                        }
                        else if (onStack[w])
                        {
                            if (index[w] < lowLink[v]) lowLink[v] = index[w];
                        }
                    }
                    else
                    {
                        if (lowLink[v] == index[v])
                        {
                            while (true)
                            {
                                int w = tarjanStack[--tarjanTop];
                                onStack[w] = false;
                                component[w] = nextComponent;
                                if (w == v) break;
                            }
                            nextComponent++;
                        }

                        frameTop--;
                        if (frameTop >= 0)
                        {
                            int parent = frameNode[frameTop];
                            if (lowLink[v] < lowLink[parent]) lowLink[parent] = lowLink[v];
                        }
                    }
                }
            }

            componentCount = (int)nextComponent;
            return component;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
