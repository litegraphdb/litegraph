'use client';
import React, { useEffect } from 'react';
import { usePathname, useRouter } from 'next/navigation';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';
import {
  HubId,
  getHub,
  resolveHubBase,
  resolveHubPath,
  tenantIdFromHubBase,
  visibleHubTabs,
} from '@/constants/hubs';
import { useCurrentTenant } from '@/hooks/entityHooks';
import { usePrincipal } from '@/hooks/permissionHooks';

interface HubIndexRedirectProps {
  hubId: HubId;
}

/**
 * Page for a bare hub path: replaces the URL with the hub's first tab the principal may view, keeping the query
 * string (so an old `/graphs?graph=<guid>` link lands on the Graphs tab with that graph). When no tab is viewable it
 * renders the standard access-denied state.
 */
const HubIndexRedirect = ({ hubId }: HubIndexRedirectProps) => {
  const router = useRouter();
  const pathname = usePathname();
  const principal = usePrincipal();
  const tenant = useCurrentTenant();
  const hub = getHub(hubId);
  const hubBase = resolveHubBase(hub.path, pathname, tenant?.GUID);
  const tenantId = tenantIdFromHubBase(hub.path, hubBase) ?? tenant?.GUID;
  const firstTab = visibleHubTabs(principal, hub)[0];
  const target = firstTab ? resolveHubPath(firstTab.path, tenantId) : '';

  useEffect(() => {
    if (target) router.replace(`${target}${window.location.search}`);
  }, [target, router]);

  if (!firstTab) {
    return (
      <CapabilityRouteGuard resource={hub.tabs[0]?.resource ?? 'home'}>
        <span />
      </CapabilityRouteGuard>
    );
  }
  return <div data-testid="hub-index-redirect" />;
};

export default HubIndexRedirect;
