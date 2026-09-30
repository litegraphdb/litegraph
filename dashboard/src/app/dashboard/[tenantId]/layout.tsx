'use client';
import { usePathname } from 'next/navigation';
import ConsolidatedDashboardShell from '@/components/layout/ConsolidatedDashboardShell';
import { withAuth } from '@/hoc/hoc';

// Only the tenant Home page keeps the header graph selector; graph-scoped hub tabs carry their own selector in the tab
// bar, so a page never shows two.
const isTenantHome = (pathname: string | null) => /^\/dashboard\/[^/]+\/?$/.test(pathname ?? '');

const RootLayout = ({ children }: Readonly<{ children: React.ReactNode }>) => {
  const pathname = usePathname();
  return (
    <ConsolidatedDashboardShell useGraphsSelector={isTenantHome(pathname)}>
      {children}
    </ConsolidatedDashboardShell>
  );
};

export default withAuth(RootLayout);
