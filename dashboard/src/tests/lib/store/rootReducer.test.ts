import '@testing-library/jest-dom';
import rootReducer, { handleLogout, apiMiddleWares } from '@/lib/store/rootReducer';
import { LitegraphAction } from '@/lib/store/litegraph/actions';
import { localStorageKeys, paths } from '@/constants/constant';

// Mock localStorage
const mockLocalStorage = {
  removeItem: jest.fn(),
};
Object.defineProperty(window, 'localStorage', {
  value: mockLocalStorage,
});

// Mock window.location
const mockLocation = {
  href: '',
};
Object.defineProperty(window, 'location', {
  value: mockLocation,
  writable: true,
});

// Mock the litegraph reducer
jest.mock('@/lib/store/litegraph/reducer', () => {
  return jest.fn((state = { test: 'initial' }, action) => {
    switch (action.type) {
      case 'TEST_ACTION':
        return { ...state, test: 'updated' };
      default:
        return state;
    }
  });
});

// Mock the RTK SDK slice
jest.mock('@/lib/store/rtk/rtkSdkInstance', () => ({
  reducerPath: 'api',
  reducer: jest.fn((state = { api: 'initial' }, action) => {
    switch (action.type) {
      case 'API_ACTION':
        return { ...state, api: 'updated' };
      default:
        return state;
    }
  }),
  middleware: jest.fn(),
}));

// Mock the RTK query error logger
jest.mock('@/lib/store/rtkApiMiddlewear', () => ({
  rtkQueryErrorLogger: jest.fn(),
}));

describe('rootReducer', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockLocation.href = '';
  });

  describe('handleLogout', () => {
    it('should remove all localStorage items and redirect to default login path', () => {
      handleLogout();

      expect(mockLocalStorage.removeItem).toHaveBeenCalledWith(localStorageKeys.token);
      expect(mockLocalStorage.removeItem).toHaveBeenCalledWith(localStorageKeys.tenant);
      expect(mockLocalStorage.removeItem).toHaveBeenCalledWith(localStorageKeys.user);
      expect(mockLocalStorage.removeItem).toHaveBeenCalledWith(localStorageKeys.adminAccessKey);
      expect(mockLocalStorage.removeItem).toHaveBeenCalledWith(localStorageKeys.serverUrl);
      expect(mockLocation.href).toBe(paths.login);
    });

    it('should remove all localStorage items and redirect to custom path', () => {
      const customPath = '/custom-login';
      handleLogout(customPath);

      expect(mockLocalStorage.removeItem).toHaveBeenCalledWith(localStorageKeys.token);
      expect(mockLocalStorage.removeItem).toHaveBeenCalledWith(localStorageKeys.tenant);
      expect(mockLocalStorage.removeItem).toHaveBeenCalledWith(localStorageKeys.user);
      expect(mockLocalStorage.removeItem).toHaveBeenCalledWith(localStorageKeys.adminAccessKey);
      expect(mockLocalStorage.removeItem).toHaveBeenCalledWith(localStorageKeys.serverUrl);
      expect(mockLocation.href).toBe(customPath);
    });
  });

  describe('apiMiddleWares', () => {
    it('should export array of middlewares', () => {
      expect(Array.isArray(apiMiddleWares)).toBe(true);
      expect(apiMiddleWares).toHaveLength(2);
    });
  });

  describe('resettableRootReducer', () => {
    it('should handle normal actions without logging out', () => {
      const initialState = undefined;
      const action = { type: 'TEST_ACTION' };

      const newState = rootReducer(initialState, action);

      expect(newState).toBeDefined();
      expect(newState.liteGraph).toEqual({ test: 'updated' });
    });

    it('should handle logout action and call handleLogout', () => {
      const initialState = undefined;
      const logoutPath = '/admin-login';
      const action = {
        type: LitegraphAction.LOG_OUT,
        payload: logoutPath,
      };

      // Call the reducer
      const newState = rootReducer(initialState, action);

      // Verify localStorage items were removed
      expect(mockLocalStorage.removeItem).toHaveBeenCalledWith(localStorageKeys.token);
      expect(mockLocalStorage.removeItem).toHaveBeenCalledWith(localStorageKeys.tenant);
      expect(mockLocalStorage.removeItem).toHaveBeenCalledWith(localStorageKeys.user);
      expect(mockLocalStorage.removeItem).toHaveBeenCalledWith(localStorageKeys.adminAccessKey);
      expect(mockLocalStorage.removeItem).toHaveBeenCalledWith(localStorageKeys.serverUrl);

      // Verify redirect happened
      expect(mockLocation.href).toBe(logoutPath);

      // Verify state is still returned
      expect(newState).toBeDefined();
    });

    it('should handle logout action without payload and use default path', () => {
      const initialState = undefined;
      const action = {
        type: LitegraphAction.LOG_OUT,
      };

      const newState = rootReducer(initialState, action);

      expect(mockLocation.href).toBe(paths.login);
      expect(newState).toBeDefined();
    });

    it('should pass through state correctly for non-logout actions', () => {
      const initialState = {
        liteGraph: { test: 'existing' },
        api: { api: 'existing' },
      };
      const action = { type: 'UNKNOWN_ACTION' };

      const newState = rootReducer(initialState, action);

      expect(newState).toBeDefined();
      expect(newState.liteGraph).toEqual({ test: 'existing' });
    });

    it('should work with undefined state', () => {
      const action = { type: 'INIT' };

      const newState = rootReducer(undefined, action);

      expect(newState).toBeDefined();
      expect(newState.liteGraph).toBeDefined();
      expect(newState.api).toBeDefined();
    });
  });
});
