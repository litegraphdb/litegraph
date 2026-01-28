import UserPage from '@/page/users/UserPage';
import { Metadata } from 'next';
import React from 'react';

export const metadata: Metadata = {
  title: 'LiteGraph | Users',
  description: 'LiteGraph',
};

const Users = () => {
  return <UserPage />;
};

export default Users;
