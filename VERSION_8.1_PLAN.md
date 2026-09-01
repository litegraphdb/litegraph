# LiteGraph v8.1 — LLM Chat Implementation Plan

LiteGraph v8.1 adds a complete LLM chat capability to the product: tenant-scoped embedding and inference endpoint management with health monitoring, a chat orchestration service in the REST server that lets a model query the graph through the same tool surface the MCP server exposes, a Chat experience in the dashboard with streaming, feedback, and per-turn performance review, and matching coverage in the MCP API, all three SDKs, Postman, tests, and documentation. Unlike v8.0, this is an additive release. No existing table changes shape, no route is removed, and a v8.0 database upgrades in place — the new chat tables are created on first boot by the same `CREATE TABLE IF NOT EXISTS` setup path that builds the rest of the schema. The version is **8.1.0**.

The design exemplar is AssistantHub (`c:\code\assistanthub`): its endpoint configuration model, health-check service, endpoint list views and modals, health histogram, chat panel, and per-turn telemetry review are the reference implementations for the equivalent LiteGraph features. Where AssistantHub and the standards in `c:\code\agents\requirements` disagree, the requirement documents win — the same precedence rule those documents state for every example application. The model-facing HTTP work is done exclusively by **PolyPrompt 2.3.0** from nuget.org; LiteGraph writes no provider wire-protocol code of its own.

This document is written to be executed and annotated, in the same convention as `VERSION_8.0_PLAN.md`. Every task is a checkbox; check it when the work is done and the stated verification passes, and leave a dated note beside anything deferred. Two rules from the v8.0 plan carry forward unchanged: **tests expand in both directions at every layer** — each behavior gets positive cases and negative cases — and **the dashboard gets explicit, repeated UX passes**, one after each dashboard milestone and a full rendered walkthrough before release. Loopback is always `127.0.0.1`, never `localhost`.

---

## Completion status

Running summary of where each section stands. Per-section checkboxes below remain the source of truth for individual items; this table is the at-a-glance view.

| § | Area | Status | Evidence |
|---|------|--------|----------|
| 0 | v8.0 closeout, branch, versions | Not started | |
| 1 | Chat data model and storage (`src/LiteGraph`) | Not started | |
| 2 | REST API surface (routes, SSE, authorization) | Not started | |
| 3 | Chat orchestration service (PolyPrompt, tool loop, RAG) | Not started | |
| 4 | Endpoint health checks | Not started | |
| 5 | Observability (metrics, traces, Grafana) | Not started | |
| 6 | MCP API (chat tools) | Not started | |
| 7 | SDKs (C#, Python, JavaScript) | Not started | |
| 8 | Dashboard (Chat, Endpoints, History, Feedback, Settings) | Not started | |
| 9 | Server settings (`litegraph.json` Chat block) | Not started | |
| 10 | Postman and documentation | Not started | |
| 11 | Docker, factory, smoke tests | Not started | |
| 12 | Test infrastructure and coverage push | Not started | |
| 13 | Release closeout | Not started | |

---

## Decisions that shape this release

These were settled with the maintainer before planning and are binding unless a task explicitly revisits them.

- **PolyPrompt 2.3.0 is the only model-facing client.** The NuGet package (published 2026-09-01) supports four providers — OpenAI, Ollama, Gemini, and Anthropic — plus any OpenAI-compatible server (vLLM, LM Studio, Groq, Ollama's `/v1` shim) through `OpenAiClient` pointed at the server's base URL. The dependency lives in `LiteGraph.Server` only; the core `LiteGraph` NuGet package stays free of it. Two library characteristics drive orchestrator design: non-streaming responses carry no token usage, so the orchestrator always streams from the provider (buffering when the caller asked for a non-streaming reply), and the library has no retry logic, so LiteGraph owns its own retry policy.
- **Endpoints are native, tenant-scoped resources.** AssistantHub stores endpoint configs in an external Partio service; LiteGraph stores them in its own SQLite/PostgreSQL repositories as first-class tenant resources, consistent with the existing `Tenant → Graph → Nodes/Edges` hierarchy. Tenant admins manage their own tenant's endpoints; system admins manage all. There is no Partio dependency.
- **Tool calls execute in-process, under the caller's authority.** The chat orchestrator advertises the MCP tool catalog (names, descriptions, JSON schemas) to the model, but dispatches the model's tool calls inside `LiteGraph.Server` through the same agnostic `ServiceHandler` the REST routes use, evaluated against the calling user's tenant and RBAC context. The MCP server's break-glass privileges are never inherited by chat. A parity test pins the advertised catalog to the MCP server's registered tools so the two surfaces cannot drift.
- **The advertised tool set is curated and read-leaning by default.** Chat advertises graph, node, edge, vector-search, label, and tag read/enumerate/search tools. Mutation tools (create/update/delete on data objects) are advertised only when the tenant's chat settings opt in. Tenant, user, credential, admin, backup, settings, batch, and transaction tools are never advertised to the model.
- **The pipeline is tool loop plus vector RAG — nothing more in 8.1.** When a chat thread is bound to a graph and RAG is enabled, the orchestrator embeds the user's message via the tenant's embedding endpoint, runs a top-K vector search, and injects the results as context; beyond that, the model drives its own graph exploration through tools. AssistantHub's retrieval gate, multi-query rewrite, LLM rerank, and answerability stages are explicitly out of scope, as are LLM conversation compaction, document attachments, and unauthenticated/public chat.
- **Chat history is server-owned.** The client sends a thread GUID and a new message; the server persists threads and turns, reconstructs conversation context, and returns/streams the reply. History review, feedback, and per-turn telemetry all hang off the persisted turn records.
- **Observability follows the v8.0 conventions.** New chat metrics use the `litegraph.chat.*` namespace with the same `component` labeling scheme, low-cardinality labels only (provider, model, endpoint name, tool name, status class — never GUIDs of unbounded sets), and rich high-cardinality detail goes on trace spans and per-turn telemetry rows instead. A dedicated Grafana chat dashboard ships provisioned as code.
- **Versioning and upgrade.** Everything moves `8.0.0 → 8.1.0`. A v8.0 database is upgraded in place: the release adds tables and touches nothing existing. `docs/UPGRADE.md` gains a short v8.0→v8.1 section saying exactly that.

---

## 0. v8.0 closeout, branch, and versions (do first)

v8.1 builds directly on v8.0's account model, authorization overlay, unified dashboard, and observability plumbing, so v8.0 must be settled on `main` before the first v8.1 commit. The very first step of this release is a branch operation: with `V8.0` merged into `main` (the merge keeps the `V8.0` branch — it is not deleted), create and switch to branch `V8.1` from `main`, and commit all v8.1 work there.

- [x] Confirm `V8.0` is merged into `main` and the `V8.0` branch is retained after the merge. *(2026-08-31: `V8.0` and `main` both point at `776a33f`; branch retained locally and on origin. The v8.0 plan's "open the PR" item is moot — the work landed on `main` directly; the v8.0 regression and live docker acceptance were recorded green in that plan's §11 before merge.)*
- [ ] Create and switch to branch `V8.1` from the updated `main`. All v8.1 work commits here.
- [ ] Bump every project/package version `8.0.0 → 8.1.0`: core/server/MCP `.csproj` `<Version>`, MCP `ServerVersion`/`SoftwareVersion`, C# SDK `.csproj`, `sdk/js/package.json`, `sdk/python` version, `dashboard/package.json`.
- [ ] Bump Docker image tags `v8.0.0 → v8.1.0` in `docker/compose.yaml` and `docker/factory/compose.yaml` (all `jchristn77/litegraph*` refs) and any `SoftwareVersion` values in `docker/*.json` / `docker/factory/*.json`.
- [ ] Add `PolyPrompt` `2.3.0` as a PackageReference to `src/LiteGraph.Server/LiteGraph.Server.csproj` (server only — not the core library, not the MCP server).
- [ ] Commit: `chore: branch 8.1, bump versions to 8.1.0, add PolyPrompt 2.3.0`.
- [ ] **Verification:** `dotnet build src/LiteGraph.sln` clean with zero warnings; `docker compose -f docker/compose.yaml config` validates.

---

## 1. Chat data model and storage (`src/LiteGraph`)

Five new entities carry the whole feature: the endpoint definition, the thread, the turn, the feedback record, and the per-tenant chat settings record. They live in the core library alongside the existing models and follow every repository convention already in place — GUID keys, `TenantGUID` scoping on every row, parallel `Sqlite/` and `Postgresql/` implementations behind shared interfaces, and the established seven-file pattern for a new entity (DDL in `SetupQueries.cs`, per-provider `Queries/<Entity>Queries.cs`, converters, `Implementations/<Entity>Methods.cs`, `Interfaces/I<Entity>Methods.cs`, sanitizer entries, and client-layer validation methods). All code follows CLAUDE.md style exactly: namespace-scoped usings, `_PascalCase` privates, XML docs on public members with defaults and ranges, `.ConfigureAwait(false)`, `CancellationToken` parameters, guard clauses, no `var`, no tuples.

Two columns in this model are deliberate, named JSON exceptions to the structured-persistence rule, and their XML docs say so: `ChatTurn.ToolTranscriptJson` (the ordered record of tool calls and results within one turn — variable-shape by nature, never queried relationally) and `ChatTurn.TelemetryJson` (the full per-stage timing detail rendered by the history modal). Every metric the dashboard filters or charts on gets its own typed column; the JSON blobs are display payloads, not query targets.

### 1.1 Models and enums

- [ ] `ChatEndpoint.cs`: `GUID`, `TenantGUID`, `Name`, `EndpointType` (`ChatEndpointTypeEnum`: `Embedding`, `Completion`), `Provider` (`ChatProviderTypeEnum`: `OpenAI`, `Ollama`, `Gemini`, `Anthropic` — `OpenAI` covers any OpenAI-compatible server), `Endpoint` (base URL), `ApiKey` (nullable), `Model`, `MaxOutputTokens`, `Temperature`, `TimeoutMs`, `MaxConcurrentRequests`, `Active`, health-check block (`HealthCheckEnabled`, `HealthCheckUrl` nullable — derives from `Endpoint` when null, `HealthCheckMethod` (`GET`/`HEAD`), `HealthCheckIntervalMs`, `HealthCheckTimeoutMs`, `HealthCheckExpectedStatusCode`, `HealthyThreshold`, `UnhealthyThreshold`, `HealthCheckUseAuth`), `CreatedUtc`, `LastUpdateUtc`. Defaults and clamp ranges mirror AssistantHub's (`30000ms` interval, `10000ms` timeout, thresholds `2`/`2`, concurrency `2`) and are documented on each property.
- [ ] `ChatThread.cs`: `GUID`, `TenantGUID`, `UserGUID` (owner), `GraphGUID` (nullable — the graph this conversation explores), `Title`, `CreatedUtc`, `LastUpdateUtc`.
- [ ] `ChatTurn.cs`: `GUID`, `TenantGUID`, `ThreadGUID`, `Sequence`, `UserMessage`, `AssistantResponse`, `Reasoning` (nullable), `ToolTranscriptJson`, `TelemetryJson`, `TraceId`, and typed metric columns: `CompletionEndpointGUID`, `EmbeddingEndpointGUID` (nullable), `Provider`, `Model`, `EmbeddingDurationMs`, `RetrievalDurationMs`, `RetrievedChunkCount`, `ToolLoopIterations`, `ToolCallCount`, `LimiterWaitMs`, `InferenceConnectionMs`, `TimeToFirstTokenMs`, `TimeToLastTokenMs`, `TotalDurationMs`, `PromptTokens`, `CompletionTokens`, `TokensPerSecondOverall`, `TokensPerSecondGeneration`, `RetryCount`, `Success`, `HttpStatus`, `Error` (nullable), `CreatedUtc`.
- [ ] `ChatFeedback.cs`: `GUID`, `TenantGUID`, `ThreadGUID`, `TurnGUID`, `UserGUID`, `Rating` (`ChatFeedbackRatingEnum`: `ThumbsUp`, `ThumbsDown`), `FeedbackText` (nullable), `CreatedUtc`.
- [ ] `ChatSettings.cs` (one row per tenant, keyed by `TenantGUID`): `DefaultCompletionEndpointGUID` (nullable), `DefaultEmbeddingEndpointGUID` (nullable), `SystemPrompt` (nullable), `EnableChat` (default true), `EnableTools` (default true), `EnableMutationTools` (default false), `MaxToolIterations` (default 10, clamped by the server-level cap in §9), `EnableRag` (default true), `RagTopK` (default 8), `RagScoreThreshold` (default 0), `MaxContextTokens` (turn-history budget, default 16384), `HistoryRetentionDays` (default 90, 0 = keep forever), `CreatedUtc`, `LastUpdateUtc`.
- [ ] One file per class/enum; enums in their own files (`ChatEndpointTypeEnum.cs`, `ChatProviderTypeEnum.cs`, `ChatFeedbackRatingEnum.cs`).

### 1.2 Repository implementation (both providers, lockstep)

- [ ] DDL for `chatendpoints`, `chatthreads`, `chatturns`, `chatfeedback`, `chatsettings` in `GraphRepositories/Sqlite/Queries/SetupQueries.cs` and `Postgresql/Queries/SetupQueries.cs`, with indexes on `(tenantguid)`, `(tenantguid, threadguid)` for turns, `(tenantguid, endpointtype)` for endpoints, and `(threadguid, sequence)` for turn ordering. `CREATE TABLE IF NOT EXISTS` so v8.0 databases upgrade in place.
- [ ] `Queries/Chat*Queries.cs`, converters, sanitizer entries, and `Implementations/Chat*Methods.cs` per provider, implementing new `Interfaces/IChat*Methods.cs`: create, read, read-many with pagination/enumeration (continuation-token style consistent with existing entities), update, delete, delete-many. Turns support filtered enumeration (by thread, by time range, by success flag). Threads and turns cascade-delete with their tenant; turns and feedback cascade with their thread.
- [ ] Client-layer methods in `Client/Implementations/` with full validation: GUID presence, URL well-formedness, clamp ranges, provider/type compatibility — **`Provider = Anthropic` with `EndpointType = Embedding` is rejected** (`ArgumentException`, message explaining Anthropic has no embeddings API), and health-check numeric fields clamp to sane minimums.
- [ ] Retention pruning: a background sweep (server-side, §3) deletes turns older than `HistoryRetentionDays` per tenant; repository exposes the dated bulk delete it needs.
- [ ] **Tests (Touchstone, `Test.Shared`, SQLite + PostgreSQL):** positive — CRUD round-trips for all five entities, pagination over 100+ turns, cascade deletes, in-place upgrade (open a v8.0-shaped DB, confirm chat tables appear and existing data is untouched). Negative — cross-tenant reads return nothing, Anthropic embedding endpoint rejected, malformed URL rejected, out-of-range clamps applied, unknown-GUID reads return null not throw, duplicate `chatsettings` for one tenant impossible.

---

## 2. REST API surface

The REST additions follow the established seven-step route pattern without deviation: `RequestTypeEnum` values, `UrlContext` matchers, route registration in `LiteGraphServer.cs` with `OpenApiRouteMetadata`, handler methods in `RestServiceHandler.cs`, authorization in `RestServiceHandler.Authorization.cs` / `AuthorizationService`, agnostic handlers in `ServiceHandler.cs`, and typed request/response models in `Classes/`. Every route is captured by request history, appears in the OpenAPI document, and is instrumented by construction through the existing `RequestTypeEnum`-driven metric labeling.

The one genuinely new mechanic is server-sent events. `POST /chat/completions` returns `text/event-stream` when the request asks to stream and plain JSON otherwise, from the same route. The SSE event vocabulary is fixed and documented (§10): `started` (thread and turn GUIDs), `delta` (content text), `thinking` (reasoning text), `tool_call` (name, arguments, iteration), `tool_result` (name, success, runtime ms, result summary), `retrieval` (RAG chunk summaries with scores), `usage` (token counts and timing), `done`, and `error`. Request-history capture stores the request body (with API keys redacted) and a truncated event transcript rather than the raw byte stream, and the existing redaction list gains `*apikey*`.

### 2.1 Routes

- [ ] Endpoint management under `/v1.0/tenants/{tenantGuid}/chat/endpoints`: create (PUT), read-all/enumerate (GET, filterable by `EndpointType`), read (GET `{endpointGuid}`), exists (HEAD), update (PUT `{endpointGuid}`), delete (DELETE `{endpointGuid}`), connectivity test (POST `{endpointGuid}/test` — PolyPrompt `ValidateConnectivityAsync` plus `ListModelsAsync`, returning reachability, model list, and whether the configured model exists), health (GET `health` for all, GET `{endpointGuid}/health` for one — served from §4's in-memory state).
- [ ] Chat under `/v1.0/tenants/{tenantGuid}/chat`: `POST completions` (body: `ThreadGUID` nullable — null creates a thread, `GraphGUID` nullable, `Message`, `Stream`, and optional per-request overrides of the tenant defaults: endpoint GUIDs, temperature, max output tokens, `EnableTools`, `EnableRag`, `RagTopK`, `SystemPrompt`).
- [ ] Threads: PUT `/chat/threads` (explicit create with optional `GraphGUID`/`Title`), GET `/chat/threads` (caller's own; tenant admins see all with a query flag), GET/DELETE `/chat/threads/{threadGuid}`, GET `/chat/threads/{threadGuid}/turns` (paginated, full metric columns).
- [ ] Feedback: POST `/chat/turns/{turnGuid}/feedback`, GET `/chat/feedback` (admin, paginated, filterable by rating/date), GET/DELETE `/chat/feedback/{feedbackGuid}`.
- [ ] Tenant chat settings: GET/PUT `/chat/settings` (the §1.1 `ChatSettings` record; API-key-free so no redaction needed here).
- [ ] `ApiKey` is redacted to its last four characters in every endpoint read/enumerate response; an update whose `ApiKey` equals the redacted sentinel preserves the stored key (the AssistantHub pattern), and this behavior is documented in `REST_API.md`.

### 2.2 Authorization

- [ ] Endpoint CRUD, endpoint test, all-thread visibility, feedback administration, and chat settings require TenantAdmin (own tenant) or SystemAdmin; break-glass token behaves as SystemAdmin as everywhere else.
- [ ] Chat completions, own-thread CRUD, and feedback submission are open to any authenticated principal of the tenant (user login or tenant credential), gated by `ChatSettings.EnableChat`.
- [ ] Thread ownership is enforced: a regular user reads/deletes only threads whose `UserGUID` is their own; tenant admins reach all threads in their tenant; nobody reaches another tenant's.
- [ ] **Tests:** extend the v8.0 RBAC matrix with rows for {endpoint CRUD, endpoint test, chat completion, own thread, other user's thread, other tenant's thread, feedback submit, feedback admin, chat settings} × the existing principal set {SystemAdmin, TenantAdmin(own), TenantAdmin(other), RegularUser(self), RegularUser(other), break-glass, unauthenticated}. Every cell asserts permit or deny explicitly. Negative cases include chat against a tenant with `EnableChat = false` (403 with a typed error) and SSE requested while unauthenticated (401 before any stream starts).

---

## 3. Chat orchestration service (`src/LiteGraph.Server`)

A new `ChatService` (plus supporting classes under `Services/Chat/`) owns the conversation lifecycle: resolve the thread and settings, rebuild context, optionally retrieve, run the tool loop against PolyPrompt, persist the turn with its telemetry, and feed the SSE writer or the buffered JSON response. It is the only component that touches PolyPrompt. Provider clients (`OpenAiClient`/`OllamaClient`/`GeminiClient`/`AnthropicClient`) are cached per endpoint GUID and invalidated on endpoint update/delete; a per-endpoint `SemaphoreSlim` honors `MaxConcurrentRequests`, and time spent waiting on it is recorded as `LimiterWaitMs`.

Streaming is the internal default regardless of what the caller asked for, because PolyPrompt only reports token usage and TTFT on its streaming paths. A non-streaming client request simply buffers the enumerated chunks and returns one JSON body. Retries are LiteGraph's job: configurable attempts with exponential backoff (§9), applied only to failures that occur before the first token arrives (connection refusals, timeouts, 429/5xx status) — a stream that dies mid-generation is surfaced as an error with partial content preserved in the turn record, never silently retried.

### 3.1 Tool catalog and dispatcher

- [ ] Build `ChatToolCatalog`: the curated tool list (per the decisions above), each entry carrying the exact MCP tool name, description, and JSON schema, exposed as PolyPrompt `ToolDefinition.Function(name, description, parameters)` values. Source the definitions so they are shared or generated, not hand-copied.
- [ ] Build `ChatToolDispatcher`: maps a `ToolCall` name to the corresponding agnostic `ServiceHandler` operation, deserializes arguments against the schema, **forces the tenant scope to the caller's tenant regardless of any tenant identifier in the model's arguments**, runs the operation under the caller's `AuthenticationContext`/RBAC evaluation, and returns a JSON result (or a structured error the model can read). Unknown tool names and schema-invalid arguments return tool-level errors, not exceptions.
- [ ] Advertise mutation tools only when `ChatSettings.EnableMutationTools` is true; never advertise tenant/user/credential/admin/backup/settings/batch/transaction tools.
- [ ] **Parity test:** start the Touchstone MCP host, call `tools/list`, and assert every catalog entry matches the MCP server's registered name and schema exactly (and flag catalog entries that vanished from MCP). Drift fails the build.
- [ ] **Tests:** positive — a scripted tool_call round-trips through the dispatcher and returns real graph data; mutation tool works when enabled. Negative — mutation tool absent from the advertised list when disabled and rejected if called anyway; forged tenant GUID in arguments is overridden by the caller's tenant; a tool the caller's RBAC denies returns a permission error to the model rather than data; unknown tool name yields a tool error, not a 500.

### 3.2 Conversation flow

- [ ] Context assembly: system prompt (tenant `SystemPrompt` merged with a fixed preamble describing the available tools and citation expectations) plus prior turns of the thread, newest-first-trimmed to `MaxContextTokens` using PolyPrompt-reported token counts where available and a chars/4 estimate otherwise.
- [ ] Vector RAG (when a graph is bound and `EnableRag` is on): embed the user message through the embedding endpoint (`EmbedAsync`), run the existing vector search (HNSW-aware per CLAUDE.md) for `RagTopK` results above `RagScoreThreshold`, inject a context block with node identity and scores, and emit the `retrieval` SSE event. Skip silently (with a telemetry note) when no embedding endpoint is configured.
- [ ] Tool loop: `ToolChatStreamingAsync` with the advertised catalog; on tool calls, emit `tool_call` events, dispatch (§3.1), append `ToAssistantMessage()` + `ChatMessage.ToolResult(...)`, and iterate up to `MaxToolIterations`; when the cap is hit, make a final call with `ToolChoice = "none"` so the model must answer with what it has. Stream `delta`/`thinking` events throughout.
- [ ] Persistence: write the `ChatTurn` with every metric column filled from PolyPrompt's streaming telemetry (`TimeToFirstTokenMs`, `TimeToLastTokenMs`, usage, tokens/sec) plus orchestrator-measured stages (embedding, retrieval, limiter wait, per-iteration inference), the tool transcript, and the telemetry JSON; update the thread's `LastUpdateUtc`; generate a thread title from the first exchange via a short non-streamed completion (best-effort, failure tolerated).
- [ ] Failure handling: provider `Success = false` before first token → retry per policy, then a typed error response/`error` event with the provider's status and message; mid-stream death → persist partial turn with `Success = false`; cancellation propagates through `CancellationToken` end-to-end.
- [ ] Retention sweep: a timer honoring `HistoryRetentionDays` per tenant (fire-and-forget, logged, metric-counted).
- [ ] **Tests (against the §12 fake LLM server):** positive — streamed and buffered completions produce identical persisted turns; tool loop executes two chained tool calls and answers; RAG context appears in the prompt and the `retrieval` event; usage and TTFT land in the turn row; title generated. Negative — 429 then success exercises exactly one retry; retries exhausted yields the typed error and a failed turn row; mid-stream disconnect persists partial content without retry; iteration cap forces a final no-tools answer; unconfigured completion endpoint yields a clear 4xx; cancellation mid-stream leaves no orphaned thread lock.

---

## 4. Endpoint health checks

AssistantHub's `EndpointHealthCheckService` ports over nearly intact, and that is deliberate — its state machine (consecutive-success/failure thresholds, rolling 24-hour history, uptime accounting) is exactly what the health histogram and status modal in the dashboard consume. Health state is in-memory only; it is operational telemetry, not a record, and a restart legitimately resets it.

- [ ] `ChatEndpointHealthService` in `LiteGraph.Server/Services/`: on startup, enumerate all active endpoints across tenants with `HealthCheckEnabled` and run one background probe loop per endpoint (`Task.Delay(HealthCheckIntervalMs)`, GET/HEAD to `HealthCheckUrl` or the endpoint base, optional auth header per provider — bearer for OpenAI-compatible, `x-api-key` for Anthropic, `x-goog-api-key` for Gemini — success = expected status code). Transitions: healthy after `HealthyThreshold` consecutive successes, unhealthy after `UnhealthyThreshold` consecutive failures. Track uptime/downtime ms, consecutive counters, last error, and a rolling 24h `CheckHistory`.
- [ ] React to endpoint create/update/delete/deactivate by starting, restarting, or stopping the corresponding loop.
- [ ] Serve the §2.1 health routes from this state: per-endpoint status DTO (status, uptime percentage, history, consecutive counts, last error, last checked) and the all-endpoints list, tenant-scoped.
- [ ] Emit health metrics (§5): a healthy/unhealthy gauge and a probe-duration histogram per endpoint name, plus a transition counter.
- [ ] **Tests:** positive — a fake endpoint that responds 200 reaches healthy after exactly `HealthyThreshold` probes; history accumulates; update to the endpoint restarts its loop with new settings. Negative — flapping below the threshold does not transition; timeout counts as failure; a deleted endpoint's loop stops and its state disappears from the health route; disabled health check reports "not monitored" rather than unhealthy.

---

## 5. Observability

Chat gets the same treatment v8.0 gave every REST route and MCP tool: metrics with consistent naming and low-cardinality labels, traces for anything with interesting internal structure, and Grafana dashboards provisioned as code. The turn record already captures the high-cardinality detail (GUIDs, exact timings, transcripts), so metric labels stay bounded: `provider`, `model`, `endpoint` (name, a small tenant-managed set), `tool` (fixed catalog), `status_class`, `streamed`. Trace spans carry the rich attributes — tenant/thread/turn/endpoint GUIDs, token counts, retry counts — where cardinality is free.

- [ ] New instruments in `ObservabilityService`: `litegraph.chat.requests` (counter), `litegraph.chat.request.duration` (histogram), `litegraph.chat.ttft` (histogram), `litegraph.chat.tokens.prompt` / `litegraph.chat.tokens.completion` (counters), `litegraph.chat.tokens_per_second` (histogram), `litegraph.chat.tool.calls` (counter, `tool` label), `litegraph.chat.tool.duration` (histogram), `litegraph.chat.tool.iterations` (histogram), `litegraph.chat.rag.duration` (histogram), `litegraph.chat.embedding.requests` / `.duration`, `litegraph.chat.retries` (counter), `litegraph.chat.active` (in-flight gauge), `litegraph.chat.feedback` (counter, `rating` label), `litegraph.chat.endpoint.healthy` (gauge), `litegraph.chat.healthcheck.duration` (histogram), `litegraph.chat.healthcheck.transitions` (counter).
- [ ] Tracing: a `chat.turn` root span per completion with child spans `chat.rag.embed`, `chat.rag.search`, `chat.llm.request` (one per loop iteration), and `chat.tool.execute` (tool name as attribute), exported through the existing OpenTelemetry pipeline; span attributes include the GUIDs, provider, model, tokens, TTFT, retry count, and success.
- [ ] Structured log lines (SyslogLogging → Alloy → Loki) at the same points other services log: turn start/finish, tool dispatch, retries, health transitions — fields, not prose, so Loki queries work.
- [ ] Grafana: a new **LiteGraph Chat** dashboard JSON in `assets/grafana/` with provisioning in `docker/grafana/provisioning/dashboards/` (and factory copy), split into request-rate/error panels, TTFT and duration percentiles, token throughput and consumption, tool-call rate by tool, endpoint health status and probe failures, retries, and feedback ratio. Keep it a separate dashboard in the product folder rather than growing the existing one.
- [ ] MCP chat tools (§6) are instrumented for free by `McpObservabilityService.RecordToolCall`; confirm the new tool names appear in the MCP `/metrics` output.
- [ ] **Tests:** drive chats and health probes against a live server (fake LLM upstream), scrape `/metrics`, and assert each new instrument appears with the expected labels; a failing completion increments the error path; feedback increments the rating-labeled counter; no metric label ever contains a GUID (assert against the scraped text).

---

## 6. MCP API

The MCP server gains the chat management and chat surfaces so an MCP-driven operator (or another agent) can do everything the dashboard can. The registration pattern is unchanged: `RegisterTool(name, description, inputSchema, handler)` entries in a new `Registrations/ChatRegistrations.cs` per transport, proxying through the REST SDK like every other tool. The MCP server keeps its elevated authority — these are operator tools, and each takes an explicit tenant GUID like the rest of the MCP surface. There is no MCP streaming; the chat completion tool is non-streaming by design and says so in its description.

- [ ] Tools: `chat/endpoint/create|read|readall|update|delete|test|health`, `chat/completions` (non-streaming, returns the full reply plus turn GUID and usage), `chat/thread/readall|read|delete`, `chat/turns/read`, `chat/feedback/create|readall|delete`, `chat/settings/read|update` — registered across the same transports as existing tools (HTTP/TCP/WebSocket).
- [ ] Wire through the C# SDK methods from §7 (the MCP server proxies REST; no direct PolyPrompt dependency in the MCP process).
- [ ] Update `docs/MCP_API.md` with the new tool table, schemas, and examples, keeping it in sync per `REPOSITORY_REQUIREMENTS.md` §14.
- [ ] **Tests:** Touchstone MCP-host cases — positive: endpoint CRUD via MCP, a full `chat/completions` round-trip against the fake LLM, feedback submit/list; negative: invalid schema arguments rejected per tool contract, unknown endpoint GUID returns a tool error, and the §3.1 parity test guards the advertised-catalog alignment from the other direction.

---

## 7. SDKs (C#, Python, JavaScript)

Each SDK grows the same surface in its own idiom: endpoint CRUD/test/health, chat (streaming and non-streaming), threads and turns, feedback, and tenant chat settings. The mapping stays as direct as the rest of each SDK — one method per route, typed models mirroring `Classes/`. Streaming is the only novel mechanic per language: C# exposes `IAsyncEnumerable<ChatStreamEvent>`, Python a generator (and an async generator), JavaScript an async iterator with an optional callback convenience. Each SDK parses the documented SSE vocabulary (§2) including `tool_call`/`tool_result`/`retrieval` events, not just deltas.

- [ ] **C#** (`sdk/csharp`): `IChatMethods` + `Implementations/ChatMethods.cs` with models (`ChatEndpoint`, `ChatRequest`, `ChatStreamEvent`, `ChatTurn`, `ChatThread`, `ChatFeedback`, `ChatSettings`, `ChatEndpointHealth`); SSE reader on the shared `SdkBase` plumbing; README section with examples.
- [ ] **Python** (`sdk/python`): `resources/chat.py` (+ models), `stream=True` returning a generator of typed events, sync and async variants consistent with the existing mixins; README + docstrings.
- [ ] **JavaScript** (`sdk/js`): chat methods on `LiteGraphSdk`, SSE parsing (fetch reader, `\n\n` framing, `event:`/`data:` lines, `[DONE]`), models in `src/models/`; README section.
- [ ] Version bumps ride §0; publish steps land in §13.
- [ ] **Tests:** each SDK gets both mocked-transport tests (C# local fake server, JS jest+msw with scripted SSE fixtures, Python pytest with response mocks) and live-harness cases in its existing harness (C# `Test.Sdk`, JS/Python integration suites) against a real server + fake LLM. Positive: full chat round-trip with events in order, endpoint CRUD, feedback. Negative: 401/403 surfaces as the SDK's error type, malformed SSE frame tolerated or surfaced cleanly, redacted API key round-trip preserves the stored key, stream cancellation closes the connection.

---

## 8. Dashboard

Chat is the headline feature of the release, and the dashboard work is correspondingly the largest single section. A new **AI** section joins the TOC between METADATA and MANAGE, tenant-scoped like its neighbors, with five entries. The chat panel itself takes its interaction design from AssistantHub's `ChatPanel.jsx` — streamed deltas, a collapsible thinking block, live tool-activity bubbles with a post-hoc tool table, thumbs feedback with a comment modal, per-message copy — rendered in this dashboard's own stack (Next.js 16, Ant Design, Redux Toolkit, next-intl) and visual language, not AssistantHub's. The endpoint pages take the AssistantHub list/modal/histogram trio. All strings go through the ten locale catalogs and `npm run i18n:check` stays green; every view works in dark and light themes and at 1280/768/390 px.

Per `DASHBOARD_STYLE_AND_USABILITY.md`, the route inventory below is binding and drives the build:

| Route | Nav item | Access | Purpose |
|---|---|---|---|
| `/dashboard/[tenantId]/ai/chat` | AI → Chat | Any tenant principal (when `EnableChat`) | Conversational panel: thread sidebar, streaming replies, thinking, tool activity, retrieval sources, feedback, graph binding selector |
| `/dashboard/[tenantId]/ai/endpoints` | AI → Endpoints | TenantAdmin, SystemAdmin | Endpoint list (type filter/tabs), create/edit/test modals, health column + health detail modal, activate/deactivate, delete |
| `/dashboard/[tenantId]/ai/history` | AI → Chat History | TenantAdmin, SystemAdmin | Turn browser with filters (thread, user, date, success), per-turn detail modal with metric charts |
| `/dashboard/[tenantId]/ai/feedback` | AI → Feedback | TenantAdmin, SystemAdmin | Feedback table with rating filter, detail modal (turn context + comment), delete |
| `/dashboard/[tenantId]/ai/settings` | AI → Chat Settings | TenantAdmin, SystemAdmin | Form editor over the tenant `ChatSettings` record (default endpoints pickers, prompts, tool/RAG toggles, budgets, retention) |

- [ ] Nav: add the AI section to `src/constants/sidebar.tsx` with `labelKey`/`titleKey` catalog entries and `resource` gating; extend `capabilities.ts` so the section filters correctly for each principal class (Chat visible to everyone in the tenant, the other four admin-gated).
- [ ] **Chat panel**: thread sidebar (list, new, rename via title edit, delete with custom confirm modal), message stream with markdown rendering, streamed delta/thinking display, live tool bubbles fed by `tool_call`/`tool_result` events plus a collapsed tool-activity table per reply, retrieval sources card (node name, score) from `retrieval` events, thumbs up/down with the feedback comment modal, copy button, status bar (thread GUID, token usage of last turn, bound graph), graph binding picker, and disabled-state messaging when chat is off or no completion endpoint exists (with a deep link to Endpoints for admins).
- [ ] **Endpoints view**: DataTable (columns: Name, Type, Provider, Model, Endpoint URL, Concurrency, Active, Health, Created; ID columns never wrap; Select Columns control) with 15s health polling; create/edit modal with grouped fields, provider-aware hints, redacted-key handling, and Anthropic-embedding combination blocked client-side with an explanatory message; test modal showing reachability + model list; health histogram cell and `HealthDetailModal` (status badge, uptime %, consecutive counters, last error, 24h histogram) ported from AssistantHub's design.
- [ ] **History view**: filterable, paginated turn table; detail modal rendering the turn's metric columns as a stage waterfall (embedding → retrieval → limiter wait → connection → TTFT → streaming) plus TTFT/duration/tokens-per-second figures and the tool transcript — charts hand-rolled SVG per the frontend standard, no chart library.
- [ ] **Feedback view**: table + detail modal (user message, assistant response, rating, comment), delete with confirm.
- [ ] **Chat settings view**: sectioned form over GET/PUT `/chat/settings` with endpoint dropdowns populated from the endpoint list, clamped numeric inputs, and save/dirty handling consistent with the v8.0 settings page.
- [ ] i18n: every new string in all ten catalogs (`de, en, es, fa, fr, it, ja, pt, yue, zh`); `npm run i18n:check` green; RTL sanity pass in `fa`.
- [ ] **Tests (jest):** rendering and interaction tests for each new view and modal, SSE store handling with scripted event fixtures (order, tool bubbles, error event), capability gating (regular user sees Chat only), i18n key presence.
- [ ] **UX pass after this milestone:** rendered walkthrough of all five routes, dark + light, three breakpoints, at least two locales, with findings fixed before the section is checked done.

---

## 9. Server settings (`litegraph.json` Chat block)

Server-level chat policy — the knobs a system operator owns, as opposed to the per-tenant `ChatSettings` record — lands as a new `Chat` block in `litegraph.json`, wired through the v8.0 settings API and the dashboard settings editor like every other block. Fields: `Enable` (master switch, default true), `MaxRetries` (default 2), `RetryBackoffMs` (base for exponential backoff, default 500), `MaxToolIterationsCap` (hard ceiling on the tenant setting, default 25), `MaxConcurrentChats` (server-wide in-flight cap, default 50), `SseKeepAliveSeconds` (comment-frame keep-alive, default 15), `DefaultTimeoutMs` (endpoint default, default 120000). Each property documents its default and range and clamps on load.

- [ ] Add the block to `LiteGraphSettings`/`Settings.cs` with validation and clamping; classify each field hot-reloadable or restart-required in the settings-update metadata.
- [ ] Surface the block in the dashboard settings page schema (`src/page/settings/schema.ts`) — SystemAdmin only, as with the rest of that page.
- [ ] Honor `Enable = false` with a typed 503 on all chat completion routes (management routes stay up so operators can fix configuration), and `MaxConcurrentChats` with a fast 429.
- [ ] Document in `docs/SETTINGS.md`.
- [ ] **Tests:** settings round-trip through GET/PUT `/v1.0/settings` including the new block; clamps applied on out-of-range PUT; chat disabled server-wide returns 503 while endpoint CRUD still works; concurrency cap returns 429 under saturation (drivable with the fake LLM's slow-response mode).

---

## 10. Postman and documentation

Both the Postman collection and the markdown docs are release gates, not afterthoughts — `REPOSITORY_REQUIREMENTS.md` requires them kept in sync with the API surface, and the v8.0 release treated them that way. The chat feature also earns a standalone architecture document, because the tool loop, the SSE vocabulary, and the security posture (in-process dispatch under caller authority) need one authoritative explanation that REST_API.md's per-route format cannot carry.

- [ ] `LiteGraph.postman_collection.json`: a **Chat** folder with subfolders for Endpoints (CRUD, test, health), Completions (streaming and non-streaming examples with documented SSE output), Threads/Turns, Feedback, and Chat Settings; collection variables for endpoint/thread/turn GUIDs; JSON validated.
- [ ] `docs/REST_API.md`: every new route with request/response bodies, the SSE event vocabulary, the API-key redaction contract, and error shapes.
- [ ] `docs/MCP_API.md`: the new chat tools (§6).
- [ ] New `docs/CHAT.md`: architecture (orchestrator, PolyPrompt, tool catalog/dispatcher, parity with MCP), provider matrix (including the Anthropic-embeddings limitation and OpenAI-compatible coverage of vLLM, LM Studio, and embeddings-only providers such as VoyageAI), RAG behavior, retry semantics, security model, and telemetry field reference for the turn record.
- [ ] `docs/OBSERVABILITY.md`: the chat metric/span/log inventory and the new Grafana dashboard.
- [ ] `docs/UPGRADE.md`: the v8.0 → v8.1 in-place note. `README.md`: "New in v8.1" section. `CHANGELOG.md`: 8.1.0 entry. `DOCKERHUB_README.md`: refreshed if present, created per repo requirements if not.
- [ ] Prose documents follow `WRITING_DOCUMENTS.md` and pass its final revision checklist.
- [ ] **Verification:** Postman JSON validates; a doc-vs-code sweep confirms every registered chat route and MCP tool appears in the docs and collection, and nothing documented is unimplemented.

---

## 11. Docker, factory, and smoke tests

The compose topology does not change — chat endpoints point at external model servers the operator already runs, so no new service joins the stack. What changes is versions, seeds, and probes. The factory seed and the smoke script both learn about chat so a fresh `docker compose up` proves the feature end to end without a live LLM (management surfaces) and optionally against one (completions).

- [ ] Image tags at `v8.1.0` in both compose files (§0); rebuild scripts untouched apart from versions.
- [ ] Factory reset assets: seeded `litegraph.db` (and PostgreSQL init) include the chat tables; optionally seed a disabled example Ollama endpoint (`http://127.0.0.1:11434`, health check off) as a template row.
- [ ] Grafana provisioning for the chat dashboard in both compose trees (§5).
- [ ] `docker/smoke.ps1` (+ `.bat`): probe chat settings read, endpoint CRUD round-trip, endpoint health route, and chat-disabled 503 behavior; document an optional environment variable pointing at a reachable Ollama/OpenAI-compatible server that, when set, adds a live completion probe.
- [ ] Compose comments/docs note how to point an endpoint at a host-local Ollama from inside the container network (`host.docker.internal` on Docker Desktop, with the Linux alternative documented).
- [ ] **Verification:** `docker compose config` validates for both trees; full `up` shows all services healthy; smoke passes; factory reset then smoke passes.

---

## 12. Test infrastructure and coverage push

Two things happen here: the chat feature gets its harness, and overall coverage moves toward the stated goal of approaching 100% of surface behaviors with positive and negative cases. The single most important new asset is a **fake LLM server** in `Test.Shared` — an in-process OpenAI-compatible HTTP host modeled on PolyPrompt's own `LocalOpenAiTestServer` — that makes every chat behavior testable deterministically on both database providers with no network dependency and no API keys. It speaks `/v1/chat/completions` (streaming SSE and non-streaming), `/v1/embeddings`, and `/v1/models`, with scripting hooks for canned tool calls, usage blocks, thinking deltas, configurable delays (to exercise TTFT and timeouts), and fault injection (429, 5xx, connection reset, mid-stream abort).

Coverage "approaching 100%" is measured against surfaces, not lines: every REST route, every MCP tool, every SDK method, and every dashboard view has at least one positive and one negative case, and the gaps are enumerated rather than guessed. The audit below produces the checklist that the rest of the section burns down.

- [ ] Build `FakeLlmServer` in `Test.Shared` with the behaviors above; port PolyPrompt's port-binding-race hardening; bind `127.0.0.1`.
- [ ] New Touchstone suites in `Test.Shared`, run by `Test.Automated`/`Test.Xunit`/`Test.Nunit` on **both** SQLite and PostgreSQL: `LiteGraphTouchstoneChatEndpointSuites` (§1/§2 CRUD + validation + RBAC), `...ChatServiceSuites` (§3 flows, retries, tool loop, RAG), `...ChatToolDispatchSuites` (§3.1 including the MCP parity test), `...ChatHealthSuites` (§4), `...ChatObservabilitySuites` (§5), `...ChatMcpSuites` (§6), `...ChatFeedbackAndHistorySuites` (threads, turns, feedback, retention pruning).
- [ ] Extend the RBAC matrix suite with the §2.2 rows; every cell explicit.
- [ ] **Coverage audit:** enumerate every `RequestTypeEnum` value, MCP tool, and SDK method against the existing suites; produce `docs/TEST_COVERAGE.md` listing each surface with its positive/negative case references; write the missing cases (chat and pre-existing gaps alike) until the table has no empty cells or a dated justification per exception.
- [ ] SDK harnesses (§7) and dashboard jest suites (§8) green; `sdk/*` READMEs document how to run each harness against `127.0.0.1`.
- [ ] **Verification:** full regression on both providers with zero failures and zero unexplained skips; the run count and result recorded in this document's status table.

---

## 13. Release closeout

Closeout mirrors v8.0's: prove the whole stack live, sweep the versions, walk the rendered dashboard, and open the PR. The live acceptance run doubles as the first real-world exercise of the chat feature against an actual model server, which the automated suites deliberately avoid depending on.

- [ ] Clean build of `src/LiteGraph.sln` and `sdk/csharp` with zero warnings; `npm run build` + `npm run i18n:check` clean for the dashboard.
- [ ] Full Touchstone regression on both providers; SDK harnesses; dashboard jest — all green, results recorded in the status table.
- [ ] Live `docker compose up` acceptance: all services healthy; REST and MCP `/metrics` show the chat instruments; create a completion endpoint against a live Ollama or OpenAI-compatible server and an embedding endpoint against VoyageAI (`https://api.voyageai.com`, provider `OpenAI`, using the maintainer-supplied test key kept in local untracked config — never committed), watch both go healthy, run a streamed chat from the dashboard that invokes at least one graph tool and one VoyageAI-backed RAG retrieval, leave feedback, and review the turn in History with its waterfall; confirm the turn's trace in Tempo/Grafana and its log lines in Loki; confirm the Chat Grafana dashboard populates.
- [ ] Rendered dashboard walkthrough of the five AI routes plus regressions on the v8.0 routes, both themes, three breakpoints, two locales minimum; findings fixed.
- [ ] Version sweep (`8.1.0` everywhere), `CHANGELOG.md` finalized, Postman/docs sync check (§10) rerun.
- [ ] Publish SDK packages (NuGet, npm, PyPI) and Docker images `v8.1.0` per the existing release scripts.
- [ ] Open the PR from `8.1` to `main` with the standard body trailer.
