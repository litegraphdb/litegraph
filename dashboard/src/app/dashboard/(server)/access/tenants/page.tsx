import React from 'react';
import { Metadata } from 'next';
import TenantPage from '@/page/tenants/TenantPage';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';

export const metadata: Metadata = {
  title: 'LiteGraph | Tenants',
  description: 'LiteGraph',
};

const Tenants = () => {
  return (
    <CapabilityRouteGuard resource="tenants">
      <TenantPage />
    </CapabilityRouteGuard>
  );
};

export default Tenants;
