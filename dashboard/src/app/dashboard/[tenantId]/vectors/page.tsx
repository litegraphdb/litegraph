import VectorPage from '@/page/vectors/VectorPage';
import { Metadata } from 'next';
import React from 'react';

export const metadata: Metadata = {
  title: 'LiteGraph | Vectors',
  description: 'LiteGraph',
};

const Vectors = () => {
  return <VectorPage />;
};

export default Vectors;
