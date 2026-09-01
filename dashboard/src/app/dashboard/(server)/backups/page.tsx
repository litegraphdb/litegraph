import React from 'react';
import { Metadata } from 'next';
import BackupPage from '@/page/backups/BackupPage';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';

export const metadata: Metadata = {
  title: 'LiteGraph | Backups',
  description: 'LiteGraph',
};

const Backups = () => {
  return (
    <CapabilityRouteGuard resource="backups">
      <BackupPage />
    </CapabilityRouteGuard>
  );
};

export default Backups;
