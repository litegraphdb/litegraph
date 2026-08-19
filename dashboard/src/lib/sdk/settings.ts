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
}

const getBaseUrl = (): string => {
  const endpoint = sdk.config.endpoint || '/';
  return endpoint.endsWith('/') ? endpoint.slice(0, -1) : endpoint;
};

const buildHeaders = (): Record<string, string> => {
  const headers: Record<string, string> = {
    Accept: 'application/json',
  };
  const defaults = (sdk.config as unknown as { defaultHeaders?: Record<string, string> })
    .defaultHeaders;
  if (defaults) {
    for (const key of Object.keys(defaults)) headers[key] = defaults[key];
  }
  const authConfig = sdk.config as unknown as { accessToken?: string; accessKey?: string };
  const bearerToken = authConfig.accessToken || authConfig.accessKey;
  if (bearerToken && !headers.Authorization) {
    headers.Authorization = `Bearer ${bearerToken}`;
  }
  return headers;
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

/** Read the current effective server settings (SystemAdmin only). */
export const getServerSettings = (): Promise<ServerSettings> =>
  request<ServerSettings>('GET', `${getBaseUrl()}/v1.0/settings`);

/** Persist the full settings document; returns live-vs-restart classification. */
export const updateServerSettings = (settings: ServerSettings): Promise<SettingsUpdateResult> =>
  request<SettingsUpdateResult>('PUT', `${getBaseUrl()}/v1.0/settings`, settings);

/** Trigger a clean server restart so the new settings file takes effect. */
export const restartServer = (): Promise<{ Success?: boolean; Message?: string }> =>
  request<{ Success?: boolean; Message?: string }>(
    'POST',
    `${getBaseUrl()}/v1.0/settings/restart`,
    { confirm: true }
  );
