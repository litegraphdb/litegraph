namespace LiteGraph.Algorithms
{
    using System;

    /// <summary>
    /// Configuration and guardrails for graph algorithm execution.
    /// </summary>
    public class GraphAlgorithmConfiguration
    {
        #region Public-Members

        /// <summary>
        /// Maximum number of nodes an algorithm will load into memory.  Graphs exceeding this are rejected rather than risking exhaustion of memory.  Minimum 0 (0 means unlimited), default 1000000.
        /// </summary>
        public int MaxNodes
        {
            get
            {
                return _MaxNodes;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(MaxNodes));
                _MaxNodes = value;
            }
        }

        /// <summary>
        /// Maximum number of edges an algorithm will load into memory.  Graphs exceeding this are rejected rather than risking exhaustion of memory.  Minimum 0 (0 means unlimited), default 10000000.
        /// </summary>
        public int MaxEdges
        {
            get
            {
                return _MaxEdges;
            }
            set
            {
                if (value < 0) throw new ArgumentOutOfRangeException(nameof(MaxEdges));
                _MaxEdges = value;
            }
        }

        #endregion

        #region Private-Members

        private int _MaxNodes = 1000000;
        private int _MaxEdges = 10000000;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public GraphAlgorithmConfiguration()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
