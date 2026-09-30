import LabelPage from '@/page/labels/LabelPage';
import { Metadata } from 'next';
import React from 'react';

export const metadata: Metadata = {
  title: 'LiteGraph | Labels',
  description: 'LiteGraph',
};

const Labels = () => {
  return <LabelPage />;
};

export default Labels;
