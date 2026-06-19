namespace LiteGraph.Client.Implementations
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph.Client.Interfaces;
    using LiteGraph.GraphRepositories;
    using LiteGraph.Query;
    using LiteGraph.Query.Ast;

    /// <summary>
    /// Native graph query execution engine.
    /// </summary>
    internal class QueryExecutionEngine
    {
        #region Private-Members

        private readonly LiteGraphClient _Client;
        private readonly GraphRepositoryBase _Repo;
        private readonly Planner _Planner;
        private readonly Executor _Executor;
        private readonly bool _AllowActiveTransaction;

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="client">LiteGraph client.</param>
        /// <param name="repo">Graph repository.</param>
        /// <param name="allowActiveTransaction">Allow mutation execution to participate in an already-active explicit transaction context.</param>
        internal QueryExecutionEngine(LiteGraphClient client, GraphRepositoryBase repo, bool allowActiveTransaction = false)
        {
            _Client = client ?? throw new ArgumentNullException(nameof(client));
            _Repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _Planner = new Planner();
            _Executor = new Executor(this);
            _AllowActiveTransaction = allowActiveTransaction;
        }

        #endregion

        #region Public-Methods

        /// <inheritdoc />
        public async Task<GraphQueryResult> Execute(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, CancellationToken token = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (String.IsNullOrEmpty(request.Query)) throw new ArgumentNullException(nameof(request.Query));

            token.ThrowIfCancellationRequested();
            Stopwatch stopwatch = Stopwatch.StartNew();
            using Activity queryActivity = LiteGraphTelemetry.ActivitySource.StartActivity(LiteGraphTelemetry.QueryActivityName, ActivityKind.Internal);
            SetQueryRequestActivityTags(queryActivity, tenantGuid, graphGuid, request);

            using (CancellationTokenSource timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(request.TimeoutSeconds)))
            using (CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token))
            {
                try
                {
                    GraphQueryExecutionProfile profile = request.IncludeProfile ? new GraphQueryExecutionProfile() : null;
                    using LiteGraphTelemetryTimingCapture timingCapture = profile != null ? LiteGraphTelemetry.BeginTimingCapture() : null;
                    GraphQueryAst ast;
                    GraphQueryPlan plan;

                    using (Activity parseActivity = StartQueryPhaseActivity(LiteGraphTelemetry.QueryParseActivityName, "parse", tenantGuid, graphGuid, request))
                    {
                        Stopwatch phase = Stopwatch.StartNew();
                        try
                        {
                            ast = Parser.Parse(request.Query);
                            phase.Stop();
                            if (profile != null) profile.ParseTimeMs = phase.Elapsed.TotalMilliseconds;
                            SetQueryAstActivityTags(parseActivity, ast);
                            CompletePhaseActivity(parseActivity, phase.Elapsed.TotalMilliseconds);
                        }
                        catch (Exception e)
                        {
                            phase.Stop();
                            parseActivity?.SetTag("litegraph.query.phase.duration_ms", phase.Elapsed.TotalMilliseconds);
                            LiteGraphTelemetry.SetActivityException(parseActivity, e);
                            throw;
                        }
                    }

                    using (Activity planActivity = StartQueryPhaseActivity(LiteGraphTelemetry.QueryPlanActivityName, "plan", tenantGuid, graphGuid, request))
                    {
                        Stopwatch phase = Stopwatch.StartNew();
                        try
                        {
                            plan = _Planner.Plan(ast, request);
                            phase.Stop();
                            if (profile != null) profile.PlanTimeMs = phase.Elapsed.TotalMilliseconds;
                            SetQueryPlanActivityTags(planActivity, plan);
                            CompletePhaseActivity(planActivity, phase.Elapsed.TotalMilliseconds);
                        }
                        catch (Exception e)
                        {
                            phase.Stop();
                            planActivity?.SetTag("litegraph.query.phase.duration_ms", phase.Elapsed.TotalMilliseconds);
                            LiteGraphTelemetry.SetActivityException(planActivity, e);
                            throw;
                        }
                    }

                    GraphQueryResult result;
                    using (Activity executeActivity = StartQueryPhaseActivity(LiteGraphTelemetry.QueryExecuteActivityName, "execute", tenantGuid, graphGuid, request))
                    {
                        Stopwatch phase = Stopwatch.StartNew();
                        try
                        {
                            SetQueryPlanActivityTags(executeActivity, plan);
                            result = await ExecutePlanned(tenantGuid, graphGuid, request, plan, linkedCts.Token).ConfigureAwait(false);
                            phase.Stop();
                            if (profile != null) profile.ExecuteTimeMs = phase.Elapsed.TotalMilliseconds;
                            SetQueryResultActivityTags(executeActivity, result);
                            CompletePhaseActivity(executeActivity, phase.Elapsed.TotalMilliseconds);
                        }
                        catch (Exception e)
                        {
                            phase.Stop();
                            executeActivity?.SetTag("litegraph.query.phase.duration_ms", phase.Elapsed.TotalMilliseconds);
                            LiteGraphTelemetry.SetActivityException(executeActivity, e);
                            throw;
                        }
                    }

                    stopwatch.Stop();
                    PopulateResultMetadata(result, plan, stopwatch.Elapsed.TotalMilliseconds);
                    if (profile != null)
                    {
                        if (timingCapture != null)
                        {
                            profile.RepositoryTimeMs = timingCapture.RepositoryTimeMs;
                            profile.RepositoryOperationCount = timingCapture.RepositoryOperationCount;
                            profile.VectorSearchTimeMs = timingCapture.VectorSearchTimeMs;
                            profile.VectorSearchCount = timingCapture.VectorSearchCount;
                            profile.TransactionTimeMs = timingCapture.TransactionTimeMs;
                        }

                        profile.TotalTimeMs = stopwatch.Elapsed.TotalMilliseconds;
                        result.ExecutionProfile = profile;
                    }

                    SetQueryPlanActivityTags(queryActivity, plan);
                    SetQueryResultActivityTags(queryActivity, result);
                    queryActivity?.SetTag("litegraph.query.duration_ms", stopwatch.Elapsed.TotalMilliseconds);
                    LiteGraphTelemetry.SetActivityOk(queryActivity);
                    return result;
                }
                catch (Exception e)
                {
                    stopwatch.Stop();
                    queryActivity?.SetTag("litegraph.query.duration_ms", stopwatch.Elapsed.TotalMilliseconds);
                    LiteGraphTelemetry.SetActivityException(queryActivity, e);
                    throw;
                }
            }
        }

        #endregion

        #region Private-Methods

        private async Task<GraphQueryResult> ExecutePlanned(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryPlan plan, CancellationToken token)
        {
            if (!plan.Mutates)
                return await _Executor.Execute(tenantGuid, graphGuid, request, plan, token).ConfigureAwait(false);

            TransactionExecutionOptions options = new TransactionExecutionOptions
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                IsolationLevel = TransactionIsolationLevelEnum.Default,
                OperationCount = 1,
                Source = "LiteGraphClient.Query.Execute"
            };

            IRepositorySession session = _Repo.CreateRepositorySession(options);
            if (session == null) throw new InvalidOperationException("Transaction repository session factory returned null.");

            if (!session.IsIsolated)
            {
                await session.DisposeAsync().ConfigureAwait(false);
                return await _Executor.Execute(tenantGuid, graphGuid, request, plan, token).ConfigureAwait(false);
            }

            await using (GraphTransactionContext context = new GraphTransactionContext(options, session))
            {
                Stopwatch transactionStopwatch = Stopwatch.StartNew();
                try
                {
                    await context.BeginAsync(token).ConfigureAwait(false);
                    QueryExecutionEngine transactionEngine = new QueryExecutionEngine(_Client, context.Repository, true);
                    GraphQueryResult result = await transactionEngine._Executor.Execute(tenantGuid, graphGuid, request, plan, token).ConfigureAwait(false);
                    await context.CommitAsync(token).ConfigureAwait(false);
                    return result;
                }
                catch
                {
                    if (context.Repository.GraphTransactionActive)
                        await context.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                    throw;
                }
                finally
                {
                    transactionStopwatch.Stop();
                    LiteGraphTelemetry.RecordTransactionTiming(transactionStopwatch.Elapsed.TotalMilliseconds);
                }
            }
        }

        internal async Task<GraphQueryResult> ExecuteMatchNode(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryAst ast, CancellationToken token)
        {
            if (HasAggregateReturn(ast))
                return await ExecuteAggregateMatchNode(tenantGuid, graphGuid, request, ast, token).ConfigureAwait(false);

            if (ast.ReturnVariables.Count != 1 || !String.Equals(ast.NodeVariable, ast.ReturnVariables[0], StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("MATCH node queries can only RETURN the matched node variable.");

            int limit = ResolveLimit(request, ast);
            GraphQueryResult result = new GraphQueryResult();
            if (!TryGetConjunctiveWherePredicates(ast, out List<GraphQueryPredicate> predicates))
            {
                IAsyncEnumerable<Node> expressionNodes = !String.IsNullOrEmpty(ast.NodeLabel)
                    ? _Repo.Node.ReadMany(tenantGuid, graphGuid, labels: LabelList(ast.NodeLabel), token: token)
                    : _Repo.Node.ReadAllInGraph(tenantGuid, graphGuid, token: token);

                await AddNodesWhereExpression(result, ast.NodeVariable, tenantGuid, graphGuid, expressionNodes, ast.WhereExpression, request.Parameters, limit, token).ConfigureAwait(false);
                return result;
            }

            if (predicates.Count > 1)
            {
                foreach (GraphQueryPredicate predicate in predicates)
                {
                    if (!String.Equals(predicate.Variable, ast.NodeVariable, StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException("WHERE variable must match the MATCH variable.");
                }

                IAsyncEnumerable<Node> predicateNodes = !String.IsNullOrEmpty(ast.NodeLabel)
                    ? _Repo.Node.ReadMany(tenantGuid, graphGuid, labels: LabelList(ast.NodeLabel), token: token)
                    : _Repo.Node.ReadAllInGraph(tenantGuid, graphGuid, token: token);

                await AddNodesWherePredicates(result, ast.NodeVariable, tenantGuid, graphGuid, predicateNodes, predicates, request.Parameters, limit, token).ConfigureAwait(false);
                return result;
            }

            if (predicates.Count == 1)
            {
                if (!String.Equals(ast.WhereVariable, ast.NodeVariable, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("WHERE variable must match the MATCH variable.");

                GraphQueryPredicate predicate = predicates[0];
                object value = ResolveValue(predicate.ValueExpression, request.Parameters);
                if (String.Equals(ast.WhereField, "guid", StringComparison.OrdinalIgnoreCase)
                    && IsEqualsOperator(ast.WhereOperator))
                {
                    Node node = await _Repo.Node.ReadByGuid(tenantGuid, ToGuid(value), token).ConfigureAwait(false);
                    if (node != null && node.GraphGUID == graphGuid && await NodeMatchesLabel(node, ast.NodeLabel, token).ConfigureAwait(false))
                        AddNodeRow(result, ast.NodeVariable, node);
                    return result;
                }

                if (String.Equals(ast.WhereField, "guid", StringComparison.OrdinalIgnoreCase)
                    && IsInOperator(ast.WhereOperator))
                {
                    await AddNodesWherePredicates(
                        result,
                        ast.NodeVariable,
                        tenantGuid,
                        graphGuid,
                        !String.IsNullOrEmpty(ast.NodeLabel)
                            ? _Repo.Node.ReadMany(tenantGuid, graphGuid, labels: LabelList(ast.NodeLabel), token: token)
                            : _Repo.Node.ReadAllInGraph(tenantGuid, graphGuid, token: token),
                        predicates,
                        request.Parameters,
                        limit,
                        token).ConfigureAwait(false);
                    return result;
                }

                if (String.Equals(ast.WhereField, "name", StringComparison.OrdinalIgnoreCase)
                    && IsEqualsOperator(ast.WhereOperator))
                {
                    await AddNodes(
                        result,
                        ast.NodeVariable,
                        _Repo.Node.ReadMany(tenantGuid, graphGuid, value?.ToString(), LabelList(ast.NodeLabel), null, null, token: token),
                        limit,
                        token).ConfigureAwait(false);
                    return result;
                }

                if (String.Equals(ast.WhereField, "name", StringComparison.OrdinalIgnoreCase)
                    && (IsStringOperator(ast.WhereOperator) || IsInOperator(ast.WhereOperator)))
                {
                    await AddNodesWherePredicates(
                        result,
                        ast.NodeVariable,
                        tenantGuid,
                        graphGuid,
                        !String.IsNullOrEmpty(ast.NodeLabel)
                            ? _Repo.Node.ReadMany(tenantGuid, graphGuid, labels: LabelList(ast.NodeLabel), token: token)
                            : _Repo.Node.ReadAllInGraph(tenantGuid, graphGuid, token: token),
                        predicates,
                        request.Parameters,
                        limit,
                        token).ConfigureAwait(false);
                    return result;
                }

                if (IsDataField(ast.WhereField))
                {
                    await AddNodesWhereData(
                        result,
                        ast.NodeVariable,
                        !String.IsNullOrEmpty(ast.NodeLabel)
                            ? _Repo.Node.ReadMany(tenantGuid, graphGuid, labels: LabelList(ast.NodeLabel), token: token)
                            : _Repo.Node.ReadAllInGraph(tenantGuid, graphGuid, token: token),
                        ast.WhereField,
                        ast.WhereOperator,
                        ResolveValue(ast.WhereValueExpression, request.Parameters),
                        limit,
                        token).ConfigureAwait(false);
                    return result;
                }

                if (IsTagField(ast.WhereField))
                {
                    await AddNodesWherePredicates(
                        result,
                        ast.NodeVariable,
                        tenantGuid,
                        graphGuid,
                        !String.IsNullOrEmpty(ast.NodeLabel)
                            ? _Repo.Node.ReadMany(tenantGuid, graphGuid, labels: LabelList(ast.NodeLabel), token: token)
                            : _Repo.Node.ReadAllInGraph(tenantGuid, graphGuid, token: token),
                        predicates,
                        request.Parameters,
                        limit,
                        token).ConfigureAwait(false);
                    return result;
                }

                throw new ArgumentException("Unsupported node WHERE clause. Supported fields: node.guid equality/list operators, node.name string/equality/list operators, node.data.<field> equality/numeric/string/list operators, node.tags.<key> string/equality/list operators.");
            }

            IAsyncEnumerable<Node> nodes = !String.IsNullOrEmpty(ast.NodeLabel)
                ? _Repo.Node.ReadMany(tenantGuid, graphGuid, labels: LabelList(ast.NodeLabel), token: token)
                : _Repo.Node.ReadAllInGraph(tenantGuid, graphGuid, token: token);

            await AddNodes(result, ast.NodeVariable, nodes, limit, token).ConfigureAwait(false);
            return result;
        }

        internal async Task<GraphQueryResult> ExecuteMatchEdge(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryAst ast, CancellationToken token)
        {
            if (HasAggregateReturn(ast))
                return await ExecuteAggregateMatchEdge(tenantGuid, graphGuid, request, ast, token).ConfigureAwait(false);

            int limit = ResolveLimit(request, ast);
            GraphQueryResult result = new GraphQueryResult();
            IAsyncEnumerable<Edge> edges;
            Func<Edge, bool> edgePredicate = null;
            Func<Edge, Task<bool>> edgeAsyncPredicate = null;
            bool conjunctiveWhere = TryGetConjunctiveWherePredicates(ast, out List<GraphQueryPredicate> predicates);

            if (!conjunctiveWhere)
            {
                edges = !String.IsNullOrEmpty(ast.EdgeLabel)
                    ? _Repo.Edge.ReadMany(tenantGuid, graphGuid, labels: LabelList(ast.EdgeLabel), token: token)
                    : _Repo.Edge.ReadAllInGraph(tenantGuid, graphGuid, token: token);
                edgeAsyncPredicate = edge => EdgeWhereExpressionMatches(tenantGuid, graphGuid, ast, request.Parameters, edge, ast.WhereExpression, token);
            }
            else if (predicates.Count > 1)
            {
                edges = !String.IsNullOrEmpty(ast.EdgeLabel)
                    ? _Repo.Edge.ReadMany(tenantGuid, graphGuid, labels: LabelList(ast.EdgeLabel), token: token)
                    : _Repo.Edge.ReadAllInGraph(tenantGuid, graphGuid, token: token);
                edgeAsyncPredicate = edge => EdgePredicatesMatch(tenantGuid, graphGuid, ast, request.Parameters, edge, predicates, token);
            }
            else if (predicates.Count == 1)
            {
                GraphQueryPredicate predicate = predicates[0];
                object value = ResolveValue(predicate.ValueExpression, request.Parameters);

                if (String.Equals(ast.WhereVariable, ast.FromVariable, StringComparison.OrdinalIgnoreCase)
                    && String.Equals(ast.WhereField, "guid", StringComparison.OrdinalIgnoreCase)
                    && IsEqualsOperator(ast.WhereOperator))
                {
                    edges = _Repo.Edge.ReadEdgesFromNode(tenantGuid, graphGuid, ToGuid(value), labels: LabelList(ast.EdgeLabel), token: token);
                }
                else if (String.Equals(ast.WhereVariable, ast.FromVariable, StringComparison.OrdinalIgnoreCase)
                    && String.Equals(ast.WhereField, "guid", StringComparison.OrdinalIgnoreCase)
                    && IsInOperator(ast.WhereOperator))
                {
                    edges = !String.IsNullOrEmpty(ast.EdgeLabel)
                        ? _Repo.Edge.ReadMany(tenantGuid, graphGuid, labels: LabelList(ast.EdgeLabel), token: token)
                        : _Repo.Edge.ReadAllInGraph(tenantGuid, graphGuid, token: token);
                    edgeAsyncPredicate = async edge =>
                    {
                        Node from = await _Repo.Node.ReadByGuid(tenantGuid, edge.From, token).ConfigureAwait(false);
                        return from != null && from.GraphGUID == graphGuid && await NodePredicateMatches(tenantGuid, graphGuid, from, predicate, value, token).ConfigureAwait(false);
                    };
                }
                else if (String.Equals(ast.WhereVariable, ast.ToVariable, StringComparison.OrdinalIgnoreCase)
                    && String.Equals(ast.WhereField, "guid", StringComparison.OrdinalIgnoreCase)
                    && IsEqualsOperator(ast.WhereOperator))
                {
                    edges = _Repo.Edge.ReadEdgesToNode(tenantGuid, graphGuid, ToGuid(value), labels: LabelList(ast.EdgeLabel), token: token);
                }
                else if (String.Equals(ast.WhereVariable, ast.ToVariable, StringComparison.OrdinalIgnoreCase)
                    && String.Equals(ast.WhereField, "guid", StringComparison.OrdinalIgnoreCase)
                    && IsInOperator(ast.WhereOperator))
                {
                    edges = !String.IsNullOrEmpty(ast.EdgeLabel)
                        ? _Repo.Edge.ReadMany(tenantGuid, graphGuid, labels: LabelList(ast.EdgeLabel), token: token)
                        : _Repo.Edge.ReadAllInGraph(tenantGuid, graphGuid, token: token);
                    edgeAsyncPredicate = async edge =>
                    {
                        Node to = await _Repo.Node.ReadByGuid(tenantGuid, edge.To, token).ConfigureAwait(false);
                        return to != null && to.GraphGUID == graphGuid && await NodePredicateMatches(tenantGuid, graphGuid, to, predicate, value, token).ConfigureAwait(false);
                    };
                }
                else if (String.Equals(ast.WhereVariable, ast.FromVariable, StringComparison.OrdinalIgnoreCase)
                    && (String.Equals(ast.WhereField, "name", StringComparison.OrdinalIgnoreCase) || IsDataField(ast.WhereField) || IsTagField(ast.WhereField)))
                {
                    edges = !String.IsNullOrEmpty(ast.EdgeLabel)
                        ? _Repo.Edge.ReadMany(tenantGuid, graphGuid, labels: LabelList(ast.EdgeLabel), token: token)
                        : _Repo.Edge.ReadAllInGraph(tenantGuid, graphGuid, token: token);
                    edgeAsyncPredicate = async edge =>
                    {
                        Node from = await _Repo.Node.ReadByGuid(tenantGuid, edge.From, token).ConfigureAwait(false);
                        return from != null && from.GraphGUID == graphGuid && await NodePredicateMatches(tenantGuid, graphGuid, from, predicate, value, token).ConfigureAwait(false);
                    };
                }
                else if (String.Equals(ast.WhereVariable, ast.ToVariable, StringComparison.OrdinalIgnoreCase)
                    && (String.Equals(ast.WhereField, "name", StringComparison.OrdinalIgnoreCase) || IsDataField(ast.WhereField) || IsTagField(ast.WhereField)))
                {
                    edges = !String.IsNullOrEmpty(ast.EdgeLabel)
                        ? _Repo.Edge.ReadMany(tenantGuid, graphGuid, labels: LabelList(ast.EdgeLabel), token: token)
                        : _Repo.Edge.ReadAllInGraph(tenantGuid, graphGuid, token: token);
                    edgeAsyncPredicate = async edge =>
                    {
                        Node to = await _Repo.Node.ReadByGuid(tenantGuid, edge.To, token).ConfigureAwait(false);
                        return to != null && to.GraphGUID == graphGuid && await NodePredicateMatches(tenantGuid, graphGuid, to, predicate, value, token).ConfigureAwait(false);
                    };
                }
                else if (String.Equals(ast.WhereVariable, ast.EdgeVariable, StringComparison.OrdinalIgnoreCase)
                    && String.Equals(ast.WhereField, "guid", StringComparison.OrdinalIgnoreCase)
                    && IsEqualsOperator(ast.WhereOperator))
                {
                    Edge edge = await _Repo.Edge.ReadByGuid(tenantGuid, ToGuid(value), token).ConfigureAwait(false);
                    if (edge != null && edge.GraphGUID == graphGuid && await EdgeMatchesLabel(edge, ast.EdgeLabel, token).ConfigureAwait(false))
                        await AddEdgeRow(result, ast.ReturnVariables, ast.FromVariable, ast.EdgeVariable, ast.ToVariable, edge, token).ConfigureAwait(false);
                    return result;
                }
                else if (String.Equals(ast.WhereVariable, ast.EdgeVariable, StringComparison.OrdinalIgnoreCase)
                    && String.Equals(ast.WhereField, "guid", StringComparison.OrdinalIgnoreCase)
                    && IsInOperator(ast.WhereOperator))
                {
                    edges = !String.IsNullOrEmpty(ast.EdgeLabel)
                        ? _Repo.Edge.ReadMany(tenantGuid, graphGuid, labels: LabelList(ast.EdgeLabel), token: token)
                        : _Repo.Edge.ReadAllInGraph(tenantGuid, graphGuid, token: token);
                    edgePredicate = edge => EdgePredicateMatches(edge, predicate, value);
                }
                else if (String.Equals(ast.WhereVariable, ast.EdgeVariable, StringComparison.OrdinalIgnoreCase)
                    && String.Equals(ast.WhereField, "name", StringComparison.OrdinalIgnoreCase)
                    && IsEqualsOperator(ast.WhereOperator))
                {
                    edges = _Repo.Edge.ReadMany(tenantGuid, graphGuid, value?.ToString(), LabelList(ast.EdgeLabel), null, null, token: token);
                }
                else if (String.Equals(ast.WhereVariable, ast.EdgeVariable, StringComparison.OrdinalIgnoreCase)
                    && String.Equals(ast.WhereField, "name", StringComparison.OrdinalIgnoreCase)
                    && (IsStringOperator(ast.WhereOperator) || IsInOperator(ast.WhereOperator)))
                {
                    edges = !String.IsNullOrEmpty(ast.EdgeLabel)
                        ? _Repo.Edge.ReadMany(tenantGuid, graphGuid, labels: LabelList(ast.EdgeLabel), token: token)
                        : _Repo.Edge.ReadAllInGraph(tenantGuid, graphGuid, token: token);
                    edgePredicate = edge => EdgePredicateMatches(edge, predicate, value);
                }
                else if (String.Equals(ast.WhereVariable, ast.EdgeVariable, StringComparison.OrdinalIgnoreCase)
                    && IsDataField(ast.WhereField))
                {
                    edges = !String.IsNullOrEmpty(ast.EdgeLabel)
                        ? _Repo.Edge.ReadMany(tenantGuid, graphGuid, labels: LabelList(ast.EdgeLabel), token: token)
                        : _Repo.Edge.ReadAllInGraph(tenantGuid, graphGuid, token: token);
                    edgePredicate = edge => DataPathMatches(edge.Data, ast.WhereField, ast.WhereOperator, value);
                }
                else if (String.Equals(ast.WhereVariable, ast.EdgeVariable, StringComparison.OrdinalIgnoreCase)
                    && IsTagField(ast.WhereField))
                {
                    edges = !String.IsNullOrEmpty(ast.EdgeLabel)
                        ? _Repo.Edge.ReadMany(tenantGuid, graphGuid, labels: LabelList(ast.EdgeLabel), token: token)
                        : _Repo.Edge.ReadAllInGraph(tenantGuid, graphGuid, token: token);
                    edgeAsyncPredicate = edge => EdgePredicateMatches(tenantGuid, graphGuid, edge, predicate, value, token);
                }
                else
                {
                    throw new ArgumentException("Unsupported edge WHERE clause. Supported fields: source/target guid equality/list operators, source/target name/data/tag filters, edge.guid equality/list operators, edge.name string/equality/list operators, edge.data.<field> equality/numeric/string/list operators, edge.tags.<key> string/equality/list operators.");
                }
            }
            else
            {
                edges = !String.IsNullOrEmpty(ast.EdgeLabel)
                    ? _Repo.Edge.ReadMany(tenantGuid, graphGuid, labels: LabelList(ast.EdgeLabel), token: token)
                    : _Repo.Edge.ReadAllInGraph(tenantGuid, graphGuid, token: token);
            }

            await foreach (Edge edge in edges.ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                if (edgePredicate != null && !edgePredicate(edge)) continue;
                if (edgeAsyncPredicate != null && !await edgeAsyncPredicate(edge).ConfigureAwait(false)) continue;
                await AddEdgeRow(result, ast.ReturnVariables, ast.FromVariable, ast.EdgeVariable, ast.ToVariable, edge, token).ConfigureAwait(false);
                if (result.RowCount >= limit) break;
            }

            return result;
        }

        internal async Task<GraphQueryResult> ExecuteMatchPath(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryAst ast, CancellationToken token)
        {
            if (ast.PathSegments == null || ast.PathSegments.Count < 1)
                throw new ArgumentException("Path queries require at least one directed edge segment.");

            if (ast.PathSegments.Count < 2 && !ast.PathSegments[0].IsVariableLength)
                throw new ArgumentException("Path queries require either a bounded variable-length segment or at least two directed edge segments.");

            int limit = ResolveLimit(request, ast);
            List<PathState> states = new List<PathState> { new PathState() };

            for (int i = 0; i < ast.PathSegments.Count; i++)
            {
                GraphQueryPathSegment segment = ast.PathSegments[i];
                List<PathState> nextStates = new List<PathState>();

                foreach (PathState state in states)
                {
                    token.ThrowIfCancellationRequested();
                    if (segment.IsVariableLength)
                    {
                        nextStates.AddRange(await ExpandVariablePathSegment(tenantGuid, graphGuid, ast, request, segment, state, token).ConfigureAwait(false));
                        continue;
                    }

                    Node expectedFrom = GetBoundNode(state, segment.FromVariable);
                    List<Edge> candidates = await ReadCandidatePathEdges(tenantGuid, graphGuid, ast, request, segment, expectedFrom, token).ConfigureAwait(false);

                    foreach (Edge edge in candidates)
                    {
                        token.ThrowIfCancellationRequested();
                        if (edge == null || edge.GraphGUID != graphGuid) continue;
                        if (!await EdgeMatchesLabel(edge, segment.EdgeLabel, token).ConfigureAwait(false)) continue;
                        if (expectedFrom != null && edge.From != expectedFrom.GUID) continue;

                        Node from = expectedFrom ?? await _Repo.Node.ReadByGuid(tenantGuid, edge.From, token).ConfigureAwait(false);
                        Node to = await _Repo.Node.ReadByGuid(tenantGuid, edge.To, token).ConfigureAwait(false);
                        if (from == null || to == null || from.GraphGUID != graphGuid || to.GraphGUID != graphGuid) continue;
                        if (!await NodeMatchesLabel(from, segment.FromLabel, token).ConfigureAwait(false)) continue;
                        if (!await NodeMatchesLabel(to, segment.ToLabel, token).ConfigureAwait(false)) continue;

                        PathState next = state.Clone();
                        if (!BindPathValue(next, segment.FromVariable, from)) continue;
                        if (!BindPathValue(next, segment.EdgeVariable, edge)) continue;
                        if (!BindPathValue(next, segment.ToVariable, to)) continue;
                        next.HopCount++;
                        nextStates.Add(next);
                    }
                }

                states = nextStates;
                if (states.Count < 1) break;
            }

            List<PathState> matchedStates = new List<PathState>();
            foreach (PathState state in states)
            {
                token.ThrowIfCancellationRequested();
                if (!await PathWhereMatches(tenantGuid, graphGuid, ast, request.Parameters, state.Values, token).ConfigureAwait(false)) continue;
                matchedStates.Add(state);
            }

            if (ast.IsShortestPath)
                matchedStates = FilterShortestPathStates(matchedStates);

            if (HasAggregateReturn(ast))
            {
                List<Dictionary<string, object>> aggregateRows = new List<Dictionary<string, object>>();
                foreach (PathState state in matchedStates)
                {
                    token.ThrowIfCancellationRequested();
                    aggregateRows.Add(new Dictionary<string, object>(state.Values, StringComparer.OrdinalIgnoreCase));
                    if (aggregateRows.Count >= limit) break;
                }

                return await BuildAggregateResult(tenantGuid, graphGuid, ast, aggregateRows, token).ConfigureAwait(false);
            }

            GraphQueryResult result = new GraphQueryResult();
            foreach (PathState state in matchedStates)
            {
                token.ThrowIfCancellationRequested();
                AddPathRow(result, ast.ReturnVariables, state.Values);
                if (result.RowCount >= limit) break;
            }

            return result;
        }

        internal async Task<GraphQueryResult> ExecuteCreateNode(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryAst ast, CancellationToken token)
        {
            string variable = RequireReturnVariable(ast, ast.NodeVariable, "created node");
            Node node = new Node
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Name = GetOptionalString(ast.Properties, "name", request.Parameters),
                Data = GetOptionalValue(ast.Properties, "data", request.Parameters),
                Labels = LabelList(ast.NodeLabel)
            };

            GraphQueryResult result = await ExecuteMutation(async () =>
            {
                Node created = await _Repo.Node.Create(node, token).ConfigureAwait(false);
                GraphQueryResult r = MutatedResult();
                AddNodeRow(r, variable, created);
                return r;
            }, tenantGuid, graphGuid, token).ConfigureAwait(false);

            return result;
        }

        internal async Task<GraphQueryResult> ExecuteCreateEdge(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryAst ast, CancellationToken token)
        {
            string variable = RequireReturnVariable(ast, ast.EdgeVariable, "created edge");
            Edge edge = new Edge
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                From = GetRequiredGuid(ast.Properties, request.Parameters, "from", "fromGuid"),
                To = GetRequiredGuid(ast.Properties, request.Parameters, "to", "toGuid"),
                Name = GetOptionalString(ast.Properties, "name", request.Parameters),
                Data = GetOptionalValue(ast.Properties, "data", request.Parameters),
                Labels = LabelList(ast.EdgeLabel)
            };

            object cost = GetOptionalValue(ast.Properties, "cost", request.Parameters);
            if (cost != null) edge.Cost = Convert.ToInt32(cost, CultureInfo.InvariantCulture);

            return await ExecuteMutation(async () =>
            {
                Edge created = await _Repo.Edge.Create(edge, token).ConfigureAwait(false);
                GraphQueryResult r = MutatedResult();
                AddObjectRow(r, variable, created);
                r.Edges.Add(created);
                return r;
            }, tenantGuid, graphGuid, token).ConfigureAwait(false);
        }

        internal async Task<GraphQueryResult> ExecuteCreateLabel(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryAst ast, CancellationToken token)
        {
            string variable = RequireReturnVariable(ast, ast.ObjectVariable, "created label");
            LabelMetadata label = new LabelMetadata
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                NodeGUID = GetOptionalGuid(ast.Properties, request.Parameters, "nodeGuid", "node"),
                EdgeGUID = GetOptionalGuid(ast.Properties, request.Parameters, "edgeGuid", "edge"),
                Label = GetRequiredString(ast.Properties, "label", request.Parameters)
            };

            return await ExecuteMutation(async () =>
            {
                LabelMetadata created = await _Repo.Label.Create(label, token).ConfigureAwait(false);
                GraphQueryResult r = MutatedResult();
                AddObjectRow(r, variable, created);
                r.Labels.Add(created);
                return r;
            }, tenantGuid, graphGuid, token).ConfigureAwait(false);
        }

        internal async Task<GraphQueryResult> ExecuteCreateTag(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryAst ast, CancellationToken token)
        {
            string variable = RequireReturnVariable(ast, ast.ObjectVariable, "created tag");
            TagMetadata tag = new TagMetadata
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                NodeGUID = GetOptionalGuid(ast.Properties, request.Parameters, "nodeGuid", "node"),
                EdgeGUID = GetOptionalGuid(ast.Properties, request.Parameters, "edgeGuid", "edge"),
                Key = GetRequiredString(ast.Properties, "key", request.Parameters),
                Value = GetOptionalString(ast.Properties, "value", request.Parameters) ?? String.Empty
            };

            return await ExecuteMutation(async () =>
            {
                TagMetadata created = await _Repo.Tag.Create(tag, token).ConfigureAwait(false);
                GraphQueryResult r = MutatedResult();
                AddObjectRow(r, variable, created);
                r.Tags.Add(created);
                return r;
            }, tenantGuid, graphGuid, token).ConfigureAwait(false);
        }

        internal async Task<GraphQueryResult> ExecuteCreateVector(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryAst ast, CancellationToken token)
        {
            string variable = RequireReturnVariable(ast, ast.ObjectVariable, "created vector");
            List<float> vectors = ToFloatList(GetRequiredValue(ast.Properties, request.Parameters, "embeddings", "vectors"));
            VectorMetadata vector = new VectorMetadata
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                NodeGUID = GetOptionalGuid(ast.Properties, request.Parameters, "nodeGuid", "node"),
                EdgeGUID = GetOptionalGuid(ast.Properties, request.Parameters, "edgeGuid", "edge"),
                Model = GetOptionalString(ast.Properties, "model", request.Parameters),
                Content = GetOptionalString(ast.Properties, "content", request.Parameters) ?? String.Empty,
                Dimensionality = vectors.Count,
                Vectors = vectors
            };

            return await ExecuteMutation(async () =>
            {
                VectorMetadata created = await _Repo.Vector.Create(vector, token).ConfigureAwait(false);
                GraphQueryResult r = MutatedResult();
                AddObjectRow(r, variable, created);
                r.Vectors.Add(created);
                return r;
            }, tenantGuid, graphGuid, token).ConfigureAwait(false);
        }

        internal async Task<GraphQueryResult> ExecuteUpdateNode(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryAst ast, CancellationToken token)
        {
            string variable = RequireMutationReturnVariable(ast, ast.NodeVariable, "updated node");
            Guid nodeGuid = RequireGuidWhere(ast, ast.NodeVariable, "Node update", request.Parameters);

            return await ExecuteMutation(async () =>
            {
                Node node = await _Repo.Node.ReadByGuid(tenantGuid, nodeGuid, token).ConfigureAwait(false);
                if (node == null || node.GraphGUID != graphGuid || !await NodeMatchesLabel(node, ast.NodeLabel, token).ConfigureAwait(false))
                    return MutatedResult();

                await HydrateNodeSubordinates(node, token).ConfigureAwait(false);
                ApplyNodeSet(node, ast.SetProperties, request.Parameters);
                Node updated = await _Repo.Node.Update(node, token).ConfigureAwait(false);

                GraphQueryResult r = MutatedResult();
                AddNodeRow(r, variable, updated);
                return r;
            }, tenantGuid, graphGuid, token).ConfigureAwait(false);
        }

        internal async Task<GraphQueryResult> ExecuteUpdateEdge(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryAst ast, CancellationToken token)
        {
            string variable = RequireMutationReturnVariable(ast, ast.EdgeVariable, "updated edge");
            Guid edgeGuid = RequireGuidWhere(ast, ast.EdgeVariable, "Edge update", request.Parameters);

            return await ExecuteMutation(async () =>
            {
                Edge edge = await _Repo.Edge.ReadByGuid(tenantGuid, edgeGuid, token).ConfigureAwait(false);
                if (edge == null || edge.GraphGUID != graphGuid || !await EdgeMatchesLabel(edge, ast.EdgeLabel, token).ConfigureAwait(false))
                    return MutatedResult();

                await HydrateEdgeSubordinates(edge, token).ConfigureAwait(false);
                ApplyEdgeSet(edge, ast.SetProperties, request.Parameters);
                Edge updated = await _Repo.Edge.Update(edge, token).ConfigureAwait(false);

                GraphQueryResult r = MutatedResult();
                AddObjectRow(r, variable, updated);
                r.Edges.Add(updated);
                return r;
            }, tenantGuid, graphGuid, token).ConfigureAwait(false);
        }

        internal async Task<GraphQueryResult> ExecuteDeleteNode(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryAst ast, CancellationToken token)
        {
            string variable = RequireMutationReturnVariable(ast, ast.NodeVariable, "deleted node");
            Guid nodeGuid = RequireGuidWhere(ast, ast.NodeVariable, "Node delete", request.Parameters);

            return await ExecuteMutation(async () =>
            {
                Node node = await _Repo.Node.ReadByGuid(tenantGuid, nodeGuid, token).ConfigureAwait(false);
                if (node == null || node.GraphGUID != graphGuid || !await NodeMatchesLabel(node, ast.NodeLabel, token).ConfigureAwait(false))
                    return MutatedResult();

                await _Repo.Node.DeleteByGuid(tenantGuid, graphGuid, node.GUID, token).ConfigureAwait(false);

                GraphQueryResult r = MutatedResult();
                AddNodeRow(r, variable, node);
                return r;
            }, tenantGuid, graphGuid, token).ConfigureAwait(false);
        }

        internal async Task<GraphQueryResult> ExecuteDeleteEdge(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryAst ast, CancellationToken token)
        {
            string variable = RequireMutationReturnVariable(ast, ast.EdgeVariable, "deleted edge");
            Guid edgeGuid = RequireGuidWhere(ast, ast.EdgeVariable, "Edge delete", request.Parameters);

            return await ExecuteMutation(async () =>
            {
                Edge edge = await _Repo.Edge.ReadByGuid(tenantGuid, edgeGuid, token).ConfigureAwait(false);
                if (edge == null || edge.GraphGUID != graphGuid || !await EdgeMatchesLabel(edge, ast.EdgeLabel, token).ConfigureAwait(false))
                    return MutatedResult();

                await _Repo.Edge.DeleteByGuid(tenantGuid, graphGuid, edge.GUID, token).ConfigureAwait(false);

                GraphQueryResult r = MutatedResult();
                AddObjectRow(r, variable, edge);
                r.Edges.Add(edge);
                return r;
            }, tenantGuid, graphGuid, token).ConfigureAwait(false);
        }

        internal async Task<GraphQueryResult> ExecuteUpdateLabel(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryAst ast, CancellationToken token)
        {
            string variable = RequireMutationReturnVariable(ast, ast.ObjectVariable, "updated label");
            Guid labelGuid = RequireGuidWhere(ast, ast.ObjectVariable, "Label update", request.Parameters);

            return await ExecuteMutation(async () =>
            {
                LabelMetadata label = await _Repo.Label.ReadByGuid(tenantGuid, labelGuid, token).ConfigureAwait(false);
                if (label == null || label.GraphGUID != graphGuid) return MutatedResult();

                ApplyLabelSet(label, ast.SetProperties, request.Parameters);
                LabelMetadata updated = await _Repo.Label.Update(label, token).ConfigureAwait(false);

                GraphQueryResult r = MutatedResult();
                AddObjectRow(r, variable, updated);
                r.Labels.Add(updated);
                return r;
            }, tenantGuid, graphGuid, token).ConfigureAwait(false);
        }

        internal async Task<GraphQueryResult> ExecuteUpdateTag(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryAst ast, CancellationToken token)
        {
            string variable = RequireMutationReturnVariable(ast, ast.ObjectVariable, "updated tag");
            Guid tagGuid = RequireGuidWhere(ast, ast.ObjectVariable, "Tag update", request.Parameters);

            return await ExecuteMutation(async () =>
            {
                TagMetadata tag = await _Repo.Tag.ReadByGuid(tenantGuid, tagGuid, token).ConfigureAwait(false);
                if (tag == null || tag.GraphGUID != graphGuid) return MutatedResult();

                ApplyTagSet(tag, ast.SetProperties, request.Parameters);
                TagMetadata updated = await _Repo.Tag.Update(tag, token).ConfigureAwait(false);

                GraphQueryResult r = MutatedResult();
                AddObjectRow(r, variable, updated);
                r.Tags.Add(updated);
                return r;
            }, tenantGuid, graphGuid, token).ConfigureAwait(false);
        }

        internal async Task<GraphQueryResult> ExecuteUpdateVector(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryAst ast, CancellationToken token)
        {
            string variable = RequireMutationReturnVariable(ast, ast.ObjectVariable, "updated vector");
            Guid vectorGuid = RequireGuidWhere(ast, ast.ObjectVariable, "Vector update", request.Parameters);

            return await ExecuteMutation(async () =>
            {
                VectorMetadata vector = await _Repo.Vector.ReadByGuid(tenantGuid, vectorGuid, token).ConfigureAwait(false);
                if (vector == null || vector.GraphGUID != graphGuid) return MutatedResult();

                ApplyVectorSet(vector, ast.SetProperties, request.Parameters);
                VectorMetadata updated = await _Repo.Vector.Update(vector, token).ConfigureAwait(false);

                GraphQueryResult r = MutatedResult();
                AddObjectRow(r, variable, updated);
                r.Vectors.Add(updated);
                return r;
            }, tenantGuid, graphGuid, token).ConfigureAwait(false);
        }

        internal async Task<GraphQueryResult> ExecuteDeleteLabel(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryAst ast, CancellationToken token)
        {
            string variable = RequireMutationReturnVariable(ast, ast.ObjectVariable, "deleted label");
            Guid labelGuid = RequireGuidWhere(ast, ast.ObjectVariable, "Label delete", request.Parameters);

            return await ExecuteMutation(async () =>
            {
                LabelMetadata label = await _Repo.Label.ReadByGuid(tenantGuid, labelGuid, token).ConfigureAwait(false);
                if (label == null || label.GraphGUID != graphGuid) return MutatedResult();

                await _Repo.Label.DeleteByGuid(tenantGuid, label.GUID, token).ConfigureAwait(false);

                GraphQueryResult r = MutatedResult();
                AddObjectRow(r, variable, label);
                r.Labels.Add(label);
                return r;
            }, tenantGuid, graphGuid, token).ConfigureAwait(false);
        }

        internal async Task<GraphQueryResult> ExecuteDeleteTag(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryAst ast, CancellationToken token)
        {
            string variable = RequireMutationReturnVariable(ast, ast.ObjectVariable, "deleted tag");
            Guid tagGuid = RequireGuidWhere(ast, ast.ObjectVariable, "Tag delete", request.Parameters);

            return await ExecuteMutation(async () =>
            {
                TagMetadata tag = await _Repo.Tag.ReadByGuid(tenantGuid, tagGuid, token).ConfigureAwait(false);
                if (tag == null || tag.GraphGUID != graphGuid) return MutatedResult();

                await _Repo.Tag.DeleteByGuid(tenantGuid, tag.GUID, token).ConfigureAwait(false);

                GraphQueryResult r = MutatedResult();
                AddObjectRow(r, variable, tag);
                r.Tags.Add(tag);
                return r;
            }, tenantGuid, graphGuid, token).ConfigureAwait(false);
        }

        internal async Task<GraphQueryResult> ExecuteDeleteVector(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryAst ast, CancellationToken token)
        {
            string variable = RequireMutationReturnVariable(ast, ast.ObjectVariable, "deleted vector");
            Guid vectorGuid = RequireGuidWhere(ast, ast.ObjectVariable, "Vector delete", request.Parameters);

            return await ExecuteMutation(async () =>
            {
                VectorMetadata vector = await _Repo.Vector.ReadByGuid(tenantGuid, vectorGuid, token).ConfigureAwait(false);
                if (vector == null || vector.GraphGUID != graphGuid) return MutatedResult();

                await _Repo.Vector.DeleteByGuid(tenantGuid, vector.GUID, token).ConfigureAwait(false);

                GraphQueryResult r = MutatedResult();
                AddObjectRow(r, variable, vector);
                r.Vectors.Add(vector);
                return r;
            }, tenantGuid, graphGuid, token).ConfigureAwait(false);
        }

        internal async Task<GraphQueryResult> ExecuteVectorSearch(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryAst ast, CancellationToken token)
        {
            if (HasAggregateReturn(ast))
                throw new NotSupportedException("Aggregate RETURN expressions are not supported for vector search queries in this release.");

            List<float> embeddings = ToFloatList(ResolveValue(ast.ProcedureArgumentExpression, request.Parameters));
            int limit = ResolveLimit(request, ast);
            bool graphDomain = ast.VectorDomain == VectorSearchDomainEnum.Graph;
            GraphQueryResult result = new GraphQueryResult();

            VectorSearchRequest search = new VectorSearchRequest
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Domain = ast.VectorDomain ?? VectorSearchDomainEnum.Node,
                Embeddings = embeddings,
                TopK = graphDomain ? null : limit
            };

            await foreach (VectorSearchResult current in _Client.Vector.Search(search, token).WithCancellation(token).ConfigureAwait(false))
            {
                if (graphDomain && (current.Graph == null || current.Graph.GUID != graphGuid)) continue;

                Dictionary<string, object> row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                foreach (string variable in ast.ReturnVariables)
                {
                    if (variable.Equals("node", StringComparison.OrdinalIgnoreCase) || variable.Equals("n", StringComparison.OrdinalIgnoreCase))
                    {
                        row[variable] = current.Node;
                        if (current.Node != null) result.Nodes.Add(current.Node);
                    }
                    else if (variable.Equals("edge", StringComparison.OrdinalIgnoreCase) || variable.Equals("e", StringComparison.OrdinalIgnoreCase))
                    {
                        row[variable] = current.Edge;
                        if (current.Edge != null) result.Edges.Add(current.Edge);
                    }
                    else if (variable.Equals("graph", StringComparison.OrdinalIgnoreCase) || variable.Equals("g", StringComparison.OrdinalIgnoreCase))
                    {
                        row[variable] = current.Graph;
                    }
                    else if (variable.Equals("score", StringComparison.OrdinalIgnoreCase))
                    {
                        row[variable] = current.Score;
                    }
                    else if (variable.Equals("distance", StringComparison.OrdinalIgnoreCase))
                    {
                        row[variable] = current.Distance;
                    }
                    else if (variable.Equals("innerProduct", StringComparison.OrdinalIgnoreCase))
                    {
                        row[variable] = current.InnerProduct;
                    }
                    else if (variable.Equals("result", StringComparison.OrdinalIgnoreCase))
                    {
                        row[variable] = current;
                    }
                    else
                    {
                        throw new ArgumentException("Unsupported vector search RETURN variable '" + variable + "'.");
                    }
                }

                result.VectorSearchResults.Add(current);
                result.Rows.Add(row);
                if (result.RowCount >= limit) break;
            }

            return result;
        }

        private async Task<GraphQueryResult> ExecuteAggregateMatchNode(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryAst ast, CancellationToken token)
        {
            int limit = ResolveLimit(request, ast);
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
            IAsyncEnumerable<Node> nodes = !String.IsNullOrEmpty(ast.NodeLabel)
                ? _Repo.Node.ReadMany(tenantGuid, graphGuid, labels: LabelList(ast.NodeLabel), token: token)
                : _Repo.Node.ReadAllInGraph(tenantGuid, graphGuid, token: token);

            await foreach (Node node in nodes.ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                if (!await NodeWhereMatches(tenantGuid, graphGuid, ast, node, request.Parameters, token).ConfigureAwait(false)) continue;
                rows.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    { ast.NodeVariable, node }
                });
                if (rows.Count >= limit) break;
            }

            return await BuildAggregateResult(tenantGuid, graphGuid, ast, rows, token).ConfigureAwait(false);
        }

        private async Task<GraphQueryResult> ExecuteAggregateMatchEdge(Guid tenantGuid, Guid graphGuid, GraphQueryRequest request, GraphQueryAst ast, CancellationToken token)
        {
            int limit = ResolveLimit(request, ast);
            List<Dictionary<string, object>> rows = new List<Dictionary<string, object>>();
            IAsyncEnumerable<Edge> edges = !String.IsNullOrEmpty(ast.EdgeLabel)
                ? _Repo.Edge.ReadMany(tenantGuid, graphGuid, labels: LabelList(ast.EdgeLabel), token: token)
                : _Repo.Edge.ReadAllInGraph(tenantGuid, graphGuid, token: token);

            await foreach (Edge edge in edges.ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                if (!await EdgeWhereExpressionMatches(tenantGuid, graphGuid, ast, request.Parameters, edge, ast.WhereExpression, token).ConfigureAwait(false)) continue;

                Dictionary<string, object> row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                if (!String.IsNullOrEmpty(ast.EdgeVariable)) row[ast.EdgeVariable] = edge;
                if (!String.IsNullOrEmpty(ast.FromVariable))
                {
                    Node from = await _Repo.Node.ReadByGuid(tenantGuid, edge.From, token).ConfigureAwait(false);
                    row[ast.FromVariable] = from;
                }

                if (!String.IsNullOrEmpty(ast.ToVariable))
                {
                    Node to = await _Repo.Node.ReadByGuid(tenantGuid, edge.To, token).ConfigureAwait(false);
                    row[ast.ToVariable] = to;
                }

                rows.Add(row);
                if (rows.Count >= limit) break;
            }

            return await BuildAggregateResult(tenantGuid, graphGuid, ast, rows, token).ConfigureAwait(false);
        }

        private async Task<GraphQueryResult> BuildAggregateResult(Guid tenantGuid, Guid graphGuid, GraphQueryAst ast, List<Dictionary<string, object>> rows, CancellationToken token)
        {
            GraphQueryResult result = new GraphQueryResult();
            Dictionary<string, object> aggregateRow = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (GraphQueryReturnItem item in ast.ReturnItems)
            {
                if (item.Kind != GraphQueryReturnItemKindEnum.Aggregate) continue;
                aggregateRow[item.Alias] = await CalculateAggregate(tenantGuid, graphGuid, rows, item, token).ConfigureAwait(false);
            }

            result.Rows.Add(aggregateRow);
            return result;
        }

        private async Task<object> CalculateAggregate(Guid tenantGuid, Guid graphGuid, List<Dictionary<string, object>> rows, GraphQueryReturnItem item, CancellationToken token)
        {
            GraphQueryAggregateFunctionEnum function = item.AggregateFunction.GetValueOrDefault();
            if (function == GraphQueryAggregateFunctionEnum.Count)
            {
                if (item.AggregateWildcard) return rows.Count;

                int count = 0;
                foreach (Dictionary<string, object> row in rows)
                {
                    token.ThrowIfCancellationRequested();
                    object value = await ResolveAggregateValue(tenantGuid, graphGuid, row, item, token).ConfigureAwait(false);
                    if (value != null) count++;
                }

                return count;
            }

            List<object> values = new List<object>();
            foreach (Dictionary<string, object> row in rows)
            {
                token.ThrowIfCancellationRequested();
                object value = await ResolveAggregateValue(tenantGuid, graphGuid, row, item, token).ConfigureAwait(false);
                if (value != null) values.Add(NormalizeJsonValue(value));
            }

            if (values.Count < 1) return null;

            if (function == GraphQueryAggregateFunctionEnum.Sum || function == GraphQueryAggregateFunctionEnum.Avg)
            {
                decimal sum = 0;
                foreach (object value in values)
                {
                    if (!IsNumeric(value))
                        throw new ArgumentException(function.ToString().ToUpperInvariant() + " requires numeric aggregate values.");
                    sum += Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                }

                if (function == GraphQueryAggregateFunctionEnum.Sum) return sum;
                return sum / values.Count;
            }

            object selected = values[0];
            for (int i = 1; i < values.Count; i++)
            {
                int comparison = OrderValueComparer.Instance.Compare(values[i], selected);
                if (function == GraphQueryAggregateFunctionEnum.Min && comparison < 0) selected = values[i];
                else if (function == GraphQueryAggregateFunctionEnum.Max && comparison > 0) selected = values[i];
            }

            return selected;
        }

        private async Task<object> ResolveAggregateValue(Guid tenantGuid, Guid graphGuid, Dictionary<string, object> row, GraphQueryReturnItem item, CancellationToken token)
        {
            if (item.AggregateWildcard) return row;
            if (String.IsNullOrEmpty(item.Variable) || !row.TryGetValue(item.Variable, out object value)) return null;
            if (String.IsNullOrEmpty(item.Field)) return value;

            if (value is Node node) return await ResolveNodeAggregateValue(tenantGuid, graphGuid, node, item.Field, token).ConfigureAwait(false);
            if (value is Edge edge) return await ResolveEdgeAggregateValue(tenantGuid, graphGuid, edge, item.Field, token).ConfigureAwait(false);
            throw new ArgumentException("Aggregate variable '" + item.Variable + "' does not reference an aggregate-capable value.");
        }

        private async Task<object> ResolveNodeAggregateValue(Guid tenantGuid, Guid graphGuid, Node node, string field, CancellationToken token)
        {
            if (String.Equals(field, "guid", StringComparison.OrdinalIgnoreCase)) return node.GUID;
            if (String.Equals(field, "name", StringComparison.OrdinalIgnoreCase)) return node.Name;
            if (IsDataField(field)) return ResolveDataPath(node.Data, field.Substring("data.".Length));
            if (IsTagField(field)) return await ReadFirstTagValue(_Repo.Tag.ReadManyNode(tenantGuid, graphGuid, node.GUID, token: token), field, token).ConfigureAwait(false);
            throw new ArgumentException("Unsupported node aggregate field '" + field + "'. Supported fields: guid, name, data.<field>, tags.<key>.");
        }

        private async Task<object> ResolveEdgeAggregateValue(Guid tenantGuid, Guid graphGuid, Edge edge, string field, CancellationToken token)
        {
            if (String.Equals(field, "guid", StringComparison.OrdinalIgnoreCase)) return edge.GUID;
            if (String.Equals(field, "name", StringComparison.OrdinalIgnoreCase)) return edge.Name;
            if (String.Equals(field, "cost", StringComparison.OrdinalIgnoreCase)) return edge.Cost;
            if (IsDataField(field)) return ResolveDataPath(edge.Data, field.Substring("data.".Length));
            if (IsTagField(field)) return await ReadFirstTagValue(_Repo.Tag.ReadManyEdge(tenantGuid, graphGuid, edge.GUID, token: token), field, token).ConfigureAwait(false);
            throw new ArgumentException("Unsupported edge aggregate field '" + field + "'. Supported fields: guid, name, cost, data.<field>, tags.<key>.");
        }

        private static async Task<object> ReadFirstTagValue(IAsyncEnumerable<TagMetadata> tags, string field, CancellationToken token)
        {
            string key = GetTagKey(field);
            await foreach (TagMetadata tag in tags.ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                if (tag != null && String.Equals(tag.Key, key, StringComparison.OrdinalIgnoreCase)) return tag.Value;
            }

            return null;
        }

        private async Task<GraphQueryResult> ExecuteMutation(Func<Task<GraphQueryResult>> action, Guid tenantGuid, Guid graphGuid, CancellationToken token)
        {
            if (_Repo.GraphTransactionActive)
            {
                if (!_AllowActiveTransaction)
                    throw new InvalidOperationException("A graph transaction is already active on this repository.");

                return await action().ConfigureAwait(false);
            }

            Stopwatch transactionStopwatch = Stopwatch.StartNew();
            bool startedTransaction = false;
            await _Repo.BeginGraphTransaction(tenantGuid, graphGuid, token).ConfigureAwait(false);
            startedTransaction = true;

            try
            {
                GraphQueryResult result = await action().ConfigureAwait(false);
                if (startedTransaction) await _Repo.CommitGraphTransaction(token).ConfigureAwait(false);
                return result;
            }
            catch
            {
                if (startedTransaction && _Repo.GraphTransactionActive)
                    await _Repo.RollbackGraphTransaction(CancellationToken.None).ConfigureAwait(false);
                throw;
            }
            finally
            {
                transactionStopwatch.Stop();
                LiteGraphTelemetry.RecordTransactionTiming(transactionStopwatch.Elapsed.TotalMilliseconds);
            }
        }

        private static async Task AddNodes(GraphQueryResult result, string variable, IAsyncEnumerable<Node> nodes, int limit, CancellationToken token)
        {
            await foreach (Node node in nodes.ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                AddNodeRow(result, variable, node);
                if (result.RowCount >= limit) break;
            }
        }

        private static void PopulateResultMetadata(GraphQueryResult result, GraphQueryPlan plan, double executionTimeMs)
        {
            if (result == null) return;

            result.ExecutionTimeMs = executionTimeMs;
            result.Plan = GraphQueryPlanSummary.FromPlan(plan);

            if (result.Warnings == null) result.Warnings = new List<string>();
            if (plan?.Warnings != null)
            {
                foreach (string warning in plan.Warnings)
                {
                    if (!String.IsNullOrEmpty(warning)) result.Warnings.Add(warning);
                }
            }
        }

        private static Activity StartQueryPhaseActivity(string name, string phase, Guid tenantGuid, Guid graphGuid, GraphQueryRequest request)
        {
            Activity activity = LiteGraphTelemetry.ActivitySource.StartActivity(name, ActivityKind.Internal);
            SetQueryRequestActivityTags(activity, tenantGuid, graphGuid, request);
            activity?.SetTag("litegraph.query.phase", phase);
            return activity;
        }

        private static void SetQueryRequestActivityTags(Activity activity, Guid tenantGuid, Guid graphGuid, GraphQueryRequest request)
        {
            if (activity == null) return;

            activity.SetTag("db.system", "litegraph");
            activity.SetTag("litegraph.tenant_guid", tenantGuid.ToString("D"));
            activity.SetTag("litegraph.graph_guid", graphGuid.ToString("D"));
            activity.SetTag("litegraph.query.length", request?.Query?.Length ?? 0);
            activity.SetTag("litegraph.query.max_results", request?.MaxResults ?? 0);
            activity.SetTag("litegraph.query.timeout_seconds", request?.TimeoutSeconds ?? 0);
            activity.SetTag("litegraph.query.include_profile", request?.IncludeProfile ?? false);
            activity.SetTag("litegraph.query.parameter_count", request?.Parameters?.Count ?? 0);
        }

        private static void SetQueryAstActivityTags(Activity activity, GraphQueryAst ast)
        {
            if (activity == null || ast == null) return;

            activity.SetTag("litegraph.query.kind", ast.Kind.ToString());
            activity.SetTag("litegraph.query.return_count", ast.ReturnItems?.Count ?? ast.ReturnVariables?.Count ?? 0);
            activity.SetTag("litegraph.query.has_aggregate", HasAggregateReturn(ast));
            if (ast.VectorDomain != null) activity.SetTag("litegraph.vector.domain", ast.VectorDomain.Value.ToString());
        }

        private static void SetQueryPlanActivityTags(Activity activity, GraphQueryPlan plan)
        {
            if (activity == null || plan == null) return;

            activity.SetTag("litegraph.query.kind", plan.Kind.ToString());
            activity.SetTag("litegraph.query.mutates", plan.Mutates);
            activity.SetTag("litegraph.query.uses_vector_search", plan.UsesVectorSearch);
            activity.SetTag("litegraph.query.has_order", plan.HasOrder);
            activity.SetTag("litegraph.query.has_limit", plan.HasLimit);
            activity.SetTag("litegraph.query.estimated_cost", plan.EstimatedCost);
            activity.SetTag("litegraph.query.seed_kind", plan.SeedKind.ToString());
            if (!String.IsNullOrEmpty(plan.SeedVariable)) activity.SetTag("litegraph.query.seed_variable", plan.SeedVariable);
            if (!String.IsNullOrEmpty(plan.SeedField)) activity.SetTag("litegraph.query.seed_field", plan.SeedField);
            if (plan.Ast?.VectorDomain != null) activity.SetTag("litegraph.vector.domain", plan.Ast.VectorDomain.Value.ToString());
        }

        private static void SetQueryResultActivityTags(Activity activity, GraphQueryResult result)
        {
            if (activity == null || result == null) return;

            activity.SetTag("litegraph.query.mutated", result.Mutated);
            activity.SetTag("litegraph.query.rows", result.RowCount);
            activity.SetTag("litegraph.query.nodes", result.Nodes?.Count ?? 0);
            activity.SetTag("litegraph.query.edges", result.Edges?.Count ?? 0);
            activity.SetTag("litegraph.query.labels", result.Labels?.Count ?? 0);
            activity.SetTag("litegraph.query.tags", result.Tags?.Count ?? 0);
            activity.SetTag("litegraph.query.vectors", result.Vectors?.Count ?? 0);
            activity.SetTag("litegraph.vector.results", result.VectorSearchResults?.Count ?? 0);
        }

        private static void CompletePhaseActivity(Activity activity, double durationMs)
        {
            if (activity == null) return;

            activity.SetTag("litegraph.query.phase.duration_ms", durationMs);
            LiteGraphTelemetry.SetActivityOk(activity);
        }

        private static async Task AddNodesWhereData(GraphQueryResult result, string variable, IAsyncEnumerable<Node> nodes, string field, string whereOperator, object expected, int limit, CancellationToken token)
        {
            await foreach (Node node in nodes.ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                if (!DataPathMatches(node.Data, field, whereOperator, expected)) continue;
                AddNodeRow(result, variable, node);
                if (result.RowCount >= limit) break;
            }
        }

        private async Task AddNodesWherePredicates(
            GraphQueryResult result,
            string variable,
            Guid tenantGuid,
            Guid graphGuid,
            IAsyncEnumerable<Node> nodes,
            List<GraphQueryPredicate> predicates,
            Dictionary<string, object> parameters,
            int limit,
            CancellationToken token)
        {
            await foreach (Node node in nodes.ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();

                bool matched = true;
                foreach (GraphQueryPredicate predicate in predicates)
                {
                    if (!await NodePredicateMatches(tenantGuid, graphGuid, node, predicate, ResolveValue(predicate.ValueExpression, parameters), token).ConfigureAwait(false))
                    {
                        matched = false;
                        break;
                    }
                }

                if (!matched) continue;
                AddNodeRow(result, variable, node);
                if (result.RowCount >= limit) break;
            }
        }

        private async Task AddNodesWhereExpression(
            GraphQueryResult result,
            string variable,
            Guid tenantGuid,
            Guid graphGuid,
            IAsyncEnumerable<Node> nodes,
            GraphQueryPredicateExpression expression,
            Dictionary<string, object> parameters,
            int limit,
            CancellationToken token)
        {
            await foreach (Node node in nodes.ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();

                bool matched = await EvaluateWhereExpressionAsync(expression, async predicate =>
                {
                    if (!String.Equals(predicate.Variable, variable, StringComparison.OrdinalIgnoreCase))
                        throw new ArgumentException("WHERE variable must match the MATCH variable.");

                    return await NodePredicateMatches(tenantGuid, graphGuid, node, predicate, ResolveValue(predicate.ValueExpression, parameters), token).ConfigureAwait(false);
                }).ConfigureAwait(false);

                if (!matched) continue;
                AddNodeRow(result, variable, node);
                if (result.RowCount >= limit) break;
            }
        }

        private async Task<List<Edge>> ReadCandidatePathEdges(
            Guid tenantGuid,
            Guid graphGuid,
            GraphQueryAst ast,
            GraphQueryRequest request,
            GraphQueryPathSegment segment,
            Node expectedFrom,
            CancellationToken token)
        {
            List<Edge> ret = new List<Edge>();

            if (expectedFrom != null)
            {
                await foreach (Edge edge in _Repo.Edge.ReadEdgesFromNode(tenantGuid, graphGuid, expectedFrom.GUID, labels: LabelList(segment.EdgeLabel), token: token).ConfigureAwait(false))
                    ret.Add(edge);
                return ret;
            }

            if (IsWhere(ast, segment.FromVariable, "guid"))
            {
                Guid fromGuid = ToGuid(ResolveValue(ast.WhereValueExpression, request.Parameters));
                await foreach (Edge edge in _Repo.Edge.ReadEdgesFromNode(tenantGuid, graphGuid, fromGuid, labels: LabelList(segment.EdgeLabel), token: token).ConfigureAwait(false))
                    ret.Add(edge);
                return ret;
            }

            if (IsWhere(ast, segment.ToVariable, "guid"))
            {
                Guid toGuid = ToGuid(ResolveValue(ast.WhereValueExpression, request.Parameters));
                await foreach (Edge edge in _Repo.Edge.ReadEdgesToNode(tenantGuid, graphGuid, toGuid, labels: LabelList(segment.EdgeLabel), token: token).ConfigureAwait(false))
                    ret.Add(edge);
                return ret;
            }

            if (IsWhere(ast, segment.EdgeVariable, "guid"))
            {
                Edge edge = await _Repo.Edge.ReadByGuid(tenantGuid, ToGuid(ResolveValue(ast.WhereValueExpression, request.Parameters)), token).ConfigureAwait(false);
                if (edge != null && edge.GraphGUID == graphGuid) ret.Add(edge);
                return ret;
            }

            if (IsWhere(ast, segment.EdgeVariable, "name"))
            {
                string edgeName = ResolveValue(ast.WhereValueExpression, request.Parameters)?.ToString();
                await foreach (Edge edge in _Repo.Edge.ReadMany(tenantGuid, graphGuid, edgeName, LabelList(segment.EdgeLabel), token: token).ConfigureAwait(false))
                    ret.Add(edge);
                return ret;
            }

            await foreach (Edge edge in _Repo.Edge.ReadAllInGraph(tenantGuid, graphGuid, token: token).ConfigureAwait(false))
                ret.Add(edge);
            return ret;
        }

        private async Task<List<PathState>> ExpandVariablePathSegment(
            Guid tenantGuid,
            Guid graphGuid,
            GraphQueryAst ast,
            GraphQueryRequest request,
            GraphQueryPathSegment segment,
            PathState state,
            CancellationToken token)
        {
            List<PathState> ret = new List<PathState>();
            List<Node> starts = await ReadVariablePathStartNodes(tenantGuid, graphGuid, ast, request, segment, state, token).ConfigureAwait(false);

            foreach (Node start in starts)
            {
                token.ThrowIfCancellationRequested();
                if (start == null || start.GraphGUID != graphGuid) continue;
                if (!await NodeMatchesLabel(start, segment.FromLabel, token).ConfigureAwait(false)) continue;

                PathState initial = state.Clone();
                if (!BindPathValue(initial, segment.FromVariable, start)) continue;

                await ExpandVariablePathFromNode(
                    tenantGuid,
                    graphGuid,
                    segment,
                    initial,
                    start,
                    new List<Edge>(),
                    new HashSet<Guid>(),
                    ret,
                    token).ConfigureAwait(false);
            }

            return ret;
        }

        private async Task<List<Node>> ReadVariablePathStartNodes(
            Guid tenantGuid,
            Guid graphGuid,
            GraphQueryAst ast,
            GraphQueryRequest request,
            GraphQueryPathSegment segment,
            PathState state,
            CancellationToken token)
        {
            List<Node> ret = new List<Node>();
            Node bound = GetBoundNode(state, segment.FromVariable);
            if (bound != null)
            {
                ret.Add(bound);
                return ret;
            }

            if (IsWhere(ast, segment.FromVariable, "guid"))
            {
                Node node = await _Repo.Node.ReadByGuid(tenantGuid, ToGuid(ResolveValue(ast.WhereValueExpression, request.Parameters)), token).ConfigureAwait(false);
                if (node != null && node.GraphGUID == graphGuid) ret.Add(node);
                return ret;
            }

            IAsyncEnumerable<Node> candidates = !String.IsNullOrEmpty(segment.FromLabel)
                ? _Repo.Node.ReadMany(tenantGuid, graphGuid, labels: LabelList(segment.FromLabel), token: token)
                : _Repo.Node.ReadAllInGraph(tenantGuid, graphGuid, token: token);

            await foreach (Node node in candidates.ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                if (node != null && node.GraphGUID == graphGuid) ret.Add(node);
            }

            return ret;
        }

        private async Task ExpandVariablePathFromNode(
            Guid tenantGuid,
            Guid graphGuid,
            GraphQueryPathSegment segment,
            PathState baseState,
            Node current,
            List<Edge> pathEdges,
            HashSet<Guid> usedEdgeGuids,
            List<PathState> results,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            int depth = pathEdges.Count;

            if (depth >= segment.MinHops)
            {
                if (await NodeMatchesLabel(current, segment.ToLabel, token).ConfigureAwait(false))
                {
                    PathState completed = baseState.Clone();
                    if (BindPathValue(completed, segment.EdgeVariable, new List<Edge>(pathEdges))
                        && BindPathValue(completed, segment.ToVariable, current))
                    {
                        completed.HopCount += pathEdges.Count;
                        results.Add(completed);
                    }
                }
            }

            if (depth >= segment.MaxHops) return;

            await foreach (Edge edge in _Repo.Edge.ReadEdgesFromNode(tenantGuid, graphGuid, current.GUID, labels: LabelList(segment.EdgeLabel), token: token).ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                if (edge == null || edge.GraphGUID != graphGuid) continue;
                if (usedEdgeGuids.Contains(edge.GUID)) continue;
                if (!await EdgeMatchesLabel(edge, segment.EdgeLabel, token).ConfigureAwait(false)) continue;

                Node next = await _Repo.Node.ReadByGuid(tenantGuid, edge.To, token).ConfigureAwait(false);
                if (next == null || next.GraphGUID != graphGuid) continue;

                usedEdgeGuids.Add(edge.GUID);
                pathEdges.Add(edge);

                await ExpandVariablePathFromNode(
                    tenantGuid,
                    graphGuid,
                    segment,
                    baseState,
                    next,
                    pathEdges,
                    usedEdgeGuids,
                    results,
                    token).ConfigureAwait(false);

                pathEdges.RemoveAt(pathEdges.Count - 1);
                usedEdgeGuids.Remove(edge.GUID);
            }
        }

        private static List<PathState> FilterShortestPathStates(List<PathState> states)
        {
            if (states == null || states.Count < 2) return states ?? new List<PathState>();

            int shortest = states.Min(state => state.HopCount);
            return states.Where(state => state.HopCount == shortest).ToList();
        }

        private static Node GetBoundNode(PathState state, string variable)
        {
            if (String.IsNullOrEmpty(variable)) return null;
            if (state.Values.TryGetValue(variable, out object value) && value is Node node) return node;
            return null;
        }

        private static bool BindPathValue(PathState state, string variable, object value)
        {
            if (String.IsNullOrEmpty(variable)) return true;
            if (state.Values.TryGetValue(variable, out object existing))
                return SameGraphObject(existing, value);

            state.Values[variable] = value;
            return true;
        }

        private static bool SameGraphObject(object left, object right)
        {
            if (left is Node leftNode && right is Node rightNode) return leftNode.GUID == rightNode.GUID;
            if (left is Edge leftEdge && right is Edge rightEdge) return leftEdge.GUID == rightEdge.GUID;
            if (left is IEnumerable<Edge> leftEdges && right is IEnumerable<Edge> rightEdges)
                return leftEdges.Select(edge => edge.GUID).SequenceEqual(rightEdges.Select(edge => edge.GUID));
            return Object.Equals(left, right);
        }

        private async Task<bool> EdgePredicatesMatch(
            Guid tenantGuid,
            Guid graphGuid,
            GraphQueryAst ast,
            Dictionary<string, object> parameters,
            Edge edge,
            List<GraphQueryPredicate> predicates,
            CancellationToken token)
        {
            Node from = null;
            Node to = null;

            foreach (GraphQueryPredicate predicate in predicates)
            {
                object expected = ResolveValue(predicate.ValueExpression, parameters);

                if (!String.IsNullOrEmpty(ast.EdgeVariable)
                    && String.Equals(predicate.Variable, ast.EdgeVariable, StringComparison.OrdinalIgnoreCase))
                {
                    if (!await EdgePredicateMatches(tenantGuid, graphGuid, edge, predicate, expected, token).ConfigureAwait(false)) return false;
                    continue;
                }

                if (!String.IsNullOrEmpty(ast.FromVariable)
                    && String.Equals(predicate.Variable, ast.FromVariable, StringComparison.OrdinalIgnoreCase))
                {
                    from ??= await _Repo.Node.ReadByGuid(tenantGuid, edge.From, token).ConfigureAwait(false);
                    if (from == null || from.GraphGUID != graphGuid || !await NodePredicateMatches(tenantGuid, graphGuid, from, predicate, expected, token).ConfigureAwait(false)) return false;
                    continue;
                }

                if (!String.IsNullOrEmpty(ast.ToVariable)
                    && String.Equals(predicate.Variable, ast.ToVariable, StringComparison.OrdinalIgnoreCase))
                {
                    to ??= await _Repo.Node.ReadByGuid(tenantGuid, edge.To, token).ConfigureAwait(false);
                    if (to == null || to.GraphGUID != graphGuid || !await NodePredicateMatches(tenantGuid, graphGuid, to, predicate, expected, token).ConfigureAwait(false)) return false;
                    continue;
                }

                throw new ArgumentException("Unsupported edge WHERE variable '" + predicate.Variable + "'.");
            }

            return true;
        }

        private async Task<bool> EdgeWhereExpressionMatches(
            Guid tenantGuid,
            Guid graphGuid,
            GraphQueryAst ast,
            Dictionary<string, object> parameters,
            Edge edge,
            GraphQueryPredicateExpression expression,
            CancellationToken token)
        {
            Node from = null;
            Node to = null;

            return await EvaluateWhereExpressionAsync(expression, async predicate =>
            {
                object expected = ResolveValue(predicate.ValueExpression, parameters);

                if (!String.IsNullOrEmpty(ast.EdgeVariable)
                    && String.Equals(predicate.Variable, ast.EdgeVariable, StringComparison.OrdinalIgnoreCase))
                {
                    return await EdgePredicateMatches(tenantGuid, graphGuid, edge, predicate, expected, token).ConfigureAwait(false);
                }

                if (!String.IsNullOrEmpty(ast.FromVariable)
                    && String.Equals(predicate.Variable, ast.FromVariable, StringComparison.OrdinalIgnoreCase))
                {
                    from ??= await _Repo.Node.ReadByGuid(tenantGuid, edge.From, token).ConfigureAwait(false);
                    return from != null && from.GraphGUID == graphGuid && await NodePredicateMatches(tenantGuid, graphGuid, from, predicate, expected, token).ConfigureAwait(false);
                }

                if (!String.IsNullOrEmpty(ast.ToVariable)
                    && String.Equals(predicate.Variable, ast.ToVariable, StringComparison.OrdinalIgnoreCase))
                {
                    to ??= await _Repo.Node.ReadByGuid(tenantGuid, edge.To, token).ConfigureAwait(false);
                    return to != null && to.GraphGUID == graphGuid && await NodePredicateMatches(tenantGuid, graphGuid, to, predicate, expected, token).ConfigureAwait(false);
                }

                throw new ArgumentException("Unsupported edge WHERE variable '" + predicate.Variable + "'.");
            }).ConfigureAwait(false);
        }

        private async Task<bool> NodeWhereMatches(Guid tenantGuid, Guid graphGuid, GraphQueryAst ast, Node node, Dictionary<string, object> parameters, CancellationToken token)
        {
            if (ast.WhereExpression == null) return true;

            return await EvaluateWhereExpressionAsync(ast.WhereExpression, async predicate =>
            {
                if (!String.Equals(predicate.Variable, ast.NodeVariable, StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("WHERE variable must match the MATCH variable.");

                object expected = ResolveValue(predicate.ValueExpression, parameters);
                return await NodePredicateMatches(tenantGuid, graphGuid, node, predicate, expected, token).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        private async Task<bool> PathWhereMatches(Guid tenantGuid, Guid graphGuid, GraphQueryAst ast, Dictionary<string, object> parameters, Dictionary<string, object> values, CancellationToken token)
        {
            if (ast.WhereExpression == null) return true;

            return await EvaluateWhereExpressionAsync(ast.WhereExpression, async predicate =>
            {
                if (!values.TryGetValue(predicate.Variable, out object value)) return false;

                object expected = ResolveValue(predicate.ValueExpression, parameters);
                if (value is Node node)
                {
                    return await NodePredicateMatches(tenantGuid, graphGuid, node, predicate, expected, token).ConfigureAwait(false);
                }

                if (value is Edge edge)
                {
                    return await EdgePredicateMatches(tenantGuid, graphGuid, edge, predicate, expected, token).ConfigureAwait(false);
                }

                if (value is IEnumerable<Edge> edges)
                {
                    foreach (Edge pathEdge in edges)
                    {
                        token.ThrowIfCancellationRequested();
                        if (pathEdge != null && await EdgePredicateMatches(tenantGuid, graphGuid, pathEdge, predicate, expected, token).ConfigureAwait(false))
                            return true;
                    }

                    return false;
                }

                throw new ArgumentException("Unsupported path WHERE variable '" + predicate.Variable + "'.");
            }).ConfigureAwait(false);
        }

        private static bool IsWhere(GraphQueryAst ast, string variable, string field)
        {
            if (!TryGetConjunctiveWherePredicates(ast, out List<GraphQueryPredicate> predicates)) return false;
            if (predicates.Count != 1) return false;
            GraphQueryPredicate predicate = predicates[0];

            return !String.IsNullOrEmpty(variable)
                && String.Equals(predicate.Variable, variable, StringComparison.OrdinalIgnoreCase)
                && String.Equals(predicate.Field, field, StringComparison.OrdinalIgnoreCase)
                && IsEqualsOperator(predicate.Operator);
        }

        private async Task<bool> NodePredicateMatches(Guid tenantGuid, Guid graphGuid, Node node, GraphQueryPredicate predicate, object expected, CancellationToken token)
        {
            if (IsTagField(predicate.Field))
            {
                return await TagPredicateMatches(
                    _Repo.Tag.ReadManyNode(tenantGuid, graphGuid, node.GUID, token: token),
                    predicate,
                    expected,
                    token).ConfigureAwait(false);
            }

            return NodePredicateMatches(node, predicate, expected);
        }

        private async Task<bool> EdgePredicateMatches(Guid tenantGuid, Guid graphGuid, Edge edge, GraphQueryPredicate predicate, object expected, CancellationToken token)
        {
            if (IsTagField(predicate.Field))
            {
                return await TagPredicateMatches(
                    _Repo.Tag.ReadManyEdge(tenantGuid, graphGuid, edge.GUID, token: token),
                    predicate,
                    expected,
                    token).ConfigureAwait(false);
            }

            return EdgePredicateMatches(edge, predicate, expected);
        }

        private static async Task<bool> TagPredicateMatches(IAsyncEnumerable<TagMetadata> tags, GraphQueryPredicate predicate, object expected, CancellationToken token)
        {
            string key = GetTagKey(predicate.Field);

            await foreach (TagMetadata tag in tags.ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();
                if (tag == null || !String.Equals(tag.Key, key, StringComparison.OrdinalIgnoreCase)) continue;
                if (CompareValues(tag.Value, predicate.Operator, expected)) return true;
            }

            return false;
        }

        private static bool NodePredicateMatches(Node node, GraphQueryPredicate predicate, object expected)
        {
            if (String.Equals(predicate.Field, "guid", StringComparison.OrdinalIgnoreCase)
                && (IsEqualsOperator(predicate.Operator) || IsInOperator(predicate.Operator)))
                return CompareValues(node.GUID, predicate.Operator, expected);
            if (String.Equals(predicate.Field, "name", StringComparison.OrdinalIgnoreCase)
                && (IsEqualsOperator(predicate.Operator) || IsStringOperator(predicate.Operator) || IsInOperator(predicate.Operator)))
                return CompareValues(node.Name, predicate.Operator, expected);
            if (IsDataField(predicate.Field)) return DataPathMatches(node.Data, predicate.Field, predicate.Operator, expected);
            throw new ArgumentException("Unsupported node WHERE clause. Supported fields: node.guid equality/list operators, node.name string/equality/list operators, node.data.<field> equality/numeric/string/list operators.");
        }

        private static bool EdgePredicateMatches(Edge edge, GraphQueryPredicate predicate, object expected)
        {
            if (String.Equals(predicate.Field, "guid", StringComparison.OrdinalIgnoreCase)
                && (IsEqualsOperator(predicate.Operator) || IsInOperator(predicate.Operator)))
                return CompareValues(edge.GUID, predicate.Operator, expected);
            if (String.Equals(predicate.Field, "name", StringComparison.OrdinalIgnoreCase)
                && (IsEqualsOperator(predicate.Operator) || IsStringOperator(predicate.Operator) || IsInOperator(predicate.Operator)))
                return CompareValues(edge.Name, predicate.Operator, expected);
            if (IsDataField(predicate.Field)) return DataPathMatches(edge.Data, predicate.Field, predicate.Operator, expected);
            throw new ArgumentException("Unsupported edge WHERE clause. Supported fields: edge.guid equality/list operators, edge.name string/equality/list operators, edge.data.<field> equality/numeric/string/list operators.");
        }

        private static List<GraphQueryPredicate> GetWherePredicates(GraphQueryAst ast)
        {
            if (ast == null) return new List<GraphQueryPredicate>();
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

        private static bool TryGetConjunctiveWherePredicates(GraphQueryAst ast, out List<GraphQueryPredicate> predicates)
        {
            predicates = new List<GraphQueryPredicate>();

            if (ast?.WhereExpression == null)
            {
                predicates = GetWherePredicates(ast);
                return true;
            }

            return TryCollectConjunctivePredicates(ast.WhereExpression, predicates);
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

        private static bool EvaluateWhereExpression(GraphQueryPredicateExpression expression, Func<GraphQueryPredicate, bool> predicateEvaluator)
        {
            if (expression == null) return true;

            switch (expression.Kind)
            {
                case GraphQueryPredicateExpressionKindEnum.Predicate:
                    return predicateEvaluator(expression.Predicate);
                case GraphQueryPredicateExpressionKindEnum.And:
                    return EvaluateWhereExpression(expression.Left, predicateEvaluator)
                        && EvaluateWhereExpression(expression.Right, predicateEvaluator);
                case GraphQueryPredicateExpressionKindEnum.Or:
                    return EvaluateWhereExpression(expression.Left, predicateEvaluator)
                        || EvaluateWhereExpression(expression.Right, predicateEvaluator);
                case GraphQueryPredicateExpressionKindEnum.Not:
                    return !EvaluateWhereExpression(expression.Left, predicateEvaluator);
                default:
                    throw new ArgumentException("Unsupported WHERE expression kind '" + expression.Kind + "'.");
            }
        }

        private static async Task<bool> EvaluateWhereExpressionAsync(GraphQueryPredicateExpression expression, Func<GraphQueryPredicate, Task<bool>> predicateEvaluator)
        {
            if (expression == null) return true;

            switch (expression.Kind)
            {
                case GraphQueryPredicateExpressionKindEnum.Predicate:
                    return await predicateEvaluator(expression.Predicate).ConfigureAwait(false);
                case GraphQueryPredicateExpressionKindEnum.And:
                    if (!await EvaluateWhereExpressionAsync(expression.Left, predicateEvaluator).ConfigureAwait(false)) return false;
                    return await EvaluateWhereExpressionAsync(expression.Right, predicateEvaluator).ConfigureAwait(false);
                case GraphQueryPredicateExpressionKindEnum.Or:
                    if (await EvaluateWhereExpressionAsync(expression.Left, predicateEvaluator).ConfigureAwait(false)) return true;
                    return await EvaluateWhereExpressionAsync(expression.Right, predicateEvaluator).ConfigureAwait(false);
                case GraphQueryPredicateExpressionKindEnum.Not:
                    return !await EvaluateWhereExpressionAsync(expression.Left, predicateEvaluator).ConfigureAwait(false);
                default:
                    throw new ArgumentException("Unsupported WHERE expression kind '" + expression.Kind + "'.");
            }
        }

        private static bool IsEqualsOperator(string whereOperator)
        {
            return String.IsNullOrEmpty(whereOperator) || String.Equals(whereOperator, "=", StringComparison.Ordinal);
        }

        private static bool IsInOperator(string whereOperator)
        {
            return String.Equals(whereOperator, "IN", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsStringOperator(string whereOperator)
        {
            return String.Equals(whereOperator, "CONTAINS", StringComparison.OrdinalIgnoreCase)
                || String.Equals(whereOperator, "STARTS WITH", StringComparison.OrdinalIgnoreCase)
                || String.Equals(whereOperator, "ENDS WITH", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDataField(string field)
        {
            return !String.IsNullOrEmpty(field)
                && field.StartsWith("data.", StringComparison.OrdinalIgnoreCase)
                && field.Length > "data.".Length;
        }

        private static bool IsTagField(string field)
        {
            return !String.IsNullOrEmpty(field)
                && ((field.StartsWith("tags.", StringComparison.OrdinalIgnoreCase) && field.Length > "tags.".Length)
                    || (field.StartsWith("tag.", StringComparison.OrdinalIgnoreCase) && field.Length > "tag.".Length));
        }

        private static string GetTagKey(string field)
        {
            if (field.StartsWith("tags.", StringComparison.OrdinalIgnoreCase)) return field.Substring("tags.".Length);
            if (field.StartsWith("tag.", StringComparison.OrdinalIgnoreCase)) return field.Substring("tag.".Length);
            throw new ArgumentException("Tag predicate fields must use tags.<key>.");
        }

        private static bool DataPathMatches(object data, string field, string whereOperator, object expected)
        {
            object actual = ResolveDataPath(data, field.Substring("data.".Length));
            return CompareValues(actual, whereOperator, expected);
        }

        private static object ResolveDataPath(object data, string path)
        {
            object current = NormalizeJsonValue(data);
            foreach (string part in path.Split('.'))
            {
                current = NormalizeJsonValue(current);
                if (current == null) return null;

                if (current is IDictionary<string, object> genericDictionary)
                {
                    if (TryGetDictionaryValue(genericDictionary, part, out current)) continue;
                    return null;
                }

                if (current is IDictionary dictionary)
                {
                    if (dictionary.Contains(part))
                    {
                        current = dictionary[part];
                        continue;
                    }

                    bool found = false;
                    foreach (DictionaryEntry entry in dictionary)
                    {
                        if (entry.Key != null && String.Equals(entry.Key.ToString(), part, StringComparison.OrdinalIgnoreCase))
                        {
                            current = entry.Value;
                            found = true;
                            break;
                        }
                    }

                    if (found) continue;
                    return null;
                }

                return null;
            }

            return NormalizeJsonValue(current);
        }

        private static bool TryGetDictionaryValue(IDictionary<string, object> dictionary, string key, out object value)
        {
            if (dictionary.TryGetValue(key, out value)) return true;

            foreach (KeyValuePair<string, object> kvp in dictionary)
            {
                if (String.Equals(kvp.Key, key, StringComparison.OrdinalIgnoreCase))
                {
                    value = kvp.Value;
                    return true;
                }
            }

            value = null;
            return false;
        }

        private static bool CompareValues(object actual, string whereOperator, object expected)
        {
            actual = NormalizeJsonValue(actual);
            expected = NormalizeJsonValue(expected);

            if (IsInOperator(whereOperator))
            {
                if (expected == null || expected is string)
                    throw new ArgumentException("Operator 'IN' requires a list value.");
                if (expected is not IEnumerable values)
                    throw new ArgumentException("Operator 'IN' requires a list value.");

                foreach (object value in values)
                {
                    if (CompareValues(actual, "=", value)) return true;
                }

                return false;
            }

            if (IsEqualsOperator(whereOperator))
            {
                if (actual == null || expected == null) return actual == null && expected == null;
                if (Guid.TryParse(actual.ToString(), out Guid actualGuid) && Guid.TryParse(expected.ToString(), out Guid expectedGuid))
                    return actualGuid == expectedGuid;

                if ((actual is bool || expected is bool)
                    && Boolean.TryParse(actual.ToString(), out bool actualBool)
                    && Boolean.TryParse(expected.ToString(), out bool expectedBool))
                    return actualBool == expectedBool;

                if (IsNumeric(actual) && IsNumeric(expected))
                    return Convert.ToDecimal(actual, CultureInfo.InvariantCulture) == Convert.ToDecimal(expected, CultureInfo.InvariantCulture);

                return String.Equals(Convert.ToString(actual, CultureInfo.InvariantCulture), Convert.ToString(expected, CultureInfo.InvariantCulture), StringComparison.Ordinal);
            }

            if (actual == null || expected == null) return false;

            if (IsStringOperator(whereOperator))
            {
                string actualString = Convert.ToString(actual, CultureInfo.InvariantCulture) ?? String.Empty;
                string expectedString = Convert.ToString(expected, CultureInfo.InvariantCulture) ?? String.Empty;

                if (String.Equals(whereOperator, "CONTAINS", StringComparison.OrdinalIgnoreCase))
                    return actualString.Contains(expectedString, StringComparison.Ordinal);
                if (String.Equals(whereOperator, "STARTS WITH", StringComparison.OrdinalIgnoreCase))
                    return actualString.StartsWith(expectedString, StringComparison.Ordinal);
                if (String.Equals(whereOperator, "ENDS WITH", StringComparison.OrdinalIgnoreCase))
                    return actualString.EndsWith(expectedString, StringComparison.Ordinal);
            }

            if (!IsNumeric(actual) || !IsNumeric(expected))
                throw new ArgumentException("Operator '" + whereOperator + "' requires numeric data values.");

            decimal left = Convert.ToDecimal(actual, CultureInfo.InvariantCulture);
            decimal right = Convert.ToDecimal(expected, CultureInfo.InvariantCulture);

            switch (whereOperator)
            {
                case ">":
                    return left > right;
                case ">=":
                    return left >= right;
                case "<":
                    return left < right;
                case "<=":
                    return left <= right;
                default:
                    throw new ArgumentException("Unsupported WHERE operator '" + whereOperator + "'.");
            }
        }

        private static bool IsNumeric(object value)
        {
            if (value == null || value is string) return false;

            switch (Type.GetTypeCode(value.GetType()))
            {
                case TypeCode.Byte:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.Single:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return true;
                default:
                    return false;
            }
        }

        private static void AddNodeRow(GraphQueryResult result, string variable, Node node)
        {
            result.Nodes.Add(node);
            AddObjectRow(result, variable, node);
        }

        private async Task AddEdgeRow(
            GraphQueryResult result,
            List<string> returnVariables,
            string fromVariable,
            string edgeVariable,
            string toVariable,
            Edge edge,
            CancellationToken token)
        {
            Dictionary<string, object> row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (returnVariables.Contains(edgeVariable, StringComparer.OrdinalIgnoreCase))
            {
                row[edgeVariable] = edge;
                result.Edges.Add(edge);
            }

            if (returnVariables.Contains(fromVariable, StringComparer.OrdinalIgnoreCase))
            {
                Node from = await _Repo.Node.ReadByGuid(edge.TenantGUID, edge.From, token).ConfigureAwait(false);
                row[fromVariable] = from;
                if (from != null) result.Nodes.Add(from);
            }

            if (returnVariables.Contains(toVariable, StringComparer.OrdinalIgnoreCase))
            {
                Node to = await _Repo.Node.ReadByGuid(edge.TenantGUID, edge.To, token).ConfigureAwait(false);
                row[toVariable] = to;
                if (to != null) result.Nodes.Add(to);
            }

            result.Rows.Add(row);
        }

        private static void AddPathRow(GraphQueryResult result, List<string> returnVariables, Dictionary<string, object> values)
        {
            Dictionary<string, object> row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (string variable in returnVariables)
            {
                if (!values.TryGetValue(variable, out object value))
                    throw new ArgumentException("RETURN variable '" + variable + "' is not bound by the path pattern.");

                row[variable] = value;
                if (value is Node node) result.Nodes.Add(node);
                else if (value is Edge edge) result.Edges.Add(edge);
                else if (value is IEnumerable<Edge> edges)
                {
                    foreach (Edge pathEdge in edges)
                    {
                        if (pathEdge != null) result.Edges.Add(pathEdge);
                    }
                }
            }

            result.Rows.Add(row);
        }

        private static void AddObjectRow(GraphQueryResult result, string variable, object value)
        {
            result.Rows.Add(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                { variable, value }
            });
        }

        private static GraphQueryResult MutatedResult()
        {
            return new GraphQueryResult { Mutated = true };
        }

        private static string RequireReturnVariable(GraphQueryAst ast, string expectedVariable, string description)
        {
            if (String.IsNullOrEmpty(expectedVariable)) throw new ArgumentException("A variable is required for the " + description + ".");
            if (ast.ReturnVariables.Count != 1 || !String.Equals(ast.ReturnVariables[0], expectedVariable, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("CREATE queries can only RETURN the " + description + " variable.");
            return expectedVariable;
        }

        private static string RequireMutationReturnVariable(GraphQueryAst ast, string expectedVariable, string description)
        {
            if (String.IsNullOrEmpty(expectedVariable)) throw new ArgumentException("A variable is required for the " + description + ".");
            if (ast.ReturnVariables.Count != 1 || !String.Equals(ast.ReturnVariables[0], expectedVariable, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Mutation queries can only RETURN the " + description + " variable.");
            return expectedVariable;
        }

        private static Guid RequireGuidWhere(GraphQueryAst ast, string expectedVariable, string description, Dictionary<string, object> parameters)
        {
            List<GraphQueryPredicate> predicates = GetWherePredicates(ast);
            if (predicates.Count < 1)
                throw new ArgumentException(description + " requires a WHERE " + expectedVariable + ".guid = ... clause.");

            if (predicates.Count != 1
                || !String.Equals(predicates[0].Variable, expectedVariable, StringComparison.OrdinalIgnoreCase)
                || !String.Equals(predicates[0].Field, "guid", StringComparison.OrdinalIgnoreCase)
                || !IsEqualsOperator(predicates[0].Operator))
                throw new ArgumentException(description + " only supports WHERE " + expectedVariable + ".guid = ... in this release.");

            return ToGuid(ResolveValue(predicates[0].ValueExpression, parameters));
        }

        private static void ApplyNodeSet(Node node, Dictionary<string, string> setProperties, Dictionary<string, object> parameters)
        {
            foreach (KeyValuePair<string, string> setProperty in setProperties)
            {
                object value = ResolveValue(setProperty.Value, parameters);
                if (setProperty.Key.Equals("name", StringComparison.OrdinalIgnoreCase))
                {
                    node.Name = value?.ToString();
                }
                else if (setProperty.Key.Equals("data", StringComparison.OrdinalIgnoreCase))
                {
                    node.Data = value;
                }
                else
                {
                    throw new ArgumentException("Unsupported node SET property '" + setProperty.Key + "'. Supported properties: name, data.");
                }
            }
        }

        private static void ApplyEdgeSet(Edge edge, Dictionary<string, string> setProperties, Dictionary<string, object> parameters)
        {
            foreach (KeyValuePair<string, string> setProperty in setProperties)
            {
                object value = ResolveValue(setProperty.Value, parameters);
                if (setProperty.Key.Equals("name", StringComparison.OrdinalIgnoreCase))
                {
                    edge.Name = value?.ToString();
                }
                else if (setProperty.Key.Equals("data", StringComparison.OrdinalIgnoreCase))
                {
                    edge.Data = value;
                }
                else if (setProperty.Key.Equals("cost", StringComparison.OrdinalIgnoreCase))
                {
                    edge.Cost = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                }
                else
                {
                    throw new ArgumentException("Unsupported edge SET property '" + setProperty.Key + "'. Supported properties: name, data, cost.");
                }
            }
        }

        private static void ApplyLabelSet(LabelMetadata label, Dictionary<string, string> setProperties, Dictionary<string, object> parameters)
        {
            foreach (KeyValuePair<string, string> setProperty in setProperties)
            {
                object value = ResolveValue(setProperty.Value, parameters);
                if (setProperty.Key.Equals("label", StringComparison.OrdinalIgnoreCase))
                {
                    label.Label = value?.ToString();
                }
                else
                {
                    throw new ArgumentException("Unsupported label SET property '" + setProperty.Key + "'. Supported properties: label.");
                }
            }
        }

        private static void ApplyTagSet(TagMetadata tag, Dictionary<string, string> setProperties, Dictionary<string, object> parameters)
        {
            foreach (KeyValuePair<string, string> setProperty in setProperties)
            {
                object value = ResolveValue(setProperty.Value, parameters);
                if (setProperty.Key.Equals("key", StringComparison.OrdinalIgnoreCase))
                {
                    tag.Key = value?.ToString();
                }
                else if (setProperty.Key.Equals("value", StringComparison.OrdinalIgnoreCase))
                {
                    tag.Value = value?.ToString() ?? String.Empty;
                }
                else
                {
                    throw new ArgumentException("Unsupported tag SET property '" + setProperty.Key + "'. Supported properties: key, value.");
                }
            }
        }

        private static void ApplyVectorSet(VectorMetadata vector, Dictionary<string, string> setProperties, Dictionary<string, object> parameters)
        {
            foreach (KeyValuePair<string, string> setProperty in setProperties)
            {
                object value = ResolveValue(setProperty.Value, parameters);
                if (setProperty.Key.Equals("model", StringComparison.OrdinalIgnoreCase))
                {
                    vector.Model = value?.ToString();
                }
                else if (setProperty.Key.Equals("content", StringComparison.OrdinalIgnoreCase))
                {
                    vector.Content = value?.ToString() ?? String.Empty;
                }
                else if (setProperty.Key.Equals("embeddings", StringComparison.OrdinalIgnoreCase)
                    || setProperty.Key.Equals("vectors", StringComparison.OrdinalIgnoreCase))
                {
                    vector.Vectors = ToFloatList(value);
                    vector.Dimensionality = vector.Vectors.Count;
                }
                else
                {
                    throw new ArgumentException("Unsupported vector SET property '" + setProperty.Key + "'. Supported properties: model, content, embeddings, vectors.");
                }
            }
        }

        private static int ResolveLimit(GraphQueryRequest request, GraphQueryAst ast)
        {
            if (HasOrder(ast)) return request.MaxResults;
            int limit = request.MaxResults;
            if (ast.Limit != null) limit = Math.Min(limit, ast.Limit.Value);
            return limit;
        }

        internal static GraphQueryResult ApplyOrderAndLimit(GraphQueryResult result, GraphQueryAst ast, GraphQueryRequest request)
        {
            if (result == null || result.Rows == null) return result;

            if (HasOrder(ast))
            {
                List<Dictionary<string, object>> ordered = result.Rows
                    .OrderBy(row => ResolveOrderValue(row, ast), OrderValueComparer.Instance)
                    .ToList();

                if (ast.OrderDescending) ordered.Reverse();
                result.Rows = ordered;

                int limit = request.MaxResults;
                if (ast.Limit != null) limit = Math.Min(limit, ast.Limit.Value);
                if (result.Rows.Count > limit) result.Rows = result.Rows.Take(limit).ToList();
                RebuildTypedResultLists(result);
            }

            return result;
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

        private static object ResolveOrderValue(Dictionary<string, object> row, GraphQueryAst ast)
        {
            if (String.IsNullOrEmpty(ast.OrderVariable))
            {
                if (row.TryGetValue(ast.OrderField, out object scalar)) return scalar;
                throw new ArgumentException("ORDER BY scalar '" + ast.OrderField + "' is not returned by the query.");
            }

            if (!row.TryGetValue(ast.OrderVariable, out object value) || value == null)
                return null;

            if (value is Node node) return ResolveNodeOrderValue(node, ast.OrderField);
            if (value is Edge edge) return ResolveEdgeOrderValue(edge, ast.OrderField);
            if (value is LabelMetadata label) return ResolveLabelOrderValue(label, ast.OrderField);
            if (value is TagMetadata tag) return ResolveTagOrderValue(tag, ast.OrderField);
            if (value is VectorMetadata vector) return ResolveVectorOrderValue(vector, ast.OrderField);
            if (value is VectorSearchResult vectorSearchResult) return ResolveVectorSearchOrderValue(vectorSearchResult, ast.OrderField);

            throw new ArgumentException("ORDER BY variable '" + ast.OrderVariable + "' does not reference a sortable value.");
        }

        private static object ResolveNodeOrderValue(Node node, string field)
        {
            if (String.Equals(field, "guid", StringComparison.OrdinalIgnoreCase)) return node.GUID;
            if (String.Equals(field, "name", StringComparison.OrdinalIgnoreCase)) return node.Name;
            if (IsDataField(field)) return ResolveDataPath(node.Data, field.Substring("data.".Length));
            throw new ArgumentException("Unsupported node ORDER BY field '" + field + "'. Supported fields: guid, name, data.<field>.");
        }

        private static object ResolveEdgeOrderValue(Edge edge, string field)
        {
            if (String.Equals(field, "guid", StringComparison.OrdinalIgnoreCase)) return edge.GUID;
            if (String.Equals(field, "name", StringComparison.OrdinalIgnoreCase)) return edge.Name;
            if (String.Equals(field, "cost", StringComparison.OrdinalIgnoreCase)) return edge.Cost;
            if (IsDataField(field)) return ResolveDataPath(edge.Data, field.Substring("data.".Length));
            throw new ArgumentException("Unsupported edge ORDER BY field '" + field + "'. Supported fields: guid, name, cost, data.<field>.");
        }

        private static object ResolveLabelOrderValue(LabelMetadata label, string field)
        {
            if (String.Equals(field, "guid", StringComparison.OrdinalIgnoreCase)) return label.GUID;
            if (String.Equals(field, "label", StringComparison.OrdinalIgnoreCase)) return label.Label;
            throw new ArgumentException("Unsupported label ORDER BY field '" + field + "'. Supported fields: guid, label.");
        }

        private static object ResolveTagOrderValue(TagMetadata tag, string field)
        {
            if (String.Equals(field, "guid", StringComparison.OrdinalIgnoreCase)) return tag.GUID;
            if (String.Equals(field, "key", StringComparison.OrdinalIgnoreCase)) return tag.Key;
            if (String.Equals(field, "value", StringComparison.OrdinalIgnoreCase)) return tag.Value;
            throw new ArgumentException("Unsupported tag ORDER BY field '" + field + "'. Supported fields: guid, key, value.");
        }

        private static object ResolveVectorOrderValue(VectorMetadata vector, string field)
        {
            if (String.Equals(field, "guid", StringComparison.OrdinalIgnoreCase)) return vector.GUID;
            if (String.Equals(field, "model", StringComparison.OrdinalIgnoreCase)) return vector.Model;
            if (String.Equals(field, "content", StringComparison.OrdinalIgnoreCase)) return vector.Content;
            if (String.Equals(field, "dimensionality", StringComparison.OrdinalIgnoreCase)) return vector.Dimensionality;
            throw new ArgumentException("Unsupported vector ORDER BY field '" + field + "'. Supported fields: guid, model, content, dimensionality.");
        }

        private static object ResolveVectorSearchOrderValue(VectorSearchResult result, string field)
        {
            if (String.Equals(field, "score", StringComparison.OrdinalIgnoreCase)) return result.Score;
            if (String.Equals(field, "distance", StringComparison.OrdinalIgnoreCase)) return result.Distance;
            if (String.Equals(field, "innerProduct", StringComparison.OrdinalIgnoreCase)) return result.InnerProduct;
            throw new ArgumentException("Unsupported vector search ORDER BY field '" + field + "'. Supported fields: score, distance, innerProduct.");
        }

        private static void RebuildTypedResultLists(GraphQueryResult result)
        {
            result.Nodes = new List<Node>();
            result.Edges = new List<Edge>();
            result.Labels = new List<LabelMetadata>();
            result.Tags = new List<TagMetadata>();
            result.Vectors = new List<VectorMetadata>();
            result.VectorSearchResults = new List<VectorSearchResult>();

            foreach (Dictionary<string, object> row in result.Rows)
            {
                foreach (object value in row.Values)
                {
                    if (value is Node node) result.Nodes.Add(node);
                    else if (value is Edge edge) result.Edges.Add(edge);
                    else if (value is IEnumerable<Edge> edges)
                    {
                        foreach (Edge pathEdge in edges)
                        {
                            if (pathEdge != null) result.Edges.Add(pathEdge);
                        }
                    }
                    else if (value is LabelMetadata label) result.Labels.Add(label);
                    else if (value is TagMetadata tag) result.Tags.Add(tag);
                    else if (value is VectorMetadata vector) result.Vectors.Add(vector);
                    else if (value is VectorSearchResult vectorSearchResult) result.VectorSearchResults.Add(vectorSearchResult);
                }
            }
        }

        private static List<string> LabelList(string label)
        {
            if (String.IsNullOrEmpty(label)) return null;
            return new List<string> { label };
        }

        private async Task<bool> NodeMatchesLabel(Node node, string label, CancellationToken token)
        {
            if (String.IsNullOrEmpty(label)) return true;
            if (LabelMatches(node, label)) return true;

            await foreach (LabelMetadata current in _Repo.Label.ReadMany(node.TenantGUID, node.GraphGUID, node.GUID, null, label, token: token).ConfigureAwait(false))
            {
                if (current != null) return true;
            }

            return false;
        }

        private async Task<bool> EdgeMatchesLabel(Edge edge, string label, CancellationToken token)
        {
            if (String.IsNullOrEmpty(label)) return true;
            if (LabelMatches(edge, label)) return true;

            await foreach (LabelMetadata current in _Repo.Label.ReadMany(edge.TenantGUID, edge.GraphGUID, null, edge.GUID, label, token: token).ConfigureAwait(false))
            {
                if (current != null) return true;
            }

            return false;
        }

        private async Task HydrateNodeSubordinates(Node node, CancellationToken token)
        {
            List<string> labels = new List<string>();
            await foreach (LabelMetadata label in _Repo.Label.ReadManyNode(node.TenantGUID, node.GraphGUID, node.GUID, token: token).ConfigureAwait(false))
            {
                if (label != null && !String.IsNullOrEmpty(label.Label)) labels.Add(label.Label);
            }

            List<TagMetadata> tags = new List<TagMetadata>();
            await foreach (TagMetadata tag in _Repo.Tag.ReadManyNode(node.TenantGUID, node.GraphGUID, node.GUID, token: token).ConfigureAwait(false))
            {
                if (tag != null) tags.Add(tag);
            }

            List<VectorMetadata> vectors = new List<VectorMetadata>();
            await foreach (VectorMetadata vector in _Repo.Vector.ReadManyNode(node.TenantGUID, node.GraphGUID, node.GUID, token: token).ConfigureAwait(false))
            {
                if (vector != null) vectors.Add(vector);
            }

            node.Labels = labels.Count > 0 ? labels : null;
            node.Tags = tags.Count > 0 ? TagMetadata.ToNameValueCollection(tags) : null;
            node.Vectors = vectors.Count > 0 ? vectors : null;
        }

        private async Task HydrateEdgeSubordinates(Edge edge, CancellationToken token)
        {
            List<string> labels = new List<string>();
            await foreach (LabelMetadata label in _Repo.Label.ReadManyEdge(edge.TenantGUID, edge.GraphGUID, edge.GUID, token: token).ConfigureAwait(false))
            {
                if (label != null && !String.IsNullOrEmpty(label.Label)) labels.Add(label.Label);
            }

            List<TagMetadata> tags = new List<TagMetadata>();
            await foreach (TagMetadata tag in _Repo.Tag.ReadManyEdge(edge.TenantGUID, edge.GraphGUID, edge.GUID, token: token).ConfigureAwait(false))
            {
                if (tag != null) tags.Add(tag);
            }

            List<VectorMetadata> vectors = new List<VectorMetadata>();
            await foreach (VectorMetadata vector in _Repo.Vector.ReadManyEdge(edge.TenantGUID, edge.GraphGUID, edge.GUID, token: token).ConfigureAwait(false))
            {
                if (vector != null) vectors.Add(vector);
            }

            edge.Labels = labels.Count > 0 ? labels : null;
            edge.Tags = tags.Count > 0 ? TagMetadata.ToNameValueCollection(tags) : null;
            edge.Vectors = vectors.Count > 0 ? vectors : null;
        }

        private static bool LabelMatches(Node node, string label)
        {
            if (String.IsNullOrEmpty(label)) return true;
            return node.Labels != null && node.Labels.Any(l => String.Equals(l, label, StringComparison.OrdinalIgnoreCase));
        }

        private static bool LabelMatches(Edge edge, string label)
        {
            if (String.IsNullOrEmpty(label)) return true;
            return edge.Labels != null && edge.Labels.Any(l => String.Equals(l, label, StringComparison.OrdinalIgnoreCase));
        }

        private static object GetOptionalValue(Dictionary<string, string> props, string key, Dictionary<string, object> parameters)
        {
            if (!props.TryGetValue(key, out string expression)) return null;
            return ResolveValue(expression, parameters);
        }

        private static object GetRequiredValue(Dictionary<string, string> props, Dictionary<string, object> parameters, params string[] keys)
        {
            foreach (string key in keys)
            {
                if (props.TryGetValue(key, out string expression)) return ResolveValue(expression, parameters);
            }

            throw new ArgumentException("Required property '" + keys[0] + "' is missing.");
        }

        private static string GetOptionalString(Dictionary<string, string> props, string key, Dictionary<string, object> parameters)
        {
            return GetOptionalValue(props, key, parameters)?.ToString();
        }

        private static string GetRequiredString(Dictionary<string, string> props, string key, Dictionary<string, object> parameters)
        {
            object value = GetRequiredValue(props, parameters, key);
            if (value == null || String.IsNullOrEmpty(value.ToString())) throw new ArgumentException("Required property '" + key + "' is empty.");
            return value.ToString();
        }

        private static Guid? GetOptionalGuid(Dictionary<string, string> props, Dictionary<string, object> parameters, params string[] keys)
        {
            foreach (string key in keys)
            {
                if (props.TryGetValue(key, out string expression))
                    return ToGuid(ResolveValue(expression, parameters));
            }

            return null;
        }

        private static Guid GetRequiredGuid(Dictionary<string, string> props, Dictionary<string, object> parameters, params string[] keys)
        {
            object value = GetRequiredValue(props, parameters, keys);
            return ToGuid(value);
        }

        private static object ResolveValue(string expression, Dictionary<string, object> parameters)
        {
            if (String.IsNullOrWhiteSpace(expression)) return null;
            expression = expression.Trim();

            if (expression.StartsWith("$"))
            {
                string key = expression.Substring(1);
                if (parameters == null || !parameters.TryGetValue(key, out object value))
                    throw new ArgumentException("Missing query parameter '" + key + "'.");
                return NormalizeJsonValue(value);
            }

            if (expression.StartsWith(Parser.ListExpressionPrefix, StringComparison.Ordinal))
            {
                string listJson = expression.Substring(Parser.ListExpressionPrefix.Length);
                List<string> itemExpressions = JsonSerializer.Deserialize<List<string>>(listJson) ?? new List<string>();
                return itemExpressions.Select(item => ResolveValue(item, parameters)).ToList();
            }

            if ((expression.StartsWith("'") && expression.EndsWith("'"))
                || (expression.StartsWith("\"") && expression.EndsWith("\"")))
            {
                return expression.Substring(1, expression.Length - 2).Replace("\\\"", "\"").Replace("\\'", "'");
            }

            if (Guid.TryParse(expression, out Guid guid)) return guid;
            if (Int64.TryParse(expression, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longValue)) return longValue;
            if (Double.TryParse(expression, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue)) return doubleValue;
            if (Boolean.TryParse(expression, out bool boolValue)) return boolValue;
            if (expression.Equals("null", StringComparison.OrdinalIgnoreCase)) return null;

            return expression;
        }

        private static object NormalizeJsonValue(object value)
        {
            if (value is JsonElement element)
            {
                switch (element.ValueKind)
                {
                    case JsonValueKind.String:
                        return element.GetString();
                    case JsonValueKind.Number:
                        if (element.TryGetInt64(out long longValue)) return longValue;
                        return element.GetDouble();
                    case JsonValueKind.True:
                        return true;
                    case JsonValueKind.False:
                        return false;
                    case JsonValueKind.Null:
                    case JsonValueKind.Undefined:
                        return null;
                    case JsonValueKind.Array:
                        return element.EnumerateArray().Select(e => NormalizeJsonValue(e)).ToList();
                    case JsonValueKind.Object:
                        return JsonSerializer.Deserialize<Dictionary<string, object>>(element.GetRawText());
                    default:
                        return JsonSerializer.Deserialize<object>(element.GetRawText());
                }
            }

            return value;
        }

        private static Guid ToGuid(object value)
        {
            if (value is Guid guid) return guid;
            if (value != null && Guid.TryParse(value.ToString(), out Guid parsed)) return parsed;
            throw new ArgumentException("Value '" + value + "' is not a GUID.");
        }

        private static List<float> ToFloatList(object value)
        {
            value = NormalizeJsonValue(value);
            if (value == null) throw new ArgumentNullException(nameof(value));

            if (value is List<float> floats) return floats;
            if (value is float[] floatArray) return floatArray.ToList();
            if (value is JsonElement element && element.ValueKind == JsonValueKind.Array)
                return element.EnumerateArray().Select(e => Convert.ToSingle(NormalizeJsonValue(e), CultureInfo.InvariantCulture)).ToList();

            if (value is IEnumerable enumerable && value is not string)
            {
                List<float> ret = new List<float>();
                foreach (object item in enumerable)
                {
                    ret.Add(Convert.ToSingle(NormalizeJsonValue(item), CultureInfo.InvariantCulture));
                }

                return ret;
            }

            throw new ArgumentException("Value must be a vector array.");
        }

        private sealed class PathState
        {
            internal Dictionary<string, object> Values { get; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            internal int HopCount { get; set; }

            internal PathState Clone()
            {
                PathState ret = new PathState();
                foreach (KeyValuePair<string, object> kvp in Values)
                {
                    ret.Values[kvp.Key] = kvp.Value;
                }

                ret.HopCount = HopCount;
                return ret;
            }
        }

        private sealed class OrderValueComparer : IComparer<object>
        {
            internal static OrderValueComparer Instance { get; } = new OrderValueComparer();

            public int Compare(object x, object y)
            {
                x = NormalizeJsonValue(x);
                y = NormalizeJsonValue(y);

                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                if (IsNumeric(x) && IsNumeric(y))
                    return Convert.ToDecimal(x, CultureInfo.InvariantCulture).CompareTo(Convert.ToDecimal(y, CultureInfo.InvariantCulture));

                if (Guid.TryParse(x.ToString(), out Guid leftGuid) && Guid.TryParse(y.ToString(), out Guid rightGuid))
                    return leftGuid.CompareTo(rightGuid);

                return StringComparer.Ordinal.Compare(
                    Convert.ToString(x, CultureInfo.InvariantCulture),
                    Convert.ToString(y, CultureInfo.InvariantCulture));
            }
        }

        #endregion
    }
}
