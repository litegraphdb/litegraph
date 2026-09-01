# Change Log

## Current Version

v8.1.0

v8.1 adds LLM chat over graph data. It is a non-breaking, in-place upgrade: the chat tables are created on first boot and nothing existing is altered.

- Chat endpoints
  - Added tenant-managed completion and embedding endpoints across five provider types: OpenAI (covering any OpenAI-compatible server), Ollama, Gemini, Anthropic (completion-only), and VoyageAI (embedding-only), all through PolyPrompt 2.4.0.
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
  - Fixed the Settings page failing for session logins: its fetch helper sent the session token as a bearer credential instead of relying on the SDK's `x-token` header.
  - Constrained every dashboard modal to open fully inside the viewport with internal body scrolling.
  - Rendered stored SSE chat responses in Request Detail as a reconstructed output plus a per-event breakdown.

- Dashboard usability
  - Added an auto-refresh selector (10/30/60/300 seconds, default off) to every table, with the interval and page size persisted per table.
  - Added deep links: `?user=` on Users and `?graph=` on Graphs open that record's details directly.
  - Added an observability links row (Grafana, Prometheus, API Requests) beneath the Home graph, informative hover tooltips across column headers, forms, and controls, and assorted layout compaction.

- Parity
  - Brought the Python and JavaScript SDK `ChatEndpoint` models to full field parity with the server, added the model catalog and thread rename to all three SDKs, refreshed SDK READMEs and tests, and regenerated the JavaScript typings.
  - Audited the Postman collection against every registered route and added the missing authorization, graph query, request history, enumeration, vector search, and vector index requests.

- Observability
  - Split the Grafana provisioning into seven focused dashboards under the LiteGraph folder — Overview (landing), API Requests, Graphs and Queries, Vector Search, Storage, Logs, and Chat and Inference.
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
