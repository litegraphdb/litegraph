import React from 'react';
import HubLayout from '@/components/hub/HubLayout';

const GraphsHubLayout = ({ children }: Readonly<{ children: React.ReactNode }>) => {
  return <HubLayout hubId="graphs">{children}</HubLayout>;
};

export default GraphsHubLayout;
