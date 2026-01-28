import TenantPage from '@/page/tenants/TenantPage';
import { Metadata } from 'next';
import React from 'react';

export const metadata: Metadata = {
  title: 'LiteGraph | Tenants',
  description: 'LiteGraph',
};

const Tenants = () => {
  return <TenantPage />;
};

export default Tenants;
