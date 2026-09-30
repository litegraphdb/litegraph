import React from 'react';
import { Metadata } from 'next';
import EndpointsPage from '@/page/ai/endpoints/EndpointsPage';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';

export const metadata: Metadata = {
  title: 'LiteGraph | AI Endpoints',
  description: 'LiteGraph',
};

const AiEndpoints = () => {
  return (
    <CapabilityRouteGuard resource="aiEndpoints">
      <EndpointsPage />
    </CapabilityRouteGuard>
  );
};

export default AiEndpoints;
