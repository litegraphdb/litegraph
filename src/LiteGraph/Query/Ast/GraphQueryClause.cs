namespace LiteGraph.Query.Ast
{
    /// <summary>
    /// A single stage in a chained native graph query pipeline: either a MATCH clause (a graph pattern plus optional
    /// WHERE) or a WITH clause that projects and reshapes the intermediate result set before the next stage.
    /// </summary>
    public class GraphQueryClause
    {
        #region Public-Members

        /// <summary>
        /// The clause type.
        /// </summary>
        public GraphQueryClauseTypeEnum Type { get; set; }

        /// <summary>
        /// The MATCH clause pattern and optional WHERE, expressed as a single-clause AST (Kind is MatchNode or MatchEdge).
        /// Populated when <see cref="Type"/> is <see cref="GraphQueryClauseTypeEnum.Match"/>.
        /// </summary>
        public GraphQueryAst Match { get; set; } = null;

        /// <summary>
        /// The WITH projection specification. Populated when <see cref="Type"/> is <see cref="GraphQueryClauseTypeEnum.With"/>.
        /// </summary>
        public GraphQueryWith With { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public GraphQueryClause()
        {

        }

        #endregion
    }
}
