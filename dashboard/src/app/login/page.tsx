import React from 'react';
import { Metadata } from 'next';
import LoginPage from '@/page/login/LoginPage';

export const metadata: Metadata = {
  title: 'LiteGraph | Login',
  description: 'LiteGraph',
};

const Login = () => {
  return <LoginPage />;
};

export default Login;
