import React from 'react';
import {
  ApiOutlined,
  CommentOutlined,
  HomeOutlined,
  SafetyCertificateOutlined,
  SettingOutlined,
  ShareAltOutlined,
} from '@ant-design/icons';
import { dynamicSlugs, paths } from './constant';
import { CapabilityResource, NavSectionId, Principal, can } from '@/lib/authz/capabilities';

export type HubId = 'home' | 'graphs' | 'chat' | 'access' | 'system' | 'developer';

/**
 * One tab of a hub. `slug` is the last path segment (`/<hub>/<slug>`); `resource` gates the tab through the
 * capability map; `graphScoped` tabs show the shared graph selector at the right of the tab bar.
 */
export interface HubTab {
  slug: string;
  resource: CapabilityResource;
  label: string;
  labelKey: string;
  titleKey: string;
  path: string;
  graphScoped?: boolean;
}

/**
 * A sidebar entry. Hubs with tabs render a tab bar in their layout; a hub is visible when at least one of its tabs
 * is. Home has no tabs and is gated by `resource`.
 */
export interface Hub {
  id: HubId;
  section: NavSectionId;
  label: string;
  labelKey: string;
  title: string;
  titleKey: string;
  icon: React.ReactNode;
  path: string;
  resource?: CapabilityResource;
  tabs: HubTab[];
}

/**
 * The single source for the dashboard's navigation: the sidebar, the hub tab bars, the bare-hub redirects and the
 * route guards all read from this list.
 */
export const dashboardHubs: Hub[] = [
  {
    id: 'home',
    section: 'workspace',
    label: 'Home',
    labelKey: 'nav.hub.home',
    title: 'Dashboard overview',
    titleKey: 'nav.hub.homeTitle',
    icon: <HomeOutlined />,
    path: paths.dashboardHome,
    resource: 'home',
    tabs: [],
  },
  {
    id: 'graphs',
    section: 'workspace',
    label: 'Graphs',
    labelKey: 'nav.hub.graphs',
    title: 'Graphs, nodes, edges, metadata, vectors and algorithms',
    titleKey: 'nav.hub.graphsTitle',
    icon: <ShareAltOutlined />,
    path: paths.graphsHub,
    tabs: [
      {
        slug: 'graphs',
        resource: 'graphs',
        label: 'Graphs',
        labelKey: 'nav.item.graphs',
        titleKey: 'nav.item.graphsTitle',
        path: paths.graphs,
        graphScoped: true,
      },
      {
        slug: 'nodes',
        resource: 'nodes',
        label: 'Nodes',
        labelKey: 'nav.item.nodes',
        titleKey: 'nav.item.nodesTitle',
        path: paths.nodes,
        graphScoped: true,
      },
      {
        slug: 'edges',
        resource: 'edges',
        label: 'Edges',
        labelKey: 'nav.item.edges',
        titleKey: 'nav.item.edgesTitle',
        path: paths.edges,
        graphScoped: true,
      },
      {
        slug: 'labels',
        resource: 'labels',
        label: 'Labels',
        labelKey: 'nav.item.labels',
        titleKey: 'nav.item.labelsTitle',
        path: paths.labels,
        graphScoped: true,
      },
      {
        slug: 'tags',
        resource: 'tags',
        label: 'Tags',
        labelKey: 'nav.item.tags',
        titleKey: 'nav.item.tagsTitle',
        path: paths.tags,
        graphScoped: true,
      },
      {
        slug: 'vectors',
        resource: 'vectors',
        label: 'Vectors',
        labelKey: 'nav.item.vectors',
        titleKey: 'nav.item.vectorsTitle',
        path: paths.vectors,
        graphScoped: true,
      },
      {
        slug: 'algorithms',
        resource: 'graphs',
        label: 'Algorithms',
        labelKey: 'nav.item.algorithms',
        titleKey: 'nav.item.algorithmsTitle',
        path: paths.algorithms,
        graphScoped: true,
      },
    ],
  },
  {
    id: 'chat',
    section: 'workspace',
    label: 'Chat',
    labelKey: 'nav.hub.chat',
    title: 'Chat with your graphs and manage chat endpoints',
    titleKey: 'nav.hub.chatTitle',
    icon: <CommentOutlined />,
    path: paths.chatHub,
    tabs: [
      {
        slug: 'chat',
        resource: 'aiChat',
        label: 'Chat',
        labelKey: 'nav.item.aiChat',
        titleKey: 'nav.item.aiChatTitle',
        path: paths.aiChat,
        graphScoped: true,
      },
      {
        slug: 'history',
        resource: 'aiHistory',
        label: 'History',
        labelKey: 'nav.item.aiHistory',
        titleKey: 'nav.item.aiHistoryTitle',
        path: paths.aiHistory,
      },
      {
        slug: 'feedback',
        resource: 'aiFeedback',
        label: 'Feedback',
        labelKey: 'nav.item.aiFeedback',
        titleKey: 'nav.item.aiFeedbackTitle',
        path: paths.aiFeedback,
      },
      {
        slug: 'endpoints',
        resource: 'aiEndpoints',
        label: 'Endpoints',
        labelKey: 'nav.item.aiEndpoints',
        titleKey: 'nav.item.aiEndpointsTitle',
        path: paths.aiEndpoints,
      },
      {
        slug: 'settings',
        resource: 'aiSettings',
        label: 'Settings',
        labelKey: 'nav.item.aiSettings',
        titleKey: 'nav.item.aiSettingsTitle',
        path: paths.aiSettings,
      },
    ],
  },
  {
    id: 'access',
    section: 'administration',
    label: 'Access',
    labelKey: 'nav.hub.access',
    title: 'Tenants, users, credentials and authorization',
    titleKey: 'nav.hub.accessTitle',
    icon: <SafetyCertificateOutlined />,
    path: paths.accessHub,
    tabs: [
      {
        slug: 'tenants',
        resource: 'tenants',
        label: 'Tenants',
        labelKey: 'nav.item.tenants',
        titleKey: 'nav.item.tenantsTitle',
        path: paths.tenants,
      },
      {
        slug: 'users',
        resource: 'users',
        label: 'Users',
        labelKey: 'nav.item.users',
        titleKey: 'nav.item.usersTitle',
        path: paths.users,
      },
      {
        slug: 'credentials',
        resource: 'credentials',
        label: 'Credentials',
        labelKey: 'nav.item.credentials',
        titleKey: 'nav.item.credentialsTitle',
        path: paths.credentials,
      },
      {
        slug: 'authorization',
        resource: 'authorization',
        label: 'Authorization',
        labelKey: 'nav.item.authorization',
        titleKey: 'nav.item.authorizationTitle',
        path: paths.authorization,
      },
    ],
  },
  {
    id: 'system',
    section: 'administration',
    label: 'System',
    labelKey: 'nav.hub.system',
    title: 'Server settings, cluster and backups',
    titleKey: 'nav.hub.systemTitle',
    icon: <SettingOutlined />,
    path: paths.systemHub,
    tabs: [
      {
        slug: 'settings',
        resource: 'settings',
        label: 'Settings',
        labelKey: 'nav.item.settings',
        titleKey: 'nav.item.settingsTitle',
        path: paths.settings,
      },
      {
        slug: 'cluster',
        resource: 'cluster',
        label: 'Cluster',
        labelKey: 'nav.item.cluster',
        titleKey: 'nav.item.clusterTitle',
        path: paths.cluster,
      },
      {
        slug: 'backups',
        resource: 'backups',
        label: 'Backups',
        labelKey: 'nav.item.backups',
        titleKey: 'nav.item.backupsTitle',
        path: paths.backups,
      },
    ],
  },
  {
    id: 'developer',
    section: 'administration',
    label: 'Developer',
    labelKey: 'nav.hub.developer',
    title: 'API request history and the API explorer',
    titleKey: 'nav.hub.developerTitle',
    icon: <ApiOutlined />,
    path: paths.developerHub,
    tabs: [
      {
        slug: 'requests',
        resource: 'requests',
        label: 'API Requests',
        labelKey: 'nav.item.requests',
        titleKey: 'nav.item.requestsTitle',
        path: paths.requestHistory,
      },
      {
        slug: 'api-explorer',
        resource: 'apiExplorer',
        label: 'API Explorer',
        labelKey: 'nav.item.apiExplorer',
        titleKey: 'nav.item.apiExplorerTitle',
        path: paths.apiExplorer,
      },
    ],
  },
];

/** Look up a hub by id. Throws when the id is unknown, which is a programming error. */
export const getHub = (id: HubId): Hub => {
  const hub = dashboardHubs.find((entry) => entry.id === id);
  if (!hub) throw new Error(`Unknown dashboard hub '${id}'.`);
  return hub;
};

/** The tabs of a hub the principal may view, in display order. */
export const visibleHubTabs = (principal: Principal | null | undefined, hub: Hub): HubTab[] =>
  hub.tabs.filter((tab) => can(principal, 'view', tab.resource));

/** Whether the hub belongs in the principal's sidebar: Home by its resource, other hubs by any visible tab. */
export const isHubVisible = (principal: Principal | null | undefined, hub: Hub): boolean => {
  if (!principal) return false;
  if (hub.tabs.length === 0) return hub.resource ? can(principal, 'view', hub.resource) : true;
  return visibleHubTabs(principal, hub).length > 0;
};

/** Replace the tenant placeholder in a hub or tab path. An empty tenant leaves the path unresolved (empty string). */
export const resolveHubPath = (path: string, tenantId: string | null | undefined): string => {
  if (!path.includes(dynamicSlugs.tenantId)) return path;
  return tenantId ? path.replace(dynamicSlugs.tenantId, tenantId) : '';
};

/**
 * The hub's base path for the current page. The tenant comes from `pathname` when it lies under the hub, so tabs
 * follow the tenant in the URL; otherwise from `fallbackTenantId` (the active tenant).
 */
export const resolveHubBase = (
  hubPath: string,
  pathname: string | null | undefined,
  fallbackTenantId: string | null | undefined
): string => {
  if (!hubPath.includes(dynamicSlugs.tenantId)) return hubPath;
  const [prefix, suffix] = hubPath.split(dynamicSlugs.tenantId);
  if (pathname && pathname.startsWith(prefix)) {
    const tenantId = pathname.slice(prefix.length).split('/')[0];
    if (tenantId && pathname.slice(prefix.length + tenantId.length).startsWith(suffix)) {
      return `${prefix}${tenantId}${suffix}`;
    }
  }
  return resolveHubPath(hubPath, fallbackTenantId);
};

/** The tenant GUID in a resolved tenant hub base, or null for server hubs. */
export const tenantIdFromHubBase = (hubPath: string, hubBase: string): string | null => {
  if (!hubPath.includes(dynamicSlugs.tenantId) || !hubBase) return null;
  const [prefix] = hubPath.split(dynamicSlugs.tenantId);
  return hubBase.slice(prefix.length).split('/')[0] || null;
};

/** The slug of the active tab for `pathname` under the resolved hub base path, or null for the bare hub path. */
export const activeTabSlug = (
  pathname: string | null | undefined,
  hubBase: string
): string | null => {
  if (!pathname || !hubBase) return null;
  const normalized = pathname.replace(/\/+$/, '');
  if (!normalized.startsWith(`${hubBase}/`)) return null;
  const rest = normalized.slice(hubBase.length + 1);
  const slug = rest.split('/')[0];
  return slug || null;
};
