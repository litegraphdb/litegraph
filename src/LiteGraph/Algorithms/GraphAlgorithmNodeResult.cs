namespace LiteGraph.Algorithms
{
    using System;

    /// <summary>
    /// Per-node result produced by a graph algorithm.
    /// </summary>
    public class GraphAlgorithmNodeResult
    {
        #region Public-Members

        /// <summary>
        /// Node GUID.
        /// </summary>
        public Guid NodeGUID { get; set; } = default(Guid);

        /// <summary>
        /// Node name, when available.
        /// </summary>
        public string Name { get; set; } = null;

        /// <summary>
        /// Score produced by the algorithm.  Used by centrality and ranking algorithms (for example PageRank score or degree count).
        /// </summary>
        public double Score { get; set; } = 0d;

        /// <summary>
        /// Number of inbound edges.  Populated by degree centrality; null otherwise.
        /// </summary>
        public int? EdgesIn { get; set; } = null;

        /// <summary>
        /// Number of outbound edges.  Populated by degree centrality; null otherwise.
        /// </summary>
        public int? EdgesOut { get; set; } = null;

        /// <summary>
        /// Community or component identifier.  Populated by community detection and connected-component algorithms; null otherwise.
        /// </summary>
        public long? Community { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public GraphAlgorithmNodeResult()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
