namespace Test.Aot
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Text;
    using System.Text.Json;
    using System.Text.Json.Nodes;
    using System.Threading.Tasks;
    using ExpressionTree;
    using LiteGraph;
    using LiteGraph.Algorithms;
    using LiteGraph.GraphRepositories;
    using LiteGraph.GraphRepositories.Sqlite;
    using LiteGraph.Indexing.Vector;
    using LiteGraph.Serialization;

    /// <summary>
    /// Exercises the LiteGraph library end to end so a Native AOT publish of this program proves the library works
    /// without reflection-based serialization or runtime code generation. Exit code 0 means every check passed.
    /// </summary>
    internal static class Program
    {
        #region Public-Members

        #endregion

        #region Private-Members

        private const string _PostgresqlEnvironmentVariable = "LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING";
        private const string _SampleDataJson = "{\"role\":\"engineer\",\"age\":41,\"active\":true,\"profile\":{\"team\":\"graph\"},\"skills\":[\"c#\",\"sql\"]}";

        private static int _Passed = 0;
        private static List<string> _Failed = new List<string>();

        #endregion

        #region Constructors-and-Factories

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        private static async Task<int> Main(string[] args)
        {
            Console.WriteLine("LiteGraph Native AOT verification");
            Console.WriteLine("  dynamic code supported:     " + RuntimeFeature.IsDynamicCodeSupported);
            Console.WriteLine("  reflection serialization:   " + JsonSerializer.IsReflectionEnabledByDefault);
            Console.WriteLine();

            string directory = Path.Combine(Path.GetTempPath(), "litegraph-aot-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            try
            {
                await RunSerializerChecks().ConfigureAwait(false);

                await RunProviderScenario(
                    "SQLite",
                    () => new SqliteGraphRepository(Path.Combine(directory, "aot.db")),
                    directory,
                    true).ConfigureAwait(false);

                string connectionString = Environment.GetEnvironmentVariable(_PostgresqlEnvironmentVariable);
                if (!String.IsNullOrWhiteSpace(connectionString))
                {
                    string schema = "aot_" + Guid.NewGuid().ToString("N").Substring(0, 12);
                    await RunProviderScenario(
                        "PostgreSQL",
                        () => GraphRepositoryFactory.Create(new DatabaseSettings
                        {
                            Type = DatabaseTypeEnum.Postgresql,
                            ConnectionString = connectionString,
                            Schema = schema
                        }),
                        directory,
                        false).ConfigureAwait(false);
                }
                else
                {
                    Console.WriteLine("PostgreSQL scenario skipped (" + _PostgresqlEnvironmentVariable + " is not set)");
                }
            }
            finally
            {
                try
                {
                    Directory.Delete(directory, true);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }

            Console.WriteLine();
            Console.WriteLine("Passed: " + _Passed + "  Failed: " + _Failed.Count);
            foreach (string failure in _Failed) Console.WriteLine("  FAILED " + failure);
            return _Failed.Count == 0 ? 0 : 1;
        }

        private static async Task RunSerializerChecks()
        {
            Serializer serializer = new Serializer();

            await Check("Serializer: model round trip with JsonElement data", () =>
            {
                Node node = new Node
                {
                    Name = "round-trip",
                    Labels = new List<string> { "a", "b" },
                    Tags = Tags("color", "red"),
                    Data = JsonDocument.Parse(_SampleDataJson).RootElement.Clone(),
                    Vectors = new List<VectorMetadata> { new VectorMetadata { Model = "m", Dimensionality = 2, Vectors = new List<float> { 0.5f, 0.25f } } }
                };

                string json = serializer.SerializeJson(node, false);
                Node copy = serializer.DeserializeJson<Node>(json);
                Expect(copy.Name == "round-trip", "name survives");
                Expect(copy.Tags != null && copy.Tags["color"] == "red", "tags survive");
                Expect(copy.Data is JsonElement element && element.GetProperty("profile").GetProperty("team").GetString() == "graph", "data survives as JsonElement");
                Expect(copy.Vectors != null && copy.Vectors[0].Vectors.Count == 2, "vectors survive");
                Expect(serializer.SerializeJson(copy.Data, false) == serializer.SerializeJson(node.Data, false), "data reserializes identically");
                return Task.CompletedTask;
            }).ConfigureAwait(false);

            await Check("Serializer: untyped data variants", () =>
            {
                List<object> variants = new List<object>
                {
                    JsonNode.Parse(_SampleDataJson)!,
                    new Dictionary<string, object> { { "count", 3L }, { "items", new List<object> { "x", 1.5d, true } } },
                    "text",
                    42L,
                    3.25d,
                    true,
                    Guid.NewGuid(),
                    DateTime.UtcNow,
                    new List<object> { "a", 1L },
                    new int[] { 1, 2 },
                    new List<long> { 3L, 4L },
                    new Guid[] { Guid.NewGuid() },
                    new List<double> { 0.5d },
                    new Dictionary<string, string> { { "k", "v" } },
                    new Dictionary<string, object> { { "nested", new List<object> { new Dictionary<string, object> { { "deep", new decimal[] { 1.5m } } } } } }
                };

                foreach (object variant in variants)
                {
                    Node node = new Node { Name = "variant", Data = variant };
                    string json = serializer.SerializeJson(node, false);
                    Node copy = serializer.DeserializeJson<Node>(json);
                    Expect(copy.Data is JsonElement, "data of type " + variant.GetType().Name + " reads back as JsonElement");
                }

                return Task.CompletedTask;
            }).ConfigureAwait(false);

            await Check("Serializer: enums are written as strings", () =>
            {
                AuthorizationRole role = new AuthorizationRole
                {
                    Name = "r",
                    Permissions = new List<AuthorizationPermissionEnum> { AuthorizationPermissionEnum.Read, AuthorizationPermissionEnum.Write }
                };

                string json = serializer.SerializeJson(role, false);
                Expect(json.Contains("\"Permissions\":[\"Read\",\"Write\"]"), "permissions written by name: " + json);
                AuthorizationRole copy = serializer.DeserializeJson<AuthorizationRole>(json);
                Expect(copy.Permissions.SequenceEqual(role.Permissions), "permissions read back");
                return Task.CompletedTask;
            }).ConfigureAwait(false);

            await Check("Serializer: exceptions, name-value collections, expressions", () =>
            {
                Exception exception;
                try
                {
                    throw new ArgumentException("outer", "paramName", new InvalidOperationException("inner"));
                }
                catch (ArgumentException e)
                {
                    exception = e;
                }

                string exceptionJson = serializer.SerializeJson(exception, false);
                Expect(exceptionJson.Contains("\"ParamName\":\"paramName\""), "exception ParamName: " + exceptionJson);
                Expect(exceptionJson.Contains("\"InnerException\":{\"Message\":\"inner\""), "inner exception: " + exceptionJson);
                Expect(exceptionJson.Contains("\"StackTrace\""), "stack trace: " + exceptionJson);

                string tagsJson = serializer.SerializeJson(Tags("k", "v"), false);
                Expect(tagsJson == "{\"k\":\"v\"}", "name-value collection: " + tagsJson);
                Expect(serializer.DeserializeJson<NameValueCollection>(tagsJson)["k"] == "v", "name-value collection read back");

                Expr expr = new Expr(new Expr("age", OperatorEnum.GreaterThan, 30L), OperatorEnum.And, new Expr("role", OperatorEnum.Equals, "engineer"));
                string exprJson = serializer.SerializeJson(expr, false);
                Expr exprCopy = serializer.DeserializeJson<Expr>(exprJson);
                Expect(exprCopy.Left is Expr left && (string)left.Left == "age", "nested expression read back: " + exprJson);
                Expect(serializer.SerializeJson(exprCopy, false) == exprJson, "expression reserializes identically");
                return Task.CompletedTask;
            }).ConfigureAwait(false);

            await Check("Serializer: timestamps keep their UTC value in any time zone", () =>
            {
                DateTime expected = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(1234560);
                Node node = serializer.DeserializeJson<Node>("{\"CreatedUtc\":\"2026-01-02T03:04:05.123456Z\",\"LastUpdateUtc\":\"2026-01-02T05:04:05.123456+02:00\"}");
                Expect(node.CreatedUtc.Ticks == expected.Ticks && node.CreatedUtc.Kind == DateTimeKind.Utc, "Z timestamp: " + node.CreatedUtc.ToString("o"));
                Expect(node.LastUpdateUtc.Ticks == expected.Ticks, "offset timestamp: " + node.LastUpdateUtc.ToString("o"));
                string json = serializer.SerializeJson(new Node { CreatedUtc = expected.ToLocalTime() }, false);
                Expect(json.Contains("\"CreatedUtc\":\"2026-01-02T03:04:05.123456Z\""), "local timestamp written as UTC: " + json);
                return Task.CompletedTask;
            }).ConfigureAwait(false);

            await Check("Serializer: application types need registration under AOT", () =>
            {
                AotAppData data = new AotAppData { Name = "app", Score = 7, Status = AotAppStatusEnum.Retired, Keywords = new List<string> { "k" } };

                if (!JsonSerializer.IsReflectionEnabledByDefault)
                {
                    bool threw = false;
                    try
                    {
                        serializer.SerializeJson(new Node { Data = data }, false);
                    }
                    catch (NotSupportedException e)
                    {
                        threw = e.ToString().Contains(nameof(AotAppData));
                    }

                    Expect(threw, "unregistered type fails with NotSupportedException naming the type");

                    bool copyThrew = false;
                    try
                    {
                        serializer.CopyObject(data);
                    }
                    catch (NotSupportedException)
                    {
                        copyThrew = true;
                    }

                    Expect(copyThrew, "CopyObject fails loudly instead of returning null");
                }

                Serializer.AddTypeInfoResolver(AotAppJsonContext.Default);
                string json = serializer.SerializeJson(new Node { Name = "n", Data = data }, false);
                Expect(json.Contains("\"Status\":\"Retired\""), "registered type serializes: " + json);

                Node node = serializer.DeserializeJson<Node>(json);
                LiteGraphClient client = new LiteGraphClient(new SqliteGraphRepository(Path.Combine(Path.GetTempPath(), "litegraph-aot-convert-" + Guid.NewGuid().ToString("N") + ".db"), true));
                try
                {
                    AotAppData converted = client.ConvertData(node.Data, AotAppJsonContext.Default.AotAppData);
                    Expect(converted.Score == 7 && converted.Status == AotAppStatusEnum.Retired, "ConvertData with type metadata");
                    AotAppData convertedByResolver = client.ConvertData<AotAppData>(node.Data);
                    Expect(convertedByResolver.Keywords[0] == "k", "ConvertData through the registered resolver");
                    AotAppData copy = serializer.CopyObject(data);
                    Expect(copy != null && copy.Name == "app", "CopyObject of a registered type");
                }
                finally
                {
                    client.Dispose();
                }

                return Task.CompletedTask;
            }).ConfigureAwait(false);
        }

        private static async Task RunProviderScenario(string provider, Func<GraphRepositoryBase> createRepository, string directory, bool isSqlite)
        {
            Console.WriteLine();
            Console.WriteLine("[" + provider + "]");

            GraphRepositoryBase repo = createRepository();
            using LiteGraphClient client = new LiteGraphClient(repo);
            TenantMetadata tenant = null;
            Graph graph = null;
            Node ada = null;
            Node bob = null;
            Node carol = null;
            List<float> adaVector = new List<float> { 0.9f, 0.1f, 0.0f, 0.1f };

            await Check(provider + ": initialize repository and seed built-in roles", () =>
            {
                client.InitializeRepository();
                return Task.CompletedTask;
            }).ConfigureAwait(false);

            await Check(provider + ": built-in roles read back with permission lists", async () =>
            {
                AuthorizationRoleSearchResult roles = await repo.AuthorizationRoles.SearchRoles(new AuthorizationRoleSearchRequest { BuiltIn = true, PageSize = 100 }).ConfigureAwait(false);
                Expect(roles.Objects.Count >= 4, "built-in roles exist (" + roles.Objects.Count + ")");
                Expect(roles.Objects.Where(r => r.BuiltInRole != BuiltInRoleEnum.Custom).All(r => r.Permissions != null && r.Permissions.Count > 0), "built-in roles other than Custom have permissions");
                Expect(roles.Objects.Any(r => r.Permissions != null && r.Permissions.Contains(AuthorizationPermissionEnum.Write)), "permission values read back");
                Expect(roles.Objects.Any(r => r.ResourceTypes != null && r.ResourceTypes.Count > 0), "resource types read back");
            }).ConfigureAwait(false);

            await Check(provider + ": tenant, user, credential", async () =>
            {
                tenant = await client.Tenant.Create(new TenantMetadata { Name = "AOT tenant" }).ConfigureAwait(false);
                UserMaster user = await client.User.Create(new UserMaster
                {
                    TenantGUID = tenant.GUID,
                    FirstName = "Ada",
                    LastName = "Lovelace",
                    Email = "ada-" + Guid.NewGuid().ToString("N") + "@example.com",
                    Password = "password"
                }).ConfigureAwait(false);

                Guid scopedGraph = Guid.NewGuid();
                Credential credential = await client.Credential.Create(new Credential
                {
                    TenantGUID = tenant.GUID,
                    UserGUID = user.GUID,
                    Name = "aot",
                    Scopes = new List<string> { "read", "write" },
                    GraphGUIDs = new List<Guid> { scopedGraph }
                }).ConfigureAwait(false);

                Credential read = await client.Credential.ReadByGuid(tenant.GUID, credential.GUID).ConfigureAwait(false);
                Expect(read.Scopes != null && read.Scopes.SequenceEqual(new[] { "read", "write" }), "credential scopes read back");
                Expect(read.GraphGUIDs != null && read.GraphGUIDs.Single() == scopedGraph, "credential graph GUIDs read back");
            }).ConfigureAwait(false);

            await Check(provider + ": graph, nodes, edges with data, labels, tags, vectors", async () =>
            {
                graph = await client.Graph.Create(new Graph
                {
                    TenantGUID = tenant.GUID,
                    Name = "AOT graph",
                    Labels = new List<string> { "aot" },
                    Tags = Tags("purpose", "verification"),
                    Data = JsonDocument.Parse("{\"owner\":\"aot\"}").RootElement.Clone()
                }).ConfigureAwait(false);

                ada = await client.Node.Create(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Ada",
                    Labels = new List<string> { "Person" },
                    Tags = Tags("team", "graph"),
                    Data = JsonDocument.Parse(_SampleDataJson).RootElement.Clone(),
                    Vectors = new List<VectorMetadata>
                    {
                        new VectorMetadata { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Model = "aot", Dimensionality = 4, Content = "ada", Vectors = adaVector }
                    }
                }).ConfigureAwait(false);

                bob = await client.Node.Create(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Bob",
                    Labels = new List<string> { "Person" },
                    Data = new Dictionary<string, object> { { "role", "designer" }, { "age", 29L }, { "active", false } },
                    Vectors = new List<VectorMetadata>
                    {
                        new VectorMetadata { TenantGUID = tenant.GUID, GraphGUID = graph.GUID, Model = "aot", Dimensionality = 4, Content = "bob", Vectors = new List<float> { 0.0f, 1.0f, 0.0f, 0.0f } }
                    }
                }).ConfigureAwait(false);

                carol = await client.Node.Create(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "Carol",
                    Labels = new List<string> { "Person" },
                    Data = JsonNode.Parse("{\"role\":\"engineer\",\"age\":35,\"active\":true}")
                }).ConfigureAwait(false);

                await client.Edge.Create(new Edge
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "knows",
                    From = ada.GUID,
                    To = bob.GUID,
                    Cost = 3,
                    Labels = new List<string> { "KNOWS" },
                    Tags = Tags("since", "2020"),
                    Data = JsonDocument.Parse("{\"strength\":0.8}").RootElement.Clone()
                }).ConfigureAwait(false);

                await client.Edge.Create(new Edge
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "knows",
                    From = bob.GUID,
                    To = carol.GUID,
                    Labels = new List<string> { "KNOWS" }
                }).ConfigureAwait(false);

                Graph readGraph = await client.Graph.ReadByGuid(tenant.GUID, graph.GUID, includeData: true, includeSubordinates: true).ConfigureAwait(false);
                Expect(readGraph.Data is JsonElement graphData && graphData.GetProperty("owner").GetString() == "aot", "graph data read back");

                Node readAda = await client.Node.ReadByGuid(tenant.GUID, graph.GUID, ada.GUID, includeData: true, includeSubordinates: true).ConfigureAwait(false);
                Expect(readAda.Data is JsonElement adaData && adaData.GetProperty("profile").GetProperty("team").GetString() == "graph", "node data read back");
                Expect(readAda.Labels != null && readAda.Labels.Contains("Person"), "node labels read back");
                Expect(readAda.Tags != null && readAda.Tags["team"] == "graph", "node tags read back");
                Expect(readAda.Vectors != null && readAda.Vectors.Count == 1 && readAda.Vectors[0].Vectors.Count == 4, "node vectors read back");
            }).ConfigureAwait(false);

            await Check(provider + ": enumeration and expression filters on data", async () =>
            {
                EnumerationResult<Node> page = await client.Node.Enumerate(new EnumerationRequest
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    MaxResults = 2,
                    IncludeData = true
                }).ConfigureAwait(false);
                Expect(page.Objects.Count == 2 && !page.EndOfResults, "first page of two");

                List<Node> engineers = new List<Node>();
                await foreach (Node node in client.Node.ReadMany(tenant.GUID, graph.GUID, nodeFilter: new Expr("role", OperatorEnum.Equals, "engineer"), includeData: true).ConfigureAwait(false))
                    engineers.Add(node);
                Expect(engineers.Select(n => n.Name).OrderBy(n => n).SequenceEqual(new[] { "Ada", "Carol" }), "expression filter on data: " + String.Join(",", engineers.Select(n => n.Name)));

                List<Node> tagged = new List<Node>();
                await foreach (Node node in client.Node.ReadMany(tenant.GUID, graph.GUID, labels: new List<string> { "Person" }, tags: Tags("team", "graph")).ConfigureAwait(false))
                    tagged.Add(node);
                Expect(tagged.Count == 1 && tagged[0].GUID == ada.GUID, "label and tag filter");
            }).ConfigureAwait(false);

            await Check(provider + ": graph query language", async () =>
            {
                GraphQueryResult result = await client.Query.Execute(tenant.GUID, graph.GUID, new GraphQueryRequest
                {
                    Query = "MATCH (a:Person)-[e:KNOWS]->(b:Person) WHERE a.data.profile.team = 'graph' AND b.name IN ['Bob', 'Nobody'] RETURN a, e, b",
                    IncludeProfile = true
                }).ConfigureAwait(false);
                Expect(result.Rows.Count == 1, "one matching path (" + result.Rows.Count + ")");

                GraphQueryResult created = await client.Query.Execute(tenant.GUID, graph.GUID, new GraphQueryRequest
                {
                    Query = "CREATE (n:Person { name: $name, data: $data }) RETURN n",
                    Parameters = new Dictionary<string, object>
                    {
                        { "name", "Dave" },
                        { "data", JsonDocument.Parse("{\"role\":\"analyst\"}").RootElement.Clone() }
                    }
                }).ConfigureAwait(false);
                Expect(created.Mutated && created.Nodes != null && created.Nodes.Count == 1, "CREATE through the query language");

                string json = new Serializer().SerializeJson(result, false);
                Expect(json.Contains("\"Rows\""), "query result serializes");
            }).ConfigureAwait(false);

            await Check(provider + ": vector search (brute force and indexed)", async () =>
            {
                List<VectorSearchResult> results = await Search(client, tenant.GUID, graph.GUID, adaVector).ConfigureAwait(false);
                Expect(results.Count > 0 && results[0].Node.GUID == ada.GUID, "brute-force search finds Ada first");

                VectorIndexConfiguration configuration = new VectorIndexConfiguration
                {
                    VectorIndexType = isSqlite ? VectorIndexTypeEnum.HnswSqlite : VectorIndexTypeEnum.HnswRam,
                    VectorIndexFile = isSqlite ? Path.Combine(directory, "aot-index.db") : null,
                    VectorDimensionality = 4,
                    VectorIndexThreshold = 1,
                    VectorIndexM = 8,
                    VectorIndexEf = 32,
                    VectorIndexEfConstruction = 64
                };

                await client.Graph.EnableVectorIndexing(tenant.GUID, graph.GUID, configuration).ConfigureAwait(false);
                VectorIndexStatistics stats = await client.Graph.GetVectorIndexStatistics(tenant.GUID, graph.GUID).ConfigureAwait(false);
                Expect(stats != null, "index statistics available");

                results = await Search(client, tenant.GUID, graph.GUID, adaVector).ConfigureAwait(false);
                Expect(results.Count > 0 && results[0].Node.GUID == ada.GUID, "indexed search finds Ada first");
            }).ConfigureAwait(false);

            await Check(provider + ": transactions commit and roll back", async () =>
            {
                Guid nodeGuid = Guid.NewGuid();
                TransactionResult committed = await client.Transaction.Execute(tenant.GUID, graph.GUID, new TransactionRequest
                {
                    Operations = new List<TransactionOperation>
                    {
                        new TransactionOperation
                        {
                            OperationType = TransactionOperationTypeEnum.Create,
                            ObjectType = TransactionObjectTypeEnum.Node,
                            Payload = JsonDocument.Parse("{\"GUID\":\"" + nodeGuid + "\",\"Name\":\"Erin\",\"Data\":{\"role\":\"tester\"}}").RootElement.Clone()
                        },
                        new TransactionOperation
                        {
                            OperationType = TransactionOperationTypeEnum.Create,
                            ObjectType = TransactionObjectTypeEnum.Node,
                            Payload = new Node { Name = "Frank", Data = new Dictionary<string, object> { { "role", "ops" } } }
                        },
                        new TransactionOperation
                        {
                            OperationType = TransactionOperationTypeEnum.Create,
                            ObjectType = TransactionObjectTypeEnum.Label,
                            Payload = new Dictionary<string, object> { { "NodeGUID", nodeGuid.ToString() }, { "Label", "Tester" } }
                        }
                    }
                }).ConfigureAwait(false);
                Expect(committed.Success, "transaction committed: " + committed.Error);
                Expect(await client.Node.ExistsByGuid(tenant.GUID, nodeGuid).ConfigureAwait(false), "transaction node exists");
                Expect(new Serializer().SerializeJson(committed, false).Contains("\"Success\":true"), "transaction result serializes");

                TransactionResult failed = await client.Transaction.Execute(tenant.GUID, graph.GUID, new TransactionRequest
                {
                    Operations = new List<TransactionOperation>
                    {
                        new TransactionOperation
                        {
                            OperationType = TransactionOperationTypeEnum.Create,
                            ObjectType = TransactionObjectTypeEnum.Node,
                            Payload = new Node { Name = "Rolled back" }
                        },
                        new TransactionOperation
                        {
                            OperationType = TransactionOperationTypeEnum.Create,
                            ObjectType = TransactionObjectTypeEnum.Node,
                            Payload = new Node { GUID = ada.GUID, Name = "Duplicate GUID" }
                        }
                    }
                }).ConfigureAwait(false);
                Expect(!failed.Success && failed.RolledBack, "failing transaction rolls back: " + failed.Error);
                Expect(!String.IsNullOrEmpty(failed.ProviderErrorCode), "provider error code captured");

                List<Node> rolledBack = new List<Node>();
                await foreach (Node node in client.Node.ReadMany(tenant.GUID, graph.GUID, name: "Rolled back").ConfigureAwait(false))
                    rolledBack.Add(node);
                Expect(rolledBack.Count == 0, "rolled-back node does not exist");
            }).ConfigureAwait(false);

            await Check(provider + ": GEXF, JSONL, and projection export; JSONL import", async () =>
            {
                string gexf = await client.RenderGraphAsGexf(tenant.GUID, graph.GUID, true, true).ConfigureAwait(false);
                Expect(gexf.StartsWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>") && gexf.Contains("<gexf ") && gexf.Contains("label=\"Ada\""), "GEXF export");

                string jsonl = await client.RenderGraphAsJsonl(tenant.GUID, graph.GUID, true, true).ConfigureAwait(false);
                Expect(jsonl.Split('\n').Count(l => l.Length > 0) >= 6, "JSONL export");

                GraphImportResult imported = await client.ImportGraphFromJsonl(tenant.GUID, jsonl, new GraphImportRequest
                {
                    Mode = GraphImportModeEnum.CreateNew,
                    GuidStrategy = GraphImportGuidStrategyEnum.Regenerate
                }).ConfigureAwait(false);
                Expect(imported.Success && imported.NodesCreated >= 4, "JSONL import (" + imported.NodesCreated + " nodes)");

                foreach (GraphExportFormatEnum format in Enum.GetValues<GraphExportFormatEnum>())
                {
                    using MemoryStream stream = new MemoryStream();
                    await client.Algorithm.ExportGraph(tenant.GUID, graph.GUID, format, GraphExportAttributeLevelEnum.Full, stream).ConfigureAwait(false);
                    Expect(stream.Length > 0, "projection export " + format);
                }
            }).ConfigureAwait(false);

            await Check(provider + ": algorithms with result write-back", async () =>
            {
                GraphAlgorithmResult result = await client.Algorithm.Run(tenant.GUID, graph.GUID, new GraphAlgorithmRequest
                {
                    AlgorithmType = GraphAlgorithmTypeEnum.PageRank,
                    WriteBack = true
                }).ConfigureAwait(false);
                Expect(result.NodeCount >= 4, "PageRank ran over the graph");
                Expect(new Serializer().SerializeJson(result, false).Contains("\"AlgorithmType\":\"PageRank\""), "algorithm result serializes");

                Node readAda = await client.Node.ReadByGuid(tenant.GUID, graph.GUID, ada.GUID, includeData: true).ConfigureAwait(false);
                Expect(readAda.Data is JsonElement data && data.GetProperty("role").GetString() == "engineer", "write-back keeps existing data");
            }).ConfigureAwait(false);

            await Check(provider + ": application data type through a registered resolver", async () =>
            {
                Node node = await client.Node.Create(new Node
                {
                    TenantGUID = tenant.GUID,
                    GraphGUID = graph.GUID,
                    Name = "App",
                    Data = new AotAppData { Name = "app", Score = 9, Status = AotAppStatusEnum.Active }
                }).ConfigureAwait(false);

                Node read = await client.Node.ReadByGuid(tenant.GUID, graph.GUID, node.GUID, includeData: true).ConfigureAwait(false);
                AotAppData data = client.ConvertData(read.Data, AotAppJsonContext.Default.AotAppData);
                Expect(data.Score == 9 && data.Status == AotAppStatusEnum.Active, "application data round trip");
            }).ConfigureAwait(false);

            await Check(provider + ": delete and flush", async () =>
            {
                await client.Graph.DeleteByGuid(tenant.GUID, graph.GUID, force: true).ConfigureAwait(false);
                Expect(!await client.Graph.ExistsByGuid(tenant.GUID, graph.GUID).ConfigureAwait(false), "graph deleted");
                client.Flush();
            }).ConfigureAwait(false);
        }

        private static async Task<List<VectorSearchResult>> Search(LiteGraphClient client, Guid tenantGuid, Guid graphGuid, List<float> embeddings)
        {
            List<VectorSearchResult> results = new List<VectorSearchResult>();
            await foreach (VectorSearchResult result in client.Vector.Search(new VectorSearchRequest
            {
                TenantGUID = tenantGuid,
                GraphGUID = graphGuid,
                Domain = VectorSearchDomainEnum.Node,
                SearchType = VectorSearchTypeEnum.CosineSimilarity,
                Embeddings = embeddings,
                TopK = 3,
                MinimumScore = 0.0f
            }).ConfigureAwait(false))
            {
                results.Add(result);
            }

            return results;
        }

        private static async Task Check(string name, Func<Task> action)
        {
            try
            {
                await action().ConfigureAwait(false);
                _Passed++;
                Console.WriteLine("  PASS " + name);
            }
            catch (Exception e)
            {
                _Failed.Add(name + ": " + e.GetType().Name + ": " + e.Message);
                Console.WriteLine("  FAIL " + name);
                Console.WriteLine(Indent(e.ToString()));
            }
        }

        private static void Expect(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("Expectation failed: " + message);
        }

        private static NameValueCollection Tags(string key, string value)
        {
            NameValueCollection tags = new NameValueCollection();
            tags.Add(key, value);
            return tags;
        }

        private static string Indent(string text)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string line in text.Split('\n')) sb.Append("       ").AppendLine(line.TrimEnd('\r'));
            return sb.ToString();
        }

        #endregion
    }
}
