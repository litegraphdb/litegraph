'use client';
import { StyleProvider } from '@ant-design/cssinjs';
import { AntdRegistry } from '@ant-design/nextjs-registry';
import { AppContext } from '@/hooks/appHooks';
import React, { useState } from 'react';
import { ThemeEnum } from '@/types/types';
import { darkTheme, primaryTheme } from '@/theme/theme';
import { ConfigProvider } from 'antd';
import AuthLayout from '@/components/layout/AuthLayout';
import StoreProvider from '@/lib/store/StoreProvider';
import { Toaster } from 'react-hot-toast';
import { localStorageKeys } from '@/constants/constant';

const getThemeFromLocalStorage = () => {
  let theme;
  if (typeof localStorage !== 'undefined') {
    theme = localStorage.getItem(localStorageKeys.theme);
  }
  return theme ? (theme as ThemeEnum) : ThemeEnum.LIGHT;
};

const AppProviders = ({ children }: { children: React.ReactNode }) => {
  const [theme, setTheme] = useState<ThemeEnum>(getThemeFromLocalStorage());
  const handleThemeChange = (theme: ThemeEnum) => {
    localStorage.setItem(localStorageKeys.theme, theme);
    setTheme(theme);
  };
  return (
    <StoreProvider>
      <AppContext.Provider value={{ theme, setTheme: handleThemeChange }}>
        <StyleProvider hashPriority="high">
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
