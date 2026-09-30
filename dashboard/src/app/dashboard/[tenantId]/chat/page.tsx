import React from 'react';
import { Metadata } from 'next';
import HubIndexRedirect from '@/components/hub/HubIndexRedirect';

export const metadata: Metadata = {
  title: 'LiteGraph | Chat',
  description: 'LiteGraph',
};

const ChatHubIndex = () => {
  return <HubIndexRedirect hubId="chat" />;
};

export default ChatHubIndex;
