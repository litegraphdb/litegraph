# CKG Backend Comparison: NetworkX vs. Neo4j vs. LiteGraph

**Scope:** Evaluation of three candidate graph technologies as the backend for a Cognitive Knowledge Graph (CKG) intended for eventual internal deployment on managed infrastructure.
**Date:** 2026-09-09
**Versions assessed:** NetworkX 3.x · Neo4j 5.x (Community and Enterprise) · LiteGraph v8.1

---

## Ground Rules For This Comparison

**Excluded by design:** market share, install base, hiring pool, and "industry standard" arguments. These are real procurement considerations but they are not engineering properties, and they crowd out the technical question.

**Not excluded — and not the same thing:** *demonstrated* production hardening. Whether a system has published scale evidence, a documented HA story, and a security posture that passes review is an engineering property, independent of how many people use it. Where a candidate is weak on that axis, this document says so.

**Disclosure:** LiteGraph is authored and maintained by this reviewer (`jchristn`). Any version of this comparison that circulates internally should carry that disclosure prominently. The assessment below is deliberately harder on LiteGraph than on the alternatives for that reason — a self-recommendation that omits its own gaps will be discounted entirely once the authorship is noticed, and rightly so.

**Documentation note:** litegraphdb.com documents v8.1; the repository README on `main` still documents v7.0.0. Worth reconciling before anyone external evaluates on the strength of the repo alone.

---

## 1. What a CKG Actually Requires

Before comparing, the dimensions that matter. A cognitive knowledge graph is not a generic graph workload — it has a specific profile:

| # | Requirement | Why it matters for a CKG specifically |
|---|---|---|
| 1 | **Rich edge semantics** | Relationships carry weight, confidence, provenance, decay — the edge *is* the knowledge |
| 2 | **Hybrid retrieval** | Concepts are reached both structurally (traversal) and semantically (embedding similarity) |
| 3 | **Graph algorithms** | Spreading activation, centrality, community detection, path ranking — the "cognition" |
| 4 | **High-frequency mutation** | The graph learns; write rate is unusually high for a knowledge store |
| 5 | **Provenance & temporality** | "Why does the system believe this, and since when" is a first-class question |
| 6 | **Agent/LLM integration** | The primary consumer is increasingly a model, not a human or a service |
| 7 | **Durability & transactions** | Learned state that vanishes or corrupts is worse than no state |
| 8 | **Access control granularity** | Knowledge is sensitivity-classified; not all consumers may see all of it |
| 9 | **Observability** | An adapting system must be explainable to its operators |
| 10 | **Scale headroom** | Knowledge accretes monotonically; the graph only grows |

Dimensions 3, 5, and 6 are where this comparison diverges most sharply from a generic graph database bake-off.

---

## 2. Summary Matrix

Rating scale: **Strong** · **Adequate** · **Weak** · **Absent**

| Dimension | NetworkX | Neo4j | LiteGraph |
|---|---|---|---|
| Property/edge model richness | Strong | Strong | Strong |
| Multigraph + directed edges | Strong | Strong | Strong |
| Nested/JSON data on objects | Strong | Weak | Strong |
| Labels + tags as separate facets | Adequate | Adequate | Strong |
| **Graph algorithms** | **Strong** | **Strong** (GDS) | **Absent** |
| Deep / unbounded traversal | Strong | Strong | Weak (32-hop cap, bounded only) |
| Query language | Absent | Strong | Adequate |
| **Vector / hybrid retrieval** | Absent | Strong | **Strong** |
| Durability & transactions | Absent | Strong | Strong |
| Concurrency / multi-writer | Absent | Strong | Adequate (Postgres-bound) |
| Scale (demonstrated) | Weak | Strong | **Unproven** |
| Per-operation latency | Strong | Weak | Adequate (embedded .NET only) |
| Multi-tenancy | Absent | Strong (Enterprise) | Strong |
| **Authorization granularity** | Absent | Strong (Enterprise) | **Weak** (graph-level; REST boundary only) |
| Enterprise identity (SSO/OIDC) | Absent | Strong (Enterprise) | **Absent** |
| Encryption at rest | Absent | Adequate | **Absent** |
| Audit trail | Absent | Strong (Enterprise) | Weak (denials only) |
| Observability | Absent | Strong | **Strong** (OTel/Prom/Loki/Grafana shipped) |
| HA / clustering | Absent | Strong (Enterprise) | Weak (delegated to Postgres) |
| Backup / DR | Absent | Strong | Adequate |
| **Agent / MCP integration** | Absent | Weak | **Strong** |
| Provenance / temporality | Absent | Weak | Weak |
| Licensing freedom | Strong (BSD) | Weak (GPLv3 / commercial) | **Strong** (MIT) |
| Prototyping velocity | Strong | Adequate | Strong |
| Python-native access | Strong | Adequate | Weak (REST SDK only) |
| Vendor support available | Absent | Strong | Absent |

---

## 3. Dimension-by-Dimension

### 3.1 Data Model

**All three are adequate here; the differences are in the details.**

- **NetworkX** — `MultiDiGraph` with arbitrary Python objects as node/edge attributes. Maximum flexibility, zero enforcement. Nothing prevents attribute drift or type inconsistency across the graph.
- **Neo4j** — Property graph with typed, directed, first-class relationships and unlimited parallel edges. The commonly repeated claim that Neo4j needs intermediate nodes for rich edges is false; that is the RDF reification problem, which the property graph model exists to avoid. **Real constraint:** property values are primitives and arrays only — no nested maps. Structured edge payloads must be JSON-serialized into strings or decomposed into nodes.
- **LiteGraph** — Nodes and edges carry `Name`, `Labels`, `Tags` (key/value), arbitrary JSON `Data`, and attached `Vectors`. Edges additionally carry a native `cost`. **This is the richest model of the three for CKG purposes**, because it separates three facets that CKGs genuinely use differently: labels for type, tags for queryable metadata, and JSON data for payload. Nested JSON is queryable (`n.data.profile.age >= 30`), which Neo4j cannot do natively.

**Verdict:** LiteGraph edges out Neo4j on model expressiveness for this workload, primarily due to nested JSON and the label/tag/vector separation.

---

### 3.2 Graph Algorithms — the decisive gap

This is the most important row in the matrix and it does not favor LiteGraph.

- **NetworkX** — Encyclopedic: centrality (all variants), community detection, PageRank, flow, matching, similarity, link prediction. Pure Python and slow, but the coverage is unmatched. `rustworkx` provides a compiled subset 10–100× faster.
- **Neo4j GDS** — Compiled, parallelized, in-memory projections: PageRank, Louvain, Leiden, node similarity, node embeddings (FastRP, GraphSAGE), pathfinding. Production-grade and fast. Separately licensed.
- **LiteGraph** — **None.** The query language provides `MATCH`, bounded variable-length paths, `MATCH SHORTEST`, and aggregates. There is no centrality, no community detection, no PageRank, no graph embedding.

For a system whose stated purpose is *cognition* — spreading activation, salience, clustering of related concepts, identification of central knowledge — this is a material gap, not a nice-to-have. It is the single strongest argument against LiteGraph as a complete CKG solution.

**Mitigation:** project the relevant subgraph out of LiteGraph into `rustworkx` for algorithmic passes and write results back as node/edge properties. This works, is a common pattern, and is not unreasonable — but it should be designed deliberately and costed, not discovered later.

---

### 3.3 Traversal and Query

| | NetworkX | Neo4j | LiteGraph |
|---|---|---|---|
| Query language | None (imperative Python) | Cypher (mature, full GQL trajectory) | Cypher/GQL-inspired native profile |
| Unbounded variable-length paths | Yes | Yes | **No — rejected by parser** |
| Max traversal depth | Unlimited | Unlimited | **32 hops** |
| Query chaining (multi-`MATCH`) | N/A | Yes | **Not yet supported** |
| Cross-partition queries | N/A | Cross-database via Fabric | **No cross-graph or cross-tenant queries** |
| Aggregates | Full (in Python) | Full | Partial — cannot mix with graph-variable returns; scan-bounded by `LIMIT`/`MaxResults` |
| `ORDER BY` semantics | Exact | Exact | **Scans up to `MaxResults`, then sorts** — not a global ordering |
| Cost-based planner | None | Yes | Plan summary with relative cost estimate |

Two LiteGraph limitations deserve emphasis for CKG use:

1. **No cross-graph queries.** If the CKG is partitioned into multiple graphs (by domain, tenant, or lifecycle), no single query can span them. Federation must be done in application code. This should drive the graph-partitioning decision early, because it is expensive to reverse.
2. **Scan-bounded `ORDER BY` and aggregates.** `RETURN COUNT(*)` over a large graph counts up to the limit, not the graph. For a CKG that reasons over global structure, this is a semantic trap — correct results require understanding the bound.

Neo4j is clearly strongest here. NetworkX has no query language at all, which is a genuine liability once more than one person writes traversal code.

---

### 3.4 Hybrid Retrieval (Graph + Vector)

The defining requirement for a modern CKG, and the dimension where the ranking inverts.

- **NetworkX** — **Nothing.** You bolt on FAISS or similar and hand-maintain consistency between two stores that can drift. This is a significant hidden cost the NetworkX proposal did not account for.
- **Neo4j** — Native vector indexes plus Lucene full-text alongside graph traversal. Solid and mature.
- **LiteGraph** — Vectors attach directly to nodes, edges, and graphs, with HNSW indexing and metadata filtering, queryable in the same language as the traversal (`CALL litegraph.vector.searchNodes($embedding)`). v8.1 adds server-side RAG with tenant-registered embedding endpoints.

**Caveat:** the query language takes *supplied* embeddings — it does not generate them. Embedding generation happens at the chat/RAG layer or in your application. Fine, but it means the query surface alone doesn't close the loop.

**Verdict:** LiteGraph and Neo4j are both strong; LiteGraph's integration is tighter (vectors are first-class graph objects rather than an indexed property). NetworkX is disqualified on this dimension alone for a retrieval-oriented CKG.

---

### 3.5 Agent and LLM Integration

- **NetworkX** — Nothing. You build it.
- **Neo4j** — Community MCP servers exist; nothing first-party and integrated. You build the tool surface.
- **LiteGraph** — MCP server over HTTP/TCP/WebSocket exposing graph, node, edge, vector, label, and tag operations as a maintained tool catalog. Built-in chat with an in-process tool loop, SSE streaming, per-turn telemetry (TTFT, tokens/sec, tool transcripts). OpenAI- and Ollama-wire-compatible graph-scoped endpoints.

If the CKG's primary consumer is an agent — which is the usual premise for a *cognitive* knowledge graph — this is a substantial and differentiating advantage. It removes an entire integration layer that would otherwise be custom-built and custom-maintained.

**Honest counterweight:** MCP tool calls pass through the REST boundary. This is correct for authorization, but it means agent operations pay HTTP latency, not embedded latency.

---

### 3.6 Durability, Transactions, and Concurrency

- **NetworkX** — No persistence, no transactions, not thread-safe, single writer, GIL-bound. Persisting arbitrary Python attributes in practice means `pickle`, which is an arbitrary-code-execution vector and will not survive security review as a system of record. **Disqualifying for the system-of-record role.**
- **Neo4j** — Full ACID, MVCC, mature concurrency. The reference standard here.
- **LiteGraph** — Graph-scoped transactions across nodes, edges, labels, tags, and vectors, with configurable isolation (`ReadCommitted`, `RepeatableRead`, `Serializable` on PostgreSQL) and detailed diagnostic results including conflict classification and retryability. Write scaling is inherited from PostgreSQL. SQLite is correctness-isolated but throughput-bound by file locking.

**Note the architecture honestly:** LiteGraph's durability and concurrency properties are PostgreSQL's, surfaced through a graph API. That is a legitimate and pragmatic design — it inherits a very well-understood storage engine — but it means LiteGraph's write-scaling ceiling is a PostgreSQL ceiling, not a native-graph-storage ceiling.

---

### 3.7 Scale

| | Position |
|---|---|
| **NetworkX** | ~1–2 KB/node and hundreds of bytes–1 KB/edge in Python object overhead. Practical ceiling: low single-digit millions of edges, one process, no sharding. |
| **Neo4j** | Billions of nodes and relationships, documented and demonstrated across many production deployments. |
| **LiteGraph** | **Unknown.** `PERF_SCALE_TESTING.md` documents a capable benchmark *harness* — profiles, topologies, workload families, concurrency ramps, regression thresholds — but publishes **no results**. No node/edge counts, no latency figures, no throughput numbers, no hardware. |

This must be stated plainly: **LiteGraph currently has no published scale evidence.** The tooling to produce it exists; the numbers do not. Any architecture review will ask, and "we have a harness" is not an answer.

*Actionable:* running the `large` and `soak` profiles against PostgreSQL and publishing the results — node/edge counts, p50/p95/p99 by workload, hardware — would convert the single largest open risk into a known quantity. This is probably the highest-leverage thing available to strengthen LiteGraph's position in any comparison.

---

### 3.8 Access Path and Latency

This is where the original NetworkX argument deserves a fair hearing, and where an important LiteGraph nuance emerges.

| | Access path |
|---|---|
| **NetworkX** | In-process dict lookup, sub-microsecond. Unbeatable — but only within one Python process, and only until the CKG becomes a shared service. |
| **Neo4j** | Bolt round-trip ~0.1–1 ms; embeddable in-process for JVM consumers. |
| **LiteGraph** | Embedded in-process via `LiteGraphClient` — **for .NET consumers only.** Python and JavaScript SDKs are **REST clients**. |

**This matters a great deal for the CKG decision.** If the CKG's consumers are Python — which the NetworkX proposal strongly implies — LiteGraph's embedded advantage does not accrue to them. Python callers pay an HTTP round-trip, in the same order of magnitude as Neo4j's Bolt hop. The embedded-vs-server distinction only pays off if the consuming code is .NET.

Stated fairly: **on latency, for Python consumers, LiteGraph and Neo4j are roughly comparable, and NetworkX is genuinely faster — right up until the CKG becomes a shared service, at which point all three pay a network hop and NetworkX's advantage evaporates.**

---

### 3.9 Security

| | NetworkX | Neo4j | LiteGraph |
|---|---|---|---|
| Authentication | None | Native users; Enterprise adds LDAP/SSO/OIDC/Kerberos | Bearer tokens, email/password, admin token |
| **SSO / OIDC / SAML** | None | Enterprise: yes | **None — explicitly out of scope** |
| Authorization granularity | None | Enterprise: node-, relationship-, and property-level | **Tenant and graph level only** |
| RBAC enforcement point | N/A | Engine | **REST/MCP boundary only** |
| Encryption at rest | None | Available | **None** |
| Encryption in transit | N/A | TLS on Bolt | TLS advised, not enforced on same-host |
| Audit | None | Enterprise: full security event log | **Denials only** |
| Token rotation / expiry / MFA | N/A | Enterprise: yes | Not documented |

Two LiteGraph findings an enterprise reviewer will raise, stated bluntly:

1. **The core `LiteGraphClient` and repository APIs are permission-agnostic.** RBAC lives at the REST/MCP boundary. **Any embedded .NET caller therefore has unrestricted access to all tenants and graphs.** This means the embedded deployment mode — LiteGraph's key latency advantage — has *no authorization model at all*. Embedded mode and enforced RBAC are mutually exclusive today. That is an architectural constraint, not a configuration issue, and it should be documented explicitly rather than discovered.
2. **Query authorization falls back to keyword matching** (`CREATE`/`MERGE`/`SET`/`DELETE`/`REMOVE`) when parsing fails. A parse failure that reaches a fallback string match is a weak last line of defense for a mutation boundary.

Add: no encryption at rest, no enterprise identity federation, and audit that records only denials — meaning **successful privileged actions leave no audit record**. For a CKG where "who changed what the system believes, and when" is the core governance question, that is a significant gap.

**Neo4j Enterprise is clearly ahead on this dimension.** Neo4j *Community*, however, is not — it lacks fine-grained security too, so the honest comparison for a zero-license-cost scenario is much closer than the table suggests.

---

### 3.10 Observability

- **NetworkX** — Nothing. There is no "query" to trace or aggregate. Everything is hand-instrumented.
- **Neo4j** — Query logs, slow-query logs, `PROFILE`/`EXPLAIN`, JMX and Prometheus metrics, transaction and page-cache telemetry. Mature.
- **LiteGraph** — Prometheus metrics on every REST route, MCP tool, and chat turn; OpenTelemetry traces spanning request, repository, query, vector index, authorization, and chat; structured logs into Loki via Grafana Alloy; seven provisioned Grafana dashboards; per-turn chat telemetry with trace IDs.

**LiteGraph is the strongest of the three here, and it is not close.** Shipping a provisioned Prometheus/OTel/Loki/Grafana stack in the default Compose deployment is materially better than what most projects at any size offer. For an internal deployment on managed infrastructure, this removes real integration work.

---

### 3.11 HA, DR, and Operations

- **NetworkX** — None of the above. Single process, SPOF, snapshot-and-pray.
- **Neo4j** — Enterprise clustering with Raft, causal consistency, online and incremental backup, point-in-time recovery. Community has none of this.
- **LiteGraph** — Explicitly does not implement failover orchestration; delegates to PostgreSQL HA plus a process supervisor. Backup via whole-database snapshot or portable per-graph JSONL export. Data can be moved between backends online via the graph export/projection and import endpoints (streaming read, no write-stop required).

Delegating HA to PostgreSQL is a defensible design — it rides infrastructure most platform teams already run well. But **note the recovery footgun**: HNSW vector index files are derived artifacts that may require rebuilding after restore or migration. That belongs in the DR runbook with a measured rebuild time, or a restore will surprise someone during an incident.

---

### 3.12 Provenance and Temporality

Worth calling out because **all three are weak**, and for a CKG this is a genuine requirement, not a refinement.

None of the three provides native bitemporal modeling, versioned nodes/edges, or point-in-time graph reconstruction. Every option requires modeling this yourself — versioned nodes, validity intervals on edges, or an append-only event log alongside the graph.

- NetworkX: no transaction log at all; nothing to reconstruct from.
- Neo4j: transaction log exists but is not a queryable history; PITR restores a whole database, not a subgraph timeline.
- LiteGraph: request history and per-turn chat records give partial forensic coverage; graph object history is not modeled.

**This should be an explicit design workstream regardless of backend choice.** For a system that learns, "why did the graph believe X on this date" is the question that eventually gets asked in an incident or an audit, and no backend answers it for free.

---

### 3.13 Licensing and Cost

| | Licensing | Practical implication |
|---|---|---|
| **NetworkX** | BSD-3 | Free, unrestricted. But the "cost" is building persistence, security, and durability yourself — plausibly 2–4 engineers indefinitely. |
| **Neo4j** | GPLv3 (Community) / commercial (Enterprise); GDS separately licensed | Community lacks clustering, hot backup, multi-database, and fine-grained security — a poor production fit. Enterprise is a procurement cycle and a recurring cost. |
| **LiteGraph** | MIT | Fully permissive, including the server, dashboard, MCP server, and SDKs. No feature gating between "community" and "enterprise" tiers. |

**LiteGraph's licensing position is genuinely strong.** The features Neo4j reserves for Enterprise — multi-tenancy, RBAC, observability — are present in LiteGraph under MIT. The gap is not licensing tier; it is depth (see §3.9).

---

## 4. Head-to-Head Verdicts

### NetworkX
**Role it should play:** research and algorithmic compute layer.
**Role it should not play:** system of record.

Excellent library, correctly chosen for prototyping and irreplaceable for algorithm coverage. As a CKG backend it has no persistence, no transactions, no concurrency, no security, no vector search, and a ceiling of low-millions of edges. The `pickle` persistence path is a security-review blocker on its own. If it stays, `rustworkx` is a near drop-in at 10–100× the speed.

### Neo4j
**Strongest at:** deep traversal, graph algorithms, demonstrated scale, fine-grained security, HA.
**Weakest at:** licensing cost, operational weight, per-operation latency from Python, agent/MCP integration, nested JSON on properties.

The safe choice when the CKG's hot path is algorithmic computation over deep graph structure, when node-level authorization is a hard requirement, or when a vendor support contract is required by policy. The cost is real money and real operational weight, and Community edition is not a production substitute.

### LiteGraph
**Strongest at:** unified graph + vector + metadata model, agent/MCP integration, observability, licensing freedom, deployment flexibility.
**Weakest at:** graph algorithms (absent), demonstrated scale (unpublished), enterprise identity and authorization depth, traversal ceilings.

Architecturally, LiteGraph is the closest match to *the shape of the CKG problem* — it is the only one of the three designed around the premise that the consumer is an agent and the retrieval is hybrid. That is a real and non-obvious advantage.

But it is also the least proven of the three, and the remaining gaps are in places an enterprise review probes hard: identity federation, authorization granularity, and published scale evidence. None of these is architecturally hard to close. (At-rest encryption, which such reviews also probe, is a deployment/infrastructure responsibility satisfied by an encrypted filesystem/volume or PostgreSQL TDE rather than product work; audit completeness has since been addressed — successful privileged actions are now audited.)

---

## 5. Recommendation

**No single one of these is a complete CKG backend. The realistic architectures are combinations.**

### Option A — LiteGraph as system of record + rustworkx as compute layer

Best fit if the CKG is agent-facing and retrieval-dominant.

- LiteGraph holds nodes, edges, labels, tags, JSON data, and vectors; serves MCP and chat; provides transactions, RBAC at the service boundary, and the observability stack.
- Periodic projection into `rustworkx` for centrality, community detection, and salience passes; results written back as node properties.
- **Prerequisites before internal deployment:** publish scale results; add OIDC or an authenticating reverse proxy; ensure at-rest encryption at the filesystem/volume or via PostgreSQL TDE (operator responsibility, not product work); document the embedded-mode authorization caveat. (Audit of successful privileged actions is now built in.)

### Option B — Neo4j Enterprise as system of record + GDS as compute layer

Best fit if node-level authorization, demonstrated scale, or vendor support are hard requirements today.

- Accepts license cost and operational weight in exchange for maturity and depth.
- Build the MCP tool surface yourself; budget for it.

### Option C — NetworkX only

Defensible **only** as an explicitly time-boxed research prototype with a written re-platforming plan. Not viable for managed-infra deployment, for the reasons in §3.6 and §3.9.

### Independent of choice — do these now

1. **Put a repository interface between the CKG and its consumers.** Every option above becomes swappable; without it, re-platforming is a full rewrite. This is nearly free today and expensive in six months.
2. **Design provenance and temporality explicitly.** No backend gives it to you (§3.12), and it is the requirement most likely to surface late and painfully.
3. **Decide the graph-partitioning strategy early** if LiteGraph is in play — no cross-graph queries means partitioning is a one-way door.
4. **Get a graph-size estimate on paper.** At 12 and 36 months, order of magnitude. It eliminates at least one option immediately and grounds every other argument in this document.

---

## 6. Sources

- [litegraphdb/litegraph — README](https://github.com/litegraphdb/litegraph)
- [LiteGraph — Multi-Modal AI Data Platform](https://litegraphdb.com)
- [LiteGraph — Native Graph Query Language (DSL.md)](https://github.com/litegraphdb/litegraph/blob/main/docs/DSL.md)
- [LiteGraph — RBAC and Scoped Credentials](https://github.com/litegraphdb/litegraph/blob/main/docs/RBAC.md)
- [LiteGraph — Storage Configuration](https://github.com/litegraphdb/litegraph/blob/main/docs/STORAGE.md)
- [LiteGraph — Performance and Scalability Testing](https://github.com/litegraphdb/litegraph/blob/main/PERF_SCALE_TESTING.md)
- [LiteGraph — Observability](https://github.com/litegraphdb/litegraph/blob/main/docs/OBSERVABILITY.md)
