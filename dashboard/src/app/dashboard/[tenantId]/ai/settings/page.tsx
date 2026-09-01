'use client';
import React from 'react';
import ChatSettingsPage from '@/page/ai/settings/ChatSettingsPage';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';

const AiSettings = () => {
  return (
    <CapabilityRouteGuard resource="aiSettings">
      <ChatSettingsPage />
    </CapabilityRouteGuard>
  );
};

export default AiSettings;
