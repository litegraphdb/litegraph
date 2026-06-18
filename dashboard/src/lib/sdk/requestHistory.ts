import { sdk } from './litegraph.service';

export type RequestHistoryEntry = {
  GUID: string;
  CreatedUtc: string;
  CompletedUtc?: string | null;
  Method: string;
  Path: string;
  Url: string;
  SourceIp?: string | null;
  TenantGUID?: string | null;
  UserGUID?: string | null;
  StatusCode: number;
  Success: boolean;
  ProcessingTimeMs: number;
  RequestBodyLength: number;
  ResponseBodyLength: number;
  RequestBodyTruncated: boolean;
  ResponseBodyTruncated: boolean;
  RequestContentType?: string | null;
  ResponseContentType?: string | null;
  TransactionDiagnosticsJson?: string | null;
};

export type RequestHistoryDetail = RequestHistoryEntry & {
  RequestHeaders: Record<string, string>;
  ResponseHeaders: Record<string, string>;
  RequestBody?: string | null;
  ResponseBody?: string | null;
};

export type RequestHistorySearchResult = {
  Objects: RequestHistoryEntry[];
  TotalCount: number;
  Page: number;
  PageSize: number;
  TotalPages: number;
};

export type RequestHistorySummaryBucket = {
  TimestampUtc: string;
  SuccessCount: number;
  FailureCount: number;
  TotalCount: number;
};

export type RequestHistorySummary = {
  StartUtc: string;
  EndUtc: string;
  Interval: string;
  TotalSuccess: number;
  TotalFailure: number;
  TotalRequests: number;
  Data: RequestHistorySummaryBucket[];
};

export type RequestHistoryListParams = {
  method?: string;
  statusCode?: number;
  success?: boolean;
  path?: string;
  sourceIp?: string;
  hasTransactionDiagnostics?: boolean;
  transactionId?: string;
  tenantGuid?: string;
  fromUtc?: string;
  toUtc?: string;
  page?: number;
  pageSize?: number;
};

export type RequestHistorySummaryParams = {
  interval: 'minute' | '15minute' | 'hour' | '6hour' | 'day';
  startUtc: string;
  endUtc: string;
  tenantGuid?: string;
};

const getBaseUrl = (): string => {
  const endpoint = sdk.config.endpoint || '/';
  return endpoint.endsWith('/') ? endpoint.slice(0, -1) : endpoint;
};

export const getMetricsEndpointUrl = (): string => `${getBaseUrl()}/metrics`;

const buildHeaders = (): Record<string, string> => {
  const headers: Record<string, string> = {
    Accept: 'application/json',
  };
  const defaults = (sdk.config as unknown as { defaultHeaders?: Record<string, string> })
    .defaultHeaders;
  if (defaults) {
    for (const key of Object.keys(defaults)) headers[key] = defaults[key];
  }
  return headers;
};

const buildQuery = (params: Record<string, string | number | boolean | undefined>): string => {
  const entries = Object.entries(params).filter(
    ([, v]) => v !== undefined && v !== null && v !== ''
  );
  if (entries.length === 0) return '';
  return (
    '?' +
    entries
      .map(
        ([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(String(v))}`
      )
      .join('&')
  );
};

const request = async <T>(method: string, url: string, body?: unknown): Promise<T> => {
  const headers = buildHeaders();
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  const response = await fetch(url, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!response.ok) {
    throw new Error(`HTTP ${response.status} ${response.statusText}`);
  }
  if (response.status === 204) return undefined as T;
  const text = await response.text();
  if (!text) return undefined as T;
  return JSON.parse(text) as T;
};

export const listRequestHistory = (params: RequestHistoryListParams = {}) => {
  const url = `${getBaseUrl()}/v1.0/requesthistory${buildQuery(params as Record<string, string | number | boolean | undefined>)}`;
  return request<RequestHistorySearchResult>('GET', url);
};

export const listRecentRequestErrors = (params: Omit<RequestHistoryListParams, 'success'> = {}) => {
  return listRequestHistory({ ...params, success: false });
};

export const getRequestHistorySummary = (params: RequestHistorySummaryParams) => {
  const url = `${getBaseUrl()}/v1.0/requesthistory/summary${buildQuery(params as Record<string, string | number | boolean | undefined>)}`;
  return request<RequestHistorySummary>('GET', url);
};

export const getRequestHistoryDetail = (id: string) => {
  const url = `${getBaseUrl()}/v1.0/requesthistory/${encodeURIComponent(id)}/detail`;
  return request<RequestHistoryDetail>('GET', url);
};

export const deleteRequestHistory = (id: string) => {
  const url = `${getBaseUrl()}/v1.0/requesthistory/${encodeURIComponent(id)}`;
  return request<void>('DELETE', url);
};

export const bulkDeleteRequestHistory = (params: RequestHistoryListParams = {}) => {
  const url = `${getBaseUrl()}/v1.0/requesthistory/bulk${buildQuery(params as Record<string, string | number | boolean | undefined>)}`;
  return request<{ Deleted: number }>('DELETE', url);
};
