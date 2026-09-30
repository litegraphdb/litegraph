import React from 'react';
import { Metadata } from 'next';
import HubIndexRedirect from '@/components/hub/HubIndexRedirect';

export const metadata: Metadata = {
  title: 'LiteGraph | Access',
  description: 'LiteGraph',
};

const AccessHubIndex = () => {
  return <HubIndexRedirect hubId="access" />;
};

export default AccessHubIndex;
