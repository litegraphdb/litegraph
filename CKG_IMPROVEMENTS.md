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

---

## v9.0.0 Re-assessment (2026-09-16)

LiteGraph v9.0.0 shipped since this document was written. Re-scoring against what actually landed:

**Fixed**
- **#2 — No graph algorithms (was the top gap, score 23).** Closed. v9.0 adds eleven native algorithms — degree/closeness/eigenvector/betweenness centrality, PageRank, weakly/strongly connected components, label-propagation and Louvain community detection, clustering coefficient, and k-core — with optional write-back into node data (DSL-queryable), an opt-in result cache, an `Algorithm` authorization resource type, and full REST/MCP/DSL (`CALL litegraph.algo.*`)/dashboard/SDK coverage. Crucially, CKG.md's own recommended mitigation — *project the subgraph to `rustworkx` and write results back* — is now a **built-in feature**: streaming projection export (node-link JSON, edge list, GraphML) plus a results-import path, so algorithms beyond native scope (or graphs past the in-memory ceiling) round-trip through external engines without custom glue. This was "the single strongest argument against LiteGraph as a complete CKG solution"; it no longer applies.
- **#5 — README/site version drift.** Closed. README, Docker image tags, and the changelog all read `v9.0.0`.

**Improved**
- **#17 — Query language doesn't generate embeddings.** Largely addressed. The query language still takes *supplied* embeddings for search, but v9.0 adds server-side **node embedding generation** (`POST .../algorithms/embeddings`) using the tenant's active embedding endpoint, storing each as an HNSW-indexable node vector. The generate → store → search loop is now closable through the API without external code (verified end-to-end against a live Ollama endpoint). The residual — embedding generation is not literally inside the `CALL` syntax — is cosmetic.

**Still open — and now the top of the list (unchanged by v9.0):**
- Enterprise/governance: **#3 audit records denials only**, **#4 no encryption at rest**, **#8 no SSO/OIDC**, **#13 authz granularity is graph-level only**, **#15 embedded mode has no authz**, **#7 keyword-match authz fallback**.
- Reasoning correctness/shape: **#6 scan-bounded `ORDER BY`/aggregates**, **#12 no cross-graph queries**, **#11 32-hop cap**, **#14 no multi-`MATCH` chaining**.
- Data lifecycle/ops: **#1 no published scale evidence**, **#10 no provenance/temporality**, **#9 HNSW rebuild-after-restore footgun**, **#16 HA delegated to Postgres**, **#19 offline migration**, **#18 Python/JS are REST-only**.

**New nuances v9.0 introduced (not new gaps, but they change the weighting):**
- Algorithms compute over a **whole-graph in-memory adjacency** with a configurable node/edge ceiling. That raises the stakes on **#1 (scale evidence)** — it should now quantify both storage scale *and* the algorithm compute ceiling — while the projection-export path is the honest escape hatch above the ceiling.
- Algorithm **write-back overwrites node-data properties with no versioning**, so re-running an algorithm silently replaces prior values. That sharpens **#10 (provenance/temporality)** for a "graph that learns."
- Embedding generation creates many more HNSW-indexed vectors, making **#9 (index rebuild after restore)** more consequential, and **#3 (audit)** more relevant since write-back and embedding generation are unaudited successful mutations.

**Overall.** The decisive functional gap is gone: LiteGraph is now the only one of the three candidates that ships the hybrid-retrieval *and* the cognition (algorithms) layer, with a first-class external-compute escape hatch. What remains is almost entirely the **enterprise-review surface** CKG.md §3.9/§5 flagged — audit completeness, encryption at rest, identity federation, authz depth — plus **published scale evidence (#1)** and **provenance (#10)**. None of these are architecturally hard; all are still open. For an *agent-facing, retrieval-and-cognition-dominant* CKG, LiteGraph's case is materially stronger post-v9.0; for a deployment gated on node-level authorization, encryption at rest, SSO, or demonstrated scale, the open items above are still the deciders.

---

## Priority Table (ordered by Score, descending)

Status column added in the v9.0.0 re-assessment above. Scores are the *original* pre-v9.0 prioritization (kept for continuity); the Status reflects what v9.0.0 delivered.

| # | Gap (§) | Brief description | Legit. | Value | Simpl. | Score | Status (post-v9.0) |
|---|---------|-------------------|:---:|:---:|:---:|:---:|---|
| 1 | Unpublished scale evidence (§3.7) | Benchmark *harness* exists but no node/edge counts, latency, throughput, or hardware ever published | 9 | 8 | 8 | 25 | **Open** (now also covers algorithm compute ceiling) |
| 2 | No graph algorithms (§3.2) | No centrality, community detection, PageRank, or embeddings — the "cognition" layer is absent | 10 | 9 | 4 | 23 | ✅ **Fixed (v9.0)** |
| 3 | Audit records denials only (§3.9) | Successful privileged actions leave no audit trail — fails the "who changed what, when" governance question | 8 | 8 | 7 | 23 | **Open** |
| 4 | No encryption at rest (§3.9) | No at-rest encryption for a sensitivity-classified knowledge store | 8 | 7 | 7 | 22 | **Open** |
| 5 | README/site version drift (intro) | README on `main` documents v7.0.0 while site documents v8.1 — misleads external evaluators | 8 | 5 | 9 | 22 | ✅ **Fixed (v9.0)** |
| 6 | Scan-bounded `ORDER BY`/aggregates (§3.3) | `COUNT(*)`/`ORDER BY` operate up to `MaxResults`, not the whole graph — a correctness trap for global reasoning | 8 | 7 | 5 | 20 | **Open** (algorithms now give a whole-graph path for some global stats) |
| 7 | Keyword-match authz fallback (§3.9) | Query authorization falls back to keyword matching (`CREATE`/`SET`/…) when parsing fails — weak mutation boundary | 7 | 6 | 7 | 20 | **Open** |
| 8 | No SSO/OIDC/SAML (§3.9) | No enterprise identity federation (out of scope today) | 8 | 7 | 5 | 20 | **Open** |
| 9 | HNSW rebuild-after-restore footgun (§3.11) | Vector index files are derived artifacts needing rebuild after restore/migration; not in a DR runbook | 7 | 5 | 8 | 20 | **Open** (more consequential — embedding generation adds vectors) |
| 10 | No provenance/temporality (§3.12) | No versioned nodes/edges or point-in-time reconstruction — must be modeled by hand | 8 | 8 | 4 | 20 | **Open** (algorithm write-back overwrites without versioning) |
| 11 | 32-hop traversal cap (§3.3) | Bounded traversal only; no unbounded variable-length paths (rejected by parser) | 7 | 6 | 6 | 19 | **Open** |
| 12 | No cross-graph/cross-tenant queries (§3.3) | Partitioned graphs can't be spanned by one query; federation must live in app code (one-way door) | 8 | 7 | 4 | 19 | **Open** (algorithms are also single-graph) |
| 13 | Authz granularity graph-level only (§3.9) | RBAC only at tenant/graph level, enforced at REST/MCP boundary — no node/edge/property-level control | 8 | 7 | 4 | 19 | **Open** (new `Algorithm` resource type added, still graph-level) |
| 14 | No multi-`MATCH` query chaining (§3.3) | Query chaining not yet supported | 7 | 6 | 5 | 18 | **Open** |
| 15 | Embedded mode has no authz (§3.9) | Core `LiteGraphClient`/repo APIs are permission-agnostic — any embedded .NET caller has unrestricted access | 8 | 6 | 4 | 18 | **Open** (`client.Algorithm` is likewise permission-agnostic embedded) |
| 16 | HA delegated to PostgreSQL (§3.11) | No native failover orchestration; relies on Postgres HA + process supervisor | 5 | 6 | 5 | 16 | **Open** |
| 17 | Query language doesn't generate embeddings (§3.4) | Vector search takes *supplied* embeddings only; generation lives at chat/RAG/app layer | 5 | 4 | 6 | 15 | 🟡 **Improved (v9.0)** — server-side node-embedding generation added |
| 18 | No embedded access for Python/JS (§3.8) | Python/JS SDKs are REST clients; embedded in-process path is .NET-only | 6 | 5 | 3 | 14 | **Open** (v9.0 added algorithm methods, still REST clients) |
| 19 | SQLite→Postgres migration offline only (§3.11) | Migration requires stopping writes ("offline only") | 5 | 4 | 5 | 14 | **Open** |

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

### 3. Audit records denials only (§3.9) — Score 23

> Add: no encryption at rest, no enterprise identity federation, and audit that records only denials — meaning **successful privileged actions leave no audit record**. For a CKG where "who changed what the system believes, and when" is the core governance question, that is a significant gap.

The security table lists Audit for LiteGraph as **"Denials only,"** and the summary matrix rates Audit trail as **Weak (denials only)**. The Option A prerequisites (§5) include:

> extend audit to successful privileged actions

---

### 4. No encryption at rest (§3.9) — Score 22

The security table lists Encryption at rest for LiteGraph as **"None,"** and the summary matrix rates it **Absent**.

> Add: no encryption at rest, no enterprise identity federation, and audit that records only denials …

The Option A prerequisites (§5) include:

> enable PostgreSQL encryption at rest

---

### 5. README/site version drift (intro) — Score 22

From the introductory **Documentation note:**

> litegraphdb.com documents v8.1; the repository README on `main` still documents v7.0.0. Worth reconciling before anyone external evaluates on the strength of the repo alone.

---

### 6. Scan-bounded `ORDER BY`/aggregates (§3.3) — Score 20

From the traversal/query table, LiteGraph rows:

> Aggregates — Partial — cannot mix with graph-variable returns; scan-bounded by `LIMIT`/`MaxResults`
> `ORDER BY` semantics — **Scans up to `MaxResults`, then sorts** — not a global ordering

> **Scan-bounded `ORDER BY` and aggregates.** `RETURN COUNT(*)` over a large graph counts up to the limit, not the graph. For a CKG that reasons over global structure, this is a semantic trap — correct results require understanding the bound.

---

### 7. Keyword-match authz fallback (§3.9) — Score 20

> **Query authorization falls back to keyword matching** (`CREATE`/`MERGE`/`SET`/`DELETE`/`REMOVE`) when parsing fails. A parse failure that reaches a fallback string match is a weak last line of defense for a mutation boundary.

---

### 8. No SSO/OIDC/SAML (§3.9) — Score 20

The security table lists **SSO / OIDC / SAML** for LiteGraph as **"None — explicitly out of scope,"** and the summary matrix rates Enterprise identity (SSO/OIDC) as **Absent**.

> Add: no encryption at rest, no enterprise identity federation, and audit that records only denials …

The Option A prerequisites (§5) include:

> add OIDC or an authenticating reverse proxy

---

### 9. HNSW rebuild-after-restore footgun (§3.11) — Score 20

> Delegating HA to PostgreSQL is a defensible design — it rides infrastructure most platform teams already run well. But **note the recovery footgun**: HNSW vector index files are derived artifacts that may require rebuilding after restore or migration. That belongs in the DR runbook with a measured rebuild time, or a restore will surprise someone during an incident.

---

### 10. No provenance/temporality (§3.12) — Score 20

> None of the three provides native bitemporal modeling, versioned nodes/edges, or point-in-time graph reconstruction. Every option requires modeling this yourself — versioned nodes, validity intervals on edges, or an append-only event log alongside the graph.

> LiteGraph: request history and per-turn chat records give partial forensic coverage; graph object history is not modeled.

> **This should be an explicit design workstream regardless of backend choice.** For a system that learns, "why did the graph believe X on this date" is the question that eventually gets asked in an incident or an audit, and no backend answers it for free.

The summary matrix rates Provenance / temporality as **Weak** for LiteGraph.

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

### 15. Embedded mode has no authz (§3.9) — Score 18

> **The core `LiteGraphClient` and repository APIs are permission-agnostic.** RBAC lives at the REST/MCP boundary. **Any embedded .NET caller therefore has unrestricted access to all tenants and graphs.** This means the embedded deployment mode — LiteGraph's key latency advantage — has *no authorization model at all*. Embedded mode and enforced RBAC are mutually exclusive today. That is an architectural constraint, not a configuration issue, and it should be documented explicitly rather than discovered.

The Option A prerequisites (§5) include:

> document the embedded-mode authorization caveat

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

### 19. SQLite→Postgres migration offline only (§3.11) — Score 14

> SQLite→PostgreSQL migration is **offline only** ("stop writes").

---

## Consolidated Deployment Prerequisites (from §5, Option A)

For internal deployment with LiteGraph as system of record, `CKG.md` lists these prerequisites, all of which map to items in the table above:

> **Prerequisites before internal deployment:** publish scale results; add OIDC or an authenticating reverse proxy; enable PostgreSQL encryption at rest; extend audit to successful privileged actions; document the embedded-mode authorization caveat.

| Prerequisite | Table item |
|---|---|
| Publish scale results | #1 |
| Add OIDC / authenticating reverse proxy | #8 |
| Enable PostgreSQL encryption at rest | #4 |
| Extend audit to successful privileged actions | #3 |
| Document the embedded-mode authorization caveat | #15 |
