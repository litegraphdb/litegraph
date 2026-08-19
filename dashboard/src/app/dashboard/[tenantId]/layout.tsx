'use client';
import ConsolidatedDashboardShell from '@/components/layout/ConsolidatedDashboardShell';
import { withAuth } from '@/hoc/hoc';

const RootLayout = ({ children }: Readonly<{ children: React.ReactNode }>) => {
  return <ConsolidatedDashboardShell useGraphsSelector={true}>{children}</ConsolidatedDashboardShell>;
};

export default withAuth(RootLayout);
