namespace LiteGraph.Query.Ast
{
    /// <summary>
    /// Type of a clause in a chained native graph query pipeline.
    /// </summary>
    public enum GraphQueryClauseTypeEnum
    {
        /// <summary>
        /// A MATCH clause contributing a graph pattern and optional WHERE filter.
        /// </summary>
        Match,

        /// <summary>
        /// A WITH clause that projects, filters, orders, and optionally aggregates the intermediate result set before the next stage.
        /// </summary>
        With
    }
}
