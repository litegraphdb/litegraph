# Bulk Insert and Hydration Plan

Status legend: `[ ]` not started, `[-]` in progress, `[x]` complete, `[!]` blocked.

This plan covers every LiteGraph bulk insert operation, not just graph node and edge creation. It is written so a developer can annotate progress directly in this file.

## Progress Update - 2026-06-13

- [x] Added public return-mode enums for core and C# SDK: `Full` and `Minimal`.
- [x] Added server constants for query parameter parsing: `return`, `full`, and `minimal`.
- [x] Preserved the existing API surface by keeping current `CreateMany(...)` methods and adding overloads/options for minimal mode.
- [x] Implemented `?return=full|minimal` parsing for all REST bulk insert endpoints with invalid-value `400 BadRequest` handling.
- [x] Implemented targeted subordinate read APIs for SQLite and PostgreSQL labels, tags, and vectors.
- [x] Updated node and edge full-mode hydration to read subordinates only for the created GUIDs.
- [x] Implemented minimal return mode for labels, tags, vectors, nodes, and edges.
- [x] Updated C#, Python, and JavaScript SDKs for bulk create return modes.
- [x] Updated REST docs, SDK docs, dashboard API Explorer metadata, and Postman bulk create requests.
- [x] Completed local validation across .NET core/server tests, C# SDK automated tests, JavaScript SDK tests/build, Python SDK tests, dashboard tests/build, and Release builds.
- [x] Updated Pneuma to request `return=minimal` for LiteGraph node, edge, and vector bulk create ingestion writes.
- [ ] Performance measurements and provider-native bulk insert follow-ups remain open backlog work.

## Summary

LiteGraph exposes multiple bulk create operations. All of them should have a consistent return-mode contract, and the operations that perform response hydration must avoid full-graph subordinate scans.

Current high-risk behavior is in node and edge bulk create. They insert the requested objects and then hydrate the response by reading every label, tag, and vector in the graph, filtering those full-graph reads in memory back down to the newly created object GUIDs. That makes each bulk create request scale with whole-graph size instead of current batch size.

The product-level fix is broader than nodes and edges:

1. Add a consistent bulk create return mode, `?return=minimal`, to every bulk insert endpoint.
2. Keep the default `return=full` behavior compatible for all existing callers.
3. For node and edge `return=full`, replace whole-graph subordinate hydration with targeted reads by created object GUIDs.
4. For label, tag, and vector bulk create, accept the same return-mode contract and verify there is no hidden whole-scope hydration or avoidable per-item validation bottleneck.

## Bulk Insert Scope

These are the REST bulk insert operations currently in scope:

| Object | Route | Request type | Core client method | Repository method |
| --- | --- | --- | --- | --- |
| Labels | `PUT /v1.0/tenants/{tenantGuid}/labels/bulk` | `LabelCreateMany` | `Label.CreateMany` | `Label.CreateMany` |
| Tags | `PUT /v1.0/tenants/{tenantGuid}/tags/bulk` | `TagCreateMany` | `Tag.CreateMany` | `Tag.CreateMany` |
| Vectors | `PUT /v1.0/tenants/{tenantGuid}/vectors/bulk` | `VectorCreateMany` | `Vector.CreateMany` | `Vector.CreateMany` |
| Nodes | `PUT /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/bulk` | `NodeCreateMany` | `Node.CreateMany` | `Node.CreateMany` |
| Edges | `PUT /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/bulk` | `EdgeCreateMany` | `Edge.CreateMany` | `Edge.CreateMany` |

Bulk operations explicitly out of scope for this plan:

- [ ] Bulk delete routes such as `/labels/bulk`, `/tags/bulk`, `/vectors/bulk`, `/nodes/bulk`, `/edges/bulk`, `/nodes/edges/bulk`, and `/requesthistory/bulk`.
- [ ] Graph transactions. Transactions can contain multiple create operations, but they are not bulk insert endpoints. Add a note to transaction docs if return-mode semantics are intentionally not supported there.
- [ ] Query language `CREATE` operations. They may create multiple objects, but they are query semantics, not bulk insert API calls.

## Current Findings

- [ ] Confirm the baseline behavior against current `main` before implementation.
- [ ] Capture current request timing for each bulk insert endpoint on small batches.
- [ ] Capture node and edge bulk create timing on a graph with many unrelated labels, tags, and vectors.
- [ ] Capture vector bulk create timing with many vectors referencing existing nodes.
- [ ] Record whether any bulk create endpoint hits request timeout under the default 60-second server timeout.

Observed code paths:

- `src/LiteGraph/Client/Implementations/LabelMethods.cs`
  - `CreateMany(...)` validates each label's graph/node/edge target, calls `_Repo.Label.CreateMany(...)`, then returns created label metadata.
  - No subordinate hydration is expected.

- `src/LiteGraph/Client/Implementations/TagMethods.cs`
  - `CreateMany(...)` validates each tag's graph/node/edge target, calls `_Repo.Tag.CreateMany(...)`, then returns created tag metadata.
  - No subordinate hydration is expected.

- `src/LiteGraph/Client/Implementations/VectorMethods.cs`
  - `CreateMany(...)` validates each vector's graph/node/edge target, calls `_Repo.Vector.CreateMany(...)`, then returns created vector metadata.
  - No subordinate hydration is expected.
  - Per-vector target validation is an N+1 scaling risk and should be measured.

- `src/LiteGraph/Client/Implementations/NodeMethods.cs`
  - `CreateMany(...)` calls `_Repo.Node.CreateMany(...)`.
  - It then unconditionally calls `PopulateNodes(created, true, true, token)`.
  - `PopulateNodes(...)` reads all graph labels, all graph tags, and all graph vectors by calling:
    - `_Repo.Label.ReadMany(tenantGuid, graphGuid, null, null, null, ...)`
    - `_Repo.Tag.ReadMany(tenantGuid, graphGuid, null, null, null, null, ...)`
    - `_Repo.Vector.ReadMany(tenantGuid, graphGuid, null, null, ...)`
  - It filters those graph-wide reads in memory with dictionaries keyed by the newly created node GUIDs.

- `src/LiteGraph/Client/Implementations/EdgeMethods.cs`
  - `CreateMany(...)` has the same response hydration pattern as nodes.
  - `PopulateEdges(...)` reads all graph labels, tags, and vectors, then filters by the newly created edge GUIDs.

- SQLite repository implementations:
  - `src/LiteGraph/GraphRepositories/Sqlite/Implementations/LabelMethods.cs`
  - `src/LiteGraph/GraphRepositories/Sqlite/Implementations/TagMethods.cs`
  - `src/LiteGraph/GraphRepositories/Sqlite/Implementations/VectorMethods.cs`
  - `src/LiteGraph/GraphRepositories/Sqlite/Implementations/NodeMethods.cs`
  - `src/LiteGraph/GraphRepositories/Sqlite/Implementations/EdgeMethods.cs`
  - Existing create-many methods generally build multi-row insert SQL, execute it, then retrieve created rows.

- PostgreSQL repository implementations:
  - `src/LiteGraph/GraphRepositories/Postgresql/Implementations/LabelMethods.cs`
  - `src/LiteGraph/GraphRepositories/Postgresql/Implementations/TagMethods.cs`
  - `src/LiteGraph/GraphRepositories/Postgresql/Implementations/VectorMethods.cs`
  - `src/LiteGraph/GraphRepositories/Postgresql/Implementations/NodeMethods.cs`
  - `src/LiteGraph/GraphRepositories/Postgresql/Implementations/EdgeMethods.cs`
  - Existing create-many methods mirror SQLite behavior and should receive equivalent changes/tests.

## Supported Database Scope

LiteGraph currently has four configured database type values:

- `Sqlite`
- `Postgresql`
- `Mysql`
- `SqlServer`

Implementation scope:

- [x] SQLite: fully implement and test all bulk insert return modes and targeted node/edge hydration.
- [x] PostgreSQL: fully implement targeted query support and shared behavior coverage for all bulk insert return modes and targeted node/edge hydration.
- [x] MySQL: confirm placeholder repository behavior remains unsupported; no bulk insert behavior to fix until the provider is implemented.
- [x] SQL Server: confirm placeholder repository behavior remains unsupported; no bulk insert behavior to fix until the provider is implemented.

MySQL and SQL Server currently derive from `UnsupportedGraphRepository`. If repository interfaces are expanded, placeholder behavior must remain explicit and tests should still verify that unsupported providers fail clearly.

## API Decision

Use `?return=minimal` as the public query parameter for every bulk insert endpoint.

Rationale:

- `?noreturn` is misleading because the endpoint will still return created top-level objects.
- `return=minimal` clearly describes the response shape.
- The parameter can be extended later without adding more Boolean flags.
- Using one parameter across labels, tags, vectors, nodes, and edges keeps SDKs and Postman simpler.

Supported values:

- `return=full`: default. Preserve current response behavior.
- `return=minimal`: return only the top-level created objects and skip optional response enrichment.

Rejected for this work:

- `return=none`: not required by the current need.
- Request-body return options: avoid changing existing bulk request body shapes.
- Header-based return options: less discoverable in Postman, SDKs, and REST docs.
- Alias parameters such as `hydrate=false`, `noreturn`, or `minimal=true`: avoid ambiguous compatibility surface.

Return-mode behavior by object:

| Object | `return=full` | `return=minimal` |
| --- | --- | --- |
| Labels | Return created label metadata. | Same top-level label metadata; no extra hydration. |
| Tags | Return created tag metadata. | Same top-level tag metadata; no extra hydration. |
| Vectors | Return created vector metadata. | Return created vector metadata; no extra hydration. Consider whether embeddings/content should remain included because they are top-level vector fields. |
| Nodes | Return created nodes with labels, tags, vectors, and data, but hydrate subordinates by created node GUID only. | Return top-level created node rows only; skip labels, tags, and vectors in response. |
| Edges | Return created edges with labels, tags, vectors, and data, but hydrate subordinates by created edge GUID only. | Return top-level created edge rows only; skip labels, tags, and vectors in response. |

Response contract:

- [x] Default `return=full` keeps current response shape for all bulk insert endpoints.
- [x] `return=minimal` is accepted by all five bulk insert endpoints.
- [x] `return=minimal` never skips persistence. It only changes response enrichment/hydration.
- [x] Invalid return values return `400 BadRequest` consistently for all five endpoints.
- [x] Minimal response serialization is documented per object type.

## Implementation Plan

### Phase 1: Return Mode Model

- [x] Add a return-mode model in core code.
  - Suggested file: `src/LiteGraph/BulkCreateReturnModeEnum.cs`
  - Values:
    - `Full`
    - `Minimal`
- [x] Make the enum public in the core `LiteGraph` assembly.
  - In-process C# callers can hit the same scaling issue as REST callers.
- [x] Add querystring constants.
  - File: `src/LiteGraph.Server/Classes/Constants.cs`
  - Add:
    - `ReturnQuerystring = "return"`
    - `ReturnFullValue = "full"`
    - `ReturnMinimalValue = "minimal"`
- [x] Add parsed request state.
  - File: `src/LiteGraph.Server/Classes/RequestContext.cs`
  - Add `BulkCreateReturnMode` or equivalent property with default `Full`.
  - Parse `?return=minimal` case-insensitively.
  - Parse missing or `return=full` as `Full`.
  - Return `400 BadRequest` for unsupported values such as `return=abc`.
- [x] Ensure query parsing only affects create-many request types.
  - `LabelCreateMany`
  - `TagCreateMany`
  - `VectorCreateMany`
  - `NodeCreateMany`
  - `EdgeCreateMany`
- [x] Add tests for query parsing.
  - Valid: no parameter, `return=full`, `return=minimal`.
  - Invalid: `return=none`, `return=false`, `return=abc`.

### Phase 2: Core Client and Service Method Signatures

- [x] Update in-process client interfaces without breaking existing callers.
  - `src/LiteGraph/Client/Interfaces/ILabelMethods.cs`
  - `src/LiteGraph/Client/Interfaces/ITagMethods.cs`
  - `src/LiteGraph/Client/Interfaces/IVectorMethods.cs`
  - `src/LiteGraph/Client/Interfaces/INodeMethods.cs`
  - `src/LiteGraph/Client/Interfaces/IEdgeMethods.cs`
- [x] Add optional return mode support to all five `CreateMany` APIs.
  - Recommended overload pattern:
    - Existing signature remains unchanged.
    - Add overload with `BulkCreateReturnModeEnum returnMode`.
  - Avoid breaking calls where the final argument is `CancellationToken`.
- [x] Update implementations.
  - `src/LiteGraph/Client/Implementations/LabelMethods.cs`
  - `src/LiteGraph/Client/Implementations/TagMethods.cs`
  - `src/LiteGraph/Client/Implementations/VectorMethods.cs`
  - `src/LiteGraph/Client/Implementations/NodeMethods.cs`
  - `src/LiteGraph/Client/Implementations/EdgeMethods.cs`
- [x] Update agnostic REST service handlers.
  - File: `src/LiteGraph.Server/API/Agnostic/ServiceHandler.cs`
  - Pass parsed return mode into all five create-many calls.
- [x] Update REST route handlers.
  - File: `src/LiteGraph.Server/API/REST/RestServiceHandler.cs`
  - No route path changes.
  - Ensure return-mode parsing and invalid-mode handling happen before `WrappedRequestHandler`.
- [ ] Update request type URL classification if needed.
  - File: `src/LiteGraph.Server/Classes/UrlContext.cs`
  - Confirm query parameter does not affect route matching.
- [ ] Update any internal call sites.
  - Query service mutations, transaction execution, MCP direct tools, seed/sample generation, and tests should use defaults unless explicitly testing minimal mode.

### Phase 3: Repository and SQL Behavior for All Bulk Inserts

- [x] Audit SQLite create-many query builders.
  - `src/LiteGraph/GraphRepositories/Sqlite/Queries/LabelQueries.cs`
  - `src/LiteGraph/GraphRepositories/Sqlite/Queries/TagQueries.cs`
  - `src/LiteGraph/GraphRepositories/Sqlite/Queries/VectorQueries.cs`
  - `src/LiteGraph/GraphRepositories/Sqlite/Queries/NodeQueries.cs`
  - `src/LiteGraph/GraphRepositories/Sqlite/Queries/EdgeQueries.cs`
- [x] Audit PostgreSQL create-many query builders.
  - `src/LiteGraph/GraphRepositories/Postgresql/Queries/LabelQueries.cs`
  - `src/LiteGraph/GraphRepositories/Postgresql/Queries/TagQueries.cs`
  - `src/LiteGraph/GraphRepositories/Postgresql/Queries/VectorQueries.cs`
  - `src/LiteGraph/GraphRepositories/Postgresql/Queries/NodeQueries.cs`
  - `src/LiteGraph/GraphRepositories/Postgresql/Queries/EdgeQueries.cs`
- [x] Confirm all create-many repository methods retrieve top-level created rows by created GUIDs only.
- [x] Confirm no label/tag/vector create-many method performs whole-tenant or whole-graph response hydration.
- [x] Confirm node and edge create-many repository methods do not need return-mode changes unless they already populate subordinates.
- [x] Decide whether repository methods need return mode at all.
  - Recommendation: keep repository create-many methods returning top-level created rows only.
  - Keep response hydration in client/service layer where it currently lives.
- [ ] Measure SQL payload size for all five bulk insert types.
  - Existing implementations build multi-row SQL strings.
  - Track whether large batch payload size, embeddings, or text content becomes a separate limit.
- [ ] Add follow-up backlog item if provider-native bulk insert APIs are needed.
  - SQLite: prepared statements/transactions may be preferable for very large payloads.
  - PostgreSQL: `COPY`, unnest parameters, or temp-table insert may be preferable for large payloads.

### Phase 4: Targeted Subordinate Hydration for Nodes and Edges

Goal: default `return=full` should still return hydrated nodes and edges, but it must not read all graph labels, tags, and vectors.

- [x] Add batch subordinate read APIs for labels.
  - Interface: `src/LiteGraph/GraphRepositories/Interfaces/ILabelMethods.cs`
  - Suggested methods:
    - `IAsyncEnumerable<LabelMetadata> ReadManyForNodes(Guid tenantGuid, Guid graphGuid, IReadOnlyCollection<Guid> nodeGuids, CancellationToken token = default)`
    - `IAsyncEnumerable<LabelMetadata> ReadManyForEdges(Guid tenantGuid, Guid graphGuid, IReadOnlyCollection<Guid> edgeGuids, CancellationToken token = default)`
- [x] Add batch subordinate read APIs for tags.
  - Interface: `src/LiteGraph/GraphRepositories/Interfaces/ITagMethods.cs`
  - Suggested methods:
    - `IAsyncEnumerable<TagMetadata> ReadManyForNodes(...)`
    - `IAsyncEnumerable<TagMetadata> ReadManyForEdges(...)`
- [x] Add batch subordinate read APIs for vectors.
  - Interface: `src/LiteGraph/GraphRepositories/Interfaces/IVectorMethods.cs`
  - Suggested methods:
    - `IAsyncEnumerable<VectorMetadata> ReadManyForNodes(...)`
    - `IAsyncEnumerable<VectorMetadata> ReadManyForEdges(...)`
- [x] Validate empty GUID lists.
  - Empty input must return no rows without issuing an unrestricted graph read.
- [x] Implement SQLite query builders.
  - `src/LiteGraph/GraphRepositories/Sqlite/Queries/LabelQueries.cs`
  - `src/LiteGraph/GraphRepositories/Sqlite/Queries/TagQueries.cs`
  - `src/LiteGraph/GraphRepositories/Sqlite/Queries/VectorQueries.cs`
  - Queries must filter by tenant, graph, and `nodeguid IN (...)` or `edgeguid IN (...)`.
- [x] Implement PostgreSQL query builders.
  - `src/LiteGraph/GraphRepositories/Postgresql/Queries/LabelQueries.cs`
  - `src/LiteGraph/GraphRepositories/Postgresql/Queries/TagQueries.cs`
  - `src/LiteGraph/GraphRepositories/Postgresql/Queries/VectorQueries.cs`
  - Queries must filter by tenant, graph, and `nodeguid IN (...)` or `edgeguid IN (...)`.
  - Keep behavior consistent with existing PostgreSQL SQL translation conventions.
- [x] Implement SQLite repository methods.
  - `src/LiteGraph/GraphRepositories/Sqlite/Implementations/LabelMethods.cs`
  - `src/LiteGraph/GraphRepositories/Sqlite/Implementations/TagMethods.cs`
  - `src/LiteGraph/GraphRepositories/Sqlite/Implementations/VectorMethods.cs`
- [x] Implement PostgreSQL repository methods.
  - `src/LiteGraph/GraphRepositories/Postgresql/Implementations/LabelMethods.cs`
  - `src/LiteGraph/GraphRepositories/Postgresql/Implementations/TagMethods.cs`
  - `src/LiteGraph/GraphRepositories/Postgresql/Implementations/VectorMethods.cs`
- [x] Update `PopulateNodes`.
  - File: `src/LiteGraph/Client/Implementations/NodeMethods.cs`
  - Replace graph-wide `ReadMany(... null, null ...)` calls with the new node-GUID batch subordinate reads.
  - Continue grouping results by created node GUID.
  - Preserve `includeData` behavior.
- [x] Update `PopulateEdges`.
  - File: `src/LiteGraph/Client/Implementations/EdgeMethods.cs`
  - Replace graph-wide `ReadMany(... null, null ...)` calls with the new edge-GUID batch subordinate reads.
  - Continue grouping results by created edge GUID.
  - Preserve `includeData` behavior.
- [ ] Confirm indexes are sufficient.
  - SQLite setup should have tenant/graph/node and tenant/graph/edge indexes for labels, tags, and vectors.
  - PostgreSQL setup should have equivalent indexes.
  - If missing in either provider, add migration-safe index creation in setup queries.

### Phase 5: Minimal Return Mode for All Bulk Inserts

- [x] Implement minimal mode for label bulk create.
  - File: `src/LiteGraph/Client/Implementations/LabelMethods.cs`
  - Confirm full and minimal responses are identical top-level label metadata unless additional enrichment is discovered.
  - Do not skip validation or persistence.
- [x] Implement minimal mode for tag bulk create.
  - File: `src/LiteGraph/Client/Implementations/TagMethods.cs`
  - Confirm full and minimal responses are identical top-level tag metadata unless additional enrichment is discovered.
  - Do not skip validation or persistence.
- [x] Implement minimal mode for vector bulk create.
  - File: `src/LiteGraph/Client/Implementations/VectorMethods.cs`
  - Confirm full and minimal responses are top-level vector metadata.
  - Decide whether embeddings are included in minimal mode, since embeddings are top-level vector fields.
  - Recommendation: keep embeddings included for compatibility unless a separate `return=summary` mode is added later.
  - Do not skip vector index updates.
- [x] Implement minimal mode for node bulk create.
  - File: `src/LiteGraph/Client/Implementations/NodeMethods.cs`
  - After `_Repo.Node.CreateMany(...)`, if `returnMode == Minimal`, skip `PopulateNodes(...)`.
  - Do not clear or modify persisted labels, tags, vectors, or vector index data.
- [x] Implement minimal mode for edge bulk create.
  - File: `src/LiteGraph/Client/Implementations/EdgeMethods.cs`
  - After `_Repo.Edge.CreateMany(...)`, if `returnMode == Minimal`, skip `PopulateEdges(...)`.
  - Do not clear or modify persisted labels, tags, vectors, or vector index data.
- [x] Decide cache behavior for node and edge minimal mode.
  - Current cache entries populated from minimal mode will not include subordinates.
  - Options:
    - Cache minimal top-level entries only and rely on explicit reads with `inclsub` to refresh.
    - Do not cache minimal node/edge responses.
  - Recommendation: do not cache minimal node/edge responses unless the cache key distinguishes hydration level.
- [x] Add tests for cache correctness.
  - Create via minimal mode, then read by GUID with `includeSubordinates = true`.
  - Assert labels, tags, and vectors are returned correctly.
  - If caching minimal responses, assert the read does not serve stale minimal cache data.

### Phase 6: Bulk Validation and Payload Follow-up

This phase covers bulk insert risks that are not response hydration but still affect large ingestions.

- [ ] Audit per-item validation for label bulk create.
- [ ] Audit per-item validation for tag bulk create.
- [ ] Audit per-item validation for vector bulk create.
- [ ] Audit per-item validation for node bulk create.
- [ ] Audit per-item validation for edge bulk create.
- [ ] Measure whether repeated graph/node/edge existence checks dominate runtime.
- [ ] Consider batch validation by graph GUID and node/edge GUID.
  - Existing batch existence endpoint may be reusable.
  - Ensure authorization and tenant scoping remain unchanged.
- [ ] Measure payload size limits.
  - Labels and tags can be numerous.
  - Vectors can carry large embeddings.
  - Nodes and edges can carry data plus subordinates.
- [ ] Add a separate backlog item for configurable max bulk insert batch sizes if needed.

## REST, SDK, MCP, Dashboard, and Documentation Plan

### REST API

- [x] Update `docs/REST_API.md`.
  - Add `?return=minimal` to all five create-many rows:
    - Labels
    - Tags
    - Vectors
    - Nodes
    - Edges
  - Add a "Bulk create return modes" section.
  - Document default `return=full`.
  - Document object-specific minimal response behavior.
  - Document invalid return value behavior.
- [x] Update server route metadata if OpenAPI generation exposes query parameters.
  - File: `src/LiteGraph.Server/API/REST/RestServiceHandler.cs`
  - File: any `OpenApiRouteMetadata` helpers if query metadata exists.
- [x] Update dashboard API Explorer static route metadata.
  - `dashboard/src/page/api-explorer/openApi.ts`
  - `dashboard/src/page/api-explorer/requestTemplates.ts`
  - `dashboard/src/page/api-explorer/codeSnippets.ts`
  - `dashboard/src/page/api-explorer/responseSummaries.ts`
- [x] Add API Explorer tests for affected bulk insert route metadata.
  - `dashboard/src/tests/page/api-explorer/openApi.test.ts`
  - `dashboard/src/tests/page/api-explorer/responseSummaries.test.ts`

### C# SDK

- [x] Update C# SDK interfaces.
  - `sdk/csharp/src/LiteGraph.Sdk/Interfaces/ILabelMethods.cs`
  - `sdk/csharp/src/LiteGraph.Sdk/Interfaces/ITagMethods.cs`
  - `sdk/csharp/src/LiteGraph.Sdk/Interfaces/IVectorMethods.cs`
  - `sdk/csharp/src/LiteGraph.Sdk/Interfaces/INodeMethods.cs`
  - `sdk/csharp/src/LiteGraph.Sdk/Interfaces/IEdgeMethods.cs`
- [x] Update C# SDK implementations.
  - `sdk/csharp/src/LiteGraph.Sdk/Implementations/LabelMethods.cs`
  - `sdk/csharp/src/LiteGraph.Sdk/Implementations/TagMethods.cs`
  - `sdk/csharp/src/LiteGraph.Sdk/Implementations/VectorMethods.cs`
  - `sdk/csharp/src/LiteGraph.Sdk/Implementations/NodeMethods.cs`
  - `sdk/csharp/src/LiteGraph.Sdk/Implementations/EdgeMethods.cs`
- [x] Add a public return mode enum or simple optional string.
  - Recommendation: enum matching server core, `BulkCreateReturnModeEnum`.
- [x] Preserve current overload behavior.
  - Existing `CreateMany(...)` must still call `/bulk` without a query parameter.
  - New overload or optional parameter should append `?return=minimal` for any supported bulk insert type.
- [x] Update C# SDK README.
  - `sdk/csharp/README.md`
- [x] Update C# SDK generated XML docs if applicable.
  - `sdk/csharp/src/LiteGraph.Sdk/LiteGraph.Sdk.xml`
- [x] Update C# SDK tests.
  - `sdk/csharp/src/Test.Sdk/Program.cs`
  - `sdk/csharp/src/Test.Automated/Program.cs`

### Python SDK

- [x] Audit current bulk URL helper behavior.
  - `sdk/python/src/litegraph_sdk/mixins.py`
  - Current `CreateableMultipleAPIResource` appears to use `"multiple"`.
  - REST docs and server routes use `"bulk"`.
- [x] Decide compatibility strategy.
  - Prefer updating Python SDK to use `/bulk` for labels, tags, vectors, nodes, and edges.
  - If `/multiple` is intentionally supported elsewhere, document both and add tests.
- [x] Add `return_mode` parameter to `create_multiple`.
  - Accepted values: `"full"` and `"minimal"`.
  - Default: `"full"` or `None` to omit the query parameter.
  - Append `?return=minimal` when requested.
- [x] Add Python SDK tests.
  - `sdk/python/tests/test_mixins.py`
  - Resource tests for label, tag, vector, node, and edge `create_multiple(..., return_mode="minimal")`.
  - Invalid return mode test.
- [x] Update Python README/docs.
  - `sdk/python/README.md`
  - `sdk/python/docs/readme.md`
  - `sdk/python/CHANGELOG.md`

### JavaScript SDK

- [x] Audit current bulk URL helper behavior.
  - `sdk/js/src/base/LiteGraphSdk.js`
  - Current `createNodes` and `createEdges` appear to use `/multiple`.
  - Confirm label, tag, and vector bulk create coverage exists; add it if missing.
  - REST docs and server routes use `/bulk`.
- [x] Decide compatibility strategy.
  - Prefer updating JavaScript SDK to use `/bulk`.
  - If `/multiple` is intentionally supported elsewhere, document both and add tests.
- [x] Add options support to every bulk insert helper.
  - Support `{ returnMode: 'minimal' }`.
  - Preserve existing call styles where the final argument is a cancellation token.
- [x] Update generated TypeScript declarations.
  - `sdk/js/types/base/LiteGraphSdk.d.ts`
- [x] Update JavaScript SDK tests.
  - `sdk/js/test/labelRoutes/labelRoutes.test.js`
  - `sdk/js/test/tagRoutes/tagRoutes.test.js`
  - `sdk/js/test/vectorRoutes/vectorRoutes.test.js`
  - `sdk/js/test/nodeRoutes/nodeRoutes.test.js`
  - `sdk/js/test/edgeRoutes/edgeRoutes.test.js`
  - Matching handlers in each route folder.
- [x] Update JavaScript SDK docs.
  - `sdk/js/README.md`
  - `sdk/js/docs/docs.md`
  - `sdk/js/CHANGELOG.md`

### MCP

- [ ] Audit MCP bulk create tools.
  - `src/LiteGraph.McpServer/Registrations/LabelRegistrations.cs`
  - `src/LiteGraph.McpServer/Registrations/TagRegistrations.cs`
  - `src/LiteGraph.McpServer/Registrations/VectorRegistrations.cs`
  - `src/LiteGraph.McpServer/Registrations/NodeRegistrations.cs`
  - `src/LiteGraph.McpServer/Registrations/EdgeRegistrations.cs`
- [ ] Decide whether MCP exposes minimal return mode.
  - Recommendation: expose optional `returnMode` for all bulk create tools.
  - Default remains full.
- [ ] Update MCP tool descriptions.
- [ ] Add MCP tests.
  - `src/Test.Shared/LiteGraphTouchstoneSuites.cs`
  - Existing methods include label, tag, vector, node, and edge MCP create-many tests.

### Postman

- [x] Update `LiteGraph.postman_collection.json`.
  - Add `return` query parameter to label bulk create request.
  - Add `return` query parameter to tag bulk create request.
  - Add `return` query parameter to vector bulk create request.
  - Add `return` query parameter to node bulk create request.
  - Add `return` query parameter to edge bulk create request.
  - Add minimal-return examples for every bulk insert endpoint or a shared folder-level variable/example.
- [x] Confirm Postman examples still serialize the same request bodies.
- [ ] Add example response bodies for minimal mode.

### Main Documentation

- [ ] Update `README.md` if bulk create behavior is described there.
- [ ] Update `docs/STORAGE.md` only if database-provider notes need clarification.
- [ ] Update `docs/OBSERVABILITY.md` only if new metrics are added.
- [ ] Update `CHANGELOG.md`.
- [x] Update `sdk/README.md` if SDK capability summaries mention bulk create.

## Test Plan

### Core Regression Tests

- [x] Add return-mode tests for label bulk create.
  - Default `return=full` works.
  - `return=minimal` works.
  - Both modes persist labels.
- [x] Add return-mode tests for tag bulk create.
  - Default `return=full` works.
  - `return=minimal` works.
  - Both modes persist tags.
- [x] Add return-mode tests for vector bulk create.
  - Default `return=full` works.
  - `return=minimal` works.
  - Both modes persist vectors and update vector index state as before.
- [x] Add a test that proves default node bulk create hydrates only created-node subordinates.
  - Seed unrelated nodes with many labels, tags, and vectors.
  - Bulk create new nodes with their own labels, tags, and vectors.
  - Assert response includes only the created nodes' subordinate labels, tags, and vectors.
  - Assert unrelated subordinates are not present.
- [x] Add a test that proves default edge bulk create hydrates only created-edge subordinates.
  - Seed unrelated edges with many labels, tags, and vectors.
  - Bulk create new edges with their own labels, tags, and vectors.
  - Assert response includes only the created edges' subordinate labels, tags, and vectors.
  - Assert unrelated subordinates are not present.
- [x] Add a test that proves minimal node bulk create skips response hydration.
  - Bulk create nodes with labels, tags, and vectors using minimal mode.
  - Assert response contains top-level nodes only.
  - Read the nodes by GUID with `includeSubordinates = true`.
  - Assert labels, tags, and vectors were persisted.
- [x] Add a test that proves minimal edge bulk create skips response hydration.
  - Bulk create edges with labels, tags, and vectors using minimal mode.
  - Assert response contains top-level edges only.
  - Read the edges by GUID with `includeSubordinates = true`.
  - Assert labels, tags, and vectors were persisted.
- [x] Add a cache regression test.
  - Bulk create nodes and edges with minimal mode.
  - Immediately read by GUID with `includeSubordinates = true`.
  - Assert the read returns subordinates and is not polluted by cached minimal objects.

### Query Builder Tests

- [ ] Add SQLite create-many query tests for labels, tags, vectors, nodes, and edges.
  - Created top-level rows are retrieved by created GUIDs only.
  - Empty bulk input behavior is safe.
  - Large GUID lists produce valid SQL.
- [ ] Add PostgreSQL create-many query tests for labels, tags, vectors, nodes, and edges.
  - Same assertions as SQLite.
  - Include translation/quoting expectations if the PostgreSQL query builder uses provider-specific translation.
- [ ] Add SQLite targeted subordinate query tests.
  - Label batch read by node GUIDs uses `nodeguid IN (...)`.
  - Label batch read by edge GUIDs uses `edgeguid IN (...)`.
  - Tag batch read by node GUIDs uses `nodeguid IN (...)`.
  - Tag batch read by edge GUIDs uses `edgeguid IN (...)`.
  - Vector batch read by node GUIDs uses `nodeguid IN (...)`.
  - Vector batch read by edge GUIDs uses `edgeguid IN (...)`.
  - Empty GUID list does not produce unrestricted graph query.
- [ ] Add PostgreSQL targeted subordinate query tests.
  - Same assertions as SQLite.

### Repository Integration Tests

- [x] Add SQLite integration tests for all five create-many methods.
  - Labels
  - Tags
  - Vectors
  - Nodes
  - Edges
- [ ] Add PostgreSQL integration tests for all five create-many methods.
  - Same behavior as SQLite.
  - Run with `LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING`.
- [x] Add SQLite integration tests for targeted subordinate reads.
  - Create labels/tags/vectors on multiple nodes and edges.
  - Query by a subset of node GUIDs.
  - Query by a subset of edge GUIDs.
  - Assert only requested targets are returned.
- [ ] Add PostgreSQL integration tests for targeted subordinate reads.
  - Same behavior as SQLite.
- [ ] Confirm MySQL placeholder tests.
  - Factory still creates `MysqlGraphRepository`.
  - Accessing repository operations still throws `NotSupportedException`.
- [ ] Confirm SQL Server placeholder tests.
  - Factory still creates `SqlServerGraphRepository`.
  - Accessing repository operations still throws `NotSupportedException`.

### REST Tests

- [x] Add REST tests for label bulk create.
  - `PUT /labels/bulk`
  - `PUT /labels/bulk?return=minimal`
- [x] Add REST tests for tag bulk create.
  - `PUT /tags/bulk`
  - `PUT /tags/bulk?return=minimal`
- [x] Add REST tests for vector bulk create.
  - `PUT /vectors/bulk`
  - `PUT /vectors/bulk?return=minimal`
- [x] Add REST tests for node bulk create.
  - `PUT /nodes/bulk`
  - `PUT /nodes/bulk?return=minimal`
  - Read created node with `?inclsub` and assert subordinate persistence.
- [x] Add REST tests for edge bulk create.
  - `PUT /edges/bulk`
  - `PUT /edges/bulk?return=minimal`
  - Read created edge with `?inclsub` and assert subordinate persistence.
- [x] Add REST test for invalid return mode on every bulk insert endpoint.
  - Expected: `400 BadRequest`.
- [x] Add authorization regression tests.
  - `return=minimal` must not bypass existing route authorization.
  - Existing admin/user scope requirements remain unchanged.

### SDK Tests

- [x] C# SDK test: default label/tag/vector/node/edge `CreateMany` omits query parameter and preserves existing behavior.
- [x] C# SDK test: minimal label/tag/vector/node/edge `CreateMany` appends `?return=minimal`.
- [x] Python SDK test: default `create_multiple` uses `/bulk` and omits query parameter for all relevant resources.
- [x] Python SDK test: `return_mode="minimal"` appends `?return=minimal` for all relevant resources.
- [x] JavaScript SDK test: default bulk helpers use `/bulk` and omit query parameter.
- [x] JavaScript SDK test: `{ returnMode: 'minimal' }` appends `?return=minimal`.
- [x] SDK backward compatibility tests for old call styles.

### Performance and Guardrail Tests

- [ ] Add a targeted performance regression test that can run outside the normal fast suite.
  - Seed one graph with at least:
    - 1,000 unrelated labels
    - 1,000 unrelated tags
    - 1,000 unrelated vectors
  - Bulk create 2 nodes with subordinates.
  - Bulk create 2 edges with subordinates.
  - Compare default hydration timing before and after fix.
  - Assert no timeout under default request timeout.
- [ ] Add vector bulk performance test.
  - Bulk create enough vectors to expose per-vector validation and payload limits.
  - Assert no unexpected full-graph reads occur.
- [ ] Add label/tag bulk performance tests.
  - Bulk create enough labels/tags to validate query payload size and created-row retrieval behavior.
- [ ] Add an instrumentation test if practical.
  - Use a counting repository or query logger to assert node/edge hydration does not issue unrestricted label/tag/vector graph reads.
- [ ] Ensure query logging redacts sensitive data if performance diagnostics log SQL.

## Validation Matrix

Commands:

- [ ] `test.bat net10.0`
- [x] `dotnet test --framework net10.0 src/Test.Xunit/Test.Xunit.csproj`
- [x] `dotnet test --framework net10.0 src/Test.Nunit/Test.Nunit.csproj`
- [x] `dotnet run --framework net10.0 --project src/Test.Automated/Test.Automated.csproj`
- [ ] PostgreSQL automated suite with `LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING` set.
- [x] `cd sdk/js && npm test`
- [x] `cd sdk/js && npm run build`
- [x] `cd sdk/python && python -m pytest`
- [x] `cd dashboard && npm test`
- [x] `cd dashboard && npm run build`

Additional completed validation:

- [x] `dotnet build src/LiteGraph.sln -c Debug`
- [x] `dotnet test src/Test.Xunit/Test.Xunit.csproj -c Debug --no-build -f net8.0`
- [x] `dotnet test src/Test.Nunit/Test.Nunit.csproj -c Debug --no-build -f net8.0`
- [x] `dotnet build src/LiteGraph.sln -c Release`
- [x] `dotnet build sdk/csharp/src/LiteGraphSdk.sln -c Debug`
- [x] `dotnet run --project sdk/csharp/src/Test.Automated/Test.Automated.csproj -c Debug --no-build`
- [x] `dotnet build sdk/csharp/src/LiteGraphSdk.sln -c Release`

Database/provider matrix:

| Provider | Current support level | Required validation |
| --- | --- | --- |
| SQLite | Full | All five bulk insert operations, targeted node/edge hydration, REST, SDK default/minimal |
| PostgreSQL | Full | All five bulk insert operations, targeted node/edge hydration, REST, SDK default/minimal |
| MySQL | Placeholder | Factory and unsupported-operation tests only |
| SQL Server | Placeholder | Factory and unsupported-operation tests only |

Runtime surfaces:

| Surface | Required validation |
| --- | --- |
| In-process C# core client | Default full and minimal return mode for all create-many methods |
| REST server | Query parsing, response shape, invalid mode, auth unchanged |
| C# SDK | URL construction, overload compatibility |
| Python SDK | URL construction, `/bulk` route alignment, return mode |
| JavaScript SDK | URL construction, `/bulk` route alignment, return mode |
| MCP | Optional return mode if exposed, default unchanged |
| Dashboard API Explorer | Route metadata, examples, summaries |
| Postman | Query parameter and example responses |

## Acceptance Criteria

- [x] Every bulk insert endpoint accepts missing `return`, `return=full`, and `return=minimal`.
- [x] Every bulk insert endpoint rejects invalid return values consistently.
- [x] Default label, tag, vector, node, and edge bulk create behavior remains compatible.
- [x] `return=minimal` label bulk create persists labels and returns top-level label metadata.
- [x] `return=minimal` tag bulk create persists tags and returns top-level tag metadata.
- [x] `return=minimal` vector bulk create persists vectors, preserves vector index updates, and returns top-level vector metadata.
- [x] Default node bulk create still returns hydrated nodes, but uses targeted subordinate reads by created node GUIDs.
- [x] Default edge bulk create still returns hydrated edges, but uses targeted subordinate reads by created edge GUIDs.
- [x] `return=minimal` node bulk create returns top-level nodes only.
- [x] `return=minimal` edge bulk create returns top-level edges only.
- [x] Minimal mode does not skip validation, persistence, vector index updates, or authorization.
- [x] Minimal mode does not poison node or edge caches with under-hydrated objects.
- [ ] SQLite and PostgreSQL both pass the same behavior tests.
- [x] MySQL and SQL Server placeholder tests still pass.
- [x] REST docs, SDK docs, dashboard API Explorer, and Postman all show the new return mode for every bulk insert endpoint.
- [x] Existing clients that do not pass `return=minimal` keep current behavior.
- [x] SDK old call styles continue to compile/run.
- [x] Automated tests cover both response modes for every bulk insert endpoint.

## Rollout Notes

- [x] Keep `return=full` as the default for compatibility.
- [x] Advertise `return=minimal` for ingestion-heavy clients that do not need enriched create responses.
- [x] After release, update high-volume clients such as Pneuma to use minimal return mode for LiteGraph projection writes.
- [ ] Consider a later default change only in a major version if metrics show most clients do not rely on hydrated create responses.

## Open Questions

- [ ] Should minimal vector responses include embeddings?
  - Recommendation: yes, because embeddings are top-level vector fields and removing them would create a second response shape.
- [ ] Should minimal node/edge responses serialize subordinates as `null`, empty collections, or omitted properties?
  - Recommendation: use whatever the current serializer naturally emits for unpopulated model properties, then document it.
- [ ] Should `return=minimal` be surfaced in MCP for every create-many tool?
  - Recommendation: yes, if the MCP tool already exposes bulk insert.
- [ ] Should performance counters be added for return mode and subordinate rows read?
  - Recommendation: optional but useful. If added, document in `docs/OBSERVABILITY.md`.
