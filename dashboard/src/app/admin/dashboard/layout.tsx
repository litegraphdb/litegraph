'use client';
import DashboardLayout from '@/components/layout/DashboardLayout';
import { paths } from '@/constants/constant';
import { adminDashboardRoutes } from '@/constants/sidebar';
import { withAdminAuth } from '@/hoc/hoc';
import { usePathname } from 'next/navigation';

const RootLayout = ({ children }: Readonly<{ children: React.ReactNode }>) => {
  const pathname = usePathname();
  const isDisableTenantSelector = pathname === paths.backups || pathname === paths.adminDashboard;
  return (
    <DashboardLayout
      menuItems={adminDashboardRoutes}
      noProfile={true}
      useTenantSelector={!isDisableTenantSelector}
      isAdmin={true}
    >
      {children}
    </DashboardLayout>
  );
};

export default withAdminAuth(RootLayout);
