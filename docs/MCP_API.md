# MCP API for LiteGraph

The LiteGraph MCP server exposes the graph database as a set of Model Context Protocol tools so that Claude, Claude Code, Cursor, and other MCP-compatible clients can create, query, and manage graph data through tool calls. It is a thin, stateless layer: each tool validates its arguments and forwards to the LiteGraph REST server over the configured endpoint and bearer token, then returns the REST response verbatim. Nothing is cached in the MCP process, so a tool call sees exactly what the REST API sees.

For the setup walkthrough (build, install, start, and connect Claude), see [Using Claude with LiteGraph](CLAUDE_MCP.md). For the underlying HTTP contract that every tool wraps, see the [REST API](REST_API.md).

## Transports

The server is built on Voltaic 2.x and listens on three transports at once. All three expose the same operations under the same names; pick whichever fits the client. `tools/list` publishes only LiteGraph tools; Voltaic's demo tools (`echo`, `getTime`, `getSessions`, `getClients`) are not exposed. The protocol `ping` method is always available and returns `{}`.

| Transport | Default endpoint | Notes |
|-----------|------------------|-------|
| HTTP (MCP) | `http://localhost:8702/mcp` | MCP Streamable HTTP. Use this URL for Claude Code and other MCP clients; it supports every MCP revision from `2024-11-05` through the stateless `2026-07-28` |
| HTTP (JSON-RPC) | `http://localhost:8702/rpc` | JSON-RPC over HTTP POST, with server-sent events at `/events`. Tools are called through `tools/call`; MCP clients that negotiate `2026-07-28` (such as Claude Code 2.1.x) must use `/mcp` |
| TCP | `localhost:8703` | Raw JSON-RPC over a socket; tools are called by their bare name as the JSON-RPC `method` |
| WebSocket | `ws://localhost:8704/mcp` | JSON-RPC over a WebSocket; tools are called by their bare name as the JSON-RPC `method` |

Hostnames and ports are configurable through `litegraph-mcp.json` or the `MCP_HTTP_*`, `MCP_TCP_*`, and `MCP_WS_*` environment variables. The LiteGraph endpoint and API key the server forwards to are set with `LITEGRAPH_ENDPOINT` and `LITEGRAPH_API_KEY`.

As of v8.0 the MCP server also exposes a Prometheus `/metrics` endpoint (default port `8705`, set with `MCP_METRICS_HOSTNAME`/`MCP_METRICS_PORT`). It emits the same metric names as the REST server tagged with `component="mcp"`, plus `transport` and `tool` labels, so REST and MCP request rate, latency, and errors appear in one Grafana view. See `OBSERVABILITY.md`. Server settings are managed only through the REST `/v1.0/settings` endpoints (see `REST_API.md`); there is no MCP settings tool.

## Request And Response Envelope

Every call is a JSON-RPC 2.0 request. On the HTTP transport (`/mcp` and `/rpc`) a tool is invoked through the MCP `tools/call` method, with the tool name in `params.name` and the tool's argument object in `params.arguments`. The standard MCP methods (`initialize`, `server/discover`, `ping`, `tools/list`, `tools/call`) are available for clients that enumerate tools before calling them. `tools/list` is paginated (100 tools per page); follow `nextCursor` to read the full catalog. `tools/call` validates arguments against each tool's input schema, so a missing required argument, or an argument of the wrong JSON type, is rejected before the tool runs. As of v10.1 (Voltaic 2.2, following MCP 2025-11-25) the rejection is a tool result with `isError: true` whose text names the argument, so a model can correct the call; earlier releases returned a `-32602` error.

As of v10.1, tool names use underscores (`graph_get`, `chat_endpoint_all`) instead of slashes (`graph/get`, `chat/endpoint/all`). The MCP specification allows only letters, digits, `_`, `-`, and `.` in tool names, and Voltaic 2.2 rejects any other name at registration. To migrate a client, replace each `/` in a tool name with `_`; the arguments and results are unchanged.

As of Voltaic 2.0, calling a tool by its bare name over HTTP (for example `"method": "graph_get"`) returns `-32601` (method not found). The TCP and WebSocket transports still accept the bare form, with the tool name as `method` and the argument object as `params`; the examples below use the `tools/call` form, and on TCP or WebSocket the `arguments` object moves to `params` unchanged. Every JSON-RPC result must be a JSON object, so as of v10.1 a bare TCP or WebSocket call returns the same tool result `tools/call` returns (`{"content":[{"type":"text","text":"..."}]}`) instead of a bare string or boolean.

Voltaic rate-limits tool calls per client. The LiteGraph MCP server turns this off by default; set `ToolCallsPerSecond` in `litegraph-mcp.json` (0 to 1,000,000, default 0 for no limit) to apply a limit on all three transports. A call over the limit returns a tool result with `isError: true`. Under the stateless `2026-07-28` revision `ping` is not defined and returns `-32601`; use `server/discover` instead.

Request:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "tools/call",
  "params": {
    "name": "graph_get",
    "arguments": {
      "tenantGuid": "00000000-0000-0000-0000-000000000000",
      "graphGuid": "00000000-0000-0000-0000-000000000000",
      "includeData": true
    }
  }
}
```

Response:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "content": [
      { "type": "text", "text": "{ ...serialized graph JSON... }" }
    ]
  }
}
```

Most tools return the REST payload as a JSON string in the text content (`result` itself on TCP and WebSocket); a handful return `true`/`false` or an empty string for operations that have no body (deletes, flushes, index rebuilds). When a tool's arguments are invalid or the REST call fails, the server returns a JSON-RPC `error` object with a message describing the failure.

| Code | Meaning |
|------|---------|
| `-32602` | Invalid arguments: schema validation failure, malformed value, or a REST `400` (for example `LiteGraph endpoint returned 400 Bad Request: No graph with GUID '...' exists.`) |
| `-32001` | REST `401 Unauthorized` |
| `-32003` | REST `403 Forbidden` |
| `-32004` | REST `404 Not Found` |
| `-32009` | REST `409 Conflict` |
| `-32000` | Any other REST failure, such as a `5xx` |
| `-32601` | Unknown method, including a bare tool name over HTTP |
| `-32603` | Unexpected failure; the message carries the cause |

For REST failures, `error.data` is `{ "statusCode": <HTTP status>, "description": "<LiteGraph error description>" }`. Argument names are camelCase. Complex request bodies (search requests, enumeration queries, subgraph extraction, vector index configuration) are passed as a JSON string in a single argument rather than as nested objects, which keeps the tool schemas flat and predictable.

## List Tools, Paging, And getmany

As of v8.1 no tool returns a bare JSON array. Every list tool returns the REST `EnumerationResult` envelope, serialized as a JSON string in `result`:

```json
{
  "Success": true,
  "Timestamp": { ... },
  "MaxResults": 1000,
  "ContinuationToken": null,
  "EndOfResults": true,
  "TotalRecords": 17,
  "RecordsRemaining": 0,
  "Objects": [ ... ]
}
```

`Objects` carries the page of records; `TotalRecords`, `RecordsRemaining`, and `EndOfResults` describe progress through the full result set; `ContinuationToken` is populated when marker-based continuation is available.

Paging arguments follow one convention across the catalog:

| Argument | Type | Default | Notes |
|----------|------|---------|-------|
| `maxResults` | integer | `1000` | Maximum results per page, 1-1000; accepted by every list tool |
| `skip` | integer | `0` | Records to skip before the page begins, where the tool declares it |
| `order` | string | `CreatedDescending` | Enumeration order, where the tool declares it |
| `continuationToken` | string (GUID) | none | Marker-based continuation from a previous response's `ContinuationToken`, on marker-backed list tools (the `all` / `readallintenant` / `readallingraph` reads of the core record families and the chat list tools) |

Tools that do not declare `continuationToken` page with `skip`. The `enumerate` tools carry the same controls inside their `Enumeration Query` JSON-string argument (`MaxResults`, `ContinuationToken`, `Ordering`). The `authorization/*/all` list tools retain their legacy `page`/`pageSize` filter arguments but still return the envelope.

Every ordering is deterministic: records that share a sort value (for example, objects created in the same microsecond, or duplicate names or costs) are ordered by their GUID in the same direction as the requested order, so paging with either `skip` or `continuationToken` returns each record exactly once.

All nine `*/getmany` tools (`tenant`, `user`, `credential`, `graph`, `node`, `edge`, `label`, `tag`, `vector`) take an array of GUIDs, proxy it to the REST `?guids=` filter, accept `maxResults`, and return the envelope. Passing an empty GUID array is rejected with a JSON-RPC error — at least one GUID is required.

The intentional exceptions mirror REST: single-object reads, statistics objects, settings, effective-permissions composites, export streams, vector index configuration/statistics, and the search tools (`graph_search`, `node_search`, `edge_search` return `SearchResult`-shaped objects; `vector_search` returns the envelope of scored matches).

## Tool Catalog

Tools are grouped by resource. The name before the underscore is the family (for example `chat_endpoint` in `chat_endpoint_all`); the name after it is the operation. Families follow the same verbs, so once you know `node_get`, `node_create`, `node_search`, and `node_enumerate`, the other families read the same way. Every tool described as listing or paging records returns the `EnumerationResult` envelope and takes the paging arguments described in [List Tools, Paging, And getmany](#list-tools-paging-and-getmany).

### graph/*

Graph lifecycle, search, statistics, export and import, subgraph extraction, and vector index management.

| Tool | Purpose |
|------|---------|
| `graph_create`, `graph_get`, `graph_update`, `graph_delete` | Graph CRUD |
| `graph_all`, `graph_readallintenant`, `graph_getmany`, `graph_enumerate` | List and page graphs |
| `graph_search`, `graph_readfirst`, `graph_exists`, `graph_statistics` | Search, existence, and statistics |
| `graph_deleteallintenant` | Delete every graph in a tenant |
| `graph_getsubgraph`, `graph_getsubgraphstatistics` | Node-rooted subgraph read and its statistics |
| `graph_exportgexf` | Render a graph as GEXF |
| `graph_exportjsonl`, `graph_exportsubgraphjsonl`, `graph_importjsonl` | Streaming JSONL export and import (see below) |
| `graph_enablevectorindexing`, `graph_rebuildvectorindex`, `graph_deletevectorindex`, `graph_getvectorindexconfig`, `graph_getvectorindexstatistics` | HNSW vector index management |
| `graph_query`, `graph_transaction` | Native graph query and graph-scoped transaction |

`graph_query` takes `tenantGuid`, `graphGuid`, and either a full `request` object/string or the convenience fields `query`, `parameters`, `maxResults`, `timeoutSeconds`, and `maxScanRows`. It forwards to the REST query endpoint, so the same authentication and credential-scope checks apply. `maxResults` bounds the returned page; `maxScanRows` (default `1000000`, `0` disables it) bounds *global* operations — aggregates and `ORDER BY` — which are evaluated over the whole matching set, and a global query that exceeds it is rejected rather than silently truncated. The query text may **chain** multiple `MATCH` clauses and `WITH` stages terminated by a single `RETURN`; intermediate join/grouping sets are bounded by `maxScanRows` too. See [DSL.md](DSL.md#query-chaining-multiple-match-and-with).

### node/*

Node CRUD plus traversal and connectivity helpers.

| Tool | Purpose |
|------|---------|
| `node_create`, `node_createmany`, `node_get`, `node_getmany`, `node_update`, `node_delete` | Node CRUD and batch create |
| `node_all`, `node_readallingraph`, `node_readallintenant`, `node_enumerate` | List and page nodes |
| `node_search`, `node_readfirst`, `node_exists` | Search and existence |
| `node_deleteall`, `node_deleteallintenant`, `node_deletemany` | Bulk delete |
| `node_neighbors`, `node_parents`, `node_children`, `node_traverse` | Traversal |
| `node_readmostconnected`, `node_readleastconnected` | Connectivity ranking |

### edge/*

Edge CRUD plus node-relative edge lookups.

| Tool | Purpose |
|------|---------|
| `edge_create`, `edge_createmany`, `edge_get`, `edge_getmany`, `edge_update`, `edge_delete` | Edge CRUD and batch create |
| `edge_all`, `edge_readallingraph`, `edge_readallintenant`, `edge_enumerate` | List and page edges |
| `edge_search`, `edge_readfirst`, `edge_exists`, `edge_betweennodes` | Search, existence, and edges between two nodes |
| `edge_fromnode`, `edge_tonode`, `edge_nodeedges` | Edges by endpoint |
| `edge_deleteallingraph`, `edge_deleteallintenant`, `edge_deletemany`, `edge_deletenodeedges`, `edge_deletenodeedgesmany` | Bulk and node-scoped delete |

### label/*, tag/*, vector/*

Metadata attached to graphs, nodes, and edges. The three families share a shape: create (single and `createmany`), read (`get`, `getmany`, `all`, `readallingraph`, `readallintenant`, `enumerate`, and per-parent `readmany*` reads), `update`, `exists`, and a set of scoped deletes (`delete`, `deletemany`, `deleteallingraph`, `deleteallintenant`, and per-parent deletes). `vector/*` adds `vector_search` for similarity search, which uses the graph's HNSW index when one is enabled and falls back to a linear scan otherwise.

### tenant/*, user/*, credential/*

Multi-tenant administration and authentication records.

| Tool | Purpose |
|------|---------|
| `tenant_create`, `tenant_get`, `tenant_getmany`, `tenant_all`, `tenant_enumerate`, `tenant_update`, `tenant_delete`, `tenant_exists` | Tenant CRUD and listing |
| `tenant_statistics`, `tenant_statisticsall` | Tenant statistics |
| `user_create`, `user_get`, `user_getmany`, `user_all`, `user_enumerate`, `user_update`, `user_delete`, `user_exists` | User CRUD and listing; the user object carries the v8.0 `isSystemAdmin`/`isTenantAdmin` flags |
| `credential_create`, `credential_get`, `credential_getmany`, `credential_all`, `credential_enumerate`, `credential_update`, `credential_delete`, `credential_exists` | Credential CRUD and listing |
| `credential_getbybearertoken`, `credential_deletebyuser`, `credential_deleteallintenant` | Credential lookup and scoped delete |

### authorization/*

RBAC roles, user-role assignments, credential scopes, and effective-permission inspection.

| Tool | Purpose |
|------|---------|
| `authorization/role/*` | Role CRUD (`create`, `get`, `all`, `update`, `delete`) |
| `authorization/userrole/*` | User-to-role assignment CRUD |
| `authorization/credentialscope/*` | Credential scope CRUD |
| `authorization_user_permissions`, `authorization_credential_permissions` | Effective permissions for a user or credential |

### admin/*, batch/*, userauthentication/*

Operational and authentication utilities.

| Tool | Purpose |
|------|---------|
| `admin_backup`, `admin_backups`, `admin_backupread`, `admin_backupexists`, `admin_backupdelete` | Binary database backup management |
| `admin_flush` | Flush an in-memory database to disk |
| `batch_existence` | Batch existence check for nodes, edges, and edges-between |
| `userauthentication_generatetoken`, `userauthentication_gettokendetails`, `userauthentication_gettenantsforemail` | Security token issuance and lookup |

### cluster/* (v10.0)

Read-only views of the node registry. Tools that change cluster state (restarting or removing nodes) are deliberately not exposed over MCP; use the REST API or the dashboard's Cluster page. All three require a system administrator token.

| Tool | Arguments | Purpose |
|------|-----------|---------|
| `cluster_status` | none | Summary: whether the server runs as a cluster, cluster name, whether Redis is reachable, `NodesTotal`, `NodesByState`, `NodesRestartPending`, `NodesBehindSettings`, and the settings and restart versions |
| `cluster_nodes` | `state` (optional, case-insensitive) | The `GET /v1.0/cluster/nodes` body, optionally filtered to one state |
| `cluster_node` | `nodeId` | One node's registry entry, or `null` when the node is not registered |

On a single node, the answering server is the only node. The MCP server itself runs as one instance even in a cluster: it keeps MCP sessions in memory and holds no LiteGraph data, so a second instance would add capacity but nothing else, and the Docker deployment points its one instance at the load balancer.

### chat/*

The v8.1 LLM chat surface: upstream endpoint management, completions, threads, feedback, and per-tenant chat settings. See [Chat Tools](#chat-tools) below for arguments and examples.

| Tool | Purpose |
|------|---------|
| `chat_endpoint_create`, `chat_endpoint_get`, `chat_endpoint_all`, `chat_endpoint_update`, `chat_endpoint_delete` | Chat endpoint CRUD |
| `chat_endpoint_test` | Upstream connectivity test |
| `chat_endpoint_health`, `chat_endpoint_healthall` | Background health-check status |
| `chat_completions` | Non-streaming chat completion |
| `chat_thread_all`, `chat_thread_get`, `chat_thread_delete`, `chat_thread_turns` | Thread listing, read, delete, and turn history |
| `chat_feedback_create`, `chat_feedback_all`, `chat_feedback_delete` | Turn feedback |
| `chat_settings_get`, `chat_settings_update` | Per-tenant chat settings |

## JSONL Export And Import Tools

Three graph tools move a graph, or a slice of one, as newline-delimited JSON. They mirror the four REST JSONL endpoints, and the [REST API](REST_API.md) documents the record envelope, the `SubgraphExtractionRequest` fields, the `GraphImportResult` fields, and the GUID strategies in full. The notes below cover the MCP argument shape.

### graph/exportjsonl

Renders an entire graph as JSONL and returns it as a string. This is also the portable per-graph backup path.

| Argument | Type | Required | Default | Notes |
|----------|------|----------|---------|-------|
| `tenantGuid` | string (GUID) | yes | — | Owning tenant |
| `graphGuid` | string (GUID) | yes | — | Graph to export |
| `includeData` | boolean | no | `false` | Include each record's `Data` object |
| `includeSubordinates` | boolean | no | `false` | Include labels, tags, and vectors |

```json
{
  "jsonrpc": "2.0",
  "id": 10,
  "method": "tools/call",
  "params": {
    "name": "graph_exportjsonl",
    "arguments": {
      "tenantGuid": "00000000-0000-0000-0000-000000000000",
      "graphGuid": "00000000-0000-0000-0000-000000000000",
      "includeData": true,
      "includeSubordinates": true
    }
  }
}
```

```json
{
  "jsonrpc": "2.0",
  "id": 10,
  "result": {
    "content": [
      { "type": "text", "text": "# litegraph-jsonl v1\n# kind: graph-backup\n{\"Type\":\"Graph\",\"Object\":{\"GUID\":\"00000000-0000-0000-0000-000000000000\",\"Name\":\"Default graph\"}}\n{\"Type\":\"Node\",\"Object\":{\"GUID\":\"11111111-1111-1111-1111-111111111111\",\"Name\":\"Ada\"}}\n{\"Type\":\"Edge\",\"Object\":{\"GUID\":\"22222222-2222-2222-2222-222222222222\",\"From\":\"11111111-1111-1111-1111-111111111111\",\"To\":\"33333333-3333-3333-3333-333333333333\"}}" }
    ]
  }
}
```

### graph/exportsubgraphjsonl

Extracts a subgraph from one or more start nodes and returns it as JSONL. The `request` argument is a `SubgraphExtractionRequest` serialized to a JSON string, the same object the `POST .../export/jsonl` REST endpoint accepts.

| Argument | Type | Required | Notes |
|----------|------|----------|-------|
| `tenantGuid` | string (GUID) | yes | Owning tenant |
| `graphGuid` | string (GUID) | yes | Graph to extract from |
| `request` | string (JSON) | yes | Serialized `SubgraphExtractionRequest` |

```json
{
  "jsonrpc": "2.0",
  "id": 11,
  "method": "tools/call",
  "params": {
    "name": "graph_exportsubgraphjsonl",
    "arguments": {
      "tenantGuid": "00000000-0000-0000-0000-000000000000",
      "graphGuid": "00000000-0000-0000-0000-000000000000",
      "request": "{\"StartNodeGUIDs\":[\"11111111-1111-1111-1111-111111111111\"],\"MaxDepth\":2,\"Direction\":\"Both\",\"IncludeData\":true}"
    }
  }
}
```

The result is a JSONL string carrying the `subgraph` kind in its header, followed by the graph record, the reached node records, and the edge records among them.

### graph/importjsonl

Reads a JSONL body back into the store and returns a `GraphImportResult` string. Supplying `graphGuid` merges into that existing graph; omitting it creates a new graph in the tenant.

| Argument | Type | Required | Default | Notes |
|----------|------|----------|---------|-------|
| `tenantGuid` | string (GUID) | yes | — | Target tenant |
| `graphGuid` | string (GUID) | no | — | Target graph; omit to create a new graph |
| `jsonl` | string | yes | — | Raw JSONL body |
| `guidStrategy` | string | no | `regenerate` | `preserve`, `regenerate`, `skip`, or `overwrite` |
| `onError` | string | no | `abort` | `abort` or `skip` |
| `batchSize` | integer | no | `1000` | Nodes buffered per insert batch |

```json
{
  "jsonrpc": "2.0",
  "id": 12,
  "method": "tools/call",
  "params": {
    "name": "graph_importjsonl",
    "arguments": {
      "tenantGuid": "00000000-0000-0000-0000-000000000000",
      "guidStrategy": "regenerate",
      "onError": "abort",
      "batchSize": 1000,
      "jsonl": "# litegraph-jsonl v1\n{\"Type\":\"Graph\",\"Object\":{\"Name\":\"Copy\"}}\n{\"Type\":\"Node\",\"Object\":{\"GUID\":\"11111111-1111-1111-1111-111111111111\",\"Name\":\"Ada\"}}"
    }
  }
}
```

```json
{
  "jsonrpc": "2.0",
  "id": 12,
  "result": {
    "content": [
      { "type": "text", "text": "{\"Success\":true,\"TenantGUID\":\"00000000-0000-0000-0000-000000000000\",\"GraphGUID\":\"9de1f1a2-4b8c-4f7a-9a1b-2c3d4e5f6a7b\",\"GraphsCreated\":1,\"NodesCreated\":1,\"EdgesCreated\":0,\"LinesRead\":2,\"LinesIgnored\":1,\"Warnings\":[],\"GuidMap\":{}}" }
    ]
  }
}
```

Under `regenerate`, the `GuidMap` in the result maps each original GUID to the fresh GUID it received, so a caller can correlate the source records with what landed in the new graph. Under `preserve`, a GUID that already exists in the store fails the import, which is why `preserve` fits a restore into an empty database rather than a merge.

## Chat Tools

The chat tools wrap the LiteGraph v8.1 chat REST surface (see the [REST API](REST_API.md)) and follow the same conventions as the rest of the catalog: every tool takes `tenantGuid`, complex bodies travel as a JSON string in a single argument, and results are the REST payload serialized as a JSON string. The five list tools — `chat_endpoint_all`, `chat_endpoint_healthall`, `chat_thread_all`, `chat_thread_turns`, and `chat_feedback_all` — return the paginated `EnumerationResult` envelope and take `skip`, `maxResults`, and `continuationToken` per [List Tools, Paging, And getmany](#list-tools-paging-and-getmany). Server-side authorization applies as it does over REST: endpoint management, feedback listing and deletion, chat settings update, and all-users thread listing require an admin principal, while completions, thread creation, and feedback submission require a user principal — the admin break-glass token is rejected for those with a 400.

### chat/endpoint/create, chat/endpoint/update

Create or update a chat endpoint, the record describing an upstream completion or embedding provider (OpenAI or OpenAI-compatible, Ollama, Gemini, Anthropic for completions, VoyageAI for embeddings). The `endpoint` argument is a `ChatEndpoint` serialized to a JSON string; on update, its `GUID` identifies the endpoint to replace. API keys are redacted to their last four characters in every response, and sending a redacted value back on update preserves the stored key.

| Argument | Type | Required | Notes |
|----------|------|----------|-------|
| `tenantGuid` | string (GUID) | yes | Owning tenant |
| `endpoint` | string (JSON) | yes | Serialized `ChatEndpoint`; `Name`, `Endpoint`, and `Model` are required, `EndpointType` is `Embedding` or `Completion` |

```json
{
  "jsonrpc": "2.0",
  "id": 20,
  "method": "chat_endpoint_create",
  "params": {
    "tenantGuid": "00000000-0000-0000-0000-000000000000",
    "endpoint": "{\"Name\":\"Local Ollama\",\"EndpointType\":\"Completion\",\"Provider\":\"Ollama\",\"Endpoint\":\"http://127.0.0.1:11434\",\"Model\":\"gemma3:4b\"}"
  }
}
```

### chat/endpoint/get, chat/endpoint/all, chat/endpoint/delete

Read one endpoint, list endpoints, or delete an endpoint. Listing accepts an optional type filter and returns the `EnumerationResult` envelope of `ChatEndpoint` records.

| Argument | Type | Required | Notes |
|----------|------|----------|-------|
| `tenantGuid` | string (GUID) | yes | Owning tenant |
| `endpointGuid` | string (GUID) | get and delete only | Endpoint to read or delete |
| `endpointType` | string | no (`all` only) | `Embedding` or `Completion`; omit for every type |
| `skip` | integer | no (`all` only) | Records to skip (default 0) |
| `maxResults` | integer | no (`all` only) | Maximum results, 1-1000, default 1000 |
| `continuationToken` | string (GUID) | no (`all` only) | Marker-based continuation from a previous response |

```json
{
  "jsonrpc": "2.0",
  "id": 21,
  "method": "chat_endpoint_all",
  "params": {
    "tenantGuid": "00000000-0000-0000-0000-000000000000",
    "endpointType": "Completion"
  }
}
```

### chat/endpoint/test

Probes the upstream provider from the LiteGraph server and returns a `ChatEndpointTestResult` string: `Reachable`, `Models` (omitted for providers without a model-listing API), `ModelExists`, `Error`, and `RuntimeMs`.

| Argument | Type | Required | Notes |
|----------|------|----------|-------|
| `tenantGuid` | string (GUID) | yes | Owning tenant |
| `endpointGuid` | string (GUID) | yes | Endpoint to test |

```json
{
  "jsonrpc": "2.0",
  "id": 22,
  "method": "chat_endpoint_test",
  "params": {
    "tenantGuid": "00000000-0000-0000-0000-000000000000",
    "endpointGuid": "11111111-1111-1111-1111-111111111111"
  }
}
```

### chat/endpoint/health, chat/endpoint/healthall

Read background health-check status — monitored flag, healthy verdict, consecutive successes and failures, uptime percentage, and the rolling probe history — for one endpoint or for every endpoint in the tenant. `health` returns a single health object; `healthall` returns the `EnumerationResult` envelope of health records.

| Argument | Type | Required | Notes |
|----------|------|----------|-------|
| `tenantGuid` | string (GUID) | yes | Owning tenant |
| `endpointGuid` | string (GUID) | `health` only | Endpoint to inspect |
| `skip` | integer | no (`healthall` only) | Records to skip (default 0) |
| `maxResults` | integer | no (`healthall` only) | Maximum results, 1-1000, default 1000 |
| `continuationToken` | string (GUID) | no (`healthall` only) | Marker-based continuation from a previous response |

```json
{
  "jsonrpc": "2.0",
  "id": 23,
  "method": "chat_endpoint_healthall",
  "params": {
    "tenantGuid": "00000000-0000-0000-0000-000000000000"
  }
}
```

### chat/completions

Executes a chat completion and returns a `ChatCompletionResult` string: the assistant message plus thread and turn GUIDs, provider, model, token counts, timing, tool call counts, and retrieval counts. This tool is non-streaming only; SSE streaming is unavailable over MCP — use the REST `POST /chat/completions` endpoint with `Stream: true` when incremental delivery is needed. Omitting `threadGuid` creates a new thread, optionally bound to `graphGuid`; pass the returned `ThreadGUID` on the next call to continue the conversation. Endpoint GUIDs default to the tenant chat settings.

| Argument | Type | Required | Notes |
|----------|------|----------|-------|
| `tenantGuid` | string (GUID) | yes | Owning tenant |
| `message` | string | yes | User message |
| `threadGuid` | string (GUID) | no | Existing thread to continue; omit to create a new thread |
| `graphGuid` | string (GUID) | no | Graph to bind a newly created thread to |
| `completionEndpointGuid` | string (GUID) | no | Completion endpoint override |
| `embeddingEndpointGuid` | string (GUID) | no | Embedding endpoint override for retrieval |
| `enableTools` | boolean | no | Tool advertisement override; defaults to the tenant chat settings |
| `enableRag` | boolean | no | Retrieval override; defaults to the tenant chat settings |

```json
{
  "jsonrpc": "2.0",
  "id": 24,
  "method": "chat_completions",
  "params": {
    "tenantGuid": "00000000-0000-0000-0000-000000000000",
    "message": "What are the most connected nodes in this graph?",
    "graphGuid": "22222222-2222-2222-2222-222222222222",
    "enableTools": true
  }
}
```

### chat/thread/all, chat/thread/get, chat/thread/delete, chat/thread/turns

Thread management. `chat_thread_all` lists the caller's own threads, or every user's threads when `allUsers` is true (admin only). `chat_thread_turns` returns the thread's turns ascending by sequence as full `ChatTurn` objects, including per-stage metrics, the tool transcript, and telemetry. Both list tools return the `EnumerationResult` envelope. Deleting a thread also deletes its turns and feedback.

| Argument | Type | Required | Notes |
|----------|------|----------|-------|
| `tenantGuid` | string (GUID) | yes | Owning tenant |
| `threadGuid` | string (GUID) | `get`, `delete`, `turns` | Thread to read, delete, or read turns from |
| `allUsers` | boolean | no (`all` only) | `true` lists every user's threads (admin only, default `false`) |
| `skip` | integer | no (`all`, `turns`) | Records to skip (default 0) |
| `maxResults` | integer | no (`all`, `turns`) | Maximum results, 1-1000, default 1000 |
| `continuationToken` | string (GUID) | no (`all`, `turns`) | Marker-based continuation from a previous response |

```json
{
  "jsonrpc": "2.0",
  "id": 25,
  "method": "chat_thread_turns",
  "params": {
    "tenantGuid": "00000000-0000-0000-0000-000000000000",
    "threadGuid": "33333333-3333-3333-3333-333333333333"
  }
}
```

### chat/feedback/create, chat/feedback/all, chat/feedback/delete

Submit a rating on an assistant turn, list all feedback in the tenant (admin only), or delete a feedback record (admin only). `chat_feedback_all` returns the `EnumerationResult` envelope of feedback records.

| Argument | Type | Required | Notes |
|----------|------|----------|-------|
| `tenantGuid` | string (GUID) | yes | Owning tenant |
| `turnGuid` | string (GUID) | `create` only | Turn being rated |
| `rating` | string | `create` only | `ThumbsUp` or `ThumbsDown` |
| `feedbackText` | string | no (`create` only) | Free-text comment |
| `feedbackGuid` | string (GUID) | `delete` only | Feedback record to delete |
| `skip` | integer | no (`all` only) | Records to skip (default 0) |
| `maxResults` | integer | no (`all` only) | Maximum results, 1-1000, default 1000 |
| `continuationToken` | string (GUID) | no (`all` only) | Marker-based continuation from a previous response |

```json
{
  "jsonrpc": "2.0",
  "id": 26,
  "method": "chat_feedback_create",
  "params": {
    "tenantGuid": "00000000-0000-0000-0000-000000000000",
    "turnGuid": "44444444-4444-4444-4444-444444444444",
    "rating": "ThumbsUp",
    "feedbackText": "Accurate and well grounded in the graph."
  }
}
```

### chat/settings/get, chat/settings/update

Read or upsert the tenant's chat settings: default completion and embedding endpoints, system prompt, chat/tool/RAG enablement, tool iteration and retrieval limits, context token budget, and history retention. Reads return defaults when no record exists; updates require an admin principal, and default endpoint GUIDs are validated for existence and type. The `settings` argument is a `ChatSettings` serialized to a JSON string.

| Argument | Type | Required | Notes |
|----------|------|----------|-------|
| `tenantGuid` | string (GUID) | yes | Owning tenant |
| `settings` | string (JSON) | `update` only | Serialized `ChatSettings` |

```json
{
  "jsonrpc": "2.0",
  "id": 27,
  "method": "chat_settings_update",
  "params": {
    "tenantGuid": "00000000-0000-0000-0000-000000000000",
    "settings": "{\"DefaultCompletionEndpointGUID\":\"11111111-1111-1111-1111-111111111111\",\"EnableChat\":true,\"EnableTools\":true,\"EnableRag\":true,\"RagTopK\":8}"
  }
}
```

## Graph Algorithm Tools

LiteGraph v9.0.0 exposes graph algorithm tools over HTTP, TCP, and WebSocket. They proxy the REST endpoints under the caller's RBAC. See [ALGORITHMS.md](ALGORITHMS.md).

- `algorithm_run` — run an algorithm over a graph. Arguments: `tenantGuid`, `graphGuid`, and either a `request` object (`GraphAlgorithmRequest`) or `algorithmType` plus optional `writeBack`/`maxResults`.
- `algorithm_export` — export a graph projection. Arguments: `tenantGuid`, `graphGuid`, `format` (`NodeLinkJson` default, `EdgeList`, `Graphml`), `attributes` (`None`, `Meta` default, `Full`).
- `algorithm_import` — write externally computed per-node values back onto nodes. Arguments: `tenantGuid`, `graphGuid`, `request` (a `GraphAlgorithmImportRequest` with a `Values` map).
