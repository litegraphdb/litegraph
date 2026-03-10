# COMPATIBILITY.md — Neo4j, TigerGraph, and ArangoDB Adapter Implementation Plan

> **Status:** Draft
> **Created:** 2026-03-10
> **Last Updated:** 2026-03-10
> **Author:** LiteGraph Team

---

## Table of Contents

- [1. Priority Recommendation](#1-priority-recommendation)
- [2. Shared Infrastructure](#2-shared-infrastructure)
- [3. TigerGraph Adapter](#3-tigergraph-adapter)
  - [3.1 Overview](#31-overview)
  - [3.2 Data Model Mapping](#32-data-model-mapping)
  - [3.3 Architecture](#33-architecture)
  - [3.4 API Endpoints / Protocol Surface](#34-api-endpoints--protocol-surface)
  - [3.5 Query Language Translation (GSQL)](#35-query-language-translation-gsql)
  - [3.6 Authentication Mapping](#36-authentication-mapping)
  - [3.7 Known Limitations](#37-known-limitations)
  - [3.8 Implementation Phases](#38-implementation-phases)
  - [3.9 Testing Strategy](#39-testing-strategy)
- [4. ArangoDB Adapter](#4-arangodb-adapter)
  - [4.1 Overview](#41-overview)
  - [4.2 Data Model Mapping](#42-data-model-mapping)
  - [4.3 Architecture](#43-architecture)
  - [4.4 API Endpoints / Protocol Surface](#44-api-endpoints--protocol-surface)
  - [4.5 Query Language Translation (AQL)](#45-query-language-translation-aql)
  - [4.6 Authentication Mapping](#46-authentication-mapping)
  - [4.7 Known Limitations](#47-known-limitations)
  - [4.8 Implementation Phases](#48-implementation-phases)
  - [4.9 Testing Strategy](#49-testing-strategy)
- [5. Neo4j Adapter](#5-neo4j-adapter)
  - [5.1 Overview](#51-overview)
  - [5.2 Data Model Mapping](#52-data-model-mapping)
  - [5.3 Architecture](#53-architecture)
  - [5.4 API Endpoints / Protocol Surface](#54-api-endpoints--protocol-surface)
  - [5.5 Query Language Translation (Cypher)](#55-query-language-translation-cypher)
  - [5.6 Authentication Mapping](#56-authentication-mapping)
  - [5.7 Known Limitations](#57-known-limitations)
  - [5.8 Implementation Phases](#58-implementation-phases)
  - [5.9 Testing Strategy](#59-testing-strategy)
- [6. Overall Testing Strategy](#6-overall-testing-strategy)

---

## 1. Priority Recommendation

**Suggested implementation order: TigerGraph → ArangoDB → Neo4j**

| Order | Adapter | Rationale |
|-------|---------|-----------|
| 1st | **TigerGraph** | REST-only API (RESTPP) is the simplest to implement. No binary protocol required. GSQL translation is narrower in scope than Cypher or AQL. Fastest path to a working adapter that validates shared infrastructure. |
| 2nd | **ArangoDB** | Pure HTTP API with JSON throughout. AQL is more expressive than GSQL but still REST-based. Multi-model nature (document + graph) maps well to LiteGraph's flexible Data property. Builds on shared infrastructure from TigerGraph work. |
| 3rd | **Neo4j** | Most complex due to dual-protocol requirement (HTTP API + Bolt binary protocol). Cypher is the most expressive query language of the three, requiring the most sophisticated parser. However, Neo4j has the largest user base, making it the highest-value adapter once the infrastructure is proven. |

**Key insight:** Building TigerGraph first lets us validate the query AST framework, result set transformer, and adapter webserver pattern with the simplest protocol surface. Each subsequent adapter reuses more shared infrastructure and can focus on its unique challenges.

---

## 2. Shared Infrastructure

These components are built once and reused across all three adapters. They should be implemented as a shared library project (`LiteGraph.Adapters.Core`).

### 2.1 Query AST Framework

A general-purpose abstract syntax tree for representing graph queries before translation to LiteGraph operations.

| Component | Description |
|-----------|-------------|
| `QueryAst` | Root AST node representing a parsed query |
| `MatchClause` | Node/edge pattern matching (used by Cypher, GSQL, AQL) |
| `FilterClause` | WHERE conditions → ExpressionTree conversion |
| `ReturnClause` | Projection/shaping of results |
| `AggregationClause` | COUNT, SUM, AVG, etc. (post-processing) |
| `PathClause` | Shortest path / route queries → RouteRequest |
| `MutationClause` | CREATE, UPDATE, DELETE operations |

**Implementation phases:**

- [ ] Define core AST node types and interfaces
- [ ] Implement AST-to-LiteGraph operation translator (AST → SearchRequest, EnumerationRequest, CRUD calls)
- [ ] Implement ExpressionTree builder from filter predicates
- [ ] Implement result projection engine (select specific fields from LiteGraph results)
- [ ] Add aggregation post-processor (COUNT, SUM, AVG, COLLECT/GROUP)

**Estimated effort:** 3–4 weeks

### 2.2 Schema Registry

Maps between LiteGraph's schema-flexible model and the more rigid schemas expected by each target API.

| Component | Description |
|-----------|-------------|
| `SchemaRegistry` | In-memory registry of advertised vertex/edge types per graph |
| `TypeInferenceEngine` | Infers schema from existing LiteGraph labels and data shapes |
| `SchemaEndpointHandler` | Serves schema metadata in each adapter's expected format |

**Implementation phases:**

- [ ] Implement SchemaRegistry with label-based type inference
- [ ] Implement schema serialization for TigerGraph format
- [ ] Implement schema serialization for ArangoDB format
- [ ] Implement schema serialization for Neo4j format

**Estimated effort:** 1–2 weeks

### 2.3 Result Set Transformer

Converts LiteGraph `EnumerationResult<T>` and search results into each target API's expected response format.

| Component | Description |
|-----------|-------------|
| `ResultTransformer<TSource, TTarget>` | Generic transformer interface |
| `PaginationAdapter` | Maps LiteGraph's ContinuationToken/Skip to target pagination models |
| `ErrorMapper` | Maps LiteGraph exceptions to target API error codes and formats |

**Implementation phases:**

- [ ] Define transformer interfaces and base classes
- [ ] Implement pagination adapters for each target format
- [ ] Implement error code mapping per target API

**Estimated effort:** 1–2 weeks

### 2.4 Adapter Webserver Base

A base class for adapter webservers that handles common concerns.

| Component | Description |
|-----------|-------------|
| `AdapterWebServerBase` | Common HTTP server setup, health checks, logging |
| `LiteGraphClientFactory` | Creates and manages LiteGraphClient connections to core |
| `TenantResolver` | Resolves incoming requests to LiteGraph tenant GUIDs |
| `RequestContextBuilder` | Builds LiteGraph request context from adapter-specific auth |

**Implementation phases:**

- [ ] Implement AdapterWebServerBase with Watson Webserver
- [ ] Implement LiteGraphClientFactory with connection pooling
- [ ] Implement TenantResolver with configurable mapping strategies
- [ ] Implement health check and status endpoints

**Estimated effort:** 1–2 weeks

### Shared Infrastructure Total Estimated Effort: 6–10 weeks

---

## 3. TigerGraph Adapter

### 3.1 Overview

**TigerGraph** is a distributed graph database designed for real-time analytics on large-scale graph data. It uses **RESTPP** (REST++) as its primary API protocol and **GSQL** as its query language.

**Why LiteGraph should support it:**
- TigerGraph is widely used in enterprise analytics, fraud detection, and recommendation engines
- RESTPP is a clean REST API, making adapter implementation straightforward
- Organizations migrating from or evaluating TigerGraph can use LiteGraph as a lightweight alternative
- Enables TigerGraph-compatible tooling (GraphStudio, pyTigerGraph) to work with LiteGraph data

**Protocol:** HTTP REST (RESTPP)
**Query Language:** GSQL
**Standard Port:** 9000 (RESTPP), 14240 (GraphStudio — out of scope)

### 3.2 Data Model Mapping

| TigerGraph Concept | LiteGraph Concept | Notes |
|--------------------|-------------------|-------|
| Graph | Graph | One-to-one mapping. TigerGraph graphs are named; LiteGraph graphs have Name + GUID. |
| Vertex | Node | Direct mapping. TigerGraph vertices have a type and primary key; LiteGraph nodes have Labels (used as type) and GUID (primary key). |
| Edge | Edge | Direct mapping. TigerGraph edges have a type, source, target, and optional directionality; LiteGraph edges have Labels (type), From/To, and Cost. |
| Vertex Type | Node Labels[0] | TigerGraph has strict vertex types; LiteGraph uses the first label as the primary type. Additional labels can represent multi-type vertices. |
| Edge Type | Edge Labels[0] | Same approach as vertex types. |
| Vertex Attributes | Node Data (JSON) | TigerGraph attributes are typed and schema-defined; LiteGraph stores arbitrary JSON in Data. Schema registry infers attribute schema from Data shape. |
| Edge Attributes | Edge Data (JSON) | Same as vertex attributes. |
| Primary ID | Node GUID | TigerGraph uses user-defined primary IDs; adapter maps these to/from LiteGraph GUIDs. A secondary index (stored in Tags) maps TigerGraph IDs to GUIDs. |
| Tags | Tags (NameValueCollection) | Direct mapping for simple key-value metadata. |
| Graph Schema | Schema Registry | TigerGraph requires explicit schema; adapter infers from LiteGraph labels and data shapes. |
| — | Tenant | No TigerGraph equivalent. Adapter maps to a configured default tenant or uses database-to-tenant mapping. |
| — | Vectors | No native TigerGraph equivalent. Could be exposed via custom vertex attributes. |
| Accumulator | — | TigerGraph accumulators have no LiteGraph equivalent; handled via post-processing. |
| Loading Job | Batch Create | TigerGraph loading jobs map to LiteGraph CreateMany operations. |

**Bridging gaps:**
- **Primary ID mapping:** Store TigerGraph-style primary IDs in Tags (`_tg_primary_id` key). Maintain an in-memory index for fast lookups.
- **Schema enforcement:** The schema registry advertises vertex/edge types derived from labels. Incoming data is validated against the inferred schema.
- **Multi-graph:** TigerGraph supports multiple graphs; each maps to a separate LiteGraph Graph within the configured tenant.

### 3.3 Architecture

```
┌──────────────────────────────────────┐
│      TigerGraph Clients              │
│  (pyTigerGraph, GraphStudio, etc.)   │
└──────────────┬───────────────────────┘
               │ HTTP :9000
┌──────────────▼───────────────────────┐
│    LiteGraph.TigerGraphAdapter       │
│    (Separate .NET webserver)         │
│                                      │
│  ┌────────────┐  ┌────────────────┐  │
│  │ RESTPP     │  │ GSQL Parser &  │  │
│  │ Endpoint   │  │ Translator     │  │
│  │ Handlers   │  │                │  │
│  └─────┬──────┘  └───────┬────────┘  │
│        │                 │           │
│  ┌─────▼─────────────────▼────────┐  │
│  │  Query AST → LiteGraph Ops    │  │
│  │  Result Transformer            │  │
│  │  Schema Registry               │  │
│  └─────────────┬──────────────────┘  │
└────────────────┼─────────────────────┘
                 │ LiteGraphClient
┌────────────────▼─────────────────────┐
│         LiteGraph Core               │
│    (SQLite / In-Memory Store)        │
└──────────────────────────────────────┘
```

**Project:** `LiteGraph.TigerGraphAdapter`
**Port:** 9000
**Connection to core:** `LiteGraphClient` pointed at LiteGraph.Server endpoint, or embedded `SqliteGraphRepository`

### 3.4 API Endpoints / Protocol Surface

#### P0 — Must-Have for Basic Compatibility

| Endpoint | Method | Description | LiteGraph Mapping |
|----------|--------|-------------|-------------------|
| `/graph/{graph}/vertices/{type}` | GET | List vertices of a type | `ReadMany` with label filter |
| `/graph/{graph}/vertices/{type}/{id}` | GET | Get vertex by ID | `Read` by GUID (via ID mapping) |
| `/graph/{graph}/vertices/{type}` | POST | Upsert vertices | `Create` / `Update` node |
| `/graph/{graph}/edges/{srcType}/{srcId}/{edgeType}/{tgtType}/{tgtId}` | GET | Get specific edge | `ReadMany` with From/To/label filter |
| `/graph/{graph}/edges/{srcType}/{srcId}/{edgeType}/{tgtType}` | GET | Get edges from vertex | `GetEdgesOut` / `GetEdgesIn` |
| `/graph/{graph}/edges` | POST | Upsert edges | `Create` / `Update` edge |
| `/graph/{graph}/vertices/{type}` | DELETE | Delete vertices | `Delete` / `DeleteMany` node |
| `/graph/{graph}/edges/{srcType}/{srcId}/{edgeType}/{tgtType}/{tgtId}` | DELETE | Delete edge | `Delete` edge |
| `/echo` | GET | Health check | Return server status |
| `/endpoints` | GET | List endpoints | Return registered routes |
| `/version` | GET | Version info | Return adapter version |
| `/statistics` | GET | Graph statistics | Aggregate from LiteGraph counts |

#### P1 — Important

| Endpoint | Method | Description | LiteGraph Mapping |
|----------|--------|-------------|-------------------|
| `/query/{graph}/{queryName}` | GET/POST | Run installed query | GSQL → LiteGraph translation |
| `/graph/{graph}/vertices/{type}` | GET (with filter) | Filter vertices by attributes | `Search` with ExpressionTree |
| `/builtins/{graph}` | POST | Built-in algorithms (shortest path, page rank) | Route finding for paths; post-processing for analytics |
| `/graph/{graph}/edges/{srcType}/{srcId}` | GET | All edges from a vertex | `GetEdgesOut` + `GetEdgesIn` |
| `/ddl/{graph}` | POST | Schema DDL | Schema registry update |
| `/rebuildnow/{graph}` | POST | Rebuild graph engine | Vector index rebuild |

#### P2 — Nice-to-Have

| Endpoint | Method | Description | LiteGraph Mapping |
|----------|--------|-------------|-------------------|
| `/gsqlserver/gsql/schema` | GET | Get schema definition | Schema registry serialization |
| `/gsqlserver/gsql/library` | GET | List installed queries | Return registered query mappings |
| `/graph/{graph}/loading` | POST | Bulk loading | `CreateMany` batch operations |
| `/graph/{graph}/allpaths` | POST | All paths between vertices | Route finding with all paths |
| `/requesttoken` | POST | OAuth token generation | Generate LiteGraph bearer token |
| `/showprocesslist` | GET | Active queries | Return active request count |

### 3.5 Query Language Translation (GSQL)

#### Vertex Lookup

**GSQL:**
```gsql
SELECT * FROM Person WHERE primary_id == "p001"
```

**LiteGraph operations:**
```
1. Resolve "p001" → GUID via Tag lookup (_tg_primary_id = "p001")
2. client.Nodes.Read(tenantGuid, graphGuid, nodeGuid)
```

#### Vertex Query with Filter

**GSQL:**
```gsql
SELECT * FROM Person WHERE age > 30 AND city == "Seattle"
```

**LiteGraph operations:**
```
SearchRequest {
    Labels: ["Person"],
    Expr: ExpressionTree {
        And(
            GreaterThan("$.age", 30),
            Equals("$.city", "Seattle")
        )
    }
}
→ client.Nodes.Search(tenantGuid, graphGuid, searchRequest)
```

#### Edge Traversal

**GSQL:**
```gsql
SELECT t FROM Person:s -(KNOWS>)- Person:t WHERE s.primary_id == "p001"
```

**LiteGraph operations:**
```
1. Resolve "p001" → nodeGuid
2. client.Edges.GetEdgesOut(tenantGuid, graphGuid, nodeGuid)
   (filtered by edge label "KNOWS")
3. For each edge, client.Nodes.Read(tenantGuid, graphGuid, edge.To)
```

#### Shortest Path

**GSQL:**
```gsql
SELECT shortestPath(s, t) FROM Person:s, Person:t
WHERE s.primary_id == "p001" AND t.primary_id == "p002"
```

**LiteGraph operations:**
```
1. Resolve "p001" → sourceGuid, "p002" → targetGuid
2. client.Routes.GetRoute(tenantGuid, graphGuid, RouteRequest {
       From: sourceGuid,
       To: targetGuid,
       Algorithm: Dijkstra
   })
```

#### Insert Vertices

**GSQL:**
```gsql
INSERT INTO Person VALUES ("p003", "Alice", 28, "Portland")
```

**LiteGraph operations:**
```
client.Nodes.Create(new Node {
    TenantGUID: tenantGuid,
    GraphGUID: graphGuid,
    Labels: ["Person"],
    Tags: { "_tg_primary_id": "p003" },
    Data: { "name": "Alice", "age": 28, "city": "Portland" }
})
```

#### Accumulators (Post-Processing)

**GSQL:**
```gsql
SumAccum<INT> @edgeCount;
SELECT s FROM Person:s
ACCUM s.@edgeCount += 1
ORDER BY s.@edgeCount DESC
LIMIT 10
```

**LiteGraph operations:**
```
1. client.Nodes.ReadMany(tenantGuid, graphGuid, labels: ["Person"])
2. For each node, count edges (EdgesTotal property)
3. Sort by edge count descending
4. Take top 10
```

### 3.6 Authentication Mapping

| TigerGraph Auth | LiteGraph Auth | Mapping |
|-----------------|----------------|---------|
| Bearer token (OAuth2) | Bearer token | Pass through directly. TigerGraph token → LiteGraph bearer token. |
| Username/password | Credential auth | Map to `x-email` / `x-password` headers on LiteGraphClient. |
| GSQL secret | Bearer token | Map GSQL secret to a pre-configured bearer token. |
| Graph-level access | Tenant + Graph | TigerGraph graph permissions map to LiteGraph tenant isolation. Each TigerGraph graph maps to a LiteGraph graph within a configured tenant. |

**Tenant resolution strategy:**
- Single-tenant mode (default): All TigerGraph requests map to a configured tenant GUID
- Multi-tenant mode: HTTP header or graph-name prefix determines tenant

### 3.7 Known Limitations

| TigerGraph Feature | Limitation | Reason |
|--------------------|------------|--------|
| **Distributed Processing** | Not supported | LiteGraph is single-node; no distributed compute layer |
| **Accumulators (full)** | Partial support | Simple accumulators (SUM, COUNT) via post-processing; complex accumulators (MapAccum, HeapAccum) not feasible |
| **Loading Jobs** | Simplified | Bulk loading maps to CreateMany; no CSV/file-based loading pipeline |
| **User-Defined Functions (UDF)** | Not supported | No plugin/UDF execution environment in LiteGraph |
| **Real-time Updates (Delta)** | Not supported | No streaming/delta update mechanism |
| **GSQL Subqueries** | Partial | Simple subqueries decomposed into multiple LiteGraph calls; complex recursion not supported |
| **Graph Partitioning** | Not supported | Single-node architecture |
| **Token-based Authentication (fine-grained)** | Simplified | TigerGraph's per-graph token scoping simplified to tenant-level bearer tokens |
| **Type-strict Schema** | Best effort | LiteGraph is schema-free; schema registry provides compatibility but doesn't enforce strict types |

### 3.8 Implementation Phases

#### Phase 1: Core RESTPP Endpoints (P0)
- [ ] Project scaffold: `LiteGraph.TigerGraphAdapter` .NET project with Watson Webserver on port 9000
- [ ] LiteGraphClient connection setup and configuration
- [ ] TigerGraph-style primary ID ↔ GUID mapping (Tag-based index)
- [ ] Vertex CRUD endpoints (`/graph/{graph}/vertices/...`)
- [ ] Edge CRUD endpoints (`/graph/{graph}/edges/...`)
- [ ] Health check (`/echo`), version (`/version`), endpoints listing (`/endpoints`)
- [ ] Statistics endpoint (`/statistics`)
- [ ] Result format transformation (LiteGraph → TigerGraph JSON response format)
- [ ] Error code mapping (LiteGraph exceptions → TigerGraph error codes)

**Estimated effort:** 3–4 weeks

#### Phase 2: GSQL Query Support (P1)
- [ ] GSQL tokenizer and parser (subset: SELECT, INSERT, UPDATE, DELETE)
- [ ] GSQL WHERE clause → ExpressionTree translator
- [ ] Query endpoint (`/query/{graph}/{queryName}`)
- [ ] Built-in shortest path (`/builtins/{graph}`)
- [ ] Attribute-filtered vertex/edge queries
- [ ] ORDER BY and LIMIT support via post-processing

**Estimated effort:** 4–5 weeks

#### Phase 3: Schema and Bulk Operations (P1–P2)
- [ ] Schema DDL endpoint (`/ddl/{graph}`)
- [ ] Schema query endpoint (`/gsqlserver/gsql/schema`)
- [ ] Bulk loading endpoint (`/graph/{graph}/loading`)
- [ ] All-paths query support

**Estimated effort:** 2–3 weeks

#### Phase 4: Advanced Features (P2)
- [ ] Simple accumulator support (SUM, COUNT, MIN, MAX)
- [ ] Token generation endpoint (`/requesttoken`)
- [ ] Multi-tenant routing via graph name prefix
- [ ] Process list / active query endpoint

**Estimated effort:** 2–3 weeks

**TigerGraph Adapter Total Estimated Effort: 11–15 weeks**

### 3.9 Testing Strategy

| Test Layer | Approach |
|------------|----------|
| **Unit Tests** | Test GSQL parsing, ID mapping, result transformation in isolation |
| **Integration Tests** | Run adapter against embedded LiteGraph; exercise all RESTPP endpoints with known data |
| **Compatibility Tests** | Use pyTigerGraph client library to exercise the adapter; compare responses to TigerGraph reference responses |
| **Load Tests** | Bulk insert 100K vertices + 500K edges; query throughput benchmarks |
| **Schema Tests** | Verify schema inference produces valid TigerGraph-compatible schema definitions |

---

## 4. ArangoDB Adapter

### 4.1 Overview

**ArangoDB** is a multi-model database supporting document, key-value, and graph data models. It uses an **HTTP REST API** and **AQL (ArangoDB Query Language)** for queries.

**Why LiteGraph should support it:**
- ArangoDB's multi-model approach aligns well with LiteGraph's flexible data model (JSON Data, Labels, Tags, Vectors)
- AQL is a powerful and expressive query language with good graph traversal support
- ArangoDB has a growing user base, particularly in knowledge graph and recommendation system use cases
- ArangoDB's REST API is well-documented and consistently designed, making adapter development clean
- Enables ArangoDB-compatible tools (ArangoDB Web UI, arangojs, python-arango) to work with LiteGraph

**Protocol:** HTTP REST API
**Query Language:** AQL
**Standard Port:** 8529

### 4.2 Data Model Mapping

| ArangoDB Concept | LiteGraph Concept | Notes |
|------------------|-------------------|-------|
| Database | Tenant | ArangoDB databases provide isolation; maps to LiteGraph tenants. |
| Graph | Graph | Named graph in ArangoDB maps directly to LiteGraph Graph. |
| Document Collection | — | ArangoDB document collections holding vertices. Label-based grouping in LiteGraph. |
| Edge Collection | — | ArangoDB edge collections. Edge labels in LiteGraph serve as collection names. |
| Document (`_key`, `_id`, `_rev`) | Node (GUID) | `_key` maps to a Tag (`_arango_key`); `_id` is `{collection}/{_key}`; `_rev` maps to UpdatedUtc timestamp hash. |
| Edge (`_from`, `_to`, `_key`, `_id`) | Edge (From, To, GUID) | `_from`/`_to` are `{collection}/{_key}` format; adapter translates to/from GUIDs. |
| Document Properties | Node/Edge Data (JSON) | Direct mapping. ArangoDB stores arbitrary JSON; LiteGraph Data is arbitrary JSON. |
| Collection | Label grouping | ArangoDB collections map to LiteGraph labels. A node with label "Person" is in the "Person" collection. |
| Named Graph (edge definitions) | Graph + Edge Labels | ArangoDB named graphs define edge collections and connected vertex collections; adapter infers from LiteGraph edge labels. |
| Index | — | ArangoDB indexes (hash, skiplist, fulltext, geo) have no direct LiteGraph equivalent. Vector indexes map to HNSW. |
| — | Tenant | No direct ArangoDB equivalent beyond `_system` database separation. |
| — | Vectors | ArangoDB has no native vector type. Stored as document array properties. |
| — | Tags | Mapped to special document properties (prefixed with `_lg_tag_`). |
| View (ArangoSearch) | — | ArangoSearch views for full-text search have no LiteGraph equivalent. |

**Bridging gaps:**
- **`_key` / `_id` mapping:** Store ArangoDB-style keys in Tags (`_arango_key`). Construct `_id` as `{label}/{_key}`. Maintain in-memory index for fast resolution.
- **`_rev` generation:** Generate deterministic revision strings from `UpdatedUtc` timestamp (e.g., base64-encoded tick count).
- **Collection abstraction:** ArangoDB collections map to LiteGraph label groupings. Listing a collection returns all nodes/edges with that label.
- **Edge definitions:** Named graph edge definitions are stored in the schema registry, mapping edge labels to valid source/target vertex labels.

### 4.3 Architecture

```
┌──────────────────────────────────────┐
│      ArangoDB Clients                │
│  (arangojs, python-arango, Web UI)   │
└──────────────┬───────────────────────┘
               │ HTTP :8529
┌──────────────▼───────────────────────┐
│    LiteGraph.ArangoAdapter           │
│    (Separate .NET webserver)         │
│                                      │
│  ┌────────────┐  ┌────────────────┐  │
│  │ REST API   │  │ AQL Parser &   │  │
│  │ Endpoint   │  │ Translator     │  │
│  │ Handlers   │  │                │  │
│  └─────┬──────┘  └───────┬────────┘  │
│        │                 │           │
│  ┌─────▼─────────────────▼────────┐  │
│  │  Query AST → LiteGraph Ops    │  │
│  │  Result Transformer            │  │
│  │  Schema Registry               │  │
│  └─────────────┬──────────────────┘  │
└────────────────┼─────────────────────┘
                 │ LiteGraphClient
┌────────────────▼─────────────────────┐
│         LiteGraph Core               │
│    (SQLite / In-Memory Store)        │
└──────────────────────────────────────┘
```

**Project:** `LiteGraph.ArangoAdapter`
**Port:** 8529
**Connection to core:** `LiteGraphClient` pointed at LiteGraph.Server endpoint, or embedded `SqliteGraphRepository`

### 4.4 API Endpoints / Protocol Surface

#### P0 — Must-Have for Basic Compatibility

| Endpoint | Method | Description | LiteGraph Mapping |
|----------|--------|-------------|-------------------|
| `/_api/document/{collection}` | POST | Create document | `Create` node/edge |
| `/_api/document/{collection}/{key}` | GET | Read document | `Read` node/edge by key mapping |
| `/_api/document/{collection}/{key}` | PUT | Replace document | `Update` node/edge |
| `/_api/document/{collection}/{key}` | PATCH | Partial update | Read + merge + `Update` |
| `/_api/document/{collection}/{key}` | DELETE | Delete document | `Delete` node/edge |
| `/_api/cursor` | POST | Execute AQL query | AQL parse → LiteGraph ops |
| `/_api/cursor/{id}` | PUT | Fetch next batch | ContinuationToken-based pagination |
| `/_api/cursor/{id}` | DELETE | Dispose cursor | Release continuation state |
| `/_api/collection` | GET | List collections | Distinct labels from nodes/edges |
| `/_api/collection` | POST | Create collection | Register label in schema registry |
| `/_api/collection/{name}` | GET | Collection info | Label metadata + count |
| `/_api/collection/{name}` | DELETE | Drop collection | Delete all nodes/edges with label |
| `/_api/collection/{name}/count` | GET | Document count | Count nodes/edges with label |
| `/_api/database/current` | GET | Current database info | Tenant metadata |
| `/_api/version` | GET | Server version | Adapter version info |
| `/_api/gharial` | GET | List named graphs | List graphs in tenant |
| `/_api/gharial` | POST | Create named graph | Create graph |
| `/_api/gharial/{graph}` | GET | Get graph info | Read graph |
| `/_api/gharial/{graph}` | DELETE | Drop graph | Delete graph |
| `/_api/gharial/{graph}/vertex/{collection}` | POST | Create vertex | Create node |
| `/_api/gharial/{graph}/vertex/{collection}/{key}` | GET | Get vertex | Read node |
| `/_api/gharial/{graph}/vertex/{collection}/{key}` | PATCH | Update vertex | Update node |
| `/_api/gharial/{graph}/vertex/{collection}/{key}` | DELETE | Delete vertex | Delete node |
| `/_api/gharial/{graph}/edge/{collection}` | POST | Create edge | Create edge |
| `/_api/gharial/{graph}/edge/{collection}/{key}` | GET | Get edge | Read edge |
| `/_api/gharial/{graph}/edge/{collection}/{key}` | PATCH | Update edge | Update edge |
| `/_api/gharial/{graph}/edge/{collection}/{key}` | DELETE | Delete edge | Delete edge |

#### P1 — Important

| Endpoint | Method | Description | LiteGraph Mapping |
|----------|--------|-------------|-------------------|
| `/_api/document/{collection}` | POST (array) | Bulk insert | `CreateMany` |
| `/_api/document/{collection}` | PUT (array) | Bulk replace | Batch `Update` |
| `/_api/document/{collection}` | DELETE (array) | Bulk delete | `DeleteMany` |
| `/_api/simple/all` | PUT | Return all documents | `ReadMany` |
| `/_api/simple/by-example` | PUT | Query by example | `Search` with ExpressionTree |
| `/_api/collection/{name}/properties` | PUT | Modify collection | Update schema registry |
| `/_api/gharial/{graph}/vertex` | GET | List vertex collections | Graph node labels |
| `/_api/gharial/{graph}/edge` | GET | List edge definitions | Graph edge labels |
| `/_api/index` | GET/POST/DELETE | Manage indexes | Vector index management |
| `/_api/database` | GET | List databases | List tenants |
| `/_api/database` | POST | Create database | Create tenant |
| `/_api/traversal` | POST | Execute traversal | Node neighbors + route finding |

#### P2 — Nice-to-Have

| Endpoint | Method | Description | LiteGraph Mapping |
|----------|--------|-------------|-------------------|
| `/_api/import` | POST | Bulk import (JSON lines) | Batch `CreateMany` |
| `/_api/export` | POST | Bulk export | `ReadAllInGraph` streaming |
| `/_api/explain` | POST | Explain AQL query | Return mapped LiteGraph operations |
| `/_api/query/properties` | GET | Query tracking | Active request tracking |
| `/_api/collection/{name}/checksum` | GET | Collection checksum | Hash of all label data |
| `/_api/tasks` | GET/POST/DELETE | Server tasks | Not applicable — stub response |

### 4.5 Query Language Translation (AQL)

#### Simple Document Lookup

**AQL:**
```aql
RETURN DOCUMENT("Person/alice")
```

**LiteGraph operations:**
```
1. Parse collection="Person", key="alice"
2. Resolve key → GUID via Tag lookup (_arango_key = "alice")
3. client.Nodes.Read(tenantGuid, graphGuid, nodeGuid)
4. Format as ArangoDB document JSON with _key, _id, _rev
```

#### Collection Scan with Filter

**AQL:**
```aql
FOR p IN Person
  FILTER p.age > 30 AND p.city == "Seattle"
  RETURN p
```

**LiteGraph operations:**
```
SearchRequest {
    Labels: ["Person"],
    Expr: ExpressionTree {
        And(
            GreaterThan("$.age", 30),
            Equals("$.city", "Seattle")
        )
    }
}
→ client.Nodes.Search(tenantGuid, graphGuid, searchRequest)
→ Transform results to ArangoDB document format
```

#### Graph Traversal

**AQL:**
```aql
FOR v, e, p IN 1..3 OUTBOUND "Person/alice" KNOWS
  RETURN v
```

**LiteGraph operations:**
```
1. Resolve "Person/alice" → nodeGuid
2. BFS/DFS traversal:
   depth 1: client.Nodes.GetNeighbors(tenantGuid, graphGuid, nodeGuid, edgeLabel: "KNOWS")
   depth 2: for each neighbor, GetNeighbors again
   depth 3: repeat
3. Deduplicate vertices
4. Return as ArangoDB result set
```

#### Shortest Path

**AQL:**
```aql
FOR v, e IN OUTBOUND SHORTEST_PATH "Person/alice" TO "Person/bob" KNOWS
  RETURN v
```

**LiteGraph operations:**
```
1. Resolve "Person/alice" → sourceGuid, "Person/bob" → targetGuid
2. client.Routes.GetRoute(tenantGuid, graphGuid, RouteRequest {
       From: sourceGuid,
       To: targetGuid,
       Algorithm: Dijkstra
   })
3. Return path vertices in ArangoDB format
```

#### Insert Document

**AQL:**
```aql
INSERT { _key: "charlie", name: "Charlie", age: 35 } INTO Person
```

**LiteGraph operations:**
```
client.Nodes.Create(new Node {
    TenantGUID: tenantGuid,
    GraphGUID: graphGuid,
    Labels: ["Person"],
    Tags: { "_arango_key": "charlie" },
    Data: { "name": "Charlie", "age": 35 }
})
```

#### Update Document

**AQL:**
```aql
UPDATE "charlie" WITH { age: 36 } IN Person
```

**LiteGraph operations:**
```
1. Resolve "charlie" → nodeGuid
2. Read existing: client.Nodes.Read(tenantGuid, graphGuid, nodeGuid)
3. Merge Data: existing.Data.age = 36
4. client.Nodes.Update(mergedNode)
```

#### Aggregation

**AQL:**
```aql
FOR p IN Person
  COLLECT city = p.city WITH COUNT INTO count
  RETURN { city, count }
```

**LiteGraph operations:**
```
1. client.Nodes.ReadMany(tenantGuid, graphGuid, labels: ["Person"])
2. Post-process: group by Data.city, count per group
3. Return as AQL result format
```

### 4.6 Authentication Mapping

| ArangoDB Auth | LiteGraph Auth | Mapping |
|---------------|----------------|---------|
| Basic Auth (username/password) | Credential auth | Map to `x-email` / `x-password` on LiteGraphClient. |
| JWT Token (`/_open/auth`) | Bearer token | ArangoDB JWT → LiteGraph bearer token exchange. |
| Database-level access | Tenant isolation | ArangoDB `_system` db = admin tenant; user databases = user tenants. |
| User management (`/_api/user`) | — | Adapter maintains its own user-to-tenant mapping; not passed to LiteGraph. |

**Database-to-tenant mapping:**
- `/_db/{database}/...` prefix → resolve database name to tenant GUID via configuration map
- Default database `_system` maps to a configured admin tenant
- Creating a new database (`POST /_api/database`) creates a new LiteGraph tenant

### 4.7 Known Limitations

| ArangoDB Feature | Limitation | Reason |
|------------------|------------|--------|
| **ArangoSearch (full-text)** | Not supported | LiteGraph has no full-text search engine; only exact match and ExpressionTree filtering |
| **Geo-spatial Indexes** | Not supported | No geo-spatial primitives in LiteGraph |
| **Persistent Indexes (hash, skiplist)** | Not applicable | LiteGraph uses its own indexing; adapter ignores index creation requests |
| **Transactions (multi-document)** | Not supported | LiteGraph has no transaction support; operations are individually atomic |
| **Foxx Microservices** | Not supported | No server-side JavaScript execution environment |
| **Satellite Collections** | Not applicable | Single-node architecture |
| **SmartGraphs / Enterprise features** | Not supported | No distributed computing layer |
| **AQL Functions (custom)** | Not supported | No UDF execution environment |
| **AQL COLLECT with complex aggregations** | Partial | Simple COUNT, SUM, AVG, MIN, MAX via post-processing; AGGREGATE and INTO with complex expressions not supported |
| **Document `_rev` for optimistic locking** | Emulated | `_rev` generated from timestamp; If-Match / If-None-Match headers supported but collision detection is approximate |
| **Stream Transactions** | Not supported | No streaming transaction support in LiteGraph |

### 4.8 Implementation Phases

#### Phase 1: Document & Collection API (P0)
- [ ] Project scaffold: `LiteGraph.ArangoAdapter` .NET project with Watson Webserver on port 8529
- [ ] LiteGraphClient connection setup and configuration
- [ ] ArangoDB key (`_key`, `_id`, `_rev`) ↔ GUID mapping system
- [ ] Collection endpoints (list, create, drop, count, info)
- [ ] Document CRUD endpoints (`/_api/document/{collection}/...`)
- [ ] Database endpoints (current, list)
- [ ] Version endpoint
- [ ] Result format transformation (LiteGraph → ArangoDB JSON with `_key`, `_id`, `_rev`)
- [ ] Error response formatting (ArangoDB error code + message format)

**Estimated effort:** 3–4 weeks

#### Phase 2: Graph API (P0)
- [ ] Named graph management (`/_api/gharial` CRUD)
- [ ] Graph vertex endpoints (create, read, update, delete)
- [ ] Graph edge endpoints (create, read, update, delete)
- [ ] Vertex/edge collection listing within graphs
- [ ] Edge definition management

**Estimated effort:** 2–3 weeks

#### Phase 3: AQL Query Engine (P0–P1)
- [ ] AQL tokenizer and parser (subset: FOR, FILTER, RETURN, INSERT, UPDATE, REMOVE, REPLACE)
- [ ] FOR...IN collection → LiteGraph ReadMany/Search
- [ ] FILTER → ExpressionTree translation
- [ ] RETURN projection
- [ ] INSERT/UPDATE/REMOVE/REPLACE → CRUD operations
- [ ] Cursor management (create, fetch next, dispose)
- [ ] LIMIT and SORT support

**Estimated effort:** 4–6 weeks

#### Phase 4: Graph Traversal & Paths (P1)
- [ ] FOR...IN OUTBOUND/INBOUND/ANY traversal → recursive neighbor lookup
- [ ] Depth range support (1..N)
- [ ] SHORTEST_PATH → RouteRequest
- [ ] Traversal endpoint (`/_api/traversal`)
- [ ] Path result formatting

**Estimated effort:** 3–4 weeks

#### Phase 5: Bulk Operations & Advanced (P1–P2)
- [ ] Bulk document operations (insert, replace, delete arrays)
- [ ] Simple query endpoints (`/_api/simple/all`, `/_api/simple/by-example`)
- [ ] Import/export endpoints
- [ ] Index management (vector index mapping)
- [ ] Database creation (tenant creation)
- [ ] AQL COLLECT / aggregation post-processing

**Estimated effort:** 3–4 weeks

#### Phase 6: Authentication & Polish (P2)
- [ ] JWT auth endpoint (`/_open/auth`)
- [ ] User management stubs (`/_api/user`)
- [ ] Database-to-tenant mapping configuration
- [ ] Query explain endpoint
- [ ] Server task stubs

**Estimated effort:** 1–2 weeks

**ArangoDB Adapter Total Estimated Effort: 16–23 weeks**

### 4.9 Testing Strategy

| Test Layer | Approach |
|------------|----------|
| **Unit Tests** | Test AQL parsing, key mapping, result transformation, and cursor management in isolation |
| **Integration Tests** | Run adapter against embedded LiteGraph; exercise all REST endpoints |
| **Compatibility Tests** | Use python-arango client library to exercise the adapter; compare responses to ArangoDB reference responses |
| **Graph Traversal Tests** | Build known graph topologies; verify traversal results match ArangoDB semantics |
| **Cursor Tests** | Verify pagination, batch sizing, and cursor cleanup |
| **Load Tests** | Bulk insert 100K documents; AQL query throughput benchmarks |

---

## 5. Neo4j Adapter

### 5.1 Overview

**Neo4j** is the most widely adopted graph database, using the **Cypher** query language and supporting both **HTTP API** and **Bolt** binary protocol access.

**Why LiteGraph should support it:**
- Neo4j has the largest graph database user base and ecosystem
- Cypher is the most widely-known graph query language (also adopted by openCypher standard)
- Broad tool support: Neo4j Browser, Bloom, Desktop, and dozens of driver libraries
- Organizations migrating from Neo4j represent the largest potential LiteGraph adoption opportunity
- Supporting Cypher makes LiteGraph accessible to the widest audience of graph database developers

**Protocols:** HTTP REST API + Bolt binary protocol
**Query Language:** Cypher
**Standard Ports:** 7474 (HTTP), 7687 (Bolt)

### 5.2 Data Model Mapping

| Neo4j Concept | LiteGraph Concept | Notes |
|---------------|-------------------|-------|
| Database | Tenant + Graph | Neo4j databases map to a LiteGraph tenant; default database maps to a default graph within that tenant. |
| Node | Node | Direct mapping. Neo4j nodes have labels and properties. |
| Relationship | Edge | Direct mapping. Neo4j relationships have a type, direction, and properties. |
| Label(s) | Labels (List\<string\>) | Direct mapping. Neo4j nodes can have multiple labels; so can LiteGraph nodes. |
| Relationship Type | Edge Labels[0] | Neo4j relationships have exactly one type; maps to first edge label. |
| Properties | Data (JSON) | Neo4j node/relationship properties map to LiteGraph Data JSON object. |
| Element ID / `id()` | GUID | Neo4j 5.x element IDs are strings; adapter generates compatible element IDs from GUIDs. |
| Legacy integer ID | — | Neo4j 4.x integer IDs; adapter generates sequential IDs mapped to GUIDs. |
| — | Tenant | No Neo4j equivalent beyond database separation. |
| — | Tags | Mapped to special properties (prefixed `_lg_tag_`), hidden from Cypher results by default. |
| — | Vectors | No native Neo4j vector type (pre-5.x). Neo4j 5.x vector index maps to LiteGraph HNSW. |
| — | Cost (Edge) | No direct Neo4j equivalent; exposed as a relationship property `_cost`. |
| Constraint | — | Neo4j uniqueness/existence constraints have no LiteGraph equivalent. |
| Index | — | Neo4j B-tree/text indexes. Vector indexes map to HNSW. |
| APOC Procedures | — | Extensive plugin library; not supported. |

**Bridging gaps:**
- **Element ID generation:** Generate Neo4j 5.x-compatible element IDs: `4:{database_id}:{sequential_number}`. Maintain a GUID ↔ element ID bidirectional map.
- **Relationship type vs labels:** Neo4j relationships have exactly one type. Use edge Labels[0] as the type; additional labels stored but not visible via Cypher.
- **Property types:** Neo4j supports typed properties (int, float, string, boolean, arrays, temporal, spatial). Adapter infers types from LiteGraph Data JSON values.
- **Database-to-graph mapping:** Neo4j databases map to tenant+graph pairs. `neo4j` (default database) maps to a configured default graph.

### 5.3 Architecture

```
┌──────────────────────────────────────┐
│        Neo4j Clients                 │
│  (Browser, Desktop, Drivers, Bloom)  │
└────────────┬─────────┬───────────────┘
             │         │
        HTTP │    Bolt │
       :7474 │   :7687 │
┌────────────▼─────────▼───────────────┐
│    LiteGraph.Neo4jAdapter            │
│    (Separate .NET webserver)         │
│                                      │
│  ┌───────────┐  ┌─────────────────┐  │
│  │ HTTP API  │  │ Bolt Protocol   │  │
│  │ Handler   │  │ Handler         │  │
│  │ (:7474)   │  │ (:7687)         │  │
│  └─────┬─────┘  └───────┬─────────┘  │
│        │                │            │
│  ┌─────▼────────────────▼──────────┐ │
│  │  Cypher Parser & Translator     │ │
│  └─────────────┬───────────────────┘ │
│  ┌─────────────▼───────────────────┐ │
│  │  Query AST → LiteGraph Ops     │ │
│  │  Result Transformer             │ │
│  │  Schema Registry                │ │
│  │  Element ID Manager             │ │
│  └─────────────┬───────────────────┘ │
└────────────────┼─────────────────────┘
                 │ LiteGraphClient
┌────────────────▼─────────────────────┐
│         LiteGraph Core               │
│    (SQLite / In-Memory Store)        │
└──────────────────────────────────────┘
```

**Project:** `LiteGraph.Neo4jAdapter`
**Ports:** 7474 (HTTP), 7687 (Bolt)
**Connection to core:** `LiteGraphClient` pointed at LiteGraph.Server endpoint, or embedded `SqliteGraphRepository`

### 5.4 API Endpoints / Protocol Surface

#### P0 — Must-Have for Basic Compatibility

**HTTP API:**

| Endpoint | Method | Description | LiteGraph Mapping |
|----------|--------|-------------|-------------------|
| `/db/{database}/tx/commit` | POST | Execute Cypher (auto-commit) | Cypher parse → LiteGraph ops |
| `/db/{database}/tx` | POST | Begin transaction | Create transaction context (limited support) |
| `/db/{database}/tx/{id}` | POST | Execute in transaction | Cypher parse → LiteGraph ops |
| `/db/{database}/tx/{id}/commit` | POST | Commit transaction | Finalize operation batch |
| `/db/{database}/tx/{id}` | DELETE | Rollback transaction | Discard pending operations |
| `/` | GET | Discovery endpoint | Return adapter metadata |
| `/db/{database}` | GET | Database info | Tenant + graph metadata |

**Bolt Protocol:**

| Message | Description | LiteGraph Mapping |
|---------|-------------|-------------------|
| `HELLO` | Client handshake with auth | Authenticate → bearer token |
| `LOGON` | Authentication (Neo4j 5.x) | Authenticate → bearer token |
| `LOGOFF` | Deauthenticate | Clear session |
| `RUN` | Execute Cypher statement | Cypher parse → LiteGraph ops |
| `PULL` | Fetch results | Return result set |
| `DISCARD` | Discard remaining results | Release resources |
| `BEGIN` | Begin transaction | Create transaction context |
| `COMMIT` | Commit transaction | Finalize operations |
| `ROLLBACK` | Rollback transaction | Discard pending operations |
| `RESET` | Reset connection | Clear session state |
| `GOODBYE` | Close connection | Clean up |
| `ROUTE` | Get routing table | Return single server (self) |

#### P1 — Important

**HTTP API:**

| Endpoint | Method | Description | LiteGraph Mapping |
|----------|--------|-------------|-------------------|
| `/db/{database}/tx/commit` (with parameters) | POST | Parameterized Cypher | Cypher parse with parameter substitution |
| `/db/system/cluster/overview` | GET | Cluster info | Return single-node info |
| `/db/{database}/labels` | GET | List all labels | Distinct node labels |
| `/db/{database}/relationship/types` | GET | List relationship types | Distinct edge labels |
| `/db/{database}/schema/index` | GET | List indexes | Vector index info |
| `/db/{database}/schema/constraint` | GET | List constraints | Empty (no constraint support) |
| `/user/{username}` | GET/POST/PUT/DELETE | User management | Stub responses |

**Bolt Protocol (additional):**

| Message | Description | LiteGraph Mapping |
|---------|-------------|-------------------|
| `TELEMETRY` | Usage metrics | Ignored / acknowledge |

#### P2 — Nice-to-Have

| Endpoint | Method | Description | LiteGraph Mapping |
|----------|--------|-------------|-------------------|
| `/db/{database}/tx/commit` (EXPLAIN/PROFILE) | POST | Query plan | Return mapped operations description |
| `/db/{database}/stats` | GET | Database statistics | Graph statistics |
| `/dbms/components` | GET | Server components | Adapter component list |
| `/dbms/config` | GET | Server configuration | Adapter configuration |
| `/db/{database}/index` | POST/DELETE | Index management | Vector index create/drop |

### 5.5 Query Language Translation (Cypher)

#### Node Lookup by Label

**Cypher:**
```cypher
MATCH (p:Person) RETURN p
```

**LiteGraph operations:**
```
SearchRequest {
    Labels: ["Person"],
    IncludeData: true
}
→ client.Nodes.Search(tenantGuid, graphGuid, searchRequest)
→ Transform each Node to Neo4j node format with labels, properties, elementId
```

#### Node Lookup with WHERE

**Cypher:**
```cypher
MATCH (p:Person) WHERE p.age > 30 AND p.city = "Seattle" RETURN p
```

**LiteGraph operations:**
```
SearchRequest {
    Labels: ["Person"],
    Expr: ExpressionTree {
        And(
            GreaterThan("$.age", 30),
            Equals("$.city", "Seattle")
        )
    }
}
→ client.Nodes.Search(tenantGuid, graphGuid, searchRequest)
```

#### Relationship Traversal

**Cypher:**
```cypher
MATCH (a:Person {name: "Alice"})-[:KNOWS]->(b:Person) RETURN b
```

**LiteGraph operations:**
```
1. client.Nodes.Search(tenantGuid, graphGuid, SearchRequest {
       Labels: ["Person"],
       Expr: Equals("$.name", "Alice")
   })
2. For matched node(s):
   client.Edges.GetEdgesOut(tenantGuid, graphGuid, nodeGuid)
   → filter by edge label "KNOWS"
3. For each matching edge:
   client.Nodes.Read(tenantGuid, graphGuid, edge.To)
   → filter by label "Person"
4. Return target nodes
```

#### Variable-Length Path

**Cypher:**
```cypher
MATCH (a:Person {name: "Alice"})-[:KNOWS*1..3]->(b:Person) RETURN b
```

**LiteGraph operations:**
```
1. Resolve Alice → nodeGuid
2. Iterative BFS up to depth 3:
   depth 1: GetEdgesOut(nodeGuid) → filter "KNOWS" → collect target nodes
   depth 2: for each depth-1 node, GetEdgesOut → filter "KNOWS" → collect
   depth 3: repeat
3. Filter results by label "Person"
4. Deduplicate and return
```

#### Shortest Path

**Cypher:**
```cypher
MATCH p = shortestPath((a:Person {name: "Alice"})-[:KNOWS*]-(b:Person {name: "Bob"}))
RETURN p
```

**LiteGraph operations:**
```
1. Resolve Alice → sourceGuid, Bob → targetGuid
2. client.Routes.GetRoute(tenantGuid, graphGuid, RouteRequest {
       From: sourceGuid,
       To: targetGuid,
       Algorithm: Dijkstra
   })
3. Format as Neo4j Path object (nodes + relationships)
```

#### Create Node

**Cypher:**
```cypher
CREATE (p:Person {name: "Charlie", age: 35})
RETURN p
```

**LiteGraph operations:**
```
client.Nodes.Create(new Node {
    TenantGUID: tenantGuid,
    GraphGUID: graphGuid,
    Labels: ["Person"],
    Data: { "name": "Charlie", "age": 35 }
})
→ Generate elementId, return in Neo4j format
```

#### Create Relationship

**Cypher:**
```cypher
MATCH (a:Person {name: "Alice"}), (b:Person {name: "Bob"})
CREATE (a)-[:KNOWS {since: 2020}]->(b)
```

**LiteGraph operations:**
```
1. Resolve Alice → aGuid, Bob → bGuid
2. client.Edges.Create(new Edge {
       TenantGUID: tenantGuid,
       GraphGUID: graphGuid,
       From: aGuid,
       To: bGuid,
       Labels: ["KNOWS"],
       Data: { "since": 2020 }
   })
```

#### Update Properties

**Cypher:**
```cypher
MATCH (p:Person {name: "Alice"})
SET p.age = 31, p.city = "Portland"
RETURN p
```

**LiteGraph operations:**
```
1. Search for Alice → node
2. Merge properties: node.Data.age = 31, node.Data.city = "Portland"
3. client.Nodes.Update(node)
```

#### Delete

**Cypher:**
```cypher
MATCH (p:Person {name: "Charlie"})
DETACH DELETE p
```

**LiteGraph operations:**
```
1. Search for Charlie → nodeGuid
2. client.Edges.DeleteAllForNode(tenantGuid, graphGuid, nodeGuid) — detach
3. client.Nodes.Delete(tenantGuid, graphGuid, nodeGuid)
```

#### Aggregation

**Cypher:**
```cypher
MATCH (p:Person) RETURN p.city AS city, count(*) AS count ORDER BY count DESC
```

**LiteGraph operations:**
```
1. client.Nodes.Search(tenantGuid, graphGuid, SearchRequest { Labels: ["Person"] })
2. Post-process: group by Data.city, count per group
3. Sort by count descending
4. Return as tabular result
```

### 5.6 Authentication Mapping

| Neo4j Auth | LiteGraph Auth | Mapping |
|------------|----------------|---------|
| Basic Auth (HTTP) | Credential auth | `Authorization: Basic base64(user:pass)` → `x-email` / `x-password` on LiteGraphClient |
| Bolt HELLO/LOGON | Bearer token | Extract credentials from HELLO message → authenticate → session token |
| Database selection | Tenant + Graph | `neo4j` (default) → default tenant+graph; named databases → configured tenant+graph pairs |
| Native auth (neo4j/password) | Credential auth | Map neo4j username/password to LiteGraph credentials |
| No auth | — | Adapter can run in no-auth mode for development |

**Database-to-tenant mapping:**
- `system` database → admin tenant (read-only system info)
- `neo4j` (default) → configured default tenant + default graph
- Named databases → tenant GUID lookup from configuration map
- Creating a new database → creates new LiteGraph tenant + graph

### 5.7 Known Limitations

| Neo4j Feature | Limitation | Reason |
|---------------|------------|--------|
| **Full Cypher Language** | Partial support | Only a subset of Cypher is supported; complex patterns, OPTIONAL MATCH, UNWIND, CALL, and procedural extensions are not supported |
| **APOC Procedures** | Not supported | No plugin execution environment |
| **Transactions (ACID)** | Not supported | LiteGraph has no multi-operation transaction support; BEGIN/COMMIT are acknowledged but operations are individually atomic |
| **Causal Clustering** | Not applicable | Single-node architecture; ROUTE message returns self |
| **Property type enforcement** | Best effort | Neo4j enforces property types; LiteGraph stores arbitrary JSON. Type coercion is attempted but not guaranteed. |
| **Temporal types** | Partial | Neo4j temporal types (Date, DateTime, Duration) stored as ISO strings in LiteGraph Data |
| **Spatial types** | Not supported | Neo4j Point type has no LiteGraph equivalent |
| **Schema constraints** | Not supported | Uniqueness and existence constraints cannot be enforced in LiteGraph |
| **Full-text search** | Not supported | Neo4j full-text indexes have no LiteGraph equivalent |
| **OPTIONAL MATCH** | Not supported | Would require outer-join semantics not available in LiteGraph |
| **MERGE** | Partial | MERGE (create-or-match) supported for simple patterns; complex MERGE with ON CREATE/ON MATCH is limited |
| **Subqueries (CALL {})** | Not supported | No subquery execution context |
| **List comprehensions** | Not supported | Complex list operations in RETURN clauses not supported |
| **Map projections** | Partial | Simple property projection supported; complex map operations not supported |
| **Bolt chunking** | Simplified | Large result streaming via Bolt uses simplified chunking |

### 5.8 Implementation Phases

#### Phase 1: HTTP API with Basic Cypher (P0)
- [ ] Project scaffold: `LiteGraph.Neo4jAdapter` .NET project with Watson Webserver on port 7474
- [ ] Discovery endpoint (`/`) with Neo4j-compatible metadata
- [ ] Database info endpoint (`/db/{database}`)
- [ ] Transaction commit endpoint (`/db/{database}/tx/commit`) — auto-commit mode
- [ ] Cypher tokenizer and parser (subset: MATCH, WHERE, RETURN, LIMIT, SKIP, ORDER BY)
- [ ] Simple pattern matching: `(n:Label)`, `(n:Label {prop: value})`
- [ ] WHERE clause → ExpressionTree translation
- [ ] RETURN clause result projection
- [ ] Node/relationship result formatting (elementId, labels, properties)
- [ ] Element ID ↔ GUID mapping system
- [ ] Error response formatting (Neo4j error codes + messages)

**Estimated effort:** 5–6 weeks

#### Phase 2: Cypher CRUD & Relationships (P0)
- [ ] CREATE node patterns
- [ ] CREATE relationship patterns
- [ ] SET (property update)
- [ ] DELETE and DETACH DELETE
- [ ] REMOVE (property/label removal)
- [ ] Relationship pattern matching: `(a)-[:TYPE]->(b)`
- [ ] Bidirectional relationships: `(a)-[:TYPE]-(b)`
- [ ] Multiple MATCH clauses
- [ ] Parameterized queries (`$param` substitution)
- [ ] Transaction endpoints (BEGIN, COMMIT, ROLLBACK — simplified)

**Estimated effort:** 4–5 weeks

#### Phase 3: Bolt Protocol (P0)
- [ ] TCP listener on port 7687
- [ ] Bolt handshake (version negotiation: support Bolt 4.x and 5.x)
- [ ] PackStream serialization/deserialization
- [ ] HELLO / LOGON authentication flow
- [ ] RUN + PULL message handling (execute Cypher, return results)
- [ ] DISCARD message handling
- [ ] BEGIN / COMMIT / ROLLBACK (transaction stubs)
- [ ] RESET message handling
- [ ] GOODBYE / connection cleanup
- [ ] ROUTE message (single-server routing table)
- [ ] Error handling and FAILURE message formatting

**Estimated effort:** 5–7 weeks

#### Phase 4: Path & Traversal Queries (P1)
- [ ] Variable-length path patterns: `[:TYPE*1..3]`
- [ ] shortestPath() function → RouteRequest
- [ ] allShortestPaths() function
- [ ] Depth-limited BFS traversal engine
- [ ] Path result formatting (Neo4j Path type)

**Estimated effort:** 3–4 weeks

#### Phase 5: Schema & Metadata Endpoints (P1)
- [ ] Labels endpoint (`/db/{database}/labels`)
- [ ] Relationship types endpoint (`/db/{database}/relationship/types`)
- [ ] Schema index endpoint
- [ ] Schema constraint endpoint (stub)
- [ ] Cluster overview endpoint (single-node)
- [ ] User management stubs

**Estimated effort:** 1–2 weeks

#### Phase 6: Advanced Cypher & Polish (P2)
- [ ] MERGE (simple patterns)
- [ ] WITH clause (query chaining)
- [ ] UNWIND (simple list expansion)
- [ ] Aggregation functions: count(), sum(), avg(), min(), max(), collect()
- [ ] DISTINCT keyword
- [ ] String functions: toString(), toUpper(), toLower(), trim(), replace(), substring()
- [ ] EXPLAIN / PROFILE stubs
- [ ] Bolt TELEMETRY handling
- [ ] Multiple database support

**Estimated effort:** 4–5 weeks

**Neo4j Adapter Total Estimated Effort: 22–29 weeks**

### 5.9 Testing Strategy

| Test Layer | Approach |
|------------|----------|
| **Unit Tests** | Test Cypher parsing, element ID mapping, PackStream serialization, result transformation |
| **Integration Tests** | Run adapter against embedded LiteGraph; test HTTP and Bolt endpoints with known data |
| **Compatibility Tests — HTTP** | Use Neo4j .NET driver in HTTP mode to exercise the adapter |
| **Compatibility Tests — Bolt** | Use Neo4j .NET driver in Bolt mode; verify handshake, query execution, and result formats |
| **Cypher Compliance** | Build a Cypher test suite from openCypher TCK (Technology Compatibility Kit); track pass/fail per feature |
| **Driver Matrix** | Test with official Neo4j drivers: .NET, Python (neo4j), Java, JavaScript (neo4j-driver) |
| **Load Tests** | Create graph with 100K nodes + 500K relationships; Cypher query throughput benchmarks over both protocols |
| **Browser Compatibility** | Verify Neo4j Browser can connect, run basic queries, and display results |

---

## 6. Overall Testing Strategy

### 6.1 Test Infrastructure

| Component | Description |
|-----------|-------------|
| **Test Harness** | Shared test runner that starts adapter + embedded LiteGraph, seeds data, runs test suites |
| **Reference Data Set** | Standard graph dataset (e.g., movie graph, social network) loaded into LiteGraph for all adapters |
| **Response Comparator** | Tool that compares adapter responses to reference database responses (field-by-field, ignoring IDs) |
| **CI Pipeline** | Automated test execution on every PR; per-adapter test isolation |

### 6.2 Test Categories

| Category | Purpose | Tools |
|----------|---------|-------|
| **Smoke Tests** | Verify adapter starts, accepts connections, responds to health checks | curl, custom test client |
| **CRUD Tests** | Verify create, read, update, delete for all entity types | Native client libraries |
| **Query Tests** | Verify query language parsing and result correctness | Test suite with expected results |
| **Pagination Tests** | Verify cursor/continuation-token behavior | Automated iteration tests |
| **Auth Tests** | Verify authentication flows and tenant isolation | Multiple credential sets |
| **Error Tests** | Verify error codes and messages match target API format | Invalid input scenarios |
| **Concurrency Tests** | Verify thread safety under concurrent load | Multi-threaded test harness |
| **Migration Tests** | Verify data round-trip: export from real DB → import to LiteGraph via adapter → query and verify | Database dump + restore scripts |

### 6.3 Compatibility Metrics

Track compatibility as a percentage of the target API surface that is fully functional:

| Metric | Target (Phase 1) | Target (Final) |
|--------|-------------------|----------------|
| **TigerGraph RESTPP endpoints** | 70% | 90% |
| **ArangoDB HTTP API endpoints** | 60% | 85% |
| **Neo4j HTTP API** | 50% | 80% |
| **Neo4j Bolt Protocol** | 50% | 80% |
| **GSQL subset** | 40% | 70% |
| **AQL subset** | 40% | 70% |
| **Cypher subset** | 30% | 65% |

### 6.4 Test Data Seeds

```
Standard test graph:
- 6 vertex types: Person, Company, City, Product, Order, Category
- 8 edge types: KNOWS, WORKS_AT, LIVES_IN, PURCHASED, CONTAINS, BELONGS_TO, MANAGES, REVIEWED
- ~1,000 nodes, ~5,000 edges
- Vector embeddings on Person and Product nodes (128-dimensional)
- Rich property data (strings, numbers, booleans, arrays, nested objects)
```

---

## Appendix: Effort Summary

| Component | Estimated Effort |
|-----------|-----------------|
| Shared Infrastructure | 6–10 weeks |
| TigerGraph Adapter | 11–15 weeks |
| ArangoDB Adapter | 16–23 weeks |
| Neo4j Adapter | 22–29 weeks |
| **Total** | **55–77 weeks** |

> **Note:** These estimates assume a single developer working full-time. With parallel development (one developer per adapter after shared infrastructure is complete), the calendar time could be reduced to approximately 28–35 weeks.

---

## Progress Tracking

Use this section to record implementation progress:

| Date | Component | Phase | Status | Notes |
|------|-----------|-------|--------|-------|
| | | | | |
