import '@testing-library/jest-dom';
import React from 'react';
import { screen, waitFor } from '@testing-library/react';
import { createMockInitialState } from '@/tests/store/mockStore';
import { renderWithRedux } from '@/tests/store/utils';
import { getServer } from '@/tests/server';
import { handlers } from './handler';
import { commonHandlers } from '@/tests/handler';

// Import the component and function
import AuthLayout, { initializeAuthFromLocalStorage } from '@/components/layout/AuthLayout';

// Mock all dependencies
jest.mock('@/lib/store/hooks', () => ({
  useAppDispatch: jest.fn(),
}));

// Create mock localStorage
const createMockLocalStorage = () => ({
  getItem: jest.fn(),
  setItem: jest.fn(),
  removeItem: jest.fn(),
  clear: jest.fn(),
  length: 0,
  key: jest.fn(),
});

// Store original localStorage
const originalLocalStorage = global.localStorage;

const server = getServer([...commonHandlers, ...handlers]);

// Mock data
const mockToken = {
  Token: 'mock-jwt-token-12345',
  RefreshToken: 'mock-refresh-token-67890',
  ExpiresAt: '2024-12-31T23:59:59Z',
  TokenType: 'Bearer',
};

const mockTenant = {
  GUID: 'tenant-123-456-789',
  Name: 'Test Tenant',
  Description: 'A test tenant for development',
  Status: 'Active',
  CreatedAt: '2024-01-01T00:00:00Z',
  UpdatedAt: '2024-01-01T00:00:00Z',
};

const mockAdminAccessKey = 'admin-access-key-12345';
const mockServerUrl = 'https://test-server.example.com';

describe('AuthLayout', () => {
  let mockDispatch: jest.Mock;
  let mockLocalStorage: ReturnType<typeof createMockLocalStorage>;

  beforeAll(() => {
    server.listen();
  });

  beforeEach(() => {
    jest.clearAllMocks();
    mockDispatch = jest.fn();
    mockLocalStorage = createMockLocalStorage();

    // Setup mocks
    require('@/lib/store/hooks').useAppDispatch.mockReturnValue(mockDispatch);

    // Mock localStorage properly
    Object.defineProperty(window, 'localStorage', {
      value: mockLocalStorage,
      writable: true,
    });
  });

  afterEach(() => {
    server.resetHandlers();
    jest.clearAllMocks();
  });

  afterAll(() => {
    server.close();
    // Restore original localStorage
    Object.defineProperty(window, 'localStorage', {
      value: originalLocalStorage,
      writable: true,
    });
  });

  describe('initializeAuthFromLocalStorage', () => {
    it('should return null when window is undefined (SSR)', () => {
      // Mock window as undefined
      const originalWindow = global.window;
      // @ts-ignore
      delete global.window;

      const result = initializeAuthFromLocalStorage();
      expect(result).toBeNull();

      // Restore window
      global.window = originalWindow;
    });

    it('should return empty auth object when localStorage is empty', () => {
      mockLocalStorage.getItem.mockReturnValue(null);

      const result = initializeAuthFromLocalStorage();

      expect(result).toEqual({
        selectedGraph: '',
        tenant: null,
        token: null,
        user: null,
        adminAccessKey: null,
      });
      expect(mockLocalStorage.getItem).toHaveBeenCalledWith('token');
      expect(mockLocalStorage.getItem).toHaveBeenCalledWith('tenant');
      expect(mockLocalStorage.getItem).toHaveBeenCalledWith('adminAccessKey');
      expect(mockLocalStorage.getItem).toHaveBeenCalledWith('serverUrl');
    });

    it('should return auth object with token when token exists in localStorage', () => {
      mockLocalStorage.getItem.mockImplementation((key: string) => {
        if (key === 'token') return JSON.stringify(mockToken);
        return null;
      });

      const result = initializeAuthFromLocalStorage();

      expect(result).toEqual({
        selectedGraph: '',
        tenant: null,
        token: mockToken,
        user: null,
        adminAccessKey: null,
      });
    });

    it('should return auth object with tenant when tenant exists in localStorage', () => {
      mockLocalStorage.getItem.mockImplementation((key: string) => {
        if (key === 'tenant') return JSON.stringify(mockTenant);
        return null;
      });

      const result = initializeAuthFromLocalStorage();

      expect(result).toEqual({
        selectedGraph: '',
        tenant: mockTenant,
        token: null,
        user: null,
        adminAccessKey: null,
      });
    });

    it('should return auth object with adminAccessKey when it exists in localStorage', () => {
      mockLocalStorage.getItem.mockImplementation((key: string) => {
        if (key === 'adminAccessKey') return mockAdminAccessKey;
        return null;
      });

      const result = initializeAuthFromLocalStorage();

      expect(result).toEqual({
        selectedGraph: '',
        tenant: null,
        token: null,
        user: null,
        adminAccessKey: mockAdminAccessKey,
      });
    });

    it('should return complete auth object when all data exists in localStorage', () => {
      mockLocalStorage.getItem.mockImplementation((key: string) => {
        switch (key) {
          case 'token':
            return JSON.stringify(mockToken);
          case 'tenant':
            return JSON.stringify(mockTenant);
          case 'adminAccessKey':
            return mockAdminAccessKey;
          case 'serverUrl':
            return mockServerUrl;
          default:
            return null;
        }
      });

      const result = initializeAuthFromLocalStorage();

      expect(result).toEqual({
        selectedGraph: '',
        tenant: mockTenant,
        token: mockToken,
        user: null,
        adminAccessKey: mockAdminAccessKey,
      });
    });
  });

  describe('AuthLayout Component', () => {
    it('should render children after initialization with empty localStorage', async () => {
      mockLocalStorage.getItem.mockReturnValue(null);
      const initialState = createMockInitialState();

      renderWithRedux(
        <AuthLayout>
          <div data-testid="test-child">Test Content</div>
        </AuthLayout>,
        initialState
      );

      await waitFor(() => {
        expect(screen.getByTestId('test-child')).toBeInTheDocument();
        expect(screen.getByText('Test Content')).toBeInTheDocument();
        expect(screen.queryByTestId('page-loading')).not.toBeInTheDocument();
      });
    });

    it('should apply custom className to root div', async () => {
      mockLocalStorage.getItem.mockReturnValue(null);
      const initialState = createMockInitialState();
      const customClassName = 'custom-auth-layout';

      renderWithRedux(
        <AuthLayout className={customClassName}>
          <div data-testid="test-child">Test Content</div>
        </AuthLayout>,
        initialState
      );

      await waitFor(() => {
        expect(screen.getByTestId('test-child')).toBeInTheDocument();
      });

      const rootDiv = document.getElementById('root-div');
      expect(rootDiv).toBeInTheDocument();
      expect(rootDiv).toHaveClass(customClassName);
    });

    it('should render root div with id="root-div"', async () => {
      mockLocalStorage.getItem.mockReturnValue(null);
      const initialState = createMockInitialState();

      renderWithRedux(
        <AuthLayout>
          <div data-testid="test-child">Test Content</div>
        </AuthLayout>,
        initialState
      );

      await waitFor(() => {
        expect(screen.getByTestId('test-child')).toBeInTheDocument();
      });

      const rootDiv = document.getElementById('root-div');
      expect(rootDiv).toBeInTheDocument();
    });
  });
});
