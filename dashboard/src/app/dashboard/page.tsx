import HomePage from '@/page/home/HomePage';
import { Metadata } from 'next';
import React from 'react';

export const metadata: Metadata = {
  title: 'LiteGraph | Dashboard',
  description: 'LiteGraph',
};

const Page = () => {
  return <HomePage />;
};

export default Page;
