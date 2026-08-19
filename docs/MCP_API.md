# MCP API for LiteGraph

The LiteGraph MCP server exposes the graph database as a set of Model Context Protocol tools so that Claude, Claude Code, Cursor, and other MCP-compatible clients can create, query, and manage graph data through tool calls. It is a thin, stateless layer: each tool validates its arguments and forwards to the LiteGraph REST server over the configured endpoint and bearer token, then returns the REST response verbatim. Nothing is cached in the MCP process, so a tool call sees exactly what the REST API sees.

For the setup walkthrough (build, install, start, and connect Claude), see [Using Claude with LiteGraph](CLAUDE_MCP.md). For the underlying HTTP contract that every tool wraps, see the [REST API](REST_API.md).

## Transports

The server is built on Voltaic and listens on three transports at once. All three expose the same tools under the same names; pick whichever fits the client.

| Transport | Default endpoint | Notes |
|-----------|------------------|-------|
| HTTP | `http://localhost:8702/rpc` | JSON-RPC over HTTP POST; server-sent events at `/events` |
| TCP | `localhost:8703` | Raw JSON-RPC over a socket |
| WebSocket | `ws://localhost:8704/mcp` | JSON-RPC over a WebSocket |

Hostnames and ports are configurable through `litegraph-mcp.json` or the `MCP_HTTP_*`, `MCP_TCP_*`, and `MCP_WS_*` environment variables. The LiteGraph endpoint and API key the server forwards to are set with `LITEGRAPH_ENDPOINT` and `LITEGRAPH_API_KEY`.

## Request And Response Envelope

Every call is a JSON-RPC 2.0 request whose `method` is the tool name and whose `params` is the tool's argument object. The standard MCP discovery methods (`initialize`, `tools/list`) are also available for clients that enumerate tools before calling them.

Request:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "graph/get",
  "params": {
    "tenantGuid": "00000000-0000-0000-0000-000000000000",
    "graphGuid": "00000000-0000-0000-0000-000000000000",
    "includeData": true
  }
}
```

Response:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": "{ ...serialized graph JSON... }"
}
```

Most tools return the REST payload as a JSON string in `result`; a handful return a bare `true`/`false` or an empty string for operations that have no body (deletes, flushes, index rebuilds). When a tool's arguments are invalid or the REST call fails, the server returns a JSON-RPC `error` object with a message describing the failure. Argument names are camelCase. Complex request bodies (search requests, enumeration queries, subgraph extraction, vector index configuration) are passed as a JSON string in a single argument rather than as nested objects, which keeps the tool schemas flat and predictable.

## Tool Catalog

Tools are grouped by resource. The name before the slash is the family; the name after it is the operation. Families follow the same verbs, so once you know `node/get`, `node/create`, `node/search`, and `node/enumerate`, the other families read the same way.

### graph/*

Graph lifecycle, search, statistics, export and import, subgraph extraction, and vector index management.

| Tool | Purpose |
|------|---------|
| `graph/create`, `graph/get`, `graph/update`, `graph/delete` | Graph CRUD |
| `graph/all`, `graph/readallintenant`, `graph/getmany`, `graph/enumerate` | List and page graphs |
| `graph/search`, `graph/readfirst`, `graph/exists`, `graph/statistics` | Search, existence, and statistics |
| `graph/deleteallintenant` | Delete every graph in a tenant |
| `graph/getsubgraph`, `graph/getsubgraphstatistics` | Node-rooted subgraph read and its statistics |
| `graph/exportgexf` | Render a graph as GEXF |
| `graph/exportjsonl`, `graph/exportsubgraphjsonl`, `graph/importjsonl` | Streaming JSONL export and import (see below) |
| `graph/enablevectorindexing`, `graph/rebuildvectorindex`, `graph/deletevectorindex`, `graph/getvectorindexconfig`, `graph/getvectorindexstatistics` | HNSW vector index management |
| `graph/query`, `graph/transaction` | Native graph query and graph-scoped transaction |

### node/*

Node CRUD plus traversal and connectivity helpers.

| Tool | Purpose |
|------|---------|
| `node/create`, `node/createmany`, `node/get`, `node/getmany`, `node/update`, `node/delete` | Node CRUD and batch create |
| `node/all`, `node/readallingraph`, `node/readallintenant`, `node/enumerate` | List and page nodes |
| `node/search`, `node/readfirst`, `node/exists` | Search and existence |
| `node/deleteall`, `node/deleteallintenant`, `node/deletemany` | Bulk delete |
| `node/neighbors`, `node/parents`, `node/children`, `node/traverse` | Traversal |
| `node/readmostconnected`, `node/readleastconnected` | Connectivity ranking |

### edge/*

Edge CRUD plus node-relative edge lookups.

| Tool | Purpose |
|------|---------|
| `edge/create`, `edge/createmany`, `edge/get`, `edge/getmany`, `edge/update`, `edge/delete` | Edge CRUD and batch create |
| `edge/all`, `edge/readallingraph`, `edge/readallintenant`, `edge/enumerate` | List and page edges |
| `edge/search`, `edge/readfirst`, `edge/exists`, `edge/betweennodes` | Search, existence, and edges between two nodes |
| `edge/fromnode`, `edge/tonode`, `edge/nodeedges` | Edges by endpoint |
| `edge/deleteallingraph`, `edge/deleteallintenant`, `edge/deletemany`, `edge/deletenodeedges`, `edge/deletenodeedgesmany` | Bulk and node-scoped delete |

### label/*, tag/*, vector/*

Metadata attached to graphs, nodes, and edges. The three families share a shape: create (single and `createmany`), read (`get`, `getmany`, `all`, `readallingraph`, `readallintenant`, `enumerate`, and per-parent `readmany*` reads), `update`, `exists`, and a set of scoped deletes (`delete`, `deletemany`, `deleteallingraph`, `deleteallintenant`, and per-parent deletes). `vector/*` adds `vector/search` for similarity search, which uses the graph's HNSW index when one is enabled and falls back to a linear scan otherwise.

### tenant/*, user/*, credential/*

Multi-tenant administration and authentication records.

| Tool | Purpose |
|------|---------|
| `tenant/create`, `tenant/get`, `tenant/getmany`, `tenant/all`, `tenant/enumerate`, `tenant/update`, `tenant/delete`, `tenant/exists` | Tenant CRUD and listing |
| `tenant/statistics`, `tenant/statisticsall` | Tenant statistics |
| `user/create`, `user/get`, `user/getmany`, `user/all`, `user/enumerate`, `user/update`, `user/delete`, `user/exists` | User CRUD and listing |
| `credential/create`, `credential/get`, `credential/getmany`, `credential/all`, `credential/enumerate`, `credential/update`, `credential/delete`, `credential/exists` | Credential CRUD and listing |
| `credential/getbybearertoken`, `credential/deletebyuser`, `credential/deleteallintenant` | Credential lookup and scoped delete |

### authorization/*

RBAC roles, user-role assignments, credential scopes, and effective-permission inspection.

| Tool | Purpose |
|------|---------|
| `authorization/role/*` | Role CRUD (`create`, `get`, `all`, `update`, `delete`) |
| `authorization/userrole/*` | User-to-role assignment CRUD |
| `authorization/credentialscope/*` | Credential scope CRUD |
| `authorization/user/permissions`, `authorization/credential/permissions` | Effective permissions for a user or credential |

### admin/*, batch/*, userauthentication/*

Operational and authentication utilities.

| Tool | Purpose |
|------|---------|
| `admin/backup`, `admin/backups`, `admin/backupread`, `admin/backupexists`, `admin/backupdelete` | Binary database backup management |
| `admin/flush` | Flush an in-memory database to disk |
| `batch/existence` | Batch existence check for nodes, edges, and edges-between |
| `userauthentication/generatetoken`, `userauthentication/gettokendetails`, `userauthentication/gettenantsforemail` | Security token issuance and lookup |

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
  "method": "graph/exportjsonl",
  "params": {
    "tenantGuid": "00000000-0000-0000-0000-000000000000",
    "graphGuid": "00000000-0000-0000-0000-000000000000",
    "includeData": true,
    "includeSubordinates": true
  }
}
```

```json
{
  "jsonrpc": "2.0",
  "id": 10,
  "result": "# litegraph-jsonl v1\n# kind: graph-backup\n{\"Type\":\"Graph\",\"Object\":{\"GUID\":\"00000000-0000-0000-0000-000000000000\",\"Name\":\"Default graph\"}}\n{\"Type\":\"Node\",\"Object\":{\"GUID\":\"11111111-1111-1111-1111-111111111111\",\"Name\":\"Ada\"}}\n{\"Type\":\"Edge\",\"Object\":{\"GUID\":\"22222222-2222-2222-2222-222222222222\",\"From\":\"11111111-1111-1111-1111-111111111111\",\"To\":\"33333333-3333-3333-3333-333333333333\"}}"
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
  "method": "graph/exportsubgraphjsonl",
  "params": {
    "tenantGuid": "00000000-0000-0000-0000-000000000000",
    "graphGuid": "00000000-0000-0000-0000-000000000000",
    "request": "{\"StartNodeGUIDs\":[\"11111111-1111-1111-1111-111111111111\"],\"MaxDepth\":2,\"Direction\":\"Both\",\"IncludeData\":true}"
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
  "method": "graph/importjsonl",
  "params": {
    "tenantGuid": "00000000-0000-0000-0000-000000000000",
    "guidStrategy": "regenerate",
    "onError": "abort",
    "batchSize": 1000,
    "jsonl": "# litegraph-jsonl v1\n{\"Type\":\"Graph\",\"Object\":{\"Name\":\"Copy\"}}\n{\"Type\":\"Node\",\"Object\":{\"GUID\":\"11111111-1111-1111-1111-111111111111\",\"Name\":\"Ada\"}}"
  }
}
```

```json
{
  "jsonrpc": "2.0",
  "id": 12,
  "result": "{\"Success\":true,\"TenantGUID\":\"00000000-0000-0000-0000-000000000000\",\"GraphGUID\":\"9de1f1a2-4b8c-4f7a-9a1b-2c3d4e5f6a7b\",\"GraphsCreated\":1,\"NodesCreated\":1,\"EdgesCreated\":0,\"LinesRead\":2,\"LinesIgnored\":1,\"Warnings\":[],\"GuidMap\":{}}"
}
```

Under `regenerate`, the `GuidMap` in the result maps each original GUID to the fresh GUID it received, so a caller can correlate the source records with what landed in the new graph. Under `preserve`, a GUID that already exists in the store fails the import, which is why `preserve` fits a restore into an empty database rather than a merge.
