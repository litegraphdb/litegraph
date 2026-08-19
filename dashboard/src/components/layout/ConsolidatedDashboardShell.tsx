'use client';
import React, { useMemo } from 'react';
import DashboardLayout from './DashboardLayout';
import { buildNavForPrincipal, navSectionsToMenuItems } from '@/constants/sidebar';
import { usePrincipal } from '@/hooks/permissionHooks';

interface ConsolidatedDashboardShellProps {
  children: React.ReactNode;
  /** Show the graph selector in the header (tenant-scoped data pages). */
  useGraphsSelector?: boolean;
}

/**
 * The single dashboard shell shared by every authenticated route. It renders
 * one grouped, permission-filtered navigation (from the capability map) and the
 * common header (tenant selector + language switcher), so tenant-scoped and
 * server-level pages read as one consolidated dashboard.
 */
const ConsolidatedDashboardShell = ({
  children,
  useGraphsSelector,
}: ConsolidatedDashboardShellProps) => {
  const principal = usePrincipal();
  const menuItems = useMemo(
    () => navSectionsToMenuItems(buildNavForPrincipal(principal)),
    [principal]
  );

  return (
    <DashboardLayout
      menuItems={menuItems}
      useGraphsSelector={useGraphsSelector}
      useTenantSelector={true}
      isAdmin={principal.isSystemAdmin || principal.isBreakGlass}
    >
      {children}
    </DashboardLayout>
  );
};

export default ConsolidatedDashboardShell;
