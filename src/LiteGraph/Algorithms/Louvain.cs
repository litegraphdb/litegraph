namespace LiteGraph.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Louvain modularity-maximizing community detection over the undirected view (in- and out-neighbors combined; each directed edge contributes weight 1 in both directions, parallel edges summed).
    /// Runs the standard two-phase algorithm (local moving then aggregation) across levels until modularity no longer improves.
    /// Node processing order is deterministic (ascending index), so runs are reproducible for a given adjacency.
    /// </summary>
    public static class Louvain
    {
        #region Public-Members

        /// <summary>
        /// Default node-data property name used when writing the community identifier back.
        /// </summary>
        public const string DefaultProperty = "louvain";

        #endregion

        #region Private-Members

        private const double _MinModularityGain = 0.0000001d;

        #endregion

        #region Public-Methods

        /// <summary>
        /// Compute communities via Louvain modularity optimization.
        /// </summary>
        /// <param name="adjacency">Graph adjacency.</param>
        /// <param name="maxIterations">Maximum number of levels.</param>
        /// <param name="communityCount">Number of distinct communities discovered.</param>
        /// <param name="levels">Number of aggregation levels performed.</param>
        /// <param name="converged">Whether the algorithm reached a modularity fixed point within the level limit.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Zero-based community identifier per node, indexed by dense node index.</returns>
        /// <exception cref="ArgumentNullException">Thrown when adjacency is null.</exception>
        public static long[] Compute(
            GraphAdjacency adjacency,
            int maxIterations,
            out int communityCount,
            out int levels,
            out bool converged,
            CancellationToken token = default)
        {
            if (adjacency == null) throw new ArgumentNullException(nameof(adjacency));

            int n = adjacency.NodeCount;
            communityCount = 0;
            levels = 0;
            converged = true;
            if (n == 0) return Array.Empty<long>();

            List<Dictionary<int, double>> graph = BuildUndirectedWeighted(adjacency);
            double totalWeight = TotalWeight(graph);

            int[] originalToCurrent = new int[n];
            for (int i = 0; i < n; i++) originalToCurrent[i] = i;

            converged = false;
            for (int level = 0; level < maxIterations; level++)
            {
                token.ThrowIfCancellationRequested();

                int[] community = new int[graph.Count];
                bool improved = OneLevel(graph, totalWeight, community, token);
                levels = level + 1;

                int[] renumbered = Renumber(community, out int communityTotal);
                for (int i = 0; i < n; i++) originalToCurrent[i] = renumbered[originalToCurrent[i]];

                if (!improved)
                {
                    converged = true;
                    communityCount = communityTotal;
                    break;
                }

                graph = Aggregate(graph, renumbered, communityTotal);
                communityCount = communityTotal;
            }

            long[] result = new long[n];
            for (int i = 0; i < n; i++) result[i] = originalToCurrent[i];
            return result;
        }

        #endregion

        #region Private-Methods

        private static List<Dictionary<int, double>> BuildUndirectedWeighted(GraphAdjacency adjacency)
        {
            int n = adjacency.NodeCount;
            List<Dictionary<int, double>> graph = new List<Dictionary<int, double>>(n);
            for (int i = 0; i < n; i++) graph.Add(new Dictionary<int, double>());

            for (int v = 0; v < n; v++)
            {
                int outStart = adjacency.OutOffsets[v];
                int outEnd = adjacency.OutOffsets[v + 1];
                for (int p = outStart; p < outEnd; p++)
                {
                    int w = adjacency.OutTargets[p];
                    AddWeight(graph[v], w, 1d);
                    AddWeight(graph[w], v, 1d);
                }
            }

            return graph;
        }

        private static void AddWeight(Dictionary<int, double> map, int key, double weight)
        {
            map.TryGetValue(key, out double existing);
            map[key] = existing + weight;
        }

        private static double TotalWeight(List<Dictionary<int, double>> graph)
        {
            double sum = 0d;
            for (int i = 0; i < graph.Count; i++)
            {
                foreach (KeyValuePair<int, double> kvp in graph[i]) sum += kvp.Value;
            }
            return sum / 2d;
        }

        private static bool OneLevel(List<Dictionary<int, double>> graph, double totalWeight, int[] community, CancellationToken token)
        {
            int count = graph.Count;
            double m2 = 2d * totalWeight;
            if (m2 <= 0d)
            {
                for (int i = 0; i < count; i++) community[i] = i;
                return false;
            }

            double[] degree = new double[count];
            double[] selfLoop = new double[count];
            for (int i = 0; i < count; i++)
            {
                foreach (KeyValuePair<int, double> kvp in graph[i])
                {
                    degree[i] += kvp.Value;
                    if (kvp.Key == i) selfLoop[i] += kvp.Value;
                }
                community[i] = i;
            }

            double[] communityTotalDegree = new double[count];
            for (int i = 0; i < count; i++) communityTotalDegree[i] = degree[i];

            bool anyImproved = false;
            bool moved = true;
            int guard = 0;

            while (moved && guard < 100)
            {
                moved = false;
                guard++;
                token.ThrowIfCancellationRequested();

                for (int v = 0; v < count; v++)
                {
                    int currentCommunity = community[v];
                    Dictionary<int, double> neighborCommunityWeight = new Dictionary<int, double>();
                    foreach (KeyValuePair<int, double> kvp in graph[v])
                    {
                        if (kvp.Key == v) continue;
                        int c = community[kvp.Key];
                        neighborCommunityWeight.TryGetValue(c, out double existing);
                        neighborCommunityWeight[c] = existing + kvp.Value;
                    }

                    communityTotalDegree[currentCommunity] -= degree[v];
                    neighborCommunityWeight.TryGetValue(currentCommunity, out double weightToCurrent);

                    int bestCommunity = currentCommunity;
                    double bestGain = 0d;
                    foreach (KeyValuePair<int, double> kvp in neighborCommunityWeight)
                    {
                        double gain = kvp.Value - (communityTotalDegree[kvp.Key] * degree[v] / m2);
                        if (gain > bestGain + _MinModularityGain)
                        {
                            bestGain = gain;
                            bestCommunity = kvp.Key;
                        }
                    }

                    double stayGain = weightToCurrent - (communityTotalDegree[currentCommunity] * degree[v] / m2);
                    if (stayGain >= bestGain) bestCommunity = currentCommunity;

                    communityTotalDegree[bestCommunity] += degree[v];
                    community[v] = bestCommunity;

                    if (bestCommunity != currentCommunity)
                    {
                        moved = true;
                        anyImproved = true;
                    }
                }
            }

            return anyImproved;
        }

        private static int[] Renumber(int[] community, out int communityCount)
        {
            int[] mapped = new int[community.Length];
            Dictionary<int, int> remap = new Dictionary<int, int>();
            int next = 0;
            for (int i = 0; i < community.Length; i++)
            {
                if (!remap.TryGetValue(community[i], out int id))
                {
                    id = next;
                    remap[community[i]] = id;
                    next++;
                }
                mapped[i] = id;
            }
            communityCount = next;
            return mapped;
        }

        private static List<Dictionary<int, double>> Aggregate(
            List<Dictionary<int, double>> graph,
            int[] community,
            int communityCount)
        {
            List<Dictionary<int, double>> aggregated = new List<Dictionary<int, double>>(communityCount);
            for (int i = 0; i < communityCount; i++) aggregated.Add(new Dictionary<int, double>());

            for (int v = 0; v < graph.Count; v++)
            {
                int cv = community[v];
                foreach (KeyValuePair<int, double> kvp in graph[v])
                {
                    int cw = community[kvp.Key];
                    AddWeight(aggregated[cv], cw, kvp.Value);
                }
            }

            return aggregated;
        }

        #endregion
    }
}
