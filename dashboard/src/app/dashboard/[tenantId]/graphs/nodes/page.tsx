import NodePage from '@/page/nodes/NodePage';
import { Metadata } from 'next';
import React from 'react';

export const metadata: Metadata = {
  title: 'LiteGraph | Nodes',
  description: 'LiteGraph',
};

const Nodes = () => {
  return <NodePage />;
};

export default Nodes;
