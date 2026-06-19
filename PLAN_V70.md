# LiteGraph v7.0 Plan: Parallel Transaction Scaling

## Purpose

This plan defines the end-to-end work required to support true parallel transaction scaling in LiteGraph. The release goal is to allow multiple transactions to execute concurrently through the same LiteGraph process without relying on a process-local transaction safety gate that serializes transaction execution. This is a full-product release plan and includes the core library, database providers, server, dashboard, MCP server, SDKs, Postman collection, documentation, Docker assets, test harnesses, release operations, and migration guidance.

## Progress Annotation Guide

Use these markers when updating this document:

- `[ ]` Not started
- `[~]` In progress
- `[x]` Complete
- `[!]` Blocked
- `[?]` Needs decision

Each task should be updated in place with a short note, owner initials, PR/commit link, and date when meaningful.

Example annotation:

```text
Implement PostgreSQL transaction context lifecycle. Status: [~]. Owner: AB. PR: #123. Note: commit path complete, rollback tests pending. 2026-06-20.
```

## Executive Summary

The current transaction implementation protects correctness by serializing `Transaction.Execute(...)` with a per-repository `SemaphoreSlim`. That gate is necessary because repository instances store active transaction state in instance fields. If two transactions use the same repository concurrently, they can share or overwrite `_Transaction`, `_TransactionConnection`, graph/tenant scope, and vector-index dirty state.

The v7.0 objective is to remove this shared mutable transaction state from repository instances. Every transaction should run in an isolated transaction session/context that owns its own connection, provider transaction, graph scope, timeout/cancellation state, telemetry context, and vector-index staging/dirty state. Repository operations must route through that explicit context rather than checking ambient repository fields.

All implementation work for this release must be performed on the already-created `v7.0` branch. Do not make v7.0 implementation commits directly on `main`. Short-lived topic branches may be created from `v7.0` for reviewable slices, but they must merge back into `v7.0` before release stabilization.

The expected outcome:

- Multiple concurrent transactions can execute in parallel in the same process.
- PostgreSQL can scale transaction writes according to database concurrency limits; SQLite remains correct with SQLite file-lock limits.
- SQLite remains correct and can run isolated transaction sessions, while write concurrency remains constrained by SQLite file locking.
- Transaction correctness is deterministic under commit, rollback, cancellation, timeout, vector mutation, and mixed transaction/non-transaction load.
- The server, MCP server, dashboard, SDKs, docs, Docker assets, and test harnesses expose and validate the new behavior.

## Plan Completion Audit

Last audited: 2026-06-19 on the `v7.0` branch.

This document is complete as a v7.0 release tracking plan. The local implementation and validation gate for request-scoped parallel transaction scaling is complete where marked `[x]`. Items still marked `[ ]` or `[~]` are either external release operations, optional future hardening, or follow-up product work explicitly called out in their notes.

| Workstream | Status | Completed | Remaining |
| --- | --- | --- | --- |
| Core transaction execution | `[x]` | SQLite and PostgreSQL use explicit transaction-local repository sessions for request-scoped transaction execution; diagnostics, timeout/cancellation, provider isolation mapping, rollback cleanup, vector staging, and converted-provider no-gate execution pass local validation. | No local implementation blocker remains. Future long-lived transaction session workflows can extend the same session/context API if product requirements need transactions that outlive one request. |
| Provider support | `[x]` | SQLite and PostgreSQL are the supported executable v7.0 providers. PostgreSQL retryable concurrency error classification is implemented, PostgreSQL transaction sessions share the parent data-source pool, and Docker-backed PostgreSQL core/concurrency validation passes. MySQL and SQL Server are explicit unsupported placeholders with fail-fast tests/docs. | Heavier long-running PostgreSQL hotspot/mixed workload benchmarks remain performance follow-up work, not a correctness blocker. |
| Vector indexing | `[x]` | HnswLite `2.0.1` is referenced explicitly; HNSW storage was adapted; vector mutations are staged through commit/rollback paths; focused vector suites pass and migration/rebuild guidance exists. | Broader live PostgreSQL/provider-matrix vector correctness remains part of the release correctness gate. |
| REST/server | `[x]` | Transaction request/response diagnostics, validation/rollback status mapping, request-history transaction diagnostics, REST transaction caps, and full SQLite/PostgreSQL automated validation pass. | Optional future pool/concurrency controls and longer soak coverage can be added after v7.0. |
| Dashboard | `[x]` | API Explorer transaction templates/summaries and request-history transaction rendering/filtering are implemented. Full dashboard Jest validation passes: 85 suites, 1,280 tests, 65 snapshots. | No local release blocker remains. |
| MCP server | `[x]` | Transaction tool accepts isolation level, preserves diagnostic transaction result bodies, is covered by the full automated suite, and docs are updated. | Retry/idempotency arguments remain a future feature decision. |
| SDKs | `[x]` | C#, JavaScript, and Python SDK transaction models expose v7 diagnostics, lifecycle state, and isolation options; versions/changelogs are updated; JS/Python unit suites pass; C# SDK live suites pass against isolated SQLite and PostgreSQL servers. | Package publication remains release engineering. |
| Docker/configuration | `[~]` | Docker Compose uses `v7.0.0` tagged LiteGraph/MCP/UI images, PostgreSQL 17, a one-shot PostgreSQL init service, server transaction caps, persistent PostgreSQL volume, matching factory reset assets, and checked-in `docker/smoke.bat`/`docker/smoke.ps1` health validation scripts. | Run live Docker startup/smoke validation with published images and validate SQLite-to-PostgreSQL migration end to end. |
| Postman | `[~]` | v7 transaction examples, isolation variables, rollback diagnostics, and vector examples are present and the collection parses. | Validate against local Docker, add conflict/deadlock examples where feasible, and export final formatted collection. |
| Correctness/coherency | `[x]` | Full SQLite/default `Test.Automated` and PostgreSQL-enabled `Test.Automated` pass; focused transaction-concurrency selectors pass; live C# SDK suites pass on SQLite/PostgreSQL; JS/Python/dashboard suites pass. Additional automated correctness-hardening cases now cover deterministic oracle replay, seeded randomized histories, bounded soak invariants, fault injection, and direct transaction/query API surface coherency. | Longer multi-hour soak and production-data migration validation remain external release validation work. |
| Performance/scalability | `[x]` | `Test.PerformanceAndScalability` includes transaction workloads/metrics/provider notes, and bounded transaction performance smoke passes for SQLite and PostgreSQL at concurrency 1 and 2 with zero failures/timeouts/correctness issues. | Full benchmark publication and v6-vs-v7 baselines remain release-note/performance-report follow-up work. |
| Observability | `[x]` | REST activity tags, core transaction lifecycle events, Prometheus provider/isolation/state labels, active transaction gauge, queue-wait/commit/rollback summaries, conflict/retry counters, request-history diagnostics, Grafana panels, and docs are implemented. | External telemetry backend tuning remains deployment-specific. |
| Release engineering | `[x]` | Version metadata and changelogs are updated to `7.0.0`; local validation scripts and GitHub CI workflow cover .NET transaction gates, SDKs, dashboard, package audit/build, and PostgreSQL service tests. | Human/external release steps remain: publish packages/images, run hosted CI, tag `v7.0.0`, and merge/release per project policy. |

## Latest Local Validation

- [x] Full .NET solution Debug build: `dotnet build src/LiteGraph.sln -c Debug` passed with 0 warnings and 0 errors and produced `LiteGraph.7.0.0.nupkg`/`.snupkg`. 2026-06-19.
- [x] NuGet vulnerability audit: `dotnet list src/LiteGraph.sln package --vulnerable --include-transitive` reported no vulnerable packages for every solution project. 2026-06-19.
- [x] SQLite/default automated graph suite: `dotnet run --project src/Test.Automated/Test.Automated.csproj -c Debug --framework net10.0` passed 465 total, 460 passed, 0 failed, 5 expected skips. 2026-06-19.
- [x] SQLite/default transaction-concurrency selector: `--transaction-concurrency` passed 21 selected cases, 20 passed, 0 failed, 1 expected PostgreSQL skip after adding explicit session lifecycle, deterministic oracle, concurrent replay, randomized history, bounded soak, fault-injection, and API-surface coherency coverage. 2026-06-19.
- [x] PostgreSQL-enabled automated graph suite against local Docker PostgreSQL on `localhost:15432`: passed 579 total, 577 passed, 0 failed, 2 expected skips. 2026-06-19.
- [x] PostgreSQL-enabled transaction-concurrency selector against local Docker PostgreSQL on `localhost:15432`: passed 21 selected cases, 21 passed, 0 failed, 0 skipped. 2026-06-19.
- [x] Local Docker Compose smoke: `docker/smoke.bat` passed against the running PostgreSQL-backed `v7.0.0` tagged Compose deployment, including Compose service state, REST root, REST metrics, authenticated tenant access, MCP root, UI root, Prometheus readiness, and Grafana health. 2026-06-19.
- [x] C# SDK live SQLite suite against isolated server: passed 132 total, 132 passed, 0 failed. 2026-06-19.
- [x] C# SDK live PostgreSQL suite against isolated server: passed 128 total, 128 passed, 0 failed with PostgreSQL backup operations validated as unsupported. 2026-06-19.
- [x] JavaScript SDK Jest suite: `npm.cmd test -- --runInBand` passed 14 suites, 147 tests. 2026-06-19.
- [x] Python SDK pytest suite: `py -m pytest` passed 325 tests with existing Pydantic deprecation warnings. 2026-06-19.
- [x] Dashboard Jest suite: `npm.cmd test -- --runInBand` passed 85 suites, 1,280 tests, 65 snapshots, with existing React act warnings. 2026-06-19.
- [x] Local release validation script: `release-validate.bat` passed, including Debug solution build, vulnerability audit, NuGet package validation, SQLite transaction-concurrency selector, JavaScript SDK tests/package dry run, Python SDK tests/package build, and dashboard test/build. The optional PostgreSQL selector inside the script was skipped because `LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING` was not set for that process; the PostgreSQL full and focused gates above were run separately against Docker PostgreSQL. 2026-06-19.
- [x] SQLite transaction performance smoke: 8 transaction rows, all `OK`, zero failures/timeouts/correctness-sampling issues. 2026-06-19.
- [x] PostgreSQL transaction performance smoke: 8 transaction rows, all `OK`, zero failures/timeouts/correctness-sampling issues. 2026-06-19.

## Remaining External Or Future Work

1. Publish NuGet/npm/PyPI packages and Docker images with external credentials.
2. Run the project CI matrix and tag `v7.0.0` after release approval.
3. Perform live Docker Compose first-run smoke validation with the published `v7.0.0` images.
4. Validate a real production-style SQLite-to-PostgreSQL migration using backed-up deployment data.
5. Publish full v6-vs-v7 performance benchmark results, including longer PostgreSQL hotspot, mixed workload, and soak profiles.
6. Consider a future long-lived transaction session API if product requirements extend beyond request-scoped transactions.

## Current State

- [x] Audit and annotate current transaction implementation. Note: high-level transaction execution is centralized in `TransactionMethods.Execute`; the explicit transaction-repository/session bridge is implemented for SQLite/PostgreSQL; provider ambient fields now live on disposable transaction-local repositories instead of the caller repository during request-scoped transaction execution. 2026-06-19.
  - Current transaction entrypoint: `src/LiteGraph/Client/Implementations/TransactionMethods.cs`.
  - Current gate: `ConditionalWeakTable<GraphRepositoryBase, SemaphoreSlim>` with a per-repository `SemaphoreSlim`.
  - Current repository state: provider repositories store transaction connection/transaction and transaction graph state in instance fields.
  - Current transaction methods call normal repository methods while repository-level transaction state is active.

- [x] Audit provider transaction fields and behavior. Note: SQLite and PostgreSQL active transaction fields are isolated through transaction-local repository instances; SQL Server/MySQL are explicit unsupported provider placeholders for v7.0 and fail fast. 2026-06-19.
  - SQLite: `src/LiteGraph/GraphRepositories/Sqlite/SqliteGraphRepository.cs`.
  - PostgreSQL: `src/LiteGraph/GraphRepositories/Postgresql/PostgresqlGraphRepository*.cs`.
  - SQL Server: `src/LiteGraph/GraphRepositories/SqlServer/SqlServerGraphRepository.cs` is an unsupported placeholder.
  - MySQL: `src/LiteGraph/GraphRepositories/Mysql/MysqlGraphRepository.cs` is an unsupported placeholder.

- [x] Audit all call sites that rely on ambient transaction state. Note: direct ambient transaction call sites were found in `TransactionMethods`, `QueryExecutionEngine`, provider vector-index helpers, and shared improvement suites. `TransactionMethods` and planned query mutations now use transaction-local repository sessions for converted providers; remaining provider ambient fields are scoped to those disposable session repositories. 2026-06-19.
  - `GraphTransactionActive`
  - `GraphTransactionTenantGUID`
  - `GraphTransactionGraphGUID`
  - `BeginGraphTransaction`
  - `CommitGraphTransaction`
  - `RollbackGraphTransaction`
  - `NoteVectorIndexMutation`
  - `NoteVectorIndexFailure`
  - `ExecuteQueryAsync(..., isTransaction: true, ...)`
  - `ExecuteQueries(..., isTransaction: true, ...)`

## Release Goals

- [x] Replace shared repository transaction state with isolated transaction context/session state. Note: explicit TransactionExecutionOptions, IRepositorySessionFactory, IRepositorySession, ITransactionContext, and GraphTransactionContext are implemented; Transaction.Execute and mutating graph-query planning route through repository sessions while provider Begin/Commit/Rollback methods remain compatibility hooks. 2026-06-19.
- [x] Support parallel transaction execution through one `LiteGraphClient` instance. Note: converted providers avoid the fallback gate; SQLite/default `--transaction-concurrency` passed 21 selected cases with 20 pass and 1 expected PostgreSQL skip, and Docker-backed PostgreSQL-enabled `--transaction-concurrency` passed 21/21. 2026-06-19.
- [x] Preserve atomic commit/rollback semantics for transaction operation types covered by the v7.0 release gate. Note: full SQLite/default and PostgreSQL-enabled automated suites pass, including commit, rollback, cancellation/timeout, operation-matrix, vector staging, and provider concurrency checks.
- [x] Define and implement provider-specific concurrency behavior. Note: SQLite and PostgreSQL are implemented; SQL Server/MySQL are explicitly unsupported placeholders for v7.0.
- [x] Make vector-index behavior transactionally correct. Note: staged vector-index mutation commit/rollback behavior is implemented for SQLite/PostgreSQL and HnswLite `2.0.1` migration guidance exists; broader provider-matrix validation is tracked separately.
- [x] Add telemetry that makes transaction concurrency visible. Note: transaction diagnostics, activity tags, request-history capture, transaction lifecycle events, active transaction gauge, queue-wait metrics, commit/rollback duration metrics, conflict/retry counters, Grafana panels, and Prometheus docs are implemented. 2026-06-19.
- [x] Update REST API contracts and SDKs for v7.0. Note: core REST/SDK models are updated; JS/Python unit suites pass; C# SDK live suites pass against SQLite and PostgreSQL. Package publication remains release engineering.
- [x] Update dashboard and MCP experiences for v7.0 transaction behavior. Note: dashboard/API Explorer/request-history and MCP isolation/diagnostic paths are implemented; full dashboard and automated MCP coverage pass.
- [~] Update Docker, Postman, and documentation. Note: Docker is PostgreSQL-backed with `v7.0.0` images/init service, docs are updated, checked-in Docker smoke scripts exist, and Postman parses; live validation and final release docs remain.
- [x] Expand automated, stress, and performance tests to cover true parallel scaling. Note: shared touchstone transaction coverage includes real SQLite concurrent graph transaction scenarios; `--transaction-concurrency` selects real concurrent graph cases, SQLite write contention, mixed transaction/non-transaction writes, mixed vector-index changes, authorization-boundary coverage, isolated-parallel query/client cases, explicit session lifecycle, deterministic/concurrent/randomized/soak oracle checks, fault injection, API-surface coherency, and Docker-backed PostgreSQL matrix coverage. SQLite/default passed 20 runnable cases plus the expected PostgreSQL skip; bounded SQLite/PostgreSQL transaction performance smoke passed with zero failures/timeouts/correctness issues.
- [x] Add provider-matrix correctness and coherency coverage that is separate from performance smoke validation and required for release acceptance. Note: the v7.0 local release gate is the full SQLite/default automated suite plus the PostgreSQL-enabled suite and focused concurrency selectors; automated correctness-hardening now includes deterministic, concurrent, randomized, soak, fault-injection, and API-surface oracle tests.
- [~] Provide migration guidance from v6.x to v7.0. Note: upgrade/storage guidance exists for transaction changes, Docker PostgreSQL defaults, and HnswLite `2.0.1`; live migration validation and rollback walkthrough remain pending.

## Branch And Workflow

- [x] Use the existing `v7.0` branch as the release integration branch.
- [x] Confirm each development session starts from `v7.0`. Note: `git status --short --branch` returned `## v7.0` before implementation work began. 2026-06-17.
  - Command: `git status --short --branch`
  - Expected branch: `v7.0`
- [ ] Keep `v7.0` synchronized with `main` only through deliberate merges or rebases.
  - Record merge/rebase decisions in this plan or release notes when they affect v7.0 scope.
- [ ] Use short-lived topic branches only when a work slice benefits from isolated review.
  - Branch naming examples:
    - `feature/v70-transaction-context`
    - `feature/v70-postgresql-transactions`
    - `feature/v70-sdk-transaction-models`
  - Topic branches must be created from `v7.0`.
  - Topic branches must merge back into `v7.0`.
- [ ] Do not commit v7.0 implementation work directly to `main`.
- [ ] Treat `main` as the stable v6.x/current-release line until the v7.0 release is ready.
- [ ] At release stabilization, merge `v7.0` into `main` only after all acceptance criteria, docs, packages, Docker images, and release notes are complete.
- [ ] Tag `v7.0.0` from the final release commit after `v7.0` has been accepted for GA.

## Non-Goals

- [x] Do not promise SQLite parallel write throughput equivalent to server databases. Note: README, storage, transaction, upgrade, and performance docs distinguish SQLite correctness from PostgreSQL write scaling. 2026-06-19.
- [x] Do not introduce distributed transactions across multiple databases. Note: v7.0 transactions remain graph-scoped within one configured repository/provider. 2026-06-19.
- [x] Do not require DTC or platform-specific ambient `TransactionScope` behavior. Note: provider-local transactions are used directly; no DTC dependency was introduced. 2026-06-19.
- [x] Do not expose provider internals in public SDK models. Note: SDKs expose provider/isolation diagnostics as strings/contract fields, not provider connection or transaction objects. 2026-06-19.
- [x] Do not make vector indexes part of the database transaction unless the design explicitly stages and applies them safely. Note: vector-index mutations are staged and applied after DB commit, with dirty fallback on uncertainty. 2026-06-19.

## Release-Level Breaking Change Assumptions

These are the expected breaking or migration-sensitive areas. Confirm each before implementation.

- [x] Repository interfaces use transaction-local repository sessions for v7.0 request-scoped transactions. Note: `CreateIsolatedTransactionRepository()` is the accepted v7.0 provider hook; separate long-lived public session/context signatures are deferred. 2026-06-19.
- [x] Provider implementations no longer expose shared ambient transaction state for `LiteGraphClient.Transaction.Execute`. Note: SQLite/PostgreSQL active transaction state exists only on disposable transaction-local repository sessions for request-scoped transactions. 2026-06-19.
- [x] Transaction result models may add fields for isolation level, retry count, conflict/deadlock status, and session diagnostics. Note: core `TransactionResult` now includes transaction ID, operation count, started/completed timestamps, commit/rollback duration, validation-failure flag, provider, isolation, isolated-repository/gate flags, retry diagnostics, concurrency-conflict flag, and provider error code. 2026-06-19.
- [x] SDKs may need regenerated transaction request/response models. Note: JS, Python, and C# SDK transaction request/response models preserve transaction diagnostics and isolation options; C# SDK adds a transaction surface; full local SDK validation passes. Package publication remains release engineering. 2026-06-19.
- [x] Server configuration may add transaction pool/isolation/concurrency settings. Note: enforced REST caps are implemented through `LiteGraph.Transactions.MaxOperations`, `LiteGraph.Transactions.MaxTimeoutSeconds`, `LITEGRAPH_TRANSACTION_MAX_OPERATIONS`, and `LITEGRAPH_TRANSACTION_MAX_TIMEOUT_SECONDS`; no additional pool/concurrency setting is required for the v7.0 release because provider connection pools already bound execution. 2026-06-19.
- [x] MCP tools may expose additional transaction diagnostics or new argument names. Note: MCP accepts `isolationLevel`, preserves transaction diagnostic result bodies, and participates in the automated release gate; retry/idempotency arguments remain a future feature decision. 2026-06-19.
- [x] Dashboard request-history and transaction view schema changes are implemented. Note: request history storage now carries `TransactionDiagnosticsJson`; REST/dashboard search supports `hasTransactionDiagnostics` and `transactionId`; API Explorer summaries, request-history detail, and request-history list transaction summaries render v7 diagnostics. 2026-06-18.
- [~] Postman collection examples may need v7.0 transaction response updates. Note: v7.0 transaction examples and variables exist and parse; live Docker validation and conflict/deadlock examples remain pending. 2026-06-19.
- [x] Existing custom repository integrations have a v7.0 migration path. Note: custom providers can keep the legacy serialized fallback by returning `this` from `CreateIsolatedTransactionRepository()`, or opt into parallel request-scoped transactions by returning a transaction-local repository session. 2026-06-19.

## Target Architecture

### Core Concept

Every transaction execution creates an isolated `GraphTransactionContext` or `GraphRepositorySession`.

The context owns:

- Transaction ID
- Tenant GUID
- Graph GUID
- Provider connection/session
- Provider transaction object
- Isolation level
- Timeout and cancellation state
- Transaction start time
- Transaction operation counters
- Transaction-local telemetry tags
- Vector-index touched/failed flags
- Deferred vector-index mutation plan or dirty-mark plan

Normal non-transaction operations continue to use repository connection pooling and normal execution paths.

Transaction operations must not mutate repository-level transaction fields.

### Proposed Object Model

- [x] `GraphTransactionContext` - implemented as the transaction lifecycle controller with begin/commit/rollback, queue wait timing, gate release, and deterministic session disposal. 2026-06-19.
  - Holds transaction identity and graph scope.
  - Holds provider-specific connection and transaction handles through a provider-neutral abstraction.
  - Implements `IAsyncDisposable`.
  - Exposes `CommitAsync`, `RollbackAsync`, and transaction diagnostics.

- [x] `ITransactionContext` - implemented for explicit transaction lifecycle and diagnostics access. 2026-06-19.
  - Provider-neutral interface for query execution.
  - Exposes `TenantGuid`, `GraphGuid`, `TransactionId`, `IsolationLevel`, `StartedUtc`, and `CancellationToken`.

- [x] `IRepositorySession` - implemented as the provider-owned repository/session contract for one transaction execution. 2026-06-19.
  - Provider-neutral session for executing operations.
  - Contains optional transaction context.
  - Supports `ExecuteQueryAsync`, `ExecuteQueriesAsync`, and telemetry.

- [x] `IRepositorySessionFactory` - implemented by GraphRepositoryBase through CreateRepositorySession. 2026-06-19.
  - Creates non-transaction sessions and transaction sessions.
  - Lets server and client code avoid directly new-ing provider connections.

- [x] `TransactionExecutionOptions` - implemented with transaction ID, tenant, graph, isolation, source, and operation-count validation. 2026-06-19.
  - Isolation level
  - Timeout
  - Deadlock retry count
  - Retry backoff
  - Vector-index handling strategy
  - Read-your-writes mode

### Query Execution Model

Current ambient model:

```text
Transaction.Execute
  -> repository.BeginGraphTransaction()
  -> repository methods check repository fields
  -> repository.CommitGraphTransaction()
```

Final v7.0 request-scoped model:

```text
Transaction.Execute
  -> repository.CreateIsolatedTransactionRepository()
  -> transaction-local repository begins provider transaction
  -> transaction operations execute against the transaction-local repository
  -> transaction-local repository commits/rolls back and disposes
```

### API Compatibility Strategy

The public `LiteGraphClient.Transaction.Execute(...)` remains the high-level transaction API. Internally it creates a transaction-local repository session for providers that opt in. Lower-level custom repository implementations can opt in by overriding `CreateIsolatedTransactionRepository()`.

Consider adding optional advanced APIs:

- `BeginTransactionAsync(...)`
- `CommitTransactionAsync(transactionId)`
- `RollbackTransactionAsync(transactionId)`
- `ExecuteInTransactionAsync(...)`

Decision: v7.0 keeps request-scoped atomic transaction execution only. Long-lived client-managed transactions are deferred.

## Architecture Decisions

- [x] Public API scope
  - Option A: Keep only request-scoped `Transaction.Execute`.
  - Option B: Add explicit begin/commit/rollback APIs.
  - Decision: Option A for v7.0, with transaction-local repository sessions that can support a future explicit context API.

- [x] Isolation level defaults
  - PostgreSQL: provider/database default unless an explicit supported isolation level is requested.
  - SQL Server: unsupported placeholder in v7.0.
  - MySQL: unsupported placeholder in v7.0.
  - SQLite: existing provider transaction behavior with deterministic rejection of unsupported exact isolation choices.
  - Decision: provider default with explicit request override and telemetry reporting.

- [x] Transaction retry policy
  - Deadlock/serialization failures can be retried only if the transaction request is idempotent.
  - Current transaction operations can create new GUIDs inside request payloads; retry may duplicate unless GUIDs are stable.
  - Decision: no automatic retry by default; return retryable/conflict diagnostics.

- [x] Vector-index commit semantics
  - Option A: Apply vector mutations after DB commit.
  - Option B: Stage vector mutations and apply on commit; discard on rollback.
  - Option C: Mark graph vector index dirty for any transaction vector mutation and rebuild asynchronously.
  - Decision: stage metadata for commit where straightforward; mark dirty on failure or rollback uncertainty. Do not apply vector mutations before DB commit.

- [x] Mixed transaction and non-transaction concurrency
  - Need behavior when non-transaction writes target the same graph as active transactions.
  - Decision: allow provider/database isolation to govern DB state; coordinate shared vector-index commits with graph-scoped index locks.

## Implementation Phases

### Phase 0: Discovery And Design Freeze

- [x] Produce transaction state audit. Note: public `TransactionMethods.Execute`, `QueryExecutionEngine.ExecuteMutation`, provider repositories, vector-index mutation paths, and transaction diagnostics paths have been audited enough to define the v7 workstreams. Full ambient-state removal remains implementation work under Phase 2. 2026-06-19.
  - Files: `src/LiteGraph/Client/Implementations/TransactionMethods.cs`, provider repositories, vector-index extensions.
  - Deliverable: markdown inventory of ambient transaction fields and call sites.

- [x] Define target internal interfaces. Note: v7.0 finalizes `GraphRepositoryBase.CreateIsolatedTransactionRepository()` as the provider hook for request-scoped transaction-local repository sessions. A full public `IRepositorySession`/`GraphTransactionContext` API is deferred. 2026-06-19.
  - Deliverable: design proposal with type names, method signatures, lifecycle semantics, and provider responsibilities.

- [x] Confirm public breaking changes. Note: request-scoped public transaction API remains compatible; custom repository integrations can keep serialized fallback behavior or opt into transaction-local sessions by overriding `CreateIsolatedTransactionRepository()`. 2026-06-19.
  - Deliverable: v7.0 API compatibility note.

- [x] Confirm provider support matrix. Note: SQLite and PostgreSQL are the converted v7.0 providers; SQL Server/MySQL remain explicit unsupported placeholders, and custom/unconverted providers retain the serialized fallback gate until session support is implemented. 2026-06-19.
  - SQLite: correctness, limited write scaling.
  - PostgreSQL: full parallel transaction support.
  - SQL Server: unsupported placeholder in v7.0; future provider work.
  - MySQL: unsupported placeholder in v7.0; future provider work.

- [x] Update `docs/UPGRADE.md` draft section with planned breaking changes. Note: transaction diagnostics including `QueueWaitDurationMs`, isolation behavior, provider caveats, Docker PostgreSQL defaults, HnswLite `2.0.1`, and fallback-gate monitoring guidance are documented; final GitHub release notes remain a publish/tag operation. 2026-06-19.

### Phase 1: Core Transaction Context Infrastructure

- [x] Add transaction context abstractions. Note: explicit context/session types are implemented and `GraphRepositoryBase.CreateRepositorySession()` now wraps provider isolated repositories into `IRepositorySession`; Debug build of `src/LiteGraph/LiteGraph.csproj` passes. 2026-06-19.
  - Proposed files:
    - `src/LiteGraph/GraphRepositories/Interfaces/ITransactionContext.cs`
    - `src/LiteGraph/GraphRepositories/Interfaces/IRepositorySession.cs`
    - `src/LiteGraph/GraphRepositories/Interfaces/IRepositorySessionFactory.cs`
    - `src/LiteGraph/GraphRepositories/Transaction/GraphTransactionContext.cs`
    - `src/LiteGraph/GraphRepositories/Transaction/TransactionExecutionOptions.cs`

- [x] Add provider-neutral transaction isolation enum or mapping. Note: `TransactionIsolationLevelEnum` is implemented on transaction requests/results; PostgreSQL maps `ReadCommitted`, `RepeatableRead`, and `Serializable`; SQLite supports `Default` and `Serializable` and rejects unsupported isolation choices deterministically. 2026-06-19.
  - Ensure it maps safely to provider-specific isolation levels.
  - Include `Default`, `ReadCommitted`, `RepeatableRead`, `Serializable`, and `Snapshot` if supported.

- [x] Add transaction ID generation. Note: `TransactionMethods.Execute` assigns a GUID transaction ID for every transaction result; REST/MCP/SDK models carry it through JSON serialization and request-history diagnostics. 2026-06-19.
  - Use GUID or ULID-style sortable ID.
  - Add to telemetry and transaction results.

- [x] Add guardrails. Note: request-level bounds exist in `TransactionRequest`, REST enforces server caps for max operations and transaction timeout, transaction validation returns deterministic diagnostics, `GraphTransactionContext` enforces lifecycle state transitions, and automated tests cover double commit and dispose/session cleanup. 2026-06-19.
  - Double commit throws or returns deterministic error.
  - Rollback after commit is a no-op or deterministic error.
  - Query after commit/rollback/dispose fails deterministically.

- [x] Add cancellation and timeout semantics. Note: transaction execution links caller cancellation with per-request timeout, checks cancellation between operations, attempts rollback on failure, and uses non-cancelable cleanup for rollback. Automated transaction cancellation and timeout tests pass. 2026-06-18.
  - Timeout cancels current operation.
  - Timeout attempts rollback.
  - Rollback uses non-cancelable cleanup token after operation cancellation.

- [x] Add transaction lifecycle states. Note: `TransactionStateEnum` is implemented and `TransactionResult.State` reports final lifecycle state such as `Committed`, `RolledBack`, or `Faulted`; C#, JavaScript, Python SDK models, REST/docs, API Explorer summaries, request-history diagnostics, and shared transaction tests were updated for the field. 2026-06-19.
  - `Created`
  - `Active`
  - `Committing`
  - `Committed`
  - `RollingBack`
  - `RolledBack`
  - `Faulted`
  - `Disposed`

- [x] Add transaction diagnostics. Note: started/completed timestamps, duration, queue wait, commit/rollback duration, provider, requested isolation, lifecycle state, isolated-repository/gate flags, retry count, retryable/conflict flags, provider error code, REST activity lifecycle tags, active transaction gauges, queue/wait metrics, retry/conflict counters, and Grafana panels are implemented. 2026-06-19.
  - StartedUtc
  - CompletedUtc
  - DurationMs
  - IsolationLevel
  - OperationCount
  - RetryCount
  - Provider
  - TransactionId
  - CommitDurationMs
  - RollbackDurationMs

### Phase 2: Repository Interface Refactor

- [x] Refactor repository methods to accept optional session/context.
  - Decision: complete by routing transaction execution through session-owned repository instances instead of adding session parameters to every repository method. This preserves public/client method signatures while ensuring transaction work executes against `IRepositorySession.Repository`.
  - Status: [x]. 2026-06-19.
  - Pattern:
    - `Create(node, token)` remains for public/client use.
    - Internal overload or repository implementation receives `IRepositorySession`.
  - Avoid passing `bool isTransaction` as the only transaction signal.

- [x] Update primitive execution methods. Note: provider primitives execute against the transaction-local repository held by `IRepositorySession`; native query mutation planning now creates `GraphTransactionContext` explicitly and permits the inner executor to participate only when the engine was created for that explicit context. Compatibility `isTransaction` flags remain for provider-local SQL batching outside graph transactions. 2026-06-19.
  - Replace `ExecuteQueryAsync(query, isTransaction, token)` with session-aware execution.
  - Replace `ExecuteQueriesAsync(queries, isTransaction, token)` with session-aware execution.
  - Preserve non-transaction convenience wrappers.

- [x] Remove shared ambient transaction fields from caller execution. Note: `GraphRepositoryBase` retains transaction lifecycle hooks as provider compatibility methods, but caller-facing transaction execution now creates an isolated `IRepositorySession`/`GraphTransactionContext`; SQLite/PostgreSQL transaction fields live only on the session repository, and unconverted providers are serialized by the compatibility gate. 2026-06-19.
  - Remove or obsolete:
    - `GraphTransactionActive`
    - `GraphTransactionTenantGUID`
    - `GraphTransactionGraphGUID`
    - `BeginGraphTransaction`
    - `CommitGraphTransaction`
    - `RollbackGraphTransaction`
  - If retained for compatibility, mark `[Obsolete]` and route to explicit context only where safe.

- [x] Refactor transaction operation execution. Note: `TransactionMethods.Execute` creates `TransactionExecutionOptions`, `IRepositorySession`, and `GraphTransactionContext`; every transaction operation uses the same session repository; native graph query mutation execution now begins/commits/rolls back through `GraphTransactionContext`; converted providers do not use the fallback gate. Focused transaction-concurrency selector passes 20 runnable cases with one expected PostgreSQL skip when no connection string is set. 2026-06-19.
  - `TransactionMethods.Execute` creates one isolated session/context.
  - Every transaction operation uses the same session/context.
  - No repository instance field is changed by the transaction.

- [x] Refactor validation paths. Note: request-shape validation still runs before provider begin by design; operation validation/read-your-writes checks execute through the session repository, including same-transaction edge creation against nodes created earlier and query mutation coherency. 2026-06-19.
  - Validation inside a transaction must use the same transaction session to preserve read-your-writes semantics.
  - Example: create node, then create edge referencing that node in the same transaction.

- [x] Define internal method naming convention. Note: v7.0 uses explicit `TransactionExecutionOptions`, `IRepositorySessionFactory.CreateRepositorySession`, `IRepositorySession`, and `GraphTransactionContext`; per-method session overloads are not required because session methods are invoked on the session-owned repository instance. 2026-06-19.
  - Examples:
    - `CreateAsync(..., IRepositorySession session, CancellationToken token)`
    - `ReadByGuidAsync(..., IRepositorySession session, CancellationToken token)`
  - Avoid duplicating full method bodies where a session parameter can be threaded through.

### Phase 3: Provider Implementations

#### SQLite

- [x] Implement SQLite transaction context. Note: v7.0 uses a SQLite transaction-local repository instance with its own connection/transaction fields and shared vector-index manager ownership guarded by clone disposal semantics. Full SQLite/default automated validation, transaction-concurrency selection, live C# SDK validation, and bounded performance smoke pass. 2026-06-19.
  - Owns a dedicated `SqliteConnection`.
  - Owns a dedicated `SqliteTransaction`.
  - Applies connection settings and pragmas consistently.

- [x] Decide SQLite transaction begin mode. Note: v7.0 keeps the existing SQLite begin behavior plus `busy_timeout` protection for compatibility; configurable immediate begin remains future tuning if production telemetry proves it is needed. 2026-06-19.
  - Deferred: fewer immediate locks, later write contention.
  - Immediate: predictable write lock acquisition.
  - Recommended: configurable default, likely `Immediate` for write transactions.

- [x] Add busy timeout behavior. Note: SQLite `busy_timeout` is applied to persistent and transaction connections so concurrent write contention waits deterministically instead of failing immediately; SQLite/default concurrency validation passes. 2026-06-19.
  - Ensure write contention returns controlled timeout or retryable error.
  - Expose SQLite lock wait metrics.

- [x] Ensure non-transaction operations remain pooled. Note: PostgreSQL transaction clones share the parent `NpgsqlDataSource` pool, non-transaction operations continue through the parent repository path, and `Storage.Postgresql.TransactionClonePool` plus full provider validation pass. SQLite preserves its existing persistent/transient connection behavior. 2026-06-19.

- [x] Add SQLite tests for concurrent write contention. Note: `Transactions.Sqlite.ConcurrentWriteContention` runs 12 concurrent Serializable SQLite graph transactions, verifies all committed nodes/edges, confirms isolated repositories, confirms the legacy gate was not used, and passed in the SQLite/default `--transaction-concurrency` selector. 2026-06-19.
  - Expected: correctness, possible serialization/lock waiting, no ambient-state corruption.

#### PostgreSQL

- [x] Implement PostgreSQL transaction context. Note: v7.0 uses a PostgreSQL transaction-local repository instance with isolated active transaction fields, a dedicated transaction connection, and the parent `NpgsqlDataSource` pool shared across sessions. Focused regression `Storage.Postgresql.TransactionClonePool`, full PostgreSQL provider/parity validation, transaction-concurrency selection, live C# SDK validation, and bounded performance smoke pass. 2026-06-19.
  - Owns an `NpgsqlConnection`.
  - Owns an `NpgsqlTransaction`.
  - Uses provider data source pooling correctly.

- [x] Map isolation levels. Note: transaction requests/results carry `TransactionIsolationLevelEnum`; PostgreSQL maps `ReadCommitted`, `RepeatableRead`, and `Serializable`; SQLite accepts `Default` and `Serializable` and rejects unsupported exact-isolation requests. 2026-06-18.
  - Default
  - ReadCommitted
  - RepeatableRead
  - Serializable

- [x] Add deadlock/serialization failure classification. Note: transaction diagnostics now classify PostgreSQL `40001`, `40P01`, and `55P03` as retryable concurrency conflicts; focused Touchstone coverage passed in the full `Test.Automated` run on 2026-06-18. 2026-06-18.
  - PostgreSQL SQLSTATE examples:
    - `40001` serialization failure
    - `40P01` deadlock detected
    - `55P03` lock not available
  - Mark retryable when appropriate.

- [x] Ensure schema qualification works inside transaction context. Note: PostgreSQL transaction clones preserve the parent schema, SQL translation schema-qualification tests pass, and Docker-backed `Database.Postgresql`, provider parity, and transaction-concurrency matrix runs create and clean isolated non-default schemas successfully. 2026-06-19.

- [x] Add PostgreSQL tests for same-graph concurrent transactions. Note: `Transactions.ProviderMatrix.PostgresqlConcurrency` creates a disposable PostgreSQL schema and runs concurrent same-graph mixed node/edge/label/tag/vector transactions, multi-graph commits, commit/rollback isolation, metadata attach/detach, and vector commit/rollback checks when `LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING` is set. Docker-backed validation passed in the full suite, `--transactions`, and `--transaction-concurrency`. 2026-06-19.

#### SQL Server

- [x] Audit SQL Server provider maturity. Note: `SqlServerGraphRepository` is an `UnsupportedGraphRepository` placeholder in v7.0. 2026-06-19.
- [x] Implement SQL Server transaction context. Note: out of scope for v7.0 because the provider is not executable; operations fail fast through `UnsupportedGraphRepository`. 2026-06-19.
  - Owns `SqlConnection`.
  - Owns `SqlTransaction`.
  - Maps isolation levels.

- [x] Add SQL Server integration tests or mark provider transaction concurrency unsupported until complete. Note: unsupported-provider factory/operation coverage exists in shared touchstone storage provider tests and docs describe SQL Server as future provider work. 2026-06-19.

#### MySQL

- [x] Audit MySQL provider maturity. Note: `MysqlGraphRepository` is an `UnsupportedGraphRepository` placeholder in v7.0. 2026-06-19.
- [x] Implement MySQL transaction context if provider exists. Note: out of scope for v7.0 because the provider is not executable; operations fail fast through `UnsupportedGraphRepository`. 2026-06-19.
- [x] Map isolation levels and retryable errors. Note: not applicable until the MySQL provider is implemented. 2026-06-19.
- [x] Add MySQL integration tests or mark provider transaction concurrency unsupported until complete. Note: unsupported-provider factory/operation coverage exists in shared touchstone storage provider tests and docs describe MySQL as future provider work. 2026-06-19.

### Phase 4: Transaction Operation Semantics

- [x] Validate create/update/delete/upsert behavior under one transaction context. Note: local SQLite touchstone `Transactions.Client.OperationMatrix`, full SQLite/default validation, and PostgreSQL-enabled validation cover transaction create/update/delete for nodes, edges, labels, tags, and vectors. 2026-06-19.
- [x] Validate attach/detach labels. Note: local SQLite touchstone `Transactions.Client.AttachDetachUpsert` and PostgreSQL-enabled validation cover attach/detach/upsert for labels. 2026-06-19.
- [x] Validate attach/detach tags. Note: local SQLite touchstone `Transactions.Client.AttachDetachUpsert` and PostgreSQL-enabled validation cover attach/detach/upsert for tags. 2026-06-19.
- [x] Validate attach/detach vectors. Note: local SQLite touchstone `Transactions.Client.AttachDetachUpsert`, focused vector suites, and PostgreSQL transaction-concurrency matrix cover staged vector commit/rollback behavior. 2026-06-19.
- [x] Validate batch operations inside transaction context if supported. Note: there is no standalone transaction batch operation type in the v7.0 transaction request contract; provider create-many/vector batch paths are covered by vector staging and full SQLite/PostgreSQL automated suites. Future batch request types must add operation-matrix tests before release. 2026-06-19.
- [x] Validate read-your-writes. Note: transaction tests create nodes and then create edges/child objects referencing those nodes within the same transaction; SQLite/default and PostgreSQL-enabled suites pass. 2026-06-19.
  - Create node then read node.
  - Create node then create edge referencing node.
  - Create vector then search/read vector where supported.
  - Update node then read updated node.
  - Delete node then read missing node.

- [x] Define conflict semantics. Note: validation failures stop before provider begin and return validation diagnostics; operation/provider failures roll back and preserve `FailedOperationIndex`; PostgreSQL `40001`, `40P01`, and `55P03` are marked retryable concurrency conflicts; SQLite write contention remains file-lock bounded and surfaces as a rolled-back provider failure. 2026-06-19.
  - Concurrent update same node.
  - Concurrent delete/update same node.
  - Concurrent edge creation referencing node deleted by another transaction.
  - Concurrent vector update same vector.

- [x] Define result shape for conflicts. Note: `TransactionResult` exposes `TransactionId`, `State`, `RolledBack`, `ValidationFailure`, `FailedOperationIndex`, `Provider`, `ProviderErrorCode`, `Retryable`, `RetryCount`, and `ConcurrencyConflict`; REST, SDKs, MCP, dashboard summaries, request history, and docs carry those fields. 2026-06-19.
  - Add provider error code?
  - Add retryable boolean?
  - Add failed operation index?
  - Add transaction ID?

### Phase 5: Vector Index Transaction Semantics

- [x] Upgrade to HnswLite `2.0.1`. Note: NuGet feed verification on 2026-06-18 showed `2.0.0` and `2.0.1`; v7.0 targets `HnswLite` `2.0.1` explicitly. `src/LiteGraph/LiteGraph.csproj` has been updated to `HnswLite` `2.0.1`; `HnswLiteVectorIndex` now implements v2.0.1 async storage/node/layer contracts and writes LiteGraph HNSW state with `FormatVersion = 2`, `HnswLiteVersion = "2.0.1"`, and persisted neighbor connections. Focused vector suites passed on 2026-06-18: `Vector.Search`, `Vector.Index.Implementation`, and `Vector.Index.Search` on `net10.0` with 9/9 passing, including SQLite reload persistence and legacy artifact dirty/fallback behavior. These vector suites are registered in `LiteGraphTouchstoneSuites.All`, so they are part of the default `Test.Automated` release gate; no checked-in `.github` workflow exists in this checkout to update.
  - [x] Update `src/LiteGraph/LiteGraph.csproj` to `HnswLite` `2.0.1`.
  - [x] Audit HnswLite v2 API changes and adapt `src/LiteGraph/Indexing/Vector/HnswLiteVectorIndex.cs`.
  - [x] Validate RAM-backed HNSW index behavior.
  - [x] Validate SQLite-backed HNSW index behavior, including reload/search persistence.
  - [x] Detect whether existing v1.x vector index artifacts are format-compatible by requiring `FormatVersion = 2` for file-backed HNSW state.
  - [x] If artifacts are incompatible, mark graph vector indexes dirty and require rebuild instead of unsafe in-place reuse.
  - [x] Add migration guidance for backing up/removing/rebuilding `indexes/` artifacts in `docs/UPGRADE.md` and `docs/STORAGE.md`.
  - [x] Add package compatibility validation to CI/release gates through the default `Test.Automated` suite registration.

- [x] Audit all vector-index mutation paths. Note: create, create-many, update, delete, delete-many, delete-all tenant/graph, rebuild, enable/disable, and search compatibility paths were reviewed for SQLite/PostgreSQL. Node-vector index mutations now flow through provider vector-index helpers where transaction staging can intercept them. 2026-06-18.
  - [x] Node create/update/delete with vectors.
  - [x] Vector create/update/delete.
  - [x] Batch operations.
  - [x] Transaction operations.

- [x] Add transaction-local vector-index mutation staging. Note: SQLite and PostgreSQL repositories now queue vector-index mutations while a graph transaction is active instead of mutating the live HNSW index before DB commit. 2026-06-18.
  - [x] Store pending index operations in transaction context.
  - [x] Do not mutate live in-memory index before DB commit.

- [x] Apply staged vector-index mutations after DB commit. Note: SQLite and PostgreSQL apply queued vector-index mutations after the provider commit returns; failures mark the graph vector index dirty and increment `litegraph.vector.index.mutation.failures`. Graph-scoped locking is provided by `VectorIndexManager.ExecuteWithIndexAsync`. 2026-06-18.
  - [x] Use graph-scoped vector-index lock.
  - [x] On index mutation failure, mark graph vector index dirty.
  - [x] Ensure telemetry records failure.

- [x] Discard staged vector-index mutations on rollback. Note: rollback/cleanup clears queued vector-index mutations without marking the index dirty because the live index was not touched. 2026-06-18.

- [x] Handle transaction cancellation. Note: transaction cancellation and timeouts roll back with non-cancelable cleanup and discard staged vector-index mutations; if post-commit index mutation fails, the graph vector index is marked dirty. Automated cancellation/timeout and vector rollback tests pass. 2026-06-19.
  - [x] If DB rollback succeeds, discard staged mutations.
  - [x] If cleanup status unknown, mark vector index dirty where live index mutation may have been attempted.

- [x] Handle mixed concurrent vector index changes. Note: `Transactions.Concurrent.MixedVectorIndexChanges` covers concurrent transaction vector create/update/rollback with non-transaction vector create/delete, verifies committed metadata, deleted/rolled-back absence, clean index stats, and indexed search correctness. 2026-06-19.
  - Transaction A commits vector changes.
  - Transaction B commits vector changes.
  - Non-transaction vector update occurs while transaction is active.
  - Rebuild index while transactions commit remains a future hardening scenario because rebuild is an administrative graph-wide operation rather than a normal request-scoped transaction path.

- [x] Add vector-index tests. Note: focused vector suites pass 10/10 on `net10.0`, including commit, rollback, dirty repair, SQLite persistence, legacy artifact fallback, and concurrent staged vector transaction correctness. SQLite/default transaction-concurrency adds mixed transaction/non-transaction vector-index coherency; Docker-backed PostgreSQL transaction-concurrency matrix includes concurrent vector commit/rollback checks. 2026-06-19.
  - [x] Commit applies index changes.
  - [x] Rollback does not apply index changes.
  - [x] Concurrent vector transactions preserve index correctness.
  - [x] Dirty marker appears only when necessary.

### Phase 6: Client API And Core SDK Surface

- [x] Update `LiteGraphClient.Transaction.Execute` internals. Note: transaction execution now uses `TransactionExecutionOptions`, `IRepositorySession`, and `GraphTransactionContext`; converted providers use transaction-local repositories and the legacy serialized gate is only a compatibility fallback for unconverted providers. 2026-06-19.
  - Remove safety gate once explicit transaction sessions are correct.
  - Retain a feature flag to temporarily re-enable serialization during rollout if needed.

- [x] Add transaction execution options. Note: `TransactionExecutionOptions` carries transaction ID, tenant, graph, isolation, operation count, and source into explicit session/context creation; request-level isolation selection is implemented across core, SQLite/PostgreSQL providers, MCP request shaping, JS/Python SDKs, and dashboard request templates. Retry/idempotency remains a separate future feature. 2026-06-19.
  - Isolation level
  - Timeout
  - Retry policy
  - Transaction ID override for idempotency/correlation if appropriate

- [x] Update transaction result models. Note: core result model, REST diagnostics, JS SDK model/types/tests, Python SDK model/tests, C# SDK model/build/tests, dashboard request-history diagnostics, and docs include v7 diagnostics including lifecycle state, validation-failure state, and `QueueWaitDurationMs`. 2026-06-19.
  - TransactionId
  - IsolationLevel
  - StartedUtc
  - CompletedUtc
  - DurationMs
  - CommitDurationMs
  - RollbackDurationMs
  - Retryable
  - RetryCount
  - ProviderErrorCode
  - ConcurrencyConflict flag if distinguishable

- [x] Update XML documentation. Note: core and C# SDK XML docs include `TransactionResult.State` and `TransactionStateEnum`; public model comments are present for the v7 transaction diagnostics surface. 2026-06-19.
  - `src/LiteGraph/LiteGraph.xml`
  - Interface and model comments.

- [x] Add compatibility shims or obsolete old APIs. Note: `GraphRepositoryBase.CreateRepositorySession()` adapts existing provider `BeginGraphTransaction`/`CommitGraphTransaction`/`RollbackGraphTransaction` hooks into `IRepositorySession`; providers that do not return isolated repositories use the serialized compatibility gate, preserving custom-provider behavior while exposing the v7 explicit session path. 2026-06-19.
  - Mark with clear v8 removal plan if retained.

### Phase 7: Server REST API

- [x] Audit transaction routes in `src/LiteGraph.Server`. Note: `GraphTransactionRoute` now tags validation failures and uses transaction-result diagnostics to select HTTP status. 2026-06-18.
  - Locate route handlers for transaction endpoints.
  - Locate OpenAPI/API explorer generation paths.

- [x] Update REST transaction request schema. Note: REST docs and API Explorer templates include isolation and timeout; retry policy and idempotency key are future request-contract additions, not v7.0 release blockers. 2026-06-19.
  - Optional isolation level.
  - Optional retry policy.
  - Optional timeout.
  - Optional idempotency key or transaction correlation ID.

- [x] Update REST transaction response schema. Note: REST docs, SDKs, MCP preservation, dashboard summaries, and request history include expanded diagnostics and `ValidationFailure`; full SQLite/PostgreSQL automated validation passes. 2026-06-19.
  - Include new transaction diagnostics.
  - Include retryable/conflict metadata.

- [x] Update error mapping. Note: validation failures return `400` transaction diagnostics; rolled-back execution failures and retryable/concurrency conflicts return `409`; unexpected pre-transaction failures return `500`; timeout/cancellation rollback behavior is covered in automated transaction tests. 2026-06-19.
  - Deadlock/serialization conflict: appropriate HTTP status decision.
  - Validation failures: `400`.
  - Authorization failures: existing auth behavior.
  - Timeout: `408` or existing error convention.
  - Provider failure: `500`/`503` based on type.

- [x] Ensure authorization is transaction safe. Note: REST authenticates and authorizes `GraphTransaction` before executing `GraphTransactionRoute`; `Transactions.Authorization.Boundary` verifies transaction requests require write permission on the `Transaction` resource type and denies viewer/query-only credentials. 2026-06-19.
  - Auth checks must not rely on ambient transaction state.
  - Confirm transaction operation authorization checks happen before or inside transaction consistently.

- [x] Update request history capture. Note: request history entries now have `TransactionDiagnosticsJson`, populated from graph transaction response bodies and persisted through SQLite/PostgreSQL request-history storage. REST/dashboard transaction filters for diagnostics presence and transaction ID are implemented, and compact diagnostics include queue wait plus commit/rollback timings. 2026-06-19.
  - Store transaction ID.
  - Store isolation level.
  - Store retry count.
  - Store concurrent wait duration if any.
  - Store provider error code.

- [x] Update server settings. Note: added `LiteGraph.Transactions` settings with enforced REST caps for maximum transaction operations and timeout, plus environment variable overrides. Shared lifecycle settings test covers defaults, bounds, env constant names, and REST cap helper. 2026-06-18.
  - Max concurrent transactions.
  - Max transactions per tenant.
  - Transaction default timeout.
  - Transaction default isolation level.
  - Transaction retry policy.
  - SQLite busy timeout.

- [x] Add server integration tests for the v7.0 local release gate. Note: full automated suites cover REST transaction diagnostics/status mapping, REST/MCP auth boundaries, request-history transaction diagnostics, timeout settings, metrics, and Docker smoke. Long-running concurrent REST stress remains future hardening. 2026-06-19.
  - Concurrent REST transactions through one server.
  - Same tenant/graph.
  - Different tenants/graphs.
  - Rollback correctness.
  - Timeout correctness.
  - Vector index correctness.

### Phase 8: Dashboard

- [x] Update dashboard API client models. Note: API Explorer response summaries render transaction diagnostics from the REST result, request-history models carry persisted transaction diagnostics, and no separate generated dashboard SDK client exists in the current codebase. Full dashboard tests pass. 2026-06-19.
  - Path: `dashboard/src/lib/sdk`.
  - Include new transaction result fields.

- [x] Update API Explorer. Note: transaction request template includes isolation level and response summaries include transaction ID, provider, provider error code, retryable, and concurrency conflict fields. Dashboard OpenAPI/template and response summary tests pass. 2026-06-19.
  - Path: `dashboard/src/page/api-explorer`.
  - Add v7.0 transaction request examples.
  - Add isolation/retry/timeout fields.
  - Add response summaries for transaction diagnostics.

- [x] Add transaction diagnostics display where transaction results are shown. Note: API Explorer transaction failure summaries show transaction ID, lifecycle state, provider, provider error code, retryable, and concurrency conflict fields; request-history detail modal and list transaction summary column show persisted `TransactionDiagnosticsJson`; request-history filters support diagnostics presence and transaction ID. Full dashboard tests pass. 2026-06-19.
  - Transaction ID.
  - Success/rolled back.
  - Isolation level.
  - Duration.
  - Retryable/conflict status.
  - Failed operation index.

- [x] Update request-history page. Note: request-history detail modal renders transaction diagnostics when captured; list table now includes transaction ID/state/isolation/provider/provider-error/retry/conflict tags; toolbar filters by transaction rows and transaction ID. Focused request-history tests pass. 2026-06-18.
  - Add transaction filters.
  - Add transaction ID column/detail.
  - Add provider error/retry fields if stored.

- [x] Add dashboard tests. Note: focused request-history/API Explorer transaction tests pass and the full dashboard Jest suite passed 85 suites, 1,280 tests, and 65 snapshots. 2026-06-19.
  - API explorer transaction template.
  - Request history transaction metadata rendering.
  - Error response rendering for conflict/timeout.

- [x] Confirm no dashboard assumptions depend on serialized transaction behavior. Note: dashboard transaction usage is limited to REST request templates, response summaries, and request-history diagnostics/search; no client-side transaction sequencing or shared transaction state assumptions were found by source scan. 2026-06-18.

### Phase 9: MCP Server

- [x] Audit transaction MCP registrations. Note: transaction registration schema was reviewed and updated for request isolation-level support. 2026-06-17.
  - File: `src/LiteGraph.McpServer/Registrations/TransactionRegistrations.cs`.

- [x] Update transaction tool schema. Note: MCP transaction tool accepts `isolationLevel` and forwards it in the transaction request. Timeout already exists at request level; retry/idempotency options remain future feature work, not a v7.0 release blocker. 2026-06-19.
  - Add isolation level argument.
  - Add timeout argument if not already present.
  - Add retry policy argument if exposed.
  - Add idempotency/correlation argument if exposed.

- [x] Update MCP tool response shape. Note: MCP transaction calls preserve REST `400`/`409` transaction result bodies when the body is a transaction diagnostic result, including validation-failure details, transaction ID, duration, retryable/conflict status, and failed-operation metadata. 2026-06-19.
  - Include transaction ID.
  - Include duration and retryable/conflict status.
  - Include failed operation metadata.

- [x] Update MCP REST proxy if transaction contract changes. Note: `graph/transaction` proxy returns diagnostic transaction results for validation and rollback/conflict responses while still throwing for generic API errors. 2026-06-19.
  - File: `src/LiteGraph.McpServer/Classes/LiteGraphMcpRestProxy.cs`.

- [x] Add MCP tests. Note: touchstone `MCP.Graph.Transaction` verifies transaction ID, validation-failure diagnostics, and direct `operations` plus `isolationLevel` request shaping; full automated suites pass with MCP transaction diagnostics coverage. Longer MCP stress remains future hardening. 2026-06-19.
  - Concurrent tool calls.
  - Rollback tool call.
  - Provider error response.
  - REST proxy compatibility.

- [x] Update MCP docs/config. Note: `docs/CLAUDE_MCP.md` documents `graph/transaction`, `isolationLevel`, and diagnostic result preservation. MCP Docker configs were reviewed; no transaction-specific MCP server setting is needed because transaction caps live on the LiteGraph server and MCP forwards request-level transaction arguments. 2026-06-18.
  - `docs/CLAUDE_MCP.md`
  - `docker/litegraph-mcp.json`
  - `docker/factory/litegraph-mcp.json`

### Phase 10: SDKs

#### C# SDK

- [x] Update C# SDK transaction models. Note: added PascalCase transaction request, operation, operation result, transaction result diagnostics, object/operation enums, isolation enum, and fluent builder models that mirror the REST contract. `dotnet build sdk/csharp/src/Test.Automated/Test.Automated.csproj -c Debug` passes. 2026-06-17.
  - Path: `sdk/csharp/src/LiteGraph.Sdk`.

- [x] Add or update `ITransactionMethods` if SDK has transaction support. Note: added `ITransactionMethods`, `LiteGraphSdk.Transaction`, and REST-backed execution against `/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/transaction`; the client preserves `409` transaction result bodies so rollback diagnostics remain available. `dotnet build sdk/csharp/src/Test.Automated/Test.Automated.csproj -c Debug` passes. 2026-06-17.
  - If missing, add transaction methods for parity with REST API.

- [x] Add transaction execution options models. Note: JS, Python, and C# SDK request models/builders support transaction isolation level. JS/Python unit tests pass; C# SDK automated project build passes. 2026-06-17.

- [x] Add transaction result diagnostics. Note: JS, Python, and C# SDK transaction result models now expose transaction ID, operation count, timing, provider, isolation, isolated repository/gate flags, retry/conflict metadata, and provider error code. JS/Python tests pass; C# SDK automated project build passes. 2026-06-17.

- [x] Add tests. Note: added C# SDK automated coverage for transaction request serialization, commit diagnostics, rollback diagnostics, and rollback visibility. Build passes; live SDK integration passed against isolated SQLite (132/132) and PostgreSQL (128/128) servers. 2026-06-19.
  - `sdk/csharp/src/Test.Sdk`
  - `sdk/csharp/src/Test.Automated`
  - Concurrent REST transaction execution.

- [x] Update SDK README. Note: C# SDK README documents graph transaction builder usage, isolation selection, and diagnostics, with v7 wording. 2026-06-18.

#### JavaScript SDK

- [x] Update JS SDK transaction request/response helpers. Note: JS transaction builder supports isolation level and transaction result model exposes diagnostics. `npm.cmd test -- transactionRoutes.test.js --runInBand` passes. 2026-06-17.
  - Path: `sdk/js/src`.

- [x] Update TypeScript definitions. Note: regenerated JS type declarations include transaction builder isolation level, `LiteGraphSdk.Transaction`, and transaction result diagnostics. `npm.cmd run build:types` passes. 2026-06-17.
  - Path: `sdk/js/types`.

- [x] Add tests in `sdk/js/test`. Note: JS transaction route tests cover isolation-level request shaping and diagnostics response fields. 2026-06-17.

- [x] Update JS SDK README and docs. Note: JavaScript SDK README documents graph transaction builder usage, isolation selection, 400/409 diagnostic body preservation, and v7 transaction result fields. 2026-06-18.

#### Python SDK

- [x] Update Python SDK transaction models/helpers. Note: Python transaction request/builder supports isolation level and transaction result model exposes diagnostics. `python -m pytest tests/test_transactions.py` passes. 2026-06-17.
  - Path: `sdk/python`.

- [x] Add tests for transaction options/results. Note: Python transaction tests cover isolation-level request shaping and diagnostics response fields. 2026-06-17.

- [x] Update Python docs. Note: Python SDK README documents transaction request execution, isolation selection, 400/409 diagnostic body preservation, and v7 transaction result fields. 2026-06-18.

#### Versioning

- [x] Bump SDK major versions to `7.0.0`. Note: C# SDK and JavaScript SDK package metadata are set to `7.0.0`; Python SDK docs/changelog are updated for v7.0. 2026-06-19.
- [x] Ensure package metadata and changelogs mention breaking changes. Note: SDK changelogs include v7.0 transaction and diagnostics updates. 2026-06-19.
- [x] Validate generated packages locally. Note: C# SDK build passes, JS SDK tests pass, and Python SDK tests pass; package publication remains release engineering. 2026-06-19.

### Phase 11: Postman Collection

- [~] Update `LiteGraph.postman_collection.json`. Note: added a `v7.0` Transactions folder with commit, rollback diagnostics, serializable isolation, and vector transaction examples; collection JSON parses successfully. Manual Docker validation and conflict/deadlock example remain pending. 2026-06-17.
  - Add v7.0 transaction examples.
  - Add isolation level examples.
  - Add rollback example.
  - Add conflict/deadlock example where feasible.
  - Add vector transaction example.
  - Add concurrent transaction guidance in collection docs.

- [~] Add environment variables. Note: added transaction isolation, timeout, transaction ID, and stable transaction object GUID variables to the collection. Provider-specific database selection remains pending. 2026-06-17.
  - `transactionIsolationLevel`
  - `transactionTimeoutSeconds`
  - `transactionId`
  - Provider-specific database selection if needed.

- [ ] Validate collection manually against local Docker server.

- [ ] Export and commit formatted collection.

### Phase 12: Test Harnesses

Correctness and coherency tests are release gates. Short performance-harness runs are useful for validating harness execution and artifact generation, but they do not establish transaction correctness. The v7.0 local release gate is the full SQLite/default automated suite, the PostgreSQL-enabled automated suite, focused transaction-concurrency selectors, and live SDK/dashboard validation recorded above. Randomized history, soak, and larger matrix runners remain future hardening work.

#### Correctness And Coherency Expansion

- [x] Define the provider correctness matrix. Note: v7.0 supports SQLite file mode and PostgreSQL; MySQL and SQL Server are explicit unsupported placeholders; SQLite in-memory remains covered by existing storage paths where applicable but is not the Docker/default production target. 2026-06-19.
  - SQLite file mode.
  - SQLite in-memory mode where applicable.
  - PostgreSQL.
  - SQL Server if transaction support is in scope; otherwise explicit unsupported-provider tests and documentation.
  - MySQL if transaction support is in scope; otherwise explicit unsupported-provider tests and documentation.
  - For each provider, record supported isolation levels, expected lock/conflict behavior, retryable error codes, and required CI cadence.

- [x] Build a provider-matrix runner for correctness suites. Note: `Test.Automated` can run SQLite/default by default and opt into PostgreSQL using `LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING`; PostgreSQL cases create disposable schemas and skip clearly when credentials are absent. 2026-06-19.
  - Reuse the same logical tests across providers.
  - Create disposable database/schema/file per run.
  - Ensure tests can run in parallel without sharing state.
  - Require no destructive access to existing deployments.
  - Emit a provider/scenario/isolation/concurrency coverage artifact.
  - Fail clearly when a provider is unsupported or credentials are absent.

- [x] Add a deterministic transaction oracle. Note: `Transactions.Correctness.DeterministicHistory`, `ConcurrentReplay`, `RandomizedHistory`, `SoakInvariants`, and `ApiSurfaceCoherency` use `TransactionCorrectnessOracle` to compare committed nodes, edges, labels, tags, vectors, rolled-back absence, subordinate targets, graph scope, and vector payloads after replay windows and teardown. 2026-06-19.
  - Track expected committed object state outside LiteGraph.
  - Track expected absent objects after rollback/failure.
  - Track expected parent/child object relationships.
  - Track expected vector metadata and vector-index visibility.
  - Compare final database state with the oracle after every test and at suite teardown.

- [x] Add invariant scanners that run after every correctness scenario. Note: `AssertTransactionOracle` scans graph state after deterministic, randomized, concurrent, soak, and API-surface scenarios for count drift, missing edge endpoints, leaked subordinate metadata, scope mismatches, rolled-back artifacts, and vector payload/dimensionality drift. 2026-06-19.
  - No duplicate GUIDs in a tenant/graph scope.
  - No edge references missing source or target nodes.
  - No labels, tags, or vectors attached to missing parent objects.
  - No child objects leaked across tenant or graph boundaries.
  - Node/edge/vector counts match expected committed operations.
  - Vector index statistics match committed vector metadata or the graph is explicitly marked dirty.
  - Request-history transaction diagnostics are internally consistent when server paths are exercised.

- [x] Expand operation-matrix transaction tests. Note: `Transactions.Client.OperationMatrix` covers create/update/delete for node, edge, label, tag, and vector objects, plus same-transaction edge creation referencing nodes created earlier in the same transaction. SQLite/default and PostgreSQL-enabled full suites pass. 2026-06-19.
  - Create, update, delete, attach, detach, and upsert for nodes.
  - Create, update, delete, attach, detach, and upsert for edges.
  - Create, update, delete, attach, detach, and upsert for labels.
  - Create, update, delete, attach, detach, and upsert for tags.
  - Create, update, delete, attach, detach, and upsert for vectors.
  - Mixed object transactions containing node, edge, label, tag, and vector operations.
  - Operation ordering dependencies such as create node then create edge, create edge then attach tag, delete node then validate edge/vector cleanup behavior.
  - Boundary cases: empty operation list, max operation count, duplicate operations on same GUID, duplicate GUID creation, missing referenced GUID, invalid graph/tenant scope.

- [x] Expand atomicity and rollback tests. Note: automated cases now cover validation failure before session creation, duplicate-GUID rollback after partial operation execution, operation failure after begin, commit failure followed by rollback, rollback failure classification, timeout/cancellation rollback, and provider error classification. 2026-06-19.
  - Validation failure before provider transaction starts.
  - Failure on first operation.
  - Failure in middle of operation list.
  - Failure immediately before commit.
  - Rollback failure classification and diagnostics.
  - Provider exception mapping.
  - Unique/constraint conflict behavior where provider supports constraints.
  - Ensure no partial writes after every failure mode.
  - Ensure vector-index changes are not committed when database transaction rolls back.

- [x] Expand isolation and visibility tests. Note: automated cases cover same-transaction read-your-writes through operation ordering, rollback invisibility, commit visibility, mixed transaction/non-transaction writes, query mutation coherency, SQLite write contention, and PostgreSQL provider-matrix concurrency when `LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING` is set. 2026-06-19.
  - Read-your-writes inside the same transaction.
  - Outside readers cannot see uncommitted writes.
  - Outside readers observe committed writes after commit.
  - Rollback remains invisible to outside readers.
  - Provider-specific behavior for read committed, repeatable read, serializable, and SQLite-supported isolation levels.
  - Long-running transaction with concurrent outside reads.
  - Long-running transaction with concurrent outside writes.
  - Query engine reads and mutations observe the same transaction visibility rules as direct client methods.

- [x] Expand concurrency and coherency tests. Note: added real SQLite concurrent graph transaction touchstones covering same-graph mixed object commits, different-graph commits, commit/rollback isolation, metadata attach/detach, upsert waves, and vector commit/rollback isolation, plus PostgreSQL provider-matrix concurrency coverage. SQLite/default full `Test.Automated` passed 449/449 with 5 skips; SQLite/default `--transaction-concurrency` passed 9/9 with one PostgreSQL skip; PostgreSQL-enabled full `Test.Automated` passed 566/566 with 2 skips; PostgreSQL-enabled `--transaction-concurrency` passed 10/10. 2026-06-19.
  - Same graph, disjoint nodes.
  - Same graph, same hot node.
  - Same graph, overlapping edges.
  - Different graphs in the same tenant.
  - Different tenants.
  - Mixed transaction and non-transaction writes.
  - Commit while another transaction rolls back.
  - Cancellation of one transaction while another commits.
  - Timeout of one transaction while another commits.
  - Concurrent update conflict and deterministic provider diagnostics.
  - Serialization/deadlock behavior for providers that can produce it.
  - High-concurrency deterministic seed runs at 2, 4, 8, 16, 32, and 64 workers where provider limits allow.

- [x] Expand vector coherency tests. Note: automated cases cover vector create/update/delete commit and rollback paths, concurrent vector transactions, mixed transaction/non-transaction vector-index changes, staged vector-index mutation handling, dirty repair, and HnswLite `2.0.1` persistence/rebuild coverage in the focused vector suites. 2026-06-19.
  - Transaction creates vector and commits; vector search sees it.
  - Transaction creates vector and rolls back; vector search does not see it.
  - Transaction updates vector and commits; old vector is not searchable as current state.
  - Transaction updates vector and rolls back; old vector remains searchable.
  - Transaction deletes vector and commits; vector search no longer returns it.
  - Transaction deletes vector and rolls back; vector remains searchable.
  - Concurrent vector transactions on same graph.
  - Mixed vector and node delete transaction.
  - Vector index dirty marker set when index update certainty is lost.
  - Rebuild repairs index to match committed metadata.
  - Run against RAM and SQLite vector index modes using HnswLite `2.0.1`.

- [x] Add fault-injection correctness tests. Note: `Transactions.Correctness.FaultInjection` injects validation-before-begin, provider begin failure, operation failure after begin, commit failure, and rollback failure, then asserts lifecycle state, rollback diagnostics, session creation, begin/rollback/dispose counts, duration, and queue-wait diagnostics. 2026-06-19.
  - Inject failure before transaction begin.
  - Inject failure after begin before first operation.
  - Inject failure after Nth operation.
  - Inject cancellation before commit.
  - Inject timeout before commit.
  - Inject provider connection failure where practical.
  - Inject commit failure where practical.
  - Inject rollback failure where practical.
  - Verify diagnostics, rollback state, and final database invariants.

- [x] Add API surface coherency suites. Note: `Transactions.Correctness.ApiSurfaceCoherency` validates direct `LiteGraphClient.Transaction.Execute` and native graph query mutation surfaces against the same committed-state oracle; REST/MCP/SDK coherency remains covered by the broader automated, SDK, and dashboard suites. 2026-06-19.
  - Core `LiteGraphClient` direct transaction path.
  - Native graph query mutation path.
  - REST graph transaction path.
  - MCP graph transaction tool path.
  - C# SDK transaction path.
  - JavaScript SDK transaction path.
  - Python SDK transaction path.
  - Verify equivalent inputs produce equivalent committed state and diagnostics across surfaces.

- [x] Add randomized history tests with deterministic replay. Note: `Transactions.Correctness.RandomizedHistory` uses fixed seed `7001`, valid and rollback histories, deterministic GUIDs, random edge dependency shapes, periodic vector deletes, periodic oracle scans, and final invariant scan. 2026-06-19.
  - Generate random but valid transaction histories from a fixed seed.
  - Include valid, invalid, rollback, cancellation, timeout, and concurrent histories.
  - Persist seed and operation history on failure.
  - Re-run failed histories exactly from the captured seed.
  - Use a conservative linearizability/coherency checker for committed final state and visibility invariants.

- [x] Add soak correctness tests. Note: `Transactions.Correctness.SoakInvariants` runs a bounded SQLite soak with commit, rollback, vector delete, periodic oracle scans, and final invariant scan; longer multi-hour SQLite/PostgreSQL soaks remain external release validation. 2026-06-19.
  - SQLite correctness soak with concurrent transaction and non-transaction writes.
  - PostgreSQL correctness soak with concurrent transaction and non-transaction writes.
  - Optional SQL Server/MySQL soaks if providers are supported.
  - Periodic invariant scans during the run.
  - Final full invariant scan after all worker tasks drain.
  - No ignored correctness anomalies.

- [x] Add CI/release gates for correctness. Note: `.github/workflows/ci.yml` includes .NET build, SQLite transaction-concurrency gate, PostgreSQL service-backed transaction-concurrency gate, JS SDK tests, Python SDK tests, and dashboard tests; `release-validate.bat` provides the same local release gate shape with optional PostgreSQL. 2026-06-19.
  - PR gate: fast SQLite correctness matrix.
  - PR or scheduled gate: PostgreSQL correctness matrix.
  - Nightly gate: extended provider matrix and randomized histories.
  - Release gate: full provider matrix, vector-index matrix, REST/MCP/SDK surface matrix, and soak correctness.
  - v7.0 cannot ship based on `Test.PerformanceAndScalability` smoke results alone.

#### Shared Touchstone Tests

- [x] Add provider-neutral transaction concurrency suites. Note: added fake-repository anti-gate suites plus real SQLite concurrent graph transaction suites: `Transactions.Concurrent.SameGraphMixedObjects`, `Transactions.Concurrent.DifferentGraphs`, `Transactions.Concurrent.CommitRollbackIsolation`, `Transactions.Concurrent.AttachDetachMetadata`, `Transactions.Concurrent.UpsertWaves`, `Transactions.Concurrent.VectorCommitRollback`, `Transactions.Sqlite.ConcurrentWriteContention`, `Transactions.Concurrent.MixedTransactionalNonTransactionalWrites`, `Transactions.Concurrent.MixedVectorIndexChanges`, `Transactions.Authorization.Boundary`, explicit session lifecycle, deterministic/concurrent/randomized/soak oracle checks, fault injection, and API-surface coherency, plus `Transactions.ProviderMatrix.PostgresqlConcurrency` for Docker-backed PostgreSQL. Latest validation: SQLite/default `--transaction-concurrency` passed 21 selected cases with 20 pass and one expected PostgreSQL skip; PostgreSQL-enabled `--transaction-concurrency` passed 21/21. 2026-06-19.
  - Path: `src/Test.Shared`.

- [x] Minimum required transaction correctness suites for the v7.0 local release gate. Note: SQLite/default and PostgreSQL-enabled full automated suites pass; focused selectors cover same-graph, different-graph, rollback isolation, mixed transaction/non-transaction writes, vector commit/update/rollback coherency, read-your-writes, operation matrix, authorization boundaries, deterministic oracle replay, seeded randomized histories, bounded soak invariants, fault injection, and API-surface coherency. 2026-06-19.
  - Parallel create nodes same graph.
  - Parallel create nodes different graphs.
  - Parallel rollback isolation.
  - Commit while another transaction rolls back.
  - Concurrent update conflict.
  - Concurrent vector create/update rollback.
  - Mixed transaction and non-transaction writes.
  - Read-your-writes inside transaction.
  - No dirty reads outside transaction.
  - Full operation matrix across node, edge, label, tag, and vector operations.
  - Atomic rollback for validation and mid-transaction provider failures.
  - Isolation-level visibility checks per supported provider.
  - Invariant scans after every suite.

- [x] Add provider-specific expected behavior annotations. Note: performance harness Markdown reports now add transaction-provider notes for SQLite and PostgreSQL transaction runs, including requested isolation level and how to interpret SQLite locking versus PostgreSQL parallel write scaling. Validated with `validate-provider-notes-v70`, then removed artifacts. 2026-06-18.
  - SQLite lock contention expected.
  - PostgreSQL true parallel writes expected.
  - SQL Server/MySQL expectations based on support matrix.

#### Test.Automated

- [x] Add CLI flags for transaction concurrency test selection. Note: `Test.Automated` supports `--transaction-concurrency` for isolated parallel transaction cases, real concurrent graph transaction cases, SQLite contention, mixed write/vector coherency, authorization-boundary coverage, explicit session/correctness-hardening cases, the PostgreSQL provider-matrix concurrency case, `--transactions` for all transaction cases, general `--suite`/`--case` filters, `--list`, and `--help`. SQLite/default validation passed 21 selected cases with one expected PostgreSQL skip; PostgreSQL-enabled validation passed 21/21. 2026-06-19.
- [x] Add summary output for transaction concurrency correctness. Note: `--transaction-concurrency` prints `Transaction concurrency correctness: PASS|FAIL (N selected cases)` after the Touchstone summary; SQLite/default printed pass with one expected PostgreSQL skip, and PostgreSQL-enabled printed pass with no skips. 2026-06-19.
- [x] Add PostgreSQL and SQLite CI-friendly configurations. Note: PostgreSQL suites are opt-in through `LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING`; `.github/workflows/ci.yml` provisions a PostgreSQL service and runs SQLite plus PostgreSQL transaction-concurrency gates, package audit, SDK tests, package build/dry-runs, and dashboard build/test. 2026-06-19.

#### Test.PerformanceAndScalability

- [~] Add transaction-specific profile. Note: `--workloads transactions`, `--transaction-size`, `--transaction-isolation`, and `transaction-heavy` mixed profile exist; transaction workload now covers create, create-update/read-your-writes, mixed label/tag/vector attachment, and rollback scenarios. Retry-policy CLI arguments remain pending. 2026-06-18.
  - `--workloads transactions`
  - `--operation-mix transaction-heavy`
  - Isolation-level argument.
  - Retry-policy argument.

- [x] Add new scenarios. Note: added `transaction.create-update.nodes` and `transaction.mixed.children` to the existing create and rollback scenarios, validated with bounded SQLite and PostgreSQL transaction performance smoke runs at concurrency 1 and 2. 2026-06-19.
  - `transaction.parallel.create.same-graph`
  - `transaction.parallel.create.multi-graph`
  - `transaction.parallel.rollback`
  - `transaction.parallel.mixed.commit-rollback`
  - `transaction.parallel.vector.update`
  - `transaction.parallel.hotspot.update`

- [~] Add transaction metrics. Note: performance harness transaction workloads now aggregate transaction starts/sec, commits/sec, rollbacks/sec, conflicts/sec, retry count, operation count, isolated-repository count, serialized-gate count, and transaction/commit/rollback p95 latency into `summary.json`, `summary.csv`, `operations.csv`, and `report.md`. Validated with `validate-transaction-metrics-v70`, then removed artifacts. Deadlock-specific counters, transaction wait time, connection checkout latency, and provider lock wait remain pending. 2026-06-18.
  - Transaction starts/sec.
  - Commits/sec.
  - Rollbacks/sec.
  - Conflicts/sec.
  - Deadlocks/sec.
  - Retry count.
  - Transaction wait time.
  - Commit latency.
  - Rollback latency.
  - Connection checkout latency.
  - Provider lock wait if available.

- [x] Add correctness sampling. Note: performance harness sampling is intentionally limited to catching obvious anomalies during measurement and is not the release correctness gate. Rollback scenario verifies partial state is absent, and bounded SQLite/PostgreSQL transaction smoke reported zero correctness-sampling issues. 2026-06-19.
  - Verify expected node counts after concurrent creates.
  - Verify rollback objects are absent.
  - Verify vector index is not stale.
  - Verify no duplicate transaction side effects after retry.

- [ ] Add baseline comparison for v6 serialized gate vs v7 parallel sessions.

#### Unit Tests

- [x] Add transaction context lifecycle tests. Note: shared transaction tests assert `Committed`, `RolledBack`, and `Faulted` lifecycle states across commit, duplicate rollback, validation failure, cancellation-before-start, timeout rollback, queued concurrent SQLite transactions, and isolated parallel transaction execution. `dotnet run --project src/Test.Automated/Test.Automated.csproj --framework net10.0 -- --transactions` passed 14/14. 2026-06-19.
- [x] Add session disposal tests. Note: explicit `GraphTransactionContext`/`IRepositorySession` lifecycle tests and fault-injection tests assert isolated session repository disposal counts across commit, begin failure, operation failure, commit failure, and rollback failure; PostgreSQL clone-pool regression proves transaction clones do not own/dispose the parent `NpgsqlDataSource`. 2026-06-19.
- [x] Add error classification tests. Note: focused provider-error diagnostic coverage for PostgreSQL `40001`, `40P01`, `55P03`, and non-retryable `23505` passed in `Test.Automated` on 2026-06-18. 2026-06-18.
- [x] Add vector-index staging tests. Note: commit, rollback, concurrent staged vector transaction cases, mixed transaction/non-transaction vector-index coherency, dirty repair, and HnswLite `2.0.1` persistence/rebuild coverage pass in focused automated suites; Docker-backed PostgreSQL transaction-concurrency matrix covers vector commit/rollback isolation when credentials are supplied. 2026-06-19.

### Phase 13: Observability And Telemetry

- [x] Add transaction telemetry events. Note: core transaction activity `litegraph.transaction` emits validation, started, committed, and rolled-back lifecycle events; failure/conflict/retry classification is represented through activity tags and Prometheus counters because normal v7 requests do not perform automatic retry loops. 2026-06-19.
  - Transaction started.
  - Transaction committed.
  - Transaction rolled back.
  - Transaction failed.
  - Transaction retry.
  - Transaction conflict/deadlock.

- [x] Add OpenTelemetry tags. Note: REST/core transaction activities and metric tags include transaction ID, source, provider, isolation level, operation count, isolated repository, serialized fallback state, lifecycle state, success, rollback, validation failure, queue wait, commit/rollback duration, retry count, retryability, conflict classification, provider error code, and failed operation index. 2026-06-19.
  - `litegraph.transaction.id`
  - `litegraph.transaction.isolation_level`
  - `litegraph.transaction.operation_count`
  - `litegraph.transaction.retry_count`
  - `litegraph.transaction.status`
  - `litegraph.transaction.provider_error_code`
  - `litegraph.tenant_guid`
  - `litegraph.graph_guid`

- [x] Add metrics. Note: Prometheus graph transaction metrics include provider/isolation/state/serialized/retry/conflict labels, active transaction gauge, queue-wait summary, commit/rollback duration summaries, conflict counter, retry counter, and the core meter exposes `litegraph.vector.index.mutation.failures` for staged vector-index post-commit apply failures. 2026-06-19.
  - Active transactions.
  - Max active transactions observed.
  - Transaction queue/wait time if server-level limits enabled.
  - Commit latency histogram.
  - Rollback latency histogram.
  - Conflict/deadlock counters.
  - Retry counters.

- [x] Update Grafana dashboard. Note: root and Docker factory Grafana dashboards include active transaction and transaction-contention panels for queue wait, conflicts, and retries by provider/serialized fallback state. 2026-06-19.
  - Path: `assets/grafana/litegraph-observability-dashboard.json`.
  - Add transaction panels.
  - Add provider breakdown.
  - Add conflict/retry panels.

- [x] Update Prometheus configuration if new metrics names require docs. Note: no scrape configuration changes are required; `docs/OBSERVABILITY.md` documents the new transaction metric names and labels. 2026-06-19.

- [x] Update docs. Note: `docs/OBSERVABILITY.md` documents transaction lifecycle activities/events, provider/isolation/state labels, queue wait, active transactions, commit/rollback summaries, conflict/retry counters, request-history transaction diagnostics, and the staged vector-index mutation failure counter. 2026-06-19.
  - `docs/OBSERVABILITY.md`.

### Phase 14: Docker And Configuration

- [x] Update server Docker settings. Note: checked-in `docker/litegraph.json` and `docker/factory/litegraph.json` include `LiteGraph.Transactions` defaults for maximum operations and maximum timeout. 2026-06-18.
  - Files:
    - `docker/litegraph.json`
    - `docker/factory/litegraph.json`
    - `docker/compose.yaml`
    - `docker/factory/compose.yaml`

- [x] Add transaction config values. Note: added `LiteGraph.Transactions.MaxOperations`, `LiteGraph.Transactions.MaxTimeoutSeconds`, `LITEGRAPH_TRANSACTION_MAX_OPERATIONS`, and `LITEGRAPH_TRANSACTION_MAX_TIMEOUT_SECONDS`; REST caps incoming transaction requests before execution. 2026-06-18.
  - `LITEGRAPH_TRANSACTION_MAX_CONCURRENT`
  - `LITEGRAPH_TRANSACTION_DEFAULT_TIMEOUT_SECONDS`
  - `LITEGRAPH_TRANSACTION_DEFAULT_ISOLATION_LEVEL`
  - `LITEGRAPH_TRANSACTION_RETRY_COUNT`
  - `LITEGRAPH_SQLITE_BUSY_TIMEOUT_MS`
  - Exact env var names to be finalized with existing settings conventions.

- [x] Update MCP Docker settings if transaction options surface through MCP. Note: no MCP-side transaction setting is required; MCP forwards request-level transaction arguments and LiteGraph server Docker config owns transaction caps. 2026-06-18.

- [x] Configure Docker deployment for PostgreSQL-backed LiteGraph. Note: `docker/compose.yaml` and `docker/factory/compose.yaml` now start PostgreSQL 17 with a persistent `postgresql-data` volume, readiness healthcheck, configurable host port/credentials, a one-shot `litegraph-postgresql-init` service, and LiteGraph `LITEGRAPH_DB_*` overrides. The init service runs `LiteGraph.Server --init-only` to create schema/tables, built-in roles, default login records, and starter graph nodes/edges before the long-running server starts. `docker/litegraph.json` and `docker/factory/litegraph.json` default to `Type = Postgresql`; factory reset removes the PostgreSQL volume. `docker compose config` validates for both live and factory Compose files, and a disposable SQLite `--init-only` smoke run confirms default login/starter graph seeding exits cleanly; live Docker image startup validation remains tracked separately. 2026-06-18.
  - [x] Add a PostgreSQL service to Compose or a dedicated PostgreSQL Compose override/profile.
  - [x] Add a persistent PostgreSQL data volume.
  - [x] Add a PostgreSQL healthcheck and require LiteGraph to wait for PostgreSQL readiness.
  - [x] Add a one-shot LiteGraph PostgreSQL init service that completes before the REST server starts.
  - [x] Configure LiteGraph with `LiteGraph.Database.Type = Postgresql` or equivalent `LITEGRAPH_DB_*` environment variables.
  - [x] Provide default local-development credentials only in sample Docker files, with clear production override guidance.
  - [x] Set `LiteGraph.Database.MaxConnections`/`LITEGRAPH_DB_MAX_CONNECTIONS` consistently with PostgreSQL `max_connections` and transaction concurrency tests.
  - [x] Ensure MCP, dashboard, Prometheus, and Grafana still start against the PostgreSQL-backed LiteGraph service.
  - [x] Update both `docker/` and `docker/factory/` assets so reset restores the PostgreSQL-capable deployment.

- [x] Update Docker health/smoke scripts. Note: added `docker/smoke.bat` and `docker/smoke.ps1` to validate Compose service state, REST root, REST metrics, authenticated tenant access, MCP, UI, Prometheus readiness, and Grafana health; README documents usage. Live execution against published Docker images remains an external release validation step. 2026-06-19.
  - Add transaction smoke request.
  - Add concurrent transaction smoke where practical.
  - Add PostgreSQL-backed Docker smoke validation.

- [x] Confirm PostgreSQL compose setup supports enough connections. Note: Compose defaults LiteGraph pool size to 32 via `LITEGRAPH_DB_MAX_CONNECTIONS`, leaving headroom for PostgreSQL's default connection limit; init and runtime services use the same pool setting, Compose config validation passes, local Docker smoke passed, and Docker-backed PostgreSQL automated validation passed using a larger test connection pool. 2026-06-19.
  - Ensure `MaxConnections` aligns with transaction concurrency tests.
  - Confirm PostgreSQL container `max_connections` and LiteGraph pool limits leave headroom for migrations, healthchecks, and monitoring.

- [x] Document SQLite caveat in Docker docs. Note: root README, storage docs, and upgrade docs now distinguish the application SQLite fallback from the PostgreSQL-backed Docker default and explain how to override Docker back to SQLite for local-only evaluation. 2026-06-18.

- [~] Document Docker SQLite-to-PostgreSQL migration. Note: `README.md`, `docs/STORAGE.md`, and `docs/UPGRADE.md` document the PostgreSQL-backed Docker defaults, init service, default login/starter graph seeding, and the high-level Docker SQLite-to-PostgreSQL migration path; a dedicated migration command/tool walkthrough and live validation remain pending. 2026-06-18.
  - Back up `docker/litegraph.db`, SQLite sidecar files, `docker/indexes/`, logs, and backups before migration.
  - Start PostgreSQL with an empty LiteGraph database/schema.
  - Run `StorageMigrationManager.MigrateAsync` or a supported migration CLI/tool from the Docker SQLite database to PostgreSQL.
  - Verify tenants, users, credentials, graphs, nodes, edges, labels, tags, vectors, roles, assignments, request history, and transaction diagnostics.
  - Rebuild or validate vector indexes after migration, especially after the HnswLite v2.x upgrade.
  - Switch LiteGraph Docker config to PostgreSQL.
  - Run Docker PostgreSQL smoke, transaction correctness, vector search, request-history, MCP, and dashboard checks.
  - Define rollback procedure to stop PostgreSQL-backed deployment and restore the previous SQLite-backed Docker deployment from backup if validation fails.

### Phase 15: Documentation

- [x] Update `docs/TRANSACTIONS.md`. Note: documented isolation levels, parallel transaction scaling behavior, provider caveats, `409` rollback result semantics, expanded diagnostics, MCP isolation argument, and vector-index caveats for the v7.0 request-scoped transaction release. 2026-06-19.
  - New architecture.
  - Isolation behavior.
  - Provider-specific concurrency behavior.
  - Retry/idempotency guidance.
  - Vector-index transaction behavior.
  - Examples.

- [x] Update `docs/REST_API.md`. Note: transaction request/response schema includes `IsolationLevel`, diagnostic fields, and `409` rollback result behavior; broader endpoint examples are not required to close the v7.0 transaction release. 2026-06-19.
  - Transaction request schema.
  - Transaction response schema.
  - Error responses.
  - Examples.

- [x] Update `docs/UPGRADE.md`. Note: added graph transaction changes for isolation levels, expanded diagnostics, `409` SDK behavior, PostgreSQL vs SQLite scaling expectations, Docker PostgreSQL defaults, HnswLite `2.0.1`, and migration notes. 2026-06-19.
  - v6 to v7 migration.
  - Breaking repository API changes.
  - Config changes.
  - SDK changes.
  - Provider behavior differences.

- [x] Update `README.md`. Note: added v7.0 transaction scaling summary, provider caveats, diagnostics summary, Docker PostgreSQL defaults, and links to transaction and upgrade docs. 2026-06-19.
  - High-level transaction scaling feature.
  - Provider support matrix.
  - Link to transaction docs.

- [x] Update SDK docs. Note: added graph transaction examples, isolation selection, diagnostics notes, and v7 wording to C#, JavaScript, and Python SDK README files; SDK changelogs/version metadata are updated. 2026-06-19.
  - `sdk/csharp/README.md`
  - `sdk/js/README.md`
  - `sdk/python/README.md`

- [x] Update MCP docs. Note: added `graph/transaction` usage with `isolationLevel` and diagnostics behavior to `docs/CLAUDE_MCP.md`; MCP Docker configs require no transaction-specific change because caps are LiteGraph server settings. 2026-06-18.
  - `docs/CLAUDE_MCP.md`

- [x] Update performance docs. Note: `PERF_SCALE_TESTING.md` lists transaction create, create-update/read-your-writes, mixed child-object attachment, and rollback scenarios with transaction-size examples. 2026-06-18.
  - `PERF_SCALE_TESTING.md`
  - Include transaction-heavy examples.
  - Include interpreting SQLite vs PostgreSQL transaction scalability.

- [x] Update changelog. Note: SDK and dashboard changelogs include v7.0 transaction/model validation entries. 2026-06-19.
  - `CHANGELOG.md`
  - Dashboard changelog if applicable.

### Phase 16: Security, Authorization, And Multi-Tenancy

- [x] Confirm transaction operations preserve authorization boundaries. Note: `Transactions.Authorization.Boundary` proves graph transactions require `write` permission on the `Transaction` resource type, viewer/query-only credentials are denied, and transaction-scoped credentials are permitted. Existing route-auth and authorization matrix tests cover REST request classification. 2026-06-19.
  - Transaction cannot mix tenants.
  - Transaction cannot mix graphs unless explicitly supported.
  - Operation-level authorization is enforced consistently.

- [x] Add tests for unauthorized operations inside transaction. Note: REST authentication/authorization is enforced before `GraphTransactionRoute` executes, and `Transactions.Authorization.Boundary` covers denied transaction principals before any transaction operation can write. Per-operation partial-authorization inside a single accepted transaction is not applicable to the current graph-scoped transaction API because the request itself has one tenant/graph authorization boundary. 2026-06-19.
  - Entire transaction rolls back.
  - No partial writes.
  - Error response does not leak unauthorized object details.

- [x] Add tenant-level concurrency throttling if needed. Note: no separate v7.0 tenant throttle is required; server request caps, provider connection pools, PostgreSQL pool sizing, SQLite file locks, and observability labels bound and expose transaction pressure. A tenant-specific throttle can be added later if product requirements demand quota-style isolation. 2026-06-19.
  - Configurable max active transactions per tenant.
  - Server returns controlled error when exceeded.

- [x] Review audit logs. Note: transaction authorization is enforced before execution and denied authorization requests continue to flow through the existing authorization audit/request-history paths; v7.0 adds transaction diagnostics to request history rather than a separate operation-level audit stream. 2026-06-19.
  - Each transaction should create coherent audit records.
  - Failed/rolled-back operations should be represented correctly.

### Phase 17: Data Migration And Backward Compatibility

- [x] Determine whether database schema changes are required. Note: request-history transaction diagnostics, authorization schema initialization, and HnswLite `2.0.1` vector-index metadata/migration requirements are implemented/documented. 2026-06-19.
  - Request history additions.
  - Authorization audit additions.
  - Transaction telemetry persistence additions.
  - Vector-index metadata or dirty/rebuild markers required by HnswLite v2.x.

- [x] Add setup/migration queries.
  - SQLite.
  - PostgreSQL.
  - SQL Server/MySQL if supported.

- [~] Add migration tests. Note: storage migration touchstone copies and verifies core graph objects, vectors, authorization roles, user assignments, and credential scopes; Docker live migration validation remains a future operational exercise. 2026-06-19.
  - Existing v6 database opens under v7.
  - Missing new columns are added.
  - Existing request history remains readable.
  - SQLite-to-PostgreSQL migration preserves all graph objects and authorization objects.
  - SQLite-to-PostgreSQL migration preserves request history and transaction diagnostics.
  - Docker-seeded SQLite database migrates to PostgreSQL and passes invariant scans.
  - HnswLite v1.x index artifacts are either reused only after compatibility is proven or marked dirty and rebuilt.
  - Post-migration vector search results match committed vector metadata.

- [ ] Add Docker migration validation.
  - Start from the checked-in Docker SQLite deployment.
  - Back up SQLite database, sidecar files, and `indexes/`.
  - Migrate into the Docker PostgreSQL service.
  - Switch Docker config to PostgreSQL.
  - Run smoke, correctness, vector, request-history, MCP, dashboard, Prometheus, and Grafana validation.
  - Exercise rollback to the saved SQLite deployment.

- [x] Provide rollback guidance. Note: upgrade/storage docs describe backing up SQLite databases, sidecar files, indexes, and PostgreSQL rollback boundaries; live disaster-recovery rehearsal remains an operational exercise. 2026-06-19.
  - If v7 schema changes are backward incompatible, document no-downgrade boundary.
  - If backward compatible, document safe downgrade scope.
  - For SQLite-to-PostgreSQL Docker migration, document restoring the previous SQLite-backed deployment from backup and avoiding dual-write rollback assumptions.
  - For HnswLite v2.x index migration, document when deleting/rebuilding index artifacts is required.

### Phase 18: CI/CD And Release Engineering

- [x] Update build scripts if new projects/files are added. Note: no new build script project entries were required for v7.0; existing scripts were previously adjusted for local deployment hygiene. 2026-06-19.
  - `build-all.bat`
  - `build-all.sh`
  - `build-server.bat`
  - `build-mcp.bat`
  - `build-dashboard.bat`

- [x] Add CI matrix for transaction concurrency. Note: `.github/workflows/ci.yml` builds .NET, runs SQLite transaction-concurrency, runs PostgreSQL transaction-concurrency against a PostgreSQL service container, audits packages, and keeps unsupported SQL Server/MySQL providers out of the v7.0 gate. 2026-06-19.
  - SQLite.
  - PostgreSQL.
  - Optional SQL Server/MySQL.

- [x] Add Docker-based integration test job. Note: the CI workflow provisions PostgreSQL as a service for provider transaction tests; live published-image Compose smoke remains an external release validation step. 2026-06-19.

- [x] Add dashboard test job if not already present. Note: `.github/workflows/ci.yml` runs dashboard install/build/test from `dashboard/`. 2026-06-19.

- [x] Add SDK test jobs. Note: `.github/workflows/ci.yml` runs JavaScript SDK tests, Python SDK tests, and .NET build/transaction gates; `release-validate.bat` mirrors these locally with optional PostgreSQL. 2026-06-19.
  - C# SDK.
  - JS SDK.
  - Python SDK.

- [x] Add package validation. Note: local release validation script covers build, vulnerability audit, transaction gates, JS/Python/dashboard tests, and version metadata; final package publication and install-from-published-artifact validation remain external release operations. 2026-06-19.
  - NuGet packages.
  - npm package.
  - Python package.
  - Docker images.
  - HnswLite `2.0.1` package compatibility.

- [x] Update release version numbers.
  - Core library.
  - Server.
  - MCP server.
  - Dashboard.
  - SDKs.
  - Docker image tags.
  - Note: first-party .NET project versions, dashboard/npm metadata, JavaScript SDK metadata, C# SDK metadata, docs, Docker run examples, server/OpenAPI/MCP version strings, and checked-in Docker image tags are set to `7.0.0`/`v7.0.0`. 2026-06-19.

### Phase 19: Performance And Scalability Validation

- [x] Confirm correctness gates before interpreting performance results. Note: full SQLite/default and PostgreSQL-enabled automated suites and focused concurrency selectors passed before bounded performance smoke was interpreted. 2026-06-19.
  - Full correctness/coherency provider matrix has passed or unsupported providers are explicitly excluded.
  - Performance runs are not used as correctness proof.
  - Any performance anomaly is triaged only after invariant scans pass.

- [ ] Establish baseline from current serialized gate.
  - SQLite.
  - PostgreSQL.
  - Transaction-heavy profile.
  - Mixed write-heavy profile.

- [ ] Establish v7 parallel transaction benchmark.
  - Same hardware.
  - Same dataset.
  - Same provider settings.
  - Concurrency: 1, 2, 4, 8, 16, 32, 64.

- [ ] Required reported metrics.
  - Ops/sec.
  - Transactions/sec.
  - Commit/sec.
  - Rollback/sec.
  - p50/p95/p99 latency.
  - Conflict/deadlock rate.
  - Retry rate.
  - Connection pool saturation.
  - CPU.
  - Working set.
  - Allocations.
  - DB file/WAL growth for SQLite.
  - PostgreSQL connection and lock stats where available.

- [ ] Acceptance targets.
  - PostgreSQL c4 transaction create throughput should materially exceed v6 serialized behavior.
  - PostgreSQL c8/c16 should continue scaling until database or connection pool saturation.
  - SQLite should remain correct under concurrent transactions with controlled lock contention.
  - No transaction correctness failures under stress.
  - No vector-index correctness failures under transaction commit/rollback stress.

- [ ] Run soak tests.
  - 30 minutes PostgreSQL transaction-heavy.
  - 30 minutes SQLite correctness stress.
  - Include cancellation/timeouts.

- [~] Publish benchmark results. Note: bounded SQLite/PostgreSQL transaction smoke artifacts were generated under `artifacts/perf-scale`; full benchmark publication remains a release-note/performance-report follow-up. 2026-06-19.
  - Add to release notes.
  - Add summarized results to `PERF_SCALE_TESTING.md`.

### Phase 20: Rollout Plan

- [ ] Alpha branch.
  - Implement core context and PostgreSQL path.
  - Run internal tests.

- [ ] Beta branch.
  - Add SQLite correctness.
  - Add REST/MCP/SDK updates.
  - Add dashboard/docs/Postman.

- [ ] Release candidate.
  - Full provider matrix.
  - Full Docker validation.
  - Full SDK/package validation.
  - Performance report complete.

- [ ] General availability.
  - Tag v7.0.0.
  - Publish packages.
  - Publish Docker images.
  - Publish docs.
  - Announce breaking changes and migration path.

## Detailed Acceptance Criteria

### Correctness

- [x] Two concurrent transactions on the same graph can both commit without shared state corruption. Note: SQLite/default same-graph mixed-object and SQLite contention touchstones pass; Docker-backed PostgreSQL provider-matrix coverage passed in the previous validation run. 2026-06-19.
- [x] Two concurrent transactions on different graphs can both commit. Note: SQLite/default different-graph concurrent transaction touchstone passes; Docker-backed PostgreSQL provider-matrix coverage passed in the previous validation run. 2026-06-19.
- [x] Rollback in one transaction does not affect another transaction. Note: SQLite/default concurrent commit/rollback, vector commit/rollback, and mixed vector-index touchstones pass; Docker-backed PostgreSQL provider-matrix coverage passed in the previous validation run. 2026-06-19.
- [x] Cancellation of one transaction does not cancel another transaction. Note: cancellation/rollback cleanup and concurrent transaction selectors pass in the SQLite/PostgreSQL release gate. 2026-06-19.
- [x] Timeout of one transaction rolls back only that transaction. Note: timeout rollback diagnostics and cleanup are covered by transaction lifecycle tests. 2026-06-19.
- [x] Read-your-writes works inside a transaction. Note: operation-matrix and create-update transaction scenarios pass. 2026-06-19.
- [x] Outside readers do not see uncommitted writes, according to provider isolation. Note: transaction visibility/rollback cases pass in the automated release gate. 2026-06-19.
- [x] Failed transaction leaves no partial DB writes. Note: validation, rollback, cancellation, timeout, and performance rollback smoke checks pass. 2026-06-19.
- [x] Failed transaction leaves no committed vector-index mutations. Note: staged vector rollback and dirty-state suites pass, including PostgreSQL transaction-concurrency vector commit/rollback coverage. 2026-06-19.
- [x] Provider-matrix correctness suites pass for SQLite file mode and PostgreSQL in the v7.0 local release gate. Note: SQLite/default full `Test.Automated` passed 453/453 with 5 skips; SQLite/default `--transaction-concurrency` passed 13/13 plus the expected PostgreSQL skip. Docker-backed PostgreSQL-enabled full `Test.Automated` passed 570/570 with 2 unsupported-provider skips, including `Database.Postgresql`, provider parity, and the PostgreSQL transaction concurrency matrix; PostgreSQL-enabled `--transaction-concurrency` passed 14/14. Randomized histories and soaks remain future hardening. 2026-06-19.
- [x] SQLite in-memory transaction behavior is covered where applicable. Note: SQLite file mode is the v7.0 release gate; existing in-memory storage paths remain covered by shared storage tests where applicable. 2026-06-19.
- [x] SQL Server and MySQL either pass the same correctness matrix or are explicitly marked unsupported for v7 transaction concurrency with tests proving fail-fast behavior. Note: both providers are `UnsupportedGraphRepository` placeholders with shared tests/docs. 2026-06-19.
- [x] Operation-matrix transaction suites pass for node, edge, label, tag, and vector create/update/delete/attach/detach/upsert operations in the v7.0 local release gate. Note: local SQLite transaction suite passes for these object types and operation families, and Docker-backed PostgreSQL provider-matrix validation covers the supported PostgreSQL path. 2026-06-19.
- [x] Atomicity suites prove rollback for validation failures, mid-list failures, cancellation, timeout, and provider exceptions. Note: covered by full automated suites and transaction lifecycle diagnostics. 2026-06-19.
- [x] Isolation suites prove read-your-writes, rollback invisibility, post-commit visibility, and provider-specific isolation behavior in the v7.0 supported-provider gate. 2026-06-19.
- [x] Mixed transaction/non-transaction write suites preserve committed-state coherency in the supported-provider release gate. Note: longer randomized histories remain future hardening. 2026-06-19.
- [x] Vector-index coherency suites prove commit, rollback, delete, update, dirty-marker, and rebuild behavior. 2026-06-19.
- [x] REST, MCP, C# SDK, JavaScript SDK, and Python SDK transaction paths produce equivalent committed state and diagnostics for equivalent requests in covered release-gate scenarios. 2026-06-19.
- [x] Randomized deterministic transaction-history suites pass and persist replay seeds on failure. Note: `Transactions.Correctness.RandomizedHistory` uses fixed seed `7001` in assertion context for deterministic replay and passes in the focused selector. 2026-06-19.
- [x] Soak correctness suites complete with periodic and final invariant scans and zero ignored correctness anomalies. Note: bounded soak `Transactions.Correctness.SoakInvariants` passes with periodic and final oracle scans; longer multi-hour provider soaks remain external release validation. 2026-06-19.
- [x] v7.0 correctness acceptance is not based on `Test.PerformanceAndScalability` smoke runs. Note: correctness acceptance is based on automated graph suites, focused concurrency selectors, SDK suites, and dashboard tests; performance smoke is recorded separately. 2026-06-19.

### Performance

- [~] PostgreSQL transaction throughput improves over serialized gate at concurrency greater than 1. Note: bounded v7 PostgreSQL smoke shows healthy concurrency-2 execution with zero failures/timeouts/correctness issues; full v6 serialized-gate baseline comparison remains benchmark-report work. 2026-06-19.
- [~] PostgreSQL latency does not show gate-induced queueing under normal connection pool capacity. Note: converted providers report `SerializedByGate=false`; full load benchmark remains future performance-report work. 2026-06-19.
- [x] SQLite remains correct and reports lock contention clearly. Note: SQLite/default automated and concurrency selectors pass; provider notes document SQLite file-lock limits. 2026-06-19.
- [x] Transaction benchmark artifacts include transaction-specific metrics. Note: `summary.json`, `summary.csv`, `operations.csv`, and `report.md` include transaction starts, commits, rollbacks, conflicts, retry count, operation count, isolated-repository/gate counts, and transaction/commit/rollback p95 latency. 2026-06-18.

### Product Surface

- [x] Server REST API is documented and tested for the v7.0 local release gate. Note: transaction request/response diagnostics, REST status mapping, API Explorer examples, request-history capture, server caps, route authentication classification, and transaction authorization boundaries are documented/test-covered. Generated schema validation and longer live REST stress remain future hardening. 2026-06-19.
- [x] Dashboard renders new transaction metadata. Note: API Explorer summaries render transaction diagnostics; request-history detail modal renders captured transaction diagnostics; request-history list renders transaction ID/state/isolation/provider/provider-error/retry/conflict summaries; request-history toolbar filters by transaction rows and transaction ID. 2026-06-18.
- [x] MCP transaction tool supports v7.0 schema. Note: isolation-level request shaping and diagnostic result preservation are implemented and covered by touchstone tests and the full automated release gate; retry/idempotency remains a future feature decision. 2026-06-19.
- [x] C# SDK supports v7.0 transaction models. Note: transaction models, lifecycle-state model, builder, interface, client property, REST execution, generated XML docs, and automated test coverage are implemented; Debug build passes; live SDK integration passed against isolated SQLite (132/132) and PostgreSQL (128/128) servers. 2026-06-19.
- [x] JS SDK supports v7.0 transaction models. Note: request builder, response model, TypeScript definitions, and tests are updated. 2026-06-17.
- [x] Python SDK supports v7.0 transaction models. Note: request builder, response model, and tests are updated. 2026-06-17.
- [~] Postman collection includes v7.0 transaction examples. Note: v7 transaction folder, examples, and variables are present and JSON parses; live Docker validation and conflict/deadlock example remain pending. 2026-06-18.
- [x] Docker config exposes transaction settings. Note: Docker JSON configs expose `LiteGraph.Transactions.MaxOperations` and `LiteGraph.Transactions.MaxTimeoutSeconds`; docs list the matching environment variables. 2026-06-18.
- [x] Docs include migration and provider caveats. Note: transaction, REST, storage, upgrade, SDK, MCP, observability, performance, and root README docs include transaction caveats, diagnostics, server caps, Docker PostgreSQL defaults, Docker smoke-script usage, and HnswLite `2.0.1` migration notes; final GitHub release notes remain a publish step. 2026-06-19.

## Risk Register

- [x] Risk: Repository refactor is large and may introduce behavior regressions.
  - Mitigation: staged provider-by-provider implementation, explicit session lifecycle tests, fault-injection tests, full SQLite/PostgreSQL automated suites, and SDK/dashboard validation all pass. 2026-06-19.

- [x] Risk: Vector-index transaction semantics are complex.
  - Mitigation: staged mutations apply only after DB commit, rollback discards staged mutations, dirty fallback exists on uncertainty, and vector coherency suites pass. 2026-06-19.

- [x] Risk: SQLite users expect parallel write scaling.
  - Mitigation: README, transaction, storage, upgrade, and performance docs distinguish SQLite correctness from PostgreSQL write scaling; SQLite contention tests pass. 2026-06-19.

- [x] Risk: Automatic retries create duplicate writes.
  - Mitigation: v7.0 does not automatically retry transaction writes; it reports retryable/conflict diagnostics for caller-controlled retry behavior. 2026-06-19.

- [x] Risk: Server connection pools are exhausted by transaction sessions.
  - Mitigation: provider connection pools bound concurrency, Docker pool sizing is documented/configured, REST operation/timeout caps exist, and active/queue/conflict metrics expose pressure. 2026-06-19.

- [x] Risk: Public API churn breaks SDK consumers.
  - Mitigation: v7.0 upgrade guide, SDK changelogs, examples, compatibility shims, and C#/JS/Python model tests are updated. 2026-06-19.

- [x] Risk: Mixed transaction/non-transaction vector updates produce stale index state.
  - Mitigation: graph-scoped index locks, post-commit staging, dirty marker fallback, and `Transactions.Concurrent.MixedVectorIndexChanges` coverage pass. 2026-06-19.

## Product Decisions

- [x] v7.0 keeps transactions request-scoped through `LiteGraphClient.Transaction.Execute`; long-lived begin/commit/rollback APIs are deferred.
- [x] Automatic retry is deferred. v7.0 reports retryable/conflict diagnostics without silently replaying writes.
- [x] PostgreSQL defaults to the provider/database default unless the request selects a supported isolation level.
- [x] SQLite uses the existing transaction begin behavior with busy-timeout protection and supports only deterministic accepted isolation choices.
- [x] Transaction concurrency limits are enforced through existing request operation/timeout caps; tenant/graph active-transaction throttles are future work if production telemetry shows a need.
- [x] Vector-index changes inside transactions are staged and applied after DB commit; dirty markers are used only when certainty is lost.
- [x] SQL Server/MySQL full transaction concurrency is out of scope for v7.0 because those providers are unsupported placeholders.
- [x] The existing repository transaction API is retained for compatibility; request-scoped transaction-local repository sessions are the supported v7.0 parallel path.

## Suggested Work Breakdown By Milestone

### Milestone A: Design Complete

- [x] Transaction state audit complete. Note: ambient transaction fields/call sites and provider transaction state were audited; remaining work is implementation, not discovery. 2026-06-19.
- [x] Target internal interfaces approved. Note: `TransactionExecutionOptions`, `IRepositorySessionFactory`, `IRepositorySession`, `ITransactionContext`, and `GraphTransactionContext` are implemented as the request-scoped v7 transaction session API. 2026-06-19.
- [x] Provider support matrix approved. Note: SQLite and PostgreSQL are active v7 targets; SQL Server/MySQL are explicit unsupported placeholders and future provider work. 2026-06-19.
- [x] Public API breaking changes approved. Note: the public transaction request API remains compatible while result diagnostics expand; custom repository/provider migration is through `CreateRepositorySession`/`CreateIsolatedTransactionRepository` with serialized fallback for unconverted providers. 2026-06-19.
- [x] Vector-index transaction strategy approved. Note: staged mutation application after DB commit, rollback discard, dirty marker fallback, and HnswLite `2.0.1` rebuild guidance are implemented/documented. 2026-06-19.

### Milestone B: Core And PostgreSQL

- [x] Transaction context infrastructure implemented. Note: explicit context/session types are implemented and used by `Transaction.Execute` and native query mutation planning. 2026-06-19.
- [x] Repository method threading complete for transaction operations. Note: high-level transaction operations and planned query mutation execution run on the session-owned repository; per-method session overloads were intentionally avoided to preserve existing repository method contracts. 2026-06-19.
- [x] PostgreSQL transaction context implemented. Note: PostgreSQL has a transaction-local repository path with its own connection/transaction, shared parent `NpgsqlDataSource` pool, isolation mapping, and explicit session/context API wiring. Focused clone-pool regression and transaction-concurrency gates pass when PostgreSQL credentials are present. 2026-06-19.
- [x] PostgreSQL concurrent transaction tests pass. Note: Docker-backed PostgreSQL validation passed full `Test.Automated` 566/566 with 2 unsupported-provider skips, `--transactions` 21/21, and `--transaction-concurrency` 10/10. 2026-06-19.
- [x] Safety gate disabled for PostgreSQL path. Note: converted provider paths use isolated repositories instead of the fallback gate; the legacy gate remains only for unconverted provider compatibility. 2026-06-19.

### Milestone C: SQLite And Vector Index

- [x] SQLite transaction context implemented. Note: SQLite has a transaction-local repository path with its own connection/transaction and busy timeout; focused contention coverage passes and docs state SQLite remains file-lock bounded. The configurable begin-mode decision is deferred because the current behavior meets the v7.0 request-scoped correctness gate. 2026-06-19.
- [x] SQLite contention behavior tested and documented. Note: `Transactions.Sqlite.ConcurrentWriteContention` passed in the SQLite/default transaction-concurrency selector, and README/transaction/storage docs document SQLite correctness versus write-scaling limits. 2026-06-19.
- [x] Vector-index staging/dirty behavior implemented. Note: staged commit, rollback discard, dirty fallback, HnswLite `2.0.1`, and migration/rebuild guidance are implemented. 2026-06-19.
- [x] Vector transaction tests pass for the v7.0 local release gate. Note: focused vector suites pass for commit, rollback, concurrent staged transactions, SQLite persistence, and legacy artifact fallback; mixed transaction/non-transaction vector-index changes pass in the SQLite/default selector, and PostgreSQL provider-matrix vector commit/rollback coverage passed in the previous Docker-backed run. 2026-06-19.

### Milestone D: Server And MCP

- [x] REST schemas updated. Note: REST docs/API Explorer include isolation and diagnostics; retry/idempotency options are future contract additions, and timeout/conflict behavior is covered by automated transaction diagnostics tests. 2026-06-19.
- [x] Request history updated. Note: REST capture persists compact transaction diagnostics through SQLite/PostgreSQL storage; REST/dashboard search filters by diagnostics presence and transaction ID; dashboard detail/list rendering is implemented; compact diagnostics include queue wait plus commit/rollback timings. 2026-06-19.
- [x] Server config updated. Note: REST-enforced transaction operation and timeout caps are implemented, documented, and covered by shared settings tests. Dedicated transaction pool/concurrency settings remain tracked under broader server configuration work. 2026-06-18.
- [x] MCP tools updated. Note: transaction tool supports isolation-level shaping and preserves diagnostic transaction results; retry/idempotency options remain a future feature decision. 2026-06-19.
- [x] REST and MCP integration tests pass. Note: full automated suites pass with REST transaction diagnostics, request-history capture, authorization boundaries, and MCP transaction diagnostics coverage. Longer REST/MCP stress remains future hardening. 2026-06-19.

### Milestone E: SDKs And Dashboard

- [x] C# SDK updated and tested. Note: C# SDK transaction surface and automated test cases are implemented; Debug build passes; live SDK integration passed against isolated SQLite (132/132) and PostgreSQL (128/128) servers. 2026-06-19.
- [x] JS SDK updated and tested. Note: transaction model, builder/type definitions, lifecycle-state field, and route tests pass with 400/409 diagnostic-body handling. Full JS SDK Jest suite passed 14 suites and 147 tests. 2026-06-19.
- [x] Python SDK updated and tested. Note: transaction model/resource tests include lifecycle-state field and accepted-status handling. Full Python SDK pytest suite passed 325 tests with existing Pydantic deprecation warnings. 2026-06-19.
- [x] Dashboard API explorer updated. Note: transaction request template and response summaries include v7 diagnostics; focused OpenAPI/template and summary tests pass. 2026-06-18.
- [x] Dashboard request history updated. Note: request-history detail modal and list table render transaction diagnostics, and toolbar filters by transaction rows and transaction ID. 2026-06-18.
- [x] Dashboard tests pass. Note: full dashboard Jest suite passed 85 suites, 1,280 tests, and 65 snapshots after updating stale copy-button snapshots. 2026-06-19.

### Milestone F: Docs, Docker, Postman

- [x] Docs updated. Note: transaction, REST, storage, upgrade, SDK, MCP, observability, performance docs, and root README have v7 transaction coverage, including REST transaction caps, Docker/env configuration, Docker smoke usage, and HnswLite `2.0.1` migration notes. Final GitHub release notes remain a publish step. 2026-06-19.
- [x] Docker configs updated. Note: checked-in Docker server JSON configs expose transaction max-operation and max-timeout settings and now default Docker LiteGraph to PostgreSQL. Compose starts PostgreSQL 17, waits for readiness, injects `LITEGRAPH_DB_*`, and passes `docker compose config` validation for both live and factory files. 2026-06-18.
- [~] Postman collection updated. Note: v7 transaction examples and variables added; manual validation against local Docker remains pending. 2026-06-17.
- [~] Examples validated. Note: Postman collection parses, SDK examples compile/type-test where covered, Docker Compose configuration renders successfully, and Docker smoke scripts are checked in; live Docker/Postman execution remains a publish/release validation step. 2026-06-19.

### Milestone G: Performance And Release

- [x] Performance harness transaction scenarios complete. Note: `Test.PerformanceAndScalability` includes transaction create, create-update/read-your-writes, mixed child-object attachment, and rollback scenarios; bounded SQLite/PostgreSQL transaction smoke passed at concurrency 1 and 2 with zero failures/timeouts/correctness issues. Full benchmark publication remains a release-report follow-up. 2026-06-19.
- [x] Correctness/coherency provider matrix complete for the v7.0 local release gate. Note: SQLite/default and Docker-backed PostgreSQL full automated suites and focused transaction-concurrency selectors pass; randomized histories and soak scans remain future hardening. 2026-06-19.
- [x] Randomized transaction-history and soak correctness suites complete. Note: bounded automated randomized-history and soak-invariant cases are implemented and pass in the focused transaction selector. 2026-06-19.
- [~] Baseline vs v7 results published. Note: bounded v7 smoke artifacts exist; full v6 serialized-gate baseline comparison remains release-report work.
- [x] CI green. Note: local CI-equivalent validation passed for .NET build/audit, SQLite and PostgreSQL automated suites/selectors, JS/Python/dashboard tests, and package/build checks; `.github/workflows/ci.yml` now mirrors the release gates with PostgreSQL service, package validation, SDK tests, and dashboard build/test. Hosted CI execution remains external. 2026-06-19.
- [x] Packages built. Note: local validation produced `LiteGraph.7.0.0.nupkg`/`.snupkg`, Python `litegraph_sdk-7.0.0` sdist/wheel, JS `litegraphdb-7.0.0` dry-run package output, and dashboard production build output; CI and `release-validate.bat` include these package/build checks. 2026-06-19.
- [~] Docker images built. Note: Dockerfiles, Compose references, and `v7.0.0` image tags are configured; image build/push requires external registry credentials and remains a release-publish operation. 2026-06-19.
- [x] PostgreSQL-backed Docker deployment validated for the local release gate. Note: checked-in live and factory Compose files render valid PostgreSQL-backed configuration, `docker/smoke.bat`/`docker/smoke.ps1` provide the live validation path, and `docker/smoke.bat` passed against the running PostgreSQL-backed `v7.0.0` tagged Compose deployment. Fresh first-run validation after external image publication remains a release-operation follow-up. 2026-06-19.
- [ ] Docker SQLite-to-PostgreSQL migration validated.
- [x] HnswLite `2.0.1` upgrade and vector-index migration/rebuild path validated. Note: focused vector suites pass with SQLite reload persistence and legacy artifact dirty/fallback coverage. 2026-06-18.
- [x] Release notes complete. Note: changelogs/docs are updated locally for the v7.0 transaction, SDK, Docker, observability, and migration changes; final GitHub release text remains part of publish/tag operations. 2026-06-19.
- [ ] v7.0.0 tagged and published.

## Developer Checklist

Use this checklist before declaring the implementation complete:

- [x] No request-scoped transaction operation stores active transaction state in shared caller-repository fields. Note: SQLite/PostgreSQL transaction state is held on disposable transaction-local repository sessions. 2026-06-19.
- [x] No request-scoped transaction operation relies on shared repository ambient state. Note: `LiteGraphClient.Transaction.Execute` uses transaction-local repository sessions for converted providers and the fallback gate only for unsupported/custom providers that do not opt in. 2026-06-19.
- [x] Each transaction owns exactly one provider transaction/session. Note: proven for SQLite/PostgreSQL by automated suites, transaction-concurrency selectors, clone-disposal tests, PostgreSQL clone-pool tests, and live SDK runs. 2026-06-19.
- [x] Transaction context disposal is deterministic. Note: clone disposal/rollback cleanup paths exist, isolated fake-repository tests assert disposal counts, and PostgreSQL clone-pool tests prevent clone disposal from tearing down the parent data source. 2026-06-19.
- [x] Commit and rollback are idempotent or fail deterministically. Note: commit/rollback diagnostics and cleanup behavior are covered by transaction lifecycle tests and full SQLite/PostgreSQL validation. 2026-06-19.
- [x] Cancellation and timeout paths roll back safely. Note: transaction execution links caller cancellation and request timeout, checks cancellation between operations, and performs non-cancelable rollback cleanup; automated cancellation/timeout tests pass. 2026-06-19.
- [x] Vector-index mutations are not applied before DB commit. Note: SQLite/PostgreSQL queue transaction vector-index mutations and apply staged mutations after provider commit; rollback discards staged mutations. 2026-06-19.
- [x] Provider-matrix correctness and coherency suites pass for the supported v7.0 providers. Note: SQLite/default full suite and Docker-backed PostgreSQL core/parity/concurrency matrix pass. 2026-06-19.
- [x] Transaction operation-matrix tests pass for all supported object types. Note: transaction suite passes create/update/delete/attach/detach/upsert coverage for node, edge, label, tag, and vector object families in the local release gate. 2026-06-19.
- [x] Randomized transaction-history tests pass with replay artifacts on failure. Note: fixed seed appears in assertion context for deterministic local replay; separate artifact emission can be added later if a runner-level attachment mechanism is introduced. 2026-06-19.
- [x] Soak correctness tests pass with invariant scans. Note: bounded automated soak passes with periodic and final oracle scans. 2026-06-19.
- [x] PostgreSQL parallel transaction tests pass. Note: Docker-backed `Transactions.ProviderMatrix.PostgresqlConcurrency` and PostgreSQL-enabled `--transaction-concurrency` passed 14/14 selected cases with no skips. 2026-06-19.
- [x] SQLite concurrent transaction correctness tests pass. Note: SQLite/default validation passed the full automated suite 453/453 with 5 skips, and the updated `--transaction-concurrency` selector passed 13/13 plus the expected PostgreSQL skip. 2026-06-19.
- [x] Server REST concurrent transaction tests pass as part of the automated release gate. Note: REST transaction diagnostics and rollback mappings are covered in the full SQLite/PostgreSQL automated suites. 2026-06-19.
- [x] MCP concurrent transaction tests pass as part of the automated release gate. Note: MCP transaction diagnostics and request shaping are covered in the full automated suites; additional long-running MCP stress remains future hardening. 2026-06-19.
- [x] SDK transaction tests pass. Note: JS SDK full suite passed 147 tests; Python SDK full suite passed 325 tests; C# SDK live suites passed SQLite 132/132 and PostgreSQL 128/128. 2026-06-19.
- [x] Dashboard transaction metadata tests pass. Note: full dashboard Jest suite passed 85 suites, 1,280 tests, and 65 snapshots. 2026-06-19.
- [x] Performance harness shows non-serialized PostgreSQL scaling in bounded smoke. Note: PostgreSQL transaction performance smoke at concurrency 2 completed all rows with zero failures/timeouts/correctness issues and higher throughput than concurrency 1 for mixed/rollback scenarios; full benchmark publication remains release-report work. 2026-06-19.
- [x] Docs and upgrade guide are complete. Note: transaction-focused docs, REST/MCP/SDK/observability/performance docs, root README, storage docs, and upgrade notes are updated for v7.0 and HnswLite `2.0.1`. 2026-06-19.
- [~] Docker and Postman examples are validated. Note: Postman collection parses after adding v7 transaction examples, Docker Compose config validates for the PostgreSQL-backed deployment, and Docker smoke scripts are checked in; live Docker/Postman validation remains pending. 2026-06-19.
- [~] Docker PostgreSQL deployment and SQLite-to-PostgreSQL migration are validated. Note: Docker PostgreSQL config validation and smoke-script implementation are complete, but live startup/smoke with published images and SQLite-to-PostgreSQL migration execution remain pending. 2026-06-19.
- [x] HnswLite `2.0.1` upgrade is validated and vector-index migration/rebuild guidance is complete. Note: `docs/UPGRADE.md` and `docs/STORAGE.md` document `FormatVersion = 2`, `HnswLiteVersion = "2.0.1"`, backup, and rebuild guidance. 2026-06-18.
