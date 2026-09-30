<img src="https://github.com/jchristn/LiteGraph/blob/main/assets/favicon.png" width="256" height="256">

# LiteGraph

[![NuGet Version](https://img.shields.io/nuget/v/LiteGraph.svg?style=flat)](https://www.nuget.org/packages/LiteGraph/) [![NuGet](https://img.shields.io/nuget/dt/LiteGraph.svg)](https://www.nuget.org/packages/LiteGraph) [![Documentation](https://img.shields.io/badge/docs-litegraph.readme.io-blue)](https://litegraph.readme.io/)

Current release: `v10.0.0`.

LiteGraph is a property graph database for applications that need graph relationships, tags, labels, JSON data, and vector search in one persistence layer. It can be embedded in a .NET process with `LiteGraphClient`, run as a standalone REST server, used through official SDKs, managed through the dashboard, or controlled by AI agents through the Model Context Protocol (MCP).

The `v7.0.0` transaction-scaling work is now merged into `main`. Historical planning material lives under [`archive/`](archive/); the files in the repository root describe the current mainline release.

## What Is Included

- Core .NET graph library targeting `net8.0` and `net10.0`
- SQLite provider for embedded, local, and test use, with in-process HNSW vector indexing through `HnswLite`
- PostgreSQL provider with pgvector for production, including multi-node clusters behind a load balancer
- Native LiteGraph graph query language for reads, traversals, vector search, and graph mutations
- Graph algorithms (centrality, PageRank, connected components, community detection) with write-back and a rustworkx/NetworkX export-compute-import path
- Graph-scoped transactions for nodes, edges, labels, tags, and vectors
- Vector search: pgvector HNSW in the database on PostgreSQL, `HnswLite` `2.0.1` in process on SQLite
- REST server with bearer-token authentication, request history, RBAC, and OpenAPI/Postman assets
- LLM chat over graph data with five provider types, SSE streaming, an in-process graph tool loop, and vector retrieval
- MCP server with HTTP, TCP, and WebSocket transports
- Next.js/React dashboard
- Official C#, Python, and JavaScript SDKs
- Docker Compose deployments for a single node on SQLite, a single node on PostgreSQL, and a multi-node cluster, each with MCP, the dashboard, Prometheus, Loki, and Grafana OSS

## Screenshots

<details>
<summary>Click to expand</summary>

Chat with your graph — natural-language questions answered through graph tool calls, streamed as markdown, with per-turn statistics, a model selector, and a streaming toggle:

![Chat with your graph](assets/ss7.png)

The home page: tenant KPIs, quick actions, and an interactive graph workspace with node inspection:

![Graph workspace](assets/ss1.png)

Node and vector editing — labels, tags, vectors, and JSON data in one editor:

![Node editing](assets/ss2.png)

Request telemetry — traffic over time with success/failure trends, duration percentiles, filters, and links into Prometheus and OpenTelemetry:

![Request telemetry](assets/ss3.png)

Provisioned Grafana dashboards — seven per-domain boards ship with the Compose stack; here, API Requests with rates, latency percentiles, errors, and authentication outcomes:

![Grafana API Requests dashboard](assets/ss8.png)

API Explorer — every REST operation, invocable with parameters and response previews:

![API Explorer](assets/ss4.png)

Authorization — built-in and custom roles (including the delegable Chat Admin), scopes, permissions, and resources:

![Authorization](assets/ss5.png)

3D graph inspection:

![3D graph view](assets/ss6.png)

</details>

## New In v10.0

v10.0 lets LiteGraph run as several identical nodes behind a load balancer. It is a major release: PostgreSQL deployments now require the pgvector extension, and stored vectors are converted to pgvector on first start, with no way back to 9.x afterward. Read the [upgrade guide](docs/UPGRADE.md) and back up before upgrading.

- Nodes keep no state of their own. On PostgreSQL, vectors live in a pgvector column and vector search runs in the database against a shared HNSW index, so every node returns the same results. Filtered and unindexed searches also run in SQL now instead of loading every candidate vector into the server.
- Cluster mode (`LITEGRAPH_CLUSTER_ENABLE=true`) turns off the object and authorization caches, so a delete or a revoked permission applies on every node immediately, and coordinates schema migrations, background jobs, vector index builds, settings writes, and rolling restarts through [Clutch](https://github.com/jchristn/clutch) distributed locks. Reads, writes, and searches take no distributed lock.
- Cluster nodes register in Redis every two seconds. `GET /v1.0/cluster/nodes` lists every node with its state and health, a settings change reaches every node within seconds, `POST /v1.0/cluster/restart` restarts the nodes one at a time while the cluster keeps serving, and single nodes can be restarted or removed. `GET /v1.0/cluster/locks` and `GET /v1.0/cluster/jobs` show the locks held in Clutch and the latest run of each background job.
- `GET /v1.0/health/live` and `GET /v1.0/health/ready`, and an `x-litegraph-node` header on every response. Losing Clutch or Redis leaves every node serving (readiness reports `Degraded`); only coordinated work waits.
- A reorganized dashboard: six sidebar entries (Home, Graphs, Chat, Access, System, Developer) with tabs, each tab at its own URL, one graph selector shared by every graph tab, and a Cluster page for nodes, rolling restarts, jobs, and locks.
- Per-node metrics and a LiteGraph Cluster Grafana dashboard; every dashboard gains a node filter. Request history records the node that handled each request and, behind a trusted load balancer, the real client address. Chat streams send keepalives so load balancer idle timeouts do not cut long answers.
- Faster vector search: results' nodes, vectors, labels, and tags load in one query each, about 40% faster on a single connection and more than twice as fast under load on a cluster.
- SDKs record the node that answered, retry idempotent requests on connection failures and 502, 503, and 504 with backoff, and gain cluster, health, and request history methods. The MCP server gains read-only `cluster/status`, `cluster/nodes`, and `cluster/node` tools.
- Three Docker deployments under [`docker/`](docker/): single node on SQLite, single node on PostgreSQL with pgvector, and a three-node cluster with Nginx (Switchboard optional), two Clutch nodes, Redis, smoke tests, and a failover test.
- Fixes: server security tokens now expire; a SQLite HnswLite index is rebuilt from the database after a restart instead of returning no results; Euclidean and dot-product searches on an indexed PostgreSQL graph return real Euclidean and dot-product values; turning caching off no longer throws.

See [Clustering](docs/CLUSTERING.md) for how a cluster works and how to run one.

## New In v9.0

v9.0 adds native graph algorithms across the whole product surface. Additive release — no storage migration required.

- Eleven algorithms: degree, closeness, eigenvector, and betweenness centrality; PageRank; weakly and strongly connected components; label-propagation and Louvain community detection; clustering coefficient; and k-core — computed over a whole-graph in-memory adjacency with a configurable node/edge ceiling.
- Optional write-back materializes per-node results into node data (DSL-queryable), an opt-in result cache, and a new `Algorithm` authorization resource type (compute/export require read; write-back/import require write).
- Callable from the client, the REST API, the `algorithm/*` MCP tools, the native query language (`CALL litegraph.algo.*`), the dashboard, and the C#, JavaScript, and Python SDKs.
- Graph projection export (node-link JSON, edge list, GraphML) and results import for round-tripping to external engines such as rustworkx/NetworkX for algorithms beyond native scope.
- Node embedding generation via the tenant's embedding endpoint (stored as HNSW-indexable node vectors), Prometheus/OpenTelemetry instrumentation with a provisioned Grafana algorithms dashboard, and dual-storage (SQLite + PostgreSQL) test coverage.

## New In v8.x

v8 unified accounts and observability (v8.0, breaking) and added LLM chat over graph data (v8.1).

- LLM chat built into the server. Tenants register completion and embedding endpoints for OpenAI (and compatibles), Ollama, Gemini, Anthropic, and VoyageAI; keys are stored server-side and returned redacted.
- The model queries the graph through a curated tool catalog (same names as the MCP tools), dispatched in-process under the caller's tenant and RBAC. Mutations are opt-in.
- Grounded, streaming answers. Graph-bound threads get automatic vector retrieval; responses stream over SSE, and every turn persists TTFT, tokens/sec, per-stage timings, tool transcripts, and a trace ID.
- OpenAI- and Ollama-compatible graph chat routes, so existing chat clients can talk to a graph using those wire formats.
- A full dashboard chat client: streaming markdown, model selector, slash commands, per-turn statistics, feedback, model preload, and an admin history view. Chat also reaches the MCP server and the C#, Python, and JavaScript SDKs.
- Delegable chat administration via a `Chat` authorization resource and built-in `ChatAdmin` role.
- Zero get-all APIs. Every list-returning REST route and MCP list tool responds with a paginated `EnumerationResult` envelope — never a bare array — with a guard test over the OpenAPI spec to prevent regression.
- One account model. `IsSystemAdmin` and `IsTenantAdmin` flags replace the separate administrator login; everyone else is governed by role and credential-scope RBAC. The static administrator token remains as a break-glass credential.
- One login, one dashboard, driven by a single capability map; system administrators edit `litegraph.json` from a settings page with live apply or restart.
- Everything is measured. Every REST route, MCP tool, and chat turn reports to Prometheus (with per-domain Grafana dashboards), and logs flow into Grafana through Loki and Alloy.
- Upgrading: v8.0 starts fresh (migrate from v7 via JSONL export/import); v8.1 upgrades in place, but clients that consumed list responses as bare arrays must adopt the enumeration envelope.

See [Chat](docs/CHAT.md) for the chat architecture and [REST API](docs/REST_API.md) for the routes.

## Repository Layout

| Directory | Description |
| --- | --- |
| [`src/`](src/) | Core LiteGraph library, REST server, MCP server, console, samples, and tests |
| [`dashboard/`](dashboard/) | Web dashboard UI built with Next.js and React |
| [`sdk/csharp/`](sdk/csharp/) | C# REST SDK published as `LiteGraph.Sdk` |
| [`sdk/python/`](sdk/python/) | Python REST SDK published as `litegraph-sdk` |
| [`sdk/js/`](sdk/js/) | JavaScript/Node.js REST SDK published as `litegraphdb` |
| [`docker/`](docker/) | Docker deployments: `single-node-sqlite`, `single-node-postgresql`, and `multi-node`, each with smoke tests and factory reset, plus shared observability config |
| [`docs/`](docs/) | Current operational and API documentation |
| [`archive/`](archive/) | Historical implementation plans and performance notes |

## Documentation

- [Storage configuration](docs/STORAGE.md)
- [Clustering and multi-node deployment](docs/CLUSTERING.md)
- [Docker deployments](docker/README.md)
- [Native graph query language](docs/DSL.md)
- [Graph algorithms and external-compute projection](docs/ALGORITHMS.md)
- [Graph transactions](docs/TRANSACTIONS.md)
- [RBAC and scoped credentials](docs/RBAC.md)
- [Chat](docs/CHAT.md)
- [Observability](docs/OBSERVABILITY.md)
- [REST API](docs/REST_API.md)
- [MCP API](docs/MCP_API.md)
- [Upgrade guide](docs/UPGRADE.md)
- [Using Claude with LiteGraph](docs/CLAUDE_MCP.md)
- [Performance and scalability testing](PERF_SCALE_TESTING.md)

Published documentation is also available at [litegraph.readme.io](https://litegraph.readme.io/).

## Quick Start From The Command Line

Run a single node on SQLite with nothing but the .NET SDK (8.0 or 10.0):

```bash
dotnet run --project src/LiteGraph.Server/LiteGraph.Server.csproj --framework net10.0
```

On first start the server writes `litegraph.json`, creates `litegraph.db` in the current directory, and creates the default tenant, user, and credential listed below. It listens on `http://127.0.0.1:8701`; check it with:

```bash
curl http://127.0.0.1:8701/v1.0/health/ready
```

Any setting can be overridden with environment variables, for example `LITEGRAPH_PORT`, or `LITEGRAPH_DB_TYPE=Postgresql` with `LITEGRAPH_DB_HOST`, `LITEGRAPH_DB_PORT`, `LITEGRAPH_DB_NAME`, `LITEGRAPH_DB_USERNAME`, and `LITEGRAPH_DB_PASSWORD` to use a PostgreSQL server that has the pgvector extension. See [Settings](docs/SETTINGS.md).

## Quick Start With Docker Compose

Three deployments live under [`docker/`](docker/); pick one, `cd` into it, and start it. They publish the same host ports, so run one at a time. [`docker/README.md`](docker/README.md) covers each in detail.

| Directory | What it runs |
| --- | --- |
| [`docker/single-node-sqlite/`](docker/single-node-sqlite/) | One LiteGraph node on SQLite with in-process HnswLite vector search |
| [`docker/single-node-postgresql/`](docker/single-node-postgresql/) | One LiteGraph node on PostgreSQL 17 with pgvector |
| [`docker/multi-node/`](docker/multi-node/) | Three LiteGraph nodes behind Nginx on one PostgreSQL database, with two Clutch lock nodes and Redis for the node registry; Switchboard is an optional profile |

```bash
cd docker/single-node-postgresql
docker compose up -d
```

Then validate it (Windows `smoke.bat`, elsewhere `pwsh ./smoke.ps1`):

```bat
smoke.bat
```

For the cluster, also run `failover.bat` in `docker/multi-node`: it keeps traffic flowing while it stops and restarts a LiteGraph node, each Clutch node, and Redis, then runs a rolling restart of every node.

Default endpoints, identical in every deployment:

| Service | Endpoint |
| --- | --- |
| LiteGraph REST (the load balancer in the cluster) | `http://127.0.0.1:8701` |
| LiteGraph MCP HTTP | `http://127.0.0.1:8702` |
| LiteGraph MCP TCP | `127.0.0.1:8703` |
| LiteGraph MCP WebSocket | `ws://127.0.0.1:8704/mcp` |
| LiteGraph UI | `http://127.0.0.1:3001` |
| Prometheus | `http://127.0.0.1:9090` |
| Grafana OSS | `http://127.0.0.1:3000` |
| PostgreSQL | `127.0.0.1:15432` (single node), `127.0.0.1:15433` (cluster) |

Default seeded LiteGraph records:

| Item | Value |
| --- | --- |
| Tenant GUID | `00000000-0000-0000-0000-000000000000` |
| Graph GUID | `00000000-0000-0000-0000-000000000000` |
| User email | `default@user.com` |
| User password | `password` |
| Credential bearer token | `default` |
| Server administrator bearer token | `litegraphadmin` |

Every host port and credential is configurable through the `.env.example` file in each deployment directory. The cluster ships with demonstration credentials so it starts without setup; change them before any real use, as `docker/multi-node/.env.example` describes.

### Load Generator

`src/LoadGenerator` seeds a LiteGraph database with realistic synthetic activity — themed graphs with nodes, edges, and vectors, backdated API request history following a diurnal curve with bursts, and chat threads with turn telemetry and feedback — so the dashboard and Grafana render a fully hydrated system. It writes through the core library directly (not REST), so timestamps are spread organically across the chosen window rather than clustered at the current time.

```bash
# Seed a SQLite database with the defaults (3 graphs, 50 nodes each, 2000 requests, 7 days)
dotnet run --project src/LoadGenerator --framework net8.0 -- --sqlite litegraph.db

# Seed the single-node PostgreSQL stack (see docker/single-node-postgresql)
dotnet run --project src/LoadGenerator --framework net8.0 -- \
  --postgres "Host=127.0.0.1;Port=15432;Database=litegraph;Username=litegraph;Password=litegraph"

# Larger dataset with a fixed RNG seed, replacing prior synthetic data
dotnet run --project src/LoadGenerator --framework net8.0 -- \
  --postgres "Host=127.0.0.1;Port=15432;Database=litegraph;Username=litegraph;Password=litegraph" \
  --graphs 5 --nodes 200 --density 0.02 --days 14 --requests 10000 --wipe --seed 42

# Remove previously generated synthetic data and exit
dotnet run --project src/LoadGenerator --framework net8.0 -- --sqlite litegraph.db --wipe-only
```

Everything the tool creates is marked (label `synthetic`, tag `generator=loadgen`, users under the `loadgen.synthetic` email domain, request-history correlation ID `loadgen-synthetic`), so `--wipe`/`--wipe-only` remove only generated data and leave real data untouched. Run with `--help` for the full argument list.

## Docker Images

The Compose deployments use these images, selected by `LITEGRAPH_IMAGE_TAG` (default `v10.0.0`):

- `jchristn77/litegraph:v10.0.0`
- `jchristn77/litegraph-mcp:v10.0.0`
- `jchristn77/litegraph-ui:v10.0.0`

Building a release tag (a plain `vMAJOR.MINOR.PATCH`) also moves `:latest`; any other tag leaves `:latest` alone. To run a build of your own, build and tag it with `build-all.bat <tag>` and start a deployment with `LITEGRAPH_IMAGE_TAG=<tag>`. PostgreSQL deployments use `pgvector/pgvector:0.8.6-pg17-trixie`; the cluster adds `jchristn77/clutch-server:v0.2.0`, `redis:7.4.9-alpine`, `nginx:1.27-alpine`, and optionally `jchristn77/switchboard:v5.2.2`.

## Factory Reset

Each deployment has its own factory reset, which touches only that deployment:

```bash
cd docker/single-node-postgresql/factory
./reset.sh
```

On Windows run `reset.bat` instead. The script asks you to type `RESET`, then stops the deployment, deletes its Docker volumes and runtime directories, and restores its configuration files from `factory/`. `update.bat` in each deployment is the non-destructive counterpart: it pulls current images and recreates the containers, keeping all data.

## Embedded C# Quick Start

Install the core package:

```bash
dotnet add package LiteGraph
```

Use SQLite directly in-process:

```csharp
using System.Collections.Generic;
using LiteGraph;
using LiteGraph.GraphRepositories.Sqlite;

using LiteGraphClient client = new LiteGraphClient(new SqliteGraphRepository("litegraph.db"));
client.InitializeRepository();

TenantMetadata tenant = await client.Tenant.Create(new TenantMetadata
{
    Name = "Example tenant"
});

Graph graph = await client.Graph.Create(new Graph
{
    TenantGUID = tenant.GUID,
    Name = "Example graph"
});

Node ada = await client.Node.Create(new Node
{
    TenantGUID = tenant.GUID,
    GraphGUID = graph.GUID,
    Name = "Ada",
    Labels = new List<string> { "Person" }
});

Node grace = await client.Node.Create(new Node
{
    TenantGUID = tenant.GUID,
    GraphGUID = graph.GUID,
    Name = "Grace",
    Labels = new List<string> { "Person" }
});

await client.Edge.Create(new Edge
{
    TenantGUID = tenant.GUID,
    GraphGUID = graph.GUID,
    From = ada.GUID,
    To = grace.GUID,
    Name = "Worked with"
});

GraphQueryResult query = await client.Query.Execute(
    tenant.GUID,
    graph.GUID,
    new GraphQueryRequest
    {
        Query = "MATCH (n:Person) RETURN n ORDER BY n.name ASC LIMIT 10"
    });

Console.WriteLine("Rows: " + query.RowCount);
```

Use the provider-neutral factory when selecting storage from configuration:

```csharp
using LiteGraph;
using LiteGraph.GraphRepositories;

DatabaseSettings settings = new DatabaseSettings
{
    Type = DatabaseTypeEnum.Postgresql,
    ConnectionString = "Host=127.0.0.1;Port=15432;Database=litegraph;Username=litegraph;Password=litegraph"
};

using GraphRepositoryBase repository = GraphRepositoryFactory.Create(settings);
using LiteGraphClient client = new LiteGraphClient(repository);

client.InitializeRepository();
```

Execute a graph-scoped transaction:

```csharp
TransactionRequest request = client.Transaction
    .CreateRequestBuilder()
    .WithIsolationLevel(TransactionIsolationLevelEnum.Default)
    .CreateNode(new Node { Name = "Transaction node" })
    .Build();

TransactionResult result = await client.Transaction.Execute(
    tenant.GUID,
    graph.GUID,
    request);

Console.WriteLine(result.State + " " + result.TransactionId);
```

For in-memory SQLite, pass `true` to `SqliteGraphRepository` and call `Flush()` when you want to persist the in-memory database to disk:

```csharp
using LiteGraphClient client = new LiteGraphClient(new SqliteGraphRepository("litegraph.db", true));
client.InitializeRepository();

// Work with the graph...

client.Flush();
```

## MCP And AI Agents

LiteGraph includes an MCP server so Claude, Claude Code, Cursor, and other MCP-compatible clients can create, query, and manage graphs through AI-agent tool calls. The MCP server is part of the Docker Compose deployment and starts automatically.

Default MCP listeners:

| Transport | Endpoint |
| --- | --- |
| HTTP (MCP clients, e.g. Claude Code) | `http://127.0.0.1:8702/mcp` |
| HTTP (plain JSON-RPC) | `http://127.0.0.1:8702/rpc` |
| TCP | `127.0.0.1:8703` |
| WebSocket | `ws://127.0.0.1:8704/mcp` |

MCP configuration can be overridden with:

| Variable | Purpose |
| --- | --- |
| `LITEGRAPH_ENDPOINT` | LiteGraph REST endpoint |
| `LITEGRAPH_API_KEY` | LiteGraph bearer token |
| `MCP_HTTP_HOSTNAME` | HTTP hostname |
| `MCP_HTTP_PORT` | HTTP port |
| `MCP_TCP_ADDRESS` | TCP bind address |
| `MCP_TCP_PORT` | TCP port |
| `MCP_WS_HOSTNAME` | WebSocket hostname |
| `MCP_WS_PORT` | WebSocket port |

Point Claude Code and other MCP clients at the `/mcp` URL, for example:

```json
{
  "mcpServers": {
    "litegraph": { "type": "http", "url": "http://127.0.0.1:8702/mcp" }
  }
}
```

`LiteGraph.McpServer install` writes this entry to `~/.claude.json` for you. Claude Code 2.1.x negotiates the stateless `2026-07-28` MCP revision, which only `/mcp` serves. If Claude Code connects but lists no LiteGraph tools, the entry most likely still points at `/rpc`, which older installs wrote. Change it to `/mcp` or re-run `install`.

See [Using Claude with LiteGraph](docs/CLAUDE_MCP.md) for client setup.

## Version History

See [`CHANGELOG.md`](CHANGELOG.md) for release history.

## Bugs, Feedback, Or Enhancement Requests

Please start an issue or discussion in the repository. For detailed documentation and guides, visit [litegraph.readme.io](https://litegraph.readme.io/).
