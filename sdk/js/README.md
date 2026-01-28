<img src="../../assets/favicon.png" height="48">

# JavaScript SDK for LiteGraph

[![npm](https://img.shields.io/npm/v/litegraphdb.svg)](https://www.npmjs.com/package/litegraphdb)

This SDK is part of the [LiteGraph monorepo](../../README.md). For other language SDKs, see the [SDK overview](../README.md).

LiteGraph is a lightweight graph database with both relational and vector support, built using SQLite, with support for exporting to GEXF. LiteGraph is intended to be a multi-modal database primarily for providing persistence and retrieval for knowledge and artificial intelligence applications.

## Features

- Multi-tenant support with tenant GUID management
- Graph management
- Node and edge operations
- Route finding between nodes
- Search capabilities for graphs, nodes, and edges
- GEXF format export support
- Built-in retry mechanism and error handling
- Comprehensive logging system
- Access key authentication support

## Requirements

- Node.js v18.20.4
- npm

### Dependencies

-  `jest` - for testing
-  `msw` - for mocking the api
-  `superagent` - for making the api calls
-  `url` - for url parsing
-  `util` - for utility functions
-  `uuid` - for generating unique ids

## Installation

Use the command below to install the package.

```bash
npm i litegraphdb
```

## Quick Start

```js
import { LiteGraphSdk } from 'litegraphdb';

const sdk = new LiteGraphSdk(
    'https://api.litegraphdb.com',
    'your-tenant-guid',
    'your-access-key'
);
const graphGuid = 'example-graph-guid';

// Create a new graph
sdk.createGraph('graph-guid', 'MyGraph').then((graph) => {
  console.log('Created graph:', graph);
});


const newMultipleNodes = [
    {
        "Name": "Active Directory",
        "Labels": [
            "test"
        ],
        "Tags": {
            "Type": "ActiveDirectory"
        },
        "Data": {
            "Name": "Active Directory"
        }
    },
    {
        "Name": "Website",
        "Labels": [
            "test"
        ],
        "Tags": {
            "Type": "Website"
        },
        "Data": {
            "Name": "Website"
        }
    }
]
//Create multiple Nodes
sdk.createNodes(graphGuid,newMultipleNodes).then((nodes) => {
  console.log('Created Multiple Nodes:', nodes);
});

const searchNodes = async () => {
  // Graph object to update

  const searchRequest = {
    GraphGUID: '00900db5-c9b7-4631-b250-c9e635a9036e',
    Ordering: 'CreatedDescending',
    Expr: {
      Left: 'Hello',
      Operator: 'Equals',
      Right: 'World',
    },
  };

sdk.searchNodes(searchRequest).then((response) => {
  console.log('Search response:', response);
})
```


## API Endpoints Reference

### Tenant Operations

| Method | Description | Parameters | Returns | Endpoint |
|--------|-------------|------------|---------|----------|
| `readTenants` | Retrieves a list of all tenants. | `cancellationToken` (optional) - `AbortController` | `Promise<TenantMetaData[]>` - Array of tenants | `GET /v1.0/tenants` |
| `readTenant` | Retrieves a specific tenant by GUID. | `tenantGuid` (string) - The GUID of the tenant <br> `cancellationToken` (optional) - `AbortController` | `Promise<TenantMetaData>` - The tenant | `GET /v1.0/tenants/{tenantGuid}` |
| `createTenant` | Creates a new tenant. | `tenant` (TenantMetaData) - The tenant object <br> `tenant.name` (string) - Name of the tenant <br> `tenant.Active` (boolean) - Active status <br> `cancellationToken` (optional) - `AbortController` | `Promise<TenantMetaData>` - Created tenant | `PUT /v1.0/tenants` |
| `updateTenant` | Updates an existing tenant. | `tenant` (TenantMetaData) - The tenant object <br> `tenant.name` (string) - Name of the tenant <br> `tenant.Active` (boolean) - Active status <br> `guid` (string) - The GUID of the tenant <br> `cancellationToken` (optional) - `AbortController` | `Promise<TenantMetaData>` - Updated tenant | `PUT /v1.0/tenants/{guid}` |
| `deleteTenant` | Deletes a tenant by GUID. | `tenantGuid` (string) - The GUID of the tenant <br> `cancellationToken` (optional) - `AbortController` | `Promise<boolean>` | `DELETE /v1.0/tenants/{tenantGuid}` |
| `tenantExists` | Checks if a tenant exists by GUID. | `tenantGuid` (string) - The GUID of the tenant <br> `cancellationToken` (optional) - `AbortController` | `Promise<boolean>` | `HEAD /v1.0/tenants/{tenantGuid}` |
| `tenantDeleteForce` | Deletes a tenant forcibly. | `tenantGuid` (string) - The GUID of the tenant <br> `cancellationToken` (optional) - `AbortController` | `Promise<boolean>` | `DELETE /v1.0/tenants/{tenantGuid}?force` |

### User Operations

| Method | Description | Parameters | Returns | Endpoint |
|--------|-------------|------------|---------|----------|
| `readAllUsers` | Retrieves all users. | `cancellationToken` (optional) - `AbortController` | `Promise<UserMetadata[]>` | `GET /v1.0/tenants/{tenantGuid}/users` |
| `readUser` | Retrieves a specific user by GUID. | `userGuid` (string) - User GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<UserMetadata>` | `GET /v1.0/tenants/{tenantGuid}/users/{userGuid}` |
| `createUser` | Creates a new user. | `user` (Object) - User object with FirstName, LastName, Active, Email, Password <br> `cancellationToken` (optional) - `AbortController` | `Promise<UserMetadata>` | `PUT /v1.0/tenants/{tenantGuid}/users` |
| `existsUser` | Checks if a user exists by GUID. | `guid` (string) - User GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<boolean>` | `HEAD /v1.0/tenants/{tenantGuid}/users/{guid}` |
| `updateUser` | Updates an existing user. | `user` (Object) - User object with FirstName, LastName, Active, Email, Password <br> `guid` (string) - User GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<UserMetadata>` | `PUT /v1.0/tenants/{tenantGuid}/users/{guid}` |
| `deleteUser` | Deletes a user by GUID. | `guid` (string) - User GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<boolean>` | `DELETE /v1.0/tenants/{tenantGuid}/users/{guid}` |



### Authorization Operations


| Method | Description | Parameters | Returns | Endpoint |
|--------|-------------|------------|---------|----------|
| `generateToken` | Generates an authentication token. | `email` (string) - User's email <br> `password` (string) - User's password <br> `tenantId` (string) - Tenant ID <br> `cancellationToken` (optional) - `AbortController` | `Promise<Token>` | `GET /v1.0/token` |
| `getTokenDetails` | Fetches details about an authentication token. | `token` (string) - Authentication token <br> `cancellationToken` (optional) - `AbortController` | `Promise<Object>` | `GET /v1.0/token/details` |
| `getTenantsForEmail` | Retrieves tenants associated with an email address. | `email` (string) - The email address to lookup tenants for. <br> `cancellationToken` (optional) - `AbortController` | `Promise<TenantMetaData[]>` | `GET /v1.0/token/tenants` |


### Credential Operations

| Method | Description | Parameters | Returns | Endpoint |
|--------|-------------|------------|---------|----------|
| `readAllCredentials` | Retrieves all credentials. | `cancellationToken` (optional) - `AbortController` | `Promise<CredentialMetadata[]>` | `GET /v1.0/tenants/{tenantGuid}/credentials` |
| `readCredential` | Retrieves a specific credential by GUID. | `guid` (string) - Credential GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<CredentialMetadata>` | `GET /v1.0/tenants/{tenantGuid}/credentials/{guid}` |
| `createCredential` | Creates a new credential. | `credential` (Object) - Credential object with Name, BearerToken, Active <br> `cancellationToken` (optional) - `AbortController` | `Promise<CredentialMetadata>` | `PUT /v1.0/tenants/{tenantGuid}/credentials` |
| `updateCredential` | Updates an existing credential. | `credential` (Object) - Credential object with Name, BearerToken, Active <br> `guid` (string) - Credential GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<CredentialMetadata>` | `PUT /v1.0/tenants/{tenantGuid}/credentials/{guid}` |
| `deleteCredential` | Deletes a credential by GUID. | `guid` (string) - Credential GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<boolean>` | `DELETE /v1.0/tenants/{tenantGuid}/credentials/{guid}` |
| `existsCredential` | Checks if a credential exists by GUID. | `guid` (string) - Credential GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<boolean>` | `HEAD /v1.0/tenants/{tenantGuid}/credentials/{guid}` |



### Label Operations

| Method | Description | Parameters | Returns | Endpoint |
|--------|-------------|------------|---------|----------|
| `readAllLabels` | Retrieves all labels. | `cancellationToken` (optional) - `AbortController` | `Promise<LabelMetadata[]>` | `GET /v1.0/tenants/{tenantGuid}/labels` |
| `readLabel` | Retrieves a specific label by GUID. | `guid` (string) - Label GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<LabelMetadata>` | `GET /v1.0/tenants/{tenantGuid}/labels/{guid}` |
| `existsLabel` | Checks if a label exists by GUID. | `guid` (string) - Label GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<boolean>` | `HEAD /v1.0/tenants/{tenantGuid}/labels/{guid}` |
| `createLabel` | Creates a new label. | `label` (Object) - Label object <br> `cancellationToken` (optional) - `AbortController` | `Promise<LabelMetadata>` | `PUT /v1.0/tenants/{tenantGuid}/labels` |
| `updateLabel` | Updates an existing label. | `label` (Object) - Label object <br> `guid` (string) - Label GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<LabelMetadata>` | `PUT /v1.0/tenants/{tenantGuid}/labels/{guid}` |
| `deleteLabel` | Deletes a label by GUID. | `guid` (string) - Label GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<void>` | `DELETE /v1.0/tenants/{tenantGuid}/labels/{guid}` |



### Tag Operations


| Method | Description | Parameters | Returns | Endpoint |
|--------|-------------|------------|---------|----------|
| `readAllTags` | Retrieves all tags. | `cancellationToken` (optional) - `AbortController` | `Promise<TagMetaData[]>` | `GET /v1.0/tenants/{tenantGuid}/tags` |
| `readTag` | Retrieves a specific tag by GUID. | `guid` (string) - Tag GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<TagMetaData>` | `GET /v1.0/tenants/{tenantGuid}/tags/{guid}` |
| `existsTag` | Checks if a tag exists by GUID. | `guid` (string) - Tag GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<boolean>` | `HEAD /v1.0/tenants/{tenantGuid}/tags/{guid}` |
| `createTag` | Creates a new tag. | `tag` (Object) - Tag object <br> `cancellationToken` (optional) - `AbortController` | `Promise<TagMetaData>` | `PUT /v1.0/tenants/{tenantGuid}/tags` |
| `updateTag` | Updates an existing tag. | `tag` (Object) - Tag object <br> `guid` (string) - Tag GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<TagMetaData>` | `PUT /v1.0/tenants/{tenantGuid}/tags/{guid}` |
| `deleteTag` | Deletes a tag by GUID. | `guid` (string) - Tag GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<void>` | `DELETE /v1.0/tenants/{tenantGuid}/tags/{guid}` |


### Vector Operations

| Method | Description | Parameters | Returns | Endpoint |
|--------|-------------|------------|---------|----------|
| `readAllVectors` | Retrieves all vectors. | `cancellationToken` (optional) - `AbortController` | `Promise<VectorMetadata[]>` | `GET /v1.0/tenants/{tenantGuid}/vectors` |
| `readVector` | Retrieves a specific vector by GUID. | `guid` (string) - Vector GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<VectorMetadata>` | `GET /v1.0/tenants/{tenantGuid}/vectors/{guid}` |
| `existsVector` | Checks if a vector exists by GUID. | `guid` (string) - Vector GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<boolean>` | `HEAD /v1.0/tenants/{tenantGuid}/vectors/{guid}` |
| `createVector` | Creates a new vector. | `vector` (Object) - Vector object <br> `cancellationToken` (optional) - `AbortController` | `Promise<VectorMetadata>` | `PUT /v1.0/tenants/{tenantGuid}/vectors` |
| `updateVector` | Updates an existing vector. | `vector` (Object) - Vector object <br> `guid` (string) - Vector GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<VectorMetadata>` | `PUT /v1.0/tenants/{tenantGuid}/vectors/{guid}` |
| `deleteVector` | Deletes a vector by GUID. | `guid` (string) - Vector GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<void>` | `DELETE /v1.0/tenants/{tenantGuid}/vectors/{guid}` |
| `searchVectors` | Searches vectors based on criteria. | `searchReq` (Object) - Search request with GraphGUID, Domain, SearchType, Labels <br> `cancellationToken` (optional) - `AbortController` | `Promise<VectorSearchResult>` | `POST /v1.0/tenants/{tenantGuid}/vectors` |


### Graph Operations

| Method | Description | Parameters | Returns | Endpoint |
|--------|-------------|------------|---------|----------|
| `graphExists` | Checks if a graph exists by GUID. | `guid` (string) - Graph GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<boolean>` | `HEAD /v1.0/tenants/{tenantGuid}/graphs/{guid}` |
| `createGraph` | Creates a new graph. | `guid` (string) - Graph GUID <br> `name` (string) - Name of the graph <br> `data` (Object) - Graph metadata (optional) <br> `cancellationToken` (optional) - `AbortController` | `Promise<Graph>` | `PUT /v1.0/tenants/{tenantGuid}/graphs` |
| `readGraphs` | Retrieves all graphs. | `cancellationToken` (optional) - `AbortController` | `Promise<Graph[]>` | `GET /v1.0/tenants/{tenantGuid}/graphs` |
| `searchGraphs` | Searches for graphs based on criteria. | `searchReq` (Object) - Search request with filters <br> `cancellationToken` (optional) - `AbortController` | `Promise<SearchResult>` | `POST /v1.0/tenants/{tenantGuid}/graphs/search` |
| `readGraph` | Retrieves a specific graph by GUID. | `guid` (string) - Graph GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<Graph>` | `GET /v1.0/tenants/{tenantGuid}/graphs/{guid}` |
| `updateGraph` | Updates an existing graph. | `graph` (Object) - Graph object with GUID, name, metadata <br> `cancellationToken` (optional) - `AbortController` | `Promise<Graph>` | `PUT /v1.0/tenants/{tenantGuid}/graphs/{graph.GUID}` |
| `deleteGraph` | Deletes a graph by GUID. | `guid` (string) - Graph GUID <br> `force` (boolean) - Force recursive deletion of edges and nodes (optional) <br> `cancellationToken` (optional) - `AbortController` | `Promise<void>` | `DELETE /v1.0/tenants/{tenantGuid}/graphs/{guid}?force=true` |
| `exportGraphToGexf` | Exports a graph to GEXF format. | `guid` (string) - Graph GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<string>` | `GET /v1.0/tenants/{tenantGuid}/graphs/{guid}/export/gexf` |




### Node Operations

| Method | Description | Parameters | Returns | Endpoint |
|--------|-------------|------------|---------|----------|
| `nodeExists` | Checks if a node exists by GUID. | `graphGuid` (string) - Graph GUID <br> `guid` (string) - Node GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<boolean>` | `HEAD /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{guid}` |
| `createNodes` | Creates multiple nodes. | `graphGuid` (string) - Graph GUID <br> `nodes` (Array<Object>) - List of node objects <br> `cancellationToken` (optional) - `AbortController` | `Promise<Array<Object>>` | `PUT /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/multiple` |
| `createNode` | Creates a single node. | `node` (Object) - Node object with GUID, GraphGUID, name, data, CreatedUtc <br> `cancellationToken` (optional) - `AbortController` | `Promise<Node>` | `PUT /v1.0/tenants/{tenantGuid}/graphs/{node.GraphGUID}/nodes` |
| `readNodes` | Retrieves all nodes in a graph. | `graphGuid` (string) - Graph GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<Node[]>` | `GET /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes` |
| `searchNodes` | Searches for nodes based on criteria. | `searchReq` (Object) - Search request object with GraphGUID, Ordering, Expr <br> `cancellationToken` (optional) - `AbortController` | `Promise<SearchResult>` | `POST /v1.0/tenants/{tenantGuid}/graphs/{searchReq.GraphGUID}/nodes/search` |
| `readNode` | Retrieves a specific node by GUID. | `graphGuid` (string) - Graph GUID <br> `nodeGuid` (string) - Node GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<Node>` | `GET /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}` |
| `updateNode` | Updates an existing node. | `node` (Object) - Node object with GUID, GraphGUID, name, data, CreatedUtc <br> `cancellationToken` (optional) - `AbortController` | `Promise<Node>` | `PUT /v1.0/tenants/{tenantGuid}/graphs/{node.GraphGUID}/nodes/{node.GUID}` |
| `deleteNode` | Deletes a node by GUID. | `graphGuid` (string) - Graph GUID <br> `nodeGuid` (string) - Node GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<void>` | `DELETE /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}` |
| `deleteNodes` | Deletes all nodes in a graph. | `graphGuid` (string) - Graph GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<void>` | `DELETE /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/all` |
| `deleteMultipleNodes` | Deletes multiple nodes by GUIDs. | `graphGuid` (string) - Graph GUID <br> `nodeGuids` (Array<string>) - List of node GUIDs <br> `cancellationToken` (optional) - `AbortController` | `Promise<void>` | `DELETE /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/multiple` |


### Edges Operations

| Method | Description | Parameters | Returns | Endpoint |
|--------|-------------|------------|---------|----------|
| `edgeExists` | Checks if an edge exists by GUID. | `graphGuid` (string) - Graph GUID <br> `guid` (string) - Edge GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<boolean>` | `HEAD /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{guid}` |
| `createEdges` | Creates multiple edges. | `graphGuid` (string) - The GUID of the graph <br> `edges` (Array<Object>) - List of edge objects <br> `cancellationToken` (optional) - `AbortController` | `Promise<Array<Object>>` | `PUT /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/multiple` |
| `createEdge` | Creates an edge. | `edge` (Object) - Edge object with GUID, GraphGUID, Name, From, To, Cost, CreatedUtc, Data <br> `cancellationToken` (optional) - `AbortController` | `Promise<Edge>` | `PUT /v1.0/tenants/{tenantGuid}/graphs/{edge.GraphGUID}/edges` |
| `readEdges` | Retrieves all edges in a graph. | `graphGuid` (string) - Graph GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<Edge[]>` | `GET /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges` |
| `searchEdges` | Searches for edges based on criteria. | `searchReq` (Object) - Search request object containing GraphGUID, Ordering, Expr <br> `cancellationToken` (optional) - `AbortController` | `Promise<SearchResult>` | `POST /v1.0/tenants/{tenantGuid}/graphs/{searchReq.GraphGUID}/edges/search` |
| `readEdge` | Retrieves an edge by GUID. | `graphGuid` (string) - Graph GUID <br> `edgeGuid` (string) - Edge GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<Edge>` | `GET /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}` |
| `updateEdge` | Updates an edge. | `edge` (Object) - Edge object with GUID, GraphGUID, Name, From, To, Cost, CreatedUtc, Data <br> `cancellationToken` (optional) - `AbortController` | `Promise<Edge>` | `PUT /v1.0/tenants/{tenantGuid}/graphs/{edge.GraphGUID}/edges/{edge.GUID}` |
| `deleteEdge` | Deletes an edge by GUID. | `graphGuid` (string) - Graph GUID <br> `edgeGuid` (string) - Edge GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<void>` | `DELETE /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/{edgeGuid}` |
| `deleteEdges` | Deletes all edges in a graph. | `graphGuid` (string) - Graph GUID <br> `cancellationToken` (optional) - `AbortController` | `Promise<void>` | `DELETE /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/all` |
| `deleteMultipleEdges` | Deletes multiple edges by GUIDs. | `graphGuid` (string) - Graph GUID <br> `edgeGuids` (Array<string>) - List of edge GUIDs <br> `cancellationToken` (optional) - `AbortController` | `Promise<void>` | `DELETE /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/multiple` |


### Route Operations

| Method | Description | Parameters | Returns | Endpoint |
|--------|-------------|------------|---------|----------|
| `getEdgesFromNode` | Retrieves edges from a given node. | `graphGuid` (string) - Graph GUID <br> `nodeGuid` (string) - Node GUID <br> `cancellationToken` (optional) - `AbortSignal` | `Promise<Edge[]>` | `GET /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/edges/from` |
| `getEdgesToNode` | Retrieves edges to a given node. | `graphGuid` (string) - Graph GUID <br> `nodeGuid` (string) - Node GUID <br> `cancellationToken` (optional) - `AbortSignal` | `Promise<Edge[]>` | `GET /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/edges/to` |
| `getEdgesBetween` | Retrieves edges from one node to another. | `graphGuid` (string) - Graph GUID <br> `fromNodeGuid` (string) - From node GUID <br> `toNodeGuid` (string) - To node GUID <br> `cancellationToken` (optional) - `AbortSignal` | `Promise<Edge[]>` | `GET /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/edges/between?from={fromNodeGuid}&to={toNodeGuid}` |
| `getAllNodeEdges` | Retrieves all edges to or from a node. | `graphGuid` (string) - Graph GUID <br> `nodeGuid` (string) - Node GUID <br> `cancellationToken` (optional) - `AbortSignal` | `Promise<Edge[]>` | `GET /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/edges` |
| `getChildrenFromNode` | Retrieves child nodes from a node. | `graphGuid` (string) - Graph GUID <br> `nodeGuid` (string) - Node GUID <br> `cancellationToken` (optional) - `AbortSignal` | `Promise<Node[]>` | `GET /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/children` |
| `getParentsFromNode` | Retrieves parent nodes from a node. | `graphGuid` (string) - Graph GUID <br> `nodeGuid` (string) - Node GUID <br> `cancellationToken` (optional) - `AbortSignal` | `Promise<Node[]>` | `GET /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/parents` |
| `getNodeNeighbors` | Retrieves neighboring nodes from a node. | `graphGuid` (string) - Graph GUID <br> `nodeGuid` (string) - Node GUID <br> `cancellationToken` (optional) - `AbortSignal` | `Promise<Node[]>` | `GET /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/{nodeGuid}/neighbors` |
| `getRoutes` | Retrieves routes between two nodes. | `graphGuid` (string) - Graph GUID <br> `fromNodeGuid` (string) - From node GUID <br> `toNodeGuid` (string) - To node GUID <br> `cancellationToken` (optional) - `AbortSignal` | `Promise<RouteResult>` | `POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/routes` |




### Batch Operations

| Method | Description | Parameters | Returns | Endpoint |
|--------|-------------|------------|---------|----------|
| `batchExistence` | Executes a batch existence request for nodes and edges. | `graphGuid` (string) - The GUID of the graph <br> `existenceRequest` (Object) - The existence request containing:<br> &nbsp;&nbsp;• `Nodes` (Array<string>) - List of node GUIDs <br> &nbsp;&nbsp;• `Edges` (Array<string>) - List of edge GUIDs <br> &nbsp;&nbsp;• `EdgesBetween` (Array<EdgeBetween>) - List of edge connections <br> `cancellationToken` (optional) - `AbortController` | `Promise<Object>` - The existence result | `POST /v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/existence` |



## Core Components

### Base Models

- `TenantMetaData`: Represents a tenant in the system
- `Graph`: Represents a graph container
- `Node`: Represents a node in a graph
- `Edge`: Represents a connection between nodes
- `RouteRequest`: Used for finding routes between nodes
- `RouteResult`: Contains route finding results
- `ExistenceRequest`: Used for checking the existence


## API Resource Operations

### Graphs

```javascript
const { LiteGraphSdk } = require('litegraphdb');

const api = new LiteGraphSdk(
    'https://api.litegraphdb.com',
    'your-tenant-guid',
    'your-access-key'
);

// Create a graph
api.createGraph('graph-guid', 'New Graph')
    .then(graph => console.log(graph))
    .catch(err => console.error(err));

// Retrieve a graph
api.readGraph('graph-guid')
    .then(graph => console.log(graph))
    .catch(err => console.error(err));

// Retrieve all graphs for tenant
api.readGraphs()
    .then(graphs => console.log(graphs))
    .catch(err => console.error(err));

// Update a graph
const graphUpdate = { Name: 'Updated Graph' };
api.updateGraph('graph-guid', graphUpdate)
    .then(graph => console.log(graph))
    .catch(err => console.error(err));

// Delete a graph
api.deleteGraph('graph-guid')
    .then(response => console.log(response))
    .catch(err => console.error(err));

// Export to GEXF
api.exportGraphToGexf('graph-guid')
    .then(gexfData => console.log(gexfData))
    .catch(err => console.error(err));

// Check if Graph Exists
api.graphExists('graph-guid')
    .then(exists => console.log(exists))
    .catch(err => console.error(err));

// Search graphs in tenant
const searchRequest = {
    Ordering: 'CreatedDescending',
    Expr: {
        Left: 'Name',
        Operator: 'Equals',
        Right: 'My Graph'
    }
};
api.searchGraphs(searchRequest)
    .then(graphResults => console.log(graphResults))
    .catch(err => console.error(err));
```

### Nodes

```javascript
// Create Multiple Nodes
const newMultipleNodes = [
    {
        Name: 'Active Directory',
        Data: { Name: 'Active Directory' }
    },
    {
        Name: 'Website',
        Data: { Name: 'Website' }
    }
];
api.createNodes('graph-guid', newMultipleNodes)
    .then(nodes => console.log(nodes))
    .catch(err => console.error(err));

// Create a single node
const nodeData = {
    Name: 'New Node',
    Data: { type: 'service' }
};
api.createNode('graph-guid', nodeData)
    .then(node => console.log(node))
    .catch(err => console.error(err));

// Retrieve a node
api.readNode('graph-guid', 'node-guid')
    .then(node => console.log(node))
    .catch(err => console.error(err));

// Retrieve all nodes in a graph
api.readNodes('graph-guid')
    .then(nodes => console.log(nodes))
    .catch(err => console.error(err));

// Update a node
const nodeUpdate = { Name: 'Updated Node' };
api.updateNode('graph-guid', 'node-guid', nodeUpdate)
    .then(node => console.log(node))
    .catch(err => console.error(err));

// Delete a node
api.deleteNode('graph-guid', 'node-guid')
    .then(response => console.log(response))
    .catch(err => console.error(err));

// Delete multiple nodes
api.deleteMultipleNodes('graph-guid', ['node-guid-1', 'node-guid-2'])
    .then(response => console.log(response))
    .catch(err => console.error(err));

// Check if Node Exists
api.nodeExists('graph-guid', 'node-guid')
    .then(exists => console.log(exists))
    .catch(err => console.error(err));

// Delete all nodes within a graph
api.deleteNodes('graph-guid')
    .then(response => console.log(response))
    .catch(err => console.error(err));

const searchReq = {
  "Ordering": "CreatedDescending",
  "Labels": [
    "test"
  ],
  "Tags": { },
  "Expr": { }
};
// Search a node
api.searchNodes('graph-guid', searchReq)
    .then(nodes => console.log(nodes))
    .catch(err => console.error(err));
```

### Edges

```javascript
// Create Multiple Edges
const newMultipleEdges = [
    {
        Name: 'Connection 1',
        From: 'node-guid-1',
        To: 'node-guid-2',
        Cost: 1
    },
    {
        Name: 'Connection 2',
        From: 'node-guid-2',
        To: 'node-guid-3',
        Cost: 1
    }
];
api.createEdges('graph-guid', newMultipleEdges)
    .then(edges => console.log(edges))
    .catch(err => console.error(err));

// Create a single edge
const edgeData = {
    Name: 'Direct Connection',
    From: 'node-guid-1',
    To: 'node-guid-2',
    Cost: 1
};
api.createEdge(edgeData)
    .then(edge => console.log(edge))
    .catch(err => console.error(err));

// Retrieve an edge
api.readEdge('graph-guid', 'edge-guid')
    .then(edge => console.log(edge))
    .catch(err => console.error(err));

// Update an edge
const edgeUpdate = { Name: 'Updated Connection', Cost: 2 };
api.updateEdge('graph-guid', 'edge-guid', edgeUpdate)
    .then(edge => console.log(edge))
    .catch(err => console.error(err));

// Delete an edge
api.deleteEdge('graph-guid', 'edge-guid')
    .then(response => console.log(response))
    .catch(err => console.error(err));

// Delete multiple edges
api.deleteMultipleEdges('graph-guid', ['edge-guid-1', 'edge-guid-2'])
    .then(response => console.log(response))
    .catch(err => console.error(err));

// Check if Edge Exists
api.edgeExists('graph-guid', 'edge-guid')
    .then(exists => console.log(exists))
    .catch(err => console.error(err));

const searchRequest = {
    GraphGUID: '01010101-0101-0101-0101-010101010101',
    Ordering: 'CreatedDescending',
    Expr: {
      Left: 'Hello',
      Operator: 'Equals',
      Right: 'World',
    },
  };
// Search edges
api.searchEdges('graph-guid', searchRequest)
    .then(edges => console.log(edges))
    .catch(err => console.error(err));

// Read all edges in a graph
api.readEdges('graph-guid')
    .then(edges => console.log(edges))
    .catch(err => console.error(err));

// Delete all edges
api.deleteEdges('graph-guid')
    .then(console.log('All edges deleted'))
    .catch(err => console.error(err));
```


### Vector Operations

```javascript

// Retrieve all vectors
api.readAllVectors()
    .then(vectors => console.log(vectors))
    .catch(err => console.error(err));

// Retrieve a specific vector
api.readVector('vector-guid')
    .then(vector => console.log(vector))
    .catch(err => console.error(err));

// Check if a vector exists
api.existsVector('vector-guid')
    .then(exists => console.log(exists))
    .catch(err => console.error(err));

// Create a new vector
const newVector = {
    GraphGUID: 'graph-guid',
    Domain: 'Test Domain',
    SearchType: 'Exact',
    Labels: ['label1', 'label2']
};
api.createVector(newVector)
    .then(vector => console.log(vector))
    .catch(err => console.error(err));

// Update a vector
const vectorUpdate = {
    Domain: 'Updated Domain',
    SearchType: 'Updated Exact',
    Labels: ['updated-label1', 'updated-label2']
};
api.updateVector( vectorUpdate,'vector-guid')
    .then(updatedVector => console.log(updatedVector))
    .catch(err => console.error(err));

// Delete a vector
api.deleteVector('vector-guid')
    .then(response => console.log(response))
    .catch(err => console.error(err));

// Search vectors
const searchRequest = {
    GraphGUID: '00000000-0000-0000-0000-000000000000',
    Domain: 'Search Domain',
    SearchType: 'Contains',
    Labels: ['search-label1', 'search-label2']
};
api.searchVectors(searchRequest)
    .then(searchResults => console.log(searchResults))
    .catch(err => console.error(err));
```

### Route and Traversal

```javascript
// Find Routes
const routeRequest = {
    Graph: 'graph-guid',
    From: 'node-guid',
    To: 'node-guid',
    NodeFilter: {
        Ordering: 'CreatedDescending',
        Expr: { Left: 'Hello', Operator: 'GreaterThan', Right: 'World' }
    }
};
api.getRoutes('graph-guid', routeRequest)
    .then(routes => console.log(routes))
    .catch(err => console.error(err));

// Get Edges From
api.getEdgesFromNode('graph-guid', 'node-guid')
    .then(edges => console.log(edges))
    .catch(err => console.error(err));

// Get Edges To
api.getEdgesToNode('graph-guid', 'node-guid')
    .then(edges => console.log(edges))
    .catch(err => console.error(err));

// Get Edges Between
api.getEdgesBetween('graph-guid', 'node-guid-1', 'node-guid-2')
    .then(edges => console.log(edges))
    .catch(err => console.error(err));

// Get All Node Edges
api.getAllNodeEdges('graph-guid', 'node-guid')
    .then(edges => console.log(edges))
    .catch(err => console.error(err));

// Get Children Node
api.getChildrenFromNode('graph-guid', 'node-guid')
    .then(nodes => console.log(nodes))
    .catch(err => console.error(err));

// Get Parent  Node
api.getParentsFromNode('graph-guid', 'node-guid')
    .then(nodes => console.log(nodes))
    .catch(err => console.error(err));

// Get Node Neighbors
api.getNodeNeighbors('graph-guid', 'node-guid')
    .then(neighbors => console.log(neighbors))
    .catch(err => console.error(err));
```

### Tenant Operations

```javascript
// Retrieve all tenants
api.readTenants()
    .then(tenants => console.log(tenants))
    .catch(err => console.error(err));

// Retrieve a specific tenant
api.readTenant('tenant-guid')
    .then(tenant => console.log(tenant))
    .catch(err => console.error(err));

// Create a new tenant
const newTenant = {
    Name: 'Another Tenant',
    Active: true
};
api.createTenant(newTenant)
    .then(tenant => console.log(tenant))
    .catch(err => console.error(err));

// Check if a tenant exists
api.tenantExists('tenant-guid')
    .then(exists => console.log(exists))
    .catch(err => console.error(err));

// Update a tenant
const tenantUpdate = {
    Name: 'Updated Tenant',
    Active: true
};
api.updateTenant('tenant-guid', tenantUpdate)
    .then(updatedTenant => console.log(updatedTenant))
    .catch(err => console.error(err));

// Delete a tenant
api.deleteTenant('tenant-guid')
    .then(response => console.log(response))
    .catch(err => console.error(err));

// Force delete a tenant
api.tenantDeleteForce('tenant-guid')
    .then(response => console.log(response))
    .catch(err => console.error(err));

```

### User Operations

```javascript
// Retrieve all users
api.readAllUsers()
    .then(users => console.log(users))
    .catch(err => console.error(err));

// Retrieve a specific user
api.readUser('user-guid')
    .then(user => console.log(user))
    .catch(err => console.error(err));

// Create a new user
const newUser = {
    FirstName: 'Another',
    LastName: 'User',
    Email: 'another@user.com',
    Password: 'password',
    Active: true
};
api.createUser(newUser)
    .then(user => console.log(user))
    .catch(err => console.error(err));

// Check if a user exists
api.existsUser('user-guid')
    .then(exists => console.log(exists))
    .catch(err => console.error(err));

// Update a user
const userUpdate = {
    FirstName: 'Again Updated',
    LastName: 'User',
    Email: 'anotherbbb@user.com',
    Password: 'password',
    Active: true
};
api.updateUser('user-guid', userUpdate)
    .then(user => console.log(user))
    .catch(err => console.error(err));

// Delete a user
api.deleteUser('user-guid')
    .then(response => console.log(response))
    .catch(err => console.error(err));

```

### Credential Operations

```javascript
// Retrieve all credentials
api.readAllCredentials()
    .then(credentials => console.log(credentials))
    .catch(err => console.error(err));

// Retrieve a specific credential
api.readCredential('credential-guid')
    .then(credential => console.log(credential))
    .catch(err => console.error(err));

// Create a new credential
const newCredential = {
    UserGUID: 'user-guid',
    Name: 'New Credential',
    BearerToken: 'foobar',
    Active: true
};
api.createCredential(newCredential)
    .then(credential => console.log(credential))
    .catch(err => console.error(err));

// Check if a credential exists
api.existsCredential('credential-guid')
    .then(exists => console.log(exists))
    .catch(err => console.error(err));

// Update a credential
const credentialUpdate = {
    UserGUID: 'user-guid',
    Name: 'Updated Credential',
    BearerToken: 'default',
    Active: true
};
api.updateCredential(credentialUpdate, 'credential-guid')
    .then(updatedCredential => console.log(updatedCredential))
    .catch(err => console.error(err));

// Delete a credential
api.deleteCredential('credential-guid')
    .then(response => console.log(response))
    .catch(err => console.error(err));

```

### Tag Operations

```javascript

// Retrieve all tags
api.readAllTags()
    .then(tags => console.log(tags))
    .catch(err => console.error(err));

// Retrieve a specific tag
api.readTag('tag-guid')
    .then(tag => console.log(tag))
    .catch(err => console.error(err));

// Check if a tag exists
api.existsTag('tag-guid')
    .then(exists => console.log(exists))
    .catch(err => console.error(err));

// Create a new tag
const newTag = {
    GraphGUID: 'graph-guid',
    NodeGUID: 'node-guid',
    EdgeGUID: 'edge-guid',
    Key: 'test-key',
    Value: 'test-value'
};
api.createTag(newTag)
    .then(tag => console.log(tag))
    .catch(err => console.error(err));

// Update a tag
const tagUpdate = {
    Key: 'updated-key',
    Value: 'updated-value'
};
api.updateTag(tagUpdate, 'tag-guid')
    .then(updatedTag => console.log(updatedTag))
    .catch(err => console.error(err));

// Delete a tag
api.deleteTag('tag-guid')
    .then(response => console.log(response))
    .catch(err => console.error(err));

```

### Label Operations

```javascript

// Retrieve all labels
api.readAllLabels()
    .then(labels => console.log(labels))
    .catch(err => console.error(err));

// Retrieve a specific label
api.readLabel('label-guid')
    .then(label => console.log(label))
    .catch(err => console.error(err));

// Check if a label exists
api.existsLabel('label-guid')
    .then(exists => console.log(exists))
    .catch(err => console.error(err));

// Create a new label
const newLabel = {
    GraphGUID: 'graph-guid',
    NodeGUID: 'node-guid',
    Label: 'test-label',
    CreatedUtc: '2025-01-16T07:23:26.752372Z',
    LastUpdateUtc: '2025-01-16T07:23:26.752417Z'
};
api.createLabel(newLabel)
    .then(label => console.log(label))
    .catch(err => console.error(err));

// Update a label
const labelUpdate = {
    Key: 'updated-label',
    Value: 'updated-value',
    CreatedUtc: '2024-12-27T18:12:38.653402Z',
    LastUpdateUtc: '2024-12-27T18:12:38.653402Z'
};
api.updateLabel(labelUpdate, 'label-guid')
    .then(updatedLabel => console.log(updatedLabel))
    .catch(err => console.error(err));

// Delete a label
api.deleteLabel('label-guid')
    .then(response => console.log(response))
    .catch(err => console.error(err));

```

### Authentication

```javascript
// Generate an authentication token
const email = 'user@example.com';
const password = 'securepassword';
const tenantId = 'tenant-guid';
api.generateToken(email, password, tenantId)
    .then(token => console.log(token))
    .catch(err => console.error(err));

// Fetch details about an authentication token
const authToken = 'token';
api.getTokenDetails(authToken)
    .then(tokenDetails => console.log(tokenDetails))
    .catch(err => console.error(err));
```
### Batch Operation

```javascript
// Execute a batch existence request
const existenceRequest = {
    "Nodes": [
        "node-guid-1",
        "node-guid-2",
        "node-guid-3"
    ],
    "Edges": [
        "edge-guid-1",
        "edge-guid-2",
        "edge-guid-3"
    ],
    "EdgesBetween": [
        { "From": "node-guid-1", "To": "node-guid-2" },
        { "From": "node-guid-3", "To": "node-guid-4" },
        { "From": "node-guid-5", "To": "node-guid-6" }
    ]
}
api.batchExistence('graph-guid',existenceRequest )
    .then(existenceResults => console.log(existenceResults))
    .catch(err => console.error(err));
```


## Development

### Linking the project locally (for development and testing)

To use the library locally without publishing to a remote npm registry, first install the dependencies with the command:

```shell
npm install
```

Next, [link](https://docs.npmjs.com/cli/link) it globally in local system's npm with the following command:

```shell
npm link
```

To use the link you just defined in your project, switch to the directory you want to use your litegraphdb from, and run:

```shell
npm link litegraphdb
```

Finally, you need to build the sdk, use command:

```shell
npm run build
```

You can then import the SDK with either import or require based on the ES Module (esm) or CommonJS (cjs) project, as shown below:

```javascript
import { LiteGraphSdk } from 'litegraphdb';
//or
var { LiteGraphSdk } = require('litegraphdb');
```


### Setting up Pre-commit Hooks

The pre-commit hooks will run automatically on `git commit`. They help maintain:

- Code formatting (using ruff)
- Import sorting
- Code quality checks
- And other project-specific checks

### Running Tests

The project uses `jest` for running tests in isolated environments. Make sure you have jest installed, which should automatically be there if you have installed dependencies via `npm i` command:

```bash
# Run only the tests
npm run test

# Run tests with coverage report
npm run test:coverage

```

### Documentation

Docs can be generated by running the following command:

```shell
npm run build:docs
```

and view the docs in the [docs/docs.md](docs/docs.md) file.



## Contributing

We welcome contributions! Please see our [Contributing Guidelines](CONTRIBUTING.md) for details

## Feedback and Issues

Have feedback or found an issue? Please file an issue in our GitHub repository.

## Version History

Please refer to [CHANGELOG.md](CHANGELOG.md) for a detailed version history.