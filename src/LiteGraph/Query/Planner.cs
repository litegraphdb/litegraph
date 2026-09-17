namespace LiteGraph.Query
{
    using System;
    using System.Collections.Generic;
    using LiteGraph.Query.Ast;

    /// <summary>
    /// Planner for LiteGraph native graph queries.
    /// </summary>
    public class Planner
    {
        /// <summary>
        /// Build a provider-neutral plan from a parsed AST.
        /// </summary>
        /// <param name="ast">Parsed AST.</param>
        /// <param name="request">Query request.</param>
        /// <returns>Query plan.</returns>
        public GraphQueryPlan Plan(GraphQueryAst ast, GraphQueryRequest request)
        {
            if (ast == null) throw new ArgumentNullException(nameof(ast));
            if (request == null) throw new ArgumentNullException(nameof(request));

            List<string> warnings = new List<string>();
            GraphQueryPlanSeedKindEnum seedKind = DetermineSeed(ast, out string seedVariable, out string seedField);
            int cost = EstimateCost(ast, seedKind);

            if (!ast.Limit.HasValue && request.MaxResults > 1000)
                warnings.Add("Query has no LIMIT and relies on MaxResults.");

            if (HasOrder(ast) && !ast.Limit.HasValue)
                warnings.Add("ORDER BY without LIMIT sorts up to MaxResults.");

            if (ast.WhereExpression != null && !IsConjunctiveWhereExpression(ast.WhereExpression))
                warnings.Add("OR/NOT predicates may require scanning candidate rows.");

            if (HasAggregateReturn(ast))
                warnings.Add("Aggregate queries scan up to LIMIT or MaxResults.");

            if (HasVariableLengthPath(ast))
                warnings.Add("Variable-length path queries expand candidate traversals up to the bounded maximum hop count.");

            if (ast.IsShortestPath)
                warnings.Add("MATCH SHORTEST evaluates bounded path candidates and returns only minimum-hop matches.");

            if (ast.IsOptional)
                warnings.Add("OPTIONAL MATCH returns a null row when no rows match.");

            if (ast.Kind == GraphQueryKindEnum.Chained)
                warnings.Add("Chained query evaluates each MATCH clause and joins on shared variables; intermediate row sets are bounded by MaxScanRows.");

            return new GraphQueryPlan(
                ast,
                IsMutation(ast.Kind),
                ast.Kind == GraphQueryKindEnum.VectorSearch,
                HasOrder(ast),
                ast.Limit.HasValue,
                cost,
                seedKind,
                seedVariable,
                seedField,
                warnings);
        }

        private static GraphQueryPlanSeedKindEnum DetermineSeed(GraphQueryAst ast, out string seedVariable, out string seedField)
        {
            seedVariable = null;
            seedField = null;

            if (ast.Kind == GraphQueryKindEnum.VectorSearch)
                return GraphQueryPlanSeedKindEnum.VectorIndex;

            List<GraphQueryPredicate> predicates = GetPredicates(ast);
            if (predicates.Count != 1) return GraphQueryPlanSeedKindEnum.None;

            GraphQueryPredicate predicate = predicates[0];
            if (!IsEqualsOperator(predicate.Operator)) return GraphQueryPlanSeedKindEnum.None;

            seedVariable = predicate.Variable;
            seedField = predicate.Field;

            if (ast.Kind == GraphQueryKindEnum.MatchNode)
            {
                if (Same(predicate.Variable, ast.NodeVariable) && Same(predicate.Field, "guid")) return GraphQueryPlanSeedKindEnum.NodeGuid;
                if (Same(predicate.Variable, ast.NodeVariable) && Same(predicate.Field, "name")) return GraphQueryPlanSeedKindEnum.NodeName;
            }

            if (ast.Kind == GraphQueryKindEnum.MatchEdge)
            {
                if (Same(predicate.Variable, ast.FromVariable) && Same(predicate.Field, "guid")) return GraphQueryPlanSeedKindEnum.EdgeFromGuid;
                if (Same(predicate.Variable, ast.ToVariable) && Same(predicate.Field, "guid")) return GraphQueryPlanSeedKindEnum.EdgeToGuid;
                if (Same(predicate.Variable, ast.EdgeVariable) && Same(predicate.Field, "guid")) return GraphQueryPlanSeedKindEnum.EdgeGuid;
                if (Same(predicate.Variable, ast.EdgeVariable) && Same(predicate.Field, "name")) return GraphQueryPlanSeedKindEnum.EdgeName;
            }

            if (ast.Kind == GraphQueryKindEnum.MatchPath)
            {
                return DeterminePathSeed(ast, predicate);
            }

            return GraphQueryPlanSeedKindEnum.None;
        }

        private static GraphQueryPlanSeedKindEnum DeterminePathSeed(GraphQueryAst ast, GraphQueryPredicate predicate)
        {
            if (ast.PathSegments == null) return GraphQueryPlanSeedKindEnum.None;

            foreach (GraphQueryPathSegment segment in ast.PathSegments)
            {
                if (Same(predicate.Variable, segment.FromVariable) && Same(predicate.Field, "guid")) return GraphQueryPlanSeedKindEnum.EdgeFromGuid;
                if (Same(predicate.Variable, segment.ToVariable) && Same(predicate.Field, "guid")) return GraphQueryPlanSeedKindEnum.EdgeToGuid;
                if (Same(predicate.Variable, segment.EdgeVariable) && Same(predicate.Field, "guid")) return GraphQueryPlanSeedKindEnum.EdgeGuid;
                if (Same(predicate.Variable, segment.EdgeVariable) && Same(predicate.Field, "name")) return GraphQueryPlanSeedKindEnum.EdgeName;
            }

            return GraphQueryPlanSeedKindEnum.None;
        }

        private static int EstimateCost(GraphQueryAst ast, GraphQueryPlanSeedKindEnum seedKind)
        {
            if (seedKind == GraphQueryPlanSeedKindEnum.VectorIndex) return 5;
            if (ast.Kind == GraphQueryKindEnum.Chained) return 100 + (ast.Clauses != null ? ast.Clauses.Count * 20 : 0);
            if (ast.Kind == GraphQueryKindEnum.MatchPath && HasVariableLengthPath(ast)) return EstimatePathCost(ast);
            if (seedKind != GraphQueryPlanSeedKindEnum.None) return 10;
            if (IsMutation(ast.Kind)) return 20;
            if (HasAggregateReturn(ast)) return 90;
            if (ast.Kind == GraphQueryKindEnum.MatchPath) return EstimatePathCost(ast);
            if (HasOrder(ast)) return 80;
            return 50;
        }

        private static int EstimatePathCost(GraphQueryAst ast)
        {
            int segmentCost = 0;
            if (ast.PathSegments != null)
            {
                foreach (GraphQueryPathSegment segment in ast.PathSegments)
                {
                    segmentCost += segment.IsVariableLength ? Math.Max(1, segment.MaxHops) * 50 : 25;
                }
            }

            if (ast.IsShortestPath) segmentCost += 25;
            return 100 + segmentCost;
        }

        private static bool IsMutation(GraphQueryKindEnum kind)
        {
            switch (kind)
            {
                case GraphQueryKindEnum.CreateNode:
                case GraphQueryKindEnum.CreateEdge:
                case GraphQueryKindEnum.CreateLabel:
                case GraphQueryKindEnum.CreateTag:
                case GraphQueryKindEnum.CreateVector:
                case GraphQueryKindEnum.UpdateNode:
                case GraphQueryKindEnum.UpdateEdge:
                case GraphQueryKindEnum.UpdateLabel:
                case GraphQueryKindEnum.UpdateTag:
                case GraphQueryKindEnum.UpdateVector:
                case GraphQueryKindEnum.DeleteNode:
                case GraphQueryKindEnum.DeleteEdge:
                case GraphQueryKindEnum.DeleteLabel:
                case GraphQueryKindEnum.DeleteTag:
                case GraphQueryKindEnum.DeleteVector:
                    return true;
                default:
                    return false;
            }
        }

        private static List<GraphQueryPredicate> GetPredicates(GraphQueryAst ast)
        {
            if (ast?.WhereExpression != null)
            {
                List<GraphQueryPredicate> predicates = new List<GraphQueryPredicate>();
                if (TryCollectConjunctivePredicates(ast.WhereExpression, predicates)) return predicates;
                return new List<GraphQueryPredicate>();
            }

            if (ast.WherePredicates != null && ast.WherePredicates.Count > 0) return ast.WherePredicates;
            if (String.IsNullOrEmpty(ast.WhereVariable)) return new List<GraphQueryPredicate>();

            return new List<GraphQueryPredicate>
            {
                new GraphQueryPredicate
                {
                    Variable = ast.WhereVariable,
                    Field = ast.WhereField,
                    Operator = ast.WhereOperator,
                    ValueExpression = ast.WhereValueExpression
                }
            };
        }

        private static bool IsConjunctiveWhereExpression(GraphQueryPredicateExpression expression)
        {
            return TryCollectConjunctivePredicates(expression, new List<GraphQueryPredicate>());
        }

        private static bool TryCollectConjunctivePredicates(GraphQueryPredicateExpression expression, List<GraphQueryPredicate> predicates)
        {
            if (expression == null) return true;

            switch (expression.Kind)
            {
                case GraphQueryPredicateExpressionKindEnum.Predicate:
                    if (expression.Predicate != null) predicates.Add(expression.Predicate);
                    return true;
                case GraphQueryPredicateExpressionKindEnum.And:
                    return TryCollectConjunctivePredicates(expression.Left, predicates)
                        && TryCollectConjunctivePredicates(expression.Right, predicates);
                default:
                    return false;
            }
        }

        private static bool HasOrder(GraphQueryAst ast)
        {
            return ast != null && !String.IsNullOrEmpty(ast.OrderField);
        }

        private static bool HasAggregateReturn(GraphQueryAst ast)
        {
            if (ast?.ReturnItems == null) return false;
            foreach (GraphQueryReturnItem item in ast.ReturnItems)
            {
                if (item.Kind == GraphQueryReturnItemKindEnum.Aggregate) return true;
            }

            return false;
        }

        private static bool HasVariableLengthPath(GraphQueryAst ast)
        {
            if (ast?.PathSegments == null) return false;
            foreach (GraphQueryPathSegment segment in ast.PathSegments)
            {
                if (segment.IsVariableLength) return true;
            }

            return false;
        }

        private static bool IsEqualsOperator(string whereOperator)
        {
            return String.IsNullOrEmpty(whereOperator) || String.Equals(whereOperator, "=", StringComparison.Ordinal);
        }

        private static bool Same(string left, string right)
        {
            return String.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }
}
