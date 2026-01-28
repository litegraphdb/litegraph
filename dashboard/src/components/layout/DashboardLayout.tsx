import React, { useEffect, useState } from 'react';
import { LogoutOutlined, ReloadOutlined } from '@ant-design/icons';
import { Layout } from 'antd';
import Navigation from '../navigation';
import LitegraphText from '../base/typograpghy/Text';
import { useLogout } from '@/hooks/authHooks';
import { useAppDispatch } from '@/lib/store/hooks';
import styles from './dashboard.module.scss';
import { MenuItemProps } from '../menu-item/types';
import LitegraphSelect from '../base/select/Select';
import { useSelectedGraph, useSelectedTenant } from '@/hooks/entityHooks';
import { storeSelectedGraph, storeTenant } from '@/lib/store/litegraph/actions';
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

const { Header, Content } = Layout;

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
  const { theme } = useAppContext();
  const dispatch = useAppDispatch();
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
          <Header className={styles.header}>
            <LitegraphFlex vertical justify="center">
              {useGraphsSelector && (
                <LitegraphFlex align="center" gap={8}>
                  <span>Graph:</span>
                  <LitegraphSelect
                    placeholder="Select a graph"
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
                  <span>Tenant:</span>
                  {tenantsError ? (
                    <LitegraphText
                      fontSize={12}
                      className={'cursor-pointer'}
                      style={{ color: 'red' }}
                      onClick={() => fetchTenantsList()}
                    >
                      <ReloadOutlined /> Retry
                    </LitegraphText>
                  ) : (
                    <LitegraphSelect
                      loading={isTenantsLoading}
                      placeholder="Select a tenant"
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
              <LitegraphTooltip
                title={`Switch to ${theme === ThemeEnum.DARK ? 'Light' : 'Dark'} mode`}
              >
                <ThemeModeSwitch />
              </LitegraphTooltip>
              {!noProfile ? (
                <LoggedUserInfo />
              ) : (
                <div
                  className={styles.logoutLink}
                  onClick={() => logOutFromSystem()}
                  role="button"
                  tabIndex={0}
                >
                  <LogoutOutlined className={styles.logoutIcon} />
                  <span>Logout</span>
                </div>
              )}
            </LitegraphFlex>
          </Header>
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
