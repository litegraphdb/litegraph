namespace LiteGraph.Query.Ast
{
    using System.Collections.Generic;

    /// <summary>
    /// Parsed native graph query.
    /// </summary>
    public class GraphQueryAst
    {
        /// <summary>
        /// Query kind.
        /// </summary>
        public GraphQueryKindEnum Kind { get; set; }

        /// <summary>
        /// True when this is an OPTIONAL MATCH query.
        /// </summary>
        public bool IsOptional { get; set; }

        /// <summary>
        /// True when a path query should return only shortest matching paths.
        /// </summary>
        public bool IsShortestPath { get; set; }

        /// <summary>
        /// Node variable for node queries.
        /// </summary>
        public string NodeVariable { get; set; }

        /// <summary>
        /// Node label.
        /// </summary>
        public string NodeLabel { get; set; }

        /// <summary>
        /// From-node variable for edge queries.
        /// </summary>
        public string FromVariable { get; set; }

        /// <summary>
        /// Edge variable for edge queries.
        /// </summary>
        public string EdgeVariable { get; set; }

        /// <summary>
        /// Edge label.
        /// </summary>
        public string EdgeLabel { get; set; }

        /// <summary>
        /// To-node variable for edge queries.
        /// </summary>
        public string ToVariable { get; set; }

        /// <summary>
        /// Directed path segments for path queries.
        /// </summary>
        public List<GraphQueryPathSegment> PathSegments { get; set; } = new List<GraphQueryPathSegment>();

        /// <summary>
        /// Object variable for LiteGraph-native object creation.
        /// </summary>
        public string ObjectVariable { get; set; }

        /// <summary>
        /// WHERE variable.
        /// </summary>
        public string WhereVariable { get; set; }

        /// <summary>
        /// WHERE field.
        /// </summary>
        public string WhereField { get; set; }

        /// <summary>
        /// WHERE operator.
        /// </summary>
        public string WhereOperator { get; set; } = "=";

        /// <summary>
        /// WHERE value expression.
        /// </summary>
        public string WhereValueExpression { get; set; }

        /// <summary>
        /// WHERE predicate expression.
        /// </summary>
        public GraphQueryPredicateExpression WhereExpression { get; set; }

        /// <summary>
        /// WHERE predicate leaves.
        /// </summary>
        public List<GraphQueryPredicate> WherePredicates { get; set; } = new List<GraphQueryPredicate>();

        /// <summary>
        /// CREATE property expressions.
        /// </summary>
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Variable targeted by a SET clause.
        /// </summary>
        public string SetVariable { get; set; }

        /// <summary>
        /// SET property expressions.
        /// </summary>
        public Dictionary<string, string> SetProperties { get; set; } = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Variable targeted by a DELETE clause.
        /// </summary>
        public string DeleteVariable { get; set; }

        /// <summary>
        /// RETURN variables.
        /// </summary>
        public List<string> ReturnVariables { get; set; } = new List<string>();

        /// <summary>
        /// RETURN items.
        /// </summary>
        public List<GraphQueryReturnItem> ReturnItems { get; set; } = new List<GraphQueryReturnItem>();

        /// <summary>
        /// ORDER BY variable. Empty when ordering by a scalar returned directly.
        /// </summary>
        public string OrderVariable { get; set; }

        /// <summary>
        /// ORDER BY field or scalar return variable.
        /// </summary>
        public string OrderField { get; set; }

        /// <summary>
        /// True when ORDER BY direction is DESC.
        /// </summary>
        public bool OrderDescending { get; set; } = false;

        /// <summary>
        /// LIMIT value.
        /// </summary>
        public int? Limit { get; set; }

        /// <summary>
        /// Procedure name for CALL queries.
        /// </summary>
        public string ProcedureName { get; set; }

        /// <summary>
        /// Procedure argument expression.
        /// </summary>
        public string ProcedureArgumentExpression { get; set; }

        /// <summary>
        /// Vector search domain.
        /// </summary>
        public VectorSearchDomainEnum? VectorDomain { get; set; }

        /// <summary>
        /// YIELD variables.
        /// </summary>
        public List<string> YieldVariables { get; set; } = new List<string>();

        /// <summary>
        /// Clause pipeline for a chained query (Kind is Chained). Each clause is a MATCH or WITH stage; the terminal
        /// RETURN, ORDER BY, and LIMIT are carried on this AST's <see cref="ReturnItems"/>, <see cref="OrderField"/>,
        /// and <see cref="Limit"/> members. Empty for single-clause queries.
        /// </summary>
        public List<GraphQueryClause> Clauses { get; set; } = new List<GraphQueryClause>();
    }

    /// <summary>
    /// WHERE predicate.
    /// </summary>
    public class GraphQueryPredicate
    {
        /// <summary>
        /// Predicate variable.
        /// </summary>
        public string Variable { get; set; }

        /// <summary>
        /// Predicate field path.
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Predicate operator.
        /// </summary>
        public string Operator { get; set; } = "=";

        /// <summary>
        /// Predicate value expression.
        /// </summary>
        public string ValueExpression { get; set; }
    }

    /// <summary>
    /// RETURN item kind.
    /// </summary>
    public enum GraphQueryReturnItemKindEnum
    {
        /// <summary>
        /// Bound graph variable.
        /// </summary>
        Variable,

        /// <summary>
        /// Aggregate expression.
        /// </summary>
        Aggregate
    }

    /// <summary>
    /// RETURN aggregate function.
    /// </summary>
    public enum GraphQueryAggregateFunctionEnum
    {
        /// <summary>
        /// Count rows or non-null values.
        /// </summary>
        Count,

        /// <summary>
        /// Sum numeric values.
        /// </summary>
        Sum,

        /// <summary>
        /// Average numeric values.
        /// </summary>
        Avg,

        /// <summary>
        /// Minimum value.
        /// </summary>
        Min,

        /// <summary>
        /// Maximum value.
        /// </summary>
        Max
    }

    /// <summary>
    /// RETURN item.
    /// </summary>
    public class GraphQueryReturnItem
    {
        /// <summary>
        /// Item kind.
        /// </summary>
        public GraphQueryReturnItemKindEnum Kind { get; set; }

        /// <summary>
        /// Variable name for variable returns or aggregate argument root.
        /// </summary>
        public string Variable { get; set; }

        /// <summary>
        /// Aggregate function.
        /// </summary>
        public GraphQueryAggregateFunctionEnum? AggregateFunction { get; set; }

        /// <summary>
        /// True for COUNT(*).
        /// </summary>
        public bool AggregateWildcard { get; set; }

        /// <summary>
        /// Aggregate field path.
        /// </summary>
        public string Field { get; set; }

        /// <summary>
        /// Output alias.
        /// </summary>
        public string Alias { get; set; }
    }

    /// <summary>
    /// WHERE predicate expression kind.
    /// </summary>
    public enum GraphQueryPredicateExpressionKindEnum
    {
        /// <summary>
        /// Predicate leaf.
        /// </summary>
        Predicate,

        /// <summary>
        /// Logical AND.
        /// </summary>
        And,

        /// <summary>
        /// Logical OR.
        /// </summary>
        Or,

        /// <summary>
        /// Logical NOT.
        /// </summary>
        Not
    }

    /// <summary>
    /// WHERE predicate expression.
    /// </summary>
    public class GraphQueryPredicateExpression
    {
        /// <summary>
        /// Expression kind.
        /// </summary>
        public GraphQueryPredicateExpressionKindEnum Kind { get; set; }

        /// <summary>
        /// Predicate for leaf expressions.
        /// </summary>
        public GraphQueryPredicate Predicate { get; set; }

        /// <summary>
        /// Left expression.
        /// </summary>
        public GraphQueryPredicateExpression Left { get; set; }

        /// <summary>
        /// Right expression.
        /// </summary>
        public GraphQueryPredicateExpression Right { get; set; }
    }

    /// <summary>
    /// Directed path segment.
    /// </summary>
    public class GraphQueryPathSegment
    {
        /// <summary>
        /// From-node variable.
        /// </summary>
        public string FromVariable { get; set; }

        /// <summary>
        /// From-node label.
        /// </summary>
        public string FromLabel { get; set; }

        /// <summary>
        /// Edge variable.
        /// </summary>
        public string EdgeVariable { get; set; }

        /// <summary>
        /// Edge label.
        /// </summary>
        public string EdgeLabel { get; set; }

        /// <summary>
        /// To-node variable.
        /// </summary>
        public string ToVariable { get; set; }

        /// <summary>
        /// To-node label.
        /// </summary>
        public string ToLabel { get; set; }

        /// <summary>
        /// True when this edge segment is a bounded variable-length traversal.
        /// </summary>
        public bool IsVariableLength { get; set; }

        /// <summary>
        /// Minimum hop count for variable-length traversal segments.
        /// </summary>
        public int MinHops { get; set; } = 1;

        /// <summary>
        /// Maximum hop count for variable-length traversal segments.
        /// </summary>
        public int MaxHops { get; set; } = 1;
    }
}
