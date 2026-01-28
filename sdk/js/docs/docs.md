## Modules

<dl>
<dt><a href="#module_LiteGraphSdk">LiteGraphSdk</a> ⇐ <code>SdkBase</code></dt>
<dd><p>LiteGraph SDK class.
Extends the SdkBase class.</p>
</dd>
<dt><a href="#module_SdkBase">SdkBase</a></dt>
<dd><p>SDK Base class for making API calls with logging and timeout functionality.</p>
</dd>
</dl>

<a name="module_LiteGraphSdk"></a>

## LiteGraphSdk ⇐ <code>SdkBase</code>
LiteGraph SDK class.
Extends the SdkBase class.

**Extends**: <code>SdkBase</code>  

* [LiteGraphSdk](#module_LiteGraphSdk) ⇐ <code>SdkBase</code>
    * [module.exports](#exp_module_LiteGraphSdk--module.exports) ⏏
        * [new module.exports(endpoint, [tenantGuid], [accessKey])](#new_module_LiteGraphSdk--module.exports_new)
        * [.graphExists(guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+graphExists) ⇒ <code>Promise.&lt;boolean&gt;</code>
        * [.createGraph(graph, [cancellationToken])](#module_LiteGraphSdk--module.exports+createGraph) ⇒ <code>Promise.&lt;Graph&gt;</code>
        * [.readGraphs([cancellationToken])](#module_LiteGraphSdk--module.exports+readGraphs) ⇒ <code>Promise.&lt;Array.&lt;Graph&gt;&gt;</code>
        * [.searchGraphs(searchReq, [cancellationToken])](#module_LiteGraphSdk--module.exports+searchGraphs) ⇒ <code>Promise.&lt;SearchResult&gt;</code>
        * [.readGraph(guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+readGraph) ⇒ <code>Promise.&lt;Graph&gt;</code>
        * [.updateGraph(graph, [cancellationToken])](#module_LiteGraphSdk--module.exports+updateGraph) ⇒ <code>Promise.&lt;Graph&gt;</code>
        * [.deleteGraph(guid, [cancellationToken], force)](#module_LiteGraphSdk--module.exports+deleteGraph)
        * [.exportGraphToGexf(guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+exportGraphToGexf) ⇒ <code>Promise.&lt;string&gt;</code>
        * [.batchExistence(graphGuid, existenceRequest, [cancellationToken])](#module_LiteGraphSdk--module.exports+batchExistence) ⇒ <code>Promise.&lt;Object&gt;</code>
        * [.nodeExists(graphGuid, guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+nodeExists) ⇒ <code>Promise.&lt;boolean&gt;</code>
        * [.createNodes(graphGuid, nodes, [cancellationToken])](#module_LiteGraphSdk--module.exports+createNodes) ⇒ <code>Promise.&lt;Array.&lt;Node&gt;&gt;</code>
        * [.createNode(node, [cancellationToken])](#module_LiteGraphSdk--module.exports+createNode) ⇒ <code>Promise.&lt;Node&gt;</code>
        * [.readNodes(graphGuid, [cancellationToken])](#module_LiteGraphSdk--module.exports+readNodes) ⇒ <code>Promise.&lt;Array.&lt;Node&gt;&gt;</code>
        * [.searchNodes(searchReq, graphGuid, [cancellationToken])](#module_LiteGraphSdk--module.exports+searchNodes) ⇒ <code>Promise.&lt;SearchResult&gt;</code>
        * [.readNode(graphGuid, nodeGuid, [cancellationToken])](#module_LiteGraphSdk--module.exports+readNode) ⇒ <code>Promise.&lt;Node&gt;</code>
        * [.updateNode(node, [cancellationToken])](#module_LiteGraphSdk--module.exports+updateNode) ⇒ <code>Promise.&lt;Node&gt;</code>
        * [.deleteNode(graphGuid, nodeGuid, [cancellationToken])](#module_LiteGraphSdk--module.exports+deleteNode)
        * [.deleteNodes(graphGuid, [cancellationToken])](#module_LiteGraphSdk--module.exports+deleteNodes)
        * [.deleteMultipleNodes(graphGuid, nodeGuids, [cancellationToken])](#module_LiteGraphSdk--module.exports+deleteMultipleNodes)
        * [.edgeExists(graphGuid, guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+edgeExists) ⇒ <code>Promise.&lt;boolean&gt;</code>
        * [.createEdges(graphGuid, edges, [cancellationToken])](#module_LiteGraphSdk--module.exports+createEdges) ⇒ <code>Promise.&lt;Array.&lt;Object&gt;&gt;</code>
        * [.createEdge(edge, [cancellationToken])](#module_LiteGraphSdk--module.exports+createEdge) ⇒ <code>Promise.&lt;Edge&gt;</code>
        * [.readEdges(graphGuid, [cancellationToken])](#module_LiteGraphSdk--module.exports+readEdges) ⇒ <code>Promise.&lt;Array.&lt;Edge&gt;&gt;</code>
        * [.searchEdges(graphGuid, searchReq, [cancellationToken])](#module_LiteGraphSdk--module.exports+searchEdges) ⇒ <code>Promise.&lt;SearchResult&gt;</code>
        * [.readEdge(graphGuid, edgeGuid, [cancellationToken])](#module_LiteGraphSdk--module.exports+readEdge) ⇒ <code>Promise.&lt;Edge&gt;</code>
        * [.updateEdge(edge, [cancellationToken])](#module_LiteGraphSdk--module.exports+updateEdge) ⇒ <code>Promise.&lt;Edge&gt;</code>
        * [.deleteEdge(graphGuid, edgeGuid, [cancellationToken])](#module_LiteGraphSdk--module.exports+deleteEdge) ⇒ <code>Promise.&lt;void&gt;</code>
        * [.deleteEdges(graphGuid, [cancellationToken])](#module_LiteGraphSdk--module.exports+deleteEdges)
        * [.deleteMultipleEdges(graphGuid, edgeGuids, [cancellationToken])](#module_LiteGraphSdk--module.exports+deleteMultipleEdges)
        * [.getEdgesFromNode(graphGuid, nodeGuid, [cancellationToken])](#module_LiteGraphSdk--module.exports+getEdgesFromNode) ⇒ <code>Promise.&lt;Array.&lt;Edge&gt;&gt;</code>
        * [.getEdgesToNode(graphGuid, nodeGuid, [cancellationToken])](#module_LiteGraphSdk--module.exports+getEdgesToNode) ⇒ <code>Promise.&lt;Array.&lt;Edge&gt;&gt;</code>
        * [.getEdgesBetween(graphGuid, fromNodeGuid, toNodeGuid, [cancellationToken])](#module_LiteGraphSdk--module.exports+getEdgesBetween) ⇒ <code>Promise.&lt;Array.&lt;Edge&gt;&gt;</code>
        * [.getAllNodeEdges(graphGuid, nodeGuid, [cancellationToken])](#module_LiteGraphSdk--module.exports+getAllNodeEdges) ⇒ <code>Promise.&lt;Array.&lt;Edge&gt;&gt;</code>
        * [.getChildrenFromNode(graphGuid, nodeGuid, [cancellationToken])](#module_LiteGraphSdk--module.exports+getChildrenFromNode) ⇒ <code>Promise.&lt;Array.&lt;Node&gt;&gt;</code>
        * [.getParentsFromNode(graphGuid, nodeGuid, [cancellationToken])](#module_LiteGraphSdk--module.exports+getParentsFromNode) ⇒ <code>Promise.&lt;Array.&lt;Node&gt;&gt;</code>
        * [.getNodeNeighbors(graphGuid, nodeGuid, [cancellationToken])](#module_LiteGraphSdk--module.exports+getNodeNeighbors) ⇒ <code>Promise.&lt;Array.&lt;Node&gt;&gt;</code>
        * [.getRoutes(graphGuid, fromNodeGuid, toNodeGuid, [cancellationToken])](#module_LiteGraphSdk--module.exports+getRoutes) ⇒ <code>Promise.&lt;RouteResult&gt;</code>
        * [.readTenants([cancellationToken])](#module_LiteGraphSdk--module.exports+readTenants) ⇒ <code>Promise.&lt;Array.&lt;TenantMetaData&gt;&gt;</code>
        * [.readTenant(tenantGuid, [cancellationToken])](#module_LiteGraphSdk--module.exports+readTenant) ⇒ <code>Promise.&lt;TenantMetaData&gt;</code>
        * [.createTenant(tenant, [cancellationToken])](#module_LiteGraphSdk--module.exports+createTenant) ⇒ <code>Promise.&lt;TenantMetaData&gt;</code>
        * [.updateTenant(tenant, guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+updateTenant) ⇒ <code>Promise.&lt;TenantMetaData&gt;</code>
        * [.deleteTenant(tenantGuid, [cancellationToken])](#module_LiteGraphSdk--module.exports+deleteTenant) ⇒ <code>Promise.&lt;Boolean&gt;</code>
        * [.tenantExists(tenantGuid, [cancellationToken])](#module_LiteGraphSdk--module.exports+tenantExists) ⇒ <code>Promise.&lt;boolean&gt;</code>
        * [.tenantDeleteForce(tenantGuid, [cancellationToken])](#module_LiteGraphSdk--module.exports+tenantDeleteForce) ⇒ <code>Promise.&lt;Boolean&gt;</code>
        * [.readAllUsers([cancellationToken])](#module_LiteGraphSdk--module.exports+readAllUsers) ⇒ <code>Promise.&lt;Array.&lt;UserMetadata&gt;&gt;</code>
        * [.readUser(userGuid, [cancellationToken])](#module_LiteGraphSdk--module.exports+readUser) ⇒ <code>Promise.&lt;UserMetadata&gt;</code>
        * [.createUser(user, [cancellationToken])](#module_LiteGraphSdk--module.exports+createUser) ⇒ <code>Promise.&lt;UserMetadata&gt;</code>
        * [.existsUser(guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+existsUser) ⇒ <code>Promise.&lt;boolean&gt;</code>
        * [.updateUser(user, guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+updateUser) ⇒ <code>Promise.&lt;UserMetadata&gt;</code>
        * [.deleteUser(guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+deleteUser) ⇒ <code>Promise.&lt;Boolean&gt;</code>
        * [.readAllCredentials([cancellationToken])](#module_LiteGraphSdk--module.exports+readAllCredentials) ⇒ <code>Promise.&lt;Array.&lt;CredentialMetadata&gt;&gt;</code>
        * [.readCredential(guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+readCredential) ⇒ <code>Promise.&lt;CredentialMetadata&gt;</code>
        * [.createCredential(credential, [cancellationToken])](#module_LiteGraphSdk--module.exports+createCredential) ⇒ <code>Promise.&lt;CredentialMetadata&gt;</code>
        * [.updateCredential(credential, guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+updateCredential) ⇒ <code>Promise.&lt;CredentialMetadata&gt;</code>
        * [.deleteCredential(guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+deleteCredential) ⇒ <code>Promise.&lt;Boolean&gt;</code>
        * [.existsCredential(guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+existsCredential) ⇒ <code>Promise.&lt;boolean&gt;</code>
        * [.readAllTags([cancellationToken])](#module_LiteGraphSdk--module.exports+readAllTags) ⇒ <code>Promise.&lt;Array.&lt;TagMetaData&gt;&gt;</code>
        * [.readTag(guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+readTag) ⇒ <code>Promise.&lt;TagMetaData&gt;</code>
        * [.existsTag(guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+existsTag) ⇒ <code>Promise.&lt;boolean&gt;</code>
        * [.createTag(tag, [cancellationToken])](#module_LiteGraphSdk--module.exports+createTag) ⇒ <code>Promise.&lt;TagMetaData&gt;</code>
        * [.updateTag(tag, guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+updateTag) ⇒ <code>Promise.&lt;TagMetaData&gt;</code>
        * [.deleteTag(guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+deleteTag) ⇒ <code>Promise.&lt;void&gt;</code>
        * [.readAllLabels([cancellationToken])](#module_LiteGraphSdk--module.exports+readAllLabels) ⇒ <code>Promise.&lt;Array.&lt;LabelMetadata&gt;&gt;</code>
        * [.readLabel(guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+readLabel) ⇒ <code>Promise.&lt;LabelMetadata&gt;</code>
        * [.existsLabel(guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+existsLabel) ⇒ <code>Promise.&lt;boolean&gt;</code>
        * [.createLabel(label, [cancellationToken])](#module_LiteGraphSdk--module.exports+createLabel) ⇒ <code>Promise.&lt;LabelMetadata&gt;</code>
        * [.updateLabel(label, guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+updateLabel) ⇒ <code>Promise.&lt;LabelMetadata&gt;</code>
        * [.deleteLabel(guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+deleteLabel) ⇒ <code>Promise.&lt;void&gt;</code>
        * [.readAllVectors([cancellationToken])](#module_LiteGraphSdk--module.exports+readAllVectors) ⇒ <code>Promise.&lt;Array.&lt;VectorMetadata&gt;&gt;</code>
        * [.readVector(guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+readVector) ⇒ <code>Promise.&lt;VectorMetadata&gt;</code>
        * [.existsVector(guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+existsVector) ⇒ <code>Promise.&lt;boolean&gt;</code>
        * [.createVector(vector, [cancellationToken])](#module_LiteGraphSdk--module.exports+createVector) ⇒ <code>Promise.&lt;VectorMetadata&gt;</code>
        * [.updateVector(vector, guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+updateVector) ⇒ <code>Promise.&lt;VectorMetadata&gt;</code>
        * [.deleteVector(guid, [cancellationToken])](#module_LiteGraphSdk--module.exports+deleteVector) ⇒ <code>Promise.&lt;void&gt;</code>
        * [.searchVectors(searchReq, [cancellationToken])](#module_LiteGraphSdk--module.exports+searchVectors) ⇒ <code>Promise.&lt;VectorSearchResult&gt;</code>
        * [.generateToken(email, tenantId, password, [cancellationToken])](#module_LiteGraphSdk--module.exports+generateToken) ⇒ <code>Promise.&lt;Token&gt;</code>
        * [.getTokenDetails(token, [cancellationToken])](#module_LiteGraphSdk--module.exports+getTokenDetails) ⇒ <code>Promise.&lt;Token&gt;</code>
        * [.getTenantsForEmail(email, [cancellationToken])](#module_LiteGraphSdk--module.exports+getTenantsForEmail) ⇒ <code>Promise.&lt;Array.&lt;TenantMetaData&gt;&gt;</code>

<a name="exp_module_LiteGraphSdk--module.exports"></a>

### module.exports ⏏
**Kind**: Exported class  
<a name="new_module_LiteGraphSdk--module.exports_new"></a>

#### new module.exports(endpoint, [tenantGuid], [accessKey])
Instantiate the SDK.


| Param | Type | Default | Description |
| --- | --- | --- | --- |
| endpoint | <code>string</code> | <code>&quot;http://localhost:8701/&quot;</code> | The endpoint URL. |
| [tenantGuid] | <code>string</code> |  | The tenant GUID. |
| [accessKey] | <code>string</code> |  | The access key. |

<a name="module_LiteGraphSdk--module.exports+graphExists"></a>

#### module.exports.graphExists(guid, [cancellationToken]) ⇒ <code>Promise.&lt;boolean&gt;</code>
Check if a graph exists by GUID.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;boolean&gt;</code> - - True if the graph exists.  

| Param | Type | Description |
| --- | --- | --- |
| guid | <code>string</code> | The GUID of the graph. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+createGraph"></a>

#### module.exports.createGraph(graph, [cancellationToken]) ⇒ <code>Promise.&lt;Graph&gt;</code>
Create a graph.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Graph&gt;</code> - - The created graph.  

| Param | Type | Description |
| --- | --- | --- |
| graph | <code>Object</code> | Information about the graph. |
| graph.GUID | <code>string</code> | Globally unique identifier (automatically generated if not provided). |
| graph.Name | <code>string</code> | Name of the graph. |
| graph.Labels | <code>Array.&lt;string&gt;</code> | Array of labels associated with the graph. |
| graph.Tags | <code>Object</code> | Key-value pairs of tags. |
| graph.Vectors | <code>Array.&lt;VectorMetadata&gt;</code> | Array of vector embeddings. |
| graph.Data | <code>Object</code> | Object data associated with the graph (default is null). |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+readGraphs"></a>

#### module.exports.readGraphs([cancellationToken]) ⇒ <code>Promise.&lt;Array.&lt;Graph&gt;&gt;</code>
Read all graphs.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Array.&lt;Graph&gt;&gt;</code> - - An array of graphs.  

| Param | Type | Description |
| --- | --- | --- |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+searchGraphs"></a>

#### module.exports.searchGraphs(searchReq, [cancellationToken]) ⇒ <code>Promise.&lt;SearchResult&gt;</code>
Search graphs.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;SearchResult&gt;</code> - - The search result.  

| Param | Type | Description |
| --- | --- | --- |
| searchReq | <code>Object</code> | Information about the search request. |
| searchReq.GraphGUID | <code>string</code> | Globally unique identifier for the graph (defaults to an empty GUID). |
| searchReq.Ordering | <code>string</code> | Ordering of the search results (default is CreatedDescending). |
| searchReq.Expr | <code>Object</code> | Expression used for the search (default is null). |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+readGraph"></a>

#### module.exports.readGraph(guid, [cancellationToken]) ⇒ <code>Promise.&lt;Graph&gt;</code>
Read a specific graph.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Graph&gt;</code> - - The requested graph.  

| Param | Type | Description |
| --- | --- | --- |
| guid | <code>string</code> | The GUID of the graph. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+updateGraph"></a>

#### module.exports.updateGraph(graph, [cancellationToken]) ⇒ <code>Promise.&lt;Graph&gt;</code>
Update a graph.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Graph&gt;</code> - - The updated graph.  

| Param | Type | Description |
| --- | --- | --- |
| graph | <code>Object</code> | Information about the graph. |
| graph.GUID | <code>string</code> | Globally unique identifier (automatically generated if not provided). |
| graph.name | <code>string</code> | Name of the graph. |
| graph.CreatedUtc | <code>Date</code> | Creation timestamp in UTC (defaults to now). |
| graph.data | <code>Object</code> | Object data associated with the graph (default is null). |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+deleteGraph"></a>

#### module.exports.deleteGraph(guid, [cancellationToken], force)
Delete a graph.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| guid | <code>string</code> | The GUID of the graph. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |
| force | <code>boolean</code> | Force recursive deletion of edges and nodes. |

<a name="module_LiteGraphSdk--module.exports+exportGraphToGexf"></a>

#### module.exports.exportGraphToGexf(guid, [cancellationToken]) ⇒ <code>Promise.&lt;string&gt;</code>
Export a graph to GEXF format.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;string&gt;</code> - - The GEXF XML data.  

| Param | Type | Description |
| --- | --- | --- |
| guid | <code>string</code> | The GUID of the graph. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+batchExistence"></a>

#### module.exports.batchExistence(graphGuid, existenceRequest, [cancellationToken]) ⇒ <code>Promise.&lt;Object&gt;</code>
Execute a batch existence request.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Object&gt;</code> - - The existence result.  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | The GUID of the graph. |
| existenceRequest | <code>Object</code> | Optional initial data for the existence request. |
| existenceRequest.Nodes | <code>Array.&lt;string&gt;</code> | Array of node GUIDs. |
| existenceRequest.Edges | <code>Array.&lt;string&gt;</code> | Array of edge GUIDs. |
| existenceRequest.EdgesBetween | <code>Array.&lt;EdgeBetween&gt;</code> | Array of EdgeBetween instances. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+nodeExists"></a>

#### module.exports.nodeExists(graphGuid, guid, [cancellationToken]) ⇒ <code>Promise.&lt;boolean&gt;</code>
Check if a node exists by GUID.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;boolean&gt;</code> - - True if the node exists.  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | The GUID of the graph. |
| guid | <code>string</code> | The GUID of the node. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+createNodes"></a>

#### module.exports.createNodes(graphGuid, nodes, [cancellationToken]) ⇒ <code>Promise.&lt;Array.&lt;Node&gt;&gt;</code>
Create multiple nodes.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Array.&lt;Node&gt;&gt;</code> - - The list of created nodes.  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | The GUID of the graph. |
| nodes | <code>Array.&lt;Object&gt;</code> | List of node objects. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+createNode"></a>

#### module.exports.createNode(node, [cancellationToken]) ⇒ <code>Promise.&lt;Node&gt;</code>
Create a node.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Node&gt;</code> - - The created node.  

| Param | Type | Description |
| --- | --- | --- |
| node | <code>Object</code> | Information about the node. |
| node.GUID | <code>string</code> | Globally unique identifier (automatically generated if not provided). |
| node.GraphGUID | <code>string</code> | Globally unique identifier for the graph (automatically generated if not provided). |
| node.name | <code>string</code> | Name of the node. |
| node.data | <code>Object</code> | Object data associated with the node (default is null). |
| node.CreatedUtc | <code>Date</code> | Creation timestamp in UTC (defaults to now). |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+readNodes"></a>

#### module.exports.readNodes(graphGuid, [cancellationToken]) ⇒ <code>Promise.&lt;Array.&lt;Node&gt;&gt;</code>
Read nodes for a specific graph.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Array.&lt;Node&gt;&gt;</code> - - An array of nodes.  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | The GUID of the graph. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+searchNodes"></a>

#### module.exports.searchNodes(searchReq, graphGuid, [cancellationToken]) ⇒ <code>Promise.&lt;SearchResult&gt;</code>
Search nodes.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;SearchResult&gt;</code> - - The search result.  

| Param | Type | Description |
| --- | --- | --- |
| searchReq | <code>Object</code> | Information about the search request. |
| searchReq.GraphGUID | <code>string</code> | Globally unique identifier for the graph (defaults to an empty GUID). |
| searchReq.Ordering | <code>string</code> | Ordering of the search results (default is CreatedDescending). |
| searchReq.Expr | <code>Object</code> | Expression used for the search (default is null). |
| graphGuid | <code>string</code> | The GUID of the graph. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+readNode"></a>

#### module.exports.readNode(graphGuid, nodeGuid, [cancellationToken]) ⇒ <code>Promise.&lt;Node&gt;</code>
Read a specific node.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Node&gt;</code> - - The requested node.  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | The GUID of the graph. |
| nodeGuid | <code>string</code> | The GUID of the node. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+updateNode"></a>

#### module.exports.updateNode(node, [cancellationToken]) ⇒ <code>Promise.&lt;Node&gt;</code>
Update a node.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Node&gt;</code> - - The updated node.  

| Param | Type | Description |
| --- | --- | --- |
| node | <code>Object</code> | Information about the node. |
| node.GUID | <code>string</code> | Globally unique identifier (automatically generated if not provided). |
| node.GraphGUID | <code>string</code> | Globally unique identifier for the graph (automatically generated if not provided). |
| node.name | <code>string</code> | Name of the node. |
| node.data | <code>Object</code> | Object data associated with the node (default is null). |
| node.CreatedUtc | <code>Date</code> | Creation timestamp in UTC (defaults to now). |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+deleteNode"></a>

#### module.exports.deleteNode(graphGuid, nodeGuid, [cancellationToken])
Delete a node.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | The GUID of the graph. |
| nodeGuid | <code>string</code> | The GUID of the node. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+deleteNodes"></a>

#### module.exports.deleteNodes(graphGuid, [cancellationToken])
Delete all nodes within a graph.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | The GUID of the graph. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+deleteMultipleNodes"></a>

#### module.exports.deleteMultipleNodes(graphGuid, nodeGuids, [cancellationToken])
Delete multiple nodes within a graph.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | The GUID of the graph. |
| nodeGuids | <code>Array.&lt;string&gt;</code> | The list of node GUIDs to delete. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+edgeExists"></a>

#### module.exports.edgeExists(graphGuid, guid, [cancellationToken]) ⇒ <code>Promise.&lt;boolean&gt;</code>
Check if an edge exists by GUID.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;boolean&gt;</code> - - True if exists.  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | Graph GUID. |
| guid | <code>string</code> | Edge GUID. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+createEdges"></a>

#### module.exports.createEdges(graphGuid, edges, [cancellationToken]) ⇒ <code>Promise.&lt;Array.&lt;Object&gt;&gt;</code>
Create multiple edges.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Array.&lt;Object&gt;&gt;</code> - - The list of created edges.  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | The GUID of the graph. |
| edges | <code>Array.&lt;Object&gt;</code> | List of edge objects. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+createEdge"></a>

#### module.exports.createEdge(edge, [cancellationToken]) ⇒ <code>Promise.&lt;Edge&gt;</code>
Create an edge.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Edge&gt;</code> - - The created edge.  

| Param | Type | Default | Description |
| --- | --- | --- | --- |
| edge | <code>Object</code> |  | Information about the edge. |
| [edge.GUID] | <code>string</code> |  | Globally unique identifier for the edge (automatically generated if not provided). |
| [edge.GraphGUID] | <code>string</code> |  | Globally unique identifier for the graph (automatically generated if not provided). |
| [edge.Name] | <code>string</code> |  | Name of the edge. |
| [edge.From] | <code>string</code> |  | Globally unique identifier of the from node. |
| [edge.To] | <code>string</code> |  | Globally unique identifier of the to node. |
| [edge.Cost] | <code>number</code> | <code>0</code> | Cost associated with the edge (default is 0). |
| [edge.CreatedUtc] | <code>Date</code> |  | Creation timestamp in UTC (defaults to current UTC time). |
| [edge.Data] | <code>Object</code> |  | Additional object data associated with the edge (default is null). |
| [cancellationToken] | <code>AbortController</code> |  | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+readEdges"></a>

#### module.exports.readEdges(graphGuid, [cancellationToken]) ⇒ <code>Promise.&lt;Array.&lt;Edge&gt;&gt;</code>
Read edges.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Array.&lt;Edge&gt;&gt;</code> - - List of edges.  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | Graph GUID. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+searchEdges"></a>

#### module.exports.searchEdges(graphGuid, searchReq, [cancellationToken]) ⇒ <code>Promise.&lt;SearchResult&gt;</code>
Search edges.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;SearchResult&gt;</code> - - The search result.  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | Graph GUID. |
| searchReq | <code>Object</code> | Information about the search request. |
| searchReq.GraphGUID | <code>string</code> | Globally unique identifier for the graph (defaults to an empty GUID). |
| searchReq.Ordering | <code>string</code> | Ordering of the search results (default is CreatedDescending). |
| searchReq.Expr | <code>Object</code> | Expression used for the search (default is null). |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+readEdge"></a>

#### module.exports.readEdge(graphGuid, edgeGuid, [cancellationToken]) ⇒ <code>Promise.&lt;Edge&gt;</code>
Read an edge.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Edge&gt;</code> - - The requested edge.  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | Graph GUID. |
| edgeGuid | <code>string</code> | Edge GUID. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+updateEdge"></a>

#### module.exports.updateEdge(edge, [cancellationToken]) ⇒ <code>Promise.&lt;Edge&gt;</code>
Update an edge.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Edge&gt;</code> - - The updated edge.  

| Param | Type | Default | Description |
| --- | --- | --- | --- |
| edge | <code>Object</code> |  | Information about the edge. |
| [edge.GUID] | <code>string</code> |  | Globally unique identifier for the edge (automatically generated if not provided). |
| [edge.GraphGUID] | <code>string</code> |  | Globally unique identifier for the graph (automatically generated if not provided). |
| [edge.Name] | <code>string</code> |  | Name of the edge. |
| [edge.From] | <code>string</code> |  | Globally unique identifier of the from node. |
| [edge.To] | <code>string</code> |  | Globally unique identifier of the to node. |
| [edge.Cost] | <code>number</code> | <code>0</code> | Cost associated with the edge (default is 0). |
| [edge.CreatedUtc] | <code>Date</code> |  | Creation timestamp in UTC (defaults to current UTC time). |
| [edge.Data] | <code>Object</code> |  | Additional object data associated with the edge (default is null). |
| [cancellationToken] | <code>AbortController</code> |  | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+deleteEdge"></a>

#### module.exports.deleteEdge(graphGuid, edgeGuid, [cancellationToken]) ⇒ <code>Promise.&lt;void&gt;</code>
Delete an edge.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;void&gt;</code> - - Promise representing the completion of the deletion.  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | Graph GUID. |
| edgeGuid | <code>string</code> | Edge GUID. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+deleteEdges"></a>

#### module.exports.deleteEdges(graphGuid, [cancellationToken])
Delete all edges within a graph.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | The GUID of the graph. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+deleteMultipleEdges"></a>

#### module.exports.deleteMultipleEdges(graphGuid, edgeGuids, [cancellationToken])
Delete multiple edges within a graph.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | The GUID of the graph. |
| edgeGuids | <code>Array.&lt;string&gt;</code> | The list of edge GUIDs to delete. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+getEdgesFromNode"></a>

#### module.exports.getEdgesFromNode(graphGuid, nodeGuid, [cancellationToken]) ⇒ <code>Promise.&lt;Array.&lt;Edge&gt;&gt;</code>
Get edges from a node.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Array.&lt;Edge&gt;&gt;</code> - - Edges.  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | Graph GUID. |
| nodeGuid | <code>string</code> | Node GUID. |
| [cancellationToken] | <code>AbortSignal</code> | Abort signal for cancellation. |

<a name="module_LiteGraphSdk--module.exports+getEdgesToNode"></a>

#### module.exports.getEdgesToNode(graphGuid, nodeGuid, [cancellationToken]) ⇒ <code>Promise.&lt;Array.&lt;Edge&gt;&gt;</code>
Get edges to a node.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Array.&lt;Edge&gt;&gt;</code> - - Edges.  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | Graph GUID. |
| nodeGuid | <code>string</code> | Node GUID. |
| [cancellationToken] | <code>AbortSignal</code> | Abort signal for cancellation. |

<a name="module_LiteGraphSdk--module.exports+getEdgesBetween"></a>

#### module.exports.getEdgesBetween(graphGuid, fromNodeGuid, toNodeGuid, [cancellationToken]) ⇒ <code>Promise.&lt;Array.&lt;Edge&gt;&gt;</code>
Get edges from a given node to a given node.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Array.&lt;Edge&gt;&gt;</code> - - Edges.  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | Graph GUID. |
| fromNodeGuid | <code>string</code> | From node GUID. |
| toNodeGuid | <code>string</code> | To node GUID. |
| [cancellationToken] | <code>AbortSignal</code> | Abort signal for cancellation. |

<a name="module_LiteGraphSdk--module.exports+getAllNodeEdges"></a>

#### module.exports.getAllNodeEdges(graphGuid, nodeGuid, [cancellationToken]) ⇒ <code>Promise.&lt;Array.&lt;Edge&gt;&gt;</code>
Get all edges to or from a node.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Array.&lt;Edge&gt;&gt;</code> - - Edges.  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | Graph GUID. |
| nodeGuid | <code>string</code> | Node GUID. |
| [cancellationToken] | <code>AbortSignal</code> | Abort signal for cancellation. |

<a name="module_LiteGraphSdk--module.exports+getChildrenFromNode"></a>

#### module.exports.getChildrenFromNode(graphGuid, nodeGuid, [cancellationToken]) ⇒ <code>Promise.&lt;Array.&lt;Node&gt;&gt;</code>
Get child nodes from a node.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Array.&lt;Node&gt;&gt;</code> - - Child nodes.  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | Graph GUID. |
| nodeGuid | <code>string</code> | Node GUID. |
| [cancellationToken] | <code>AbortSignal</code> | Abort signal for cancellation. |

<a name="module_LiteGraphSdk--module.exports+getParentsFromNode"></a>

#### module.exports.getParentsFromNode(graphGuid, nodeGuid, [cancellationToken]) ⇒ <code>Promise.&lt;Array.&lt;Node&gt;&gt;</code>
Get parent nodes from a node.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Array.&lt;Node&gt;&gt;</code> - - Parent nodes.  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | Graph GUID. |
| nodeGuid | <code>string</code> | Node GUID. |
| [cancellationToken] | <code>AbortSignal</code> | Abort signal for cancellation. |

<a name="module_LiteGraphSdk--module.exports+getNodeNeighbors"></a>

#### module.exports.getNodeNeighbors(graphGuid, nodeGuid, [cancellationToken]) ⇒ <code>Promise.&lt;Array.&lt;Node&gt;&gt;</code>
Get neighboring nodes from a node.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Array.&lt;Node&gt;&gt;</code> - - Neighboring nodes.  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | Graph GUID. |
| nodeGuid | <code>string</code> | Node GUID. |
| [cancellationToken] | <code>AbortSignal</code> | Abort signal for cancellation. |

<a name="module_LiteGraphSdk--module.exports+getRoutes"></a>

#### module.exports.getRoutes(graphGuid, fromNodeGuid, toNodeGuid, [cancellationToken]) ⇒ <code>Promise.&lt;RouteResult&gt;</code>
Get routes between two nodes.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;RouteResult&gt;</code> - - Routes.  

| Param | Type | Description |
| --- | --- | --- |
| graphGuid | <code>string</code> | Graph GUID. |
| fromNodeGuid | <code>string</code> | From node GUID. |
| toNodeGuid | <code>string</code> | To node GUID. |
| [cancellationToken] | <code>AbortSignal</code> | Abort signal for cancellation. |

<a name="module_LiteGraphSdk--module.exports+readTenants"></a>

#### module.exports.readTenants([cancellationToken]) ⇒ <code>Promise.&lt;Array.&lt;TenantMetaData&gt;&gt;</code>
Read all tenants.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Array.&lt;TenantMetaData&gt;&gt;</code> - - An array of tenants.  

| Param | Type | Description |
| --- | --- | --- |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+readTenant"></a>

#### module.exports.readTenant(tenantGuid, [cancellationToken]) ⇒ <code>Promise.&lt;TenantMetaData&gt;</code>
Read a tenant.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;TenantMetaData&gt;</code> - - The tenant.  

| Param | Type | Description |
| --- | --- | --- |
| tenantGuid | <code>string</code> | The GUID of the tenant. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+createTenant"></a>

#### module.exports.createTenant(tenant, [cancellationToken]) ⇒ <code>Promise.&lt;TenantMetaData&gt;</code>
Create a tenant.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;TenantMetaData&gt;</code> - - The created tenant.  

| Param | Type | Description |
| --- | --- | --- |
| tenant | <code>TenantMetaData</code> | The tenant to create. |
| tenant.name | <code>String</code> | The name of the tenant. |
| tenant.Active | <code>boolean</code> | Indicates if tenant is active. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+updateTenant"></a>

#### module.exports.updateTenant(tenant, guid, [cancellationToken]) ⇒ <code>Promise.&lt;TenantMetaData&gt;</code>
Update a tenant.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;TenantMetaData&gt;</code> - - The updated tenant.  

| Param | Type | Description |
| --- | --- | --- |
| tenant | <code>TenantMetaData</code> | The tenant to update. |
| tenant.name | <code>String</code> | The name of the tenant. |
| tenant.Active | <code>boolean</code> | Indicates if tenant is active. |
| guid | <code>string</code> | The GUID of the tenant. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+deleteTenant"></a>

#### module.exports.deleteTenant(tenantGuid, [cancellationToken]) ⇒ <code>Promise.&lt;Boolean&gt;</code>
Delete a tenant.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| tenantGuid | <code>string</code> | The GUID of the tenant. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+tenantExists"></a>

#### module.exports.tenantExists(tenantGuid, [cancellationToken]) ⇒ <code>Promise.&lt;boolean&gt;</code>
Tenant exists.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| tenantGuid | <code>string</code> | The GUID of the tenant. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+tenantDeleteForce"></a>

#### module.exports.tenantDeleteForce(tenantGuid, [cancellationToken]) ⇒ <code>Promise.&lt;Boolean&gt;</code>
Tenant delete force.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| tenantGuid | <code>string</code> | The GUID of the tenant. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+readAllUsers"></a>

#### module.exports.readAllUsers([cancellationToken]) ⇒ <code>Promise.&lt;Array.&lt;UserMetadata&gt;&gt;</code>
Read all users.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Array.&lt;UserMetadata&gt;&gt;</code> - - An array of users.  

| Param | Type | Description |
| --- | --- | --- |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+readUser"></a>

#### module.exports.readUser(userGuid, [cancellationToken]) ⇒ <code>Promise.&lt;UserMetadata&gt;</code>
Read a user.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;UserMetadata&gt;</code> - - The user.  

| Param | Type | Description |
| --- | --- | --- |
| userGuid | <code>string</code> | The GUID of the user. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+createUser"></a>

#### module.exports.createUser(user, [cancellationToken]) ⇒ <code>Promise.&lt;UserMetadata&gt;</code>
Create a user.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;UserMetadata&gt;</code> - - The created user.  

| Param | Type | Description |
| --- | --- | --- |
| user | <code>UserMetadata</code> | The user to create. |
| user.FirstName | <code>String</code> | The first name of the user. |
| user.LastName | <code>String</code> | The last name of the user. |
| user.Active | <code>boolean</code> | Indicates if user is active. |
| user.Email | <code>string</code> | The email of the user. |
| user.Password | <code>string</code> | The password of the user. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+existsUser"></a>

#### module.exports.existsUser(guid, [cancellationToken]) ⇒ <code>Promise.&lt;boolean&gt;</code>
User exists.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| guid | <code>string</code> | The GUID of the user. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+updateUser"></a>

#### module.exports.updateUser(user, guid, [cancellationToken]) ⇒ <code>Promise.&lt;UserMetadata&gt;</code>
Update a user.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;UserMetadata&gt;</code> - - The updated user.  

| Param | Type | Description |
| --- | --- | --- |
| user | <code>UserMetadata</code> | The user to update. |
| user.FirstName | <code>String</code> | The first name of the user. |
| user.LastName | <code>String</code> | The last name of the user. |
| user.Active | <code>boolean</code> | Indicates if user is active. |
| user.Email | <code>string</code> | The email of the user. |
| user.Password | <code>string</code> | The password of the user. |
| guid | <code>string</code> | The GUID of the user. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+deleteUser"></a>

#### module.exports.deleteUser(guid, [cancellationToken]) ⇒ <code>Promise.&lt;Boolean&gt;</code>
Delete a user.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| guid | <code>string</code> | The GUID of the user. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+readAllCredentials"></a>

#### module.exports.readAllCredentials([cancellationToken]) ⇒ <code>Promise.&lt;Array.&lt;CredentialMetadata&gt;&gt;</code>
Read all credentials.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Array.&lt;CredentialMetadata&gt;&gt;</code> - - An array of credentials.  

| Param | Type | Description |
| --- | --- | --- |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+readCredential"></a>

#### module.exports.readCredential(guid, [cancellationToken]) ⇒ <code>Promise.&lt;CredentialMetadata&gt;</code>
Read a credential.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;CredentialMetadata&gt;</code> - - The credential.  

| Param | Type | Description |
| --- | --- | --- |
| guid | <code>string</code> | The GUID of the credential. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+createCredential"></a>

#### module.exports.createCredential(credential, [cancellationToken]) ⇒ <code>Promise.&lt;CredentialMetadata&gt;</code>
Create a credential.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;CredentialMetadata&gt;</code> - - The created credential.  

| Param | Type | Description |
| --- | --- | --- |
| credential | <code>CredentialMetadata</code> | The credential to create. |
| credential.Name | <code>string</code> | The name of the credential. |
| credential.BearerToken | <code>string</code> | The bearer token of the credential. |
| credential.Active | <code>boolean</code> | Indicates if credential is active. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+updateCredential"></a>

#### module.exports.updateCredential(credential, guid, [cancellationToken]) ⇒ <code>Promise.&lt;CredentialMetadata&gt;</code>
Update a credential.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;CredentialMetadata&gt;</code> - - The updated credential.  

| Param | Type | Description |
| --- | --- | --- |
| credential | <code>CredentialMetadata</code> | The credential to update. |
| credential.Name | <code>string</code> | The name of the credential. |
| credential.BearerToken | <code>string</code> | The bearer token of the credential. |
| credential.Active | <code>boolean</code> | Indicates if credential is active. |
| guid | <code>string</code> | The GUID of the credential. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+deleteCredential"></a>

#### module.exports.deleteCredential(guid, [cancellationToken]) ⇒ <code>Promise.&lt;Boolean&gt;</code>
Delete a credential.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| guid | <code>string</code> | The GUID of the credential. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+existsCredential"></a>

#### module.exports.existsCredential(guid, [cancellationToken]) ⇒ <code>Promise.&lt;boolean&gt;</code>
Credential exists.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| guid | <code>string</code> | The GUID of the credential. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+readAllTags"></a>

#### module.exports.readAllTags([cancellationToken]) ⇒ <code>Promise.&lt;Array.&lt;TagMetaData&gt;&gt;</code>
Read all tags.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+readTag"></a>

#### module.exports.readTag(guid, [cancellationToken]) ⇒ <code>Promise.&lt;TagMetaData&gt;</code>
Read a tag.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| guid | <code>string</code> | The GUID of the tag. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+existsTag"></a>

#### module.exports.existsTag(guid, [cancellationToken]) ⇒ <code>Promise.&lt;boolean&gt;</code>
Tag exists.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| guid | <code>string</code> | The GUID of the tag. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+createTag"></a>

#### module.exports.createTag(tag, [cancellationToken]) ⇒ <code>Promise.&lt;TagMetaData&gt;</code>
Create a tag.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| tag | <code>TagMetaData</code> | The tag to create. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+updateTag"></a>

#### module.exports.updateTag(tag, guid, [cancellationToken]) ⇒ <code>Promise.&lt;TagMetaData&gt;</code>
Update a tag.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| tag | <code>TagMetaData</code> | The tag to update. |
| guid | <code>string</code> | The GUID of the tag. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+deleteTag"></a>

#### module.exports.deleteTag(guid, [cancellationToken]) ⇒ <code>Promise.&lt;void&gt;</code>
Delete a tag.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| guid | <code>string</code> | The GUID of the tag. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+readAllLabels"></a>

#### module.exports.readAllLabels([cancellationToken]) ⇒ <code>Promise.&lt;Array.&lt;LabelMetadata&gt;&gt;</code>
Read all labels.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+readLabel"></a>

#### module.exports.readLabel(guid, [cancellationToken]) ⇒ <code>Promise.&lt;LabelMetadata&gt;</code>
Read a label.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| guid | <code>string</code> | The GUID of the label. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+existsLabel"></a>

#### module.exports.existsLabel(guid, [cancellationToken]) ⇒ <code>Promise.&lt;boolean&gt;</code>
Label exists.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| guid | <code>string</code> | The GUID of the label. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+createLabel"></a>

#### module.exports.createLabel(label, [cancellationToken]) ⇒ <code>Promise.&lt;LabelMetadata&gt;</code>
Create a label.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| label | <code>LabelMetadata</code> | The label to create. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+updateLabel"></a>

#### module.exports.updateLabel(label, guid, [cancellationToken]) ⇒ <code>Promise.&lt;LabelMetadata&gt;</code>
Update a label.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| label | <code>LabelMetadata</code> | The label to update. |
| guid | <code>string</code> | The GUID of the label. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+deleteLabel"></a>

#### module.exports.deleteLabel(guid, [cancellationToken]) ⇒ <code>Promise.&lt;void&gt;</code>
Delete a label.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| guid | <code>string</code> | The GUID of the label. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+readAllVectors"></a>

#### module.exports.readAllVectors([cancellationToken]) ⇒ <code>Promise.&lt;Array.&lt;VectorMetadata&gt;&gt;</code>
Read all vectors.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+readVector"></a>

#### module.exports.readVector(guid, [cancellationToken]) ⇒ <code>Promise.&lt;VectorMetadata&gt;</code>
Read a vector.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| guid | <code>string</code> | The GUID of the vector. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+existsVector"></a>

#### module.exports.existsVector(guid, [cancellationToken]) ⇒ <code>Promise.&lt;boolean&gt;</code>
Vector exists.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| guid | <code>string</code> | The GUID of the vector. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+createVector"></a>

#### module.exports.createVector(vector, [cancellationToken]) ⇒ <code>Promise.&lt;VectorMetadata&gt;</code>
Create a vector.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| vector | <code>VectorMetadata</code> | The vector to create. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+updateVector"></a>

#### module.exports.updateVector(vector, guid, [cancellationToken]) ⇒ <code>Promise.&lt;VectorMetadata&gt;</code>
Update a vector.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| vector | <code>VectorMetadata</code> | The vector to update. |
| guid | <code>string</code> | The GUID of the vector. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+deleteVector"></a>

#### module.exports.deleteVector(guid, [cancellationToken]) ⇒ <code>Promise.&lt;void&gt;</code>
Delete a vector.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| guid | <code>string</code> | The GUID of the vector. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+searchVectors"></a>

#### module.exports.searchVectors(searchReq, [cancellationToken]) ⇒ <code>Promise.&lt;VectorSearchResult&gt;</code>
Search Vectors.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;VectorSearchResult&gt;</code> - - The search result.  

| Param | Type | Description |
| --- | --- | --- |
| searchReq | <code>Object</code> | Information about the search request. |
| searchReq.GraphGUID | <code>string</code> | Globally unique identifier for the graph (defaults to an empty GUID). |
| searchReq.Domain | <code>string</code> | Ordering of the search results (default is CreatedDescending). |
| searchReq.SearchType | <code>String</code> | Expression used for the search (default is null). |
| searchReq.Labels | <code>Array.&lt;string&gt;</code> | The domain of the search type. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+generateToken"></a>

#### module.exports.generateToken(email, tenantId, password, [cancellationToken]) ⇒ <code>Promise.&lt;Token&gt;</code>
Generate an authentication token.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Token&gt;</code> - The generated authentication token  

| Param | Type | Description |
| --- | --- | --- |
| email | <code>string</code> | The user's email address. |
| tenantId | <code>string</code> | The tenant ID. |
| password | <code>string</code> | The user's password. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+getTokenDetails"></a>

#### module.exports.getTokenDetails(token, [cancellationToken]) ⇒ <code>Promise.&lt;Token&gt;</code>
Fetch details about an authentication token.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Token&gt;</code> - The token details  

| Param | Type | Description |
| --- | --- | --- |
| token | <code>string</code> | The authentication token to inspect. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_LiteGraphSdk--module.exports+getTenantsForEmail"></a>

#### module.exports.getTenantsForEmail(email, [cancellationToken]) ⇒ <code>Promise.&lt;Array.&lt;TenantMetaData&gt;&gt;</code>
Get tenants associated with an email address.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_LiteGraphSdk--module.exports)  
**Returns**: <code>Promise.&lt;Array.&lt;TenantMetaData&gt;&gt;</code> - Array of tenants associated with the email  

| Param | Type | Description |
| --- | --- | --- |
| email | <code>string</code> | The email address to lookup tenants for. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_SdkBase"></a>

## SdkBase
SDK Base class for making API calls with logging and timeout functionality.


* [SdkBase](#module_SdkBase)
    * [module.exports](#exp_module_SdkBase--module.exports) ⏏
        * [new module.exports(endpoint, [tenantGuid], [accessKey])](#new_module_SdkBase--module.exports_new)
        * [.tenantGuid](#module_SdkBase--module.exports+tenantGuid) ⇒ <code>string</code>
        * [.tenantGuid](#module_SdkBase--module.exports+tenantGuid)
        * [.accessKey](#module_SdkBase--module.exports+accessKey) ⇒ <code>string</code>
        * [.accessKey](#module_SdkBase--module.exports+accessKey)
        * [.accessToken](#module_SdkBase--module.exports+accessToken) ⇒ <code>string</code>
        * [.accessToken](#module_SdkBase--module.exports+accessToken)
        * [.header](#module_SdkBase--module.exports+header) ⇒ <code>string</code>
        * [.header](#module_SdkBase--module.exports+header)
        * [.endpoint](#module_SdkBase--module.exports+endpoint) ⇒ <code>string</code>
        * [.endpoint](#module_SdkBase--module.exports+endpoint)
        * [.timeoutMs](#module_SdkBase--module.exports+timeoutMs) ⇒ <code>number</code>
        * [.timeoutMs](#module_SdkBase--module.exports+timeoutMs)
        * [.log(sev, msg)](#module_SdkBase--module.exports+log)
        * [.validateConnectivity([cancellationToken])](#module_SdkBase--module.exports+validateConnectivity) ⇒ <code>Promise.&lt;boolean&gt;</code>
        * [.putCreate(url, obj, model, [cancellationToken])](#module_SdkBase--module.exports+putCreate) ⇒ <code>Promise.&lt;Object&gt;</code>
        * [.head(url, [cancellationToken])](#module_SdkBase--module.exports+head) ⇒ <code>Promise.&lt;boolean&gt;</code>
        * [.get(url, model, [cancellationToken], [headers])](#module_SdkBase--module.exports+get) ⇒ <code>Promise.&lt;Object&gt;</code>
        * [.getDataInBytes(url, [cancellationToken])](#module_SdkBase--module.exports+getDataInBytes) ⇒ <code>Promise.&lt;Object&gt;</code>
        * [.getMany(url, model, [cancellationToken], [headers])](#module_SdkBase--module.exports+getMany) ⇒ <code>Promise.&lt;Array&gt;</code>
        * [.putUpdate(url, obj, model, [cancellationToken])](#module_SdkBase--module.exports+putUpdate) ⇒ <code>Promise.&lt;Object&gt;</code>
        * [.delete(url, [cancellationToken])](#module_SdkBase--module.exports+delete) ⇒ <code>Promise.&lt;void&gt;</code>
        * [.post(url, data, model, [cancellationToken])](#module_SdkBase--module.exports+post) ⇒ <code>Promise.&lt;Object&gt;</code>
        * [.deleteMany(url, obj, [cancellationToken])](#module_SdkBase--module.exports+deleteMany) ⇒ <code>Promise.&lt;void&gt;</code>
        * [.postBatch(url, obj, model, [cancellationToken])](#module_SdkBase--module.exports+postBatch) ⇒ <code>Promise.&lt;(Object\|null)&gt;</code>

<a name="exp_module_SdkBase--module.exports"></a>

### module.exports ⏏
**Kind**: Exported class  
<a name="new_module_SdkBase--module.exports_new"></a>

#### new module.exports(endpoint, [tenantGuid], [accessKey])
Creates an instance of SdkBase.

**Throws**:

- <code>Error</code> Throws an error if the endpoint is null or empty.


| Param | Type | Description |
| --- | --- | --- |
| endpoint | <code>string</code> | The API endpoint base URL. |
| [tenantGuid] | <code>string</code> | The tenant GUID. |
| [accessKey] | <code>string</code> | The access key. |

<a name="module_SdkBase--module.exports+tenantGuid"></a>

#### module.exports.tenantGuid ⇒ <code>string</code>
Getter for the tenant GUID.

**Kind**: instance property of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  
**Returns**: <code>string</code> - The tenant GUID.  
<a name="module_SdkBase--module.exports+tenantGuid"></a>

#### module.exports.tenantGuid
Setter for the tenant GUID.

**Kind**: instance property of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  
**Throws**:

- <code>Error</code> Throws an error if the tenant GUID is null or empty.


| Param | Type | Description |
| --- | --- | --- |
| value | <code>string</code> | The tenant GUID. |

<a name="module_SdkBase--module.exports+accessKey"></a>

#### module.exports.accessKey ⇒ <code>string</code>
Getter for the access key.

**Kind**: instance property of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  
**Returns**: <code>string</code> - The access key.  
<a name="module_SdkBase--module.exports+accessKey"></a>

#### module.exports.accessKey
Setter for the access key.

**Kind**: instance property of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  
**Throws**:

- <code>Error</code> Throws an error if the access key is null or empty.


| Param | Type | Description |
| --- | --- | --- |
| value | <code>string</code> | The access key. |

<a name="module_SdkBase--module.exports+accessToken"></a>

#### module.exports.accessToken ⇒ <code>string</code>
Getter for the access token.

**Kind**: instance property of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  
**Returns**: <code>string</code> - The access token.  
<a name="module_SdkBase--module.exports+accessToken"></a>

#### module.exports.accessToken
Setter for the access token.

**Kind**: instance property of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  
**Throws**:

- <code>Error</code> Throws an error if the access token is null or empty.


| Param | Type | Description |
| --- | --- | --- |
| value | <code>string</code> | The access token. |

<a name="module_SdkBase--module.exports+header"></a>

#### module.exports.header ⇒ <code>string</code>
Getter for the request header prefix.

**Kind**: instance property of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  
**Returns**: <code>string</code> - The header prefix.  
<a name="module_SdkBase--module.exports+header"></a>

#### module.exports.header
Setter for the request header prefix.

**Kind**: instance property of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| value | <code>string</code> | The header prefix. |

<a name="module_SdkBase--module.exports+endpoint"></a>

#### module.exports.endpoint ⇒ <code>string</code>
Getter for the API endpoint.

**Kind**: instance property of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  
**Returns**: <code>string</code> - The endpoint URL.  
<a name="module_SdkBase--module.exports+endpoint"></a>

#### module.exports.endpoint
Setter for the API endpoint.

**Kind**: instance property of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  
**Throws**:

- <code>Error</code> Throws an error if the endpoint is null or empty.


| Param | Type | Description |
| --- | --- | --- |
| value | <code>string</code> | The endpoint URL. |

<a name="module_SdkBase--module.exports+timeoutMs"></a>

#### module.exports.timeoutMs ⇒ <code>number</code>
Getter for the timeout in milliseconds.

**Kind**: instance property of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  
**Returns**: <code>number</code> - The timeout in milliseconds.  
<a name="module_SdkBase--module.exports+timeoutMs"></a>

#### module.exports.timeoutMs
Setter for the timeout in milliseconds.

**Kind**: instance property of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  
**Throws**:

- <code>Error</code> Throws an error if the timeout is less than 1.


| Param | Type | Description |
| --- | --- | --- |
| value | <code>number</code> | Timeout value in milliseconds. |

<a name="module_SdkBase--module.exports+log"></a>

#### module.exports.log(sev, msg)
Logs a message with a severity level.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  

| Param | Type | Description |
| --- | --- | --- |
| sev | <code>string</code> | The severity level (e.g., SeverityEnum.Debug, 'warn'). |
| msg | <code>string</code> | The message to log. |

<a name="module_SdkBase--module.exports+validateConnectivity"></a>

#### module.exports.validateConnectivity([cancellationToken]) ⇒ <code>Promise.&lt;boolean&gt;</code>
Validates API connectivity using a HEAD request.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  
**Returns**: <code>Promise.&lt;boolean&gt;</code> - Resolves to true if the connection is successful.  
**Throws**:

- <code>Error</code> Rejects with the error in case of failure.


| Param | Type | Description |
| --- | --- | --- |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_SdkBase--module.exports+putCreate"></a>

#### module.exports.putCreate(url, obj, model, [cancellationToken]) ⇒ <code>Promise.&lt;Object&gt;</code>
Sends a PUT request to create an object at a given URL.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  
**Returns**: <code>Promise.&lt;Object&gt;</code> - Resolves with the created object.  
**Throws**:

- <code>Error</code> Rejects if the URL or object is invalid or if the request fails.


| Param | Type | Description |
| --- | --- | --- |
| url | <code>string</code> | The URL where the object is created. |
| obj | <code>Object</code> | The object to be created. |
| model | <code>Class</code> | Modal to deserialize on |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_SdkBase--module.exports+head"></a>

#### module.exports.head(url, [cancellationToken]) ⇒ <code>Promise.&lt;boolean&gt;</code>
Checks if an object exists at a given URL using a HEAD request.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  
**Returns**: <code>Promise.&lt;boolean&gt;</code> - Resolves to true if the object exists.  
**Throws**:

- <code>Error</code> Rejects if the URL is invalid or if the request fails.


| Param | Type | Description |
| --- | --- | --- |
| url | <code>string</code> | The URL to check. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_SdkBase--module.exports+get"></a>

#### module.exports.get(url, model, [cancellationToken], [headers]) ⇒ <code>Promise.&lt;Object&gt;</code>
Retrieves an object from a given URL using a GET request.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  
**Returns**: <code>Promise.&lt;Object&gt;</code> - Resolves with the retrieved object.  
**Throws**:

- <code>Error</code> Rejects if the URL is invalid or if the request fails.


| Param | Type | Description |
| --- | --- | --- |
| url | <code>string</code> | The URL of the object. |
| model | <code>Class</code> | Modal to deserialize on |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |
| [headers] | <code>Object</code> | Additional headers. |

<a name="module_SdkBase--module.exports+getDataInBytes"></a>

#### module.exports.getDataInBytes(url, [cancellationToken]) ⇒ <code>Promise.&lt;Object&gt;</code>
Retrieves raw data from a given URL using a GET request.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  
**Returns**: <code>Promise.&lt;Object&gt;</code> - Resolves with the retrieved data.  
**Throws**:

- <code>Error</code> Rejects if the URL is invalid or if the request fails.


| Param | Type | Description |
| --- | --- | --- |
| url | <code>string</code> | The URL of the object. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_SdkBase--module.exports+getMany"></a>

#### module.exports.getMany(url, model, [cancellationToken], [headers]) ⇒ <code>Promise.&lt;Array&gt;</code>
Retrieves a list of objects from a given URL using a GET request.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  
**Returns**: <code>Promise.&lt;Array&gt;</code> - Resolves with the list of retrieved objects.  
**Throws**:

- <code>Error</code> Rejects if the URL is invalid or if the request fails.


| Param | Type | Description |
| --- | --- | --- |
| url | <code>string</code> | The URL of the objects. |
| model | <code>Class</code> | Modal to deserialize on |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |
| [headers] | <code>Object</code> | Additional headers. |

<a name="module_SdkBase--module.exports+putUpdate"></a>

#### module.exports.putUpdate(url, obj, model, [cancellationToken]) ⇒ <code>Promise.&lt;Object&gt;</code>
Sends a PUT request to update an object at a given URL.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  
**Returns**: <code>Promise.&lt;Object&gt;</code> - Resolves with the created object.  
**Throws**:

- <code>Error</code> Rejects if the URL or object is invalid or if the request fails.


| Param | Type | Description |
| --- | --- | --- |
| url | <code>string</code> | The URL where the object is created. |
| obj | <code>Object</code> | The object to be created. |
| model | <code>Class</code> | Modal to deserialize on |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_SdkBase--module.exports+delete"></a>

#### module.exports.delete(url, [cancellationToken]) ⇒ <code>Promise.&lt;void&gt;</code>
Sends a DELETE request to remove an object at a given URL.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  
**Returns**: <code>Promise.&lt;void&gt;</code> - Resolves if the object is successfully deleted.  
**Throws**:

- <code>Error</code> Rejects if the URL is invalid or if the request fails.


| Param | Type | Description |
| --- | --- | --- |
| url | <code>string</code> | The URL of the object to delete. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_SdkBase--module.exports+post"></a>

#### module.exports.post(url, data, model, [cancellationToken]) ⇒ <code>Promise.&lt;Object&gt;</code>
Submits data using a POST request to a given URL.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  
**Returns**: <code>Promise.&lt;Object&gt;</code> - Resolves with the response data.  
**Throws**:

- <code>Error</code> Rejects if the URL or data is invalid or if the request fails.


| Param | Type | Description |
| --- | --- | --- |
| url | <code>string</code> | The URL to post data to. |
| data | <code>Object</code> \| <code>string</code> | The data to send in the POST request. |
| model | <code>Class</code> | Modal to deserialize on |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_SdkBase--module.exports+deleteMany"></a>

#### module.exports.deleteMany(url, obj, [cancellationToken]) ⇒ <code>Promise.&lt;void&gt;</code>
Sends a DELETE request to remove an object at a given URL.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  
**Returns**: <code>Promise.&lt;void&gt;</code> - Resolves if the object is successfully deleted.  
**Throws**:

- <code>Error</code> Rejects if the URL is invalid, the object is not serializable, or if the request fails.


| Param | Type | Description |
| --- | --- | --- |
| url | <code>string</code> | The URL of the object to delete. |
| obj | <code>Object</code> | The object to be created. |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token for cancelling the request. |

<a name="module_SdkBase--module.exports+postBatch"></a>

#### module.exports.postBatch(url, obj, model, [cancellationToken]) ⇒ <code>Promise.&lt;(Object\|null)&gt;</code>
Submits a POST request.

**Kind**: instance method of [<code>module.exports</code>](#exp_module_SdkBase--module.exports)  
**Returns**: <code>Promise.&lt;(Object\|null)&gt;</code> - The response data parsed as an object of type Object, or null if unsuccessful.  
**Throws**:

- <code>Error</code> If the URL is invalid or the object cannot be serialized to JSON.


| Param | Type | Description |
| --- | --- | --- |
| url | <code>string</code> | The URL to which the request is sent. |
| obj | <code>Object</code> | The object to send in the POST request body. |
| model | <code>Class</code> | Modal to deserialize on |
| [cancellationToken] | <code>AbortController</code> | Optional cancellation token to cancel the request. |

