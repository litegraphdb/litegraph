import { setAccessKey, setTenant, useGetUser } from '@/lib/sdk/litegraph.service';
import { useAppDispatch } from '@/lib/store/hooks';
import {
  logOut,
  storeToken,
  storeTenant,
  storeUser,
  storeAdminAccessKey,
} from '@/lib/store/litegraph/actions';
import { TenantMetaData, Token } from 'litegraphdb/dist/types/types';
import { setEndpoint, setAccessToken, sdk } from '@/lib/sdk/litegraph.service';
import { useGetTokenDetailsMutation } from '@/lib/store/slice/slice';
import toast from 'react-hot-toast';
import { globalToastId } from '@/constants/config';
import { localStorageKeys } from '@/constants/constant';

export const useFetchUserDetails = () => {
  const dispatch = useAppDispatch();
  const { fetchUser, isLoading, error } = useGetUser();

  const fetchUserDetails = async (userId: string, storeInRedux: boolean = true) => {
    const response = await fetchUser(userId);
    if (response?.GUID && storeInRedux) {
      dispatch(storeUser(response));
    }
  };
  return { fetchUserDetails, isLoading };
};

export const useCredentialsToLogin = () => {
  const dispatch = useAppDispatch();
  const loginWithCredentials = (token: Token, tenant: TenantMetaData) => {
    dispatch(storeToken(token));
    dispatch(storeTenant(tenant));
  };
  return loginWithCredentials;
};

export const useAdminCredentialsToLogin = () => {
  const dispatch = useAppDispatch();
  const loginWithAdminCredentials = (accessKey: string) => {
    dispatch(storeAdminAccessKey(accessKey));
  };
  return loginWithAdminCredentials;
};

export const useLogout = () => {
  const dispatch = useAppDispatch();

  const logOutFromSystem = (path?: string) => {
    dispatch(logOut(path));
  };
  return logOutFromSystem;
};

export const useApiKeyToLogin = () => {
  const loginWithCredentials = useCredentialsToLogin();
  const [getTokenDetails] = useGetTokenDetailsMutation();
  const loginWithApiKey = async (apikey: string, endpoint: string) => {
    setAccessToken(apikey);
    setEndpoint(endpoint);

    try {
      const tokenDetails = await getTokenDetails(apikey);
      if (tokenDetails?.data?.Tenant) {
        loginWithCredentials(tokenDetails.data, tokenDetails.data.Tenant);
        setTenant(tokenDetails.data.Tenant.GUID);

        // Store the API key separately for SDK configuration
        const tokenWithApiKey = {
          ...tokenDetails.data,
          Token: apikey, // Add the API key to the token object
        };

        localStorage.setItem(localStorageKeys.token, JSON.stringify(tokenWithApiKey));
        localStorage.setItem(localStorageKeys.tenant, JSON.stringify(tokenDetails.data.Tenant));
        localStorage.setItem(localStorageKeys.serverUrl, endpoint);

        // Also store user data if available
        if (tokenDetails.data.User) {
          localStorage.setItem(localStorageKeys.user, JSON.stringify(tokenDetails.data.User));
        }

        toast.success('Logged in successfully via SSO.', { id: globalToastId });
        return { success: true, tenant: tokenDetails.data.Tenant };
      }
      return { success: false, tenant: null };
    } catch (error) {
      console.error('Error: ', error);
      return { success: false, tenant: null };
    }
  };
  return { loginWithApiKey };
};
