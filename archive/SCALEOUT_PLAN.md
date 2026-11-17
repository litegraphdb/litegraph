# LiteGraph Scale-Out Implementation Plan

LiteGraph today runs as one `LiteGraph.Server` process against one database. This plan makes it run as N identical server nodes in Docker behind a load balancer (Nginx by default, Switchboard as a supported alternative), all sharing one PostgreSQL database, with every product surface updated to match: backend, storage, dashboard, MCP server, all three SDKs, tests, Postman, documentation, README, and CHANGELOG. The end state of the repository is a `docker/` directory with three ready-to-run deployments: `docker/single-node-sqlite/compose.yaml`, `docker/single-node-postgresql/compose.yaml`, and `docker/multi-node/compose.yaml`. The release is **LiteGraph 10.0.0**, built on branch **`V10.0`**.

## Definition of done

The release is done when all four of these work from a clean clone, following the README literally:

1. **Single node from the CLI.** `dotnet run --project src/LiteGraph.Server/LiteGraph.Server.csproj` starts a working server on SQLite with HnswLite, and `GET /v1.0/health/ready` returns 200.
2. **Single node in Docker with SQLite.** `docker compose up -d` in `docker/single-node-sqlite/` brings up a healthy stack.
3. **Single node in Docker with PostgreSQL and pgvector.** `docker compose up -d` in `docker/single-node-postgresql/` brings up a healthy stack, and vector search runs through pgvector.
4. **Multi-node cluster in Docker with PostgreSQL and pgvector.** `docker compose up -d` in `docker/multi-node/` brings up three LiteGraph nodes, two Clutch nodes, and a load balancer, all healthy. Requests spread across nodes, and a write through any node is visible through every other node.

Each deployment has a smoke script that checks its mode, and each mode is verified by actually running it.

## Branch policy

All work happens on `V10.0`, created from `main` before the first change. `V10.0` is **not** merged to `main` until the maintainer has reviewed and tested it, and it is kept after the merge.

The design rests on one principle: **PostgreSQL is the only state, so nodes keep none.** A node that holds nothing it would have to keep in sync with other nodes needs no replication, no change feed, no invalidation protocol, and no client cooperation to be correct. Every request, from an SDK or from plain curl, is correct no matter which node answers it. What remains is ordinary coordination (who runs a background job, who migrates the schema, who writes the settings file), and that is exactly what a distributed lock is for. **Clutch** (`jchristn77/clutch-server`, `Clutch.Sdk` on NuGet) provides it. **Padlock** (NuGet package `Padlock`) provides keyed in-process locking.

Two storage pairings follow from that principle and are the only supported combinations:

| Storage | Vector index | Deployment |
|---|---|---|
| SQLite | HnswLite (in process, as today) | Single node only |
| PostgreSQL with pgvector | pgvector HNSW (in the database) | Single node or multi-node |

The plan follows the standards in `c:\code\agents\requirements`: `CODE_STYLE.md`, `BACKEND_ARCHITECTURE.md`, `BACKEND_TEST_ARCHITECTURE.md` (Touchstone), `TELEMETRY_REQUIREMENTS.md`, `FRONTEND_ARCHITECTURE.md`, `DASHBOARD_STYLE_AND_USABILITY.md`, `I18N.md`, `REPOSITORY_REQUIREMENTS.md`, `SIMULATED_USER_TESTING.md`, `VERSIONING.md`, and `WRITING_DOCUMENTS.md`. Where this plan and a requirements document disagree, the requirements document wins.

It is written to be executed and annotated, in the same convention as the archived `VERSION_8.0_PLAN.md` and `VERSION_8.1_PLAN.md`. Every task is a checkbox. Check it when the work is done and its verification passes, and leave a dated italic note beside anything deferred. **Tests expand in both directions at every layer**: each behavior gets positive and negative cases. **The dashboard gets explicit, repeated UX passes.** **Loopback is always `127.0.0.1`, never `localhost`.** **No em-dashes anywhere**, including code comments, Postman descriptions, and commit messages.

---

## Completion status

| § | Area | Status | Evidence |
|---|------|--------|----------|
| 0 | Branch, version approval, dependencies | **Done** | Branch `V10.0`; all versions 10.0.0; Pgvector 0.3.2 and Padlock 1.1.0 added; Dependabot alerts resolved. `Clutch.Sdk` 0.2.0 added to the server only. |
| 1 | pgvector for PostgreSQL | **Done** | pgvector column, SQL search for every search type, per-dimensionality cosine HNSW index, BYTEA migration; `ScaleOut.PgvectorSearchParity`, `PgvectorLegacyMigration`, `PgvectorIndexLifecycle` pass; real 9.0.0 to 10.0 upgrade verified (300 vectors). |
| 2 | HnswLite for SQLite: restart fix | **Done** | Rebuild-on-load plus inline-vector indexing fix; `ScaleOut.SqliteIndexRebuild` passes; verified across a container restart. Debounced `HnswSqlite` persistence not done. |
| 3 | Stateless nodes: caches | **Done** | Object and authorization caches off in cluster mode; caching-disabled crash fixed (`ScaleOut.CachingDisabled`); cross-node delete verified on three local nodes. Algorithm result cache not changed. |
| 4 | Cluster settings and node registry | **Done (revised)** | `ClusterSettings`/`ClutchSettings`/`RedisSettings`, env overrides, startup validation (`ScaleOut.ClusterSettings`). Node registry in Redis, not a database table (see deviations): `ScaleOut.ClusterRegistry` against a real Redis. |
| 5 | Locking: Clutch and Padlock | **Done** | `ILockProvider`, `LocalLockProvider` (Padlock, `ScaleOut.LocalLocks`), `ClutchLockProvider` on `Clutch.Sdk` (one WebSocket lock connection per node, background reconnect, lost-lock detection); schema and index-build locks; failover test cuts every node's lock connection and passes. |
| 6 | Background jobs and shared chat health | **Done (revised)** | Retention and purge jobs run on one node per cycle; chat health uses per-node probing with database resync (see deviations). |
| 7 | Startup and data-integrity races | **Done** | Schema lock, unique indexes with de-duplication, turn retry; three nodes started together produced one schema and one role set. Compat-thread race left as harmless. |
| 8 | REST server | **Done** | Health endpoints (with storage and vector index provider), `x-litegraph-node`, cluster routes (nodes, node read/restart/remove, locks, jobs, restart), rolling restart with drain, settings read from the file and saved without environment values, live settings on every node, request history `NodeId` column and filter, client address from `X-Forwarded-For` for trusted proxies (`ScaleOut.ClientAddress`, `ScaleOut.RequestHistoryNode`), SSE keepalive on chat streams (`Chat.Rest.StreamingKeepAlive`). |
| 9 | Security fixes | **Done** | Token expiry fix (`ScaleOut.TokenExpiry`), env overrides, default-key warning. |
| 10 | Observability | **Done (revised)** | `node` scrape label per target, Node variable on every dashboard, Loki host name = node id; node, state, connectivity, restart, and lock metrics (`ObservabilityService.Cluster.cs`, `InstrumentedLockProvider`); `litegraph-cluster` Grafana dashboard; OTLP `service.instance.id`/`service.namespace`; smoke checks every node `up` and Healthy in Prometheus and both dashboards provisioned. Singleton-job and per-provider vector search families folded into `litegraph_lock_acquires_total{key_class="job"}` and the existing vector search metrics. |
| 11 | MCP server | **Done** | Pointed at the load balancer; settings file no longer rewritten; read-only `cluster_status`, `cluster_nodes`, `cluster_node` on all transports (MCP.Cluster.* tests); MCP_API.md. |
| 12 | SDKs | **Done (revised)** | C#, JavaScript, Python: cluster and per-node methods, health helpers, `LastNodeId` (and node id on JS/Python errors), retry policy with backoff and jitter (POST opt-in, no retry after a stream starts); READMEs and CHANGELOGs current; JS 218, Python 416, C# live 149 tests pass. Lock and job listing and the new health fields added (JS 222, Python 420, C# live 151). Request history added to all three SDKs (JS 230, Python 435, C# live 157); the Python SDK's request log no longer contains credentials. |
| 13 | Dashboard | **Done (revised)** | Cluster page (summary, nodes table with per-node restart and remove, rolling restart progress, jobs and locks panels, auto-refresh), Administration nav gated by a `cluster` capability; Settings Cluster section (backend ranges, masked secrets, read-only node id, environment-override tags, cluster banner, caching note); vector index modal shows pgvector or HnswLite and hides the index file on pgvector; request history Node column and filter; header tooltip shows the last answering node; all ten locales; tests pass. Request history node filter is a picker filled from the cluster's nodes; the header shows a node badge in cluster mode. |
| 14 | Docker: single-node and multi-node, PostgreSQL init, image tags | **Done** | Three deployments; smoke 30/32/54 checks; failover passes (LiteGraph node, each Clutch node, Redis, rolling restart); Switchboard profile verified; upgrade path verified; `build-all.sh` now calls new `.sh` build scripts. `DOCKERHUB_README.md` added. |
| 15 | Tests, including deployment testing | **Partial** | `ScaleOut` suite (15 cases, run in CI with PostgreSQL and Redis), MCP and chat keepalive cases, route guards; full Touchstone 817 cases with PostgreSQL and Redis; smoke 63 checks and failover (node, each Clutch node, Redis, rolling restart) on the cluster; cluster REST load test at 1 and 3 nodes in PERF_SCALE_TESTING.md (1.86x throughput). Not done: a Touchstone multi-process cluster harness (the Docker smoke and failover scripts cover cluster behavior), simulated user testing, and the pgvector versus HnswLite recall and backfill performance runs. |
| 16 | Postman and documentation | **Done** | CLUSTERING.md, docker/README.md, README, UPGRADE, SETTINGS, STORAGE, REST_API, CHANGELOG, CLAUDE.md, Postman `v10.0 > Health` and `v10.0 > Cluster`. |
| 17 | Release closeout | **Open** | Awaiting maintainer review; `V10.0` not merged. |

### Definition of done (2026-09-29)

All four definition-of-done checks pass, each verified by running it with images built from this branch (`v10.0.0-local`, not pushed):

1. **Single node from the CLI**: `dotnet run --project src/LiteGraph.Server/LiteGraph.Server.csproj --framework net10.0` from an empty directory; readiness 200; vector create, index, search, and delete round trip passes. (`--framework` is required because the server targets net8.0 and net10.0; the README says so.)
2. **Single node, Docker, SQLite**: `docker/single-node-sqlite`, all services healthy, smoke 30/30, index survives a container restart.
3. **Single node, Docker, PostgreSQL + pgvector**: `docker/single-node-postgresql`, all services healthy, smoke 32/32 including pgvector 0.8.6 and a built HNSW index.
4. **Multi-node cluster, Docker, PostgreSQL + pgvector**: `docker/multi-node`, all services healthy in about a minute, smoke 54/54 (requests spread to every node through Nginx and Switchboard, writes visible and search results identical through every node, every node registered healthy in Redis and returning the same settings, Clutch and Redis healthy, role separation both ways), failover passes (0.67% errors while a LiteGraph node restarts; none while each Clutch node in turn is down; 0% while Redis is down, with restart requests refused and every node reconnecting; 0.36% during a rolling restart of all three nodes, never more than one out of service). With both Clutch nodes down, reads still succeed and readiness reports `Degraded` with 200.

### Deviations from this plan, and why

- **Readiness does not require Clutch.** The plan had readiness return 503 while Clutch is down. Switchboard and the container healthchecks use readiness, so that would take every node out of rotation during a Clutch outage even though reads, writes, and searches take no lock. Readiness now returns 200 with `Status` `Degraded` and `Checks.Clutch` false; only the database and draining make it 503.
- **Redis for the node registry and change signals, not a database table.** At the maintainer's direction (2026-09-30), nodes register in a Redis hash and poll Redis keys for settings changes (`settings:version`, `settings:changed-utc`) and restart requests (`restart:version`, `restart:requested-utc`). Redis runs without persistence: registry entries are rewritten every heartbeat, and a restart signal is compared with each node's own start time, so a Redis restart can lose a signal but never repeat one. Redis is never needed to serve a request; readiness reports `Degraded` without it.
- **The settings file stays the source of settings.** Settings are not moved into the database. `GET /v1.0/settings` returns the shared file, and `PUT` keeps the file's value for every path that came from an environment variable or was derived at startup (found by comparing the file with the running settings at startup), so node identity and secrets never reach the shared file.
- **Rolling restart is a process exit under the Clutch `restart` lock.** The node holding the lock waits until no peer is `Restarting` or `Draining`, reports `Restarting`, drains for `Cluster.RestartDrainMs`, shuts down cleanly, and the container restart policy brings it back. A node outside a supervisor stays stopped.
- **Nginx re-resolves node names** (`resolver 127.0.0.11`, `resolve` on upstream servers, both load balancers). Found by the rolling restart test: after `docker compose up` recreated the nodes, `litegraph-lb` kept routing to their old addresses, and it would have done the same after every `update.bat`.
- **`clutch-lb` uses `least_conn` and a 2-second connect timeout, not `ip_hash`.** Nginx's `ip_hash` hashes only the first three octets of an IPv4 address, so every container on the Docker network landed on the same Clutch node, and a stopped container's address does not refuse connections, so with the default 60-second connect timeout reconnects never failed over. Found by stopping the Clutch node that actually held the connections; `failover.ps1` now stops each Clutch node in turn.
- **Chat endpoint health is not shared state.** Each node probes independently (every node's view is real) and resyncs its endpoint list from the database every `Cluster.EndpointResyncIntervalMs`. This avoided new tables in both providers for no correctness gain; the cost is N times the probe traffic.
- **No `VectorIndexMetric` setting.** HnswLite has always been cosine-only, so PostgreSQL gets one cosine index per dimensionality; the other search types run exactly in SQL. This also fixed 9.x returning cosine-derived values for Euclidean and dot-product searches on indexed graphs.
- **Vectors are written as pgvector text literals**, matching the repository's hand-written SQL style, rather than as Npgsql parameters.
- **No `Pgvector` enum value.** `HnswRam` and `HnswSqlite` mean "pgvector index" on PostgreSQL, which avoided SDK and dashboard enum churn.
- **Switchboard uses a catch-all route.** Switchboard 5.2 supports `{*path}`, so `sb.json` has one route per method and no route-sync test is needed. Power-of-two-choices balancing replaced least-connections, which sends every sequential request to the first idle origin.
- **Docker project name `litegraph` plus `LITEGRAPH_POSTGRESQL_VOLUME`, not pinned legacy volume names.** On this development machine many unrelated projects also produce `docker_*` volumes, so silently reusing `docker_postgresql-data` could convert another product's database. Keeping a 9.x volume is an explicit opt-in, documented in UPGRADE.md.
- **PostgreSQL image is `pgvector/pgvector:0.8.6-pg17-trixie`**, pinned, on the same Debian base as current `postgres:17`; the plain `pg17` tag is bookworm-based and produced a collation-version warning on an upgraded 9.x volume.
- **Additional fixes found along the way**: SQLite nodes created with inline vectors never reached the HnswLite index (single create skipped it; bulk create dropped vectors without a node GUID); the server passed caching settings after constructing the client; a legacy 2-byte embedding crashed the conversion (now skipped with a warning).

### Still open

Simulated user testing (§15) and the pgvector recall and backfill performance runs (§15). Vector search result loading is batched (PERF_SCALE_TESTING.md); the remaining single-connection search time is spent outside the database query and is the next place to look.
---

## What blocks scale-out today

A survey of the codebase (2026-09-28, `main` at `3cb046c`) found the request path already close to stateless. Authentication reads credentials from the database on every request, security tokens are self-contained encrypted blobs, pagination continuation tokens are object GUIDs re-read from storage, chat threads and turns are persisted rather than held in memory, and all primary keys are client-generated GUIDs. PostgreSQL is already a supported, pooled, shared backend. The blockers are the places where a node holds state of its own.

**The vector index lives in process memory.** `VectorIndexManager` holds an HNSW index per graph per process. `HnswRam` starts empty on load and is never rebuilt from the database, and a search against an empty index returns an empty list rather than `null` (`HnswLiteVectorIndex.cs:236-239`), so `SearchNode` does not fall back (`Postgresql/Implementations/VectorMethods.cs:782-785`). On a second node, and on one node after a restart, indexed search silently returns nothing. `HnswSqlite` rewrites a whole JSON file per mutation, so two nodes sharing it would overwrite each other.

**PostgreSQL does not know it is storing vectors.** Embeddings live in `vectors.embeddings`, a `BYTEA` column (declared `BLOB` in `Postgresql/Queries/SetupQueries.cs:246` and rewritten to `BYTEA` by `PostgresqlSqlTranslator.cs:88`). `Converters.VectorToBlob` (`Postgresql/Converters.cs:825`) packs the floats as 4-byte `BitConverter` values (little-endian on every platform LiteGraph ships for), and `BytesToHex` inlines them into the SQL text as an `X'...'` hex literal (`VectorQueries.cs:30`, `:604`), about 8 characters per float, so a 1,536-dimension vector is roughly 12 KB of SQL text. Every filtered or unindexed search loads candidate rows into C# and compares them in `CompareVectors`. That is slow at any node count, and it is also why the only fast path today is the in-memory index that cannot be shared.

**Caches are node-local.** The `LiteGraphClient` tenant, graph, node, and edge caches have no TTL and cannot actually be disabled: the server builds the client before assigning `Caching`, and the method classes call `AddReplace` without null checks. The authorization caches in `AuthorizationService` are invalidated by a process-static counter (`AuthorizationPolicyChangeTracker`), so a permission revoked through one node stays in force on another until it restarts. The algorithm result cache invalidates on node and edge counts only.

**Background work runs on every node.** `ChatEndpointHealthService` probes every endpoint from every node, keeps health state and history in memory, and learns about endpoint changes only through local calls. The chat retention sweep and request-history purge run N times.

**Startup and some writes assume one writer.** Every node runs schema DDL and built-in role seeding at startup; seeding is read-then-insert with no unique constraint, so simultaneous starts create duplicate roles. Chat turns take `MAX(sequence) + 1` with no unique constraint. The OpenAI-compatible chat surface finds-then-creates threads.

**Configuration and operations are node-local.** `PUT /v1.0/settings` writes the file on whichever node received it, and `POST /v1.0/settings/restart` exits whichever node received it. `Encryption.Key`/`Iv` and `AdminBearerToken` have no environment overrides. `Storage.BackupsDirectory` is ignored by the server. There is no database-aware readiness endpoint, metrics carry no instance identity, `X-Forwarded-For` is ignored, request history does not record the serving node, and `SseKeepAliveSeconds` is never used.

**Two single-node bugs are fixed here too.** Server security tokens never expire (`LiteGraph.Server/Classes/AuthenticationToken.cs:28-33` compares `TimestampUtc > ExpirationUtc`). Disabling caching throws a `NullReferenceException`.

---

## Decisions that shape this work

Items marked **(confirm)** need maintainer sign-off in §0. Everything else is binding unless a task revisits it.

- **Nodes are stateless.** No per-node cache, index, or registry is authoritative for anything. If a node needs to know something, it asks PostgreSQL. That removes every cross-node synchronization problem instead of solving it.
- **PostgreSQL uses pgvector; SQLite uses HnswLite.** On PostgreSQL, embeddings move to a pgvector column and indexed search is a SQL query against a pgvector HNSW index, so every node sees the same index. HnswLite remains only for SQLite, which is single-node by definition. `VectorIndexManager` is never constructed for PostgreSQL.
- **Multi-node requires PostgreSQL.** The server refuses to start with `Cluster.Enable = true` on SQLite.
- **Caches that could go stale across nodes are off in cluster mode.** Existence checks and authorization lookups read the database. Authentication already reads the database on every request; a few more indexed reads is not a meaningful cost, and correctness does not depend on invalidation.
- **Clutch is used only for coordination:** schema migration at startup, singleton background jobs, pgvector index builds, settings file writes, and rolling restarts. Ordinary reads and writes take no distributed lock, and nothing on the request path depends on Clutch being up.
- **Invariants are database constraints.** Unique role names and unique chat turn sequences are enforced by unique indexes plus retry, not by locks.
- **Settings are cluster-wide in one shared file.** Writes go through the Clutch `settings` lock. Every node re-reads the file when its content hash changes (a local file check on a timer; no database or network traffic) and applies live fields. Restart-required fields take effect through a rolling restart.
- **Nodes are named.** The multi-node compose file defines `litegraph-1`, `litegraph-2`, `litegraph-3` with stable `LITEGRAPH_NODE_ID` values, so metrics, logs, request history, and the dashboard stay readable across container recreation.
- **Nginx is the default load balancer; Switchboard is a supported profile.** Nginx fronts the nodes on host port `8701`, so the dashboard, SDKs, MCP server, and existing documentation keep working unchanged. Switchboard runs under the compose profile `switchboard` on host port `8711`. Switchboard has no catch-all route, so its `sb.json` enumerates every LiteGraph route and a test keeps it in sync.
- **The MCP server stays a single instance** pointed at the load balancer. Voltaic tracks MCP sessions in process, so scaling it needs load-balancer session affinity and is deferred.
- **Compose files run whatever image tag you built.** Every LiteGraph image reference is `${LITEGRAPH_IMAGE_TAG:-v<version>}`, so `build-all.bat <tag>` followed by setting `LITEGRAPH_IMAGE_TAG=<tag>` runs any build in either deployment. CI builds its own images locally and never pushes. The build scripts move `:latest` whenever a release tag (a plain `vMAJOR.MINOR.PATCH`) is built, and leave it alone for any other tag, so testing an `-rc` build does not change what `latest` users pull (confirmed by the maintainer 2026-09-30; done).
- **PostgreSQL is initialized by scripts, and the application maintains it.** Init scripts create roles, databases, and the pgvector extension on a fresh volume. They run once. Everything that must also happen on an existing volume is a LiteGraph migration, never an init script.
- **Single-node PostgreSQL also moves to pgvector.** One PostgreSQL code path, not two. The single-node compose file switches to the `pgvector/pgvector:pg17` image, which is the stock `postgres:17` image plus the extension, so the existing data volume is compatible.
- **Version: 10.0.0 (approved by the maintainer 2026-09-29).** A major version because a 9.0 PostgreSQL deployment cannot upgrade without the pgvector extension, and cannot roll back to 9.0 after the embedding migration clears the old column. `docs/UPGRADE.md` leads with "back up before upgrading; there is no rollback to 9.x". Other visible changes carried by the major: `VectorIndexType` semantics on PostgreSQL, token expiry enforcement, and the Docker directory move.

---

## 0. Branch, version approval, and dependencies (do first)

- [x] Create and switch to branch `V10.0` from `main`, **before any other change**. All work commits there. *(2026-09-29)*
- [x] Maintainer approval of the version: **10.0.0**. *(2026-09-29)*
- [ ] Get maintainer answers on the remaining **(confirm)** items and record them here with the date.
- [ ] Bump every version `9.0.0 -> 10.0.0`: all `.csproj` files, MCP `ServerVersion`/`SoftwareVersion`/`Constants.Version`, REST OpenAPI `Info.Version`, `sdk/csharp`, `sdk/js/package.json`, `sdk/python/setup.cfg` and `__init__.py`, `dashboard/package.json`, every `jchristn77/litegraph*` image tag in every compose file, and `SoftwareVersion` in every `docker/**/*.json`.
- [ ] Add `Pgvector` (the official .NET package, which registers the `vector` and `halfvec` types with Npgsql) to `src/LiteGraph/LiteGraph.csproj`.
- [ ] Add `Padlock` (1.1.0 or later) to `src/LiteGraph/LiteGraph.csproj`.
- [ ] Add `Clutch.Sdk` (0.2.0 or later) to `src/LiteGraph.Server/LiteGraph.Server.csproj` only. Confirm it restores for `net8.0` and `net10.0`. If it does not, raise it with the Clutch maintainer rather than vendoring.
- [ ] Confirm the pinned Clutch server image contains `curl` (`docker run --rm --entrypoint sh jchristn77/clutch-server:v0.2.0 -c "which curl"`). If not, raise it with the Clutch maintainer; `REPOSITORY_REQUIREMENTS.md` item 11 requires cURL healthchecks.
- [x] Resolve open Dependabot alerts on `V10.0` (next 16.3.7, sharp 0.35.5, js-yaml 4.3.2, fflate); `npm audit` clean in `dashboard/` and `sdk/js/`, dashboard 1,330 tests and build green, JS SDK 198 tests green. *(2026-09-29, commit `6a96cde`)*
- [ ] Commit: `chore: scale-out baseline, add Pgvector, Padlock, Clutch.Sdk`.
- [ ] **Verification:** `dotnet build src/LiteGraph.sln` clean with zero warnings on both target frameworks; `Clutch.Sdk` does not appear in `dotnet list src/LiteGraph/LiteGraph.csproj package`.

---

## 1. pgvector for PostgreSQL

The biggest piece of work, and the one that makes the rest simple. The current storage format makes the migration mechanical: `embeddings` is a flat little-endian float32 array with no framing, and `dimensionality` is already a column on every row.

### 1.1 Spike (do first, record findings here)

- [ ] Confirm which distance HnswLite uses for each graph today, so the pgvector operator class chosen per graph gives the same results.
- [ ] Confirm the exact formulas in `CompareVectors` (`Postgresql/Implementations/VectorMethods.cs:1058-1081`) for `CosineDistance`, `CosineSimilarity`, `EuclidianDistance`, `EuclidianSimilarity`, and `DotProduct`, and map each to pgvector (`<=>` cosine distance, `<->` L2 distance, `<#>` negative inner product) plus the arithmetic needed to return identical values.
- [ ] Check for rows where `dimensionality` disagrees with `octet_length(embeddings) / 4`, and decide how the backfill treats them (recommended: trust the byte length, log the mismatch, correct the column).
- [ ] Measure the dimension distribution in a realistic dataset. pgvector HNSW indexes support up to 2,000 dimensions on `vector` and up to 4,000 on `halfvec`.
- [ ] Confirm the pgvector version in `pgvector/pgvector:pg17` supports iterative index scans (`hnsw.iterative_scan`, pgvector 0.8+), which keeps filtered searches (labels, tags, graph) from under-returning.
- [ ] Confirm whether the application database role can run `CREATE EXTENSION vector`, in the compose image and on the managed services named in the docs (Amazon RDS, Azure Database for PostgreSQL, Google Cloud SQL all offer pgvector).
- [ ] Record each answer with the date. Anything that contradicts §1.2 through §1.5 gets resolved with the maintainer before implementation.

### 1.2 Schema, writes, and migration

- [ ] Introduce a `schemamigrations` table (`version`, `description`, `appliedutc`) per `BACKEND_ARCHITECTURE.md` "Migrations". Existing `IF NOT EXISTS` statements stay; every change in this plan is a numbered, tracked migration.
- [ ] Migration: `CREATE EXTENSION IF NOT EXISTS vector`. If it fails for lack of privilege, stop startup with a specific exception naming the fix ("ask your database administrator to run CREATE EXTENSION vector in database X, or grant the role permission to").
- [ ] Migration: add `vectors.embedding vector` (no fixed dimension, so mixed dimensions keep working).
- [ ] Backfill in SQL, in batches of 5,000 rows, resumable (`WHERE embedding IS NULL AND embeddings IS NOT NULL`), logging progress. The byte-to-float conversion can be done server-side with `get_byte` arithmetic or, more simply and verifiably, in application code with the existing `Converters.BlobToVector` followed by a parameterized `UPDATE`. Pick one in the spike and record why. Runs under the Clutch `schema` lock in cluster mode and inside the one-shot init container in compose.
- [ ] After backfill, `embedding` is canonical on PostgreSQL. Reads use `embedding`. `embeddings` is set to `NULL` for migrated rows to reclaim space, and the column itself is dropped in the next major version (noted in `docs/UPGRADE.md`).
- [ ] **Writes use parameters, not hex literals.** Vector insert, bulk insert, and update on PostgreSQL bind `Pgvector.Vector` values as Npgsql parameters instead of inlining `BytesToHex(VectorToBlob(...))` into the SQL string. Bulk inserts use Npgsql binary `COPY` or multi-row parameterized inserts, whichever the spike shows is faster at 10K rows. Every other column in those statements keeps the existing hand-written SQL style.
- [ ] SQLite schema and write path are unchanged.

### 1.3 Indexes

pgvector indexes need a fixed dimension, and LiteGraph allows mixed dimensions, so indexes are expression indexes per dimension and operator class.

- [ ] Add graph setting `VectorIndexMetric` (`Cosine`, `Euclidean`, `InnerProduct`; default from the §1.1 finding). It decides which operator class the graph's searches are indexed for. Searches using a different metric are still correct; they run exact in SQL.
- [ ] Enabling an index on a PostgreSQL graph ensures an index exists for `(dimensionality, metric)`: `CREATE INDEX CONCURRENTLY IF NOT EXISTS idx_vectors_hnsw_{metric}_{dim} ON vectors USING hnsw ((embedding::vector({dim})) vector_{metric}_ops) WHERE dimensionality = {dim}`. Use `halfvec({dim})` with `halfvec_{metric}_ops` for 2,001 to 4,000 dimensions. Above 4,000 there is no index and searches run exact in SQL; the enable call says so in its response.
- [ ] Index creation takes the Clutch `vectorindex/{metric}/{dim}` lock (cluster mode) or a Padlock key (single node), so two nodes never build the same index. `CONCURRENTLY` keeps writes flowing during the build.
- [ ] Rebuild on PostgreSQL is `REINDEX INDEX CONCURRENTLY` for the graph's `(dimensionality, metric)` index, under the same lock.
- [ ] Disable on PostgreSQL clears the graph's index settings only. The maintenance job (§6) drops a dimension index once no graph with that `(dimensionality, metric)` has indexing enabled.
- [ ] Per-graph HNSW parameters: `VectorIndexEf` maps to `SET LOCAL hnsw.ef_search` for that search's transaction. `VectorIndexM` and `VectorIndexEfConstruction` apply at index build time and are shared by every graph using that dimension index; take them from cluster-level defaults in a new `Settings.LiteGraph.Vectors` block and document that per-graph values are ignored on PostgreSQL.
- [ ] `VectorIndexType` on PostgreSQL: any value other than `None` means "pgvector HNSW". `HnswRam` and `HnswSqlite` are accepted for compatibility and reported back as-is; `VectorIndexFile` is ignored and returned as `null`. Add `Pgvector` as an explicit enum value for new graphs, documented as PostgreSQL-only (rejected with 400 on SQLite).
- [ ] `vectorindexdirty` is meaningless on PostgreSQL (the index is always consistent with the table) and is ignored there.

### 1.4 Search

- [ ] Replace the PostgreSQL search path (`SearchGraph`, `SearchNode`, `SearchEdge`) with SQL: filters (tenant, graph, labels, tags, and expression predicates where they already compile to SQL) in `WHERE`, `ORDER BY` the pgvector operator against a bound query vector, `LIMIT topK`. For indexed dimensions, `WHERE` includes `dimensionality = @dim` and the matching expression cast so the planner uses the partial index.
- [ ] Filters that cannot be expressed in SQL today keep today's semantics (applied in application code), but candidates still come from SQL ordered by distance, never from a full table load.
- [ ] Score and distance values equal today's C# results within floating-point tolerance for every `VectorSearchTypeEnum`.
- [ ] Threshold filters on score or distance become `WHERE` predicates on the computed distance.
- [ ] Delete the PostgreSQL uses of `VectorIndexManager`, `VectorMethodsWithIndex`, the post-commit index staging in `PostgresqlGraphRepository`, and `./indexes/postgresql/` handling.
- [ ] **Verification:** `Vectors.Pgvector` suite (§15): every search type, with and without filters, indexed and unindexed dimensions, `vector` and `halfvec` ranges, parity against SQLite brute-force results on the same dataset, migration from a 9.0 database with populated `embeddings`, resumed migration after interruption, dimensionality mismatch handling, and the missing-privilege failure message.

---

## 2. HnswLite for SQLite: restart fix

SQLite is single-node, so HnswLite keeps working as it does today with correctness fixes.

- [ ] When the index has no entry point but the graph has vectors, `HnswLiteVectorIndex.SearchAsync` returns `null` (cannot answer) so the caller falls back to brute force. An empty list stays correct only when the graph has zero vectors.
- [ ] On first load, an empty `HnswRam` index (or an `HnswSqlite` file older than the graph's last vector change) is built from the `vectors` table in pages, with cancellation. Searches during the build fall back to brute force.
- [ ] `HnswSqlite` persists on a debounce (`VectorIndexSnapshotIntervalSeconds`, default 60) and on graceful shutdown instead of rewriting the whole file per mutation.
- [ ] Wire `Storage.IndexesDirectory` (new, default `./indexes/`) and `Storage.BackupsDirectory` from server settings into the client; today the server passes no storage settings (`LiteGraphServer.cs:565`).
- [ ] Unload a graph's index on graph delete.
- [ ] Replace the per-graph `ConcurrentDictionary<Guid, SemaphoreSlim>` in `VectorIndexManager` with `Padlock<Guid>`.
- [ ] **Verification:** index a graph, restart the server, the same search returns the same results; a zero-vector graph returns empty without fallback; search during rebuild is correct.

---

## 3. Stateless nodes: caches

- [ ] Fix `LiteGraphClient` construction so `CachingSettings` is passed to the constructor instead of assigned afterwards (`LiteGraphServer.cs:566`).
- [ ] Make `Caching.Enable = false` work by giving the method classes a no-op cache implementation when disabled, so call sites stay unconditional. Regression case: full CRUD pass with caching disabled.
- [ ] In cluster mode, force `Caching.Enable = false` (logged at startup; the settings page shows the field as overridden by cluster mode).
- [ ] Authorization: add `Settings.AuthorizationCache.Enable` (default `true` single-node, forced `false` in cluster mode). When off, `AuthorizationService` resolves roles and effective policy from the database per request. Add indexes if the query plans show sequential scans.
- [ ] `AlgorithmResultManager`: disabled in cluster mode (results computed per request). Single-node behavior unchanged.
- [ ] Measure per-request latency and database queries per request with caches off (§15) and record the numbers here.
- [ ] **Verification:** two server processes against one database: delete a node through A, then creating an edge to it through B fails; revoke a role through A and the next request through B is denied; algorithm results through B reflect an edit through A.

---

## 4. Cluster settings and node registry

- [ ] `ClusterSettings` (`src/LiteGraph.Server/Classes/ClusterSettings.cs`) with XML docs stating default, range, and effect for each member:
  - `Enable` (default `false`).
  - `ClusterName` (default `litegraph`, 1..64 chars, `[a-z0-9-]`). Prefixes Clutch lock keys so clusters can share a Clutch deployment.
  - `NodeId` (default: container hostname), `NodeName` (default: `NodeId`).
  - `HeartbeatIntervalMs` (default 5000, clamp 1000..60000), `NodeStaleAfterMs` (default 20000), `NodePruneAfterMs` (default 86400000).
  - `SettingsReloadIntervalMs` (default 5000, clamp 1000..60000).
  - `DrainTimeoutMs` (default 30000), `RollingRestartNodeTimeoutMs` (default 180000).
  - `TrustForwardedHeaders` (default `false`), `TrustedProxies` (CIDR list), `ForwardLimit` (default 1).
  - `AllowInsecureDefaults` (default `false`; `true` only in the demo compose file).
  - `Clutch`: `Endpoint`, `AccessKey` (secret), `DefaultLeaseMs` (default 30000), `AcquireTimeoutMs` (default 10000), `ReconnectBackoffMs` (default 1000).
- [ ] Environment overrides: `LITEGRAPH_CLUSTER_ENABLE`, `LITEGRAPH_CLUSTER_NAME`, `LITEGRAPH_NODE_ID`, `LITEGRAPH_NODE_NAME`, `LITEGRAPH_CLUTCH_ENDPOINT`, `LITEGRAPH_CLUTCH_ACCESS_KEY`, `LITEGRAPH_TRUSTED_PROXIES`.
- [ ] Startup validation in cluster mode, each a specific exception with an actionable message: database is not PostgreSQL; `InMemory` is set; Clutch endpoint or key missing; encryption Key/Iv all-zero or `AdminBearerToken` default while `AllowInsecureDefaults` is false.
- [ ] Log resolved cluster configuration at startup with secrets masked.
- [ ] Node registry, for visibility and rolling restarts only (never for correctness): `clusternodes` table in both providers (`nodeid`, `clustername`, `nodename`, `hostname`, `softwareversion`, `state` of `Starting`/`Ready`/`Draining`/`Stopped`, `settingshash`, `restartrequestedutc`, `startedutc`, `lastheartbeatutc`). `ClusterService` registers, heartbeats, and records state transitions. A node is shown `Stale` when its heartbeat is older than `NodeStaleAfterMs`.
- [ ] Every `Cluster` field annotated live or restart-required for the settings API.
- [ ] **Verification:** `Cluster.Settings` and `Cluster.Registry` suites on SQLite (single node) and PostgreSQL, including every validation failure.

---

## 5. Locking: Clutch and Padlock

- [ ] `src/LiteGraph/Coordination/ILockProvider.cs`: `AcquireAsync(key, mode, options, token)` and `TryAcquireAsync` (fail fast, returns `null`). `ILockHandle : IAsyncDisposable, IDisposable` with `Key`, `Mode`, `FencingToken`, `IsHeld`, and `LostToken` (cancelled if the lease is lost). Supporting types one per file: `LockModeEnum` (`Read`, `Write`, `Exclusive`), `LockAcquireOptions`, `LockNotAcquiredException`, `LockProviderUnavailableException`.
- [ ] `LockKeys` holds the full key catalogue: `schema`, `settings`, `restart`, `job/{name}`, `vectorindex/{metric}/{dim}`.
- [ ] `LocalLockProvider` (core library) on `Padlock<string>`. Used in single-node mode and by embedded library users.
- [ ] `ClutchLockProvider` (server) on one long-lived `ClutchLockClient` WebSocket per process with reconnect and backoff. Mode mapping: `Read -> Read`, `Write -> Write`, `Exclusive -> Delete` (Clutch's fully exclusive mode). When the socket closes, Clutch releases that session's locks, so the provider marks every handle not held and cancels `LostToken`. Work done under a lock links `LostToken` into its own cancellation.
- [ ] When Clutch is unreachable: settings writes, index builds, and restarts return `503` naming Clutch; singleton jobs skip the cycle; startup schema migration retries, then exits non-zero so the container restarts. Reads and ordinary writes are unaffected because they take no distributed lock.
- [ ] Settings writes record the fencing token they ran under in the file and refuse to overwrite a file written under a higher token.
- [ ] Padlock replaces the remaining keyed-lock dictionaries: Ollama preload de-duplication and compat thread creation in `ChatService`, and settings file writes.
- [ ] **Verification:** `Cluster.Locking` suite against real Clutch and against a small `FakeClutchServer` (implements `welcome`, `acquire`, `acquired`, `denied`, `release`, `released`, `heartbeat`) so lock tests run without Docker: exclusivity across two providers, fail-fast denial, wait with timeout, lease loss cancels `LostToken`, fencing tokens increase, `503` on settings write while Clutch is down, CRUD unaffected while Clutch is down.

---

## 6. Background jobs and shared chat health

- [ ] `SingletonJobRunner` (server): runs a named job on its interval only while holding `job/{name}` (`Exclusive`, fail fast); the job's cancellation is linked to `LostToken`. Single-node always acquires, so behavior is unchanged.
- [ ] Move onto it: chat retention sweep (`ChatService.cs:86`), request-history purge (`RequestHistoryService.cs:322-365`), stale node pruning, unused pgvector index cleanup (§1.3), authorization audit retention if present.
- [ ] Chat endpoint health: add `chatendpointhealth` (current state per endpoint, including consecutive success and failure counters) and `chatendpointhealthhistory` (24-hour window, pruned by the retention job). Probing runs only on the holder of `job/chat-endpoint-health`, which writes results to the tables. The health routes on every node read the tables, so every node reports the same state. The probing node re-reads the endpoint list from the database each cycle, so endpoint changes made through any node are picked up without notifications. A new leader resumes from the stored counters.
- [ ] Per-node limits (`MaxConcurrentChats`, per-endpoint `MaxConcurrentRequests`) stay per node. Document that the effective cluster limit is N times the setting.
- [ ] **Verification:** three nodes, exactly one probes (count requests at `FakeLlmServer`); every node returns identical health; killing the leader moves probing to another node within `DefaultLeaseMs` plus one interval; an endpoint added through a non-leader is probed within one cycle.

---

## 7. Startup and data-integrity races

- [ ] Schema setup and migrations run under the Clutch `schema` lock in cluster mode. The compose files keep the one-shot `litegraph-init` container (`--init-only`) as the primary path.
- [ ] Built-in roles: a migration de-duplicates `authorizationroles` by `(tenantguid, name)` (re-pointing `userroleassignments` to the survivor), then adds a unique index. Seeding uses `ON CONFLICT DO NOTHING` (PostgreSQL) and `INSERT OR IGNORE` (SQLite).
- [ ] Chat turns: a migration de-duplicates, then adds a unique index on `chatturns (threadguid, sequence)`. `PersistTurn` retries on unique violation (`catch (PostgresException ex) when (ex.SqlState == "23505")` and the SQLite constraint equivalent), up to 5 times.
- [ ] Compat threads: a unique index on the compat thread identity columns; find-or-create becomes insert-or-select.
- [ ] Audit the repositories for any other read-then-write on a shared key and record each with its resolution here.
- [ ] **Verification:** three nodes started simultaneously against an empty database with the init container disabled produce one schema and one set of built-in roles; 50 concurrent turns on one thread across three nodes produce contiguous, unique sequences.

---

## 8. REST server

- [ ] **Health.** `GET /v1.0/health/live` (anonymous, 200 while running). `GET /v1.0/health/ready` (anonymous; 200 only when the database answers, migrations are current, Clutch is connected in cluster mode, and the node is not draining; otherwise 503). Body: `{ status, nodeId, checks: { database, schema, clutch }, utc }`, no secrets. Excluded from request history. `HEAD /` and `GET /` unchanged.
- [ ] **Node identity.** Header `x-litegraph-node` on every response (keep `x-hostname`). New `nodeid` column on `requesthistory`, populated on capture and filterable in the API.
- [ ] **Cluster routes** (system admin for mutations; a read-only `cluster` scope for reads; OpenAPI metadata; RBAC documented):
  - `GET /v1.0/cluster`: `{ enabled, clusterName, nodes: { total, ready }, clutch: { endpoint, connected }, jobs: [ { name, holderNodeId, sinceUtc } ] }`. Works in single-node mode.
  - `GET /v1.0/cluster/nodes` (paginated), `GET /v1.0/cluster/nodes/{nodeId}`, `DELETE /v1.0/cluster/nodes/{nodeId}` (only `Stale` or `Stopped`; 409 otherwise).
  - `POST /v1.0/cluster/nodes/{nodeId}/restart`: sets `restartrequestedutc` on the target's `clusternodes` row; the target sees it at its next heartbeat, drains, and exits. No messaging system needed.
  - `POST /v1.0/cluster/restart`: rolling restart, 202 with an operation ID; `GET /v1.0/cluster/operations/{id}` reports progress (`clusteroperations` table).
  - `GET /v1.0/cluster/locks`: active LiteGraph holders from Clutch (`ClutchAdminClient.ListLocksAsync`, filtered by the cluster prefix); empty in single-node mode.
- [ ] **Settings in cluster mode.** `PUT /v1.0/settings` writes the shared file under the Clutch `settings` lock. Each node checks the file's hash every `SettingsReloadIntervalMs`, reloads on change, applies live fields, and reports its hash in `clusternodes`. The settings response adds `cluster: { settingsHash, nodesOnDifferentHash }` and a per-field `scope` (`Cluster` or `Node`).
- [ ] **Restart in cluster mode.** `POST /v1.0/settings/restart` starts a rolling restart. The receiving node holds the `restart` lock, restarts every other node one at a time (sets `restartrequestedutc`, waits for that node's `startedutc` to advance and its state to return `Ready`, bounded by `RollingRestartNodeTimeoutMs`), then restarts itself. A second rolling restart gets 409. If a node does not return in time, the operation stops and reports which node, rather than taking down healthy nodes.
- [ ] **Graceful drain.** On SIGTERM or a restart request: state `Draining` (readiness 503), stop accepting new chat streams, wait for in-flight requests up to `DrainTimeoutMs`, flush HnswLite snapshots (SQLite), release locks, state `Stopped`, exit 0. Close the listener after `DrainListenerGraceMs` (default 2000) so Nginx's passive checks move new connections elsewhere.
- [ ] **Proxy awareness.** With `TrustForwardedHeaders` on and the peer in `TrustedProxies`, resolve the client IP from `X-Forwarded-For` for request history, audit, and logs, and configure Watson's telemetry forwarded-header settings to match. Never use it for access control.
- [ ] **SSE keepalive.** Implement `ChatServerSettings.SseKeepAliveSeconds` (default 15, clamp 0..120) as `: keepalive` comment lines on native and compat SSE streams.
- [ ] **Backups.** Implement `BackupEnumerate`. PostgreSQL backup routes return `501` pointing at `docs/STORAGE.md#postgresql-backups` instead of throwing into a 500. Document the `pg_dump` runbook.
- [ ] **Verification:** `Cluster.Rest` suite covers every route with authorized and unauthorized callers, unknown and stale nodes, overlapping rolling restart (409), readiness 503 while draining and while Clutch is down, and keepalive frames on a slow `FakeLlmServer` stream.

---

## 9. Security fixes

- [ ] Fix `AuthenticationToken.IsExpired` in `LiteGraph.Server/Classes/AuthenticationToken.cs` to compare `DateTime.UtcNow` with `ExpirationUtc`. Positive and negative cases. Call out in CHANGELOG and `docs/UPGRADE.md` that already-expired tokens stop working.
- [ ] Environment overrides `LITEGRAPH_ADMIN_BEARER_TOKEN`, `LITEGRAPH_ENCRYPTION_KEY`, `LITEGRAPH_ENCRYPTION_IV`. Every node must share Key and Iv or tokens from one node fail on another; state this at the top of the Encryption section in `docs/SETTINGS.md`.
- [ ] Startup warning in every mode when Key/Iv are the all-zero defaults.
- [ ] Settings API shows environment-overridden secrets as read-only and overridden.
- [ ] Follow-up, out of scope: per-token random IV and key rotation.

---

## 10. Observability

- [ ] OpenTelemetry resource `service.instance.id = NodeId`, `service.namespace = ClusterName`; a `node` label on LiteGraph Prometheus metrics.
- [ ] New families: `litegraph_cluster_nodes{state}`, `litegraph_lock_acquire_duration_seconds{key_class,outcome}`, `litegraph_lock_lost_total{key_class}`, `litegraph_clutch_connected`, `litegraph_singleton_job_runs_total{job,outcome}`, `litegraph_singleton_job_leader{job}`, `litegraph_vector_search_duration_seconds{provider,indexed}`. `key_class` is the first key segment, never a GUID.
- [ ] Spans: `lock.acquire`, `job.run`, `cluster.rolling_restart` (child per node), `vector.search` with `provider` (`pgvector` or `hnswlite`) and `indexed` attributes.
- [ ] Prometheus scrapes each node by name (`litegraph-1:8701` and so on), never through the load balancer, plus Clutch if its image exposes metrics.
- [ ] Grafana: a `node` variable on every existing dashboard in `assets/grafana/`; new `litegraph-cluster.json` (nodes by state, heartbeat age, requests per node, lock latency and losses, job leaders, vector search latency by provider).
- [ ] Loki: syslog hostname set to `NodeId`, `node` label in `alloy/config.alloy`.
- [ ] Dashboard external-services card lists Clutch and, when enabled, Switchboard.
- [ ] Update `docs/OBSERVABILITY.md`.
- [ ] **Verification:** in the multi-node stack all nodes show `up` in Prometheus and the cluster dashboard renders data.

---

## 11. MCP server

- [ ] Multi-node compose points `LITEGRAPH_ENDPOINT` at `http://litegraph-lb:8701`; verify every tool, including SSE-backed chat tools, through the load balancer.
- [ ] Read-only tools `cluster_status` and `cluster_node_list` on all transports. No mutation tools for cluster operations.
- [ ] Stop rewriting `litegraph-mcp.json` on every startup for `LastStartUtc`.
- [ ] Healthcheck on `127.0.0.1`.
- [ ] `docs/MCP_API.md`: both tools, plus why the MCP server is a single instance.

---

## 12. SDKs (C#, JavaScript, Python)

- [ ] Cluster methods: `GetCluster`, `ListClusterNodes`, `GetClusterNode`, `DeleteClusterNode`, `RestartClusterNode`, `StartRollingRestart`, `GetClusterOperation`, `ListClusterLocks`, `HealthLive`, `HealthReady`, with typed models. C# methods async with `CancellationToken`; enumerations get `IAsyncEnumerable` variants.
- [ ] Graph models gain `VectorIndexMetric` and the `Pgvector` index type.
- [ ] Expose the last response's `x-litegraph-node` (`LastNodeId` / `lastNodeId` / `last_node_id`) and include it on SDK exceptions.
- [ ] Retry policy: `MaxRetries` (default 2), `RetryBaseDelayMs` (default 200, exponential with jitter, cap 5000), on connection failures and 502/503/504, for GET, HEAD, PUT, and DELETE only. POST only when the caller opts in. Streams never retried after the first byte. Replaces the Python SDK's no-backoff loop (`base.py:109`).
- [ ] Tests: C# `Test.Automated` (live), JS Jest plus MSW (`test/clusterRoutes/`), Python pytest (`tests/test_cluster.py`, `tests/test_retry.py`). Retry and no-retry-on-POST cases in each.
- [ ] Defaults and tests use `127.0.0.1`.
- [ ] READMEs gain the new methods, retry configuration, and a "behind a load balancer" section. Each SDK `CHANGELOG.md` is brought current (all three still show v7.0.0 as current).
- [ ] **Verification:** all three suites green, plus a live run of each against the multi-node stack.

---

## 13. Dashboard

- [ ] **Cluster page** at `dashboard/(server)/cluster` (`src/page/cluster/`), in the ADMINISTER section next to Backups and Settings, gated by the cluster capability:
  - summary cards (nodes ready/total, Clutch connected, settings drift, single-node badge when cluster mode is off);
  - nodes table (backend pagination, sort, filter by state; node ID with copy button, name, host, version, state tag, started, last heartbeat, settings match, job leaderships; row actions restart and remove with custom confirmation modals);
  - jobs panel (job, leader, since);
  - locks panel (key, mode, node, acquired, lease expiry; explanatory empty state in single-node mode);
  - rolling restart with a custom confirmation modal and a progress view that survives the dashboard's own connection drops;
  - auto-refresh with a visible interval, paused while a modal is open.
- [ ] **Settings page:** a Cluster section from `schema.ts` with correct controls, backend-matching clamps, live/restart badges, masked Clutch key; node-scoped fields read-only with "set per node by environment variable"; a banner that settings apply to every node; a drift alert linking to the Cluster page; the restart button becomes "Rolling restart" in cluster mode. Caching fields show "disabled by cluster mode".
- [ ] **Graphs:** vector index modals show the provider (pgvector or HnswLite), the metric selector (PostgreSQL), and an explanation when a dimension above 4,000 cannot be indexed. The HnswLite file field is hidden on PostgreSQL.
- [ ] **Elsewhere:** header tooltip shows the node that served the last request and a cluster badge; home page cluster card and external-services entries; Request History gains a Node column and filter.
- [ ] Fix `dashboard/CHANGELOG.md` (stale) and `Dockerrun.sh`'s default tag.
- [ ] **i18n:** every string through `next-intl` in all ten catalogs (`en`, `es`, `pt`, `fr`, `de`, `it`, `ja`, `fa`, `yue`, `zh`), translated; shared date and duration formatters; `fa` checked right-to-left.
- [ ] **Tests:** Jest plus MSW for the cluster page (render, single-node empty state, actions with confirmation, rolling restart success and failure, read-only user), the settings cluster section, the vector index modal changes, and the request history node filter. `npm run i18n:check`, `npm test`, `npm run build` green.
- [ ] **UX passes:** after the cluster page, after the settings and graphs changes, and a rendered walkthrough against the live multi-node stack at desktop, tablet, and phone widths, including a rolling restart and a node failure. Record issues found and fixed here.

---

## 14. Docker: single-node and multi-node

### 14.1 Layout

```
docker/
  README.md                   Which deployment to use, ports, credentials, adding nodes, upgrade notes
  shared/
    alloy/config.alloy
    loki/config.yaml
    grafana/provisioning/     (dashboards stay in assets/grafana)
  single-node-sqlite/
    compose.yaml              litegraph (SQLite + HnswLite), MCP, UI, observability; no PostgreSQL
    .env.example
    litegraph.json
    litegraph-mcp.json
    prometheus.yaml
    update.bat
    smoke.ps1, smoke.bat
    factory/                  pristine copies (including the seeded litegraph.db), reset.bat, reset.sh
    data/ logs/ backups/ indexes/   runtime, .gitkeep only
  single-node-postgresql/
    compose.yaml              PostgreSQL (pgvector image), init, litegraph, MCP, UI, observability
    .env.example              image tag, ports, passwords (copy to .env, which is gitignored)
    litegraph.json
    litegraph-mcp.json
    prometheus.yaml
    postgresql/init/01-litegraph.sh   pgvector extension in the LiteGraph database
    update.bat
    smoke.ps1, smoke.bat
    factory/                  pristine copies, reset.bat, reset.sh
    logs/ backups/            runtime, .gitkeep only
  multi-node/
    compose.yaml
    litegraph.json            shared cluster settings, mounted into every node
    litegraph-mcp.json
    prometheus.yaml
    nginx/litegraph.conf      LiteGraph load balancer
    nginx/clutch.conf         Clutch load balancer (WebSocket-aware)
    switchboard/sb.json
    .env.example              image tag, ports, passwords (copy to .env, which is gitignored)
    postgresql/init/01-litegraph.sh   LiteGraph role, database, schema, pgvector extension
    postgresql/init/02-clutch.sh      Clutch role and database
    update.bat
    smoke.ps1, smoke.bat
    failover.ps1, failover.bat
    factory/                  pristine copies, reset.bat, reset.sh
    logs/ backups/            runtime, .gitkeep only
```

- [ ] Move today's `docker/compose.yaml` and its config into `docker/single-node-postgresql/`, create `docker/single-node-sqlite/` from it without the PostgreSQL services, and move observability config into `docker/shared/`. Update every reference (README, docs, build scripts, `.dockerignore`, CI) and delete the old top-level files.
- [ ] **Preserve existing data.** Today's compose file has no `name:`, so its volumes are `docker_postgresql-data` and friends. `single-node-postgresql/compose.yaml` sets `name: litegraph` and pins each volume's `name:` to its legacy value. Upgrading users keep their data with no manual step. Document in `docs/UPGRADE.md`.
- [ ] Both files: images pinned (never `latest`), `.yaml`, `restart: unless-stopped` on long-running services, every HTTP service healthchecked with `["CMD", "curl", "-f", "http://127.0.0.1:<port>/<path>"]`, `interval: 5s`, `timeout: 2s`, `retries: 2`, and long-form `depends_on` with `service_healthy` (or `service_completed_successfully` for init). Fix today's healthchecks that use `localhost` and `retries: 3`.

### 14.2 `single-node-sqlite/compose.yaml` and `single-node-postgresql/compose.yaml`

- [ ] `single-node-sqlite`: `name: litegraph-sqlite`; `litegraph` with `Database.Type = Sqlite`, the database file under a bind-mounted `./data/`, and indexes under `./indexes/`; plus `litegraph-mcp`, `litegraph-ui`, and the observability services. No PostgreSQL, no init container. Host ports match the PostgreSQL deployment, so only one single-node stack runs at a time (documented).
- [ ] `single-node-postgresql`: same services as today: `postgresql` (now `pgvector/pgvector:pg17`, mounting `postgresql/init/` per §14.7), `litegraph-init`, `litegraph` (`8701`, healthcheck `/v1.0/health/ready`), `litegraph-mcp` (`8702`-`8705`), `litegraph-ui` (`3001`), `prometheus`, `loki`, `alloy`, `syslog-shim`, `grafana`. No Clutch.

### 14.3 `multi-node/compose.yaml`

- [ ] `name: litegraph-cluster` so it never touches single-node volumes; no `container_name` values.
- [ ] `postgresql` (`pgvector/pgvector:pg17`), host port `15433` by default so it does not collide with a running single-node stack. `postgresql/init/` creates separate `litegraph` and `clutch` roles and databases per §14.7. Clutch shares the server, never the LiteGraph database.
- [ ] `clutch-1`, `clutch-2` (pinned `jchristn77/clutch-server`), pointed at the `clutch` database as the `clutch` role (`CLUTCH_DB_*` from the same `.env` variables the init script uses), healthcheck `/v1.0/api/health`, after PostgreSQL is healthy.
- [ ] `clutch-lb` (`nginx:1.27-alpine`, includes `curl`): internal `8090`, `nginx/clutch.conf` (`ip_hash`, WebSocket upgrade headers), after both Clutch nodes are healthy. It is separate from `litegraph-lb` because the LiteGraph nodes depend on Clutch and the LiteGraph load balancer depends on the LiteGraph nodes; one shared Nginx would be a `depends_on` cycle.
- [ ] `litegraph-init`: one-shot `--init-only`, runs migrations including the pgvector backfill, after PostgreSQL and `clutch-lb` are healthy.
- [ ] `litegraph-1`, `litegraph-2`, `litegraph-3` from one `x-litegraph-node` anchor: same image and shared `./litegraph.json` mount, per-node `LITEGRAPH_NODE_ID` and log directory, shared `./backups/`, `LITEGRAPH_CLUSTER_ENABLE=true`, `LITEGRAPH_CLUTCH_ENDPOINT=http://clutch-lb:8090`, `LITEGRAPH_TRUSTED_PROXIES` set to the compose network. No published ports. Healthcheck `/v1.0/health/ready`. After init completes and `clutch-lb` and `alloy` are healthy.
- [ ] `litegraph-lb` (`nginx:1.27-alpine`): publishes `8701`, `nginx/litegraph.conf`, healthcheck `curl -f http://127.0.0.1:8701/v1.0/health/ready`, after the three nodes are healthy.
- [ ] `switchboard` (pinned `jchristn77/switchboard`, profile `switchboard`): publishes `8711`, `switchboard/sb.json`, active health checks on `/v1.0/health/ready`.
- [ ] `litegraph-mcp` pointed at `http://litegraph-lb:8701`; `litegraph-ui` on `3001` using `http://127.0.0.1:8701/`; `clutch-ui` on `3002` under profile `tools`.
- [ ] `prometheus` (each node and the MCP server), `loki`, `alloy`, `syslog-shim`, `grafana` (with the cluster dashboard), with collectors starting before the services that export to them.
- [ ] Demo `litegraph.json` sets `AllowInsecureDefaults: true`; a comment block at the top of the compose file lists every credential to change for production.

### 14.4 `nginx/litegraph.conf`

- [ ] `upstream` with `least_conn` over the three nodes, `max_fails=2 fail_timeout=10s`, `keepalive 64`. Least-connections suits the mix of millisecond CRUD and long chat streams. No affinity is needed, because any node can answer any request.
- [ ] `proxy_http_version 1.1`, `Connection ""`, `Host`, `X-Real-IP`, `X-Forwarded-For`, `X-Forwarded-Proto`.
- [ ] `proxy_buffering off`, `proxy_request_buffering off`, `proxy_read_timeout 3600s`, `proxy_send_timeout 3600s`, `client_max_body_size` matching the server's largest accepted body.
- [ ] `proxy_next_upstream error timeout http_502 http_503 http_504; proxy_next_upstream_tries 2;` (Nginx does not retry non-idempotent methods by default; keep it that way).
- [ ] `add_header X-LiteGraph-Upstream $upstream_addr always;`

### 14.5 `switchboard/sb.json`

- [ ] Origins `litegraph-1..3` (`HealthCheckUrl` `/v1.0/health/ready`, thresholds 2 and 2, interval 5000), one endpoint with `LoadBalancing: LeastConnections`, every LiteGraph route under `Unauthenticated.ParameterizedUrls` (LiteGraph authenticates itself).
- [ ] Test `Docker.SwitchboardRoutes` compares `sb.json` with the server's registered routes and fails listing missing or extra routes, so a new route cannot ship without it.
- [ ] Confirm against Switchboard v5 whether endpoints and origins seed from `sb.json` or need the management API (add a one-shot seed service if so), and whether `x-forwarded-for` in `BlockedHeaders` strips or replaces the header. Record both answers here.

### 14.6 Helpers

- [ ] `update.bat` in each deployment: `docker compose pull`, `down`, `up -d`, `docker ps -a`.
- [ ] `factory/reset.bat` and `reset.sh` in each: require typing `RESET`, `compose down -v` for that project only, restore pristine configs, wipe logs and backups.
- [ ] `single-node-sqlite/smoke.ps1` and `single-node-postgresql/smoke.ps1`: today's checks plus both health routes and a vector search (HnswLite and pgvector respectively).
- [ ] `multi-node/smoke.ps1`: the same through the load balancer, plus all nodes `Ready`; 30 requests hit at least two distinct `x-litegraph-node` values; a write through the load balancer is readable from each node directly; an indexed vector search returns identical results from each node; Clutch health; Switchboard checks when its profile is up.
- [ ] `multi-node/failover.ps1`: runs `LoadGenerator` through the load balancer, stops `litegraph-2` and asserts the error rate stays under a stated threshold and the node shows stale, starts it and asserts it returns `Ready`; stops `clutch-1` and asserts CRUD and search continue and job leadership moves.
- [ ] Add the missing root `DOCKERHUB_README.md` (`REPOSITORY_REQUIREMENTS.md` item 4) covering both deployments.
- [ ] **Verification:** `docker compose config` validates both files and both profiles; both stacks come up healthy from a clean clone; both smoke scripts and `failover.ps1` pass.

### 14.7 PostgreSQL initialization

The official PostgreSQL image runs everything in `/docker-entrypoint-initdb.d` once, when the data directory is empty: on first start and after a factory reset, never on an existing volume. The scripts therefore own only what a fresh server needs before any application connects (roles, databases, the extension). Schema, tables, indexes, and the pgvector backfill belong to LiteGraph's migrations (§1.2), which run on every start and are the only path that reaches existing volumes.

- [ ] Mount `./postgresql/init` read-only at `/docker-entrypoint-initdb.d` in both deployments.
- [ ] Scripts are `.sh`, not `.sql`, so passwords come from environment variables instead of being written into files. Each starts with `set -euo pipefail`, runs `psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER"`, and is idempotent (`CREATE ROLE` and `CREATE DATABASE` guarded with `SELECT ... WHERE NOT EXISTS ... \gexec`), so an operator can re-run one by hand safely.
- [ ] Add `.gitattributes` forcing `*.sh text eol=lf`. A Windows checkout with CRLF endings breaks the shebang, and the container then fails to initialize with an error that does not mention line endings. CI fails if any `.sh` file contains CRLF.
- [ ] **Single-node, `01-litegraph.sh`:** `CREATE EXTENSION IF NOT EXISTS vector` in `$POSTGRES_DB`. The single-node stack keeps today's credential model (`POSTGRES_USER=litegraph`, the role LiteGraph connects as, which is the superuser) so existing volumes keep working unchanged. On an existing volume the script does not run, and LiteGraph's own `CREATE EXTENSION` migration succeeds because that role is superuser. `docker/README.md` says production deployments should use the multi-node role model below.
- [ ] **Multi-node:** `POSTGRES_USER=postgres` is the superuser, used only by init scripts and operators.
  - `01-litegraph.sh`: role `litegraph` (`LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE`, password `$LITEGRAPH_DB_PASSWORD`); database `litegraph` owned by it; `REVOKE ALL ON DATABASE litegraph FROM PUBLIC`; schema `$LITEGRAPH_DB_SCHEMA` owned by `litegraph`; `CREATE EXTENSION IF NOT EXISTS vector` run as the superuser, so the application role never needs extension privileges. LiteGraph's migration then finds the extension present and skips it.
  - `02-clutch.sh`: role `clutch` (same restrictions, password `$CLUTCH_DB_PASSWORD`); database `clutch` owned by it; `REVOKE ALL ON DATABASE clutch FROM PUBLIC`. Clutch creates its own tables (`ManageSchema: true`).
  - Neither application role can connect to the other's database.
  - The `postgresql` service and the application services read the same `.env` variables (`LITEGRAPH_DB_PASSWORD`, `CLUTCH_DB_PASSWORD`), so the passwords cannot drift apart. `.env.example` ships demo values with a warning to change them.
- [ ] Healthcheck `pg_isready -h 127.0.0.1 -U "$POSTGRES_USER"` over TCP, with `start_period: 60s`. The image runs init scripts against a temporary server that listens only on the Unix socket, so the socket-based check used today can report healthy while scripts are still running and let dependents start early. The permanent server opens TCP only after the scripts finish.
- [ ] Factory reset restores the pristine scripts and removes the volume, so they run again on the next start.
- [ ] **Verification:** on a fresh multi-node stack, the `vector` extension exists in `litegraph`; `litegraph` is not a superuser; Clutch's tables exist in `clutch`; connecting as `clutch` to `litegraph` fails, and the reverse fails (negative cases). On a fresh single-node stack the extension exists. Restarting either stack with its existing volume does not re-run the scripts and nothing changes. A 9.0 single-node volume upgrades with the extension created by the migration.

### 14.8 Image tags and local builds

- [ ] Every LiteGraph image in both compose files is `jchristn77/litegraph:${LITEGRAPH_IMAGE_TAG:-v<approved version>}` (likewise `litegraph-mcp` and `litegraph-ui`). Third-party images stay pinned literally.
- [ ] Maintainer workflow, documented in `docker/README.md`: run `build-all.bat v10.0.0-rc1` (or any tag), put `LITEGRAPH_IMAGE_TAG=v10.0.0-rc1` in `.env` or the shell, then `docker compose up -d` and `smoke.ps1` in either deployment. `update.bat` follows the same variable.
- [ ] `.env.example` in each deployment lists `LITEGRAPH_IMAGE_TAG`, host ports, and database passwords. `.env` is gitignored.
- [x] Fix `build-all.sh`, which today calls the `.bat` scripts and therefore fails on Linux and macOS: add `build-server.sh`, `build-dashboard.sh`, and `build-mcp.sh`, and call those. *(2026-09-29)*
- [x] The build scripts add `:latest` whenever the tag is a plain `vMAJOR.MINOR.PATCH`, and never for other tags (confirmed 2026-09-30).
- [ ] CI never pushes. It builds single-architecture images locally with `docker build`, tagged `ci-<commit sha>`, sets `LITEGRAPH_IMAGE_TAG` to that tag, and runs compose with `--pull never` so a missing local image fails loudly instead of silently pulling a published one.
- [ ] Nightly, CI also runs `docker buildx build --platform linux/amd64,linux/arm64/v8` for all three Dockerfiles without pushing, so an arm64 break shows up before a release build.

---

## 15. Tests

All suites live in `Test.Shared` (Touchstone) and run through `Test.Automated`, `Test.Xunit`, and `Test.Nunit`.

- [ ] **Harness.** `LiteGraphTouchstoneClusterHost` starts N server processes on free ports against one PostgreSQL schema, following the existing `LiteGraphTouchstoneMcpHost` process-launch pattern. Clutch comes from `LITEGRAPH_TEST_CLUTCH_ENDPOINT` and `LITEGRAPH_TEST_CLUTCH_ACCESS_KEY`; without them, cluster suites report skipped with a reason, never pass silently. `FakeClutchServer` covers lock-provider unit tests everywhere. PostgreSQL test databases need pgvector; CI and local docs use `pgvector/pgvector:pg17`.
- [ ] **Suites:** `Vectors.Pgvector` (§1.4), `Vectors.HnswLiteRestart` (§2), `Cluster.Caches` (§3), `Cluster.Settings`, `Cluster.Registry` (§4), `Cluster.Locking` (§5), `Cluster.Jobs`, `Cluster.ChatHealth` (§6), `Cluster.Integrity` (§7), `Cluster.Rest` (§8), `Security.TokenExpiry` (§9), `Docker.SwitchboardRoutes` (§14.5). Every existing vector suite runs on both SQLite (HnswLite) and PostgreSQL (pgvector).
- [ ] Update `docs/TEST_COVERAGE.md` with every new REST surface and MCP tool.
- [ ] **CI unit and integration** (`.github/workflows/ci.yml`): switch the PostgreSQL service to `pgvector/pgvector:pg17`; add a Clutch service container so cluster suites run on every push instead of skipping.
- [ ] **Deployment testing.** Everything below runs against images built from the commit under test (§14.8), never against published tags.
  - *Every push:* `docker compose config` for both deployments and both profiles; `shellcheck` and the CRLF check on every init script; `Docker.SwitchboardRoutes`.
  - *Nightly, on manual dispatch, and on pull requests that touch `docker/`, a Dockerfile, the server, or migrations:* a `deploy` job that builds the three images, then for each deployment runs `docker compose up -d --wait --pull never`, runs `smoke.ps1` under `pwsh`, and tears down with `down -v`. The multi-node run adds `failover.ps1` and the `switchboard` profile smoke. The §14.7 verification checks (extension present, role separation both ways, no re-run on restart) run inside the smoke scripts through `docker compose exec postgresql psql`.
  - *Upgrade job, same triggers:* check out `docker/compose.yaml` as it was at the 9.0.0 release (the repository has no git tags today, so either tag that commit `v9.0.0` first, recommended, or pin its hash in the workflow), start it on published `v9.0.0` images, load data with `LoadGenerator` including vectors, record search results, then bring up `docker/single-node-postgresql/compose.yaml` on the commit's images against the same volume. Assert data counts match, the migration and backfill ran exactly once (`schemamigrations`), the extension exists, and search results match the recorded ones.
  - *Nightly only:* the multi-architecture build from §14.8.
  - Every deployment job uploads `docker compose logs` for all services on failure.
  - *Locally:* `docker/README.md` has a "Testing a build" section: `build-all.bat <tag>`, set `LITEGRAPH_IMAGE_TAG`, `docker compose up -d`, run `smoke.ps1` (and `failover.ps1` for multi-node).
- [ ] **Simulated user testing** per `SIMULATED_USER_TESTING.md` against the multi-node stack from a clean clone, following `docker/README.md` literally: kill a node mid-chat-stream, rolling restart from the dashboard, change a setting and confirm every node applies it, revoke a permission and retry from another browser, stop Clutch and keep working, run `update.bat` and both factory resets, upgrade a 9.0 single-node PostgreSQL stack with vectors. Record findings with `UT-` IDs.
- [ ] **Performance** (`Test.PerformanceAndScalability`, `LoadGenerator`, results in `PERF_SCALE_TESTING.md`):
  - throughput and p50/p95/p99 by operation at 1, 2, and 3 nodes through the load balancer;
  - vector search latency and recall, pgvector versus the current HnswLite path, at 10K, 100K, and 1M vectors;
  - vector write throughput, parameterized pgvector versus today's hex-literal inserts;
  - per-request latency and query count with caches off versus on (§3);
  - pgvector backfill duration per million vectors.
  - Any regression over 10% on a single-node operation blocks release until explained and accepted by the maintainer.

---

## 16. Postman and documentation

- [ ] Postman: `Cluster` and `Health` folders with collection-, folder-, and request-level descriptions; variables for base URL, a separate `lbUrl`, node ID, operation ID, and credentials. Graph requests updated for `VectorIndexMetric` and `Pgvector`. Consider moving the collection to `assets/postman/` per `REPOSITORY_REQUIREMENTS.md` item 13.
- [ ] New `docs/CLUSTERING.md`: architecture diagram, the stateless-node principle, when to use multi-node, requirements (PostgreSQL with pgvector, Clutch, shared settings file, shared Key/Iv), what Clutch is and is not used for, failure modes (node down, Clutch down, PostgreSQL down), Nginx and Switchboard configuration, adding nodes, rolling restarts and upgrades, backup and restore, limits.
- [ ] `docs/STORAGE.md`: the two storage pairings, pgvector requirements and managed-service notes, the embedding column migration and backfill, dimension limits and `halfvec`, index management, migrations table, PostgreSQL backup runbook.
- [ ] `docs/REST_API.md`, `docs/MCP_API.md`, `docs/SETTINGS.md`, `docs/OBSERVABILITY.md`, `docs/RBAC.md`, `docs/CHAT.md` updated for every change in §1 through §12.
- [ ] `docs/UPGRADE.md`: backup-first and no-rollback warning, pgvector requirement and automatic backfill, token expiry behavior change, Docker path move with volume preservation, converting a single-node PostgreSQL deployment to multi-node.
- [ ] `docker/README.md`, `README.md` (New in, layout, Quick Start split into single-node and multi-node, Docker images, factory reset paths), `DOCKERHUB_README.md`, `CHANGELOG.md` (existing format, with the pgvector requirement, token fix, and Docker move stated prominently), `CLAUDE.md` (the storage pairings, stateless-node rule, and new test environment variables; remove the now-inaccurate "Vector Index Integration Bug" guidance for PostgreSQL).
- [ ] Every document reviewed against `WRITING_DOCUMENTS.md`; an em-dash sweep over the whole diff returns nothing.

---

## 17. Release closeout

- [ ] `dotnet build src/LiteGraph.sln` with zero warnings on both target frameworks.
- [ ] Full Touchstone regression on SQLite and on PostgreSQL with pgvector and Clutch, no unexplained skips.
- [ ] SDK suites green; dashboard `i18n:check`, tests, and build green.
- [ ] The four definition-of-done checks pass. Images built with `build-all.bat <release candidate tag>`; all three deployments brought up from a clean clone with `LITEGRAPH_IMAGE_TAG` set to that tag; both smoke scripts and `failover.ps1` pass; Switchboard profile smoke passes; the CI `deploy` and upgrade jobs are green on the release commit.
- [ ] Upgrade test: a 9.0 stack started from today's `docker/compose.yaml` with vectors in it, then `docker compose -f docker/single-node-postgresql/compose.yaml up -d` on the new branch: data intact, backfill completes once, search results match pre-upgrade results.
- [ ] Rendered dashboard walkthrough and simulated user testing complete; performance results recorded and accepted.
- [ ] Version sweep: every file in §0 carries the approved version and nothing else changed a version.
- [ ] CHANGELOG final. Do **not** merge `V10.0`; hand it to the maintainer for review and testing. Opening the PR, merging, image push, and package publishing stay with the maintainer, and `V10.0` is kept after the merge.

---

## Suggested execution order

1. §0 approvals, branch, packages, then the §1.1 spike. Its answers can change §1, so nothing else in §1 starts before they are recorded.
2. §9 token fix, §2 HnswLite restart fix, §3 caching construction fix. Each ships value to single-node users on its own.
3. §1 pgvector (schema, parameterized writes, backfill, indexes, search) with the `Vectors.Pgvector` suite.
4. §4 settings and registry, §5 locking, §6 jobs and chat health, §7 races.
5. §8 REST, §10 observability, §11 MCP.
6. §14 Docker (the multi-node stack then serves as the integration environment).
7. §12 SDKs and §13 dashboard in parallel, with the UX passes.
8. §15 CI, simulated user testing, performance.
9. §16 documentation, then §17.

---

## Risks

**pgvector becomes a hard requirement for PostgreSQL users.** Most managed PostgreSQL services offer it, but a self-managed server without it will refuse to start after the upgrade. The startup error names the fix, `docs/UPGRADE.md` says it first, and the version decision in §0 accounts for it.

**Shared index parameters.** pgvector indexes are per dimension and metric, not per graph, so graphs sharing a dimension share `m` and `ef_construction`. `ef_search` stays per graph. If per-graph build parameters turn out to matter, per-graph partial indexes are possible later, at the cost of more indexes to maintain.

**Very high dimensions.** Above 4,000 dimensions there is no pgvector index and search is exact in SQL. That is still far faster than today's PostgreSQL path, which loads every candidate embedding into C#, but it is not approximate search. Documented, and reported by the enable-index API.

**Caches off costs some latency.** A few extra indexed reads per request in cluster mode. Measured in §15 and accepted explicitly, in exchange for having nothing to invalidate.

**Clutch is alpha (0.2.x).** Nothing on the request path depends on it: reads, writes, and search continue when it is down; only settings writes, index builds, restarts, and job leadership pause. Two Clutch nodes run in the multi-node stack. Pin versions and treat Clutch upgrades as their own change.

## Non-goals

Multi-region clusters. PostgreSQL high availability (use a managed service or Patroni; LiteGraph only needs a connection string). SQLite clustering. sqlite-vec (revisit when it reaches v1 and ships a .NET distribution). Kubernetes manifests and autoscaling. A cluster-wide chat concurrency limit. More than one MCP server instance. Per-token IV and key rotation.
