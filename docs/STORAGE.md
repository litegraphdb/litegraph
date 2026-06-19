# LiteGraph Storage Configuration

LiteGraph uses provider-neutral database settings while keeping SQLite as the default zero-configuration backend.

The current implementation supports SQLite execution through the repository factory and includes an executable PostgreSQL provider backed by `NpgsqlDataSource`.

## Defaults

Default database settings:

```json
{
  "LiteGraph": {
    "Database": {
      "Type": "Sqlite",
      "Filename": "litegraph.db",
      "InMemory": false,
      "Hostname": "localhost",
      "Port": null,
      "DatabaseName": "litegraph",
      "Username": null,
      "Password": null,
      "Schema": "litegraph",
      "ConnectionString": null,
      "MaxConnections": 32,
      "CommandTimeoutSeconds": 30
    }
  }
}
```

The legacy `LiteGraph.GraphRepositoryFilename` setting is still supported. Setting it updates the SQLite filename in `LiteGraph.Database.Filename`.

## SQLite

SQLite is the default backend:

```json
{
  "LiteGraph": {
    "Database": {
      "Type": "Sqlite",
      "Filename": "litegraph.db",
      "InMemory": false
    }
  }
}
```

For a temporary in-memory repository:

```json
{
  "LiteGraph": {
    "Database": {
      "Type": "Sqlite",
      "Filename": "litegraph.db",
      "InMemory": true
    }
  }
}
```

SQLite is appropriate for local development, tests, embedded deployments, and small single-process deployments.

## Environment Variables

Server startup applies these environment variables after reading `litegraph.json`:

| Variable | Setting |
| --- | --- |
| `LITEGRAPH_DB_TYPE` | `LiteGraph.Database.Type` |
| `LITEGRAPH_DB` | `LiteGraph.GraphRepositoryFilename` |
| `LITEGRAPH_DB_FILENAME` | `LiteGraph.GraphRepositoryFilename` |
| `LITEGRAPH_DB_HOST` | `LiteGraph.Database.Hostname` |
| `LITEGRAPH_DB_PORT` | `LiteGraph.Database.Port` |
| `LITEGRAPH_DB_NAME` | `LiteGraph.Database.DatabaseName` |
| `LITEGRAPH_DB_USERNAME` | `LiteGraph.Database.Username` |
| `LITEGRAPH_DB_PASSWORD` | `LiteGraph.Database.Password` |
| `LITEGRAPH_DB_SCHEMA` | `LiteGraph.Database.Schema` |
| `LITEGRAPH_DB_CONNECTION_STRING` | `LiteGraph.Database.ConnectionString` |
| `LITEGRAPH_DB_MAX_CONNECTIONS` | `LiteGraph.Database.MaxConnections` |
| `LITEGRAPH_DB_COMMAND_TIMEOUT_SECONDS` | `LiteGraph.Database.CommandTimeoutSeconds` |
| `LITEGRAPH_TRANSACTION_MAX_OPERATIONS` | `LiteGraph.Transactions.MaxOperations` |
| `LITEGRAPH_TRANSACTION_MAX_TIMEOUT_SECONDS` | `LiteGraph.Transactions.MaxTimeoutSeconds` |

`LITEGRAPH_DB` and `LITEGRAPH_DB_FILENAME` are aliases for the SQLite filename. `LITEGRAPH_DB` takes precedence when both are set.

## Provider Selection

Embedded callers can create repositories through `GraphRepositoryFactory`:

```csharp
using LiteGraph;
using LiteGraph.GraphRepositories;

DatabaseSettings settings = new DatabaseSettings
{
    Type = DatabaseTypeEnum.Sqlite,
    Filename = "litegraph.db"
};

using GraphRepositoryBase repository = GraphRepositoryFactory.Create(settings);
using LiteGraphClient client = new LiteGraphClient(repository);

client.InitializeRepository();
```

The factory returns `SqliteGraphRepository` for `DatabaseTypeEnum.Sqlite` and `PostgresqlGraphRepository` for `DatabaseTypeEnum.Postgresql`.

The embedded C# SDK surface uses the same public storage model. `DatabaseSettings`, `DatabaseTypeEnum`, and `GraphRepositoryFactory` are the supported storage configuration API for in-process callers. There is no separate storage-admin REST model in this release because the running server's provider is selected at startup and is not mutable through an authenticated admin route.

## PostgreSQL Target Configuration

PostgreSQL is the recommended production backend. The configuration shape is:

```json
{
  "LiteGraph": {
    "Database": {
      "Type": "Postgresql",
      "Hostname": "postgres.example.internal",
      "Port": 5432,
      "DatabaseName": "litegraph",
      "Username": "litegraph",
      "Password": "use-a-secret-manager",
      "Schema": "litegraph",
      "MaxConnections": 32,
      "CommandTimeoutSeconds": 30
    }
  }
}
```

Or with a connection string:

```json
{
  "LiteGraph": {
    "Database": {
      "Type": "Postgresql",
      "ConnectionString": "Host=postgres.example.internal;Port=5432;Database=litegraph;Username=litegraph;Password=..."
    }
  }
}
```

Use a dedicated PostgreSQL database and schema for LiteGraph. Before production deployment, run the PostgreSQL provider suite by setting `LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING` against a disposable test database.

PostgreSQL supports:

- schema creation and indexes in the configured schema
- tenants, users, credentials, graphs, nodes, edges, labels, tags, vectors, request history, authorization audit, authorization roles, batch, vector index metadata, and admin repository methods
- graph-scoped transactions
- JSON data filters through PostgreSQL `jsonb` extraction, including numeric and boolean comparisons
- pooled concurrent writes through `NpgsqlDataSource`
- synchronous and asynchronous repository initialization/disposal

### Docker Compose PostgreSQL Defaults

The checked-in Docker deployment in `docker/compose.yaml` is PostgreSQL-backed by default. It starts a `postgresql` service, runs a one-shot `litegraph-postgresql-init` service, and injects the matching LiteGraph settings into the init and server containers with `LITEGRAPH_DB_*` environment variables.

Default local Docker values:

| Setting | Default |
|---------|---------|
| Host PostgreSQL port | `15432` |
| Compose PostgreSQL host | `postgresql` |
| Database | `litegraph` |
| Username | `litegraph` |
| Password | `litegraph` |
| Schema | `litegraph` |
| Data volume | `postgresql-data` |

Startup order:

1. `postgresql` starts and creates the configured database from `POSTGRES_DB` when the volume is new.
2. `litegraph-postgresql-init` waits for PostgreSQL health, runs `LiteGraph.Server --init-only`, creates the configured schema and tables through the repository setup path, seeds built-in authorization roles, creates `default@user.com` / `password` and bearer token `default`, and creates a starter graph with nodes and edges when the default graph is empty.
3. `litegraph` starts only after the init service exits successfully.
4. MCP, dashboard, Prometheus, and Grafana wait on the long-running LiteGraph service.

Override sample Docker values with:

| Variable | Purpose |
|----------|---------|
| `LITEGRAPH_POSTGRESQL_HOST_PORT` | Host port published for PostgreSQL |
| `LITEGRAPH_POSTGRESQL_DATABASE` | PostgreSQL database name |
| `LITEGRAPH_POSTGRESQL_USERNAME` | PostgreSQL username |
| `LITEGRAPH_POSTGRESQL_PASSWORD` | PostgreSQL password |
| `LITEGRAPH_POSTGRESQL_SCHEMA` | LiteGraph schema inside the database |
| `LITEGRAPH_DB_MAX_CONNECTIONS` | LiteGraph PostgreSQL pool size |
| `LITEGRAPH_DB_COMMAND_TIMEOUT_SECONDS` | LiteGraph database command timeout |

The mounted `docker/litegraph.json` and `docker/factory/litegraph.json` also use `Type = Postgresql`, `Hostname = postgresql`, and the sample credentials so factory reset preserves the PostgreSQL-backed deployment. For SQLite Docker experiments, change the JSON or override `LITEGRAPH_DB_TYPE=Sqlite` and set a SQLite filename.

### PostgreSQL Production Hardening

Use this checklist before promoting PostgreSQL-backed LiteGraph to production:

1. Create a dedicated database, schema, and database user for LiteGraph. The LiteGraph user needs ownership of the configured schema so repository initialization can create and update tables and indexes.
2. Store `LITEGRAPH_DB_CONNECTION_STRING` or `LITEGRAPH_DB_PASSWORD` in a secret manager. Do not place passwords in source-controlled `litegraph.json` files.
3. Require TLS for networked PostgreSQL traffic when LiteGraph and PostgreSQL do not run on the same trusted host or private network.
4. Set `LITEGRAPH_DB_MAX_CONNECTIONS` below the PostgreSQL server's available connection budget after accounting for other applications, migrations, monitoring, and administrative sessions.
5. Tune `LITEGRAPH_DB_COMMAND_TIMEOUT_SECONDS` for expected graph query and transaction workloads. Keep the server `Settings.RequestTimeoutSeconds` greater than or equal to the database command timeout unless a shorter HTTP timeout is intentional.
6. Tune `LITEGRAPH_TRANSACTION_MAX_OPERATIONS` and `LITEGRAPH_TRANSACTION_MAX_TIMEOUT_SECONDS` for the largest graph transaction workload the server should accept. REST transaction requests are capped by these values before execution.
7. Run `dotnet run --project src/Test.Automated/Test.Automated.csproj --framework net10.0` with `LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING` pointed at a disposable PostgreSQL database during release validation.
8. Enable regular PostgreSQL backups and test restore into a disposable database before switching production traffic.
9. Monitor `/metrics` for `litegraph_storage_backend_info`, repository operation counts, repository operation durations, HTTP errors, graph query errors, transaction rollbacks, and `litegraph.vector.index.mutation.failures`.
10. Keep PostgreSQL autovacuum enabled. Schedule `VACUUM ANALYZE` according to write volume if operational monitoring shows bloat or stale plans.
11. Rebuild file-backed vector indexes after restoring or migrating database content if vector index files were not restored with the database.
12. For high-availability deployments, place LiteGraph behind a process supervisor or orchestrator and use PostgreSQL-managed failover. LiteGraph does not implement database failover orchestration itself.
13. Re-run provider verification after PostgreSQL major-version upgrades, schema migrations, or connection-string changes.

## Provider Test Suites

SQLite tests run by default. The PostgreSQL provider suite is registered in `Test.Shared` but skips unless a dedicated test database is configured through an environment variable:

- `LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING`

This value is intentionally a connection string so test runners do not need to print or assemble credentials. When the variable is absent, the test is reported as skipped with a reason. When the PostgreSQL variable is present, the suite initializes PostgreSQL storage and runs a live provider smoke covering core CRUD, JSON data filtering, concurrent writes, and graph transaction commit/rollback.

## Logging Safety

`DatabaseSettings.ToSafeString()` redacts:

- `Password`
- `ConnectionString`

Do not log raw settings objects or connection strings from application code.

## Migration

LiteGraph includes provider-neutral migration and verification helpers in `LiteGraph.Storage.StorageMigrationManager`.

Example SQLite-to-PostgreSQL migration:

```csharp
using LiteGraph;
using LiteGraph.Storage;

DatabaseSettings source = new DatabaseSettings
{
    Type = DatabaseTypeEnum.Sqlite,
    Filename = "litegraph.db"
};

DatabaseSettings destination = new DatabaseSettings
{
    Type = DatabaseTypeEnum.Postgresql,
    ConnectionString = "Host=postgres.example.internal;Port=5432;Database=litegraph;Username=litegraph;Password=..."
};

StorageMigrationResult result = await StorageMigrationManager.MigrateAsync(
    source,
    destination,
    verify: true,
    sampleSize: 25);

if (!result.Succeeded)
{
    foreach (string difference in result.Verification.Differences)
        Console.WriteLine(difference);
}
```

The migration path copies tenants, users, credentials, graphs, nodes, edges, labels, tags, vectors, custom authorization roles, user role assignments, and credential scope assignments. Destination repositories are initialized before import, so PostgreSQL built-in roles are seeded and source built-in role references are mapped to destination built-in roles by name.

Verification compares entity counts and sampled source GUIDs in the destination. The recommended production sequence is:

1. stop writes to the SQLite deployment
2. run `StorageMigrationManager.MigrateAsync` from SQLite to PostgreSQL with verification enabled
3. review `StorageMigrationResult.Verification.Differences`
4. start LiteGraph with `Database.Type = Postgresql`
5. rebuild vector indexes if the deployment uses file-backed vector indexes and the index files were not copied with the database

## File-Backed Vector Index Artifacts

LiteGraph v7.0 uses `HnswLite` `2.0.1` for HNSW vector indexes. `HnswSqlite` index artifacts written by v7.0 include `FormatVersion = 2`, `HnswLiteVersion = "2.0.1"`, vector metadata, layer assignments, and persisted neighbor connections. The neighbor connection data is required for reload-safe indexed search after process restart.

When migrating storage providers, restoring backups, or upgrading from earlier LiteGraph builds, treat file-backed HNSW index files as derived artifacts. Back up `indexes/`, but prefer rebuilding indexes from persisted vectors unless the artifact is known to be v7.0 format. If an existing index file lacks `FormatVersion = 2`, rebuild it with `client.Graph.RebuildVectorIndex(...)`, `client.VectorIndex.RebuildVectorIndex(...)`, or `POST /v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/rebuild` before relying on indexed search results.

## Current Limits

- SQLite and PostgreSQL are implemented providers.
- Provider-specific query generation is normalized for SQLite and PostgreSQL.
- Provider-neutral migration copies repository data but does not perform online dual-write cutover or external backup orchestration.
- PostgreSQL provider coverage runs through the live provider suite when `LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING` is configured.
