# LiteGraph Upgrade Guide

## Upgrading From v10.1 To v10.2 (In-Place)

v10.2 makes all of LiteGraph available as Native AOT (see [Native AOT](AOT.md)). Storage, settings files, JSON output, REST and MCP behavior, and the public API are unchanged, so this is a routine in-place upgrade on SQLite and PostgreSQL: deploy the new binaries or bump the Docker image tags to `v10.2.0`. No migration runs, and rolling back to 10.1 is a matter of restoring the previous binaries.

Two behavior fixes apply:

- On machines whose time zone is not UTC, timestamps that passed through JSON were shifted by the local UTC offset; they no longer are, and `DateTime` values read through the serializer now have `Kind` `Utc`. Code that compensated by calling `ToUniversalTime()` keeps working (the call is now a no-op). Code that compensated in the other direction, or that relied on `Kind` being `Local`, should be checked.
- A server with `Rest.Ssl` configured from a PFX file can serialize its settings again (the settings API and `--showconfig` failed before).

Running natively is optional. To switch a server, MCP server, or console tool to a native executable, publish it with `-p:PublishAot=true` for the target platform and run it in place of the JIT build, with the same `litegraph.json` and environment variables; for containers, build the native images and start the deployment with its `compose.native.yaml`. Switching back is the same in reverse. See [Building native executables](AOT.md#building-native-executables) and [docker/README.md](../docker/README.md#native-aot-images).

Applications that embed LiteGraph or use the C# SDK need no changes under the JIT. To publish them with Native AOT, see [Native AOT](AOT.md#using-the-library-or-the-c-sdk-in-a-native-aot-application): classes the application stores in `Data` must be registered with `Serializer.AddTypeInfoResolver`, and anonymous objects in `Data` must become dictionaries or named classes.

Code that builds on the server project itself (custom builds, forks) should know that the server, MCP server, and tools now turn reflection-based System.Text.Json off in every build: a type serialized there needs source-generated metadata (`LiteGraphServerJsonContext`), or it fails with a `NotSupportedException` that names it.

---

## Upgrading From v9.x To v10.0 (Breaking On PostgreSQL)

**Back up first. There is no rollback to 9.x on PostgreSQL.** On its first start, 10.0 converts the `vectors.embeddings` column from raw bytes to a pgvector `vector` column and records the conversion in a new `schemamigrations` table. A 9.x server cannot read the converted column, so the only way back is restoring the backup. On PostgreSQL, take a `pg_dump` (or a volume snapshot) before anything else. On SQLite, copy the database file.

### PostgreSQL deployments

PostgreSQL now needs the pgvector extension. LiteGraph creates it on first start when the database role is allowed to (the role owns the database, or is a superuser). Otherwise the server stops with a message naming the database, and an administrator runs `CREATE EXTENSION vector;` in that database once. Amazon RDS, Azure Database for PostgreSQL, and Google Cloud SQL all offer pgvector; self-managed servers need the pgvector package installed. The Docker deployments use `pgvector/pgvector:0.8.6-pg17-trixie`, which is `postgres:17` on the same Debian base with pgvector added.

The conversion runs in batches of 1,000 rows, logs progress, and resumes where it stopped if the server is interrupted. Rows whose stored bytes are not a float32 array, or contain NaN or infinity, are skipped with a warning naming each vector; pgvector cannot store them, and they could never match a search. The migration also adds two unique indexes: one on built-in role names (duplicates left by concurrent first starts are merged; user-defined roles are never touched) and one on chat turn sequences within a thread (duplicate sequences are renumbered in order).

Vector search changes behind the same API:

- Graphs with a vector index use a pgvector HNSW index (cosine) shared by every graph with the same dimensionality, maintained by PostgreSQL on every write. `VectorIndexType` values `HnswRam` and `HnswSqlite` are still accepted and mean "pgvector index" on PostgreSQL. `VectorIndexFile` no longer applies and reads back as `null`, and index statistics report the pgvector index name, size, and validity. The first graph to enable an index for a given dimensionality sets that index's `M` and `EfConstruction`; `VectorIndexEf` still applies per search. Dimensionalities above 4,000 cannot be indexed by pgvector and are searched exactly.
- Euclidean and dot-product searches on an indexed graph now return true Euclidean and dot-product values. In 9.x they returned cosine-derived values under those names. Cosine searches return the same results as before.
- Filtered and unindexed searches run in SQL instead of loading every candidate vector into the server, which is much faster on large graphs.
- The per-process HNSW index files under `./indexes/postgresql/` are no longer used and can be deleted.

### Docker deployments

`docker/compose.yaml` is now [`docker/single-node-postgresql/compose.yaml`](../docker/single-node-postgresql/compose.yaml); see [`docker/README.md`](../docker/README.md) for all three deployments. The Compose project is named `litegraph` instead of taking its name from the `docker` directory, so its volumes are no longer shared with other projects that keep compose files in a directory called `docker`. To keep a 9.x database, back it up and set `LITEGRAPH_POSTGRESQL_VOLUME=docker_postgresql-data` in `docker/single-node-postgresql/.env` before the first start. If PostgreSQL then logs `database "litegraph" has a collation version mismatch`, the volume was created by an older Debian-bookworm `postgres:17` image; rebuild text indexes once with `REINDEX DATABASE litegraph; ALTER DATABASE litegraph REFRESH COLLATION VERSION;`.

### Behavior changes on every deployment

- **Security tokens expire.** `x-token` security tokens are now rejected after their expiry time, as they were always meant to be; 9.x accepted them indefinitely. Clients holding long-lived tokens must request new ones.
- **New environment overrides.** `LITEGRAPH_ADMIN_BEARER_TOKEN`, `LITEGRAPH_ENCRYPTION_KEY`, and `LITEGRAPH_ENCRYPTION_IV`. The server now warns at startup when the encryption key or IV is the all-zero default.
- **Health endpoints.** `GET /v1.0/health/live` and `GET /v1.0/health/ready` are new; `HEAD /` and `GET /` still answer as before. Every response carries an `x-litegraph-node` header.
- **SQLite restart fix.** An in-memory (`HnswRam`) index is rebuilt from the database on first use after a restart, instead of silently returning no results.
- **MCP settings file.** The MCP server no longer rewrites its settings file on every start to record `LastStartUtc`.
- **Settings API.** `GET /v1.0/settings` returns the settings file rather than the running settings, so values supplied by environment variables no longer appear in it. `PUT /v1.0/settings` keeps the file's value for every setting supplied by an environment variable and lists those settings in `EnvironmentOverrides`. `POST /v1.0/settings/restart` returns `{"Restarting": true, "Rolling": false, ...}` instead of `{"restarting": true}`. A new `Unavailable` error code maps to 503.

### Moving to a cluster

A single-node PostgreSQL deployment becomes a cluster by pointing several nodes at the same database with `LITEGRAPH_CLUSTER_ENABLE=true`, a Clutch lock service, a Redis instance (`LITEGRAPH_REDIS_CONNECTION_STRING`), and one shared settings file. [Clustering](CLUSTERING.md) describes the requirements and the [`docker/multi-node`](../docker/multi-node/) deployment shows a complete setup. SQLite cannot be clustered; export the data with the JSONL export API and import it into a PostgreSQL deployment first.

---

## Upgrading From v8.0 To v8.1 (In-Place)

v8.1 adds the LLM chat feature and changes nothing that already exists, so this is a routine in-place upgrade: stop the server, deploy the new binaries (or bump the Docker image tags to `v8.1.0`), and start it again. On first boot the schema initializer creates the new chat tables — endpoints, threads, turns, feedback, and settings — alongside the existing schema on both SQLite and PostgreSQL. No existing table is altered and no data migration runs. A v8.0 database opened by v8.1 simply gains the empty chat tables. Rolling back is equally simple — restore the previous binaries; the extra tables sit unused. Take the usual pre-upgrade backup regardless.

Chat is enabled by default but inert until configured: no completion runs until a tenant administrator creates a chat endpoint and either sets it as the tenant default or passes it explicitly. Operators who want the feature off entirely can set `Chat.Enable` to `false` in `litegraph.json` — the new `Chat` block and its defaults are documented in [SETTINGS.md](SETTINGS.md). A missing `Chat` block is fine; defaults apply.

---

## Upgrading To v8.0 (Breaking)

v8.0 changes the account model and the schema, so it is a clean break rather than an in-place migration. Plan for a fresh v8 deployment and move data across with the JSONL interchange that shipped in v7.1.

**What changed.** The administrator-versus-user split is gone. Accounts are ordinary user records with `IsSystemAdmin` and `IsTenantAdmin` flags; the old static administrator token survives only as a break-glass credential. The users table gains the two flag columns, and the login flow is unified (server URL, email, tenant if more than one, password). Because the schema and the account model both change, v7 databases are not read by v8.

**How to move.** Stand up a fresh v8 deployment (the `docker compose` stack seeds a default tenant and a system-administrator user). For each graph you want to carry over, export it from v7 as JSONL and import it into v8:

1. On the v7 server, `GET /v1.0/tenants/{tenant}/graphs/{graph}/export/jsonl?incldata&inclsub` and save the stream.
2. On the v8 server, `POST /v1.0/tenants/{tenant}/graphs/import/jsonl` (new graph) or `POST /v1.0/tenants/{tenant}/graphs/{graph}/import/jsonl` (merge) with the saved JSONL.

Graph data, labels, tags, vectors, and edges come across. Users, credentials, and roles are re-created in v8 — recreate the accounts you need and set the `IsSystemAdmin`/`IsTenantAdmin` flags as appropriate. The dashboard, REST, MCP, and SDKs all move to the single account model at the same time.

**Docker.** The v8 compose adds Loki and Grafana Alloy for logs and gives the LiteGraph services `restart: unless-stopped` so the Settings restart control can bring the server back with new configuration. Bump image tags to `v8.0.0`.

---

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
