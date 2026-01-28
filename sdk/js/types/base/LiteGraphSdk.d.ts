/**
 * LiteGraph SDK class.
 * Extends the SdkBase class.
 * @module  LiteGraphSdk
 * @extends SdkBase
 */
export default class LiteGraphSdk extends SdkBase {
    /**
     * Instantiate the SDK.
     * @param {string} endpoint - The endpoint URL.
     * @param {string} [tenantGuid] - The tenant GUID.
     * @param {string} [accessKey] - The access key.
     */
    constructor(endpoint?: string, tenantGuid?: string, accessKey?: string);
    /**
     * Check if a graph exists by GUID.
     * @param {string} guid - The GUID of the graph.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<boolean>} - True if the graph exists.
     */
    graphExists(guid: string, cancellationToken?: AbortController): Promise<boolean>;
    /**
     * Create a graph.
     * @param {Object} graph - Information about the graph.
     * @param {string} graph.GUID - Globally unique identifier (automatically generated if not provided).
     * @param {string} graph.Name - Name of the graph.
     * @param {string[]} graph.Labels - Array of labels associated with the graph.
     * @param {Object} graph.Tags - Key-value pairs of tags.
     * @param {Array<VectorMetadata>} graph.Vectors - Array of vector embeddings.
     * @param {Object} graph.Data - Object data associated with the graph (default is null).
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Graph>} - The created graph.
     */
    createGraph(graph: {
        GUID: string;
        Name: string;
        Labels: string[];
        Tags: any;
        Vectors: Array<VectorMetadata>;
        Data: any;
    }, cancellationToken?: AbortController): Promise<Graph>;
    /**
     * Read all graphs.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Graph[]>} - An array of graphs.
     */
    readGraphs(cancellationToken?: AbortController): Promise<Graph[]>;
    /**
     * Search graphs.
     * @param {Object} searchReq - Information about the search request.
     * @param {string} searchReq.GraphGUID - Globally unique identifier for the graph (defaults to an empty GUID).
     * @param {string} searchReq.Ordering - Ordering of the search results (default is CreatedDescending).
     * @param {Object} searchReq.Expr - Expression used for the search (default is null).
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<SearchResult>} - The search result.
     */
    searchGraphs(searchReq: {
        GraphGUID: string;
        Ordering: string;
        Expr: any;
    }, cancellationToken?: AbortController): Promise<SearchResult>;
    /**
     * Read a specific graph.
     * @param {string} guid - The GUID of the graph.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Graph>} - The requested graph.
     */
    readGraph(guid: string, cancellationToken?: AbortController): Promise<Graph>;
    /**
     * Update a graph.
     * @param {Object} graph - Information about the graph.
     * @param {string} graph.GUID - Globally unique identifier (automatically generated if not provided).
     * @param {string} graph.name - Name of the graph.
     * @param {Date} graph.CreatedUtc - Creation timestamp in UTC (defaults to now).
     * @param {Object} graph.data - Object data associated with the graph (default is null).
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Graph>} - The updated graph.
     */
    updateGraph(graph: {
        GUID: string;
        name: string;
        CreatedUtc: Date;
        data: any;
    }, cancellationToken?: AbortController): Promise<Graph>;
    /**
     * Delete a graph.
     * @param {string} guid - The GUID of the graph.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @param {boolean} force - Force recursive deletion of edges and nodes.
     */
    deleteGraph(guid: string, force?: boolean, cancellationToken?: AbortController): Promise<void>;
    /**
     * Export a graph to GEXF format.
     * @param {string} guid - The GUID of the graph.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<string>} - The GEXF XML data.
     */
    exportGraphToGexf(guid: string, cancellationToken?: AbortController): Promise<string>;
    /**
     * Execute a batch existence request.
     * @param {string} graphGuid - The GUID of the graph.
     * @param {Object} existenceRequest - Optional initial data for the existence request.
     * @param {string[]} existenceRequest.Nodes - Array of node GUIDs.
     * @param {string[]} existenceRequest.Edges - Array of edge GUIDs.
     * @param {EdgeBetween[]} existenceRequest.EdgesBetween - Array of EdgeBetween instances.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Object>} - The existence result.
     */
    batchExistence(graphGuid: string, existenceRequest: {
        Nodes: string[];
        Edges: string[];
        EdgesBetween: EdgeBetween[];
    }, cancellationToken?: AbortController): Promise<any>;
    /**
     * Check if a node exists by GUID.
     * @param {string} graphGuid - The GUID of the graph.
     * @param {string} guid - The GUID of the node.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<boolean>} - True if the node exists.
     */
    nodeExists(graphGuid: string, guid: string, cancellationToken?: AbortController): Promise<boolean>;
    /**
     * Create multiple nodes.
     * @param {string} graphGuid - The GUID of the graph.
     * @param {Array<Object>} nodes - List of node objects.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Array<Node>>} - The list of created nodes.
     */
    createNodes(graphGuid: string, nodes: Array<any>, cancellationToken?: AbortController): Promise<Array<Node>>;
    /**
     * Create a node.
     * @param {Object} node - Information about the node.
     * @param {string} node.GUID - Globally unique identifier (automatically generated if not provided).
     * @param {string} node.GraphGUID - Globally unique identifier for the graph (automatically generated if not provided).
     * @param {string} node.name - Name of the node.
     * @param {Object} node.data - Object data associated with the node (default is null).
     * @param {Date} node.CreatedUtc - Creation timestamp in UTC (defaults to now).
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Node>} - The created node.
     */
    createNode(node: {
        GUID: string;
        GraphGUID: string;
        name: string;
        data: any;
        CreatedUtc: Date;
    }, cancellationToken?: AbortController): Promise<Node>;
    /**
     * Read nodes for a specific graph.
     * @param {string} graphGuid - The GUID of the graph.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Node[]>} - An array of nodes.
     */
    readNodes(graphGuid: string, cancellationToken?: AbortController): Promise<Node[]>;
    /**
     * Search nodes.
     * @param {Object} searchReq - Information about the search request.
     * @param {string} searchReq.GraphGUID - Globally unique identifier for the graph (defaults to an empty GUID).
     * @param {string} searchReq.Ordering - Ordering of the search results (default is CreatedDescending).
     * @param {Object} searchReq.Expr - Expression used for the search (default is null).
     * @param {string} graphGuid - The GUID of the graph.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<SearchResult>} - The search result.
     */
    searchNodes(graphGuid: string, searchReq: {
        GraphGUID: string;
        Ordering: string;
        Expr: any;
    }, cancellationToken?: AbortController): Promise<SearchResult>;
    /**
     * Read a specific node.
     * @param {string} graphGuid - The GUID of the graph.
     * @param {string} nodeGuid - The GUID of the node.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Node>} - The requested node.
     */
    readNode(graphGuid: string, nodeGuid: string, cancellationToken?: AbortController): Promise<Node>;
    /**
     * Update a node.
     * @param {Object} node - Information about the node.
     * @param {string} node.GUID - Globally unique identifier (automatically generated if not provided).
     * @param {string} node.GraphGUID - Globally unique identifier for the graph (automatically generated if not provided).
     * @param {string} node.name - Name of the node.
     * @param {Object} node.data - Object data associated with the node (default is null).
     * @param {Date} node.CreatedUtc - Creation timestamp in UTC (defaults to now).
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Node>} - The updated node.
     */
    updateNode(node: {
        GUID: string;
        GraphGUID: string;
        name: string;
        data: any;
        CreatedUtc: Date;
    }, cancellationToken?: AbortController): Promise<Node>;
    /**
     * Delete a node.
     * @param {string} graphGuid - The GUID of the graph.
     * @param {string} nodeGuid - The GUID of the node.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     */
    deleteNode(graphGuid: string, nodeGuid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Delete all nodes within a graph.
     * @param {string} graphGuid - The GUID of the graph.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     */
    deleteNodes(graphGuid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Delete multiple nodes within a graph.
     * @param {string} graphGuid - The GUID of the graph.
     * @param {Array<string>} nodeGuids - The list of node GUIDs to delete.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     */
    deleteMultipleNodes(graphGuid: string, nodeGuids: Array<string>, cancellationToken?: AbortController): Promise<any[]>;
    /**
     * Check if an edge exists by GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} guid - Edge GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<boolean>} - True if exists.
     */
    edgeExists(graphGuid: string, guid: string, cancellationToken?: AbortController): Promise<boolean>;
    /**
     * Create multiple edges.
     * @param {string} graphGuid - The GUID of the graph.
     * @param {Array<Object>} edges - List of edge objects.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Array<Object>>} - The list of created edges.
     */
    createEdges(graphGuid: string, edges: Array<any>, cancellationToken?: AbortController): Promise<Array<any>>;
    /**
     * Create an edge.
     * @param {Object} edge - Information about the edge.
     * @param {string} [edge.GUID] - Globally unique identifier for the edge (automatically generated if not provided).
     * @param {string} [edge.GraphGUID] - Globally unique identifier for the graph (automatically generated if not provided).
     * @param {string} [edge.Name] - Name of the edge.
     * @param {string} [edge.From] - Globally unique identifier of the from node.
     * @param {string} [edge.To] - Globally unique identifier of the to node.
     * @param {number} [edge.Cost=0] - Cost associated with the edge (default is 0).
     * @param {Date} [edge.CreatedUtc] - Creation timestamp in UTC (defaults to current UTC time).
     * @param {Object} [edge.Data] - Additional object data associated with the edge (default is null).
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Edge>} - The created edge.
     */
    createEdge(edge: {
        GUID?: string;
        GraphGUID?: string;
        Name?: string;
        From?: string;
        To?: string;
        Cost?: number;
        CreatedUtc?: Date;
        Data?: any;
    }, cancellationToken?: AbortController): Promise<Edge>;
    /**
     * Read edges.
     * @param {string} graphGuid - Graph GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Edge[]>} - List of edges.
     */
    readEdges(graphGuid: string, cancellationToken?: AbortController): Promise<Edge[]>;
    /**
     * Search edges.
     * @param {string} graphGuid - Graph GUID.
     * @param {Object} searchReq - Information about the search request.
     * @param {string} searchReq.GraphGUID - Globally unique identifier for the graph (defaults to an empty GUID).
     * @param {string} searchReq.Ordering - Ordering of the search results (default is CreatedDescending).
     * @param {Object} searchReq.Expr - Expression used for the search (default is null).
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<SearchResult>} - The search result.
     */
    searchEdges(graphGuid: string, searchReq: {
        GraphGUID: string;
        Ordering: string;
        Expr: any;
    }, cancellationToken?: AbortController): Promise<SearchResult>;
    /**
     * Read an edge.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} edgeGuid - Edge GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Edge>} - The requested edge.
     */
    readEdge(graphGuid: string, edgeGuid: string, cancellationToken?: AbortController): Promise<Edge>;
    /**
     * Update an edge.
     * @param {Object} edge - Information about the edge.
     * @param {string} [edge.GUID] - Globally unique identifier for the edge (automatically generated if not provided).
     * @param {string} [edge.GraphGUID] - Globally unique identifier for the graph (automatically generated if not provided).
     * @param {string} [edge.Name] - Name of the edge.
     * @param {string} [edge.From] - Globally unique identifier of the from node.
     * @param {string} [edge.To] - Globally unique identifier of the to node.
     * @param {number} [edge.Cost=0] - Cost associated with the edge (default is 0).
     * @param {Date} [edge.CreatedUtc] - Creation timestamp in UTC (defaults to current UTC time).
     * @param {Object} [edge.Data] - Additional object data associated with the edge (default is null).
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Edge>} - The updated edge.
     */
    updateEdge(edge: {
        GUID?: string;
        GraphGUID?: string;
        Name?: string;
        From?: string;
        To?: string;
        Cost?: number;
        CreatedUtc?: Date;
        Data?: any;
    }, cancellationToken?: AbortController): Promise<Edge>;
    /**
     * Delete an edge.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} edgeGuid - Edge GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<void>} - Promise representing the completion of the deletion.
     */
    deleteEdge(graphGuid: string, edgeGuid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Delete all edges within a graph.
     * @param {string} graphGuid - The GUID of the graph.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     */
    deleteEdges(graphGuid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Delete multiple edges within a graph.
     * @param {string} graphGuid - The GUID of the graph.
     * @param {Array<string>} edgeGuids - The list of edge GUIDs to delete.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     */
    deleteMultipleEdges(graphGuid: string, edgeGuids: Array<string>, cancellationToken?: AbortController): Promise<any[]>;
    /**
     * Get edges from a node.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} nodeGuid - Node GUID.
     * @param {AbortSignal} [cancellationToken] - Abort signal for cancellation.
     * @returns {Promise<Edge[]>} - Edges.
     */
    getEdgesFromNode(graphGuid: string, nodeGuid: string, cancellationToken?: AbortSignal): Promise<Edge[]>;
    /**
     * Get edges to a node.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} nodeGuid - Node GUID.
     * @param {AbortSignal} [cancellationToken] - Abort signal for cancellation.
     * @returns {Promise<Edge[]>} - Edges.
     */
    getEdgesToNode(graphGuid: string, nodeGuid: string, cancellationToken?: AbortSignal): Promise<Edge[]>;
    /**
     * Get edges from a given node to a given node.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} fromNodeGuid - From node GUID.
     * @param {string} toNodeGuid - To node GUID.
     * @param {AbortSignal} [cancellationToken] - Abort signal for cancellation.
     * @returns {Promise<Edge[]>} - Edges.
     */
    getEdgesBetween(graphGuid: string, fromNodeGuid: string, toNodeGuid: string, cancellationToken?: AbortSignal): Promise<Edge[]>;
    /**
     * Get all edges to or from a node.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} nodeGuid - Node GUID.
     * @param {AbortSignal} [cancellationToken] - Abort signal for cancellation.
     * @returns {Promise<Edge[]>} - Edges.
     */
    getAllNodeEdges(graphGuid: string, nodeGuid: string, cancellationToken?: AbortSignal): Promise<Edge[]>;
    /**
     * Get child nodes from a node.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} nodeGuid - Node GUID.
     * @param {AbortSignal} [cancellationToken] - Abort signal for cancellation.
     * @returns {Promise<Node[]>} - Child nodes.
     */
    getChildrenFromNode(graphGuid: string, nodeGuid: string, cancellationToken?: AbortSignal): Promise<Node[]>;
    /**
     * Get parent nodes from a node.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} nodeGuid - Node GUID.
     * @param {AbortSignal} [cancellationToken] - Abort signal for cancellation.
     * @returns {Promise<Node[]>} - Parent nodes.
     */
    getParentsFromNode(graphGuid: string, nodeGuid: string, cancellationToken?: AbortSignal): Promise<Node[]>;
    /**
     * Get neighboring nodes from a node.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} nodeGuid - Node GUID.
     * @param {AbortSignal} [cancellationToken] - Abort signal for cancellation.
     * @returns {Promise<Node[]>} - Neighboring nodes.
     */
    getNodeNeighbors(graphGuid: string, nodeGuid: string, cancellationToken?: AbortSignal): Promise<Node[]>;
    /**
     * Get routes between two nodes.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} fromNodeGuid - From node GUID.
     * @param {string} toNodeGuid - To node GUID.
     * @param {AbortSignal} [cancellationToken] - Abort signal for cancellation.
     * @returns {Promise<RouteResult>} - Routes.
     */
    getRoutes(graphGuid: string, fromNodeGuid: string, toNodeGuid: string, cancellationToken?: AbortSignal): Promise<RouteResult>;
    /**
     * Read all tenants.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<TenantMetaData[]>} - An array of tenants.
     */
    readTenants(cancellationToken?: AbortController): Promise<TenantMetaData[]>;
    /**
     * Read a tenant.
     * @param {string} tenantGuid - The GUID of the tenant.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<TenantMetaData>} - The tenant.
     */
    readTenant(tenantGuid: string, cancellationToken?: AbortController): Promise<TenantMetaData>;
    /**
     * Create a tenant.
     * @param {TenantMetaData} tenant - The tenant to create.
     * @param {String} tenant.name - The name of the tenant.
     * @param {boolean} tenant.Active - Indicates if tenant is active.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<TenantMetaData>} - The created tenant.
     */
    createTenant(tenant: TenantMetaData, cancellationToken?: AbortController): Promise<TenantMetaData>;
    /**
     * Update a tenant.
     * @param {TenantMetaData} tenant - The tenant to update.
     * @param {String} tenant.name - The name of the tenant.
     * @param {boolean} tenant.Active - Indicates if tenant is active.
     * @param {string} guid - The GUID of the tenant.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<TenantMetaData>} - The updated tenant.
     */
    updateTenant(tenant: TenantMetaData, guid: string, cancellationToken?: AbortController): Promise<TenantMetaData>;
    /**
     * Delete a tenant.
     * @param {string} tenantGuid - The GUID of the tenant.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Boolean>}
     */
    deleteTenant(tenantGuid: string, cancellationToken?: AbortController): Promise<boolean>;
    /**
     * Tenant exists.
     * @param {string} tenantGuid - The GUID of the tenant.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<boolean>}
     */
    tenantExists(tenantGuid: string, cancellationToken?: AbortController): Promise<boolean>;
    /**
     * Tenant delete force.
     * @param {string} tenantGuid - The GUID of the tenant.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Boolean>}
     */
    tenantDeleteForce(tenantGuid: string, cancellationToken?: AbortController): Promise<boolean>;
    /**
     * Read all users.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<UserMetadata[]>} - An array of users.
     */
    readAllUsers(cancellationToken?: AbortController): Promise<UserMetadata[]>;
    /**
     * Read a user.
     * @param {string} userGuid - The GUID of the user.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<UserMetadata>} - The user.
     */
    readUser(userGuid: string, cancellationToken?: AbortController): Promise<UserMetadata>;
    /**
     * Create a user.
     * @param {UserMetadata} user - The user to create.
     * @param {String} user.FirstName - The first name of the user.
     * @param {String} user.LastName - The last name of the user.
     * @param {boolean} user.Active - Indicates if user is active.
     * @param {string} user.Email - The email of the user.
     * @param {string} user.Password - The password of the user.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<UserMetadata>} - The created user.
     */
    createUser(user: UserMetadata, cancellationToken?: AbortController): Promise<UserMetadata>;
    /**
     * User exists.
     * @param {string} guid - The GUID of the user.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<boolean>}
     */
    existsUser(guid: string, cancellationToken?: AbortController): Promise<boolean>;
    /**
     * Update a user.
     * @param {UserMetadata} user - The user to update.
     * @param {String} user.FirstName - The first name of the user.
     * @param {String} user.LastName - The last name of the user.
     * @param {boolean} user.Active - Indicates if user is active.
     * @param {string} user.Email - The email of the user.
     * @param {string} user.Password - The password of the user.
     * @param {string} guid - The GUID of the user.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<UserMetadata>} - The updated user.
     */
    updateUser(user: UserMetadata, guid: string, cancellationToken?: AbortController): Promise<UserMetadata>;
    /**
     * Delete a user.
     * @param {string} guid - The GUID of the user.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Boolean>}
     */
    deleteUser(guid: string, cancellationToken?: AbortController): Promise<boolean>;
    /**
     * Read all credentials.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<CredentialMetadata[]>} - An array of credentials.
     */
    readAllCredentials(cancellationToken?: AbortController): Promise<CredentialMetadata[]>;
    /**
     * Read a credential.
     * @param {string} guid - The GUID of the credential.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<CredentialMetadata>} - The credential.
     */
    readCredential(guid: string, cancellationToken?: AbortController): Promise<CredentialMetadata>;
    /**
     * Create a credential.
     * @param {CredentialMetadata} credential - The credential to create.
     * @param {string} credential.Name - The name of the credential.
     * @param {string} credential.BearerToken - The bearer token of the credential.
     * @param {boolean} credential.Active - Indicates if credential is active.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<CredentialMetadata>} - The created credential.
     */
    createCredential(credential: CredentialMetadata, cancellationToken?: AbortController): Promise<CredentialMetadata>;
    /**
     * Update a credential.
     * @param {CredentialMetadata} credential - The credential to update.
     * @param {string} credential.Name - The name of the credential.
     * @param {string} credential.BearerToken - The bearer token of the credential.
     * @param {boolean} credential.Active - Indicates if credential is active.
     * @param {string} guid - The GUID of the credential.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<CredentialMetadata>} - The updated credential.
     */
    updateCredential(credential: CredentialMetadata, guid: string, cancellationToken?: AbortController): Promise<CredentialMetadata>;
    /**
     * Delete a credential.
     * @param {string} guid - The GUID of the credential.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Boolean>}
     */
    deleteCredential(guid: string, cancellationToken?: AbortController): Promise<boolean>;
    /**
     * Credential exists.
     * @param {string} guid - The GUID of the credential.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<boolean>}
     */
    existsCredential(guid: string, cancellationToken?: AbortController): Promise<boolean>;
    /**
     * Read all tags.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<TagMetaData[]>}
     */
    readAllTags(cancellationToken?: AbortController): Promise<TagMetaData[]>;
    /**
     * Read a tag.
     * @param {string} guid - The GUID of the tag.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<TagMetaData>}
     */
    readTag(guid: string, cancellationToken?: AbortController): Promise<TagMetaData>;
    /**
     * Tag exists.
     * @param {string} guid - The GUID of the tag.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<boolean>}
     */
    existsTag(guid: string, cancellationToken?: AbortController): Promise<boolean>;
    /**
     * Create a tag.
     * @param {TagMetaData} tag - The tag to create.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<TagMetaData>}
     */
    createTag(tag: TagMetaData, cancellationToken?: AbortController): Promise<TagMetaData>;
    /**
     * Update a tag.
     * @param {TagMetaData} tag - The tag to update.
     * @param {string} guid - The GUID of the tag.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<TagMetaData>}
     */
    updateTag(tag: TagMetaData, guid: string, cancellationToken?: AbortController): Promise<TagMetaData>;
    /**
     * Delete a tag.
     * @param {string} guid - The GUID of the tag.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<void>}
     */
    deleteTag(guid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Read all labels.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<LabelMetadata[]>}
     */
    readAllLabels(cancellationToken?: AbortController): Promise<LabelMetadata[]>;
    /**
     * Read a label.
     * @param {string} guid - The GUID of the label.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<LabelMetadata>}
     */
    readLabel(guid: string, cancellationToken?: AbortController): Promise<LabelMetadata>;
    /**
     * Label exists.
     * @param {string} guid - The GUID of the label.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<boolean>}
     */
    existsLabel(guid: string, cancellationToken?: AbortController): Promise<boolean>;
    /**
     * Create a label.
     * @param {LabelMetadata} label - The label to create.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<LabelMetadata>}
     */
    createLabel(label: LabelMetadata, cancellationToken?: AbortController): Promise<LabelMetadata>;
    /**
     * Update a label.
     * @param {LabelMetadata} label - The label to update.
     * @param {string} guid - The GUID of the label.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<LabelMetadata>}
     */
    updateLabel(label: LabelMetadata, guid: string, cancellationToken?: AbortController): Promise<LabelMetadata>;
    /**
     * Delete a label.
     * @param {string} guid - The GUID of the label.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<void>}
     */
    deleteLabel(guid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Read all vectors.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<VectorMetadata[]>}
     */
    readAllVectors(cancellationToken?: AbortController): Promise<VectorMetadata[]>;
    /**
     * Read a vector.
     * @param {string} guid - The GUID of the vector.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<VectorMetadata>}
     */
    readVector(guid: string, cancellationToken?: AbortController): Promise<VectorMetadata>;
    /**
     * Vector exists.
     * @param {string} guid - The GUID of the vector.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<boolean>}
     */
    existsVector(guid: string, cancellationToken?: AbortController): Promise<boolean>;
    /**
     * Create a vector.
     * @param {VectorMetadata} vector - The vector to create.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<VectorMetadata>}
     */
    createVector(vector: VectorMetadata, cancellationToken?: AbortController): Promise<VectorMetadata>;
    /**
     * Update a vector.
     * @param {VectorMetadata} vector - The vector to update.
     * @param {string} guid - The GUID of the vector.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<VectorMetadata>}
     */
    updateVector(vector: VectorMetadata, guid: string, cancellationToken?: AbortController): Promise<VectorMetadata>;
    /**
     * Delete a vector.
     * @param {string} guid - The GUID of the vector.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<void>}
     */
    deleteVector(guid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Search Vectors.
     * @param {Object} searchReq - Information about the search request.
     * @param {string} searchReq.GraphGUID - Globally unique identifier for the graph (defaults to an empty GUID).
     * @param {string} searchReq.Domain - Ordering of the search results (default is CreatedDescending).
     * @param {String} searchReq.SearchType - Expression used for the search (default is null).
     * @param {Array<string>} searchReq.Labels - The domain of the search type.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<VectorSearchResult>} - The search result.
     */
    searchVectors(searchReq: {
        GraphGUID: string;
        Domain: string;
        SearchType: string;
        Labels: Array<string>;
    }, cancellationToken?: AbortController): Promise<VectorSearchResult>;
    /**
     * Generate an authentication token.
     * @param {string} email - The user's email address.
     * @param {string} tenantId - The tenant ID.
     * @param {string} password - The user's password.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Token>} The generated authentication token
     */
    generateToken(email: string, password: string, tenantId: string, cancellationToken?: AbortController): Promise<Token>;
    /**
     * Fetch details about an authentication token.
     * @param {string} token - The authentication token to inspect.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Token>} The token details
     */
    getTokenDetails(token: string, cancellationToken?: AbortController): Promise<Token>;
    /**
     * Get tenants associated with an email address.
     * @param {string} email - The email address to lookup tenants for.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<TenantMetaData[]>} Array of tenants associated with the email
     */
    getTenantsForEmail(email: string, cancellationToken?: AbortController): Promise<TenantMetaData[]>;
}
import SdkBase from './SdkBase';
import { VectorMetadata } from '../models/VectorMetadata';
import Graph from '../models/Graph';
import SearchResult from '../models/SearchResult';
import EdgeBetween from '../models/EdgeBetween';
import Node from '../models/Node';
import Edge from '../models/Edge';
import RouteResult from '../models/RouteResult';
import TenantMetaData from '../models/TenantMetaData';
import UserMetadata from '../models/UserMetadata';
import CredentialMetadata from '../models/CredentialMetadata';
import TagMetaData from '../models/TagMetaData';
import LabelMetadata from '../models/LabelMetadata';
import { VectorSearchResult } from '../models/VectorSearchResult';
import Token from '../models/Token';
