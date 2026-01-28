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
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Array<Node>>} - The list of created nodes.
   */
  async createNodes(graphGuid, nodes, cancellationToken) {
    if (!nodes) {
      GenericExceptionHandlers.ArgumentNullException('Nodes');
    }
    if (nodes.length < 1) return [];

    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/nodes/multiple`;
    return await this.putCreate(url, nodes, Node, cancellationToken);
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
   * @param {AbortController} [cancellationToken] - Optional cancellation token for cancelling the request.
   * @returns {Promise<Array<Object>>} - The list of created edges.
   */
  async createEdges(graphGuid, edges, cancellationToken) {
    if (!edges) {
      GenericExceptionHandlers.ArgumentNullException('Edges');
    }
    if (edges.length < 1) return [];

    const url = `${this._endpoint}v1.0/tenants/${this.tenantGuid}/graphs/${graphGuid}/edges/multiple`;
    return await this.putCreate(url, edges, Edge, cancellationToken);
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
}
