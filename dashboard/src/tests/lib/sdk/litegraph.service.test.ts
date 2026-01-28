import '@testing-library/jest-dom';
import { renderHook, act } from '@testing-library/react';
import {
  sdk,
  setEndpoint,
  setAccessToken,
  setAccessKey,
  setTenant,
  useGetTenants,
  useValidateConnectivity,
  useGetUser,
  useFlushDBtoDisk,
} from '@/lib/sdk/litegraph.service';
import { liteGraphInstanceURL } from '@/constants/config';
import toast from 'react-hot-toast';
import { globalToastId } from '@/constants/config';

// Mock react-hot-toast
jest.mock('react-hot-toast', () => ({
  error: jest.fn(),
  success: jest.fn(),
}));

// Mock the Redux store
jest.mock('@/lib/store/hooks', () => ({
  useAppDispatch: jest.fn(() => jest.fn()),
}));

// Mock the Redux actions
jest.mock('@/lib/store/litegraph/actions', () => ({
  storeUser: jest.fn(),
}));

describe('LiteGraph Service', () => {
  beforeEach(() => {
    jest.clearAllMocks();

    // Mock the SDK methods after the instance is created
    jest.spyOn(sdk.Tenant, 'readAll').mockImplementation(jest.fn());
    jest.spyOn(sdk, 'validateConnectivity').mockImplementation(jest.fn());
    jest.spyOn(sdk.User, 'read').mockImplementation(jest.fn());
    jest.spyOn(sdk.Admin, 'flush').mockImplementation(jest.fn());
  });

  afterEach(() => {
    jest.restoreAllMocks();
  });

  describe('SDK Configuration', () => {
    it('should initialize SDK with correct URL', () => {
      expect(sdk).toBeDefined();
      expect(sdk.config.endpoint).toBe(liteGraphInstanceURL);
    });

    it('should set endpoint correctly', () => {
      const newEndpoint = 'https://new-endpoint.com/';
      setEndpoint(newEndpoint);
      expect(sdk.config.endpoint).toBe(newEndpoint);
    });

    it('should set access token correctly', () => {
      const token = 'test-token';
      setAccessToken(token);
      expect(sdk.config.accessToken).toBe(token);
    });

    it('should set access key correctly', () => {
      const key = 'test-key';
      setAccessKey(key);
      expect(sdk.config.accessKey).toBe(key);
    });

    it('should set tenant correctly', () => {
      const tenantId = 'test-tenant-id';
      setTenant(tenantId);
      expect(sdk.config.tenantGuid).toBe(tenantId);
    });
  });

  describe('useGetTenants', () => {
    it('should fetch tenants successfully', async () => {
      const mockTenants = [{ id: '1', name: 'Tenant 1' }];
      (sdk.Tenant.readAll as jest.Mock).mockResolvedValue(mockTenants);

      const { result } = renderHook(() => useGetTenants());

      let data;
      await act(async () => {
        data = await result.current.getTenants();
      });

      expect(data).toEqual(mockTenants);
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBeNull();
    });

    it('should handle error when fetching tenants fails', async () => {
      const error = new Error('API Error');
      (sdk.Tenant.readAll as jest.Mock).mockRejectedValue(error);

      const { result } = renderHook(() => useGetTenants());

      let data;
      await act(async () => {
        data = await result.current.getTenants();
      });

      expect(data).toBeNull();
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toEqual(error);
      expect(toast.error).toHaveBeenCalledWith('Unable to fetch tenants.', { id: globalToastId });
    });

    it('should handle error with custom toast message', async () => {
      const error = new Error('API Error');
      const customMessage = 'Custom error message';
      (sdk.Tenant.readAll as jest.Mock).mockRejectedValue(error);

      const { result } = renderHook(() => useGetTenants());

      let data;
      await act(async () => {
        data = await result.current.getTenants(customMessage);
      });

      expect(data).toBeNull();
      expect(toast.error).toHaveBeenCalledWith(customMessage, { id: globalToastId });
    });

    it('should handle non-Error objects', async () => {
      const error = 'String error';
      (sdk.Tenant.readAll as jest.Mock).mockRejectedValue(error);

      const { result } = renderHook(() => useGetTenants());

      let data;
      await act(async () => {
        data = await result.current.getTenants();
      });

      expect(data).toBeNull();
      expect(result.current.error).toBeInstanceOf(Error);
      expect(result.current.error?.message).toBe('String error');
    });
  });

  describe('useValidateConnectivity', () => {
    it('should validate connectivity successfully', async () => {
      const mockResult = { status: 'ok' };
      (sdk.validateConnectivity as jest.Mock).mockResolvedValue(mockResult);

      const { result } = renderHook(() => useValidateConnectivity());

      let data;
      await act(async () => {
        data = await result.current.validateConnectivity();
      });

      expect(data).toEqual(mockResult);
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBeNull();
    });

    it('should handle error when validation fails', async () => {
      const error = new Error('Connection failed');
      (sdk.validateConnectivity as jest.Mock).mockRejectedValue(error);

      const { result } = renderHook(() => useValidateConnectivity());

      let data;
      await act(async () => {
        data = await result.current.validateConnectivity();
      });

      expect(data).toBeNull();
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toEqual(error);
      expect(toast.error).toHaveBeenCalledWith('Unable to validate connectivity.', {
        id: globalToastId,
      });
    });

    it('should handle non-Error objects in validation', async () => {
      const error = 'Connection string error';
      (sdk.validateConnectivity as jest.Mock).mockRejectedValue(error);

      const { result } = renderHook(() => useValidateConnectivity());

      let data;
      await act(async () => {
        data = await result.current.validateConnectivity();
      });

      expect(data).toBeNull();
      expect(result.current.error).toBeInstanceOf(Error);
      expect(result.current.error?.message).toBe('Connection string error');
    });
  });

  describe('useGetUser', () => {
    it('should fetch user successfully without storing in Redux', async () => {
      const mockUser = { id: '1', name: 'Test User' };
      (sdk.User.read as jest.Mock).mockResolvedValue(mockUser);

      const { result } = renderHook(() => useGetUser());

      let data;
      await act(async () => {
        data = await result.current.fetchUser('user1', false);
      });

      expect(data).toEqual(mockUser);
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBeNull();
    });

    it('should fetch user and store in Redux when requested', async () => {
      const mockUser = { id: '1', name: 'Test User' };
      const mockDispatch = jest.fn();
      const { storeUser } = require('@/lib/store/litegraph/actions');
      (sdk.User.read as jest.Mock).mockResolvedValue(mockUser);
      require('@/lib/store/hooks').useAppDispatch.mockReturnValue(mockDispatch);

      const { result } = renderHook(() => useGetUser());

      let data;
      await act(async () => {
        data = await result.current.fetchUser('user1', true);
      });

      expect(data).toEqual(mockUser);
      expect(mockDispatch).toHaveBeenCalledWith(storeUser(mockUser));
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBeNull();
    });

    it('should handle error when fetching user fails', async () => {
      const error = new Error('User not found');
      (sdk.User.read as jest.Mock).mockRejectedValue(error);

      const { result } = renderHook(() => useGetUser());

      let data;
      await act(async () => {
        data = await result.current.fetchUser('user1');
      });

      expect(data).toBeNull();
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toEqual(error);
      expect(toast.error).toHaveBeenCalledWith('Unable to fetch user.', { id: globalToastId });
    });

    it('should handle non-Error objects in user fetch', async () => {
      const error = 'User fetch string error';
      (sdk.User.read as jest.Mock).mockRejectedValue(error);

      const { result } = renderHook(() => useGetUser());

      let data;
      await act(async () => {
        data = await result.current.fetchUser('user1');
      });

      expect(data).toBeNull();
      expect(result.current.error).toBeInstanceOf(Error);
      expect(result.current.error?.message).toBe('User fetch string error');
    });
  });

  describe('useFlushDBtoDisk', () => {
    it('should flush database successfully', async () => {
      (sdk.Admin.flush as jest.Mock).mockResolvedValue(true);

      const { result } = renderHook(() => useFlushDBtoDisk());

      let data;
      await act(async () => {
        data = await result.current.flushDBtoDisk();
      });

      expect(data).toBe(true);
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBeNull();
      expect(toast.success).toHaveBeenCalledWith('Database flushed to disk.', {
        id: globalToastId,
      });
    });

    it('should handle flush failure', async () => {
      (sdk.Admin.flush as jest.Mock).mockResolvedValue(false);

      const { result } = renderHook(() => useFlushDBtoDisk());

      let data;
      await act(async () => {
        data = await result.current.flushDBtoDisk();
      });

      expect(data).toBe(false);
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toBeNull();
      expect(toast.error).toHaveBeenCalledWith('Unable to flush database to disk.', {
        id: globalToastId,
      });
    });

    it('should handle error when flush throws exception', async () => {
      const error = new Error('Database error');
      (sdk.Admin.flush as jest.Mock).mockRejectedValue(error);

      const { result } = renderHook(() => useFlushDBtoDisk());

      let data;
      await act(async () => {
        data = await result.current.flushDBtoDisk();
      });

      expect(data).toBe(false);
      expect(result.current.isLoading).toBe(false);
      expect(result.current.error).toEqual(error);
      expect(toast.error).toHaveBeenCalledWith('Unable to flush database to disk.', {
        id: globalToastId,
      });
    });

    it('should handle non-Error objects in flush', async () => {
      const error = 'Database flush string error';
      (sdk.Admin.flush as jest.Mock).mockRejectedValue(error);

      const { result } = renderHook(() => useFlushDBtoDisk());

      let data;
      await act(async () => {
        data = await result.current.flushDBtoDisk();
      });

      expect(data).toBe(false);
      expect(result.current.error).toBeInstanceOf(Error);
      expect(result.current.error?.message).toBe('Database flush string error');
    });
  });
});
