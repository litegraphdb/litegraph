# Performance Improvements Plan

## Scope
This plan covers backend performance and scalability work in:

- `src/LiteGraph`
- `src/LiteGraph.Server`

The goal is to improve throughput, latency, and concurrency without changing the public API surface, endpoint names, or request/response schema.

## Architectural Notes
- The REST server uses Watson `7.0.14` via `src/LiteGraph.Server/LiteGraph.Server.csproj`.
- The core library does not use Watson.
- The MCP server uses `Voltaic`, not Watson.
- Based on the current code paths, the most valuable improvements are in serialization, request-history persistence, repository execution, query/index design, and traversal/query hydration.

## Non-Goals
- No REST route changes.
- No wire-format schema changes.
- No client-breaking changes.
- No Watson replacement as part of this plan unless profiling later proves the HTTP stack is the bottleneck.

## How To Use This File
For each work item:

- Fill in `Owner`, `PR`, `Started`, and `Completed`.
- Change `Status` as work progresses.
- Check off tasks and validation steps as they are completed.
- Add benchmark numbers and notes directly under the work item.

Suggested status values:

- `Not started`
- `In progress`
- `Blocked`
- `Done`

## Recommended Delivery Order
1. Establish baseline metrics and regression guardrails.
2. Land the lowest-risk wins first:
   - serializer option caching
   - compact JSON by default
   - request-history pipeline cleanup
   - removal of unnecessary read-only transactions
3. Land additive database/index improvements.
4. Land query-path improvements:
   - bulk subordinate hydration
   - cursor paging without `OFFSET`
   - request-scoped memoization
5. Tackle SQLite concurrency improvements.
6. Treat repository-global graph transaction redesign as a separate stretch item.

## Shared Acceptance Criteria
- No public API or schema changes.
- Existing tests still pass.
- Representative REST and repository scenarios show measurable improvement.
- No correctness regressions in pagination, traversal, subordinate loading, or request-history capture.

---

## Work Item 0 - Baseline and Guardrails
Priority: `P0`
Status: `In progress`
Owner:
PR:
Started: `2026-05-19`
Completed:
Dependencies: none

Primary files and areas:

- `src/LiteGraph.Server/API/REST`
- `src/LiteGraph/Client/Implementations`
- `src/LiteGraph/GraphRepositories/Postgresql`
- `src/LiteGraph/GraphRepositories/Sqlite`

Objective:
Capture before/after metrics so each optimization is provable and regressions are visible.

Tasks:

- [ ] Define a representative workload matrix for both PostgreSQL and SQLite.
- [ ] Include at least one JSON-heavy REST read path.
- [ ] Include at least one list or enumerate path with `IncludeSubordinates=true`.
- [ ] Include at least one graph query or traversal-heavy path.
- [ ] Include runs with request history enabled and disabled.
- [ ] Record p50, p95, and p99 latency where practical.
- [ ] Record throughput, allocation rate, and CPU usage where practical.
- [ ] Record query counts for the N+1-sensitive paths.
- [ ] Add a short benchmark summary section at the end of this file.

Validation:

- [ ] Baseline numbers are captured before any code changes.
- [ ] The same workload is reused after each completed work item.
- [ ] Benchmark dataset size is documented so runs are comparable.

Completion Notes:

Benchmark Summary:

- Before: Not captured on this working tree before implementation began.
- After: `dotnet build src/LiteGraph.sln -c Debug --no-restore`, `dotnet test src/LiteGraph.sln -c Debug --no-build`, and `dotnet run --project src/LiteGraph.SampleDatabase -- --database docker/litegraph.db` all succeeded on `2026-05-19`.
- Notes: This is correctness validation plus sample-database regeneration, not a full latency or throughput benchmark pass.

---

## Work Item 1 - Serializer Caching and Compact JSON Responses
Priority: `P0`
Status: `In progress`
Owner:
PR:
Started: `2026-05-19`
Completed:
Dependencies: Work Item 0

Primary files:

- `src/LiteGraph/Serialization/Serializer.cs`
- `src/LiteGraph.Server/API/REST/RestServiceHandler.cs`
- `src/LiteGraph.Server/API/REST/RestServiceHandler.Authorization.cs`

Why this matters:

- `Serializer` currently rebuilds `JsonSerializerOptions` for every serialize and deserialize call.
- REST handlers currently pretty-print many responses, which adds CPU cost and increases payload size.
- This is a low-risk, internal-only improvement.

Tasks:

- [x] Replace per-call `JsonSerializerOptions` construction with cached static/shared option instances.
- [x] Keep separate cached options for compact and pretty output if both are still needed.
- [x] Switch standard REST responses to compact JSON by default.
- [x] Decide whether pretty JSON should remain available behind a debug-only or config-controlled path.
- [x] Audit the REST handlers for `SerializeJson(..., true)` call sites and convert the non-essential ones.
- [ ] Review graph query serialization flow and remove avoidable duplicate serialization where possible.
- [x] Confirm null-handling and enum serialization remain unchanged.

Validation:

- [ ] Response payloads are semantically identical except for whitespace.
- [x] Snapshot or integration tests continue to pass.
- [ ] Benchmark shows lower allocation rate and lower response size on JSON-heavy endpoints.
- [ ] Graph query response still includes execution profile data correctly when enabled.

Exit Criteria:

- [ ] No behavior change visible to clients other than JSON whitespace.
- [ ] Serializer allocations per request are measurably reduced.

Notes:

- Keep API semantics stable. Whitespace-only changes are acceptable.
- Pretty output remains available via `SerializeJson(..., true)` for opt-in/internal use.

---

## Work Item 2 - Request History Pipeline and Body Capture Efficiency
Priority: `P0`
Status: `In progress`
Owner:
PR:
Started: `2026-05-19`
Completed:
Dependencies: Work Item 0

Primary files:

- `src/LiteGraph.Server/Services/RequestHistoryService.cs`
- `src/LiteGraph.Server/API/REST/RestServiceHandler.cs`
- `src/LiteGraph/GraphRepositories/Postgresql/Queries/RequestHistoryQueries.cs`
- `src/LiteGraph/GraphRepositories/Sqlite/Queries/RequestHistoryQueries.cs`
- `src/LiteGraph/GraphRepositories/Postgresql/Implementations/RequestHistoryMethods.cs`
- `src/LiteGraph/GraphRepositories/Sqlite/Implementations/RequestHistoryMethods.cs`

Why this matters:

- The current implementation starts a separate `Task.Run` for each captured request.
- Request and response bodies are copied and re-encoded multiple times.
- Under load, this can create thread-pool pressure and unnecessary allocations.

Tasks:

- [x] Replace per-request fire-and-forget `Task.Run` writes with a bounded in-process queue.
- [x] Use a single background consumer or a small fixed consumer pool.
- [x] Define and document overflow behavior when the queue is full.
- [ ] Add queue depth, dropped-capture count, and failure counters to logs or metrics.
- [x] Reduce request/response body copying where possible.
- [x] Avoid converting response bodies from string to bytes and back unless strictly required.
- [x] Keep request-history persistence failure-isolated so request handling is not blocked.
- [ ] Consider optional insert batching if queue-based single inserts are still expensive.

Validation:

- [ ] Request handling remains non-blocking when request history is enabled.
- [ ] Load test with request history enabled shows better latency stability than the current implementation.
- [ ] Captured request-history entries still contain the expected fields and truncation behavior.
- [ ] Shutdown and disposal behavior flushes or cleanly abandons queued work without hanging the server.

Exit Criteria:

- [x] Request-history capture no longer spawns unbounded background tasks.
- [ ] Allocation pressure and latency spikes are reduced under load.

Notes:

- Preserve current API-visible history semantics.
- Implemented as a bounded `Channel<RequestHistoryDetail>` with a single background consumer and drop logging when the queue fills.

---

## Work Item 3 - Add Composite Edge Indexes for Traversal Paths
Priority: `P1`
Status: `In progress`
Owner:
PR:
Started: `2026-05-19`
Completed:
Dependencies: Work Item 0

Primary files:

- `src/LiteGraph/GraphRepositories/Postgresql/Queries/SetupQueries.cs`
- `src/LiteGraph/GraphRepositories/Sqlite/Queries/SetupQueries.cs`
- `src/LiteGraph/GraphRepositories/Postgresql/Queries/EdgeQueries.cs`
- `src/LiteGraph/GraphRepositories/Sqlite/Queries/EdgeQueries.cs`

Why this matters:

- Current edge indexes are mostly single-column plus `(tenantguid, graphguid)`.
- Hot traversal queries commonly filter on:
  - `tenantguid + graphguid + fromguid`
  - `tenantguid + graphguid + toguid`
  - `tenantguid + graphguid + fromguid + toguid`

Tasks:

- [x] Add composite index for `(tenantguid, graphguid, fromguid)`.
- [x] Add composite index for `(tenantguid, graphguid, toguid)`.
- [x] Add composite index for `(tenantguid, graphguid, fromguid, toguid)` if query plans justify it.
- [x] Apply the same index strategy to both PostgreSQL and SQLite setup scripts.
- [ ] Verify repository initialization creates the new indexes idempotently.
- [ ] Capture before/after query plans for representative edge traversal queries.

Validation:

- [ ] Traversal and edge lookup query plans prefer the new indexes.
- [ ] Route search and edge enumeration benchmarks improve measurably.
- [ ] No migration or initialization failures occur on existing databases.

Exit Criteria:

- [ ] Composite indexes are present and validated in both providers.
- [ ] Traversal-related queries show lower scan cost or lower latency.

Notes:

- Keep this additive. Do not remove existing indexes until query-plan evidence justifies it.
- Added composite edge indexes to both provider setup scripts; query-plan capture is still pending.

---

## Work Item 4 - Remove Unnecessary Read-Only Transactions
Priority: `P1`
Status: `In progress`
Owner:
PR:
Started: `2026-05-19`
Completed:
Dependencies: Work Item 0

Primary files:

- `src/LiteGraph/GraphRepositories/Postgresql/PostgresqlGraphRepository.Execution.cs`
- `src/LiteGraph/GraphRepositories/Sqlite/SqliteGraphRepository.cs`
- `src/LiteGraph/GraphRepositories/Postgresql/Implementations`
- `src/LiteGraph/GraphRepositories/Sqlite/Implementations`

Why this matters:

- Several pure read/count/statistics paths call `ExecuteQueryAsync(..., true)` or equivalent.
- In the PostgreSQL repository, `true` explicitly opens a database transaction for that call.
- For single-query read-only operations, this is usually wasted overhead.

Tasks:

- [ ] Audit all read-only `SELECT`, count, and statistics methods using `isTransaction=true`.
- [x] Change them to non-transactional execution where no transactional guarantee is actually required.
- [x] Pay special attention to statistics and count methods in graph and tenant implementations.
- [ ] Document any read-only paths that intentionally remain transactional and why.
- [ ] Re-run the benchmark suite after each cluster of changes rather than changing everything blindly.

Validation:

- [x] Result correctness is unchanged.
- [ ] Read-only latency improves or connection overhead decreases in benchmarks.
- [ ] No consistency-sensitive read path was accidentally weakened.

Exit Criteria:

- [ ] Unnecessary read-only transaction wrappers are removed.
- [ ] Remaining transaction-wrapped read paths are explicitly justified.

Notes:

- Keep this conservative. If a path depends on snapshot-like consistency, record that rationale.
- Graph and tenant statistics/count paths in both providers now execute without unnecessary read-only transaction wrappers.

---

## Work Item 5 - SQLite Concurrency and Connection Strategy
Priority: `P1`
Status: `In progress`
Owner:
PR:
Started: `2026-05-19`
Completed:
Dependencies: Work Items 0 and 4

Primary files:

- `src/LiteGraph/GraphRepositories/Sqlite/SqliteGraphRepository.cs`

Why this matters:

- The synchronous SQLite execution path currently serializes all queries behind a single lock.
- Disk-backed SQLite uses `Pooling=false`.
- The async and sync execution paths are not aligned in how much concurrency they allow.

Tasks:

- [x] Split transaction/shared-connection locking from non-transaction per-connection execution.
- [x] Keep locking only where a shared connection or shared transaction actually requires it.
- [x] Align sync and async execution-path behavior where possible.
- [ ] Benchmark with connection pooling enabled versus disabled for disk-backed SQLite.
- [ ] Preserve correctness for in-memory mode, which uses shared connection state.
- [ ] Add targeted concurrency tests for read-heavy and mixed read/write workloads.
- [x] Ensure `docker/compose.yaml` persists the full host directory containing `docker/litegraph.db` rather than only the primary `.db` file, so `*.db`, `*.db-wal`, `*.db-shm`, and vector index artifacts survive container restarts.
- [x] Ensure a valid seeded placeholder `docker/litegraph.db` exists for directory-mounted deployments, with default data generated by `LiteGraph.SampleDatabase`.

Validation:

- [ ] Multi-request SQLite benchmarks show improved concurrency and better throughput.
- [ ] In-memory mode still behaves correctly.
- [ ] Active graph transactions still serialize correctly and do not corrupt state.
- [ ] Compose-backed SQLite deployments retain the primary database file and any SQLite sidecar files across restarts.
- [x] `LiteGraph.SampleDatabase` seeds `docker/litegraph.db` successfully against the current schema.

Exit Criteria:

- [x] Non-transaction disk-backed SQLite work is no longer needlessly serialized.
- [ ] Any pooling change is supported by benchmark evidence and correctness testing.

Notes:

- This is likely the highest-value SQLite-specific improvement.
- The Compose deployment now keeps the legacy `docker/litegraph.db` path while persisting generated artifacts such as `docker/indexes/`, `litegraph.db-wal`, and `litegraph.db-shm` through a full-directory mount.

---

## Work Item 6 - Bulk Subordinate Hydration to Remove N+1 Patterns
Priority: `P1`
Status: `In progress`
Owner:
PR:
Started: `2026-05-19`
Completed:
Dependencies: Work Item 0

Primary files:

- `src/LiteGraph/Client/Implementations/NodeMethods.cs`
- `src/LiteGraph/Client/Implementations/EdgeMethods.cs`
- `src/LiteGraph/Client/Implementations/GraphMethods.cs`
- Relevant repository methods under `src/LiteGraph/GraphRepositories/*/Implementations`

Why this matters:

- `includeSubordinates` currently causes per-object label, tag, and vector fetches.
- On list and enumeration paths, that creates an N+1 pattern.

Tasks:

- [x] Inventory all paths that hydrate labels, tags, and vectors one object at a time.
- [ ] Add bulk repository helpers for fetching subordinates for a set of node or edge GUIDs.
- [x] Group bulk-loaded labels, tags, and vectors in memory by parent GUID.
- [x] Refactor node and edge population paths to use bulk subordinate loading on list/enumeration flows.
- [x] Audit graph-level subordinate hydration for similar patterns.
- [ ] Preserve current ordering and null/empty semantics for subordinate collections.

Validation:

- [ ] Query count drops materially for `IncludeSubordinates=true` list flows.
- [ ] Returned labels, tags, and vectors are identical to the current implementation.
- [ ] Benchmarks show improved latency on list endpoints and large enumerations.

Exit Criteria:

- [x] N+1 subordinate loading is removed from the main node and edge list paths.
- [ ] Query count and latency improvements are documented.

Notes:

- Favor bulk fetch per page or batch rather than per entire dataset to control memory growth.
- Current implementation batches subordinate reads per `(tenantGuid, graphGuid)` group and reassigns them in memory.

---

## Work Item 7 - Make Continuation Token Paging Truly Cursor-Based
Priority: `P1`
Status: `In progress`
Owner:
PR:
Started: `2026-05-19`
Completed:
Dependencies: Work Item 0

Primary files:

- `src/LiteGraph/GraphRepositories/Postgresql/Queries`
- `src/LiteGraph/GraphRepositories/Sqlite/Queries`
- `src/LiteGraph/GraphRepositories/Postgresql/Implementations`
- `src/LiteGraph/GraphRepositories/Sqlite/Implementations`
- `src/LiteGraph.Server/API/REST/RestServiceHandler.cs`

Why this matters:

- The API already supports continuation tokens.
- Some page builders still append `OFFSET`, which defeats the main scalability benefit of cursor-style pagination on deep pages.

Tasks:

- [x] Audit all `GetRecordPage` and related query builders in both providers.
- [x] When `ContinuationToken` is present, stop relying on `OFFSET` for page advancement.
- [x] Keep `Skip` behavior unchanged when no continuation token is supplied.
- [x] Define precedence rules if both `Skip` and `ContinuationToken` are provided.
- [ ] Verify ordering is stable enough for cursor-based paging to avoid duplicates and gaps.
- [ ] Update internal comments and docs to reflect the intended paging behavior.

Validation:

- [ ] Deep-page latency improves compared to offset-only paging.
- [ ] No duplicate or missing records appear across page boundaries.
- [ ] Existing clients can continue using the same request fields.

Exit Criteria:

- [x] Continuation-token flows no longer degrade like offset scans on deep pages.
- [ ] Paging semantics are documented and tested.

Notes:

- This is an internal execution improvement, not an API change.
- If both `Skip` and `ContinuationToken` are supplied, paging now uses `OFFSET` only when the continuation marker is absent.

---

## Work Item 8 - Request-Scoped Memoization for Query Execution and Traversal
Priority: `P2`
Status: `Not started`
Owner:
PR:
Started:
Completed:
Dependencies: Work Item 0

Primary files:

- `src/LiteGraph/Client/Implementations/QueryExecutionEngine.cs`
- `src/LiteGraph/GraphRepositories/Postgresql/Implementations/NodeMethods.cs`
- `src/LiteGraph/GraphRepositories/Sqlite/Implementations/NodeMethods.cs`
- `src/LiteGraph/LiteGraphClient.cs`

Why this matters:

- Traversal and graph query logic repeatedly calls `_Repo.*.ReadByGuid` inside loops.
- Existing client-level LRU caches do not help when the query engine directly uses repository methods.

Tasks:

- [ ] Add request-scoped memoization dictionaries for nodes and edges at minimum.
- [ ] Consider label, tag, and vector memoization if benchmarks show repeated lookups there as well.
- [ ] Refactor repeated `ReadByGuid` loops in `QueryExecutionEngine` to go through memoized helpers.
- [ ] Refactor DFS route traversal to reuse memoized node lookups.
- [ ] Keep memoization scoped to the operation so no global cache invalidation complexity is introduced.

Validation:

- [ ] Repeated traversal/query workloads show fewer repository reads.
- [ ] Query and traversal outputs remain identical.
- [ ] Memory growth stays bounded per request.

Exit Criteria:

- [ ] Duplicate repository lookups are significantly reduced on traversal-heavy paths.
- [ ] Results remain correct and deterministic.

Notes:

- This is a medium-effort change with good upside on graph-heavy workloads.
- Deferred for discussion per the `2026-05-19` request before implementation begins.

---

## Work Item 9 - Repository-Global Graph Transaction Redesign
Priority: `P3`
Status: `Not started`
Owner:
PR:
Started:
Completed:
Dependencies: Work Items 0 through 8

Primary files:

- `src/LiteGraph/GraphRepositories/Postgresql/PostgresqlGraphRepository.cs`
- `src/LiteGraph/GraphRepositories/Postgresql/PostgresqlGraphRepository.Execution.cs`
- `src/LiteGraph/GraphRepositories/Sqlite/SqliteGraphRepository.cs`
- Transaction-related client code under `src/LiteGraph/Client/Implementations`

Why this matters:

- Both repositories keep graph transaction state on the repository instance.
- Only one active graph transaction can exist per repository instance.
- This is a real concurrency ceiling, but it is not a low-risk quick win.

Tasks:

- [ ] Design a request-scoped or operation-scoped transaction model.
- [ ] Decide how transaction handles will be associated with client operations internally.
- [ ] Preserve current public transaction semantics.
- [ ] Define concurrency guarantees and error behavior before implementation.
- [ ] Build focused tests for overlapping graph transactions.

Validation:

- [ ] Multiple concurrent graph transactions can execute safely where expected.
- [ ] No transaction leakage or cross-request contamination occurs.
- [ ] Existing transaction APIs and semantics remain intact.

Exit Criteria:

- [ ] Repository-global mutable transaction state is removed or isolated behind a safe internal model.

Notes:

- Do not start this item until the lower-risk improvements are complete and measured.
- Deferred for discussion per the `2026-05-19` request.

---

## Work Item 10 - Settings and Documentation Cleanup
Priority: `P3`
Status: `Done`
Owner:
PR:
Started: `2026-05-19`
Completed: `2026-05-19`
Dependencies: none

Primary files:

- `src/LiteGraph.Server/Classes/LiteGraphSettings.cs`
- Any related server settings docs

Why this matters:

- `MaxConcurrentOperations` exists in settings and its comment is confusing.
- The setting does not currently appear to be enforced in the examined code paths.
- This is not a major performance win by itself, but it removes confusion and prevents bad tuning assumptions.

Tasks:

- [x] Confirm whether `MaxConcurrentOperations` is intentionally unused.
- [x] Fix the XML comment and any user-facing docs so they describe behavior accurately.
- [x] Decide whether the setting should remain documentation-only, be removed later, or be wired to an actual limiter in a separate task.

Validation:

- [x] The setting description is accurate.
- [x] Operators cannot misread the setting and tune the server incorrectly.

Exit Criteria:

- [x] Configuration behavior and documentation are aligned.

Notes:

- `MaxConcurrentOperations` remains as a compatibility setting only; wiring it to an actual limiter should be handled as a separate future task if desired.

---

## Suggested Milestones

### Milestone A - Cheap, Low-Risk Wins
- [ ] Work Item 0 complete
- [ ] Work Item 1 complete
- [ ] Work Item 2 complete
- [ ] Work Item 4 complete

Expected outcome:

- Lower JSON overhead
- More stable latency when request history is enabled
- Fewer unnecessary DB transactions

### Milestone B - Query and Index Efficiency
- [ ] Work Item 3 complete
- [ ] Work Item 6 complete
- [ ] Work Item 7 complete

Expected outcome:

- Better traversal and list performance
- Lower query counts
- Better deep-page behavior

### Milestone C - Concurrency and Traversal Depth
- [ ] Work Item 5 complete
- [ ] Work Item 8 complete

Expected outcome:

- Better SQLite scalability
- Better traversal/query performance on graph-heavy workloads

### Milestone D - Stretch
- [ ] Work Item 9 complete
- [x] Work Item 10 complete

Expected outcome:

- Better transaction concurrency model
- Cleaner operator guidance

---

## Benchmark Log

### Run 1
- Date: `2026-05-19`
- Branch/PR:
- Database: SQLite (`docker/litegraph.db`)
- Dataset: `LiteGraph.SampleDatabase` default seed (`10` nodes, `10` edges, `10` vectors)
- Request history: Not benchmarked in this run
- Scenario: Correctness validation after implementation updates
- Before: Not captured on this working tree before implementation began
- After: `dotnet build src/LiteGraph.sln -c Debug --no-restore` succeeded, `dotnet test src/LiteGraph.sln -c Debug --no-build` passed, and `dotnet run --project src/LiteGraph.SampleDatabase -- --database docker/litegraph.db` succeeded
- Notes: This run validates build/test/schema health and the seeded Docker SQLite placeholder; it is not a latency or throughput benchmark.

### Run 2
- Date:
- Branch/PR:
- Database:
- Dataset:
- Request history:
- Scenario:
- Before:
- After:
- Notes:

### Run 3
- Date:
- Branch/PR:
- Database:
- Dataset:
- Request history:
- Scenario:
- Before:
- After:
- Notes:
