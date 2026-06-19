# LiteGraph Performance And Scalability Harness Implementation Plan

This plan tracks implementation of the `Test.PerformanceAndScalability` console harness described in `PERF_SCALE_TESTS.md`.

Audit date: 2026-06-19

Audit basis: current repository source, `PERF_SCALE_TESTING.md`, and the evidence log below. This audit annotates what is implemented, what is partially implemented, and what remains incomplete. Runtime-only local files such as Docker logs are not considered part of this plan.

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
- `[~]` in progress or partially implemented
- `[x]` complete
- `[!]` blocked

## Progress Summary

| Area | Status | Owner | Started | Completed | Notes |
| --- | --- | --- | --- | --- | --- |
| Phase 0: Review and scope confirmation | Done | Codex | 2026-06-17 | 2026-06-17 | Scope was confirmed for an in-process `LiteGraphClient` harness with REST/MCP/distributed modes deferred. |
| Phase 1: Project skeleton and CLI | Done | Codex | 2026-06-17 | 2026-06-17 | Project exists, is in the solution, targets `net8.0;net10.0`, and exposes the planned CLI options. |
| Phase 2: Run controller, provider setup, and environment capture | Done | Codex | 2026-06-17 | 2026-06-17 | SQLite default, PostgreSQL settings, fail-fast unsupported providers, redaction, run id, output directories, environment capture, and cleanup are implemented. |
| Phase 3: Metrics, telemetry, and reporting | In Progress | Codex | 2026-06-17 |  | Core metrics and artifacts are implemented; PostgreSQL catalog/storage metrics and full secret-scan validation remain incomplete. |
| Phase 4: Dataset generator | In Progress | Codex | 2026-06-17 |  | Deterministic scalable data and topologies are implemented; exhaustive correctness/coherency fact modeling remains incomplete. |
| Phase 5: Core CRUD, read, search, and paging workloads | In Progress | Codex | 2026-06-17 |  | Common node/edge/vector workloads exist; tenant/graph measured workloads, vector delete, automatic sweeps, and data-filter/search selectivity coverage remain incomplete. |
| Phase 6: Traversal, query, vector, and transaction workloads | In Progress | Codex | 2026-06-17 |  | Traversal, route fixture, query, vector search/update/index rebuild, and several transaction scenarios exist; several exhaustive query/vector/transaction cases remain incomplete. |
| Phase 7: Mixed load, stress, soak, and regression comparison | In Progress | Codex | 2026-06-17 |  | Mixed profiles, closed/open-loop load, concurrency ramps, soak profile, anomalies, baseline comparison, and `--fail-on-regression` exist; tenant/graph isolated mixes and early-stop thresholds remain incomplete. |
| Phase 8: User documentation in `PERF_SCALE_TESTING.md` | Review | Codex | 2026-06-17 | 2026-06-17 | User guide exists and is broad; it needs a pass to align documented operation mixes and examples with still-incomplete workload coverage. |
| Phase 9: Validation and polish | In Progress | Codex | 2026-06-17 |  | Solution build and smoke validations were recorded; full provider matrix validation, in-memory validation, current artifact secret scan, and cancellation verification remain incomplete. |

## Completion Evidence Log

| Date | Area | Evidence | Link Or Command | Notes |
| --- | --- | --- | --- | --- |
| 2026-06-17 | Branch | Created `feature/perfscale`. | `git branch --show-current` | Implementation branch for initial work. Current active branch is `v7.0`. |
| 2026-06-17 | Project | Added `Test.PerformanceAndScalability` to the solution. | `dotnet sln src/LiteGraph.sln add src/Test.PerformanceAndScalability/Test.PerformanceAndScalability.csproj` | Project targets `net8.0;net10.0`. |
| 2026-06-17 | CLI | Verified redacted effective configuration. | `dotnet run --project src/Test.PerformanceAndScalability/Test.PerformanceAndScalability.csproj -f net8.0 -- --dry-run true --workloads reads --duration 0.1 --warmup 0` | Dry-run succeeds. |
| 2026-06-17 | Reads | Ran short SQLite read workload validation. | `dotnet run --project src/Test.PerformanceAndScalability/Test.PerformanceAndScalability.csproj -f net8.0 -- --workloads reads --duration 0.2 --warmup 0 --capture-process-metrics false --output %TEMP%/litegraph-perfscale-validate` | Completed with zero failures/timeouts. |
| 2026-06-17 | Transactions | Ran short SQLite transaction workload validation. | `dotnet run --project src/Test.PerformanceAndScalability/Test.PerformanceAndScalability.csproj -f net8.0 -- --workloads transactions --duration 0.5 --warmup 0 --capture-process-metrics false --output %TEMP%/litegraph-perfscale-transactions2` | Completed with zero harness anomalies; this is performance harness smoke validation, not correctness acceptance. |
| 2026-06-17 | Query/vector | Ran short SQLite query/vector validation with query profiles. | `dotnet run --project src/Test.PerformanceAndScalability/Test.PerformanceAndScalability.csproj -f net8.0 -- --workloads query,vector --duration 0.5 --warmup 0 --capture-process-metrics false --include-query-profile true --output %TEMP%/litegraph-perfscale-vector-query` | Completed with zero anomalies. |
| 2026-06-17 | All workloads | Ran compact all-workload SQLite smoke. | `dotnet run --project src/Test.PerformanceAndScalability/Test.PerformanceAndScalability.csproj -f net8.0 -- --workloads all --duration 0.2 --warmup 0 --capture-process-metrics false --output %TEMP%/litegraph-perfscale-all` | Completed; tiny vector search window cancellation informed anomaly-rule refinement. |
| 2026-06-17 | Build | Built full solution. | `dotnet build src/LiteGraph.sln` | Succeeded with 0 warnings and 0 errors. |
| 2026-06-18 | Transactions | Added and validated read-your-writes and mixed child-object transaction scenarios. | `dotnet run --project src/Test.PerformanceAndScalability/Test.PerformanceAndScalability.csproj -f net10.0 -- --workloads transactions --duration 0.2 --warmup 0 --capture-process-metrics false --output ./artifacts/perf-scale/validate-transactions-v70 --run-id validate-transactions-v70` | Completed four performance smoke scenarios with zero failures, timeouts, or sampled correctness anomalies; validation artifacts were removed. This does not satisfy v7 correctness/coherency acceptance. |
| 2026-06-18 | Transactions | Added and validated transaction isolation CLI selection. | `dotnet run --project src/Test.PerformanceAndScalability/Test.PerformanceAndScalability.csproj -f net10.0 -- --workloads transactions --duration 0.2 --warmup 0 --capture-process-metrics false --transaction-isolation serializable --output ./artifacts/perf-scale/validate-transactions-isolation-v70 --run-id validate-transactions-isolation-v70` | Completed four performance smoke scenarios with zero failures, timeouts, or sampled correctness anomalies; validation artifacts were removed. This does not satisfy v7 correctness/coherency acceptance. |
| 2026-06-18 | Transactions | Added and validated transaction-specific artifact metrics. | `dotnet run --project src/Test.PerformanceAndScalability/Test.PerformanceAndScalability.csproj -f net10.0 -- --workloads transactions --duration 0.2 --warmup 0 --capture-process-metrics false --transaction-isolation serializable --output ./artifacts/perf-scale/validate-transaction-metrics-v70 --run-id validate-transaction-metrics-v70` | `report.md` contained a transaction summary table and provider notes; `summary.csv` included `tx_*` lifecycle, contention, isolation, gate, and latency columns; validation artifacts were removed. |
| 2026-06-19 | Audit | Reconciled plan with implementation state. | `PERF_SCALE_TESTS_PLAN.md` | Detailed task status was updated because the prior summary said Done while most task rows still said Not Started. |

## Phase 0: Review And Scope Confirmation

Goal: confirm implementation scope before adding code.

| Task | Status | Owner | Notes |
| --- | --- | --- | --- |
| [x] Review `PERF_SCALE_TESTS.md` and confirm initial harness scope. | Done | Codex | Initial implementation followed the single-console-harness scope. |
| [x] Confirm first implementation targets in-process `LiteGraphClient` and repository mode. | Done | Codex | Implemented through `LiteGraphClient` and `GraphRepositoryFactory`. |
| [x] Confirm REST or MCP driver support is deferred unless explicitly required. | Done | Codex | Deferred below. |
| [x] Confirm SQLite file mode is the no-argument default. | Done | Codex | Default creates a temporary SQLite file. |
| [x] Confirm PostgreSQL is supported when connection settings are supplied. | Done | Codex | CLI builds PostgreSQL `DatabaseSettings`; runtime depends on supplied database. |
| [x] Confirm MySQL and SQL Server fail clearly as unsupported until repositories are implemented. | Done | Codex | CLI fail-fast validation is implemented. |
| [x] Confirm artifact formats: JSON, CSV, and Markdown. | Done | Codex | Implemented in `ArtifactWriter`. |

## Phase 1: Project Skeleton And CLI

Goal: create the console project and a stable command-line contract.

| Task | Status | Owner | Notes |
| --- | --- | --- | --- |
| [x] Create `src/Test.PerformanceAndScalability/`. | Done | Codex | Project folder exists. |
| [x] Create `Test.PerformanceAndScalability.csproj`. | Done | Codex | Project exists and targets `net8.0;net10.0`. |
| [x] Add the project to `src/LiteGraph.sln`. | Done | Codex | Solution includes the project. |
| [x] Target the same framework set used by adjacent test projects where feasible. | Done | Codex | Uses multi-targeting for `net8.0;net10.0`. |
| [x] Reference the core `LiteGraph` project. | Done | Codex | Project reference is present. |
| [x] Decide whether any `Test.Shared` helpers are safe to reference. | Done | Codex | Not referenced; harness uses local helpers. |
| [x] Add application entry point. | Done | Codex | `Program.Main` implemented. |
| [x] Implement CLI parsing. | Done | Codex | `BenchmarkOptions.Parse` implemented. |
| [x] Implement `--help`. | Done | Codex | Help text is implemented. |
| [x] Implement `--dry-run`. | Done | Codex | Prints redacted effective configuration. |
| [x] Implement no-argument default settings. | Done | Codex | Smoke profile and temporary SQLite default are implemented. |
| [x] Validate unknown or incompatible CLI options. | Done | Codex | Unknown options and invalid combinations fail with messages. |

## Phase 2: Run Controller, Provider Setup, And Environment Capture

Goal: initialize LiteGraph reliably and capture enough metadata to reproduce a run.

| Task | Status | Owner | Notes |
| --- | --- | --- | --- |
| [x] Implement run id generation. | Done | Codex | `BenchmarkOptions.CreateRunId`. |
| [x] Implement output directory creation under `artifacts/perf-scale/{run-id}/` by default. | Done | Codex | `PrepareRun` creates the directory. |
| [x] Implement temporary SQLite file creation for no-argument runs. | Done | Codex | `BuildDatabaseSettings` creates temp file path. |
| [x] Implement `--keep-database`. | Done | Codex | Cleanup skips preserved DBs. |
| [x] Build `DatabaseSettings` from CLI options. | Done | Codex | SQLite and PostgreSQL settings are mapped. |
| [x] Use `GraphRepositoryFactory.Create(DatabaseSettings)`. | Done | Codex | Implemented in `Program.Main`. |
| [x] Create and initialize `LiteGraphClient`. | Done | Codex | Client is initialized before dataset generation. |
| [x] Fail fast for unsupported MySQL and SQL Server providers. | Done | Codex | Validation rejects placeholders. |
| [x] Redact secrets in all console and artifact output. | Done | Codex | Redaction is implemented; needs periodic artifact scan in Phase 9. |
| [x] Capture git SHA and dirty-tree status. | Done | Codex | `EnvironmentCapture`. |
| [x] Capture LiteGraph assembly version. | Done | Codex | `EnvironmentCapture`. |
| [x] Capture .NET runtime version. | Done | Codex | `EnvironmentCapture`. |
| [x] Capture OS and process architecture. | Done | Codex | `EnvironmentCapture`. |
| [~] Capture CPU count and memory details where available. | In Progress | Codex | CPU count is captured. Process memory is sampled; total system memory is not captured. |
| [x] Capture GC mode and latency mode. | Done | Codex | `EnvironmentCapture`. |
| [x] Capture redacted provider settings. | Done | Codex | `config.redacted.json`. |
| [x] Dispose client and repository cleanly after each run. | Done | Codex | `await using` and cleanup path implemented. |

## Phase 3: Metrics, Telemetry, And Reporting

Goal: collect credible measurements and write reusable artifacts.

| Task | Status | Owner | Notes |
| --- | --- | --- | --- |
| [x] Define common scenario result model. | Done | Codex | `ScenarioResult`. |
| [x] Define operation sample or histogram model. | Done | Codex | Latency lists are aggregated into percentiles; no external histogram library. |
| [x] Implement latency percentile calculation. | Done | Codex | `LatencyStats`. |
| [x] Implement throughput and item-throughput calculation. | Done | Codex | `ScenarioAccumulator.ToResult`. |
| [x] Track attempted, completed, failed, timed-out, and canceled operations. | Done | Codex | Implemented in `LoadEngine`. |
| [x] Subscribe to `LiteGraphTelemetry.RepositoryOperationRecorded`. | Done | Codex | `TelemetryCollector`. |
| [x] Subscribe to `LiteGraphTelemetry.VectorSearchRecorded`. | Done | Codex | `TelemetryCollector`. |
| [x] Aggregate repository telemetry by provider, operation, success, and transactional flag. | Done | Codex | `RepositoryAggregate`. |
| [x] Capture native query profile metrics when enabled. | Done | Codex | Query scenarios call `RecordQueryProfile`. |
| [x] Implement process metric sampling. | Done | Codex | `ProcessMetricsCollector`. |
| [x] Implement SQLite database, WAL, SHM, and vector index file size capture. | Done | Codex | `StorageMetrics.Capture` handles SQLite and index directories. |
| [~] Add optional PostgreSQL storage metadata hooks where practical. | In Progress | Codex | Placeholder provider field exists, but no PostgreSQL catalog/table/index size queries are implemented. |
| [x] Write `config.redacted.json`. | Done | Codex | Implemented. |
| [x] Write `environment.json`. | Done | Codex | Implemented. |
| [x] Write `dataset.json`. | Done | Codex | Implemented. |
| [x] Write `summary.json`. | Done | Codex | Implemented. |
| [x] Write `summary.csv`. | Done | Codex | Implemented. |
| [x] Write `operations.csv` or equivalent operation summary artifact. | Done | Codex | Same scenario CSV schema is written to `operations.csv`. |
| [x] Write `repository-telemetry.csv`. | Done | Codex | Implemented. |
| [x] Write `vector-telemetry.csv`. | Done | Codex | Implemented. |
| [x] Write `query-profiles.csv`. | Done | Codex | Implemented. |
| [x] Write `process-metrics.csv`. | Done | Codex | Implemented. |
| [x] Write `storage-metrics.csv`. | Done | Codex | Implemented, with PostgreSQL gaps noted above. |
| [x] Write `report.md`. | Done | Codex | Implemented with scenario, transaction, provider, repository, and anomaly sections. |

## Phase 4: Dataset Generator

Goal: create deterministic, scalable graph datasets.

| Task | Status | Owner | Notes |
| --- | --- | --- | --- |
| [x] Implement seeded random generation. | Done | Codex | Seeded `Random` and deterministic GUIDs are used. |
| [x] Implement tenant generation. | Done | Codex | Dataset generator creates tenants. |
| [x] Implement graph generation. | Done | Codex | Dataset generator creates graphs. |
| [x] Implement node generation. | Done | Codex | Dataset generator creates nodes. |
| [x] Implement edge generation. | Done | Codex | Dataset generator creates edges. |
| [x] Implement label generation. | Done | Codex | Node/edge labels are generated. |
| [x] Implement tag generation. | Done | Codex | Node/edge/graph tags are generated. |
| [x] Implement data payload generation: small, medium, large. | Done | Codex | Payload sizes vary text/nested fields. |
| [x] Implement vector generation. | Done | Codex | Embeddings are generated and attached to nodes. |
| [x] Implement random topology. | Done | Codex | Default topology. |
| [x] Implement chain topology. | Done | Codex | Implemented. |
| [x] Implement tree or DAG topology. | Done | Codex | Tree topology implemented. |
| [x] Implement hub-and-spoke topology. | Done | Codex | `hub` topology implemented. |
| [x] Implement power-law topology. | Done | Codex | `power-law` and `powerlaw` supported. |
| [x] Implement community topology. | Done | Codex | Implemented as `communities`. |
| [x] Implement dense topology. | Done | Codex | Implemented. |
| [x] Record dataset metadata to `dataset.json`. | Done | Codex | Artifact writer persists metadata. |
| [~] Track expected IDs and relationship facts for correctness sampling. | In Progress | Codex | In-memory IDs, hot node GUIDs, and known route fixtures exist; exhaustive expected relationship facts for broad correctness/coherency validation do not. |
| [x] Implement `smoke`, `small`, `medium`, `large`, `soak`, and `custom` profile defaults. | Done | Codex | `ProfileDefaults.For`. |

## Phase 5: Core CRUD, Read, Search, And Paging Workloads

Goal: benchmark common LiteGraph operations first.

| Workload | Tasks | Status | Owner | Notes |
| --- | --- | --- | --- | --- |
| Ingestion | [~] Single create tenants, graphs, nodes, edges, vectors. | In Progress | Codex | Measured scenarios cover nodes, edges, vectors. Tenants/graphs are created during dataset setup but not measured as single-create workloads. |
| Ingestion | [~] Bulk create nodes, edges, and vectors. | In Progress | Codex | Measured bulk workload exists for nodes. Dataset setup bulk-creates edges/vectors, but they are not separate measured scenarios. |
| Ingestion | [~] Batch-size sweep. | In Progress | Codex | `--batch-size` exists, but automatic multi-size sweep scenarios are not implemented. |
| Point reads | [~] Read by GUID for graphs, nodes, edges, vectors, tenants. | In Progress | Codex | Nodes, edges, and vectors are covered. Graph and tenant point-read scenarios are missing. |
| Point reads | [~] Read many by GUID. | In Progress | Codex | Node batch read exists. Edge/vector/graph/tenant batch reads are missing. |
| Existence | [~] Exists checks for major entities. | In Progress | Codex | Node, graph, edge, vector exists checks are grouped. Tenant exists is missing. |
| Enumeration | [~] Enumerate graphs, nodes, edges, and vectors. | In Progress | Codex | Nodes and edges are covered. Graph and vector enumeration scenarios are missing. |
| Enumeration | [ ] Measure time to first row and rows per second. | Not Started |  | Current harness counts bounded rows; it does not separately record time-to-first-row. |
| Paging | [~] Test skip, limits, and continuation-token paths where available. | In Progress | Codex | Node/edge enumeration uses bounded skip/count. Dedicated paging and continuation-token coverage is missing. |
| Search | [x] Search by name. | Done | Codex | Node name search exists. |
| Search | [x] Search by labels. | Done | Codex | Node label search exists. |
| Search | [x] Search by tags. | Done | Codex | Node tag search exists. |
| Search | [ ] Search by data expression filters. | Not Started |  | No measured data-expression search scenario. |
| Search | [ ] Sweep low-selectivity and high-selectivity filters. | Not Started |  | No automatic selectivity sweep. |
| Update | [~] Update nodes, edges, and vectors with small and large payloads. | In Progress | Codex | Node, edge, and vector updates exist. Separate small/large payload update scenarios are not implemented. |
| Delete | [~] Delete nodes, edges, and vectors. | In Progress | Codex | Node and edge delete-with-setup scenarios exist. Vector delete is missing. |
| Delete | [ ] Bulk delete where APIs support it. | Not Started |  | No bulk delete scenarios. |

## Phase 6: Traversal, Query, Vector, And Transaction Workloads

Goal: cover graph-specific and higher-complexity operation families.

| Workload | Tasks | Status | Owner | Notes |
| --- | --- | --- | --- | --- |
| Neighborhood | [x] Read node edges. | Done | Codex | Covered through neighbors and outgoing edge reads. |
| Neighborhood | [~] Read edges from and to a node. | In Progress | Codex | Edges-from is covered. Edges-to is not a distinct measured scenario. |
| Neighborhood | [ ] Read edges between nodes. | Not Started |  | No explicit edges-between scenario. |
| Neighborhood | [x] Read parents, children, and neighbors. | Done | Codex | Implemented. |
| Neighborhood | [~] Test low-degree, median-degree, and high-degree nodes. | In Progress | Codex | Hot-or-random node selection exists. Explicit low/median/high degree buckets are missing. |
| Traversal | [x] Route reads between connected nodes. | Done | Codex | Known route fixture is preloaded and validated. |
| Traversal | [ ] Route reads between disconnected nodes. | Not Started |  | Missing explicit disconnected route scenario. |
| Traversal | [x] Subgraph extraction with depth and max-node limits. | Done | Codex | Implemented. |
| Traversal | [~] Graph and subgraph statistics. | In Progress | Codex | Graph statistics exists under maintenance; subgraph statistics are not distinct. |
| Native query | [x] One-hop `MATCH` query. | Done | Codex | `query.match.edge`. |
| Native query | [ ] Fixed two-hop query. | Not Started |  | Missing. |
| Native query | [ ] Bounded variable-length query. | Not Started |  | Missing. |
| Native query | [~] Data-filtered, label-filtered, and tag-filtered queries. | In Progress | Codex | Label/guid forms exist; data-filtered and tag-filtered query scenarios are missing. |
| Native query | [~] Aggregate, ordering, and limit queries. | In Progress | Codex | Aggregate and limits exist. Ordering is missing. |
| Native query | [x] Query profile capture. | Done | Codex | `--include-query-profile` and `query-profiles.csv`. |
| Vector | [~] Vector create, bulk create, read, update, and delete. | In Progress | Codex | Create/read/update/search exist. Bulk create is dataset-only, and delete is missing as a measured scenario. |
| Vector | [~] Vector search dimension sweep. | In Progress | Codex | `--vector-dimensions` exists. Automatic sweep orchestration is missing. |
| Vector | [~] Top-K sweep. | In Progress | Codex | `--vector-top-k` exists. Automatic sweep orchestration is missing. |
| Vector | [~] Enable, rebuild, and delete vector indexes. | In Progress | Codex | Enable/rebuild exists. Delete vector index scenario is missing. |
| Vector | [ ] Compare indexed and non-indexed search where supported. | Not Started |  | No paired indexed/non-indexed comparison scenario. |
| Transaction | [~] Transactional create, update, delete, attach, detach, and upsert. | In Progress | Codex | Create, create-update, child label/tag/vector attach, and rollback exist. Delete, detach, and upsert transaction scenarios are missing. |
| Transaction | [~] Transaction-size sweep. | In Progress | Codex | `--transaction-size` exists. Automatic sweep orchestration is missing. |
| Transaction | [x] Rollback validation with intentionally invalid operations. | Done | Codex | `transaction.rollback`. |
| Transaction | [~] Concurrent same-graph transactions. | In Progress | Codex | Concurrency can apply to transaction scenarios, but same-graph contention is not isolated as a named scenario. |
| Transaction | [~] Concurrent different-graph and different-tenant transactions. | In Progress | Codex | Dataset can contain multiple graphs/tenants, but dedicated graph/tenant-isolated transaction scenarios are missing. |

## Phase 7: Mixed Load, Stress, Soak, And Regression Comparison

Goal: make the harness useful for capacity analysis and performance regression detection.

| Task | Status | Owner | Notes |
| --- | --- | --- | --- |
| [x] Implement weighted operation mixes. | Done | Codex | `MixedAsync` uses weighted branches. |
| [x] Implement read-heavy mix. | Done | Codex | Implemented. |
| [x] Implement write-heavy mix. | Done | Codex | Implemented. |
| [x] Implement balanced mix. | Done | Codex | Default fallback branch. |
| [x] Implement vector-heavy mix. | Done | Codex | Implemented. |
| [x] Implement transaction-heavy mix. | Done | Codex | Implemented. |
| [x] Implement hotspot mix. | Done | Codex | Implemented. |
| [ ] Implement tenant-isolated mix. | Not Started |  | CLI/help mentions it, but `MixedAsync` has no explicit tenant-isolated behavior. |
| [ ] Implement graph-isolated mix. | Not Started |  | CLI/help mentions it, but `MixedAsync` has no explicit graph-isolated behavior. |
| [x] Implement closed-loop worker scheduling. | Done | Codex | Implemented. |
| [x] Implement open-loop target-rate scheduling. | Done | Codex | Implemented. |
| [x] Implement concurrency ramp parsing and execution. | Done | Codex | Supports comma list and range syntax. |
| [ ] Implement early stop on timeout or error-rate threshold. | Not Started |  | No configurable threshold-based early stop. |
| [x] Implement soak run mode. | Done | Codex | `soak` profile exists. |
| [x] Implement anomaly detection output. | Done | Codex | `anomalies.json` and report anomaly section. |
| [x] Implement baseline result loading. | Done | Codex | `RegressionAnalyzer.Compare`. |
| [x] Implement regression comparison. | Done | Codex | Throughput, p99, and failure comparison implemented. |
| [x] Implement `--fail-on-regression`. | Done | Codex | Exit code `3` when anomalies exist. |

## Phase 8: User Documentation In `PERF_SCALE_TESTING.md`

Goal: create a practical user guide for running the CLI against common scenarios.

| Section | Status | Owner | Notes |
| --- | --- | --- | --- |
| [x] Overview, prerequisites, build, and quick start. | Done | Codex | Present. |
| [x] SQLite file and in-memory examples. | Done | Codex | Present. |
| [x] PostgreSQL examples using connection string and properties. | Done | Codex | Present. |
| [x] Unsupported provider behavior for MySQL and SQL Server. | Done | Codex | Present. |
| [x] Smoke, small, medium, large, and soak profile examples. | Done | Codex | Present. |
| [x] Workload selection, concurrency ramp, open-loop, batch-size, and transaction-size examples. | Done | Codex | Present. |
| [x] Vector, native query profile, mixed workload, and soak examples. | Done | Codex | Present. |
| [x] Baseline and regression comparison examples. | Done | Codex | Present. |
| [x] Output artifact explanation and interpretation guidance. | Done | Codex | Present. |
| [x] Troubleshooting, security, and optimization workflow. | Done | Codex | Present. |
| [~] Ensure docs match implemented CLI behavior exactly. | Review | Codex | Docs mention some modes that are not fully implemented as distinct behaviors, especially tenant-isolated and graph-isolated mixes. |

## Phase 9: Validation And Polish

Goal: make the implementation reliable enough for repeat use.

| Task | Status | Owner | Notes |
| --- | --- | --- | --- |
| [ ] Run `dotnet format` if the repo uses it or formatting changes are needed. | Not Started |  | No evidence this was run for the harness. |
| [x] Run `dotnet build src/LiteGraph.sln`. | Done | Codex | Evidence logged for 2026-06-17. |
| [x] Run no-argument or default SQLite smoke benchmark. | Done | Codex | Compact SQLite all-workload smoke evidence is logged. |
| [x] Run SQLite file smoke benchmark. | Done | Codex | Default/temp SQLite file smoke evidence is logged. |
| [ ] Run SQLite in-memory smoke benchmark. | Not Started |  | No evidence found in this plan. |
| [~] Run PostgreSQL smoke benchmark when a test database is available. | In Progress | Codex | PostgreSQL runs were performed during later analysis, but this plan lacks specific evidence rows and artifact references. |
| [x] Verify output artifacts are generated and readable. | Done | Codex | Evidence includes generated `report.md` and CSV outputs. |
| [~] Verify no secrets appear in output artifacts. | In Progress | Codex | Redaction is implemented; no current explicit artifact secret-scan evidence is recorded. |
| [x] Verify temporary files are removed by default. | Done | Codex | Validation artifact cleanup is noted in evidence. |
| [ ] Verify `--keep-database true` preserves files. | Not Started |  | No explicit evidence recorded. |
| [~] Verify unsupported providers fail clearly. | In Progress | Codex | CLI validation exists; no explicit command evidence recorded in this plan. |
| [~] Verify cancellation handles `Ctrl+C` cleanly. | In Progress | Codex | Cancellation handling exists and was exercised informally, but no repeatable validation evidence is recorded. |
| [~] Verify docs match implemented CLI behavior. | Review | Codex | Needs pass after incomplete workload items are either implemented or documented as limitations. |

## Highest-Priority Remaining Work

| Priority | Status | Work Item | Why It Matters |
| --- | --- | --- | --- |
| High | In Progress | Add exhaustive correctness/coherency tests across SQLite and PostgreSQL outside the performance-smoke harness. | Current sampled correctness is not sufficient for release-grade provider coherency. |
| High | In Progress | Add missing transaction scenarios: delete, detach, upsert, same-graph contention, different-graph concurrency, and different-tenant concurrency. | Required to evaluate true parallel transaction scaling end to end. |
| High | In Progress | Add missing vector scenarios: bulk create as measured workload, delete, delete index, and indexed vs non-indexed paired search. | Needed for accurate vector performance and lifecycle coverage. |
| High | In Progress | Add missing search/query coverage: data-expression search, tag/data native queries, fixed two-hop, variable-length, ordering, and selectivity sweeps. | These are likely performance-sensitive paths. |
| Medium | In Progress | Add graph/tenant point reads and measured graph/tenant create workloads. | Completes CRUD breadth. |
| Medium | In Progress | Add explicit pagination and time-to-first-row metrics. | Helps diagnose streaming/enumeration behavior. |
| Medium | In Progress | Add PostgreSQL catalog/table/index size metrics. | Needed for PostgreSQL capacity analysis. |
| Medium | In Progress | Add tenant-isolated and graph-isolated mixed profiles or remove/document them as unsupported. | Current CLI/help imply support that is not behaviorally distinct. |
| Medium | In Progress | Add threshold-based early stop on error rate/timeouts. | Prevents long runs from continuing after obvious failure. |
| Medium | Review | Run and record full validation matrix: SQLite file, SQLite in-memory, PostgreSQL, unsupported providers, `--keep-database`, cancellation, secret scan. | Converts implemented code into repeatable release evidence. |

## Deferred Or Optional Work

| Item | Reason Deferred | Revisit Trigger | Status | Notes |
| --- | --- | --- | --- | --- |
| REST driver mode | In-process repository benchmarks were the first requirement. | Need to measure HTTP/server overhead. | Deferred |  |
| MCP driver mode | Same workload catalog can be reused later. | Need MCP performance coverage. | Deferred |  |
| Distributed coordinator/worker mode | Single-process harness is simpler and sufficient for first pass. | One process cannot saturate target deployment. | Deferred |  |
| BenchmarkDotNet microbenchmarks | Main harness needs configurable load and persistent storage. | Need isolated microbenchmark for a specific hot method. | Deferred |  |

## Implementation Notes

| Date | Decision | Rationale | Owner |
| --- | --- | --- | --- |
| 2026-06-17 | Primary driver is in-process `LiteGraphClient`. | This isolates LiteGraph repository/client performance before adding HTTP or MCP transport overhead. | Codex |
| 2026-06-17 | Expensive maintenance scenarios support run-once execution. | Vector index rebuild and flush should measure one operation cleanly instead of looping for an arbitrary duration. | Codex |
| 2026-06-18 | Transaction workloads cover create, read-your-writes update, mixed child attachments, and rollback. | This keeps per-operation setup small while exercising more transaction operation types and child-object paths inside the measured transaction. | Codex |
| 2026-06-19 | This plan now distinguishes implemented smoke/performance coverage from exhaustive correctness/coherency coverage. | Short performance smoke runs do not satisfy full provider correctness acceptance. | Codex |

## Blockers

Track blockers explicitly so progress can resume quickly.

| Date | Blocker | Impact | Owner | Resolution |
| --- | --- | --- | --- | --- |
|  |  |  |  |  |
