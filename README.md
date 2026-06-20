<img src="https://github.com/jchristn/LiteGraph/blob/main/assets/favicon.png" width="256" height="256">

# LiteGraph

[![NuGet Version](https://img.shields.io/nuget/v/LiteGraph.svg?style=flat)](https://www.nuget.org/packages/LiteGraph/) [![NuGet](https://img.shields.io/nuget/dt/LiteGraph.svg)](https://www.nuget.org/packages/LiteGraph) [![Documentation](https://img.shields.io/badge/docs-litegraph.readme.io-blue)](https://litegraph.readme.io/)

Current release: `v7.0.0`.

LiteGraph is a property graph database for applications that need graph relationships, tags, labels, JSON data, and vector search in one persistence layer. It can be embedded in a .NET process with `LiteGraphClient`, run as a standalone REST server, used through official SDKs, managed through the dashboard, or controlled by AI agents through the Model Context Protocol (MCP).

The `v7.0.0` transaction-scaling work is now merged into `main`. Historical planning material lives under [`archive/`](archive/); the files in the repository root describe the current mainline release.

## What Is Included

- Core .NET graph library targeting `net8.0` and `net10.0`
- SQLite provider for embedded, local, and test use
- PostgreSQL provider for production deployments and parallel transaction write scaling
- Native LiteGraph graph query language for reads, traversals, vector search, and graph mutations
- Graph-scoped transactions for nodes, edges, labels, tags, and vectors
- HNSW vector indexing through `HnswLite` `2.0.1`
- REST server with bearer-token authentication, request history, RBAC, and OpenAPI/Postman assets
- MCP server with HTTP, TCP, and WebSocket transports
- Next.js/React dashboard
- Official C#, Python, and JavaScript SDKs
- Docker Compose deployment for PostgreSQL, LiteGraph, MCP, dashboard, Prometheus, and Grafana OSS

## New In v7.0.0

- Graph transactions now use transaction-local repository/session state for converted providers.
- PostgreSQL transaction sessions use separate pooled connections, so parallel writes can scale according to PostgreSQL locking, isolation, and pool limits.
- SQLite transaction sessions are isolated for correctness, but write throughput remains bounded by SQLite file locking.
- Transaction requests support `IsolationLevel`, including PostgreSQL `ReadCommitted`, `RepeatableRead`, and `Serializable` where supported.
- Transaction responses include lifecycle and diagnostic fields such as `TransactionId`, `State`, `ValidationFailure`, `Provider`, `IsolationLevel`, `IsolatedRepository`, `SerializedByGate`, queue wait, commit and rollback timing, retryability, conflict classification, and provider error code.
- REST, MCP, C#, Python, and JavaScript transaction surfaces preserve diagnostic transaction result bodies for validation and rollback failures.
- Request history, Prometheus metrics, OpenTelemetry activities, and the Grafana dashboard include transaction diagnostics.
- File-backed HNSW vector index artifacts use the v7 format with `FormatVersion = 2` and `HnswLiteVersion = "2.0.1"`.
- Docker Compose now defaults to PostgreSQL-backed LiteGraph with `v7.0.0` LiteGraph, MCP, and UI images plus a one-shot PostgreSQL initialization container.

See [Graph transactions](docs/TRANSACTIONS.md), [Storage configuration](docs/STORAGE.md), and the [Upgrade guide](docs/UPGRADE.md) for provider caveats, migration guidance, rollback semantics, and operational details.

## Repository Layout

| Directory | Description |
| --- | --- |
| [`src/`](src/) | Core LiteGraph library, REST server, MCP server, console, samples, and tests |
| [`dashboard/`](dashboard/) | Web dashboard UI built with Next.js and React |
| [`sdk/csharp/`](sdk/csharp/) | C# REST SDK published as `LiteGraph.Sdk` |
| [`sdk/python/`](sdk/python/) | Python REST SDK published as `litegraph-sdk` |
| [`sdk/js/`](sdk/js/) | JavaScript/Node.js REST SDK published as `litegraphdb` |
| [`docker/`](docker/) | PostgreSQL-backed Docker Compose deployment, MCP config, Prometheus, Grafana, smoke test, and factory reset assets |
| [`docs/`](docs/) | Current operational and API documentation |
| [`archive/`](archive/) | Historical implementation plans and performance notes |

## Documentation

- [Storage configuration](docs/STORAGE.md)
- [Native graph query language](docs/DSL.md)
- [Graph transactions](docs/TRANSACTIONS.md)
- [RBAC and scoped credentials](docs/RBAC.md)
- [Observability](docs/OBSERVABILITY.md)
- [REST API](docs/REST_API.md)
- [Upgrade guide](docs/UPGRADE.md)
- [Using Claude with LiteGraph](docs/CLAUDE_MCP.md)
- [Performance and scalability testing](PERF_SCALE_TESTING.md)

Published documentation is also available at [litegraph.readme.io](https://litegraph.readme.io/).

## Quick Start With Docker Compose

The checked-in Docker deployment starts PostgreSQL 17, runs LiteGraph schema/default-data initialization once, and then starts LiteGraph, LiteGraph MCP, the dashboard, Prometheus, and Grafana OSS.

```bash
cd docker
docker compose up -d
```

Run the smoke test from the Docker directory after startup:

```bat
smoke.bat
```

Default endpoints:

| Service | Endpoint |
| --- | --- |
| LiteGraph REST | `http://localhost:8701` |
| LiteGraph MCP HTTP | `http://localhost:8702` |
| LiteGraph MCP TCP | `localhost:8703` |
| LiteGraph MCP WebSocket | `ws://localhost:8704/mcp` |
| LiteGraph UI | `http://localhost:3001` |
| PostgreSQL | `localhost:15432` |
| Prometheus | `http://localhost:9090` |
| Grafana OSS | `http://localhost:3000` |

Default seeded LiteGraph records:

| Item | Value |
| --- | --- |
| Tenant GUID | `00000000-0000-0000-0000-000000000000` |
| Graph GUID | `00000000-0000-0000-0000-000000000000` |
| User email | `default@user.com` |
| User password | `password` |
| Credential bearer token | `default` |
| Server administrator bearer token | `litegraphadmin` |

Default PostgreSQL values:

| Setting | Value |
| --- | --- |
| Host port | `15432` |
| Compose hostname | `postgresql` |
| Database | `litegraph` |
| Username | `litegraph` |
| Password | `litegraph` |
| Schema | `litegraph` |

Override the sample Docker PostgreSQL settings with `LITEGRAPH_POSTGRESQL_HOST_PORT`, `LITEGRAPH_POSTGRESQL_DATABASE`, `LITEGRAPH_POSTGRESQL_USERNAME`, `LITEGRAPH_POSTGRESQL_PASSWORD`, `LITEGRAPH_POSTGRESQL_SCHEMA`, `LITEGRAPH_DB_MAX_CONNECTIONS`, and `LITEGRAPH_DB_COMMAND_TIMEOUT_SECONDS`.

SQLite remains available for local Docker experiments by changing [`docker/litegraph.json`](docker/litegraph.json) or setting `LITEGRAPH_DB_TYPE=Sqlite` with a SQLite filename. PostgreSQL is the default Compose provider because it is the provider that can scale parallel writes.

## Docker Images

The Compose deployment uses these `v7.0.0` images:

- `jchristn77/litegraph:v7.0.0`
- `jchristn77/litegraph-mcp:v7.0.0`
- `jchristn77/litegraph-ui:v7.0.0`

The LiteGraph service uses [`docker/litegraph.json`](docker/litegraph.json). The MCP service uses [`docker/litegraph-mcp.json`](docker/litegraph-mcp.json). Keep the PostgreSQL volume and the `docker/` directory persisted so database state, vector index artifacts, logs, and backups are retained.

## Factory Reset

To reset the Docker deployment to the checked-in factory state:

```bash
cd docker
docker compose down
cd factory
./reset.sh
```

On Windows:

```bat
cd docker
docker compose down
cd factory
reset.bat
```

The reset script asks you to type `RESET`, deletes runtime Docker data for the deployment, restores Compose/configuration/provisioning files from [`docker/factory/`](docker/factory/), empties `docker/indexes/`, and resets PostgreSQL, Prometheus, and Grafana volumes.

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
    ConnectionString = "Host=localhost;Port=15432;Database=litegraph;Username=litegraph;Password=litegraph"
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

## Running The Server Locally

Run the REST server:

```bash
dotnet run --project src/LiteGraph.Server/LiteGraph.Server.csproj
```

By default the generated local server configuration listens on `http://localhost:8701` and uses SQLite unless you configure `LiteGraph.Database` or `LITEGRAPH_DB_*` environment variables. The Docker configuration listens on `0.0.0.0:8701` and uses PostgreSQL.

Useful environment variables:

| Variable | Purpose |
| --- | --- |
| `LITEGRAPH_DB_TYPE` | `Sqlite` or `Postgresql` |
| `LITEGRAPH_DB_FILENAME` | SQLite database filename |
| `LITEGRAPH_DB_CONNECTION_STRING` | Provider connection string |
| `LITEGRAPH_DB_HOST` | PostgreSQL host |
| `LITEGRAPH_DB_PORT` | PostgreSQL port |
| `LITEGRAPH_DB_NAME` | PostgreSQL database |
| `LITEGRAPH_DB_USERNAME` | PostgreSQL user |
| `LITEGRAPH_DB_PASSWORD` | PostgreSQL password |
| `LITEGRAPH_DB_SCHEMA` | PostgreSQL schema |
| `LITEGRAPH_TRANSACTION_MAX_OPERATIONS` | REST transaction operation cap |
| `LITEGRAPH_TRANSACTION_MAX_TIMEOUT_SECONDS` | REST transaction timeout cap |

## MCP And AI Agents

LiteGraph includes an MCP server so Claude, Claude Code, Cursor, and other MCP-compatible clients can create, query, and manage graphs through AI-agent tool calls.

Start LiteGraph REST first, then start MCP:

```bash
dotnet run --project src/LiteGraph.Server/LiteGraph.Server.csproj
dotnet run --project src/LiteGraph.McpServer/LiteGraph.McpServer.csproj
```

Default local MCP listeners:

| Transport | Endpoint |
| --- | --- |
| HTTP | `http://localhost:8702/rpc` |
| TCP | `localhost:8703` |
| WebSocket | `ws://localhost:8704/mcp` |

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

See [Using Claude with LiteGraph](docs/CLAUDE_MCP.md) for client setup.

## Dashboard

The dashboard lives in [`dashboard/`](dashboard/) and is included in the Docker Compose deployment. For local dashboard development:

```bash
cd dashboard
npm install
npm run dev
```

The dashboard includes graph management, node/edge/label/tag/vector screens, authorization management, request history, and an API Explorer with query and transaction examples.

## Client SDKs

Official REST SDKs:

| Language | Package | Directory |
| --- | --- | --- |
| C# | [![NuGet](https://img.shields.io/nuget/v/LiteGraph.Sdk.svg)](https://www.nuget.org/packages/LiteGraph.Sdk/) | [`sdk/csharp/`](sdk/csharp/) |
| Python | [![PyPI](https://img.shields.io/pypi/v/litegraph-sdk.svg)](https://pypi.org/project/litegraph-sdk/) | [`sdk/python/`](sdk/python/) |
| JavaScript | [![npm](https://img.shields.io/npm/v/litegraphdb.svg)](https://www.npmjs.com/package/litegraphdb) | [`sdk/js/`](sdk/js/) |

See [`sdk/README.md`](sdk/README.md) and each SDK directory for installation and usage details.

## Build And Test

Build the full .NET solution:

```bash
dotnet build src/LiteGraph.sln -c Debug
```

Run the default .NET test wrapper on Windows:

```bat
test.bat
```

Run the transaction-concurrency gate:

```bash
dotnet run --project src/Test.Automated/Test.Automated.csproj -c Debug --framework net10.0 -- --transaction-concurrency
```

Run the PostgreSQL provider gate by setting a disposable test database connection string:

```powershell
$env:LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING = "Host=localhost;Port=15432;Database=litegraph;Username=litegraph;Password=litegraph;Maximum Pool Size=128;Timeout=15;Command Timeout=60"
dotnet run --project src/Test.Automated/Test.Automated.csproj -c Debug --framework net10.0 -- --transaction-concurrency
```

SDK and dashboard tests:

```bash
cd sdk/js
npm test -- --runInBand

cd ../python
python -m pytest

cd ../../dashboard
npm test -- --runInBand
```

## Version History

See [`CHANGELOG.md`](CHANGELOG.md) for release history.

## Bugs, Feedback, Or Enhancement Requests

Please start an issue or discussion in the repository. For detailed documentation and guides, visit [litegraph.readme.io](https://litegraph.readme.io/).
