import { LiteGraphSdk } from 'litegraphdb';
import { useState } from 'react';
import { useAppDispatch } from '../store/hooks';
import toast from 'react-hot-toast';
import { globalToastId, liteGraphInstanceURL } from '@/constants/config';
import { storeUser } from '../store/litegraph/actions';

// Initialize the SDK once and reuse the instance

export const sdk = new LiteGraphSdk(liteGraphInstanceURL);

export const setEndpoint = (endpoint: string) => {
  sdk.config.endpoint = endpoint;
};
export const setAccessToken = (accessToken: string) => {
  sdk.config.accessToken = accessToken;
};
export const setAccessKey = (accessKey: string) => {
  sdk.config.accessKey = accessKey;
};

export const setTenant = (tenantId: string) => {
  sdk.config.tenantGuid = tenantId;
};
// region Graph

export const useGetTenants = () => {
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<Error | null>(null);

  const getTenants = async (toastMessage?: string) => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await sdk.Tenant.readAll();
      setIsLoading(false);
      return data;
    } catch (err) {
      toast.error(toastMessage || 'Unable to fetch tenants.', { id: globalToastId });
      setIsLoading(false);
      setError(err instanceof Error ? err : new Error(String(err)));
      return null;
    }
  };
  return { getTenants, isLoading, error };
};

export const useValidateConnectivity = () => {
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<Error | null>(null);

  const validateConnectivity = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await sdk.validateConnectivity();
      setIsLoading(false);
      return data;
    } catch (err) {
      toast.error('Unable to validate connectivity.', { id: globalToastId });
      setIsLoading(false);
      setError(err instanceof Error ? err : new Error(String(err)));
      return null;
    }
  };
  return { validateConnectivity, isLoading, error };
};

// Fetch user
export const useGetUser = () => {
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<Error | null>(null);
  const dispatch = useAppDispatch();

  const fetchUser = async (userId: string, storeInRedux?: boolean) => {
    setIsLoading(true);
    setError(null);
    try {
      const data = await sdk.User.read(userId);
      setIsLoading(false);
      if (data && storeInRedux) {
        dispatch(storeUser(data));
      }
      return data;
    } catch (err) {
      toast.error('Unable to fetch user.', { id: globalToastId });
      setIsLoading(false);
      setError(err instanceof Error ? err : new Error(String(err)));
      return null;
    }
  };

  return { fetchUser, isLoading, error };
};

export const useFlushDBtoDisk = () => {
  const [isLoading, setIsLoading] = useState<boolean>(false);
  const [error, setError] = useState<Error | null>(null);

  const flushDBtoDisk = async () => {
    setIsLoading(true);
    setError(null);
    try {
      const done = await sdk.Admin.flush();
      setIsLoading(false);
      if (done) {
        toast.success('Database flushed to disk.', { id: globalToastId });
      } else {
        toast.error('Unable to flush database to disk.', { id: globalToastId });
      }
      return done;
    } catch (err) {
      toast.error('Unable to flush database to disk.', { id: globalToastId });
      setIsLoading(false);
      setError(err instanceof Error ? err : new Error(String(err)));
      return false;
    }
  };

  return { flushDBtoDisk, isLoading, error };
};
