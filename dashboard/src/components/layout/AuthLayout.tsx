import { localStorageKeys } from '@/constants/constant';
import { useAppDispatch } from '@/lib/store/hooks';
import { LiteGraphStore } from '@/lib/store/litegraph/reducer';
import React, { useEffect, useState } from 'react';
import PageLoading from '../base/loading/PageLoading';
import { storeToken, storeTenant, storeAdminAccessKey } from '@/lib/store/litegraph/actions';
import { setAccessKey, setAccessToken, setEndpoint, setTenant } from '@/lib/sdk/litegraph.service';

const initializeAuthFromLocalStorage = (): LiteGraphStore | null => {
  const auth: LiteGraphStore = {
    selectedGraph: '',
    tenant: null,
    token: null,
    user: null,
    adminAccessKey: null,
  };
  try {
    const token = localStorage.getItem(localStorageKeys.token);
    const tenant = localStorage.getItem(localStorageKeys.tenant);
    const adminAccessKey = localStorage.getItem(localStorageKeys.adminAccessKey);
    const url = localStorage.getItem(localStorageKeys.serverUrl);

    if (token) {
      auth.token = JSON.parse(token);
    }
    if (tenant) {
      auth.tenant = JSON.parse(tenant);
    }
    if (adminAccessKey) {
      auth.adminAccessKey = adminAccessKey;
    }
    if (url) {
      setEndpoint(url);
    }
    return auth;
  } catch (error) {
    console.error(error);
  }
  return null;
};

const AuthLayout = ({
  children,
  className,
}: Readonly<{ children: React.ReactNode; className?: string }>) => {
  const [isReady, setIsReady] = useState(false);
  const dispatch = useAppDispatch();

  useEffect(() => {
    const localStorageAuth = initializeAuthFromLocalStorage();

    if (localStorageAuth?.token) {
      dispatch(storeToken(localStorageAuth.token));
      setAccessToken(localStorageAuth.token.Token);
    }
    if (localStorageAuth?.tenant) {
      dispatch(storeTenant(localStorageAuth.tenant));
      setTenant(localStorageAuth.tenant?.GUID);
    }
    if (localStorageAuth?.adminAccessKey) {
      dispatch(storeAdminAccessKey(localStorageAuth.adminAccessKey));
      setAccessKey(localStorageAuth.adminAccessKey);
    }
    setIsReady(true);
  }, [dispatch]);

  if (!isReady) {
    return <PageLoading />;
  }
  return (
    <div id="root-div" className={className}>
      {children}
    </div>
  );
};

export default AuthLayout;
