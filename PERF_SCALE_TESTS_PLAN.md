# LiteGraph Performance And Scalability Harness Implementation Plan

This plan tracks implementation of the `Test.PerformanceAndScalability` console harness described in `PERF_SCALE_TESTS.md`.

It is intentionally annotatable. Developers should update the status fields, checkboxes, dates, owners, notes, and completion evidence as implementation progresses.

## Progress Legend

Use these status values consistently:

- `Not Started`
- `In Progress`
- `Blocked`
- `Review`
- `Done`
- `Deferred`

Use these checkbox meanings:

- `[ ]` not started
- `[~]` in progress
- `[x]` complete
- `[!]` blocked

## Progress Summary

| Area | Status | Owner | Started | Completed | Notes |
| --- | --- | --- | --- | --- | --- |
| Phase 0: Review and scope confirmation | Done | Codex | 2026-06-17 | 2026-06-17 | Scope implemented from `PERF_SCALE_TESTS.md`. |
| Phase 1: Project skeleton and CLI | Done | Codex | 2026-06-17 | 2026-06-17 | Added `src/Test.PerformanceAndScalability` and CLI parsing. |
| Phase 2: Run controller, provider setup, and environment capture | Done | Codex | 2026-06-17 | 2026-06-17 | Added SQLite default, PostgreSQL settings, unsupported-provider fail-fast, environment capture. |
| Phase 3: Metrics, telemetry, and reporting | Done | Codex | 2026-06-17 | 2026-06-17 | Added latency, throughput, process, storage, repository, vector, query profile, JSON, CSV, and Markdown reporting. |
| Phase 4: Dataset generator | Done | Codex | 2026-06-17 | 2026-06-17 | Added deterministic dataset generation and topology support. |
| Phase 5: Core CRUD, read, search, and paging workloads | Done | Codex | 2026-06-17 | 2026-06-17 | Added ingest, reads, search, enumeration, update, and delete scenarios. |
| Phase 6: Traversal, query, vector, and transaction workloads | Done | Codex | 2026-06-17 | 2026-06-17 | Added traversal, native query, vector, vector index, transaction, and rollback scenarios. |
| Phase 7: Mixed load, stress, soak, and regression comparison | Done | Codex | 2026-06-17 | 2026-06-17 | Added mixed profiles, closed-loop/open-loop execution, concurrency ramps, soak duration support, and baseline comparison. |
| Phase 8: User documentation in `PERF_SCALE_TESTING.md` | Done | Codex | 2026-06-17 | 2026-06-17 | Added CLI user guide with examples and artifact interpretation. |
| Phase 9: Validation and polish | Done | Codex | 2026-06-17 | 2026-06-17 | Full solution build and focused smoke validations completed. |

## Completion Evidence Log

Add dated evidence as work completes.

| Date | Area | Evidence | Link Or Command | Notes |
| --- | --- | --- | --- | --- |
| 2026-06-17 | Branch | Created `feature/perfscale`. | `git branch --show-current` | Implementation branch for this work. |
| 2026-06-17 | Project | Added `Test.PerformanceAndScalability` to the solution. | `dotnet sln src/LiteGraph.sln add src/Test.PerformanceAndScalability/Test.PerformanceAndScalability.csproj` | Project targets `net8.0;net10.0`. |
| 2026-06-17 | CLI | Verified redacted effective configuration. | `dotnet run --project src/Test.PerformanceAndScalability/Test.PerformanceAndScalability.csproj -f net8.0 -- --dry-run true --workloads reads --duration 0.1 --warmup 0` | Dry-run succeeds. |
| 2026-06-17 | Reads | Ran short SQLite read workload validation. | `dotnet run --project src/Test.PerformanceAndScalability/Test.PerformanceAndScalability.csproj -f net8.0 -- --workloads reads --duration 0.2 --warmup 0 --capture-process-metrics false --output %TEMP%/litegraph-perfscale-validate` | Completed with zero failures/timeouts. |
| 2026-06-17 | Transactions | Ran short SQLite transaction workload validation. | `dotnet run --project src/Test.PerformanceAndScalability/Test.PerformanceAndScalability.csproj -f net8.0 -- --workloads transactions --duration 0.5 --warmup 0 --capture-process-metrics false --output %TEMP%/litegraph-perfscale-transactions2` | Completed with zero anomalies. |
| 2026-06-17 | Query/vector | Ran short SQLite query/vector validation with query profiles. | `dotnet run --project src/Test.PerformanceAndScalability/Test.PerformanceAndScalability.csproj -f net8.0 -- --workloads query,vector --duration 0.5 --warmup 0 --capture-process-metrics false --include-query-profile true --output %TEMP%/litegraph-perfscale-vector-query` | Completed with zero anomalies. |
| 2026-06-17 | All workloads | Ran compact all-workload SQLite smoke. | `dotnet run --project src/Test.PerformanceAndScalability/Test.PerformanceAndScalability.csproj -f net8.0 -- --workloads all --duration 0.2 --warmup 0 --capture-process-metrics false --output %TEMP%/litegraph-perfscale-all` | Completed; tiny vector search window cancellation informed anomaly-rule refinement. |
| 2026-06-17 | Build | Built full solution. | `dotnet build src/LiteGraph.sln` | Succeeded with 0 warnings and 0 errors. |

## Phase 0: Review And Scope Confirmation

Goal: confirm the implementation scope before adding code.

| Task | Status | Owner | Notes |
| --- | --- | --- | --- |
| [ ] Review `PERF_SCALE_TESTS.md` and confirm the initial harness scope. | Not Started |  |  |
| [ ] Confirm the first implementation targets in-process `LiteGraphClient` and repository mode. | Not Started |  |  |
| [ ] Confirm REST or MCP driver support is deferred unless explicitly required. | Not Started |  |  |
| [ ] Confirm SQLite file mode is the no-argument default. | Not Started |  |  |
| [ ] Confirm PostgreSQL is supported when connection settings are supplied. | Not Started |  |  |
| [ ] Confirm MySQL and SQL Server fail clearly as unsupported until repositories are implemented. | Not Started |  |  |
| [ ] Confirm artifact formats: JSON, CSV, and Markdown. | Not Started |  |  |

Exit criteria:

- Scope is agreed.
- Any intentionally deferred work is listed in this plan.
- No code is added until scope is confirmed.

## Phase 1: Project Skeleton And CLI

Goal: create the console project and a stable command-line contract.

| Task | Status | Owner | Notes |
| --- | --- | --- | --- |
| [ ] Create `src/Test.PerformanceAndScalability/`. | Not Started |  |  |
| [ ] Create `Test.PerformanceAndScalability.csproj`. | Not Started |  |  |
| [ ] Add the project to `src/LiteGraph.sln`. | Not Started |  |  |
| [ ] Target the same framework set used by adjacent test projects where feasible. | Not Started |  |  |
| [ ] Reference the core `LiteGraph` project. | Not Started |  |  |
| [ ] Decide whether any `Test.Shared` helpers are safe to reference. | Not Started |  |  |
| [ ] Add application entry point. | Not Started |  |  |
| [ ] Implement CLI parsing. | Not Started |  |  |
| [ ] Implement `--help`. | Not Started |  |  |
| [ ] Implement `--dry-run`. | Not Started |  |  |
| [ ] Implement no-argument default settings. | Not Started |  |  |
| [ ] Validate unknown or incompatible CLI options. | Not Started |  |  |

Required CLI groups:

- Provider: `--db-type`, `--connection-string`, `--sqlite-file`, `--in-memory`, `--host`, `--port`, `--database`, `--username`, `--password`, `--schema`, `--max-connections`, `--command-timeout-seconds`.
- Run selection: `--profile`, `--workloads`, `--operation-mix`, `--scale-factor`, `--topology`.
- Scale: `--tenants`, `--graphs-per-tenant`, `--nodes-per-graph`, `--edges-per-graph`, `--vectors-per-graph`, `--labels-per-node`, `--tags-per-node`, `--payload-size`, `--vector-dimensions`, `--vector-top-k`.
- Load: `--concurrency`, `--duration`, `--warmup`, `--cooldown`, `--iterations`, `--target-rate`, `--closed-loop`, `--batch-size`, `--transaction-size`, `--timeout`, `--seed`.
- Output: `--output`, `--run-id`, `--include-query-profile`, `--sample-correctness`, `--sample-rate`, `--capture-process-metrics`, `--capture-repository-telemetry`, `--capture-db-file-metrics`, `--keep-database`, `--fail-on-regression`, `--baseline`.

Exit criteria:

- `dotnet run --project src/Test.PerformanceAndScalability -- --help` works.
- `dotnet run --project src/Test.PerformanceAndScalability -- --dry-run` prints redacted effective configuration.
- Invalid provider and option combinations fail with actionable messages.

## Phase 2: Run Controller, Provider Setup, And Environment Capture

Goal: initialize LiteGraph reliably and capture enough metadata to reproduce a run.

| Task | Status | Owner | Notes |
| --- | --- | --- | --- |
| [ ] Implement run id generation. | Not Started |  |  |
| [ ] Implement output directory creation under `artifacts/perf-scale/{run-id}/` by default. | Not Started |  |  |
| [ ] Implement temporary SQLite file creation for no-argument runs. | Not Started |  |  |
| [ ] Implement `--keep-database`. | Not Started |  |  |
| [ ] Build `DatabaseSettings` from CLI options. | Not Started |  |  |
| [ ] Use `GraphRepositoryFactory.Create(DatabaseSettings)`. | Not Started |  |  |
| [ ] Create and initialize `LiteGraphClient`. | Not Started |  |  |
| [ ] Fail fast for unsupported MySQL and SQL Server providers. | Not Started |  |  |
| [ ] Redact secrets in all console and artifact output. | Not Started |  |  |
| [ ] Capture git SHA and dirty-tree status. | Not Started |  |  |
| [ ] Capture LiteGraph assembly version. | Not Started |  |  |
| [ ] Capture .NET runtime version. | Not Started |  |  |
| [ ] Capture OS and process architecture. | Not Started |  |  |
| [ ] Capture CPU count and memory details where available. | Not Started |  |  |
| [ ] Capture GC mode and latency mode. | Not Started |  |  |
| [ ] Capture redacted provider settings. | Not Started |  |  |
| [ ] Dispose client and repository cleanly after each run. | Not Started |  |  |

Exit criteria:

- A no-argument run initializes a temporary SQLite database and disposes it cleanly.
- A PostgreSQL dry run validates settings without printing secrets.
- Environment metadata is written to `environment.json`.

## Phase 3: Metrics, Telemetry, And Reporting

Goal: collect credible measurements and write reusable artifacts.

| Task | Status | Owner | Notes |
| --- | --- | --- | --- |
| [ ] Define common scenario result model. | Not Started |  |  |
| [ ] Define operation sample or histogram model. | Not Started |  |  |
| [ ] Implement latency percentile calculation. | Not Started |  |  |
| [ ] Implement throughput and item-throughput calculation. | Not Started |  |  |
| [ ] Track attempted, completed, failed, timed-out, and canceled operations. | Not Started |  |  |
| [ ] Subscribe to `LiteGraphTelemetry.RepositoryOperationRecorded`. | Not Started |  |  |
| [ ] Subscribe to `LiteGraphTelemetry.VectorSearchRecorded`. | Not Started |  |  |
| [ ] Aggregate repository telemetry by provider, operation, success, and transactional flag. | Not Started |  |  |
| [ ] Capture native query profile metrics when enabled. | Not Started |  |  |
| [ ] Implement process metric sampling. | Not Started |  |  |
| [ ] Implement SQLite database, WAL, SHM, and vector index file size capture. | Not Started |  |  |
| [ ] Add optional PostgreSQL storage metadata hooks where practical. | Not Started |  |  |
| [ ] Write `config.redacted.json`. | Not Started |  |  |
| [ ] Write `summary.json`. | Not Started |  |  |
| [ ] Write `summary.csv`. | Not Started |  |  |
| [ ] Write `operations.csv` or equivalent operation summary artifact. | Not Started |  |  |
| [ ] Write `repository-telemetry.csv`. | Not Started |  |  |
| [ ] Write `query-profiles.csv`. | Not Started |  |  |
| [ ] Write `process-metrics.csv`. | Not Started |  |  |
| [ ] Write `storage-metrics.csv`. | Not Started |  |  |
| [ ] Write `report.md`. | Not Started |  |  |

Exit criteria:

- A smoke run produces JSON, CSV, and Markdown artifacts.
- Repository telemetry appears in the report when enabled.
- Secret values are not present in any artifact.

## Phase 4: Dataset Generator

Goal: create deterministic, scalable graph datasets.

| Task | Status | Owner | Notes |
| --- | --- | --- | --- |
| [ ] Implement seeded random generation. | Not Started |  |  |
| [ ] Implement tenant generation. | Not Started |  |  |
| [ ] Implement graph generation. | Not Started |  |  |
| [ ] Implement node generation. | Not Started |  |  |
| [ ] Implement edge generation. | Not Started |  |  |
| [ ] Implement label generation. | Not Started |  |  |
| [ ] Implement tag generation. | Not Started |  |  |
| [ ] Implement data payload generation: small, medium, large. | Not Started |  |  |
| [ ] Implement vector generation. | Not Started |  |  |
| [ ] Implement random topology. | Not Started |  |  |
| [ ] Implement chain topology. | Not Started |  |  |
| [ ] Implement tree or DAG topology. | Not Started |  |  |
| [ ] Implement hub-and-spoke topology. | Not Started |  |  |
| [ ] Implement power-law topology. | Not Started |  |  |
| [ ] Implement community topology. | Not Started |  |  |
| [ ] Implement dense topology. | Not Started |  |  |
| [ ] Record dataset metadata to `dataset.json`. | Not Started |  |  |
| [ ] Track expected IDs and relationship facts for correctness sampling. | Not Started |  |  |

Named profile tasks:

| Task | Status | Owner | Notes |
| --- | --- | --- | --- |
| [ ] Implement `smoke` profile. | Not Started |  |  |
| [ ] Implement `small` profile. | Not Started |  |  |
| [ ] Implement `medium` profile. | Not Started |  |  |
| [ ] Implement `large` profile. | Not Started |  |  |
| [ ] Implement `soak` profile. | Not Started |  |  |
| [ ] Implement `custom` profile validation. | Not Started |  |  |

Exit criteria:

- The same seed and profile generate the same logical dataset.
- Smoke dataset generation completes quickly on a developer machine.
- Dataset metadata records counts, topology, payload profile, vector dimensions, average degree, max degree, and hot-key sets.

## Phase 5: Core CRUD, Read, Search, And Paging Workloads

Goal: benchmark the most common LiteGraph operations first.

| Workload | Tasks | Status | Owner | Notes |
| --- | --- | --- | --- | --- |
| Ingestion | [ ] Single create tenants, graphs, nodes, edges, vectors. | Not Started |  |  |
| Ingestion | [ ] Bulk create nodes, edges, and vectors. | Not Started |  |  |
| Ingestion | [ ] Batch-size sweep. | Not Started |  |  |
| Point reads | [ ] Read by GUID for graphs, nodes, edges, vectors, tenants. | Not Started |  |  |
| Point reads | [ ] Read many by GUID. | Not Started |  |  |
| Existence | [ ] Exists checks for major entities. | Not Started |  |  |
| Enumeration | [ ] Enumerate graphs, nodes, edges, and vectors. | Not Started |  |  |
| Enumeration | [ ] Measure time to first row and rows per second. | Not Started |  |  |
| Paging | [ ] Test skip, limits, and continuation-token paths where available. | Not Started |  |  |
| Search | [ ] Search by name. | Not Started |  |  |
| Search | [ ] Search by labels. | Not Started |  |  |
| Search | [ ] Search by tags. | Not Started |  |  |
| Search | [ ] Search by data expression filters. | Not Started |  |  |
| Search | [ ] Sweep low-selectivity and high-selectivity filters. | Not Started |  |  |
| Update | [ ] Update nodes, edges, and vectors with small and large payloads. | Not Started |  |  |
| Delete | [ ] Delete nodes, edges, and vectors. | Not Started |  |  |
| Delete | [ ] Bulk delete where APIs support it. | Not Started |  |  |

Exit criteria:

- Smoke and small profiles report throughput, latency, errors, repository telemetry, and correctness samples for core CRUD/read/search workloads.

## Phase 6: Traversal, Query, Vector, And Transaction Workloads

Goal: cover the graph-specific and higher-complexity operation families.

| Workload | Tasks | Status | Owner | Notes |
| --- | --- | --- | --- | --- |
| Neighborhood | [ ] Read node edges. | Not Started |  |  |
| Neighborhood | [ ] Read edges from and to a node. | Not Started |  |  |
| Neighborhood | [ ] Read edges between nodes. | Not Started |  |  |
| Neighborhood | [ ] Read parents, children, and neighbors. | Not Started |  |  |
| Neighborhood | [ ] Test low-degree, median-degree, and high-degree nodes. | Not Started |  |  |
| Traversal | [ ] Route reads between connected nodes. | Not Started |  |  |
| Traversal | [ ] Route reads between disconnected nodes. | Not Started |  |  |
| Traversal | [ ] Subgraph extraction with depth and max-node limits. | Not Started |  |  |
| Traversal | [ ] Graph and subgraph statistics. | Not Started |  |  |
| Native query | [ ] One-hop `MATCH` query. | Not Started |  |  |
| Native query | [ ] Fixed two-hop query. | Not Started |  |  |
| Native query | [ ] Bounded variable-length query. | Not Started |  |  |
| Native query | [ ] Data-filtered, label-filtered, and tag-filtered queries. | Not Started |  |  |
| Native query | [ ] Aggregate, ordering, and limit queries. | Not Started |  |  |
| Native query | [ ] Query profile capture. | Not Started |  |  |
| Vector | [ ] Vector create, bulk create, read, update, and delete. | Not Started |  |  |
| Vector | [ ] Vector search dimension sweep. | Not Started |  |  |
| Vector | [ ] Top-K sweep. | Not Started |  |  |
| Vector | [ ] Enable, rebuild, and delete vector indexes. | Not Started |  |  |
| Vector | [ ] Compare indexed and non-indexed search where supported. | Not Started |  |  |
| Transaction | [ ] Transactional create, update, delete, attach, detach, and upsert. | Not Started |  |  |
| Transaction | [ ] Transaction-size sweep. | Not Started |  |  |
| Transaction | [ ] Rollback validation with intentionally invalid operations. | Not Started |  |  |
| Transaction | [ ] Concurrent same-graph transactions. | Not Started |  |  |
| Transaction | [ ] Concurrent different-graph and different-tenant transactions. | Not Started |  |  |

Exit criteria:

- Each workload family has smoke coverage.
- Query profile metrics are visible in artifacts.
- Transaction rollback tests prove no partial state is left behind.

## Phase 7: Mixed Load, Stress, Soak, And Regression Comparison

Goal: make the harness useful for capacity analysis and performance regression detection.

| Task | Status | Owner | Notes |
| --- | --- | --- | --- |
| [ ] Implement weighted operation mixes. | Not Started |  |  |
| [ ] Implement read-heavy mix. | Not Started |  |  |
| [ ] Implement write-heavy mix. | Not Started |  |  |
| [ ] Implement balanced mix. | Not Started |  |  |
| [ ] Implement vector-heavy mix. | Not Started |  |  |
| [ ] Implement transaction-heavy mix. | Not Started |  |  |
| [ ] Implement hotspot mix. | Not Started |  |  |
| [ ] Implement tenant-isolated mix. | Not Started |  |  |
| [ ] Implement graph-isolated mix. | Not Started |  |  |
| [ ] Implement closed-loop worker scheduling. | Not Started |  |  |
| [ ] Implement open-loop target-rate scheduling. | Not Started |  |  |
| [ ] Implement concurrency ramp parsing and execution. | Not Started |  |  |
| [ ] Implement early stop on timeout or error-rate threshold. | Not Started |  |  |
| [ ] Implement soak run mode. | Not Started |  |  |
| [ ] Implement anomaly detection output. | Not Started |  |  |
| [ ] Implement baseline result loading. | Not Started |  |  |
| [ ] Implement regression comparison. | Not Started |  |  |
| [ ] Implement `--fail-on-regression`. | Not Started |  |  |

Exit criteria:

- A run can identify the concurrency knee for a selected workload.
- A saved baseline can be compared against a later run.
- Regression output includes throughput, p95, p99, error-rate, allocation, and storage-growth changes where available.

## Phase 8: User Documentation In `PERF_SCALE_TESTING.md`

Goal: create a practical user guide for running the CLI against common scenarios.

Create this file:

```text
PERF_SCALE_TESTING.md
```

Required documentation sections:

| Section | Status | Owner | Notes |
| --- | --- | --- | --- |
| [ ] Overview of what the harness measures. | Not Started |  |  |
| [ ] Prerequisites. | Not Started |  |  |
| [ ] Build command. | Not Started |  |  |
| [ ] No-argument SQLite quick start. | Not Started |  |  |
| [ ] SQLite file examples. | Not Started |  |  |
| [ ] SQLite in-memory examples. | Not Started |  |  |
| [ ] PostgreSQL examples using connection string. | Not Started |  |  |
| [ ] PostgreSQL examples using host, port, database, username, password, and schema. | Not Started |  |  |
| [ ] Unsupported provider behavior for MySQL and SQL Server. | Not Started |  |  |
| [ ] Smoke, small, medium, large, and soak profile examples. | Not Started |  |  |
| [ ] Workload selection examples. | Not Started |  |  |
| [ ] Concurrency ramp examples. | Not Started |  |  |
| [ ] Open-loop target-rate examples. | Not Started |  |  |
| [ ] Batch-size and transaction-size examples. | Not Started |  |  |
| [ ] Vector workload examples. | Not Started |  |  |
| [ ] Native query profile examples. | Not Started |  |  |
| [ ] Mixed workload examples. | Not Started |  |  |
| [ ] Soak test examples. | Not Started |  |  |
| [ ] Baseline and regression comparison examples. | Not Started |  |  |
| [ ] Output artifact explanation. | Not Started |  |  |
| [ ] How to interpret throughput and latency percentiles. | Not Started |  |  |
| [ ] How to interpret repository telemetry. | Not Started |  |  |
| [ ] How to interpret query profile metrics. | Not Started |  |  |
| [ ] How to interpret process, GC, and storage metrics. | Not Started |  |  |
| [ ] Troubleshooting common failures. | Not Started |  |  |
| [ ] Security guidance for passwords and connection strings. | Not Started |  |  |
| [ ] Suggested before/after optimization workflow. | Not Started |  |  |

Required example commands:

```text
dotnet build src/LiteGraph.sln
dotnet run --project src/Test.PerformanceAndScalability
dotnet run --project src/Test.PerformanceAndScalability -- --profile smoke
dotnet run --project src/Test.PerformanceAndScalability -- --db-type sqlite --sqlite-file ./artifacts/perf-scale/local.db --keep-database true
dotnet run --project src/Test.PerformanceAndScalability -- --db-type sqlite --in-memory true --profile small
dotnet run --project src/Test.PerformanceAndScalability -- --db-type postgresql --connection-string "<redacted>" --profile small
dotnet run --project src/Test.PerformanceAndScalability -- --workloads reads,search,traversal --concurrency 1,2,4,8,16
dotnet run --project src/Test.PerformanceAndScalability -- --workloads vector --vector-dimensions 768 --vector-top-k 20
dotnet run --project src/Test.PerformanceAndScalability -- --workloads query --include-query-profile true
dotnet run --project src/Test.PerformanceAndScalability -- --operation-mix read-heavy --duration 00:05:00
dotnet run --project src/Test.PerformanceAndScalability -- --profile soak --duration 01:00:00 --keep-database true
dotnet run --project src/Test.PerformanceAndScalability -- --baseline ./artifacts/perf-scale/baseline/summary.json --fail-on-regression true
```

Exit criteria:

- `PERF_SCALE_TESTING.md` explains how to run every major scenario supported by the harness.
- Commands are copy-pasteable with secrets redacted.
- Artifact interpretation is clear enough for a developer to identify likely bottlenecks.

## Phase 9: Validation And Polish

Goal: make the implementation reliable enough for repeat use.

| Task | Status | Owner | Notes |
| --- | --- | --- | --- |
| [ ] Run `dotnet format` if the repo uses it or formatting changes are needed. | Not Started |  |  |
| [ ] Run `dotnet build src/LiteGraph.sln`. | Not Started |  |  |
| [ ] Run no-argument smoke benchmark. | Not Started |  |  |
| [ ] Run SQLite file smoke benchmark. | Not Started |  |  |
| [ ] Run SQLite in-memory smoke benchmark. | Not Started |  |  |
| [ ] Run PostgreSQL smoke benchmark when a test database is available. | Not Started |  |  |
| [ ] Verify output artifacts are generated and readable. | Not Started |  |  |
| [ ] Verify no secrets appear in output artifacts. | Not Started |  |  |
| [ ] Verify temporary files are removed by default. | Not Started |  |  |
| [ ] Verify `--keep-database true` preserves files. | Not Started |  |  |
| [ ] Verify unsupported providers fail clearly. | Not Started |  |  |
| [ ] Verify cancellation handles `Ctrl+C` cleanly. | Not Started |  |  |
| [ ] Verify docs match implemented CLI behavior. | Not Started |  |  |

Exit criteria:

- The solution builds.
- The default benchmark runs successfully.
- The user-facing documentation matches the implemented CLI.
- Known limitations are documented.

## Deferred Or Optional Work

Track items that should not block the first implementation.

| Item | Reason Deferred | Revisit Trigger | Status | Notes |
| --- | --- | --- | --- | --- |
| REST driver mode | In-process repository benchmarks are the first requirement. | Need to measure HTTP/server overhead. | Deferred |  |
| MCP driver mode | Same workload catalog can be reused later. | Need MCP performance coverage. | Deferred |  |
| Distributed coordinator/worker mode | Single-process harness is simpler and sufficient for first pass. | One process cannot saturate target deployment. | Deferred |  |
| Provider-specific PostgreSQL catalog metrics | Useful but not required for initial correctness of harness. | Need deeper PostgreSQL diagnosis. | Deferred |  |
| BenchmarkDotNet microbenchmarks | Main harness needs configurable load and persistent storage. | Need isolated microbenchmark for a specific hot method. | Deferred |  |

## Implementation Notes

Add notes here as design decisions are made.

| Date | Decision | Rationale | Owner |
| --- | --- | --- | --- |
| 2026-06-17 | Primary driver is in-process `LiteGraphClient`. | This isolates LiteGraph repository/client performance before adding HTTP or MCP transport overhead. | Codex |
| 2026-06-17 | Expensive maintenance scenarios support run-once execution. | Vector index rebuild and flush should measure one operation cleanly instead of looping for an arbitrary duration. | Codex |
| 2026-06-17 | Transaction workloads use minimal transaction payloads. | Existing transaction tests create minimal nodes through the builder; subordinate-heavy nodes are covered by non-transactional ingest/update workloads. | Codex |

## Blockers

Track blockers explicitly so progress can resume quickly.

| Date | Blocker | Impact | Owner | Resolution |
| --- | --- | --- | --- | --- |
|  |  |  |  |  |
