using System.Collections.Specialized;
using LiteGraph;
using LiteGraph.GraphRepositories.Sqlite;

const int MaxSampleNodeId = 22;
const int MaxSampleEdgeId = 34;

string dbPath = ResolveDatabasePath(args);
Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

var tenantGuid = Guid.Empty;
var userGuid = Guid.Empty;
var credentialGuid = Guid.Empty;
var graphGuid = Guid.Empty;
var now = DateTime.UtcNow;
IReadOnlyList<NodeSeed> nodeSeeds = NodeSeeds();
IReadOnlyList<EdgeSeed> edgeSeeds = EdgeSeeds();

SqliteGraphRepository repo = new(dbPath, false);
repo.InitializeRepository();
using LiteGraphClient client = new(repo);

await EnsureDefaultRecords(repo, now);
await EnsureScreenshotGraph(client, tenantGuid, graphGuid, now);
CleanupResult cleanup = await DeleteStaleSampleRecords(client, tenantGuid, graphGuid, nodeSeeds, edgeSeeds);

int nodesCreated = 0;
int nodesUpdated = 0;
foreach (NodeSeed seed in nodeSeeds)
{
    Node node = BuildNode(seed, tenantGuid, graphGuid, now);
    if (await client.Node.ExistsByGuid(tenantGuid, node.GUID))
    {
        await client.Node.Update(node);
        nodesUpdated++;
    }
    else
    {
        await client.Node.Create(node);
        nodesCreated++;
    }
}

int edgesCreated = 0;
int edgesUpdated = 0;
foreach (EdgeSeed seed in edgeSeeds)
{
    Edge edge = BuildEdge(seed, tenantGuid, graphGuid, now);
    if (await client.Edge.ExistsByGuid(tenantGuid, graphGuid, edge.GUID))
    {
        await client.Edge.Update(edge);
        edgesUpdated++;
    }
    else
    {
        await client.Edge.Create(edge);
        edgesCreated++;
    }
}

int nodeCount = await repo.Node.GetRecordCount(tenantGuid, graphGuid);
int edgeCount = await repo.Edge.GetRecordCount(tenantGuid, graphGuid);
int vectorCount = await repo.Vector.GetRecordCount(tenantGuid, graphGuid);
GraphStatistics stats = await client.Graph.GetStatistics(tenantGuid, graphGuid);

Console.WriteLine("LiteGraph sample database seed complete.");
Console.WriteLine($"Database: {dbPath}");
Console.WriteLine($"Graph: AI Support Demo ({graphGuid})");
Console.WriteLine($"Stale sample records removed: {cleanup.NodesDeleted} nodes, {cleanup.EdgesDeleted} edges");
Console.WriteLine($"Nodes created/updated: {nodesCreated}/{nodesUpdated}");
Console.WriteLine($"Edges created/updated: {edgesCreated}/{edgesUpdated}");
Console.WriteLine($"Current graph totals: {nodeCount} nodes, {edgeCount} edges, {vectorCount} vectors");
Console.WriteLine($"Statistics: {stats?.Nodes ?? nodeCount} nodes, {stats?.Edges ?? edgeCount} edges");
Console.WriteLine("Default login: default@user.com / password");
Console.WriteLine("Default bearer token: default");

static string ResolveDatabasePath(string[] args)
{
    string? explicitPath = null;

    for (int i = 0; i < args.Length; i++)
    {
        string arg = args[i];
        if (arg.Equals("--help", StringComparison.OrdinalIgnoreCase) || arg.Equals("-h", StringComparison.OrdinalIgnoreCase))
        {
            PrintUsage();
            Environment.Exit(0);
        }

        if (arg.Equals("--database", StringComparison.OrdinalIgnoreCase) || arg.Equals("--db", StringComparison.OrdinalIgnoreCase))
        {
            if (i + 1 >= args.Length) throw new ArgumentException("--database requires a path.");
            explicitPath = args[++i];
            continue;
        }

        const string databasePrefix = "--database=";
        if (arg.StartsWith(databasePrefix, StringComparison.OrdinalIgnoreCase))
        {
            explicitPath = arg[databasePrefix.Length..];
            continue;
        }

        const string dbPrefix = "--db=";
        if (arg.StartsWith(dbPrefix, StringComparison.OrdinalIgnoreCase))
        {
            explicitPath = arg[dbPrefix.Length..];
            continue;
        }

        if (!arg.StartsWith("-", StringComparison.Ordinal))
        {
            explicitPath = arg;
            continue;
        }

        throw new ArgumentException($"Unknown argument: {arg}");
    }

    return Path.GetFullPath(explicitPath ?? DefaultDatabasePath());
}

static string DefaultDatabasePath()
{
    string srcDirectory = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    return Path.Combine(srcDirectory, "LiteGraph.Server", "bin", "Debug", "net10.0", "litegraph.db");
}

static void PrintUsage()
{
    Console.WriteLine("LiteGraph.SampleDatabase");
    Console.WriteLine("Creates a small, screenshot-ready LiteGraph SQLite database.");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run --project src/LiteGraph.SampleDatabase -- --database <path>");
    Console.WriteLine("  dotnet run --project src/LiteGraph.SampleDatabase -- <path>");
    Console.WriteLine();
    Console.WriteLine("When no path is supplied, the default is:");
    Console.WriteLine("  src/LiteGraph.Server/bin/Debug/net10.0/litegraph.db");
}

async Task EnsureDefaultRecords(SqliteGraphRepository repository, DateTime timestamp)
{
    TenantMetadata tenant = new()
    {
        GUID = tenantGuid,
        Name = "Default tenant",
        Active = true,
        CreatedUtc = timestamp.AddDays(-45),
        LastUpdateUtc = timestamp
    };

    if (!await repository.Tenant.ExistsByGuid(tenant.GUID))
        await repository.Tenant.Create(tenant);

    UserMaster user = new()
    {
        GUID = userGuid,
        TenantGUID = tenantGuid,
        FirstName = "Default",
        LastName = "User",
        Email = "default@user.com",
        Password = "password",
        Active = true,
        CreatedUtc = timestamp.AddDays(-44),
        LastUpdateUtc = timestamp
    };

    if (!await repository.User.ExistsByGuid(tenantGuid, user.GUID))
        await repository.User.Create(user);

    Credential credential = new()
    {
        GUID = credentialGuid,
        TenantGUID = tenantGuid,
        UserGUID = userGuid,
        Name = "Default credential",
        BearerToken = "default",
        Active = true,
        Scopes = new List<string> { "admin" },
        CreatedUtc = timestamp.AddDays(-44),
        LastUpdateUtc = timestamp
    };

    if (!await repository.Credential.ExistsByGuid(tenantGuid, credential.GUID))
        await repository.Credential.Create(credential);
}

async Task EnsureScreenshotGraph(LiteGraphClient graphClient, Guid tenant, Guid graph, DateTime timestamp)
{
    Graph demoGraph = new()
    {
        GUID = graph,
        TenantGUID = tenant,
        Name = "AI Support Demo",
        Labels = new List<string> { "AI", "Demo" },
        Tags = TagSet(
            ("environment", "demo"),
            ("release", "7.0"),
            ("purpose", "dashboard screenshots")),
        Data = new
        {
            description = "Small sample graph for dashboard screenshots.",
            layoutHint = "LiteGraph Server is the hub. Data ingestion enters on the left; dashboard and operations views sit on the right.",
            status = "ready",
            lastSeededUtc = timestamp
        },
        CreatedUtc = timestamp.AddDays(-30),
        LastUpdateUtc = timestamp
    };

    if (await graphClient.Graph.ExistsByGuid(tenant, graph))
        await graphClient.Graph.Update(demoGraph);
    else
        await graphClient.Graph.Create(demoGraph);
}

async Task<CleanupResult> DeleteStaleSampleRecords(
    LiteGraphClient graphClient,
    Guid tenant,
    Guid graph,
    IReadOnlyList<NodeSeed> activeNodes,
    IReadOnlyList<EdgeSeed> activeEdges)
{
    HashSet<Guid> activeNodeGuids = activeNodes.Select(seed => NodeGuid(seed.Id)).ToHashSet();
    HashSet<Guid> activeEdgeGuids = activeEdges.Select(seed => EdgeGuid(seed.Id)).ToHashSet();
    int nodesDeleted = 0;
    int edgesDeleted = 0;

    for (int id = 1; id <= MaxSampleEdgeId; id++)
    {
        Guid edgeGuid = EdgeGuid(id);
        if (activeEdgeGuids.Contains(edgeGuid)) continue;
        if (!await graphClient.Edge.ExistsByGuid(tenant, graph, edgeGuid)) continue;

        await graphClient.Edge.DeleteByGuid(tenant, graph, edgeGuid);
        edgesDeleted++;
    }

    for (int id = 1; id <= MaxSampleNodeId; id++)
    {
        Guid nodeGuid = NodeGuid(id);
        if (activeNodeGuids.Contains(nodeGuid)) continue;
        if (!await graphClient.Node.ExistsByGuid(tenant, nodeGuid)) continue;

        await graphClient.Node.DeleteByGuid(tenant, graph, nodeGuid);
        nodesDeleted++;
    }

    return new CleanupResult(nodesDeleted, edgesDeleted);
}

Node BuildNode(NodeSeed seed, Guid tenant, Guid graph, DateTime timestamp)
{
    DateTime created = timestamp.AddMinutes(-seed.Id * 17);
    return new Node
    {
        GUID = NodeGuid(seed.Id),
        TenantGUID = tenant,
        GraphGUID = graph,
        Name = seed.Name,
        Labels = seed.Labels.ToList(),
        Tags = Tags(seed.Tags),
        Data = seed.Data,
        Vectors = new List<VectorMetadata>
        {
            DemoVector(VectorGuid(seed.Id), tenant, graph, NodeGuid(seed.Id), seed.Id, seed.Name + " " + string.Join(" ", seed.Labels))
        },
        CreatedUtc = created,
        LastUpdateUtc = timestamp
    };
}

Edge BuildEdge(EdgeSeed seed, Guid tenant, Guid graph, DateTime timestamp)
{
    DateTime created = timestamp.AddMinutes(-200 - seed.Id * 13);
    return new Edge
    {
        GUID = EdgeGuid(seed.Id),
        TenantGUID = tenant,
        GraphGUID = graph,
        Name = seed.Name,
        From = NodeGuid(seed.From),
        To = NodeGuid(seed.To),
        Cost = seed.Cost,
        Labels = seed.Labels.ToList(),
        Tags = Tags(seed.Tags),
        Data = seed.Data,
        CreatedUtc = created,
        LastUpdateUtc = timestamp
    };
}

static NameValueCollection TagSet(params (string Key, string Value)[] values)
{
    return Tags(values);
}

static NameValueCollection Tags(IEnumerable<(string Key, string Value)> values)
{
    NameValueCollection tags = new(StringComparer.OrdinalIgnoreCase);
    foreach ((string key, string value) in values)
        tags.Add(key, value);
    return tags;
}

static VectorMetadata DemoVector(Guid guid, Guid tenant, Guid graph, Guid node, int seed, string content)
{
    List<float> values = new();
    for (int i = 0; i < 8; i++)
    {
        int raw = Math.Abs((seed * 37) + (i * 19) + ((i + 3) * seed)) % 100;
        values.Add((float)Math.Round(raw / 100.0, 4));
    }

    return new VectorMetadata
    {
        GUID = guid,
        TenantGUID = tenant,
        GraphGUID = graph,
        NodeGUID = node,
        Model = "demo-embedding-v1",
        Dimensionality = values.Count,
        Content = content,
        Vectors = values,
        CreatedUtc = DateTime.UtcNow.AddMinutes(-seed),
        LastUpdateUtc = DateTime.UtcNow
    };
}

static Guid NodeGuid(int id) => Guid.Parse($"10000000-0000-0000-0000-{id:D12}");
static Guid EdgeGuid(int id) => Guid.Parse($"20000000-0000-0000-0000-{id:D12}");
static Guid VectorGuid(int id) => Guid.Parse($"30000000-0000-0000-0000-{id:D12}");

static IReadOnlyList<NodeSeed> NodeSeeds() =>
[
    new(1, "LiteGraph Server",
        ["Service", "Database"],
        [("domain", "graph"), ("tier", "core"), ("status", "active")],
        new { kind = "service", version = "7.0.0", surfaces = new[] { "REST", "SDK", "MCP" } }),

    new(2, "AI Assistant",
        ["Application", "AI"],
        [("domain", "ai"), ("tier", "app"), ("status", "active")],
        new { kind = "application", useCase = "support answers", traffic = "interactive" }),

    new(3, "Knowledge Graph",
        ["Graph", "Data"],
        [("domain", "knowledge"), ("tier", "data"), ("status", "active")],
        new { kind = "graph", contents = new[] { "documents", "entities", "relationships" } }),

    new(4, "Vector Index",
        ["Vector", "Search"],
        [("domain", "retrieval"), ("tier", "data"), ("status", "active")],
        new { kind = "index", dimensions = 8, model = "demo-embedding-v1" }),

    new(5, "Document Ingestion",
        ["Pipeline", "Ingestion"],
        [("domain", "data"), ("tier", "pipeline"), ("status", "active")],
        new { kind = "pipeline", cadence = "hourly", mode = "append and update" }),

    new(6, "Source Documents",
        ["DataSource", "Documents"],
        [("domain", "content"), ("tier", "source"), ("status", "active")],
        new { kind = "source", formats = new[] { "markdown", "pdf" }, freshness = "daily" }),

    new(7, "PostgreSQL Store",
        ["Storage", "Production"],
        [("domain", "storage"), ("tier", "database"), ("status", "recommended")],
        new { kind = "database", provider = "postgresql", usage = "production storage" }),

    new(8, "Dashboard",
        ["Application", "Admin"],
        [("domain", "operations"), ("tier", "ui"), ("status", "active")],
        new { kind = "dashboard", portals = new[] { "admin", "user" } }),

    new(9, "Observability",
        ["Metrics", "Monitoring"],
        [("domain", "operations"), ("tier", "monitoring"), ("status", "active")],
        new { kind = "monitoring", tools = new[] { "Prometheus", "Grafana" } }),

    new(10, "Authorization",
        ["Security", "RBAC"],
        [("domain", "security"), ("tier", "policy"), ("status", "active")],
        new { kind = "policy", scopes = new[] { "read", "write", "admin" } })
];

static IReadOnlyList<EdgeSeed> EdgeSeeds() =>
[
    new(1, "CALLS", 2, 1, 1, ["API"], [("path", "runtime"), ("direction", "inbound")], new { description = "The assistant calls LiteGraph for context." }),
    new(2, "SERVES", 1, 3, 1, ["Graph"], [("path", "data"), ("mode", "query")], new { description = "LiteGraph serves graph queries." }),
    new(3, "SEARCHES", 3, 4, 1, ["Vector"], [("path", "retrieval"), ("mode", "semantic")], new { description = "The graph uses vector search for relevant content." }),
    new(4, "READS", 5, 6, 2, ["Ingestion"], [("path", "source"), ("mode", "pull")], new { description = "The ingestion pipeline reads source content." }),
    new(5, "WRITES", 5, 3, 1, ["Mutation"], [("path", "ingestion"), ("mode", "transaction")], new { description = "Ingestion updates graph data." }),
    new(6, "PERSISTS_TO", 1, 7, 1, ["Storage"], [("path", "storage"), ("mode", "production")], new { description = "LiteGraph persists data to PostgreSQL in production." }),
    new(7, "MANAGES", 8, 1, 1, ["Dashboard"], [("path", "admin"), ("mode", "control")], new { description = "The dashboard manages LiteGraph data and operations." }),
    new(8, "OBSERVES", 9, 1, 2, ["Telemetry"], [("path", "metrics"), ("mode", "scrape")], new { description = "Observability tools track server health and latency." }),
    new(9, "ENFORCES", 1, 10, 1, ["Security"], [("path", "authorization"), ("mode", "policy")], new { description = "LiteGraph enforces authorization policy." }),
    new(10, "PROTECTS", 10, 3, 1, ["Security"], [("path", "data-access"), ("mode", "rbac")], new { description = "Authorization controls access to graph data." })
];

internal sealed record NodeSeed(
    int Id,
    string Name,
    IReadOnlyList<string> Labels,
    IReadOnlyList<(string Key, string Value)> Tags,
    object Data);

internal sealed record EdgeSeed(
    int Id,
    string Name,
    int From,
    int To,
    int Cost,
    IReadOnlyList<string> Labels,
    IReadOnlyList<(string Key, string Value)> Tags,
    object Data);

internal sealed record CleanupResult(int NodesDeleted, int EdgesDeleted);
