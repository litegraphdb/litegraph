# LiteGraph Observability

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

An importable Grafana dashboard template is available at `assets/grafana/litegraph-observability-dashboard.json`.

The dashboard expects a Prometheus datasource and includes panels for:

- HTTP request rate, status mix, and average latency
- native graph query and graph transaction rates, outcomes, and average latency
- vector search rates and result counts
- repository operation rates by provider, operation, and success
- latest entity count gauges
- storage backend and configured storage limits
- authentication and authorization outcomes

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

In Grafana, browse to the `LiteGraph` folder and open the provisioned LiteGraph observability dashboard. Some panels remain empty until the corresponding LiteGraph operations have run.

The bundled compose path uses:

- [`docker/litegraph.json`](docker/litegraph.json), where `Observability.EnablePrometheus` is enabled and `MetricsPath` is `/metrics`
- [`docker/prometheus.yml`](docker/prometheus.yml), where Prometheus scrapes `localhost:8701`
- [`docker/grafana/provisioning/datasources/litegraph-prometheus.yml`](docker/grafana/provisioning/datasources/litegraph-prometheus.yml), where Grafana points to Prometheus
- [`assets/grafana/litegraph-observability-dashboard.json`](assets/grafana/litegraph-observability-dashboard.json), the provisioned dashboard

The manual example below assumes LiteGraph.Server is running locally on `http://localhost:8701` and that `Settings.Observability.EnablePrometheus` is `true`.

1. Verify that LiteGraph exposes metrics:

```bash
curl http://localhost:8701/metrics
```

You should see Prometheus text output with metrics such as `litegraph_http_requests_total`.

2. Create `prometheus.yml`:

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
      - --config.file=/etc/prometheus/prometheus.yml
    ports:
      - "9090:9090"
    volumes:
      - ./prometheus.yml:/etc/prometheus/prometheus.yml:ro

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
- Import `assets/grafana/litegraph-observability-dashboard.json`.
- Select the Prometheus datasource during import.

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
litegraph_graph_transactions_total{success="true",rolled_back="false"} 3
litegraph_graph_transaction_operations_total{success="true",rolled_back="false"} 9
litegraph_graph_transaction_duration_ms_sum{success="true",rolled_back="false"} 18.4
litegraph_graph_transaction_duration_ms_count{success="true",rolled_back="false"} 3
```

Labels:

- `success`
- `rolled_back`

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
- repository operation activity: `litegraph.repository.operation`

REST request activities parse incoming W3C `traceparent` and `tracestate` headers. When a valid parent context is supplied, LiteGraph starts its request activity under that trace. Server query activities include the required scope, success state, mutation state, row count, query kind, and vector-search tags where applicable. Core query activities add parse, plan, execute, planner seed, estimated cost, row count, and object count tags without recording query text. Vector search activities include domain, search type, dimensions, filter presence, top-k, and result count. Vector index activities include index type, dirty state, used/skip reason, top-k, and result count. Transaction activities include operation count, success state, rollback state, and failed operation index where applicable.

Repository operation activities include provider, operation, transactional state, statement count, row count, success, and duration tags. They do not include SQL text.

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
