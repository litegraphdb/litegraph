import React from 'react';
import { Metadata } from 'next';
import TenantPage from '@/page/tenants/TenantPage';

export const metadata: Metadata = {
  title: 'LiteGraph | Tenants',
  description: 'LiteGraph',
};

const page = () => {
  return <TenantPage />;
};

export default page;
