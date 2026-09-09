# LiteGraph Graph Algorithms

LiteGraph v9.0.0 adds native graph algorithms computed over the whole graph, plus a portable projection export/import path for running algorithms in external engines such as `rustworkx` or NetworkX.

Algorithms are available through the embedded client (`client.Algorithm`), the REST API, the MCP tool catalog, and all three SDKs (C#, JavaScript, Python).

## How it works

Algorithms load the entire node and edge set of a single graph into a compact in-memory adjacency (compressed sparse row) built from streaming enumeration, then compute over it. This is deliberately separate from the DSL's per-hop traversal, which is optimized for bounded pattern matching rather than whole-graph analytics.

Because the whole graph is loaded into memory, a configurable ceiling (`MaxNodes` / `MaxEdges`, defaults 1,000,000 / 10,000,000) rejects graphs that are too large rather than risking memory exhaustion. For graphs beyond the ceiling — or for algorithms LiteGraph does not implement natively — use the **projection export** to compute externally and **import** the results back (see below).

## Supported algorithms

| Algorithm | `AlgorithmType` | Output | Default write-back property |
|---|---|---|---|
| Degree centrality | `DegreeCentrality` | `Score` = total degree; `EdgesIn`/`EdgesOut` populated | `degree` |
| PageRank | `PageRank` | `Score` = rank (sums to 1) | `pagerank` |
| Closeness centrality | `ClosenessCentrality` | `Score` = Wasserman-Faust closeness | `closeness` |
| Eigenvector centrality | `EigenvectorCentrality` | `Score` = eigenvector centrality (unit L2 norm) | `eigenvector` |
| Betweenness centrality | `BetweennessCentrality` | `Score` = Brandes betweenness (normalized) | `betweenness` |
| Weakly connected components | `WeaklyConnectedComponents` | `Community` = component id | `component` |
| Strongly connected components | `StronglyConnectedComponents` | `Community` = component id | `scc` |
| Label propagation | `LabelPropagation` | `Community` = community id | `community` |
| Louvain | `Louvain` | `Community` = community id | `louvain` |
| Clustering coefficient | `ClusteringCoefficient` | `Score` = local clustering coefficient | `clustering` |
| k-core | `KCore` | `Score` = core number | `kcore` |

Centrality and PageRank results are returned sorted by score descending; community/component results are grouped by identifier. PageRank, degree, and strongly connected components are directed; closeness, eigenvector, and betweenness centrality, clustering coefficient, k-core, Louvain, label propagation, and weakly connected components operate on the undirected view.

## Request parameters

`GraphAlgorithmRequest`:

| Field | Type | Default | Applies to |
|---|---|---|---|
| `AlgorithmType` | enum | `DegreeCentrality` | all |
| `DampingFactor` | double (0–1) | 0.85 | PageRank |
| `MaxIterations` | int (≥1) | 100 | PageRank, label propagation, eigenvector |
| `Tolerance` | double (≥0) | 0.000001 | PageRank, eigenvector |
| `TreatAsUndirected` | bool | false | reserved |
| `MaxResults` | int? | null (all) | all |
| `WriteBack` | bool | false | all |
| `WriteBackProperty` | string? | null (algorithm default) | all |

## Result shape

`GraphAlgorithmResult` includes `Success`, `AlgorithmType`, `NodeCount`, `EdgeCount`, `Iterations`, `Converged`, `CommunityCount` (community/component algorithms), `ComputeMs`, `LoadMs`, `WrittenBack`, `WriteBackProperty`, and `Nodes` — a list of `{ NodeGUID, Name, Score, EdgesIn, EdgesOut, Community }`.

## Write-back

Setting `WriteBack: true` materializes each node's result into that node's JSON `Data` under `WriteBackProperty` (or the algorithm default). Once written, results are queryable through the DSL, e.g.:

```
MATCH (n) WHERE n.data.pagerank > 0.05 RETURN n ORDER BY n.data.pagerank DESC
```

Write-back mutates nodes and therefore requires the **write** scope; compute-only requests require only **read** (see RBAC below).

## REST API

```
POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/algorithms
POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/algorithms/import
GET  /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/export/projection?format={NodeLinkJson|EdgeList|Graphml}&attributes={None|Meta|Full}
```

Run example:

```bash
curl -X POST http://localhost:8701/v1.0/tenants/$T/graphs/$G/algorithms \
  -H "Authorization: Bearer $TOKEN" -H "Content-Type: application/json" \
  -d '{"AlgorithmType":"PageRank","WriteBack":true}'
```

## Authorization

A new `Algorithm` authorization resource type governs these routes. Compute (`POST /algorithms` without write-back) and export (`GET /export/projection`) require **read** scope; write-back and import (`POST /algorithms/import`) require **write** scope. Built-in roles: `Viewer` can run read-only algorithms; `Editor` and `GraphAdmin` can run algorithms and write results back. See [RBAC.md](RBAC.md).

## MCP tools

`algorithm/run`, `algorithm/export`, and `algorithm/import` are exposed over HTTP, TCP, and WebSocket. They proxy the REST endpoints under the caller's RBAC. See [MCP_API.md](MCP_API.md).

## Graph projection export / import (external compute)

Export a graph as a portable projection, compute in an external engine, and import the per-node results back.

Formats:
- **NodeLinkJson** (default) — loads directly into `networkx.node_link_graph`.
- **EdgeList** — CSV `source,target,weight`; the simplest feed for `rustworkx`.
- **Graphml** — portable XML for igraph/Gephi/NetworkX.

Attribute levels: `None` (structure only), `Meta` (adds name/labels/tags), `Full` (adds JSON data). Export streams, so graphs larger than the native compute ceiling can still be projected out.

### Round-trip example (rustworkx / NetworkX)

```python
import json, networkx as nx, litegraph_sdk as lg

lg.configure(endpoint="http://localhost:8701/", access_key="TOKEN", tenant_guid=T)

# 1) Export the graph as node-link JSON
text = lg.Algorithm.export_projection(graph_guid=G, export_format="NodeLinkJson", attributes="Meta")
data = json.loads(text)

# 2) Compute externally (any algorithm NetworkX/rustworkx offers)
Gx = nx.node_link_graph(data)
betweenness = nx.betweenness_centrality(Gx)

# 3) Import the results back onto nodes (queryable via the DSL afterward)
lg.Algorithm.import_results(
    {"Values": {node: {"betweenness": score} for node, score in betweenness.items()}},
    graph_guid=G,
)
```

Node identity is preserved by GUID across the round trip, so imported values land on the correct nodes.

## SDK entry points

- **C#**: `client.Algorithm.Run(...)`, `client.Algorithm.ExportGraph(...)`, `client.Algorithm.ImportResults(...)` (embedded); `sdk.Algorithm.*` (REST SDK).
- **JavaScript**: `sdk.runAlgorithm(graphGuid, request)`, `sdk.exportGraphProjection(graphGuid, { format, attributes })`, `sdk.importAlgorithmResults(graphGuid, request)`.
- **Python**: `Algorithm.run(request, graph_guid)`, `Algorithm.export_projection(graph_guid, export_format, attributes)`, `Algorithm.import_results(request, graph_guid)`.

## Node embeddings

When a tenant has an active **embedding endpoint** configured (the same PolyPrompt endpoints used by chat/RAG), you can generate a vector for every node from its content (name + JSON data) and store it as a node vector — which the HNSW index then makes searchable, so "find similar nodes" becomes an existing vector search.

```
POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/algorithms/embeddings
{ "MaxNodes": null, "SkipNodesWithVectors": true }
```

Requires **write** scope (it creates node vectors) and the chat/embedding feature enabled. If no active embedding endpoint is configured for the tenant, the request returns `400` with a clear message. SDK entry points: C# `sdk.Algorithm.GenerateEmbeddings(...)`, Python `Algorithm.generate_embeddings(...)`, dashboard "Generate embeddings" on the algorithms page. Embedding generation calls the external endpoint per node; run it deliberately on large graphs.

## Result caching

Set `UseCache: true` on a request to serve a cached result when the graph is unchanged and to cache a freshly computed result. Cache entries are keyed by graph, algorithm, and parameters, and are invalidated automatically when the graph's node or edge count changes. Structural rewrites that preserve both counts are not detected automatically — call `client.Algorithm.InvalidateCache(graphGuid)` after such changes. Caching is skipped for write-back runs. A served result has `FromCache: true`.

## Notes and limits

- The in-memory ceiling is a guardrail, not a scaling strategy; for very large graphs use projection export + external compute.
- Edge `cost` is not treated as an algorithm weight in v9.0.0; edges are unweighted (weight 1.0).

## DSL surface

Algorithms are also callable from the native query language:

```
CALL litegraph.algo.pagerank() RETURN guid, score
CALL litegraph.algo.louvain() RETURN guid, community
CALL litegraph.algo.betweenness() RETURN guid, score LIMIT 10
```

The procedure name's suffix selects the algorithm (`pagerank`, `degree`, `closeness`, `eigenvector`, `betweenness`, `wcc`/`weaklyconnectedcomponents`, `scc`/`stronglyconnectedcomponents`, `labelpropagation`, `louvain`, `clustering`, `kcore`). Supported `RETURN`/`YIELD` variables are `guid` (alias `node`/`n`/`nodeGuid`), `name`, `score`, `community` (alias `component`), `edgesIn`, `edgesOut`, and `result` (the full per-node object). `LIMIT` bounds the rows returned. The call runs against the query's graph with default parameters; for tuned parameters or write-back, use the typed API or REST route. Aggregates over algorithm output are not supported.
