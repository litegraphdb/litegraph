import React from 'react';
import { Metadata } from 'next';
import AuthorizationPage from '@/page/authorization/AuthorizationPage';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';

export const metadata: Metadata = {
  title: 'LiteGraph | Authorization',
  description: 'LiteGraph',
};

const Authorization = () => {
  return (
    <CapabilityRouteGuard resource="authorization">
      <AuthorizationPage />
    </CapabilityRouteGuard>
  );
};

export default Authorization;
