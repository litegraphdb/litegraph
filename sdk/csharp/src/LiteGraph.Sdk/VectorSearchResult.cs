namespace LiteGraph.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Vector search result.
    /// </summary>
    public class VectorSearchResult
    {
        #region Public-Members

        /// <summary>
        /// Score.
        /// </summary>
        public float? Score { get; set; } = null;

        /// <summary>
        /// Distance.
        /// </summary>
        public float? Distance { get; set; } = null;

        /// <summary>
        /// Inner product.
        /// </summary>
        public float? InnerProduct { get; set; } = null;

        /// <summary>
        /// Graph.
        /// </summary>
        public Graph Graph { get; set; } = null;

        /// <summary>
        /// Node.
        /// </summary>
        public Node Node { get; set; } = null;

        /// <summary>
        /// Edge.
        /// </summary>
        public Edge Edge { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate the object.
        /// </summary>
        public VectorSearchResult()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
