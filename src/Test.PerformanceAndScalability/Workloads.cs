namespace Test.PerformanceAndScalability
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;
    using LiteGraph.Indexing.Vector;

    internal static class WorkloadCatalog
    {
        public static List<BenchmarkScenario> Build(BenchmarkOptions options)
        {
            HashSet<string> selected = ExpandWorkloads(options.Workloads);
            List<BenchmarkScenario> scenarios = new List<BenchmarkScenario>();

            if (selected.Contains("ingest"))
            {
                scenarios.Add(Scenario("ingest.node.single", "ingest", "Create one node per operation.", CreateSingleNodeAsync));
                scenarios.Add(Scenario("ingest.node.bulk", "ingest", "Create a batch of nodes per operation.", CreateBulkNodesAsync));
                scenarios.Add(Scenario("ingest.edge.single", "ingest", "Create one edge per operation.", CreateSingleEdgeAsync));
                scenarios.Add(Scenario("ingest.vector.single", "ingest", "Create one node vector per operation.", CreateSingleVectorAsync));
            }

            if (selected.Contains("reads"))
            {
                scenarios.Add(Scenario("read.node.guid", "reads", "Read a node by GUID.", ReadNodeByGuidAsync));
                scenarios.Add(Scenario("read.node.guids", "reads", "Read a batch of nodes by GUID.", ReadNodesByGuidsAsync));
                scenarios.Add(Scenario("read.edge.guid", "reads", "Read an edge by GUID.", ReadEdgeByGuidAsync));
                scenarios.Add(Scenario("read.vector.guid", "reads", "Read a vector by GUID.", ReadVectorByGuidAsync));
                scenarios.Add(Scenario("read.exists", "reads", "Run node, edge, graph, and vector existence checks.", ExistsAsync));
            }

            if (selected.Contains("search"))
            {
                scenarios.Add(Scenario("search.node.name", "search", "Read first node by name.", SearchNodeByNameAsync));
                scenarios.Add(Scenario("search.node.label", "search", "Read nodes by label.", SearchNodeByLabelAsync));
                scenarios.Add(Scenario("search.node.tag", "search", "Read nodes by tag.", SearchNodeByTagAsync));
                scenarios.Add(Scenario("enumerate.nodes", "search", "Enumerate nodes with a bounded read.", EnumerateNodesAsync));
                scenarios.Add(Scenario("enumerate.edges", "search", "Enumerate edges with a bounded read.", EnumerateEdgesAsync));
            }

            if (selected.Contains("traversal"))
            {
                scenarios.Add(Scenario("traversal.neighbors", "traversal", "Read neighbors for hot or random nodes.", ReadNeighborsAsync));
                scenarios.Add(Scenario("traversal.edges.from", "traversal", "Read outgoing edges for a node.", ReadEdgesFromNodeAsync));
                scenarios.Add(Scenario("traversal.parents.children", "traversal", "Read parents and children for a node.", ReadParentsChildrenAsync));
                scenarios.Add(Scenario("traversal.routes", "traversal", "Read routes between two nodes.", ReadRoutesAsync));
                scenarios.Add(Scenario("traversal.subgraph", "traversal", "Read a bounded subgraph.", ReadSubgraphAsync));
            }

            if (selected.Contains("query"))
            {
                scenarios.Add(Scenario("query.match.node", "query", "Native query node match.", QueryNodeMatchAsync));
                scenarios.Add(Scenario("query.match.edge", "query", "Native query one-hop edge match.", QueryEdgeMatchAsync));
                scenarios.Add(Scenario("query.aggregate", "query", "Native query aggregate count.", QueryAggregateAsync));
            }

            if (selected.Contains("vector"))
            {
                scenarios.Add(Scenario("vector.search", "vector", "Vector search over node vectors.", VectorSearchAsync));
                scenarios.Add(Scenario("vector.update", "vector", "Read and update an existing vector.", UpdateVectorAsync));
                scenarios.Add(Scenario("vector.index.rebuild", "vector", "Enable and rebuild graph vector index.", RebuildVectorIndexAsync, runOnce: true));
            }

            if (selected.Contains("transactions"))
            {
                scenarios.Add(Scenario("transaction.create.nodes", "transactions", "Create nodes in graph-scoped transactions.", TransactionCreateNodesAsync));
                scenarios.Add(Scenario("transaction.rollback", "transactions", "Force a rollback and verify partial state is not committed.", TransactionRollbackAsync));
            }

            if (selected.Contains("updates"))
            {
                scenarios.Add(Scenario("update.node", "updates", "Read and update a node payload.", UpdateNodeAsync));
                scenarios.Add(Scenario("update.edge", "updates", "Read and update an edge payload.", UpdateEdgeAsync));
            }

            if (selected.Contains("deletes"))
            {
                scenarios.Add(Scenario("delete.node.with-setup", "deletes", "Create then delete a node to isolate destructive delete load.", DeleteNodeWithSetupAsync));
                scenarios.Add(Scenario("delete.edge.with-setup", "deletes", "Create then delete an edge to isolate destructive delete load.", DeleteEdgeWithSetupAsync));
            }

            if (selected.Contains("maintenance"))
            {
                scenarios.Add(Scenario("maintenance.graph.statistics", "maintenance", "Read graph statistics.", GraphStatisticsAsync));
                scenarios.Add(Scenario("maintenance.flush", "maintenance", "Flush repository state.", FlushAsync, runOnce: true));
            }

            if (selected.Contains("mixed") || selected.Contains("stress"))
            {
                scenarios.Add(Scenario("mixed." + options.OperationMix, "mixed", "Weighted mixed workload profile.", MixedAsync));
            }

            return scenarios;
        }

        private static BenchmarkScenario Scenario(
            string name,
            string category,
            string description,
            Func<BenchmarkContext, WorkerContext, CancellationToken, Task<OperationOutcome>> operation,
            bool runOnce = false)
        {
            return new BenchmarkScenario
            {
                Name = name,
                Category = category,
                Description = description,
                RunOnce = runOnce,
                Operation = operation
            };
        }

        private static HashSet<string> ExpandWorkloads(List<string> requested)
        {
            HashSet<string> ret = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (requested.Count == 0 || requested.Contains("all"))
            {
                foreach (string workload in new[]
                {
                    "ingest",
                    "reads",
                    "search",
                    "traversal",
                    "query",
                    "vector",
                    "transactions",
                    "updates",
                    "deletes",
                    "maintenance",
                    "mixed"
                })
                {
                    ret.Add(workload);
                }
            }
            else
            {
                foreach (string workload in requested)
                {
                    if (workload == "stress" || workload == "soak")
                    {
                        ret.Add("mixed");
                        ret.Add(workload);
                    }
                    else
                    {
                        ret.Add(workload);
                    }
                }
            }

            return ret;
        }

        private static async Task<OperationOutcome> CreateSingleNodeAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            Node node = DatasetGenerator.BuildNode(context.Options, graph, worker.OperationIndex, worker.Random);
            Node created = await context.Client.Node.Create(node, token).ConfigureAwait(false);
            return created != null ? OperationOutcome.Success() : OperationOutcome.Incorrect();
        }

        private static async Task<OperationOutcome> CreateBulkNodesAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            List<Node> nodes = new List<Node>(context.Options.BatchSize);
            for (int i = 0; i < context.Options.BatchSize; i++)
            {
                nodes.Add(DatasetGenerator.BuildNode(context.Options, graph, (worker.OperationIndex * context.Options.BatchSize) + i, worker.Random));
            }

            List<Node> created = await context.Client.Node.CreateMany(graph.Tenant.GUID, graph.Graph.GUID, nodes, BulkCreateReturnModeEnum.Minimal, token).ConfigureAwait(false);
            return OperationOutcome.Success(nodes.Count, created.Count);
        }

        private static async Task<OperationOutcome> CreateSingleEdgeAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            Edge edge = DatasetGenerator.BuildEdge(context.Options, graph, worker.OperationIndex, worker.Random);
            Edge created = await context.Client.Edge.Create(edge, token).ConfigureAwait(false);
            return created != null ? OperationOutcome.Success() : OperationOutcome.Incorrect();
        }

        private static async Task<OperationOutcome> CreateSingleVectorAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            VectorMetadata vector = DatasetGenerator.BuildVector(context.Options, graph, worker.OperationIndex, worker.Random);
            VectorMetadata created = await context.Client.Vector.Create(vector, token).ConfigureAwait(false);
            return created != null ? OperationOutcome.Success() : OperationOutcome.Incorrect();
        }

        private static async Task<OperationOutcome> ReadNodeByGuidAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            Node node = graph.PickNode(worker.Random);
            Node read = await context.Client.Node.ReadByGuid(graph.Tenant.GUID, graph.Graph.GUID, node.GUID, false, false, token).ConfigureAwait(false);
            return read != null ? OperationOutcome.Success(resultCount: 1) : OperationOutcome.Incorrect();
        }

        private static async Task<OperationOutcome> ReadNodesByGuidsAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            List<Guid> guids = PickNodeGuids(graph, worker.Random, Math.Min(context.Options.BatchSize, graph.Nodes.Count));
            int count = await AsyncEnumerableHelpers.CountUpToAsync(context.Client.Node.ReadByGuids(graph.Tenant.GUID, guids, false, false, token), guids.Count, token).ConfigureAwait(false);
            return OperationOutcome.Success(guids.Count, count);
        }

        private static async Task<OperationOutcome> ReadEdgeByGuidAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            Edge? edge = graph.PickEdge(worker.Random);
            if (edge == null) return OperationOutcome.Success(resultCount: 0);
            Edge read = await context.Client.Edge.ReadByGuid(graph.Tenant.GUID, graph.Graph.GUID, edge.GUID, false, false, token).ConfigureAwait(false);
            return read != null ? OperationOutcome.Success(resultCount: 1) : OperationOutcome.Incorrect();
        }

        private static async Task<OperationOutcome> ReadVectorByGuidAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            VectorMetadata? vector = graph.PickVector(worker.Random);
            if (vector == null) return OperationOutcome.Success(resultCount: 0);
            VectorMetadata read = await context.Client.Vector.ReadByGuid(graph.Tenant.GUID, vector.GUID, token).ConfigureAwait(false);
            return read != null ? OperationOutcome.Success(resultCount: 1) : OperationOutcome.Incorrect();
        }

        private static async Task<OperationOutcome> ExistsAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            Node node = graph.PickNode(worker.Random);
            Edge? edge = graph.PickEdge(worker.Random);
            VectorMetadata? vector = graph.PickVector(worker.Random);

            bool nodeExists = await context.Client.Node.ExistsByGuid(graph.Tenant.GUID, node.GUID, token).ConfigureAwait(false);
            bool graphExists = await context.Client.Graph.ExistsByGuid(graph.Tenant.GUID, graph.Graph.GUID, token).ConfigureAwait(false);
            bool edgeExists = edge == null || await context.Client.Edge.ExistsByGuid(graph.Tenant.GUID, graph.Graph.GUID, edge.GUID, token).ConfigureAwait(false);
            bool vectorExists = vector == null || await context.Client.Vector.ExistsByGuid(graph.Tenant.GUID, vector.GUID, token).ConfigureAwait(false);

            return nodeExists && graphExists && edgeExists && vectorExists
                ? OperationOutcome.Success(items: 4, resultCount: 4)
                : OperationOutcome.Incorrect(items: 4);
        }

        private static async Task<OperationOutcome> SearchNodeByNameAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            Node node = graph.PickNode(worker.Random);
            Node read = await context.Client.Node.ReadFirst(graph.Tenant.GUID, graph.Graph.GUID, name: node.Name, includeData: false, includeSubordinates: false, token: token).ConfigureAwait(false);
            return read != null ? OperationOutcome.Success(resultCount: 1) : OperationOutcome.Incorrect();
        }

        private static async Task<OperationOutcome> SearchNodeByLabelAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            int count = await AsyncEnumerableHelpers.CountUpToAsync(
                context.Client.Node.ReadMany(graph.Tenant.GUID, graph.Graph.GUID, labels: new List<string> { "PerfNode" }, includeData: false, includeSubordinates: false, token: token),
                100,
                token).ConfigureAwait(false);
            return OperationOutcome.Success(resultCount: count);
        }

        private static async Task<OperationOutcome> SearchNodeByTagAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            NameValueCollection tags = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase)
            {
                ["hot"] = "true"
            };
            int count = await AsyncEnumerableHelpers.CountUpToAsync(
                context.Client.Node.ReadMany(graph.Tenant.GUID, graph.Graph.GUID, tags: tags, includeData: false, includeSubordinates: false, token: token),
                100,
                token).ConfigureAwait(false);
            return OperationOutcome.Success(resultCount: count);
        }

        private static async Task<OperationOutcome> EnumerateNodesAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            int count = await AsyncEnumerableHelpers.CountUpToAsync(
                context.Client.Node.ReadAllInGraph(graph.Tenant.GUID, graph.Graph.GUID, skip: worker.Random.Next(Math.Min(10, graph.Nodes.Count)), includeData: false, includeSubordinates: false, token: token),
                250,
                token).ConfigureAwait(false);
            return OperationOutcome.Success(resultCount: count);
        }

        private static async Task<OperationOutcome> EnumerateEdgesAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            int count = await AsyncEnumerableHelpers.CountUpToAsync(
                context.Client.Edge.ReadAllInGraph(graph.Tenant.GUID, graph.Graph.GUID, skip: graph.Edges.Count == 0 ? 0 : worker.Random.Next(Math.Min(10, graph.Edges.Count)), includeData: false, includeSubordinates: false, token: token),
                250,
                token).ConfigureAwait(false);
            return OperationOutcome.Success(resultCount: count);
        }

        private static async Task<OperationOutcome> ReadNeighborsAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            Guid nodeGuid = graph.PickHotOrRandomNodeGuid(worker.Random);
            int count = await AsyncEnumerableHelpers.CountUpToAsync(context.Client.Node.ReadNeighbors(graph.Tenant.GUID, graph.Graph.GUID, nodeGuid, token: token), 100, token).ConfigureAwait(false);
            return OperationOutcome.Success(resultCount: count);
        }

        private static async Task<OperationOutcome> ReadEdgesFromNodeAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            Guid nodeGuid = graph.PickHotOrRandomNodeGuid(worker.Random);
            int count = await AsyncEnumerableHelpers.CountUpToAsync(context.Client.Edge.ReadEdgesFromNode(graph.Tenant.GUID, graph.Graph.GUID, nodeGuid, token: token), 100, token).ConfigureAwait(false);
            return OperationOutcome.Success(resultCount: count);
        }

        private static async Task<OperationOutcome> ReadParentsChildrenAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            Guid nodeGuid = graph.PickHotOrRandomNodeGuid(worker.Random);
            int parents = await AsyncEnumerableHelpers.CountUpToAsync(context.Client.Node.ReadParents(graph.Tenant.GUID, graph.Graph.GUID, nodeGuid, token: token), 100, token).ConfigureAwait(false);
            int children = await AsyncEnumerableHelpers.CountUpToAsync(context.Client.Node.ReadChildren(graph.Tenant.GUID, graph.Graph.GUID, nodeGuid, token: token), 100, token).ConfigureAwait(false);
            return OperationOutcome.Success(items: 2, resultCount: parents + children);
        }

        private static async Task<OperationOutcome> ReadRoutesAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            if (graph.Nodes.Count < 2) return OperationOutcome.Success(resultCount: 0);
            Node from = graph.Nodes[worker.Random.Next(graph.Nodes.Count)];
            Node to = graph.Nodes[worker.Random.Next(graph.Nodes.Count)];
            int count = await AsyncEnumerableHelpers.CountUpToAsync(
                context.Client.Node.ReadRoutes(SearchTypeEnum.DepthFirstSearch, graph.Tenant.GUID, graph.Graph.GUID, from.GUID, to.GUID, token: token),
                25,
                token).ConfigureAwait(false);
            return OperationOutcome.Success(resultCount: count);
        }

        private static async Task<OperationOutcome> ReadSubgraphAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            Guid nodeGuid = graph.PickHotOrRandomNodeGuid(worker.Random);
            SearchResult result = await context.Client.Graph.GetSubgraph(graph.Tenant.GUID, graph.Graph.GUID, nodeGuid, maxDepth: 2, maxNodes: 100, maxEdges: 200, includeData: false, includeSubordinates: false, token: token).ConfigureAwait(false);
            int count = (result.Nodes?.Count ?? 0) + (result.Edges?.Count ?? 0);
            return OperationOutcome.Success(resultCount: count);
        }

        private static async Task<OperationOutcome> QueryNodeMatchAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            Node node = graph.PickNode(worker.Random);
            GraphQueryResult result = await context.Client.Query.Execute(graph.Tenant.GUID, graph.Graph.GUID, new GraphQueryRequest
            {
                Query = "MATCH (n:PerfNode) WHERE n.guid = $nodeGuid RETURN n LIMIT 10",
                Parameters = new Dictionary<string, object> { ["nodeGuid"] = node.GUID },
                IncludeProfile = context.Options.IncludeQueryProfile,
                MaxResults = 10,
                TimeoutSeconds = (int)Math.Max(1, context.Options.Timeout.TotalSeconds)
            }, token).ConfigureAwait(false);
            RecordQueryProfile(context, result);
            return OperationOutcome.Success(resultCount: result.RowCount);
        }

        private static async Task<OperationOutcome> QueryEdgeMatchAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            Node node = graph.PickNode(worker.Random);
            GraphQueryResult result = await context.Client.Query.Execute(graph.Tenant.GUID, graph.Graph.GUID, new GraphQueryRequest
            {
                Query = "MATCH (a)-[e:LINKS]->(b) WHERE a.guid = $start RETURN a, e, b LIMIT 25",
                Parameters = new Dictionary<string, object> { ["start"] = node.GUID },
                IncludeProfile = context.Options.IncludeQueryProfile,
                MaxResults = 25,
                TimeoutSeconds = (int)Math.Max(1, context.Options.Timeout.TotalSeconds)
            }, token).ConfigureAwait(false);
            RecordQueryProfile(context, result);
            return OperationOutcome.Success(resultCount: result.RowCount);
        }

        private static async Task<OperationOutcome> QueryAggregateAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            GraphQueryResult result = await context.Client.Query.Execute(graph.Tenant.GUID, graph.Graph.GUID, new GraphQueryRequest
            {
                Query = "MATCH (n:PerfNode) RETURN COUNT(*) AS total",
                IncludeProfile = context.Options.IncludeQueryProfile,
                MaxResults = 10,
                TimeoutSeconds = (int)Math.Max(1, context.Options.Timeout.TotalSeconds)
            }, token).ConfigureAwait(false);
            RecordQueryProfile(context, result);
            return OperationOutcome.Success(resultCount: result.RowCount);
        }

        private static async Task<OperationOutcome> VectorSearchAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            VectorMetadata? vector = graph.PickVector(worker.Random);
            if (vector == null) return OperationOutcome.Success(resultCount: 0);

            VectorSearchRequest request = new VectorSearchRequest
            {
                TenantGUID = graph.Tenant.GUID,
                GraphGUID = graph.Graph.GUID,
                Domain = VectorSearchDomainEnum.Node,
                SearchType = VectorSearchTypeEnum.CosineSimilarity,
                TopK = context.Options.VectorTopK,
                Embeddings = vector.Vectors
            };

            int count = await AsyncEnumerableHelpers.CountUpToAsync(context.Client.Vector.Search(request, token), context.Options.VectorTopK, token).ConfigureAwait(false);
            return OperationOutcome.Success(resultCount: count);
        }

        private static async Task<OperationOutcome> UpdateVectorAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            VectorMetadata? vector = graph.PickVector(worker.Random);
            if (vector == null) return OperationOutcome.Success(resultCount: 0);

            VectorMetadata read = await context.Client.Vector.ReadByGuid(graph.Tenant.GUID, vector.GUID, token).ConfigureAwait(false);
            read.Content = "updated " + worker.OperationIndex;
            read.Vectors = DatasetGenerator.BuildEmbedding(context.Options.VectorDimensions, (int)(worker.OperationIndex % int.MaxValue));
            VectorMetadata updated = await context.Client.Vector.Update(read, token).ConfigureAwait(false);
            return updated != null ? OperationOutcome.Success(resultCount: 1) : OperationOutcome.Incorrect();
        }

        private static async Task<OperationOutcome> RebuildVectorIndexAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            VectorIndexConfiguration configuration = new VectorIndexConfiguration
            {
                VectorIndexType = VectorIndexTypeEnum.HnswRam,
                VectorDimensionality = context.Options.VectorDimensions,
                VectorIndexThreshold = 1
            };

            await context.Client.Graph.EnableVectorIndexing(graph.Tenant.GUID, graph.Graph.GUID, configuration, token).ConfigureAwait(false);
            await context.Client.Graph.RebuildVectorIndex(graph.Tenant.GUID, graph.Graph.GUID, token).ConfigureAwait(false);
            VectorIndexStatistics? stats = await context.Client.Graph.GetVectorIndexStatistics(graph.Tenant.GUID, graph.Graph.GUID, token).ConfigureAwait(false);
            return OperationOutcome.Success(resultCount: stats?.VectorCount ?? 0);
        }

        private static async Task<OperationOutcome> TransactionCreateNodesAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            TransactionRequestBuilder builder = context.Client.Transaction.CreateRequestBuilder()
                .WithMaxOperations(Math.Max(context.Options.TransactionSize, 1))
                .WithTimeoutSeconds((int)Math.Max(1, context.Options.Timeout.TotalSeconds));

            for (int i = 0; i < context.Options.TransactionSize; i++)
            {
                builder.CreateNode(new Node
                {
                    GUID = Guid.NewGuid(),
                    Name = "transaction-node-" + worker.OperationIndex + "-" + i
                });
            }

            TransactionResult result = await context.Client.Transaction.Execute(graph.Tenant.GUID, graph.Graph.GUID, builder.Build(), token).ConfigureAwait(false);
            if (!result.Success && token.IsCancellationRequested) token.ThrowIfCancellationRequested();
            return result.Success
                ? OperationOutcome.Success(context.Options.TransactionSize, result.Operations.Count)
                : OperationOutcome.Incorrect(context.Options.TransactionSize, result.Operations.Count, result.Error);
        }

        private static async Task<OperationOutcome> TransactionRollbackAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            Node node = new Node
            {
                GUID = Guid.NewGuid(),
                Name = "rollback-node-" + worker.OperationIndex
            };
            Guid missingNodeGuid = Guid.NewGuid();

            TransactionRequest request = context.Client.Transaction.CreateRequestBuilder()
                .WithMaxOperations(2)
                .CreateNode(node)
                .UpdateNode(new Node { Name = "missing-node-update" }, missingNodeGuid)
                .Build();

            try
            {
                TransactionResult result = await context.Client.Transaction.Execute(graph.Tenant.GUID, graph.Graph.GUID, request, token).ConfigureAwait(false);
                if (!result.Success && token.IsCancellationRequested) token.ThrowIfCancellationRequested();
                bool exists = await context.Client.Node.ExistsByGuid(graph.Tenant.GUID, node.GUID, token).ConfigureAwait(false);
                return !result.Success && result.RolledBack && !exists ? OperationOutcome.Success(items: 2) : OperationOutcome.Incorrect(items: 2);
            }
            catch
            {
                bool exists = await context.Client.Node.ExistsByGuid(graph.Tenant.GUID, node.GUID, token).ConfigureAwait(false);
                return !exists ? OperationOutcome.Success(items: 2) : OperationOutcome.Incorrect(items: 2);
            }
        }

        private static async Task<OperationOutcome> UpdateNodeAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            Node node = graph.PickNode(worker.Random);
            Node read = await context.Client.Node.ReadByGuid(graph.Tenant.GUID, graph.Graph.GUID, node.GUID, true, false, token).ConfigureAwait(false);
            read.Name = "updated-node-" + worker.OperationIndex;
            read.Data = new Dictionary<string, object> { ["updated"] = worker.OperationIndex, ["kind"] = "node-update" };
            Node updated = await context.Client.Node.Update(read, token).ConfigureAwait(false);
            return updated != null ? OperationOutcome.Success(resultCount: 1) : OperationOutcome.Incorrect();
        }

        private static async Task<OperationOutcome> UpdateEdgeAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            Edge? edge = graph.PickEdge(worker.Random);
            if (edge == null) return OperationOutcome.Success(resultCount: 0);
            Edge read = await context.Client.Edge.ReadByGuid(graph.Tenant.GUID, graph.Graph.GUID, edge.GUID, true, false, token).ConfigureAwait(false);
            read.Name = "updated-edge-" + worker.OperationIndex;
            read.Cost = (int)(worker.OperationIndex % 100);
            read.Data = new Dictionary<string, object> { ["updated"] = worker.OperationIndex, ["kind"] = "edge-update" };
            Edge updated = await context.Client.Edge.Update(read, token).ConfigureAwait(false);
            return updated != null ? OperationOutcome.Success(resultCount: 1) : OperationOutcome.Incorrect();
        }

        private static async Task<OperationOutcome> DeleteNodeWithSetupAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            Node node = DatasetGenerator.BuildNode(context.Options, graph, worker.OperationIndex, worker.Random);
            Node created = await context.Client.Node.Create(node, token).ConfigureAwait(false);
            await context.Client.Node.DeleteByGuid(graph.Tenant.GUID, graph.Graph.GUID, created.GUID, token).ConfigureAwait(false);
            bool exists = await context.Client.Node.ExistsByGuid(graph.Tenant.GUID, created.GUID, token).ConfigureAwait(false);
            return !exists ? OperationOutcome.Success() : OperationOutcome.Incorrect();
        }

        private static async Task<OperationOutcome> DeleteEdgeWithSetupAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            Edge edge = DatasetGenerator.BuildEdge(context.Options, graph, worker.OperationIndex, worker.Random);
            Edge created = await context.Client.Edge.Create(edge, token).ConfigureAwait(false);
            await context.Client.Edge.DeleteByGuid(graph.Tenant.GUID, graph.Graph.GUID, created.GUID, token).ConfigureAwait(false);
            bool exists = await context.Client.Edge.ExistsByGuid(graph.Tenant.GUID, graph.Graph.GUID, created.GUID, token).ConfigureAwait(false);
            return !exists ? OperationOutcome.Success() : OperationOutcome.Incorrect();
        }

        private static async Task<OperationOutcome> GraphStatisticsAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            GraphDataset graph = context.Dataset.PickGraph(worker.Random);
            GraphStatistics stats = await context.Client.Graph.GetStatistics(graph.Tenant.GUID, graph.Graph.GUID, token).ConfigureAwait(false);
            return OperationOutcome.Success(resultCount: stats == null ? 0 : 1);
        }

        private static async Task<OperationOutcome> FlushAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            await context.Client.FlushAsync(token).ConfigureAwait(false);
            return OperationOutcome.Success();
        }

        private static async Task<OperationOutcome> MixedAsync(BenchmarkContext context, WorkerContext worker, CancellationToken token)
        {
            double roll = worker.Random.NextDouble();
            string mix = context.Options.OperationMix.ToLowerInvariant();

            if (mix == "read-heavy")
            {
                if (roll < 0.70) return await ReadNodeByGuidAsync(context, worker, token).ConfigureAwait(false);
                if (roll < 0.90) return await SearchNodeByLabelAsync(context, worker, token).ConfigureAwait(false);
                return await CreateSingleNodeAsync(context, worker, token).ConfigureAwait(false);
            }

            if (mix == "write-heavy")
            {
                if (roll < 0.45) return await CreateSingleNodeAsync(context, worker, token).ConfigureAwait(false);
                if (roll < 0.80) return await CreateSingleEdgeAsync(context, worker, token).ConfigureAwait(false);
                return await ReadNodeByGuidAsync(context, worker, token).ConfigureAwait(false);
            }

            if (mix == "vector-heavy")
            {
                if (roll < 0.60) return await VectorSearchAsync(context, worker, token).ConfigureAwait(false);
                if (roll < 0.80) return await CreateSingleVectorAsync(context, worker, token).ConfigureAwait(false);
                return await ReadNodeByGuidAsync(context, worker, token).ConfigureAwait(false);
            }

            if (mix == "transaction-heavy")
            {
                if (roll < 0.70) return await TransactionCreateNodesAsync(context, worker, token).ConfigureAwait(false);
                return await ReadNodeByGuidAsync(context, worker, token).ConfigureAwait(false);
            }

            if (mix == "hotspot")
            {
                if (roll < 0.50) return await ReadNeighborsAsync(context, worker, token).ConfigureAwait(false);
                if (roll < 0.80) return await ReadEdgesFromNodeAsync(context, worker, token).ConfigureAwait(false);
                return await UpdateNodeAsync(context, worker, token).ConfigureAwait(false);
            }

            if (roll < 0.45) return await ReadNodeByGuidAsync(context, worker, token).ConfigureAwait(false);
            if (roll < 0.65) return await SearchNodeByLabelAsync(context, worker, token).ConfigureAwait(false);
            if (roll < 0.80) return await ReadNeighborsAsync(context, worker, token).ConfigureAwait(false);
            if (roll < 0.90) return await CreateSingleNodeAsync(context, worker, token).ConfigureAwait(false);
            return await VectorSearchAsync(context, worker, token).ConfigureAwait(false);
        }

        private static List<Guid> PickNodeGuids(GraphDataset graph, Random random, int count)
        {
            List<Guid> ret = new List<Guid>(count);
            for (int i = 0; i < count; i++) ret.Add(graph.PickNode(random).GUID);
            return ret;
        }

        private static void RecordQueryProfile(BenchmarkContext context, GraphQueryResult result)
        {
            if (result.ExecutionProfile == null) return;
            context.QueryProfiles.Add(new QueryProfileSample
            {
                Scenario = context.Telemetry.CurrentScenario,
                ParseTimeMs = result.ExecutionProfile.ParseTimeMs,
                PlanTimeMs = result.ExecutionProfile.PlanTimeMs,
                ExecuteTimeMs = result.ExecutionProfile.ExecuteTimeMs,
                RepositoryTimeMs = result.ExecutionProfile.RepositoryTimeMs,
                RepositoryOperationCount = result.ExecutionProfile.RepositoryOperationCount,
                VectorSearchTimeMs = result.ExecutionProfile.VectorSearchTimeMs,
                VectorSearchCount = result.ExecutionProfile.VectorSearchCount,
                TransactionTimeMs = result.ExecutionProfile.TransactionTimeMs,
                TotalTimeMs = result.ExecutionProfile.TotalTimeMs,
                RowCount = result.RowCount
            });
        }
    }
}
