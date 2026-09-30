import React from 'react';
import { Metadata } from 'next';
import HubIndexRedirect from '@/components/hub/HubIndexRedirect';

export const metadata: Metadata = {
  title: 'LiteGraph | Developer',
  description: 'LiteGraph',
};

const DeveloperHubIndex = () => {
  return <HubIndexRedirect hubId="developer" />;
};

export default DeveloperHubIndex;
