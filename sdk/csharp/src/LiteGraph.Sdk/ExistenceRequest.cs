namespace LiteGraph.Sdk
{
    using System;
    using System.Collections.Generic;
    using ExpressionTree;

    /// <summary>
    /// Existence check for multiple identifiers request.
    /// </summary>
    public class ExistenceRequest
    {
        #region Public-Members

        /// <summary>
        /// List of node GUIDs.
        /// </summary>
        public List<Guid> Nodes { get; set; } = null;

        /// <summary>
        /// List of edge GUIDs.
        /// </summary>
        public List<Guid> Edges { get; set; } = null;

        /// <summary>
        /// List of edges between two nodes.
        /// </summary>
        public List<EdgeBetween> EdgesBetween { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Route request.
        /// </summary>
        public ExistenceRequest()
        {

        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Verify that the object contains at least one existence request.
        /// </summary>
        /// <returns>True if present.</returns>
        public bool ContainsExistenceRequest()
        {
            if (Nodes != null && Nodes.Count > 0) return true;
            if (Edges != null && Edges.Count > 0) return true;
            if (EdgesBetween != null && EdgesBetween.Count > 0) return true;
            return false;
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}
