import React from 'react';
import { Metadata } from 'next';
import EdgePage from '@/page/edges/EdgePage';

export const metadata: Metadata = {
  title: 'LiteGraph | Edges',
  description: 'LiteGraph',
};

const Edges = () => {
  return <EdgePage />;
};

export default Edges;
