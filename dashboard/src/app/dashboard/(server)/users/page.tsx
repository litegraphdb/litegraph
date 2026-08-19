'use client';
import React from 'react';
import UserPage from '@/page/users/UserPage';
import CapabilityRouteGuard from '@/components/route-guard/CapabilityRouteGuard';

const Users = () => {
  return (
    <CapabilityRouteGuard resource="users">
      <UserPage />
    </CapabilityRouteGuard>
  );
};

export default Users;
