import Graph from '../models/Graph';
import SdkBase from './SdkBase';
import GenericExceptionHandlers from '../exception/GenericExceptionHandlers';
import Node from '../models/Node';
import Edge from '../models/Edge';
import SearchResult from '../models/SearchResult';
import RouteResult from '../models/RouteResult';
import ExistenceResult from '../models/ExistenceResult';
import EdgeBetween from '../models/EdgeBetween';
import UserMetadata from '../models/UserMetadata';
import CredentialMetadata from '../models/CredentialMetadata';
import TagMetaData from '../models/TagMetaData';
import LabelMetadata from '../models/LabelMetadata';
import TenantMetaData from '../models/TenantMetaData';
import { VectorMetadata } from '../models/VectorMetadata';
import Token from '../models/Token';
import { VectorSearchResult } from '../models/VectorSearchResult';
import GraphTransactionBuilder from '../models/GraphTransactionBuilder';
import TransactionResult from '../models/TransactionResult';
import GraphQueryResult from '../models/GraphQueryResult';
import {
  AuthorizationEffectivePermissionsResult,
  AuthorizationRole,
  AuthorizationRoleSearchResult,
  CredentialScopeAssignment,
  CredentialScopeAssignmentSearchResult,
  UserRoleAssignment,
  UserRoleAssignmentSearchResult,
} from '../models/AuthorizationModels';
import ChatEndpoint from '../models/ChatEndpoint';
import ChatThread from '../models/ChatThread';
import ChatTurn from '../models/ChatTurn';
import ChatFeedback from '../models/ChatFeedback';
import ChatSettings from '../models/ChatSettings';
import ChatEndpointHealth from '../models/ChatEndpointHealth';
import ChatEndpointTestResult from '../models/ChatEndpointTestResult';
import ChatCompletionResult from '../models/ChatCompletionResult';

const buildQueryString = (params = {}) => {
  const entries = Object.entries(params).filter(([, value]) => value !== undefined && value !== null && value !== '');
  if (entries.length === 0) return '';
  return `?${entries.map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`).join('&')}`;
};

const normalizeBulkCreateArgs = (optionsOrCancellationToken, cancellationToken) => {
  if (
    optionsOrCancellationToken &&
    typeof optionsOrCancellationToken === 'object' &&
    (Object.prototype.hasOwnProperty.call(optionsOrCancellationToken, 'returnMode') ||
      Object.prototype.hasOwnProperty.call(optionsOrCancellationToken, 'return') ||
      Object.prototype.hasOwnProperty.call(optionsOrCancellationToken, 'cancellationToken'))
  ) {
    return {
      returnMode: optionsOrCancellationToken.returnMode || optionsOrCancellationToken.return,
      cancellationToken: cancellationToken || optionsOrCancellationToken.cancellationToken,
    };
  }

  return {
    returnMode: undefined,
    cancellationToken: optionsOrCancellationToken,
  };
};

const normalizeBulkReturnMode = (returnMode) => {
  if (returnMode === undefined || returnMode === null || returnMode === '') return undefined;
  const normalized = String(returnMode).toLowerCase();
  if (normalized !== 'full' && normalized !== 'minimal') {
    throw new Error("returnMode must be 'full' or 'minimal'");
  }
  return normalized;
};

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
  constructor(endpoint = 'http://localhost:8701/', tenantGuid, accessKey) {
    super(endpoint, tenantGuid, accessKey);
  }

  //region Graph-Routes

  /**
   * Check if a graph exists by GUID.
   * @param {string} guid - The GUID of the graph.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<boolean>} - True if the graph exists.
   */
  async graphExists(guid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${guid}`;
    return await this.head(url, cancellationToken);
  }

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
  async createGraph(graph, cancellationToken) {
    if (!graph) {
      GenericExceptionHandlers.ArgumentNullException('Graph');
    }
    const url = `${this.endpoint}v1.0/tenants/${this.tenantGuid}/graphs`;
    return await this.putCreate(url, graph, Graph, cancellationToken);
  }

  /**
   * Read all graphs.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Graph[]>} - An array of graphs.
   */
  async readGraphs(cancellationToken) {
    const url = `${this.endpoint}v1.0/tenants/${this.tenantGuid}/graphs`;
    return await this.getMany(url, Graph, cancellationToken);
  }

  /**
   * Search graphs.
   * @param {Object} searchReq - Information about the search request.
   * @param {string} searchReq.GraphGUID - Globally unique identifier for the graph (defaults to an empty GUID).
   * @param {string} searchReq.Ordering - Ordering of the search results (default is CreatedDescending).
   * @param {Object} searchReq.Expr - Expression used for the search (default is null).
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<SearchResult>} - The search result.
   */
  async searchGraphs(searchReq, cancellationToken) {
    if (!searchReq) {
      GenericExceptionHandlers.ArgumentNullException('Search Request');
    }
    const url = `${this.endpoint}v1.0/tenants/${this.tenantGuid}/graphs/search`;
    const json = JSON.stringify(searchReq);
    const response = await this.post(url, json, SearchResult, cancellationToken);

    return response;
  }

  /**
   * Read a specific graph.
   * @param {string} guid - The GUID of the graph.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Graph>} - The requested graph.
   */
  async readGraph(guid, cancellationToken) {
    const url = `${this.endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${guid}`;
    return await this.get(url, Graph, cancellationToken);
  }

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
  async updateGraph(graph, cancellationToken) {
    if (!graph) {
      GenericExceptionHandlers.ArgumentNullException('Graph');
    }
    const url = `${this.endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graph.GUID}`;
    return await this.putUpdate(url, graph, Graph, cancellationToken);
  }

  /**
   * Delete a graph.
   * @param {string} guid - The GUID of the graph.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @param {boolean} force - Force recursive deletion of edges and nodes.
   */
  async deleteGraph(guid, force = false, cancellationToken) {
    let url = `${this.endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${guid}`;
    if (force) url += '?force=true';
    await this.delete(url, cancellationToken);
  }

  /**
   * Export a graph to GEXF format.
   * @param {string} guid - The GUID of the graph.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<string>} - The GEXF XML data.
   */
  async exportGraphToGexf(guid, cancellationToken) {
    const url = `${this.endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${guid}/export/gexf`;
    const bytes = await this.getDataInBytes(url, cancellationToken);
    // return bytes ? new util.TextDecoder('utf-8').decode(bytes) : null;
    return bytes;
  }

  /**
   * Export an entire graph to JSONL format.
   * @param {string} graphGuid - The GUID of the graph.
   * @param {Object} [options] - Export options.
   * @param {boolean} [options.includeData=false] - Include object data for graph, nodes, and edges.
   * @param {boolean} [options.includeSubordinates=false] - Include subordinate labels, tags, and vectors.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<string>} - The JSONL data.
   */
  async exportGraphToJsonl(graphGuid, { includeData = false, includeSubordinates = false } = {}, cancellationToken) {
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('GraphGuid');
    }
    const flags = [];
    if (includeData) flags.push('incldata');
    if (includeSubordinates) flags.push('inclsub');
    const query = flags.length > 0 ? `?${flags.join('&')}` : '';
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/export/jsonl${query}`;
    return await this.getDataInBytes(url, cancellationToken);
  }

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
  async exportSubgraphToJsonl(graphGuid, subgraphExtractionRequest, cancellationToken) {
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('GraphGuid');
    }
    if (!subgraphExtractionRequest) {
      GenericExceptionHandlers.ArgumentNullException('SubgraphExtractionRequest');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/export/jsonl`;
    const json = JSON.stringify(subgraphExtractionRequest);
    return await this.postForText(url, json, 'application/json', cancellationToken);
  }

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
  async importGraphFromJsonl(graphGuid, jsonlString, { guidStrategy, onError, batchSize } = {}, cancellationToken) {
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('GraphGuid');
    }
    if (!jsonlString) {
      GenericExceptionHandlers.ArgumentNullException('JsonlString');
    }
    const query = buildQueryString({ guidstrategy: guidStrategy, onerror: onError, batchsize: batchSize });
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/import/jsonl${query}`;
    return await this.postRawForJson(url, jsonlString, 'application/x-ndjson', cancellationToken);
  }

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
  async importGraphAsNewFromJsonl(jsonlString, { guidStrategy, onError, batchSize } = {}, cancellationToken) {
    if (!jsonlString) {
      GenericExceptionHandlers.ArgumentNullException('JsonlString');
    }
    const query = buildQueryString({ guidstrategy: guidStrategy, onerror: onError, batchsize: batchSize });
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/import/jsonl${query}`;
    return await this.postRawForJson(url, jsonlString, 'application/x-ndjson', cancellationToken);
  }
  // endregion

  //region Batch
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
  async batchExistence(graphGuid, existenceRequest, cancellationToken) {
    if (!existenceRequest) {
      GenericExceptionHandlers.ArgumentNullException('existenceRequest');
    }

    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/existence`;
    return await this.post(url, existenceRequest, ExistenceResult, cancellationToken);
  }

  //endreagion

  //region Transaction Routes

  /**
   * Create a graph-scoped transaction builder.
   * @param {string} graphGuid - The GUID of the graph.
   * @param {Object} [options] - Transaction defaults.
   * @param {number} [options.MaxOperations=1000] - Maximum operation count.
   * @param {number} [options.TimeoutSeconds=60] - Transaction timeout in seconds.
   * @param {string} [options.IsolationLevel=Default] - Transaction isolation level.
   * @returns {GraphTransactionBuilder} - Transaction builder.
   */
  transaction(graphGuid, options = {}) {
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('GraphGuid');
    }
    return new GraphTransactionBuilder(this, graphGuid, options);
  }

  /**
   * Execute a graph-scoped transaction.
   * @param {string} graphGuid - The GUID of the graph.
   * @param {Object} request - Transaction request.
   * @param {Array<Object>} request.Operations - Operations to execute atomically.
   * @param {string} [request.IsolationLevel=Default] - Transaction isolation level.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<TransactionResult>} - Transaction result.
   */
  async executeTransaction(graphGuid, request, cancellationToken) {
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('GraphGuid');
    }
    if (!request) {
      GenericExceptionHandlers.ArgumentNullException('TransactionRequest');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/transaction`;
    return await this.post(url, request, TransactionResult, cancellationToken, [400, 409]);
  }

  //end region

  //region Query Routes

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
  queryRequest(query, parameters = {}, options = {}) {
    if (!query) {
      GenericExceptionHandlers.ArgumentNullException('Query');
    }

    return {
      Query: query,
      Parameters: parameters || {},
      MaxResults: options.MaxResults || options.maxResults || 1000,
      TimeoutSeconds: options.TimeoutSeconds || options.timeoutSeconds || 30,
      IncludeProfile: options.IncludeProfile || options.includeProfile || false,
    };
  }

  /**
   * Execute a native graph query.
   * @param {string} graphGuid - The GUID of the graph.
   * @param {Object|string} request - Query request or query text.
   * @param {Object} [parameters] - Query parameters when request is query text.
   * @param {Object} [options] - Query execution options.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<GraphQueryResult>} - Query result.
   */
  async executeQuery(graphGuid, request, parameters = {}, options = {}, cancellationToken) {
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('GraphGuid');
    }
    if (!request) {
      GenericExceptionHandlers.ArgumentNullException('QueryRequest');
    }

    const payload = typeof request === 'string' ? this.queryRequest(request, parameters, options) : request;
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/query`;
    return await this.post(url, payload, GraphQueryResult, cancellationToken);
  }

  //end region

  //region Authorization Routes

  /**
   * List authorization roles for the configured tenant.
   * @param {Object} [options] - Role list options.
   * @param {number} [options.page=0] - Page index.
   * @param {number} [options.pageSize=1000] - Page size.
   * @param {boolean} [options.includeBuiltIns=true] - Include built-in roles.
   * @param {boolean} [options.builtIn] - Filter by built-in status.
   * @param {AbortController} [cancellationToken] - Optional cancellation token.
   * @returns {Promise<AuthorizationRoleSearchResult>} - Role search result.
   */
  async listAuthorizationRoles(options = {}, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/roles${buildQueryString({
      page: options.page ?? 0,
      pageSize: options.pageSize ?? 1000,
      includeBuiltIns: options.includeBuiltIns ?? true,
      builtIn: options.builtIn,
      name: options.name,
      resourceScope: options.resourceScope,
      permission: options.permission,
      resourceType: options.resourceType,
    })}`;
    return await this.get(url, AuthorizationRoleSearchResult, cancellationToken);
  }

  /**
   * Create an authorization role.
   * @param {Object} role - Role payload.
   * @param {AbortController} [cancellationToken] - Optional cancellation token.
   * @returns {Promise<AuthorizationRole>} - Created role.
   */
  async createAuthorizationRole(role, cancellationToken) {
    if (!role) {
      GenericExceptionHandlers.ArgumentNullException('AuthorizationRole');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/roles`;
    return await this.putCreate(url, role, AuthorizationRole, cancellationToken);
  }

  /**
   * Read an authorization role.
   * @param {string} roleGuid - Role GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token.
   * @returns {Promise<AuthorizationRole>} - Role.
   */
  async readAuthorizationRole(roleGuid, cancellationToken) {
    if (!roleGuid) {
      GenericExceptionHandlers.ArgumentNullException('RoleGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/roles/${roleGuid}`;
    return await this.get(url, AuthorizationRole, cancellationToken);
  }

  /**
   * Update an authorization role.
   * @param {Object} role - Role payload containing GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token.
   * @returns {Promise<AuthorizationRole>} - Updated role.
   */
  async updateAuthorizationRole(role, cancellationToken) {
    if (!role) {
      GenericExceptionHandlers.ArgumentNullException('AuthorizationRole');
    }
    if (!role.GUID) {
      GenericExceptionHandlers.ArgumentNullException('RoleGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/roles/${role.GUID}`;
    return await this.putUpdate(url, role, AuthorizationRole, cancellationToken);
  }

  /**
   * Delete an authorization role.
   * @param {string} roleGuid - Role GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token.
   * @returns {Promise<void>}
   */
  async deleteAuthorizationRole(roleGuid, cancellationToken) {
    if (!roleGuid) {
      GenericExceptionHandlers.ArgumentNullException('RoleGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/roles/${roleGuid}`;
    await this.delete(url, cancellationToken);
  }

  /**
   * List user role assignments.
   * @param {string} userGuid - User GUID.
   * @param {Object} [options] - List filters.
   * @param {AbortController} [cancellationToken] - Optional cancellation token.
   * @returns {Promise<UserRoleAssignmentSearchResult>} - Assignment search result.
   */
  async listUserRoleAssignments(userGuid, options = {}, cancellationToken) {
    if (!userGuid) {
      GenericExceptionHandlers.ArgumentNullException('UserGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/users/${userGuid}/roles${buildQueryString({
      page: options.page ?? 0,
      pageSize: options.pageSize ?? 1000,
      roleName: options.roleName,
      resourceScope: options.resourceScope,
      graphGuid: options.graphGuid,
    })}`;
    return await this.get(url, UserRoleAssignmentSearchResult, cancellationToken);
  }

  /**
   * Create a user role assignment.
   * @param {string} userGuid - User GUID.
   * @param {Object} assignment - Assignment payload.
   * @param {AbortController} [cancellationToken] - Optional cancellation token.
   * @returns {Promise<UserRoleAssignment>} - Created assignment.
   */
  async createUserRoleAssignment(userGuid, assignment, cancellationToken) {
    if (!userGuid) {
      GenericExceptionHandlers.ArgumentNullException('UserGuid');
    }
    if (!assignment) {
      GenericExceptionHandlers.ArgumentNullException('UserRoleAssignment');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/users/${userGuid}/roles`;
    return await this.putCreate(url, assignment, UserRoleAssignment, cancellationToken);
  }

  /**
   * Read a user role assignment.
   * @param {string} userGuid - User GUID.
   * @param {string} assignmentGuid - Assignment GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token.
   * @returns {Promise<UserRoleAssignment>} - Assignment.
   */
  async readUserRoleAssignment(userGuid, assignmentGuid, cancellationToken) {
    if (!userGuid) {
      GenericExceptionHandlers.ArgumentNullException('UserGuid');
    }
    if (!assignmentGuid) {
      GenericExceptionHandlers.ArgumentNullException('AssignmentGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/users/${userGuid}/roles/${assignmentGuid}`;
    return await this.get(url, UserRoleAssignment, cancellationToken);
  }

  /**
   * Update a user role assignment.
   * @param {string} userGuid - User GUID.
   * @param {Object} assignment - Assignment payload containing GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token.
   * @returns {Promise<UserRoleAssignment>} - Updated assignment.
   */
  async updateUserRoleAssignment(userGuid, assignment, cancellationToken) {
    if (!userGuid) {
      GenericExceptionHandlers.ArgumentNullException('UserGuid');
    }
    if (!assignment) {
      GenericExceptionHandlers.ArgumentNullException('UserRoleAssignment');
    }
    if (!assignment.GUID) {
      GenericExceptionHandlers.ArgumentNullException('AssignmentGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/users/${userGuid}/roles/${assignment.GUID}`;
    return await this.putUpdate(url, assignment, UserRoleAssignment, cancellationToken);
  }

  /**
   * Delete a user role assignment.
   * @param {string} userGuid - User GUID.
   * @param {string} assignmentGuid - Assignment GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token.
   * @returns {Promise<void>}
   */
  async deleteUserRoleAssignment(userGuid, assignmentGuid, cancellationToken) {
    if (!userGuid) {
      GenericExceptionHandlers.ArgumentNullException('UserGuid');
    }
    if (!assignmentGuid) {
      GenericExceptionHandlers.ArgumentNullException('AssignmentGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/users/${userGuid}/roles/${assignmentGuid}`;
    await this.delete(url, cancellationToken);
  }

  /**
   * Read effective permissions for a user.
   * @param {string} userGuid - User GUID.
   * @param {string} [graphGuid] - Optional graph GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token.
   * @returns {Promise<AuthorizationEffectivePermissionsResult>} - Effective permissions.
   */
  async getUserEffectivePermissions(userGuid, graphGuid, cancellationToken) {
    if (!userGuid) {
      GenericExceptionHandlers.ArgumentNullException('UserGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/users/${userGuid}/permissions${buildQueryString({
      graphGuid,
    })}`;
    return await this.get(url, AuthorizationEffectivePermissionsResult, cancellationToken);
  }

  /**
   * List credential scope assignments.
   * @param {string} credentialGuid - Credential GUID.
   * @param {Object} [options] - List filters.
   * @param {AbortController} [cancellationToken] - Optional cancellation token.
   * @returns {Promise<CredentialScopeAssignmentSearchResult>} - Scope search result.
   */
  async listCredentialScopeAssignments(credentialGuid, options = {}, cancellationToken) {
    if (!credentialGuid) {
      GenericExceptionHandlers.ArgumentNullException('CredentialGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/credentials/${credentialGuid}/scopes${buildQueryString(
      {
        page: options.page ?? 0,
        pageSize: options.pageSize ?? 1000,
        roleName: options.roleName,
        resourceScope: options.resourceScope,
        graphGuid: options.graphGuid,
        permission: options.permission,
        resourceType: options.resourceType,
      }
    )}`;
    return await this.get(url, CredentialScopeAssignmentSearchResult, cancellationToken);
  }

  /**
   * Create a credential scope assignment.
   * @param {string} credentialGuid - Credential GUID.
   * @param {Object} assignment - Assignment payload.
   * @param {AbortController} [cancellationToken] - Optional cancellation token.
   * @returns {Promise<CredentialScopeAssignment>} - Created scope.
   */
  async createCredentialScopeAssignment(credentialGuid, assignment, cancellationToken) {
    if (!credentialGuid) {
      GenericExceptionHandlers.ArgumentNullException('CredentialGuid');
    }
    if (!assignment) {
      GenericExceptionHandlers.ArgumentNullException('CredentialScopeAssignment');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/credentials/${credentialGuid}/scopes`;
    return await this.putCreate(url, assignment, CredentialScopeAssignment, cancellationToken);
  }

  /**
   * Read a credential scope assignment.
   * @param {string} credentialGuid - Credential GUID.
   * @param {string} assignmentGuid - Assignment GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token.
   * @returns {Promise<CredentialScopeAssignment>} - Scope assignment.
   */
  async readCredentialScopeAssignment(credentialGuid, assignmentGuid, cancellationToken) {
    if (!credentialGuid) {
      GenericExceptionHandlers.ArgumentNullException('CredentialGuid');
    }
    if (!assignmentGuid) {
      GenericExceptionHandlers.ArgumentNullException('AssignmentGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/credentials/${credentialGuid}/scopes/${assignmentGuid}`;
    return await this.get(url, CredentialScopeAssignment, cancellationToken);
  }

  /**
   * Update a credential scope assignment.
   * @param {string} credentialGuid - Credential GUID.
   * @param {Object} assignment - Assignment payload containing GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token.
   * @returns {Promise<CredentialScopeAssignment>} - Updated scope.
   */
  async updateCredentialScopeAssignment(credentialGuid, assignment, cancellationToken) {
    if (!credentialGuid) {
      GenericExceptionHandlers.ArgumentNullException('CredentialGuid');
    }
    if (!assignment) {
      GenericExceptionHandlers.ArgumentNullException('CredentialScopeAssignment');
    }
    if (!assignment.GUID) {
      GenericExceptionHandlers.ArgumentNullException('AssignmentGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/credentials/${credentialGuid}/scopes/${assignment.GUID}`;
    return await this.putUpdate(url, assignment, CredentialScopeAssignment, cancellationToken);
  }

  /**
   * Delete a credential scope assignment.
   * @param {string} credentialGuid - Credential GUID.
   * @param {string} assignmentGuid - Assignment GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token.
   * @returns {Promise<void>}
   */
  async deleteCredentialScopeAssignment(credentialGuid, assignmentGuid, cancellationToken) {
    if (!credentialGuid) {
      GenericExceptionHandlers.ArgumentNullException('CredentialGuid');
    }
    if (!assignmentGuid) {
      GenericExceptionHandlers.ArgumentNullException('AssignmentGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/credentials/${credentialGuid}/scopes/${assignmentGuid}`;
    await this.delete(url, cancellationToken);
  }

  /**
   * Read effective permissions for a credential.
   * @param {string} credentialGuid - Credential GUID.
   * @param {string} [graphGuid] - Optional graph GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token.
   * @returns {Promise<AuthorizationEffectivePermissionsResult>} - Effective permissions.
   */
  async getCredentialEffectivePermissions(credentialGuid, graphGuid, cancellationToken) {
    if (!credentialGuid) {
      GenericExceptionHandlers.ArgumentNullException('CredentialGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/credentials/${credentialGuid}/permissions${buildQueryString(
      {
        graphGuid,
      }
    )}`;
    return await this.get(url, AuthorizationEffectivePermissionsResult, cancellationToken);
  }

  //end region

  // region Node-Routes

  /**
   * Check if a node exists by GUID.
   * @param {string} graphGuid - The GUID of the graph.
   * @param {string} guid - The GUID of the node.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<boolean>} - True if the node exists.
   */
  async nodeExists(graphGuid, guid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/nodes/${guid}`;
    return await this.head(url, cancellationToken);
  }

  /**
   * Create multiple nodes.
   * @param {string} graphGuid - The GUID of the graph.
   * @param {Array<Object>} nodes - List of node objects.
   * @param {Object|AbortController} [optionsOrCancellationToken] - Optional return mode options or cancellation token.
   * @param {string} [optionsOrCancellationToken.returnMode] - Optional bulk create return mode: full or minimal.
   * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
   * @returns {Promise<Array<Node>>} - The list of created nodes.
   */
  async createNodes(graphGuid, nodes, optionsOrCancellationToken, cancellationToken) {
    if (!nodes) {
      GenericExceptionHandlers.ArgumentNullException('Nodes');
    }
    if (nodes.length < 1) return [];

    const args = normalizeBulkCreateArgs(optionsOrCancellationToken, cancellationToken);
    const query = buildQueryString({ return: normalizeBulkReturnMode(args.returnMode) });
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/nodes/bulk${query}`;
    return await this.putCreate(url, nodes, Node, args.cancellationToken);
  }

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
  async createNode(node, cancellationToken) {
    if (!node) {
      GenericExceptionHandlers.ArgumentNullException('Node');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${node.GraphGUID}/nodes`;
    return await this.putCreate(url, node, Node, cancellationToken);
  }

  /**
   * Read nodes for a specific graph.
   * @param {string} graphGuid - The GUID of the graph.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Node[]>} - An array of nodes.
   */
  async readNodes(graphGuid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/nodes`;
    return await this.getMany(url, Node, cancellationToken);
  }

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
  async searchNodes(graphGuid, searchReq, cancellationToken) {
    if (!searchReq) {
      GenericExceptionHandlers.ArgumentNullException('Search Request');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/nodes/search`;
    const json = JSON.stringify(searchReq);
    const response = await this.post(url, json, SearchResult, cancellationToken);
    return response;
  }

  /**
   * Read a specific node.
   * @param {string} graphGuid - The GUID of the graph.
   * @param {string} nodeGuid - The GUID of the node.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Node>} - The requested node.
   */
  async readNode(graphGuid, nodeGuid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/nodes/${nodeGuid}`;
    return await this.get(url, Node, cancellationToken);
  }

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
  async updateNode(node, cancellationToken) {
    if (!node) {
      GenericExceptionHandlers.ArgumentNullException('Node');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${node.GraphGUID}/nodes/${node.GUID}`;
    return await this.putUpdate(url, node, Node, cancellationToken);
  }

  /**
   * Delete a node.
   * @param {string} graphGuid - The GUID of the graph.
   * @param {string} nodeGuid - The GUID of the node.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   */
  async deleteNode(graphGuid, nodeGuid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/nodes/${nodeGuid}`;
    await this.delete(url, cancellationToken);
  }

  /**
   * Delete all nodes within a graph.
   * @param {string} graphGuid - The GUID of the graph.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   */
  async deleteNodes(graphGuid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/nodes/all`;
    await this.delete(url, cancellationToken);
  }

  /**
   * Delete multiple nodes within a graph.
   * @param {string} graphGuid - The GUID of the graph.
   * @param {Array<string>} nodeGuids - The list of node GUIDs to delete.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   */
  async deleteMultipleNodes(graphGuid, nodeGuids, cancellationToken) {
    if (!nodeGuids) {
      GenericExceptionHandlers.ArgumentNullException('nodeGuids');
    }
    if (nodeGuids.length < 1) return [];
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/nodes/multiple`;
    await this.deleteMany(url, nodeGuids, cancellationToken);
  }

  // endregion

  // region Edge Routes

  /**
   * Check if an edge exists by GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} guid - Edge GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<boolean>} - True if exists.
   */
  async edgeExists(graphGuid, guid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/edges/${guid}`;
    return await this.head(url, cancellationToken);
  }

  /**
   * Create multiple edges.
   * @param {string} graphGuid - The GUID of the graph.
   * @param {Array<Object>} edges - List of edge objects.
   * @param {Object|AbortController} [optionsOrCancellationToken] - Optional return mode options or cancellation token.
   * @param {string} [optionsOrCancellationToken.returnMode] - Optional bulk create return mode: full or minimal.
   * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
   * @returns {Promise<Array<Object>>} - The list of created edges.
   */
  async createEdges(graphGuid, edges, optionsOrCancellationToken, cancellationToken) {
    if (!edges) {
      GenericExceptionHandlers.ArgumentNullException('Edges');
    }
    if (edges.length < 1) return [];

    const args = normalizeBulkCreateArgs(optionsOrCancellationToken, cancellationToken);
    const query = buildQueryString({ return: normalizeBulkReturnMode(args.returnMode) });
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/edges/bulk${query}`;
    return await this.putCreate(url, edges, Edge, args.cancellationToken);
  }

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
  async createEdge(edge, cancellationToken) {
    if (!edge) {
      GenericExceptionHandlers.ArgumentNullException('edge');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${edge.GraphGUID}/edges`;
    return await this.putCreate(url, edge, Edge, cancellationToken);
  }

  /**
   * Read edges.
   * @param {string} graphGuid - Graph GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Edge[]>} - List of edges.
   */
  async readEdges(graphGuid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/edges`;
    return await this.getMany(url, Edge, cancellationToken);
  }

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
  async searchEdges(graphGuid, searchReq, cancellationToken) {
    if (!searchReq) {
      GenericExceptionHandlers.ArgumentNullException('searchReq');
    }

    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/edges/search`;
    const body = JSON.stringify(searchReq);

    const response = await this.post(url, body, SearchResult, cancellationToken);

    return response;
  }

  /**
   * Read an edge.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} edgeGuid - Edge GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Edge>} - The requested edge.
   */
  async readEdge(graphGuid, edgeGuid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/edges/${edgeGuid}`;
    return await this.get(url, Edge, cancellationToken);
  }

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
  async updateEdge(edge, cancellationToken) {
    if (!edge) {
      GenericExceptionHandlers.ArgumentNullException('Edge');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${edge.GraphGUID}/edges/${edge.GUID}`;
    return await this.putUpdate(url, edge, Edge, cancellationToken);
  }

  /**
   * Delete an edge.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} edgeGuid - Edge GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<void>} - Promise representing the completion of the deletion.
   */
  async deleteEdge(graphGuid, edgeGuid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/edges/${edgeGuid}`;
    await this.delete(url, cancellationToken);
  }

  /**
   * Delete all edges within a graph.
   * @param {string} graphGuid - The GUID of the graph.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   */
  async deleteEdges(graphGuid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/edges/all`;
    await this.delete(url, cancellationToken);
  }

  /**
   * Delete multiple edges within a graph.
   * @param {string} graphGuid - The GUID of the graph.
   * @param {Array<string>} edgeGuids - The list of edge GUIDs to delete.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   */
  async deleteMultipleEdges(graphGuid, edgeGuids, cancellationToken) {
    if (!edgeGuids) {
      GenericExceptionHandlers.ArgumentNullException('edgeGuids');
    }
    if (edgeGuids.length < 1) return [];
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/edges/multiple`;
    await this.deleteMany(url, edgeGuids, cancellationToken);
  }

  //end region

  //region Routes and Traversal

  /**
   * Get edges from a node.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} nodeGuid - Node GUID.
   * @param {AbortSignal} [cancellationToken] - Abort signal for cancellation.
   * @returns {Promise<Edge[]>} - Edges.
   */
  async getEdgesFromNode(graphGuid, nodeGuid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/nodes/${nodeGuid}/edges/from`;
    return await this.getMany(url, Edge, cancellationToken);
  }

  /**
   * Get edges to a node.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} nodeGuid - Node GUID.
   * @param {AbortSignal} [cancellationToken] - Abort signal for cancellation.
   * @returns {Promise<Edge[]>} - Edges.
   */
  async getEdgesToNode(graphGuid, nodeGuid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/nodes/${nodeGuid}/edges/to`;
    return await this.getMany(url, Edge, cancellationToken);
  }

  /**
   * Get edges from a given node to a given node.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} fromNodeGuid - From node GUID.
   * @param {string} toNodeGuid - To node GUID.
   * @param {AbortSignal} [cancellationToken] - Abort signal for cancellation.
   * @returns {Promise<Edge[]>} - Edges.
   */
  async getEdgesBetween(graphGuid, fromNodeGuid, toNodeGuid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/edges/between?from=${fromNodeGuid}&to=${toNodeGuid}`;
    return await this.getMany(url, Edge, cancellationToken);
  }

  /**
   * Get all edges to or from a node.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} nodeGuid - Node GUID.
   * @param {AbortSignal} [cancellationToken] - Abort signal for cancellation.
   * @returns {Promise<Edge[]>} - Edges.
   */
  async getAllNodeEdges(graphGuid, nodeGuid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/nodes/${nodeGuid}/edges`;
    return await this.getMany(url, Edge, cancellationToken);
  }

  /**
   * Get child nodes from a node.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} nodeGuid - Node GUID.
   * @param {AbortSignal} [cancellationToken] - Abort signal for cancellation.
   * @returns {Promise<Node[]>} - Child nodes.
   */
  async getChildrenFromNode(graphGuid, nodeGuid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/nodes/${nodeGuid}/children`;
    return await this.getMany(url, Node, cancellationToken);
  }

  /**
   * Get parent nodes from a node.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} nodeGuid - Node GUID.
   * @param {AbortSignal} [cancellationToken] - Abort signal for cancellation.
   * @returns {Promise<Node[]>} - Parent nodes.
   */
  async getParentsFromNode(graphGuid, nodeGuid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/nodes/${nodeGuid}/parents`;
    return await this.getMany(url, Node, cancellationToken);
  }

  /**
   * Get neighboring nodes from a node.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} nodeGuid - Node GUID.
   * @param {AbortSignal} [cancellationToken] - Abort signal for cancellation.
   * @returns {Promise<Node[]>} - Neighboring nodes.
   */
  async getNodeNeighbors(graphGuid, nodeGuid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/nodes/${nodeGuid}/neighbors`;
    return await this.getMany(url, Node, cancellationToken);
  }

  /**
   * Get routes between two nodes.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} fromNodeGuid - From node GUID.
   * @param {string} toNodeGuid - To node GUID.
   * @param {AbortSignal} [cancellationToken] - Abort signal for cancellation.
   * @returns {Promise<RouteResult>} - Routes.
   */
  async getRoutes(graphGuid, fromNodeGuid, toNodeGuid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/routes`;

    const req = {
      Graph: graphGuid,
      From: fromNodeGuid,
      To: toNodeGuid,
    };
    const response = await this.post(url, JSON.stringify(req), RouteResult, cancellationToken);
    return response;
  }

  //end region

  //region Tenants

  /**
   * Read all tenants.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<TenantMetaData[]>} - An array of tenants.
   */
  async readTenants(cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants`;
    return await this.getMany(url, TenantMetaData, cancellationToken);
  }

  /**
   * Read a tenant.
   * @param {string} tenantGuid - The GUID of the tenant.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<TenantMetaData>} - The tenant.
   */
  async readTenant(tenantGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}`;
    return await this.get(url, TenantMetaData, cancellationToken);
  }

  /**
   * Create a tenant.
   * @param {TenantMetaData} tenant - The tenant to create.
   * @param {String} tenant.name - The name of the tenant.
   * @param {boolean} tenant.Active - Indicates if tenant is active.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<TenantMetaData>} - The created tenant.
   */
  async createTenant(tenant, cancellationToken) {
    if (!tenant) {
      GenericExceptionHandlers.ArgumentNullException('tenant');
    }
    const url = `${this._endpoint}v1.0/tenants`;
    return await this.putCreate(url, tenant, TenantMetaData, cancellationToken);
  }

  /**
   * Update a tenant.
   * @param {TenantMetaData} tenant - The tenant to update.
   * @param {String} tenant.name - The name of the tenant.
   * @param {boolean} tenant.Active - Indicates if tenant is active.
   * @param {string} guid - The GUID of the tenant.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<TenantMetaData>} - The updated tenant.
   */
  async updateTenant(tenant, guid, cancellationToken) {
    if (!guid) {
      GenericExceptionHandlers.ArgumentNullException('guid');
    }
    if (!tenant) {
      GenericExceptionHandlers.ArgumentNullException('tenant');
    }
    const url = `${this._endpoint}v1.0/tenants/${guid}`;
    return await this.putUpdate(url, tenant, TenantMetaData, cancellationToken);
  }

  /**
   * Delete a tenant.
   * @param {string} tenantGuid - The GUID of the tenant.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Boolean>}
   */
  async deleteTenant(tenantGuid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}`;
    return await this.delete(url, cancellationToken);
  }

  /**
   * Tenant exists.
   * @param {string} tenantGuid - The GUID of the tenant.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<boolean>}
   */
  async tenantExists(tenantGuid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}`;
    return await this.head(url, cancellationToken);
  }

  /**
   * Tenant delete force.
   * @param {string} tenantGuid - The GUID of the tenant.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Boolean>}
   */
  async tenantDeleteForce(tenantGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}?force`;
    return await this.delete(url, cancellationToken);
  }

  //end region

  //region Users

  /**
   * Read all users.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<UserMetadata[]>} - An array of users.
   */
  async readAllUsers(cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/users`;
    return await this.getMany(url, UserMetadata, cancellationToken);
  }

  /**
   * Read a user.
   * @param {string} userGuid - The GUID of the user.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<UserMetadata>} - The user.
   */
  async readUser(userGuid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/users/${userGuid}`;
    return await this.get(url, UserMetadata, cancellationToken);
  }

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
  async createUser(user, cancellationToken) {
    if (!user) {
      GenericExceptionHandlers.ArgumentNullException('user');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/users`;
    return await this.putCreate(url, user, UserMetadata, cancellationToken);
  }

  /**
   * User exists.
   * @param {string} guid - The GUID of the user.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<boolean>}
   */
  async existsUser(guid, cancellationToken) {
    if (!guid) {
      GenericExceptionHandlers.ArgumentNullException('guid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/users/${guid}`;
    return await this.head(url, cancellationToken);
  }

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
  async updateUser(user, guid, cancellationToken) {
    if (!guid) {
      GenericExceptionHandlers.ArgumentNullException('guid');
    }
    if (!user) {
      GenericExceptionHandlers.ArgumentNullException('user');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/users/${guid}`;
    return await this.putUpdate(url, user, UserMetadata, cancellationToken);
  }

  /**
   * Delete a user.
   * @param {string} guid - The GUID of the user.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Boolean>}
   */
  async deleteUser(guid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/users/${guid}`;
    return await this.delete(url, cancellationToken);
  }
  //end region

  //region Credentials

  /**
   * Read all credentials.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<CredentialMetadata[]>} - An array of credentials.
   */
  async readAllCredentials(cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/credentials`;
    return await this.getMany(url, CredentialMetadata, cancellationToken);
  }

  /**
   * Read a credential.
   * @param {string} guid - The GUID of the credential.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<CredentialMetadata>} - The credential.
   */
  async readCredential(guid, cancellationToken) {
    if (!guid) {
      GenericExceptionHandlers.ArgumentNullException('guid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/credentials/${guid}`;
    return await this.get(url, CredentialMetadata, cancellationToken);
  }

  /**
   * Create a credential.
   * @param {CredentialMetadata} credential - The credential to create.
   * @param {string} credential.Name - The name of the credential.
   * @param {string} credential.BearerToken - The bearer token of the credential.
   * @param {boolean} credential.Active - Indicates if credential is active.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<CredentialMetadata>} - The created credential.
   */
  async createCredential(credential, cancellationToken) {
    if (!credential) {
      GenericExceptionHandlers.ArgumentNullException('credential');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/credentials`;
    return await this.putCreate(url, credential, CredentialMetadata, cancellationToken);
  }

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
  async updateCredential(credential, guid, cancellationToken) {
    if (!guid) {
      GenericExceptionHandlers.ArgumentNullException('guid');
    }
    if (!credential) {
      GenericExceptionHandlers.ArgumentNullException('credential');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/credentials/${guid}`;
    return await this.putUpdate(url, credential, CredentialMetadata, cancellationToken);
  }

  /**
   * Delete a credential.
   * @param {string} guid - The GUID of the credential.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Boolean>}
   */
  async deleteCredential(guid, cancellationToken) {
    if (!guid) {
      GenericExceptionHandlers.ArgumentNullException('guid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/credentials/${guid}`;
    return await this.delete(url, cancellationToken);
  }

  /**
   * Credential exists.
   * @param {string} guid - The GUID of the credential.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<boolean>}
   */
  async existsCredential(guid, cancellationToken) {
    if (!guid) {
      GenericExceptionHandlers.ArgumentNullException('guid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/credentials/${guid}`;
    return await this.head(url, cancellationToken);
  }
  //end region

  //region TagMetaData

  /**
   * Read all tags.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<TagMetaData[]>}
   */
  async readAllTags(cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/tags`;
    return await this.getMany(url, TagMetaData, cancellationToken);
  }

  /**
   * Read a tag.
   * @param {string} guid - The GUID of the tag.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<TagMetaData>}
   */
  async readTag(guid, cancellationToken) {
    if (!guid) {
      GenericExceptionHandlers.ArgumentNullException('guid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/tags/${guid}`;
    return await this.get(url, TagMetaData, cancellationToken);
  }

  /**
   * Tag exists.
   * @param {string} guid - The GUID of the tag.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<boolean>}
   */
  async existsTag(guid, cancellationToken) {
    if (!guid) {
      GenericExceptionHandlers.ArgumentNullException('guid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/tags/${guid}`;
    return await this.head(url, cancellationToken);
  }

  /**
   * Create a tag.
   * @param {TagMetaData} tag - The tag to create.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<TagMetaData>}
   */
  async createTag(tag, cancellationToken) {
    if (!tag) {
      GenericExceptionHandlers.ArgumentNullException('tag');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/tags`;
    return await this.putCreate(url, tag, TagMetaData, cancellationToken);
  }

  /**
   * Create multiple tags.
   * @param {Array<TagMetaData>} tags - The tags to create.
   * @param {Object|AbortController} [optionsOrCancellationToken] - Optional return mode options or cancellation token.
   * @param {string} [optionsOrCancellationToken.returnMode] - Optional bulk create return mode: full or minimal.
   * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
   * @returns {Promise<TagMetaData[]>}
   */
  async createTags(tags, optionsOrCancellationToken, cancellationToken) {
    if (!tags) {
      GenericExceptionHandlers.ArgumentNullException('tags');
    }
    if (tags.length < 1) return [];

    const args = normalizeBulkCreateArgs(optionsOrCancellationToken, cancellationToken);
    const query = buildQueryString({ return: normalizeBulkReturnMode(args.returnMode) });
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/tags/bulk${query}`;
    return await this.putCreate(url, tags, TagMetaData, args.cancellationToken);
  }

  /**
   * Update a tag.
   * @param {TagMetaData} tag - The tag to update.
   * @param {string} guid - The GUID of the tag.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<TagMetaData>}
   */
  async updateTag(tag, guid, cancellationToken) {
    if (!guid) {
      GenericExceptionHandlers.ArgumentNullException('guid');
    }
    if (!tag) {
      GenericExceptionHandlers.ArgumentNullException('tag');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/tags/${guid}`;
    return await this.putUpdate(url, tag, TagMetaData, cancellationToken);
  }

  /**
   * Delete a tag.
   * @param {string} guid - The GUID of the tag.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<void>}
   */
  async deleteTag(guid, cancellationToken) {
    if (!guid) {
      GenericExceptionHandlers.ArgumentNullException('guid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/tags/${guid}`;
    return await this.delete(url, cancellationToken);
  }

  //end region

  //region Labels

  /**
   * Read all labels.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<LabelMetadata[]>}
   */
  async readAllLabels(cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/labels`;
    return await this.getMany(url, LabelMetadata, cancellationToken);
  }

  /**
   * Read a label.
   * @param {string} guid - The GUID of the label.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<LabelMetadata>}
   */
  async readLabel(guid, cancellationToken) {
    if (!guid) {
      GenericExceptionHandlers.ArgumentNullException('guid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/labels/${guid}`;
    return await this.get(url, LabelMetadata, cancellationToken);
  }

  /**
   * Label exists.
   * @param {string} guid - The GUID of the label.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<boolean>}
   */
  async existsLabel(guid, cancellationToken) {
    if (!guid) {
      GenericExceptionHandlers.ArgumentNullException('guid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/labels/${guid}`;
    return await this.head(url, cancellationToken);
  }

  /**
   * Create a label.
   * @param {LabelMetadata} label - The label to create.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<LabelMetadata>}
   */
  async createLabel(label, cancellationToken) {
    if (!label) {
      GenericExceptionHandlers.ArgumentNullException('label');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/labels`;
    return await this.putCreate(url, label, LabelMetadata, cancellationToken);
  }

  /**
   * Create multiple labels.
   * @param {Array<LabelMetadata>} labels - The labels to create.
   * @param {Object|AbortController} [optionsOrCancellationToken] - Optional return mode options or cancellation token.
   * @param {string} [optionsOrCancellationToken.returnMode] - Optional bulk create return mode: full or minimal.
   * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
   * @returns {Promise<LabelMetadata[]>}
   */
  async createLabels(labels, optionsOrCancellationToken, cancellationToken) {
    if (!labels) {
      GenericExceptionHandlers.ArgumentNullException('labels');
    }
    if (labels.length < 1) return [];

    const args = normalizeBulkCreateArgs(optionsOrCancellationToken, cancellationToken);
    const query = buildQueryString({ return: normalizeBulkReturnMode(args.returnMode) });
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/labels/bulk${query}`;
    return await this.putCreate(url, labels, LabelMetadata, args.cancellationToken);
  }

  /**
   * Update a label.
   * @param {LabelMetadata} label - The label to update.
   * @param {string} guid - The GUID of the label.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<LabelMetadata>}
   */
  async updateLabel(label, guid, cancellationToken) {
    if (!guid) {
      GenericExceptionHandlers.ArgumentNullException('guid');
    }
    if (!label) {
      GenericExceptionHandlers.ArgumentNullException('label');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/labels/${guid}`;
    return await this.putUpdate(url, label, LabelMetadata, cancellationToken);
  }

  /**
   * Delete a label.
   * @param {string} guid - The GUID of the label.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<void>}
   */
  async deleteLabel(guid, cancellationToken) {
    if (!guid) {
      GenericExceptionHandlers.ArgumentNullException('guid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/labels/${guid}`;
    return await this.delete(url, cancellationToken);
  }

  //end region

  //region Vectors

  /**
   * Read all vectors.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<VectorMetadata[]>}
   */
  async readAllVectors(cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/vectors`;
    return await this.getMany(url, VectorMetadata, cancellationToken);
  }

  /**
   * Read a vector.
   * @param {string} guid - The GUID of the vector.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<VectorMetadata>}
   */
  async readVector(guid, cancellationToken) {
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/vectors/${guid}`;
    return await this.get(url, VectorMetadata, cancellationToken);
  }

  /**
   * Vector exists.
   * @param {string} guid - The GUID of the vector.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<boolean>}
   */
  async existsVector(guid, cancellationToken) {
    if (!guid) {
      GenericExceptionHandlers.ArgumentNullException('guid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/vectors/${guid}`;
    return await this.head(url, cancellationToken);
  }

  /**
   * Create a vector.
   * @param {VectorMetadata} vector - The vector to create.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<VectorMetadata>}
   */
  async createVector(vector, cancellationToken) {
    if (!vector) {
      GenericExceptionHandlers.ArgumentNullException('vector');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/vectors`;
    return await this.putCreate(url, vector, VectorMetadata, cancellationToken);
  }

  /**
   * Create multiple vectors.
   * @param {Array<VectorMetadata>} vectors - The vectors to create.
   * @param {Object|AbortController} [optionsOrCancellationToken] - Optional return mode options or cancellation token.
   * @param {string} [optionsOrCancellationToken.returnMode] - Optional bulk create return mode: full or minimal.
   * @param {AbortController} [cancellationToken] - Optional cancellation token when options are supplied.
   * @returns {Promise<VectorMetadata[]>}
   */
  async createVectors(vectors, optionsOrCancellationToken, cancellationToken) {
    if (!vectors) {
      GenericExceptionHandlers.ArgumentNullException('vectors');
    }
    if (vectors.length < 1) return [];

    const args = normalizeBulkCreateArgs(optionsOrCancellationToken, cancellationToken);
    const query = buildQueryString({ return: normalizeBulkReturnMode(args.returnMode) });
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/vectors/bulk${query}`;
    return await this.putCreate(url, vectors, VectorMetadata, args.cancellationToken);
  }

  /**
   * Update a vector.
   * @param {VectorMetadata} vector - The vector to update.
   * @param {string} guid - The GUID of the vector.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<VectorMetadata>}
   */
  async updateVector(vector, guid, cancellationToken) {
    if (!guid) {
      GenericExceptionHandlers.ArgumentNullException('guid');
    }
    if (!vector) {
      GenericExceptionHandlers.ArgumentNullException('vector');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/vectors/${guid}`;
    return await this.putUpdate(url, vector, VectorMetadata, cancellationToken);
  }

  /**
   * Delete a vector.
   * @param {string} guid - The GUID of the vector.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<void>}
   */
  async deleteVector(guid, cancellationToken) {
    if (!guid) {
      GenericExceptionHandlers.ArgumentNullException('guid');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/vectors/${guid}`;
    return await this.delete(url, cancellationToken);
  }

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
  async searchVectors(searchReq, cancellationToken) {
    if (!searchReq) {
      GenericExceptionHandlers.ArgumentNullException('Search Request');
    }
    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/vectors`;
    const json = JSON.stringify(searchReq);
    const response = await this.post(url, json, VectorSearchResult, cancellationToken);

    return response;
  }

  //end region

  //region Authentication

  /**
   * Generate an authentication token.
   * @param {string} email - The user's email address.
   * @param {string} tenantId - The tenant ID.
   * @param {string} password - The user's password.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Token>} The generated authentication token
   */
  async generateToken(email, password, tenantId, cancellationToken) {
    if (!email) {
      GenericExceptionHandlers.ArgumentNullException('email');
    }
    if (!password) {
      GenericExceptionHandlers.ArgumentNullException('password');
    }
    if (!tenantId) {
      GenericExceptionHandlers.ArgumentNullException('tenantId');
    }

    const url = `${this._endpoint}v1.0/token`;
    const headers = {
      'x-email': email,
      'x-password': password,
      'x-tenant-guid': tenantId,
    };

    return await this.get(url, Token, cancellationToken, headers);
  }

  /**
   * Fetch details about an authentication token.
   * @param {string} token - The authentication token to inspect.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Token>} The token details
   */
  async getTokenDetails(token, cancellationToken) {
    if (!token) {
      GenericExceptionHandlers.ArgumentNullException('token');
    }

    const url = `${this._endpoint}v1.0/token/details`;
    const headers = {
      'x-token': token,
    };

    return await this.get(url, Token, cancellationToken, headers);
  }

  /**
   * Get tenants associated with an email address.
   * @param {string} email - The email address to lookup tenants for.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<TenantMetaData[]>} Array of tenants associated with the email
   */
  async getTenantsForEmail(email, cancellationToken) {
    if (!email) {
      GenericExceptionHandlers.ArgumentNullException('email');
    }

    const url = `${this._endpoint}v1.0/token/tenants`;
    return await this.getMany(url, TenantMetaData, cancellationToken, {
      'x-email': email,
    });
  }

  //endregion

  //region Admin Methods

  /**
   * List all available backups.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Array>} List of backup metadata.
   */
  async listBackups(cancellationToken) {
    const url = `${this._endpoint}v1.0/backups`;
    return await this.get(url, null, cancellationToken);
  }

  /**
   * Create a new database backup.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Object>} Backup metadata.
   */
  async createBackup(cancellationToken) {
    const url = `${this._endpoint}v1.0/backups`;
    return await this.post(url, null, null, cancellationToken);
  }

  /**
   * Read a specific backup file.
   * @param {string} backupFilename - The backup filename.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Object>} Backup data.
   */
  async readBackup(backupFilename, cancellationToken) {
    if (!backupFilename) {
      GenericExceptionHandlers.ArgumentNullException('backupFilename');
    }
    const url = `${this._endpoint}v1.0/backups/${backupFilename}`;
    return await this.get(url, null, cancellationToken);
  }

  /**
   * Check if a backup file exists.
   * @param {string} backupFilename - The backup filename.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<boolean>} True if the backup exists.
   */
  async backupExists(backupFilename, cancellationToken) {
    if (!backupFilename) {
      GenericExceptionHandlers.ArgumentNullException('backupFilename');
    }
    const url = `${this._endpoint}v1.0/backups/${backupFilename}`;
    return await this.head(url, cancellationToken);
  }

  /**
   * Delete a backup file.
   * @param {string} backupFilename - The backup filename.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<void>}
   */
  async deleteBackup(backupFilename, cancellationToken) {
    if (!backupFilename) {
      GenericExceptionHandlers.ArgumentNullException('backupFilename');
    }
    const url = `${this._endpoint}v1.0/backups/${backupFilename}`;
    return await this.delete(url, cancellationToken);
  }

  /**
   * Flush the in-memory database to disk.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<void>}
   */
  async flushDatabase(cancellationToken) {
    const url = `${this._endpoint}v1.0/flush`;
    return await this.post(url, null, null, cancellationToken);
  }

  /**
   * Read the server settings. Requires system administrator privileges.
   * @param {AbortController} [cancellationToken] - Optional cancellation token.
   * @returns {Promise<Object>} The server settings object.
   */
  async readSettings(cancellationToken) {
    const url = `${this._endpoint}v1.0/settings`;
    return await this.get(url, Object, cancellationToken);
  }

  /**
   * Update the server settings. Requires system administrator privileges.
   * @param {Object} settings - The full settings object.
   * @param {AbortController} [cancellationToken] - Optional cancellation token.
   * @returns {Promise<Object>} Settings update result ({ Success, AppliedLive, RestartRequired, Message }).
   */
  async updateSettings(settings, cancellationToken) {
    if (!settings) {
      GenericExceptionHandlers.ArgumentNullException('settings');
    }
    const url = `${this._endpoint}v1.0/settings`;
    return await this.putUpdate(url, settings, Object, cancellationToken);
  }

  /**
   * Request a server restart. The server exits so the container restart policy applies the new settings.
   * Requires system administrator privileges. Best-effort; the connection may drop as the server exits.
   * @param {AbortController} [cancellationToken] - Optional cancellation token.
   * @returns {Promise<void>}
   */
  async restartServer(cancellationToken) {
    const url = `${this._endpoint}v1.0/settings/restart`;
    try {
      return await this.post(url, { confirm: true }, Object, cancellationToken);
    } catch (e) {
      // The server may drop the connection as it exits; this is expected.
      return undefined;
    }
  }

  //end region

  //region Vector Index Methods

  /**
   * Enable vector indexing on a graph.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {Object} config - Vector index configuration.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Object>} Result.
   */
  async enableVectorIndex(tenantGuid, graphGuid, config, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/vectorindex/enable`;
    return await this.putUpdate(url, config, null, cancellationToken);
  }

  /**
   * Disable vector indexing on a graph.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<void>}
   */
  async disableVectorIndex(tenantGuid, graphGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/vectorindex`;
    return await this.delete(url, cancellationToken);
  }

  /**
   * Rebuild the vector index for a graph.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Object>} Result.
   */
  async rebuildVectorIndex(tenantGuid, graphGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/vectorindex/rebuild`;
    return await this.post(url, null, null, cancellationToken);
  }

  /**
   * Get the vector index configuration for a graph.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Object>} Vector index configuration.
   */
  async getVectorIndexConfig(tenantGuid, graphGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/vectorindex/config`;
    return await this.get(url, null, cancellationToken);
  }

  /**
   * Get vector index statistics for a graph.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Object>} Vector index statistics.
   */
  async getVectorIndexStats(tenantGuid, graphGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/vectorindex/stats`;
    return await this.get(url, null, cancellationToken);
  }

  //end region

  //region Graph Advanced Methods

  /**
   * Get a subgraph starting from a specific node.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} nodeGuid - Starting node GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Object>} Subgraph data.
   */
  async getSubgraph(tenantGuid, graphGuid, nodeGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    if (!nodeGuid) {
      GenericExceptionHandlers.ArgumentNullException('nodeGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/nodes/${nodeGuid}/subgraph`;
    return await this.get(url, null, cancellationToken);
  }

  /**
   * Get subgraph statistics starting from a specific node.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} nodeGuid - Starting node GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Object>} Subgraph statistics.
   */
  async getSubgraphStatistics(tenantGuid, graphGuid, nodeGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    if (!nodeGuid) {
      GenericExceptionHandlers.ArgumentNullException('nodeGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/nodes/${nodeGuid}/subgraph/stats`;
    return await this.get(url, null, cancellationToken);
  }

  /**
   * Get statistics for a specific graph.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Object>} Graph statistics.
   */
  async getGraphStatistics(tenantGuid, graphGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/stats`;
    return await this.get(url, null, cancellationToken);
  }

  /**
   * Get statistics for all graphs in a tenant.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Object>} All graph statistics.
   */
  async getAllGraphStatistics(tenantGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/stats`;
    return await this.get(url, null, cancellationToken);
  }

  //end region

  //region Node Advanced Methods

  /**
   * Get the most connected nodes in a graph.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Array>} List of most connected nodes.
   */
  async getMostConnectedNodes(tenantGuid, graphGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/nodes/mostconnected`;
    return await this.get(url, null, cancellationToken);
  }

  /**
   * Get the least connected nodes in a graph.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Array>} List of least connected nodes.
   */
  async getLeastConnectedNodes(tenantGuid, graphGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/nodes/leastconnected`;
    return await this.get(url, null, cancellationToken);
  }

  //end region

  //region Scoped Label Operations

  /**
   * Read labels for a specific graph.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<LabelMetadata[]>} List of labels.
   */
  async readGraphLabels(tenantGuid, graphGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/labels`;
    return await this.getMany(url, LabelMetadata, cancellationToken);
  }

  /**
   * Read labels for a specific node.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} nodeGuid - Node GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<LabelMetadata[]>} List of labels.
   */
  async readNodeLabels(tenantGuid, graphGuid, nodeGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    if (!nodeGuid) {
      GenericExceptionHandlers.ArgumentNullException('nodeGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/nodes/${nodeGuid}/labels`;
    return await this.getMany(url, LabelMetadata, cancellationToken);
  }

  /**
   * Read labels for a specific edge.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} edgeGuid - Edge GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<LabelMetadata[]>} List of labels.
   */
  async readEdgeLabels(tenantGuid, graphGuid, edgeGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    if (!edgeGuid) {
      GenericExceptionHandlers.ArgumentNullException('edgeGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/edges/${edgeGuid}/labels`;
    return await this.getMany(url, LabelMetadata, cancellationToken);
  }

  /**
   * Delete all labels for a specific graph.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<void>}
   */
  async deleteGraphLabels(tenantGuid, graphGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/labels`;
    return await this.delete(url, cancellationToken);
  }

  /**
   * Delete all labels for a specific node.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} nodeGuid - Node GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<void>}
   */
  async deleteNodeLabels(tenantGuid, graphGuid, nodeGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    if (!nodeGuid) {
      GenericExceptionHandlers.ArgumentNullException('nodeGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/nodes/${nodeGuid}/labels`;
    return await this.delete(url, cancellationToken);
  }

  /**
   * Delete all labels for a specific edge.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} edgeGuid - Edge GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<void>}
   */
  async deleteEdgeLabels(tenantGuid, graphGuid, edgeGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    if (!edgeGuid) {
      GenericExceptionHandlers.ArgumentNullException('edgeGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/edges/${edgeGuid}/labels`;
    return await this.delete(url, cancellationToken);
  }

  //end region

  //region Scoped Tag Operations

  /**
   * Read tags for a specific graph.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<TagMetaData[]>} List of tags.
   */
  async readGraphTags(tenantGuid, graphGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/tags`;
    return await this.getMany(url, TagMetaData, cancellationToken);
  }

  /**
   * Read tags for a specific node.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} nodeGuid - Node GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<TagMetaData[]>} List of tags.
   */
  async readNodeTags(tenantGuid, graphGuid, nodeGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    if (!nodeGuid) {
      GenericExceptionHandlers.ArgumentNullException('nodeGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/nodes/${nodeGuid}/tags`;
    return await this.getMany(url, TagMetaData, cancellationToken);
  }

  /**
   * Read tags for a specific edge.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} edgeGuid - Edge GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<TagMetaData[]>} List of tags.
   */
  async readEdgeTags(tenantGuid, graphGuid, edgeGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    if (!edgeGuid) {
      GenericExceptionHandlers.ArgumentNullException('edgeGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/edges/${edgeGuid}/tags`;
    return await this.getMany(url, TagMetaData, cancellationToken);
  }

  /**
   * Delete all tags for a specific graph.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<void>}
   */
  async deleteGraphTags(tenantGuid, graphGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/tags`;
    return await this.delete(url, cancellationToken);
  }

  /**
   * Delete all tags for a specific node.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} nodeGuid - Node GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<void>}
   */
  async deleteNodeTags(tenantGuid, graphGuid, nodeGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    if (!nodeGuid) {
      GenericExceptionHandlers.ArgumentNullException('nodeGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/nodes/${nodeGuid}/tags`;
    return await this.delete(url, cancellationToken);
  }

  /**
   * Delete all tags for a specific edge.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} edgeGuid - Edge GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<void>}
   */
  async deleteEdgeTags(tenantGuid, graphGuid, edgeGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    if (!edgeGuid) {
      GenericExceptionHandlers.ArgumentNullException('edgeGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/edges/${edgeGuid}/tags`;
    return await this.delete(url, cancellationToken);
  }

  //end region

  //region Scoped Vector Operations

  /**
   * Read vectors for a specific graph.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<VectorMetadata[]>} List of vectors.
   */
  async readGraphVectors(tenantGuid, graphGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/vectors`;
    return await this.getMany(url, VectorMetadata, cancellationToken);
  }

  /**
   * Read vectors for a specific node.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} nodeGuid - Node GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<VectorMetadata[]>} List of vectors.
   */
  async readNodeVectors(tenantGuid, graphGuid, nodeGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    if (!nodeGuid) {
      GenericExceptionHandlers.ArgumentNullException('nodeGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/nodes/${nodeGuid}/vectors`;
    return await this.getMany(url, VectorMetadata, cancellationToken);
  }

  /**
   * Read vectors for a specific edge.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} edgeGuid - Edge GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<VectorMetadata[]>} List of vectors.
   */
  async readEdgeVectors(tenantGuid, graphGuid, edgeGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    if (!edgeGuid) {
      GenericExceptionHandlers.ArgumentNullException('edgeGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/edges/${edgeGuid}/vectors`;
    return await this.getMany(url, VectorMetadata, cancellationToken);
  }

  /**
   * Delete all vectors for a specific graph.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<void>}
   */
  async deleteGraphVectors(tenantGuid, graphGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/vectors`;
    return await this.delete(url, cancellationToken);
  }

  /**
   * Delete all vectors for a specific node.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} nodeGuid - Node GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<void>}
   */
  async deleteNodeVectors(tenantGuid, graphGuid, nodeGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    if (!nodeGuid) {
      GenericExceptionHandlers.ArgumentNullException('nodeGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/nodes/${nodeGuid}/vectors`;
    return await this.delete(url, cancellationToken);
  }

  /**
   * Delete all vectors for a specific edge.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} graphGuid - Graph GUID.
   * @param {string} edgeGuid - Edge GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<void>}
   */
  async deleteEdgeVectors(tenantGuid, graphGuid, edgeGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!graphGuid) {
      GenericExceptionHandlers.ArgumentNullException('graphGuid');
    }
    if (!edgeGuid) {
      GenericExceptionHandlers.ArgumentNullException('edgeGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/graphs/${graphGuid}/edges/${edgeGuid}/vectors`;
    return await this.delete(url, cancellationToken);
  }

  //end region

  //region Chat Routes

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
  async createChatEndpoint(tenantGuid, endpoint, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!endpoint) {
      GenericExceptionHandlers.ArgumentNullException('endpoint');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/endpoints`;
    return await this.putCreate(url, endpoint, ChatEndpoint, cancellationToken);
  }

  /**
   * Read all chat endpoints, optionally filtered by endpoint type.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} [endpointType] - Optional endpoint type filter: Embedding or Completion.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<ChatEndpoint[]>} - List of chat endpoints (ApiKey redacted).
   */
  async readChatEndpoints(tenantGuid, endpointType, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    const query = buildQueryString({ endpointType });
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/endpoints${query}`;
    return await this.getMany(url, ChatEndpoint, cancellationToken);
  }

  /**
   * Read a specific chat endpoint.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} endpointGuid - Chat endpoint GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<ChatEndpoint>} - The requested chat endpoint (ApiKey redacted).
   */
  async readChatEndpoint(tenantGuid, endpointGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!endpointGuid) {
      GenericExceptionHandlers.ArgumentNullException('endpointGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/endpoints/${endpointGuid}`;
    return await this.get(url, ChatEndpoint, cancellationToken);
  }

  /**
   * Check if a chat endpoint exists by GUID.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} endpointGuid - Chat endpoint GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<boolean>} - True if the chat endpoint exists.
   */
  async chatEndpointExists(tenantGuid, endpointGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!endpointGuid) {
      GenericExceptionHandlers.ArgumentNullException('endpointGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/endpoints/${endpointGuid}`;
    return await this.head(url, cancellationToken);
  }

  /**
   * Update a chat endpoint. Sending back a redacted ApiKey preserves the stored key.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {Object} endpoint - Chat endpoint payload containing GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<ChatEndpoint>} - The updated chat endpoint (ApiKey redacted).
   */
  async updateChatEndpoint(tenantGuid, endpoint, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!endpoint) {
      GenericExceptionHandlers.ArgumentNullException('endpoint');
    }
    if (!endpoint.GUID) {
      GenericExceptionHandlers.ArgumentNullException('endpoint.GUID');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/endpoints/${endpoint.GUID}`;
    return await this.putUpdate(url, endpoint, ChatEndpoint, cancellationToken);
  }

  /**
   * Delete a chat endpoint.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} endpointGuid - Chat endpoint GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<void>}
   */
  async deleteChatEndpoint(tenantGuid, endpointGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!endpointGuid) {
      GenericExceptionHandlers.ArgumentNullException('endpointGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/endpoints/${endpointGuid}`;
    return await this.delete(url, cancellationToken);
  }

  /**
   * Test connectivity of a chat endpoint.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} endpointGuid - Chat endpoint GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<ChatEndpointTestResult>} - Connectivity test result ({ Reachable, Models, ModelExists, Error, RuntimeMs }).
   */
  async testChatEndpoint(tenantGuid, endpointGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!endpointGuid) {
      GenericExceptionHandlers.ArgumentNullException('endpointGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/endpoints/${endpointGuid}/test`;
    return await this.post(url, null, ChatEndpointTestResult, cancellationToken);
  }

  /**
   * Read health status for a specific chat endpoint.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} endpointGuid - Chat endpoint GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<ChatEndpointHealth>} - Health status for the endpoint.
   */
  async readChatEndpointHealth(tenantGuid, endpointGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!endpointGuid) {
      GenericExceptionHandlers.ArgumentNullException('endpointGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/endpoints/${endpointGuid}/health`;
    return await this.get(url, ChatEndpointHealth, cancellationToken);
  }

  /**
   * Read health status for all chat endpoints.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<ChatEndpointHealth[]>} - Health status list.
   */
  async readAllChatEndpointHealth(tenantGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/endpoints/health`;
    return await this.getMany(url, ChatEndpointHealth, cancellationToken);
  }

  /**
   * Create a chat thread. The caller becomes the thread owner; requires a user principal.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {Object} [thread] - Optional thread payload.
   * @param {string} [thread.GraphGUID] - Optional graph GUID to bind the thread to.
   * @param {string} [thread.Title] - Optional thread title.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<ChatThread>} - The created chat thread.
   */
  async createChatThread(tenantGuid, thread, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/threads`;
    return await this.putCreate(url, thread || {}, ChatThread, cancellationToken);
  }

  /**
   * Read chat threads owned by the caller, or all users' threads when allUsers is true (admin only).
   * @param {string} tenantGuid - Tenant GUID.
   * @param {boolean} [allUsers=false] - When true, list every user's threads (requires administrator privileges).
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<ChatThread[]>} - List of chat threads.
   */
  async readChatThreads(tenantGuid, allUsers = false, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    let url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/threads`;
    if (allUsers) url += '?all';
    return await this.getMany(url, ChatThread, cancellationToken);
  }

  /**
   * Read a specific chat thread.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} threadGuid - Chat thread GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<ChatThread>} - The requested chat thread.
   */
  async readChatThread(tenantGuid, threadGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!threadGuid) {
      GenericExceptionHandlers.ArgumentNullException('threadGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/threads/${threadGuid}`;
    return await this.get(url, ChatThread, cancellationToken);
  }

  /**
   * Update (rename) a chat thread. Only the Title property is honored and it must be non-empty.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} threadGuid - Chat thread GUID.
   * @param {Object} thread - Thread payload.
   * @param {string} thread.Title - New thread title.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<ChatThread>} - The updated chat thread.
   */
  async updateChatThread(tenantGuid, threadGuid, thread, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!threadGuid) {
      GenericExceptionHandlers.ArgumentNullException('threadGuid');
    }
    if (!thread) {
      GenericExceptionHandlers.ArgumentNullException('thread');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/threads/${threadGuid}`;
    return await this.putUpdate(url, thread, ChatThread, cancellationToken);
  }

  /**
   * Delete a chat thread along with its turns and feedback.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} threadGuid - Chat thread GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<void>}
   */
  async deleteChatThread(tenantGuid, threadGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!threadGuid) {
      GenericExceptionHandlers.ArgumentNullException('threadGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/threads/${threadGuid}`;
    return await this.delete(url, cancellationToken);
  }

  /**
   * Read the turns of a chat thread, ascending by sequence.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} threadGuid - Chat thread GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<ChatTurn[]>} - List of chat turns.
   */
  async readChatThreadTurns(tenantGuid, threadGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!threadGuid) {
      GenericExceptionHandlers.ArgumentNullException('threadGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/threads/${threadGuid}/turns`;
    return await this.getMany(url, ChatTurn, cancellationToken);
  }

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
  async chatCompletion(tenantGuid, request, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!request) {
      GenericExceptionHandlers.ArgumentNullException('request');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/completions`;
    const payload = { ...request, Stream: false };
    return await this.post(url, JSON.stringify(payload), ChatCompletionResult, cancellationToken);
  }

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
  // eslint-disable-next-line node/no-unsupported-features/es-syntax -- streaming requires Node 18+ (fetch)
  async *chatCompletionStreaming(tenantGuid, request, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!request) {
      GenericExceptionHandlers.ArgumentNullException('request');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/completions`;
    const payload = { ...request, Stream: true };
    yield* this.postSse(url, payload, cancellationToken);
  }

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
  async submitChatFeedback(tenantGuid, turnGuid, feedback, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!turnGuid) {
      GenericExceptionHandlers.ArgumentNullException('turnGuid');
    }
    if (!feedback) {
      GenericExceptionHandlers.ArgumentNullException('feedback');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/turns/${turnGuid}/feedback`;
    return await this.post(url, JSON.stringify(feedback), ChatFeedback, cancellationToken);
  }

  /**
   * Read all chat feedback for the tenant. Requires tenant administrator privileges.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<ChatFeedback[]>} - List of feedback records.
   */
  async readAllChatFeedback(tenantGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/feedback`;
    return await this.getMany(url, ChatFeedback, cancellationToken);
  }

  /**
   * Read a specific chat feedback record. Requires tenant administrator privileges.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} feedbackGuid - Chat feedback GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<ChatFeedback>} - The requested feedback record.
   */
  async readChatFeedback(tenantGuid, feedbackGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!feedbackGuid) {
      GenericExceptionHandlers.ArgumentNullException('feedbackGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/feedback/${feedbackGuid}`;
    return await this.get(url, ChatFeedback, cancellationToken);
  }

  /**
   * Delete a chat feedback record. Requires tenant administrator privileges.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {string} feedbackGuid - Chat feedback GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<void>}
   */
  async deleteChatFeedback(tenantGuid, feedbackGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!feedbackGuid) {
      GenericExceptionHandlers.ArgumentNullException('feedbackGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/feedback/${feedbackGuid}`;
    return await this.delete(url, cancellationToken);
  }

  /**
   * Read tenant chat settings. Returns defaults when no settings record exists.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<ChatSettings>} - The tenant chat settings.
   */
  async readChatSettings(tenantGuid, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/settings`;
    return await this.get(url, ChatSettings, cancellationToken);
  }

  /**
   * Upsert tenant chat settings. Requires tenant administrator privileges.
   * @param {string} tenantGuid - Tenant GUID.
   * @param {Object} settings - Chat settings payload.
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<ChatSettings>} - The updated chat settings.
   */
  async updateChatSettings(tenantGuid, settings, cancellationToken) {
    if (!tenantGuid) {
      GenericExceptionHandlers.ArgumentNullException('tenantGuid');
    }
    if (!settings) {
      GenericExceptionHandlers.ArgumentNullException('settings');
    }
    const url = `${this._endpoint}v1.0/tenants/${tenantGuid}/chat/settings`;
    return await this.putUpdate(url, settings, ChatSettings, cancellationToken);
  }

  //end region
}
