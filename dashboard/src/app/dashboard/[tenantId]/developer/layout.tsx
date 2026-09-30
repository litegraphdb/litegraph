import React from 'react';
import HubLayout from '@/components/hub/HubLayout';

const DeveloperHubLayout = ({ children }: Readonly<{ children: React.ReactNode }>) => {
  return <HubLayout hubId="developer">{children}</HubLayout>;
};

export default DeveloperHubLayout;
