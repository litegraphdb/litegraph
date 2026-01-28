'use client';
import { Button, Layout } from 'antd';
import LitegraphFlex from './base/flex/Flex';
import MenuItems from './menu-item/MenuItems';
import { MenuItemProps } from './menu-item/types';
import { DatabaseOutlined, DoubleLeftOutlined, DoubleRightOutlined } from '@ant-design/icons';
import Image from 'next/image';
import LitegraphTitle from './base/typograpghy/Title';
import styles from './layout/nav.module.scss';
import LitegraphTooltip from './base/tooltip/Tooltip';
import { useFlushDBtoDisk } from '@/lib/sdk/litegraph.service';
import ConfirmationModal from './confirmation-modal/ConfirmationModal';
import { useState } from 'react';

const { Sider } = Layout;

const Navigation = ({
  collapsed,
  menuItems,
  setCollapsed,
  isAdmin,
}: {
  collapsed: boolean;
  menuItems: MenuItemProps[];
  setCollapsed: (collapsed: boolean) => void;
  isAdmin?: boolean;
}) => {
  const [open, setOpen] = useState(false);
  const { flushDBtoDisk, isLoading, error } = useFlushDBtoDisk();
  const onFlushDBtoDisk = async () => {
    const result = await flushDBtoDisk();
    if (result) {
      setOpen(false);
    }
  };
  return (
    <Sider
      theme="light"
      width={170}
      trigger={null}
      collapsible
      collapsed={collapsed}
      collapsedWidth={60}
      className={styles.sidebarContainer}
    >
      <LitegraphFlex justify="center" gap={8} align="center" className={styles.logoContainer}>
        {collapsed ? (
          <Image src={'/favicon.png'} alt="Litegraph logo" width={30} height={30} />
        ) : (
          <>
            <Image src={'/favicon.png'} alt="Litegraph logo" width={30} height={30} />
            <LitegraphTitle level={4} className="mt-xs fade-in" weight={600}>
              LiteGraph
            </LitegraphTitle>
          </>
        )}
      </LitegraphFlex>
      <LitegraphFlex justify="flex-end" className="pl-sm pr-sm pt-sm">
        <Button
          type="link"
          icon={collapsed ? <DoubleRightOutlined /> : <DoubleLeftOutlined />}
          onClick={() => setCollapsed(!collapsed)}
          style={{
            fontSize: '16px',
            top: '150',
            right: '8%',
          }}
        />
      </LitegraphFlex>
      {isAdmin && (
        <LitegraphFlex className="mt mb-sm" gap={10} justify="center" align="center">
          <LitegraphTooltip title="Flush the database to disk">
            <Button type="default" icon={<DatabaseOutlined />} onClick={() => setOpen(true)}>
              {collapsed ? '' : 'Flush to disk'}
            </Button>
          </LitegraphTooltip>
        </LitegraphFlex>
      )}
      <MenuItems menuItems={menuItems} />
      <ConfirmationModal
        title="Flush the database to disk"
        content="Are you sure you want to flush the database to disk?"
        onCancel={() => setOpen(false)}
        onConfirm={onFlushDBtoDisk}
        open={open}
        loading={isLoading}
      />
    </Sider>
  );
};

export default Navigation;
