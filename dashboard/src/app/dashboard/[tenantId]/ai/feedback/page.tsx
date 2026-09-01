'use client';
import React from 'react';
import FeedbackPage from '@/page/ai/feedback/FeedbackPage';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';

const AiFeedback = () => {
  return (
    <CapabilityRouteGuard resource="aiFeedback">
      <FeedbackPage />
    </CapabilityRouteGuard>
  );
};

export default AiFeedback;
