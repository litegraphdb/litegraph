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
  // Tenant-scoped surfaces, bound to the active tenant chosen in the header selector. Hubs group related pages as
  // tabs: /dashboard/<tenant>/<hub>/<tab>. A bare hub path redirects to its first visible tab.
  dashboardHome: `/dashboard/${dynamicSlugs.tenantId}`,
  graphsHub: `/dashboard/${dynamicSlugs.tenantId}/graphs`,
  graphs: `/dashboard/${dynamicSlugs.tenantId}/graphs/graphs`,
  nodes: `/dashboard/${dynamicSlugs.tenantId}/graphs/nodes`,
  edges: `/dashboard/${dynamicSlugs.tenantId}/graphs/edges`,
  labels: `/dashboard/${dynamicSlugs.tenantId}/graphs/labels`,
  tags: `/dashboard/${dynamicSlugs.tenantId}/graphs/tags`,
  vectors: `/dashboard/${dynamicSlugs.tenantId}/graphs/vectors`,
  algorithms: `/dashboard/${dynamicSlugs.tenantId}/graphs/algorithms`,
  chatHub: `/dashboard/${dynamicSlugs.tenantId}/chat`,
  aiChat: `/dashboard/${dynamicSlugs.tenantId}/chat/chat`,
  aiHistory: `/dashboard/${dynamicSlugs.tenantId}/chat/history`,
  aiFeedback: `/dashboard/${dynamicSlugs.tenantId}/chat/feedback`,
  aiEndpoints: `/dashboard/${dynamicSlugs.tenantId}/chat/endpoints`,
  aiSettings: `/dashboard/${dynamicSlugs.tenantId}/chat/settings`,
  developerHub: `/dashboard/${dynamicSlugs.tenantId}/developer`,
  requestHistory: `/dashboard/${dynamicSlugs.tenantId}/developer/requests`,
  apiExplorer: `/dashboard/${dynamicSlugs.tenantId}/developer/api-explorer`,
  // Server-level hubs, permission-filtered and not bound to a tenant path segment.
  accessHub: `/dashboard/access`,
  tenants: `/dashboard/access/tenants`,
  users: `/dashboard/access/users`,
  credentials: `/dashboard/access/credentials`,
  authorization: `/dashboard/access/authorization`,
  systemHub: `/dashboard/system`,
  settings: `/dashboard/system/settings`,
  cluster: `/dashboard/system/cluster`,
  backups: `/dashboard/system/backups`,
};

export const keepUnusedDataFor = 900; //15mins

export const MAX_NODES_TO_FETCH = 500;
export const MAX_NODES_AND_EDGES_TO_FETCH_IN_SINGLE_REQUEST = 50;
