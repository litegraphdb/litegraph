<p align="center">
  <img src="https://raw.githubusercontent.com/jchristn/litegraph/main/assets/favicon.png" width="100" height="100" alt="LiteGraph">
</p>

# LiteGraph Test Coverage Audit (v8.1)

This document maps every externally visible surface of LiteGraph — REST routes and MCP tools — to the automated tests that exercise it. Coverage here is measured against surfaces, not lines: a surface counts as covered when an automated test drives it and asserts on the result. Full coverage of a surface means both a positive case (the operation succeeds and the result is verified) and a negative case (bad input, a missing resource, or a denied credential produces the expected failure). Where a surface has one but not the other, the table says so. Where I could not find a test at all, the Gap column says "no test found" — no coverage is claimed that could not be traced to a specific case.

The audit was produced by enumerating three inventories and cross-referencing them by hand:

1. **REST routes** from `src/LiteGraph.Server/Classes/RequestTypeEnum.cs` (204 enum values) and the route registrations in `RestServiceHandler.cs`, `RestServiceHandler.Chat.cs`, and `RestServiceHandler.Authorization.cs`. The registrations include ten surfaces with no enum value at all (six request-history routes, three token routes, and the Prometheus metrics route), which are audited here as first-class surfaces.
2. **MCP tools** from the `RegisterTool` / `ToolDefinition` registrations in `src/LiteGraph.McpServer/Registrations/*.cs` — 205 tools across 17 files.
3. **Test surfaces** from the Touchstone suites in `src/Test.Shared/` (run by `Test.Automated`, and wrapped as facts by `Test.Nunit` and `Test.Xunit`), plus the three SDK test trees (`sdk/csharp/src/Test.Automated`, `sdk/python/tests`, `sdk/js/test`).

One distinction matters throughout, and the tables are explicit about it. The Touchstone core suites (`Database.InMemory`, `Database.OnDisk`, `Database.Postgresql`) exercise the in-process `LiteGraphClient`, which is the same code the server routes call — but a core case does not cross the HTTP layer, so it validates the operation, not the route. The `Mcp.Server` suite, by contrast, launches a real `LiteGraph.Server` and a real `LiteGraph.McpServer` as child processes and drives tools over JSON-RPC; because the MCP server proxies every tool through the SDK to the REST API, each MCP case exercises the corresponding REST route end to end. The `Authorization`, `Chat.Rest`, `Observability`, and REST-transaction improvement cases also hit live routes directly. Where a surface's only coverage is in-process, the tables mark it "SDK layer" and the gap list calls out that the route itself is untested.

## Reference key

Suite/case references use these prefixes. Case IDs are exact strings from the suite factories.

| Key | Source |
|---|---|
| `Core` | `Database.InMemory` / `Database.OnDisk` / `Database.Postgresql` shared cases (in-process `LiteGraphClient`) — `LiteGraphTouchstoneSuites.cs` |
| `MCP` | `Mcp.Server` suite (live REST server behind a live MCP server) — `LiteGraphTouchstoneSuites.cs` |
| `Imp` | `Improvements.Foundation` — `LiteGraphTouchstoneImprovementSuites.cs` |
| `Auth` | `Authorization` suite (live REST) — `LiteGraphTouchstoneAuthorizationSuites.cs` |
| `Acct` | `Accounts` suite (in-process) — `LiteGraphTouchstoneAccountsSuites.cs` |
| `IE` | `ImportExport` suite (in-process) — `LiteGraphTouchstoneImportExportSuites.cs` |
| `Obs` | `Observability` suite (live REST + MCP metrics) — `LiteGraphTouchstoneObservabilitySuites.cs` |
| `Vec` | `Vector.Search` / `Vector.Index.Implementation` / `Vector.Index.Search` (in-process) — `LiteGraphTouchstoneVectorSuites.cs` |
| `ChatS` / `ChatR` | `Chat.Storage` (in-process) / `Chat.Rest` (live REST with `FakeLlmServer`) — `LiteGraphTouchstoneChatSuites.cs` |
| `RouteAuth` | `Routes.Authentication` parity snapshot (static source analysis of route registrations) |
| `SDK-C#` | `sdk/csharp/src/Test.Automated` (live REST via `LiteGraphSdk`) |
| `SDK-Py` / `SDK-JS` | `sdk/python/tests` (mocked client) / `sdk/js/test` (Jest + MSW mock server) — these validate the SDK clients, not the server routes |

Two blanket cases carry a large share of the negative coverage and are cited repeatedly. `Imp:Credentials.AuthorizationMcpBoundary` drives nearly the full MCP tool surface twice — once with an allowed credential and once with a denied one — so it supplies a route-level authorization-denial negative for every tool family it touches (nodes, edges, labels, tags, vectors, batch, tenant/user/credential CRUD, graph query/transaction/JSONL import-export, authorization tools, `admin_flush`). `Auth:Authorization.UnauthenticatedDenied` and `Obs:Observability.RestErrorCounter` cover the unauthenticated-rejection path. Cited as "denial" below.

## Tenants

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| TenantCreate (`PUT /v1.0/tenants`; `tenant_create`) | MCP:MCP.Tenant.Create; Auth:SystemAdminFullAccess; Core:Tenant.Create | Auth:TenantAdminScope (401); Core:Negative.Tenant.CreateNull (SDK) | |
| TenantRead (`GET .../tenants/{g}`; `tenant_get`) | MCP:MCP.Tenant.Get; Core:Tenant.ReadByGuid | Core:Negative.Tenant.ReadNonExistent (SDK null); denial | |
| TenantReadAll (`GET /v1.0/tenants`; `tenant_all`) | MCP:MCP.Tenant.All; SDK-C# | Obs:RestErrorCounter (unauthenticated 4xx); denial | |
| TenantEnumerate (`GET/POST /v2.0/tenants`; `tenant_enumerate`) | MCP:MCP.Tenant.Enumerate; Core:Tenant.Enumerate, Enumeration.Tenants.* | denial | |
| TenantExists (`HEAD .../tenants/{g}`; `tenant_exists`) | MCP:MCP.Tenant.Exists; Core:Tenant.ExistsByGuid | Core + MCP assert false for missing/deleted GUIDs | |
| TenantUpdate (`PUT .../tenants/{g}`; `tenant_update`) | MCP:MCP.Tenant.Update; Core:Tenant.Update | Core:Negative.Tenant.UpdateNull (SDK); denial | |
| TenantDelete (`DELETE .../tenants/{g}`; `tenant_delete`) | MCP:MCP.Tenant.Delete; ChatS:TenantCascade (force-delete cascade) | Core:Negative.Tenant.DeleteWithDependents (SDK); denial | |
| TenantStatistics (`GET .../tenants/stats`, `.../{g}/stats`; `tenant_statistics`, `tenant_statisticsall`) | MCP:MCP.Tenant.Statistics; Imp:McpBoundary (`statisticsall`); Core:Tenant.GetStatistics | denial | |

## Users

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| UserCreate (`PUT .../users`; `user_create`) | MCP:MCP.User.Create; Auth (provisioning); Acct:FlagsDefaultFalse; Core:User.Create | denial | |
| UserRead (`GET .../users/{g}`; `user_get`) | MCP:MCP.User.Get; Auth:RegularUserSelfService (200 own) | Auth:RegularUserSelfService (401 reading another user) | |
| UserReadAll (`GET .../users`; `user_all`) | MCP:MCP.User.All; Auth:SystemAdminFullAccess | Auth:RegularUserSelfService, UnauthenticatedDenied (401) | |
| UserEnumerate (`GET/POST /v2.0/.../users`; `user_enumerate`) | MCP:MCP.User.Enumerate; Core:User.Enumerate | denial | |
| UserExists (`HEAD .../users/{g}`; `user_exists`) | MCP:MCP.User.Exists; Core:User.ExistsByGuid/ExistsByEmail | Core asserts false for missing | |
| UserUpdate (`PUT .../users/{g}`; `user_update`) | MCP:MCP.User.Update; Auth:RegularUserSelfService (own); Acct:UpdateFlags | Auth:TenantAdminScope (cross-tenant 401) | |
| UserDelete (`DELETE .../users/{g}`; `user_delete`) | MCP:MCP.User.Delete | denial | |
| UserReadTenants (enum value) | — | — | Orphan enum value: no route resolves to it. The related surface is `GET /v1.0/token/tenants` (below). Finding, not a test gap. |

## Tokens and authentication

Routes with no `RequestTypeEnum` value; the MCP twins are the three `userauthentication/*` tools.

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| `GET /v1.0/token/tenants` (pre-auth; `userauthentication_gettenantsforemail`) | SDK-C# (get-tenants-for-email); RouteAuth asserts it is deliberately public | — | No negative case (unknown email); MCP tool untested. Needs a case. |
| `GET /v1.0/token` (`userauthentication_generatetoken`) | SDK-C# (generate-token) | — | No negative case (bad credentials); MCP tool untested. Needs a case. |
| `GET /v1.0/token/details` (`userauthentication_gettokendetails`) | SDK-C# (get-token-details) | — | No negative case (expired/garbage token); MCP tool untested. Needs a case. |

## Credentials

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| CredentialCreate (`PUT .../credentials`; `credential_create`) | MCP:MCP.Credential.Create; Auth (provisioning); Imp:Credentials.Scoped.Persistence | denial | |
| CredentialRead (`GET .../credentials/{g}`; `credential_get`) | MCP:MCP.Credential.Get; Core:Credential.ReadByGuid | denial | |
| CredentialReadAll (`GET .../credentials`; `credential_all`) | MCP:MCP.Credential.All | denial | |
| CredentialEnumerate (`GET/POST /v2.0/...`; `credential_enumerate`) | MCP:MCP.Credential.Enumerate; Core:Credential.Enumerate | denial | |
| CredentialExists (`HEAD .../credentials/{g}`; `credential_exists`) | MCP:MCP.Credential.Exists | MCP asserts false after delete | |
| CredentialUpdate (`PUT .../credentials/{g}`; `credential_update`) | MCP:MCP.Credential.Update; Imp:Scoped.Persistence | denial | |
| CredentialDelete (`DELETE .../credentials/{g}`; `credential_delete`) | MCP:MCP.Credential.Delete | denial | |
| CredentialReadByBearerToken (`GET /v1.0/credentials/bearer/{t}`; `credential_getbybearertoken`) | MCP:MCP.Credential.GetByBearerToken; Imp:AuthorizationMigrationCompatibility | denial | |
| CredentialDeleteAllInTenant (`DELETE .../credentials`; `credential_deleteallintenant`) | MCP:MCP.Credential.DeleteAllInTenant (exists-flip asserted) | denial | |
| CredentialDeleteByUser (`DELETE .../users/{u}/credentials`; `credential_deletebyuser`) | MCP:MCP.Credential.DeleteByUser (exists-flip asserted) | denial | |

## Authorization

All seventeen routes and all seventeen `authorization/*` MCP tools are driven — on both channels, with both permitted and denied credentials — by `Imp:Credentials.AuthorizationRoles.RestManagement` (201/200/204 success, 409 on built-in role mutation, 401 for viewer/unassigned credentials, 404 after delete). Depth behind the routes comes from the service-layer cases (`Imp:Credentials.Authorization*`: policy definitions, permission matrix over the full `RequestTypeEnum`, role storage, effective access, cache invalidation, legacy migration) and the audit-trail cases.

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| AuthorizationRoleCreate/Read/ReadAll/Update/Delete (5 routes; `authorization/role/*`) | Imp:RestManagement; Imp:AuthorizationRoleStorage (SDK) | Imp:RestManagement (409 built-in, 401, 404) | |
| UserRoleAssignmentCreate/Read/ReadAll/Update/Delete (5 routes; `authorization/userrole/*`) | Imp:RestManagement; Imp:AuthorizationRoleStorage | Imp:RestManagement | |
| CredentialScopeAssignmentCreate/Read/ReadAll/Update/Delete (5 routes; `authorization/credentialscope/*`) | Imp:RestManagement; Imp:AuthorizationRoleEffectiveAccess | Imp:RestManagement | |
| UserEffectivePermissionsRead (`GET .../users/{u}/permissions`; `authorization_user_permissions`) | Imp:RestManagement | Imp:RestManagement | |
| CredentialEffectivePermissionsRead (`GET .../credentials/{c}/permissions`; `authorization_credential_permissions`) | Imp:RestManagement | Imp:RestManagement | |

## Graphs

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| GraphCreate (`PUT .../graphs`; `graph_create`) | MCP:MCP.Graph.Create; Core:Graph.Create | denial | |
| GraphRead (`GET .../graphs/{g}`; `graph_get`) | MCP:MCP.Graph.Get; Core:Graph.ReadByGuid | Core:Negative.Graph.ReadNonExistent (SDK); denial | |
| GraphReadAll (`GET .../graphs`; `graph_all`) | MCP:MCP.Graph.All; Core:Graph.ReadMany | — | No explicit negative. |
| GraphReadAllInTenant (`GET .../graphs/all`; `graph_readallintenant`) | MCP:MCP.Graph.ReadAllInTenant; Core:Graph.ReadAllInTenant | — | No explicit negative. |
| GraphEnumerate (`GET/POST /v2.0/.../graphs`; `graph_enumerate`) | MCP:MCP.Graph.Enumerate; Core:Enumeration.Graphs.Paginated | — | No explicit negative. |
| GraphReadFirst (`POST .../graphs/first`; `graph_readfirst`) | MCP:MCP.Graph.ReadFirst; Core:Graph.ReadFirst | — | No explicit negative. |
| GraphSearch (`POST .../graphs/search`; `graph_search`) | MCP:MCP.Graph.Search | — | No explicit negative. |
| GraphExists (`HEAD .../graphs/{g}`; `graph_exists`) | MCP:MCP.Graph.Exists; Core:Graph.ExistsByGuid | Core asserts false for missing | |
| GraphUpdate (`PUT .../graphs/{g}`; `graph_update`) | MCP:MCP.Graph.Update; Core:Graph.Update | denial | |
| GraphDelete (`DELETE .../graphs/{g}`; `graph_delete`) | MCP:MCP.Graph.Delete | denial | |
| GraphDeleteAllInTenant (`DELETE .../graphs/all`; `graph_deleteallintenant`) | MCP:MCP.Graph.DeleteAllInTenant | — | No explicit negative. |
| GraphStatistics (`GET .../graphs/stats`, `.../{g}/stats`; `graph_statistics`) | MCP:MCP.Graph.Statistics; Core:Graph.GetStatistics | — | No explicit negative. |
| GraphSubgraph (`GET .../nodes/{n}/subgraph`; `graph_getsubgraph`) | SDK-C# (live route); IE:Extract.* (SDK depth: depth, direction, filters, cost, cap) | IE:Extract.NoStartNodeThrows / MissingStartNodeThrows / NegativeDepthThrows (SDK) | Route-level negative absent; MCP `graph_getsubgraph` untested. |
| GraphSubgraphStatistics (`GET .../subgraph/stats`; `graph_getsubgraphstatistics`) | SDK-C# (live route) | — | No negative; MCP tool untested. |
| GraphQuery (`POST .../graphs/{g}/query`; `graph_query`) | MCP:MCP.Graph.Query; Imp:Observability.RestQueryProfile (live REST); Imp:Query.* (SDK depth) | Imp:Credentials.AuthorizationAudit.RestDeniedQuery (401 + audit record); Imp:Query.Lexer/Parser/ParameterErrors | |

## Nodes

Every node surface is exercised end to end by the `Mcp.Server` suite (each tool proxies through the live REST route) and again at the SDK layer by the core suites; `Imp:Credentials.AuthorizationMcpBoundary` supplies the route-level denial negative for every tool listed.

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| NodeCreate (`node_create`) | MCP:MCP.Node.Create; Core:Node.Create | Core:Negative.Node.CreateNull / CreateInvalidGraph (SDK); denial | |
| NodeCreateMany (`node_createmany`) | MCP:MCP.Node.CreateMany; Core:Node.CreateMany (full + Minimal return modes) | denial | |
| NodeRead (`node_get`) | MCP:MCP.Node.Get; Core:Node.ReadByGuid | Core:Negative.Node.ReadNonExistent (SDK null); denial | |
| NodeReadAll (`node_all`) | MCP:MCP.Node.All; Core:Node.ReadMany; Imp:Transactions.Server.* (live REST) | denial | |
| NodeReadAllInGraph / NodeReadAllInTenant (`node_readallingraph`, `node_readallintenant`) | MCP:MCP.Node.ReadAllInGraph / ReadAllInTenant; Core | denial | |
| NodeEnumerate (`node_enumerate`) | MCP:MCP.Node.Enumerate; Core:Enumeration.Nodes.Paginated | denial | |
| NodeReadFirst (`node_readfirst`) | MCP:MCP.Node.ReadFirst; Core:Node.ReadFirst | denial | |
| NodeSearch (`node_search`) | MCP:MCP.Node.Search | denial | |
| NodeUpdate (`node_update`) | MCP:MCP.Node.Update; Core:Node.Update | denial | |
| NodeExists (`node_exists`) | MCP:MCP.Node.Exists; Core:Node.ExistsByGuid | asserted false after deletes | |
| NodeDelete (`node_delete`) | MCP:MCP.Node.Delete | denial | |
| NodeDeleteAll (`node_deleteall`) | MCP:MCP.Graph.Delete (teardown) | denial | |
| NodeDeleteMany (`node_deletemany`) | Imp:McpBoundary (allowed leg) | denial | |
| NodeDeleteAllInTenant (`node_deleteallintenant`) | MCP:MCP.Node.DeleteAllInTenant (exists-flip) | denial | |
| NodeReadMostConnected / NodeReadLeastConnected (`node_readmostconnected`, `node_readleastconnected`) | MCP:MCP.Node.ReadMostConnected / ReadLeastConnected; Core | denial | |

## Edges

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| EdgeCreate (`edge_create`) | MCP:MCP.Edge.Create; Core:Edge.Create | Core:Negative.Edge.CreateNull / CreateInvalidGraph (SDK); denial | |
| EdgeCreateMany (`edge_createmany`) | MCP:MCP.Edge.CreateMany; Core:Edge.CreateMany | denial | |
| EdgeRead (`edge_get`) | MCP:MCP.Edge.Get; Core:Edge.ReadByGuid | Core:Negative.Edge.ReadNonExistent (SDK null); denial | |
| EdgeReadAll / EdgeReadMany (`edge_all`, `edge_getmany`) | MCP:MCP.Edge.All / GetMany; Core:Edge.ReadMany | denial | |
| EdgeReadAllInGraph / EdgeReadAllInTenant (`edge_readallingraph`, `edge_readallintenant`) | MCP:MCP.Edge.ReadAllInGraph / ReadAllInTenant; Core | denial | |
| EdgeEnumerate (`edge_enumerate`) | MCP:MCP.Edge.Enumerate; Core:Edge.Enumerate | denial | |
| EdgeSearch (`edge_search`) | MCP:MCP.Edge.Search | denial | |
| EdgeBetween (`GET .../edges/between`; `edge_betweennodes`) | MCP:MCP.Edge.BetweenNodes; Core:Edge.ReadEdgesBetweenNodes | denial | |
| `POST .../edges/first` (`edge_readfirst`) | MCP:MCP.Edge.ReadFirst; Core:Edge.ReadFirst | denial | Enum quirk: resolves to `EdgeReadAll`; there is no `EdgeReadFirst` enum member (see Findings). |
| EdgeUpdate (`edge_update`) | MCP:MCP.Edge.Update; Core:Edge.Update | denial | |
| EdgeExists (`edge_exists`) | MCP:MCP.Edge.Exists; Core:Edge.ExistsByGuid | asserted false after deletes | |
| EdgeDelete (`edge_delete`) | Imp:McpBoundary (allowed leg) | denial | |
| EdgeDeleteAll (`edge_deleteallingraph`) | MCP:MCP.Edge.DeleteAllInGraph (exists-flip) | denial | |
| EdgeDeleteMany (`edge_deletemany`) | Imp:McpBoundary (allowed leg) | denial | |
| EdgeDeleteAllInTenant (`edge_deleteallintenant`) | MCP:MCP.Edge.DeleteAllInTenant (exists-flip) | denial | |
| EdgeDeleteNodeEdges (`DELETE .../nodes/{n}/edges`; `edge_deletenodeedges`) | — | — | No test found on either channel. Needs a case. |
| EdgeDeleteNodeEdgesMany (`DELETE .../nodes/edges/bulk`; `edge_deletenodeedgesmany`) | MCP:MCP.Edge.DeleteNodeEdgesMany (exists-flip) | denial | |

## Topology and traversal

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| EdgesFromNode / EdgesToNode (`edge_fromnode`, `edge_tonode`) | MCP:MCP.Edge.FromNode / ToNode; Core:Edge.ReadEdgesFromNode / ToNode | denial | |
| AllEdgesToNode (`GET/POST .../nodes/{n}/edges`; `edge_nodeedges`) | MCP:MCP.Edge.NodeEdges; Core:Edge.ReadNodeEdges | denial | |
| NodeParents / NodeChildren / NodeNeighbors (`node_parents`, `node_children`, `node_neighbors`) | MCP:MCP.Node.Parents / Children / Neighbors; Core:Node.ReadParents / ReadChildren / ReadNeighbors | denial | |
| GetRoutes (`POST .../graphs/{g}/routes`; `node_traverse`) | Imp:McpBoundary (allowed leg); SDK-C# (routes) | denial | |

## Labels

The `Mcp.Server` suite runs the complete label lifecycle over live routes — create, create-many, read, read-many by graph/node/edge, enumerate, exists, update, and all seven delete variants — with existence-flip assertions on the deletes. `Imp:McpBoundary` provides the denial negative for the family; the core suites repeat the lifecycle in-process.

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| LabelCreate / LabelCreateMany (`label_create`, `label_createmany`) | MCP:MCP.Label.Create / CreateMany; Core | denial | |
| LabelRead / LabelReadAll / LabelReadAllInTenant / LabelReadAllInGraph (`label_get`, `label_all`, `label_readallintenant`, `label_readallingraph`) | MCP + Core equivalents | denial | |
| LabelReadManyGraph / ManyNode / ManyEdge (`label/readmanygraph|node|edge`) | MCP:MCP.Label.ReadManyGraph / Node / Edge; Core | denial | |
| LabelEnumerate (`label_enumerate`) | MCP:MCP.Label.Enumerate; Core | denial | |
| LabelExists (`label_exists`) | MCP:MCP.Label.Exists; Core | asserted false after deletes | |
| LabelUpdate (`label_update`) | MCP:MCP.Label.Update; Core | denial | |
| LabelDelete / DeleteMany (`label_delete`, `label_deletemany`) | MCP:MCP.Label.Delete / DeleteMany | denial | |
| LabelDeleteAllInTenant / AllInGraph / GraphLabels / NodeLabels / EdgeLabels (5 delete-scope routes) | MCP:MCP.Label.DeleteAllInTenant / DeleteAllInGraph / DeleteGraphLabels / DeleteNodeLabels / DeleteEdgeLabels | denial | |

## Tags

Identical shape to Labels; the `Mcp.Server` suite runs the full tag lifecycle and `Imp:McpBoundary` supplies the denials.

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| TagCreate / TagCreateMany (`tag_create`, `tag_createmany`) | MCP:MCP.Tag.Create / CreateMany; Core | denial | |
| TagRead / TagReadAll / TagReadAllInTenant / TagReadAllInGraph (`tag_get`, `tag_readmany`, `tag_readallintenant`, `tag_readallingraph`) | MCP + Core equivalents | denial | |
| TagReadManyGraph / ManyNode / ManyEdge (`tag/readmanygraph|node|edge`) | MCP:MCP.Tag.ReadManyGraph / Node / Edge; Core | denial | |
| TagEnumerate (`tag_enumerate`) | MCP:MCP.Tag.Enumerate; Core | denial | |
| TagExists (`tag_exists`) | MCP:MCP.Tag.Exists; Core | asserted false after deletes | |
| TagUpdate (`tag_update`) | MCP:MCP.Tag.Update; Core | denial | |
| TagDelete / DeleteMany (`tag_delete`, `tag_deletemany`) | MCP:MCP.Tag.Delete / DeleteMany | denial | |
| TagDeleteAllInTenant / AllInGraph / GraphTags / NodeTags / EdgeTags (5 delete-scope routes) | MCP:MCP.Tag.DeleteAllInTenant / DeleteAllInGraph / DeleteGraphTags / DeleteNodeTags / DeleteEdgeTags | denial | MCP tool names for graph/node tag-scope deletes are registered as `tag_deletegraphlabels` and `tag_deletenodelabels` (see Findings). |

## Vectors

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| VectorCreate / VectorCreateMany (`vector_create`, `vector_createmany`) | MCP:MCP.Vector.Create / CreateMany; Core | Vec:DirtyRepair (dimensionality-mismatch failure path, SDK); denial | |
| VectorRead / ReadAll / ReadAllInTenant / ReadAllInGraph (`vector_get`, `vector_all`, `vector_readallintenant`, `vector_readallingraph`) | MCP + Core equivalents | denial | |
| VectorReadManyGraph / ManyNode / ManyEdge (`vector/readmanygraph|node|edge`) | MCP:MCP.Vector.ReadManyGraph / Node / Edge; Core | denial | |
| VectorEnumerate (`vector_enumerate`) | MCP:MCP.Vector.Enumerate; Core | denial | |
| VectorExists (`vector_exists`) | MCP:MCP.Vector.Exists; Core | asserted false after deletes | |
| VectorUpdate (`vector_update`) | MCP:MCP.Vector.Update; Core; Vec:DirtyRepair | denial | |
| VectorSearch (`POST .../vectors`, `.../vectors/search`; `vector_search`) | MCP:MCP.Vector.Search; Vec:CosineSimilarity, Lifecycle, RamVsSqlite (deterministic results, three distance metrics, RAM/SQLite parity); Core:Vector.Search | Vec:LegacySqliteArtifact / DirtyRepair (index-unavailable fallback, SDK); denial | |
| VectorDelete / DeleteMany (`vector_delete`, `vector_deletemany`) | MCP:MCP.Vector.Delete / DeleteMany | denial | |
| VectorDeleteAllInTenant / AllInGraph / GraphVectors / NodeVectors / EdgeVectors (5 delete-scope routes) | MCP:MCP.Vector.DeleteAllInTenant / DeleteAllInGraph / DeleteGraphVectors / DeleteNodeVectors / DeleteEdgeVectors | denial | |

### Vector index management

The five `vectorindex` route pairs (registered at both v1.0 and v2.0) have no live-route coverage anywhere; what exists is SDK-layer, and two of the five operations have no test at all.

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| GraphVectorIndexEnable (`PUT .../vectorindex/enable`; `graph_enablevectorindexing`) | Vec:Lifecycle, LegacySqliteArtifact, DirtyRepair; Imp:*VectorSearchMutation (all SDK layer) | Vec:LegacySqliteArtifact (legacy artifact marked dirty, SDK) | Route and MCP tool never driven over HTTP. |
| GraphVectorIndexStats (`GET .../vectorindex/stats`; `graph_getvectorindexstatistics`) | Vec:Lifecycle, DirtyRepair, RamVsSqlite (SDK layer) | Vec:DirtyRepair (dirty stats asserted) | Route and MCP tool never driven over HTTP. |
| GraphVectorIndexRebuild (`POST .../vectorindex/rebuild`; `graph_rebuildvectorindex`) | Vec:DirtyRepair (rebuild clears dirty, SDK layer) | — | Route and MCP tool never driven over HTTP. |
| GraphVectorIndexConfig (`GET .../vectorindex/config`; `graph_getvectorindexconfig`) | — | — | No test found. Vec:Configuration round-trips the `VectorIndexConfiguration` model but never reads config through client, route, or tool. Needs a case. |
| GraphVectorIndexDisable (`DELETE .../vectorindex`; `graph_deletevectorindex`) | — | — | No test found on any layer. Needs a case. |

## Batch

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| GraphExistence (`POST .../graphs/{g}/existence`; `batch_existence`) | Imp:McpBoundary (allowed leg, live); Core:Batch.Existence.EmptySiblingFilters / LargePayload (SDK: 600-item payloads, existing/missing bucketing) | Core cases assert missing GUIDs land in Missing buckets; denial | |

## Transactions

The transaction surface is the most heavily tested in the codebase: the `Transactions.*` cases in `Improvements.Foundation` cover commit, rollback, limits, cancellation, timeout, serializable conflicts, concurrency matrices on both providers, fault injection, and a committed-state oracle, and the vector suites add index-staging rollback.

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| GraphTransaction (`POST .../graphs/{g}/transaction`; `graph_transaction`) | Imp:Transactions.Server.SqliteRestConcurrency / PostgresqlRestConcurrency (live REST, 200 committed); MCP:MCP.Graph.Transaction (typed + serialized payloads); Imp:Transactions.Client.* and Correctness.* (SDK depth); Vec:TransactionRollbackStaging / ConcurrentTransactionStaging | Imp REST cases (409 duplicate rollback); MCP case (ValidationFailure on targetless delete); Imp:Client.Cancellation / Timeout / MixedRollbackAndLimits; denial | |

## Import/Export

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| GraphExport — GEXF (`GET .../export/gexf`; `graph_exportgexf`) | SDK-C# (live route); SDK-Py/JS (client side) | SDK-Py mixin gexf error (client side only) | No server-side negative; MCP `graph_exportgexf` untested. |
| GraphExportJsonl (`GET .../export/jsonl`; `graph_exportjsonl`) | Imp:McpBoundary (allowed leg, live); IE:Jsonl.RoundTripWholeGraph (SDK) | denial | |
| GraphExportSubgraphJsonl (`POST .../export/jsonl`; `graph_exportsubgraphjsonl`) | Imp:McpBoundary (allowed leg, live); SDK-Py (client side) | denial | |
| GraphImportJsonl (`POST .../{g}/import/jsonl`; `graph_importjsonl`) | Imp:McpBoundary (allowed leg, live); IE:Import.Merge* (SDK: skip/overwrite/regenerate merge modes) | IE:Import.PreserveCollisionThrows / MalformedAbortThrows / MergeMissingTargetThrows / DanglingEdgeDropped (SDK); denial | |
| GraphImportJsonlNew (`POST .../graphs/import/jsonl`) | IE:Jsonl.RoundTripWholeGraph, Import.CreateNewPreservesNodeGuids (SDK create-new semantics); SDK-JS (mock client) | IE:Import.MalformedAbortThrows, EmptyBodyCreatesEmptyGraph boundary (SDK) | The create-new route itself is never driven over HTTP; coverage is SDK-layer. |

## Backups and flush

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| Backup (`POST /v1.0/backups`; `admin_backup`) | MCP:MCP.Admin.Backup; SDK-C# | SDK-C# TestAdminBackupUnsupported (unsupported provider) | Enum-resolution mismatch on this route (see Findings). No invalid-request negative. |
| BackupReadAll (`GET /v1.0/backups`; `admin_backups`) | MCP:MCP.Admin.Backups; SDK-C# | SDK-C# unsupported-provider case | |
| BackupRead (`GET /v1.0/backups/{f}`; `admin_backupread`) | MCP:MCP.Admin.BackupRead; SDK-C# | — | No missing-filename negative. |
| BackupExists (`HEAD /v1.0/backups/{f}`; `admin_backupexists`) | MCP:MCP.Admin.BackupExists; SDK-C# | — | No missing-filename negative. |
| BackupDelete (`DELETE /v1.0/backups/{f}`; `admin_backupdelete`) | MCP:MCP.Admin.BackupDelete; SDK-C# | — | No missing-filename negative. |
| FlushDatabase (`POST /v1.0/flush`; `admin_flush`) | MCP:MCP.Admin.Flush; SDK-C# | denial (Imp:McpBoundary) | |

## Settings

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| SettingsRead (`GET /v1.0/settings`) | Auth:SettingsRoundTrip, SystemAdminFullAccess (live) | Auth:UnauthenticatedDenied, TenantAdminScope (401) | |
| SettingsUpdate (`PUT /v1.0/settings`) | Auth:SettingsRoundTrip (update + read-back) | Auth:SettingsDeniedForNonAdmin (401 for tenant admin and regular user) | |
| SettingsRestart (`POST /v1.0/settings/restart`) | — | Auth:SettingsDeniedForNonAdmin (401 for non-admin) | No positive case — deliberate: a successful restart kills the server under test. SDK-Py/JS cover the client call (incl. dropped-connection handling) against mocks. Accepted gap. |

## Request History

The six `/v1.0/requesthistory` routes have no `RequestTypeEnum` values and no route-level tests. The storage and service layers underneath are partially covered: `Imp:Observability.RequestHistoryCorrelation` exercises repository insert/read/read-detail/search with correlation, trace, and transaction-diagnostics filters, and `Imp:Observability.RequestHistoryRedaction` covers header redaction and body truncation in `RequestHistoryService`.

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| `GET /v1.0/requesthistory` (list) | Imp:RequestHistoryCorrelation (repository layer) | — | Route never driven over HTTP. Needs a case. |
| `GET /v1.0/requesthistory/{g}` (read) | Imp:RequestHistoryCorrelation (repository layer) | — | Route never driven over HTTP. Needs a case. |
| `GET /v1.0/requesthistory/{g}/detail` | Imp:RequestHistoryCorrelation (repository layer) | — | Route never driven over HTTP. Needs a case. |
| `GET /v1.0/requesthistory/summary` | — | — | No test found on any layer (the summary aggregation itself is untested). Needs a case. |
| `DELETE /v1.0/requesthistory/{g}` | — | — | No test found on any layer. Needs a case. |
| `DELETE /v1.0/requesthistory/bulk` | — | — | No test found on any layer. Needs a case. |

## Chat

The REST chat surface is tested by two complementary suites: `Chat.Storage` (nine cases, in-process, covering validation, sequencing, cascades, retention, and tenant isolation; runs against PostgreSQL as well when `LITEGRAPH_TEST_POSTGRESQL_CONNECTION_STRING` is set) and `Chat.Rest` (fifteen cases against a live server with a scripted `FakeLlmServer` upstream, covering CRUD with RBAC, completion happy path, tool loop, retry/exhaustion, SSE streaming, feedback incl. single reads and unknown-GUID negatives, thread ownership, endpoint test/health routes, metrics, MCP catalog parity, and an MCP chat-tool smoke pass).

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| ChatEndpointCreate (`PUT .../chat/endpoints`) | ChatR:EndpointCrudAndAuthz (key redacted on create); ChatS:EndpointCrud | ChatR (400 embeddings-only model; 401 non-admin); ChatS:EndpointValidation (4 invalid shapes) | |
| ChatEndpointReadAll (`GET .../chat/endpoints`) | ChatR:EndpointCrudAndAuthz (list hides raw key) | ChatR (401 non-admin) | |
| ChatEndpointRead (`GET .../chat/endpoints/{g}`) | ChatR:EndpointReadUpdateTest; ChatS:EndpointCrud (SDK) | ChatS:TenantIsolation (cross-tenant null, SDK) | |
| ChatEndpointExists (`HEAD .../chat/endpoints/{g}`) | ChatR:EndpointReadUpdateTest | ChatR:EndpointReadUpdateTest (404 unknown) | |
| ChatEndpointUpdate (`PUT .../chat/endpoints/{g}`) | ChatR:EndpointReadUpdateTest; ChatS:RedactedKeyPreserved (SDK) | — | Redacted-key-over-REST negative still SDK-layer only. |
| ChatEndpointDelete (`DELETE .../chat/endpoints/{g}`) | ChatR:EndpointCrudAndAuthz; ChatS:EndpointCrud | ChatR (401 non-admin) | |
| ChatEndpointTest (`POST .../chat/endpoints/{g}/test`) | ChatR:EndpointReadUpdateTest (reachable + model list vs `FakeLlmServer`) | — | |
| ChatEndpointHealthReadAll / HealthRead (`GET .../endpoints/health`, `.../{g}/health`) | ChatR:EndpointHealthRoutes (monitored endpoint reaches healthy vs `FakeLlmServer`) | ChatR:EndpointHealthRoutes (404 unknown endpoint) | |
| ChatCompletion (`POST .../chat/completions`) | ChatR:CompletionHappyPath, ToolLoop, Streaming (SSE frame ordering), RetryThenSuccess | ChatR:RetriesExhausted (502 + persisted failed turn); RetryThenSuccess (upstream 429s) | |
| ChatThreadCreate (`PUT .../chat/threads`) | ChatR:ThreadOwnership; ChatS:ThreadTurnLifecycle | ChatS (KeyNotFound on turn against unknown thread, SDK) | |
| ChatThreadReadAll (`GET .../chat/threads`) | ChatR:ThreadOwnership, RetriesExhausted | ChatR (other user's list omits thread) | |
| ChatThreadRead (`GET .../chat/threads/{g}`) | ChatR:ThreadOwnership (owner + admin) | ChatR (401/403 other user) | |
| ChatThreadDelete (`DELETE .../chat/threads/{g}`) | ChatR:ThreadOwnership; ChatS cascade | ChatR (401/403 other user) | |
| ChatThreadTurnsRead (`GET .../threads/{g}/turns`) | ChatR:CompletionHappyPath, RetriesExhausted; ChatS sequencing | ChatR:FeedbackReadAndNegatives (foreign-thread denial) | |
| ChatFeedbackCreate (`POST .../turns/{g}/feedback`) | ChatR:Feedback; ChatS:Feedback | ChatS (KeyNotFound on unknown turn, SDK) | |
| ChatFeedbackReadAll (`GET .../chat/feedback`) | ChatR:Feedback (admin list) | ChatR (401/403 regular user) | |
| ChatFeedbackRead (`GET .../chat/feedback/{g}`) | ChatR:FeedbackReadAndNegatives | ChatR:FeedbackReadAndNegatives (404 delete-unknown, 404 feedback-on-unknown-turn) | |
| ChatFeedbackDelete (`DELETE .../chat/feedback/{g}`) | ChatR:Feedback (admin delete) | — | No negative (non-admin delete attempt). |
| ChatSettingsRead (`GET .../chat/settings`) | ChatR:SettingsRoundTrip (admin + regular read); ChatS:Settings | — | |
| ChatSettingsUpdate (`PUT .../chat/settings`) | ChatR:SettingsRoundTrip; ChatS:Settings upsert | ChatR (401/403 regular user); ChatS (ArgumentException embedding-as-completion) | |

## MCP

The bulk of the 205 MCP tools are covered inside the domain tables above: the `Mcp.Server` suite executes 150+ tools end to end against a live REST-backed stack, and `Imp:Credentials.AuthorizationMcpBoundary` re-runs the broad tool surface under allowed and denied credentials. `Obs:McpMetricLabels` / `McpErrorCounter` add the metrics contract, including the generic failing-tool path, and `ChatR:ToolCatalogParity` asserts that all 32 chat-advertised tool names exist in the `tools/list` catalog. The table below covers the MCP-only surfaces (tools with no REST-enum twin) and the tool groups with no coverage.

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| `tenant_getmany`, `user_getmany`, `credential_getmany`, `graph_getmany`, `node_getmany`, `edge_getmany`, `label_getmany`, `tag_getmany`, `vector_getmany` | MCP:MCP.*.GetMany cases | denial (families in Imp:McpBoundary) | |
| `tenant_statisticsall` | Imp:McpBoundary (allowed leg) | denial | |
| `userauthentication_gettenantsforemail`, `generatetoken`, `gettokendetails` (3 tools) | — | — | No test found (REST twins are covered by SDK-C#). Needs a case. |
| `chat/*` (18 tools: endpoint CRUD/test/health, completions, threads, feedback, settings) | ChatR:McpChatTools smokes chat/settings/get, chat/endpoint/create|all|delete, chat/thread/all | — | Remaining 13 chat tools (get/update/test/health, completions, thread get/delete/turns, feedback tools, settings update) are thin proxies onto REST routes Chat.Rest covers; their argument marshalling is untested. |
| `graph_getsubgraph`, `graph_getsubgraphstatistics` | — | — | No test found (REST twins covered by SDK-C#). Needs a case. |
| `graph_exportgexf` | — | — | No test found (REST twin covered by SDK-C#). Needs a case. |
| `graph_enablevectorindexing`, `graph_rebuildvectorindex`, `graph_getvectorindexstatistics` | — | — | No test found; SDK-layer twins covered by Vec suites. |
| `graph_deletevectorindex`, `graph_getvectorindexconfig` | — | — | No test found on any layer (matches the route gap). Needs a case. |
| `edge_deletenodeedges` | — | — | No test found (matches the route gap). Needs a case. |
| Unknown-tool error path | — | Obs:McpErrorCounter (unregistered tool → error counter) | |

## General and infrastructure

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| Root (`GET /`) | Obs:RestMetricLabels (drives `GET /`) | — | Trivial static response; accepted. |
| Loopback (`HEAD /`) / Favicon (`GET /favicon.ico`) | — | — | No test found; trivial static passthroughs. Accepted gap. |
| Metrics (`GET /metrics`, unauthenticated) | Obs:RestMetricLabels, McpMetricLabels; Imp:Observability.MetricsEndpoint; ChatR:Metrics (chat counters, no tenant-GUID leak) | Obs:RestErrorCounter / McpErrorCounter (error counters increment) | |
| Route/auth-bucket parity | RouteAuth:ParitySnapshot (asserts exactly 4 public routes, 207 authenticated, no overlap, ~24 sensitive routes pinned to the authenticated bucket) | inherent (the case fails on any drift in either direction) | |

## Native AOT (v10.2)

| Surface | Positive | Negative | Gap? |
|---|---|---|---|
| JSON output of every model type (compact, indented, round trip) | `Aot.Serialization`: `Aot.SerializationParity.Compact`, `.Pretty`, `.RoundTrip` (byte-for-byte against baselines captured from 10.1) | — | — |
| Timestamps keep their UTC value through JSON in any time zone | `Aot.DateTimeUtc`; `SDK-C#`: `Serializer.DateTimeUtc`; `Test.Aot` timestamp check | Offset and zone-less inputs | — |
| Source-generated metadata covers every model type | `Aot.ContextCoverage` | — | — |
| JIT behavior for exceptions and unregistered types | `Aot.JitCompatibility` | — | — |
| GEXF export output | `Aot.GexfParity` (against the `XmlSerializer` baseline) | — | — |
| Library under Native AOT (SQLite and PostgreSQL) | `src/Test.Aot`, published with `PublishAot` and run in CI | Unregistered application type fails with `NotSupportedException`; failing transaction rolls back with a provider error code | — |
| C# SDK under Native AOT | `SDK-C#` suite published with `PublishAot=true` against a live server (157 cases), run in CI | Same negatives as the JIT run | — |
| REST and MCP servers under Native AOT | The full Touchstone run (net10.0) and the server-backed suites (net8.0) against native executables (`LITEGRAPH_TEST_SERVER_EXECUTABLE`, `LITEGRAPH_TEST_MCP_EXECUTABLE`) on Linux, macOS, and Windows in CI | Same negatives as the JIT run | — |
| Servers and tools serialize without reflection | Reflection-based System.Text.Json is off in every build of the servers and tools, so every server-backed case runs the AOT JSON path under the JIT too; `Aot.Server.ContextCoverage` | A type without metadata throws `NotSupportedException` | — |
| Server JSON output (types, default settings, chat stream events and tool transcripts, OpenAPI, seed data, chat tool schemas) | `Aot.Server`: `TypeParity`, `DefaultSettings`, `PayloadShapes`, `Live` (byte for byte against baselines captured before the server work) | — | — |
| SSL settings with a PFX certificate | `Aot.Server.SslSettings` (real certificate: serializes without the certificate object and round-trips) | — | — |
| Console tools under Native AOT | CI publishes `lg`, `LiteGraph.SampleDatabase`, and `LoadGenerator` natively and runs them (seed, query, load) | — | — |
| Docker deployments with native images | CI `deploy` job: all three deployments with `compose.native.yaml`, smoke, and multi-node failover | — | — |
| MCP tool names, descriptions, and schemas | `Mcp.Protocol.ToolsListBaseline` (211 tools, byte for byte against a baseline captured before the schemas moved from anonymous objects to JSON) | — | — |

## SDK client coverage (summary)

The three shipped SDKs carry their own test trees, which validate the clients rather than the server. `sdk/csharp/src/Test.Automated` is a live-server regression suite spanning every domain except chat and JSONL import/export; it is almost entirely happy-path (its one explicit negative is the unsupported-backup-provider case). `sdk/python/tests` runs against a mocked transport with strong negative coverage (~79 `pytest.raises`), covering CRUD generically through mixin tests plus dedicated chat, import/export, authorization, transaction, and query modules; its admin tests cover server settings only, not backups or flush. `sdk/js/test` (Jest + MSW) has a dedicated per-domain file for every surface including chat and JSONL import/export, with error-path assertions throughout except in the authorization, query, and traversal test files. Cross-SDK: chat and JSONL import/export are missing from the C# SDK tests; admin backups/flush are missing from Python and JS.

## Gaps

Every surface lacking positive or negative coverage, with a justification or a "needs a case" marker. Nothing below is claimed to be covered elsewhere unless the covering test is named.

**No test on any layer — needs a case:**

- `GraphVectorIndexDisable` (`DELETE .../vectorindex`, v1+v2, and MCP `graph_deletevectorindex`) — the only vector-index lifecycle operation with zero coverage.
- `GraphVectorIndexConfig` (`GET .../vectorindex/config`, v1+v2, and MCP `graph_getvectorindexconfig`) — the model round-trips in `Vec:Configuration`, but no test reads config through client, route, or tool.
- `EdgeDeleteNodeEdges` (`DELETE .../nodes/{n}/edges` and MCP `edge_deletenodeedges`) — the bulk variant is tested; the singular is not.
- `GET /v1.0/requesthistory/summary`, `DELETE /v1.0/requesthistory/{g}`, `DELETE /v1.0/requesthistory/bulk` — summary aggregation and both delete paths are untested at every layer.
- MCP `userauthentication/*` (3 tools), 13 of the `chat/*` tools, `graph_getsubgraph`, `graph_getsubgraphstatistics`, `graph_exportgexf` — proxies whose REST twins are tested, but the tools' own argument handling is not (five chat tools now have a smoke pass via ChatR:McpChatTools).

**Covered below the route only — route-level case would close the gap:**

- Vector-index enable/stats/rebuild: exercised thoroughly at the SDK layer (`Vec` suites, `Imp` mutation matrices) but never over HTTP on either channel.
- `GraphImportJsonlNew` (`POST .../graphs/import/jsonl`): create-new semantics covered by `IE` at the SDK layer; the dedicated route is never driven.
- Request-history list/read/detail routes: repository layer covered by `Imp:Observability.RequestHistoryCorrelation`; routes untested.

**Positive covered, negative missing:**

- Graph read/list family: `GraphReadAll`, `GraphReadAllInTenant`, `GraphEnumerate`, `GraphReadFirst`, `GraphSearch`, `GraphStatistics`, `GraphDeleteAllInTenant`, `GraphSubgraphStatistics` — no explicit negative; these graph tools are not part of the `Imp:McpBoundary` denial matrix, unlike their node/edge/label/tag/vector counterparts. Extending the boundary matrix to the graph read tools would close all of these at once.
- Backup read/exists/delete: no missing-filename (404) case; the only backup negative is the unsupported-provider case in SDK-C#.
- Token routes (`/v1.0/token`, `/token/details`, `/token/tenants`): no bad-credential, expired-token, or unknown-email case.
- GEXF export: no server-side negative (Python covers a client-side error only).
- `ChatFeedbackDelete`: no non-admin negative; `ChatSettingsRead`: no negative (benign — read is intentionally open to tenant users).

**Justified — no case wanted:**

- `SettingsRestart` positive: a passing restart tears down the server under test; the denial negative exists and SDK clients cover the call shape against mocks.
- `Loopback` / `Favicon`: static one-liners; `Root` is asserted via the observability suite.
- `UserReadTenants` enum value: an orphan with no route (see Findings) — there is nothing to test until it is wired or removed.

## Findings (surface inventory defects noticed during the audit)

These are inventory-level defects, not test gaps, but they belong in the record because they affect what "covered" means:

1. `POST /v1.0/backups` is the registered route, but `UrlContext` maps only the singular `POST /v1.0/backup` to `RequestTypeEnum.Backup` — so backup creation resolves to `Unknown` at the enum layer (observability route labels and authorization classification see `Unknown`, not `Backup`).
2. `RequestTypeEnum.UserReadTenants` is an orphan: no route resolves to it. The related public route `GET /v1.0/token/tenants` resolves to `Unknown`.
3. `RequestTypeEnum.EdgeReadMany` carries `[EnumMember(Value = "EdgeReadFirst")]` — a copy-paste artifact; there is no distinct `EdgeReadFirst` member, and `POST .../edges/first` resolves to `EdgeReadAll`.
4. The MCP tag scope-delete tools for graph and node are registered as `tag_deletegraphlabels` and `tag_deletenodelabels` (the "labels" suffix), while the edge variant is `tag_deleteedgetags`. Tests call them verbatim, so they are covered — under the misleading names.
5. The six request-history routes and three token routes have no `RequestTypeEnum` values, which keeps them outside the authorization permission matrix that `Imp:Credentials.AuthorizationPermissionMatrix` verifies.

## Totals

| Measure | Count |
|---|---|
| REST surfaces audited (202 enum-valued excluding `Unknown`/orphan + 6 request-history + 3 token + metrics) | 212 |
| REST surfaces with positive coverage (11 of these only below the HTTP layer, as flagged above) | 200 |
| REST surfaces with no positive coverage | 12 |
| MCP tools audited | 205 |
| MCP tools with positive coverage | 175 |
| MCP tools with no coverage | 30 |
| Surfaces flagged in the Gaps section (needs-a-case or partial) | 24 needs-a-case (route/tool groups) + 16 negative-only gaps + 4 justified |

The single highest-leverage additions are a route-level suite for the ten `vectorindex` registrations (which would also close the two zero-coverage operations), an MCP smoke pass over the 24 untested proxy tools, and adding the graph read tools to the `AuthorizationMcpBoundary` denial matrix.
