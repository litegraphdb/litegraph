import React from 'react';
import { Metadata } from 'next';
import SettingsPage from '@/page/settings/SettingsPage';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';

export const metadata: Metadata = {
  title: 'LiteGraph | Settings',
  description: 'LiteGraph',
};

const Settings = () => {
  return (
    <CapabilityRouteGuard resource="settings">
      <SettingsPage />
    </CapabilityRouteGuard>
  );
};

export default Settings;
