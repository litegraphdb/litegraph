# LiteGraph Storage Configuration

LiteGraph now uses provider-neutral database settings while keeping SQLite as the default zero-configuration backend.

The current implementation supports SQLite execution through the repository factory and includes an executable PostgreSQL provider backed by `NpgsqlDataSource`. MySQL and SQL Server are represented in the public settings model and provider folders so the storage contract can grow without another configuration redesign, but those provider implementations remain placeholders.

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

Selecting `Mysql` or `SqlServer` returns a provider-specific placeholder repository. Calling repository operations on those placeholders throws `NotSupportedException` naming the provider and operation. This keeps configuration and factory wiring testable while making the remaining provider work explicit.

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

### PostgreSQL Production Hardening

Use this checklist before promoting PostgreSQL-backed LiteGraph to production:

1. Create a dedicated database, schema, and database user for LiteGraph. The LiteGraph user needs ownership of the configured schema so repository initialization can create and update tables and indexes.
2. Store `LITEGRAPH_DB_CONNECTION_STRING` or `LITEGRAPH_DB_PASSWORD` in a secret manager. Do not place passwords in source-controlled `litegraph.json` files.
3. Require TLS for networked PostgreSQL traffic when LiteGraph and PostgreSQL do not run on the same trusted host or private network.
4. Set `LITEGRAPH_DB_MAX_CONNECTIONS` below the PostgreSQL server's available connection budget after accounting for other applications, migrations, monitoring, and administrative sessions.
5. Tune `LITEGRAPH_DB_COMMAND_TIMEOUT_SECONDS` for expected graph query and transaction workloads. Keep the server `Settings.RequestTimeoutSeconds` greater than or equal to the database command timeout unless a shorter HTTP timeout is intentional.
6. Run `dotnet run --project src/Test.Automated/Test.Automated.csproj --framework net8.0` with `LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING` pointed at a disposable PostgreSQL database during release validation.
7. Enable regular PostgreSQL backups and test restore into a disposable database before switching production traffic.
8. Monitor `/metrics` for `litegraph_storage_backend_info`, repository operation counts, repository operation durations, HTTP errors, graph query errors, and transaction rollbacks.
9. Keep PostgreSQL autovacuum enabled. Schedule `VACUUM ANALYZE` according to write volume if operational monitoring shows bloat or stale plans.
10. Rebuild file-backed vector indexes after restoring or migrating database content if vector index files were not restored with the database.
11. For high-availability deployments, place LiteGraph behind a process supervisor or orchestrator and use PostgreSQL-managed failover. LiteGraph does not implement database failover orchestration itself.
12. Re-run provider verification after PostgreSQL major-version upgrades, schema migrations, or connection-string changes.

## Provider Test Suites

SQLite tests run by default. Server-backed provider suites are registered in `Test.Shared` but skip unless a dedicated test database is configured through an environment variable:

- `LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING`
- `LITEGRAPH_TEST_MYSQL_CONNECTION_STRING`
- `LITEGRAPH_TEST_SQLSERVER_CONNECTION_STRING`

These values are intentionally connection strings so test runners do not need to print or assemble credentials. When a variable is absent, the test is reported as skipped with a reason. When the PostgreSQL variable is present, the suite initializes PostgreSQL storage and runs a live provider smoke covering core CRUD, JSON data filtering, concurrent writes, and graph transaction commit/rollback.

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

## Current Limits

- SQLite and PostgreSQL are implemented providers.
- MySQL and SQL Server provider implementations are placeholders and are not complete.
- Provider-specific query generation is normalized for SQLite and PostgreSQL; MySQL and SQL Server remain future provider work.
- Provider-neutral migration copies repository data but does not perform online dual-write cutover or external backup orchestration.
- PostgreSQL provider coverage runs through the live provider suite when `LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING` is configured; MySQL and SQL Server suites remain skipped until those providers are implemented.
