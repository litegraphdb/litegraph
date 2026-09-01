import { paths } from './constant';
import {
  HomeOutlined,
  ShareAltOutlined,
  ApartmentOutlined,
  BranchesOutlined,
  CrownOutlined,
  TeamOutlined,
  TagOutlined,
  TagsOutlined,
  RadarChartOutlined,
  LockOutlined,
  SaveOutlined,
  HistoryOutlined,
  ApiOutlined,
  SafetyCertificateOutlined,
  SettingOutlined,
  CommentOutlined,
  CloudServerOutlined,
  FieldTimeOutlined,
  LikeOutlined,
  SlidersOutlined,
} from '@ant-design/icons';
import { MenuItemProps } from '@/components/menu-item/types';
import {
  CapabilityResource,
  NavSectionId,
  Principal,
  can,
  canViewSection,
} from '@/lib/authz/capabilities';

/**
 * A single navigation entry. `label`/`title` hold the English source strings
 * (fallbacks + used in unit tests); `labelKey`/`titleKey` reference the catalog
 * and are what actually render, translated at runtime in MenuItems. `resource`
 * ties the item to the capability map for permission gating.
 */
export interface NavItem extends MenuItemProps {
  resource: CapabilityResource;
}

/** A grouped navigation section with an `nav.section.*` header key. */
export interface NavSection {
  id: NavSectionId;
  labelKey: string;
  label: string;
  items: NavItem[];
}

/**
 * The single, consolidated, grouped navigation for the v8.0 dashboard. Section
 * headers are `nav.section.*` keys. DATA / METADATA / MANAGE are tenant-scoped
 * (their paths embed the active tenant); SECURE / ADMINISTER are server-level.
 */
export const dashboardNavSections: NavSection[] = [
  {
    id: 'home',
    labelKey: 'nav.section.home',
    label: 'Home',
    items: [
      {
        key: '/',
        resource: 'home',
        icon: <HomeOutlined />,
        label: 'Home',
        title: 'Dashboard overview',
        labelKey: 'nav.item.home',
        titleKey: 'nav.item.homeTitle',
        path: paths.dashboardHome,
      },
    ],
  },
  {
    id: 'data',
    labelKey: 'nav.section.data',
    label: 'Data',
    items: [
      {
        key: '/graphs',
        resource: 'graphs',
        icon: <ShareAltOutlined />,
        label: 'Graphs',
        title: 'Manage graph containers',
        labelKey: 'nav.item.graphs',
        titleKey: 'nav.item.graphsTitle',
        path: paths.graphs,
      },
      {
        key: '/nodes',
        resource: 'nodes',
        icon: <ApartmentOutlined />,
        label: 'Nodes',
        title: 'Manage graph nodes',
        labelKey: 'nav.item.nodes',
        titleKey: 'nav.item.nodesTitle',
        path: paths.nodes,
      },
      {
        key: '/edges',
        resource: 'edges',
        icon: <BranchesOutlined />,
        label: 'Edges',
        title: 'Manage graph edges',
        labelKey: 'nav.item.edges',
        titleKey: 'nav.item.edgesTitle',
        path: paths.edges,
      },
    ],
  },
  {
    id: 'metadata',
    labelKey: 'nav.section.metadata',
    label: 'Metadata',
    items: [
      {
        key: '/labels',
        resource: 'labels',
        icon: <TagOutlined />,
        label: 'Labels',
        title: 'Manage classification labels',
        labelKey: 'nav.item.labels',
        titleKey: 'nav.item.labelsTitle',
        path: paths.labels,
      },
      {
        key: '/tags',
        resource: 'tags',
        icon: <TagsOutlined />,
        label: 'Tags',
        title: 'Manage key-value tags',
        labelKey: 'nav.item.tags',
        titleKey: 'nav.item.tagsTitle',
        path: paths.tags,
      },
      {
        key: '/vectors',
        resource: 'vectors',
        icon: <RadarChartOutlined />,
        label: 'Vectors',
        title: 'Manage vector embeddings',
        labelKey: 'nav.item.vectors',
        titleKey: 'nav.item.vectorsTitle',
        path: paths.vectors,
      },
    ],
  },
  {
    id: 'ai',
    labelKey: 'nav.section.ai',
    label: 'AI',
    items: [
      {
        key: '/ai/chat',
        resource: 'aiChat',
        icon: <CommentOutlined />,
        label: 'Chat',
        title: 'Chat with your graphs',
        labelKey: 'nav.item.aiChat',
        titleKey: 'nav.item.aiChatTitle',
        path: paths.aiChat,
      },
      {
        key: '/ai/endpoints',
        resource: 'aiEndpoints',
        icon: <CloudServerOutlined />,
        label: 'Endpoints',
        title: 'Manage LLM endpoints',
        labelKey: 'nav.item.aiEndpoints',
        titleKey: 'nav.item.aiEndpointsTitle',
        path: paths.aiEndpoints,
      },
      {
        key: '/ai/history',
        resource: 'aiHistory',
        icon: <FieldTimeOutlined />,
        label: 'Chat History',
        title: 'Browse chat threads and turns',
        labelKey: 'nav.item.aiHistory',
        titleKey: 'nav.item.aiHistoryTitle',
        path: paths.aiHistory,
      },
      {
        key: '/ai/feedback',
        resource: 'aiFeedback',
        icon: <LikeOutlined />,
        label: 'Feedback',
        title: 'Review chat feedback',
        labelKey: 'nav.item.aiFeedback',
        titleKey: 'nav.item.aiFeedbackTitle',
        path: paths.aiFeedback,
      },
      {
        key: '/ai/settings',
        resource: 'aiSettings',
        icon: <SlidersOutlined />,
        label: 'Chat Settings',
        title: 'Tenant chat configuration',
        labelKey: 'nav.item.aiSettings',
        titleKey: 'nav.item.aiSettingsTitle',
        path: paths.aiSettings,
      },
    ],
  },
  {
    id: 'manage',
    labelKey: 'nav.section.manage',
    label: 'Manage',
    items: [
      {
        key: '/request-history',
        resource: 'requests',
        icon: <HistoryOutlined />,
        label: 'API Requests',
        title: 'HTTP request history',
        labelKey: 'nav.item.requests',
        titleKey: 'nav.item.requestsTitle',
        path: paths.requestHistory,
      },
      {
        key: '/api-explorer',
        resource: 'apiExplorer',
        icon: <ApiOutlined />,
        label: 'API Explorer',
        title: 'Explore and invoke API endpoints',
        labelKey: 'nav.item.apiExplorer',
        titleKey: 'nav.item.apiExplorerTitle',
        path: paths.apiExplorer,
      },
    ],
  },
  {
    id: 'secure',
    labelKey: 'nav.section.secure',
    label: 'Secure',
    items: [
      {
        key: '/tenants',
        resource: 'tenants',
        icon: <CrownOutlined />,
        label: 'Tenants',
        title: 'Manage tenants',
        labelKey: 'nav.item.tenants',
        titleKey: 'nav.item.tenantsTitle',
        path: paths.tenants,
      },
      {
        key: '/users',
        resource: 'users',
        icon: <TeamOutlined />,
        label: 'Users',
        title: 'Manage user accounts',
        labelKey: 'nav.item.users',
        titleKey: 'nav.item.usersTitle',
        path: paths.users,
      },
      {
        key: '/credentials',
        resource: 'credentials',
        icon: <LockOutlined />,
        label: 'Credentials',
        title: 'Manage API credentials',
        labelKey: 'nav.item.credentials',
        titleKey: 'nav.item.credentialsTitle',
        path: paths.credentials,
      },
      {
        key: '/authorization',
        resource: 'authorization',
        icon: <SafetyCertificateOutlined />,
        label: 'Authorization',
        title: 'Manage roles and credential scopes',
        labelKey: 'nav.item.authorization',
        titleKey: 'nav.item.authorizationTitle',
        path: paths.authorization,
      },
    ],
  },
  {
    id: 'administer',
    labelKey: 'nav.section.administer',
    label: 'Administer',
    items: [
      {
        key: '/backups',
        resource: 'backups',
        icon: <SaveOutlined />,
        label: 'Backup',
        title: 'Manage database backups',
        labelKey: 'nav.item.backups',
        titleKey: 'nav.item.backupsTitle',
        path: paths.backups,
      },
      {
        key: '/settings',
        resource: 'settings',
        icon: <SettingOutlined />,
        label: 'Settings',
        title: 'Server configuration',
        labelKey: 'nav.item.settings',
        titleKey: 'nav.item.settingsTitle',
        path: paths.settings,
      },
    ],
  },
];

/**
 * Produce the visible, permission-filtered grouped nav for a principal. Empty
 * sections (and sections the principal cannot view) are dropped; within a
 * section, only viewable items are kept. Consumed by the sidebar so navigation,
 * route guards, and controls all read from one capability map.
 */
export const buildNavForPrincipal = (
  principal: Principal | null | undefined
): NavSection[] => {
  if (!principal) return [];
  return dashboardNavSections
    .filter((section) => canViewSection(principal, section.id))
    .map((section) => ({
      ...section,
      items: section.items.filter((item) => can(principal, 'view', item.resource)),
    }))
    .filter((section) => section.items.length > 0);
};

/** Convert visible sections into antd Menu group items for MenuItems. */
export const navSectionsToMenuItems = (sections: NavSection[]): MenuItemProps[] =>
  sections.map((section) => ({
    key: `section:${section.id}`,
    type: 'group',
    label: section.label,
    labelKey: section.labelKey,
    children: section.items,
  }));
