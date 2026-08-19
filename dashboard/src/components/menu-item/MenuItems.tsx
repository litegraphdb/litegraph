'use client';

import { Menu, MenuProps } from 'antd';
import React, { useMemo } from 'react';
import { MenuItemProps } from './types';
import { useAppDynamicNavigation } from '@/hooks/hooks';
import { ItemType } from 'antd/es/menu/interface';
import Link from 'next/link';
import { usePathname } from 'next/navigation';
import { useTranslations } from 'next-intl';

interface MenuItemsProps extends MenuProps {
  menuItems: MenuItemProps[];
  handleClickMenuItem?: (item: MenuItemProps) => void;
}

const MenuItems = ({ menuItems, handleClickMenuItem, ...rest }: MenuItemsProps) => {
  const { serializePath } = useAppDynamicNavigation();
  const pathname = usePathname();
  const t = useTranslations();
  const translate = (key: string | undefined, fallback: string | undefined) =>
    key ? t(key) : fallback;

  const selectedKeys = useMemo(() => {
    const find = (items: MenuItemProps[]): string[] => {
      for (const item of items) {
        if (item.path) {
          const serialized = serializePath(item.path);
          if (serialized && pathname === serialized) {
            return [item.key];
          }
        }
        if (item.children) {
          const childMatch = find(item.children);
          if (childMatch.length > 0) return childMatch;
        }
      }
      return [];
    };
    return find(menuItems);
  }, [menuItems, pathname, serializePath]);

  const convertToMenuItems = (items: MenuItemProps[]): ItemType[] =>
    items.map((item: MenuItemProps) => {
      const label = translate(item.labelKey, item.label);
      const title = translate(item.titleKey, item.title) || label;
      if (item.children) {
        return {
          key: item.key,
          icon: item.icon,
          label,
          children: convertToMenuItems(item.children),
        };
      }
      const href = serializePath(item.path) || '#';
      return {
        key: item.key,
        icon: item.icon,
        label: (
          <Link
            href={href}
            style={{ color: 'inherit', textDecoration: 'none', display: 'block' }}
            onClick={() => handleClickMenuItem && handleClickMenuItem(item)}
          >
            {label}
          </Link>
        ),
        title,
      };
    });

  return (
    <Menu
      {...rest}
      mode="inline"
      selectedKeys={selectedKeys}
      items={convertToMenuItems(menuItems)}
    />
  );
};

export default MenuItems;
