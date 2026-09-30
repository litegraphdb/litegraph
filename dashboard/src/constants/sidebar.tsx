import { MenuItemProps } from '@/components/menu-item/types';
import {
  CapabilityResource,
  NavSectionId,
  Principal,
  canViewSection,
} from '@/lib/authz/capabilities';
import { HubId, dashboardHubs, getHub, isHubVisible, visibleHubTabs } from './hubs';

/**
 * A single sidebar entry: one hub. `label`/`title` hold the English source strings (fallbacks and unit tests);
 * `labelKey`/`titleKey` reference the catalog and are what render. `resources` lists the capability resources whose
 * tabs the principal may view; the entry links to the hub path, which opens its first visible tab.
 */
export interface NavItem extends MenuItemProps {
  hubId: HubId;
  resources: CapabilityResource[];
}

/** A grouped navigation section with a `nav.section.*` header key. */
export interface NavSection {
  id: NavSectionId;
  labelKey: string;
  label: string;
  items: NavItem[];
}

const sectionLabels: Record<NavSectionId, { label: string; labelKey: string }> = {
  workspace: { label: 'Workspace', labelKey: 'nav.section.workspace' },
  administration: { label: 'Administration', labelKey: 'nav.section.administration' },
};

const sectionOrder: NavSectionId[] = ['workspace', 'administration'];

/**
 * The consolidated sidebar: two groups and one entry per hub, derived from `dashboardHubs`. WORKSPACE (Home, Graphs,
 * Chat) is tenant-scoped; ADMINISTRATION holds Access and System (server-level) and Developer (tenant-scoped).
 */
export const dashboardNavSections: NavSection[] = sectionOrder.map((id) => ({
  id,
  ...sectionLabels[id],
  items: dashboardHubs
    .filter((hub) => hub.section === id)
    .map((hub) => ({
      key: `/${hub.id}`,
      hubId: hub.id,
      resources:
        hub.tabs.length > 0 ? hub.tabs.map((tab) => tab.resource) : [hub.resource ?? 'home'],
      icon: hub.icon,
      label: hub.label,
      title: hub.title,
      labelKey: hub.labelKey,
      titleKey: hub.titleKey,
      path: hub.path,
      // Hubs stay highlighted on every tab; Home only on its own path.
      matchPrefix: hub.tabs.length > 0,
    })),
}));

/**
 * Produce the visible, permission-filtered grouped nav for a principal. A hub is kept when at least one of its tabs
 * is viewable (Home when its resource is), and empty groups are dropped. Each kept item's `resources` narrows to the
 * tabs the principal can see.
 */
export const buildNavForPrincipal = (principal: Principal | null | undefined): NavSection[] => {
  if (!principal) return [];
  return dashboardNavSections
    .filter((section) => canViewSection(principal, section.id))
    .map((section) => ({
      ...section,
      items: section.items
        .filter((item) => isHubVisible(principal, getHub(item.hubId)))
        .map((item) => {
          const hub = getHub(item.hubId);
          return hub.tabs.length > 0
            ? { ...item, resources: visibleHubTabs(principal, hub).map((tab) => tab.resource) }
            : item;
        }),
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
