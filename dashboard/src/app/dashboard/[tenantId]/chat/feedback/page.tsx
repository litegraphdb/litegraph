import React from 'react';
import { Metadata } from 'next';
import FeedbackPage from '@/page/ai/feedback/FeedbackPage';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';

export const metadata: Metadata = {
  title: 'LiteGraph | Chat Feedback',
  description: 'LiteGraph',
};

const AiFeedback = () => {
  return (
    <CapabilityRouteGuard resource="aiFeedback">
      <FeedbackPage />
    </CapabilityRouteGuard>
  );
};

export default AiFeedback;
