namespace Test.PerformanceAndScalability
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LiteGraph;

    internal static class DatasetGenerator
    {
        public static async Task<DatasetState> GenerateAsync(BenchmarkContext context, CancellationToken token)
        {
            BenchmarkOptions options = context.Options;
            DatasetState state = new DatasetState();
            Stopwatch stopwatch = Stopwatch.StartNew();
            Random random = new Random(options.Seed);

            for (int tenantIndex = 0; tenantIndex < options.Tenants; tenantIndex++)
            {
                TenantMetadata tenant = await context.Client.Tenant.Create(new TenantMetadata
                {
                    GUID = DeterministicGuid(options.Seed, "tenant", tenantIndex),
                    Name = "perf-tenant-" + tenantIndex
                }, token).ConfigureAwait(false);

                for (int graphIndex = 0; graphIndex < options.GraphsPerTenant; graphIndex++)
                {
                    Graph graph = await context.Client.Graph.Create(new Graph
                    {
                        TenantGUID = tenant.GUID,
                        GUID = DeterministicGuid(options.Seed, "graph", tenantIndex, graphIndex),
                        Name = "perf-graph-" + tenantIndex + "-" + graphIndex,
                        Labels = new List<string> { "PerfGraph" },
                        Tags = Tags(("profile", options.Profile), ("topology", options.Topology)),
                        Data = BuildPayload(options.PayloadSize, tenantIndex, graphIndex, -1, "graph")
                    }, token).ConfigureAwait(false);

                    GraphDataset graphDataset = new GraphDataset
                    {
                        Tenant = tenant,
                        Graph = graph
                    };

                    List<Node> nodes = new List<Node>(options.NodesPerGraph);
                    for (int nodeIndex = 0; nodeIndex < options.NodesPerGraph; nodeIndex++)
                    {
                        Guid nodeGuid = DeterministicGuid(options.Seed, "node", tenantIndex, graphIndex, nodeIndex);
                        nodes.Add(new Node
                        {
                            TenantGUID = tenant.GUID,
                            GraphGUID = graph.GUID,
                            GUID = nodeGuid,
                            Name = "node-" + tenantIndex + "-" + graphIndex + "-" + nodeIndex,
                            Labels = Labels("PerfNode", "Bucket" + (nodeIndex % Math.Max(1, options.LabelsPerNode))),
                            Tags = BuildTags(options.TagsPerNode, tenantIndex, graphIndex, nodeIndex, nodeIndex < Math.Max(1, options.NodesPerGraph / 20)),
                            Data = BuildPayload(options.PayloadSize, tenantIndex, graphIndex, nodeIndex, "node")
                        });
                    }

                    foreach (List<Node> chunk in Chunk(nodes, options.BatchSize))
                    {
                        List<Node> created = await context.Client.Node.CreateMany(tenant.GUID, graph.GUID, chunk, BulkCreateReturnModeEnum.Minimal, token).ConfigureAwait(false);
                        graphDataset.Nodes.AddRange(created.Count == 0 ? chunk : created);
                    }

                    List<Edge> edges = BuildEdges(options, random, tenant.GUID, graph.GUID, graphDataset.Nodes, tenantIndex, graphIndex);
                    foreach (List<Edge> chunk in Chunk(edges, options.BatchSize))
                    {
                        List<Edge> created = await context.Client.Edge.CreateMany(tenant.GUID, graph.GUID, chunk, BulkCreateReturnModeEnum.Minimal, token).ConfigureAwait(false);
                        graphDataset.Edges.AddRange(created.Count == 0 ? chunk : created);
                    }

                    List<VectorMetadata> vectors = BuildVectors(options, tenant.GUID, graph.GUID, graphDataset.Nodes, tenantIndex, graphIndex);
                    foreach (List<VectorMetadata> chunk in Chunk(vectors, options.BatchSize))
                    {
                        List<VectorMetadata> created = await context.Client.Vector.CreateMany(tenant.GUID, chunk, BulkCreateReturnModeEnum.Minimal, token).ConfigureAwait(false);
                        graphDataset.Vectors.AddRange(created.Count == 0 ? chunk : created);
                    }

                    graphDataset.RouteFixtures.Add(await CreateRouteFixtureAsync(context, options, tenant, graph, tenantIndex, graphIndex, token).ConfigureAwait(false));

                    foreach (Node hot in graphDataset.Nodes.Take(Math.Max(1, graphDataset.Nodes.Count / 20)))
                    {
                        graphDataset.HotNodeGuids.Add(hot.GUID);
                    }

                    state.Graphs.Add(graphDataset);
                }
            }

            stopwatch.Stop();

            int totalNodes = state.Graphs.Sum(g => g.Nodes.Count);
            int totalEdges = state.Graphs.Sum(g => g.Edges.Count);
            int totalRouteNodes = state.Graphs.Sum(g => g.RouteFixtures.Sum(f => f.NodeGuids.Count));
            int totalRouteEdges = state.Graphs.Sum(g => g.RouteFixtures.Sum(f => f.EdgeGuids.Count));
            Dictionary<Guid, int> degree = new Dictionary<Guid, int>();
            foreach (GraphDataset graph in state.Graphs)
            {
                foreach (Node node in graph.Nodes) degree[node.GUID] = 0;
                foreach (Edge edge in graph.Edges)
                {
                    degree[edge.From] = degree.TryGetValue(edge.From, out int from) ? from + 1 : 1;
                    degree[edge.To] = degree.TryGetValue(edge.To, out int to) ? to + 1 : 1;
                }

                foreach (RouteFixture fixture in graph.RouteFixtures)
                {
                    foreach (Guid nodeGuid in fixture.NodeGuids)
                    {
                        if (!degree.ContainsKey(nodeGuid)) degree[nodeGuid] = 0;
                    }

                    for (int i = 0; i < fixture.NodeGuids.Count - 1; i++)
                    {
                        Guid from = fixture.NodeGuids[i];
                        Guid to = fixture.NodeGuids[i + 1];
                        degree[from] = degree.TryGetValue(from, out int fromDegree) ? fromDegree + 1 : 1;
                        degree[to] = degree.TryGetValue(to, out int toDegree) ? toDegree + 1 : 1;
                    }
                }
            }

            state.Metadata = new DatasetMetadata
            {
                Tenants = options.Tenants,
                Graphs = state.Graphs.Count,
                Nodes = totalNodes + totalRouteNodes,
                Edges = totalEdges + totalRouteEdges,
                Vectors = state.Graphs.Sum(g => g.Vectors.Count),
                LabelsPerNode = options.LabelsPerNode,
                TagsPerNode = options.TagsPerNode,
                PayloadSize = options.PayloadSize,
                Topology = options.Topology,
                VectorDimensions = options.VectorDimensions,
                AverageDegree = degree.Count == 0 ? 0 : degree.Values.Average(),
                MaxDegree = degree.Count == 0 ? 0 : degree.Values.Max(),
                Seed = options.Seed,
                GenerationDurationMs = stopwatch.Elapsed.TotalMilliseconds
            };

            return state;
        }

        public static Node BuildNode(BenchmarkOptions options, GraphDataset graph, long index, Random random)
        {
            int nodeIndex = (int)(index % int.MaxValue);
            return new Node
            {
                TenantGUID = graph.Tenant.GUID,
                GraphGUID = graph.Graph.GUID,
                GUID = Guid.NewGuid(),
                Name = "runtime-node-" + index,
                Labels = Labels("PerfNode", "Runtime", "Bucket" + random.Next(Math.Max(1, options.LabelsPerNode))),
                Tags = BuildTags(options.TagsPerNode, 0, 0, nodeIndex, false),
                Data = BuildPayload(options.PayloadSize, 0, 0, nodeIndex, "runtime-node")
            };
        }

        public static Edge BuildEdge(BenchmarkOptions options, GraphDataset graph, long index, Random random)
        {
            Node from = graph.PickNode(random);
            Node to = graph.PickNode(random);
            if (from.GUID == to.GUID && graph.Nodes.Count > 1) to = graph.Nodes[(graph.Nodes.IndexOf(from) + 1) % graph.Nodes.Count];

            return new Edge
            {
                TenantGUID = graph.Tenant.GUID,
                GraphGUID = graph.Graph.GUID,
                GUID = Guid.NewGuid(),
                Name = "runtime-edge-" + index,
                From = from.GUID,
                To = to.GUID,
                Cost = random.Next(1, 10),
                Labels = Labels("LINKS", "Runtime"),
                Tags = Tags(("kind", "runtime"), ("bucket", (index % Math.Max(1, options.TagsPerNode)).ToString())),
                Data = BuildPayload(options.PayloadSize, 0, 0, (int)(index % int.MaxValue), "runtime-edge")
            };
        }

        public static VectorMetadata BuildVector(BenchmarkOptions options, GraphDataset graph, long index, Random random, Guid? nodeGuid = null)
        {
            Guid? targetNode = nodeGuid ?? graph.PickNode(random).GUID;
            return new VectorMetadata
            {
                TenantGUID = graph.Tenant.GUID,
                GraphGUID = graph.Graph.GUID,
                NodeGUID = targetNode,
                GUID = Guid.NewGuid(),
                Model = "perf-" + options.VectorDimensions,
                Dimensionality = options.VectorDimensions,
                Content = "runtime vector " + index,
                Vectors = BuildEmbedding(options.VectorDimensions, (int)(index % int.MaxValue))
            };
        }

        public static List<float> BuildEmbedding(int dimensions, int index)
        {
            List<float> ret = new List<float>(dimensions);
            double baseAngle = (index + 1) * 0.017453292519943295;
            for (int i = 0; i < dimensions; i++)
            {
                double value = Math.Sin(baseAngle * (i + 1)) + Math.Cos((baseAngle + 0.37) * (i + 1));
                ret.Add((float)value);
            }

            return ret;
        }

        private static List<Edge> BuildEdges(
            BenchmarkOptions options,
            Random random,
            Guid tenantGuid,
            Guid graphGuid,
            List<Node> nodes,
            int tenantIndex,
            int graphIndex)
        {
            List<Edge> ret = new List<Edge>(options.EdgesPerGraph);
            if (nodes.Count < 2 || options.EdgesPerGraph < 1) return ret;

            string topology = options.Topology.Equals("powerlaw", StringComparison.OrdinalIgnoreCase) ? "power-law" : options.Topology;

            for (int edgeIndex = 0; edgeIndex < options.EdgesPerGraph; edgeIndex++)
            {
                int fromIndex;
                int toIndex;

                switch (topology)
                {
                    case "chain":
                        fromIndex = edgeIndex % (nodes.Count - 1);
                        toIndex = fromIndex + 1;
                        break;
                    case "tree":
                        toIndex = (edgeIndex % (nodes.Count - 1)) + 1;
                        fromIndex = Math.Max(0, (toIndex - 1) / 3);
                        break;
                    case "hub":
                        fromIndex = edgeIndex % Math.Max(1, Math.Min(5, nodes.Count));
                        toIndex = (edgeIndex % (nodes.Count - 1)) + 1;
                        break;
                    case "power-law":
                        fromIndex = PickPowerLawIndex(random, nodes.Count);
                        toIndex = random.Next(nodes.Count);
                        if (fromIndex == toIndex) toIndex = (toIndex + 1) % nodes.Count;
                        break;
                    case "communities":
                        int communitySize = Math.Max(2, nodes.Count / 5);
                        int community = edgeIndex % Math.Max(1, nodes.Count / communitySize);
                        int start = community * communitySize;
                        fromIndex = Math.Min(nodes.Count - 1, start + random.Next(Math.Min(communitySize, nodes.Count - start)));
                        if (random.NextDouble() < 0.9)
                            toIndex = Math.Min(nodes.Count - 1, start + random.Next(Math.Min(communitySize, nodes.Count - start)));
                        else
                            toIndex = random.Next(nodes.Count);
                        if (fromIndex == toIndex) toIndex = (toIndex + 1) % nodes.Count;
                        break;
                    case "dense":
                        fromIndex = edgeIndex % nodes.Count;
                        toIndex = (edgeIndex / nodes.Count + fromIndex + 1) % nodes.Count;
                        break;
                    case "grid":
                        int width = Math.Max(2, (int)Math.Sqrt(nodes.Count));
                        fromIndex = edgeIndex % nodes.Count;
                        bool right = edgeIndex % 2 == 0;
                        toIndex = right ? fromIndex + 1 : fromIndex + width;
                        if (toIndex >= nodes.Count) toIndex %= nodes.Count;
                        if (fromIndex == toIndex) toIndex = (toIndex + 1) % nodes.Count;
                        break;
                    default:
                        fromIndex = random.Next(nodes.Count);
                        toIndex = random.Next(nodes.Count);
                        if (fromIndex == toIndex) toIndex = (toIndex + 1) % nodes.Count;
                        break;
                }

                ret.Add(new Edge
                {
                    TenantGUID = tenantGuid,
                    GraphGUID = graphGuid,
                    GUID = DeterministicGuid(options.Seed, "edge", tenantIndex, graphIndex, edgeIndex),
                    Name = "edge-" + tenantIndex + "-" + graphIndex + "-" + edgeIndex,
                    From = nodes[fromIndex].GUID,
                    To = nodes[toIndex].GUID,
                    Cost = 1 + (edgeIndex % 10),
                    Labels = Labels("LINKS", "Bucket" + (edgeIndex % Math.Max(1, options.LabelsPerNode))),
                    Tags = Tags(("kind", "perf"), ("bucket", (edgeIndex % Math.Max(1, options.TagsPerNode)).ToString())),
                    Data = BuildPayload(options.PayloadSize, tenantIndex, graphIndex, edgeIndex, "edge")
                });
            }

            return ret;
        }

        private static List<VectorMetadata> BuildVectors(
            BenchmarkOptions options,
            Guid tenantGuid,
            Guid graphGuid,
            List<Node> nodes,
            int tenantIndex,
            int graphIndex)
        {
            List<VectorMetadata> ret = new List<VectorMetadata>();
            int count = Math.Min(options.VectorsPerGraph, nodes.Count);
            for (int vectorIndex = 0; vectorIndex < count; vectorIndex++)
            {
                ret.Add(new VectorMetadata
                {
                    TenantGUID = tenantGuid,
                    GraphGUID = graphGuid,
                    NodeGUID = nodes[vectorIndex].GUID,
                    GUID = DeterministicGuid(options.Seed, "vector", tenantIndex, graphIndex, vectorIndex),
                    Model = "perf-" + options.VectorDimensions,
                    Dimensionality = options.VectorDimensions,
                    Content = "vector " + tenantIndex + "-" + graphIndex + "-" + vectorIndex,
                    Vectors = BuildEmbedding(options.VectorDimensions, vectorIndex)
                });
            }

            return ret;
        }

        private static async Task<RouteFixture> CreateRouteFixtureAsync(
            BenchmarkContext context,
            BenchmarkOptions options,
            TenantMetadata tenant,
            Graph graph,
            int tenantIndex,
            int graphIndex,
            CancellationToken token)
        {
            const int routeNodeCount = 5;
            RouteFixture fixture = new RouteFixture();
            List<Node> routeNodes = new List<Node>(routeNodeCount);

            for (int routeIndex = 0; routeIndex < routeNodeCount; routeIndex++)
            {
                routeNodes.Add(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    GUID = DeterministicGuid(options.Seed, "route-node", tenantIndex, graphIndex, routeIndex),
                    Name = "route-node-" + tenantIndex + "-" + graphIndex + "-" + routeIndex,
                    Labels = Labels("PerfRouteNode"),
                    Tags = Tags(("kind", "route-fixture"), ("route", "primary"), ("sequence", routeIndex.ToString())),
                    Data = BuildPayload(options.PayloadSize, tenantIndex, graphIndex, routeIndex, "route-node")
                });
            }

            await context.Client.Node.CreateMany(tenant.GUID, graph.GUID, routeNodes, BulkCreateReturnModeEnum.Minimal, token).ConfigureAwait(false);
            foreach (Node node in routeNodes)
            {
                fixture.NodeGuids.Add(node.GUID);
            }

            List<Edge> routeEdges = new List<Edge>(routeNodeCount - 1);
            for (int edgeIndex = 0; edgeIndex < routeNodeCount - 1; edgeIndex++)
            {
                routeEdges.Add(new Edge
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    GUID = DeterministicGuid(options.Seed, "route-edge", tenantIndex, graphIndex, edgeIndex),
                    Name = "route-edge-" + tenantIndex + "-" + graphIndex + "-" + edgeIndex,
                    From = routeNodes[edgeIndex].GUID,
                    To = routeNodes[edgeIndex + 1].GUID,
                    Cost = 1,
                    Labels = Labels("PerfRouteEdge"),
                    Tags = Tags(("kind", "route-fixture"), ("route", "primary"), ("sequence", edgeIndex.ToString())),
                    Data = BuildPayload(options.PayloadSize, tenantIndex, graphIndex, edgeIndex, "route-edge")
                });
            }

            await context.Client.Edge.CreateMany(tenant.GUID, graph.GUID, routeEdges, BulkCreateReturnModeEnum.Minimal, token).ConfigureAwait(false);
            foreach (Edge edge in routeEdges)
            {
                fixture.EdgeGuids.Add(edge.GUID);
            }

            return fixture;
        }

        private static int PickPowerLawIndex(Random random, int count)
        {
            double skewed = Math.Pow(random.NextDouble(), 3);
            return Math.Min(count - 1, (int)(skewed * count));
        }

        private static object BuildPayload(string size, int tenantIndex, int graphIndex, int itemIndex, string kind)
        {
            int textLength = size switch
            {
                "large" => 4096,
                "medium" => 512,
                _ => 64
            };

            string text = new string((char)('a' + Math.Abs(itemIndex % 26)), textLength);

            Dictionary<string, object> payload = new Dictionary<string, object>
            {
                ["kind"] = kind,
                ["tenant"] = tenantIndex,
                ["graph"] = graphIndex,
                ["index"] = itemIndex,
                ["bucket"] = Math.Abs(itemIndex % 10),
                ["active"] = itemIndex % 3 != 0,
                ["score"] = itemIndex < 0 ? 0 : itemIndex % 1000,
                ["text"] = text
            };

            if (size == "medium" || size == "large")
            {
                payload["profile"] = new Dictionary<string, object>
                {
                    ["age"] = 20 + Math.Abs(itemIndex % 60),
                    ["role"] = "role-" + Math.Abs(itemIndex % 8),
                    ["region"] = "region-" + Math.Abs(itemIndex % 5)
                };
            }

            if (size == "large")
            {
                payload["history"] = Enumerable.Range(0, 16)
                    .Select(i => new Dictionary<string, object>
                    {
                        ["sequence"] = i,
                        ["value"] = itemIndex + i,
                        ["label"] = "history-" + i
                    })
                    .ToList();
            }

            return payload;
        }

        private static List<string> Labels(params string[] labels)
        {
            return labels.Where(l => !string.IsNullOrWhiteSpace(l)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static NameValueCollection BuildTags(int count, int tenantIndex, int graphIndex, int itemIndex, bool hot)
        {
            NameValueCollection tags = Tags(
                ("tenant", tenantIndex.ToString()),
                ("graph", graphIndex.ToString()),
                ("bucket", Math.Abs(itemIndex % Math.Max(1, count)).ToString()),
                ("hot", hot ? "true" : "false"));

            for (int i = 0; i < count; i++)
            {
                tags["tag" + i] = "value" + Math.Abs((itemIndex + i) % 10);
            }

            return tags;
        }

        private static NameValueCollection Tags(params (string Key, string Value)[] values)
        {
            NameValueCollection tags = new NameValueCollection(StringComparer.InvariantCultureIgnoreCase);
            foreach ((string Key, string Value) value in values)
            {
                tags[value.Key] = value.Value;
            }

            return tags;
        }

        private static IEnumerable<List<T>> Chunk<T>(List<T> items, int size)
        {
            for (int i = 0; i < items.Count; i += size)
            {
                yield return items.GetRange(i, Math.Min(size, items.Count - i));
            }
        }

        private static Guid DeterministicGuid(int seed, params object[] parts)
        {
            string input = seed + ":" + string.Join(":", parts.Select(p => p.ToString()));
            byte[] bytes = MD5.HashData(Encoding.UTF8.GetBytes(input));
            return new Guid(bytes);
        }
    }
}
