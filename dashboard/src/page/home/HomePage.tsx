'use client';
import React from 'react';
import { useRouter } from 'next/navigation';
import { useEffect } from 'react';
import { paths } from '@/constants/constant';

const HomePage = () => {
  const router = useRouter();

  useEffect(() => {
    router.push(paths.login);
  }, [router]);

  return <div>Redirecting...</div>;
};

export default HomePage;
