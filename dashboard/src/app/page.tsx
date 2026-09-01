import React from 'react';
import { Metadata } from 'next';
import HomePage from '@/page/home/HomePage';
export const metadata: Metadata = {
  title: 'LiteGraph | Home',
  description: 'LiteGraph',
};

const Home = () => {
  return <HomePage />;
};

export default Home;
