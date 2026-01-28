import PageLoading from '@/components/base/loading/PageLoading';
import LogoutFallBack from '@/components/logout-fallback/LogoutFallBack';
import { paths } from '@/constants/constant';
import { useLogout } from '@/hooks/authHooks';
import { useAppDynamicNavigation } from '@/hooks/hooks';
import {
  setAccessKey,
  setAccessToken,
  setTenant,
  useGetTenants,
} from '@/lib/sdk/litegraph.service';
import { useAppDispatch, useAppSelector } from '@/lib/store/hooks';
import { storeUser } from '@/lib/store/litegraph/actions';
import { useGetTokenDetailsMutation } from '@/lib/store/slice/slice';
import { RootState } from '@/lib/store/store';
import { TenantMetaData, Token } from 'litegraphdb/dist/types/types';
import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';

export const withAuth = (WrappedComponent: React.ElementType) => {
  const WithAuth = (props: any) => {
    const token = useAppSelector((state: RootState) => state.liteGraph.token);
    const [hasValidAuth, setHasValidAuth] = useState<boolean | null>(null);
    const [fetchTokenDetails, { isLoading: isFetchUserDetailsLoading }] =
      useGetTokenDetailsMutation();
    const logout = useLogout();
    const dispatch = useAppDispatch();

    const authToken = token?.Token;

    useEffect(() => {
      if (authToken) {
        fetchTokenDetails(token.Token).then(({ data: response }: { data?: Token }) => {
          if (response?.User?.GUID) {
            dispatch(storeUser(response.User));
            setHasValidAuth(true);
          } else {
            setHasValidAuth(false);
          }
        });
      } else {
        logout();
      }
      //eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);
    return hasValidAuth === null ? (
      <PageLoading className="h-100vh" />
    ) : hasValidAuth ? (
      <WrappedComponent {...props} />
    ) : (
      <LogoutFallBack />
    );
  };
  return WithAuth;
};

export const withAdminAuth = (WrappedComponent: React.ElementType) => {
  const WithAuth = (props: any) => {
    const adminAccessKey = useAppSelector((state: RootState) => state.liteGraph.adminAccessKey);
    const [hasValidAuth, setHasValidAuth] = useState<boolean | null>(null);
    const logout = useLogout();
    const { getTenants, isLoading: isValidateConnectivityLoading } = useGetTenants();
    const [logoutMessage, setLogoutMessage] = useState<string>('');

    useEffect(() => {
      if (adminAccessKey) {
        setAccessKey(adminAccessKey);
        getTenants()
          .then((res: TenantMetaData[] | null) => {
            if (res) {
              setHasValidAuth(true);
            } else {
              logout();
            }
          })
          .catch(() => {
            logout();
          });
      }
      //eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    return hasValidAuth === null ? (
      <PageLoading className="h-100vh" />
    ) : hasValidAuth ? (
      <WrappedComponent {...props} />
    ) : (
      <LogoutFallBack message={logoutMessage} logoutPath={paths.adminLogin} />
    );
  };
  return WithAuth;
};

export const forGuest = (WrappedComponent: React.ElementType) => {
  const ForGuest = (props: any) => {
    const token = useAppSelector((state: RootState) => state.liteGraph.token);
    const adminAccessKey = useAppSelector((state: RootState) => state.liteGraph.adminAccessKey);
    const tenant = useAppSelector((state: RootState) => state.liteGraph.tenant);
    const { serializePath } = useAppDynamicNavigation();
    const router = useRouter();
    useEffect(() => {
      if (token?.Token && tenant?.GUID) {
        setAccessToken(token.Token);
        setTenant(tenant.GUID);
        router.push(serializePath(paths.dashboardHome));
      }
      if (adminAccessKey) {
        setAccessKey(adminAccessKey);
        tenant?.GUID && setTenant(tenant.GUID);
        router.push(serializePath(paths.adminDashboard));
      }
      //eslint-disable-next-line react-hooks/exhaustive-deps
    }, [adminAccessKey, token, tenant]);

    return !token?.Token && !adminAccessKey ? <WrappedComponent {...props} /> : null;
  };
  return ForGuest;
};
