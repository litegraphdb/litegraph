namespace LiteGraph.Sdk
{
    using System;

    /// <summary>
    /// Per-node result produced by a graph algorithm.
    /// </summary>
    public class GraphAlgorithmNodeResult
    {
        /// <summary>
        /// Node GUID.
        /// </summary>
        public Guid NodeGUID { get; set; } = default(Guid);

        /// <summary>
        /// Node name, when available.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Score produced by the algorithm (for example PageRank score or degree count).
        /// </summary>
        public double Score { get; set; } = 0d;

        /// <summary>
        /// Number of inbound edges (degree centrality only).
        /// </summary>
        public int? EdgesIn { get; set; } = null;

        /// <summary>
        /// Number of outbound edges (degree centrality only).
        /// </summary>
        public int? EdgesOut { get; set; } = null;

        /// <summary>
        /// Community or component identifier (community detection and connected-component algorithms only).
        /// </summary>
        public long? Community { get; set; } = null;

        /// <summary>
        /// Instantiate.
        /// </summary>
        public GraphAlgorithmNodeResult()
        {
        }
    }
}
