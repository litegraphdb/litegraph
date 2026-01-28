import AdminLoginPage from '@/page/admin-login/AdminLoginPage';
import { Metadata } from 'next';
import React from 'react';

export const metadata: Metadata = {
  title: 'LiteGraph | Admin Login',
  description: 'LiteGraph',
};

const AdminLogin = () => {
  return <AdminLoginPage />;
};

export default AdminLogin;
