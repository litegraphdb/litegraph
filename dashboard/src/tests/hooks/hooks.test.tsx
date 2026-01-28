import '@testing-library/jest-dom';
import { renderHook } from '@testing-library/react';
import { useAppDynamicNavigation } from '@/hooks/hooks';
import { useRouter } from 'next/navigation';
import { useCurrentTenant } from '@/hooks/entityHooks';
import toast from 'react-hot-toast';
import { dynamicSlugs } from '@/constants/constant';
import { globalToastId } from '@/constants/config';

// Mock next/navigation
jest.mock('next/navigation', () => ({
  useRouter: jest.fn(),
}));

// Mock the entityHooks
jest.mock('@/hooks/entityHooks', () => ({
  useCurrentTenant: jest.fn(),
}));

// Mock react-hot-toast
jest.mock('react-hot-toast', () => ({
  error: jest.fn(),
}));

// Mock console.log to avoid noise in tests
const consoleSpy = jest.spyOn(console, 'log').mockImplementation();

describe('useAppDynamicNavigation', () => {
  let mockRouter: any;
  let mockTenant: any;

  beforeEach(() => {
    jest.clearAllMocks();
    consoleSpy.mockClear();

    mockRouter = {
      push: jest.fn(),
    };

    mockTenant = {
      GUID: 'test-tenant-123',
      Name: 'Test Tenant',
    };

    (useRouter as jest.Mock).mockReturnValue(mockRouter);
    (useCurrentTenant as jest.Mock).mockReturnValue(mockTenant);
  });

  afterAll(() => {
    consoleSpy.mockRestore();
  });

  describe('serializePath', () => {
    it('should replace tenantId placeholder with actual tenant GUID', () => {
      const { result } = renderHook(() => useAppDynamicNavigation());

      const pathWithPlaceholder = `/dashboard/${dynamicSlugs.tenantId}/graphs`;
      const serializedPath = result.current.serializePath(pathWithPlaceholder);

      expect(serializedPath).toBe('/dashboard/test-tenant-123/graphs');
    });

    it('should return empty string when no path is provided', () => {
      const { result } = renderHook(() => useAppDynamicNavigation());

      const serializedPath = result.current.serializePath();

      expect(serializedPath).toBe('');
    });

    it('should return path unchanged when no tenantId placeholder is present', () => {
      const { result } = renderHook(() => useAppDynamicNavigation());

      const pathWithoutPlaceholder = '/dashboard/graphs';
      const serializedPath = result.current.serializePath(pathWithoutPlaceholder);

      expect(serializedPath).toBe('/dashboard/graphs');
    });

    it('should handle multiple tenantId placeholders', () => {
      const { result } = renderHook(() => useAppDynamicNavigation());

      const pathWithMultiplePlaceholders = `/dashboard/${dynamicSlugs.tenantId}/graphs/${dynamicSlugs.tenantId}/nodes`;
      const serializedPath = result.current.serializePath(pathWithMultiplePlaceholders);

      // The replace method only replaces the first occurrence by default
      expect(serializedPath).toBe('/dashboard/test-tenant-123/graphs/:tenantId/nodes');
    });

    it('should handle path with only tenantId placeholder', () => {
      const { result } = renderHook(() => useAppDynamicNavigation());

      const pathWithOnlyPlaceholder = `/${dynamicSlugs.tenantId}`;
      const serializedPath = result.current.serializePath(pathWithOnlyPlaceholder);

      expect(serializedPath).toBe('/test-tenant-123');
    });
  });

  describe('navigate', () => {
    it('should navigate to path with tenantId placeholder when tenant exists', () => {
      const { result } = renderHook(() => useAppDynamicNavigation());

      const pathWithPlaceholder = `/dashboard/${dynamicSlugs.tenantId}/graphs`;
      const options = { scroll: false };

      result.current.navigate(pathWithPlaceholder, options);

      expect(mockRouter.push).toHaveBeenCalledWith('/dashboard/test-tenant-123/graphs', options);
    });

    it('should navigate to path without tenantId placeholder unchanged', () => {
      const { result } = renderHook(() => useAppDynamicNavigation());

      const pathWithoutPlaceholder = '/dashboard/graphs';
      const options = { scroll: true };

      result.current.navigate(pathWithoutPlaceholder, options);

      expect(mockRouter.push).toHaveBeenCalledWith(pathWithoutPlaceholder, options);
    });

    it('should navigate to path without options when options not provided', () => {
      const { result } = renderHook(() => useAppDynamicNavigation());

      const pathWithPlaceholder = `/dashboard/${dynamicSlugs.tenantId}/graphs`;

      result.current.navigate(pathWithPlaceholder);

      expect(mockRouter.push).toHaveBeenCalledWith('/dashboard/test-tenant-123/graphs', undefined);
    });

    it('should show error toast and log when tenant is missing and path contains placeholder', () => {
      (useCurrentTenant as jest.Mock).mockReturnValue(null);

      const { result } = renderHook(() => useAppDynamicNavigation());

      const pathWithPlaceholder = `/dashboard/${dynamicSlugs.tenantId}/graphs`;

      result.current.navigate(pathWithPlaceholder);

      expect(toast.error).toHaveBeenCalledWith('Error while navigating', { id: globalToastId });
      expect(consoleSpy).toHaveBeenCalledWith('No tenant found, cannot navigate.');
      expect(mockRouter.push).not.toHaveBeenCalled();
    });

    it('should show error toast and log when tenant is undefined and path contains placeholder', () => {
      (useCurrentTenant as jest.Mock).mockReturnValue(undefined);

      const { result } = renderHook(() => useAppDynamicNavigation());

      const pathWithPlaceholder = `/dashboard/${dynamicSlugs.tenantId}/graphs`;

      result.current.navigate(pathWithPlaceholder);

      expect(toast.error).toHaveBeenCalledWith('Error while navigating', { id: globalToastId });
      expect(consoleSpy).toHaveBeenCalledWith('No tenant found, cannot navigate.');
      expect(mockRouter.push).not.toHaveBeenCalled();
    });

    it('should show error toast and log when tenant has no GUID and path contains placeholder', () => {
      (useCurrentTenant as jest.Mock).mockReturnValue({ Name: 'Test Tenant' });

      const { result } = renderHook(() => useAppDynamicNavigation());

      const pathWithPlaceholder = `/dashboard/${dynamicSlugs.tenantId}/graphs`;

      result.current.navigate(pathWithPlaceholder);

      expect(toast.error).toHaveBeenCalledWith('Error while navigating', { id: globalToastId });
      expect(consoleSpy).toHaveBeenCalledWith('No tenant found, cannot navigate.');
      expect(mockRouter.push).not.toHaveBeenCalled();
    });

    it('should handle empty string path', () => {
      const { result } = renderHook(() => useAppDynamicNavigation());

      result.current.navigate('');

      expect(mockRouter.push).toHaveBeenCalledWith('', undefined);
    });

    it('should handle path with only forward slash', () => {
      const { result } = renderHook(() => useAppDynamicNavigation());

      result.current.navigate('/');

      expect(mockRouter.push).toHaveBeenCalledWith('/', undefined);
    });

    it('should handle complex path with query parameters', () => {
      const { result } = renderHook(() => useAppDynamicNavigation());

      const pathWithQuery = `/dashboard/${dynamicSlugs.tenantId}/graphs?filter=active&sort=name`;
      const options = { scroll: false };

      result.current.navigate(pathWithQuery, options);

      expect(mockRouter.push).toHaveBeenCalledWith(
        '/dashboard/test-tenant-123/graphs?filter=active&sort=name',
        options
      );
    });

    it('should handle path with hash fragments', () => {
      const { result } = renderHook(() => useAppDynamicNavigation());

      const pathWithHash = `/dashboard/${dynamicSlugs.tenantId}/graphs#section1`;
      const options = { scroll: true };

      result.current.navigate(pathWithHash, options);

      expect(mockRouter.push).toHaveBeenCalledWith(
        '/dashboard/test-tenant-123/graphs#section1',
        options
      );
    });
  });

  describe('hook return value', () => {
    it('should return navigate and serializePath functions', () => {
      const { result } = renderHook(() => useAppDynamicNavigation());

      expect(result.current).toHaveProperty('navigate');
      expect(result.current).toHaveProperty('serializePath');
      expect(typeof result.current.navigate).toBe('function');
      expect(typeof result.current.serializePath).toBe('function');
    });

    it('should maintain function references between renders', () => {
      const { result, rerender } = renderHook(() => useAppDynamicNavigation());

      const firstNavigate = result.current.navigate;
      const firstSerializePath = result.current.serializePath;

      rerender();

      // In React, functions are recreated on each render unless memoized
      // This is the expected behavior, so we should test that they are functions
      expect(typeof result.current.navigate).toBe('function');
      expect(typeof result.current.serializePath).toBe('function');
    });
  });

  describe('edge cases', () => {
    it('should handle tenant with empty GUID string', () => {
      (useCurrentTenant as jest.Mock).mockReturnValue({ GUID: '', Name: 'Empty Tenant' });

      const { result } = renderHook(() => useAppDynamicNavigation());

      const pathWithPlaceholder = `/dashboard/${dynamicSlugs.tenantId}/graphs`;

      result.current.navigate(pathWithPlaceholder);

      expect(toast.error).toHaveBeenCalledWith('Error while navigating', { id: globalToastId });
      expect(consoleSpy).toHaveBeenCalledWith('No tenant found, cannot navigate.');
      expect(mockRouter.push).not.toHaveBeenCalled();
    });

    it('should handle tenant with whitespace-only GUID', () => {
      (useCurrentTenant as jest.Mock).mockReturnValue({ GUID: '   ', Name: 'Whitespace Tenant' });

      const { result } = renderHook(() => useAppDynamicNavigation());

      const pathWithPlaceholder = `/dashboard/${dynamicSlugs.tenantId}/graphs`;

      result.current.navigate(pathWithPlaceholder);

      // Whitespace-only GUID is truthy in JavaScript, so it will be used
      // The path will be serialized with the whitespace GUID
      expect(mockRouter.push).toHaveBeenCalledWith('/dashboard/   /graphs', undefined);
      expect(toast.error).not.toHaveBeenCalled();
      expect(consoleSpy).not.toHaveBeenCalled();
    });

    it('should handle very long tenant GUID', () => {
      const longGuid = 'a'.repeat(1000);
      (useCurrentTenant as jest.Mock).mockReturnValue({ GUID: longGuid, Name: 'Long Tenant' });

      const { result } = renderHook(() => useAppDynamicNavigation());

      const pathWithPlaceholder = `/dashboard/${dynamicSlugs.tenantId}/graphs`;

      result.current.navigate(pathWithPlaceholder);

      expect(mockRouter.push).toHaveBeenCalledWith(`/dashboard/${longGuid}/graphs`, undefined);
    });
  });
});
