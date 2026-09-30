# Change Log

## Current Version

v10.0.0

v10.0 lets LiteGraph run as several identical nodes behind a load balancer, all sharing one PostgreSQL database. The nodes hold no state of their own, so any node can answer any request and a node can stop at any time without losing anything. **This is a breaking release for PostgreSQL deployments**: PostgreSQL now requires the pgvector extension, and existing vectors are converted to pgvector on first start with no way back to 9.x afterward. Back up before upgrading; see [UPGRADE.md](docs/UPGRADE.md). SQLite deployments upgrade in place.

- **pgvector storage and search on PostgreSQL**
  - `vectors.embeddings` is a pgvector `vector` column. Existing BYTEA embeddings are converted once, in resumable batches of 1,000, under a schema lock; rows that are not valid float32 arrays are skipped with a warning. Migrations are now tracked in a `schemamigrations` table.
  - Vector search runs in SQL. Graphs with indexing enabled use a shared cosine HNSW index per dimensionality (`idx_vectors_hnsw_cosine_<dimensions>`, `halfvec` above 2,000 dimensions, exact search above 4,000), built with `CREATE INDEX CONCURRENTLY` and maintained by PostgreSQL on every write, with iterative index scans so label, tag, and expression filters do not cut results short. Filtered and unindexed searches no longer load every candidate vector into the server.
  - Vector search loads every result's node, edge, or graph, its vectors, labels, and tags with one query each instead of several per result, on SQLite and PostgreSQL (top-10 search about 40% faster on a single connection, 2.3 times faster at load on a 3-node cluster; see PERF_SCALE_TESTING.md).
  - Euclidean and dot-product searches on an indexed graph now return real Euclidean and dot-product values (9.x returned cosine-derived values under those names). Vector values are written as pgvector literals instead of hex-encoded bytes.
  - `VectorIndexType` `HnswRam` and `HnswSqlite` mean "pgvector index" on PostgreSQL; `VectorIndexFile` reads back as `null` and is no longer required. Index statistics report the pgvector index name, size, and validity.
  - The per-process `VectorIndexManager`, index files under `./indexes/postgresql/`, and the post-commit index staging are gone from the PostgreSQL provider.
- **Cluster mode**
  - `Cluster` settings block and `LITEGRAPH_CLUSTER_ENABLE`, `LITEGRAPH_CLUSTER_NAME`, `LITEGRAPH_NODE_ID`, `LITEGRAPH_CLUTCH_ENDPOINT`, `LITEGRAPH_CLUTCH_ACCESS_KEY`, `LITEGRAPH_TRUSTED_PROXIES`. A cluster node requires PostgreSQL and a Clutch access key, and refuses to start on the default encryption key or administrator token unless `Cluster.AllowInsecureDefaults` is set.
  - Distributed locks through [Clutch](https://github.com/jchristn/clutch) (`Clutch.Sdk` WebSocket lock connection per node, background lease renewal, automatic reconnect, lost-lock detection) for schema migration, pgvector index builds, and the hourly chat-retention and request-history purge jobs, which now run on one node per cycle. Reads, writes, and searches take no distributed lock.
  - New `LiteGraph.Coordination` namespace: `ILockProvider`, `LocalLockProvider` (Padlock), lock keys, and `GraphRepositoryBase.LockProvider`.
  - Object caches and authorization policy caches are off in cluster mode (`AuthorizationService.EnableCache`), so deletes and permission changes made through any node apply on every node immediately.
  - Unique indexes on built-in role names and on chat turn sequences, with de-duplication of existing rows; chat turns retry on a sequence conflict. Chat endpoint health resyncs endpoints from the database so each node monitors endpoints changed through the others.
  - Node registry and change signals through Redis (`Cluster.Redis`, `LITEGRAPH_REDIS_CONNECTION_STRING`, StackExchange.Redis 3.3.1). Every node writes its state every two seconds and polls `settings:version`/`settings:changed-utc` and `restart:version`/`restart:requested-utc`. `GET /v1.0/cluster/nodes` lists every node with its state and health.
  - Rolling restarts: `POST /v1.0/cluster/restart` (and `POST /v1.0/settings/restart` in cluster mode) restarts every node one at a time under the Clutch `restart` lock, each waiting for the previous one to report healthy (`Cluster.RestartDrainMs`, `Cluster.RestartPeerTimeoutMs`).
  - Per-node operations: `GET /v1.0/cluster/nodes/{nodeId}`, `POST /v1.0/cluster/nodes/{nodeId}/restart` (one node, under the same restart lock), and `DELETE /v1.0/cluster/nodes/{nodeId}` (removes an Offline or Stopped node's registry entry).
  - `GET /v1.0/cluster/locks` lists the locks the cluster holds in Clutch, attributed to nodes through their Clutch sessions; `GET /v1.0/cluster/jobs` lists the latest run of each singleton job (node, time, duration, outcome).
  - Request history records the node that handled each request (`NodeId`, filter `nodeId`; new `nodeid` column added in place on SQLite and PostgreSQL).
  - Behind a load balancer, `Cluster.TrustForwardedHeaders` and `Cluster.TrustedProxies` now take effect: request history, audit, and traces record the client address from `X-Forwarded-For` when the request comes from a trusted proxy.
  - `Chat.SseKeepAliveSeconds` now takes effect: a silent chat stream gets a keepalive event (`retry: 3000`) so load balancer idle timeouts do not cut long generations.
  - Health responses report `StorageProvider` and `VectorIndexProvider`.
  - Request history filters are URL-decoded, so an encoded filter such as `path=%2Fv1.0%2Ftenants` matches.
- **Settings API**
  - `GET /v1.0/settings` returns the settings file instead of the running settings, so every node returns the same answer.
  - `PUT /v1.0/settings` never writes values supplied by environment variables or derived at startup into the file (listed in the new `EnvironmentOverrides`), takes the Clutch `settings` lock in cluster mode, and signals every node, which applies `RequestTimeoutSeconds` within seconds. This also stops a save from prefixing `Logging.LogDirectory` onto `Logging.LogFilename` again on every restart.
  - `POST /v1.0/settings/restart` returns a restart result (`Restarting`, `Rolling`, `RestartVersion`, `Message`) instead of `{"restarting": true}`.
  - New `Unavailable` error (503).
- **Health and identity**
  - `GET /v1.0/health/live` and `GET /v1.0/health/ready` (database, Clutch, Redis, draining), unauthenticated and excluded from request history. Readiness fails (503) only on the database or draining; a node that cannot reach Clutch or Redis reports `Degraded` with 200, since reads, writes, and searches need neither.
  - `x-litegraph-node` header on every response.
- **Docker deployments**
  - `docker/` now holds three deployments: `single-node-sqlite`, `single-node-postgresql` (`pgvector/pgvector:0.8.6-pg17-trixie`), and `multi-node` (three LiteGraph nodes behind Nginx, two Clutch nodes behind their own Nginx, Redis `7.4.9-alpine` without persistence, separate LiteGraph and Clutch PostgreSQL roles created by init scripts, optional Switchboard and Clutch dashboard profiles). Both Nginx load balancers re-resolve node names through Docker's DNS, so recreated containers are found at their new addresses. Each has a smoke test, `update.bat`, `.env.example`, and a factory reset; the cluster adds a failover test covering a LiteGraph node, each Clutch node, Redis, and a rolling restart.
  - The Compose project for the single-node PostgreSQL deployment is named `litegraph`; set `LITEGRAPH_POSTGRESQL_VOLUME=docker_postgresql-data` to keep a 9.x volume. All LiteGraph images follow `LITEGRAPH_IMAGE_TAG`, and every host port is configurable. Health checks use cURL against the readiness endpoint.
  - The build scripts move `:latest` whenever a release tag (`vMAJOR.MINOR.PATCH`) is built and never for other tags, so testing a release candidate does not change what `latest` users get.
  - CI builds images from each commit and runs every deployment's smoke test (and the cluster failover test) on pull requests, nightly, and on demand; the .NET job's PostgreSQL service uses pgvector.
- **Fixes**
  - Server security tokens now expire (`AuthenticationToken.IsExpired` compared the issue time with the expiry instead of the current time).
  - SQLite: nodes created with inline vectors now reach the HnswLite index, and an in-memory index is rebuilt from the database on first use after a restart instead of returning no results.
  - Turning caching off no longer throws; the server now passes `Caching` settings to the client at construction.
  - The MCP server no longer rewrites its settings file on every start.
  - New environment overrides: `LITEGRAPH_ADMIN_BEARER_TOKEN`, `LITEGRAPH_ENCRYPTION_KEY`, `LITEGRAPH_ENCRYPTION_IV`; a startup warning when the encryption key or IV is the all-zero default.
  - Dependency updates for Dependabot alerts: Next.js 16.3.7, sharp 0.35.5, js-yaml 4.3.2, fflate (dashboard and JavaScript SDK).
- **Observability**
  - Prometheus scrapes every node under a `node` label; every provisioned Grafana dashboard gains a Node variable, and the logs dashboard filters on the node's host name (set to the node identifier in the Docker deployments).
  - New metrics: `litegraph_node_info`, `litegraph_node_start_time_seconds`, `litegraph_node_state`, `litegraph_node_restart_pending`, `litegraph_node_settings_version`, `litegraph_clutch_connected`, `litegraph_redis_connected`, `litegraph_lock_acquires_total`, `litegraph_lock_acquire_duration_ms`, and `litegraph_lock_lost_total`. OTLP export sets `service.instance.id` and `service.namespace` on cluster nodes.
  - New **LiteGraph Cluster** Grafana dashboard (`litegraph-cluster`): node states, traffic and latency per node, lock activity and losses, Clutch and Redis connectivity, and uptime.
- **MCP**: read-only `cluster/status`, `cluster/nodes`, and `cluster/node` tools on every transport.
- **Dashboard**: a new **Cluster** page under Administration (nodes table with copy, sort, and state filter; per-node restart and remove with confirmation; rolling restart with a progress view that survives connection drops; jobs and locks panels; summary cards; auto-refresh every 5 seconds, paused while a confirmation is open). The Settings page gains a Cluster section (masked Clutch and Redis secrets, read-only node identifier, environment-override tags, and a banner that settings apply to every node); the vector index modal shows whether pgvector or HnswLite is in use and hides the index file on pgvector; Request History gains a Node column and a node picker filled from the cluster's nodes; in cluster mode the header shows a badge naming the node that answered the last request. The Settings page lists every node with its state, heartbeat, settings version, and pending restart (refreshing every five seconds), its restart button becomes **Restart Cluster** and requests a rolling restart in cluster mode, and it reports settings that came from the environment after a save.
- **SDKs**: `ReadClusterNodes`/`readClusterNodes`/`read_cluster_nodes`, `ReadClusterNode`, `RestartCluster`, `RestartClusterNode`, `DeleteClusterNode`, `HealthLive`, and `HealthReady` (and their JavaScript and Python equivalents); `ReadClusterLocks`, `ReadClusterJobs`, the health `StorageProvider`/`VectorIndexProvider` fields, and request history in all three SDKs (search with every filter including node, enumerate, read, detail, summary, delete, bulk delete); the Python SDK no longer writes credentials to its request log; `RestartServer` returns the restart result; the `Unavailable` error code. Every SDK records the node that answered the last request (`LastNodeId`/`lastNodeId`/`last_node_id`; JavaScript and Python errors carry it too) and retries connection failures and 502, 503, and 504 responses with exponential backoff and jitter (`MaxRetries` 2, `RetryBaseDelayMs` 200, capped at 5 seconds) for GET, HEAD, PUT, and DELETE, and for POST only when `RetryPost` is set. Streams are never retried once the body has started.
- **Tests**: a new `ScaleOut` Touchstone suite (token expiry, lock providers, cluster settings validation, caching disabled, SQLite index rebuild, the settings file service, the Redis node registry and rolling restart against a real Redis when `LITEGRAPH_TEST_REDIS_CONNECTION_STRING` is set, pgvector search parity for every search type indexed, unindexed, and filtered, legacy BYTEA migration, and pgvector index lifecycle), plus route guard updates for the health and cluster endpoints.
- **Docs**: new [CLUSTERING.md](docs/CLUSTERING.md) and [docker/README.md](docker/README.md); updated README, [UPGRADE.md](docs/UPGRADE.md), [SETTINGS.md](docs/SETTINGS.md), [STORAGE.md](docs/STORAGE.md), and [REST_API.md](docs/REST_API.md).

## Previous Versions

v9.0.0

v9.0 adds native **graph algorithms** — degree, closeness, eigenvector, and betweenness centrality; PageRank; weakly and strongly connected components; label-propagation and Louvain community detection; local clustering coefficient; and k-core decomposition — computed over the whole graph and optionally written back onto nodes so results are queryable through the DSL. It also adds a first-class **graph export/projection** feature (node-link JSON, edge list, GraphML) with a results **import** path, so graphs can be round-tripped to external engines such as `rustworkx`/NetworkX for algorithms beyond native scope or for graphs past the in-memory ceiling. The feature spans the full product surface: core library (`client.Algorithm`), REST server, MCP tools, the native query language, the dashboard, and all three SDKs (C#, JavaScript, Python). Additive release — no storage migration required.

- Graph algorithms
  - Eleven algorithms: `DegreeCentrality`, `PageRank`, `ClosenessCentrality`, `EigenvectorCentrality`, `BetweennessCentrality`, `WeaklyConnectedComponents`, `StronglyConnectedComponents`, `LabelPropagation`, `Louvain`, `ClusteringCoefficient`, and `KCore`.
  - Native compute over a whole-graph in-memory adjacency (compressed sparse row) built from streaming enumeration (`Node.ReadAllInGraph` / `Edge.ReadAllInGraph`), with a configurable node/edge ceiling that rejects oversized graphs rather than risking OOM.
  - New `client.Algorithm` surface, REST route `POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/algorithms`, `algorithm/run` MCP tool, a dashboard algorithms page, and SDK bindings in all three languages.
  - Native query-language surface: `CALL litegraph.algo.<name>() RETURN guid, score` (and `community`, `name`, `edgesIn`, `edgesOut`, `result`), with `LIMIT` support.
  - Optional write-back materializes per-node results (scores, community/component labels) into node `Data`, making them DSL-queryable (e.g. `WHERE n.data.pagerank > 0.1`).
  - Optional opt-in result cache (`UseCache`) with node/edge-count auto-invalidation and explicit `InvalidateCache`.
  - Node embedding generation (`POST .../algorithms/embeddings`) using the tenant's active embedding endpoint: each node's content is embedded and stored as a node vector (HNSW-indexable), exposed in all SDKs and the dashboard. Verified end to end against a live Ollama-compatible endpoint (384-dim all-MiniLM).
  - Chat endpoint base URLs may now include a path prefix (for reverse proxies and model-pinned gateways such as a LiteGraph Ollama-compatible proxy at `/v1.0/api/{model}/`); only base URLs already ending in a known API path (e.g. `/api/embeddings`) are rejected, preventing double-append.
  - New `Algorithm` authorization resource type: compute requires read scope; write-back requires write scope. `Viewer` can run read-only algorithms; `Editor` and `GraphAdmin` can also write results back.
  - Observability: `litegraph_graph_algorithms_total` counter and `litegraph_graph_algorithm_duration_ms` summary (labeled by `algorithm`) plus a `litegraph.graph.algorithm` OpenTelemetry span, covering REST, MCP, and `CALL litegraph.algo.*` DSL runs; a provisioned **LiteGraph Algorithms** Grafana dashboard; and an operator-tunable in-memory ceiling (`LiteGraph.MaxAlgorithmNodes` / `MaxAlgorithmEdges`).
  - Test coverage: dual-storage (SQLite + PostgreSQL) Touchstone cases for every algorithm, the DSL surface, the result cache, the MCP tools, and read/write authorization.

- Graph export / external-compute interop
  - Export a graph as node-link JSON (loads directly into `networkx.node_link_graph`), edge list (CSV), or GraphML via `GET /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/export/projection` — streaming, with `None`/`Meta`/`Full` attribute levels, so graphs too large for native compute can still be projected out.
  - Import externally computed results (node GUID → property/value map) back onto nodes via `POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/algorithms/import`.
  - Available over REST, MCP (`algorithm/export`, `algorithm/import`), the dashboard, and all SDKs. See [docs/ALGORITHMS.md](docs/ALGORITHMS.md).

- Authorization audit of successful privileged actions
  - The `authorizationaudit` store previously recorded only denials, leaving successful privileged actions with no audit trail. It now also records **permitted** requests that required `write` or `admin` scope, with an `AuthorizationResult` of `Permitted` and the request's actual response status code. Read-scope requests are never audited, so routine reads do not inflate the store.
  - New `AuthorizationAudit` settings block (`Enable`, `AuditSuccessfulActions`; both default `true`) lets operators disable auditing entirely or revert to denials-only. See [docs/SETTINGS.md](docs/SETTINGS.md) and [docs/RBAC.md](docs/RBAC.md).
  - Covered by a dual-storage (SQLite + PostgreSQL) Touchstone case validating that permitted writes are audited, reads are not, and denials remain audited.

- Disaster-recovery documentation for file-backed vector indexes
  - A database backup (`VACUUM INTO`) and a provider migration copy the main database and the raw stored vectors but not the derived file-backed HNSW index artifacts, so a restore or migration silently degrades indexed search (brute-force fallback, or stale results) until the index is rebuilt. Added a consolidated **Backup, Restore, and Disaster Recovery Runbook** to [docs/STORAGE.md](docs/STORAGE.md) that makes the per-graph vector-index rebuild an explicit, timed post-restore step with rebuild-time budgeting guidance, and documents the silent-degradation failure mode. The backup API section in [docs/REST_API.md](docs/REST_API.md) now states the index exclusion inline. No code change — `RebuildVectorIndex` already exists across REST/SDK/MCP; this closes the runbook gap.

- Native-query chaining (multiple MATCH and WITH)
  - A read query can now chain several `MATCH` clauses, optionally separated by `WITH` clauses, terminated by a single `RETURN`. Later clauses reference variables bound by earlier ones and join on them, so one query can express multi-step patterns (e.g. `MATCH (a)-[:KNOWS]->(b) MATCH (b)-[:WORKS_AT]->(c) RETURN a, c`). A `WHERE` after a clause may reference any variable bound so far (cross-clause filtering).
  - `WITH` reshapes the intermediate result set between stages: it projects and re-scopes variables (only projected names are visible downstream), filters (`WHERE` after `WITH`, HAVING semantics — including on an aggregate alias such as `WHERE known > 1`), orders, skips, and limits. `WITH` also **aggregates with grouping**: when any item is `COUNT`/`SUM`/`AVG`/`MIN`/`MAX`, the non-aggregate items form the grouping key and one row is produced per group.
  - Each chained `MATCH` clause matches a node or a single directed edge (express multi-hop as separate single-edge clauses); variable-length, `MATCH SHORTEST`, and `OPTIONAL MATCH` inside a chain are rejected in this release. Referencing an unbound (or WITH-dropped) variable is a parse error. Intermediate join and grouping row sets are bounded by `MaxScanRows` — a chain that exceeds the ceiling is rejected with a `400` rather than truncated.
  - Available wherever native queries run (C# SDK `client.Query.Execute`, REST `POST .../query`, MCP `graph/query`); single-clause queries are unchanged. Covered by a dual-storage (SQLite + PostgreSQL) Touchstone case exercising two-clause joins, cross-clause WHERE, WITH projection/filter/order, WITH grouping + HAVING, and negative cases (unbound variables, variable-length in a chain, scan-ceiling overflow). Docs in [docs/DSL.md](docs/DSL.md#query-chaining-multiple-match-and-with), [docs/REST_API.md](docs/REST_API.md), [docs/MCP_API.md](docs/MCP_API.md), and the Postman collection.

- Global aggregates and ordering are now whole-graph correct
  - Native-query aggregates (`COUNT`/`SUM`/`AVG`/`MIN`/`MAX`) and `ORDER BY` previously operated only over the first `MaxResults` rows in storage order, so `COUNT(*)` under-counted and `ORDER BY … LIMIT k` could miss the true global top-k. They are now evaluated over the **whole matching set**: `COUNT(*)` returns the real count, and `ORDER BY … LIMIT k` returns the genuine global top-k.
  - New `MaxScanRows` query-request field (default `1000000`, `0` disables) bounds the matching set a global operation will examine. A global query whose matching set exceeds the ceiling is rejected with a `400` rather than silently truncated into a wrong result. Ordinary (non-global) reads are unchanged — still bounded by `MaxResults` and any `LIMIT`. Exposed over the C# SDK, REST, and the MCP `graph/query` convenience field `maxScanRows`.
  - Covered by a dual-storage (SQLite + PostgreSQL) Touchstone case validating whole-set `COUNT`/`SUM`/`AVG`/`MIN`/`MAX`, global `ORDER BY` top-N/bottom-N, that ordinary reads stay page-bounded, and that exceeding the scan ceiling is rejected (positive and negative). Docs in [docs/DSL.md](docs/DSL.md), [docs/REST_API.md](docs/REST_API.md), [docs/MCP_API.md](docs/MCP_API.md), and the Postman collection.

- Native-query authorization hardening
  - Query scope (read vs. write) is now classified **authoritatively from the parsed AST**. The previous keyword-matching fallback — which, on a parse failure, decided the mutation boundary with a substring search for `CREATE`/`MERGE`/`SET`/`DELETE`/`REMOVE` — has been removed. A query that cannot be parsed during scope classification is rejected with a `400 Bad Request` **before authorization** (fail closed) instead of being guessed; because the execution engine re-parses with the same parser, no valid query is lost.
  - Covered by unit-level classifier assertions (valid queries scope correctly; unparseable queries throw rather than keyword-guess) and an API-level Touchstone case (valid read `200`, valid mutation denied for a read-only credential `401`, unparseable query `400` for both admin and read-only). See [docs/RBAC.md](docs/RBAC.md).

- MCP server: Voltaic 2.0.0 and Claude Code compatibility
  - Upgraded the MCP server to Voltaic 2.0.0 (from 0.2.0, via 1.1.0). Tool handlers now receive Voltaic's DOM-free `RpcParameters` and convert them once at the boundary; tool names, arguments, and results are unchanged.
  - Voltaic 2.0 breaking changes, as they affect LiteGraph MCP clients:
    - `tools/list` now returns only LiteGraph tools. Voltaic's demo tools (`ping`, `echo`, `getTime`) and its `getSessions`/`getClients` tools are no longer published or callable on any transport; `getSessions` exposed other callers' MCP session IDs.
    - The protocol `ping` returns `{}` (and `resultType: "complete"` under `2026-07-28`) instead of `"pong"`, as the MCP specification requires.
    - On the HTTP transport (`/mcp` and `/rpc`), tools are invoked only through `tools/call`. Calling a tool by its bare name (for example `"method": "graph/get"`) now returns `-32601` method not found; send `{"method":"tools/call","params":{"name":"graph/get","arguments":{...}}}` instead. The TCP and WebSocket transports still accept bare method calls.
    - Tool arguments are always validated against the tool's input schema, so an argument sent with the wrong JSON type (for example `request` as a JSON string where the schema declares an object) is rejected with `-32602`.
  - MCP tool failures now report their cause. Previously any handler exception reached the client as a bare `-32603 Internal error`, with the real reason hidden in `error.data`; for example, reading a deleted graph showed only "Internal error" in Claude Code. Every tool and method on all three transports is now registered through a wrapper that translates exceptions: REST failures carry the LiteGraph error description in the message (for example `LiteGraph endpoint returned 400 Bad Request: No graph with GUID '...' exists.`) and `{ statusCode, description }` in `data`, with codes `-32602` (400), `-32001` (401), `-32003` (403), `-32004` (404), `-32009` (409), and `-32000` otherwise; argument and format errors map to `-32602`; anything else stays `-32603` but with the exception message instead of "Internal error".
  - The HTTP listener now serves MCP Streamable HTTP at `/mcp` for every MCP revision from `2024-11-05` through the stateless `2026-07-28`. This includes `server/discover`, `resultType`/`ttlMs`/`cacheScope` on stateless results, and `initialize` capped at `2025-11-25`. Claude Code 2.1.x negotiates `2026-07-28` and must use `/mcp`. `/rpc` remains available for plain JSON-RPC calls.
  - `LiteGraph.McpServer install` now writes `http://<host>:<port>/mcp` into `~/.claude.json` instead of `/rpc`. Re-run it, or change the URL to `/mcp` by hand, to repair installs where Claude Code showed no LiteGraph tools. The startup banner lists both the `/mcp` and `/rpc` URLs.
  - Verified live with Claude Code 2.1.281 over `/mcp`: all 212 tools listed across three pages, and tenant read plus graph, node, and edge create, read-back, and forced delete succeeded.
  - Docs: README, `docs/MCP_API.md`, and `docs/CLAUDE_MCP.md` now point MCP clients at `/mcp`. `docs/CLAUDE_MCP.md` now gives the correct default MCP HTTP port (8702, not 8200).
  - Behavior changes from Voltaic: `tools/list` is paginated at 100 tools per page (follow `nextCursor`), and `tools/call` validates arguments against each tool's input schema before the tool runs.
  - New `Mcp.Protocol` Touchstone suite (stateless discover, paginated tools/list, stateless and handshake tools/call, missing-argument and unknown-tool rejection, initialize version capping), extended for Voltaic 2.0 with positive and negative cases for the `{}` ping (handshake and stateless), bare tool-name rejection, the absence of Voltaic demo tools from `tools/list` and `tools/call`, schema type-mismatch rejection, and the TCP and WebSocket transports. All MCP test calls now go through `tools/call`. The chat tool-catalog parity test now follows `tools/list` pagination.
  - Dependency refresh: Microsoft.Data.Sqlite / System.Text.Json 10.0.12, RestWrapper 3.3.0, OpenTelemetry 1.19.1, PolyPrompt 2.6.0, Watson 7.2.0, Microsoft.NET.Test.Sdk 18.10.1, NUnit3TestAdapter 6.3.0, NUnit.Analyzers 4.15.0.

- Deterministic enumeration ordering (no skipped or repeated rows)
  - Every ordering now ends with the record GUID as a tiebreaker, in both storage providers. Previously, records that shared a sort value (objects created in the same microsecond, as batch creation routinely produces, or duplicate names or costs) had no defined order among themselves. PostgreSQL could return tied rows in a different order on each page query, so batched reads (`ReadAllInGraph`, `ReadMany`, and the native-query engine that pages through them) could skip some records and repeat others; a native-query `SUM` over 250 nodes came out off by one or two in about half of the runs.
  - Continuation-token enumeration resumed with a strict comparison on the sort value alone, so every record tied with the last record of a page was silently skipped. When a whole batch shared a timestamp, enumeration ended after the first page. The continuation predicate now compares the sort value and the GUID together, matching the ordering, so enumeration resumes exactly after the last returned record.
  - Edge enumeration by `CostAscending`/`CostDescending` on PostgreSQL now resumes a continuation on cost, matching its ordering (it previously resumed on creation time). SQLite list reads that ordered by `GuidAscending`/`GuidDescending` referenced a nonexistent `id` column; they now order by `guid`.
  - Covered by dual-storage (SQLite + PostgreSQL) Touchstone cases that create tied timestamps, names, and costs and assert every record is returned exactly once, across node and edge orderings, for both batched reads and continuation-token enumeration. Before the fix, the PostgreSQL batched-read case and both continuation cases failed deterministically. See [docs/REST_API.md](docs/REST_API.md#enumeration-and-pagination) and [docs/MCP_API.md](docs/MCP_API.md).

- Transaction concurrency fixes
  - PostgreSQL graph transactions are now fully asynchronous. Beginning, executing, committing, and rolling back previously made blocking database calls under a lock, which could exhaust the .NET thread pool under many concurrent transactions and make other requests time out opening connections. I/O on a transaction's connection is now serialized with an async-compatible lock.
  - SQLite: starting a transaction retries briefly on transient `SQLITE_CANTOPEN`/`SQLITE_PROTOCOL` errors, and those errors are reported as retryable (`409`) instead of `500`. Isolated transaction repositories no longer open an unused connection per transaction.

- Not yet included: structural graph embeddings (FastRP/node2vec) are a planned follow-on; the v9.0 embedding feature generates content embeddings via a configured embedding endpoint.

## Previous Versions

v8.1.0

v8.1 adds LLM chat over graph data and eliminates every get-all API in favor of paginated enumeration. Storage upgrades in place — the chat tables are created on first boot and nothing stored is altered — but the enumeration conversion is a **breaking change** for clients that consumed list responses as bare JSON arrays.

- Enumeration everywhere (breaking)
  - Converted every list-returning REST route to the paginated `EnumerationResult` envelope (`Success`, `Timestamp`, `MaxResults`, `ContinuationToken`, `EndOfResults`, `TotalRecords`, `RecordsRemaining`, `Objects`); zero routes return a bare JSON array. Affected families: backups, tenants (including `/v1.0/token/tenants`), users, credentials, roles, user-role assignments, credential scopes, labels, tags, vectors (including both vector search POST routes), graphs, nodes (including most/least connected), edges (including between/from/to/node-edges), traversal (neighbors, parents, children), request history, and the chat endpoints, health, models, threads, turns, and feedback lists.
  - Added shared pagination query parameters to every list-shaped GET route: `max-keys` (1-1000, default 1000; `maxKeys` accepted), `skip`, `order` (`EnumerationOrderEnum`), and `token` for marker-based continuation where supported; authorization and request-history lists keep accepting legacy `page`/`pageSize`, with `max-keys`/`skip` taking precedence.
  - Converted every MCP list tool to return the same envelope, with `maxResults` (and `skip`/`order` where applicable) arguments plus `continuationToken` on marker-backed tools; all nine `*/getmany` tools proxy the REST `?guids=` filter and reject empty GUID arrays with an error.
  - Kept the intentional exceptions: single-object reads, statistics and settings objects, effective-permissions composites, export streams, `SearchResult`-shaped graph/node/edge search routes, and the graph-scoped OpenAI/Ollama-compatible chat routes, which preserve their wire formats.
  - Added the permanent `Chat.Rest.ZeroGetAllGuard` test, which sweeps the live OpenAPI spec and fails by name on any list-shaped route that neither returns the envelope nor appears on the justified exception list.

- Chat endpoints
  - Added tenant-managed completion and embedding endpoints across five provider types: OpenAI (covering any OpenAI-compatible server), Ollama, Gemini, Anthropic (completion-only), and VoyageAI (embedding-only), all through PolyPrompt 2.4.1.
  - Validated provider/type pairings at create and update (Anthropic embedding and VoyageAI completion endpoints are rejected) and redacted stored API keys to their last four characters in every response; sending the redacted placeholder back on update preserves the stored key.
  - Added on-demand connectivity testing with model listing and configured-model verification where the provider supports it.

- Chat completions
  - Added `POST /chat/completions` with buffered JSON and SSE streaming responses; the SSE vocabulary covers started, delta, thinking, retrieval, tool_call, tool_result, usage, and error events with a `[DONE]` terminator and keep-alive frames.
  - Added an in-process tool loop over a curated catalog of 23 read tools and 9 opt-in mutation tools whose names mirror the MCP catalog; every tool call executes under the caller's tenant and RBAC, and `vector/search` accepts natural-language text that the server embeds.
  - Added optional vector RAG: thread-bound graphs get automatic retrieval through the tenant embedding endpoint with configurable top-K and score threshold.
  - Consumed providers streaming-first on every turn so token usage, time to first token, and tokens per second are captured even for buffered responses; retries apply only before the first token, with exponential backoff.
  - Bounded concurrency with a server-wide cap (429 beyond it) and per-endpoint limiters.

- Threads, turns, and feedback
  - Added user-owned chat threads, optionally bound to a graph, with automatic title generation and history assembly under a per-tenant context token budget.
  - Persisted every turn — including failed ones — with per-stage telemetry: embedding, retrieval, limiter wait, connection, TTFT, TTLT, tokens per second, tool transcript, retry count, and trace ID.
  - Added per-turn thumbs-up/thumbs-down feedback with optional text, submitted by users and administered by admins.
  - Added retention pruning of old turns per the tenant's `HistoryRetentionDays`.

- Endpoint health
  - Added background health checks per endpoint with configurable probe URL, method, interval, timeout, expected status, and debounced healthy/unhealthy thresholds, plus health read routes with uptime and 24-hour probe history.

- Observability
  - Added the `litegraph_chat_*` Prometheus metric family (requests, errors, durations, TTFT, token counters, tokens per second, tool calls and durations, iterations, RAG and embedding timings, retries, feedback, health probes and transitions, endpoint health gauge, in-flight gauge) with low-cardinality labels.
  - Added chat trace spans (`chat.turn`, `chat.llm.request`, `chat.tool.execute`, `chat.rag.embed`, `chat.rag.search`) carrying the high-cardinality detail, and a dedicated LiteGraph Chat Grafana dashboard.

- Settings
  - Added per-tenant chat settings (default endpoints, system prompt, tool/mutation/RAG policy, context budget, retention) over `GET`/`PUT /chat/settings`, with defaults returned when no record exists.
  - Added the server-side `Chat` block to `litegraph.json` (enable, retries, backoff, tool iteration cap, concurrency cap, SSE keep-alive, default timeout), read at startup.

- Surfaces
  - Exposed chat across the dashboard, the MCP server, and the C#, Python, and JavaScript SDKs.
  - Added chat coverage to the Postman collection and documented the feature in `docs/CHAT.md`, the REST API reference, and the observability, settings, and upgrade guides.

- Chat experience (dashboard)
  - Added slash commands to the chat window (`/help`, `/?`, `/context`, `/clear`) rendered as in-thread system notices.
  - Added a model selector backed by the new non-privileged model catalog, and a streaming toggle (default on) whose off path renders the buffered result through the same event pipeline.
  - Upgraded the markdown renderer to GFM: tables, blockquotes, strikethrough, nested and task lists, horizontal rules, and code blocks with language labels and copy buttons.
  - Added a per-turn statistics popover (model, prompt/completion/total tokens, TTFT, streaming time, total duration, tokens per second, tool calls and iterations, retrieved chunks, retries) and an "AI can make mistakes" disclaimer under the composer.
  - Added conversation rename (sidebar pencil and modal), hover tooltips with the conversation title, and compacted thread cards; the selected graph is sent with every prompt.
  - Reworked Chat History: threads lead with UPDATED and CREATED, user shown as a linked email and graph as a linked name; turns lead with CREATED and gained a TTFT column; the turn detail modal is wider with markdown rendering and fixed-size stage-duration bars.

- Chat API additions
  - Added model preloading: `POST /chat/endpoints/{guid}/preload` (any tenant member) warms the model on the inference endpoint as a deduplicated fire-and-forget background task — Ollama receives a native load with a 30-minute keep-alive; cloud providers report `Supported=false` without contact. The dashboard preloads the tenant default when the chat page opens and the chosen model on selector change.
  - Added graph-scoped protocol-compatible chat: `POST /graphs/{guid}/chat/completions` accepts OpenAI chat-completions request/response bodies (including streamed `chat.completion.chunk` frames and an optional usage chunk), `POST /graphs/{guid}/chat/ollama` speaks Ollama's `/api/chat` format (newline-delimited JSON streaming by default), and `GET /graphs/{guid}/chat/models` returns an OpenAI model list of the tenant's active completion endpoints — so any OpenAI- or Ollama-capable client can chat with a specific graph without learning LiteGraph's API. The body's `model` field selects a chat endpoint by name, model, or GUID; errors use the protocol's own envelope; exchanges persist as turns in an implicit per-user thread.
  - Added `PUT /chat/threads/{guid}` to rename a thread (owner or administrator; `Title` only), mirrored in all three SDKs and the dashboard.
  - Added `GET /chat/models`, a non-privileged catalog of active endpoints projected to GUID, name, model, provider, type, and default flag — never URLs, keys, or health configuration — so chat users can pick a model while full endpoint listing stays administrative.
  - Injected the tenant name and the selected graph (resolved to its name) into the chat system prompt so the model knows its scope.

- Authorization
  - Added the `Chat` authorization resource type and a built-in `ChatAdmin` role, so endpoint, settings, feedback, and all-user history administration can be delegated through roles or credential scopes without granting full tenant administration.
  - Fixed graph-scoped roles (Editor/Viewer) incorrectly denying member-level chat operations, on both the user-role and credential-scope evaluation paths.
  - Exposed the Chat resource type in the dashboard's Authorization page.

- Endpoint health
  - Deduplicated health checks by probe target: endpoints sharing URL, method, expected status, and auth material share a single probe loop at the fastest configured interval, and all report the shared verdict.
  - Rebuilt the dashboard health detail modal around stat cards, a time-bucketed probe histogram, and first/last check timestamps.

- Fixes
  - Stopped the SQL sanitizers (SQLite and PostgreSQL) from stripping `--`, `/*`, and `*/` out of stored values; quote doubling already secures quoted literals, and the stripping permanently corrupted stored markdown (table separators, horizontal rules) and code content.
  - Fixed the PostgreSQL SQL translator rewriting identifiers and keywords inside quoted string literals (a tag value of exactly `data` became a column reference and failed inserts; hex-like and keyword-bearing values could be silently corrupted). The translator now masks literals before applying rewrites and restores them byte-for-byte, with reserved-word round-trip regression tests on both providers.
  - Fixed the LiteGraph client mutating the server's shared `LoggingSettings` instance, which flipped the reported `Logging.Enable` to false; the client now receives its own copy.
  - Fixed the Settings page failing for session logins: its fetch helper sent the session token as a bearer credential instead of relying on the SDK's `x-token` header.
  - Constrained every dashboard modal to open fully inside the viewport with internal body scrolling.
  - Rendered stored SSE chat responses in Request Detail as a reconstructed output plus a per-event breakdown.

- Dashboard usability
  - Reworked the API Explorer: single-column layout with the request card above the response, a category selector with per-tag operation counts, and an operation dropdown covering every route in the served OpenAPI spec.
  - Hid the flush-to-disk control on PostgreSQL-backed deployments (the operation is SQLite-specific).
  - Added an auto-refresh selector (10/30/60/300 seconds, default off) to every table, with the interval and page size persisted per table.
  - Added deep links: `?user=` on Users and `?graph=` on Graphs open that record's details directly.
  - Added an observability links row (Grafana, Prometheus, API Requests) beneath the Home graph, informative hover tooltips across column headers, forms, and controls, and assorted layout compaction.

- Parity
  - Brought the Python and JavaScript SDK `ChatEndpoint` models to full field parity with the server, added the model catalog and thread rename to all three SDKs, refreshed SDK READMEs and tests, and regenerated the JavaScript typings.
  - Audited the Postman collection against every registered route and added the missing authorization, graph query, request history, enumeration, vector search, and vector index requests.

- Observability
  - Split the Grafana provisioning into seven focused dashboards under the LiteGraph folder — Overview (landing), API Requests, Graphs and Queries, Vector Search, Storage, Logs, and Chat and Inference.
  - Added a syslog-shim relay to the Compose stack: SyslogLogging stamps RFC3164 frames with a zero-padded day that Grafana Alloy's strict parser silently rejects, so no logs ever reached Loki; the shim space-pads the day in transit. Removable once SyslogLogging emits compliant timestamps.
  - Extended instrumentation after a full audit: backup operation counts and durations, JSONL import record and warning counters, HNSW rebuild counters and durations, retention sweep telemetry for request history and chat history, a request-history capture-drop counter, and correct route classification for token issuance; new trace spans for backups and vector index rebuilds.

- Tooling
  - Added the `LoadGenerator` console project: seeds a database with themed synthetic graphs, nodes, edges, vectors, backdated request history, and chat activity under a diurnal-with-bursts temporal distribution, controlled by CLI arguments (`--graphs`, `--nodes`, `--density`, `--days`, `--requests`, `--chat-threads`, and more, with `/?` usage) and reversible via `--wipe`.

- Storage fixes
  - Converted positional `INSERT` statements to explicit column lists so inserts survive databases whose columns were appended by migrations (user creation previously failed on migrated PostgreSQL deployments).

- Dependencies
  - Added PolyPrompt 2.4.1.

## Previous Versions

v8.0.0

**Breaking change.** v8.0 replaces the separate administrator and user split with a single account model, unifies the two logins and two dashboards into one, and finishes the observability story. Existing v7 databases are not upgraded in place — see the upgrade notes below.

- Accounts and authentication
  - Replaced the administrator-versus-user split with a single account model: users carry `IsSystemAdmin` (server-wide superuser) and `IsTenantAdmin` (full rights within their own tenant) flags. The same email may exist in multiple tenants as independent records.
  - Unified the login: server URL, email, a tenant picker only when the email belongs to more than one tenant, then password. Removed the separate administrator login.
  - Kept the static administrator bearer token as a break-glass and bootstrap credential; it authenticates as a system administrator.
  - Seeded the default user as a system administrator on a fresh database.

- Authorization
  - Overlaid the flags on the existing role and credential-scope RBAC: system administrators bypass tenant and scope checks; tenant administrators have full rights within their own tenant; everyone else is governed by RBAC.
  - Restricted regular users to reading their own tenant and reading and updating only their own user record; confined user and credential management to tenant administrators and system administrators; kept tenant lifecycle, backup, flush, and settings as system-administrator-only.

- Dashboard
  - Collapsed the administrator and tenant dashboards into one, organized under a HOME / DATA / METADATA / MANAGE / SECURE / ADMINISTER hierarchy.
  - Gated every section and control through one declarative capability map so the navigation, route guards, and buttons agree.
  - Added a form-based Settings page that edits `litegraph.json`, shows which changes apply live versus require a restart, and offers a Restart Server control.

- Settings API
  - Added `GET`/`PUT /v1.0/settings` and `POST /v1.0/settings/restart` for system administrators; live settings apply immediately, the rest are written and applied on restart, and the restart exits the process so the container restart policy brings it back.

- Observability
  - Instrumented every REST route and every MCP tool with a shared Prometheus metric scheme distinguished by a `component` label, added an MCP `/metrics` endpoint, and expanded the Grafana dashboards.
  - Added a Loki and Grafana Alloy log pipeline over syslog so LiteGraph logs are searchable and time-correlated in Grafana. Upgraded SyslogLogging to 2.2.2.

- Docker
  - Added Loki and Alloy services, a Loki datasource, a second Prometheus scrape target for MCP, and `restart: unless-stopped` on the LiteGraph services so the settings restart applies.

- Upgrade
  - v8 is a clean break: stand up a fresh v8 deployment and move data with the v7.1 JSONL export/import. Users, credentials, and roles are re-created in v8.

- Validation
  - Added account-flag round-trip coverage on both providers and validated the full authorization matrix (system administrator, tenant administrator, and regular user) live, plus settings read/update/restart and the observability metric surface.

## Previous Versions

v7.1.0

- Subgraph selection and interchange
  - Added subgraph extraction from one or more start nodes with limits on depth, traversal direction, node and edge counts, edge cost, labels, tags, and expression filters over node and edge `Data`.
  - Kept start nodes in the result even when they fail the node filters so a selection is never empty because of a filter on the seeds.
  - Added streaming JSONL export for whole graphs and for extracted subgraphs over a chunked `application/x-ndjson` response.
  - Added streaming JSONL import that merges into an existing graph or creates a new graph, with `preserve`, `regenerate`, `skip`, and `overwrite` GUID strategies, `abort`/`skip` error handling, and configurable node batch size.
  - Added dangling-edge handling that imports bridging edges to nodes already in the target and drops unresolved edges with a warning.
  - Made JSONL import streaming with node batching, buffered edge resolution through a GUID map, and compensating rollback on failure.
  - Positioned whole-graph JSONL export as the portable, provider-agnostic per-graph backup complement to the binary `Admin.Backup`.

- REST, MCP, SDKs, and dashboard
  - Added REST endpoints for whole-graph JSONL export, subgraph JSONL export, JSONL merge import, and JSONL new-graph import.
  - Added MCP tools `graph/exportjsonl`, `graph/exportsubgraphjsonl`, and `graph/importjsonl`.
  - Added `ExtractSubgraph`, `ExportGraphToJsonlStream`/`File`, `ExportSubgraphToJsonlStream`/`File`, `RenderGraphAsJsonl`, `ImportGraphFromJsonlStream`, and `ImportGraphFromJsonl` to the client facade and SDKs.
  - Added Postman items and REST documentation for the JSONL export and import endpoints.

- Internationalization
  - Externalized dashboard UI strings for localization.

- Documentation
  - Added the JSONL format, subgraph extraction request, and graph import result to the REST API reference.
  - Added `docs/MCP_API.md` as the MCP API reference and linked it from the Claude/MCP guide.
  - Documented JSONL export as a portable per-graph backup in the storage guide.

- Validation
  - Added coverage for subgraph extraction limits, JSONL round-trips across GUID strategies, dangling-edge resolution, malformed-line handling, and import rollback.

## Previous Versions

v7.0.0

- Parallel graph transaction scaling
  - Added transaction-local repository/session state for converted providers so request-scoped graph transactions no longer rely on the legacy per-repository serialization gate for correctness.
  - Enabled PostgreSQL graph transactions to use separate pooled connections for parallel write scaling.
  - Kept SQLite transaction execution correct under concurrent requests while documenting that SQLite write throughput is still bounded by file-level locking.
  - Added provider isolation selection through `TransactionIsolationLevelEnum` / `IsolationLevel`.

- Transaction diagnostics and API behavior
  - Expanded `TransactionResult` with `TransactionId`, lifecycle `State`, operation count, provider, isolation level, queue wait, commit and rollback duration, validation-failure state, isolated-repository state, serialized-fallback state, retryability, concurrency-conflict classification, and provider error code.
  - Updated REST transaction validation failures to return HTTP `400` with a diagnostic `TransactionResult` body when possible.
  - Updated REST transaction execution failures to return HTTP `409` with rollback diagnostics.
  - Added request-history transaction diagnostics and dashboard filtering by transaction diagnostics and transaction ID.

- Providers and storage
  - Hardened SQLite and PostgreSQL transaction session lifecycles, commit/rollback cleanup, cancellation, timeout, and concurrency behavior.
  - Updated PostgreSQL transaction conflict classification for retryable provider errors.
  - Added provider-matrix correctness coverage for SQLite and PostgreSQL transaction scenarios.
  - Kept SQLite and PostgreSQL as the implemented storage providers for this release.

- Vector indexing
  - Upgraded HNSW vector indexing to `HnswLite` `2.0.1`.
  - Added v7 file-backed HNSW index metadata with `FormatVersion = 2` and `HnswLiteVersion = "2.0.1"`.
  - Added transaction-aware vector-index staging and dirty-state fallback behavior for uncertain index mutations.
  - Documented vector-index backup, rebuild, and migration guidance.

- REST, MCP, SDKs, and dashboard
  - Updated REST contracts, Postman examples, and API Explorer transaction templates for v7 transaction diagnostics and isolation levels.
  - Updated MCP transaction tooling to accept isolation level and preserve diagnostic transaction results.
  - Updated C#, Python, and JavaScript SDK transaction models and helpers for v7 diagnostics.
  - Updated dashboard API Explorer and request-history views for v7 transaction metadata.

- Docker and operations
  - Set Docker Compose LiteGraph, MCP, and UI images to `v7.0.0`.
  - Made the checked-in Docker Compose deployment PostgreSQL-backed by default.
  - Added a one-shot PostgreSQL initialization container that creates schema, tables, built-in roles, default login records, and starter graph data.
  - Added Prometheus/Grafana transaction panels and metrics for provider, isolation, state, fallback, conflicts, retries, queue wait, commit, and rollback timing.
  - Added Docker smoke validation for REST, metrics, authenticated tenant access, MCP, UI, Prometheus, and Grafana.

- Validation
  - Added CI coverage for .NET build/audit/package validation, SQLite and PostgreSQL transaction-concurrency gates, JavaScript SDK tests/package dry run, Python SDK tests/package build, and dashboard tests/build.
  - Added correctness coverage for deterministic, concurrent, randomized, soak, fault-injection, and API-surface transaction cases.

v6.0.3

- Added minimal/full bulk create return modes for labels, tags, vectors, nodes, and edges.
- Added SDK support for bulk create return mode selection.
- Optimized batch and bulk insert/hydration paths.
- Fixed large batch existence checks across SQLite and PostgreSQL providers.
- Fixed empty batch existence filters.
- Added vector batch existence support and SQLite WAL/open-failure hardening.
- Updated Postman, REST documentation, OpenAPI/API Explorer metadata, and SDK docs for bulk create response modes.

v6.0.1

- Added Docker deployment improvements, factory reset assets, and Grafana/Prometheus provisioning refinements.
- Improved performance-sensitive SQLite and PostgreSQL query paths.
- Improved request-history behavior and PostgreSQL summary bucketing.
- Updated SDK and Docker release metadata for the v6 maintenance line.

v6.0.0

- Native graph query language
  - Added LiteGraph-native graph query execution with read and mutation support.
  - Added query documentation in `docs/DSL.md`.
  - Added SDK and REST/MCP boundary support.

- Graph transactions
  - Added graph-scoped transaction support for child objects including nodes, edges, tags, labels, and vectors.
  - Added transaction request/result models and client helpers.
  - Added rollback-aware vector index dirty tracking and rebuild paths.

- Authorization and credentials
  - Added RBAC roles, scoped credential assignment, authorization audit models, and dashboard authorization management.
  - Added immutable built-in role handling and authorization UI support.

- Storage architecture
  - Added provider-neutral repository selection and storage settings.
  - Added PostgreSQL repository implementation alongside SQLite.
  - Added SQLite-to-PostgreSQL migration and verification helpers.

- Observability and operations
  - Added Prometheus metrics at `/metrics`.
  - Added OpenTelemetry-compatible activities and metrics.
  - Added Grafana dashboard assets and Docker Compose provisioning for Prometheus and Grafana OSS.
  - Integrated request history with administrator dashboard monitoring workflows.

- LiteGraphConsole
  - Added `LiteGraphConsole`, an interactive terminal shell installable as the `lg` global tool.
  - Added scripts to install, reinstall, and remove the console tool.

- Dashboard
  - Improved authorization tables and JSON viewing.
  - Improved request history metrics, filters, table layout, and detail modal wrapping.
  - Added API Explorer coverage for query and transaction workflows.

v5.0.x

- Breaking changes: full API migration to async/await.
  - All public methods that perform I/O operations are now async and return `Task` or `Task<T>`.
  - Methods returning collections now use `IAsyncEnumerable<T>` where appropriate.
  - Existing synchronous code must be updated to use `await` or `.GetAwaiter().GetResult()` for blocking calls.
  - `InitializeRepository()` and `Flush()` remain synchronous.
- Added MCP server (`LiteGraph.McpServer`).
  - Enables AI assistants and LLMs to interact with LiteGraph.
  - Exposes graph operations as MCP tools for AI integration.
  - Supports HTTP, TCP, and WebSocket transport protocols.
  - Docker image available at `jchristn77/litegraph-mcp`.

v4.x

- Major internal refactor for both the graph repository base and the client class.
- Separated responsibilities: graph repository base owns primitives, client class owns validation and cross-cutting behavior.
- Improved interface API naming and behavior consistency.
- Improved query parameter handling across implementations and primitives.
- Consolidated create, update, and delete actions within a single transaction.
- Added batch APIs for creation and deletion of labels, tags, vectors, edges, and nodes.
- Added enumeration APIs and statistics APIs.
- Added simple database caching for tenant, graph, node, and edge existence validation.
- Added in-memory operation with controlled flushing to disk.
- Added vector search parameters including topK, minimum score, maximum distance, and minimum inner product.
- Added optional graph-wide HNSW index for graph, node, and edge vectors.
- Added dependency updates, bug fixes, and Postman fixes.

v3.1.x

- Added support for labels on graphs, nodes, and edges.
- Added support for vector persistence and search.
- Updated SDK, test, and Postman collections.
- Updated GEXF export to support labels and tags.
- Reduced internal code bloat and fixed multiple bugs.

v3.0.x

- Added multitenancy and authentication through tenants, users, and credentials.
- Scoped graph, node, and edge objects to a tenant through `TenantGUID`.
- Added extensible tag metadata for graphs, nodes, and edges.
- Renamed schema columns from `id` to `guid`.
- Added setup script to create default records.
- Added environment variables for webserver port and database filename.
- Moved logic into a protocol-agnostic handler layer.
- Added `LastUpdateUtc` timestamps.
- Added bearer-token authentication.
- Added administrator bearer token configuration.
- Added tag-based retrieval and filtering for graphs, nodes, and edges.
- Updated SDK and Postman collection.

v2.1.0

- Added batch APIs for existence, deletion, and creation.
- Minor internal refactor.

v2.0.0

- Major overhaul, refactor, and breaking changes.
- Integrated webserver and REST API.
- Added extensibility through the base repository class.
- Added hierarchical expression support while filtering over graph, node, and edge data objects.
- Removed property constraints on nodes and edges.

v1.0.0

- Initial release.
