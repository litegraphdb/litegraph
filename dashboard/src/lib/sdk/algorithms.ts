import { sdk } from './litegraph.service';

export type GraphAlgorithmType =
  | 'DegreeCentrality'
  | 'PageRank'
  | 'ClosenessCentrality'
  | 'EigenvectorCentrality'
  | 'BetweennessCentrality'
  | 'WeaklyConnectedComponents'
  | 'StronglyConnectedComponents'
  | 'LabelPropagation'
  | 'Louvain'
  | 'ClusteringCoefficient'
  | 'KCore';

export type GraphExportFormat = 'NodeLinkJson' | 'EdgeList' | 'Graphml';
export type GraphExportAttributeLevel = 'None' | 'Meta' | 'Full';

export type GraphAlgorithmRequest = {
  AlgorithmType: GraphAlgorithmType;
  DampingFactor?: number;
  MaxIterations?: number;
  Tolerance?: number;
  TreatAsUndirected?: boolean;
  MaxResults?: number | null;
  WriteBack?: boolean;
  WriteBackProperty?: string | null;
};

export type GraphAlgorithmNodeResult = {
  NodeGUID: string;
  Name?: string | null;
  Score: number;
  EdgesIn?: number | null;
  EdgesOut?: number | null;
  Community?: number | null;
};

export type GraphAlgorithmResult = {
  Success: boolean;
  TenantGUID?: string;
  GraphGUID?: string;
  AlgorithmType: GraphAlgorithmType;
  NodeCount: number;
  EdgeCount: number;
  Iterations: number;
  Converged: boolean;
  CommunityCount?: number | null;
  ComputeMs: number;
  LoadMs: number;
  WrittenBack: boolean;
  WriteBackProperty?: string | null;
  Nodes: GraphAlgorithmNodeResult[];
};

export type GraphAlgorithmImportRequest = {
  Values: Record<string, Record<string, number>>;
};

export type GraphAlgorithmImportResult = {
  Success: boolean;
  NodesUpdated: number;
};

const getBaseUrl = (): string => {
  const endpoint = sdk.config.endpoint || '/';
  return endpoint.endsWith('/') ? endpoint.slice(0, -1) : endpoint;
};

const buildHeaders = (accept: string, contentType?: string): Record<string, string> => {
  const headers: Record<string, string> = { Accept: accept };
  const defaults = (sdk.config as unknown as { defaultHeaders?: Record<string, string> })
    .defaultHeaders;
  if (defaults) {
    for (const key of Object.keys(defaults)) headers[key] = defaults[key];
  }
  if (contentType) headers['Content-Type'] = contentType;
  return headers;
};

const extractErrorMessage = async (response: Response): Promise<string> => {
  let message = `HTTP ${response.status} ${response.statusText}`;
  try {
    const text = await response.text();
    if (text) {
      try {
        const errorBody = JSON.parse(text);
        message = errorBody?.Description || errorBody?.Message || text;
      } catch {
        message = text;
      }
    }
  } catch {
    // Keep the HTTP status message when the body cannot be read.
  }
  return message;
};

/**
 * Runs a graph algorithm over a single graph.
 * @throws {Error} When the server responds with a non-2xx status.
 */
export const runAlgorithm = async (
  tenantGuid: string,
  graphGuid: string,
  request: GraphAlgorithmRequest
): Promise<GraphAlgorithmResult> => {
  const url = `${getBaseUrl()}/v1.0/tenants/${encodeURIComponent(
    tenantGuid
  )}/graphs/${encodeURIComponent(graphGuid)}/algorithms`;
  const response = await fetch(url, {
    method: 'POST',
    headers: buildHeaders('application/json', 'application/json'),
    body: JSON.stringify(request),
  });
  if (!response.ok) throw new Error(await extractErrorMessage(response));
  return (await response.json()) as GraphAlgorithmResult;
};

/**
 * Exports a graph as a portable projection for external computation (rustworkx / NetworkX).
 * @throws {Error} When the server responds with a non-2xx status.
 */
export const exportGraphProjection = async (
  tenantGuid: string,
  graphGuid: string,
  format: GraphExportFormat = 'NodeLinkJson',
  attributes: GraphExportAttributeLevel = 'Meta'
): Promise<string> => {
  const url = `${getBaseUrl()}/v1.0/tenants/${encodeURIComponent(
    tenantGuid
  )}/graphs/${encodeURIComponent(graphGuid)}/export/projection?format=${encodeURIComponent(
    format
  )}&attributes=${encodeURIComponent(attributes)}`;
  const response = await fetch(url, { method: 'GET', headers: buildHeaders('*/*') });
  if (!response.ok) throw new Error(await extractErrorMessage(response));
  return response.text();
};

/**
 * Imports externally computed per-node values back onto graph nodes.
 * @throws {Error} When the server responds with a non-2xx status.
 */
export const importAlgorithmResults = async (
  tenantGuid: string,
  graphGuid: string,
  request: GraphAlgorithmImportRequest
): Promise<GraphAlgorithmImportResult> => {
  const url = `${getBaseUrl()}/v1.0/tenants/${encodeURIComponent(
    tenantGuid
  )}/graphs/${encodeURIComponent(graphGuid)}/algorithms/import`;
  const response = await fetch(url, {
    method: 'POST',
    headers: buildHeaders('application/json', 'application/json'),
    body: JSON.stringify(request),
  });
  if (!response.ok) throw new Error(await extractErrorMessage(response));
  return (await response.json()) as GraphAlgorithmImportResult;
};
