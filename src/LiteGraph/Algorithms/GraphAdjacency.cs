namespace LiteGraph.Algorithms
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Compact in-memory adjacency (compressed sparse row) representation of a single graph, built once from streaming enumeration and consumed by graph algorithms.
    /// This structure holds both the forward (out-edge) and reverse (in-edge) adjacency so directed and undirected algorithms can share one load.
    /// Edge weights are uniform (1.0) in this representation; edge cost is not treated as weight.
    /// This type is not thread-safe; build one instance per algorithm run.
    /// </summary>
    public class GraphAdjacency
    {
        #region Public-Members

        /// <summary>
        /// Number of nodes.
        /// </summary>
        public int NodeCount { get; private set; } = 0;

        /// <summary>
        /// Number of directed edges.
        /// </summary>
        public int EdgeCount { get; private set; } = 0;

        /// <summary>
        /// Node GUID by dense index (length <see cref="NodeCount"/>).
        /// </summary>
        public Guid[] NodeGuids { get; private set; } = Array.Empty<Guid>();

        /// <summary>
        /// Node name by dense index (length <see cref="NodeCount"/>).  Entries may be null.
        /// </summary>
        public string[] NodeNames { get; private set; } = Array.Empty<string>();

        /// <summary>
        /// Out-edge row offsets (length <see cref="NodeCount"/> + 1).  Out-neighbors of node i occupy <see cref="OutTargets"/>[OutOffsets[i] .. OutOffsets[i+1]).
        /// </summary>
        public int[] OutOffsets { get; private set; } = new int[1];

        /// <summary>
        /// Out-edge targets (length <see cref="EdgeCount"/>).
        /// </summary>
        public int[] OutTargets { get; private set; } = Array.Empty<int>();

        /// <summary>
        /// In-edge row offsets (length <see cref="NodeCount"/> + 1).  In-neighbors of node i occupy <see cref="InSources"/>[InOffsets[i] .. InOffsets[i+1]).
        /// </summary>
        public int[] InOffsets { get; private set; } = new int[1];

        /// <summary>
        /// In-edge sources (length <see cref="EdgeCount"/>).
        /// </summary>
        public int[] InSources { get; private set; } = Array.Empty<int>();

        #endregion

        #region Private-Members

        private Dictionary<Guid, int> _GuidToIndex = new Dictionary<Guid, int>();

        #endregion

        #region Constructors-and-Factories

        private GraphAdjacency()
        {

        }

        /// <summary>
        /// Build an adjacency structure for a graph by streaming its nodes and edges.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="maxNodes">Maximum node count permitted; 0 means unlimited.  Exceeding this throws.</param>
        /// <param name="maxEdges">Maximum edge count permitted; 0 means unlimited.  Exceeding this throws.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Graph adjacency.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the client is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the graph exceeds the configured node or edge ceiling.</exception>
        public static async Task<GraphAdjacency> BuildAsync(
            LiteGraphClient client,
            Guid tenantGuid,
            Guid graphGuid,
            int maxNodes,
            int maxEdges,
            CancellationToken token = default)
        {
            if (client == null) throw new ArgumentNullException(nameof(client));

            GraphAdjacency adj = new GraphAdjacency();

            List<Guid> guids = new List<Guid>();
            List<string> names = new List<string>();

            await foreach (Node node in client.Node.ReadAllInGraph(
                tenantGuid,
                graphGuid,
                EnumerationOrderEnum.CreatedAscending,
                0,
                false,
                false,
                token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                if (node == null) continue;
                if (adj._GuidToIndex.ContainsKey(node.GUID)) continue;

                adj._GuidToIndex[node.GUID] = guids.Count;
                guids.Add(node.GUID);
                names.Add(node.Name);

                if (maxNodes > 0 && guids.Count > maxNodes)
                    throw new InvalidOperationException(
                        "Graph node count exceeds the configured algorithm ceiling of " + maxNodes + " nodes. " +
                        "Reduce the graph size, raise the ceiling, or export the graph for external computation.");
            }

            adj.NodeCount = guids.Count;
            adj.NodeGuids = guids.ToArray();
            adj.NodeNames = names.ToArray();

            List<int> edgeFrom = new List<int>();
            List<int> edgeTo = new List<int>();

            await foreach (Edge edge in client.Edge.ReadAllInGraph(
                tenantGuid,
                graphGuid,
                EnumerationOrderEnum.CreatedAscending,
                0,
                false,
                false,
                token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                if (edge == null) continue;

                int fromIndex;
                int toIndex;
                if (!adj._GuidToIndex.TryGetValue(edge.From, out fromIndex)) continue;
                if (!adj._GuidToIndex.TryGetValue(edge.To, out toIndex)) continue;

                edgeFrom.Add(fromIndex);
                edgeTo.Add(toIndex);

                if (maxEdges > 0 && edgeFrom.Count > maxEdges)
                    throw new InvalidOperationException(
                        "Graph edge count exceeds the configured algorithm ceiling of " + maxEdges + " edges. " +
                        "Reduce the graph size, raise the ceiling, or export the graph for external computation.");
            }

            adj.EdgeCount = edgeFrom.Count;
            adj.BuildCsr(edgeFrom, edgeTo);
            return adj;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Get the dense index for a node GUID.
        /// </summary>
        /// <param name="nodeGuid">Node GUID.</param>
        /// <param name="index">Dense index when found.</param>
        /// <returns>True when the node exists in this adjacency.</returns>
        public bool TryGetIndex(Guid nodeGuid, out int index)
        {
            return _GuidToIndex.TryGetValue(nodeGuid, out index);
        }

        /// <summary>
        /// Out-degree of a node by index.
        /// </summary>
        /// <param name="index">Node index.</param>
        /// <returns>Out-degree.</returns>
        public int OutDegree(int index)
        {
            return OutOffsets[index + 1] - OutOffsets[index];
        }

        /// <summary>
        /// In-degree of a node by index.
        /// </summary>
        /// <param name="index">Node index.</param>
        /// <returns>In-degree.</returns>
        public int InDegree(int index)
        {
            return InOffsets[index + 1] - InOffsets[index];
        }

        #endregion

        #region Private-Methods

        private void BuildCsr(List<int> edgeFrom, List<int> edgeTo)
        {
            int n = NodeCount;
            int e = EdgeCount;

            OutOffsets = new int[n + 1];
            OutTargets = new int[e];
            InOffsets = new int[n + 1];
            InSources = new int[e];

            for (int i = 0; i < e; i++)
            {
                OutOffsets[edgeFrom[i] + 1]++;
                InOffsets[edgeTo[i] + 1]++;
            }

            for (int i = 0; i < n; i++)
            {
                OutOffsets[i + 1] += OutOffsets[i];
                InOffsets[i + 1] += InOffsets[i];
            }

            int[] outCursor = new int[n];
            int[] inCursor = new int[n];
            Array.Copy(OutOffsets, outCursor, n);
            Array.Copy(InOffsets, inCursor, n);

            for (int i = 0; i < e; i++)
            {
                int from = edgeFrom[i];
                int to = edgeTo[i];

                OutTargets[outCursor[from]] = to;
                outCursor[from]++;

                InSources[inCursor[to]] = from;
                inCursor[to]++;
            }
        }

        #endregion
    }
}
