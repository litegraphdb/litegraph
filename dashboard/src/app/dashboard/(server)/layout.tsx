'use client';
import ConsolidatedDashboardShell from '@/components/layout/ConsolidatedDashboardShell';
import { withAuth } from '@/hoc/hoc';

const ServerLayout = ({ children }: Readonly<{ children: React.ReactNode }>) => {
  return <ConsolidatedDashboardShell useGraphsSelector={false}>{children}</ConsolidatedDashboardShell>;
};

export default withAuth(ServerLayout);
