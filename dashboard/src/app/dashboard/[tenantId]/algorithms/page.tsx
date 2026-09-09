import AlgorithmsPage from '@/page/algorithms/AlgorithmsPage';
import { Metadata } from 'next';
import React from 'react';

export const metadata: Metadata = {
  title: 'LiteGraph | Algorithms',
  description: 'LiteGraph',
};

const Algorithms = () => {
  return <AlgorithmsPage />;
};

export default Algorithms;
