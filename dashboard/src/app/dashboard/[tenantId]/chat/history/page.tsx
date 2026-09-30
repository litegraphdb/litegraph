import React from 'react';
import { Metadata } from 'next';
import HistoryPage from '@/page/ai/history/HistoryPage';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';

export const metadata: Metadata = {
  title: 'LiteGraph | Chat History',
  description: 'LiteGraph',
};

const AiHistory = () => {
  return (
    <CapabilityRouteGuard resource="aiHistory">
      <HistoryPage />
    </CapabilityRouteGuard>
  );
};

export default AiHistory;
