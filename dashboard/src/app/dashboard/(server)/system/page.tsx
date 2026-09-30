import React from 'react';
import { Metadata } from 'next';
import HubIndexRedirect from '@/components/hub/HubIndexRedirect';

export const metadata: Metadata = {
  title: 'LiteGraph | System',
  description: 'LiteGraph',
};

const SystemHubIndex = () => {
  return <HubIndexRedirect hubId="system" />;
};

export default SystemHubIndex;
