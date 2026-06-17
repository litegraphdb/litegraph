# LiteGraph Performance and Scalability Test Harness Plan

## Purpose

Build a single C# console application named `Test.PerformanceAndScalability` that can apply realistic and adversarial load to LiteGraph, measure the system from several angles, and produce repeatable artifacts that explain where throughput, latency, concurrency, memory, storage, and query behavior break down.

The harness should answer these questions:

- How many operations per second can LiteGraph sustain for each major operation family?
- How does latency change as graph size, data payload size, vector dimensionality, topology, and concurrency increase?
- Which LiteGraph APIs translate into excessive repository calls, row reads, allocations, lock contention, or storage growth?
- Where is the scalability knee for SQLite and PostgreSQL under read, write, mixed, traversal, query, vector, and transaction workloads?
- Which changes improve performance without hiding regressions in correctness, durability, or tail latency?

The first deliverable is this plan. Implementation should happen after the plan is reviewed.

## Repository Findings That Shape The Harness

LiteGraph already has patterns the harness should reuse where practical:

- The main solution is `src/LiteGraph.sln`.
- Core APIs live in `src/LiteGraph`.
- Existing correctness suites live in `src/Test.Shared` and `src/Test.Automated`, with `Test.Automated` using `Touchstone.Cli`.
- `LiteGraphClient` exposes the main API surface: tenants, graphs, nodes, edges, labels, tags, vectors, vector indexes, native graph queries, transactions, batching, request history, and admin/storage operations.
- `GraphRepositoryFactory.Create(DatabaseSettings)` is the natural provider entry point.
- `DatabaseSettings` supports `Sqlite`, `Postgresql`, `Mysql`, and `SqlServer` settings, but only SQLite and PostgreSQL currently have real repository implementations. MySQL and SQL Server should be accepted by the CLI but fail clearly until their repositories are implemented.
- SQLite supports file and in-memory modes and configures WAL, cache, synchronous mode, temp store, and memory mapping. It uses repository-level synchronization in several transaction/shared-connection paths, so concurrency testing must explicitly expose lock contention.
- PostgreSQL uses `NpgsqlDataSource`, schema-aware storage, command timeout settings, max connections, graph-scoped transactions, and provider-specific vector index storage.
- LiteGraph already emits useful telemetry through `LiteGraphTelemetry`, including repository operation timing, statement counts, row counts, transactional flags, vector search timing, and native query profile timings.
- The native graph query language supports query profiling through `GraphQueryRequest.IncludeProfile`.

The harness should be a load and measurement tool, not a replacement for the existing correctness test suites. It should still perform lightweight correctness sampling so performance numbers are not accepted when the system is returning invalid results.

## Target Project

Create a new console project:

```text
src/Test.PerformanceAndScalability/Test.PerformanceAndScalability.csproj
```

Recommended shape:

- Target the same frameworks used by the test projects, currently `net8.0` and `net10.0` where feasible.
- Reference `LiteGraph` directly.
- Reuse small helpers from `Test.Shared` only when they do not bring correctness-test assumptions into the performance harness.
- Prefer a custom runtime-configurable load engine over BenchmarkDotNet for the primary harness. BenchmarkDotNet is good for isolated microbenchmarks, but this harness needs configurable providers, persistent databases, long runs, concurrency ramps, open-loop load, telemetry aggregation, and artifact generation.
- Keep the harness runnable from command line and CI without a server. The primary driver should be in-process `LiteGraphClient` plus repository.
- Allow a later optional driver mode for REST or MCP, but do not make server hosting a prerequisite for the first implementation.

## CLI Contract

The harness must run with no arguments and default to a temporary SQLite file.

Default behavior:

```text
dotnet run --project src/Test.PerformanceAndScalability
```

Expected default:

- Create a temporary SQLite database file, for example under `%TEMP%/litegraph-perf/{run-id}/litegraph.db`.
- Run a smoke-scale benchmark set that completes quickly.
- Emit console progress plus JSON, CSV, and Markdown artifacts under an output directory.
- Delete the temporary database unless `--keep-database` is specified.

Provider options:

```text
--db-type sqlite|postgresql|mysql|sqlserver
--connection-string <connection-string>
--sqlite-file <path>
--in-memory true|false
--host <hostname>
--port <port>
--database <name>
--username <name>
--password <password>
--schema <schema>
--max-connections <count>
--command-timeout-seconds <seconds>
```

Run selection options:

```text
--profile smoke|small|medium|large|soak|custom
--workloads all|ingest,reads,search,traversal,query,vector,transactions,updates,deletes,maintenance,stress
--operation-mix read-heavy|write-heavy|balanced|vector-heavy|transaction-heavy|custom
--scale-factor <number>
--tenants <count>
--graphs-per-tenant <count>
--nodes-per-graph <count>
--edges-per-graph <count>
--vectors-per-graph <count>
--labels-per-node <count>
--tags-per-node <count>
--payload-size small|medium|large|custom
--vector-dimensions <count>
--vector-top-k <count>
--topology random|power-law|grid|tree|chain|hub|communities|dense|custom
```

Load options:

```text
--concurrency <list-or-range>       # examples: 1,2,4,8,16 or 1..64x2
--duration <timespan>               # measurement duration per scenario
--warmup <timespan>
--cooldown <timespan>
--iterations <count>
--target-rate <ops-per-second>      # enables open-loop mode
--closed-loop true|false
--batch-size <count>
--transaction-size <count>
--timeout <timespan>
--seed <integer>
```

Measurement and output options:

```text
--output <directory>
--run-id <id>
--include-query-profile true|false
--sample-correctness true|false
--sample-rate <0.0-1.0>
--capture-process-metrics true|false
--capture-repository-telemetry true|false
--capture-db-file-metrics true|false
--keep-database true|false
--dry-run true|false
--fail-on-regression true|false
--baseline <previous-result-json>
```

Security requirement:

- Never print passwords or full connection strings unless explicitly requested with a dangerous diagnostic flag.
- Use `DatabaseSettings.ToSafeString()` or equivalent redaction in logs and reports.

## Harness Architecture

The console app should be organized around clear layers.

### Configuration Layer

Responsibilities:

- Parse CLI arguments.
- Build `DatabaseSettings`.
- Expand named profiles into concrete scale, workload, and duration settings.
- Validate unsupported combinations early. For example, MySQL and SQL Server should currently report that the repository provider is not implemented.
- Write the effective configuration to the output directory with secrets redacted.

### Run Controller

Responsibilities:

- Create a run id.
- Capture environment metadata.
- Create and initialize the repository and `LiteGraphClient`.
- Execute workload phases in a stable order.
- Isolate scenarios when needed by rebuilding the dataset or by creating separate tenants/graphs.
- Coordinate warmup, measurement, cooldown, and cleanup.
- Stop runs cleanly on cancellation.

### Dataset Generator

Responsibilities:

- Produce deterministic tenants, graphs, nodes, edges, labels, tags, vectors, and data payloads from a seed.
- Generate multiple graph topologies.
- Track expected IDs and relationship structure for sampled correctness checks.
- Support incremental scaling so the same database can be expanded from small to medium to large without regenerating everything.
- Emit dataset metadata: counts, topology, payload profile, vector dimensions, graph density, max degree, average degree, and expected hot keys.

### Workload Catalog

Responsibilities:

- Encapsulate each operation family behind a common interface.
- Declare required dataset features for each workload.
- Declare whether the workload is read-only, write-only, mixed, transactional, vector-dependent, query-dependent, destructive, or maintenance-oriented.
- Provide operation-level validation hooks.

### Load Engine

Responsibilities:

- Run closed-loop workloads where each worker issues the next operation after the previous one completes.
- Run open-loop workloads where operations are scheduled at a target rate to expose queueing and coordinated-omission effects.
- Support fixed concurrency, stepped concurrency ramps, and sustained soak periods.
- Use per-worker deterministic random streams to avoid lock contention in the harness itself.
- Track attempted, started, completed, failed, timed out, and canceled operations.
- Preserve enough per-operation timing data to compute accurate percentiles.

### Metrics Collectors

Responsibilities:

- Subscribe to `LiteGraphTelemetry.RepositoryOperationRecorded`.
- Subscribe to `LiteGraphTelemetry.VectorSearchRecorded`.
- Capture native query execution profiles when requested.
- Capture process metrics from .NET and the OS.
- Capture storage size metrics for SQLite database, WAL, SHM, and vector index files.
- Capture PostgreSQL connection and provider settings. Optional later work can add provider-specific database statistics through SQL queries.

### Reporters

Responsibilities:

- Print concise live progress.
- Emit raw operation samples or histograms.
- Emit per-scenario JSON.
- Emit normalized CSV for spreadsheet and dashboard use.
- Emit a Markdown summary with the top bottlenecks, best throughput, worst tail latency, error rates, and suspected causes.
- Emit machine-readable regression comparison results when a baseline is provided.

## Execution Methodology

Every benchmark scenario should follow the same lifecycle so runs are comparable.

### 1. Capture Environment

Capture before any workload starts:

- Git commit SHA and dirty-tree status.
- LiteGraph assembly version.
- .NET runtime version.
- OS name and version.
- Process architecture.
- CPU model if available.
- Logical processor count.
- Total memory if available.
- GC mode and latency mode.
- Current power plan if available.
- Database provider and redacted settings.
- SQLite file path or PostgreSQL database/schema.
- Harness command line with secrets redacted.

Why it matters: performance numbers without environment metadata are difficult to compare. CPU count, GC mode, provider settings, and commit SHA are often enough to explain otherwise confusing differences.

### 2. Initialize Storage

For each run:

- Build `DatabaseSettings`.
- Create the repository through `GraphRepositoryFactory`.
- Create `LiteGraphClient`.
- Call repository/client initialization paths required by LiteGraph.
- Confirm the provider is supported.
- Record initial storage size.

Why it matters: initialization cost and schema readiness can affect first-operation latency. Recording it separately prevents confusing setup time with workload performance.

### 3. Generate Dataset

For each scenario or scenario group:

- Create tenants and graphs.
- Generate deterministic node, edge, label, tag, data, and vector content.
- Build the selected topology.
- Optionally enable vector indexes before or after ingestion depending on the scenario.
- Record generation time separately from measurement unless ingestion itself is the workload under test.

Why it matters: graph shape and data distribution are central to graph performance. A random graph, a power-law graph, and a hub graph stress different code paths.

### 4. Warm Up

Before measuring:

- Run the workload for a configured warmup period.
- Discard warmup samples from benchmark summaries.
- Keep warmup errors visible.
- Allow JIT compilation, connection pool establishment, prepared internal caches, and SQLite page cache behavior to stabilize.

Why it matters: first-use costs distort steady-state results.

### 5. Measure

During measurement:

- Start a monotonic timer.
- Start telemetry aggregation.
- Run the workload for either a fixed duration or a fixed operation count.
- Record operation timings, outcomes, result sizes, and telemetry.
- Apply cancellation at the configured timeout.
- Avoid logging per operation unless diagnostic tracing is explicitly enabled.

Why it matters: stable, bounded measurement windows make throughput and latency comparable across providers and commits.

### 6. Sample Correctness

During or after measurement:

- Randomly sample successful operations and verify expected results.
- Verify entity counts after ingest/update/delete workloads.
- Verify transaction rollback scenarios leave no partial state.
- Verify traversal and query result shapes against the generated topology for small and smoke profiles.
- Verify vector search returns the expected nearest synthetic vectors in controlled datasets.

Why it matters: a fast wrong answer is not a valid performance improvement.

### 7. Cool Down And Cleanup

After measurement:

- Stop workers.
- Flush pending client or repository state if needed.
- Record final storage size.
- Dispose the client and repository.
- Delete temporary storage unless `--keep-database` is set.
- Write final artifacts even if the run failed.

Why it matters: storage growth, cleanup failures, and dispose-time exceptions are part of scalability behavior.

### 8. Repeat And Compare

For serious analysis:

- Run each scenario multiple times.
- Report median run, best run, worst run, and variability.
- Compare against a baseline result file when requested.
- Flag throughput regressions, latency regressions, allocation increases, storage growth, and error-rate changes.

Why it matters: single benchmark runs are noisy. Repetition separates noise from real movement.

## Metrics To Capture

### Operation Metrics

Capture for every operation family:

- Attempted operations.
- Completed operations.
- Failed operations.
- Timed-out operations.
- Canceled operations.
- Success rate.
- Throughput in operations per second.
- Item throughput for batch operations, such as nodes per second or edges per second.
- Latency min, mean, standard deviation, p50, p90, p95, p99, p99.9, and max.
- Result count, row count, or item count where relevant.
- Payload size profile.

Why relevant:

- Throughput shows capacity.
- Tail latency shows user-visible degradation under pressure.
- Error and timeout rates show saturation or correctness risk.
- Item throughput prevents a batch of 1,000 nodes from being treated like a single-node operation.

### Repository Telemetry

From `LiteGraphTelemetry.RepositoryOperationRecorded`, aggregate by provider, operation, success, and transactional flag:

- Repository operation count per high-level LiteGraph operation.
- Repository operation duration.
- Statement count.
- Row count.
- Transactional versus non-transactional operation mix.

Why relevant:

- Reveals N+1 patterns.
- Separates client-side time from repository time.
- Shows whether high-level APIs are doing too many statements or reading too many rows.
- Helps prove whether an optimization reduced storage work or merely shifted time elsewhere.

### Native Query Profile Metrics

When `GraphQueryRequest.IncludeProfile` is enabled, capture:

- Parse time.
- Planning time.
- Execution time.
- Repository time.
- Vector time.
- Transaction time.
- Total time.
- Result rows.

Why relevant:

- Separates parser/planner overhead from storage execution.
- Helps locate whether query improvements should target planning, repository access, traversal execution, or vector search.

### Vector Metrics

Capture for vector create, search, index enable, rebuild, and delete:

- Vector dimension count.
- Vector count.
- Top-K.
- Search result count.
- Search latency percentiles.
- Index build time.
- Index rebuild time.
- Index file size.
- Dirty or fallback behavior when index state changes.
- Exact search versus indexed search comparison where supported.

Why relevant:

- Vector performance is highly sensitive to dimension count, corpus size, and indexing.
- Index maintenance can dominate write-heavy workloads.
- Search accuracy and latency must be considered together.

### Process Metrics

Capture periodically:

- Process CPU percentage.
- Total process CPU time.
- Working set.
- Private bytes where available.
- Managed heap size.
- Allocated bytes.
- Gen0, Gen1, and Gen2 collection counts.
- Time in GC if available.
- Thread count.
- ThreadPool worker and IO availability where available.
- Handle count where available.

Why relevant:

- CPU saturation identifies compute-bound scenarios.
- Memory and GC growth expose leaks, excessive allocation, and poor streaming behavior.
- ThreadPool starvation can explain tail latency under concurrency.

### Storage Metrics

For SQLite:

- Main database file size.
- WAL file size.
- SHM file size.
- Vector index directory size.
- Growth rate during writes.
- Size after deletes and flush/maintenance operations.

For PostgreSQL:

- Provider settings and connection pool settings.
- Optional later metrics from database catalog views, such as database size, active connections, locks, sequential scans, index scans, and dead tuples.

Why relevant:

- Storage growth affects deployment costs and long-running stability.
- SQLite WAL growth can reveal write pressure and checkpoint behavior.
- PostgreSQL pool and lock behavior often explains throughput cliffs.

### Scalability Metrics

For each concurrency or scale step:

- Throughput versus concurrency.
- p95 and p99 latency versus concurrency.
- Error rate versus concurrency.
- CPU versus concurrency.
- Repository statements per operation versus data size.
- Rows read per operation versus data size.
- Memory growth over time.
- Storage growth over time.

Why relevant:

- These curves identify the scalability knee where more concurrency no longer increases throughput.
- Rows and statements versus data size expose algorithmic scaling issues.
- Memory and storage trends expose long-run risks.

## Dataset Profiles

Named profiles should expand into concrete dataset sizes and durations. Exact values can be adjusted during implementation, but the harness should define a stable starting point.

### Smoke

Purpose: quick validation on any machine.

- 1 tenant.
- 1 to 2 graphs.
- Hundreds of nodes.
- Hundreds to low thousands of edges.
- Small payloads.
- 3D and 128D vectors.
- Short warmup and measurement windows.

### Small

Purpose: local development baseline.

- 1 to 2 tenants.
- 5 to 10 graphs.
- Thousands of nodes.
- Tens of thousands of edges.
- Mixed payloads.
- 128D and 384D vectors.

### Medium

Purpose: meaningful provider comparison.

- Multiple tenants.
- Tens of graphs.
- Tens to hundreds of thousands of nodes.
- Hundreds of thousands to millions of edges where hardware allows.
- 384D and 768D vectors.
- Longer measurement windows.

### Large

Purpose: scalability and degradation analysis.

- Many tenants and graphs.
- Hundreds of thousands to millions of nodes.
- Millions of edges.
- Large payload subsets.
- 768D and 1536D vectors.
- High-concurrency read, write, mixed, traversal, query, and vector workloads.

### Soak

Purpose: long-run stability.

- Configurable dataset.
- Sustained mixed load for tens of minutes to hours.
- Periodic destructive and maintenance operations if enabled.
- Memory, GC, file growth, error drift, and latency drift emphasized.

## Graph Topologies

The generator should support several topologies because graph workload performance is topology-sensitive.

### Uniform Random

Edges are distributed randomly across nodes.

Use for:

- General baseline.
- Broad query and traversal behavior.
- Mixed workloads without extreme skew.

### Power-Law

Most nodes have low degree and a small number of nodes have very high degree.

Use for:

- Social-network-like graphs.
- Hot node contention.
- High-degree traversal and edge lookup stress.

### Hub And Spoke

One or more central nodes connect to many leaf nodes.

Use for:

- Worst-case neighbor expansion.
- Delete-node cascade behavior.
- ReadNodeEdges, ReadEdgesFrom, ReadEdgesTo, and subgraph limits.

### Chain

Nodes form long paths.

Use for:

- Route finding.
- Bounded variable-length query patterns.
- Deep traversal behavior.

### Tree Or DAG

Parent-child relationships are structured with predictable depth.

Use for:

- ReadParents, ReadChildren, hierarchy traversal, and route correctness.
- Testing depth limits and branching factors.

### Grid Or Lattice

Nodes have predictable local connectivity.

Use for:

- Bounded pathfinding.
- Route explosion behavior.
- Traversal consistency.

### Communities

Dense clusters with sparse cross-cluster edges.

Use for:

- Subgraph extraction.
- Community-like query selectivity.
- Mixed local and global traversal behavior.

### Dense

Many nodes connect to many other nodes.

Use for:

- Stressing edge storage and edge filters.
- Exposing algorithms that become expensive with high edge density.

## Data Payload Profiles

Payloads should be generated with deterministic distributions.

Small payload:

- Several scalar properties.
- Short strings.
- A few numeric fields.

Medium payload:

- Nested objects.
- Arrays.
- Mixed numeric, boolean, date, and string values.
- Fields used for filter predicates.

Large payload:

- Larger strings.
- Deeper nested objects.
- Larger arrays.
- Fields that are rarely queried but affect serialization and storage cost.

Why relevant:

- LiteGraph operations often include optional data hydration.
- Serialization, deserialization, filtering, and storage costs change materially with payload size.

## Workload Catalog

The harness should implement workloads across the full public surface of LiteGraph. Each workload should support scale, concurrency, and repeatability.

### 1. Ingestion Workloads

Operations:

- Create tenants.
- Create graphs.
- Create nodes one at a time.
- Create nodes through `CreateMany`.
- Create edges one at a time.
- Create edges through `CreateMany`.
- Attach labels and tags during creation where supported.
- Create vectors one at a time.
- Create vectors through `CreateMany`.
- Compare return modes for bulk create operations.

Measurements:

- Operations per second.
- Items per second.
- Latency percentiles.
- Repository statements per item.
- Storage growth per item.
- Allocation per item.

Method:

- Run single-item create baselines.
- Run batch-size sweeps, for example 10, 100, 500, 1,000, and any known max-safe size.
- Run with vector index disabled and enabled.
- Run with small, medium, and large payloads.
- Run with SQLite file, SQLite in-memory, and PostgreSQL when configured.

Why relevant:

- Ingestion is often the first scalability bottleneck.
- Batch behavior determines whether LiteGraph can load large graphs efficiently.
- Index maintenance cost needs to be visible rather than hidden.

### 2. Point Read And Existence Workloads

Operations:

- Read node by GUID.
- Read many nodes by GUID.
- Read edge by GUID.
- Read many edges by GUID.
- Read graph by GUID.
- Read tenant by GUID.
- Read vector by GUID.
- Exists checks for each major entity.

Measurements:

- Hot-key and cold-key latency.
- Batch read item throughput.
- Repository calls per read.
- Cache effect when caching is enabled.

Method:

- Pre-generate hot and cold ID sets.
- Use a Zipf-like distribution for hot-key reads.
- Compare single GUID reads with batched GUID reads.
- Run read-only concurrency ramps.

Why relevant:

- Point reads are a baseline for repository overhead.
- Hot-key behavior reveals cache effectiveness and lock contention.

### 3. Enumeration And Paging Workloads

Operations:

- Read all nodes in tenant.
- Read all nodes in graph.
- Enumerate nodes.
- Enumerate edges.
- Enumerate graphs.
- Enumerate vectors.
- Read with `includeData` enabled and disabled where available.
- Read with subordinate hydration enabled and disabled where available.
- Deep paging through skip and continuation token paths.

Measurements:

- Time to first row.
- Rows per second.
- Total enumeration time.
- Memory growth.
- Allocations per row.
- Repository row counts.

Method:

- Run against increasing graph sizes.
- Compare streaming enumeration with materialized reads.
- Test early cancellation.
- Test deep pages and continuation tokens.

Why relevant:

- Enumeration workloads expose memory pressure and streaming quality.
- Time to first row matters for large result sets.

### 4. Search And Filter Workloads

Operations:

- Read by name.
- Read by labels.
- Read by tags.
- Read by data expression filters.
- Combined name, label, tag, and data filters.
- Low-selectivity and high-selectivity searches.

Measurements:

- Latency by selectivity.
- Rows scanned versus rows returned.
- Repository statements and row counts.
- Payload hydration cost.

Method:

- Generate fields with known cardinality.
- Sweep selectivity from very narrow to broad.
- Run with and without data hydration.
- Compare query latency as graph size increases.

Why relevant:

- Search scalability depends on filter selectivity, indexing, and hydration behavior.
- Broad filters can reveal full-scan behavior.

### 5. Edge And Neighborhood Workloads

Operations:

- Read node edges.
- Read edges from node.
- Read edges to node.
- Read edges between nodes.
- Read parents.
- Read children.
- Read neighbors.
- Read most connected nodes.
- Read least connected nodes.

Measurements:

- Latency by node degree.
- Result count.
- Repository row counts.
- Memory allocation.

Method:

- Run on uniform, power-law, hub, tree, and dense topologies.
- Separate low-degree, median-degree, and high-degree node samples.
- Apply concurrency to hot high-degree nodes and distributed nodes.

Why relevant:

- Graph systems often fail at high-degree nodes before they fail at average cases.
- These APIs are likely to reveal missing edge indexes or expensive subordinate hydration.

### 6. Traversal And Route Workloads

Operations:

- Route reads between known connected nodes.
- Route reads between disconnected nodes.
- Parent/child traversal at increasing depth.
- Neighbor traversal with limits.
- Subgraph extraction with depth and max-node limits.
- Subgraph statistics.

Measurements:

- Latency by depth.
- Expanded node count.
- Expanded edge count.
- Repository operation count.
- Rows read.
- Cancellation and timeout behavior.

Method:

- Use chain, grid, tree, community, and dense topologies.
- Sweep depth and max result limits.
- Include impossible route requests to measure negative-case cost.
- Confirm result correctness on smoke and small datasets.

Why relevant:

- Traversal costs can grow explosively.
- Negative cases are important because they can require more search than successful cases.

### 7. Native Graph Query Workloads

Operations:

- One-hop `MATCH` expansion.
- Fixed two-hop traversal.
- Bounded variable-length traversal.
- Data-filtered node lookup.
- Label-filtered and tag-filtered node lookup.
- Aggregates and counts.
- Ordering and limits.
- Optional matches if supported.
- Query mutations where supported.
- Vector search through query calls.

Measurements:

- Total query latency.
- Parse, plan, execute, repository, vector, and transaction profile timings.
- Result row count.
- Repository statements and rows.
- Error rate for invalid or timeout-prone queries.

Method:

- Use parameterized query templates.
- Run each query with `IncludeProfile` enabled for profiling scenarios.
- Run the same logical operation through direct API and native query when possible.
- Compare profile stages across graph sizes.

Why relevant:

- Native query performance may bottleneck in planning, execution, repository access, or vector search.
- Comparing direct API and query paths shows the overhead of query abstraction.

### 8. Vector Workloads

Operations:

- Create vectors.
- Bulk create vectors.
- Read vectors.
- Update vectors.
- Delete vectors.
- Search vectors.
- Enable vector index.
- Rebuild vector index.
- Delete vector index.
- Search while index is clean.
- Search after writes that make index state dirty or require fallback behavior.

Measurements:

- Create throughput.
- Search throughput.
- Search latency percentiles.
- Index build and rebuild time.
- Index file size.
- Memory usage during index operations.
- Search quality sanity checks on synthetic vectors.

Method:

- Sweep vector dimensions, for example 3, 128, 384, 768, and 1536.
- Sweep corpus sizes.
- Sweep top-K values.
- Compare exact and indexed paths where available.
- Run read/search concurrency while writes are occurring.
- Run index rebuild during query/search load as a stress case if supported safely.

Why relevant:

- Vector workloads scale differently from graph traversal and CRUD.
- Index maintenance can create large latency spikes and storage growth.

### 9. Transaction Workloads

Operations:

- Transactional create node, edge, label, tag, and vector.
- Transactional update.
- Transactional delete.
- Transactional attach and detach.
- Transactional upsert.
- Mixed transactions with create, update, delete, attach, detach, and vector operations.
- Transaction rollback through an intentionally invalid operation.
- Concurrent transactions on the same tenant and graph.
- Concurrent transactions on different graphs.
- Concurrent transactions on different tenants.

Measurements:

- Transactions per second.
- Operations per second inside transactions.
- Commit latency.
- Rollback latency.
- Failure cleanup correctness.
- Lock contention and timeout rate.
- Repository transactional operation counts.

Method:

- Sweep transaction size up to the configured max operation count.
- Compare transaction workloads to equivalent non-transactional operations.
- Run same-graph and different-graph concurrency tests.
- Verify rollback leaves no partial state.

Why relevant:

- LiteGraph transactions are graph-scoped, so scalability depends on graph and tenant partitioning.
- Transaction overhead and lock behavior are central to write scalability.

### 10. Update And Delete Workloads

Operations:

- Update nodes.
- Update edges.
- Update vectors.
- Delete nodes.
- Delete edges.
- Delete vectors.
- Delete many nodes.
- Delete many edges.
- Delete all vectors.
- Delete graph.
- Delete tenant.

Measurements:

- Operations per second.
- Cascading effect where applicable.
- Repository statements and rows.
- Storage size before and after.
- Vector index dirty or rebuild behavior.

Method:

- Run updates with small and large payload changes.
- Delete low-degree and high-degree nodes separately.
- Compare single deletes with bulk deletes.
- Run destructive workloads in isolated tenants or disposable databases.

Why relevant:

- Deletes and updates often expose referential cleanup cost, index maintenance cost, and storage bloat.

### 11. Maintenance And Admin Workloads

Operations:

- Repository initialization.
- Flush.
- Backup where supported.
- Statistics.
- Graph statistics.
- Subgraph statistics.
- Vector index rebuild.
- Request history behavior if enabled.

Measurements:

- Operation duration.
- Storage reads and rows.
- Storage growth.
- Blocking impact on concurrent foreground operations.

Method:

- Run maintenance operations on quiet databases.
- Run selected maintenance operations during read-heavy and mixed load.
- Measure foreground latency impact.

Why relevant:

- Production systems need predictable maintenance behavior, not only fast foreground CRUD.

### 12. Mixed Workload Profiles

Profiles:

- Read-heavy: 90 percent reads, 10 percent writes.
- Write-heavy: 80 percent writes, 20 percent reads.
- Balanced: 50 percent reads, 30 percent writes, 10 percent traversal, 10 percent query.
- Vector-heavy: 60 percent vector search, 20 percent vector writes, 20 percent graph reads.
- Transaction-heavy: 70 percent transactional writes, 30 percent reads.
- Hotspot: most operations target a small fraction of nodes or graphs.
- Tenant-isolated: operations spread across many tenants.
- Graph-isolated: operations spread across many graphs in one tenant.

Measurements:

- Overall throughput.
- Per-operation throughput.
- Per-operation latency.
- Error rate by operation type.
- Repository operation mix.
- Tail latency during writes, vector index work, and maintenance.

Method:

- Use weighted random operation selection.
- Keep operation weights in the report.
- Use deterministic seeds.
- Run closed-loop and open-loop variants.

Why relevant:

- Real systems are mixed. A high single-operation throughput number can hide bad interaction between reads, writes, traversal, and index maintenance.

### 13. Stress And Soak Workloads

Stress tests:

- Step concurrency until throughput stops improving or errors exceed a threshold.
- Apply open-loop target rates above observed capacity.
- Exercise hot high-degree nodes.
- Run concurrent writes and traversals.
- Run concurrent vector writes and vector searches.
- Run cancellation and timeout-heavy workloads.

Soak tests:

- Run sustained mixed load for a long duration.
- Capture memory, GC, storage, and latency trends.
- Periodically sample correctness.
- Record error drift and tail-latency drift.

Why relevant:

- Short tests find obvious bottlenecks.
- Long tests find leaks, file growth, pool exhaustion, and gradual latency degradation.

## Concurrency And Scalability Model

The harness should support both closed-loop and open-loop load.

Closed-loop model:

- A fixed number of workers repeatedly issue operations.
- The next operation starts only after the previous operation finishes.
- Useful for finding maximum sustainable throughput for a given concurrency.

Open-loop model:

- Operations are scheduled at a target arrival rate.
- The harness records queueing and missed schedule time.
- Useful for exposing coordinated omission and overload behavior.

Concurrency ramps:

- Run each workload at configured levels such as 1, 2, 4, 8, 16, 32, and 64 workers.
- Stop early if error rate or timeout rate exceeds a configured limit.
- Record the saturation point where throughput flattens or p99 latency grows sharply.

Partitioning model:

- Same graph, same tenant.
- Different graphs, same tenant.
- Different tenants.
- Hot nodes versus distributed nodes.

Why relevant:

- LiteGraph has graph-scoped transactional behavior and provider-specific locking. Partitioning tests reveal whether concurrency scales by graph, by tenant, or not at all.

## Methodology For Finding Improvements

The harness should not only produce numbers. It should point to likely causes and make before/after optimization work disciplined.

### Baseline First

For every provider and profile:

- Establish a baseline result file.
- Save raw scenario summaries and environment metadata.
- Commit or archive baseline artifacts with the performance investigation when appropriate.

Use the baseline to compare future changes by:

- Throughput change percentage.
- p95 and p99 latency change percentage.
- Error-rate change.
- Repository statements per high-level operation.
- Rows read per high-level operation.
- Allocations per operation.
- Storage growth per operation.

### Locate The Bottleneck Layer

For each slow scenario, compare:

- High-level operation latency.
- Repository operation duration.
- Repository operation count.
- Statement count.
- Row count.
- Query profile timings.
- Process CPU and GC.
- Storage growth.

Interpretation examples:

- High high-level latency but low repository time points to client-side processing, serialization, allocation, or synchronization.
- High repository time with many statements points to N+1 access patterns or missing bulk paths.
- High row counts with low result counts points to poor selectivity or missing indexes.
- p99 spikes during writes can point to locks, checkpoints, index rebuilds, or GC.
- Throughput flattening while CPU is low can point to locks, connection pool limits, or storage waits.

### Sweep One Dimension At A Time

Run controlled sweeps for:

- Data size.
- Node degree.
- Edge density.
- Payload size.
- Vector dimension.
- Vector corpus size.
- Top-K.
- Batch size.
- Transaction size.
- Concurrency.
- PostgreSQL max connections.
- SQLite file versus in-memory.

Why relevant:

- Single-dimension sweeps expose complexity curves. A linear curve may be acceptable. A curve that grows with edges squared is a design problem.

### Compare Equivalent Paths

Compare:

- Single create versus bulk create.
- Non-transactional writes versus transaction writes.
- Direct API reads versus native graph query reads.
- Exact vector search versus indexed vector search.
- Read with data hydration versus without data hydration.
- Same graph concurrency versus different graph concurrency.

Why relevant:

- Equivalent-path comparisons isolate overhead introduced by a specific abstraction or feature.

### Identify N+1 And Hydration Cost

For operations that return nodes, edges, labels, tags, or data:

- Record repository operations per high-level call.
- Record row counts per high-level call.
- Compare include-data and include-subordinate modes.
- Compare small and large payloads.

Why relevant:

- N+1 patterns and unnecessary hydration are common causes of graph API performance problems.

### Identify Lock And Pool Contention

For concurrency workloads:

- Compare SQLite file and SQLite in-memory.
- Compare SQLite read-only and mixed read/write workloads.
- Compare PostgreSQL max connection settings.
- Compare same-graph and different-graph transactions.
- Track timeout and p99 behavior.

Why relevant:

- Lock contention and pool starvation usually show up as tail latency before they show up as average latency.

### Turn Findings Into Work Items

Each performance issue should produce a short finding:

- Scenario name.
- Provider.
- Dataset profile.
- Symptom.
- Evidence from metrics.
- Suspected cause.
- Proposed code area to inspect.
- Suggested optimization.
- Before/after benchmark command.
- Pass/fail threshold.

Why relevant:

- This makes performance work reproducible and reviewable instead of anecdotal.

## Reporting Artifacts

Each run should create an output directory like:

```text
artifacts/perf-scale/{run-id}/
```

Suggested files:

```text
config.redacted.json
environment.json
dataset.json
summary.json
summary.csv
operations.csv
repository-telemetry.csv
query-profiles.csv
process-metrics.csv
storage-metrics.csv
report.md
anomalies.json
```

`report.md` should include:

- Run metadata.
- Provider and database settings with secrets redacted.
- Dataset summary.
- Best throughput scenarios.
- Worst p99 latency scenarios.
- Error and timeout summary.
- Scalability knee by workload.
- Top repository operations by total time.
- Top high-level operations by repository statement count.
- Vector index summary.
- Storage growth summary.
- Regression comparison when a baseline is provided.
- Recommended next investigations.

## Suggested Implementation Phases

### Phase 0: Plan

- Create this document.
- Review scope and methodology.

### Phase 1: Harness Skeleton

- Add `Test.PerformanceAndScalability` project to `src/LiteGraph.sln`.
- Implement CLI parsing.
- Implement provider configuration.
- Implement default temporary SQLite file behavior.
- Implement output directory creation.
- Implement environment capture.
- Implement basic JSON, CSV, and Markdown report writing.
- Implement telemetry subscribers.

Exit criteria:

- `dotnet run --project src/Test.PerformanceAndScalability` creates a temp SQLite database, initializes LiteGraph, and writes a minimal report.

### Phase 2: Dataset Generator

- Implement deterministic tenants, graphs, nodes, edges, labels, tags, data payloads, and vectors.
- Implement smoke and small profiles.
- Implement random, chain, tree, hub, and power-law topologies.
- Implement correctness sampling metadata.

Exit criteria:

- A smoke dataset can be generated and verified repeatedly with the same seed.

### Phase 3: Core CRUD And Read Workloads

- Implement ingestion, point reads, existence checks, enumeration, paging, search, update, and delete workloads.
- Implement concurrency ramps.
- Implement batch-size sweeps.

Exit criteria:

- Smoke and small profiles produce per-workload throughput and latency reports.

### Phase 4: Traversal, Query, Vector, And Transaction Workloads

- Implement neighborhood and route workloads.
- Implement native query templates and profile capture.
- Implement vector create/search/index workloads.
- Implement transaction workloads and rollback validation.

Exit criteria:

- The harness covers the main LiteGraph API surface and captures repository telemetry for each operation family.

### Phase 5: Stress, Soak, And Regression Comparison

- Implement open-loop load.
- Implement long-running soak profiles.
- Implement anomaly detection.
- Implement baseline comparison and optional threshold-based failure.

Exit criteria:

- A baseline can be saved and compared against a later run with clear regression output.

### Phase 6: Optional Server Driver

- Add optional REST driver mode if direct in-process results need to be compared with deployed server behavior.
- Capture HTTP latency separately from LiteGraph repository telemetry.

Exit criteria:

- The same workload definitions can run against direct client mode or REST mode.

## Acceptance Criteria For The First Complete Harness

- Running with no arguments uses a temporary SQLite file.
- SQLite file, SQLite in-memory, and PostgreSQL can be selected through CLI arguments.
- MySQL and SQL Server options fail with a clear unsupported-provider message until implemented.
- The harness can generate deterministic smoke and small datasets.
- The harness measures ingestion, reads, search, traversal, native query, vector, transaction, update, delete, mixed, stress, and soak workload families.
- Every scenario reports throughput, latency percentiles, success rate, timeout rate, and operation counts.
- Repository telemetry is captured and correlated to high-level scenarios.
- Query profile metrics are captured for native query scenarios when requested.
- Process and storage metrics are captured when requested.
- Reports are written in JSON, CSV, and Markdown.
- A run can be reproduced from its saved redacted configuration and seed.
- A baseline result can be compared with a later run.
- Correctness sampling can fail a scenario when results are invalid.

## Risks And Open Decisions

- A single-process console app can saturate only what one machine can drive. For distributed load, the same harness may later need a coordinator/worker mode, but that is outside the first implementation.
- Large profiles can consume significant disk, memory, and time. Defaults must be conservative, with explicit opt-in for large and soak runs.
- Provider-specific database metrics for PostgreSQL are useful but should not block the first implementation.
- MySQL and SQL Server settings exist in `DatabaseSettings`, but the current repository implementations are placeholders. The harness should not pretend to benchmark unsupported providers.
- BenchmarkDotNet can be valuable for future microbenchmarks, but the main harness should remain a configurable load generator.
- Vector index behavior must be carefully isolated because index files live outside normal entity tables and can affect repeatability if not cleaned between runs.
- Destructive workloads must use isolated tenants, graphs, or disposable databases so they do not corrupt shared test datasets.
- Performance reports must avoid leaking connection strings, passwords, bearer tokens, or sensitive payload data.
