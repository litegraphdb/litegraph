'use client';
import { Button, Layout } from 'antd';
import LitegraphFlex from './base/flex/Flex';
import MenuItems from './menu-item/MenuItems';
import { MenuItemProps } from './menu-item/types';
import { DatabaseOutlined, MenuFoldOutlined, MenuUnfoldOutlined } from '@ant-design/icons';
import Image from 'next/image';
import LitegraphTitle from './base/typograpghy/Title';
import styles from './layout/nav.module.scss';
import LitegraphTooltip from './base/tooltip/Tooltip';
import { useFlushDBtoDisk } from '@/lib/sdk/litegraph.service';
import ConfirmationModal from './confirmation-modal/ConfirmationModal';
import { useState } from 'react';
import { useAppContext } from '@/hooks/appHooks';
import { ThemeEnum } from '@/types/types';
import { useTranslations } from 'next-intl';

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
  const { theme } = useAppContext();
  const t = useTranslations('sidebar');
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
      width={220}
      trigger={null}
      collapsible
      collapsed={collapsed}
      collapsedWidth={60}
      className={`${styles.sidebarContainer} ${theme === ThemeEnum.LIGHT ? styles.sidebarLight : ''}`}
    >
      <LitegraphFlex justify="center" gap={8} align="center" className={styles.logoContainer}>
        {collapsed ? (
          <Image src={'/favicon.png'} alt={t('logoAlt')} width={30} height={30} />
        ) : (
          <>
            <Image src={'/favicon.png'} alt={t('logoAlt')} width={30} height={30} />
            <LitegraphTitle level={4} className="mt-xs fade-in" weight={600}>
              LiteGraph
            </LitegraphTitle>
          </>
        )}
      </LitegraphFlex>
      <LitegraphFlex justify="flex-end" className="pl-sm pr-sm pt-sm">
        <LitegraphTooltip title={collapsed ? t('expand') : t('collapse')}>
          <Button
            type="text"
            size="small"
            icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
            onClick={() => setCollapsed(!collapsed)}
            style={{ fontSize: '14px', color: 'var(--ant-color-text-tertiary)' }}
          />
        </LitegraphTooltip>
      </LitegraphFlex>
      {isAdmin && (
        <LitegraphFlex className="mt mb-sm" gap={10} justify="center" align="center">
          <LitegraphTooltip title={t('flushTitle')}>
            <Button type="default" icon={<DatabaseOutlined />} onClick={() => setOpen(true)}>
              {collapsed ? '' : t('flushToDisk')}
            </Button>
          </LitegraphTooltip>
        </LitegraphFlex>
      )}
      <div className={styles.navScroll}>
        <MenuItems menuItems={menuItems} className={styles.navMenu} />
      </div>
      <ConfirmationModal
        title={t('flushTitle')}
        content={t('flushConfirm')}
        onCancel={() => setOpen(false)}
        onConfirm={onFlushDBtoDisk}
        open={open}
        loading={isLoading}
      />
    </Sider>
  );
};

export default Navigation;
