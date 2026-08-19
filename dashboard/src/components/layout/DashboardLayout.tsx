import React, { useEffect, useMemo, useState } from 'react';
import { GithubOutlined, LogoutOutlined, ReloadOutlined, TranslationOutlined } from '@ant-design/icons';
import { Button, Layout, Tag } from 'antd';
import { useTranslations } from 'next-intl';
import Navigation from '../navigation';
import LitegraphText from '../base/typograpghy/Text';
import { useLogout } from '@/hooks/authHooks';
import { useAppDispatch, useAppSelector } from '@/lib/store/hooks';
import styles from './dashboard.module.scss';
import { MenuItemProps } from '../menu-item/types';
import LitegraphSelect from '../base/select/Select';
import { useSelectedGraph, useSelectedTenant } from '@/hooks/entityHooks';
import { storeSelectedGraph, storeTenant, storeLocale } from '@/lib/store/litegraph/actions';
import { setStoredLocale } from '@/i18n/persistence';
import { AppLocale, LOCALE_REGISTRY } from '@/i18n/locales';
import { LayoutContext } from './context';
import { setTenant } from '@/lib/sdk/litegraph.service';
import { localStorageKeys } from '@/constants/constant';
import LitegraphFlex from '../base/flex/Flex';
import { useGetAllGraphsQuery, useGetAllTenantsQuery } from '@/lib/store/slice/slice';
import { transformToOptions } from '@/lib/graph/utils';
import LoggedUserInfo from '../logged-in-user/LoggedUserInfo';
import sdkSlice from '@/lib/store/rtk/rtkSdkInstance';
import { SliceTags } from '@/lib/store/slice/types';
import { TenantMetaData } from 'litegraphdb/dist/types/types';
import { useAppContext } from '@/hooks/appHooks';
import { ThemeEnum } from '@/types/types';
import LitegraphTooltip from '../base/tooltip/Tooltip';
import ThemeModeSwitch from '../theme-mode-switch/ThemeModeSwitch';

const { Content } = Layout;

interface LayoutWrapperProps {
  children: React.ReactNode;
  menuItems: MenuItemProps[];
  noProfile?: boolean;
  useGraphsSelector?: boolean;
  useTenantSelector?: boolean;
  isAdmin?: boolean;
}

const DashboardLayout = ({
  children,
  menuItems,
  noProfile,
  useGraphsSelector,
  useTenantSelector,
  isAdmin,
}: LayoutWrapperProps) => {
  const [collapsed, setCollapsed] = useState(false);
  const [serverUrl, setServerUrl] = useState<string | null>(null);
  const { theme } = useAppContext();
  const t = useTranslations('header');
  const dispatch = useAppDispatch();
  const locale = useAppSelector((state) => state.liteGraph.locale);
  const languageOptions = LOCALE_REGISTRY.map((entry) => ({
    value: entry.code,
    label: entry.nativeName,
  }));

  const handleLocaleChange = (value: string | number | string[]) => {
    const nextLocale = value as AppLocale;
    setStoredLocale(nextLocale);
    dispatch(storeLocale(nextLocale));
  };
  const selectedGraphRedux = useSelectedGraph();
  const selectedTenantRedux = useSelectedTenant();
  const {
    data: graphsList,
    isLoading: isGraphsLoading,
    error: graphError,
    refetch: fetchGraphsList,
  } = useGetAllGraphsQuery(undefined, { skip: !useGraphsSelector });
  const graphOptions = transformToOptions(graphsList);
  const {
    data: tenantsList = [],
    isLoading: isTenantsLoading,
    isError: tenantsError,
    refetch: fetchTenantsList,
  } = useGetAllTenantsQuery(undefined, { skip: !useTenantSelector });
  const tenantOptions = transformToOptions(tenantsList);

  useEffect(() => {
    const url = localStorage.getItem(localStorageKeys.serverUrl);
    setServerUrl(url);
  }, []);

  const serverHostDisplay = useMemo(() => {
    if (!serverUrl) return null;
    try {
      const parsed = new URL(serverUrl);
      return parsed.host;
    } catch {
      return serverUrl.replace(/^https?:\/\//, '').replace(/\/+$/, '');
    }
  }, [serverUrl]);

  useEffect(() => {
    if (!selectedGraphRedux && graphOptions?.length > 0) {
      dispatch(storeSelectedGraph({ graph: graphOptions[0].value }));
    }
  }, [selectedGraphRedux, graphOptions, dispatch]);

  useEffect(() => {
    if (!selectedTenantRedux && tenantsList?.length > 0) {
      localStorage.setItem(localStorageKeys.tenant, JSON.stringify(tenantsList[0]));
      setTenant(tenantsList[0].GUID);
      dispatch(storeTenant(tenantsList[0]));
    }
  }, [selectedTenantRedux, tenantsList, dispatch]);

  const handleGraphSelect = async (graphId: any) => {
    dispatch(storeSelectedGraph({ graph: graphId.toString() }));
  };

  const handleTenantSelect = async (tenantId: any) => {
    if (!useTenantSelector) return;
    const tenant = tenantsList.find((tenant: TenantMetaData) => tenant.GUID === tenantId);
    if (tenant) {
      localStorage.setItem(localStorageKeys.tenant, JSON.stringify(tenant));
      setTenant(tenant.GUID);
      dispatch(storeTenant(tenant));
      dispatch(sdkSlice.util.invalidateTags([SliceTags.USER, SliceTags.CREDENTIAL] as any));
    }
  };

  const logOutFromSystem = useLogout();

  return (
    <LayoutContext.Provider value={{ isGraphsLoading, graphError, refetchGraphs: fetchGraphsList }}>
      <Layout style={{ minHeight: '100vh' }}>
        <Navigation
          collapsed={collapsed}
          menuItems={menuItems}
          setCollapsed={setCollapsed}
          isAdmin={isAdmin}
          data-testid="navigation"
        />
        <Layout>
          <div className={styles.header}>
            <LitegraphFlex vertical justify="center">
              {useGraphsSelector && (
                <LitegraphFlex align="center" gap={8}>
                  <LitegraphTooltip title={t('selectGraph')}>
                    <span>{t('graphLabel')}</span>
                  </LitegraphTooltip>
                  <LitegraphSelect
                    placeholder={t('selectAGraph')}
                    options={graphOptions}
                    value={selectedGraphRedux || undefined}
                    onChange={handleGraphSelect}
                    style={{ width: 250 }}
                    loading={isGraphsLoading}
                    data-testid="litegraph-select"
                    tooltip={true}
                  />
                </LitegraphFlex>
              )}
              {useTenantSelector && (
                <LitegraphFlex align="center" gap={8}>
                  <LitegraphTooltip title={t('selectTenant')}>
                    <span>{t('tenantLabel')}</span>
                  </LitegraphTooltip>
                  {tenantsError ? (
                    <LitegraphText
                      fontSize={12}
                      className={'cursor-pointer'}
                      style={{ color: 'red' }}
                      onClick={() => fetchTenantsList()}
                    >
                      <ReloadOutlined /> {t('retry')}
                    </LitegraphText>
                  ) : (
                    <LitegraphSelect
                      loading={isTenantsLoading}
                      placeholder={t('selectATenant')}
                      options={tenantOptions}
                      value={selectedTenantRedux?.GUID || undefined}
                      onChange={handleTenantSelect}
                      style={{ width: 250 }}
                      disabled={!useTenantSelector}
                      data-testid="tenant-select"
                      tooltip={true}
                    />
                  )}
                </LitegraphFlex>
              )}
              {!useTenantSelector && !useGraphsSelector && <span></span>}
            </LitegraphFlex>

            <LitegraphFlex
              className={styles.userSection}
              align="center"
              gap={isAdmin ? 20 : 8}
              justify="flex-end"
              data-testid="user-section"
            >
              <LitegraphTooltip title={t('selectLanguage')}>
                <LitegraphSelect
                  aria-label={t('language')}
                  value={locale}
                  options={languageOptions}
                  onChange={handleLocaleChange}
                  data-testid="language-select"
                  size="small"
                  variant="borderless"
                  suffixIcon={<TranslationOutlined />}
                  style={{ width: 120 }}
                />
              </LitegraphTooltip>
              {serverHostDisplay && (
                <LitegraphTooltip title={t('connectedServer')}>
                  <Tag
                    bordered={false}
                    style={{
                      fontSize: 11,
                      color: 'var(--ant-color-text-tertiary)',
                      background: 'transparent',
                      margin: 0,
                    }}
                  >
                    {serverHostDisplay}
                  </Tag>
                </LitegraphTooltip>
              )}
              <LitegraphTooltip
                title={theme === ThemeEnum.DARK ? t('switchToLight') : t('switchToDark')}
              >
                <ThemeModeSwitch />
              </LitegraphTooltip>
              <LitegraphTooltip title={t('github')}>
                <a
                  className={styles.headerIconLink}
                  href="https://github.com/litegraphdb/litegraph"
                  target="_blank"
                  rel="noreferrer"
                  aria-label={t('githubAria')}
                >
                  <GithubOutlined />
                </a>
              </LitegraphTooltip>
              {!noProfile && <LoggedUserInfo />}
              <LitegraphTooltip title={t('signOut')}>
                <Button
                  type="text"
                  className={styles.headerIconButton}
                  icon={<LogoutOutlined />}
                  onClick={() => logOutFromSystem()}
                  aria-label={t('logoutAria')}
                />
              </LitegraphTooltip>
            </LitegraphFlex>
          </div>
          <Content
            style={{
              minHeight: 280,
              background: 'var(--ant-color-bg-base)',
            }}
            data-testid="layout-children"
          >
            {children}
          </Content>
        </Layout>
      </Layout>
    </LayoutContext.Provider>
  );
};

export default DashboardLayout;
