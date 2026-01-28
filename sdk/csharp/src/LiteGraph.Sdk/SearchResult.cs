namespace LiteGraph.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Search result.
    /// </summary>
    public class SearchResult
    {
        #region Public-Members

        /// <summary>
        /// Graphs.
        /// </summary>
        public List<Graph> Graphs { get; set; } = null;

        /// <summary>
        /// Nodes.
        /// </summary>
        public List<Node> Nodes { get; set; } = null;

        /// <summary>
        /// Edges.
        /// </summary>
        public List<Edge> Edges { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public SearchResult()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
