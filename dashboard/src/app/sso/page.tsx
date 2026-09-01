import { Metadata } from 'next';
import React from 'react';
import SsoPage from '@/page/sso/SsoPage';

export const metadata: Metadata = {
  title: 'LiteGraph | SSO',
  description: 'LiteGraph',
};

const Sso = () => {
  return <SsoPage />;
};

export default Sso;
