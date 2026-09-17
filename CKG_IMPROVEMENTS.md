# LiteGraph CKG Improvements

**Source:** Derived from `CKG.md` (CKG Backend Comparison: NetworkX vs. Neo4j vs. LiteGraph, dated 2026-09-09).

This document enumerates the LiteGraph gaps identified in `CKG.md`, scored for prioritization, followed by the supporting evidence quoted from that comparison for each item.

**Scoring key:**
- **Legitimacy (1-10)** — how real and valid the gap is as stated.
- **Value if fixed (1-10)** — impact for the Cognitive Knowledge Graph (CKG) use case if the gap is closed.
- **Simplicity (1-10)** — ease of implementation (10 = trivial, 1 = very hard).
- **Score** — sum of Legitimacy + Value + Simplicity.

The scores are prioritization judgment calls and are **not** drawn from `CKG.md` itself; the supporting evidence that follows the table **is** drawn directly from `CKG.md`.

> **Note:** An earlier draft included a "empty-scope fail-open" item (empty `Scopes`/`GraphGUIDs` meaning unrestricted, not restricted). It has been removed as not a gap: it is a deliberate, documented permissive default on the *graph* (soft) partition, sitting behind the *tenant* boundary, which fails closed (a non-admin request to another tenant is stopped before scope evaluation). Per-domain isolation is achieved with a tenant per domain; graph-level restriction remains available as opt-in for anyone who wants it. Flipping the default would break the legitimate tenant-as-boundary deployments, which is the signal that this is a design choice rather than a defect.

> **Note:** An earlier draft included a "SQLite→Postgres migration offline only" item. It has been removed as not a gap: the v9.0 graph export/projection endpoint is a streaming, online read (no write-stop required), and its companion import path writes data into the destination. Graphs can therefore be moved between backends while the source stays live, so "migration requires stopping writes" no longer holds as a categorical limitation.

---

## v9.0.0 Re-assessment (2026-09-16)

LiteGraph v9.0.0 shipped since this document was written. Re-scoring against what actually landed:

**Fixed**
- **#2 — No graph algorithms (was the top gap, score 23).** Closed. v9.0 adds eleven native algorithms — degree/closeness/eigenvector/betweenness centrality, PageRank, weakly/strongly connected components, label-propagation and Louvain community detection, clustering coefficient, and k-core — with optional write-back into node data (DSL-queryable), an opt-in result cache, an `Algorithm` authorization resource type, and full REST/MCP/DSL (`CALL litegraph.algo.*`)/dashboard/SDK coverage. Crucially, CKG.md's own recommended mitigation — *project the subgraph to `rustworkx` and write results back* — is now a **built-in feature**: streaming projection export (node-link JSON, edge list, GraphML) plus a results-import path, so algorithms beyond native scope (or graphs past the in-memory ceiling) round-trip through external engines without custom glue. This was "the single strongest argument against LiteGraph as a complete CKG solution"; it no longer applies.
- **#5 — README/site version drift.** Closed. README, Docker image tags, and the changelog all read `v9.0.0`.
- **#6 — Scan-bounded `ORDER BY`/aggregates (score 20).** Closed. Aggregates (`COUNT`/`SUM`/`AVG`/`MIN`/`MAX`) and `ORDER BY` previously operated only over the first `MaxResults` rows in storage order, so `COUNT(*)` under-counted and `ORDER BY … LIMIT k` could miss the true global top-k — the exact "global reasoning" correctness trap CKG.md flagged. They now evaluate over the **whole matching set**: `COUNT(*)` returns the real count and `ORDER BY … LIMIT k` returns the genuine global top-k. A new `MaxScanRows` request bound (default 1,000,000, 0 = unlimited) caps the matching set a global op examines and **rejects with a 400 rather than silently truncating** into a wrong result; ordinary reads stay page-bounded by `MaxResults`/`LIMIT`. Validated by a dual-storage (SQLite + PostgreSQL) Touchstone case covering whole-set aggregates, global top-N/bottom-N ordering, page-bounded ordinary reads, and ceiling rejection (positive and negative).
- **#7 — Keyword-match authz fallback (score 20).** Closed. Native-query scope (read vs. write) is now classified authoritatively from the parsed AST; the substring keyword fallback on `CREATE`/`MERGE`/`SET`/`DELETE`/`REMOVE` is gone. A query that fails to parse during scope classification is rejected with a `400` **before authorization** rather than guessed. Because the execution engine re-parses with the same parser, failing closed loses no valid query while removing keyword matching as a mutation-boundary decision. Validated by a classifier unit test (unparseable queries throw, not keyword-guess) and an API-level Touchstone case (read `200`, denied mutation `401`, unparseable `400`).
- **#9 — HNSW rebuild-after-restore footgun (score 20).** Closed. The gap was explicitly a *runbook* gap — `Admin.Backup` (`VACUUM INTO`) and provider migration copy the database and raw vectors but not the derived file-backed HNSW index, so a restore silently degrades indexed search (brute-force fallback, or stale results) until the index is rebuilt. The `RebuildVectorIndex` capability already existed across REST/SDK/MCP; v9.0 adds the missing **Backup, Restore, and Disaster Recovery Runbook** ([docs/STORAGE.md](docs/STORAGE.md)) that makes the per-graph rebuild an explicit, timed post-restore step, documents the silent-degradation failure mode, and directs operators to measure and budget the rebuild time into their RTO — which is exactly what CKG.md §3.11 asked for ("belongs in the DR runbook with a measured rebuild time").
- **#3 — Audit records denials only (score 23).** Closed. The `authorizationaudit` store now records **successful privileged actions** — any REST request that required `write` or `admin` scope and was authorized is written in PostRouting with `AuthorizationResult=Permitted` and the request's actual response status code, alongside the existing denial records. Read-scope requests are never audited, so the store answers "who changed what, when" without being flooded by routine reads. A new `AuthorizationAudit` settings block (`Enable`, `AuditSuccessfulActions`) lets operators disable auditing or revert to denials-only, and a dual-storage (SQLite + PostgreSQL) Touchstone case pins the positive (permitted write audited), negative (read not audited), and denial behaviors. This directly satisfies CKG.md §5 Option A's "extend audit to successful privileged actions" prerequisite.

**Improved**
- **#17 — Query language doesn't generate embeddings.** Largely addressed. The query language still takes *supplied* embeddings for search, but v9.0 adds server-side **node embedding generation** (`POST .../algorithms/embeddings`) using the tenant's active embedding endpoint, storing each as an HNSW-indexable node vector. The generate → store → search loop is now closable through the API without external code (verified end-to-end against a live Ollama endpoint). The residual — embedding generation is not literally inside the `CALL` syntax — is cosmetic.

**Still open — and now the top of the list (unchanged by v9.0):**
- Enterprise/governance: **#8 no SSO/OIDC**, **#13 authz granularity is graph-level only**.

> **Note:** An earlier draft included "no encryption at rest." It has been removed as not a product gap: at-rest encryption is a deployment/infrastructure concern the operator already controls — an encrypted filesystem/volume (LUKS, dm-crypt, BitLocker, cloud-provider disk encryption) or PostgreSQL's own transparent data encryption covers the SQLite file and the Postgres data directory with no application involvement. Building a second, in-product encryption layer on top would duplicate a solved OS/storage capability. LiteGraph ships as a container/binary over storage the operator provisions, so this is theirs to enable, not the product's to reimplement.
- Reasoning correctness/shape: **#12 no cross-graph queries**, **#11 32-hop cap**, **#14 no multi-`MATCH` chaining**.
- Data lifecycle/ops: **#1 no published scale evidence**, **#16 HA delegated to Postgres**, **#18 Python/JS are REST-only**.

> **Note:** An earlier draft included "no provenance/temporality" (no versioned nodes/edges or point-in-time reconstruction). It has been removed as not a product gap: LiteGraph stores current graph state, and *when and how the graph changes* is the application owner's control surface — a graph database is not object storage with built-in versioning. Owners that need history model it deliberately at the app layer (an append-only event log, validity intervals on edges, or versioned records), which is the right place for it because only the app knows which changes are semantically meaningful. Building implicit, universal versioning into the store would impose storage growth and write-path cost on every deployment for a policy that belongs to the owner. Request history and per-turn chat records remain available for partial forensic coverage.

> **Note:** An earlier draft included "embedded mode has no authz." It has been removed as not a product gap: embedded mode runs the core `LiteGraphClient`/repository in-process inside a host application, and in-process code is trusted by definition — authentication and authorization are the responsibility of the application into which LiteGraph is embedded, exactly as they are for any in-process library (an ORM, a cache, an embedded database). The REST/MCP server is the surface that offers enforced RBAC; embedding is the deliberate trade that swaps that boundary for in-process latency. Adding an authorization layer to the embedded API would impose a policy model on every host for a decision that belongs to the host, and callers that want enforced RBAC already have it — run the server. The design property (embedded callers are trusted) remains documented as a caveat.

**New nuances v9.0 introduced (not new gaps, but they change the weighting):**
- Algorithms compute over a **whole-graph in-memory adjacency** with a configurable node/edge ceiling. That raises the stakes on **#1 (scale evidence)** — it should now quantify both storage scale *and* the algorithm compute ceiling — while the projection-export path is the honest escape hatch above the ceiling.
- Embedding generation creates many more HNSW-indexed vectors, which raised the stakes on index rebuild after restore — now addressed by the DR runbook (**#9**, closed). (This originally also sharpened **#3 (audit)** because algorithm write-back and embedding generation were unaudited successful mutations — now closed: those privileged REST actions produce `Permitted` audit records.)

**Overall.** The decisive functional gap is gone: LiteGraph is now the only one of the three candidates that ships the hybrid-retrieval *and* the cognition (algorithms) layer, with a first-class external-compute escape hatch. Audit completeness — the first of the enterprise-review items — is now closed (#3). What remains is the rest of the **enterprise-review surface** CKG.md §3.9/§5 flagged — identity federation, authz depth — plus **published scale evidence (#1)**, still the single highest-scored open item. None of these are architecturally hard; all are still open. For an *agent-facing, retrieval-and-cognition-dominant* CKG, LiteGraph's case is materially stronger post-v9.0; for a deployment gated on node-level authorization, SSO, or demonstrated scale, the open items above are still the deciders. (Encryption at rest and native provenance/versioning, previously listed here, are treated as operator/deployment responsibilities rather than product gaps — see the notes above.)

---

## Priority Table (ordered by Score, descending)

Status column added in the v9.0.0 re-assessment above. Scores are the *original* pre-v9.0 prioritization (kept for continuity); the Status reflects what v9.0.0 delivered. The **Priority** column is the forward-looking recommendation for what to do next among the *open* items (fixed/improved items are **Done**): **P1** do next, **P2** near-term, **P3** later/roadmap, **Defer** deliberately low (e.g., a deliberate design limit).

| # | Gap (§) | Brief description | Legit. | Value | Simpl. | Score | Status (post-v9.0) | Priority |
|---|---------|-------------------|:---:|:---:|:---:|:---:|---|:---:|
| 1 | Unpublished scale evidence (§3.7) | Benchmark *harness* exists but no node/edge counts, latency, throughput, or hardware ever published | 9 | 8 | 8 | 25 | **Open** (now also covers algorithm compute ceiling) | **P1** |
| 2 | No graph algorithms (§3.2) | No centrality, community detection, PageRank, or embeddings — the "cognition" layer is absent | 10 | 9 | 4 | 23 | ✅ **Fixed (v9.0)** | Done |
| 3 | Audit records denials only (§3.9) | Successful privileged actions leave no audit trail — fails the "who changed what, when" governance question | 8 | 8 | 7 | 23 | ✅ **Fixed (v9.0)** | Done |
| 5 | README/site version drift (intro) | README on `main` documents v7.0.0 while site documents v8.1 — misleads external evaluators | 8 | 5 | 9 | 22 | ✅ **Fixed (v9.0)** | Done |
| 6 | Scan-bounded `ORDER BY`/aggregates (§3.3) | `COUNT(*)`/`ORDER BY` operate up to `MaxResults`, not the whole graph — a correctness trap for global reasoning | 8 | 7 | 5 | 20 | ✅ **Fixed (v9.0)** | Done |
| 7 | Keyword-match authz fallback (§3.9) | Query authorization falls back to keyword matching (`CREATE`/`SET`/…) when parsing fails — weak mutation boundary | 7 | 6 | 7 | 20 | ✅ **Fixed (v9.0)** | Done |
| 8 | No SSO/OIDC/SAML (§3.9) | No enterprise identity federation (out of scope today) | 8 | 7 | 5 | 20 | **Open** | **P2** |
| 9 | HNSW rebuild-after-restore footgun (§3.11) | Vector index files are derived artifacts needing rebuild after restore/migration; not in a DR runbook | 7 | 5 | 8 | 20 | ✅ **Fixed (v9.0)** — DR runbook added | Done |
| 11 | 32-hop traversal cap (§3.3) | Bounded traversal only; no unbounded variable-length paths (rejected by parser) | 7 | 6 | 6 | 19 | **Open** | Defer |
| 12 | No cross-graph/cross-tenant queries (§3.3) | Partitioned graphs can't be spanned by one query; federation must live in app code (one-way door) | 8 | 7 | 4 | 19 | **Open** (algorithms are also single-graph) | **P3** |
| 13 | Authz granularity graph-level only (§3.9) | RBAC only at tenant/graph level, enforced at REST/MCP boundary — no node/edge/property-level control | 8 | 7 | 4 | 19 | **Open** (new `Algorithm` resource type added, still graph-level) | **P2** |
| 14 | No multi-`MATCH` query chaining (§3.3) | Query chaining not yet supported | 7 | 6 | 5 | 18 | **Open** | **P3** |
| 16 | HA delegated to PostgreSQL (§3.11) | No native failover orchestration; relies on Postgres HA + process supervisor | 5 | 6 | 5 | 16 | **Open** | **P3** |
| 17 | Query language doesn't generate embeddings (§3.4) | Vector search takes *supplied* embeddings only; generation lives at chat/RAG/app layer | 5 | 4 | 6 | 15 | 🟡 **Improved (v9.0)** — server-side node-embedding generation added | Done |
| 18 | No embedded access for Python/JS (§3.8) | Python/JS SDKs are REST clients; embedded in-process path is .NET-only | 6 | 5 | 3 | 14 | **Open** (v9.0 added algorithm methods, still REST clients) | Defer |

---

## Supporting Evidence

### 1. Unpublished scale evidence (§3.7) — Score 25

> **LiteGraph** — **Unknown.** `PERF_SCALE_TESTING.md` documents a capable benchmark *harness* — profiles, topologies, workload families, concurrency ramps, regression thresholds — but publishes **no results**. No node/edge counts, no latency figures, no throughput numbers, no hardware.

> This must be stated plainly: **LiteGraph currently has no published scale evidence.** The tooling to produce it exists; the numbers do not. Any architecture review will ask, and "we have a harness" is not an answer.

> *Actionable:* running the `large` and `soak` profiles against PostgreSQL and publishing the results — node/edge counts, p50/p95/p99 by workload, hardware — would convert the single largest open risk into a known quantity. This is probably the highest-leverage thing available to strengthen LiteGraph's position in any comparison.

The summary matrix also rates LiteGraph scale as **"Unproven."**

---

### 2. No graph algorithms (§3.2) — Score 23

> **LiteGraph** — **None.** The query language provides `MATCH`, bounded variable-length paths, `MATCH SHORTEST`, and aggregates. There is no centrality, no community detection, no PageRank, no graph embedding.

> For a system whose stated purpose is *cognition* — spreading activation, salience, clustering of related concepts, identification of central knowledge — this is a material gap, not a nice-to-have. It is the single strongest argument against LiteGraph as a complete CKG solution.

> **Mitigation:** project the relevant subgraph out of LiteGraph into `rustworkx` for algorithmic passes and write results back as node/edge properties. This works, is a common pattern, and is not unreasonable — but it should be designed deliberately and costed, not discovered later.

The summary matrix rates **Graph algorithms** as **Absent** for LiteGraph.

---

### 3. Audit records denials only (§3.9) — Score 23 — ✅ Fixed (v9.0)

> Add: no encryption at rest, no enterprise identity federation, and audit that records only denials — meaning **successful privileged actions leave no audit record**. For a CKG where "who changed what the system believes, and when" is the core governance question, that is a significant gap.

The security table lists Audit for LiteGraph as **"Denials only,"** and the summary matrix rates Audit trail as **Weak (denials only)**. The Option A prerequisites (§5) include:

> extend audit to successful privileged actions

**Resolution (v9.0).** The `authorizationaudit` store now records permitted `write`/`admin` REST actions in addition to denials. Each successful privileged request is written in PostRouting with `AuthorizationResult=Permitted` and its actual response status code; read-scope requests are never audited. A new `AuthorizationAudit` settings block (`Enable`, `AuditSuccessfulActions`; both default `true`) governs the behavior and is reported as restart-required by the settings API. A dual-storage (SQLite + PostgreSQL) Touchstone case — *"Permitted write/admin actions are audited; reads are not; denials remain audited"* — validates the positive, negative, and denial paths. See [docs/RBAC.md](docs/RBAC.md#audit) and [docs/SETTINGS.md](docs/SETTINGS.md). This satisfies the Option A prerequisite above.

---

### 4. No encryption at rest (§3.9) — Removed (not a product gap)

CKG.md factually notes LiteGraph has no *application-level* at-rest encryption. This has been removed from the gap list rather than tracked as work: at-rest encryption is a deployment/infrastructure responsibility that the operator already owns. An encrypted filesystem or volume (LUKS, dm-crypt, BitLocker, cloud-provider disk encryption) or PostgreSQL transparent data encryption protects the SQLite file and the Postgres data directory transparently, with no application involvement. A second in-product encryption layer would duplicate a solved OS/storage capability. See the note at the top of this document.

---

### 5. README/site version drift (intro) — Score 22

From the introductory **Documentation note:**

> litegraphdb.com documents v8.1; the repository README on `main` still documents v7.0.0. Worth reconciling before anyone external evaluates on the strength of the repo alone.

---

### 6. Scan-bounded `ORDER BY`/aggregates (§3.3) — Score 20 — ✅ Fixed (v9.0)

From the traversal/query table, LiteGraph rows:

> Aggregates — Partial — cannot mix with graph-variable returns; scan-bounded by `LIMIT`/`MaxResults`
> `ORDER BY` semantics — **Scans up to `MaxResults`, then sorts** — not a global ordering

> **Scan-bounded `ORDER BY` and aggregates.** `RETURN COUNT(*)` over a large graph counts up to the limit, not the graph. For a CKG that reasons over global structure, this is a semantic trap — correct results require understanding the bound.

**Resolution (v9.0).** Aggregates and `ORDER BY` are now global. `COUNT`/`SUM`/`AVG`/`MIN`/`MAX` are computed over the entire matching set (the aggregate match collectors no longer stop at the return page), so `COUNT(*)` returns the true count; `ORDER BY … LIMIT k` scans the whole matching set, sorts it, and returns the genuine global top-k rather than the top-k of the first page in storage order. A new `MaxScanRows` query-request bound (default 1,000,000, 0 = unlimited) caps the matching set a global operation will examine and — critically — **rejects an overflow with a 400 rather than silently truncating** it into a wrong answer, so the result is either correct or an explicit error, never quietly partial. The mixed-aggregate-with-graph-variable restriction is unchanged (a separate design limitation, not this correctness trap). Ordinary (non-global) reads are unaffected and remain bounded by `MaxResults`/`LIMIT`. See [docs/DSL.md](docs/DSL.md#ordering-and-limit). Validated by the dual-storage `Query.GlobalScanBounded.*` Touchstone case.

---

### 7. Keyword-match authz fallback (§3.9) — Score 20 — ✅ Fixed (v9.0)

> **Query authorization falls back to keyword matching** (`CREATE`/`MERGE`/`SET`/`DELETE`/`REMOVE`) when parsing fails. A parse failure that reaches a fallback string match is a weak last line of defense for a mutation boundary.

**Resolution (v9.0).** `GraphQueryRequiredScope` now classifies scope solely from the parsed AST kind; the keyword-matching fallback (token scan plus a raw `IndexOf` substring check) has been deleted. When the parser rejects a query during scope classification, the query route returns a `400 Bad Request` **before** authorization rather than guessing a scope. This is safe to fail closed because the execution engine re-parses the query with the same parser — any query the classifier rejects would also fail at execution, so no legitimate request is lost, and a mutation boundary no longer rests on substring matching. Verified by a classifier unit test (valid queries scope correctly; unparseable/whitespace/keyword-substring queries throw instead of being keyword-classified) and an API-level Touchstone case (`Authorization.QueryScopeFailsClosed`: valid read `200`, valid mutation denied for a read-only credential `401`, unparseable query `400` for both admin and read-only). See [docs/RBAC.md](docs/RBAC.md#query-scope-mapping).

---

### 8. No SSO/OIDC/SAML (§3.9) — Score 20

The security table lists **SSO / OIDC / SAML** for LiteGraph as **"None — explicitly out of scope,"** and the summary matrix rates Enterprise identity (SSO/OIDC) as **Absent**.

> Add: no encryption at rest, no enterprise identity federation, and audit that records only denials …

The Option A prerequisites (§5) include:

> add OIDC or an authenticating reverse proxy

---

### 9. HNSW rebuild-after-restore footgun (§3.11) — Score 20 — ✅ Fixed (v9.0)

> Delegating HA to PostgreSQL is a defensible design — it rides infrastructure most platform teams already run well. But **note the recovery footgun**: HNSW vector index files are derived artifacts that may require rebuilding after restore or migration. That belongs in the DR runbook with a measured rebuild time, or a restore will surprise someone during an incident.

**Resolution (v9.0).** This was a documentation gap, and the fix is documentation. `Admin.Backup` uses SQLite `VACUUM INTO`, which copies only the main database (including raw stored vectors) and not the file-backed HNSW index artifacts; a restore or provider migration therefore leaves indexed search degraded — brute-force fallback, or stale results — until the index is rebuilt. The `RebuildVectorIndex` capability already existed across REST (`POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/vectorindex/rebuild`), the C# SDK, and MCP. v9.0 adds the missing **Backup, Restore, and Disaster Recovery Runbook** ([docs/STORAGE.md](docs/STORAGE.md#backup-restore-and-disaster-recovery-runbook)) that makes the per-graph rebuild an explicit, timed post-restore step, spells out the silent-degradation failure mode, and directs operators to measure the rebuild against production-representative data and budget it into their RTO — precisely the "DR runbook with a measured rebuild time" the evidence asked for.

---

### 10. No provenance/temporality (§3.12) — Removed (owner responsibility)

> None of the three provides native bitemporal modeling, versioned nodes/edges, or point-in-time graph reconstruction. Every option requires modeling this yourself — versioned nodes, validity intervals on edges, or an append-only event log alongside the graph.

> LiteGraph: request history and per-turn chat records give partial forensic coverage; graph object history is not modeled.

CKG.md factually notes LiteGraph stores only current graph state. This has been removed from the gap list rather than tracked as work: *when and how the graph changes* is the application owner's control surface, not the store's. A graph database is not object storage with built-in versioning; owners that need history model it deliberately at the app layer (append-only event log, edge validity intervals, versioned records) — the right place for it, because only the app knows which changes are semantically meaningful, and universal implicit versioning would impose storage-growth and write-path cost on every deployment for a policy that belongs to the owner. Request history and per-turn chat records remain available for partial forensic coverage. See the note at the top of the re-assessment.

---

### 11. 32-hop traversal cap (§3.3) — Score 19

From the traversal/query table, LiteGraph rows:

> Unbounded variable-length paths — **No — rejected by parser**
> Max traversal depth — **32 hops**

The summary matrix rates Deep / unbounded traversal as **Weak (32-hop cap, bounded only)**.

---

### 12. No cross-graph/cross-tenant queries (§3.3) — Score 19

From the traversal/query table, LiteGraph row:

> Cross-partition queries — **No cross-graph or cross-tenant queries**

> **No cross-graph queries.** If the CKG is partitioned into multiple graphs (by domain, tenant, or lifecycle), no single query can span them. Federation must be done in application code. This should drive the graph-partitioning decision early, because it is expensive to reverse.

Reinforced in §5:

> **Decide the graph-partitioning strategy early** if LiteGraph is in play — no cross-graph queries means partitioning is a one-way door.

---

### 13. Authz granularity graph-level only (§3.9) — Score 19

The security table lists Authorization granularity for LiteGraph as **"Tenant and graph level only"** and RBAC enforcement point as **"REST/MCP boundary only."** The summary matrix rates Authorization granularity as **Weak (graph-level; REST boundary only)**.

---

### 14. No multi-`MATCH` query chaining (§3.3) — Score 18

From the traversal/query table, LiteGraph row:

> Query chaining (multi-`MATCH`) — **Not yet supported**

---

### 15. Embedded mode has no authz (§3.9) — Removed (host-application responsibility)

> **The core `LiteGraphClient` and repository APIs are permission-agnostic.** RBAC lives at the REST/MCP boundary. **Any embedded .NET caller therefore has unrestricted access to all tenants and graphs.**

CKG.md factually notes that embedded mode has no authorization model. This has been removed from the gap list rather than tracked as work: embedded mode runs the core client/repository **in-process** inside a host application, and in-process code is trusted by definition — authentication and authorization belong to the application into which LiteGraph is embedded, exactly as they do for any in-process library (an ORM, a cache, an embedded database engine). The REST/MCP server is the surface that offers enforced RBAC; embedding is the deliberate trade that swaps that boundary for in-process latency. A caller that wants enforced RBAC runs the server. Imposing an authorization model on the embedded API would force a policy on every host for a decision that belongs to the host. The design property remains documented as a caveat; it is not a defect to fix.

---

### 16. HA delegated to PostgreSQL (§3.11) — Score 16

> **LiteGraph** — Explicitly does not implement failover orchestration; delegates to PostgreSQL HA plus a process supervisor. Backup via whole-database snapshot or portable per-graph JSONL export.

The summary matrix rates HA / clustering as **Weak (delegated to Postgres)**.

---

### 17. Query language doesn't generate embeddings (§3.4) — Score 15

> **Caveat:** the query language takes *supplied* embeddings — it does not generate them. Embedding generation happens at the chat/RAG layer or in your application. Fine, but it means the query surface alone doesn't close the loop.

---

### 18. No embedded access for Python/JS (§3.8) — Score 14

From the latency table, LiteGraph row:

> Embedded in-process via `LiteGraphClient` — **for .NET consumers only.** Python and JavaScript SDKs are **REST clients.**

> **This matters a great deal for the CKG decision.** If the CKG's consumers are Python — which the NetworkX proposal strongly implies — LiteGraph's embedded advantage does not accrue to them. Python callers pay an HTTP round-trip, in the same order of magnitude as Neo4j's Bolt hop. The embedded-vs-server distinction only pays off if the consuming code is .NET.

The summary matrix rates Python-native access as **Weak (REST SDK only)**.

---

## Consolidated Deployment Prerequisites (from §5, Option A)

For internal deployment with LiteGraph as system of record, `CKG.md` lists these prerequisites, all of which map to items in the table above:

> **Prerequisites before internal deployment:** publish scale results; add OIDC or an authenticating reverse proxy; enable PostgreSQL encryption at rest; extend audit to successful privileged actions; document the embedded-mode authorization caveat.

| Prerequisite | Table item | Status |
|---|---|---|
| Publish scale results | #1 | Open |
| Add OIDC / authenticating reverse proxy | #8 | Open |
| Enable PostgreSQL encryption at rest | — | Operator responsibility (removed as a product gap — encrypt the filesystem/volume or use PostgreSQL TDE) |
| Extend audit to successful privileged actions | #3 | ✅ Fixed (v9.0) |
| Document the embedded-mode authorization caveat | — | Host-application responsibility (removed as a product gap — embedded in-process callers are trusted; run the server for enforced RBAC) |
