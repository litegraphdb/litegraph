export const localStorageKeys = {
  tenant: 'tenant',
  token: 'token',
  adminAccessKey: 'adminAccessKey',
  user: 'user',
  serverUrl: 'serverUrl',
  theme: 'theme',
};

export const dynamicSlugs = {
  tenantId: ':tenantId',
};
export const paths = {
  login: `/login`,
  sso: `/sso`,
  dashboardHome: `/dashboard/${dynamicSlugs.tenantId}`,
  graphs: `/dashboard/${dynamicSlugs.tenantId}/graphs`,
  nodes: `/dashboard/${dynamicSlugs.tenantId}/nodes`,
  edges: `/dashboard/${dynamicSlugs.tenantId}/edges`,
  tags: `/dashboard/${dynamicSlugs.tenantId}/tags`,
  vectors: `/dashboard/${dynamicSlugs.tenantId}/vectors`,
  labels: `/dashboard/${dynamicSlugs.tenantId}/labels`,
  adminLogin: `/login/admin`,
  adminDashboard: `/admin/dashboard`,
  credentials: `/admin/dashboard/credentials`,
  users: `/admin/dashboard/users`,
  tenants: `/admin/dashboard/tenants`,
  backups: `/admin/dashboard/backups`,
};

export const keepUnusedDataFor = 900; //15mins

export const MAX_NODES_TO_FETCH = 500;
export const MAX_NODES_AND_EDGES_TO_FETCH_IN_SINGLE_REQUEST = 50;
