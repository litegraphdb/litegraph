# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and Development Commands

```bash
# Build the entire solution
dotnet build src/LiteGraph.sln

# Build specific projects
dotnet build src/LiteGraph/LiteGraph.csproj
dotnet build src/LiteGraph.Server/LiteGraph.Server.csproj

# Run the Touchstone suites (SQLite always; PostgreSQL cases when the variable is set;
# the database needs the pgvector extension, e.g. docker image pgvector/pgvector:0.8.6-pg17-trixie)
dotnet run --project src/Test.Automated/Test.Automated.csproj --framework net10.0
LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING="Host=127.0.0.1;Port=5432;Username=...;Password=...;Database=..." \
  dotnet run --project src/Test.Automated/Test.Automated.csproj --framework net10.0 -- --suite ScaleOut

# Native AOT check of the library (trim/AOT warnings are errors; runs SQLite, plus PostgreSQL when the variable is set)
dotnet publish src/Test.Aot/Test.Aot.csproj -c Release -f net10.0 -r osx-arm64 -o out/aot && out/aot/Test.Aot

# Native AOT: publish the servers (any IL warning fails the publish), then run every suite against the executables
dotnet publish src/LiteGraph.Server/LiteGraph.Server.csproj -c Release -f net10.0 -r osx-arm64 -p:PublishAot=true -o out/server-aot
dotnet publish src/LiteGraph.McpServer/LiteGraph.McpServer.csproj -c Release -f net10.0 -r osx-arm64 -p:PublishAot=true -o out/mcp-aot
LITEGRAPH_TEST_SERVER_EXECUTABLE=out/server-aot/LiteGraph.Server LITEGRAPH_TEST_MCP_EXECUTABLE=out/mcp-aot/LiteGraph.McpServer \
  dotnet run --project src/Test.Automated/Test.Automated.csproj --framework net10.0

# Run the server (single node, SQLite, creates litegraph.json and litegraph.db in the current directory)
dotnet run --project src/LiteGraph.Server/LiteGraph.Server.csproj --framework net10.0

# Docker deployments (see docker/README.md)
cd docker/single-node-sqlite      && docker compose up -d && smoke.bat
cd docker/single-node-postgresql  && docker compose up -d && smoke.bat
cd docker/multi-node              && docker compose up -d && smoke.bat && failover.bat
```

The Touchstone harness starts server and MCP child processes; if a run is interrupted they can linger and lock
`bin/` outputs, so check for stray `LiteGraph.Server.dll` processes before rebuilding.

## High-Level Architecture

### Layered Architecture Pattern

LiteGraph follows a strict layered architecture with clear separation of concerns:

1. **Client Layer** (`LiteGraph.Client`): Handles input validation and cross-cutting logic
2. **Repository Layer** (`GraphRepositories`): Contains primitives and data access
3. **Storage Layer**: SQLite (with optional in-memory operation) or PostgreSQL with pgvector

### Key Architectural Components

#### Client-Repository Separation
- **Client classes** (e.g., `GraphMethods` in `Client/Implementations/`) perform validation and business logic
- **Repository classes** (e.g., `GraphMethods` in `GraphRepositories/Sqlite/Implementations/`) handle raw data operations
- Client classes call repository methods via `_Repo` field

#### Multi-Tenant Design
All operations require a `tenantGuid` parameter. The hierarchy is:
```
Tenant → Graph → Nodes/Edges → Labels/Tags/Vectors
```

#### Storage and Vector Search Pairings (v10.0)
- **SQLite + HnswLite**: vectors stored as float32 bytes; the per-process HNSW index is managed by
  `Indexing/Vector/VectorIndexManager.cs`, wrapped by `HnswLiteVectorIndex.cs`, and maintained through
  `GraphRepositories/Sqlite/Implementations/VectorMethodsWithIndex.cs`. SQLite is single-process only.
- **PostgreSQL + pgvector**: `vectors.embeddings` is a pgvector `vector` column; all vector search is SQL
  (`GraphRepositories/Postgresql/Queries/PgvectorQueries.cs`) with a shared cosine HNSW index per dimensionality.
  There is no `VectorIndexManager` on PostgreSQL. Search scores must match `Helpers/VectorHelper.cs` exactly
  (the `ScaleOut.PgvectorSearchParity` case checks every search type).
- Schema changes on PostgreSQL are numbered migrations in `PostgresqlGraphRepository.Migrations.cs`, recorded in the
  `schemamigrations` table and run under the `schema` lock.

#### Stateless Nodes and Cluster Mode (v10.0)
- In cluster mode (`Cluster.Enable`, PostgreSQL only) several server nodes share one database behind a load balancer.
  **Nodes must keep no state that another node could disagree with**: no caches that answer for data (client object
  caches and `AuthorizationService` caches are off in cluster mode), no in-process indexes, no node-local settings.
- Coordination that needs one actor at a time goes through `ILockProvider` (`LiteGraph.Coordination`): `LocalLockProvider`
  (Padlock) on a single node, `ClutchLockProvider` (server, `Clutch.Sdk` WebSocket lock connection) in a cluster. Lock keys live in
  `LockKeys`. Ordinary reads, writes, and searches must never take a distributed lock.
- Invariants are enforced with database unique constraints plus retry, not with locks.
- Nodes register in Redis (`Services/Cluster/ClusterRegistry.cs`) and poll it for settings changes and restart requests;
  `RollingRestartCoordinator` restarts nodes one at a time under the Clutch `restart` lock. Redis and Clutch are never
  required to serve a request: readiness reports `Degraded`, not unavailable, when either is down.
- The settings API reads and writes the shared settings file through `Services/SettingsFileService.cs`, which keeps
  values that came from environment variables (node identity, secrets) out of the file.
- See `docs/CLUSTERING.md`.

#### Native AOT (v10.2)
- Everything runs on the JIT (default) or as Native AOT with identical behavior: the library and SDK (`IsAotCompatible`)
  in applications, and `LiteGraph.Server`, `LiteGraph.McpServer`, `LiteGraphConsole`, `LiteGraph.SampleDatabase`, and
  `LoadGenerator` as native executables (`-p:PublishAot=true`) or native images (`Dockerfile.native`,
  `compose.native.yaml`). The solution must build with no warnings; the trim and AOT analyzers run in every project.
- The servers and tools set `JsonSerializerIsReflectionEnabledByDefault=false` in every build, so the JIT build runs the
  same JSON path as the native one; a type without metadata throws `NotSupportedException` in ordinary test runs.
- Rules everywhere: JSON goes through `Serializer` (or options built with `Serializer.CreateResolver`, or a
  `JsonTypeInfo<T>`); never `JsonSerializer` with plain options. No reflection, `XmlSerializer`, anonymous types in
  serialized values (use a named class or `Dictionary<string, object>`), non-generic `JsonStringEnumConverter`, or
  `Enum.GetValues(Type)`.
- Where metadata lives: library `Serialization/LiteGraphJsonContext.cs` (plus the parity list in
  `Test.Shared/LiteGraphTouchstoneAotSuites.cs`); SDK `LiteGraphSdkJsonContext.cs`; server
  `Classes/LiteGraphServerJsonContext.cs` (registered by `ServerJson.Register`; plus `_AotServerParityTypes` in
  `Test.Shared/LiteGraphTouchstoneAotServerSuites.cs`); MCP server `LiteGraphMcpJsonContext` (settings). MCP tool schemas
  are JSON text through `LiteGraphMcpSchema.Parse`; chat tool property schemas are JSON text in `ChatToolCatalog`.
- JSON output is pinned by baselines in `Test.Shared/Baselines` (`Aot.Serialization`, `Aot.Server`,
  `Mcp.Protocol.ToolsListBaseline`); recapture with `LITEGRAPH_CAPTURE_AOT_BASELINES=<dir>` only for intended changes.
- `LITEGRAPH_TEST_SERVER_EXECUTABLE` / `LITEGRAPH_TEST_MCP_EXECUTABLE` run the server-backed suites against native builds.
- See `docs/AOT.md`.

### Data Model Key Points

#### Graph Objects
- **Graph**: Container with optional vector indexing (HNSW)
- **Node**: Can have multiple vectors, labels, tags, and arbitrary data
- **Edge**: Connects two nodes with cost, direction, labels, tags, and data
- **Vector**: Multi-dimensional embeddings with model metadata

#### Vector Search Performance
- **With HNSW Index**: Sub-100ms search times for large datasets
- **Without Index**: Linear scan through all vectors (very slow for large datasets)
- **Index Threshold**: Configurable via `Graph.VectorIndexThreshold` property

### Important Implementation Details

#### Memory Management
- **In-Memory Mode**: Set second parameter to `true` in `SqliteGraphRepository` constructor
- **Flushing**: Must call `client.Flush()` to persist in-memory changes to disk
- **Caching**: Uses `LRUCache` for tenant, graph, node, and edge validation

#### Vector Index Search (SQLite)
On SQLite, `VectorMethods.SearchNode()` uses the HnswLite index when the graph has indexing enabled and no label, tag,
or expression filter is given, and falls back to brute force otherwise. An empty in-memory index is rebuilt from the
database on first use (`VectorIndexManager.IndexLoader`), and nodes created with inline vectors must reach the index
(`NodeMethods.StampInlineVectors`).

#### Batch Operations
All entity types support batch creation via `CreateMany()` methods for performance optimization.

#### Expression Filtering
Uses `ExpressionTree` library for filtering on the `Data` property of graphs, nodes, and edges.

## Project Structure

- **LiteGraph**: Core library and NuGet package
- **LiteGraph.Server**: REST API server with authentication
- **Test projects**: Various performance and functionality tests
- **Docker**: Container deployment configuration

## Performance Considerations

- Vector searches with 10K+ vectors require HNSW indexing for reasonable performance
- Batch operations significantly outperform individual operations
- In-memory mode provides better performance but requires manual flushing
- Cache settings can be tuned via `CachingSettings` for validation performance

## Coding Style and Implementation Rules

To maximize consistency and maintainability, all code files must follow these rules:

### Code Organization and Structure
- **Namespace declaration** should always be at the top, with `using` statements contained INSIDE the namespace block
- **Using statement order**: Microsoft and standard system libraries first (alphabetical), followed by other using statements (alphabetical)
- **Class organization**: Always use exactly five regions in this order:
  1. `Public-Members`
  2. `Private-Members` 
  3. `Constructors-and-Factories`
  4. `Public-Methods`
  5. `Private-Methods`
- **Region formatting**: Extra line break before and after region statements, unless adjacent to opening/closing braces `{` or `}`
- **File structure**: Limit each file to exactly one class or exactly one enum - no nesting multiple classes/enums

### Documentation and Naming
- **Public documentation**: All public members, constructors, and public methods must have XML code documentation
- **Private documentation**: No code documentation on private members or private methods
- **Private member naming**: Must start with underscore and be Pascal cased (e.g., `_FooBar` not `_fooBar`)
- **Documentation details**: Include default values, minimum/maximum values, and effects of different values where appropriate
- **Exception documentation**: Use `/// <exception>` tags to document which exceptions public methods can throw

### Properties and Members
- **Public properties**: Must have explicit getters and setters using backing variables when value ranges require validation
- **Configurable values**: Avoid constants for values developers may want to configure - use public members with backing private members set to reasonable defaults

### Async and Threading
- **ConfigureAwait**: Use `.ConfigureAwait(false)` where appropriate
- **CancellationToken**: Every async method should accept a CancellationToken parameter, unless the class has a CancellationToken member or CancellationTokenSource member
- **Cancellation checks**: Check for cancellation requests at appropriate places in async methods
- **IEnumerable methods**: When implementing methods that return IEnumerable, also create async variants with CancellationToken
- **Thread safety**: Document thread safety guarantees in XML comments
- **Concurrency**: Use `Interlocked` operations for simple atomic operations, prefer `ReaderWriterLockSlim` over `lock` for read-heavy scenarios

### Error Handling and Exceptions
- **Specific exceptions**: Use specific exception types rather than generic `Exception`
- **Error messages**: Always include meaningful error messages with context
- **Custom exceptions**: Consider custom exception types for domain-specific errors
- **Exception filters**: Use when appropriate: `catch (SqlException ex) when (ex.Number == 2601)`

### Resource Management
- **IDisposable**: Implement `IDisposable`/`IAsyncDisposable` when holding unmanaged resources or disposable objects
- **Using statements**: Use `using` statements or declarations for IDisposable objects
- **Dispose pattern**: Follow full Dispose pattern with `protected virtual void Dispose(bool disposing)`
- **Base disposal**: Always call `base.Dispose()` in derived classes

### Null Safety and Validation
- **Nullable reference types**: Use nullable reference types (enable `<Nullable>enable</Nullable>` in project files)
- **Input validation**: Validate input parameters with guard clauses at method start
- **Null checks**: Use `ArgumentNullException.ThrowIfNull()` for .NET 6+ or manual null checks
- **Result patterns**: Consider using Result pattern or Option/Maybe types for methods that can fail
- **Null documentation**: Document nullability in XML comments
- **Proactive null handling**: Eliminate situations where null might cause exceptions

### LINQ and Collections
- **LINQ preference**: Prefer LINQ methods over manual loops when readability is not compromised
- **Existence checks**: Use `.Any()` instead of `.Count() > 0` for existence checks
- **Multiple enumeration**: Be aware of multiple enumeration issues - consider `.ToList()` when needed
- **Safe access**: Use `.FirstOrDefault()` with null checks rather than `.First()` when element might not exist

### General Principles
- **Variable declaration**: Do not use `var` - use actual type names
- **Assumptions**: Do not make assumptions about opaque class members or methods - ask for implementation details
- **SQL statements**: If manually prepared SQL strings exist, assume there's a good reason for the implementation
- **Tuple avoidance**: Do not use tuples unless absolutely necessary
- **Code compilation**: Always compile code and ensure it's free of errors and warnings