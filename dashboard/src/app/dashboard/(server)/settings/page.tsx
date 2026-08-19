'use client';
import React from 'react';
import SettingsPage from '@/page/settings/SettingsPage';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';

const Settings = () => {
  return (
    <CapabilityRouteGuard resource="settings">
      <SettingsPage />
    </CapabilityRouteGuard>
  );
};

export default Settings;
