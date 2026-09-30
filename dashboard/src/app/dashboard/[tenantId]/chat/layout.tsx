import React from 'react';
import HubLayout from '@/components/hub/HubLayout';

const ChatHubLayout = ({ children }: Readonly<{ children: React.ReactNode }>) => {
  return <HubLayout hubId="chat">{children}</HubLayout>;
};

export default ChatHubLayout;
