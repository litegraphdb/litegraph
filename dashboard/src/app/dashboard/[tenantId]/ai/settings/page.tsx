import React from 'react';
import { Metadata } from 'next';
import ChatSettingsPage from '@/page/ai/settings/ChatSettingsPage';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';

export const metadata: Metadata = {
  title: 'LiteGraph | Chat Settings',
  description: 'LiteGraph',
};

const AiSettings = () => {
  return (
    <CapabilityRouteGuard resource="aiSettings">
      <ChatSettingsPage />
    </CapabilityRouteGuard>
  );
};

export default AiSettings;
