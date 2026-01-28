import GraphPage from '@/page/graphs/GraphPage';
import { Metadata } from 'next';
import React from 'react';

export const metadata: Metadata = {
  title: 'LiteGraph | Graphs',
  description: 'LiteGraph',
};

const Graphs = () => {
  return <GraphPage />;
};

export default Graphs;
