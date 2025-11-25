# REST API for LiteGraph

## Authentication

Users can authenticate API requests in one of three ways.

### Bearer Token

A bearer token can be supplied in the `Authorization` header, i.e. `Authorization: Bearer {token}`.  This bearer token can either be from a `Credential` object mapped to a user by GUID, or, the administrator bearer token defined in `litegraph.json`.  

### Credentials

The user's email, password, and tenant GUID can be passed in as headers using `x-email`, `x-email`, and `x-tenant-guid`.  This method does not work for administrative API calls, as the administrator is only defined by bearer token in `litegraph.json`.

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

If you do not know the tenant GUID ahead of time, use the API to retrieve tenants for a given email by calling `GET /v1.0/token/tenants` with the `x-email` header set.  It will return the list of tenants associated with the supplied email address.
```
GET /v1.0/token/tenants
x-email: default@user.com

Response:
[
    {
        "GUID": "00000000-0000-0000-0000-000000000000",
        "Name": "Default tenant",
        "Active": true,
        "CreatedUtc": "2025-02-06T18:22:56.789353Z",
        "LastUpdateUtc": "2025-02-06T18:22:56.788994Z"
    }
]
```

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

### Enumeration Query
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
    "CreatedUtc": "2024-12-27T22:09:09.446911Z",
    "LastUpdateUtc": "2024-12-27T22:09:09.446777Z"
}
```

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

```
[
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
```

## General APIs

| API                   | Method | URL |
|-----------------------|--------|-----|
| Validate connectivity | HEAD   | /   |

## Admin APIs

Admin APIs require administrator bearer token authentication.

| API                              | Method | URL         |
|----------------------------------|--------|-------------|
| Flush in-memory database to disk | POST   | /v1.0/flush |

## Backup APIs

Backup APIs require administrator bearer token authentication.

| API                | Method | URL                        |
|--------------------|--------|----------------------------|
| Create             | POST   | /v1.0/backups              |
| Read many          | GET    | /v1.0/backups              |
| Read               | GET    | /v1.0/backups/[guid]       |
| Delete             | DELETE | /v1.0/backups/[guid]       |
| Exists             | HEAD   | /v1.0/backups/[guid]       |

## Tenant APIs

Tenant APIs require administrator bearer token authentication.

When specifying multiple GUIDs to retrieve, i.e. `?guids=...`, use a comma-separated list of values, i.e. `?guids=00000000-0000-0000-0000-000000000000,11111111-1111-1111-1111-111111111111`.

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

User APIs require administrator bearer token authentication.

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

Credential APIs require administrator bearer token authentication.

| API                  | Method | URL                                           |
|----------------------|--------|-----------------------------------------------|
| Create               | PUT    | /v1.0/tenants/[guid]/credentials              |
| Update               | PUT    | /v1.0/tenants/[guid]/credentials/[guid]       |
| Read many            | GET    | /v1.0/tenants/[guid]/credentials              |
| Read many            | GET    | /v1.0/tenants/[guid]/credentials?guids=...    |
| Read                 | GET    | /v1.0/tenants/[guid]/credentials/[guid]       |
| Read by bearer token | GET    | /v1.0/credentials/bearer                      |
| Delete               | DELETE | /v1.0/tenants/[guid]/credentials/[guid]       |
| Delete all in tenant | DELETE | /v1.0/tenants/[guid]/credentials              |
| Delete by user       | DELETE | /v1.0/tenants/[guid]/users/[guid]/credentials |
| Exists               | HEAD   | /v1.0/tenants/[guid]/credentials/[guid]       |

## Label APIs

Label APIs require administrator bearer token authentication.

| API                      | Method | URL                                                       |
|--------------------------|--------|-----------------------------------------------------------|
| Create                   | PUT    | /v1.0/tenants/[guid]/labels                               |
| Create many              | PUT    | /v1.0/tenants/[guid]/labels/bulk                          |
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

## Graph APIs

| API                  | Method | URL                                                        |
|----------------------|--------|------------------------------------------------------------|
| Create               | PUT    | /v1.0/tenants/[guid]/graphs                                |
| Update               | PUT    | /v1.0/tenants/[guid]/graphs/[guid]                         |
| Read                 | GET    | /v1.0/tenants/[guid]/graphs/[guid]                         |
| Read many            | GET    | /v1.0/tenants/[guid]/graphs                                |
| Read many            | GET    | /v1.0/tenants/[guid]/graphs?guids=...                      |
| Read all in tenant   | GET    | /v1.0/tenants/[guid]/graphs/all                            |
| Delete               | DELETE | /v1.0/tenants/[guid]/graphs/[guid]                         |
| Delete w/ cascade    | DELETE | /v1.0/tenants/[guid]/graphs/[guid]?force                   |
| Delete all in tenant | DELETE | /v1.0/tenants/[guid]/graphs/all                            |
| Exists               | HEAD   | /v1.0/tenants/[guid]/graphs/[guid]                         |
| Search               | POST   | /v1.0/tenants/[guid]/graphs/search                         |
| Render as GEXF       | GET    | /v1.0/tenants/[guid]/graphs/[guid]/export/gexf?incldata    |
| Batch existence      | POST   | /v1.0/tenants/[guid]/graphs/[guid]/existence               |

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
| Create many              | PUT    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/bulk            |
| Update                   | PUT    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]          |
| Read                     | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]          |
| Read many                | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes                 |
| Read many                | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes?guids=...       |
| Read all in tenant       | GET    | /v1.0/tenants/[guid]/nodes                               |
| Read all in graph        | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/all             |
| Read most connected      | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/mostconnected   |
| Read least connected     | GET    | /v1.0/tenants/[guid]/graphs/[guid]/nodes/leastconnected  |
| Delete                   | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]          |
| Delete all in graph      | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/nodes/all             |
| Delete all in tenant     | DELETE | /v1.0/tenants/[guid]/nodes                               |
| Delete multiple          | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/nodes/bulk            |
| Exists                   | HEAD   | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]          |
| Search                   | POST   | /v1.0/tenants/[guid]/graphs/[guid]/nodes/search          |

## Edge APIs

| API                      | Method | URL                                                       |
|--------------------------|--------|-----------------------------------------------------------|
| Create                   | PUT    | /v1.0/tenants/[guid]/graphs/[guid]/edges                  |
| Create many              | PUT    | /v1.0/tenants/[guid]/graphs/[guid]/edges/bulk             |
| Update                   | PUT    | /v1.0/tenants/[guid]/graphs/[guid]/edges/[guid]           |
| Read                     | GET    | /v1.0/tenants/[guid]/graphs/[guid]/edges/[guid]           |
| Read many                | GET    | /v1.0/tenants/[guid]/graphs/[guid]/edges                  |
| Read many                | GET    | /v1.0/tenants/[guid]/graphs/[guid]/edges?guids=...        |
| Read all in tenant       | GET    | /v1.0/tenants/[guid]/edges                                |
| Read all in graph        | GET    | /v1.0/tenants/[guid]/graphs/[guid]/edges/all              |
| Read between nodes       | GET    | /v1.0/tenants/[guid]/graphs/[guid]/edges/between          |
| Delete                   | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/edges/[guid]           |
| Delete all in graph      | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/edges/all              |
| Delete all in tenant     | DELETE | /v1.0/tenants/[guid]/edges                                |
| Delete multiple          | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/edges/bulk             |
| Delete node edges        | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/nodes/[guid]/edges     |
| Delete node edges (bulk) | DELETE | /v1.0/tenants/[guid]/graphs/[guid]/nodes/edges            |
| Exists                   | HEAD   | /v1.0/tenants/[guid]/graphs/[guid]/edges/[guid]           |
| Search                   | POST   | /v1.0/tenants/[guid]/graphs/[guid]/edges/search           |

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
