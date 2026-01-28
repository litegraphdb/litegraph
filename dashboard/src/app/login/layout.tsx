'use client';
import { forGuest } from '@/hoc/hoc';
import React from 'react';

const Layout = ({ children }: { children: React.ReactNode }) => {
  return <>{children}</>;
};

export default forGuest(Layout);
