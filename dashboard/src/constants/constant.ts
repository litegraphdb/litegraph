export const localStorageKeys = {
  tenant: 'tenant',
  token: 'token',
  adminAccessKey: 'adminAccessKey',
  user: 'user',
  serverUrl: 'serverUrl',
  theme: 'theme',
  locale: 'locale',
};

export const dynamicSlugs = {
  tenantId: ':tenantId',
};
export const paths = {
  login: `/login`,
  sso: `/sso`,
  // Tenant-scoped surfaces (DATA / METADATA / MANAGE) — bound to the active
  // tenant chosen in the header selector.
  dashboardHome: `/dashboard/${dynamicSlugs.tenantId}`,
  graphs: `/dashboard/${dynamicSlugs.tenantId}/graphs`,
  nodes: `/dashboard/${dynamicSlugs.tenantId}/nodes`,
  edges: `/dashboard/${dynamicSlugs.tenantId}/edges`,
  tags: `/dashboard/${dynamicSlugs.tenantId}/tags`,
  vectors: `/dashboard/${dynamicSlugs.tenantId}/vectors`,
  labels: `/dashboard/${dynamicSlugs.tenantId}/labels`,
  requestHistory: `/dashboard/${dynamicSlugs.tenantId}/request-history`,
  apiExplorer: `/dashboard/${dynamicSlugs.tenantId}/api-explorer`,
  // Server-level surfaces (SECURE / ADMINISTER) — one consolidated dashboard,
  // permission-filtered, not bound to a single tenant path segment.
  tenants: `/dashboard/tenants`,
  users: `/dashboard/users`,
  credentials: `/dashboard/credentials`,
  authorization: `/dashboard/authorization`,
  backups: `/dashboard/backups`,
  settings: `/dashboard/settings`,
};

export const keepUnusedDataFor = 900; //15mins

export const MAX_NODES_TO_FETCH = 500;
export const MAX_NODES_AND_EDGES_TO_FETCH_IN_SINGLE_REQUEST = 50;
