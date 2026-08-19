/**
 * Declarative schema for the curated, form-based Settings editor. Each section
 * maps to a portion of the server settings document (litegraph.json). The
 * `applies` hint states whether changes in that section take effect live or
 * need a restart; after a save the actual server response overrides the hint.
 * `serverSection` is the settings-document key the server reports back in the
 * PUT response's AppliedLive / RestartRequired arrays.
 */

export type SettingFieldType = 'text' | 'number' | 'boolean' | 'password';

export interface SettingField {
  /** Dot path into the settings document, e.g. "Logging.MinimumSeverity". */
  path: string;
  /** i18n key suffix under `settings.fields.*` for the label. */
  labelKey: string;
  type: SettingFieldType;
}

export interface SettingsSectionSchema {
  id: string;
  /** i18n key suffix under `settings.sections.*` for the section title. */
  titleKey: string;
  /** Whether changes here apply live or need a restart (pre-save hint). */
  applies: 'live' | 'restart';
  /** Settings-document key the server reports in AppliedLive/RestartRequired. */
  serverSection: string;
  fields: SettingField[];
}

export const SETTINGS_SCHEMA: SettingsSectionSchema[] = [
  {
    id: 'general',
    titleKey: 'general',
    applies: 'live',
    serverSection: 'RequestTimeoutSeconds',
    fields: [{ path: 'RequestTimeoutSeconds', labelKey: 'requestTimeoutSeconds', type: 'number' }],
  },
  {
    id: 'logging',
    titleKey: 'logging',
    applies: 'live',
    serverSection: 'Logging',
    fields: [
      { path: 'Logging.Enable', labelKey: 'loggingEnable', type: 'boolean' },
      { path: 'Logging.ConsoleLogging', labelKey: 'consoleLogging', type: 'boolean' },
      { path: 'Logging.MinimumSeverity', labelKey: 'minimumSeverity', type: 'number' },
      { path: 'Logging.LogDirectory', labelKey: 'logDirectory', type: 'text' },
      { path: 'Logging.LogFilename', labelKey: 'logFilename', type: 'text' },
      { path: 'Logging.LogQueries', labelKey: 'logQueries', type: 'boolean' },
      { path: 'Logging.LogResults', labelKey: 'logResults', type: 'boolean' },
    ],
  },
  {
    id: 'caching',
    titleKey: 'caching',
    applies: 'live',
    serverSection: 'Caching',
    fields: [
      { path: 'Caching.Enable', labelKey: 'cachingEnable', type: 'boolean' },
      { path: 'Caching.Capacity', labelKey: 'cacheCapacity', type: 'number' },
      { path: 'Caching.EvictCount', labelKey: 'cacheEvictCount', type: 'number' },
    ],
  },
  {
    id: 'requestHistory',
    titleKey: 'requestHistory',
    applies: 'live',
    serverSection: 'RequestHistory',
    fields: [
      { path: 'RequestHistory.Enable', labelKey: 'requestHistoryEnable', type: 'boolean' },
      { path: 'RequestHistory.MaxRequestBodyBytes', labelKey: 'maxRequestBodyBytes', type: 'number' },
      { path: 'RequestHistory.MaxResponseBodyBytes', labelKey: 'maxResponseBodyBytes', type: 'number' },
      { path: 'RequestHistory.RetentionDays', labelKey: 'retentionDays', type: 'number' },
      { path: 'RequestHistory.PurgeIntervalMinutes', labelKey: 'purgeIntervalMinutes', type: 'number' },
    ],
  },
  {
    id: 'observability',
    titleKey: 'observability',
    applies: 'live',
    serverSection: 'Observability',
    fields: [
      { path: 'Observability.Enable', labelKey: 'observabilityEnable', type: 'boolean' },
      { path: 'Observability.EnablePrometheus', labelKey: 'enablePrometheus', type: 'boolean' },
      { path: 'Observability.EnableOpenTelemetry', labelKey: 'enableOpenTelemetry', type: 'boolean' },
      { path: 'Observability.EnableOtlpExporter', labelKey: 'enableOtlpExporter', type: 'boolean' },
      { path: 'Observability.OtlpEndpoint', labelKey: 'otlpEndpoint', type: 'text' },
      { path: 'Observability.MetricsPath', labelKey: 'metricsPath', type: 'text' },
    ],
  },
  {
    id: 'debug',
    titleKey: 'debug',
    applies: 'live',
    serverSection: 'Debug',
    fields: [
      { path: 'Debug.Authentication', labelKey: 'debugAuthentication', type: 'boolean' },
      { path: 'Debug.Exceptions', labelKey: 'debugExceptions', type: 'boolean' },
      { path: 'Debug.Requests', labelKey: 'debugRequests', type: 'boolean' },
      { path: 'Debug.DatabaseQueries', labelKey: 'debugDatabaseQueries', type: 'boolean' },
    ],
  },
  {
    id: 'rest',
    titleKey: 'rest',
    applies: 'restart',
    serverSection: 'Rest',
    fields: [
      { path: 'Rest.Hostname', labelKey: 'restHostname', type: 'text' },
      { path: 'Rest.Port', labelKey: 'restPort', type: 'number' },
      { path: 'Rest.Ssl.Enable', labelKey: 'restSslEnable', type: 'boolean' },
    ],
  },
  {
    id: 'database',
    titleKey: 'database',
    applies: 'restart',
    serverSection: 'LiteGraph',
    fields: [
      { path: 'LiteGraph.Database.Type', labelKey: 'databaseType', type: 'text' },
      { path: 'LiteGraph.Database.Hostname', labelKey: 'databaseHostname', type: 'text' },
      { path: 'LiteGraph.Database.Port', labelKey: 'databasePort', type: 'number' },
      { path: 'LiteGraph.Database.DatabaseName', labelKey: 'databaseName', type: 'text' },
      { path: 'LiteGraph.Database.Username', labelKey: 'databaseUsername', type: 'text' },
      { path: 'LiteGraph.Database.Password', labelKey: 'databasePassword', type: 'password' },
      { path: 'LiteGraph.Database.MaxConnections', labelKey: 'databaseMaxConnections', type: 'number' },
    ],
  },
  {
    id: 'security',
    titleKey: 'security',
    applies: 'restart',
    serverSection: 'LiteGraph',
    fields: [
      { path: 'LiteGraph.AdminBearerToken', labelKey: 'adminBearerToken', type: 'password' },
      { path: 'Encryption.Key', labelKey: 'encryptionKey', type: 'password' },
      { path: 'Encryption.Iv', labelKey: 'encryptionIv', type: 'password' },
    ],
  },
];

/** Read a dot-path value from a nested object; returns undefined when absent. */
export const getPath = (obj: any, path: string): any => {
  if (!obj) return undefined;
  return path.split('.').reduce((acc, key) => (acc == null ? undefined : acc[key]), obj);
};

/** Immutably set a dot-path value, creating intermediate objects as needed. */
export const setPath = (obj: any, path: string, value: any): any => {
  const keys = path.split('.');
  const clone = Array.isArray(obj) ? [...obj] : { ...(obj || {}) };
  let cursor: any = clone;
  for (let i = 0; i < keys.length - 1; i += 1) {
    const key = keys[i];
    const next = cursor[key];
    cursor[key] = next && typeof next === 'object' ? { ...next } : {};
    cursor = cursor[key];
  }
  cursor[keys[keys.length - 1]] = value;
  return clone;
};
