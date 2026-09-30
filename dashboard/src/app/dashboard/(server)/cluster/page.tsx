import React from 'react';
import { Metadata } from 'next';
import ClusterPage from '@/page/cluster/ClusterPage';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';

export const metadata: Metadata = {
  title: 'LiteGraph | Cluster',
  description: 'LiteGraph',
};

const Cluster = () => {
  return (
    <CapabilityRouteGuard resource="cluster">
      <ClusterPage />
    </CapabilityRouteGuard>
  );
};

export default Cluster;
