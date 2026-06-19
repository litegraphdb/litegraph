# LiteGraph Next Release Improvement Plan

> **Status Key:** `[ ]` Not started | `[~]` In progress | `[x]` Complete | `[!]` Blocked | `[?]` Needs decision
>
> **Last updated:** 2026-04-17
>
> **Release target:** Next LiteGraph release
>
> **Audience:** Public roadmap plus maintainer execution plan

## Release Goal

This release is a strategic architecture release. The goal is to make LiteGraph more useful as a production graph database by adding:

- a native graph query capability with familiar graph-query syntax
- graph-scoped transactional mutations
- provider-neutral storage with SQLite as the default and PostgreSQL as the recommended production backend
- RBAC and scoped credentials at REST/MCP boundaries
- production observability with Prometheus and OpenTelemetry

All five areas are **P1** for this release. The release should be planned as one coordinated effort because these items overlap at the repository, request handling, authorization, testing, and SDK/MCP surfaces.

## Non-Negotiable Constraints

- SQLite remains the default zero-config backend.
- PostgreSQL is the recommended production backend.
- Storage backend implementations live inside the main LiteGraph project, following the provider folder pattern used in `C:\code\Verbex\Verbex\src\Verbex\Database`.
- Query execution should prefer repository-level planning/execution. `LiteGraphClient` methods may be used where they are the correct abstraction.
- Transactions are limited to one tenant and one graph.
- Transactions can mutate graph child objects only: nodes, edges, labels, tags, and vectors.
- Query mutations can mutate graph child objects only and must not update graph metadata.
- Vector search in the query language accepts supplied embeddings/vectors. LiteGraph will not generate embeddings in this release.
- RBAC is enforced at REST and MCP boundaries only. Core `LiteGraphClient` and repository APIs remain permission-agnostic.
- Existing users and credentials must retain effective full access after migration.
- Request history remains supported and should be used as a subordinate source for observability.
- Test coverage should be built into `Test.Shared` and exposed through `Test.Automated`, `Test.Xunit`, and `Test.Nunit`.
- SDK, MCP, and dashboard work is required where the feature has a relevant user-facing surface.

## Release Sequencing

The work should be delivered in this order, even though all items are P1:

1. Storage architecture and request lifecycle foundations.
2. Transactions and repository execution primitives.
3. Query language design decisions, then parser/planner/executor.
4. RBAC and scoped credential enforcement at REST/MCP boundaries.
5. Observability instrumentation, metrics, traces, and dashboard/docs.
6. SDK, MCP, dashboard, and full test matrix completion.

This ordering reduces rework. Query execution, transactions, RBAC, and observability all depend on the storage and request lifecycle work.

---

## 0. Release Architecture Gates

**Priority:** P1
**Status:** `[x]` Complete for release gating; implementation is now unblocked
**Impact:** Prevents incompatible implementations across query, storage, RBAC, SDKs, and MCP

### 0.1 Query Language Direction

Decision:

- Selected profile: Cypher/GQL-inspired LiteGraph-native query profile.
- Use Cypher-style graph pattern syntax for user familiarity.
- Align with ISO GQL where practical.
- Do not claim full openCypher, Neo4j Cypher, or ISO GQL conformance in this release.
- Gremlin, SPARQL, SQL/PGQ, and GraphQL are not native query-language targets for this release.
- Feature name in code and docs: native graph query or LiteGraph native graph query. Do not use "LiteQL" unless a separate naming decision revives it later.

Decision criteria:

- familiar to graph database users
- practical to implement in this release
- maps cleanly to LiteGraph nodes, edges, labels, tags, data, and vectors
- supports mutations without forcing graph metadata changes
- supports repository-level planning
- does not lock LiteGraph into Neo4j-specific behavior unless explicitly chosen

Deliverables:

- [x] Short design note comparing candidate query syntax options.
- [x] Final decision on syntax compatibility target.
- [x] Final naming decision for the feature.
  - [x] 20+ example queries covering reads, traversals, filters, vector search, and mutations.
- [x] Query feature marked unblocked only after this decision is recorded.

### 0.2 Query Parameter Model

Decision:

- allow inline literals for simple constants
- support separate parameters for user input, vectors, large JSON payloads, and reusable queries
- require errors for missing parameters
- preserve JSON/object parameter values instead of forcing string interpolation

Why separate parameters may be needed:

- avoids string escaping bugs in generated queries
- improves safety for mutation queries
- preserves value types for GUIDs, numbers, booleans, arrays, objects, and vectors
- avoids embedding large vector arrays directly in query text
- enables query plan caching later
- creates a cleaner SDK and MCP contract for user-provided values

Possible request shape:

```json
{
  "Query": "MATCH (n:Person) WHERE n.data.age > $age RETURN n",
  "Parameters": {
    "age": 30
  }
}
```

Deliverables:

- [x] Decide whether parameter binding is required for this release.
- [x] Decide whether inline literals are supported.
- [x] Define parameter value types.
- [x] Define error behavior for missing, unused, or type-invalid parameters.
- [x] Define SDK/MCP request models after the decision.

### Acceptance Criteria

- Query language implementation started only after 0.1 and 0.2 were resolved.
- The selected query syntax is documented with examples before full parser work begins.
- SDK, MCP, REST, and tests all use the same query request contract when each surface is added.

---

## 1. Provider-Neutral Storage Backends

**Priority:** P1
**Effort:** Very Large
**Status:** `[x]` Completed for SQLite and PostgreSQL
**Impact:** Removes SQLite-only architecture assumptions and enables production deployment on PostgreSQL

### Why This Matters

- SQLite is excellent as the zero-config default.
- Production deployments need server-backed storage, write concurrency, operational tooling, replication, and backup workflows.
- PostgreSQL is the first production backend.

### Implementation Steps

- [x] **1.1** Introduce provider-neutral database settings and type selection.
  - [x] Add `DatabaseTypeEnum`: `Sqlite`, `Postgresql`.
  - [x] Add provider-neutral settings modeled after Verbex `DatabaseSettings`.
  - [x] Preserve backward compatibility with `LiteGraph.GraphRepositoryFilename`.
  - [x] Support environment variables for database type, filename, host, port, database, username, password, schema, pool size, and command timeout.
  - [x] Redact passwords and connection strings from logs.

- [x] **1.2** Add a repository factory.
  - [x] File: `src/LiteGraph/GraphRepositories/GraphRepositoryFactory.cs`
  - [x] Create repositories from settings.
  - [x] Update `LiteGraph.Server` startup to use the factory instead of directly constructing `SqliteGraphRepository`.
  - [x] Keep `LiteGraphClient(GraphRepositoryBase repo, ...)` working.

- [x] **1.3** Refactor repository base primitives.
  - [x] Add async initialization/close patterns where needed without breaking current public behavior.
  - [x] Add explicit transaction primitives on `GraphRepositoryBase`: begin, commit, rollback, transaction-active state.
  - [x] Add provider-neutral query execution hooks needed by query planning and observability.
    - [x] Core repository operation telemetry event and `LiteGraph` meter/activity names are provider-neutral.
    - [x] SQLite execution primitives emit repository operation telemetry for read, write, transaction, and batch work.
    - [x] PostgreSQL execution primitives emit repository operation telemetry for read, write, transaction, and batch work.
  - [x] Remove SQLite-specific assumptions from shared interfaces and comments where practical.

- [x] **1.4** Normalize provider-specific query generation.
  - [x] Keep provider-specific SQL in provider folders.
  - [x] Preserve `Interfaces/`, `Implementations/`, `Queries/`, `Sanitizer.cs`, and converter patterns.
  - [x] Move JSON expression translation behind provider-specific components.
  - [x] Do not leak SQLite JSON path assumptions into user-facing docs for provider-neutral features.

- [x] **1.5** Implement PostgreSQL backend.
  - [x] Folder: `src/LiteGraph/GraphRepositories/Postgresql/`
  - [x] Replace the provider placeholder with an executable repository wired through `GraphRepositoryFactory`.
  - [x] Use `NpgsqlDataSource`.
  - [x] Implement schema creation and indexes in the configured PostgreSQL schema.
  - [x] Implement repository interfaces for tenants, users, credentials, graphs, nodes, edges, labels, tags, vectors, vector indexes, request history, batch, and admin operations.
  - [x] Implement transaction primitives.
  - [x] Implement PostgreSQL JSON data filtering.
    - [x] Translate provider JSON expressions to PostgreSQL `jsonb` extraction at execution.
    - [x] Live PostgreSQL JSON filter coverage with a configured PostgreSQL test database.
    - [x] Numeric and boolean JSON comparisons are translated to PostgreSQL casts.
  - [x] Verify concurrent writer behavior.
    - [x] Use server-side connection pooling and database transactions.
    - [x] Live concurrent writer coverage with a configured PostgreSQL test database.

- [x] **1.7** Migration and backup tools.
  - [x] Export/import SQLite to PostgreSQL through the provider-neutral `StorageMigrationManager`.
  - [x] Verification tool compares entity counts and sampled records.
  - [x] Document large database migration strategy.
    - [x] Added migration usage, verification behavior, and PostgreSQL target guidance to `STORAGE.md`.

- [x] **1.8** Tests.
  - [x] Add backend-neutral storage foundation suites in `Test.Shared`.
  - [x] Run the core provider functional tests against SQLite and PostgreSQL.
    - [x] PostgreSQL SQL translation unit coverage verifies schema qualification, GUID primary keys, `BYTEA`, JSON field filters, and blob literal translation.
    - [x] PostgreSQL live provider smoke covers initialization, tenant/graph/node/edge/label/tag/vector CRUD, JSON filters, concurrent writes, and graph transaction commit/rollback when `LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING` is configured.
    - [x] Storage migration round-trip coverage verifies tenants, users, credentials, graphs, nodes, edges, labels, tags, vectors, custom roles, user role assignments, credential scopes, counts, sampled records, and async disposal cleanup.
  - [x] Add provider implementation test for PostgreSQL.
  - [x] Expose via `Test.Automated`, `Test.Xunit`, and `Test.Nunit`.

### Verification

- [x] `dotnet build src\LiteGraph.sln -f net8.0`
- [x] `dotnet run --project src\Test.Automated\Test.Automated.csproj --framework net8.0` with `LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING` against a disposable PostgreSQL 16 container: 429 total, 427 passed, 0 failed, 2 skipped.

### Acceptance Criteria

- SQLite remains default and backward compatible.
- PostgreSQL passes the same core repository test suite as SQLite.
- Server startup selects storage backend by configuration.
- PostgreSQL supports concurrent writes from multiple server instances.
- Existing LiteGraph data can be migrated from SQLite to PostgreSQL with verification.

---

## 2. Graph-Scoped Transactions

**Priority:** P1
**Effort:** Large
**Status:** `[x]` Complete; graph-scoped transaction model, execution, SDK helpers, MCP, dashboard API Explorer support, and transaction coverage are implemented
**Impact:** Provides all-or-nothing graph child mutations

### Why This Matters

- Creating a meaningful graph change often requires multiple node, edge, label, tag, and vector writes.
- Today, failure in the middle of a multi-step workflow can leave partial graph state.
- Transactions are required for reliable imports, migrations, agent operations, and query-language mutations.

### Scope

Transactions are limited to:

- one tenant
- one graph
- graph child objects only: nodes, edges, labels, tags, vectors

Transactions do not include:

- tenant creation/update/delete
- graph creation/update/delete
- user or credential management
- backup/restore
- cross-graph operations
- cross-tenant operations

### Implementation Steps

- [x] **2.1** Define generic transaction operation model.
  - [x] `TransactionRequest`
  - [x] `TransactionOperation`
  - [x] `TransactionResult`
  - [x] `TransactionOperationResult`
  - [x] Operation types for create, update, and delete.
  - [x] Attach, detach, and upsert operation types where appropriate.
    - [x] `Attach` and `Detach` support labels, tags, and vectors as node/edge subordinate records.
    - [x] `Upsert` supports nodes, edges, labels, tags, and vectors by GUID within the transaction graph.
  - [x] Payloads are typed internally after JSON conversion; C#, Python, JavaScript, REST, MCP, and dashboard request templates share the implemented operation shapes.
  - [x] Maximum operations per transaction: configurable, default 1000.
  - [x] Transaction timeout: configurable, default 60 seconds.

- [x] **2.2** Add REST endpoint.
  - [x] `POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/transaction`
  - [x] Request body is a generic operation list.
  - [x] Response includes per-operation results when successful.
  - [x] Failure response identifies the failed operation index, operation type, and reason.

- [x] **2.3** Implement repository transaction execution.
  - [x] Use provider transaction primitives.
  - [x] Roll back all DB writes on failure in SQLite.
  - [x] Respect cancellation tokens and timeout in the C# transaction executor.
  - [x] Ensure validation happens before writes when practical.
    - [x] Structural operation validation now runs before opening a graph transaction.
  - [x] Ensure repository operations can participate in an active transaction in SQLite.
  - [x] Public transaction and query mutation entry points reject accidental nested/concurrent use of an active repository transaction.

- [x] **2.4** Handle vector index consistency.
  - [x] DB atomicity is required.
  - [x] Vector index rollback is not required.
  - [x] If vector index update fails after DB commit, mark the affected graph index dirty or schedule repair.
    - [x] Graphs now persist vector index dirty state, dirty timestamp, and dirty reason.
    - [x] Dirty indexes are surfaced through vector index statistics and are bypassed by indexed search so brute-force search remains correct until repair.
    - [x] Graph transaction rollback or commit failure after vector index mutation marks the graph index dirty.
  - [x] Provide rebuild/repair guidance and tests.
    - [x] `TRANSACTIONS.md` documents dirty index behavior and repair through rebuild.
    - [x] Touchstone coverage verifies post-commit index failure fallback/repair and transaction rollback dirty marking.

- [x] **2.5** Add SDK support.
  - [x] C#: transaction request builder.
    - [x] Builder includes typed attach, detach, and upsert helpers.
  - [x] Python: transaction helper/context-style API where appropriate.
  - [x] JavaScript: async transaction builder.

- [x] **2.6** Add MCP support.
  - [x] `graph/transaction` graph-scoped tool for MCP HTTP, TCP, and WebSocket surfaces.
  - [x] Enforce RBAC at tool boundary by routing through the REST transaction endpoint; per-tool MCP authorization coverage is complete under Section 4.7.

- [x] **2.7** Add dashboard support where appropriate.
  - [x] Transaction execution is exposed through API Explorer rather than as a primary dashboard workflow.
  - [x] Add API Explorer examples and request templates.
  - [x] Surface transaction failures clearly when dashboard invokes transaction-capable APIs.

- [x] **2.8** Tests.
  - [x] Rollback and commit coverage for node create in SQLite.
  - [x] Rollback on failed node create.
  - [x] Rollback after mixed operations include an edge create.
  - [x] Rollback/commit coverage for mixed node/edge operations.
  - [x] Rollback on mixed node/edge/label/tag/vector operations.
  - [x] Pre-write structural validation coverage.
  - [x] Attach, detach, and upsert coverage for node, edge, label, tag, and vector transaction paths.
  - [x] Operation limit enforcement.
  - [x] Timeout/cancellation behavior.
    - [x] Pre-cancelled transaction leaves no graph child writes.
    - [x] Timeout during a long-running transaction rolls back and clears active repository state.
  - [x] Concurrent transaction behavior for SQLite and PostgreSQL.
    - [x] SQLite active-transaction guard prevents transaction API and query mutations from joining an already-active repository transaction.
    - [x] PostgreSQL live provider coverage verifies concurrent graph transactions through separate repository instances.
  - [x] Vector index dirty/repair behavior when index update fails.

### Acceptance Criteria

- Multi-operation graph child mutations succeed or fail atomically at the database layer.
- Transactions cannot cross tenant or graph boundaries.
- Failed transactions leave no partial DB mutations.
- Failure response identifies the failing operation.
- Vector index consistency is recoverable through dirty marking or rebuild.
- SDK and MCP support is available where appropriate.

---

## 3. Native Graph Query Language

**Priority:** P1
**Effort:** Very Large
**Status:** `[x]` Complete; parser/planner/executor-backed reads, fixed and bounded variable-length traversals, shortest and optional matches, graph-child CREATE/UPDATE/DELETE mutations, supplied-vector search, REST, C#/Python/JavaScript SDK helpers, MCP, API Explorer support, DSL docs, console execution, and shared test coverage are implemented
**Impact:** Enables one-call graph traversal, filtering, vector search, and mutation

### Why This Matters

- Traversals often require multiple REST calls today.
- A native query endpoint lets users express graph intent in a single request.
- AI agents benefit from structured query and mutation capabilities.
- The query language should use syntax familiar to the larger graph database user base while preserving LiteGraph-native behavior.

### Scope

The query language must support:

- graph-scoped execution only
- reads and traversals
- filtering by labels, tags, name, data fields, and vector similarity
- returning full LiteGraph objects
- mutations of nodes, edges, labels, tags, and vectors
- repository-level planning and execution where possible
- cancellation and timeout

The query language must not support:

- cross-tenant queries
- cross-graph queries in this release
- graph metadata mutation
- tenant/user/credential/admin mutation
- embedding generation

### Implementation Steps

- [x] **3.1** Complete language design after Section 0 decisions.
  - [x] Select Cypher/GQL-inspired LiteGraph-native profile.
  - [x] Define supported read clauses for the initial profile.
  - [x] Define supported mutation clauses.
    - [x] Graph-child `CREATE`, `SET`, and `DELETE` for nodes, edges, labels, tags, and vectors.
  - [x] Define supported operators.
    - [x] Equality for GUID, name, and `data.<field>` predicates in implemented query paths.
    - [x] Numeric comparison operators for `data.<field>` predicates: `>`, `>=`, `<`, `<=`.
    - [x] String predicate operators for supported `name` and `data.<field>` filters: `CONTAINS`, `STARTS WITH`, `ENDS WITH`.
    - [x] AND-combined predicate lists for implemented node, edge, and fixed-path match paths.
    - [x] Basic `ORDER BY` support for returned scalar values and supported LiteGraph object fields.
    - [x] OR/NOT, list, tag, and aggregation operators.
      - [x] OR/NOT predicate expressions with AND precedence, parentheses, planner scan warnings, and executor fallback evaluation.
      - [x] `IN` list predicates for literal lists, parameter lists, GUIDs, names, and `data.<field>` values.
      - [x] Tag-aware predicates using `tags.<key>` for node and edge tag records.
      - [x] Aggregate `RETURN` operators for read-only node, edge, and fixed-path `MATCH`: `COUNT`, `SUM`, `AVG`, `MIN`, and `MAX`.
      - [x] Bounded variable-length path ranges with `*min..max`.
      - [x] `MATCH SHORTEST` over bounded path candidates.
      - [x] Top-level read-only `OPTIONAL MATCH` with null-row behavior.
  - [x] Define graph pattern syntax for node and edge pattern reads.
  - [x] Define vector search syntax using supplied vectors/parameters.
  - [x] Define parameter behavior if parameter binding is selected.
  - [x] Define unsupported syntax and error messages for the initial profile.
  - [x] Publish examples before implementation.

- [x] **3.2** Define query request/response models.
  - [x] `GraphQueryRequest`
  - [x] `GraphQueryResponse`
  - [x] `GraphQueryResult`
  - [x] Include execution time, row count/object count, warnings, and optional profile data.
  - [x] Request includes opt-in `IncludeProfile` for query phase timings.
  - [x] Response includes optional `ExecutionProfile` with parse, plan, execute, and total timings.
  - [x] Return full LiteGraph objects for implemented node, edge, label, tag, vector, and vector-search rows.

- [x] **3.3** Implement lexer/tokenizer.
  - [x] File: `src/LiteGraph/Query/Lexer.cs`
  - [x] Token types for identifiers/keywords, operators, literals, parameters, and punctuation in the initial profile.
  - [x] Line/column error reporting.
  - [x] Initial unit tests for supported token types and invalid tokens.

- [x] **3.4** Implement parser and AST.
  - [x] File: `src/LiteGraph/Query/Parser.cs`
  - [x] Folder: `src/LiteGraph/Query/Ast/`
  - [x] AST nodes for query, pattern, node, edge, AND-combined filters, return, order, limit, vector search, and create/update/delete mutation operations.
  - [x] AST predicate-expression tree for `AND`, `OR`, `NOT`, parentheses, and `IN` list operands.
  - [x] AST aggregate return items for `COUNT`, `SUM`, `AVG`, `MIN`, and `MAX`, with aliases.
  - [x] Helpful syntax errors with line/column positions.
  - [x] Unit tests for valid and invalid queries, including 20+ documented DSL examples.

- [x] **3.5** Implement planner.
  - [x] File: `src/LiteGraph/Query/Planner.cs`
  - [x] Convert AST into provider-neutral execution plan.
  - [x] Push filters to repository operations where possible.
    - [x] Planner selects repository seed hints for node GUID/name, edge GUID/name/source/target, and fixed-path segment edge/source/target equality predicates.
    - [x] Planner avoids unsafe seed pushdown for OR/NOT predicate expressions and emits scan warnings.
  - [x] Use vector indexes where available.
    - [x] Vector search plans use a `VectorIndex` seed and execution routes through `client.Vector.Search(...)`, which uses configured graph vector indexes when eligible.
  - [x] Estimate plan cost enough to reject obviously dangerous queries.
    - [x] Planner emits aggregate scan warnings and assigns aggregate scan cost.
    - [x] Planner emits variable-length, shortest-path, optional-match, and unbounded-order warnings and assigns higher traversal expansion cost.
  - [x] Detect unsupported cross-graph or graph-metadata operations.
    - [x] Parser/executor expose no cross-tenant, cross-graph, graph metadata, tenant, user, credential, or admin mutation syntax in this release profile.

- [x] **3.6** Implement executor.
  - [x] File: `src/LiteGraph/Query/Executor.cs`
  - [x] Route `QueryMethods.Execute` through planner and executor components.
  - [x] Execute implemented plans against repository-level APIs, except vector search.
  - [x] Evaluate OR/NOT predicate expressions, `IN` list predicates, and `tags.<key>` predicates for node, edge, and fixed-path match queries.
  - [x] Execute aggregate scalar rows for node, edge, and fixed-path match queries over object fields, data fields, edge cost, and tag values.
  - [x] Execute bounded variable-length path queries and shortest bounded path queries.
  - [x] Execute top-level read-only `OPTIONAL MATCH` with null-row behavior.
  - [x] Use `LiteGraphClient` only where it is the correct abstraction.
    - [x] Query vector search routes through `client.Vector.Search(...)` so graph vector-index configuration remains centralized.
  - [x] Stream internal results where practical.
    - [x] Repository-backed node/edge/vector reads use async enumeration and early limit checks; bounded path expansion materializes only bounded candidate state where traversal semantics require it.
  - [x] Respect cancellation tokens in implemented query paths.
  - [x] Enforce query timeout: configurable, default 30 seconds.
  - [x] Route implemented mutation queries through transaction infrastructure.
  - [x] Move execution helper logic out of the public `QueryMethods` facade into `QueryExecutionEngine`.

- [x] **3.7** Add REST endpoint.
  - [x] `POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/query`
  - [x] No tenant-level cross-graph query endpoint in this release.
  - [x] Enforce graph allow-list and read/write credential scopes before execution.

- [x] **3.8** Add SDK support.
  - [x] C#: `client.Query.Execute(...)`
  - [x] Python: query resource/helper.
  - [x] JavaScript: query helper.
  - [x] Typed result deserialization for full LiteGraph object lists where the SDK has model classes; row payloads preserve full returned objects.

- [x] **3.9** Add MCP support.
  - [x] `graph/query` tool for MCP HTTP, TCP, and WebSocket surfaces.
  - [x] Query request model matches REST/SDK model by accepting a `GraphQueryRequest` payload and forwarding it unchanged to REST.
  - [x] Enforce RBAC at MCP boundary by routing query execution through the REST query endpoint and its credential-scope checks. Full per-tool MCP authorization for all existing tools remains under Section 4.7.

- [x] **3.10** Add dashboard support where appropriate.
  - [x] API Explorer examples.
  - [x] Optional query console if scope allows.
    - [x] Dashboard scope uses API Explorer for query execution; the dedicated terminal query console is `LiteGraphConsole`.
  - [x] Display query errors with line/column information.

- [x] **3.11** Add terminal query console.
  - [x] Project: `src/LiteGraphConsole/LiteGraphConsole.csproj`.
  - [x] Add `LiteGraphConsole` to `src/LiteGraph.sln`.
  - [x] Package as a .NET global tool with command name `lg`.
  - [x] Add install/reinstall/remove scripts modeled after the Armada global-tool scripts.
  - [x] Support local SQLite database files through `--database` / `--file`.
  - [x] Support remote LiteGraph endpoints through `--endpoint`.
  - [x] Accept `--tenant`, `--graph`, `--token`, `--max-results`, `--timeout`, and script/file execution arguments where appropriate.
  - [x] Provide an interactive shell similar to `sqlite3`: prompt, multiline query input, dot commands, readable JSON output, and non-interactive execution.
  - [x] Keep graph execution scoped to one tenant and one graph per session.

- [x] **3.12** Tests and benchmarks.
  - [x] Initial lexer tests.
  - [x] Parser tests.
  - [x] Documented DSL example parser coverage.
  - [x] Initial parameter error and query cancellation tests.
  - [x] Initial planner tests for seed selection, mutation classification, vector search, warnings, and cost.
  - [x] Initial node and edge read query integration tests.
  - [x] Initial fixed directed multi-hop path query integration tests.
  - [x] Initial node and edge `data.<field>` equality and numeric comparison filter tests.
  - [x] Node, edge, and fixed-path OR/NOT/parenthesized predicate, `IN` list predicate, and tag-aware predicate integration tests.
  - [x] Aggregate parser, planner, and node/edge/fixed-path execution coverage for `COUNT`, `SUM`, `AVG`, `MIN`, and `MAX`.
  - [x] Initial mutation query transaction tests.
  - [x] Initial node/edge update and delete query integration tests.
  - [x] Initial label/tag/vector update and delete query integration tests.
  - [x] Vector query tests with supplied embeddings for node, edge, graph-scoped vector search, and vector-indexed node search.
  - [x] Bounded variable-length path, shortest path, optional match, parser, planner, and executor tests.
  - [x] Python SDK query helper and model tests.
  - [x] JavaScript SDK query helper and model tests.
  - [x] Dashboard API Explorer query template and line/column error-summary tests.
  - [x] RBAC tests for query endpoint/tool.
  - [x] SQLite and PostgreSQL backend tests.
    - [x] SQLite native query coverage runs in the shared query suites.
    - [x] PostgreSQL native query smoke is included in the environment-gated PostgreSQL provider suite.
  - [x] Benchmarks against equivalent multi-call sequences.
    - [x] Benchmark scenarios are documented in `DSL.md` for one-hop, fixed two-hop, bounded variable-length traversal, data-filtered lookup, and supplied-vector search comparisons.

### Acceptance Criteria

- A user can execute a single graph-scoped query that matches patterns, filters data, performs vector similarity, and returns full LiteGraph objects.
- A user can execute graph child object mutations through the query endpoint.
- Mutation queries are database-atomic through transaction infrastructure.
- Syntax errors include line and column.
- Query timeout prevents runaway queries.
- Query execution uses repository-level planning where practical.
- Query language decision and parameter model are documented before implementation.

---

## 4. RBAC and Scoped Credentials

**Priority:** P1
**Effort:** Large
**Status:** `[x]` Complete; credential scopes, graph allow-lists, stored role/assignment policies, effective-permission caching/invalidation, authorization audit, REST role/scope management endpoints, MCP role/scope tools, MCP boundary enforcement for existing tools, PostgreSQL schema support, dashboard management UI, and dashboard tests are implemented
**Impact:** Provides enterprise-grade access control at REST and MCP boundaries

### Why This Matters

- Current authorization primarily enforces tenant isolation.
- Enterprises need read-only users, graph-level restrictions, scoped service credentials, and auditability.
- RBAC must be added without breaking existing deployments.

### Scope

RBAC applies to:

- REST requests
- MCP tools
- dashboard workflows that call REST APIs

RBAC does not apply inside:

- `LiteGraphClient`
- repository interfaces
- direct embedded use of LiteGraph

### Implementation Steps

- [x] **4.1** Define permission model.
  - [x] Built-in roles: `TenantAdmin`, `GraphAdmin`, `Editor`, `Viewer`, `Custom`.
  - [x] Permissions: `Read`, `Write`, `Delete`, `Admin`.
  - [x] Resource scopes: tenant, graph.
  - [x] Resource types: graph, node, edge, label, tag, vector, query, transaction, admin where applicable.
  - [x] Define inheritance rules.
  - [x] Define default migration behavior.

- [x] **4.2** Add role and scope storage.
  - [x] Stored role records through `authorizationroles`.
  - [x] Stored user-role assignments through `userroleassignments`.
  - [x] Stored credential-scope assignments through `credentialscopeassignments`.
  - [x] Repository and embedded client access through `AuthorizationRoles`.
  - [x] SQLite initialization seeds and refreshes global built-in role records.
  - [x] `authorizationaudit` table and repository/client access for denied authorization records.
  - [x] Provider-specific schema for SQLite and PostgreSQL.
    - [x] SQLite role, user-role assignment, and credential-scope assignment schema and indices.
    - [x] SQLite authorization audit schema and indices.
    - [x] PostgreSQL role, user-role assignment, and credential-scope assignment schema.
    - [x] PostgreSQL authorization audit schema.
  - [x] SQLite credentials persist `Scopes` and graph GUID allow-lists on the existing credential record.

- [x] **4.3** Preserve existing access on migration.
  - [x] Existing users receive effective full access in their tenant by migration policy definition.
  - [x] Existing credentials receive effective full access in their tenant because null/empty scopes and graph allow-lists preserve full access.
  - [x] Admin bearer token remains admin by migration policy definition.
  - [x] SQLite credential scope column migration is idempotent.
  - [x] SQLite built-in role seeding is idempotent and preserves existing built-in role GUIDs.
  - [x] Legacy SQLite credential migration test verifies scope/graph allow-list columns, role/scope/audit tables, built-in role seed idempotency, and unrestricted compatibility access.

- [x] **4.4** Implement authorization service.
  - [x] Centralized policy evaluation for the existing REST tenant, credential scope, and graph allow-list checks.
  - [x] Effective user-role and credential-scope assignment evaluation where stored assignments exist.
  - [x] Built-in role-name fallback for stored assignments using `AuthorizationPolicyDefinitions`.
  - [x] Graph query route re-checks effective access after parser-backed read/write classification.
  - [x] Resolve graph context for label/tag/vector GUID routes before policy evaluation so graph allow-lists apply to graph child objects.
  - [x] Extend centralized policy evaluation to all existing MCP tools.
    - [x] REST-proxy MCP enforcement is in place for graph query, graph transaction, batch existence, graph child-object tools, node list/bulk/enumeration/traversal tools, edge list/bulk/enumeration/search/existence/node-edge traversal tools, label/tag/vector list/bulk/enumeration/existence/search and graph/node/edge child-object tools, `admin/flush`, and tenant/user/credential admin-sensitive plus read/list/enumeration tools.
    - [x] Complete role/scope MCP tool review and expose only authenticated REST-proxy role/scope tools.
  - [x] Cache effective permissions.
  - [x] Invalidate cache on role/scope changes.
  - [x] Audit denied REST requests with request ID, correlation ID, trace ID, tenant, graph, user, credential, request type, reason, required scope, and status code where available.
  - [x] Return authorization errors with machine-readable context for reason, required scope, request type, tenant, and graph where available.

- [x] **4.5** Implement scoped credentials.
  - [x] Restrict credentials by operation and graph at REST.
  - [x] Support read-only service credentials at REST.
  - [x] Support graph-specific credentials at REST.
  - [x] Query endpoint classifies parser-backed mutation queries, including `MATCH ... SET` and `MATCH ... DELETE`, as write operations.
  - [x] Log denied credential usage for graph allow-list and missing-scope denials.

- [x] **4.6** Add role management REST endpoints.
  - [x] CRUD roles.
  - [x] Assign/revoke user roles.
  - [x] Assign/revoke credential scopes.
  - [x] List effective permissions.
  - [x] Require admin permission for role management.

- [x] **4.7** Add MCP enforcement and tools where appropriate.
  - [x] Enforce permissions for all existing MCP tools.
    - [x] Route `graph/query` and `graph/transaction` through authenticated REST endpoints for query/transaction RBAC.
    - [x] Route `batch/existence` through the authenticated REST graph existence endpoint so scoped credentials and graph allow-lists are honored.
    - [x] Route representative `graph/get`, `graph/create`, `graph/update`, `graph/delete`, `node/get`, `node/create`, `node/update`, `node/delete`, `edge/get`, `edge/create`, `edge/update`, `edge/delete`, `label/get`, `label/create`, `label/update`, `label/delete`, `tag/get`, `tag/create`, `tag/update`, `tag/delete`, `vector/get`, `vector/create`, `vector/update`, `vector/delete`, and `admin/flush` MCP tools through authenticated REST calls so scoped credentials and graph allow-lists are honored.
    - [x] Route `node/all`, `node/readallintenant`, `node/readallingraph`, `node/readmostconnected`, `node/readleastconnected`, `node/getmany`, `node/exists`, `node/search`, `node/readfirst`, `node/enumerate`, `node/parents`, `node/children`, `node/neighbors`, `node/traverse`, `node/createmany`, `node/deletemany`, `node/deleteall`, and `node/deleteallintenant` through authenticated REST calls so scoped credentials and graph allow-lists are honored.
    - [x] Route `edge/all`, `edge/readallintenant`, `edge/readallingraph`, `edge/getmany`, `edge/exists`, `edge/search`, `edge/readfirst`, `edge/enumerate`, `edge/nodeedges`, `edge/fromnode`, `edge/tonode`, `edge/betweennodes`, `edge/createmany`, `edge/deletemany`, `edge/deletenodeedges`, `edge/deleteallingraph`, `edge/deleteallintenant`, and `edge/deletenodeedgesmany` through authenticated REST calls so scoped credentials and graph allow-lists are honored.
    - [x] Route `label/all`, `label/readallintenant`, `label/readallingraph`, `label/readmanygraph`, `label/readmanynode`, `label/readmanyedge`, `label/getmany`, `label/exists`, `label/enumerate`, `label/createmany`, `label/deletemany`, `label/deleteallintenant`, `label/deleteallingraph`, `label/deletegraphlabels`, `label/deletenodelabels`, and `label/deleteedgelabels` through authenticated REST calls so scoped credentials and graph allow-lists are honored.
    - [x] Route `tag/readmany`, `tag/readallintenant`, `tag/readallingraph`, `tag/readmanygraph`, `tag/readmanynode`, `tag/readmanyedge`, `tag/getmany`, `tag/exists`, `tag/enumerate`, `tag/createmany`, `tag/deletemany`, `tag/deleteallintenant`, `tag/deleteallingraph`, `tag/deletegraphlabels`, `tag/deletenodelabels`, and `tag/deleteedgetags` through authenticated REST calls so scoped credentials and graph allow-lists are honored.
    - [x] Route `vector/all`, `vector/readallintenant`, `vector/readallingraph`, `vector/readmanygraph`, `vector/readmanynode`, `vector/readmanyedge`, `vector/getmany`, `vector/exists`, `vector/enumerate`, `vector/search`, `vector/createmany`, `vector/deletemany`, `vector/deleteallintenant`, `vector/deleteallingraph`, `vector/deletegraphvectors`, `vector/deletenodevectors`, and `vector/deleteedgevectors` through authenticated REST calls so scoped credentials and graph allow-lists are honored.
    - [x] Route `tenant/create`, `tenant/get`, `tenant/update`, `tenant/delete`, `user/create`, `user/get`, `user/update`, `user/delete`, `credential/create`, `credential/get`, `credential/getbybearertoken`, `credential/update`, `credential/delete`, `credential/deletebyuser`, and `credential/deleteallintenant` through authenticated REST calls so admin requirements, tenant boundaries, and authorization audit behavior are honored.
    - [x] Route `tenant/all`, `tenant/enumerate`, `tenant/exists`, `tenant/statistics`, `tenant/statisticsall`, `tenant/getmany`, `user/all`, `user/enumerate`, `user/exists`, `user/getmany`, `credential/all`, `credential/enumerate`, `credential/exists`, and `credential/getmany` through authenticated REST calls so admin requirements and same-tenant read policies are honored.
    - [x] Route `authorization/role/*`, `authorization/userrole/*`, `authorization/credentialscope/*`, `authorization/user/permissions`, and `authorization/credential/permissions` through authenticated REST calls so role/scope management remains behind the REST RBAC and audit boundary.
  - [x] Add role/scoping tools only if safe and useful.

- [x] **4.8** Identity boundary.
  - [x] RBAC is based on LiteGraph-native users and credentials.
  - [x] Do not map arbitrary external identities to LiteGraph users.
  - [x] Do not auto-provision users from external identities.
  - [x] Do not add email/claim heuristics for external identity mapping.
  - [x] If JWT-formatted tokens are supported, they must represent LiteGraph-controlled users or credentials by an explicit LiteGraph identifier.
  - [x] Treat external identity federation as out of scope unless a separate design explicitly adds it.

- [x] **4.9** Dashboard support.
  - [x] Role management page under the admin dashboard.
  - [x] Built-in roles are visible and immutable; custom roles can be created, edited, and deleted where authorized.
  - [x] User role assignment UI supports tenant and graph scopes.
  - [x] Credential scope UI supports role-backed and direct permission/resource grants.
  - [x] Effective user and credential permissions are visible from the dashboard.
  - [x] Permission-aware hiding/disabling of restricted operations.

- [x] **4.10** Tests.
  - [x] Permission matrix tests in `Test.Shared`.
  - [x] REST boundary tests.
  - [x] MCP boundary tests.
  - [x] Permission model definition tests for built-in roles, permissions, resource scopes/types, inheritance intent, clone protection, and migration defaults.
  - [x] Role and assignment storage tests for built-in seeding plus create, read, search, update, pagination, filtering, and delete across roles, user-role assignments, and credential-scope assignments.
  - [x] Effective role and credential-scope authorization tests for compatibility fallback, graph scoping, missing permission denials, tenant-role inheritance, direct credential permissions, and query mutation scope.
  - [x] Credential scope and graph allow-list persistence tests in `Test.Shared`.
  - [x] Authorization service tests for credential scope, graph allow-list, unrestricted-credential compatibility, and tenant decisions.
  - [x] Authorization failure response context tests.
  - [x] Authorization audit persistence/search/delete tests with tenant, graph, user, credential, request ID, correlation ID, trace ID, request type, reason, scope, time window, and pagination filters.
  - [x] Live REST denied-query test verifies a missing-scope denial writes an authorization audit record.
  - [x] Live REST role-management coverage for role CRUD, built-in role immutability, user-role assignment CRUD, credential-scope assignment CRUD, effective permission listing, TenantAdmin access, Viewer denial, and unassigned-user admin denial.
  - [x] Live MCP role/scope management coverage for authorization role CRUD/list, user-role assignment CRUD/list/effective permissions, credential-scope assignment CRUD/list/effective permissions, and admin-token delete results.
  - [x] Live MCP scoped-credential boundary coverage for allowed graph/node/edge/label/tag/vector/query/batch-existence reads plus same-tenant tenant statistics, allowed node and edge list/read-all/search/read-first/enumerate/existence/traversal paths, allowed label/tag/vector list/read-all/enumerate/existence/graph/node/edge paths, allowed vector search, and denied graph create/update/delete, node create/update/delete/bulk-create/bulk-delete/delete-all, edge create/update/delete/bulk-create/bulk-delete/delete-all/node-edge-delete, label/tag/vector create/update/delete/bulk-create/bulk-delete/delete-all/graph-child-delete/node-child-delete/edge-child-delete, mutation queries, transactions, graph allow-list violations including batch existence, node/edge/label/tag/vector list/enumeration/traversal/search, label/tag/vector GUID routes, admin tools, tenant/user/credential admin-boundary and list/enumeration/exists/getmany routes, authorization role/scope/effective-permission admin-boundary routes, and authorization-audit records.
  - [x] Migration/backward compatibility tests for legacy SQLite credentials and authorization schema initialization.
  - [x] Cache invalidation tests for effective policy cache reuse plus role create/update/delete, user-role create/update/delete, and credential-scope create/update/delete invalidation.
  - [x] Query and transaction authorization tests.
    - [x] Query scope classification test for read, vector search, create, `MATCH ... SET`, and `MATCH ... DELETE`.
    - [x] Transaction request type/resource mapping tests cover required `Write` permission over `Transaction`.
    - [x] Effective role and credential-scope tests cover graph transaction allow/deny behavior.
    - [x] Live MCP scoped-credential boundary tests cover transaction denial paths and matching authorization-audit records.
  - [x] Dashboard authorization tests for role management, credential scopes, effective permissions, and permission-aware disabled actions.

### Acceptance Criteria

- Viewer and Editor roles work out of the box.
- Graph-level permissions isolate restricted graphs within a tenant.
- API credentials can be scoped to least privilege.
- Existing deployments retain effective access after migration.
- Authorization is enforced consistently at REST and MCP boundaries.
- Permission checks add minimal request overhead when cached.

---

## 5. Observability

**Priority:** P1
**Effort:** Large
**Status:** `[x]` Complete for this release slice; Prometheus, OpenTelemetry/OTLP, profiling, request-history correlation, JSON request logs, dashboard, docs, and tests are implemented
**Impact:** Enables production debugging, metrics, tracing, and performance analysis

### Why This Matters

- Production users need standard monitoring and tracing.
- Prometheus and OpenTelemetry are the priority targets.
- Existing request history should be used as supporting operational data, not replaced.

### Implementation Steps

- [x] **5.1** Add request lifecycle foundation.
  - [x] Pass meaningful cancellation tokens from REST handlers into service/repository work.
    - [x] REST authentication, generic agnostic handlers, native query execution, and graph transaction execution now receive request timeout cancellation tokens.
    - [x] Request-history list, summary, read, detail, delete, and bulk-delete routes now pass request timeout cancellation tokens into repository work.
    - [x] Authorization management, token detail, graph update, GEXF export, and vector-index routes now pass request timeout cancellation tokens into service/repository work.
    - [x] REST handler coverage asserts no `CancellationToken.None` remains in the REST handler partials.
  - [x] Add request timeout settings where missing.
    - [x] `Settings.RequestTimeoutSeconds` defaults to 60 seconds and can be overridden by `LITEGRAPH_REQUEST_TIMEOUT_SECONDS`.
  - [x] Ensure query and transaction timeouts are enforced.
    - [x] REST native query and transaction endpoints return HTTP 408 with `RequestTimeout` when the request timeout token fires.
    - [x] Add correlation/request ID propagation.
    - [x] REST emits `x-request-id` and `x-correlation-id` response headers.
    - [x] REST accepts incoming `x-request-id`, `x-correlation-id`, `traceparent`, and `tracestate` headers.

- [x] **5.2** Add OpenTelemetry tracing.
  - [x] Add OpenTelemetry-compatible `Meter` and `ActivitySource` foundation.
  - [x] Trace REST request lifecycle.
    - [x] Server activity per REST request with method, path, request ID, correlation ID, tenant, graph, status, and duration tags.
  - [x] Trace authentication and authorization.
  - [x] Trace repository operations.
    - [x] Core `LiteGraph` ActivitySource emits repository operation spans tagged by provider, operation, transactional state, statement count, row count, success, and duration.
  - [x] Trace transactions.
  - [x] Trace query parse/plan/execute phases.
    - [x] REST query activity includes required scope, success, mutation, row count, query kind, and vector-search tags.
    - [x] Core `LiteGraph` ActivitySource emits dedicated native query, parse, plan, and executor spans for direct client, REST, MCP, and console query execution.
  - [x] Trace vector search and vector index usage.
    - [x] Native query vector search adds vector domain and result count tags to the query activity.
    - [x] Core vector search spans include domain, search type, dimensions, filters, top-k, and result count tags.
    - [x] SQLite HNSW vector index search spans include index type, dirty state, used/skip reason, top-k, and result count tags.
  - [x] Propagate incoming trace context.
  - [x] Export via OTLP configuration.
    - [x] `ObservabilitySettings` includes opt-in built-in OTLP exporter settings for endpoint, protocol, headers, timeout, and service name.
    - [x] LiteGraph Server subscribes the OTLP tracer and meter providers to both the configured server source/meter and the core `LiteGraph` source/meter.
    - [x] `LITEGRAPH_OTLP_*` and standard `OTEL_*` environment variables are supported where appropriate.

- [x] **5.3** Add Prometheus metrics.
  - [x] `GET /metrics`
  - [x] Endpoint can be unauthenticated initially.
  - [x] Request count by route, method, and status.
  - [x] Request duration histograms.
    - [x] Prometheus exposes HTTP request duration bucket, sum, and count samples by method, path, and status code.
  - [x] Query count/duration/failure metrics.
  - [x] Transaction count/duration/rollback metrics.
  - [x] Repository operation count/duration by provider.
    - [x] Prometheus exposes repository operation count and duration summaries by provider, operation, and success.
  - [x] Vector search latency and result count.
  - [x] Authorization allow/deny counters.
  - [x] Storage backend info gauge.
  - [x] Storage backend and connection/pool gauges where available.
    - [x] Prometheus and .NET meters expose configured storage provider, max connection pool size, and command timeout; live pool utilization remains provider-specific future work.
  - [x] Entity count gauges where efficient.
    - [x] Prometheus and .NET meters expose low-cardinality latest-observed entity counts from tenant and graph statistics responses.

- [x] **5.4** Use request history as observability support.
  - [x] Keep existing request capture and retention.
  - [x] Add links between request history records and correlation/trace IDs.
    - [x] Request history persists request ID, correlation ID, and trace ID.
    - [x] Request history search supports request ID, correlation ID, and trace ID filters.
  - [x] Use request history for recent error/debug views.
    - [x] Request history search supports a `success` filter, dashboard request-history views include an outcome filter, and the dashboard SDK exposes a recent-errors helper.
  - [x] Ensure sensitive headers and bodies remain redacted/truncated.

- [x] **5.5** Add structured operational logging where needed.
  - [x] Keep existing logging behavior compatible.
  - [x] Add JSON log output as an option if practical.
    - [x] `Settings.Logging.JsonLogOutput` enables supported REST request completion logs as single-line JSON records.
  - [x] Include correlation/request IDs.
    - [x] REST request log lines include request ID, correlation ID, and trace ID when available.
  - [x] Avoid logging credentials, tokens, passwords, connection strings, or vector payloads by default.
    - [x] Operational log and trace URL redaction covers bearer-token route segments, sensitive query-string keys, sensitive headers, and vector payload key names.
    - [x] REST debug request logging now emits a sanitized request summary and omits request bodies.

- [x] **5.6** Add query/request profiling.
  - [x] Profiling request option.
  - [x] Include parse, plan, auth, repository, vector search, transaction, and serialization timing.
    - [x] Query response profile includes parse, plan, execute, and total timings.
    - [x] Scoped query profiling now includes repository operation time/count, vector search time/count, and mutation transaction timing.
    - [x] REST query profiling adds authorization and response serialization timings.
  - [x] Ensure profiling is off by default.
  - [x] Keep overhead low when profiling is disabled.

- [x] **5.7** Dashboard and documentation.
  - [x] Grafana dashboard JSON template.
    - [x] Added `assets/grafana/litegraph-observability-dashboard.json` with Prometheus panels for HTTP, query, transaction, vector search, repository, entity count, storage, and auth metrics.
  - [x] Prometheus scrape docs.
  - [x] OpenTelemetry instrumentation docs.
    - [x] Document server and core Meter/ActivitySource names, native query phase spans, vector search spans, vector index search spans, and repository operation spans.
    - [x] Document built-in OTLP exporter settings and environment variables.
  - [x] Dashboard views may use request history for recent request/error inspection.
    - [x] The request-history dashboard view can filter all requests, successful requests, or errors.

- [x] **5.8** Tests.
  - [x] `/metrics` service renderer exposes expected metrics.
  - [x] Live `/metrics` endpoint smoke test verifies unauthenticated access and Prometheus output.
  - [x] Request metrics increment correctly in service-level coverage.
  - [x] HTTP request duration histogram bucket coverage.
  - [x] Query and transaction metrics render correctly in service-level coverage.
  - [x] Vector search metrics render correctly in service-level coverage.
  - [x] Authentication/authorization metrics render correctly in service-level coverage.
  - [x] Storage backend info metric renders correctly in service-level coverage.
  - [x] Trace instrumentation can be enabled without breaking requests.
  - [x] Native query tracing verifies query, parse, plan, execute, vector search, and vector index activity hierarchy and tags.
  - [x] Repository operation metrics render in Prometheus output.
  - [x] Repository operation tracing verifies provider and success tags under native query execution.
  - [x] Storage connection pool and command timeout metrics render in service-level and live `/metrics` coverage.
  - [x] Entity count metrics render in service-level coverage.
  - [x] Request history includes correlation/trace IDs.
  - [x] Request history success/failure search coverage.
  - [x] Request history redaction and body truncation coverage.
  - [x] Operational log redaction coverage for sensitive URL, header, scalar, connection-string, and vector payload values.
  - [x] Request timeout setting, bounds, environment variable, and HTTP 408 API error coverage.
  - [x] Grafana dashboard template parses and covers exported Prometheus metric families.
  - [x] Dashboard SDK request-history recent-error helper coverage.
  - [x] OTLP exporter settings and provider initialization coverage.
  - [x] Query profile coverage for repository, vector search, transaction, authorization, and serialization timings.
  - [x] JSON operational request log formatting coverage.

### Acceptance Criteria

- Prometheus metrics cover the primary operational signals.
- OpenTelemetry traces cover REST, auth, repository, query, transaction, and vector search work.
- `/metrics` works out of the box.
- Request history remains available and is correlated with observability IDs.
- Sensitive data is not exposed in metrics, traces, logs, or request history.

---

## 6. SDK, MCP, Dashboard, and Documentation Integration

**Priority:** P1
**Effort:** Medium to Large
**Impact:** Makes the release usable across all LiteGraph entry points

### Implementation Steps

- [x] **6.1** C# SDK updates.
  - [x] Query execution.
  - [x] Transaction request models and execution.
  - [x] Transaction request builder.
  - [x] Role and credential scope management through `LiteGraphClient.AuthorizationRoles`.
  - [x] Storage-related admin models where relevant; embedded C# callers use `DatabaseSettings`, `DatabaseTypeEnum`, and `GraphRepositoryFactory`, and no runtime storage-admin REST model is required for this release.
  - [x] Observability docs/examples where relevant.

- [x] **6.2** Python SDK updates.
  - [x] Query execution.
  - [x] Transaction helper.
  - [x] Role and credential scope management where appropriate.

- [x] **6.3** JavaScript SDK updates.
  - [x] Query execution.
  - [x] Transaction helper.
  - [x] Role and credential scope management where appropriate.

- [x] **6.4** MCP tools.
  - [x] `graph/query`
  - [x] graph-scoped transaction tool
  - [x] RBAC enforcement for all existing tools
    - [x] Authenticated REST proxy coverage for representative graph/node/edge/label/tag/vector read/write/delete and admin tools, including child-object update/delete denial paths.
    - [x] Authenticated REST proxy coverage for `batch/existence` graph read checks.
    - [x] Authenticated REST proxy coverage for tenant/user/credential create/read/update/delete and credential bearer/bulk-delete admin-boundary tools.
    - [x] Authenticated REST proxy coverage for tenant/user/credential list/enumerate/exists/getmany plus tenant statistics tools.
    - [x] Authenticated REST proxy coverage for node list/read-all/search/read-first/enumerate/existence/traversal and bulk create/delete tools.
    - [x] Authenticated REST proxy coverage for edge list/read-all/search/read-first/enumerate/existence/node-edge traversal and bulk create/delete tools.
    - [x] Authenticated REST proxy coverage for label list/read-all/enumerate/existence/graph/node/edge and bulk create/delete tools.
    - [x] Authenticated REST proxy coverage for tag/vector list/read-all/enumerate/existence/search/graph/node/edge and bulk create/delete tools.
    - [x] Live MCP scoped-credential boundary coverage for representative read/write/query/transaction/batch-existence/admin tools, node, edge, label, tag, and vector list/bulk/enumeration/traversal/search-style tools, plus tenant/user/credential admin-boundary and list/enumeration tools.
  - [x] Role/scope tools only where safe.
    - [x] `authorization/role/*`, `authorization/userrole/*`, `authorization/credentialscope/*`, `authorization/user/permissions`, and `authorization/credential/permissions` are authenticated REST-proxy tools on HTTP, TCP, and WebSocket MCP transports.

- [x] **6.5** Dashboard.
  - [x] API Explorer examples for query and transaction.
    - [x] Transaction request template and failure summary.
    - [x] Query request template.
    - [x] Query line/column error summary.
  - [x] Role and credential scope management where appropriate.
  - [x] Permission-aware UI behavior.
  - [x] Request history observability enhancements.
  - [x] Metrics/tracing setup docs or links.

- [x] **6.6** Documentation.
  - [x] Storage backend configuration.
  - [x] PostgreSQL production setup and production hardening.
  - [x] SQLite-to-PostgreSQL migration; target sequence, provider-neutral migration tooling, and verification behavior are documented.
  - [x] Create `DSL.md` with thorough native graph query language syntax, semantics, examples, parameters, result shape, and current limitations.
  - [x] Transaction API guide.
  - [x] RBAC and scoped credential guide for currently implemented scoped credentials and current RBAC limits.
  - [x] Prometheus and OpenTelemetry guide for currently implemented metrics/instrumentation.
  - [x] `LiteGraphConsole` usage and global-tool install guide.
  - [x] Upgrade/migration guide for existing deployments.

### Acceptance Criteria

- Each new user-facing feature has REST documentation.
- SDKs expose the features where appropriate.
- MCP exposes the features where appropriate.
- Dashboard includes support where appropriate.
- Documentation includes migration and operational setup guidance.

---

## 7. Test Strategy

**Priority:** P1
**Effort:** Large
**Impact:** Ensures the release is safe across backends and entry points

### Required Test Suites

- [x] **7.1** Storage backend suites.
  - [x] SQLite.
  - [x] PostgreSQL.
    - [x] Factory coverage verifies the PostgreSQL repository implementation is selected.
    - [x] SQL translation unit coverage verifies core provider dialect rewrites.
    - [x] Environment-gated live smoke covers initialization, core graph child CRUD, and graph transaction commit/rollback.
    - [x] Full SQLite parity suite against PostgreSQL.
  - [x] PostgreSQL provider suites are registered with explicit dependency skip reasons when test connection strings are not configured.

- [x] **7.2** Transaction suites.
  - [x] Successful mixed operation commit.
  - [x] Rollback on failure.
  - [x] Attach, detach, and upsert operation coverage.
  - [x] Timeout/cancellation.
    - [x] Pre-cancelled transaction coverage.
    - [x] Long-running transaction timeout rollback coverage.
  - [x] Concurrent transaction behavior.
    - [x] SQLite active-transaction guard coverage for transaction API and query mutation rejection.
    - [x] PostgreSQL live concurrent transaction coverage through separate repository instances.
  - [x] Vector index dirty/repair behavior.

- [x] **7.3** Query suites.
  - [x] Initial lexer coverage.
  - [x] Parser.
  - [x] Planner.
    - [x] Initial planner seed/warning/mutation tests in `Test.Shared`.
    - [x] OR/NOT scan-warning and seed-avoidance coverage.
    - [x] Aggregate scan-warning and cost coverage.
  - [x] Read queries.
    - [x] Initial node, edge, fixed multi-hop, data filter, string predicate, and ordering coverage.
    - [x] OR/NOT, parentheses, `IN` list, and tag-aware predicate coverage for node, edge, and fixed-path reads.
    - [x] Aggregate read coverage for node, edge, fixed-path, data-field, cost, and tag-value aggregates.
  - [x] Mutation queries.
    - [x] Initial node, edge, label, tag, and vector create/update/delete coverage.
  - [x] Vector queries.
    - [x] Node, edge, graph-scoped, and vector-indexed node search through native query `CALL`.
  - [x] Error messages.
    - [x] Initial parser, missing parameter, invalid GUID parameter, and unsupported return-variable messages.
  - [x] Timeout/cancellation.
    - [x] Initial pre-cancelled query execution coverage.
  - [x] Opt-in query execution profile coverage.
  - [x] Bounded variable-length path, shortest path, optional match, and query execution engine extraction coverage.
  - [x] Environment-gated PostgreSQL native-query smoke coverage.

- [x] **7.4** RBAC suites.
  - [x] Built-in roles.
    - [x] Built-in role definition coverage.
    - [x] Request type, permission, resource type, and role matrix coverage.
    - [x] SQLite built-in role seeding and role storage coverage.
    - [x] Effective built-in role assignment coverage through `AuthorizationService`.
  - [x] Graph-level permissions.
    - [x] Effective graph-scope assignment coverage through `AuthorizationService`.
  - [x] Scoped credentials.
    - [x] Persistence coverage for credential scopes and graph allow-lists.
    - [x] User-role assignment and credential-scope assignment storage coverage.
    - [x] Effective credential-scope assignment coverage through `AuthorizationService`.
    - [x] Central authorization service coverage for credential scope, graph allow-list, unrestricted-credential compatibility, and tenant decisions.
    - [x] Effective-permission cache reuse and invalidation coverage for role/scope mutation paths.
    - [x] Authorization audit storage and live REST missing-scope denial coverage.
  - [x] Existing deployment migration coverage for legacy SQLite credentials and authorization schema initialization.
  - [x] REST enforcement.
    - [x] Live REST graph-query missing-scope denial coverage.
    - [x] Live REST role-management coverage for role CRUD, assignment CRUD, effective permission listing, built-in immutability, TenantAdmin access, Viewer denial, and unassigned-user admin denial.
  - [x] MCP enforcement.
    - [x] Live MCP scoped-credential coverage for allowed graph/node/edge/label/tag/vector/query/batch-existence reads plus same-tenant tenant statistics, allowed node and edge list/read-all/search/read-first/enumerate/existence/traversal paths, allowed label/tag/vector list/read-all/enumerate/existence/graph/node/edge paths, allowed vector search, denied graph create/update/delete, node create/update/delete/bulk-create/bulk-delete/delete-all, edge create/update/delete/bulk-create/bulk-delete/delete-all/node-edge-delete, label/tag/vector create/update/delete/bulk-create/bulk-delete/delete-all/graph-child-delete/node-child-delete/edge-child-delete, mutation query, graph transaction, graph allow-list violations including batch existence, node/edge/label/tag/vector list/enumeration/traversal/search, and child-object GUID routes, admin flush, tenant/user/credential admin-boundary/list/enumeration tools, authorization role/scope/effective-permission admin-boundary tools, and matching authorization audit records.
    - [x] Live MCP admin-token role/scope tool coverage for role CRUD/list, user-role CRUD/list/effective permissions, and credential-scope CRUD/list/effective permissions.

- [x] **7.5** Observability suites.
  - [x] Metrics endpoint.
  - [x] Metrics values after representative requests.
    - [x] Service-level HTTP, query, vector search, transaction, and auth metric rendering.
  - [x] Trace enablement smoke tests.
  - [x] Request history correlation.
  - [x] Sensitive data redaction.

- [x] **7.6** SDK/MCP/dashboard tests.
  - [x] SDK request/response model tests.
    - [x] Python query request/result model and execution helper tests.
    - [x] JavaScript query request/result model and execution helper tests.
    - [x] Python transaction request builder, execution, and context helper tests.
    - [x] JavaScript transaction builder, execution, and rollback-detail tests.
    - [x] Python authorization role, user-role, credential-scope, and effective-permission helper tests.
    - [x] JavaScript authorization role, user-role, credential-scope, and effective-permission helper tests.
  - [x] C# transaction request builder test in `Test.Shared`.
  - [x] MCP tool tests.
  - [x] MCP `graph/query` live-host test in `Test.Shared`.
  - [x] MCP `graph/transaction` live-host test in `Test.Shared`.
  - [x] MCP authorization role/scope live-host tests in `Test.Shared`.
  - [x] Dashboard unit/integration tests where UI changes exist.
    - [x] Authorization dashboard tests cover role list immutability, user role assignment display, credential scope display, effective-permission display, and disabled mutation actions without admin permission.
    - [x] API Explorer tests cover query request templates and query line/column error summaries.
    - [x] Request history observability tests cover metrics/tracing links and visible request statistics.

### Runner Requirements

- [x] Test descriptors live in `src/Test.Shared`.
- [x] Console execution through `src/Test.Automated`.
- [x] xUnit exposure through `src/Test.Xunit`.
- [x] NUnit exposure through `src/Test.Nunit`.
- [x] Tests create and clean up their own data.
- [x] Provider-specific tests can be skipped with clear skip reasons when dependencies are unavailable.

### Acceptance Criteria

- The same core behavior tests run against SQLite and PostgreSQL.
- Release blockers are visible from `Test.Automated`.
- xUnit and NUnit runners expose the shared suites.
- Performance and benchmark tests are documented even if not run on every commit.

---

## Release Acceptance Criteria

The release is complete when:

- SQLite remains backward compatible and default.
- PostgreSQL is production-ready and documented.
- Graph-scoped transactions work for nodes, edges, labels, tags, and vectors.
- Native query endpoint supports reads, traversals, vector search, and child-object mutations.
- Query language syntax and parameter model decisions are documented in `DSL.md`.
- `LiteGraphConsole` is available as a solution project and can be installed as the `lg` global tool for local database or remote endpoint sessions.
- RBAC and scoped credentials are enforced at REST and MCP boundaries.
- Existing users and credentials retain effective full access after migration.
- Prometheus metrics and OpenTelemetry tracing work out of the box.
- Request history is correlated with observability IDs.
- SDK, MCP, dashboard, and docs are updated where appropriate.
- Tests are implemented in `Test.Shared` and exposed through automated, xUnit, and NUnit runners.

## Remaining Query Work

- [x] Choose the familiar graph-query syntax target.
- [x] Decide the query parameter model.
- [x] Expand examples to 20+ covered queries.
- [x] Replace the initial regex-backed implementation with the full lexer/parser/planner/executor pipeline.
  - [x] Query execution now routes through `Planner` and `Executor` components.
- [x] Add supplied-vector search coverage.
  - [x] Native query vector search covers node, edge, graph-scoped graph vector, and vector-indexed node domains.
- [x] Add edge/label/tag/vector create mutation coverage.
- [x] Add update/delete mutation query coverage.
  - [x] Node and edge update/delete via `MATCH ... WHERE <var>.guid = ... SET/DELETE ... RETURN ...`.
  - [x] Label, tag, and vector update/delete via `MATCH LABEL|TAG|VECTOR ... WHERE <var>.guid = ... SET/DELETE ... RETURN ...`.
- [x] Add deeper traversal and multi-hop pattern coverage.
  - [x] Fixed directed multi-hop patterns such as `(a)-[e1]->(b)-[e2]->(c)`.
  - [x] Bounded variable-length paths, shortest path, optional matches, and richer traversal operators.
- [x] Create and maintain `DSL.md` as the canonical query-language guide.
- [x] Create `LiteGraphConsole` and package it as the `lg` global tool for interactive and scripted query execution.
- [x] Add MCP `graph/query` execution through the REST query endpoint.
- [x] Move execution into dedicated planner/executor components.
  - [x] Added `GraphQueryPlan`, `Planner`, and `Executor`.
  - [x] Removed obsolete direct `QueryMethods.ExecuteParsed` dispatcher.
  - [x] Move remaining execution helper logic out of `QueryMethods`.

The query language is no longer blocked by product decisions. The current implementation is a parser-backed Cypher/GQL-inspired LiteGraph-native profile for node `MATCH`, edge `MATCH`, fixed directed multi-hop `MATCH`, bounded variable-length path `MATCH`, `MATCH SHORTEST`, top-level read-only `OPTIONAL MATCH`, equality predicates over GUID/name/data fields, numeric comparison predicates over data fields, string predicates over supported name/data fields, OR/NOT/AND predicate expressions, list predicates, tag predicates, aggregate returns, `ORDER BY`, node/edge/label/tag/vector `CREATE`, graph-child `SET` and `DELETE` mutations, supplied-vector search through `CALL litegraph.vector.searchNodes(...)`, REST execution, C#/Python/JavaScript SDK execution, MCP `graph/query`, API Explorer execution templates, and `LiteGraphConsole` execution.
