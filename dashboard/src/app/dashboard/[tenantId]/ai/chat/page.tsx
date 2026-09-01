'use client';
import React from 'react';
import ChatPage from '@/page/ai/chat/ChatPage';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';

const AiChat = () => {
  return (
    <CapabilityRouteGuard resource="aiChat">
      <ChatPage />
    </CapabilityRouteGuard>
  );
};

export default AiChat;
