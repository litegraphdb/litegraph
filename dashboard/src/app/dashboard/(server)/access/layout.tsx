import React from 'react';
import HubLayout from '@/components/hub/HubLayout';

const AccessHubLayout = ({ children }: Readonly<{ children: React.ReactNode }>) => {
  return <HubLayout hubId="access">{children}</HubLayout>;
};

export default AccessHubLayout;
