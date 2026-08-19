# Subgraph Import and Export (JSONL), Streaming, and Dashboard Internationalization

This document is the implementation plan for filtered subgraph selection, streaming JSONL export and import (including merge-into-existing-graph and provider-agnostic whole-graph backup), and full dashboard internationalization, across every layer of LiteGraph: the core library, the REST server, the three SDKs, the MCP server, the dashboard, the Docker assets, the documentation set, and the test suites. It is written to be handed to a developer and executed section by section. It targets release **v7.1.0**.

The plan follows the conventions already in the repository. Where a comparable feature exists, the plan names it and mirrors it rather than inventing a new pattern. Read "What already exists" (§2) first — a large part of the interchange feature is assembly of primitives that are already present, not new graph machinery.

Two capabilities that were originally deferred are now first-class requirements: **streaming** (export and import must work on graphs too large to hold in memory, because the same path doubles as a provider-agnostic backup format), and **internationalization** (the whole dashboard, not just the new screens, is translated). Both are reflected throughout.

---

## 0. Branch and Docker versioning (do this first)

Before any code, create and switch to the working branch, then move the Docker assets to the new image tag so nothing is committed against `main` and so the compose files describe the release under construction.

1. `git checkout -b v7.1` from `main`. All work commits here.
2. In **`docker/compose.yaml`** and **`docker/factory/compose.yaml`**, change every LiteGraph image tag from `v7.0.0` to `v7.1.0`: `jchristn77/litegraph:v7.1.0` (two service definitions each), `jchristn77/litegraph-mcp:v7.1.0`, and `jchristn77/litegraph-ui:v7.1.0`. Leave `postgres`, `prometheus`, and `grafana` tags alone.
3. Sanity-check the sibling assets under `docker/` and `docker/factory/` — `litegraph.json`, `litegraph-mcp.json`, `factory/reset.sh`, `factory/reset.bat`, `update.bat`, `smoke.ps1` — for any hard-coded `7.0.0` string or version-tagged reference and bump those too. The smoke scripts (`docker/smoke.ps1`) should keep validating REST, metrics, MCP, UI, Prometheus, and Grafana; extend them at the end (§16) to also probe a JSONL export.
4. Commit: `chore: branch v7.1 and bump docker image tags to v7.1.0`.

Everything else in this plan lands on `v7.1` in the build order of §15.

---

## 1. Motivation and the four user journeys

LiteGraph can already extract a subgraph in a limited way and can already export a whole graph to GEXF, but there is no way to pull out a *filtered, directional* neighborhood, no way to move a piece of one graph into another, no line-oriented interchange format that survives a round trip, and no streaming path for graphs that do not fit in memory. Those gaps close four concrete workflows.

**Pull a subgraph out of a graph.** An operator picks a starting node, sets how far to walk and in which direction, and constrains the walk with filters — edge labels, edge tags, a maximum edge cost, node labels, node tags, and the existing `Data` expression filter. What comes back is a self-contained slice: the reachable nodes, the edges among them, and their labels, tags, vectors, and data. That slice is written to a JSONL file whose leading `#` lines carry human-readable metadata and are ignored on import.

**Merge that slice into a different graph.** The operator takes the exported file and imports it into an existing graph that already has its own nodes and edges. The importer has to decide what to do when a GUID in the file already exists in the target, keep edge endpoints valid when GUIDs are rewritten, and do the whole thing atomically so a failure halfway through does not leave a half-merged graph.

**Stand up a new graph from the slice.** Same file, different intent: create a brand-new graph (optionally in a different tenant) whose contents are the exported subgraph. Preserving the original GUIDs is usually desirable here, because nothing collides in an empty target.

**Back up and restore a whole graph, provider-agnostically.** A JSONL export of an *entire* graph is a portable, human-inspectable, append-friendly backup that does not depend on SQLite or PostgreSQL internals. A graph with millions of nodes must export and re-import without ever materializing the whole thing in memory. Streaming is what makes this journey real rather than aspirational, which is why it is built in from the start rather than bolted on.

Everything below serves those four journeys and nothing wider. Whole-*database* binary backup already exists (`Admin.Backup`) and stays as-is; the JSONL path is the logical, portable, per-graph complement to it.

---

## 2. What already exists (do not rebuild these)

The exploration that preceded this plan confirmed the following building blocks. The feature composes them.

- **Limited subgraph extraction.** `IGraphMethods.GetSubgraph(tenantGuid, graphGuid, nodeGuid, maxDepth, maxNodes, maxEdges, includeData, includeSubordinates, token)` returns a `SearchResult` (`{ List<Graph> Graphs; List<Node> Nodes; List<Edge> Edges; }`). The traversal runs down in the repository layer (`_Repo.Graph.GetSubgraph`) and supports only depth and count caps — **no direction, no label/tag/cost/expression filtering**. Keep it as-is for backward compatibility; do not extend it in place.
- **Filter-aware traversal primitives** on `IEdgeMethods`: `ReadEdgesFromNode`, `ReadEdgesToNode`, and `ReadNodeEdges`, each already accepting `List<string> labels`, `NameValueCollection tags`, and `Expr edgeFilter`, and each returning `IAsyncEnumerable<Edge>` (already streaming-friendly). On `INodeMethods`: `ReadChildren`, `ReadParents`, `ReadNeighbors`, `ReadByGuids`, `ReadMany` (also `IAsyncEnumerable`). These are the pieces the filtered walk is built from.
- **Whole-graph export precedent**: `GexfWriter` plus the `LiteGraphClient.RenderGraphAsGexf` / `ExportGraphToGexfFile` facade, streaming `client.Node.ReadMany` then `client.Edge.ReadMany`. Note that GEXF *builds the whole XML string in memory* — the JSONL writer must not; it writes line by line to a stream. There is **no GEXF import** — nothing to mirror for the import direction, which is genuinely new.
- **Bulk insert**: `Node.CreateMany` and `Edge.CreateMany`, each with a `BulkCreateReturnModeEnum` overload (`Full` hydrates subordinates after insert; `Minimal` skips that and is faster for large imports).
- **Existence pre-check**: `IBatchMethods.Existence(tenantGuid, graphGuid, ExistenceRequest, token)` → `ExistenceResult`, where `ExistenceRequest` carries `List<Guid> Nodes/Edges/Vectors` and `List<EdgeBetween> EdgesBetween`. This is how the importer detects collisions in batches instead of one-at-a-time.
- **Atomicity**: `ITransactionMethods` with `Transaction.Execute(tenantGuid, graphGuid, TransactionRequest, token)` and `CreateRequestBuilder()`. Graph-scoped, which is exactly the scope of an import.
- **Client-supplied GUIDs, no upsert.** Every model initializes `GUID`/`TenantGUID`/`GraphGUID` to `Guid.NewGuid()` but honors caller-set values; the SQLite/PostgreSQL inserts write them verbatim. There is **no `ON CONFLICT` / upsert** anywhere — re-inserting an existing GUID is a primary-key violation. Merge therefore has to be existence-check-then-create-or-update, in code, inside a transaction. This constraint drives the whole import design.
- **Serializer**: `LiteGraph.Serialization.Serializer` (System.Text.Json, `DefaultIgnoreCondition = WhenWritingNull`, custom converters for `NameValueCollection`, `Expr`, enums-as-strings, and the `yyyy-MM-ddTHH:mm:ss.ffffffZ` date format). Reachable as `LiteGraphClient.Serializer`. Reuse it for every JSONL line so the on-disk shape matches the REST shape.
- **Web server**: WatsonWebserver 7.1.0. It supports chunked transfer (`ctx.Response.ChunkedTransfer = true`, then `SendChunk(byte[])` / `SendFinalChunk(byte[])`) and exposes the raw request body stream (`ctx.Request.Data`) in addition to the buffered `DataAsString`/`DataAsBytes`. Streaming export and import use these.

One hard rule carried from the core review: repository SQL is assembled by string concatenation behind a per-provider `Sanitizer`. **The importer must go through the client/repository methods** (`CreateMany`, `Update`, `Transaction`) and must never compose SQL. No exceptions.

---

## 3. Scope and non-goals

In scope: filtered directional subgraph extraction; streaming JSONL serialization of a `SearchResult` or a whole graph; streaming JSONL export and import endpoints, SDK methods, MCP tools, and dashboard actions; JSONL import with create-new and merge-into-existing modes and a choice of GUID collision strategies; whole-graph JSONL backup/restore as a first-class use of the same path; **full internationalization of the dashboard** (existing screens and new ones); the Docker image-tag bump; the full test expansion; and every doc surface that mentions the feature, including a new `docs/MCP_API.md`.

Out of scope, stated so nobody gilds it: whole-*database* binary backup/restore (already exists as `Admin.Backup`), GraphML/CSV/Parquet formats, cross-format conversion, conflict *resolution* UIs beyond the strategy choice, translation of the C#/server logs or SDK exception text (dashboard-facing strings only), and right-to-left layout mirroring (the i18n framework should not preclude it later, but no RTL locale ships in v1).

---

## 4. The JSONL format

The format is the contract every layer agrees on, so it is specified before any code. It is deliberately line-oriented so that a multi-million-line backup streams in constant memory and so that ordinary tools (`grep`, `wc -l`, `head`, `split`) work on it.

### 4.1 Shape

A LiteGraph JSONL export is UTF-8 text, one record per line, LF-terminated (`\n`). The file has two kinds of lines:

- **Comment lines** begin with `#` and are ignored by the reader. They carry the human-readable metadata header. A `#` anywhere other than column zero is not special.
- **Record lines** are single-line JSON objects, each with a `type` discriminator.

A well-formed file:

```
# litegraph-jsonl v1
# kind: subgraph            (or: graph-backup)
# exported-utc: 2026-08-19T17:04:22.104630Z
# source-tenant: 00000000-0000-0000-0000-000000000000
# source-graph: 3f2b1c9a-...   (name: "Production Topology")
# selection: start=9a1f...,depth=2,direction=Both,maxNodes=0,maxEdges=0
# counts: graphs=1 nodes=42 edges=87
# generator: LiteGraph 7.1.0
{"type":"graph","object":{"GUID":"3f2b1c9a-...","TenantGUID":"...","Name":"Production Topology","Labels":[...],"Tags":{...},"Data":{...}}}
{"type":"node","object":{"GUID":"9a1f...","GraphGUID":"3f2b1c9a-...","Name":"web-01","Labels":["service"],"Tags":{"tier":"frontend"},"Data":{...},"Vectors":[...]}}
{"type":"edge","object":{"GUID":"c4...","GraphGUID":"3f2b1c9a-...","From":"9a1f...","To":"b2...","Cost":3,"Labels":["depends-on"],"Tags":{...},"Data":{...}}}
```

Rules the reader enforces:

- The first record line, if `type` is `graph`, is the source graph's own metadata (labels, tags, data, vector-index configuration). Optional on import; used only when creating a new graph and the caller supplied no graph metadata of their own.
- **Nodes are emitted before edges.** For a whole-graph backup and for a subgraph export the writer always emits the `graph` line, then all `node` lines, then all `edge` lines. The streaming importer relies on this ordering to keep memory bounded (§6). A scrambled file still imports correctly but forces the importer into its buffered fallback (§5.5), so tools that regenerate files should preserve the order.
- Labels, tags, vectors, and data ride *inside* the node/edge/graph object exactly as the REST API serializes them. No separate label/tag/vector lines. A line is self-contained, and its shape is identical to the object JSON the rest of the API already returns.
- Blank lines are ignored. A record line that is not valid JSON, or whose `type` is unknown, is handled per the import `OnError` policy (§5.5).

### 4.2 Why an envelope rather than a bare object

Each line is `{"type":"...","object":{...}}` rather than a bare `Node`/`Edge` with an extra field. The payload stays byte-for-byte identical to what `Serializer.SerializeJson(node)` already produces (same converters, same null-omission, same date format), and the discriminator can never collide with a user's `Data` keys. The envelope is `JsonlRecord` (§5.2).

### 4.3 Content type and file extension

Over HTTP the media type is `application/x-ndjson`; downloads use the extension `.jsonl`. Add `Constants.NdjsonContentType = "application/x-ndjson"` beside the existing `XmlContentType`.

---

## 5. Core library changes (`src/LiteGraph`)

All new code follows the repository's code style: namespace-scoped usings (system first, alphabetical), the five standard regions in files over ~500 lines, `_PascalCase` private fields, XML docs on every public member, `.ConfigureAwait(false)`, a `CancellationToken` on every async method, no `var`, no tuples, guard clauses first, specific exception types with `<exception>` docs, and no `Console.WriteLine`. One class or one enum per file. Every method that streams exposes an `IAsyncEnumerable`/stream variant, per the async-enumerable rule.

### 5.1 New enums

Each in its own file under `src/LiteGraph/`.

- **`GraphTraversalDirectionEnum.cs`** — `Outbound`, `Inbound`, `Both`. Chooses whether the walk follows edges away from the frontier (`ReadEdgesFromNode`), into it (`ReadEdgesToNode`), or both (`ReadNodeEdges`).
- **`GraphImportModeEnum.cs`** — `CreateNew`, `MergeIntoExisting`.
- **`GraphImportGuidStrategyEnum.cs`** — `Preserve` (keep original GUIDs; a collision is an error), `Regenerate` (fresh GUIDs everywhere, with references remapped), `Skip` (leave an existing record untouched, import the rest), `Overwrite` (update an existing record in place, create the missing ones). `Regenerate` defaults for `MergeIntoExisting`; `Preserve` defaults for `CreateNew`.
- **`JsonlRecordTypeEnum.cs`** — `Graph`, `Node`, `Edge`.
- **`GraphImportErrorPolicyEnum.cs`** — `Abort` (first bad line rolls back) or `Skip` (bad lines collected into warnings; good ones proceed).

### 5.2 New model classes

- **`SubgraphExtractionRequest.cs`** — the rich selection object. Members, with validation in setters where a range applies:
  - `Guid TenantGUID`, `Guid GraphGUID`.
  - `List<Guid> StartNodeGUIDs` — one or more roots (single-root convenience at the facade, §5.7).
  - `int MaxDepth` — default `2`, minimum `0` (0 = just the start nodes and the edges among them). Negative → `ArgumentOutOfRangeException`.
  - `GraphTraversalDirectionEnum Direction` — default `Both`.
  - `int MaxNodes` — default `0` (unlimited). `int MaxEdges` — default `0` (unlimited).
  - `List<string> EdgeLabels`, `NameValueCollection EdgeTags`, `Expr EdgeFilter` — constrain which edges may be *traversed and included*.
  - `int? MaxEdgeCost` — when set, an edge with `Cost` above it is not traversed. Minimum `0`.
  - `List<string> NodeLabels`, `NameValueCollection NodeTags`, `Expr NodeFilter` — constrain which neighbor nodes may be *included and expanded*. Start nodes are always included even if they fail the node filter (documented; otherwise a self-filtering root yields nothing).
  - `bool IncludeData` (default `false`), `bool IncludeSubordinates` (default `false`).
  - XML docs must state every default, minimum, and the start-node exemption.
- **`GraphImportRequest.cs`** — options that accompany a JSONL body:
  - `GraphImportModeEnum Mode`; `GraphImportGuidStrategyEnum GuidStrategy`; `GraphImportErrorPolicyEnum OnError`.
  - `Guid? TargetGraphGUID` — required for `MergeIntoExisting`; ignored for `CreateNew`.
  - `Graph NewGraph` — optional metadata for `CreateNew`; null falls back to the file's `graph` line, then a minimal default.
  - `bool IncludeVectors` — default `true`.
  - `BulkCreateReturnModeEnum ReturnMode` — default `Minimal`.
  - `int BatchSize` — default `1000`, minimum `1`; how many nodes/edges per `CreateMany` call while streaming.
- **`GraphImportResult.cs`** — `bool Success`; `Guid TenantGUID`; `Guid GraphGUID`; counts `NodesCreated/NodesUpdated/NodesSkipped`, `EdgesCreated/EdgesUpdated/EdgesSkipped`, `GraphsCreated`; `List<string> Warnings`; `Dictionary<Guid, Guid> GuidMap` (populated only under `Regenerate`); `int LinesRead`, `int LinesIgnored`. No tuples anywhere in this type.
- **`JsonlRecord.cs`** — the line envelope: `JsonlRecordTypeEnum Type`; the raw JSON of the payload retained so `Graph AsGraph()`, `Node AsNode()`, `Edge AsEdge()` re-parse via the shared `Serializer`.
- **`JsonlExportMetadata.cs`** — the values rendered into the `#` header (format version, `kind`, exported-UTC, source tenant/graph, selection summary, counts, generator). Rendering to comment lines is a private writer helper, not a JSON record.

### 5.3 Subgraph extraction — `SubgraphExtractor`

New folder `src/LiteGraph/Subgraph/`, class `SubgraphExtractor` (a standalone helper the client owns, like `GexfWriter`). It walks using the *client's* filter-aware primitives, so all sanitization and validation stay on the existing path.

Public surface — provide both a materialized and a streaming form:

```csharp
public async Task<SearchResult> Extract(
    LiteGraphClient client, SubgraphExtractionRequest request, CancellationToken token = default);

public async IAsyncEnumerable<JsonlRecord> ExtractAsRecords(
    LiteGraphClient client, SubgraphExtractionRequest request,
    [EnumeratorCancellation] CancellationToken token = default);
```

Algorithm — a bounded breadth-first walk:

1. Guard the request (non-null; graph exists; ≥1 start node; each start node exists in the graph). `ArgumentException` with context on failure.
2. Seed `visitedNodes` and a depth-0 `frontier` from `StartNodeGUIDs`; load the start nodes with `client.Node.ReadByGuids`; include them unconditionally.
3. For each depth level up to `MaxDepth`, for each frontier node, pull candidate edges per `Direction` (`ReadEdgesFromNode` / `ReadEdgesToNode` / `ReadNodeEdges`), passing `EdgeLabels`, `EdgeTags`, `EdgeFilter` straight through (SQL-filtered). Apply `MaxEdgeCost` in memory.
4. For each surviving edge, compute the neighbor endpoint. If unvisited, load it and test `NodeLabels`/`NodeTags`/`NodeFilter`. Passing nodes are added, marked visited, enqueued for the next level. Failing nodes are not expanded, and an edge leading only to a failed node is dropped.
5. Respect `MaxNodes`/`MaxEdges`; once a cap is hit, stop collecting that entity type (debug-log it, matching `GetSubgraph`).
6. Keep an edge only if **both** endpoints are in the final node set — the invariant that makes an export importable without dangling references.
7. `Extract` returns a `SearchResult` (`Graphs` = the single source graph metadata, `Nodes`, `Edges`); `ExtractAsRecords` yields the `graph` record, then node records as they are discovered, then edge records at the end (buffering only edges, which lets the node stream stay unbounded while keeping the both-endpoints invariant cheap to enforce). Honor `IncludeData`/`IncludeSubordinates` on every read; honor cancellation between reads.

For very large neighborhoods a repository-level recursive walk would be faster; if profiling demands it later, the internals can be replaced behind this same surface without touching callers.

### 5.4 JSONL writer — `src/LiteGraph/Jsonl/JsonlGraphWriter.cs`

Streaming-first. Methods:

- `Task WriteRecords(IAsyncEnumerable<JsonlRecord> records, JsonlExportMetadata metadata, Stream stream, CancellationToken token = default)` — the primitive. Writes the `#` header, then serializes each record with the shared `Serializer` (`pretty: false`) followed by `\n`, flushing periodically. Never materializes the whole document.
- `Task WriteSearchResult(SearchResult result, JsonlExportMetadata metadata, Stream stream, CancellationToken token = default)` — adapts a materialized slice to the record stream (graph, then nodes, then edges).
- `Task WriteGraph(LiteGraphClient client, Guid tenantGuid, Guid graphGuid, JsonlExportMetadata metadata, bool includeData, bool includeSubordinates, Stream stream, CancellationToken token = default)` — the **whole-graph backup path**: emits the `graph` line, then streams `client.Node.ReadMany(...)` as node records, then `client.Edge.ReadMany(...)` as edge records, all straight to `stream`. Constant memory regardless of graph size.
- `Task WriteToFile(...)` overloads for the three above.
- `Task<string> Render(...)` convenience overloads that wrap a `MemoryStream` for small callers and tests (documented as non-streaming; not for large graphs).

### 5.5 JSONL reader — `src/LiteGraph/Jsonl/JsonlGraphReader.cs`

- `IAsyncEnumerable<JsonlRecord> ReadAsync(Stream stream, [EnumeratorCancellation] CancellationToken token = default)` — reads the stream **line by line** (a buffered `StreamReader`, never `ReadToEnd`), skips comment/blank lines (counting them), deserializes each record line, and yields it. A malformed line surfaces as `JsonlFormatException` (with the 1-based line number) so the importer can honor `OnError`.
- `IEnumerable<JsonlRecord> Read(string content)` — the synchronous small-input convenience, for tests and short bodies.

### 5.6 Import orchestration — `src/LiteGraph/Jsonl/JsonlGraphImporter.cs`

The one genuinely new algorithm. It consumes reader output and a `GraphImportRequest` and returns a `GraphImportResult`. Streaming-first with a buffered fallback for out-of-order files.

```csharp
public async Task<GraphImportResult> Import(
    LiteGraphClient client, Guid tenantGuid,
    IAsyncEnumerable<JsonlRecord> records,
    GraphImportRequest request, CancellationToken token = default);
```

Steps:

1. **Validate** against the mode: `MergeIntoExisting` requires an existing `TargetGraphGUID` in `tenantGuid`; `CreateNew` must not name one. `ArgumentException` on mismatch.
2. **Resolve the target graph** up front. `CreateNew`: build from `request.NewGraph`, else the (first) file `graph` record, else a default; fresh GUID under `Regenerate`, file GUID under `Preserve` (fail if it already exists); create it; `GraphsCreated++`. `MergeIntoExisting`: target is `TargetGraphGUID`; create nothing.
3. **Stream nodes in order.** Because the writer emits nodes before edges, consume records as they arrive:
   - `graph` record after the first → warn and ignore (a file has one source graph).
   - `node` record → remap (`TenantGUID` = tenant, `GraphGUID` = target, `GUID` through the map, subordinate back-references remapped, vectors dropped if `IncludeVectors = false`), and stage it. When the stage buffer reaches `BatchSize`, flush a `CreateMany`/existence-branch (below) and continue. This keeps memory to one batch of nodes plus the GUID map.
   - `edge` record → hold in an edge buffer (edges are validated only after all nodes are known, to enforce the dangling-edge rule). If the edge buffer would exceed a safety cap, spill is out of scope; instead the importer switches to the documented buffered fallback and warns.
   - If an `edge` is seen and then a `node` appears afterward (out-of-order file), fall back to full buffering: drain the remaining stream into memory and proceed as in the non-streaming case. Warn once.
4. **GUID map.** Under `Regenerate`, allocate a fresh `Guid` per incoming node and edge, recorded old→new in `GuidMap`. Otherwise identity.
5. **Collision handling** (only under `Preserve`/`Skip`/`Overwrite`; `Regenerate` cannot collide). Per node batch and once for the edge set, call `Batch.Existence` against the target graph. `Preserve`: any collision → abort, `Success = false`, `Conflict` warning naming the first few GUIDs, nothing written (roll back). `Skip`: created set = new only; present ones counted as `*Skipped`. `Overwrite`: new → `CreateMany`, present → per-entity `Update`.
6. **Drop danglers.** Once the target's node set is known (existing target nodes for a merge, plus incoming-new, plus incoming-overwritten), discard any incoming edge whose remapped `From`/`To` is not in that set, with a `Warnings` note per drop.
7. **Write atomically.** Wrap the writes in `client.Transaction.Execute(...)` on the target graph: nodes first (batched `CreateMany`, `request.ReturnMode`), then edges, then any overwrites. Any failure rolls back; `Success = false` with the exception message in `Warnings`. Note in XML docs that SQLite serializes writes at the file level — a throughput caveat consistent with the v7 transaction notes.
8. Populate and return the `GraphImportResult`.

Streaming note for a transaction: if a single graph transaction cannot straddle an unbounded number of batches for a given provider, the importer commits in bounded chunks and records that the import was **chunk-atomic, not whole-file-atomic**, in a warning — the caller chose a backup-scale restore and is told exactly what atomicity they got. For merge-scale imports that fit one transaction, it stays whole-file-atomic. Make this boundary explicit and configurable via `BatchSize` and a `bool SingleTransaction` (default `true`; when `true` and the file exceeds a provider-specific safety threshold, the importer either honors it or downgrades with a warning — decide per provider during implementation and document it).

### 5.7 Exceptions, constants, and the `LiteGraphClient` facade

- **`JsonlFormatException.cs`** — `long LineNumber`, `string Line`. Documented on reader/importer via `<exception>`.
- Add `NdjsonContentType` where content types live (server `Constants.cs`, and SDK constants if needed).
- **`LiteGraphClient` facade**, mirroring the GEXF facade (private `_Gexf` writer → public methods). Add private `_SubgraphExtractor`, `_JsonlWriter`, `_JsonlReader`, `_JsonlImporter`, and these public methods with full XML docs:

```csharp
// Selection
Task<SearchResult> ExtractSubgraph(SubgraphExtractionRequest request, CancellationToken token = default);
Task<SearchResult> ExtractSubgraph(Guid tenantGuid, Guid graphGuid, Guid startNodeGuid,
    int maxDepth = 2, GraphTraversalDirectionEnum direction = GraphTraversalDirectionEnum.Both, CancellationToken token = default);

// Export (streaming)
Task ExportSubgraphToJsonlStream(SubgraphExtractionRequest request, Stream stream, CancellationToken token = default);
Task ExportGraphToJsonlStream(Guid tenantGuid, Guid graphGuid, bool includeData, bool includeSubordinates, Stream stream, CancellationToken token = default);
Task ExportSubgraphToJsonlFile(SubgraphExtractionRequest request, string filename, CancellationToken token = default);
Task ExportGraphToJsonlFile(Guid tenantGuid, Guid graphGuid, string filename, bool includeData, bool includeSubordinates, CancellationToken token = default);
// Small-caller convenience (documented non-streaming)
Task<string> RenderGraphAsJsonl(Guid tenantGuid, Guid graphGuid, bool includeData, bool includeSubordinates, CancellationToken token = default);

// Import (streaming)
Task<GraphImportResult> ImportGraphFromJsonlStream(Guid tenantGuid, Stream jsonl, GraphImportRequest request, CancellationToken token = default);
Task<GraphImportResult> ImportGraphFromJsonl(Guid tenantGuid, string jsonl, GraphImportRequest request, CancellationToken token = default);
```

---

## 6. Server / REST API changes (`src/LiteGraph.Server`)

Four endpoints, wired through the server's existing seven-step pattern (GEXF and subgraph routes are the templates). Verbs follow the house convention (PUT = create, POST = action/search, GET = read). Export streams the response; import streams the request body.

### 6.1 Endpoints

| Purpose | Method | Route |
|---|---|---|
| Export whole graph as JSONL (backup) | GET | `/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/export/jsonl?incldata&inclsub` |
| Export filtered subgraph as JSONL | POST | `/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/export/jsonl` (body: `SubgraphExtractionRequest`) |
| Import (merge) into existing graph | POST | `/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/import/jsonl?mode=merge&guids=regenerate&onerror=abort` (body: raw JSONL) |
| Import as a new graph | POST | `/v1.0/tenants/{tenantGuid}/graphs/import/jsonl?guids=preserve` (body: raw JSONL) |

The GET form is the backup convenience; the POST-with-body export carries the filter set. Import options ride as query parameters because the body is raw JSONL, mapped onto `GraphImportRequest`. The merge route fixes `mode = MergeIntoExisting`; the tenant-level route fixes `mode = CreateNew`.

### 6.2 The seven wiring points

1. **`Classes/RequestTypeEnum.cs`** (`#region Graphs`) — `GraphExportJsonl`, `GraphExportSubgraphJsonl`, `GraphImportJsonl`, `GraphImportJsonlNew`.
2. **`Classes/UrlContext.cs`** — `matcher.Match(...)` lines in the GET and POST blocks; ensure `/export/jsonl` and `/import/jsonl` match before broader graph routes.
3. **`API/REST/RestServiceHandler.cs` `InitializeRoutes()`** (`#region Graphs`, near GEXF) — four `Add(...)` registrations with `ExceptionRoute` and `OpenApiRouteMetadata.Create("...", "Graphs")`.
4. **Route methods** in the same file:
   - Export (GET and POST) follow `GraphGexfExportRoute` but **stream**: set `ctx.Response.ContentType = Constants.NdjsonContentType`, `ctx.Response.ChunkedTransfer = true`, and pass `ctx.Response.OutputStream` (or repeated `SendChunk`) into `_LiteGraph.ExportGraphToJsonlStream` / `ExportSubgraphToJsonlStream`, ending with `SendFinalChunk`. Never build the whole body.
   - Import (both) read the request **stream** (`ctx.Request.Data`) rather than `DataAsString`, and hand it to `_LiteGraph.ImportGraphFromJsonlStream`. Null/empty body guard first.
5. **`API/Agnostic/ServiceHandler.cs`** (`#region Graph-Routes`, near `GraphGexfExport`) — handlers returning `ResponseContext` for the import routes; export handlers do their streaming in the REST layer (like GEXF does its send there) but resolve/validate the graph in the agnostic layer. Failure mapping: export not-found → 404; malformed selection → 400; import success → 200 with `GraphImportResult`; `Preserve` collision → 409 (`Conflict`); malformed JSONL under `Abort` → 400 (`DeserializationError`) with the line number in the description; over-size → 413.
6. **`Services/AuthorizationService.cs`** — the two export types default to `read`; add the two import types to the `write` scope list in `RequiredScope` and to the write branch of `RequiredPermission`.
7. **`Classes/RequestContext.cs`** — a `SubgraphExtractionRequest` object property (parsed in the POST export route) and parsing of the new query params (`mode`, `guids`, `onerror`, `batchsize`) into a `GraphImportRequest`, reusing existing `IncludeData`/`IncludeSubordinates` parsing for the GET export. New query-param names go in `Classes/Constants.cs`.

### 6.3 Size, timeouts, streaming

Streaming import removes the whole-body-in-memory constraint, but keep a guardrail: `Settings.MaxImportBytes` (default `0` = unlimited when streaming; when >0 the importer aborts with 413 after that many bytes). The per-request timeout (`Settings.RequestTimeoutSeconds`, 1–3600) threads its token into extractor/writer/importer; for backup-scale operations, document raising it. Confirm during implementation that Watson's `ChunkedTransfer` send and `ctx.Request.Data` streaming behave under the auth/post-routing pipeline (the post-routing handler records history and metrics after the body is sent — verify it does not try to buffer a streamed response).

### 6.4 OpenAPI / API Explorer

Extend the `Graphs` tag description (currently "…and GEXF export") to mention JSONL import/export so the generated spec and the dashboard API Explorer pick it up.

---

## 7. SDK changes

Every SDK gains the same four capabilities, named consistently, plus streaming file helpers where the language makes them natural. Each SDK's README and test harness updates in lockstep. SDK HTTP clients and their tests target `127.0.0.1`, never `localhost` (Windows resolves `::1` first and stalls).

### 7.1 C# SDK (`sdk/csharp/src/LiteGraph.Sdk`)

- **`Interfaces/IGraphMethods.cs`** + **`Implementations/GraphMethods.cs`**, mirroring `ExportGraphToGexf`:
  - `Task<string> ExportGraphToJsonl(Guid tenantGuid, Guid graphGuid, bool includeData = false, bool includeSubordinates = false, CancellationToken token = default)` and a `Task ExportGraphToJsonlFile(..., string filename, ...)` that streams the HTTP response to disk (use `RestWrapper`'s streaming response if available; otherwise document the buffered fallback).
  - `Task<string> ExportSubgraphToJsonl(Guid tenantGuid, Guid graphGuid, SubgraphExtractionRequest request, CancellationToken token = default)`.
  - `Task<GraphImportResult> ImportGraphFromJsonl(Guid tenantGuid, Guid graphGuid, string jsonl, GraphImportRequest options, CancellationToken token = default)` and a `Stream`-taking overload for large files.
  - `Task<GraphImportResult> ImportGraphAsNewFromJsonl(Guid tenantGuid, string jsonl, GraphImportRequest options, CancellationToken token = default)` and its `Stream` overload.
- Port `SubgraphExtractionRequest`, `GraphImportRequest`, `GraphImportResult`, and the enums into the SDK model namespace (the SDK already duplicates such models).
- Tests: extend `sdk/csharp/src/Test.Automated/Program.cs` (it already tracks a `_SubgraphRootNodeGuid`) with export, round-trip, merge, backup-restore, and negative cases against a live server.

### 7.2 JavaScript SDK (`sdk/js`)

- **`src/base/LiteGraphSdk.js`**, `Graph-Routes` region, mirroring `exportGraphToGexf`: `exportGraphToJsonl`, `exportSubgraphToJsonl`, `importGraphFromJsonl`, `importGraphAsNewFromJsonl`. Validate nulls via `GenericExceptionHandlers.ArgumentNullException`. New models in `src/models/`, exported from `src/index.js`.
- Tests: mock handlers in `sdk/js/test/GraphRoutes/handlers.js`; jest cases in `graphRoutes.test.js` (round trip, malformed line, merge strategies).
- README: rows under `### Graph Operations` and `### Graphs`.

### 7.3 Python SDK (`sdk/python`)

- **`src/litegraph_sdk/resources/graphs.py`** — mixin (like `ExportGexfMixin`) or `@classmethod` (like `get_subgraph`, `export_gexf`): `export_jsonl`, `export_subgraph_jsonl`, `import_jsonl`, `import_jsonl_as_new`. Pydantic models under `models/`; re-export from `__init__.py`. Prefer `client.request(..., stream=True)` for export/import of large payloads if the base client supports it; otherwise document buffered behavior and file it as a follow-up.
- Tests: pytest under `tests/` (`test_models/`, new `test_import_export.py`). README + Sphinx docs updated.

### 7.4 `sdk/README.md`

Add the feature with a short quick-start per language.

---

## 8. MCP server changes (`src/LiteGraph.McpServer`)

Three tools across all three transports (HTTP, TCP, WebSocket), thin wrappers over the SDK methods from §7.1, with writes routed through `LiteGraphMcpRestProxy` to preserve RBAC.

- New file **`Registrations/SubgraphRegistrations.cs`** (`public static class`, four-region layout matching `GraphRegistrations.cs`) with `RegisterHttpTools`, `RegisterTcpMethods`, `RegisterWebSocketMethods`, and shared `private static` helpers. Tools:
  - `graph/exportjsonl` — `tenantGuid`, `graphGuid`, `includeData?`, `includeSubordinates?` → ndjson string.
  - `graph/exportsubgraphjsonl` — `tenantGuid`, `graphGuid`, plus selection fields (complex ones as JSON strings, deserialized with `Serializer.DeserializeJson<T>` as `SearchRequest` is elsewhere) → ndjson string.
  - `graph/importjsonl` — `tenantGuid`, `graphGuid?` (absent ⇒ import-as-new), `jsonl`, `mode`, `guids`, `onerror` → `GraphImportResult` JSON, routed via `LiteGraphMcpRestProxy.SendJson`.
- Wire into `RegisterMcpTools()` in `LiteGraphMcpServer.cs` (three lines beside the `GraphRegistrations.*` calls). Validate with `LiteGraphMcpServerHelpers`; serialize with `Serializer.SerializeJson(result, true)`.
- MCP tools carry large payloads as strings; note in the docs that for backup-scale graphs the REST endpoints (streaming) are preferred over MCP.

---

## 9. Dashboard — internationalization (foundational, do before the feature UI)

The dashboard ships **no** i18n today; every string is an inline literal. Making the product properly international is now in scope, and it comes first because the new import/export screens should be authored against the i18n system rather than retrofitted. The work has two halves: stand up the framework, then migrate existing strings.

### 9.1 Framework choice and setup

Use **`next-intl`** — it is the best-supported i18n library for the Next.js App Router and integrates without a Redux dependency. To avoid restructuring every route under a `[locale]` segment (which would churn the whole `src/app` tree), adopt next-intl's **`NextIntlClientProvider`** with a locale resolved from a cookie/localStorage value rather than the URL path. Concretely:

- Add `next-intl` to `dashboard/package.json`.
- Create `dashboard/messages/en.json` as the source catalog (and stub `messages/es.json`, at least partially, to prove the pipeline is real and not single-locale by accident).
- Add a locale store: extend the existing hand-written `src/lib/store/litegraph` slice with `locale` (default `en`), persisted to `localStorage` beside `tenant`/`token`, with an action `storeLocale`.
- Wrap the app: in the root layout / `StoreProvider`, mount `NextIntlClientProvider` with `locale` from the store and `messages` loaded for that locale. A small `messages/index.ts` maps locale → catalog.
- Provide a `useTranslations()`-based helper and a typed key surface. Where a string is needed outside React (rare), expose a `getMessages(locale)` accessor.
- Wire AntD localization too: pass the matching `antd/locale/*` to AntD's `ConfigProvider` `locale` prop so built-in component text (pickers, pagination, empty states) tracks the app locale.
- Add a **language switcher** to `src/components/layout/DashboardLayout.tsx` header (an AntD `Select` beside the graph selector) that dispatches `storeLocale` and re-renders.

### 9.2 String migration

Migrate every hard-coded user-facing string into the catalog, page by page, replacing literals with `t('...')` keys. Cover: the layout/nav (`DashboardLayout`, `sidebar.tsx`), every page under `src/page/**` (graphs, nodes, edges, labels, tags, vectors, request-history, api-explorer, backups, tenants, users, credentials, authorization, home/explorer), shared components under `src/components/**` (including `Litegraph*` base wrappers that render text, the graph explorer tooltips/controls, node/edge modals, selectors), validation messages in the various `constant.tsx` files, table column headers and tooltips (`columnTooltip`), and all `toast.success/error` messages. Organize catalog keys by feature namespace (e.g. `graphs.actions.exportJsonl`, `common.cancel`, `import.result.nodesCreated`).

This is broad but mechanical. Keep keys stable and descriptive; do not translate developer-facing console logs or API error codes. Where a string interpolates values, use next-intl's ICU message formatting (counts, names) rather than string concatenation, so plurals localize correctly.

### 9.3 Definition of done for i18n

No user-facing literal remains in `src/page/**` or in the text-rendering shared components; switching the language switcher visibly re-renders nav, tables, modals, toasts, and AntD component chrome; `messages/en.json` is complete and `messages/es.json` covers at least the graphs/import/export surfaces end to end so the mechanism is provably multi-locale. Add a lint/CI check (a script that scans for suspicious inline JSX string literals in `src/page`) to keep regressions out — wire it into the dashboard test job.

---

## 10. Dashboard — the import/export feature UI

Built on the i18n system from §9 (every new string is a catalog key from the start). Comply with `DASHBOARD_STYLE_AND_USABILITY.md`: use the `Litegraph*` base wrappers, `PageContainer`, the modal conventions, and `react-hot-toast`. Three surfaces gain actions; two new modals do the work.

### 10.1 Data layer (first)

Add the four SDK calls to `litegraphdb`'s `GraphSdk` (source in `sdk/js`, §7.2) and rebuild, or — if the packaged SDK lags — add a local helper `src/lib/sdk/importExport.ts` following the `authorization.ts` precedent (raw authenticated fetch, streaming where the browser allows via `fetch` + `ReadableStream`). Wrap either source as RTK endpoints in **`src/lib/store/slice/slice.ts`**: `exportGraphJsonl`, `exportSubgraphJsonl`, `importGraphJsonl` (`invalidatesTags: [GRAPH, NODE, EDGE]`), `importGraphAsNewJsonl` (`invalidatesTags: [GRAPH]`). Export the generated hooks.

### 10.2 Graphs table — per-row actions

In **`src/page/graphs/constant.tsx`**, add to the existing row `Dropdown` (which already has "Export to GEXF"): **Export to JSONL** (`handleExportJsonl(record)` in `GraphPage.tsx`, mirroring `handleExportGexf`: mutation → `new Blob([text], { type: 'application/x-ndjson' })` → `saveAs(blob, \`graph-${record.GUID}.jsonl\`)` → localized toast), and **Import into this graph** (opens the import modal with `mode = merge`, `targetGraphGuid = record.GUID`). For very large graphs, prefer triggering the download by pointing the browser at the streaming GET endpoint (with auth header via a fetch → stream → `saveAs`), so the tab does not hold the whole file in a JS string.

### 10.3 Graphs page — import as new

Add an **Import** button to the `PageContainer`'s `pageTitleRightContent` beside "Create Graph"; opens the import modal with `mode = createNew`.

### 10.4 Import modal — `src/page/graphs/components/ImportJsonlModal.tsx`

Built on `LitegraphModal`; introduces file upload (a new pattern here). An AntD `Upload.Dragger` (`beforeUpload` returns `false`) reads the file; for preview, read only the header via a streamed/first-chunk read (`file.stream()` or `file.slice(0, N).text()`) and show the `#` metadata plus quick node/edge counts without loading a huge file into memory. Mode is fixed by the caller and shown, not re-picked. `LitegraphSelect` for **GUID strategy** and **on-error**, each with localized helper text. On submit, stream the file to the import endpoint, then render the `GraphImportResult` inline (created/updated/skipped counts, warnings, and for create-new a link to the new graph); localized toast; invalidate caches. Model the state wiring on the existing vector-index modals in `src/page/graphs/components/`.

### 10.5 Graph explorer — export the selected subgraph

The explorer (`src/components/base/graph/GraphViewer.tsx`) already tracks a subgraph root in `selectedNodeGuid` and previews via `useGetSubGraphsMutation`. Add an **Export subgraph** entry to the "Controls" dropdown (via `controlsPortalTarget`) and to `NodeToolTip`, enabled when `selectedNodeGuid` is set. It opens `src/components/base/graph/ExportSubgraphModal.tsx` (`LitegraphModal`) exposing start node (prefilled, changeable via `NodeSelector`), `maxDepth` (`InputNumber`), direction (`LitegraphSelect`), node/edge label and tag filters (reuse existing widgets), optional `maxEdgeCost`, and `includeData`/`includeSubordinates` switches. Submit → `useExportSubgraphJsonlMutation` → `Blob` + `saveAs`. Optionally preview counts via `readSubGraphStatistics` first.

### 10.6 Navigation and constants

No new routes; everything hangs off the Graphs page and the explorer, so `sidebar.tsx` and `paths` are untouched.

---

## 11. Documentation changes

Updated in the same branch as the code.

- **`docs/REST_API.md`** — `## Data Structures`: subsections for `SubgraphExtractionRequest`, `GraphImportRequest`, `GraphImportResult`, and a **JSONL Format** subsection reproducing §4 (comment convention, record envelope, node-before-edge ordering, backup vs. subgraph `kind`). `## Graph APIs`: four rows beside the GEXF and Subgraph rows, with example query strings and a note on streaming/chunked responses.
- **`docs/MCP_API.md`** — **new file** (there is no MCP API reference today, only the `CLAUDE_MCP.md` walkthrough). A complete reference of the MCP tool surface: transports, request/response envelope, the full tool catalog grouped by resource, and — front and center — the three new JSONL tools with worked request/response examples. `docs/CLAUDE_MCP.md` links to it and bumps its tool count and `## What's Available` table; add a round-trip example (export → import) to the walkthrough.
- **`README.md`** (root) — a `## New In v7.1.0` section (interchange, streaming/backup, i18n), a short JSONL sample, and a `## Version History` entry.
- **`CHANGELOG.md`** — move `v7.0.0` under `## Previous Versions`; add a `v7.1.0` block under `## Current Version`, grouped as the file groups entries (§14).
- **SDK READMEs** — `sdk/js/README.md`, `sdk/python/README.md`, any C# SDK README, and `sdk/README.md`: new methods with snippets.
- **`docs/STORAGE.md`** — a paragraph positioning JSONL export as the portable, provider-agnostic per-graph backup complement to the binary `Admin.Backup`.
- **`LiteGraph.postman_collection.json`** — items in the `Graphs` folder(s). GEXF appears four times across API-version/auth variants; mirror that multiplicity for the JSONL export (GET and POST) and import (merge and new) items, using the existing `{{protocol}}/{{hostname}}/{{port}}/{{tenant}}/{{graph}}` variables and split `path` arrays (`"export","jsonl"` / `"import","jsonl"`).

---

## 12. Design decisions and edge cases (resolve before coding)

### 12.1 Extraction at the client layer, not the repository
`GetSubgraph` walks in SQL. The filtered/directional walk composes the client's filter-aware edge/node reads instead — the label/tag/`Expr` filters are already implemented and sanitized there, and no new string-built SQL is introduced. Slower for huge neighborhoods; replaceable behind the same facade if profiling demands.

### 12.2 Merge without upsert
No `ON CONFLICT`, so merge is existence-check-then-branch inside a transaction. `Batch.Existence` keeps the check to a batch round trip. The four strategies exist because different merges want different answers to "this GUID already exists."

### 12.3 The dangling-edge invariant
The exporter guarantees both endpoints per edge. The importer re-checks after remapping, because a *merge* file can legitimately reference nodes that live in the target but not the file. An edge whose endpoint resolves to neither is dropped with a warning — not silently written (breaks integrity) and not hard-failed (makes partial slices unusable).

### 12.4 Vectors and dimensionality
Vectors ride inside node/edge objects. On `CreateNew`, honor the file graph's vector-index config unless `NewGraph` overrides. On `MergeIntoExisting`, incoming vectors must match the target's dimensionality; mismatch is a per-record warning under `Skip` and an abort otherwise. `IncludeVectors = false` drops them.

### 12.5 Cross-tenant moves
Import stamps `TenantGUID` to the route's tenant, so A→B moves are supported (the "new database from subgraph" story). Under `Preserve` across tenants, GUIDs can be kept (uniqueness is per-tenant); a `Preserve` import back into the same tenant+graph collides by design.

### 12.6 Atomicity vs. scale
Merge-scale imports are whole-file-atomic (one transaction). Backup-scale restores may exceed what one transaction should hold; the importer then commits in `BatchSize` chunks and says so in a warning (§5.6). The caller picks the trade via `SingleTransaction`/`BatchSize`.

### 12.7 Ordering and memory
Writer emits graph→nodes→edges; reader and importer stream in that order with only an edge buffer held (plus the GUID map). Out-of-order files trigger a buffered fallback with a warning. This is what lets a million-node graph round-trip.

### 12.8 i18n approach
`next-intl` with a cookie/store-driven locale (no `[locale]` route segment) to avoid churning the route tree, plus AntD `ConfigProvider locale`. Ships `en` complete and `es` at least across the graph/import/export surfaces to prove multi-locale. RTL is out of v1 but not designed out.

---

## 13. Test plan

Tests are the bulk of this work. The backend uses Touchstone: descriptors live once in `src/Test.Shared` and run through `Test.Automated`, `Test.Xunit`, and `Test.Nunit` unchanged. Every case creates and cleans up its own data; nothing writes to the console from `Test.Shared`; loopback is `127.0.0.1`.

### 13.1 New shared suite — `src/Test.Shared/LiteGraphTouchstoneImportExportSuites.cs`

Register in `All`; runs on SQLite and PostgreSQL.

**Extraction — positive:** depth 0/1/2 counts on a fixture; `Outbound`/`Inbound`/`Both` distinct slices on a directed fixture; edge-label, edge-tag, and `EdgeFilter` pruning; node-label/tag/`NodeFilter` exclusion (and exclusion of edges leading only to excluded nodes); `MaxEdgeCost` boundary (`==` included, `+1` excluded); `MaxNodes`/`MaxEdges` caps; multiple start nodes union/dedupe; `IncludeData`/`IncludeSubordinates` toggles; both-endpoints invariant everywhere.

**Extraction — negative:** non-existent start node; start node in another graph; negative `MaxDepth`/`MaxEdgeCost`; empty `StartNodeGUIDs`; cancellation mid-walk.

**Round trip / streaming:** export a subgraph to a stream, read it back, assert the record set equals the `SearchResult`; header `#` lines parse and count as ignored; scrambled order still imports (buffered fallback path exercised); interleaved extra comments tolerated; **whole-graph backup** of a several-thousand-node fixture streams out and re-imports to an identical graph (compare node/edge/label/tag/vector/data); a large-fixture export asserts bounded memory (or at least that `WriteGraph` never calls a materializing path).

**Import — create-new, positive:** `CreateNew`+`Preserve` into empty target reproduces the slice with original GUIDs; `CreateNew` into a different tenant keeps GUIDs and stamps the tenant; `NewGraph` override; vector-index config carried from the file.

**Import — merge, positive:** `Regenerate` into a populated target (new GUIDs, remapped endpoints, `GuidMap` populated, existing data untouched); double `Regenerate` import → two disjoint copies; `Skip` (collisions skipped/counted; second import a no-op); `Overwrite` (collisions updated — assert a changed field; new ones created); merge whose file bridges to a target-only node imports the bridging edge.

**Import — negative:** `Preserve` collision → `Success=false`, `Conflict`, nothing written; malformed line + `Abort` → `JsonlFormatException` with line number, rollback, target unchanged; malformed line + `Skip` → good lines imported, bad in `Warnings`; unknown `type` per policy; `MergeIntoExisting` without/with-bad `TargetGraphGUID`; `CreateNew`+`Preserve` where graph GUID exists → `Conflict`; dangling edge dropped + warned; vector dimensionality mismatch; empty/comments-only body (create-new makes an empty graph, zero nodes/edges — assert); cancellation mid-import rolls back; `BatchSize` chunk-atomic path leaves a partial-but-consistent graph and reports it.

**Isolation:** a tenant-A caller cannot import into tenant-B's graph via the merge route.

### 13.2 REST-level tests
Through the running server (via the C# SDK harness): status codes (200 export, 200 import, 400 malformed, 409 preserve-collision, 413 over-size when `MaxImportBytes` set, 404 unknown graph); `application/x-ndjson` and chunked transfer on export; `GraphImportResult` body on import; a streamed large export/import round trip.

### 13.3 SDK tests
C# `Test.Automated` (export, round-trip, merge, backup-restore, negatives, live server); JS jest (mock handlers + cases incl. malformed and round trip); Python pytest (`test_import_export.py` + model tests).

### 13.4 Performance and scale
Add a bulk import/export workload to `src/Test.PerformanceAndScalability` (`Workloads.cs`, `DatasetGenerator.cs`): stream-export a 100k+-node graph and time it; stream-import large JSONL under `Regenerate` and `Skip`; assert peak memory stays flat (streaming proof). Report via `Metrics`/`Reporting`; document ranges in `PERF_SCALE_TESTING.md`.

### 13.5 Dashboard tests
Jest (`dashboard/jest.config.js`): component tests for `ImportJsonlModal` (file read, header preview, result rendering, error toast) and `ExportSubgraphModal` (control wiring, blob download); a slice test for the four RTK endpoints against a mocked SDK; and **i18n tests** — a language-switch test that asserts a locale change swaps rendered strings, and the inline-literal scanner (§9.3) wired into the test job so untranslated strings fail CI.

---

## 14. Versioning and release

Target **v7.1.0** (backward-compatible feature addition). Bump: core `LiteGraph.csproj`; server and MCP `ServerVersion` (from `7.0.0`); the three SDK package versions; `litegraphdb` and the dashboard `package.json` (from `7.0.0`); and the Docker image tags (already done in §0). `CHANGELOG.md` `v7.1.0`, grouped like existing entries:

- *Subgraph selection and interchange* — filtered directional extraction; streaming JSONL export (whole-graph backup and filtered subgraph); streaming JSONL import with create-new and merge modes and Preserve/Regenerate/Skip/Overwrite strategies; atomic merge with dangling-edge protection and chunk-atomic backup restore.
- *REST, MCP, SDKs, and dashboard* — four streaming graph endpoints; three MCP tools; C#/JS/Python SDK methods and models; dashboard export actions, subgraph-export modal, and the first file-upload import modal.
- *Internationalization* — full dashboard i18n via next-intl, a language switcher, AntD locale wiring, `en` complete and `es` across the interchange surfaces, and a CI guard against untranslated strings.
- *Documentation* — REST_API JSONL spec and data structures; new `MCP_API.md`; STORAGE backup positioning; SDK READMEs; Postman items.
- *Validation* — new Touchstone extraction/round-trip/import/backup suites (positive and negative, SQLite and PostgreSQL); SDK tests; a streaming perf workload; dashboard component/slice/i18n tests.

---

## 15. Build order

Bottom-up; each layer green (compiles with no warnings, tests pass) before the next.

0. **Branch + Docker** (§0) — `v7.1`, image tags to `v7.1.0`. Commit.
1. **Core** — enums, models, `SubgraphExtractor`, streaming `JsonlGraphWriter`/`Reader`, `JsonlGraphImporter`, `LiteGraphClient` facade, and the full `Test.Shared` suite (both providers). Land first; everything else is transport over it.
2. **Server** — four streaming endpoints, seven wiring points, REST status-code and streaming tests.
3. **C# SDK** — methods, models, live harness cases (proves the endpoints end to end).
4. **JS and Python SDKs** — methods, models, mocked tests, READMEs.
5. **MCP** — three tools; `MCP_API.md`; `CLAUDE_MCP.md` example.
6. **Dashboard i18n** (§9) — framework, language switcher, string migration, i18n tests. Do this before the feature UI.
7. **Dashboard feature** (§10) — data layer, table actions, the two modals, component/slice tests.
8. **Docs, Postman, versions** — REST_API, MCP_API, README, CHANGELOG, STORAGE, SDK/aggregate READMEs, Postman items, version bumps.

Commit per layer with a descriptive message. Keep `v7.1` compiling throughout.

---

## 16. Final tasks (after the feature is functionally complete)

These two passes are explicit deliverables, not optional polish.

1. **Revisit the dashboard for usability, aesthetics, and layout.** With the feature and i18n in place, walk the whole dashboard as a user: the graphs table and its now-crowded row-action dropdown, the two new modals, the explorer controls, and the header (which now holds a graph selector *and* a language switcher). Check spacing, alignment, responsive behavior, empty/loading/error states, focus order, and that the new upload/download flows feel native to the existing design language in `DASHBOARD_STYLE_AND_USABILITY.md`. Fix what looks bolted-on. Confirm every string is translated and nothing overflows or truncates in the longer `es` strings (a common i18n layout bug). Capture before/after notes in the PR.

2. **Revisit Postman, REST_API.md, and MCP_API.md for completeness and accuracy — especially the new features.** Diff the actual routes registered in `InitializeRoutes()` and the actual MCP tools registered in `RegisterMcpTools()` against what the docs and the Postman collection claim. Verify every new endpoint's method, path, query params, request body, response body, content type, and status codes match the implementation; run the new Postman items against a live `v7.1` server; confirm the `SubgraphExtractionRequest`/`GraphImportRequest`/`GraphImportResult` schemas in REST_API.md match the serialized shapes byte-for-byte; and confirm `MCP_API.md`'s tool signatures match the registered schemas. Correct any drift. This is the last gate before the feature is called done.

When both passes are complete and the full suite is green on both providers, run one manual end-to-end pass through the MCP round-trip example and one through the dashboard (export a subgraph, import it into a second graph, import it as a fresh graph, and export→re-import a whole graph as a backup) to confirm the four journeys from §1 work as a user would drive them.
