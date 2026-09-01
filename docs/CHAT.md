# LiteGraph Chat

v8.1 adds an LLM chat surface to the LiteGraph server: a user asks questions in natural language, and the model answers by actually querying the graph — through a curated tool catalog dispatched in-process under the caller's own authority, optionally grounded by vector retrieval. The server owns the whole loop: it talks to the model provider, executes the model's tool calls, streams the answer back over SSE, and records a fully instrumented turn record for every exchange. Nothing about the graph API changed to make this work; chat is a consumer of the same agnostic handler layer that REST and MCP already share.

The REST routes are listed in [REST_API.md](REST_API.md); this document explains how the pieces fit together and why they behave the way they do.

## Data Model

Five entities carry the feature, all tenant-scoped like everything else in LiteGraph:

| Entity | Purpose |
|---|---|
| `ChatEndpoint` | A completion (inference) or embedding endpoint: provider, base URL, API key, model, limits, and health-check policy |
| `ChatThread` | A conversation owned by one user, optionally bound to a graph |
| `ChatTurn` | One user message and its assistant response, with per-stage telemetry |
| `ChatFeedback` | A thumbs-up/thumbs-down rating of a single turn, with optional free text |
| `ChatSettings` | Per-tenant defaults and policy — one record per tenant |

Endpoints are managed by administrators (system or tenant); threads belong to the user who created them; feedback is written by users and read by administrators. A thread bound to a graph gets two things: retrieval runs against that graph, and the system prompt tells the model to prefer it. An unbound thread still works — the model can enumerate graphs through tools — it just starts without a target.

The turn record is deliberately heavyweight. Beyond the message pair it stores the reasoning text, a JSON transcript of every tool call, and a column per timing stage (see the telemetry reference below). The high-cardinality detail lives here and on trace spans, never on metric labels, which is the same division of labor v8.0 established for the rest of the server.

## Providers

Chat speaks to model providers through [PolyPrompt](https://www.nuget.org/packages/PolyPrompt) `2.4.0`, which gives every provider the same streaming tool-chat and embedding interface. Five provider types are supported:

| Provider | Completions | Embeddings | Notes |
|---|---|---|---|
| `OpenAI` | yes | yes | Also any OpenAI-compatible server (vLLM, LM Studio, llama.cpp server, LiteLLM, and so on) — point the endpoint URL at it and keep the provider as `OpenAI` |
| `Ollama` | yes | yes | Local models; no API key needed by default |
| `Gemini` | yes | yes | Google AI endpoints |
| `Anthropic` | yes | no | Completion-only; creating an Anthropic embedding endpoint is rejected with 400 |
| `VoyageAI` | no | yes | Embedding-only; creating a VoyageAI completion endpoint is rejected with 400. Model listing is not available, so connectivity tests omit the model inventory |

The `OpenAI` default is the escape hatch: any server that implements the OpenAI wire format works without LiteGraph knowing its name. Provider choice is per endpoint, so a tenant can pair, say, an Ollama completion endpoint with a VoyageAI embedding endpoint — the completion and retrieval paths are independent.

## Orchestration

A completion request runs through a fixed pipeline. Understanding it explains most of the observable behavior — what appears in the SSE stream, what lands in the turn record, and where each millisecond is accounted for.

**Context assembly.** The server builds the message list from a fixed preamble (identity, "use the tools, do not guess", the tenant's name and GUID, and the selected graph — the thread's bound graph, or the request's `GraphGUID` when the thread is unbound, resolved to its name), the tenant's `SystemPrompt` (or the request's override), and prior turns from the thread. History is fitted to a token budget derived from the completion endpoint's `ContextWindowTokens` (minus `MaxOutputTokens`, with a 16384-token default when unspecified) — newest turns first, estimated at four characters per token — so long threads degrade by forgetting their oldest exchanges rather than failing.

**Retrieval.** When the thread is bound to a graph, retrieval is enabled, and an embedding endpoint resolves, the server embeds the user's message through the tenant's embedding endpoint, runs a vector search against the graph (honoring `RagTopK` and `RagScoreThreshold`), and injects the results as a system message: node name, GUID, similarity score, and up to 500 characters of node data each. Streaming clients see the same chunks in a `retrieval` event. Retrieval failure is non-fatal — the turn continues without context and the error is tagged on the trace span.

**The tool loop.** With tools enabled, the advertised catalog goes to the model and the server loops: call the provider, execute whatever tool calls come back, append the results, call again. Each tool result is truncated at 64 KB before it re-enters the context. The loop runs until the model produces content without tool calls, or until `min(tenant MaxToolIterations, server MaxToolIterationsCap)` iterations — the final iteration is issued with tools withheld and tool choice forced to `none`, so the model must answer with what it has rather than looping forever.

**Streaming-first, always.** The provider is consumed as a stream on every turn, even when the client asked for a buffered JSON response. That is not an implementation shortcut — it is the only way to measure time-to-first-token and inter-token throughput, and several providers only report token usage reliably on their streaming paths. A non-streaming client simply gets the accumulated result at the end; the telemetry is identical either way. Concurrency is bounded twice: a server-wide `MaxConcurrentChats` semaphore (excess requests get 429 immediately, no queueing) and a per-endpoint `MaxConcurrentRequests` limiter whose wait time is recorded on the turn as `LimiterWaitMs`.

**Retries.** A provider call that fails before the first token — connection failure, 429, or any 5xx — is retried up to `MaxRetries` times with exponential backoff (`RetryBackoffMs`, doubling per attempt). Once the first token has been streamed to the client there is no retry: the client has already seen partial output, and replaying the turn would duplicate it. Failed turns are persisted with `Success=false`, the upstream status, the error message, and whatever partial content arrived, so a dead endpoint leaves a diagnosable trail rather than a silent gap in the thread.

After the loop, the turn is persisted (with its tool transcript and a serialized copy of the result as `TelemetryJson`), metrics are recorded, and — on a thread's first successful exchange — a short title is generated from the opening message with a best-effort extra model call.

## Security Model

Tool calls execute in-process against the same agnostic service handler REST uses, and every call runs under the calling user's authentication context. The dispatcher forces the caller's tenant GUID onto the request after argument binding, so nothing the model writes into tool arguments can reach another tenant, and each call passes through the standard authorization service — a user who cannot read a graph over REST cannot read it by asking the model nicely. The MCP server's configured elevated credential plays no part here; chat tools never inherit it.

The catalog itself is the second line of defense. It is curated and read-leaning: 23 read tools covering graphs, nodes, edges, traversal, labels, tags, and vector search. Nine mutation tools (`graph/create|update|delete`, `node/create|update|delete`, `edge/create|update|delete`) exist but are advertised only when the tenant has set `EnableMutationTools` — which defaults to `false` — and a mutation call that arrives anyway is refused at dispatch. Everything else the server can do — tenant management, users, credentials, roles, backups, settings — is simply not in the catalog. The model cannot call what it has never been told exists.

Tool failures (unknown tool, bad arguments, authorization denial, handler error) are returned to the model as readable tool-level errors rather than aborting the turn, so the model can correct course; they still count in the tool metrics with `success="false"`.

## Advertised Tools

Tool names mirror the MCP catalog (a parity test asserts alignment), so a graph agent built against MCP transfers its vocabulary directly. The full set, from `src/LiteGraph.Server/Services/Chat/ChatToolCatalog.cs`:

| Group | Tools |
|---|---|
| Graph (read) | `graph/all`, `graph/get`, `graph/search`, `graph/statistics` |
| Node (read) | `node/readallingraph`, `node/get`, `node/search`, `node/neighbors`, `node/children`, `node/parents` |
| Edge (read) | `edge/readallingraph`, `edge/get`, `edge/search`, `edge/betweennodes`, `edge/fromnode`, `edge/tonode` |
| Vector | `vector/search` |
| Labels and tags | `label/readallingraph`, `label/readmanynode`, `label/readmanyedge`, `tag/readallingraph`, `tag/readmanynode`, `tag/readmanyedge` |
| Mutations (opt-in) | `graph/create`, `graph/update`, `graph/delete`, `node/create`, `node/update`, `node/delete`, `edge/create`, `edge/update`, `edge/delete` |

`vector/search` is the one deliberate divergence from MCP: it takes natural-language `text` rather than raw embeddings, and the server embeds it through the tenant's embedding endpoint before searching. Models produce text readily; they do not produce 1024-dimension float arrays. Without an embedding endpoint configured, the tool reports itself unavailable to the model instead of failing the turn.

## Turn Telemetry Reference

Every turn records where its time went. The waterfall the dashboard renders is built entirely from these columns:

| Field | Meaning |
|---|---|
| `EmbeddingDurationMs` | Time embedding the user message for retrieval; null when retrieval did not run |
| `RetrievalDurationMs` | Total retrieval stage time (embedding plus vector search); null when retrieval did not run |
| `RetrievedChunkCount` | Context chunks injected into the prompt |
| `LimiterWaitMs` | Time queued on the endpoint's `MaxConcurrentRequests` limiter |
| `InferenceConnectionMs` | Request start to response headers on the final inference call |
| `TimeToFirstTokenMs` | First token latency on the final inference call |
| `TimeToLastTokenMs` | Last token latency on the final inference call |
| `TotalDurationMs` | Wall-clock duration of the whole turn, all stages included |
| `PromptTokens` / `CompletionTokens` | Usage as reported by the provider; null when the provider reported none |
| `TokensPerSecondOverall` | Throughput across the whole response, including time to first token |
| `TokensPerSecondGeneration` | Throughput between first and last token — the model's raw generation speed |
| `ToolLoopIterations` | Model calls within the turn (1 means no tool use) |
| `ToolCallCount` | Individual tool executions within the turn |
| `RetryCount` | Provider retries before the response started |
| `Success` / `HttpStatus` / `Error` | Outcome; upstream status and message on failure, null on success |
| `TraceId` | Correlates the turn with its distributed trace and request history |

`ToolTranscriptJson` holds the ordered tool calls with arguments, outcome, and per-call runtime; `TelemetryJson` holds the serialized completion result. Both are display payloads, never queried relationally.

## Server Settings Versus Tenant Settings

Policy splits across two owners. The operator sets the guardrails in `litegraph.json` under the `Chat` block — the feature kill switch, retry counts and backoff, the tool-iteration ceiling, the concurrency cap, SSE keep-alive, and the fallback timeout. Tenant administrators set behavior within those guardrails through `PUT /chat/settings` — default endpoints, the system prompt, whether tools and mutations and retrieval are enabled, retrieval tuning, and history retention. Where the two overlap, the server wins: a tenant asking for 40 tool iterations under a server cap of 25 gets 25.

The `Chat` block is read at startup and is documented field by field in [SETTINGS.md](SETTINGS.md). Tenant chat settings apply on the next completion request, no restart involved; when a tenant has never saved a record, the documented defaults apply.

History retention runs as a background sweep: turns older than the tenant's `HistoryRetentionDays` are pruned hourly, and a value of `0` retains forever.

## Endpoint Health

Each endpoint with `HealthCheckEnabled` (the default) is probed in the background: `HealthCheckMethod` against `HealthCheckUrl` (or the base endpoint URL when unset) every `HealthCheckIntervalMs`, expecting `HealthCheckExpectedStatusCode` within `HealthCheckTimeoutMs`. Probes are deduplicated by target — endpoints sharing the same URL, method, expected status, and authentication material share one probe loop (running at the fastest interval among them) and all report the shared verdict, so five models on one Ollama host cost one probe, not five. Verdicts are debounced through consecutive-count thresholds — `HealthyThreshold` successes to go healthy, `UnhealthyThreshold` failures to go unhealthy — so one dropped packet does not flap the state. Probes send no credentials unless `HealthCheckUseAuth` is set.

The health read routes return the current verdict (`Healthy` is null until monitoring reaches one), consecutive counts, an uptime percentage, and a 24-hour probe history. Health state is advisory: an unhealthy endpoint is still usable for completions, which matters because a probe target and an inference path can disagree — but the state feeds `litegraph_chat_endpoint_healthy` and transition counters, so an endpoint dying shows up on the dashboard before users report it. Connectivity can also be tested on demand through `POST /chat/endpoints/{guid}/test`, which additionally lists the upstream's models and checks that the configured model exists, for providers that expose a model inventory.

Chat observability — the full metric inventory, trace spans, and the provisioned Grafana chat dashboard — is documented in [OBSERVABILITY.md](OBSERVABILITY.md).
