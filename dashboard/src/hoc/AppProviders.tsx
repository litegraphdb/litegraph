'use client';
import '@ant-design/v5-patch-for-react-19';
import { createCache, StyleProvider } from '@ant-design/cssinjs';
import { AntdRegistry } from '@ant-design/nextjs-registry';
import { AppContext } from '@/hooks/appHooks';
import React, { useState, useEffect, useMemo } from 'react';
import { ThemeEnum } from '@/types/types';
import { darkTheme, primaryTheme } from '@/theme/theme';
import { ConfigProvider } from 'antd';
import AuthLayout from '@/components/layout/AuthLayout';
import StoreProvider from '@/lib/store/StoreProvider';
import { Toaster } from 'react-hot-toast';
import { localStorageKeys } from '@/constants/constant';

const AppProviders = ({ children }: { children: React.ReactNode }) => {
  // Always start with LIGHT theme to ensure consistent server/client hydration
  const [theme, setTheme] = useState<ThemeEnum>(ThemeEnum.LIGHT);

  // Create a stable cache instance
  const cache = useMemo(() => createCache(), []);

  // Load theme from localStorage only after hydration
  useEffect(() => {
    const savedTheme = localStorage.getItem(localStorageKeys.theme);
    if (savedTheme) {
      setTheme(savedTheme as ThemeEnum);
    }
  }, []);

  const handleThemeChange = (newTheme: ThemeEnum) => {
    localStorage.setItem(localStorageKeys.theme, newTheme);
    setTheme(newTheme);
  };

  return (
    <StoreProvider>
      <AppContext.Provider value={{ theme, setTheme: handleThemeChange }}>
        <StyleProvider cache={cache} hashPriority="high">
          <AntdRegistry>
            <ConfigProvider theme={theme === ThemeEnum.LIGHT ? primaryTheme : darkTheme}>
              <AuthLayout className={theme === ThemeEnum.DARK ? 'theme-dark-mode' : ''}>
                {children}
              </AuthLayout>
              <Toaster />
            </ConfigProvider>
          </AntdRegistry>
        </StyleProvider>
      </AppContext.Provider>
    </StoreProvider>
  );
};

export default AppProviders;
