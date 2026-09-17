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
import { usePathname, useRouter } from 'next/navigation';
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
  const router = useRouter();
  const pathname = usePathname();
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
    data: graphsEnvelope,
    isLoading: isGraphsLoading,
    error: graphError,
    refetch: fetchGraphsList,
  } = useGetAllGraphsQuery(undefined, { skip: !useGraphsSelector });
  const graphOptions = transformToOptions(graphsEnvelope?.Objects);
  const {
    data: tenantsEnvelope,
    isLoading: isTenantsLoading,
    isError: tenantsError,
    refetch: fetchTenantsList,
  } = useGetAllTenantsQuery(undefined, { skip: !useTenantSelector });
  const tenantsList = useMemo(() => tenantsEnvelope?.Objects ?? [], [tenantsEnvelope]);
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
      const previousGuid = selectedTenantRedux?.GUID;
      localStorage.setItem(localStorageKeys.tenant, JSON.stringify(tenant));
      setTenant(tenant.GUID);
      dispatch(storeTenant(tenant));
      dispatch(sdkSlice.util.invalidateTags([SliceTags.USER, SliceTags.CREDENTIAL] as any));
      // On a tenant-scoped page, switch the active tenant in the URL too so the
      // page re-renders against the newly selected tenant.
      if (previousGuid && tenant.GUID !== previousGuid && pathname?.includes(`/dashboard/${previousGuid}`)) {
        router.push(pathname.replace(`/dashboard/${previousGuid}`, `/dashboard/${tenant.GUID}`));
      }
    }
  };

  const logOutFromSystem = useLogout();

  return (
    <LayoutContext.Provider value={{ isGraphsLoading, graphError, refetchGraphs: fetchGraphsList }}>
      <Layout style={{ height: '100vh', overflow: 'hidden' }}>
        <Navigation
          collapsed={collapsed}
          menuItems={menuItems}
          setCollapsed={setCollapsed}
          isAdmin={isAdmin}
          data-testid="navigation"
        />
        <Layout style={{ height: '100vh' }}>
          <div className={styles.header}>
            <LitegraphFlex align="center" justify="flex-start" gap={20}>
              {useGraphsSelector && (
                <LitegraphFlex align="center" gap={8}>
                  <LitegraphTooltip title={t('selectGraph')}>
                    <span>{t('graphLabel')}</span>
                  </LitegraphTooltip>
                  <LitegraphSelect
                    size="small"
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
                      size="small"
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
              <LitegraphTooltip title={t('discord')}>
                <a
                  className={styles.headerIconLink}
                  href="https://discord.gg/tRAN8HgvK5"
                  target="_blank"
                  rel="noreferrer"
                  aria-label={t('discordAria')}
                >
                  <svg
                    viewBox="0 0 24 24"
                    width="1em"
                    height="1em"
                    fill="currentColor"
                    aria-hidden="true"
                    focusable="false"
                  >
                    <path d="M20.317 4.369a19.79 19.79 0 0 0-4.885-1.515.074.074 0 0 0-.079.037c-.211.375-.444.864-.608 1.249a18.27 18.27 0 0 0-5.487 0 12.64 12.64 0 0 0-.617-1.249.077.077 0 0 0-.079-.037A19.736 19.736 0 0 0 3.677 4.369a.07.07 0 0 0-.032.027C.533 9.046-.32 13.58.099 18.057a.082.082 0 0 0 .031.057 19.9 19.9 0 0 0 5.993 3.03.078.078 0 0 0 .084-.028c.462-.63.874-1.295 1.226-1.994a.076.076 0 0 0-.041-.106 13.107 13.107 0 0 1-1.872-.892.077.077 0 0 1-.008-.128c.126-.094.252-.192.372-.291a.074.074 0 0 1 .077-.01c3.928 1.793 8.18 1.793 12.062 0a.074.074 0 0 1 .078.009c.12.099.246.198.373.292a.077.077 0 0 1-.006.127 12.3 12.3 0 0 1-1.873.891.077.077 0 0 0-.041.107c.36.698.772 1.362 1.225 1.993a.076.076 0 0 0 .084.028 19.839 19.839 0 0 0 6.002-3.03.077.077 0 0 0 .032-.054c.5-5.177-.838-9.674-3.549-13.66a.061.061 0 0 0-.031-.03zM8.02 15.331c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.956-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.956 2.418-2.157 2.418zm7.975 0c-1.183 0-2.157-1.085-2.157-2.419 0-1.333.955-2.419 2.157-2.419 1.21 0 2.176 1.096 2.157 2.42 0 1.333-.946 2.418-2.157 2.418z" />
                  </svg>
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
              minHeight: 0,
              flex: 1,
              overflowY: 'auto',
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
