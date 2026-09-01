import React from 'react';
import { Metadata } from 'next';
import ChatPage from '@/page/ai/chat/ChatPage';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';

export const metadata: Metadata = {
  title: 'LiteGraph | Chat',
  description: 'LiteGraph',
};

const AiChat = () => {
  return (
    <CapabilityRouteGuard resource="aiChat">
      <ChatPage />
    </CapabilityRouteGuard>
  );
};

export default AiChat;
