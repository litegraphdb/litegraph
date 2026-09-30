import fs from 'fs';
import path from 'path';
import {
  activeTabSlug,
  dashboardHubs,
  getHub,
  isHubVisible,
  resolveHubBase,
  resolveHubPath,
  tenantIdFromHubBase,
  visibleHubTabs,
} from '@/constants/hubs';
import { Principal } from '@/lib/authz/capabilities';

// eslint-disable-next-line @typescript-eslint/no-require-imports
const { legacyRedirects } = require('../../../legacy-redirects');
// eslint-disable-next-line @typescript-eslint/no-require-imports
const nextConfig = require('../../../next.config');

const APP = path.join(__dirname, '..', '..', 'app', 'dashboard');

/** The app route folder for a dashboard URL pattern, following the (server) group and [tenantId] segment. */
const routeFolder = (url: string): string => {
  const segments = url
    .replace(/^\/dashboard\/?/, '')
    .split('/')
    .filter(Boolean);
  if (segments[0] === ':tenantId') return path.join(APP, '[tenantId]', ...segments.slice(1));
  return path.join(APP, '(server)', ...segments);
};

const regular: Principal = {
  isSystemAdmin: false,
  isTenantAdmin: false,
  isBreakGlass: false,
  userGuid: 'u',
  tenantGuid: 't',
};
const systemAdmin: Principal = { ...regular, isSystemAdmin: true };

describe('dashboard hubs', () => {
  it('declares Home plus five tabbed hubs', () => {
    expect(dashboardHubs.map((h) => h.id)).toEqual([
      'home',
      'graphs',
      'chat',
      'access',
      'system',
      'developer',
    ]);
    expect(getHub('graphs').tabs.map((t) => t.slug)).toEqual([
      'graphs',
      'nodes',
      'edges',
      'labels',
      'tags',
      'vectors',
      'algorithms',
    ]);
    expect(getHub('chat').tabs.map((t) => t.slug)).toEqual([
      'chat',
      'history',
      'feedback',
      'endpoints',
      'settings',
    ]);
    expect(getHub('access').tabs.map((t) => t.slug)).toEqual([
      'tenants',
      'users',
      'credentials',
      'authorization',
    ]);
    expect(getHub('system').tabs.map((t) => t.slug)).toEqual(['settings', 'cluster', 'backups']);
    expect(getHub('developer').tabs.map((t) => t.slug)).toEqual(['requests', 'api-explorer']);
  });

  it('has a layout and a bare-hub page for every tabbed hub, and a page for every tab', () => {
    for (const hub of dashboardHubs.filter((h) => h.tabs.length > 0)) {
      expect(fs.existsSync(path.join(routeFolder(hub.path), 'layout.tsx'))).toBe(true);
      expect(fs.existsSync(path.join(routeFolder(hub.path), 'page.tsx'))).toBe(true);
      for (const tab of hub.tabs) {
        expect(tab.path).toBe(`${hub.path}/${tab.slug}`);
        expect(fs.existsSync(path.join(routeFolder(tab.path), 'page.tsx'))).toBe(true);
      }
    }
  });

  it('shows a hub only when at least one of its tabs is visible', () => {
    expect(isHubVisible(regular, getHub('system'))).toBe(false);
    expect(isHubVisible(systemAdmin, getHub('system'))).toBe(true);
    expect(isHubVisible(regular, getHub('home'))).toBe(true);
    expect(isHubVisible(null, getHub('home'))).toBe(false);
    expect(visibleHubTabs(regular, getHub('chat')).map((t) => t.slug)).toEqual(['chat']);
  });

  it('marks the graph tabs and the Chat tab as graph-scoped', () => {
    expect(getHub('graphs').tabs.every((t) => t.graphScoped)).toBe(true);
    expect(
      getHub('chat')
        .tabs.filter((t) => t.graphScoped)
        .map((t) => t.slug)
    ).toEqual(['chat']);
  });
});

describe('hub path helpers', () => {
  it('resolves the tenant placeholder', () => {
    expect(resolveHubPath('/dashboard/:tenantId/graphs', 't1')).toBe('/dashboard/t1/graphs');
    expect(resolveHubPath('/dashboard/:tenantId/graphs', null)).toBe('');
    expect(resolveHubPath('/dashboard/access', null)).toBe('/dashboard/access');
  });

  it('takes the hub base tenant from the URL, falling back to the active tenant', () => {
    const graphs = '/dashboard/:tenantId/graphs';
    expect(resolveHubBase(graphs, '/dashboard/url/graphs/nodes', 'redux')).toBe(
      '/dashboard/url/graphs'
    );
    expect(resolveHubBase(graphs, '/dashboard/url/chat/chat', 'redux')).toBe(
      '/dashboard/redux/graphs'
    );
    expect(resolveHubBase('/dashboard/system', '/dashboard/system/cluster', 'redux')).toBe(
      '/dashboard/system'
    );
    expect(tenantIdFromHubBase(graphs, '/dashboard/url/graphs')).toBe('url');
    expect(tenantIdFromHubBase('/dashboard/system', '/dashboard/system')).toBeNull();
  });

  it('finds the active tab slug', () => {
    expect(activeTabSlug('/dashboard/t/graphs/nodes', '/dashboard/t/graphs')).toBe('nodes');
    expect(activeTabSlug('/dashboard/t/graphs/nodes/', '/dashboard/t/graphs')).toBe('nodes');
    expect(activeTabSlug('/dashboard/t/graphs', '/dashboard/t/graphs')).toBeNull();
    expect(activeTabSlug('/dashboard/t/chat/chat', '/dashboard/t/graphs')).toBeNull();
  });
});

describe('legacy page redirects', () => {
  const byOld = (source: string) =>
    legacyRedirects.find((r: { source: string }) => r.source === source)?.destination;

  it('are served by next.config', async () => {
    expect(await nextConfig.redirects()).toEqual(legacyRedirects);
  });

  it('send every pre-hub page URL to its tab', () => {
    const expected: Record<string, string> = {
      '/dashboard/:tenantId/nodes': '/dashboard/:tenantId/graphs/nodes',
      '/dashboard/:tenantId/edges': '/dashboard/:tenantId/graphs/edges',
      '/dashboard/:tenantId/labels': '/dashboard/:tenantId/graphs/labels',
      '/dashboard/:tenantId/tags': '/dashboard/:tenantId/graphs/tags',
      '/dashboard/:tenantId/vectors': '/dashboard/:tenantId/graphs/vectors',
      '/dashboard/:tenantId/algorithms': '/dashboard/:tenantId/graphs/algorithms',
      '/dashboard/:tenantId/ai/chat': '/dashboard/:tenantId/chat/chat',
      '/dashboard/:tenantId/ai/history': '/dashboard/:tenantId/chat/history',
      '/dashboard/:tenantId/ai/feedback': '/dashboard/:tenantId/chat/feedback',
      '/dashboard/:tenantId/ai/endpoints': '/dashboard/:tenantId/chat/endpoints',
      '/dashboard/:tenantId/ai/settings': '/dashboard/:tenantId/chat/settings',
      '/dashboard/:tenantId/request-history': '/dashboard/:tenantId/developer/requests',
      '/dashboard/:tenantId/api-explorer': '/dashboard/:tenantId/developer/api-explorer',
      '/dashboard/tenants': '/dashboard/access/tenants',
      '/dashboard/users': '/dashboard/access/users',
      '/dashboard/credentials': '/dashboard/access/credentials',
      '/dashboard/authorization': '/dashboard/access/authorization',
      '/dashboard/settings': '/dashboard/system/settings',
      '/dashboard/cluster': '/dashboard/system/cluster',
      '/dashboard/backups': '/dashboard/system/backups',
    };
    for (const [source, destination] of Object.entries(expected)) {
      expect(byOld(source)).toBe(destination);
    }
  });

  it('point at routes that exist, and the old page files are gone', () => {
    for (const redirect of legacyRedirects) {
      expect(fs.existsSync(path.join(routeFolder(redirect.destination), 'page.tsx'))).toBe(true);
      expect(fs.existsSync(path.join(routeFolder(redirect.source), 'page.tsx'))).toBe(false);
    }
  });

  it('keep the query string (no query-dropping destination) and are temporary', () => {
    for (const redirect of legacyRedirects) {
      expect(redirect.destination).not.toContain('?');
      expect(redirect.permanent).toBe(false);
    }
  });

  it('list server pages before the tenant patterns', () => {
    const firstTenant = legacyRedirects.findIndex((r: { source: string }) =>
      r.source.includes(':tenantId')
    );
    const lastServer = legacyRedirects
      .map((r: { source: string }) => r.source.includes(':tenantId'))
      .lastIndexOf(false);
    expect(lastServer).toBeLessThan(firstTenant);
  });
});
