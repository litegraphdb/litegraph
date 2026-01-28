import { Menu, MenuProps } from 'antd';
import React from 'react';
import { MenuItemProps } from './types';
import Link from 'next/link';
import Sider from 'antd/es/layout/Sider';
import { useAppDynamicNavigation } from '@/hooks/hooks';

interface MenuItemsProps extends MenuProps {
  menuItems: MenuItemProps[];
  handleClickMenuItem?: (item: MenuItemProps) => void;
}

const MenuItems = ({ menuItems, handleClickMenuItem, ...rest }: MenuItemsProps) => {
  const { serializePath } = useAppDynamicNavigation();
  const renderMenuItems = (items: MenuItemProps[], showVerticalSubMenu: boolean = false) =>
    items.map((item: MenuItemProps) =>
      item.children ? (
        showVerticalSubMenu ? (
          <div id={item.key}>
            <Sider theme="light">
              <Menu
                mode="vertical"
                getPopupContainer={() => document.getElementById('main-layout') || document.body}
              >
                <Menu.SubMenu key={item.key} icon={item.icon} title={item.label}>
                  {renderMenuItems(item.children, true)}
                </Menu.SubMenu>
              </Menu>
            </Sider>
          </div>
        ) : (
          <Menu.SubMenu key={item.key} icon={item.icon} title={item.label}>
            {renderMenuItems(item.children, true)}
          </Menu.SubMenu>
        )
      ) : (
        <Menu.Item
          key={item.key}
          icon={item.icon}
          onClick={() => handleClickMenuItem && handleClickMenuItem(item)}
        >
          <Link href={serializePath(item.path) || '#'}>
            <span>{item.label}</span>
          </Link>
        </Menu.Item>
      )
    );
  return (
    <Menu mode="inline" defaultSelectedKeys={['1']} {...rest}>
      {renderMenuItems(menuItems)}
    </Menu>
  );
};

export default MenuItems;
