import React from 'react';
import { Metadata } from 'next';
import HubIndexRedirect from '@/components/hub/HubIndexRedirect';

export const metadata: Metadata = {
  title: 'LiteGraph | Graphs',
  description: 'LiteGraph',
};

const GraphsHubIndex = () => {
  return <HubIndexRedirect hubId="graphs" />;
};

export default GraphsHubIndex;
