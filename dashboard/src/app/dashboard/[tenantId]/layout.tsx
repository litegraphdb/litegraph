'use client';
import DashboardLayout from '@/components/layout/DashboardLayout';
import { tenantDashboardRoutes } from '@/constants/sidebar';
import { withAuth } from '@/hoc/hoc';

const RootLayout = ({ children }: Readonly<{ children: React.ReactNode }>) => {
  return (
    <DashboardLayout menuItems={tenantDashboardRoutes} useGraphsSelector={true}>
      {children}
    </DashboardLayout>
  );
};

export default withAuth(RootLayout);
