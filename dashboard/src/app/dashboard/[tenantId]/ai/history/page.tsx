'use client';
import React from 'react';
import HistoryPage from '@/page/ai/history/HistoryPage';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';

const AiHistory = () => {
  return (
    <CapabilityRouteGuard resource="aiHistory">
      <HistoryPage />
    </CapabilityRouteGuard>
  );
};

export default AiHistory;
