'use client';
import React from 'react';
import CredentialPage from '@/page/credentials/CredentialPage';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';

const Credentials = () => {
  return (
    <CapabilityRouteGuard resource="credentials">
      <CredentialPage />
    </CapabilityRouteGuard>
  );
};

export default Credentials;
