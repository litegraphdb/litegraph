import React from 'react';
import { DarkModeSwitch } from 'react-toggle-dark-mode';
import { useAppContext } from '@/hooks/appHooks';
import { ThemeEnum } from '@/types/types';

const DarkModeToggle = DarkModeSwitch as React.ComponentType<{
  checked: boolean;
  onChange: (checked: boolean) => void;
  size: number;
}>;

const ThemeModeSwitch = () => {
  const { theme, setTheme } = useAppContext();
  return (
    <DarkModeToggle
      checked={theme === ThemeEnum.DARK}
      onChange={(checked) => setTheme(checked ? ThemeEnum.DARK : ThemeEnum.LIGHT)}
      size={20}
    />
  );
};

export default ThemeModeSwitch;
