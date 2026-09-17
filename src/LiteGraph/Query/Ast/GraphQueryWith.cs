namespace LiteGraph.Query.Ast
{
    using System.Collections.Generic;

    /// <summary>
    /// WITH clause specification for a chained native graph query. Projects and renames variables, optionally aggregates
    /// them, applies a post-projection WHERE filter (HAVING semantics), and optionally orders, skips, and limits the
    /// intermediate result set before it flows into the next pipeline stage.
    /// </summary>
    public class GraphQueryWith
    {
        #region Public-Members

        /// <summary>
        /// Projection items. Each item is either a graph variable (optionally aliased with AS) or an aggregate expression.
        /// When any item is an aggregate, the non-aggregate items form the grouping key.
        /// </summary>
        public List<GraphQueryReturnItem> Items { get; set; } = new List<GraphQueryReturnItem>();

        /// <summary>
        /// Optional post-projection WHERE filter (HAVING semantics), evaluated against the projected rows.
        /// </summary>
        public GraphQueryPredicateExpression WhereExpression { get; set; } = null;

        /// <summary>
        /// Optional ORDER BY variable when ordering by an object field (for example the variable in variable.field).
        /// </summary>
        public string OrderVariable { get; set; } = null;

        /// <summary>
        /// Optional ORDER BY field or projected scalar/alias to order by.
        /// </summary>
        public string OrderField { get; set; } = null;

        /// <summary>
        /// True when ORDER BY direction is DESC.
        /// </summary>
        public bool OrderDescending { get; set; } = false;

        /// <summary>
        /// Optional number of rows to skip after ordering. Null when no SKIP is present.
        /// </summary>
        public int? Skip { get; set; } = null;

        /// <summary>
        /// Optional maximum number of rows to carry forward after ordering and skipping. Null when no LIMIT is present.
        /// </summary>
        public int? Limit { get; set; } = null;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public GraphQueryWith()
        {

        }

        #endregion
    }
}
