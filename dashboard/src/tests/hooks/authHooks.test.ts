import React from 'react';
import { renderHook, act } from '@testing-library/react';
import {
  useFetchUserDetails,
  useCredentialsToLogin,
  useAdminCredentialsToLogin,
  useLogout,
} from '@/hooks/authHooks';
import { TenantMetaData, Token } from 'litegraphdb/dist/types/types';
import { mockCredentialData, mockTenantData, mockUserData } from '../pages/mockData';

// Mock dependencies - same as before
const mockUseGetUser = jest.fn();
const mockLogOut = jest.fn();
const mockStoreToken = jest.fn();
const mockStoreTenant = jest.fn();
const mockStoreUser = jest.fn();
const mockStoreAdminAccessKey = jest.fn();
const mockDispatch = jest.fn();
const mockUserResponse = mockUserData[0];

jest.mock('@/lib/store/hooks', () => ({
  useAppDispatch: () => mockDispatch,
}));

jest.mock('@/lib/sdk/litegraph.service', () => ({
  useGetUser: () => mockUseGetUser(),
}));

jest.mock('@/lib/store/litegraph/actions', () => ({
  logOut: (...args: any[]) => mockLogOut(...args),
  storeToken: (...args: any[]) => mockStoreToken(...args),
  storeTenant: (...args: any[]) => mockStoreTenant(...args),
  storeUser: (...args: any[]) => mockStoreUser(...args),
  storeAdminAccessKey: (...args: any[]) => mockStoreAdminAccessKey(...args),
}));

describe('authHooks', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockDispatch.mockClear();
    mockUseGetUser.mockClear();
    mockLogOut.mockClear();
    mockStoreToken.mockClear();
    mockStoreTenant.mockClear();
    mockStoreUser.mockClear();
    mockStoreAdminAccessKey.mockClear();
  });

  describe('useFetchUserDetails', () => {
    const mockFetchUser = jest.fn();

    beforeEach(() => {
      mockUseGetUser.mockReturnValue({
        fetchUser: mockFetchUser,
        isLoading: false,
        error: null,
      });
    });

    it('should return fetchUserDetails function and isLoading state', () => {
      const { result } = renderHook(() => useFetchUserDetails());

      expect(result.current.fetchUserDetails).toBeDefined();
      expect(typeof result.current.fetchUserDetails).toBe('function');
      expect(result.current.isLoading).toBe(false);
    });

    it('should fetch user details and store in Redux when storeInRedux is true (default)', async () => {
      mockFetchUser.mockResolvedValue(mockUserResponse);
      mockStoreUser.mockReturnValue({ type: 'STORE_USER', payload: mockUserResponse });

      const { result } = renderHook(() => useFetchUserDetails());

      await act(async () => {
        await result.current.fetchUserDetails(mockUserResponse.GUID);
      });

      expect(mockFetchUser).toHaveBeenCalledWith(mockUserResponse.GUID);
      expect(mockStoreUser).toHaveBeenCalledWith(mockUserResponse);
      expect(mockDispatch).toHaveBeenCalledWith({ type: 'STORE_USER', payload: mockUserResponse });
    });

    it('should fetch user details and store in Redux when storeInRedux is explicitly true', async () => {
      mockFetchUser.mockResolvedValue(mockUserResponse);
      mockStoreUser.mockReturnValue({ type: 'STORE_USER', payload: mockUserResponse });

      const { result } = renderHook(() => useFetchUserDetails());

      await act(async () => {
        await result.current.fetchUserDetails(mockUserResponse.GUID, true);
      });

      expect(mockFetchUser).toHaveBeenCalledWith(mockUserResponse.GUID);
      expect(mockStoreUser).toHaveBeenCalledWith(mockUserResponse);
      expect(mockDispatch).toHaveBeenCalledWith({ type: 'STORE_USER', payload: mockUserResponse });
    });

    it('should fetch user details but not store in Redux when storeInRedux is false', async () => {
      mockFetchUser.mockResolvedValue(mockUserResponse);

      const { result } = renderHook(() => useFetchUserDetails());

      await act(async () => {
        await result.current.fetchUserDetails(mockUserResponse.GUID, false);
      });

      expect(mockFetchUser).toHaveBeenCalledWith(mockUserResponse.GUID);
      expect(mockStoreUser).not.toHaveBeenCalled();
      expect(mockDispatch).not.toHaveBeenCalled();
    });

    it('should not store user in Redux when response has no GUID', async () => {
      const responseWithoutGUID = { ...mockUserData[0], GUID: undefined } as any;
      mockFetchUser.mockResolvedValue(responseWithoutGUID);

      const { result } = renderHook(() => useFetchUserDetails());

      await act(async () => {
        await result.current.fetchUserDetails(mockUserResponse.GUID);
      });

      expect(mockFetchUser).toHaveBeenCalledWith(mockUserResponse.GUID);
      expect(mockStoreUser).not.toHaveBeenCalled();
      expect(mockDispatch).not.toHaveBeenCalled();
    });

    it('should not store user in Redux when response is null', async () => {
      mockFetchUser.mockResolvedValue(null);

      const { result } = renderHook(() => useFetchUserDetails());

      await act(async () => {
        await result.current.fetchUserDetails(mockUserResponse.GUID);
      });

      expect(mockFetchUser).toHaveBeenCalledWith(mockUserResponse.GUID);
      expect(mockStoreUser).not.toHaveBeenCalled();
      expect(mockDispatch).not.toHaveBeenCalled();
    });

    it('should handle fetchUser rejection gracefully', async () => {
      mockFetchUser.mockRejectedValue(new Error('Fetch failed'));

      const { result } = renderHook(() => useFetchUserDetails());

      await act(async () => {
        await expect(result.current.fetchUserDetails(mockUserResponse.GUID)).rejects.toThrow(
          'Fetch failed'
        );
      });

      expect(mockFetchUser).toHaveBeenCalledWith(mockUserResponse.GUID);
      expect(mockStoreUser).not.toHaveBeenCalled();
      expect(mockDispatch).not.toHaveBeenCalled();
    });

    it('should return loading state from useGetUser', () => {
      mockUseGetUser.mockReturnValue({
        fetchUser: mockFetchUser,
        isLoading: true,
        error: null,
      });

      const { result } = renderHook(() => useFetchUserDetails());

      expect(result.current.isLoading).toBe(true);
    });
  });

  describe('useCredentialsToLogin', () => {
    const mockToken: Token = {
      TimestampUtc: new Date().toISOString(),
      ExpirationUtc: new Date().toISOString(),
      IsExpired: false,
      User: {
        GUID: mockUserResponse.GUID,
        FirstName: mockUserResponse.FirstName,
        LastName: mockUserResponse.LastName,
        Email: mockUserResponse.Email,
        CreatedUtc: new Date().toISOString(),
        LastUpdateUtc: new Date().toISOString(),
      },
      Valid: true,
      TenantGUID: mockTenantData[0].GUID,
      Token: 'test-token',
      UserGUID: mockUserResponse.GUID,
    };

    beforeEach(() => {
      mockStoreToken.mockReturnValue({ type: 'STORE_TOKEN', payload: mockToken });
      mockStoreTenant.mockReturnValue({ type: 'STORE_TENANT', payload: mockTenantData[0] });
    });

    it('should return loginWithCredentials function', () => {
      const { result } = renderHook(() => useCredentialsToLogin());

      expect(result.current).toBeDefined();
      expect(typeof result.current).toBe('function');
    });

    it('should dispatch storeToken and storeTenant actions when called', () => {
      const { result } = renderHook(() => useCredentialsToLogin());

      act(() => {
        result.current(mockToken, mockTenantData[0]);
      });

      expect(mockStoreToken).toHaveBeenCalledWith(mockToken);
      expect(mockStoreTenant).toHaveBeenCalledWith(mockTenantData[0]);
      expect(mockDispatch).toHaveBeenCalledWith({ type: 'STORE_TOKEN', payload: mockToken });
      expect(mockDispatch).toHaveBeenCalledWith({
        type: 'STORE_TENANT',
        payload: mockTenantData[0],
      });
      expect(mockDispatch).toHaveBeenCalledTimes(2);
    });

    it('should handle empty token object', () => {
      const emptyToken = {} as Token;
      const { result } = renderHook(() => useCredentialsToLogin());

      act(() => {
        result.current(emptyToken, mockTenantData[0]);
      });

      expect(mockStoreToken).toHaveBeenCalledWith(emptyToken);
      expect(mockStoreTenant).toHaveBeenCalledWith(mockTenantData[0]);
      expect(mockDispatch).toHaveBeenCalledTimes(2);
    });

    it('should handle empty tenant object', () => {
      const emptyTenant = {} as TenantMetaData;
      const { result } = renderHook(() => useCredentialsToLogin());

      act(() => {
        result.current(mockToken, emptyTenant);
      });

      expect(mockStoreToken).toHaveBeenCalledWith(mockToken);
      expect(mockStoreTenant).toHaveBeenCalledWith(emptyTenant);
      expect(mockDispatch).toHaveBeenCalledTimes(2);
    });
  });

  describe('useAdminCredentialsToLogin', () => {
    const mockAccessKey = mockCredentialData[0].BearerToken;

    beforeEach(() => {
      mockStoreAdminAccessKey.mockReturnValue({
        type: 'STORE_ADMIN_ACCESS_KEY',
        payload: mockAccessKey,
      });
    });

    it('should return loginWithAdminCredentials function', () => {
      const { result } = renderHook(() => useAdminCredentialsToLogin());

      expect(result.current).toBeDefined();
      expect(typeof result.current).toBe('function');
    });

    it('should dispatch storeAdminAccessKey action when called', () => {
      const { result } = renderHook(() => useAdminCredentialsToLogin());

      act(() => {
        result.current(mockAccessKey);
      });

      expect(mockStoreAdminAccessKey).toHaveBeenCalledWith(mockAccessKey);
      expect(mockDispatch).toHaveBeenCalledWith({
        type: 'STORE_ADMIN_ACCESS_KEY',
        payload: mockAccessKey,
      });
      expect(mockDispatch).toHaveBeenCalledTimes(1);
    });

    it('should handle empty access key', () => {
      const emptyAccessKey = '';
      const { result } = renderHook(() => useAdminCredentialsToLogin());

      act(() => {
        result.current(emptyAccessKey);
      });

      expect(mockStoreAdminAccessKey).toHaveBeenCalledWith(emptyAccessKey);
      expect(mockDispatch).toHaveBeenCalledTimes(1);
    });

    it('should handle null access key', () => {
      const nullAccessKey = null as any;
      const { result } = renderHook(() => useAdminCredentialsToLogin());

      act(() => {
        result.current(nullAccessKey);
      });

      expect(mockStoreAdminAccessKey).toHaveBeenCalledWith(nullAccessKey);
      expect(mockDispatch).toHaveBeenCalledTimes(1);
    });
  });

  describe('useLogout', () => {
    const mockPath = '/login';

    beforeEach(() => {
      mockLogOut.mockReturnValue({ type: 'LOG_OUT', payload: mockPath });
    });

    it('should return logOutFromSystem function', () => {
      const { result } = renderHook(() => useLogout());

      expect(result.current).toBeDefined();
      expect(typeof result.current).toBe('function');
    });

    it('should dispatch logOut action with path when called with path', () => {
      const { result } = renderHook(() => useLogout());

      act(() => {
        result.current(mockPath);
      });

      expect(mockLogOut).toHaveBeenCalledWith(mockPath);
      expect(mockDispatch).toHaveBeenCalledWith({ type: 'LOG_OUT', payload: mockPath });
      expect(mockDispatch).toHaveBeenCalledTimes(1);
    });

    it('should dispatch logOut action without path when called without arguments', () => {
      mockLogOut.mockReturnValue({ type: 'LOG_OUT', payload: undefined });

      const { result } = renderHook(() => useLogout());

      act(() => {
        result.current();
      });

      expect(mockLogOut).toHaveBeenCalledWith(undefined);
      expect(mockDispatch).toHaveBeenCalledWith({ type: 'LOG_OUT', payload: undefined });
      expect(mockDispatch).toHaveBeenCalledTimes(1);
    });

    it('should handle empty string path', () => {
      const emptyPath = '';
      mockLogOut.mockReturnValue({ type: 'LOG_OUT', payload: emptyPath });

      const { result } = renderHook(() => useLogout());

      act(() => {
        result.current(emptyPath);
      });

      expect(mockLogOut).toHaveBeenCalledWith(emptyPath);
      expect(mockDispatch).toHaveBeenCalledWith({ type: 'LOG_OUT', payload: emptyPath });
      expect(mockDispatch).toHaveBeenCalledTimes(1);
    });

    it('should handle null path', () => {
      const nullPath = null as any;
      mockLogOut.mockReturnValue({ type: 'LOG_OUT', payload: nullPath });

      const { result } = renderHook(() => useLogout());

      act(() => {
        result.current(nullPath);
      });

      expect(mockLogOut).toHaveBeenCalledWith(nullPath);
      expect(mockDispatch).toHaveBeenCalledWith({ type: 'LOG_OUT', payload: nullPath });
      expect(mockDispatch).toHaveBeenCalledTimes(1);
    });
  });
});
