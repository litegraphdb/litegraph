import React from 'react';
import { Metadata } from 'next';
import CredentialPage from '@/page/credentials/CredentialPage';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';

export const metadata: Metadata = {
  title: 'LiteGraph | Credentials',
  description: 'LiteGraph',
};

const Credentials = () => {
  return (
    <CapabilityRouteGuard resource="credentials">
      <CredentialPage />
    </CapabilityRouteGuard>
  );
};

export default Credentials;
