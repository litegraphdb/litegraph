namespace LiteGraph.Sdk
{
    using System;
    using ExpressionTree;

    /// <summary>
    /// Route request.
    /// </summary>
    public class RouteRequest
    {
        #region Public-Members

        /// <summary>
        /// Graph GUID.
        /// </summary>
        public Guid Graph { get; set; } = default(Guid);

        /// <summary>
        /// From node GUID.
        /// </summary>
        public Guid From { get; set; } = default(Guid);

        /// <summary>
        /// To node GUID.
        /// </summary>
        public Guid To { get; set; } = default(Guid);

        /// <summary>
        /// Edge filters.
        /// </summary>
        public Expr EdgeFilter { get; set; } = null;

        /// <summary>
        /// Node filters.
        /// </summary>
        public Expr NodeFilter { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Route request.
        /// </summary>
        public RouteRequest()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}
