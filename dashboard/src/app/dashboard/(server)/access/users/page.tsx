import React from 'react';
import { Metadata } from 'next';
import UserPage from '@/page/users/UserPage';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';

export const metadata: Metadata = {
  title: 'LiteGraph | Users',
  description: 'LiteGraph',
};

const Users = () => {
  return (
    <CapabilityRouteGuard resource="users">
      <UserPage />
    </CapabilityRouteGuard>
  );
};

export default Users;
