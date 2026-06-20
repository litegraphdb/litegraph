# LiteGraph v7.0 Upgrade Guide

This guide covers upgrades from existing SQLite-only, pre-RBAC, or v6.x LiteGraph deployments to `v7.0.0`. It focuses on the storage, authorization, transaction, Docker, observability, and vector-index changes now merged into `main`.

## Before Upgrading

1. Stop background writers, scheduled jobs, and MCP clients that can mutate graph data.
2. Create a database backup. For SQLite, copy the database file while LiteGraph is stopped or use the existing admin backup route before stopping writes.
3. Record the current `litegraph.json` and deployment environment variables.
4. Export or record the administrator bearer token. It remains the break-glass administrator credential after the upgrade.
5. Run the current test suite or an application smoke test against the old deployment so post-upgrade behavior can be compared.

## Configuration Changes

SQLite remains the default backend. Existing deployments that use `LiteGraph.GraphRepositoryFilename`, `LITEGRAPH_DB`, or `LITEGRAPH_DB_FILENAME` can continue using those values.

New provider-neutral storage settings live under:

```json
{
  "LiteGraph": {
    "Database": {
      "Type": "Sqlite",
      "Filename": "litegraph.db"
    }
  }
}
```

PostgreSQL deployments should set either individual database fields or `LITEGRAPH_DB_CONNECTION_STRING`. See `STORAGE.md` for production hardening.

The checked-in Docker Compose deployment now starts PostgreSQL and configures LiteGraph with `LITEGRAPH_DB_TYPE=Postgresql`. Existing Docker users who want to keep SQLite for local-only evaluation must override `LITEGRAPH_DB_TYPE=Sqlite` and set a SQLite filename explicitly.

## Existing Access Behavior

Existing users and credentials retain effective access after migration. The upgrade initializes the authorization schema and seeds built-in roles. The administrator bearer token is still unconstrained by role and credential-scope assignments.

After upgrade, use `RBAC.md` to assign narrower user roles or credential scopes. Do not remove the administrator bearer token until another operational path can manage roles, credentials, and scopes.

## SQLite In-Place Upgrade

Use this path when staying on SQLite:

1. Stop LiteGraph.
2. Back up the SQLite database file.
3. Deploy the new binaries.
4. Start LiteGraph with the existing SQLite filename.
5. Verify startup logs do not report schema initialization errors.
6. Run representative reads, writes, graph transactions, native graph queries, and authorization-management calls.
7. Rebuild vector indexes if index files were not deployed with the database.

## HnswLite 2.0.1 Vector Index Upgrade

LiteGraph v7.0 uses `HnswLite` `2.0.1` explicitly. File-backed `HnswSqlite` index state written by this release includes:

- `FormatVersion = 2`
- `HnswLiteVersion = "2.0.1"`
- persisted HNSW neighbor connections for reload-safe indexed search

Before upgrading an existing deployment that uses file-backed vector indexes, back up the database, SQLite sidecar files, and the full `indexes/` directory. After upgrade, inspect each `HnswSqlite` index JSON file. If it does not include `FormatVersion` with value `2`, treat the artifact as legacy and rebuild it with `client.Graph.RebuildVectorIndex(...)`, `client.VectorIndex.RebuildVectorIndex(...)`, `POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/rebuild`, or `POST /v2.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/rebuild`.

If a legacy artifact cannot be trusted or validated, delete the index file and its `.layers` sidecar only after the database backup is complete, then rebuild the graph's vector index from persisted vectors. Do not copy stale `HnswLite` 1.x artifacts into a v7.0 deployment and assume indexed search is valid without a rebuild or a post-upgrade search validation.

## SQLite To PostgreSQL

Use this path when moving production storage to PostgreSQL:

1. Provision a dedicated PostgreSQL database, schema, and user.
2. Run the PostgreSQL provider smoke suite against a disposable database using `LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING`.
3. Stop writes to the SQLite deployment.
4. Run `StorageMigrationManager.MigrateAsync` with verification enabled.
5. Review `StorageMigrationResult.Verification.Differences`.
6. Start LiteGraph with `LiteGraph.Database.Type = Postgresql`.
7. Confirm `/metrics` reports `litegraph_storage_backend_info{provider="Postgresql",production_recommended="true"} 1`.
8. Rebuild vector indexes if file-backed vector index files were not migrated with the database.
9. Keep the SQLite backup until application smoke tests and operational dashboards are clean.

For the checked-in Docker deployment, Compose publishes PostgreSQL on `${LITEGRAPH_POSTGRESQL_HOST_PORT:-15432}` and stores data in the `postgresql-data` volume. To migrate an existing Docker SQLite deployment, stop writes, back up `docker/litegraph.db` plus SQLite sidecar files, run the migration into a disposable PostgreSQL database first, then repeat into the Compose PostgreSQL database and start the v7.0 Compose stack only after verification succeeds.

## SDK Changes

C# embedded callers should use:

- `DatabaseSettings`
- `DatabaseTypeEnum`
- `GraphRepositoryFactory`
- `LiteGraphClient.Query`
- `LiteGraphClient.Transaction`
- `LiteGraphClient.AuthorizationRoles`

Python and JavaScript SDK consumers should update to the release that includes:

- native graph query helpers
- graph transaction helpers
- role and credential-scope helpers

Existing resource CRUD calls are unchanged.

## Graph Transaction Changes

Graph transaction requests now accept `IsolationLevel` with `Default`, `ReadCommitted`, `RepeatableRead`, or `Serializable`. PostgreSQL supports `ReadCommitted`, `RepeatableRead`, and `Serializable`; SQLite supports `Default` and `Serializable` and rejects isolation levels it cannot provide exactly.

Transaction responses include additional diagnostics:

- `TransactionId`
- `State`
- `OperationCount`
- `StartedUtc`, `CompletedUtc`, `DurationMs`
- `QueueWaitDurationMs`
- `CommitDurationMs`, `RollbackDurationMs`
- `Provider`, `IsolationLevel`
- `IsolatedRepository`, `SerializedByGate`
- `ValidationFailure`
- `RetryCount`, `Retryable`, `ConcurrencyConflict`, `ProviderErrorCode`

REST transaction validation failures return HTTP `400` with a `TransactionResult` body when LiteGraph can identify the failed operation. Failures during execution return HTTP `409` with a `TransactionResult` body. Updated SDKs preserve those diagnostic bodies so callers can inspect validation and rollback details. Older clients that treat all non-2xx responses as exceptions may need to catch the response body explicitly.

PostgreSQL is the primary provider for parallel transaction write scaling. SQLite uses isolated transaction sessions for correctness, but file-level write locking still limits write throughput. Monitor `SerializedByGate`; it should be `false` for providers using transaction-local repository/session state. Monitor `QueueWaitDurationMs` and `litegraph_graph_transaction_queue_wait_duration_ms`; sustained non-zero values identify traffic flowing through the compatibility gate instead of provider-isolated sessions.

## Dashboard And Operations

The dashboard includes authorization management, API Explorer examples for query and transaction requests, request-history inspection, and links to Prometheus metrics and OpenTelemetry setup.

Prometheus metrics are exposed at `/metrics` when observability is enabled. The initial metrics endpoint is unauthenticated, so protect it with network policy or a reverse proxy when needed.

Request history remains a recent-debugging tool. Use Prometheus and OpenTelemetry for aggregate operational monitoring.

## Post-Upgrade Validation

Run these checks before reopening writes:

1. Authenticate with the administrator bearer token.
2. Read tenants, users, credentials, graphs, nodes, edges, labels, tags, and vectors.
3. Execute a native read query and a native mutation query against a non-production graph.
4. Execute a graph transaction that commits and one that rolls back.
5. Verify a scoped credential can read an allowed graph and is denied from a graph outside its allow-list.
6. Check `/metrics` for HTTP, repository, query, transaction, auth, and storage samples.
7. Open request history and confirm request IDs, correlation IDs, trace IDs, status codes, durations, and failure filters work.
8. Review operational logs for redaction of bearer tokens, passwords, connection strings, and vector payloads.

## Rollback

For SQLite, stop LiteGraph and restore the backed-up database file plus the previous binaries.

For PostgreSQL migrations, stop the new deployment, restore the previous SQLite deployment from backup, and point clients back to the previous endpoint. LiteGraph does not provide automatic dual-write rollback.

Do not continue writing to both the old and new deployments unless the application owns reconciliation.
