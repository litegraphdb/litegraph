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
     * Read graphs as a paginated enumeration result.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options or cancellation token.
     * @param {number} [optionsOrCancellationToken.maxKeys] - Maximum number of results (1-1000, default 1000). Sent as max-keys.
     * @param {number} [optionsOrCancellationToken.skip] - Number of records to skip (default 0).
     * @param {string} [optionsOrCancellationToken.order] - Enumeration ordering.
     * @param {string} [optionsOrCancellationToken.token] - Continuation token GUID from a previous page.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are Graph instances.
     */
    readGraphs(optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
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
     * Export an entire graph to JSONL format.
     * @param {string} graphGuid - The GUID of the graph.
     * @param {Object} [options] - Export options.
     * @param {boolean} [options.includeData=false] - Include object data for graph, nodes, and edges.
     * @param {boolean} [options.includeSubordinates=false] - Include subordinate labels, tags, and vectors.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<string>} - The JSONL data.
     */
    exportGraphToJsonl(graphGuid: string, { includeData, includeSubordinates }?: {
        includeData?: boolean;
        includeSubordinates?: boolean;
    }, cancellationToken?: AbortController): Promise<string>;
    /**
     * Export a subgraph to JSONL format using a subgraph extraction request.
     * @param {string} graphGuid - The GUID of the graph.
     * @param {Object} subgraphExtractionRequest - The subgraph extraction request.
     * @param {string[]} subgraphExtractionRequest.StartNodeGUIDs - Starting node GUIDs for extraction.
     * @param {number} [subgraphExtractionRequest.MaxDepth=2] - Maximum traversal depth.
     * @param {string} [subgraphExtractionRequest.Direction=Both] - Traversal direction: Outbound, Inbound, or Both.
     * @param {number} [subgraphExtractionRequest.MaxNodes=0] - Maximum number of nodes (0 = unlimited).
     * @param {number} [subgraphExtractionRequest.MaxEdges=0] - Maximum number of edges (0 = unlimited).
     * @param {boolean} [subgraphExtractionRequest.IncludeData] - Include object data.
     * @param {boolean} [subgraphExtractionRequest.IncludeSubordinates] - Include subordinate labels, tags, and vectors.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<string>} - The JSONL data.
     */
    exportSubgraphToJsonl(graphGuid: string, subgraphExtractionRequest: {
        StartNodeGUIDs: string[];
        MaxDepth?: number;
        Direction?: string;
        MaxNodes?: number;
        MaxEdges?: number;
        IncludeData?: boolean;
        IncludeSubordinates?: boolean;
    }, cancellationToken?: AbortController): Promise<string>;
    /**
     * Import JSONL data into an existing graph.
     * @param {string} graphGuid - The GUID of the graph.
     * @param {string} jsonlString - The raw JSONL data to import.
     * @param {Object} [options] - Import options.
     * @param {string} [options.guidStrategy] - GUID handling strategy: preserve, regenerate, skip, or overwrite.
     * @param {string} [options.onError] - Error handling behavior: abort or skip.
     * @param {number} [options.batchSize] - Batch size for import operations (positive integer).
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Object>} - The GraphImportResult.
     */
    importGraphFromJsonl(graphGuid: string, jsonlString: string, { guidStrategy, onError, batchSize }?: {
        guidStrategy?: string;
        onError?: string;
        batchSize?: number;
    }, cancellationToken?: AbortController): Promise<any>;
    /**
     * Import JSONL data as a new graph.
     * @param {string} jsonlString - The raw JSONL data to import.
     * @param {Object} [options] - Import options.
     * @param {string} [options.guidStrategy] - GUID handling strategy: preserve, regenerate, skip, or overwrite.
     * @param {string} [options.onError] - Error handling behavior: abort or skip.
     * @param {number} [options.batchSize] - Batch size for import operations (positive integer).
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Object>} - The GraphImportResult.
     */
    importGraphAsNewFromJsonl(jsonlString: string, { guidStrategy, onError, batchSize }?: {
        guidStrategy?: string;
        onError?: string;
        batchSize?: number;
    }, cancellationToken?: AbortController): Promise<any>;
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
     * Create a graph-scoped transaction builder.
     * @param {string} graphGuid - The GUID of the graph.
     * @param {Object} [options] - Transaction defaults.
     * @param {number} [options.MaxOperations=1000] - Maximum operation count.
     * @param {number} [options.TimeoutSeconds=60] - Transaction timeout in seconds.
     * @param {string} [options.IsolationLevel=Default] - Transaction isolation level.
     * @returns {GraphTransactionBuilder} - Transaction builder.
     */
    transaction(graphGuid: string, options?: {
        MaxOperations?: number;
        TimeoutSeconds?: number;
        IsolationLevel?: string;
    }): GraphTransactionBuilder;
    /**
     * Execute a graph-scoped transaction.
     * @param {string} graphGuid - The GUID of the graph.
     * @param {Object} request - Transaction request.
     * @param {Array<Object>} request.Operations - Operations to execute atomically.
     * @param {string} [request.IsolationLevel=Default] - Transaction isolation level.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<TransactionResult>} - Transaction result.
     */
    executeTransaction(graphGuid: string, request: {
        Operations: Array<any>;
        IsolationLevel?: string;
    }, cancellationToken?: AbortController): Promise<TransactionResult>;
    /**
     * Create a native graph query request.
     * @param {string} query - Query text.
     * @param {Object} [parameters] - Query parameters.
     * @param {Object} [options] - Query execution options.
     * @param {number} [options.MaxResults=1000] - Maximum returned rows.
     * @param {number} [options.TimeoutSeconds=30] - Query timeout in seconds.
     * @param {boolean} [options.IncludeProfile=false] - Include execution profile timings.
     * @returns {Object} - Query request.
     */
    queryRequest(query: string, parameters?: any, options?: {
        MaxResults?: number;
        TimeoutSeconds?: number;
        IncludeProfile?: boolean;
    }): any;
    /**
     * Execute a native graph query.
     * @param {string} graphGuid - The GUID of the graph.
     * @param {Object|string} request - Query request or query text.
     * @param {Object} [parameters] - Query parameters when request is query text.
     * @param {Object} [options] - Query execution options.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<GraphQueryResult>} - Query result.
     */
    executeQuery(graphGuid: string, request: any | string, parameters?: any, options?: any, cancellationToken?: AbortController): Promise<GraphQueryResult>;
    /**
     * List authorization roles for the configured tenant as a paginated enumeration result.
     * @param {Object} [options] - Role list options.
     * @param {number} [options.page=0] - Page index.
     * @param {number} [options.pageSize=1000] - Page size.
     * @param {boolean} [options.includeBuiltIns=true] - Include built-in roles.
     * @param {boolean} [options.builtIn] - Filter by built-in status.
     * @param {number} [options.maxKeys] - Maximum number of results (1-1000). Sent as max-keys and overrides pageSize.
     * @param {number} [options.skip] - Number of records to skip (default 0). Overrides page.
     * @param {string} [options.order] - Enumeration ordering.
     * @param {string} [options.token] - Continuation token GUID from a previous page.
     * @param {AbortController} [cancellationToken] - Optional cancellation token.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are AuthorizationRole instances.
     */
    listAuthorizationRoles(options?: {
        page?: number;
        pageSize?: number;
        includeBuiltIns?: boolean;
        builtIn?: boolean;
        maxKeys?: number;
        skip?: number;
        order?: string;
        token?: string;
    }, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Create an authorization role.
     * @param {Object} role - Role payload.
     * @param {AbortController} [cancellationToken] - Optional cancellation token.
     * @returns {Promise<AuthorizationRole>} - Created role.
     */
    createAuthorizationRole(role: any, cancellationToken?: AbortController): Promise<AuthorizationRole>;
    /**
     * Read an authorization role.
     * @param {string} roleGuid - Role GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token.
     * @returns {Promise<AuthorizationRole>} - Role.
     */
    readAuthorizationRole(roleGuid: string, cancellationToken?: AbortController): Promise<AuthorizationRole>;
    /**
     * Update an authorization role.
     * @param {Object} role - Role payload containing GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token.
     * @returns {Promise<AuthorizationRole>} - Updated role.
     */
    updateAuthorizationRole(role: any, cancellationToken?: AbortController): Promise<AuthorizationRole>;
    /**
     * Delete an authorization role.
     * @param {string} roleGuid - Role GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token.
     * @returns {Promise<void>}
     */
    deleteAuthorizationRole(roleGuid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * List user role assignments as a paginated enumeration result.
     * @param {string} userGuid - User GUID.
     * @param {Object} [options] - List filters, including pagination options ({ maxKeys, skip, order, token }).
     * @param {AbortController} [cancellationToken] - Optional cancellation token.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are UserRoleAssignment instances.
     */
    listUserRoleAssignments(userGuid: string, options?: any, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Create a user role assignment.
     * @param {string} userGuid - User GUID.
     * @param {Object} assignment - Assignment payload.
     * @param {AbortController} [cancellationToken] - Optional cancellation token.
     * @returns {Promise<UserRoleAssignment>} - Created assignment.
     */
    createUserRoleAssignment(userGuid: string, assignment: any, cancellationToken?: AbortController): Promise<UserRoleAssignment>;
    /**
     * Read a user role assignment.
     * @param {string} userGuid - User GUID.
     * @param {string} assignmentGuid - Assignment GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token.
     * @returns {Promise<UserRoleAssignment>} - Assignment.
     */
    readUserRoleAssignment(userGuid: string, assignmentGuid: string, cancellationToken?: AbortController): Promise<UserRoleAssignment>;
    /**
     * Update a user role assignment.
     * @param {string} userGuid - User GUID.
     * @param {Object} assignment - Assignment payload containing GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token.
     * @returns {Promise<UserRoleAssignment>} - Updated assignment.
     */
    updateUserRoleAssignment(userGuid: string, assignment: any, cancellationToken?: AbortController): Promise<UserRoleAssignment>;
    /**
     * Delete a user role assignment.
     * @param {string} userGuid - User GUID.
     * @param {string} assignmentGuid - Assignment GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token.
     * @returns {Promise<void>}
     */
    deleteUserRoleAssignment(userGuid: string, assignmentGuid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Read effective permissions for a user.
     * @param {string} userGuid - User GUID.
     * @param {string} [graphGuid] - Optional graph GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token.
     * @returns {Promise<AuthorizationEffectivePermissionsResult>} - Effective permissions.
     */
    getUserEffectivePermissions(userGuid: string, graphGuid?: string, cancellationToken?: AbortController): Promise<AuthorizationEffectivePermissionsResult>;
    /**
     * List credential scope assignments as a paginated enumeration result.
     * @param {string} credentialGuid - Credential GUID.
     * @param {Object} [options] - List filters, including pagination options ({ maxKeys, skip, order, token }).
     * @param {AbortController} [cancellationToken] - Optional cancellation token.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are CredentialScopeAssignment instances.
     */
    listCredentialScopeAssignments(credentialGuid: string, options?: any, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Create a credential scope assignment.
     * @param {string} credentialGuid - Credential GUID.
     * @param {Object} assignment - Assignment payload.
     * @param {AbortController} [cancellationToken] - Optional cancellation token.
     * @returns {Promise<CredentialScopeAssignment>} - Created scope.
     */
    createCredentialScopeAssignment(credentialGuid: string, assignment: any, cancellationToken?: AbortController): Promise<CredentialScopeAssignment>;
    /**
     * Read a credential scope assignment.
     * @param {string} credentialGuid - Credential GUID.
     * @param {string} assignmentGuid - Assignment GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token.
     * @returns {Promise<CredentialScopeAssignment>} - Scope assignment.
     */
    readCredentialScopeAssignment(credentialGuid: string, assignmentGuid: string, cancellationToken?: AbortController): Promise<CredentialScopeAssignment>;
    /**
     * Update a credential scope assignment.
     * @param {string} credentialGuid - Credential GUID.
     * @param {Object} assignment - Assignment payload containing GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token.
     * @returns {Promise<CredentialScopeAssignment>} - Updated scope.
     */
    updateCredentialScopeAssignment(credentialGuid: string, assignment: any, cancellationToken?: AbortController): Promise<CredentialScopeAssignment>;
    /**
     * Delete a credential scope assignment.
     * @param {string} credentialGuid - Credential GUID.
     * @param {string} assignmentGuid - Assignment GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token.
     * @returns {Promise<void>}
     */
    deleteCredentialScopeAssignment(credentialGuid: string, assignmentGuid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Read effective permissions for a credential.
     * @param {string} credentialGuid - Credential GUID.
     * @param {string} [graphGuid] - Optional graph GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token.
     * @returns {Promise<AuthorizationEffectivePermissionsResult>} - Effective permissions.
     */
    getCredentialEffectivePermissions(credentialGuid: string, graphGuid?: string, cancellationToken?: AbortController): Promise<AuthorizationEffectivePermissionsResult>;
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
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional return mode options or cancellation token.
     * @param {string} [optionsOrCancellationToken.returnMode] - Optional bulk create return mode: full or minimal.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<Array<Node>>} - The list of created nodes.
     */
    createNodes(graphGuid: string, nodes: Array<any>, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<Array<Node>>;
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
     * Read nodes for a specific graph as a paginated enumeration result.
     * @param {string} graphGuid - The GUID of the graph.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options or cancellation token.
     * @param {number} [optionsOrCancellationToken.maxKeys] - Maximum number of results (1-1000, default 1000). Sent as max-keys.
     * @param {number} [optionsOrCancellationToken.skip] - Number of records to skip (default 0).
     * @param {string} [optionsOrCancellationToken.order] - Enumeration ordering.
     * @param {string} [optionsOrCancellationToken.token] - Continuation token GUID from a previous page.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are Node instances.
     */
    readNodes(graphGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
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
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional return mode options or cancellation token.
     * @param {string} [optionsOrCancellationToken.returnMode] - Optional bulk create return mode: full or minimal.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<Array<Object>>} - The list of created edges.
     */
    createEdges(graphGuid: string, edges: Array<any>, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<Array<any>>;
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
     * Read edges as a paginated enumeration result.
     * @param {string} graphGuid - Graph GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options or cancellation token.
     * @param {number} [optionsOrCancellationToken.maxKeys] - Maximum number of results (1-1000, default 1000). Sent as max-keys.
     * @param {number} [optionsOrCancellationToken.skip] - Number of records to skip (default 0).
     * @param {string} [optionsOrCancellationToken.order] - Enumeration ordering.
     * @param {string} [optionsOrCancellationToken.token] - Continuation token GUID from a previous page.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are Edge instances.
     */
    readEdges(graphGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
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
     * Get edges from a node as a paginated enumeration result.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} nodeGuid - Node GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options or cancellation token.
     * @param {number} [optionsOrCancellationToken.maxKeys] - Maximum number of results (1-1000, default 1000). Sent as max-keys.
     * @param {number} [optionsOrCancellationToken.skip] - Number of records to skip (default 0).
     * @param {string} [optionsOrCancellationToken.order] - Enumeration ordering.
     * @param {string} [optionsOrCancellationToken.token] - Continuation token GUID from a previous page.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are Edge instances.
     */
    getEdgesFromNode(graphGuid: string, nodeGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Get edges to a node as a paginated enumeration result.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} nodeGuid - Node GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options or cancellation token.
     * @param {number} [optionsOrCancellationToken.maxKeys] - Maximum number of results (1-1000, default 1000). Sent as max-keys.
     * @param {number} [optionsOrCancellationToken.skip] - Number of records to skip (default 0).
     * @param {string} [optionsOrCancellationToken.order] - Enumeration ordering.
     * @param {string} [optionsOrCancellationToken.token] - Continuation token GUID from a previous page.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are Edge instances.
     */
    getEdgesToNode(graphGuid: string, nodeGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Get edges from a given node to a given node as a paginated enumeration result.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} fromNodeGuid - From node GUID.
     * @param {string} toNodeGuid - To node GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options or cancellation token.
     * @param {number} [optionsOrCancellationToken.maxKeys] - Maximum number of results (1-1000, default 1000). Sent as max-keys.
     * @param {number} [optionsOrCancellationToken.skip] - Number of records to skip (default 0).
     * @param {string} [optionsOrCancellationToken.order] - Enumeration ordering.
     * @param {string} [optionsOrCancellationToken.token] - Continuation token GUID from a previous page.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are Edge instances.
     */
    getEdgesBetween(graphGuid: string, fromNodeGuid: string, toNodeGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Get all edges to or from a node as a paginated enumeration result.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} nodeGuid - Node GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options or cancellation token.
     * @param {number} [optionsOrCancellationToken.maxKeys] - Maximum number of results (1-1000, default 1000). Sent as max-keys.
     * @param {number} [optionsOrCancellationToken.skip] - Number of records to skip (default 0).
     * @param {string} [optionsOrCancellationToken.order] - Enumeration ordering.
     * @param {string} [optionsOrCancellationToken.token] - Continuation token GUID from a previous page.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are Edge instances.
     */
    getAllNodeEdges(graphGuid: string, nodeGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Get child nodes from a node as a paginated enumeration result.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} nodeGuid - Node GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options or cancellation token.
     * @param {number} [optionsOrCancellationToken.maxKeys] - Maximum number of results (1-1000, default 1000). Sent as max-keys.
     * @param {number} [optionsOrCancellationToken.skip] - Number of records to skip (default 0).
     * @param {string} [optionsOrCancellationToken.order] - Enumeration ordering.
     * @param {string} [optionsOrCancellationToken.token] - Continuation token GUID from a previous page.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are Node instances.
     */
    getChildrenFromNode(graphGuid: string, nodeGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Get parent nodes from a node as a paginated enumeration result.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} nodeGuid - Node GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options or cancellation token.
     * @param {number} [optionsOrCancellationToken.maxKeys] - Maximum number of results (1-1000, default 1000). Sent as max-keys.
     * @param {number} [optionsOrCancellationToken.skip] - Number of records to skip (default 0).
     * @param {string} [optionsOrCancellationToken.order] - Enumeration ordering.
     * @param {string} [optionsOrCancellationToken.token] - Continuation token GUID from a previous page.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are Node instances.
     */
    getParentsFromNode(graphGuid: string, nodeGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Get neighboring nodes from a node as a paginated enumeration result.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} nodeGuid - Node GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options or cancellation token.
     * @param {number} [optionsOrCancellationToken.maxKeys] - Maximum number of results (1-1000, default 1000). Sent as max-keys.
     * @param {number} [optionsOrCancellationToken.skip] - Number of records to skip (default 0).
     * @param {string} [optionsOrCancellationToken.order] - Enumeration ordering.
     * @param {string} [optionsOrCancellationToken.token] - Continuation token GUID from a previous page.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are Node instances.
     */
    getNodeNeighbors(graphGuid: string, nodeGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
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
     * Read tenants as a paginated enumeration result.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options or cancellation token.
     * @param {number} [optionsOrCancellationToken.maxKeys] - Maximum number of results (1-1000, default 1000). Sent as max-keys.
     * @param {number} [optionsOrCancellationToken.skip] - Number of records to skip (default 0).
     * @param {string} [optionsOrCancellationToken.order] - Enumeration ordering.
     * @param {string} [optionsOrCancellationToken.token] - Continuation token GUID from a previous page.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are TenantMetaData instances.
     */
    readTenants(optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
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
     * Read users as a paginated enumeration result.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options or cancellation token.
     * @param {number} [optionsOrCancellationToken.maxKeys] - Maximum number of results (1-1000, default 1000). Sent as max-keys.
     * @param {number} [optionsOrCancellationToken.skip] - Number of records to skip (default 0).
     * @param {string} [optionsOrCancellationToken.order] - Enumeration ordering.
     * @param {string} [optionsOrCancellationToken.token] - Continuation token GUID from a previous page.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are UserMetadata instances.
     */
    readAllUsers(optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
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
     * Read credentials as a paginated enumeration result.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options or cancellation token.
     * @param {number} [optionsOrCancellationToken.maxKeys] - Maximum number of results (1-1000, default 1000). Sent as max-keys.
     * @param {number} [optionsOrCancellationToken.skip] - Number of records to skip (default 0).
     * @param {string} [optionsOrCancellationToken.order] - Enumeration ordering.
     * @param {string} [optionsOrCancellationToken.token] - Continuation token GUID from a previous page.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are CredentialMetadata instances.
     */
    readAllCredentials(optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
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
     * Read tags as a paginated enumeration result.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options or cancellation token.
     * @param {number} [optionsOrCancellationToken.maxKeys] - Maximum number of results (1-1000, default 1000). Sent as max-keys.
     * @param {number} [optionsOrCancellationToken.skip] - Number of records to skip (default 0).
     * @param {string} [optionsOrCancellationToken.order] - Enumeration ordering.
     * @param {string} [optionsOrCancellationToken.token] - Continuation token GUID from a previous page.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are TagMetaData instances.
     */
    readAllTags(optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
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
     * Create multiple tags.
     * @param {Array<TagMetaData>} tags - The tags to create.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional return mode options or cancellation token.
     * @param {string} [optionsOrCancellationToken.returnMode] - Optional bulk create return mode: full or minimal.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<TagMetaData[]>}
     */
    createTags(tags: Array<TagMetaData>, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<TagMetaData[]>;
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
     * Read labels as a paginated enumeration result.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options or cancellation token.
     * @param {number} [optionsOrCancellationToken.maxKeys] - Maximum number of results (1-1000, default 1000). Sent as max-keys.
     * @param {number} [optionsOrCancellationToken.skip] - Number of records to skip (default 0).
     * @param {string} [optionsOrCancellationToken.order] - Enumeration ordering.
     * @param {string} [optionsOrCancellationToken.token] - Continuation token GUID from a previous page.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are LabelMetadata instances.
     */
    readAllLabels(optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
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
     * Create multiple labels.
     * @param {Array<LabelMetadata>} labels - The labels to create.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional return mode options or cancellation token.
     * @param {string} [optionsOrCancellationToken.returnMode] - Optional bulk create return mode: full or minimal.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<LabelMetadata[]>}
     */
    createLabels(labels: Array<LabelMetadata>, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<LabelMetadata[]>;
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
     * Read vectors as a paginated enumeration result.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options or cancellation token.
     * @param {number} [optionsOrCancellationToken.maxKeys] - Maximum number of results (1-1000, default 1000). Sent as max-keys.
     * @param {number} [optionsOrCancellationToken.skip] - Number of records to skip (default 0).
     * @param {string} [optionsOrCancellationToken.order] - Enumeration ordering.
     * @param {string} [optionsOrCancellationToken.token] - Continuation token GUID from a previous page.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are VectorMetadata instances.
     */
    readAllVectors(optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
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
     * Create multiple vectors.
     * @param {Array<VectorMetadata>} vectors - The vectors to create.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional return mode options or cancellation token.
     * @param {string} [optionsOrCancellationToken.returnMode] - Optional bulk create return mode: full or minimal.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<VectorMetadata[]>}
     */
    createVectors(vectors: Array<VectorMetadata>, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<VectorMetadata[]>;
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
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are VectorSearchResult instances.
     */
    searchVectors(searchReq: {
        GraphGUID: string;
        Domain: string;
        SearchType: string;
        Labels: Array<string>;
    }, cancellationToken?: AbortController): Promise<EnumerationResult>;
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
     * Get tenants associated with an email address as a paginated enumeration result.
     * @param {string} email - The email address to lookup tenants for.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options or cancellation token.
     * @param {number} [optionsOrCancellationToken.maxKeys] - Maximum number of results (1-1000, default 1000). Sent as max-keys.
     * @param {number} [optionsOrCancellationToken.skip] - Number of records to skip (default 0).
     * @param {string} [optionsOrCancellationToken.order] - Enumeration ordering.
     * @param {string} [optionsOrCancellationToken.token] - Continuation token GUID from a previous page.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are TenantMetaData instances.
     */
    getTenantsForEmail(email: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * List available backups as a paginated enumeration result.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options or cancellation token.
     * @param {number} [optionsOrCancellationToken.maxKeys] - Maximum number of results (1-1000, default 1000). Sent as max-keys.
     * @param {number} [optionsOrCancellationToken.skip] - Number of records to skip (default 0).
     * @param {string} [optionsOrCancellationToken.order] - Enumeration ordering.
     * @param {string} [optionsOrCancellationToken.token] - Continuation token GUID from a previous page.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are backup metadata objects.
     */
    listBackups(optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Create a new database backup.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Object>} Backup metadata.
     */
    createBackup(cancellationToken?: AbortController): Promise<any>;
    /**
     * Read a specific backup file.
     * @param {string} backupFilename - The backup filename.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Object>} Backup data.
     */
    readBackup(backupFilename: string, cancellationToken?: AbortController): Promise<any>;
    /**
     * Check if a backup file exists.
     * @param {string} backupFilename - The backup filename.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<boolean>} True if the backup exists.
     */
    backupExists(backupFilename: string, cancellationToken?: AbortController): Promise<boolean>;
    /**
     * Delete a backup file.
     * @param {string} backupFilename - The backup filename.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<void>}
     */
    deleteBackup(backupFilename: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Flush the in-memory database to disk.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<void>}
     */
    flushDatabase(cancellationToken?: AbortController): Promise<void>;
    /**
     * Read the server settings. Requires system administrator privileges.
     * @param {AbortController} [cancellationToken] - Optional cancellation token.
     * @returns {Promise<Object>} The server settings object.
     */
    readSettings(cancellationToken?: AbortController): Promise<any>;
    /**
     * Update the server settings. Requires system administrator privileges.
     * @param {Object} settings - The full settings object.
     * @param {AbortController} [cancellationToken] - Optional cancellation token.
     * @returns {Promise<Object>} Settings update result ({ Success, AppliedLive, RestartRequired, Message }).
     */
    updateSettings(settings: any, cancellationToken?: AbortController): Promise<any>;
    /**
     * Request a server restart. The server exits so the container restart policy applies the new settings.
     * Requires system administrator privileges. Best-effort; the connection may drop as the server exits.
     * @param {AbortController} [cancellationToken] - Optional cancellation token.
     * @returns {Promise<void>}
     */
    restartServer(cancellationToken?: AbortController): Promise<void>;
    /**
     * Enable vector indexing on a graph.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {Object} config - Vector index configuration.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Object>} Result.
     */
    enableVectorIndex(tenantGuid: string, graphGuid: string, config: any, cancellationToken?: AbortController): Promise<any>;
    /**
     * Disable vector indexing on a graph.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<void>}
     */
    disableVectorIndex(tenantGuid: string, graphGuid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Rebuild the vector index for a graph.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Object>} Result.
     */
    rebuildVectorIndex(tenantGuid: string, graphGuid: string, cancellationToken?: AbortController): Promise<any>;
    /**
     * Get the vector index configuration for a graph.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Object>} Vector index configuration.
     */
    getVectorIndexConfig(tenantGuid: string, graphGuid: string, cancellationToken?: AbortController): Promise<any>;
    /**
     * Get vector index statistics for a graph.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Object>} Vector index statistics.
     */
    getVectorIndexStats(tenantGuid: string, graphGuid: string, cancellationToken?: AbortController): Promise<any>;
    /**
     * Get a subgraph starting from a specific node.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} nodeGuid - Starting node GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Object>} Subgraph data.
     */
    getSubgraph(tenantGuid: string, graphGuid: string, nodeGuid: string, cancellationToken?: AbortController): Promise<any>;
    /**
     * Get subgraph statistics starting from a specific node.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} nodeGuid - Starting node GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Object>} Subgraph statistics.
     */
    getSubgraphStatistics(tenantGuid: string, graphGuid: string, nodeGuid: string, cancellationToken?: AbortController): Promise<any>;
    /**
     * Get statistics for a specific graph.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Object>} Graph statistics.
     */
    getGraphStatistics(tenantGuid: string, graphGuid: string, cancellationToken?: AbortController): Promise<any>;
    /**
     * Get statistics for all graphs in a tenant.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<Object>} All graph statistics.
     */
    getAllGraphStatistics(tenantGuid: string, cancellationToken?: AbortController): Promise<any>;
    /**
     * Get the most connected nodes in a graph as a paginated enumeration result.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options or cancellation token.
     * @param {number} [optionsOrCancellationToken.maxKeys] - Maximum number of results (1-1000, default 1000). Sent as max-keys.
     * @param {number} [optionsOrCancellationToken.skip] - Number of records to skip (default 0).
     * @param {string} [optionsOrCancellationToken.order] - Enumeration ordering.
     * @param {string} [optionsOrCancellationToken.token] - Continuation token GUID from a previous page.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are Node instances.
     */
    getMostConnectedNodes(tenantGuid: string, graphGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Get the least connected nodes in a graph as a paginated enumeration result.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options or cancellation token.
     * @param {number} [optionsOrCancellationToken.maxKeys] - Maximum number of results (1-1000, default 1000). Sent as max-keys.
     * @param {number} [optionsOrCancellationToken.skip] - Number of records to skip (default 0).
     * @param {string} [optionsOrCancellationToken.order] - Enumeration ordering.
     * @param {string} [optionsOrCancellationToken.token] - Continuation token GUID from a previous page.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are Node instances.
     */
    getLeastConnectedNodes(tenantGuid: string, graphGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Read labels for a specific graph.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options ({ maxKeys, skip, order, token }) or cancellation token.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are LabelMetadata instances.
     */
    readGraphLabels(tenantGuid: string, graphGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Read labels for a specific node.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} nodeGuid - Node GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options ({ maxKeys, skip, order, token }) or cancellation token.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are LabelMetadata instances.
     */
    readNodeLabels(tenantGuid: string, graphGuid: string, nodeGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Read labels for a specific edge.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} edgeGuid - Edge GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options ({ maxKeys, skip, order, token }) or cancellation token.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are LabelMetadata instances.
     */
    readEdgeLabels(tenantGuid: string, graphGuid: string, edgeGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Delete all labels for a specific graph.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<void>}
     */
    deleteGraphLabels(tenantGuid: string, graphGuid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Delete all labels for a specific node.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} nodeGuid - Node GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<void>}
     */
    deleteNodeLabels(tenantGuid: string, graphGuid: string, nodeGuid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Delete all labels for a specific edge.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} edgeGuid - Edge GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<void>}
     */
    deleteEdgeLabels(tenantGuid: string, graphGuid: string, edgeGuid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Read tags for a specific graph.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options ({ maxKeys, skip, order, token }) or cancellation token.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are TagMetaData instances.
     */
    readGraphTags(tenantGuid: string, graphGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Read tags for a specific node.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} nodeGuid - Node GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options ({ maxKeys, skip, order, token }) or cancellation token.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are TagMetaData instances.
     */
    readNodeTags(tenantGuid: string, graphGuid: string, nodeGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Read tags for a specific edge.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} edgeGuid - Edge GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options ({ maxKeys, skip, order, token }) or cancellation token.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are TagMetaData instances.
     */
    readEdgeTags(tenantGuid: string, graphGuid: string, edgeGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Delete all tags for a specific graph.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<void>}
     */
    deleteGraphTags(tenantGuid: string, graphGuid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Delete all tags for a specific node.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} nodeGuid - Node GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<void>}
     */
    deleteNodeTags(tenantGuid: string, graphGuid: string, nodeGuid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Delete all tags for a specific edge.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} edgeGuid - Edge GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<void>}
     */
    deleteEdgeTags(tenantGuid: string, graphGuid: string, edgeGuid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Read vectors for a specific graph.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options ({ maxKeys, skip, order, token }) or cancellation token.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are VectorMetadata instances.
     */
    readGraphVectors(tenantGuid: string, graphGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Read vectors for a specific node.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} nodeGuid - Node GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options ({ maxKeys, skip, order, token }) or cancellation token.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are VectorMetadata instances.
     */
    readNodeVectors(tenantGuid: string, graphGuid: string, nodeGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Read vectors for a specific edge.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} edgeGuid - Edge GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options ({ maxKeys, skip, order, token }) or cancellation token.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are VectorMetadata instances.
     */
    readEdgeVectors(tenantGuid: string, graphGuid: string, edgeGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Delete all vectors for a specific graph.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<void>}
     */
    deleteGraphVectors(tenantGuid: string, graphGuid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Delete all vectors for a specific node.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} nodeGuid - Node GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<void>}
     */
    deleteNodeVectors(tenantGuid: string, graphGuid: string, nodeGuid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Delete all vectors for a specific edge.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} graphGuid - Graph GUID.
     * @param {string} edgeGuid - Edge GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<void>}
     */
    deleteEdgeVectors(tenantGuid: string, graphGuid: string, edgeGuid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Create a chat endpoint. Requires tenant administrator privileges.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {Object} endpoint - Information about the chat endpoint.
     * @param {string} [endpoint.GUID] - Globally unique identifier (automatically generated if not provided).
     * @param {string} endpoint.Name - Name of the chat endpoint.
     * @param {string} endpoint.EndpointType - Endpoint type: Embedding or Completion.
     * @param {string} endpoint.Provider - Provider type: OpenAI, Ollama, Gemini, Anthropic, or VoyageAI.
     * @param {string} endpoint.Endpoint - Absolute http/https URL of the upstream provider endpoint.
     * @param {string} [endpoint.ApiKey] - API key for the provider (returned redacted).
     * @param {string} endpoint.Model - Model name to use with this endpoint.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<ChatEndpoint>} - The created chat endpoint (ApiKey redacted).
     */
    createChatEndpoint(tenantGuid: string, endpoint: {
        GUID?: string;
        Name: string;
        EndpointType: string;
        Provider: string;
        Endpoint: string;
        ApiKey?: string;
        Model: string;
    }, cancellationToken?: AbortController): Promise<ChatEndpoint>;
    /**
     * Read chat endpoints as a paginated enumeration result, optionally filtered by endpoint type.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} [endpointType] - Optional endpoint type filter: Embedding or Completion.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options ({ maxKeys, skip, order, token }) or cancellation token.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are ChatEndpoint instances (ApiKey redacted).
     */
    readChatEndpoints(tenantGuid: string, endpointType?: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Read a specific chat endpoint.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} endpointGuid - Chat endpoint GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<ChatEndpoint>} - The requested chat endpoint (ApiKey redacted).
     */
    readChatEndpoint(tenantGuid: string, endpointGuid: string, cancellationToken?: AbortController): Promise<ChatEndpoint>;
    /**
     * Check if a chat endpoint exists by GUID.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} endpointGuid - Chat endpoint GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<boolean>} - True if the chat endpoint exists.
     */
    chatEndpointExists(tenantGuid: string, endpointGuid: string, cancellationToken?: AbortController): Promise<boolean>;
    /**
     * Update a chat endpoint. Sending back a redacted ApiKey preserves the stored key.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {Object} endpoint - Chat endpoint payload containing GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<ChatEndpoint>} - The updated chat endpoint (ApiKey redacted).
     */
    updateChatEndpoint(tenantGuid: string, endpoint: any, cancellationToken?: AbortController): Promise<ChatEndpoint>;
    /**
     * Delete a chat endpoint.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} endpointGuid - Chat endpoint GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<void>}
     */
    deleteChatEndpoint(tenantGuid: string, endpointGuid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Test connectivity of a chat endpoint.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} endpointGuid - Chat endpoint GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<ChatEndpointTestResult>} - Connectivity test result ({ Reachable, Models, ModelExists, Error, RuntimeMs }).
     */
    testChatEndpoint(tenantGuid: string, endpointGuid: string, cancellationToken?: AbortController): Promise<ChatEndpointTestResult>;
    /**
     * Read health status for a specific chat endpoint.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} endpointGuid - Chat endpoint GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<ChatEndpointHealth>} - Health status for the endpoint.
     */
    readChatEndpointHealth(tenantGuid: string, endpointGuid: string, cancellationToken?: AbortController): Promise<ChatEndpointHealth>;
    /**
     * Read health status for all chat endpoints as a paginated enumeration result.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options ({ maxKeys, skip, order, token }) or cancellation token.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are ChatEndpointHealth instances.
     */
    readAllChatEndpointHealth(tenantGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Read the model catalog as a paginated enumeration result: active chat endpoints projected as model summaries.
     * Does not require administrator privileges.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options ({ maxKeys, skip, order, token }) or cancellation token.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are ChatModelSummary instances ({ GUID, Name, Model, Provider, EndpointType, IsDefault }).
     */
    readChatModels(tenantGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Create a chat thread. The caller becomes the thread owner; requires a user principal.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {Object} [thread] - Optional thread payload.
     * @param {string} [thread.GraphGUID] - Optional graph GUID to bind the thread to.
     * @param {string} [thread.Title] - Optional thread title.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<ChatThread>} - The created chat thread.
     */
    createChatThread(tenantGuid: string, thread?: {
        GraphGUID?: string;
        Title?: string;
    }, cancellationToken?: AbortController): Promise<ChatThread>;
    /**
     * Read chat threads owned by the caller as a paginated enumeration result, or all users' threads when allUsers is true (admin only).
     * @param {string} tenantGuid - Tenant GUID.
     * @param {boolean} [allUsers=false] - When true, list every user's threads (requires administrator privileges).
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options ({ maxKeys, skip, order, token }) or cancellation token.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are ChatThread instances.
     */
    readChatThreads(tenantGuid: string, allUsers?: boolean, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Read a specific chat thread.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} threadGuid - Chat thread GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<ChatThread>} - The requested chat thread.
     */
    readChatThread(tenantGuid: string, threadGuid: string, cancellationToken?: AbortController): Promise<ChatThread>;
    /**
     * Update (rename) a chat thread. Only the Title property is honored and it must be non-empty.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} threadGuid - Chat thread GUID.
     * @param {Object} thread - Thread payload.
     * @param {string} thread.Title - New thread title.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<ChatThread>} - The updated chat thread.
     */
    updateChatThread(tenantGuid: string, threadGuid: string, thread: {
        Title: string;
    }, cancellationToken?: AbortController): Promise<ChatThread>;
    /**
     * Delete a chat thread along with its turns and feedback.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} threadGuid - Chat thread GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<void>}
     */
    deleteChatThread(tenantGuid: string, threadGuid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Read the turns of a chat thread as a paginated enumeration result, ascending by sequence.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} threadGuid - Chat thread GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options ({ maxKeys, skip, order, token }) or cancellation token.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are ChatTurn instances.
     */
    readChatThreadTurns(tenantGuid: string, threadGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Execute a non-streaming chat completion. Requires a user principal.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {Object} request - Chat completion request.
     * @param {string} [request.ThreadGUID] - Optional thread GUID; a new thread is created when omitted.
     * @param {string} [request.GraphGUID] - Optional graph GUID for RAG and tool scope.
     * @param {string} request.Message - The user's message.
     * @param {string} [request.CompletionEndpointGUID] - Optional completion endpoint GUID (defaults to tenant settings).
     * @param {string} [request.EmbeddingEndpointGUID] - Optional embedding endpoint GUID (defaults to tenant settings).
     * @param {number} [request.Temperature] - Optional sampling temperature.
     * @param {number} [request.MaxOutputTokens] - Optional maximum output tokens.
     * @param {boolean} [request.EnableTools] - Optional tool use override.
     * @param {boolean} [request.EnableRag] - Optional RAG override.
     * @param {number} [request.RagTopK] - Optional RAG top-K override.
     * @param {string} [request.SystemPrompt] - Optional system prompt override.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<ChatCompletionResult>} - The chat completion result.
     */
    chatCompletion(tenantGuid: string, request: {
        ThreadGUID?: string;
        GraphGUID?: string;
        Message: string;
        CompletionEndpointGUID?: string;
        EmbeddingEndpointGUID?: string;
        Temperature?: number;
        MaxOutputTokens?: number;
        EnableTools?: boolean;
        EnableRag?: boolean;
        RagTopK?: number;
        SystemPrompt?: string;
    }, cancellationToken?: AbortController): Promise<ChatCompletionResult>;
    /**
     * Execute a streaming chat completion. Requires a user principal.
     * Returns an async generator that yields parsed SSE event objects as they arrive. Each event
     * carries an `event` discriminator: started, delta, thinking, retrieval, tool_call, tool_result,
     * usage, or error. Iteration completes when the server sends the final `[DONE]` frame.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {Object} request - Chat completion request (see {@link LiteGraphSdk#chatCompletion}).
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {AsyncGenerator<Object>} - Yields parsed streaming event objects.
     */
    chatCompletionStreaming(tenantGuid: string, request: any, cancellationToken?: AbortController): AsyncGenerator<any>;
    /**
     * Submit feedback for a chat turn. Requires a user principal.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} turnGuid - Chat turn GUID.
     * @param {Object} feedback - Feedback payload.
     * @param {string} feedback.Rating - Rating: ThumbsUp or ThumbsDown.
     * @param {string} [feedback.FeedbackText] - Optional free-form feedback text.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<ChatFeedback>} - The created feedback.
     */
    submitChatFeedback(tenantGuid: string, turnGuid: string, feedback: {
        Rating: string;
        FeedbackText?: string;
    }, cancellationToken?: AbortController): Promise<ChatFeedback>;
    /**
     * Read chat feedback for the tenant as a paginated enumeration result. Requires tenant administrator privileges.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {Object|AbortController} [optionsOrCancellationToken] - Optional pagination options ({ maxKeys, skip, order, token }) or cancellation token.
     * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
     * @returns {Promise<EnumerationResult>} - Enumeration result whose Objects are ChatFeedback instances.
     */
    readAllChatFeedback(tenantGuid: string, optionsOrCancellationToken?: any | AbortController, cancellationToken?: AbortController): Promise<EnumerationResult>;
    /**
     * Read a specific chat feedback record. Requires tenant administrator privileges.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} feedbackGuid - Chat feedback GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<ChatFeedback>} - The requested feedback record.
     */
    readChatFeedback(tenantGuid: string, feedbackGuid: string, cancellationToken?: AbortController): Promise<ChatFeedback>;
    /**
     * Delete a chat feedback record. Requires tenant administrator privileges.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {string} feedbackGuid - Chat feedback GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<void>}
     */
    deleteChatFeedback(tenantGuid: string, feedbackGuid: string, cancellationToken?: AbortController): Promise<void>;
    /**
     * Read tenant chat settings. Returns defaults when no settings record exists.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<ChatSettings>} - The tenant chat settings.
     */
    readChatSettings(tenantGuid: string, cancellationToken?: AbortController): Promise<ChatSettings>;
    /**
     * Upsert tenant chat settings. Requires tenant administrator privileges.
     * @param {string} tenantGuid - Tenant GUID.
     * @param {Object} settings - Chat settings payload.
     * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
     * @returns {Promise<ChatSettings>} - The updated chat settings.
     */
    updateChatSettings(tenantGuid: string, settings: any, cancellationToken?: AbortController): Promise<ChatSettings>;
}
import SdkBase from './SdkBase';
import { VectorMetadata } from '../models/VectorMetadata';
import Graph from '../models/Graph';
import EnumerationResult from '../models/EnumerationResult';
import SearchResult from '../models/SearchResult';
import EdgeBetween from '../models/EdgeBetween';
import GraphTransactionBuilder from '../models/GraphTransactionBuilder';
import TransactionResult from '../models/TransactionResult';
import GraphQueryResult from '../models/GraphQueryResult';
import { AuthorizationRole } from '../models/AuthorizationModels';
import { UserRoleAssignment } from '../models/AuthorizationModels';
import { AuthorizationEffectivePermissionsResult } from '../models/AuthorizationModels';
import { CredentialScopeAssignment } from '../models/AuthorizationModels';
import Node from '../models/Node';
import Edge from '../models/Edge';
import RouteResult from '../models/RouteResult';
import TenantMetaData from '../models/TenantMetaData';
import UserMetadata from '../models/UserMetadata';
import CredentialMetadata from '../models/CredentialMetadata';
import TagMetaData from '../models/TagMetaData';
import LabelMetadata from '../models/LabelMetadata';
import Token from '../models/Token';
import ChatEndpoint from '../models/ChatEndpoint';
import ChatEndpointTestResult from '../models/ChatEndpointTestResult';
import ChatEndpointHealth from '../models/ChatEndpointHealth';
import ChatThread from '../models/ChatThread';
import ChatCompletionResult from '../models/ChatCompletionResult';
import ChatFeedback from '../models/ChatFeedback';
import ChatSettings from '../models/ChatSettings';
