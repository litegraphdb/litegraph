import { localStorageKeys } from '@/constants/constant';
import { useAppDispatch } from '@/lib/store/hooks';
import { LiteGraphStore } from '@/lib/store/litegraph/reducer';
import React, { useEffect, useState } from 'react';
import PageLoading from '../base/loading/PageLoading';
import { storeToken, storeTenant, storeAdminAccessKey } from '@/lib/store/litegraph/actions';
import { setAccessKey, setAccessToken, setEndpoint, setTenant } from '@/lib/sdk/litegraph.service';

export const initializeAuthFromLocalStorage = () => {
  const auth: LiteGraphStore = {
    selectedGraph: '',
    tenant: null,
    token: null,
    user: null,
    adminAccessKey: null,
  };
  try{
    if (typeof window !== 'undefined') {
    const token = localStorage.getItem(localStorageKeys.token);

    const tenant = localStorage.getItem(localStorageKeys.tenant);
    const adminAccessKey = localStorage.getItem(localStorageKeys.adminAccessKey);
    const url = localStorage.getItem(localStorageKeys.serverUrl);
    if (token) {
      // const permissions = localStorage.getItem('permissions');
      // auth.permissions = permissions && JSON.parse(permissions);
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
  }
  }catch(error){
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
  const localStorageAuth = initializeAuthFromLocalStorage();

  useEffect(() => {
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
    //eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

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
