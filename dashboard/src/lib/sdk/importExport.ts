import { sdk } from './litegraph.service';

export type GuidStrategy = 'preserve' | 'regenerate' | 'skip' | 'overwrite';
export type ImportOnError = 'abort' | 'skip';
export type SubgraphDirection = 'Outbound' | 'Inbound' | 'Both';

export type ExportGraphJsonlOptions = {
  includeData?: boolean;
  includeSubordinates?: boolean;
};

export type SubgraphExtractionRequest = {
  TenantGUID?: string;
  GraphGUID?: string;
  StartNodeGUIDs: string[];
  MaxDepth?: number;
  Direction?: SubgraphDirection;
  MaxNodes?: number;
  MaxEdges?: number;
  EdgeLabels?: string[];
  EdgeTags?: Record<string, string>;
  MaxEdgeCost?: number | null;
  NodeLabels?: string[];
  NodeTags?: Record<string, string>;
  IncludeData?: boolean;
  IncludeSubordinates?: boolean;
};

export type ImportJsonlOptions = {
  guidStrategy?: GuidStrategy;
  onError?: ImportOnError;
  batchSize?: number;
};

export type GraphImportResult = {
  Success: boolean;
  TenantGUID?: string;
  GraphGUID?: string;
  GraphsCreated: number;
  NodesCreated: number;
  NodesUpdated: number;
  NodesSkipped: number;
  EdgesCreated: number;
  EdgesUpdated: number;
  EdgesSkipped: number;
  LinesRead: number;
  LinesIgnored: number;
  Warnings: string[];
  GuidMap: Record<string, string>;
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

const buildQuery = (params: Record<string, string | number | boolean | undefined>): string => {
  const entries = Object.entries(params).filter(
    ([, value]) => value !== undefined && value !== null && value !== ''
  );
  if (entries.length === 0) return '';
  return (
    '?' +
    entries
      .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`)
      .join('&')
  );
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

const buildImportQuery = (options: ImportJsonlOptions = {}): string => {
  const params: Record<string, string | number | undefined> = {};
  // Always send the chosen GUID strategy so the server does not silently apply a
  // different default than the operator selected.
  if (options.guidStrategy) params.guidstrategy = options.guidStrategy;
  // `abort` is the server default, so only append `onerror` for a non-default choice.
  if (options.onError && options.onError !== 'abort') params.onerror = options.onError;
  if (options.batchSize && options.batchSize > 0) params.batchsize = options.batchSize;
  return buildQuery(params);
};

/**
 * Exports an entire graph as JSONL text.
 * @throws {Error} When the server responds with a non-2xx status.
 */
export const exportGraphJsonl = async (
  tenantGuid: string,
  graphGuid: string,
  options: ExportGraphJsonlOptions = {}
): Promise<string> => {
  const query: Record<string, string | boolean | undefined> = {};
  // These flags are presence-based on the server, so only append when enabled.
  if (options.includeData) query.incldata = true;
  if (options.includeSubordinates) query.inclsub = true;
  const url = `${getBaseUrl()}/v1.0/tenants/${encodeURIComponent(
    tenantGuid
  )}/graphs/${encodeURIComponent(graphGuid)}/export/jsonl${buildQuery(query)}`;
  const response = await fetch(url, { method: 'GET', headers: buildHeaders('application/x-ndjson') });
  if (!response.ok) throw new Error(await extractErrorMessage(response));
  return response.text();
};

/**
 * Extracts and exports a subgraph rooted at the provided start node(s) as JSONL text.
 * @throws {Error} When the server responds with a non-2xx status.
 */
export const exportSubgraphJsonl = async (
  tenantGuid: string,
  graphGuid: string,
  request: SubgraphExtractionRequest
): Promise<string> => {
  const url = `${getBaseUrl()}/v1.0/tenants/${encodeURIComponent(
    tenantGuid
  )}/graphs/${encodeURIComponent(graphGuid)}/export/jsonl`;
  const response = await fetch(url, {
    method: 'POST',
    headers: buildHeaders('application/x-ndjson', 'application/json'),
    body: JSON.stringify(request),
  });
  if (!response.ok) throw new Error(await extractErrorMessage(response));
  return response.text();
};

/**
 * Imports JSONL content into an existing graph.
 * @throws {Error} When the server responds with a non-2xx status.
 */
export const importGraphJsonl = async (
  tenantGuid: string,
  graphGuid: string,
  jsonl: string,
  options: ImportJsonlOptions = {}
): Promise<GraphImportResult> => {
  const url = `${getBaseUrl()}/v1.0/tenants/${encodeURIComponent(
    tenantGuid
  )}/graphs/${encodeURIComponent(graphGuid)}/import/jsonl${buildImportQuery(options)}`;
  const response = await fetch(url, {
    method: 'POST',
    headers: buildHeaders('application/json', 'application/x-ndjson'),
    body: jsonl,
  });
  if (!response.ok) throw new Error(await extractErrorMessage(response));
  return (await response.json()) as GraphImportResult;
};

/**
 * Imports JSONL content into a newly created graph within the tenant.
 * @throws {Error} When the server responds with a non-2xx status.
 */
export const importGraphAsNewJsonl = async (
  tenantGuid: string,
  jsonl: string,
  options: ImportJsonlOptions = {}
): Promise<GraphImportResult> => {
  const url = `${getBaseUrl()}/v1.0/tenants/${encodeURIComponent(
    tenantGuid
  )}/graphs/import/jsonl${buildImportQuery(options)}`;
  const response = await fetch(url, {
    method: 'POST',
    headers: buildHeaders('application/json', 'application/x-ndjson'),
    body: jsonl,
  });
  if (!response.ok) throw new Error(await extractErrorMessage(response));
  return (await response.json()) as GraphImportResult;
};
