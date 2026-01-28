import CredentialPage from '@/page/credentials/CredentialPage';
import { Metadata } from 'next';
import React from 'react';

export const metadata: Metadata = {
  title: 'LiteGraph | Credentials',
  description: 'LiteGraph',
};

const Credentials = () => {
  return <CredentialPage />;
};

export default Credentials;
