'use client';
import React, { Suspense } from 'react';
import { useTranslations } from 'next-intl';
import { usePathname } from 'next/navigation';
import TabBar, { TabBarItem } from '@/components/tab-bar/TabBar';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';
import { LayoutContext } from '@/components/layout/context';
import {
  HubId,
  activeTabSlug,
  getHub,
  resolveHubBase,
  resolveHubPath,
  tenantIdFromHubBase,
  visibleHubTabs,
} from '@/constants/hubs';
import { useCurrentTenant, useSelectedGraph } from '@/hooks/entityHooks';
import { usePrincipal } from '@/hooks/permissionHooks';
import { useGetAllGraphsQuery } from '@/lib/store/slice/slice';
import GraphScopeSelector, { GRAPH_SCOPE_PARAM } from './GraphScopeSelector';
import styles from './hub.module.scss';

interface HubLayoutProps {
  hubId: HubId;
  children: React.ReactNode;
}

/**
 * Layout for a hub: the shared tab bar above the active tab's page. Only tabs the principal may view are shown; a
 * forbidden tab URL renders the standard access-denied state under the tab bar. Graph-scoped tabs get the shared
 * graph selector at the right of the bar, and links between them carry the selected graph (`?graph=`).
 */
const HubLayout = ({ hubId, children }: HubLayoutProps) => {
  const t = useTranslations();
  const principal = usePrincipal();
  const pathname = usePathname();
  const tenant = useCurrentTenant();
  const selectedGraph = useSelectedGraph();
  const hub = getHub(hubId);
  const hubBase = resolveHubBase(hub.path, pathname, tenant?.GUID);
  const tenantId = tenantIdFromHubBase(hub.path, hubBase) ?? tenant?.GUID;
  const slug = activeTabSlug(pathname, hubBase);
  const activeTab = hub.tabs.find((tab) => tab.slug === slug) ?? null;
  const hasGraphScope = hub.tabs.some((tab) => tab.graphScoped);

  const {
    isLoading: isGraphsLoading,
    error: graphError,
    refetch: refetchGraphs,
  } = useGetAllGraphsQuery(undefined, { skip: !hasGraphScope });

  const items: TabBarItem[] = visibleHubTabs(principal, hub).map((tab) => {
    const href = resolveHubPath(tab.path, tenantId);
    const scoped = tab.graphScoped && selectedGraph;
    return {
      key: tab.slug,
      label: t(tab.labelKey),
      title: t(tab.titleKey),
      href: scoped ? `${href}?${GRAPH_SCOPE_PARAM}=${encodeURIComponent(selectedGraph)}` : href,
    };
  });

  return (
    <LayoutContext.Provider
      value={{
        isGraphsLoading,
        graphError,
        refetchGraphs: hasGraphScope ? refetchGraphs : () => {},
      }}
    >
      <div className={styles.hub} data-testid={`hub-${hub.id}`}>
        <TabBar
          items={items}
          activeKey={activeTab?.slug}
          ariaLabel={t('nav.tabBarAria', { hub: t(hub.labelKey) })}
          rightSlot={
            activeTab?.graphScoped ? (
              <Suspense fallback={null}>
                <GraphScopeSelector />
              </Suspense>
            ) : undefined
          }
        />
        {activeTab ? (
          <CapabilityRouteGuard resource={activeTab.resource}>{children}</CapabilityRouteGuard>
        ) : (
          children
        )}
      </div>
    </LayoutContext.Provider>
  );
};

export default HubLayout;
