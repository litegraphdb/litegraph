import RequestHistoryPage from '@/page/request-history/RequestHistoryPage';
import React from 'react';
import { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'LiteGraph | API Requests',
  description: 'LiteGraph',
};

const Page = async ({ params }: { params: Promise<{ tenantId: string }> }) => {
  const { tenantId } = await params;
  return <RequestHistoryPage mode="tenant" tenantScope={tenantId} />;
};

export default Page;
