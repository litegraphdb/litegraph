import ApiExplorerPage from '@/page/api-explorer/ApiExplorerPage';
import React from 'react';
import { Metadata } from 'next';

export const metadata: Metadata = {
  title: 'LiteGraph | API Explorer',
  description: 'LiteGraph',
};

const Page = () => {
  return <ApiExplorerPage />;
};

export default Page;
