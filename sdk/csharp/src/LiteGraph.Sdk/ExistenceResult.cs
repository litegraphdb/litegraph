namespace LiteGraph.Sdk
{
    using System;
    using System.Collections.Generic;
    using ExpressionTree;

    /// <summary>
    /// Existence check for multiple identifiers result.
    /// </summary>
    public class ExistenceResult
    {
        #region Public-Members

        /// <summary>
        /// List of existing node GUIDs.
        /// </summary>
        public List<Guid> ExistingNodes { get; set; } = null;

        /// <summary>
        /// List of missing node GUIDs.
        /// </summary>
        public List<Guid> MissingNodes { get; set; } = null;

        /// <summary>
        /// List of existing edge GUIDs.
        /// </summary>
        public List<Guid> ExistingEdges { get; set; } = null;

        /// <summary>
        /// List of missing edge GUIDs.
        /// </summary>
        public List<Guid> MissingEdges { get; set; } = null;

        /// <summary>
        /// List of existing edges between two nodes.
        /// </summary>
        public List<EdgeBetween> ExistingEdgesBetween { get; set; } = null;

        /// <summary>
        /// List of missing edges between two nodes.
        /// </summary>
        public List<EdgeBetween> MissingEdgesBetween { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Route request.
        /// </summary>
        public ExistenceResult()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
