# LiteGraph v9.0.0 — Graph Algorithms Implementation Plan

**Feature:** Native graph algorithms (centrality, community detection, PageRank, connected components, and graph embeddings).
**Target release:** v9.0.0
**Branch:** `V9.0`
**Closes:** CKG gap #2 — "No graph algorithms (§3.2)" (see `CKG_IMPROVEMENTS.md`).
**Status:** Planning.

---

## 1. Motivation

`CKG.md` §3.2 identifies the absence of graph algorithms as "the single strongest argument against LiteGraph as a complete CKG solution." A cognitive knowledge graph needs spreading activation, salience, centrality, and clustering — the "cognition" layer. LiteGraph today provides `MATCH`, bounded variable-length paths, `MATCH SHORTEST`, and aggregates, but no centrality, community detection, PageRank, or embeddings.

This release adds a first-class **graph algorithms** capability spanning the full product surface: core library, REST server, MCP server, dashboard, SDKs, tests, and documentation.

---

## 2. Design principles (grounded in current architecture)

1. **Whole-graph compute, not per-hop traversal.** The DSL executes traversal per-hop (`Node.ReadByGuid` / `Edge.ReadEdgesFromNode` per edge). That is correct for bounded pattern matching but wrong for whole-graph analytics. Algorithms load the entire node/edge set **once** via the existing streaming enumeration primitives `Node.ReadAllInGraph(...)` and `Edge.ReadAllInGraph(...)`, then build a compact in-memory adjacency (CSR arrays keyed by a `Dictionary<Guid,int>` index).
2. **Sit above the client layer where possible.** On-demand compute consumes existing client enumeration methods, so **Phase 1 requires no storage/SQL changes**. This is the single biggest lever on delivery cost.
3. **Follow the `VectorIndexManager` precedent** for any per-graph cached/persisted state (lifecycle, per-graph locking, file persistence, repository ownership, clone/transaction sharing).
4. **Reuse existing cross-cutting rails**: OpenTelemetry activities, timeout `CancellationTokenSource`, LRU caches, and the REST authorization pipeline (`AuthorizeRequestScope`).
5. **Every public method takes a `CancellationToken`** and honors a configurable timeout, per repository coding standards in `CLAUDE.md`.
6. **Write-back makes results DSL-queryable for free.** Materializing results into node `Data`/`Tags` means `WHERE n.data.pagerank > 0.1` works with zero query-engine changes.

---

## 3. Algorithm scope (by phase)

| Algorithm | Category | Phase | Notes |
|---|---|---|---|
| Degree centrality (in / out / total) | Centrality | 1 | Repo already computes degree in SQL (`ReadMostConnected`/`ReadLeastConnected`) |
| PageRank | Centrality | 1 | Iterative, damping + tolerance + max-iterations params |
| Weakly connected components | Structure | 1 | Union-find |
| Strongly connected components | Structure | 1 | Tarjan |
| Label propagation | Community | 1 | Fast, near-linear |
| Closeness centrality | Centrality | 2 | Multi-source BFS |
| Betweenness centrality (exact + sampled) | Centrality | 2 | Brandes; sampled variant for large graphs |
| Louvain modularity | Community | 2 | Higher quality than label propagation |
| Eigenvector centrality | Centrality | 3 | Power iteration |
| Triangle count / local clustering coefficient | Structure | 3 | |
| k-core decomposition | Structure | 3 | |
| FastRP node embeddings | Embedding | 4 | Emit as node `Vector` → indexable by existing HNSW |
| node2vec node embeddings | Embedding | 4 | Random-walk based |

**Embedding synergy (Phase 4):** node embeddings produce a vector per node that can be attached via the existing `Vector` model and indexed by `VectorIndexManager` — turning "find structurally similar concepts" into an existing HNSW vector search. No new retrieval infrastructure required.

### Non-goals for v9.0.0
- Distributed / out-of-core computation for graphs exceeding the in-memory ceiling. Above a documented node/edge threshold, native algorithm requests are **rejected with a clear error** rather than risking OOM. (Ties to CKG gap #1 — unproven scale; guardrails must be honest.)
- Incremental / streaming maintenance of results on every write (recompute-on-demand is the v9 model; optional caching in Phase 3 uses explicit invalidation).
- **Hosting or orchestrating the external `rustworkx`/NetworkX compute itself.** LiteGraph does **not** run Python or embed a Python runtime. What v9 **does** build (see §6A) is a first-class, documented **export/projection format** and a results **import** path so a user can round-trip a graph to `rustworkx`/NetworkX for algorithms beyond native scope (or for graphs past the in-memory ceiling) and write the computed values back. The compute happens in the user's environment; the interop format is a shipped, tested product feature — not merely a documentation suggestion.

---

## 4. Phase 0 — Branch and version bump (FIRST STEP)

### 4.1 Create the branch
```bash
git checkout main
git pull
git checkout -b V9.0
```

### 4.2 Set all version numbers to `9.0.0`

**`.csproj` files** — change `<Version>8.1.0</Version>` → `<Version>9.0.0</Version>` in all 15 versioned projects:

- `src/LiteGraph/LiteGraph.csproj`
- `src/LiteGraph.Server/LiteGraph.Server.csproj`
- `src/LiteGraph.McpServer/LiteGraph.McpServer.csproj`
- `src/LiteGraph.SampleDatabase/LiteGraph.SampleDatabase.csproj`
- `src/LiteGraphConsole/LiteGraphConsole.csproj`
- `src/LoadGenerator/LoadGenerator.csproj`
- `src/Test/Test.csproj`
- `src/Test.Automated/Test.Automated.csproj`
- `src/Test.Nunit/Test.Nunit.csproj`
- `src/Test.PerformanceAndScalability/Test.PerformanceAndScalability.csproj`
- `src/Test.Shared/Test.Shared.csproj`
- `src/Test.Xunit/Test.Xunit.csproj`
- `sdk/csharp/src/LiteGraph.Sdk/LiteGraph.Sdk.csproj`
- `sdk/csharp/src/Test.Automated/Test.Automated.csproj`
- `sdk/csharp/src/Test.Sdk/Test.Sdk.csproj`

Also update `<PackageReleaseNotes>` in `src/LiteGraph/LiteGraph.csproj` to the v9.0.0 summary (graph algorithms).

**JavaScript / dashboard** — set `"version": "9.0.0"` and refresh the lockfiles via `npm install`:
- `dashboard/package.json` (currently 8.1.0)
- `sdk/js/package.json` — npm package `litegraphdb` (currently 8.1.0)

**Python SDK (`sdk/python`)** — set the version to `9.0.0` in every place it is declared:
- `sdk/python/src/litegraph_sdk/__init__.py` — `__version__ = "9.0.0"` (currently 8.1.0)
- `sdk/python/pyproject.toml` and `sdk/python/setup.py` — update the `version` field if statically declared; if the version is sourced dynamically from `__init__.py`, updating `__version__` is sufficient (verify at bump time).

**All three SDKs (C#, JavaScript, Python) ship at `9.0.0` in lockstep with the core/server/MCP/dashboard.** No SDK is left on a prior version.

### 4.3 Seed the changelogs
- Add a `## Current Version` → `v9.0.0` block at the top of `CHANGELOG.md` (move the existing v8.1.0 block under a "Previous Versions" heading).
- Add a matching entry to `dashboard/CHANGELOG.md`.

### 4.4 Reconcile the README version drift (CKG gap #5, cheap to fold in here)
The root `README.md` documents an older version than litegraphdb.com. Update it to v9.0.0 as part of this release so the repo and site agree.

**Exit criteria for Phase 0:** solution builds on the branch (`dotnet build src/LiteGraph.sln`), dashboard installs, all version strings read `9.0.0`.

---

## 5. Workstream A — Core library (`src/LiteGraph`)

### 5.1 New namespace `LiteGraph.Algorithms`
New folder `src/LiteGraph/Algorithms/`:

- **`GraphAdjacency.cs`** — compact in-memory graph built from two streaming scans (`Node.ReadAllInGraph`, `Edge.ReadAllInGraph`). CSR representation: `Dictionary<Guid,int>` node index, `int[]` row offsets, `int[]` column targets, `double[]` edge costs, plus a reverse index for directed algorithms. Includes a `Build(...)` factory that accepts a `CancellationToken` and enforces the node/edge ceiling.
- **`IGraphAlgorithm.cs`** — contract for an algorithm: takes a `GraphAdjacency` + parameters, returns a result payload, respects `CancellationToken`.
- **One file per algorithm** (one class per file, per `CLAUDE.md`): `PageRank.cs`, `DegreeCentrality.cs`, `WeaklyConnectedComponents.cs`, `StronglyConnectedComponents.cs`, `LabelPropagation.cs`, … (added per phase table §3).
- **`GraphAlgorithmRunner.cs`** — orchestrates: load adjacency → run algorithm → map integer indices back to node GUIDs → assemble result.

### 5.2 Models (`src/LiteGraph/`)
- `GraphAlgorithmTypeEnum.cs` — enum of supported algorithms.
- `GraphAlgorithmRequest.cs` — `AlgorithmType`, algorithm-specific parameters (damping factor, max iterations, tolerance, sample size, top-K), `WriteBack` flag + target property name, optional node/edge filters.
- `GraphAlgorithmResult.cs` — per-node scores or community labels, iteration/convergence metadata, elapsed time, node/edge counts processed.
- `GraphAlgorithmConfiguration.cs` — server-side defaults and the max node/edge ceiling.

### 5.3 Client surface `client.Algorithm`
- `Client/Interfaces/IAlgorithmMethods.cs` and `Client/Implementations/AlgorithmMethods.cs` — validates tenant/graph via existing `ValidateGraphExists`, invokes `GraphAlgorithmRunner`, optionally writes results back through the existing `Batch`/`CreateMany`/update path.
- Wire into `LiteGraphClient.cs` composition root (constructor ~line 281) and expose public property `Algorithm` (alongside `Node`, `Edge`, `Query`, `Vector`, `VectorIndex`).

### 5.4 Optional per-graph result cache (Phase 3)
Only if repeated queries over stable graphs justify it. Model on `VectorIndexManager`:
- `Algorithms/AlgorithmResultManager.cs` — `ConcurrentDictionary<Guid, …>` keyed by graph GUID, per-graph `SemaphoreSlim`, file persistence under a storage dir, owned by the repository with clone/transaction sharing and full `Dispose`. Explicit invalidation on graph mutation (dirty flag) — recompute-on-demand remains the default.

### 5.5 DSL `CALL litegraph.algo.*` (Phase 3)
Mirror the `litegraph.vector.searchNodes` wiring — four edit sites:
1. `Query/Parser.cs` — recognize `litegraph.algo.pagerank` / `.degree` / `.community` / … (extend `ProcedureToVectorDomain` or generalize CALL to store an arbitrary `ProcedureName` + args).
2. `Query/GraphQueryKindEnum.cs` — add `Algorithm`.
3. `Query/Executor.cs` — add the `Algorithm` case.
4. `Client/Implementations/QueryExecutionEngine.cs` — add `ExecuteAlgorithm(...)`, projecting `YIELD`/`RETURN` variables (e.g. `node`, `score`, `community`).

Note: algorithm output is per-node and composes poorly with graph-variable `RETURN` (same limitation as aggregates); the typed `client.Algorithm` API is the primary surface, CALL is for query composition and write-back convenience.

---

## 6. Workstream B — REST server (`src/LiteGraph.Server`)

### 6.1 Routes
Add to `API/REST/RestServiceHandler.cs` `InitializeRoutes()`, following the `GraphQueryRoute` template (line ~402) and the existing `.../routes` traversal route (line ~484):

```
POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/algorithms/{algorithm}
POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/algorithms        (body carries AlgorithmType)
GET  /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/algorithms         (list supported algorithms + params)
```

- Handler `AlgorithmRoute(HttpContextBase ctx)`: pull `RequestContext`, guard empty body, deserialize `GraphAlgorithmRequest`, start OTel `Activity`, create timeout CTS, authorize, call `_LiteGraph.Algorithm.Run(...)`, serialize `GraphAlgorithmResult`.
- Provider-agnostic logic (if any) in `API/Agnostic/ServiceHandler.cs`; DTOs in `Classes/`.

### 6.2 Authorization
- Add `AuthorizationResourceTypeEnum.Algorithm`.
- Compute-only requests require **read** scope; requests with `WriteBack=true` require **write** scope (they mutate node `Data`). Map in `AuthorizationService` scope mapping, consistent with `docs/RBAC.md` §"Operation Scope Mapping".

### 6.3 Settings & observability
- Extend server settings with `GraphAlgorithmConfiguration` (max node/edge ceiling, default timeout).
- Add Prometheus counters/histograms (`litegraph_algorithm_runs_total`, duration histogram by algorithm) and OTel spans, consistent with `docs/OBSERVABILITY.md`. Add a Grafana panel to the shipped dashboards.

---

## 6A. Workstream B2 — Graph export / projection for external compute (`rustworkx` / NetworkX interop)

This is a **shipped, first-class feature**, not a documentation note. It exists so any algorithm beyond native scope — or any graph past the in-memory ceiling — can be computed externally (in `rustworkx`, NetworkX, igraph, or anything that reads a standard graph format) and the results written back into LiteGraph. It is accessible via **API, dashboard, MCP, and all SDKs**.

### 6A.1 Export formats
Reuse the existing streaming primitives — `Node.ReadAllInGraph` / `Edge.ReadAllInGraph` for whole-graph, and `SubgraphExtractor.ExtractAsRecords` (already streams JSONL) for a bounded region — to emit any of:

- **Node-link JSON** (default) — the shape NetworkX consumes natively via `networkx.node_link_graph(...)`. `{ "directed": true, "multigraph": true, "nodes": [{ "id": "<guid>", ...selected attributes }], "links": [{ "source": "<guid>", "target": "<guid>", "weight": <cost>, ...}] }`.
- **Edge list (CSV/JSONL)** — `source, target, weight` rows; the simplest and most memory-frugal feed for `rustworkx` (which builds from edge lists / integer node indices). A companion node-attribute stream carries labels/tags/data when needed.
- **GraphML** — portable XML for igraph/Gephi/yEd and NetworkX `read_graphml`.

Design points:
- **Streaming** (JSONL/CSV/GraphML written incrementally) so export does not itself hit the in-memory ceiling — this is how a graph *too large for native compute* still gets projected out.
- **Stable node identity**: export includes the LiteGraph node GUID as the node id so results import back unambiguously. Optionally also emits a dense 0..N-1 integer index alongside the GUID for `rustworkx` (which is index-oriented), plus the index→GUID mapping.
- **Attribute selection**: caller chooses which node/edge fields to include (none / labels+tags / full `Data`) to control payload size.
- **Direction & weight**: directed by default; edge `cost` maps to `weight`.
- Implemented in `src/LiteGraph/Algorithms/GraphProjectionExporter.cs` (or `Subgraph/`), one exporter per format behind a common interface, with a `GraphExportRequest` (format, scope=whole-graph|subgraph, attribute level, filters) and streaming output.

### 6A.2 Results import (the return trip)
- `GraphAlgorithmImportRequest` — a map of node GUID → computed value(s) (e.g. `{ "<guid>": { "pagerank": 0.031, "community": 4 } }`), plus the target property name(s).
- Writes values into node `Data` (or `Tags`) via the existing `Batch`/`CreateMany`/update path — the same write-back mechanism native algorithms use — so externally computed results become **DSL-queryable** (`WHERE n.data.pagerank > 0.1`) with no query-engine changes.
- Requires **write** scope (it mutates nodes); compute-only export requires **read** scope.

### 6A.3 REST routes (`src/LiteGraph.Server`)
```
GET  /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/export?format=node-link|edge-list|graphml&attributes=none|meta|full   (streaming download)
POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/export        (body = GraphExportRequest, for subgraph/filtered export)
POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/algorithms/import   (GraphAlgorithmImportRequest → write-back)
```
Handlers follow the existing streaming-export and `GraphQueryRoute` templates; export is `Content-Disposition: attachment` with the right content type per format. Authorization: export = read scope, import = write scope (new mapping in `AuthorizationService`).

### 6A.4 Client surface
Add to `client.Algorithm` (or a sibling `client.Export`): `ExportGraph(request, stream, token)` and `ImportAlgorithmResults(request, token)`.

### 6A.5 SDKs
All three SDKs (§9) expose `exportGraph(...)` (streaming to a file/stream) and `importAlgorithmResults(...)`.

### 6A.6 Dashboard
On the algorithms page (§8): an **"Export for external compute"** control — pick format + attribute level + whole-graph/subgraph, download the file — and an **"Import results"** control — upload a results file (GUID→value map / JSONL) and write back, gated on write permission.

### 6A.7 MCP
Add `algorithm/export` and `algorithm/import` tools in `AlgorithmRegistrations.cs`, proxying the REST routes under caller RBAC.

### 6A.8 Documentation
`docs/ALGORITHMS.md` includes a complete **round-trip worked example**: export a graph as node-link JSON → load in `rustworkx`/NetworkX → run (e.g.) betweenness or Leiden → post results back via the import route → query them in the DSL. Include copy-pasteable Python. Cross-link from `README.md` and `docs/REST_API.md`/`docs/MCP_API.md`.

---

## 7. Workstream C — MCP server (`src/LiteGraph.McpServer`)

MCP tools proxy REST (per `docs/CLAUDE_MCP.md` / `MCP_API.md`). Follow the per-entity registration pattern:

- **New `Registrations/AlgorithmRegistrations.cs`** with `RegisterHttpTools` / `RegisterTcpTools` / `RegisterWebSocketTools` (match the transports used by `GraphRegistrations.cs`), registering tools:
  - `algorithm/pagerank`, `algorithm/degree`, `algorithm/communities`, `algorithm/components`, `algorithm/centrality`, and a generic `algorithm/run` (algorithm named in args).
  - `algorithm/export` (project the graph out for external `rustworkx`/NetworkX compute, §6A) and `algorithm/import` (write externally computed results back).
  - Each tool schema declares `tenantGuid`, `graphGuid`, algorithm params, and optional `writeBack`; the handler calls the REST proxy (`LiteGraphMcpRestProxy.cs`) so caller RBAC is enforced at the REST boundary.
- Register the new group in `LiteGraphMcpServer.cs` alongside the existing `GraphRegistrations.RegisterHttpTools(...)` calls.
- Add the tools to the MCP catalog/count referenced in docs and the `Chat` in-process tool loop only if they should be agent-callable in chat (decide per phase; default: expose as MCP tools, add to chat read-tool catalog in Phase 3).

---

## 8. Workstream D — Dashboard (`dashboard/`)

Next.js app; pages under `dashboard/src/page/*`, SDK bindings in `dashboard/src/lib/sdk`, i18n in `dashboard/messages/`.

- **New page `dashboard/src/page/algorithms/`** — select a graph, choose an algorithm, set parameters, run, and view results (sortable table of node → score/community; optional "write back to node data" toggle gated on write permission).
- **Visualization**: render community assignments and centrality scores over the existing graph view (color/size by score). Reuse existing node/edge selector components.
- **SDK binding**: add algorithm calls to `dashboard/src/lib/sdk` (wrapping the JS SDK below).
- **Navigation & RBAC**: add a menu item (`components/menu-item`), mirror the server permission matrix so the run/write-back controls disable when the user lacks scope (`lib/authz`).
- **i18n**: add strings to all locale files under `dashboard/messages/`.
- **Dashboard changelog**: update `dashboard/CHANGELOG.md`.

---

## 9. Workstream E — SDKs

All three SDKs ship at **9.0.0** (see Phase 0 §4.2) and expose the same surface: algorithm runs, **graph export** (`exportGraph`, streaming to a file/stream), and **results import** (`importAlgorithmResults`).

### 9.1 C# SDK (`sdk/csharp/src/LiteGraph.Sdk`)
- Add `Algorithm` methods to `LiteGraphSdk` calling the new REST routes; add `ExportGraph(...)` (streaming) and `ImportAlgorithmResults(...)`; add request/response model classes mirroring the core models.

### 9.2 JavaScript SDK (`sdk/js`, npm `litegraphdb`)
- Add algorithm, export, and import methods + TypeScript types; version 9.0.0; add unit tests.

### 9.3 Python SDK (`sdk/python`, `litegraph_sdk`)
- Add algorithm, export, and import client methods + types; version 9.0.0. The export method returning **node-link JSON that loads directly into `rustworkx`/NetworkX** is the primary interop path — include a docstring example that hands the export straight to `networkx.node_link_graph(...)`.

---

## 10. Workstream F — Tests

Match the existing multi-framework test layout.

### 10.1 Core algorithm correctness (`src/Test.Nunit`, `src/Test.Xunit`)
- **Known-answer tests** against textbook fixtures: PageRank on a small directed graph vs. hand-computed values (tolerance-based); degree centrality on a star/ring; SCC/WCC on graphs with known component structure; label propagation / Louvain on graphs with obvious community structure (e.g. two cliques joined by one edge); closeness/betweenness on path and star graphs with closed-form answers.
- **Edge cases**: empty graph, single node, disconnected graph, self-loops, parallel edges, dangling nodes (PageRank), directed vs. undirected handling.
- **Determinism**: seeded runs produce stable results (embeddings/label-propagation tie-breaking).
- **Ceiling guardrail**: graph exceeding the configured node/edge limit returns the defined error, not OOM.
- **Cancellation**: long run cancels promptly via `CancellationToken`.

### 10.2 Persistence parity (`src/Test.Automated`)
- Run each algorithm against both **SQLite** and **PostgreSQL** repositories and assert identical results (the compute core is storage-agnostic, but enumeration parity must be proven).
- Write-back: results correctly materialized into node `Data`, then queryable via DSL (`WHERE n.data.pagerank > x`).

### 10.3 REST / server (`src/Test.Automated`, SDK tests)
- Route happy-path + validation (unknown algorithm, missing graph, bad params).
- **Authorization**: read-scope credential can compute but cannot write back (403/authorization error); write-scope can. Add to the RBAC enforcement suite.
- Enumeration/envelope conventions where list responses apply (the `ZeroGetAllGuard` sweep must stay green — algorithm list route must conform or be justified).

### 10.3a Export / interop round-trip (`src/Test.Automated`, SDK tests)
- **Format validity**: each export format is well-formed — node-link JSON and GraphML load without error, edge-list rows parse. Where feasible in CI, a Python check loads the node-link export via `networkx.node_link_graph` (and/or builds a `rustworkx` graph from the edge list) to prove real-world consumability.
- **Round-trip fidelity**: export → (compute a value keyed by GUID externally / simulated) → import → the values land on the correct nodes and are queryable via DSL (`WHERE n.data.pagerank > x`). Node/edge counts and GUID identity are preserved end to end.
- **Streaming past the ceiling**: a graph larger than the native in-memory limit still exports successfully (streaming), proving the projection path is the escape hatch for oversized graphs.
- **Attribute-level selection** (`none`/`meta`/`full`) produces the expected payloads; **authorization**: export = read scope, import = write scope.

### 10.4 MCP (`src/Test.Automated`)
- Each algorithm tool invocable over the transports; proxies REST and enforces RBAC; error surfaces cleanly.

### 10.5 Dashboard (`dashboard`, Jest)
- Component/page tests for the algorithms page: renders, submits, disables write-back without permission, displays results.

### 10.6 Performance / scale (`src/Test.PerformanceAndScalability`, `LoadGenerator`)
- Add algorithm profiles to the harness; capture p50/p95/p99 and memory by graph size for PageRank/degree/components/label-propagation. **Publish representative numbers** — this simultaneously chips at CKG gap #1 (unproven scale) and validates the in-memory ceiling.

### 10.7 SDK sample database
- Extend `src/LiteGraph.SampleDatabase` with a graph that has clear community/centrality structure for demos and manual verification.

---

## 11. Workstream G — Documentation

- **New `docs/ALGORITHMS.md`** — the authoritative reference: supported algorithms, parameters, result shapes, complexity, in-memory ceiling and guardrails, write-back semantics, the `CALL litegraph.algo.*` DSL surface, the **export/projection formats (§6A)**, and a full **`rustworkx`/NetworkX round-trip worked example** (export → external compute → import → DSL query) with copy-pasteable Python.
- **`docs/REST_API.md`** — document the new `/algorithms` routes **and the `/export` + `/algorithms/import` routes** (request/response, formats, scopes, errors, streaming/content-type).
- **`docs/MCP_API.md`** — document the new `algorithm/*` tools **including `algorithm/export` and `algorithm/import`** and update the tool count/catalog.
- **`docs/DSL.md`** — add the `CALL litegraph.algo.*` section (Phase 3).
- **`docs/RBAC.md`** — add `Algorithm` resource type and read/write scope mapping.
- **`docs/OBSERVABILITY.md`** — new metrics and the Grafana panel.
- **`README.md`** — add graph algorithms to the feature list; reconcile to v9.0.0 (CKG gap #5).
- **`CHANGELOG.md`** (root) and **`dashboard/CHANGELOG.md`** — finalize the v9.0.0 entries.
- **`docs/TEST_COVERAGE.md`** — record the new algorithm coverage.
- **`docs/UPGRADE.md`** — v8.1 → v9.0 upgrade notes (additive; no storage migration expected — confirm).
- **`LiteGraph.postman_collection.json`** — add the new routes.

---

## 12. Milestones

| Milestone | Contents | Storage changes |
|---|---|---|
| **M0** | Phase 0: `V9.0` branch, all versions → 9.0.0, changelog seeds, README reconcile | None |
| **M1** | Core `LiteGraph.Algorithms` + `client.Algorithm`; degree, PageRank, WCC/SCC, label propagation; correctness + parity tests | None |
| **M2** | REST routes + authorization + write-back; **graph export/projection + results import (§6A) across API/dashboard/MCP/SDKs**; C#/JS/Python SDK bindings; closeness/betweenness/Louvain; dashboard page (basic) | None |
| **M3** | MCP `algorithm/*` tools; `CALL litegraph.algo.*` DSL; optional result cache manager; eigenvector/triangles/k-core; dashboard visualization | Parser/Executor; optional manager on repo |
| **M4** | FastRP/node2vec embeddings → node vectors indexed by HNSW; perf/scale profiles + published numbers | None |
| **M5** | Docs finalized, Postman, upgrade notes, full test sweep green, release | — |

---

## 13. Risks and mitigations

- **In-memory ceiling on large graphs.** Mitigation: hard, configurable node/edge limit with a clear error; publish measured memory/latency (M4); document the `rustworkx` projection fallback.
- **Betweenness/Louvain cost and correctness.** Mitigation: known-answer tests; sampled betweenness variant; defer to M2/M3 so M1 ships value early.
- **Enumeration parity (SQLite vs. PostgreSQL).** Mitigation: dedicated parity tests (§10.2).
- **RBAC on write-back.** Mitigation: write-back requires write scope; explicit authorization tests (§10.3).
- **Scope creep from embeddings.** Mitigation: embeddings isolated to M4 and reuse existing vector infrastructure; can slip without blocking the core value.
- **DSL composition limits.** Mitigation: typed API is primary; CALL documented with its per-node/aggregate composition caveat.

---

## 14. Acceptance criteria

1. `git` branch `V9.0`; every version string reads `9.0.0`; solution and dashboard build clean (no errors/warnings, per `CLAUDE.md`).
2. All Phase-scoped algorithms produce known-answer-correct results with identical output on SQLite and PostgreSQL.
3. REST, MCP, and all three SDKs (C#, JS, Python — all at 9.0.0) expose the algorithms; RBAC enforced (write-back gated on write scope).
4. Dashboard can run algorithms, display results, export a graph for external compute, import results back, and respect permissions.
5. Write-back results — from native algorithms **and** from imported external results — are queryable through the existing DSL.
6. **Export/import round-trip works end to end**: a graph exports in node-link JSON that loads directly into `rustworkx`/NetworkX, and computed values import back onto the correct nodes by GUID. A graph larger than the native ceiling still exports via streaming.
7. Documentation complete across `ALGORITHMS.md`, `REST_API.md`, `MCP_API.md`, `DSL.md`, `RBAC.md`, `OBSERVABILITY.md`, `README.md`, and both changelogs.
8. Perf/scale profiles exist and representative numbers are published.
9. Full test suite (NUnit, xUnit, Automated, SDK, dashboard Jest) green.
