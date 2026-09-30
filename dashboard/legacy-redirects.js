/**
 * Redirects from the dashboard's pre-hub page URLs to their hub tabs. Next.js keeps the query string, so deep links
 * such as `/dashboard/<tenant>/ai/history?thread=<guid>` keep working. They are temporary (307) so browsers do not
 * cache them while the navigation settles. The bare hub paths (`/dashboard/<tenant>/graphs`, `/dashboard/access`,
 * ...) are pages that open the first tab the user may view.
 */
const tenantRedirects = [
  ['nodes', 'graphs/nodes'],
  ['edges', 'graphs/edges'],
  ['labels', 'graphs/labels'],
  ['tags', 'graphs/tags'],
  ['vectors', 'graphs/vectors'],
  ['algorithms', 'graphs/algorithms'],
  ['ai', 'chat'],
  ['ai/chat', 'chat/chat'],
  ['ai/history', 'chat/history'],
  ['ai/feedback', 'chat/feedback'],
  ['ai/endpoints', 'chat/endpoints'],
  ['ai/settings', 'chat/settings'],
  ['request-history', 'developer/requests'],
  ['api-explorer', 'developer/api-explorer'],
];

const serverRedirects = [
  ['tenants', 'access/tenants'],
  ['users', 'access/users'],
  ['credentials', 'access/credentials'],
  ['authorization', 'access/authorization'],
  ['settings', 'system/settings'],
  ['cluster', 'system/cluster'],
  ['backups', 'system/backups'],
];

const legacyRedirects = [
  // Server-level pages first: `/dashboard/<name>` would otherwise read as a tenant home.
  ...serverRedirects.map(([from, to]) => ({
    source: `/dashboard/${from}`,
    destination: `/dashboard/${to}`,
    permanent: false,
  })),
  ...tenantRedirects.map(([from, to]) => ({
    source: `/dashboard/:tenantId/${from}`,
    destination: `/dashboard/:tenantId/${to}`,
    permanent: false,
  })),
];

module.exports = { legacyRedirects };
