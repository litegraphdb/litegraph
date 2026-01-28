import TagPage from '@/page/tags/TagPage';
import { Metadata } from 'next';
import React from 'react';

export const metadata: Metadata = {
  title: 'LiteGraph | Tags',
  description: 'LiteGraph',
};

const Tags = () => {
  return <TagPage />;
};

export default Tags;
