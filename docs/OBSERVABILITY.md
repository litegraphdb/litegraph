# LiteGraph Observability

## v8.1 — Operational Metrics: Backups, Imports, Index Rebuilds, Retention

Administrative and maintenance operations are instrumented alongside the request-path metrics. Labels stay low-cardinality (`operation`, `result`, `component`, `index_type`, `success`); tenant and graph GUIDs never appear on metric labels.

**Operational metric inventory.**

| Metric | Type | Labels | Meaning |
|---|---|---|---|
| `litegraph_backup_operations_total` | counter | `operation`, `success` | Backup admin operations (`create`, `read`, `read_all`, `enumerate`, `exists`, `delete`) |
| `litegraph_backup_operation_duration_ms` | summary | `operation`, `success` | Backup operation duration (sum/count) |
| `litegraph_graph_import_records_total` | counter | `result` | Records processed by JSONL imports, split `created`/`updated`/`skipped` (graphs + nodes + edges) |
| `litegraph_graph_import_warnings_total` | counter | — | Warnings raised during JSONL imports (dropped dangling edges, malformed lines) |
| `litegraph_vector_index_rebuilds_total` | counter | `index_type`, `success` | HNSW vector index rebuilds |
| `litegraph_vector_index_rebuild_vectors_total` | counter | `index_type`, `success` | Vectors added during index rebuilds |
| `litegraph_vector_index_rebuild_duration_ms` | summary | `index_type`, `success` | Index rebuild duration (sum/count) |
| `litegraph_retention_sweeps_total` | counter | `component`, `success` | Retention sweep passes (`request_history`, `chat_history`) |
| `litegraph_retention_sweep_duration_ms` | summary | `component`, `success` | Retention sweep duration (sum/count) |
| `litegraph_retention_deleted_total` | counter | `component` | Records deleted by retention sweeps (`chat_history` reports zero because the underlying delete does not return a count) |
| `litegraph_request_history_dropped_total` | counter | — | Request history captures dropped because the bounded capture queue was full (non-zero means history data loss under load) |

The same instruments are exposed through the .NET `Meter` for OTLP consumers: `litegraph.backup.operations`, `litegraph.backup.operation.duration`, `litegraph.graph.import.records`, `litegraph.graph.import.warnings`, `litegraph.server.vector.index.rebuilds`, `litegraph.server.vector.index.rebuild.vectors`, `litegraph.server.vector.index.rebuild.duration`, `litegraph.retention.sweeps`, `litegraph.retention.sweep.duration`, `litegraph.retention.deleted`, and `litegraph.request_history.dropped`. The core `LiteGraph` meter additionally emits `litegraph.vector.index.rebuilds`, `litegraph.vector.index.rebuild.vectors`, and `litegraph.vector.index.rebuild.duration` from the index manager itself.

Counts, durations, and status classes for the backup, import/export (JSONL and GEXF), and token issuance HTTP routes are additionally covered by the per-route `litegraph_http_*` family (routes such as `backup`, `graph.export.jsonl`, `graph.import.jsonl`, `token.create`), so the dedicated series above add operation-level success/failure and volume detail rather than duplicating request accounting.

**New trace spans.** `litegraph.backup` (internal, tag `litegraph.backup.operation`) wraps backup creation under the REST request span. `litegraph.vector.index.rebuild` (internal, tags `litegraph.graph.guid`, `litegraph.vector.index.type`, `litegraph.vector.index.vectors`) is emitted by the core `LiteGraph` activity source around HNSW index rebuilds.

---

## v8.1 — Chat Metrics, Traces, And Dashboard

The chat feature ships fully instrumented from day one: every completion, tool call, retrieval, embedding request, retry, feedback submission, and endpoint health probe is measured. Labels stay low-cardinality on purpose — `provider`, `model`, `tool` (a fixed catalog), `endpoint` (endpoint name, a small tenant-managed set), `streamed`, `status_class`, `success`, `rating`, `to_state` — while per-turn GUIDs, exact timings, and transcripts live on trace spans and the persisted `ChatTurn` record instead of metric labels.

**Chat metric inventory.** All series carry the `component` label; the additional labels per family are listed below.

| Metric | Type | Labels | Meaning |
|---|---|---|---|
| `litegraph_chat_requests_total` | counter | `provider`, `model`, `streamed`, `status_class` | Chat completion requests processed |
| `litegraph_chat_request_errors_total` | counter | `provider`, `model`, `streamed`, `status_class` | Completion requests that ended in an error (status >= 400) |
| `litegraph_chat_request_duration_ms` | histogram | `provider`, `model`, `streamed`, `status_class` | Total turn duration, all stages included |
| `litegraph_chat_ttft_ms` | histogram | `provider`, `model` | Time to first token on the final inference call |
| `litegraph_chat_tokens_prompt_total` | counter | `provider`, `model` | Prompt tokens consumed, as reported by the provider |
| `litegraph_chat_tokens_completion_total` | counter | `provider`, `model` | Completion tokens produced, as reported by the provider |
| `litegraph_chat_tokens_per_second` | histogram | `provider`, `model` | Overall tokens per second per turn |
| `litegraph_chat_tool_iterations` | histogram | `provider`, `model` | Tool loop iterations per turn (1 means no tool use) |
| `litegraph_chat_retries_total` | counter | `provider` | Pre-first-token provider retries |
| `litegraph_chat_tool_calls_total` | counter | `tool`, `success` | Tool calls executed by the in-process dispatcher |
| `litegraph_chat_tool_duration_ms` | histogram | `tool`, `success` | Tool call execution duration |
| `litegraph_chat_rag_duration_ms` | histogram | — | Retrieval stage duration (embedding plus vector search) |
| `litegraph_chat_embedding_requests_total` | counter | `provider`, `model`, `success` | Embedding requests (retrieval and the `vector/search` tool) |
| `litegraph_chat_embedding_duration_ms` | histogram | `provider`, `model`, `success` | Embedding request duration |
| `litegraph_chat_feedback_total` | counter | `rating` | Feedback submissions, split ThumbsUp/ThumbsDown |
| `litegraph_chat_healthcheck_duration_ms` | histogram | `endpoint`, `endpoint_type`, `success` | Endpoint health probe duration |
| `litegraph_chat_healthcheck_transitions_total` | counter | `endpoint`, `to_state` | Health state transitions (`healthy`/`unhealthy`) |
| `litegraph_chat_endpoint_healthy` | gauge | `endpoint`, `endpoint_type` | Current health state, 1 healthy / 0 unhealthy; the series is removed when an endpoint is deleted or unmonitored |
| `litegraph_chat_active` | gauge | — | Chat completions currently in flight |

The same instruments are exposed through the .NET `Meter` under `litegraph.chat.*` names (`litegraph.chat.requests`, `litegraph.chat.ttft`, `litegraph.chat.tool.calls`, and so on) for OpenTelemetry consumers, including the built-in OTLP exporter.

**Chat trace spans.** A completion turn produces a span tree under the REST request activity:

- `chat.turn` (internal) — the whole turn. Tags: `litegraph.tenant.guid`, `litegraph.chat.thread.guid`, `litegraph.chat.turn.guid`, `litegraph.chat.provider`, `litegraph.chat.model`, `litegraph.chat.streamed`; on completion `litegraph.chat.success`, `litegraph.chat.tool.calls`, `litegraph.chat.tokens.prompt`, `litegraph.chat.tokens.completion`, `litegraph.chat.retries`, and `litegraph.chat.error` on failure.
- `chat.rag.search` (internal) — the retrieval stage. Tags: `litegraph.chat.rag.results`, and `litegraph.chat.rag.error` when retrieval failed non-fatally.
- `chat.rag.embed` (client) — each embedding request. Tags: `litegraph.chat.provider`, `litegraph.chat.model`.
- `chat.llm.request` (client) — each provider inference call, one per attempt. Tags: `litegraph.chat.provider`, `litegraph.chat.model`, `litegraph.chat.attempt`.
- `chat.tool.execute` (internal) — each tool call. Tags: `litegraph.chat.tool`, `litegraph.chat.tool.success`.

The turn record stores the active trace ID as `TraceId`, so a slow or failed turn in the dashboard's history view links directly to its distributed trace.

**Dashboard.** A dedicated **LiteGraph Chat** Grafana dashboard is provisioned alongside the existing observability dashboard, covering request rate and errors, TTFT and duration percentiles, token throughput and consumption, tool-call rate by tool, endpoint health and probe failures, retries, and the feedback ratio.

---

## v8.0 — REST And MCP Metrics, And Logs In Grafana

v8.0 closed the remaining gaps: every REST route and every MCP tool is measured, and logs reach Grafana.

**Unified metrics.** REST and MCP emit the same metric names, distinguished by a `component` label (`rest` or `mcp`), so a Grafana panel treats them as one system:

- `litegraph_http_requests_total` — counter. REST labels: `component`, `route`, `method`, `status_class`. MCP labels: `component`, `transport` (`http`/`tcp`/`ws`), `tool`, `status_class`.
- `litegraph_http_request_duration_ms` — histogram, same labels.
- `litegraph_http_request_errors_total` — counter, incremented on error responses.
- `litegraph_http_requests_in_flight` — gauge, by `component` (and `transport` for MCP).

The REST `route` label is a low-cardinality template name derived from the request type (for example `graph.export.jsonl`), not the raw path, so per-GUID cardinality never leaks in. Because the label is derived from the request-type enumeration, a new route is instrumented by construction; a startup assertion fails if any request type maps to a missing or duplicate label.

**Scrape targets.** The REST server exposes `/metrics` on `8701`; the MCP server exposes its own `/metrics` on `8705` (configurable via `MCP_METRICS_HOSTNAME`/`MCP_METRICS_PORT`). Prometheus scrapes both.

**Logs into Grafana.** LiteGraph ships structured syslog to Grafana Alloy, which stamps `component=rest` (received on `:1514`) or `component=mcp` (received on `:2514`) and forwards to Loki. Grafana carries both a Prometheus and a Loki datasource. In Grafana Explore, query `{service=~"litegraph.*"}` and filter by `component` and severity; a logged error lines up in time with the metric error spike. SyslogLogging is `2.2.2`.

**Dashboards.** The provisioned Grafana dashboards cover request rate, latency, and errors per REST route and per MCP tool (unified via `component`), in-flight requests, the storage and transaction panels retained from v7, and a Loki logs panel filtered by `component` and severity.

---

LiteGraph exposes production observability through Prometheus-compatible metrics and OpenTelemetry-compatible .NET instrumentation hooks.

The current implementation provides:

- an unauthenticated Prometheus scrape endpoint, enabled by default at `/metrics`
- request ID and correlation ID response headers
- request timeout cancellation for REST authentication, generic REST handlers, native graph queries, and graph transactions
- HTTP request counters and duration summaries
- native graph query counters and duration summaries
- vector search counters, result counters, and duration summaries
- graph transaction counters, operation counters, and duration summaries
- authentication and authorization result counters
- storage backend info gauge
- storage connection pool and command timeout gauges where configuration exposes them
- latest-observed entity count gauges from tenant and graph statistics responses
- a .NET `Meter` and `ActivitySource` named from `Settings.Observability.ServiceName`
- optional built-in OTLP export for server and core LiteGraph metrics/traces
- server-side trace activities for REST requests, authentication/authorization, generic REST handlers, native graph queries, and graph transactions
- W3C `traceparent`/`tracestate` parent context parsing for REST request activities
- request history capture as a separate operational data source
- optional single-line JSON formatting for supported operational request logs

## Configuration

Observability settings live under `Settings.Observability`.

```json
{
  "Observability": {
    "Enable": true,
    "EnablePrometheus": true,
    "EnableOpenTelemetry": true,
    "EnableOtlpExporter": false,
    "MetricsPath": "/metrics",
    "ServiceName": "LiteGraph.Server",
    "OtlpEndpoint": "http://localhost:4317",
    "OtlpProtocol": "grpc",
    "OtlpHeaders": null,
    "OtlpTimeoutMilliseconds": 10000
  }
}
```

Defaults:

- `Enable`: `true`
- `EnablePrometheus`: `true`
- `EnableOpenTelemetry`: `true`
- `EnableOtlpExporter`: `false`
- `MetricsPath`: `/metrics`
- `ServiceName`: `LiteGraph.Server`
- `OtlpEndpoint`: `null`
- `OtlpProtocol`: `grpc`
- `OtlpHeaders`: `null`
- `OtlpTimeoutMilliseconds`: `10000`

`MetricsPath` may be set without a leading slash; LiteGraph normalizes it to an absolute path.

Supported OTLP protocols are `grpc` and `http/protobuf`. `http-protobuf` is accepted as an alias.

## Request Lifecycle

LiteGraph applies `Settings.RequestTimeoutSeconds` to REST authentication, generic agnostic request handlers, authorization management, request history, token detail, graph update, GEXF export, vector-index routes, native graph query execution, and graph transaction execution. The default is 60 seconds. Values must be between 1 and 3600 seconds.

The timeout can be overridden with:

```text
LITEGRAPH_REQUEST_TIMEOUT_SECONDS=60
```

When the request timeout fires, REST returns HTTP 408 with the `RequestTimeout` API error code.

## Prometheus

Scrape the LiteGraph server metrics endpoint:

```yaml
scrape_configs:
  - job_name: litegraph
    static_configs:
      - targets:
          - localhost:8701
    metrics_path: /metrics
```

The metrics endpoint is registered before authentication when observability and Prometheus are enabled. This is intentional for the initial release slice and should be revisited before deployments that require authenticated metrics.

## Grafana

Importable Grafana dashboard templates are available under `assets/grafana/`. The dashboards are split per domain, all tagged `litegraph`, and are provisioned into the `LiteGraph` folder by the bundled compose deployment:

- **LiteGraph Overview** (`litegraph-overview.json`) — the landing dashboard: one row of high-level stats (request rate, average latency, error rate, active chats, active transactions, entity counts) plus a dashboard list linking to the other dashboards.
- **LiteGraph API Requests** (`litegraph-api-requests.json`) — REST and MCP request rates by route/tool, error rates by component and status class, request latency percentiles (p50/p95), in-flight requests, authentication and authorization outcomes, and token issuance routes.
- **LiteGraph Graphs and Queries** (`litegraph-graphs-queries.json`) — native graph query rate, outcomes, and latency; graph transactions (rate, outcomes, retries, conflicts, queue wait, commit/rollback durations, active gauge); repository operation rates and durations; latest entity counts; and JSONL graph import records and warnings.
- **LiteGraph Vector Search** (`litegraph-vector-search.json`) — vector search rate, duration, and result counts by domain, plus HNSW vector index rebuilds (rate, duration, vectors added).
- **LiteGraph Storage** (`litegraph-storage.json`) — storage backend configuration, connection pool and command timeout settings, backup operations and durations, retention sweeps and deletions, request-history capture drops, and database flush requests.
- **LiteGraph Logs** (`litegraph-logs.json`) — Loki panels: log volume by severity over time, recent REST and MCP logs with component and severity filters, and an error/critical-only panel.
- **LiteGraph Chat and Inference** (`litegraph-chat-inference.json`) — chat request rate and errors, TTFT and duration percentiles, token throughput and consumption, tool calls, retrieval and embedding, endpoint health, and feedback.

All dashboards except Logs expect a Prometheus datasource; the Logs dashboard expects a Loki datasource.

## Quick Start: Prometheus And Grafana

The checked-in Docker Compose deployment starts LiteGraph, LiteGraph MCP, Prometheus, and Grafana OSS with the datasource and LiteGraph dashboard already provisioned.

```bash
cd docker
docker compose up -d
```

Then open:

- Grafana: `http://localhost:3000` with `admin` / `admin`
- Prometheus targets: `http://localhost:9090/targets`
- LiteGraph metrics: `http://localhost:8701/metrics`

In Grafana, browse to the `LiteGraph` folder and open the **LiteGraph Overview** dashboard, which links to the per-domain dashboards. Some panels remain empty until the corresponding LiteGraph operations have run.

The bundled compose path uses:

- [`docker/litegraph.json`](docker/litegraph.json), where `Observability.EnablePrometheus` is enabled and `MetricsPath` is `/metrics`
- [`docker/prometheus.yaml`](../docker/prometheus.yaml), where Prometheus scrapes `localhost:8701`
- [`docker/grafana/provisioning/datasources/litegraph-prometheus.yml`](../docker/grafana/provisioning/datasources/litegraph-prometheus.yml), where Grafana points to Prometheus
- [`assets/grafana/`](../assets/grafana/), the provisioned per-domain dashboards

The manual example below assumes LiteGraph.Server is running locally on `http://localhost:8701` and that `Settings.Observability.EnablePrometheus` is `true`.

1. Verify that LiteGraph exposes metrics:

```bash
curl http://localhost:8701/metrics
```

You should see Prometheus text output with metrics such as `litegraph_http_requests_total`.

2. Create `prometheus.yaml`:

```yaml
global:
  scrape_interval: 15s

scrape_configs:
  - job_name: litegraph
    metrics_path: /metrics
    static_configs:
      - targets:
          - host.docker.internal:8701
```

Use `localhost:8701` when Prometheus runs directly on the same host as LiteGraph. Use `host.docker.internal:8701` when Prometheus runs in Docker Desktop and LiteGraph runs on the host. On Linux Docker hosts, use the container network name or add an `extra_hosts` mapping for `host.docker.internal`.

3. Start Prometheus and Grafana with Docker:

```yaml
services:
  prometheus:
    image: prom/prometheus:latest
    command:
      - --config.file=/etc/prometheus/prometheus.yaml
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yaml:/etc/prometheus/prometheus.yaml:ro

  grafana:
    image: grafana/grafana:latest
    ports:
      - "3000:3000"
    environment:
      GF_SECURITY_ADMIN_USER: admin
      GF_SECURITY_ADMIN_PASSWORD: admin
    depends_on:
      - prometheus
```

Save this as `compose.observability.yml`, then run:

```bash
docker compose -f compose.observability.yml up
```

4. Confirm Prometheus is scraping LiteGraph:

- Open `http://localhost:9090/targets`.
- Confirm the `litegraph` target is `UP`.
- Query `litegraph_http_requests_total` from the Prometheus graph page.

5. Configure Grafana:

- Open `http://localhost:3000`.
- Sign in with `admin` / `admin` unless you changed the compose environment.
- Add a Prometheus datasource with URL `http://prometheus:9090`.
- Import the dashboards under `assets/grafana/`, starting with `litegraph-overview.json` (import `litegraph-logs.json` only if you also run Loki).
- Select the Prometheus (or Loki) datasource during import.

6. Generate traffic and refresh the dashboard:

```bash
curl http://localhost:8701/v1.0/tenants
curl http://localhost:8701/v1.0/requesthistory
```

The HTTP panels should begin showing request rate, status mix, and latency. Query, transaction, vector, repository, and authorization panels populate after those operations run.

## Prometheus Metrics

### HTTP Requests

```text
litegraph_http_requests_total{method="GET",path="/v1.0/tenants",status_code="200"} 12
litegraph_http_request_duration_ms_bucket{method="GET",path="/v1.0/tenants",status_code="200",le="25"} 8
litegraph_http_request_duration_ms_bucket{method="GET",path="/v1.0/tenants",status_code="200",le="+Inf"} 12
litegraph_http_request_duration_ms_sum{method="GET",path="/v1.0/tenants",status_code="200"} 40.5
litegraph_http_request_duration_ms_count{method="GET",path="/v1.0/tenants",status_code="200"} 12
```

Labels:

- `method`
- `path`
- `status_code`
- `le` on duration bucket samples

### Native Graph Queries

```text
litegraph_graph_queries_total{mutated="false",success="true"} 4
litegraph_graph_query_duration_ms_sum{mutated="false",success="true"} 21.7
litegraph_graph_query_duration_ms_count{mutated="false",success="true"} 4
```

Labels:

- `mutated`
- `success`

### Vector Search

```text
litegraph_vector_searches_total{domain="Node",success="true"} 4
litegraph_vector_search_results_total{domain="Node",success="true"} 20
litegraph_vector_search_duration_ms_sum{domain="Node",success="true"} 12.4
litegraph_vector_search_duration_ms_count{domain="Node",success="true"} 4
```

Labels:

- `domain`
- `success`

Vector search metrics are recorded for successful native graph query `CALL litegraph.vector.search...` executions at the REST boundary.

### Graph Transactions

```text
litegraph_graph_transactions_total{success="true",rolled_back="false",validation_failure="false",provider="Postgresql",isolation_level="Serializable",state="Committed",serialized_by_gate="false",retryable="false",concurrency_conflict="false"} 3
litegraph_graph_transaction_operations_total{success="true",rolled_back="false",validation_failure="false",provider="Postgresql",isolation_level="Serializable",state="Committed",serialized_by_gate="false",retryable="false",concurrency_conflict="false"} 9
litegraph_graph_transaction_duration_ms_sum{success="true",rolled_back="false",validation_failure="false",provider="Postgresql",isolation_level="Serializable",state="Committed",serialized_by_gate="false",retryable="false",concurrency_conflict="false"} 18.4
litegraph_graph_transaction_duration_ms_count{success="true",rolled_back="false",validation_failure="false",provider="Postgresql",isolation_level="Serializable",state="Committed",serialized_by_gate="false",retryable="false",concurrency_conflict="false"} 3
litegraph_graph_transaction_queue_wait_duration_ms_sum{success="true",rolled_back="false",validation_failure="false",provider="Postgresql",isolation_level="Serializable",state="Committed",serialized_by_gate="false",retryable="false",concurrency_conflict="false"} 0
litegraph_graph_transaction_queue_wait_duration_ms_count{success="true",rolled_back="false",validation_failure="false",provider="Postgresql",isolation_level="Serializable",state="Committed",serialized_by_gate="false",retryable="false",concurrency_conflict="false"} 3
litegraph_graph_transaction_commit_duration_ms_sum{success="true",rolled_back="false",validation_failure="false",provider="Postgresql",isolation_level="Serializable",state="Committed",serialized_by_gate="false",retryable="false",concurrency_conflict="false"} 4.2
litegraph_graph_transaction_commit_duration_ms_count{success="true",rolled_back="false",validation_failure="false",provider="Postgresql",isolation_level="Serializable",state="Committed",serialized_by_gate="false",retryable="false",concurrency_conflict="false"} 3
litegraph_graph_transaction_rollback_duration_ms_sum{success="false",rolled_back="true",validation_failure="false",provider="Postgresql",isolation_level="Serializable",state="RolledBack",serialized_by_gate="false",retryable="true",concurrency_conflict="true"} 2.8
litegraph_graph_transaction_rollback_duration_ms_count{success="false",rolled_back="true",validation_failure="false",provider="Postgresql",isolation_level="Serializable",state="RolledBack",serialized_by_gate="false",retryable="true",concurrency_conflict="true"} 1
litegraph_graph_transaction_conflicts_total{success="false",rolled_back="true",validation_failure="false",provider="Postgresql",isolation_level="Serializable",state="RolledBack",serialized_by_gate="false",retryable="true",concurrency_conflict="true"} 1
litegraph_graph_transaction_retries_total{success="false",rolled_back="true",validation_failure="false",provider="Postgresql",isolation_level="Serializable",state="RolledBack",serialized_by_gate="false",retryable="true",concurrency_conflict="true"} 0
litegraph_graph_transaction_active 0
```

Labels:

- `success`
- `rolled_back`
- `validation_failure`
- `provider`
- `isolation_level`
- `state`
- `serialized_by_gate`
- `retryable`
- `concurrency_conflict`

`litegraph_graph_transaction_queue_wait_duration_ms` measures time spent waiting on the legacy serialized fallback gate. It should remain near zero for PostgreSQL and SQLite v7 transaction-local sessions. A non-zero value usually means an unconverted provider is using the compatibility path.

`litegraph_graph_transaction_commit_duration_ms` and `litegraph_graph_transaction_rollback_duration_ms` isolate provider commit/rollback cost from operation execution time. `litegraph_graph_transaction_conflicts_total` increments for transactions classified as provider concurrency conflicts. `litegraph_graph_transaction_retries_total` reports retries recorded by LiteGraph; automatic retries are not enabled for normal graph transaction requests. `litegraph_graph_transaction_active` is a point-in-time gauge of REST graph transactions currently executing.

### Authentication And Authorization

```text
litegraph_authentication_requests_total{authentication_result="Success",authorization_result="Allowed"} 20
litegraph_authentication_requests_total{authentication_result="Success",authorization_result="Denied"} 2
```

Labels:

- `authentication_result`
- `authorization_result`

### Repository Operations

```text
litegraph_repository_operations_total{provider="Sqlite",operation="read",success="true"} 42
litegraph_repository_operation_duration_ms_sum{provider="Sqlite",operation="read",success="true"} 51.2
litegraph_repository_operation_duration_ms_count{provider="Sqlite",operation="read",success="true"} 42
```

Labels:

- `provider`
- `operation`
- `success`

SQLite repository primitives classify operations as `read`, `write`, `transaction`, or `batch`. The metric labels never include SQL text, parameter values, credentials, or vector payloads.

### Storage Backend

```text
litegraph_storage_backend_info{provider="Sqlite",production_recommended="false"} 1
litegraph_storage_connection_pool_max{provider="Sqlite"} 32
litegraph_storage_command_timeout_seconds{provider="Sqlite"} 30
```

Labels:

- `provider`
- `production_recommended`

`litegraph_storage_connection_pool_max` and `litegraph_storage_command_timeout_seconds` use the `provider` label only. They expose configured limits from `Settings.LiteGraph.Database`; they do not report active pool utilization because the current repository abstraction does not expose live pool state.

### Entity Counts

```text
litegraph_entity_count{scope="tenant",entity="nodes"} 12
litegraph_entity_count{scope="graph",entity="edges"} 30
```

Labels:

- `scope`
- `entity`

Entity count gauges are updated from existing statistics responses instead of polling the database independently. Scopes are intentionally low cardinality: `tenant`, `all_tenants`, `graph`, and `tenant_graphs`. Tenant and graph GUIDs are not used as metric labels.

## OpenTelemetry

LiteGraph creates server instrumentation objects when `EnableOpenTelemetry` is true:

- `Meter`: `Settings.Observability.ServiceName`
- `ActivitySource`: `Settings.Observability.ServiceName`

The core `LiteGraph` library also exposes always-available `ActivitySource` and `Meter` instances named `LiteGraph` for embedded client work such as native query execution, vector search, vector index lookup, and repository operations. The source only emits activities when an application has subscribed to it through an `ActivityListener` or OpenTelemetry.

The current server implementation records metrics through the .NET `Meter` and emits REST/server activities through the configured server `ActivitySource`. Core query and vector activities are emitted through the `LiteGraph` source so direct client, REST, MCP, and console paths share the same query internals.

Current server activities:

- REST request activity: server span named `<METHOD> <PATH>`
- authentication/authorization child activity: `litegraph.auth`
- generic agnostic REST handler child activity: `litegraph.rest.handler`
- native graph query child activity: `litegraph.graph.query`
- graph transaction child activity: `litegraph.graph.transaction`

Core activities:

- native query activity: `litegraph.query`
- query parse phase child activity: `litegraph.query.parse`
- query plan phase child activity: `litegraph.query.plan`
- query executor phase child activity: `litegraph.query.execute`
- vector search activity: `litegraph.vector.search`
- SQLite HNSW vector index search activity: `litegraph.vector.index.search`
- vector index rebuild activity: `litegraph.vector.index.rebuild`
- repository operation activity: `litegraph.repository.operation`
- graph transaction activity: `litegraph.transaction`

REST request activities parse incoming W3C `traceparent` and `tracestate` headers. When a valid parent context is supplied, LiteGraph starts its request activity under that trace. Server query activities include the required scope, success state, mutation state, row count, query kind, and vector-search tags where applicable. Core query activities add parse, plan, execute, planner seed, estimated cost, row count, and object count tags without recording query text. Vector search activities include domain, search type, dimensions, filter presence, top-k, and result count. Vector index activities include index type, dirty state, used/skip reason, top-k, and result count. Transaction activities include transaction ID, provider, isolation level, operation count, lifecycle state, success state, validation-failure state, rollback state, serialized fallback state, queue wait, commit/rollback duration, retryability, concurrency-conflict classification, provider error code, and failed operation index where applicable.

Graph transaction activities emit lifecycle events named `litegraph.transaction.validated`, `litegraph.transaction.started`, `litegraph.transaction.committed`, and `litegraph.transaction.rolled_back` where those lifecycle steps occur.

Repository operation activities include provider, operation, transactional state, statement count, row count, success, and duration tags. They do not include SQL text.

Core metrics:

- `litegraph.repository.operations`
- `litegraph.repository.operation.duration`
- `litegraph.vector.searches`
- `litegraph.vector.search.results`
- `litegraph.vector.search.duration`
- `litegraph.vector.index.mutation.failures`
- `litegraph.vector.index.rebuilds`
- `litegraph.vector.index.rebuild.vectors`
- `litegraph.vector.index.rebuild.duration`

`litegraph.vector.index.mutation.failures` increments when a staged vector-index mutation fails after the database transaction has committed. It is tagged by repository provider, vector index type, and error type. A non-zero value means the database commit succeeded but the derived vector index was marked dirty and should be rebuilt before relying on indexed vector search latency or recall.

Applications embedding LiteGraph can subscribe to the meter name:

```csharp
builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter("LiteGraph.Server", "LiteGraph");
        metrics.AddOtlpExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.AddSource("LiteGraph.Server", "LiteGraph");
        tracing.AddOtlpExporter();
    });
```

If the configured service name changes, include both the configured server name and the fixed `LiteGraph` core name in `AddMeter` and `AddSource`.

### Embedded C# Observability Example

In-process C# callers do not need a separate SDK package to access observability metadata. The core `LiteGraph` assembly exposes:

- `LiteGraphTelemetry.ActivitySourceName`
- `LiteGraphTelemetry.MeterName`
- `LiteGraphTelemetry.ActivitySource`
- `LiteGraphTelemetry.Meter`

Example OpenTelemetry setup for an embedded service:

```csharp
using LiteGraph;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics.AddMeter(LiteGraphTelemetry.MeterName);
        metrics.AddOtlpExporter();
    })
    .WithTracing(tracing =>
    {
        tracing.AddSource(LiteGraphTelemetry.ActivitySourceName);
        tracing.AddOtlpExporter();
    });
```

For query-level timing in SDK consumers, set `IncludeProfile = true` on `GraphQueryRequest`. The response `ExecutionProfile` reports parse, plan, execution, repository, vector-search, transaction, and total timing without recording query text or parameter values in telemetry.

### Built-In OTLP Export

LiteGraph can create OpenTelemetry SDK tracer and meter providers itself when `EnableOtlpExporter` is true. The built-in exporter subscribes to:

- the configured server `ActivitySource` and `Meter`
- the core `LiteGraph` `ActivitySource` and `Meter`

Example:

```json
{
  "Observability": {
    "Enable": true,
    "EnableOpenTelemetry": true,
    "EnableOtlpExporter": true,
    "ServiceName": "LiteGraph.Server",
    "OtlpEndpoint": "http://localhost:4317",
    "OtlpProtocol": "grpc"
  }
}
```

Environment variables:

- `LITEGRAPH_OTLP_ENABLE`: enable or disable the built-in exporter (`true`, `false`, `1`, `0`, `yes`, `no`, `on`, `off`)
- `LITEGRAPH_OTLP_ENDPOINT`: LiteGraph-specific OTLP endpoint
- `LITEGRAPH_OTLP_PROTOCOL`: LiteGraph-specific OTLP protocol
- `LITEGRAPH_OTLP_HEADERS`: LiteGraph-specific OTLP headers in `key=value,key2=value2` format
- `LITEGRAPH_OTLP_TIMEOUT_MILLISECONDS`: LiteGraph-specific OTLP timeout
- `OTEL_SERVICE_NAME`: standard OpenTelemetry service name
- `OTEL_EXPORTER_OTLP_ENDPOINT`: standard OTLP endpoint fallback
- `OTEL_EXPORTER_OTLP_PROTOCOL`: standard OTLP protocol fallback
- `OTEL_EXPORTER_OTLP_HEADERS`: standard OTLP headers fallback
- `OTEL_EXPORTER_OTLP_TIMEOUT`: standard OTLP timeout fallback

The built-in exporter is opt-in so applications embedding LiteGraph can keep ownership of their own OpenTelemetry SDK configuration.

## Query Profiling

Native graph queries can include an opt-in execution profile:

```json
{
  "Query": "MATCH (n:Person) WHERE n.name = $name RETURN n LIMIT 10",
  "Parameters": {
    "name": "Ada Lovelace"
  },
  "IncludeProfile": true
}
```

When enabled, the query response includes `ExecutionProfile` with:

- `ParseTimeMs`
- `PlanTimeMs`
- `ExecuteTimeMs`
- `RepositoryTimeMs`
- `RepositoryOperationCount`
- `VectorSearchTimeMs`
- `VectorSearchCount`
- `TransactionTimeMs`
- `TotalTimeMs`

REST query execution also adds:

- `AuthorizationTimeMs`
- `SerializationTimeMs`

`RepositoryTimeMs` and `VectorSearchTimeMs` are captured from scoped LiteGraph telemetry during the query. `TransactionTimeMs` is populated for mutation queries that run inside LiteGraph's graph-scoped mutation transaction envelope. Profiling is off by default so normal responses remain compact.

## Request History

Request history remains separate from Prometheus and OpenTelemetry. It is useful for recent request inspection and debugging, while metrics are useful for aggregate operational monitoring.

LiteGraph accepts and emits:

- `x-request-id`
- `x-correlation-id`

LiteGraph also accepts W3C `traceparent` and `tracestate`. When tracing is enabled, request history stores the active request activity trace ID. If a valid `traceparent` is supplied, that trace ID is preserved.

Request history endpoints:

- `GET /v1.0/requesthistory`
- `GET /v1.0/requesthistory/summary`
- `GET /v1.0/requesthistory/{requestGuid}`
- `GET /v1.0/requesthistory/{requestGuid}/detail`
- `DELETE /v1.0/requesthistory/{requestGuid}`
- `DELETE /v1.0/requesthistory/bulk`

Current request history behavior redacts headers through the request history service and truncates captured bodies according to request history settings.

Request history records include:

- `RequestId`
- `CorrelationId`
- `TraceId`
- `TransactionDiagnosticsJson` for graph transaction responses, including transaction ID, lifecycle state, isolation, retry/conflict state, validation state, rollback state, queue wait, commit/rollback duration, and provider error code.

Request history search accepts `hasTransactionDiagnostics=true|false` to include or exclude captured graph transaction rows, and `transactionId` to find entries by full or partial transaction ID.

`GET /v1.0/requesthistory` supports these filters:

- `requestId`
- `correlationId`
- `traceId`
- `success`

Use `success=false` to return recent failed requests for debugging and operational triage. The dashboard request-history view exposes this as an outcome filter.

## Operational Notes

- Do not expose unauthenticated `/metrics` outside trusted networks unless protected by a reverse proxy or network policy.
- Do not put bearer tokens, passwords, connection strings, or vector payloads in metric labels.
- Operational logs redact bearer-token route segments, sensitive query-string values, sensitive headers, passwords, connection strings, and vector payload query keys.
- REST debug request logging records method, sanitized URL, source, content metadata, and redacted headers; request bodies are omitted from debug request logs.
- Set `Settings.Logging.JsonLogOutput` to `true` to emit supported REST request completion logs as single-line JSON records with request ID, correlation ID, trace ID, status, and duration fields.
- Prefer stable route paths in dashboards. The current HTTP metric path label removes query strings but does not yet template route parameters.
- Use request history for recent details and Prometheus for aggregate counts and latency.
- REST request logs include request ID, correlation ID, and trace ID when available.
- REST request timeouts return HTTP 408 with `RequestTimeout`.

## Current Limits

- Prometheus bucketed histograms are currently implemented for HTTP request duration. Other duration metrics are rendered as sum/count summaries.
- Storage connection metrics expose configured pool size and command timeout only; active/idle pool utilization is not exposed yet.
- Entity count gauges are latest-observed values from statistics endpoint responses; they are empty until a statistics request has run.
- Query authorization and serialization profile timings are only available through REST query execution.
