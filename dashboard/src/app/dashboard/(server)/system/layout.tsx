import React from 'react';
import HubLayout from '@/components/hub/HubLayout';

const SystemHubLayout = ({ children }: Readonly<{ children: React.ReactNode }>) => {
  return <HubLayout hubId="system">{children}</HubLayout>;
};

export default SystemHubLayout;
