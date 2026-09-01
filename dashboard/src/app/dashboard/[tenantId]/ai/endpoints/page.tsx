'use client';
import React from 'react';
import EndpointsPage from '@/page/ai/endpoints/EndpointsPage';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';

const AiEndpoints = () => {
  return (
    <CapabilityRouteGuard resource="aiEndpoints">
      <EndpointsPage />
    </CapabilityRouteGuard>
  );
};

export default AiEndpoints;
