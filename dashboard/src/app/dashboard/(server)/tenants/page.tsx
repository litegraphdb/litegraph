'use client';
import React from 'react';
import TenantPage from '@/page/tenants/TenantPage';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';

const Tenants = () => {
  return (
    <CapabilityRouteGuard resource="tenants">
      <TenantPage />
    </CapabilityRouteGuard>
  );
};

export default Tenants;
