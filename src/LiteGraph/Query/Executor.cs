namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Query;

    /// <summary>
    /// Executor for planned native graph queries.
    /// </summary>
    internal class Executor
    {
        private readonly QueryExecutionEngine _Methods;

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="methods">Query execution engine.</param>
        internal Executor(QueryExecutionEngine methods)
        {
            _Methods = methods ?? throw new ArgumentNullException(nameof(methods));
        }

        /// <summary>
        /// Execute a query plan.
        /// </summary>
        /// <param name="tenantGuid">Tenant GUID.</param>
        /// <param name="graphGuid">Graph GUID.</param>
        /// <param name="request">Query request.</param>
        /// <param name="plan">Query plan.</param>
        /// <param name="token">Cancellation token.</param>
        /// <returns>Query result.</returns>
        internal async Task<GraphQueryResult> Execute(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryPlan plan, CancellationToken token)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            GraphQueryResult result;
            switch (plan.Kind)
            {
                case GraphQueryKindEnum.MatchNode:
                    result = await _Methods.ExecuteMatchNode(tenantGuid, graphGuid, request, plan.Ast, token).ConfigureAwait(false);
                    break;
                case GraphQueryKindEnum.MatchEdge:
                    result = await _Methods.ExecuteMatchEdge(tenantGuid, graphGuid, request, plan.Ast, token).ConfigureAwait(false);
                    break;
                case GraphQueryKindEnum.MatchPath:
                    result = await _Methods.ExecuteMatchPath(tenantGuid, graphGuid, request, plan.Ast, token).ConfigureAwait(false);
                    break;
                case GraphQueryKindEnum.CreateNode:
                    result = await _Methods.ExecuteCreateNode(tenantGuid, graphGuid, request, plan.Ast, token).ConfigureAwait(false);
                    break;
                case GraphQueryKindEnum.CreateEdge:
                    result = await _Methods.ExecuteCreateEdge(tenantGuid, graphGuid, request, plan.Ast, token).ConfigureAwait(false);
                    break;
                case GraphQueryKindEnum.CreateLabel:
                    result = await _Methods.ExecuteCreateLabel(tenantGuid, graphGuid, request, plan.Ast, token).ConfigureAwait(false);
                    break;
                case GraphQueryKindEnum.CreateTag:
                    result = await _Methods.ExecuteCreateTag(tenantGuid, graphGuid, request, plan.Ast, token).ConfigureAwait(false);
                    break;
                case GraphQueryKindEnum.CreateVector:
                    result = await _Methods.ExecuteCreateVector(tenantGuid, graphGuid, request, plan.Ast, token).ConfigureAwait(false);
                    break;
                case GraphQueryKindEnum.UpdateNode:
                    result = await _Methods.ExecuteUpdateNode(tenantGuid, graphGuid, request, plan.Ast, token).ConfigureAwait(false);
                    break;
                case GraphQueryKindEnum.UpdateEdge:
                    result = await _Methods.ExecuteUpdateEdge(tenantGuid, graphGuid, request, plan.Ast, token).ConfigureAwait(false);
                    break;
                case GraphQueryKindEnum.DeleteNode:
                    result = await _Methods.ExecuteDeleteNode(tenantGuid, graphGuid, request, plan.Ast, token).ConfigureAwait(false);
                    break;
                case GraphQueryKindEnum.DeleteEdge:
                    result = await _Methods.ExecuteDeleteEdge(tenantGuid, graphGuid, request, plan.Ast, token).ConfigureAwait(false);
                    break;
                case GraphQueryKindEnum.UpdateLabel:
                    result = await _Methods.ExecuteUpdateLabel(tenantGuid, graphGuid, request, plan.Ast, token).ConfigureAwait(false);
                    break;
                case GraphQueryKindEnum.UpdateTag:
                    result = await _Methods.ExecuteUpdateTag(tenantGuid, graphGuid, request, plan.Ast, token).ConfigureAwait(false);
                    break;
                case GraphQueryKindEnum.UpdateVector:
                    result = await _Methods.ExecuteUpdateVector(tenantGuid, graphGuid, request, plan.Ast, token).ConfigureAwait(false);
                    break;
                case GraphQueryKindEnum.DeleteLabel:
                    result = await _Methods.ExecuteDeleteLabel(tenantGuid, graphGuid, request, plan.Ast, token).ConfigureAwait(false);
                    break;
                case GraphQueryKindEnum.DeleteTag:
                    result = await _Methods.ExecuteDeleteTag(tenantGuid, graphGuid, request, plan.Ast, token).ConfigureAwait(false);
                    break;
                case GraphQueryKindEnum.DeleteVector:
                    result = await _Methods.ExecuteDeleteVector(tenantGuid, graphGuid, request, plan.Ast, token).ConfigureAwait(false);
                    break;
                case GraphQueryKindEnum.VectorSearch:
                    result = await _Methods.ExecuteVectorSearch(tenantGuid, graphGuid, request, plan.Ast, token).ConfigureAwait(false);
                    break;
                case GraphQueryKindEnum.Algorithm:
                    result = await _Methods.ExecuteAlgorithm(tenantGuid, graphGuid, request, plan.Ast, token).ConfigureAwait(false);
                    break;
                case GraphQueryKindEnum.Chained:
                    // ExecuteChained applies its own terminal projection, ORDER BY, and LIMIT.
                    return await _Methods.ExecuteChained(tenantGuid, graphGuid, request, plan.Ast, token).ConfigureAwait(false);
                default:
                    throw new NotSupportedException("Unsupported query kind '" + plan.Kind + "'.");
            }

            ApplyOptionalEmptyRow(result, plan);
            return QueryExecutionEngine.ApplyOrderAndLimit(result, plan.Ast, request);
        }

        private static void ApplyOptionalEmptyRow(GraphQueryResult result, GraphQueryPlan plan)
        {
            if (result == null || plan?.Ast == null) return;
            if (!plan.Ast.IsOptional || plan.Mutates || result.RowCount > 0) return;
            if (plan.Ast.ReturnVariables == null || plan.Ast.ReturnVariables.Count < 1) return;

            System.Collections.Generic.Dictionary<string, object> row = new System.Collections.Generic.Dictionary<string, object>(System.StringComparer.OrdinalIgnoreCase);
            foreach (string variable in plan.Ast.ReturnVariables)
            {
                if (!System.String.IsNullOrEmpty(variable)) row[variable] = null;
            }

            result.Rows.Add(row);
        }
    }
}
