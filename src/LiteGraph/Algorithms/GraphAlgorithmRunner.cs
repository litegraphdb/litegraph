namespace LiteGraph.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Loads a graph into an in-memory adjacency, dispatches to the requested algorithm, and assembles a result mapped back to node GUIDs.
    /// Compute is storage-agnostic; this runner performs no write-back (write-back is handled by the client method).
    /// </summary>
    public static class GraphAlgorithmRunner
    {
        #region Public-Members

        #endregion

        #region Private-Members

        #endregion

        #region Public-Methods

        /// <summary>
        /// Run an algorithm over a graph and return a result.  Does not write results back.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="request">Algorithm request.</param>
        /// <param name="maxNodes">Maximum node ceiling; 0 means unlimited.</param>
        /// <param name="maxEdges">Maximum edge ceiling; 0 means unlimited.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Algorithm result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when client or request is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the graph exceeds the configured ceiling.</exception>
        public static async Task<GraphAlgorithmResult> RunAsync(
            LiteGraphClient client,
            Guid tenantGuid,
            Guid graphGuid,
            GraphAlgorithmRequest request,
            int maxNodes,
            int maxEdges,
            CancellationToken token = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));
            if (request == null) throw new ArgumentNullException(nameof(request));

            Stopwatch loadTimer = Stopwatch.StartNew();
            GraphAdjacency adjacency = await GraphAdjacency.BuildAsync(client, tenantGuid, graphGuid, maxNodes, maxEdges, token).ConfigureAwait(false);
            loadTimer.Stop();

            GraphAlgorithmResult result = new GraphAlgorithmResult();
            result.TenantGUID = tenantGuid;
            result.GraphGUID = graphGuid;
            result.AlgorithmType = request.AlgorithmType;
            result.NodeCount = adjacency.NodeCount;
            result.EdgeCount = adjacency.EdgeCount;
            result.LoadMs = loadTimer.Elapsed.TotalMilliseconds;

            Stopwatch computeTimer = Stopwatch.StartNew();
            List<GraphAlgorithmNodeResult> nodes = Dispatch(adjacency, request, result, token);
            computeTimer.Stop();

            result.ComputeMs = computeTimer.Elapsed.TotalMilliseconds;

            if (request.MaxResults != null && nodes.Count > request.MaxResults.Value)
                nodes = nodes.Take(request.MaxResults.Value).ToList();

            result.Nodes = nodes;
            return result;
        }

        #endregion

        #region Private-Methods

        private static List<GraphAlgorithmNodeResult> Dispatch(
            GraphAdjacency adjacency,
            GraphAlgorithmRequest request,
            GraphAlgorithmResult result,
            CancellationToken token)
        {
            switch (request.AlgorithmType)
            {
                case GraphAlgorithmTypeEnum.DegreeCentrality:
                    return BuildDegree(adjacency, token);
                case GraphAlgorithmTypeEnum.PageRank:
                    return BuildPageRank(adjacency, request, result, token);
                case GraphAlgorithmTypeEnum.WeaklyConnectedComponents:
                    return BuildComponents(adjacency, result, false, token);
                case GraphAlgorithmTypeEnum.StronglyConnectedComponents:
                    return BuildComponents(adjacency, result, true, token);
                case GraphAlgorithmTypeEnum.LabelPropagation:
                    return BuildLabelPropagation(adjacency, request, result, token);
                case GraphAlgorithmTypeEnum.ClosenessCentrality:
                    return BuildScored(adjacency, ClosenessCentrality.Compute(adjacency, token));
                case GraphAlgorithmTypeEnum.EigenvectorCentrality:
                    return BuildEigenvector(adjacency, request, result, token);
                case GraphAlgorithmTypeEnum.BetweennessCentrality:
                    return BuildScored(adjacency, BetweennessCentrality.Compute(adjacency, token));
                case GraphAlgorithmTypeEnum.ClusteringCoefficient:
                    return BuildScored(adjacency, ClusteringCoefficient.Compute(adjacency, token));
                case GraphAlgorithmTypeEnum.KCore:
                    return BuildScored(adjacency, ToDouble(KCore.Compute(adjacency, token)));
                case GraphAlgorithmTypeEnum.Louvain:
                    return BuildLouvain(adjacency, request, result, token);
                default:
                    throw new NotSupportedException("Algorithm '" + request.AlgorithmType + "' is not supported.");
            }
        }

        private static List<GraphAlgorithmNodeResult> BuildDegree(GraphAdjacency adjacency, CancellationToken token)
        {
            int[] total = DegreeCentrality.Compute(adjacency, out int[] inDegrees, out int[] outDegrees, token);
            List<GraphAlgorithmNodeResult> nodes = new List<GraphAlgorithmNodeResult>(adjacency.NodeCount);

            for (int i = 0; i < adjacency.NodeCount; i++)
            {
                GraphAlgorithmNodeResult item = new GraphAlgorithmNodeResult();
                item.NodeGUID = adjacency.NodeGuids[i];
                item.Name = adjacency.NodeNames[i];
                item.Score = total[i];
                item.EdgesIn = inDegrees[i];
                item.EdgesOut = outDegrees[i];
                nodes.Add(item);
            }

            nodes.Sort((a, b) => b.Score.CompareTo(a.Score));
            return nodes;
        }

        private static List<GraphAlgorithmNodeResult> BuildPageRank(
            GraphAdjacency adjacency,
            GraphAlgorithmRequest request,
            GraphAlgorithmResult result,
            CancellationToken token)
        {
            double[] rank = PageRank.Compute(
                adjacency,
                request.DampingFactor,
                request.MaxIterations,
                request.Tolerance,
                out int iterations,
                out bool converged,
                token);

            result.Iterations = iterations;
            result.Converged = converged;

            List<GraphAlgorithmNodeResult> nodes = new List<GraphAlgorithmNodeResult>(adjacency.NodeCount);
            for (int i = 0; i < adjacency.NodeCount; i++)
            {
                GraphAlgorithmNodeResult item = new GraphAlgorithmNodeResult();
                item.NodeGUID = adjacency.NodeGuids[i];
                item.Name = adjacency.NodeNames[i];
                item.Score = rank[i];
                nodes.Add(item);
            }

            nodes.Sort((a, b) => b.Score.CompareTo(a.Score));
            return nodes;
        }

        private static List<GraphAlgorithmNodeResult> BuildScored(GraphAdjacency adjacency, double[] scores)
        {
            List<GraphAlgorithmNodeResult> nodes = new List<GraphAlgorithmNodeResult>(adjacency.NodeCount);
            for (int i = 0; i < adjacency.NodeCount; i++)
            {
                GraphAlgorithmNodeResult item = new GraphAlgorithmNodeResult();
                item.NodeGUID = adjacency.NodeGuids[i];
                item.Name = adjacency.NodeNames[i];
                item.Score = scores[i];
                nodes.Add(item);
            }

            nodes.Sort((a, b) => b.Score.CompareTo(a.Score));
            return nodes;
        }

        private static List<GraphAlgorithmNodeResult> BuildEigenvector(
            GraphAdjacency adjacency,
            GraphAlgorithmRequest request,
            GraphAlgorithmResult result,
            CancellationToken token)
        {
            double[] scores = EigenvectorCentrality.Compute(
                adjacency,
                request.MaxIterations,
                request.Tolerance,
                out int iterations,
                out bool converged,
                token);

            result.Iterations = iterations;
            result.Converged = converged;
            return BuildScored(adjacency, scores);
        }

        private static double[] ToDouble(int[] values)
        {
            double[] result = new double[values.Length];
            for (int i = 0; i < values.Length; i++) result[i] = values[i];
            return result;
        }

        private static List<GraphAlgorithmNodeResult> BuildLouvain(
            GraphAdjacency adjacency,
            GraphAlgorithmRequest request,
            GraphAlgorithmResult result,
            CancellationToken token)
        {
            long[] labels = Louvain.Compute(
                adjacency,
                request.MaxIterations,
                out int communityCount,
                out int levels,
                out bool converged,
                token);

            result.CommunityCount = communityCount;
            result.Iterations = levels;
            result.Converged = converged;

            List<GraphAlgorithmNodeResult> nodes = new List<GraphAlgorithmNodeResult>(adjacency.NodeCount);
            for (int i = 0; i < adjacency.NodeCount; i++)
            {
                GraphAlgorithmNodeResult item = new GraphAlgorithmNodeResult();
                item.NodeGUID = adjacency.NodeGuids[i];
                item.Name = adjacency.NodeNames[i];
                item.Community = labels[i];
                nodes.Add(item);
            }

            nodes.Sort((a, b) => a.Community.Value.CompareTo(b.Community.Value));
            return nodes;
        }

        private static List<GraphAlgorithmNodeResult> BuildComponents(
            GraphAdjacency adjacency,
            GraphAlgorithmResult result,
            bool strong,
            CancellationToken token)
        {
            long[] labels;
            int count;
            if (strong) labels = StronglyConnectedComponents.Compute(adjacency, out count, token);
            else labels = WeaklyConnectedComponents.Compute(adjacency, out count, token);

            result.CommunityCount = count;

            List<GraphAlgorithmNodeResult> nodes = new List<GraphAlgorithmNodeResult>(adjacency.NodeCount);
            for (int i = 0; i < adjacency.NodeCount; i++)
            {
                GraphAlgorithmNodeResult item = new GraphAlgorithmNodeResult();
                item.NodeGUID = adjacency.NodeGuids[i];
                item.Name = adjacency.NodeNames[i];
                item.Community = labels[i];
                nodes.Add(item);
            }

            nodes.Sort((a, b) => a.Community.Value.CompareTo(b.Community.Value));
            return nodes;
        }

        private static List<GraphAlgorithmNodeResult> BuildLabelPropagation(
            GraphAdjacency adjacency,
            GraphAlgorithmRequest request,
            GraphAlgorithmResult result,
            CancellationToken token)
        {
            long[] labels = LabelPropagation.Compute(
                adjacency,
                request.MaxIterations,
                out int communityCount,
                out int iterations,
                out bool converged,
                token);

            result.CommunityCount = communityCount;
            result.Iterations = iterations;
            result.Converged = converged;

            List<GraphAlgorithmNodeResult> nodes = new List<GraphAlgorithmNodeResult>(adjacency.NodeCount);
            for (int i = 0; i < adjacency.NodeCount; i++)
            {
                GraphAlgorithmNodeResult item = new GraphAlgorithmNodeResult();
                item.NodeGUID = adjacency.NodeGuids[i];
                item.Name = adjacency.NodeNames[i];
                item.Community = labels[i];
                nodes.Add(item);
            }

            nodes.Sort((a, b) => a.Community.Value.CompareTo(b.Community.Value));
            return nodes;
        }

        #endregion
    }
}
