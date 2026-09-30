import { sdk } from './litegraph.service';

/**
 * The full server settings document (litegraph.json). It is deeply nested and
 * may evolve, so it is typed loosely; the Settings page edits a curated subset
 * and PUTs the whole document back.
 */
export type ServerSettings = Record<string, any>;

/** Result of a settings write, per the v8.0 `PUT /v1.0/settings` contract. */
export interface SettingsUpdateResult {
  Success: boolean;
  /** Sections whose change applied live without a restart. */
  AppliedLive: string[];
  /** Sections whose change needs a server restart to take effect. */
  RestartRequired: string[];
  Message?: string;
  /** Settings supplied by environment variables or derived at startup; their file values were left unchanged. */
  EnvironmentOverrides?: string[];
  /** Cluster settings version after the save; absent on a single node. */
  SettingsVersion?: number | null;
}

/** Readiness checks reported for a node. */
export interface NodeHealthChecks {
  Database: boolean;
  Clutch?: boolean | null;
  Redis?: boolean | null;
  Draining: boolean;
}

/** One node's entry in the cluster node registry (v10.0 `GET /v1.0/cluster/nodes`). */
export interface ClusterNode {
  NodeId: string;
  Hostname?: string;
  Version?: string;
  StartedUtc: string;
  LastHeartbeatUtc: string;
  HeartbeatAgeMs?: number | null;
  State: 'Healthy' | 'Degraded' | 'Unavailable' | 'Draining' | 'Restarting' | 'Stopped' | 'Offline';
  Checks: NodeHealthChecks;
  SettingsVersion: number;
  RestartPending: boolean;
  RestartVersion: number;
}

/** Cluster status: registered nodes plus the settings and restart counters. */
export interface ClusterStatus {
  ClusterEnabled: boolean;
  ClusterName?: string | null;
  AnsweredBy?: string | null;
  RegistryAvailable?: boolean | null;
  SettingsVersion: number;
  SettingsUpdatedUtc?: string | null;
  RestartVersion: number;
  RestartRequestedUtc?: string | null;
  Nodes: ClusterNode[];
  Utc: string;
}

/** Result of a restart request: a rolling restart in cluster mode, otherwise this server restarts. */
export interface ClusterRestartResult {
  Restarting: boolean;
  Rolling: boolean;
  RestartVersion?: number | null;
  Message?: string;
  RequestedUtc?: string;
}

export const getBaseUrl = (): string => {
  const endpoint = sdk.config.endpoint || '/';
  return endpoint.endsWith('/') ? endpoint.slice(0, -1) : endpoint;
};

const buildHeaders = (): Record<string, string> => {
  // Rely on the SDK's defaultHeaders for authentication: session logins carry
  // x-token there, break-glass carries Authorization. Appending a session
  // token as a bearer credential makes the server reject it with 401.
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

/** Authenticated JSON request against the server's admin routes; throws with the server's error description. */
export const request = async <T>(method: string, url: string, body?: unknown): Promise<T> => {
  const headers = buildHeaders();
  if (body !== undefined) headers['Content-Type'] = 'application/json';
  const response = await fetch(url, {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (!response.ok) {
    let message = `HTTP ${response.status} ${response.statusText}`;
    try {
      const errorBody = await response.json();
      message = errorBody?.Description || errorBody?.Message || message;
    } catch {
      // Keep the HTTP status message when the server did not return JSON.
    }
    throw new Error(message);
  }
  if (response.status === 204) return undefined as T;
  const text = await response.text();
  if (!text) return undefined as T;
  return JSON.parse(text) as T;
};

/** Read the settings file shared by every node (SystemAdmin only). */
export const getServerSettings = (): Promise<ServerSettings> =>
  request<ServerSettings>('GET', `${getBaseUrl()}/v1.0/settings`);

/** Persist the full settings document; returns live-vs-restart classification. */
export const updateServerSettings = (settings: ServerSettings): Promise<SettingsUpdateResult> =>
  request<SettingsUpdateResult>('PUT', `${getBaseUrl()}/v1.0/settings`, settings);

/**
 * Apply saved settings by restarting: a rolling restart of every node in cluster mode (nodes restart one at a
 * time), otherwise a clean restart of this server.
 */
export const restartServer = (): Promise<ClusterRestartResult> =>
  request<ClusterRestartResult>('POST', `${getBaseUrl()}/v1.0/settings/restart`, { confirm: true });

/** List the nodes in the cluster with their state and health (the answering node alone on a single node). */
export const getClusterNodes = (): Promise<ClusterStatus> =>
  request<ClusterStatus>('GET', `${getBaseUrl()}/v1.0/cluster/nodes`);
