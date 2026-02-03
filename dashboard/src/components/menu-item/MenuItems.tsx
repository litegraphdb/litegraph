import { Menu, MenuProps } from 'antd';
import React from 'react';
import { MenuItemProps } from './types';
import Link from 'next/link';
import { useAppDynamicNavigation } from '@/hooks/hooks';
import { ItemType } from 'antd/es/menu/interface';

interface MenuItemsProps extends MenuProps {
  menuItems: MenuItemProps[];
  handleClickMenuItem?: (item: MenuItemProps) => void;
}

const MenuItems = ({ menuItems, handleClickMenuItem, ...rest }: MenuItemsProps) => {
  const { serializePath } = useAppDynamicNavigation();

  const convertToMenuItems = (items: MenuItemProps[]): ItemType[] =>
    items.map((item: MenuItemProps) => {
      if (item.children) {
        return {
          key: item.key,
          icon: item.icon,
          label: item.label,
          children: convertToMenuItems(item.children),
        };
      }
      return {
        key: item.key,
        icon: item.icon,
        label: (
          <Link href={serializePath(item.path) || '#'}>
            <span>{item.label}</span>
          </Link>
        ),
        onClick: () => handleClickMenuItem && handleClickMenuItem(item),
      };
    });

  return (
    <Menu
      mode="inline"
      defaultSelectedKeys={['1']}
      items={convertToMenuItems(menuItems)}
      {...rest}
    />
  );
};

export default MenuItems;
