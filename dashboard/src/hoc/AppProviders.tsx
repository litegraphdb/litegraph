'use client';
import '@ant-design/v5-patch-for-react-19';
import { createCache } from '@ant-design/cssinjs';
import { AppContext } from '@/hooks/appHooks';
import React, { useState, useEffect, useMemo } from 'react';
import { ThemeEnum } from '@/types/types';
import StoreProvider from '@/lib/store/StoreProvider';
import LocalizedApp from '@/hoc/LocalizedApp';
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
        <LocalizedApp cache={cache} theme={theme}>
          {children}
        </LocalizedApp>
      </AppContext.Provider>
    </StoreProvider>
  );
};

export default AppProviders;
