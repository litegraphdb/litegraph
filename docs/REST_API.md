# REST API for LiteGraph

This document describes the REST API endpoints for LiteGraph Server.

For client SDK libraries that wrap this API, see the [`sdk/`](sdk/) directory:
- [C# SDK](sdk/csharp/) - NuGet package `LiteGraph.Sdk`
- [Python SDK](sdk/python/) - PyPI package `litegraph-sdk`
- [JavaScript SDK](sdk/js/) - npm package `litegraphdb`

## Authentication

### v8.0 Account Model

As of v8.0 there is a single kind of account. Administrators are ordinary users carrying elevated flags, not a separate identity:

- `IsSystemAdmin` — full access to every tenant and every request type. Equivalent to the legacy administrator bearer token, but attached to a real user record.
- `IsTenantAdmin` — full management of the user's own tenant, including its users and credentials, but not tenant creation/deletion or server settings.

A user with neither flag is a regular user, constrained by the roles and credential scopes described in `RBAC.md`. The administrator bearer token defined in `litegraph.json` remains as a break-glass credential and is treated as system-admin. Endpoints that read below say "administrator bearer token authentication" describe the pre-v8 requirement; under v8 the same endpoints are additionally reachable by a user whose flags or granted scopes permit the request — see `RBAC.md` for the full permission matrix.

Users can authenticate API requests in one of three ways.

### Bearer Token

A bearer token can be supplied in the `Authorization` header, i.e. `Authorization: Bearer {token}`.  This bearer token can either be from a `Credential` object mapped to a user by GUID, or, the administrator bearer token defined in `litegraph.json`.  

### Credentials

The user's email, password, and tenant GUID can be passed in as headers using `x-email`, `x-password`, and `x-tenant-guid`.  This method does not work for administrative API calls, as the administrator is only defined by bearer token in `litegraph.json`.

### Security token

Temporal security tokens can be generated for regular users (not for the administrator).  These security tokens expire after 24 hours, and can be used in the `x-token` header as an alternative to using bearer tokens or credentials.

To generate a security token, set the `x-email`, `x-password`, and `x-tenant-guid` headers, and call `GET /v1.0/token`.  The result will look as follows:
```
{
    "TimestampUtc": "2025-01-30T22:54:41.963425Z",
    "ExpirationUtc": "2025-01-31T22:54:41.963426Z",
    "IsExpired": false,
    "TenantGUID": "00000000-0000-0000-0000-000000000000",
    "UserGUID": "00000000-0000-0000-0000-000000000000",
    "Token": "mXCNtMWDsW0/pr+IwRFUje2n5Z9/qDGprgAY26bz4KYoJOUyufkzkzfK+Kiq0iv/PsZkzwewIXsuCMkpqJbsMJFMd94fyt8LLHr4CL0NMn1etyK7AC+uLH/xUqVnP+Jdww8LhEV2ly3gx27h91fiXMT60ScKNM772o3zq1WUkD1yBL1MCcZsUkHXQw3ZiP4EsFoZ6oxqquwN+/cRZROKXAbPWvArwcDNIIz9vnBvcvjDJYVCz/LiPq5BXIHtzSP7QffBqiZtttEaql8LIu17c9ms02N2mB/nyF0FF6U97ay1Vbo0V/0/akiRnieOKGYCOjiJBuU1kZ28uiDj1pENpzS1GUqkt5HqK44Jl4LtIco=",
    "Valid": true
}
```

The value found in `Token` can then be used when making API requests to LiteGraph, by adding the `x-token` header with the value, i.e.
```
GET /v1.0/tenants/00000000-0000-0000-0000-000000000000/graphs
x-token: mXCNtMWDsW0/pr+IwRFUje2...truncated...4Jl4LtIco=
```

To retrieve the details of a token and to verify it has not expired, call `GET /v1.0/token/details` with the `x-token` header set.
```
GET /v1.0/token/details
x-token: mXCNtMWDsW0/pr+IwRFUje2...truncated...4Jl4LtIco=

Response:
{
    "TimestampUtc": "2025-01-30T14:54:41.963425Z",
    "ExpirationUtc": "2025-01-31T14:54:41.963426Z",
    "IsExpired": false,
    "TenantGUID": "00000000-0000-0000-0000-000000000000",
    "UserGUID": "00000000-0000-0000-0000-000000000000",
    "Valid": true
}
```

If you do not know the tenant GUID ahead of time, use the API to retrieve tenants for a given email by calling `GET /v1.0/token/tenants` with the `x-email` header set.  It returns the tenants associated with the supplied email address inside the standard [enumeration envelope](#enumeration-and-pagination).
```
GET /v1.0/token/tenants
x-email: default@user.com

Response:
{
    "Success": true,
    "Timestamp": { "Start": "2026-08-31T00:00:00.000000Z", "End": "2026-08-31T00:00:00.004120Z", "TotalMs": 4.12, "Messages": {} },
    "MaxResults": 1000,
    "ContinuationToken": null,
    "EndOfResults": true,
    "TotalRecords": 1,
    "RecordsRemaining": 0,
    "Objects": [
        {
            "GUID": "00000000-0000-0000-0000-000000000000",
            "Name": "Default tenant",
            "Active": true,
            "CreatedUtc": "2025-02-06T18:22:56.789353Z",
            "LastUpdateUtc": "2025-02-06T18:22:56.788994Z"
        }
    ]
}
```

## Enumeration And Pagination

As of v8.1 there are **zero get-all APIs**. Every list-returning route — every `GET` that reads more than one record and every `POST` that enumerates or searches vectors — responds with the paginated `EnumerationResult` envelope shown under [Enumeration Result](#enumeration-result), never a bare JSON array:

```
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

`Objects` carries the page of records; `TotalRecords` and `RecordsRemaining` describe the full result set; `EndOfResults` is `true` on the final page; `ContinuationToken` is non-null when marker-based continuation is available for the route.

All list-shaped `GET` routes accept the same query parameters:

| Parameter  | Type    | Default            | Meaning |
|------------|---------|--------------------|---------|
| `max-keys` | integer | `1000`             | Maximum records per page; valid range 1-1000. `maxKeys` is accepted as an alternate spelling |
| `skip`     | integer | `0`                | Number of records to skip before the page begins |
| `order`    | string  | `CreatedDescending` | Sort order; an `EnumerationOrderEnum` value such as `CreatedAscending`, `CreatedDescending`, `NameAscending`, `NameDescending`, `GuidAscending`, `GuidDescending`, `CostAscending`, `CostDescending`, `MostConnected`, `LeastConnected` |
| `token`    | GUID    | none               | Continuation token from a previous response, where marker-based continuation is supported |

Use either `skip` (offset paging) or `token` (marker paging, where a previous response returned a `ContinuationToken`); the v2.0 enumeration `POST` routes carry the same controls in the `Enumeration Query` body (`MaxResults`, `ContinuationToken`, `Ordering`). The authorization and request-history list routes additionally accept their legacy `page`/`pageSize` parameters, but `max-keys` and `skip` take precedence when supplied.

A small set of routes intentionally does **not** return the envelope, because they are not record lists: single-object reads, statistics objects (`.../stats`, tenant/graph statistics), server and chat settings, effective-permissions composites, token issuance, export streams (GEXF, JSONL), vector index configuration/statistics, node subgraph reads (a `SearchResult`-shaped composite), the graph/node/edge `search` POST routes (which return `SearchResult`-shaped envelopes, also never bare arrays), and the graph-scoped OpenAI/Ollama-compatible chat routes, which keep their respective wire formats by design.

## Data Structures

### Backup File
```
{
    "Filename": "my-backup.db",
    "Length": 352256,
    "MD5Hash": "EF2A390E654BCFE3052DAF7364037DBE",
    "SHA1Hash": "74625881C00FEF2E654AB9B800A0C8E23CC7CBB0",
    "SHA256Hash": "584F2D85362F7E7B9755DF7A363120E6FF8F93A162E918E7085C795021D14DCF",
    "CreatedUtc": "2025-05-27T03:31:10.904886Z",
    "LastUpdateUtc": "2025-05-27T03:31:10.909897Z",
    "LastAccessUtc": "2025-05-27T03:31:13.634489Z",
    "Data": "... base64 data ..."
}
```

### Subgraph Extraction Request

Body for `POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/export/jsonl`. The traversal starts at one or more nodes and walks edges outward, honoring the depth, count, label, tag, filter, and cost limits below. Start nodes are always included in the output even when they fail the node filters, so a selection never comes back empty because of an over-tight filter on the seeds themselves.

```
{
    "TenantGUID": "00000000-0000-0000-0000-000000000000",
    "GraphGUID": "00000000-0000-0000-0000-000000000000",
    "StartNodeGUIDs": [ "11111111-1111-1111-1111-111111111111" ],
    "MaxDepth": 2,
    "Direction": "Both",
    "MaxNodes": 0,
    "MaxEdges": 0,
    "EdgeLabels": [ ],
    "EdgeTags": { },
    "EdgeFilter": { },
    "MaxEdgeCost": null,
    "NodeLabels": [ ],
    "NodeTags": { },
    "NodeFilter": { },
    "IncludeData": false,
    "IncludeSubordinates": false
}
```

| Field | Type | Default | Notes |
|-------|------|---------|-------|
| `StartNodeGUIDs` | array of GUID | required | At least one seed node; the traversal fails with `400` if empty or a GUID is not in the graph |
| `MaxDepth` | int | `2` | Edge hops from the seeds; `0` returns the start nodes plus edges among them |
| `Direction` | string | `Both` | `Outbound`, `Inbound`, or `Both` |
| `MaxNodes` | int | `0` | Node cap; `0` is unlimited |
| `MaxEdges` | int | `0` | Edge cap; `0` is unlimited |
| `EdgeLabels` | array of string | `[]` | Edges must carry one of these labels to be traversed |
| `EdgeTags` | object | `{}` | Edge tag key/value constraints |
| `EdgeFilter` | expression | `{}` | Expression filter over edge `Data` |
| `MaxEdgeCost` | int or null | `null` | Edges above this cost are not traversed |
| `NodeLabels` | array of string | `[]` | Non-seed nodes must carry one of these labels |
| `NodeTags` | object | `{}` | Node tag key/value constraints |
| `NodeFilter` | expression | `{}` | Expression filter over node `Data` |
| `IncludeData` | bool | `false` | Emit the `Data` object on each record |
| `IncludeSubordinates` | bool | `false` | Emit labels, tags, and vectors on each record |

### Graph Import Result

Returned by both `import/jsonl` endpoints. Counters report exactly what happened per record type, `Warnings` collects non-fatal problems such as dropped dangling edges and skipped malformed lines, and `GuidMap` maps each original GUID to its replacement (populated only under the `regenerate` strategy).

```
{
    "Success": true,
    "TenantGUID": "00000000-0000-0000-0000-000000000000",
    "GraphGUID": "9de1f1a2-4b8c-4f7a-9a1b-2c3d4e5f6a7b",
    "GraphsCreated": 1,
    "NodesCreated": 128,
    "NodesUpdated": 0,
    "NodesSkipped": 0,
    "EdgesCreated": 205,
    "EdgesUpdated": 0,
    "EdgesSkipped": 0,
    "LinesRead": 334,
    "LinesIgnored": 6,
    "Warnings": [ ],
    "GuidMap": { }
}
```

For a merge into an existing graph, `GraphGUID` is the target graph; for an import that creates a new graph, it is the GUID of the graph that was created.

### JSONL Format

The interchange format is UTF-8 text with one JSON value per line, terminated by `\n`. A line beginning with `#` is a comment and is ignored on import, so exporters can prepend a metadata header without breaking round-trips. Every non-comment line is a typed envelope: `{"Type":"Node"|"Edge"|"Graph","Object":{...}}`, where `Object` is the same JSON shape the REST API returns for that entity. The writer emits the graph record first, then node records, then edge records, but the importer does not depend on that order.

A whole-graph export carries the `graph-backup` kind; a subgraph export carries the `subgraph` kind. A typical header looks like this:

```
# litegraph-jsonl v1
# kind: graph-backup
# exported-utc: 2026-08-19T17:22:34.986575Z
# source-tenant: 00000000-0000-0000-0000-000000000000
# source-graph: 00000000-0000-0000-0000-000000000000 (name: "Default graph")
# generator: LiteGraph
{"Type":"Graph","Object":{"GUID":"00000000-0000-0000-0000-000000000000","Name":"Default graph"}}
{"Type":"Node","Object":{"GUID":"11111111-1111-1111-1111-111111111111","Name":"Ada"}}
{"Type":"Edge","Object":{"GUID":"22222222-2222-2222-2222-222222222222","From":"11111111-1111-1111-1111-111111111111","To":"33333333-3333-3333-3333-333333333333"}}
```

Import reconciles incoming GUIDs according to `guidstrategy`. `preserve` keeps original GUIDs and errors on any collision, which suits a restore into a fresh database because node and edge GUIDs are globally unique in the store. `regenerate` assigns fresh GUIDs everywhere, remaps every reference through the `GuidMap`, and cannot collide, so it is the default for merging one graph into another. `skip` leaves existing records untouched and imports only what is new. `overwrite` updates existing records in place and creates the rest. When a file references a node that already exists in the target graph but is not present in the file, the bridging edge still imports; an edge whose endpoint resolves to neither the file nor the store is dropped with a warning. Imports stream through node batches and a buffered edge pass, and a failure triggers compensating rollback of the records written so far.

### Enumeration Query

The body accepted by the v2.0 enumeration `POST` routes. `MaxResults` and `ContinuationToken` play the same roles as the `max-keys` and `token` query parameters described under [Enumeration And Pagination](#enumeration-and-pagination).
```
{
    "Ordering": "CreatedDescending",
    "IncludeData": true,
    "IncludeSubordinates": true,
    "MaxResults": 5,
    "ContinuationToken": null,
    "Labels": [ ],
    "Tags": { },
    "Expr": { }
}
```

### Enumeration Result
```
{
    "Success": true,
    "Timestamp": {
        "Start": "2025-06-22T01:17:42.984885Z",
        "End": "2025-06-22T01:17:43.066948Z",
        "TotalMs": 82.06,
        "Messages": {}
    },
    "MaxResults": 5,
    "ContinuationToken": "ca10f6ca-f4c2-4040-adfe-9de3a81b9f55",
    "EndOfResults": false,
    "TotalRecords": 17,
    "RecordsRemaining": 12,
    "Objects": [
        {
            "TenantGUID": "00000000-0000-0000-0000-000000000000",
            "GUID": "ebefc55b-6f74-4997-8c87-e95e40cb83d3",
            "GraphGUID": "00000000-0000-0000-0000-000000000000",
            "Name": "Active Directory",
            "CreatedUtc": "2025-06-21T05:23:14.100128Z",
            "LastUpdateUtc": "2025-06-21T05:23:14.100128Z",
            "Labels": [],
            "Tags": {},
            "Data": {
                "Name": "Active Directory"
            },
            "Vectors": []
        }, ...
    ]
}
```

### Tenant Statistics (All)
```
{
    "00000000-0000-0000-0000-000000000000": {
        "Graphs": 1,
        "Nodes": 17,
        "Edges": 22,
        "Labels": 0,
        "Tags": 0,
        "Vectors": 0
    }, ...
}
```

### Tenant Statistics (Individual)
```
{
    "Graphs": 1,
    "Nodes": 17,
    "Edges": 22,
    "Labels": 0,
    "Tags": 0,
    "Vectors": 0
}
```

### Graph Statistics (All)
```
{
    "00000000-0000-0000-0000-000000000000": {
        "Nodes": 17,
        "Edges": 22,
        "Labels": 0,
        "Tags": 0,
        "Vectors": 0
    }
}
```

### Graph Statistics (Individual)
```
{
    "Nodes": 17,
    "Edges": 22,
    "Labels": 0,
    "Tags": 0,
    "Vectors": 0
}
```

### Tenant
```
{
    "GUID": "00000000-0000-0000-0000-000000000000",
    "Name": "Default tenant",
    "Active": true,
    "CreatedUtc": "2024-12-27T22:09:09.410802Z",
    "LastUpdateUtc": "2024-12-27T22:09:09.410168Z"
}
```

### User
```
{
    "GUID": "00000000-0000-0000-0000-000000000000",
    "TenantGUID": "00000000-0000-0000-0000-000000000000",
    "FirstName": "Default",
    "LastName": "User",
    "Email": "default@user.com",
    "Password": "password",
    "Active": true,
    "IsSystemAdmin": true,
    "IsTenantAdmin": true,
    "CreatedUtc": "2024-12-27T22:09:09.446911Z",
    "LastUpdateUtc": "2024-12-27T22:09:09.446777Z"
}
```

`IsSystemAdmin` and `IsTenantAdmin` were added in v8.0 and default to `false`. The seeded default user shown above is a system administrator.

### Credential
```
{
    "GUID": "00000000-0000-0000-0000-000000000000",
    "TenantGUID": "00000000-0000-0000-0000-000000000000",
    "UserGUID": "00000000-0000-0000-0000-000000000000",
    "Name": "Default credential",
    "BearerToken": "default",
    "Active": true,
    "CreatedUtc": "2024-12-27T22:09:09.468134Z",
    "LastUpdateUtc": "2024-12-27T22:09:09.467977Z"
}
```

### Label
```
{
    "GUID": "738d4956-a833-429a-9531-c99336638617",
    "TenantGUID": "ba1dc0a6-372d-47ee-aea5-75e7dbbbd175",
    "GraphGUID": "97826e1a-d0c1-4884-820a-bfda74b3be33",
    "EdgeGUID": "971da046-8234-4627-8ae8-e062311874c8",
    "Label": "edge",
    "CreatedUtc": "2025-01-08T23:28:05.312128Z",
    "LastUpdateUtc": "2025-01-08T23:28:05.312128Z"
}
```

### Tag
```
{
    "GUID": "00000000-0000-0000-0000-000000000000",
    "TenantGUID": "00000000-0000-0000-0000-000000000000",
    "GraphGUID": "00000000-0000-0000-0000-000000000000",
    "NodeGUID": "00000000-0000-0000-0000-000000000000",
    "EdgeGUID": "00000000-0000-0000-0000-000000000000",
    "Key": "mykey",
    "Value": "myvalue",
    "CreatedUtc": "2024-12-27T22:14:36.459901Z",
    "LastUpdateUtc": "2024-12-27T22:14:36.459902Z"
}
```

### Vector
```
{
    "GUID": "00000000-0000-0000-0000-000000000000",
    "TenantGUID": "00000000-0000-0000-0000-000000000000",
    "GraphGUID": "00000000-0000-0000-0000-000000000000",
    "NodeGUID": "00000000-0000-0000-0000-000000000000",
    "EdgeGUID": "00000000-0000-0000-0000-000000000000",
    "Model": "testmodel",
    "Dimensionality": 3,
    "Content": "test content",
    "Vectors": [ 0.05, -0.25, 0.45 ],
    "CreatedUtc": "2025-01-15T10:41:13.243174Z",
    "LastUpdateUtc": "2025-01-15T10:41:13.243188Z"
}
```

### Graph
```
{
    "TenantGUID": "00000000-0000-0000-0000-000000000000",
    "GUID": "00000000-0000-0000-0000-000000000000",
    "Name": "My test graph",
    "Labels": [ "test" ],
    "Tags": {
        "Key": "Value"
    },
    "Data": {
        "Hello": "World"
    },
    "Vectors": [
        {
            "GUID": "00000000-0000-0000-0000-000000000000",
            "TenantGUID": "00000000-0000-0000-0000-000000000000",
            "GraphGUID": "00000000-0000-0000-0000-000000000000",
            "NodeGUID": "00000000-0000-0000-0000-000000000000",
            "EdgeGUID": "00000000-0000-0000-0000-000000000000",
            "Model": "testmodel",
            "Dimensionality": 3,
            "Content": "test content",
            "Vectors": [ 0.05, -0.25, 0.45 ],
            "CreatedUtc": "2025-01-15T10:41:13.243174Z",
            "LastUpdateUtc": "2025-01-15T10:41:13.243188Z"
        }
    ],
    "CreatedUtc": "2024-07-01 15:43:06.991834"
}
```

### Graph Vector Index
```
{
    "VectorIndexType": "HnswSqlite",
    "VectorIndexFile": "graph-00000000-0000-0000-0000-000000000000-hnsw.db",
    "VectorIndexThreshold": null,
    "VectorDimensionality": 384,
    "VectorIndexM": 16,
    "VectorIndexEf": 50,
    "VectorIndexEfConstruction": 200
}
```

### Node
```
{
    "TenantGUID": "00000000-0000-0000-0000-000000000000",
    "GUID": "11111111-1111-1111-1111-111111111111",
    "GraphGUID": "00000000-0000-0000-0000-000000000000",
    "Name": "My test node",
    "Labels": [ "test" ],
    "Tags": {
        "Key": "Value"
    },
    "Data": {
        "Hello": "World"
    },
    "Vectors": [
        {
            "GUID": "00000000-0000-0000-0000-000000000000",
            "TenantGUID": "00000000-0000-0000-0000-000000000000",
            "GraphGUID": "00000000-0000-0000-0000-000000000000",
            "NodeGUID": "00000000-0000-0000-0000-000000000000",
            "EdgeGUID": "00000000-0000-0000-0000-000000000000",
            "Model": "testmodel",
            "Dimensionality": 3,
            "Content": "test content",
            "Vectors": [ 0.05, -0.25, 0.45 ],
            "CreatedUtc": "2025-01-15T10:41:13.243174Z",
            "LastUpdateUtc": "2025-01-15T10:41:13.243188Z"
        }
    ],
    "CreatedUtc": "2024-07-01 15:43:06.991834"
}
```

### Edge
```
{
    "TenantGUID": "00000000-0000-0000-0000-000000000000",
    "GUID": "FFFFFFFF-FFFF-FFFF-FFFF-FFFFFFFFFFFF",
    "GraphGUID": "00000000-0000-0000-0000-000000000000",
    "Name": "My test edge",
    "From": "11111111-1111-1111-1111-111111111111",
    "To": "22222222-2222-2222-2222-222222222222",
    "Cost": 10,
    "Labels": [ "test" ],
    "Tags": {
        "Key": "Value"
    },
    "Data": {
        "Hello": "World"
    },
    "Vectors": [
        {
            "GUID": "00000000-0000-0000-0000-000000000000",
            "TenantGUID": "00000000-0000-0000-0000-000000000000",
            "GraphGUID": "00000000-0000-0000-0000-000000000000",
            "NodeGUID": "00000000-0000-0000-0000-000000000000",
            "EdgeGUID": "00000000-0000-0000-0000-000000000000",
            "Model": "testmodel",
            "Dimensionality": 3,
            "Content": "test content",
            "Vectors": [ 0.05, -0.25, 0.45 ],
            "CreatedUtc": "2025-01-15T10:41:13.243174Z",
            "LastUpdateUtc": "2025-01-15T10:41:13.243188Z"
        }
    ],
    "CreatedUtc": "2024-07-01 15:43:06.991834"
}
```

### Route Request
```
{
    "TenantGUID": "00000000-0000-0000-0000-000000000000",
    "Graph": "00000000-0000-0000-0000-000000000000",
    "From": "11111111-1111-1111-1111-111111111111",
    "To": "22222222-2222-2222-2222-222222222222",
    "NodeFilter": null,
    "EdgeFilter": null,
}
```

### Existence Request
```
{
    "Nodes": [
        "[guid1]",
        "[guid2]",
        ...
    ],
    "Edges": [
        "[guid1]",
        "[guid2]",
        ...
    ],
    "EdgesBetween": [
        {
            "From": "[fromguid]",
            "To": "[toguid]"
        },
        ...
    ]
}
```

### Existence Result
```
{
    "ExistingNodes": [
        "[guid1]",
        "[guid2]",
        ...
    ],
    "MissingNodes": [
        "[guid1]",
        "[guid2]",
        ...
    ],
    "ExistingEdges": [
        "[guid1]",
        "[guid2]",
        ...
    ],
    "MissingEdges": [
        "[guid1]",
        "[guid2]",
        ...
    ],
    "ExistingEdgesBetween": [
        {
            "From": "[fromguid]",
            "To": "[toguid]"
        },
        ...
    ],
    "MissingEdgesBetween": [
        {
            "From": "[fromguid]",
            "To": "[toguid]"
        },
        ...
    ]
}
```

### Vector Search Request

```
{
    "GraphGUID": "00000000-0000-0000-0000-000000000000",
    "Domain": "Node",
    "SearchType": "CosineSimilarity",
    "Labels": [],
    "Tags": {},
    "Expr": null,
    "TopK": 10,
    "MinimumScore": 0.1,
    "MaximumDistance": 100,
    "MinimumInnerProduct": 0.1,
    "Embeddings": [ 0.1, 0.2, 0.3 ]
}
```

Valid domains are `Graph` `Node` `Edge`
Valid search types are `CosineSimilarity` `CosineDistance` `EuclidianSimilarity` `EuclidianDistance` `DotProduct`

### Vector Search Result

Both vector search `POST` routes return the standard [enumeration envelope](#enumeration-and-pagination); each entry in `Objects` is a scored match. The number of entries is bounded by the request's `TopK`.

```
{
    "Success": true,
    "Timestamp": { ... },
    "MaxResults": 1000,
    "ContinuationToken": null,
    "EndOfResults": true,
    "TotalRecords": 2,
    "RecordsRemaining": 0,
    "Objects": [
        {
            "Score": 0.874456,
            "Distance": null,
            "InnerProduct": null,
            "Graph": { ... },
            "Node": { ... },
            "Edge": { ... }
        },
        ...
    ]
}
```

### Graph Query Request

Native graph queries execute within one tenant and one graph. See [DSL.md](DSL.md) for the supported syntax, parameter rules, result metadata, mutation behavior, and examples.

```
{
    "Query": "MATCH (n:Person) WHERE n.data.role = $role RETURN n LIMIT 10",
    "Parameters": {
        "role": "engineer"
    },
    "MaxResults": 100,
    "TimeoutSeconds": 30,
    "IncludeProfile": false
}
```

Read-only queries require read permission. Mutation queries require write permission.

### Graph Query Response

```
{
    "Success": true,
    "Mutated": false,
    "RowCount": 1,
    "Rows": [
        {
            "n": {
                "GUID": "00000000-0000-0000-0000-000000000000",
                "Name": "Ada"
            }
        }
    ],
    "Objects": [],
    "Plan": {
        "Kind": "Read",
        "UsesVectorSearch": false,
        "Mutates": false
    },
    "ExecutionProfile": null
}
```

### Graph Transaction Request

Graph transactions execute atomically inside one tenant and one graph. See [TRANSACTIONS.md](TRANSACTIONS.md) for the complete operation model.

```
{
    "Operations": [
        {
            "OperationType": "Create",
            "ObjectType": "Node",
            "Payload": {
                "Name": "Ada",
                "Data": {
                    "role": "mathematician"
                }
            }
        }
    ],
    "MaxOperations": 100,
    "TimeoutSeconds": 30,
    "IsolationLevel": "Default"
}
```

Server-side REST settings can cap transaction size and timeout through `LiteGraph.Transactions.MaxOperations`, `LiteGraph.Transactions.MaxTimeoutSeconds`, `LITEGRAPH_TRANSACTION_MAX_OPERATIONS`, and `LITEGRAPH_TRANSACTION_MAX_TIMEOUT_SECONDS`.

### Graph Transaction Response

Committed transactions return HTTP `200`. Request-shape validation failures return HTTP `400` with a transaction result body and `ValidationFailure: true` when LiteGraph can identify the failed operation. Failed transaction execution after provider work begins returns HTTP `409` with the same transaction result shape so callers can inspect rollback diagnostics.

```
{
    "Success": true,
    "TransactionId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "State": "Committed",
    "RolledBack": false,
    "ValidationFailure": false,
    "FailedOperationIndex": null,
    "Error": null,
    "Operations": [
        {
            "Index": 0,
            "OperationType": "Create",
            "ObjectType": "Node",
            "GUID": "00000000-0000-0000-0000-000000000000",
            "Success": true,
            "Result": { },
            "Error": null
        }
    ],
    "OperationCount": 1,
    "StartedUtc": "2026-06-17T19:00:00.000000Z",
    "CompletedUtc": "2026-06-17T19:00:00.012500Z",
    "DurationMs": 12.5,
    "CommitDurationMs": 1.25,
    "RollbackDurationMs": 0,
    "Provider": "Postgresql",
    "IsolationLevel": "Default",
    "IsolatedRepository": true,
    "SerializedByGate": false,
    "RetryCount": 0,
    "Retryable": false,
    "ConcurrencyConflict": false,
    "ProviderErrorCode": null
}
```

### Authorization Role

```
{
    "GUID": "00000000-0000-0000-0000-000000000000",
    "TenantGUID": "00000000-0000-0000-0000-000000000000",
    "Name": "GraphReader",
    "Description": "Read-only graph access",
    "BuiltIn": false,
    "Immutable": false,
    "Permissions": [ "Read" ],
    "ResourceTypes": [ "Graph", "Node", "Edge", "Label", "Tag", "Vector", "Query" ],
    "ResourceScope": "Graph",
    "InheritToGraphs": false
}
```

### User Role Assignment

```
{
    "GUID": "00000000-0000-0000-0000-000000000000",
    "TenantGUID": "00000000-0000-0000-0000-000000000000",
    "UserGUID": "00000000-0000-0000-0000-000000000000",
    "RoleGUID": "00000000-0000-0000-0000-000000000000",
    "GraphGUID": "00000000-0000-0000-0000-000000000000",
    "Active": true
}
```

### Credential Scope Assignment

```
{
    "GUID": "00000000-0000-0000-0000-000000000000",
    "TenantGUID": "00000000-0000-0000-0000-000000000000",
    "CredentialGUID": "00000000-0000-0000-0000-000000000000",
    "RoleGUID": "00000000-0000-0000-0000-000000000000",
    "GraphGUID": "00000000-0000-0000-0000-000000000000",
    "Permissions": [ "Read" ],
    "ResourceTypes": [ "Graph", "Node", "Edge", "Query" ],
    "Active": true
}
```

## General APIs

| API                   | Method | URL |
|-----------------------|--------|-----|
| Validate connectivity | HEAD   | /   |
| Server information    | GET    | /   |
| Prometheus metrics    | GET    | /metrics |

The metrics route is registered only when observability and Prometheus are enabled. It is intentionally unauthenticated in v6.0.0 and should be protected by network policy or a reverse proxy when exposed outside trusted networks.

## Admin APIs

Admin APIs require system-administrator authentication (the administrator bearer token, or a user with `IsSystemAdmin`).

| API                              | Method | URL         |
|----------------------------------|--------|-------------|
| Flush in-memory database to disk | POST   | /v1.0/flush |

## Settings APIs

Introduced in v8.0. Settings APIs require system-administrator authentication. See `SETTINGS.md` for the field-by-field reference and which fields apply live versus require a restart.

| API                | Method | URL                       |
|--------------------|--------|---------------------------|
| Read settings      | GET    | /v1.0/settings            |
| Update settings    | PUT    | /v1.0/settings            |
| Restart server     | POST   | /v1.0/settings/restart    |

`GET /v1.0/settings` returns the effective server settings (secrets redacted). `PUT /v1.0/settings` persists the supplied settings to `litegraph.json`, hot-reloads fields that can apply live, and returns a `SettingsUpdateResult`:

```
{
    "Success": true,
    "AppliedLive": [ "RequestTimeoutSeconds" ],
    "RestartRequired": [ "Rest.Port" ],
    "Message": "Settings saved. 1 field applied live; 1 field requires a restart."
}
```

`POST /v1.0/settings/restart` flushes pending state and exits the process so the container/orchestrator restarts it with the new configuration. In the checked-in Docker deployment the LiteGraph services run with `restart: unless-stopped`, so this brings the server back automatically.

## Backup APIs

Backup APIs require administrator bearer token authentication. `GET /v1.0/backups` returns the [enumeration envelope](#enumeration-and-pagination) of backup files and accepts the shared pagination query parameters.

| API                | Method | URL                        |
|--------------------|--------|----------------------------|
| Create             | POST   | /v1.0/backups              |
| Read many          | GET    | /v1.0/backups              |
| Read               | GET    | /v1.0/backups/[guid]       |
| Delete             | DELETE | /v1.0/backups/[guid]       |
| Exists             | HEAD   | /v1.0/backups/[guid]       |

## Tenant APIs

Tenant create and delete require system-administrator authentication. Tenant read/exists and update are additionally available to a tenant-administrator of that tenant (`IsTenantAdmin`), and any authenticated user may read the tenants associated with their email via `GET /v1.0/token/tenants`.

When specifying multiple GUIDs to retrieve, i.e. `?guids=...`, use a comma-separated list of values, i.e. `?guids=00000000-0000-0000-0000-000000000000,11111111-1111-1111-1111-111111111111`.

Throughout this document, every `Read many`, `Read all in ...`, `Read ... [labels|tags|vectors]`, and `?guids=` filtered read returns the [enumeration envelope](#enumeration-and-pagination) and accepts the shared `max-keys`, `skip`, `order`, and (where supported) `token` query parameters. This applies to the backup, tenant, user, credential, role/assignment/scope, label, tag, vector, graph, node, edge, traversal, request history, and chat list routes alike.

| API                | Method | URL                        |
|--------------------|--------|----------------------------|
| Create             | PUT    | /v1.0/tenants              |
| Update             | PUT    | /v1.0/tenants/[guid]       |
| Read many          | GET    | /v1.0/tenants              |
| Read many          | GET    | /v1.0/tenants?guids=...    |
| Read               | GET    | /v1.0/tenants/[guid]       |
| Delete             | DELETE | /v1.0/tenants/[guid]       |
| Delete w/ cascade  | DELETE | /v1.0/tenants/[guid]?force |
| Exists             | HEAD   | /v1.0/tenants/[guid]       |

## User APIs

User management (create, delete, list, cross-user read/update) requires system-administrator authentication or a tenant-administrator of the target tenant. A regular user may additionally read, check existence of, and update their own user record (self-service); they cannot list users or act on other users.

The `UserMaster` object carries two v8.0 flags: `IsSystemAdmin` and `IsTenantAdmin` (both default `false`). Only a system-administrator may set `IsSystemAdmin`; a tenant-administrator may set `IsTenantAdmin` on users within their own tenant. Both flags are returned on user reads and are redacted-safe (they are not secrets).

| API                | Method | URL                                  |
|--------------------|--------|--------------------------------------|
| Create             | PUT    | /v1.0/tenants/[guid]/users           |
| Update             | PUT    | /v1.0/tenants/[guid]/users/[guid]    |
| Read many          | GET    | /v1.0/tenants/[guid]/users           |
| Read many          | GET    | /v1.0/tenants/[guid]/users?guids=... |
| Read               | GET    | /v1.0/tenants/[guid]/users/[guid]    |
| Delete             | DELETE | /v1.0/tenants/[guid]/users/[guid]    |
| Exists             | HEAD   | /v1.0/tenants/[guid]/users/[guid]    |

## Credential APIs

Credential APIs require system-administrator authentication or a tenant-administrator of the target tenant.

| API                  | Method | URL                                           |
|----------------------|--------|-----------------------------------------------|
| Create               | PUT    | /v1.0/tenants/[guid]/credentials              |
| Update               | PUT    | /v1.0/tenants/[guid]/credentials/[guid]       |
| Read many            | GET    | /v1.0/tenants/[guid]/credentials              |
| Read many            | GET    | /v1.0/tenants/[guid]/credentials?guids=...    |
| Read                 | GET    | /v1.0/tenants/[guid]/credentials/[guid]       |
| Read by bearer token | GET    | /v1.0/credentials/bearer/[bearerToken]        |
| Delete               | DELETE | /v1.0/tenants/[guid]/credentials/[guid]       |
| Delete all in tenant | DELETE | /v1.0/tenants/[guid]/credentials              |
| Delete by user       | DELETE | /v1.0/tenants/[guid]/users/[guid]/credentials |
| Exists               | HEAD   | /v1.0/tenants/[guid]/credentials/[guid]       |

## Authorization APIs

Authorization APIs require an administrator bearer token or an authenticated user/credential with an effective admin grant for the requested scope. Built-in roles are readable but immutable.

The role, user-role-assignment, and credential-scope list routes return the [enumeration envelope](#enumeration-and-pagination). They continue to accept their legacy `page`/`pageSize` parameters, but `max-keys` and `skip` take precedence when supplied. The two effective-permissions routes return a composite object (assignments, roles, and grants), not a record list, and are intentionally not enveloped.

| API                                   | Method | URL                                                                                 |
|---------------------------------------|--------|-------------------------------------------------------------------------------------|
| Create role                           | PUT    | /v1.0/tenants/[tenantGuid]/roles                                                    |
| Read roles                            | GET    | /v1.0/tenants/[tenantGuid]/roles                                                    |
| Read role                             | GET    | /v1.0/tenants/[tenantGuid]/roles/[roleGuid]                                         |
| Update role                           | PUT    | /v1.0/tenants/[tenantGuid]/roles/[roleGuid]                                         |
| Delete role                           | DELETE | /v1.0/tenants/[tenantGuid]/roles/[roleGuid]                                         |
| Assign user role                      | PUT    | /v1.0/tenants/[tenantGuid]/users/[userGuid]/roles                                   |
| Read user role assignments            | GET    | /v1.0/tenants/[tenantGuid]/users/[userGuid]/roles                                   |
| Read user role assignment             | GET    | /v1.0/tenants/[tenantGuid]/users/[userGuid]/roles/[assignmentGuid]                  |
| Update user role assignment           | PUT    | /v1.0/tenants/[tenantGuid]/users/[userGuid]/roles/[assignmentGuid]                  |
| Delete user role assignment           | DELETE | /v1.0/tenants/[tenantGuid]/users/[userGuid]/roles/[assignmentGuid]                  |
| Read user effective permissions       | GET    | /v1.0/tenants/[tenantGuid]/users/[userGuid]/permissions                             |
| Assign credential scope               | PUT    | /v1.0/tenants/[tenantGuid]/credentials/[credentialGuid]/scopes                      |
| Read credential scope assignments     | GET    | /v1.0/tenants/[tenantGuid]/credentials/[credentialGuid]/scopes                      |
| Read credential scope assignment      | GET    | /v1.0/tenants/[tenantGuid]/credentials/[credentialGuid]/scopes/[assignmentGuid]     |
| Update credential scope assignment    | PUT    | /v1.0/tenants/[tenantGuid]/credentials/[credentialGuid]/scopes/[assignmentGuid]     |
| Delete credential scope assignment    | DELETE | /v1.0/tenants/[tenantGuid]/credentials/[credentialGuid]/scopes/[assignmentGuid]     |
| Read credential effective permissions | GET    | /v1.0/tenants/[tenantGuid]/credentials/[credentialGuid]/permissions                 |

See [RBAC.md](RBAC.md) for role definitions, permission/resource mappings, compatibility behavior for existing users, and credential-scope examples.

## Label APIs

Label APIs require administrator bearer token authentication.

Bulk create endpoints accept an optional `return` query parameter. Omit it or use `return=full` to keep the existing response shape. Use `return=minimal` to return only top-level created objects and skip optional node/edge subordinate hydration. Invalid values return `400 BadRequest`.

| API                      | Method | URL                                                       |
|--------------------------|--------|-----------------------------------------------------------|
| Create                   | PUT    | /v1.0/tenants/[guid]/labels                               |
| Create many              | PUT    | /v1.0/tenants/[guid]/labels/bulk?return=minimal           |
| Update                   | PUT    | /v1.0/tenants/[guid]/labels/[guid]                        |
| Read many                | GET    | /v1.0/tenants/[guid]/labels                               |
| Read many                | GET    | /v1.0/tenants/[guid]/labels?guids=...                     |
| Read                     | GET    | /v1.0/tenants/[guid]/labels/[guid]                        |
| Read all in tenant       | GET    | /v1.0/tenants/[guid]/labels/all                           |
| Read all in graph        | GET    | /v1.0/tenants/[guid]/graphs/[guid]/labels/all             |
| Read graph labels        | GET    | /v1.0/tenants/[guid]/graphs/[guid]/labels                 |
| Read node labels         | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]/labels    |
| Read edge labels         | GET    | /v1.0/tenants/[guid]/graphs/[guid]/edges/[guid]/labels    |
| Delete                   | DELETE | /v1.0/tenants/[guid]/labels/[guid]                        |
| Delete multiple          | DELETE | /v1.0/tenants/[guid]/labels/bulk                          |
| Delete all in tenant     | DELETE | /v1.0/tenants/[guid]/labels/all                           |
| Delete all in graph      | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/labels/all             |
| Delete graph labels      | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/labels                 |
| Delete node labels       | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]/labels    |
| Delete edge labels       | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/edges/[guid]/labels    |
| Exists                   | HEAD   | /v1.0/tenants/[guid]/labels/[guid]                        |

## Tag APIs

Tag APIs require administrator bearer token authentication.

| API                      | Method | URL                                                      |
|--------------------------|--------|----------------------------------------------------------|
| Create                   | PUT    | /v1.0/tenants/[guid]/tags                                |
| Create many              | PUT    | /v1.0/tenants/[guid]/tags/bulk?return=minimal            |
| Update                   | PUT    | /v1.0/tenants/[guid]/tags/[guid]                         |
| Read many                | GET    | /v1.0/tenants/[guid]/tags                                |
| Read many                | GET    | /v1.0/tenants/[guid]/tags?guids=...                      |
| Read                     | GET    | /v1.0/tenants/[guid]/tags/[guid]                         |
| Read all in tenant       | GET    | /v1.0/tenants/[guid]/tags/all                            |
| Read all in graph        | GET    | /v1.0/tenants/[guid]/graphs/[guid]/tags/all              |
| Read graph tags          | GET    | /v1.0/tenants/[guid]/graphs/[guid]/tags                  |
| Read node tags           | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]/tags     |
| Read edge tags           | GET    | /v1.0/tenants/[guid]/graphs/[guid]/edges/[guid]/tags     |
| Delete                   | DELETE | /v1.0/tenants/[guid]/tags/[guid]                         |
| Delete all in tenant     | DELETE | /v1.0/tenants/[guid]/tags/all                            |
| Delete all in graph      | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/tags/all              |
| Delete graph tags        | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/tags                  |
| Delete node tags         | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]/tags     |
| Delete edge tags         | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/edges/[guid]/tags     |
| Exists                   | HEAD   | /v1.0/tenants/[guid]/tags/[guid]                         |

## Vector APIs

Vector APIs require administrator bearer token authentication, aside from the vector search API.

| API                      | Method | URL                                                      |
|--------------------------|--------|----------------------------------------------------------|
| Create                   | PUT    | /v1.0/tenants/[guid]/vectors                             |
| Create many              | PUT    | /v1.0/tenants/[guid]/vectors/bulk?return=minimal         |
| Update                   | PUT    | /v1.0/tenants/[guid]/vectors/[guid]                      |
| Read many                | GET    | /v1.0/tenants/[guid]/vectors                             |
| Read many                | GET    | /v1.0/tenants/[guid]/vectors?guids=...                   |
| Read                     | GET    | /v1.0/tenants/[guid]/vectors/[guid]                      |
| Read all in tenant       | GET    | /v1.0/tenants/[guid]/vectors/all                         |
| Read all in graph        | GET    | /v1.0/tenants/[guid]/graphs/[guid]/vectors/all           |
| Read graph vectors       | GET    | /v1.0/tenants/[guid]/graphs/[guid]/vectors               |
| Read node vectors        | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]/vectors  |
| Read edge vectors        | GET    | /v1.0/tenants/[guid]/graphs/[guid]/edges/[guid]/vectors  |
| Delete                   | DELETE | /v1.0/tenants/[guid]/vectors/[guid]                      |
| Delete all in tenant     | DELETE | /v1.0/tenants/[guid]/vectors/all                         |
| Delete all in graph      | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/vectors/all           |
| Delete graph vectors     | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/vectors               |
| Delete node vectors      | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]/vectors  |
| Delete edge vectors      | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/edges/[guid]/vectors  |
| Exists                   | HEAD   | /v1.0/tenants/[guid]/vectors/[guid]                      |
| Search                   | POST   | /v1.0/tenants/[guid]/vectors                             |
| Search in graph          | POST   | /v1.0/tenants/[guid]/graphs/[guid]/vectors/search        |

Both vector search `POST` routes return the [enumeration envelope](#enumeration-and-pagination) whose `Objects` are [Vector Search Results](#vector-search-result).

## Graph APIs

| API                  | Method | URL                                                        |
|----------------------|--------|------------------------------------------------------------|
| Create               | PUT    | /v1.0/tenants/[guid]/graphs                                |
| Update               | PUT    | /v1.0/tenants/[guid]/graphs/[guid]                         |
| Read                 | GET    | /v1.0/tenants/[guid]/graphs/[guid]                         |
| Read many            | GET    | /v1.0/tenants/[guid]/graphs                                |
| Read many            | GET    | /v1.0/tenants/[guid]/graphs?guids=...                      |
| Read all in tenant   | GET    | /v1.0/tenants/[guid]/graphs/all                            |
| Read first           | POST   | /v1.0/tenants/[guid]/graphs/first                          |
| Statistics           | GET    | /v1.0/tenants/[guid]/graphs/[guid]/stats                   |
| All graph statistics | GET    | /v1.0/tenants/[guid]/graphs/stats                          |
| Delete               | DELETE | /v1.0/tenants/[guid]/graphs/[guid]                         |
| Delete w/ cascade    | DELETE | /v1.0/tenants/[guid]/graphs/[guid]?force                   |
| Delete all in tenant | DELETE | /v1.0/tenants/[guid]/graphs/all                            |
| Exists               | HEAD   | /v1.0/tenants/[guid]/graphs/[guid]                         |
| Search               | POST   | /v1.0/tenants/[guid]/graphs/search                         |
| Render as GEXF       | GET    | /v1.0/tenants/[guid]/graphs/[guid]/export/gexf?incldata    |
| Export as JSONL      | GET    | /v1.0/tenants/[guid]/graphs/[guid]/export/jsonl?incldata&inclsub |
| Export subgraph JSONL | POST  | /v1.0/tenants/[guid]/graphs/[guid]/export/jsonl            |
| Import JSONL (merge) | POST   | /v1.0/tenants/[guid]/graphs/[guid]/import/jsonl?guidstrategy=regenerate&onerror=abort&batchsize=1000 |
| Import JSONL (new graph) | POST | /v1.0/tenants/[guid]/graphs/import/jsonl?guidstrategy=regenerate&onerror=abort&batchsize=1000 |
| Batch existence      | POST   | /v1.0/tenants/[guid]/graphs/[guid]/existence               |
| Native query         | POST   | /v1.0/tenants/[guid]/graphs/[guid]/query                   |
| Graph transaction    | POST   | /v1.0/tenants/[guid]/graphs/[guid]/transaction             |
| Node subgraph        | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]/subgraph   |
| Node subgraph stats  | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]/subgraph/stats |

Native graph query and graph transaction endpoints are graph scoped. They cannot cross tenants or graphs.

The graph `Read many` and `Read all in tenant` routes return the [enumeration envelope](#enumeration-and-pagination). The `search` route (like node and edge `search`) returns a `SearchResult`-shaped object (`Graphs`/`Nodes`/`Edges` arrays inside an object) and is intentionally not enveloped; the statistics, GEXF, and JSONL export routes likewise keep their own shapes.

The four JSONL endpoints move a graph (or a slice of one) as newline-delimited JSON. Export needs read scope; import needs write scope. Both directions stream: exports send a chunked `application/x-ndjson` body so the server never buffers the whole graph in memory, and imports read the request body line by line. `GET .../export/jsonl` renders an entire graph and doubles as a provider-agnostic per-graph backup; `incldata` and `inclsub` are valueless flags that pull in the `Data` object and subordinate labels, tags, and vectors. `POST .../export/jsonl` takes a `SubgraphExtractionRequest` body and streams only the traversed subgraph. The `.../import/jsonl` endpoints accept a raw JSONL body and return a `GraphImportResult`; the graph-scoped form merges into an existing graph, and the tenant-scoped `graphs/import/jsonl` form creates a new graph. A missing graph returns `404`; a malformed request or JSONL line under `onerror=abort` returns `400`; a GUID collision under the `preserve` strategy returns `409`.

Import query parameters:

| Parameter | Values | Default | Meaning |
|-----------|--------|---------|---------|
| `guidstrategy` | `preserve`, `regenerate`, `skip`, `overwrite` | `regenerate` | How incoming GUIDs are reconciled against the store |
| `onerror` | `abort`, `skip` | `abort` | Whether a bad line stops the import or is counted and skipped |
| `batchsize` | positive integer | `1000` | Nodes buffered per insert batch |

## Graph Vector Index APIs

| API                | Method |                                                            |
|--------------------|--------|------------------------------------------------------------|
| Enable             | PUT    | /v1.0/tenants/[guid]/graphs/[guid]/vectorindex/enable      |
| Delete             | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/vectorindex             | 
| Read configuration | GET    | /v1.0/tenants/[guid]/graphs/[guid]/vectorindex/config      | 
| Read statistics    | GET    | /v1.0/tenants/[guid]/graphs/[guid]/vectorindex/stats       | 
| Rebuild index      | POST   | /v1.0/tenants/[guid]/graphs/[guid]/vectorindex/rebuild     | 

## Node APIs

| API                      | Method | URL                                                      |
|--------------------------|--------|----------------------------------------------------------|
| Create                   | PUT    | /v1.0/tenants/[guid]/graphs/[guid]/nodes                 |
| Create many              | PUT    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/bulk?return=minimal |
| Update                   | PUT    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]          |
| Read                     | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]          |
| Read many                | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes                 |
| Read many                | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes?guids=...       |
| Read all in tenant       | GET    | /v1.0/tenants/[guid]/nodes/all                           |
| Read all in graph        | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/all             |
| Read most connected      | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/mostconnected   |
| Read least connected     | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/leastconnected  |
| Delete                   | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]          |
| Delete all in graph      | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/nodes/all             |
| Delete all in tenant     | DELETE | /v1.0/tenants/[guid]/nodes/all                           |
| Delete multiple          | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/nodes/bulk            |
| Exists                   | HEAD   | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]          |
| Search                   | POST   | /v1.0/tenants/[guid]/graphs/[guid]/nodes/search          |

The node list routes — `Read many`, both `Read all` variants, `mostconnected`, and `leastconnected` — return the [enumeration envelope](#enumeration-and-pagination) and accept the shared pagination query parameters.

## Edge APIs

| API                      | Method | URL                                                       |
|--------------------------|--------|-----------------------------------------------------------|
| Create                   | PUT    | /v1.0/tenants/[guid]/graphs/[guid]/edges                  |
| Create many              | PUT    | /v1.0/tenants/[guid]/graphs/[guid]/edges/bulk?return=minimal |
| Update                   | PUT    | /v1.0/tenants/[guid]/graphs/[guid]/edges/[guid]           |
| Read                     | GET    | /v1.0/tenants/[guid]/graphs/[guid]/edges/[guid]           |
| Read many                | GET    | /v1.0/tenants/[guid]/graphs/[guid]/edges                  |
| Read many                | GET    | /v1.0/tenants/[guid]/graphs/[guid]/edges?guids=...        |
| Read all in tenant       | GET    | /v1.0/tenants/[guid]/edges/all                            |
| Read all in graph        | GET    | /v1.0/tenants/[guid]/graphs/[guid]/edges/all              |
| Read between nodes       | GET    | /v1.0/tenants/[guid]/graphs/[guid]/edges/between          |
| Delete                   | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/edges/[guid]           |
| Delete all in graph      | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/edges/all              |
| Delete all in tenant     | DELETE | /v1.0/tenants/[guid]/edges/all                            |
| Delete multiple          | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/edges/bulk             |
| Delete node edges        | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]/edges     |
| Delete node edges (bulk) | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/nodes/edges            |
| Exists                   | HEAD   | /v1.0/tenants/[guid]/graphs/[guid]/edges/[guid]           |
| Search                   | POST   | /v1.0/tenants/[guid]/graphs/[guid]/edges/search           |

The edge list routes — `Read many`, both `Read all` variants, and `Read between nodes` (`?from=[guid]&to=[guid]`) — return the [enumeration envelope](#enumeration-and-pagination) and accept the shared pagination query parameters.

## Traversal and Networking

| API                            | Method | URL                                                         |
|--------------------------------|--------|-------------------------------------------------------------|
| Get edges from a node          | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]/edges/from  |
| Get edges to a node            | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]/edges/to    |
| Get edges connected to a node  | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]/edges       |
| Get node neighbors             | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]/neighbors   |
| Get node parents               | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]/parents     |
| Get node children              | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]/children    |
| Get routes between nodes       | POST   | /v1.0/tenants/[guid]/graphs/[guid]/routes                   |

Every traversal `GET` route returns the [enumeration envelope](#enumeration-and-pagination) (of edges or of nodes, as appropriate) and accepts the shared pagination query parameters. The node-edges route also accepts `POST` with a filter body and returns the same envelope.

## Request History APIs

Request history APIs require read/admin access according to the authenticated principal and optional tenant scope. Request history is intended for recent diagnostics; use Prometheus and OpenTelemetry for aggregate monitoring.

| API                     | Method | URL                                       |
|-------------------------|--------|-------------------------------------------|
| List request history    | GET    | /v1.0/requesthistory                      |
| Request history summary | GET    | /v1.0/requesthistory/summary              |
| Read entry              | GET    | /v1.0/requesthistory/[requestGuid]        |
| Read detailed entry     | GET    | /v1.0/requesthistory/[requestGuid]/detail |
| Delete entry            | DELETE | /v1.0/requesthistory/[requestGuid]        |
| Bulk delete             | DELETE | /v1.0/requesthistory/bulk                 |

`GET /v1.0/requesthistory` returns the [enumeration envelope](#enumeration-and-pagination) of request history entries. Common query-string filters include `tenantGuid`, `method`, `statusCode`, `success`, `path`, `sourceIp`, `hasTransactionDiagnostics`, `transactionId`, paging (legacy `page`/`pageSize` remain accepted, with `max-keys` and `skip` taking precedence), and time-range filters. The summary and per-entry reads return single objects. Detailed entries include captured request/response metadata subject to configured redaction and truncation.

Graph transaction entries include `TransactionDiagnosticsJson` when LiteGraph can parse the transaction result body. The compact JSON includes transaction ID, operation count, isolation level, provider, rollback and validation state, retry/conflict fields, and provider error code.
Use `hasTransactionDiagnostics=true` to list only graph transaction rows, `hasTransactionDiagnostics=false` to exclude them, and `transactionId=[full-or-partial-id]` to find entries for a known transaction ID.

## Enumeration APIs

The v2.0 enumeration routes accept an [Enumeration Query](#enumeration-query) as JSON on POST and the shared pagination query parameters (`max-keys`, `skip`, `order`, `token`) on GET where supported. All of them return the [enumeration envelope](#enumeration-and-pagination).

| Resource      | Method | URL                                                    |
|---------------|--------|--------------------------------------------------------|
| Tenants       | GET    | /v2.0/tenants                                         |
| Tenants       | POST   | /v2.0/tenants                                         |
| Users         | GET    | /v2.0/tenants/[tenantGuid]/users                      |
| Users         | POST   | /v2.0/tenants/[tenantGuid]/users                      |
| Credentials   | GET    | /v2.0/tenants/[tenantGuid]/credentials                |
| Credentials   | POST   | /v2.0/tenants/[tenantGuid]/credentials                |
| Graphs        | GET    | /v2.0/tenants/[tenantGuid]/graphs                     |
| Graphs        | POST   | /v2.0/tenants/[tenantGuid]/graphs                     |
| Nodes         | GET    | /v2.0/tenants/[tenantGuid]/graphs/[graphGuid]/nodes   |
| Nodes         | POST   | /v2.0/tenants/[tenantGuid]/graphs/[graphGuid]/nodes   |
| Edges         | GET    | /v2.0/tenants/[tenantGuid]/graphs/[graphGuid]/edges   |
| Edges         | POST   | /v2.0/tenants/[tenantGuid]/graphs/[graphGuid]/edges   |
| Labels        | GET    | /v2.0/tenants/[tenantGuid]/labels                     |
| Labels        | POST   | /v2.0/tenants/[tenantGuid]/labels                     |
| Graph labels  | POST   | /v2.0/tenants/[tenantGuid]/graphs/[graphGuid]/labels  |
| Tags          | GET    | /v2.0/tenants/[tenantGuid]/tags                       |
| Tags          | POST   | /v2.0/tenants/[tenantGuid]/tags                       |
| Graph tags    | POST   | /v2.0/tenants/[tenantGuid]/graphs/[graphGuid]/tags    |
| Vectors       | GET    | /v2.0/tenants/[tenantGuid]/vectors                    |
| Vectors       | POST   | /v2.0/tenants/[tenantGuid]/vectors                    |
| Graph vectors | POST   | /v2.0/tenants/[tenantGuid]/graphs/[graphGuid]/vectors |

## Chat APIs

Introduced in v8.1. Chat routes live under `/v1.0/tenants/[tenantGuid]/chat` and use the same authentication as every other route. Authorization splits along two lines: endpoint management, endpoint testing, health reads, feedback administration, chat settings updates, and all-users thread listing require a system administrator or a tenant administrator of the target tenant; completions, own-thread operations, feedback submission, and chat settings reads are open to any authenticated tenant principal. Completions, thread creation, and feedback submission additionally require a **user principal** — a user login or a user-linked credential — because threads and feedback are owned by a user. The break-glass administrator token has no user identity and is rejected with `400` on those routes.

See [CHAT.md](CHAT.md) for the architecture: providers, the tool loop, retrieval, retries, telemetry, and health checking.

### Chat Endpoints

| API                     | Method | URL                                                             |
|-------------------------|--------|-----------------------------------------------------------------|
| Create                  | PUT    | /v1.0/tenants/[guid]/chat/endpoints                             |
| Read many               | GET    | /v1.0/tenants/[guid]/chat/endpoints                             |
| Read many by type       | GET    | /v1.0/tenants/[guid]/chat/endpoints?endpointType=[type]         |
| Read health (all)       | GET    | /v1.0/tenants/[guid]/chat/endpoints/health                      |
| Read health (one)       | GET    | /v1.0/tenants/[guid]/chat/endpoints/[guid]/health               |
| Read                    | GET    | /v1.0/tenants/[guid]/chat/endpoints/[guid]                      |
| Update                  | PUT    | /v1.0/tenants/[guid]/chat/endpoints/[guid]                      |
| Delete                  | DELETE | /v1.0/tenants/[guid]/chat/endpoints/[guid]                      |
| Exists                  | HEAD   | /v1.0/tenants/[guid]/chat/endpoints/[guid]                      |
| Test connectivity       | POST   | /v1.0/tenants/[guid]/chat/endpoints/[guid]/test                 |

`endpointType` is `Completion` or `Embedding`. Both `Read many` variants and `Read health (all)` return the [enumeration envelope](#enumeration-and-pagination) — of `ChatEndpoint` and `ChatEndpointHealth` records respectively — and accept the shared pagination query parameters; `Read health (one)` returns a single health object. Create and update take a `ChatEndpoint` body:

```
{
    "Name": "Local Ollama",
    "EndpointType": "Completion",
    "Provider": "Ollama",
    "Endpoint": "http://localhost:11434",
    "ApiKey": null,
    "Model": "gemma3:4b",
    "MaxOutputTokens": 4096,
    "Temperature": 0.7,
    "TimeoutMs": 120000,
    "MaxConcurrentRequests": 2,
    "Active": true,
    "HealthCheckEnabled": true,
    "HealthCheckUrl": null,
    "HealthCheckMethod": "GET",
    "HealthCheckIntervalMs": 30000,
    "HealthCheckTimeoutMs": 10000,
    "HealthCheckExpectedStatusCode": 200,
    "HealthyThreshold": 2,
    "UnhealthyThreshold": 2,
    "HealthCheckUseAuth": false
}
```

Validation on create and update returns `400` with a `Description` explaining the failure when: `Name`, `Endpoint`, or `Model` is missing; `Endpoint` is not an absolute `http`/`https` URL; the provider is `Anthropic` with type `Embedding` (Anthropic is completion-only); or the provider is `VoyageAI` with type `Completion` (VoyageAI is embedding-only). Valid providers are `OpenAI` (which also covers any OpenAI-compatible server), `Ollama`, `Gemini`, `Anthropic`, and `VoyageAI`.

**ApiKey redaction contract.** Every response — create, read, list, update — redacts `ApiKey` to eight asterisks plus its last four characters (`"********abcd"`). Sending that redacted placeholder back on update preserves the stored key unchanged; sending a new plaintext value replaces it; sending `null` clears it. Clients therefore never need to hold the plaintext key to round-trip an endpoint object.

`POST .../test` probes the upstream and returns:

```
{
    "Reachable": true,
    "Models": [ "gemma3:4b", "llama3.2:3b" ],
    "ModelExists": true,
    "Error": null,
    "RuntimeMs": 84.2
}
```

`Models` and `ModelExists` are omitted for providers without a model-listing API (VoyageAI). The health routes return `ChatEndpointHealth` records with `Monitored`, `Healthy` (null until background monitoring reaches a verdict), `LastCheckedUtc`, `LastError`, consecutive success/failure counts, `UptimePercentage`, and a rolling 24-hour `CheckHistory`.

### Chat Completions

| API        | Method | URL                                     |
|------------|--------|-----------------------------------------|
| Completion | POST   | /v1.0/tenants/[guid]/chat/completions   |

The request body:

```
{
    "ThreadGUID": null,
    "GraphGUID": "00000000-0000-0000-0000-000000000000",
    "Message": "What are the most connected nodes in this graph?",
    "Stream": false,
    "CompletionEndpointGUID": null,
    "EmbeddingEndpointGUID": null,
    "Temperature": 0.7,
    "MaxOutputTokens": 4096,
    "EnableTools": true,
    "EnableRag": true,
    "RagTopK": 8,
    "SystemPrompt": null
}
```

A null `ThreadGUID` creates a new thread, bound to `GraphGUID` when supplied; a non-null `ThreadGUID` continues an existing thread (owner or administrator only) and `GraphGUID` is ignored. Endpoint GUIDs default to the tenant chat settings; every nullable field falls back to the endpoint or tenant-settings value. With `Stream` false the response is `200` with a `ChatCompletionResult`:

```
{
    "ThreadGUID": "00000000-0000-0000-0000-000000000000",
    "TurnGUID": "00000000-0000-0000-0000-000000000000",
    "Message": "The most connected node is Ada (guid 11111111-...), with 14 edges.",
    "Reasoning": null,
    "Provider": "Ollama",
    "Model": "gemma3:4b",
    "PromptTokens": 812,
    "CompletionTokens": 96,
    "TimeToFirstTokenMs": 412.6,
    "TimeToLastTokenMs": 1873.4,
    "TotalDurationMs": 2405.1,
    "TokensPerSecondOverall": 51.2,
    "ToolCallCount": 2,
    "ToolLoopIterations": 3,
    "RetrievedChunkCount": 8,
    "RetryCount": 0
}
```

With `Stream` true the response is `200 text/event-stream`. Every frame is `data: <json>` where the JSON carries an `event` discriminator, and the stream always terminates with `data: [DONE]`:

| Event | Payload | Meaning |
|---|---|---|
| `started` | `threadGuid`, `turnGuid` | First frame; identifies the thread and turn before any output |
| `delta` | `content` | A fragment of assistant text |
| `thinking` | `content` | A fragment of model reasoning text, when the model emits any |
| `retrieval` | `chunks: [{nodeGuid, name, score}]` | The vector-retrieval results injected into the prompt |
| `tool_call` | `name`, `arguments` (JSON string), `iteration` | The model invoked a tool |
| `tool_result` | `name`, `success`, `error`, `runtimeMs` | The server executed the tool |
| `usage` | `usage` (a `ChatCompletionResult`) | Final telemetry frame on success |
| `error` | `message`, optional `statusCode` | The turn failed; `statusCode` carries the upstream status when known |

Comment keep-alive frames are emitted every `SseKeepAliveSeconds` (server setting) so idle proxies do not sever long generations.

Completion error responses:

| Status | Cause |
|---|---|
| `400` | Missing `Message`, no user principal, or no completion endpoint resolves from the request or the tenant defaults (or it is missing, inactive, or not a completion endpoint) |
| `403` | The tenant's chat settings have `EnableChat` false, or the thread belongs to another user |
| `404` | The supplied `ThreadGUID` does not exist |
| `429` | The server is at its `MaxConcurrentChats` capacity |
| `502` | The upstream provider failed after all pre-first-token retries |
| `503` | Chat is disabled server-wide (`Chat.Enable` false in `litegraph.json`) |

On a streaming connection these surface as an `error` event rather than an HTTP status once the stream has opened. Failed turns are persisted with `Success=false` and the upstream status and error, so the failure is visible in the thread's turn history.

### Chat Models

| API       | Method | URL                                     |
|-----------|--------|-----------------------------------------|
| Read many | GET    | /v1.0/tenants/[guid]/chat/models        |

Lists the tenant's selectable chat models for any tenant member, inside the [enumeration envelope](#enumeration-and-pagination): each entry in `Objects` is an active endpoint projected to `GUID`, `Name`, `Model`, `Provider`, `EndpointType`, and `IsDefault`. Endpoint URLs, API keys, and health configuration are never included, so the full endpoint listing can stay administrator-only while chat users still pick a model. Supply an entry's `GUID` as `CompletionEndpointGUID` (or `EmbeddingEndpointGUID`) on completion requests to override the tenant default.

### Graph-Scoped Compatible Chat

| API                        | Method | URL                                                        |
|----------------------------|--------|------------------------------------------------------------|
| OpenAI-format completion   | POST   | /v1.0/tenants/[guid]/graphs/[guid]/chat/completions        |
| Ollama-format chat         | POST   | /v1.0/tenants/[guid]/graphs/[guid]/chat/ollama             |
| OpenAI-format model list   | GET    | /v1.0/tenants/[guid]/graphs/[guid]/chat/models             |

These routes let any application that already speaks the OpenAI chat completions protocol or the Ollama `/api/chat` protocol point at LiteGraph and chat with a specific graph, without knowing LiteGraph's own chat API. The URLs are LiteGraph's; the request and response bodies are wire-compatible with the respective protocol. For that reason these three routes — including the model list — are the deliberate exception to the [enumeration envelope](#enumeration-and-pagination) mandate: they keep the OpenAI and Ollama wire shapes exactly. Authentication is normal LiteGraph authentication (bearer credential or `x-token`), and authorization is member-level chat, identical to the native completion route.

The routes are stateless at the protocol level: the client supplies the entire `messages` transcript on every call. System messages from the request are appended after LiteGraph's own system prompt, earlier user and assistant messages are replayed as history, and the final user message is treated as the current message. The graph in the URL must exist in the tenant (`404` otherwise) and becomes the bound context for the exchange — the tool loop and retrieval run per tenant chat settings with that graph as the retrieval target. Each exchange is persisted as a turn in an implicit per-user, per-graph thread titled `OpenAI-compatible: <graph name>`, so telemetry and history remain visible through the normal thread APIs.

Model selection is shared by both POST routes: when `model` is present it selects the tenant chat endpoint whose `Name`, `Model`, or `GUID` matches (case-insensitive, active completion endpoints only); when absent or empty the tenant default completion endpoint is used. An unknown `model` yields `404`. All error responses on these routes use the OpenAI error envelope rather than LiteGraph's:

```
{
    "error": {
        "message": "The model 'no-such-model' does not exist.  Supply a chat endpoint name, model, or GUID, or omit model to use the tenant default.",
        "type": "invalid_request_error"
    }
}
```

An OpenAI-format request (`temperature`, `max_tokens`, and its synonym `max_completion_tokens` are optional; unknown fields are ignored):

```
{
    "model": "my-endpoint",
    "messages": [
        { "role": "system", "content": "Answer briefly." },
        { "role": "user", "content": "What are the most connected nodes?" }
    ],
    "temperature": 0.7,
    "max_tokens": 4096,
    "stream": false
}
```

The non-streaming response is a standard `chat.completion` object; `model` reflects the resolved endpoint's `Model`:

```
{
    "id": "chatcmpl-00000000-0000-0000-0000-000000000000",
    "object": "chat.completion",
    "created": 1767225600,
    "model": "gemma3:4b",
    "choices": [
        {
            "index": 0,
            "message": { "role": "assistant", "content": "The most connected node is Ada, with 14 edges." },
            "finish_reason": "stop"
        }
    ],
    "usage": { "prompt_tokens": 812, "completion_tokens": 96, "total_tokens": 908 }
}
```

With `"stream": true` the response is `text/event-stream` carrying `data: <chat.completion.chunk>` frames: the first chunk carries `delta.role`, subsequent chunks carry `delta.content` fragments, the terminal chunk carries `finish_reason` (`stop` or `length`), a usage-bearing chunk follows when `stream_options.include_usage` is set, and the stream ends with `data: [DONE]`.

The Ollama-format route accepts the `/api/chat` request shape (`stream` defaults to true, matching Ollama; `options.temperature` and `options.num_predict` are honored):

```
{
    "model": "my-endpoint",
    "messages": [ { "role": "user", "content": "What are the most connected nodes?" } ],
    "stream": false,
    "options": { "temperature": 0.7, "num_predict": 4096 }
}
```

The non-streaming response:

```
{
    "model": "gemma3:4b",
    "created_at": "2026-01-01T00:00:00.0000000Z",
    "message": { "role": "assistant", "content": "The most connected node is Ada, with 14 edges." },
    "done": true,
    "done_reason": "stop",
    "total_duration": 2405100000,
    "eval_duration": 1460800000,
    "prompt_eval_count": 812,
    "eval_count": 96
}
```

With streaming enabled the response is newline-delimited JSON (`application/x-ndjson`): each line carries a `message.content` fragment with `done` false, and the final line has `done` true with the duration and token counters.

The model list returns the OpenAI models shape, with one entry per active completion endpoint. `id` is the endpoint's `Name`; `Name`, `Model`, and `GUID` are all accepted in the `model` field of the POST routes.

```
{
    "object": "list",
    "data": [
        { "id": "my-endpoint", "object": "model", "created": 1767225600, "owned_by": "litegraph" }
    ]
}
```

### Chat Threads

| API             | Method | URL                                              |
|-----------------|--------|--------------------------------------------------|
| Create          | PUT    | /v1.0/tenants/[guid]/chat/threads                |
| Read many (own) | GET    | /v1.0/tenants/[guid]/chat/threads                |
| Read many (all) | GET    | /v1.0/tenants/[guid]/chat/threads?all            |
| Read            | GET    | /v1.0/tenants/[guid]/chat/threads/[guid]         |
| Read turns      | GET    | /v1.0/tenants/[guid]/chat/threads/[guid]/turns   |
| Update (rename) | PUT    | /v1.0/tenants/[guid]/chat/threads/[guid]         |
| Delete          | DELETE | /v1.0/tenants/[guid]/chat/threads/[guid]         |

Create takes an optional body of `{ "GraphGUID": ..., "Title": ... }`; the caller becomes the owner, and a missing title is generated from the first exchange. The plain list returns the caller's own threads; the valueless `all` flag returns every user's threads and requires an administrator. Both thread listings and the turns listing return the [enumeration envelope](#enumeration-and-pagination) and accept the shared pagination query parameters, including marker-based continuation via `token`. Update renames a thread — the body is `{ "Title": ... }`, only `Title` is honored, and a blank title is rejected with `400`. Read, turns, update, and delete are available to the owner or an administrator. Deleting a thread removes its turns and their feedback. Turns are returned ascending by `Sequence` as full `ChatTurn` objects in the envelope's `Objects` array, including all telemetry columns plus `ToolTranscriptJson` and `TelemetryJson` — see [CHAT.md](CHAT.md) for the field-by-field reference.

### Chat Feedback

| API       | Method | URL                                              |
|-----------|--------|--------------------------------------------------|
| Submit    | POST   | /v1.0/tenants/[guid]/chat/turns/[guid]/feedback  |
| Read many | GET    | /v1.0/tenants/[guid]/chat/feedback               |
| Read      | GET    | /v1.0/tenants/[guid]/chat/feedback/[guid]        |
| Delete    | DELETE | /v1.0/tenants/[guid]/chat/feedback/[guid]        |

Submission takes `{ "Rating": "ThumbsUp" | "ThumbsDown", "FeedbackText": "optional comment" }` and requires a user principal. Listing, reading, and deleting feedback are administrator operations. The `Read many` route returns the [enumeration envelope](#enumeration-and-pagination) and accepts the shared pagination query parameters, including marker-based continuation via `token`.

### Chat Settings

| API    | Method | URL                                  |
|--------|--------|--------------------------------------|
| Read   | GET    | /v1.0/tenants/[guid]/chat/settings   |
| Update | PUT    | /v1.0/tenants/[guid]/chat/settings   |

One settings record exists per tenant. Read is open to any authenticated tenant principal and returns the defaults when no record has been saved:

```
{
    "TenantGUID": "00000000-0000-0000-0000-000000000000",
    "DefaultCompletionEndpointGUID": null,
    "DefaultEmbeddingEndpointGUID": null,
    "SystemPrompt": null,
    "EnableChat": true,
    "EnableTools": true,
    "EnableMutationTools": false,
    "MaxToolIterations": 10,
    "EnableRag": true,
    "RagTopK": 8,
    "RagScoreThreshold": 0,
    "MaxContextTokens": 16384,
    "HistoryRetentionDays": 90,
    "CreatedUtc": "2026-08-31T00:00:00.000000Z",
    "LastUpdateUtc": "2026-08-31T00:00:00.000000Z"
}
```

Update is an administrator upsert of the full object. The default endpoint GUIDs are validated for existence and type — the completion default must reference an existing `Completion` endpoint and the embedding default an existing `Embedding` endpoint, otherwise the update is rejected with `400`.
