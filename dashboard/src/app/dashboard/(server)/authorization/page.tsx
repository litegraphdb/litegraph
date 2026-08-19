'use client';
import React from 'react';
import AuthorizationPage from '@/page/authorization/AuthorizationPage';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';

const Authorization = () => {
  return (
    <CapabilityRouteGuard resource="authorization">
      <AuthorizationPage />
    </CapabilityRouteGuard>
  );
};

export default Authorization;
