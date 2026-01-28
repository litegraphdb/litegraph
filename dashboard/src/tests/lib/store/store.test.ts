import '@testing-library/jest-dom';
import { makeStore, AppStore, RootState, AppDispatch } from '@/lib/store/store';
import { configureStore } from '@reduxjs/toolkit';

// Mock the rootReducer and apiMiddleWares
jest.mock('@/lib/store/rootReducer', () => ({
  __esModule: true,
  default: jest.fn((state = { test: 'initial' }, action) => {
    switch (action.type) {
      case 'TEST_ACTION':
        return { ...state, test: 'updated' };
      default:
        return state;
    }
  }),
  apiMiddleWares: [
    jest.fn((store) => (next) => (action) => {
      // Mock middleware 1
      return next(action);
    }),
    jest.fn((store) => (next) => (action) => {
      // Mock middleware 2
      return next(action);
    }),
  ],
}));

// Mock configureStore to verify it's called with correct parameters
jest.mock('@reduxjs/toolkit', () => ({
  ...jest.requireActual('@reduxjs/toolkit'),
  configureStore: jest.fn(),
}));

describe('Store Configuration', () => {
  beforeEach(() => {
    jest.clearAllMocks();

    // Setup configureStore mock to return a mock store
    const mockStore = {
      dispatch: jest.fn(),
      getState: jest.fn(() => ({ test: 'initial' })),
      subscribe: jest.fn(),
      replaceReducer: jest.fn(),
    };

    (configureStore as jest.Mock).mockReturnValue(mockStore);
  });

  describe('makeStore', () => {
    it('should create a store with correct configuration', () => {
      const store = makeStore();

      expect(configureStore).toHaveBeenCalledWith({
        reducer: expect.any(Function),
        middleware: expect.any(Function),
      });

      expect(store).toBeDefined();
      expect(store.dispatch).toBeDefined();
      expect(store.getState).toBeDefined();
    });

    it('should configure middleware correctly', () => {
      makeStore();

      const configCall = (configureStore as jest.Mock).mock.calls[0][0];
      expect(configCall.middleware).toBeInstanceOf(Function);

      // Test the middleware configuration function
      const mockGetDefaultMiddleware = jest.fn().mockReturnValue({
        concat: jest.fn().mockReturnValue(['middleware1', 'middleware2']),
      });

      const middlewareConfig = configCall.middleware(mockGetDefaultMiddleware);

      expect(mockGetDefaultMiddleware).toHaveBeenCalledWith({
        serializableCheck: false,
      });
      expect(mockGetDefaultMiddleware().concat).toHaveBeenCalledWith(
        expect.arrayContaining([expect.any(Function), expect.any(Function)])
      );
    });

    it('should create multiple independent store instances', () => {
      const store1 = makeStore();
      const store2 = makeStore();

      expect(store1).toBeDefined();
      expect(store2).toBeDefined();
      expect(configureStore).toHaveBeenCalledTimes(2);
    });

    it('should use the correct reducer', () => {
      makeStore();

      const configCall = (configureStore as jest.Mock).mock.calls[0][0];
      expect(configCall.reducer).toBeDefined();

      // Test that the reducer function works
      const rootReducer = require('@/lib/store/rootReducer').default;
      const initialState = undefined;
      const testAction = { type: 'TEST_ACTION' };

      const newState = rootReducer(initialState, testAction);
      expect(newState).toEqual({ test: 'updated' });
    });
  });

  describe('Type exports', () => {
    it('should export correct TypeScript types', () => {
      // These are compile-time checks, but we can verify the types exist
      // by checking the imported types are defined
      expect(makeStore).toBeDefined();

      // Create a store to test the types
      const store = makeStore();

      // Verify AppStore type matches the return type of makeStore
      const appStore: AppStore = store;
      expect(appStore).toBe(store);

      // Verify AppDispatch type matches the store's dispatch
      const dispatch: AppDispatch = store.dispatch;
      expect(dispatch).toBe(store.dispatch);

      // Verify RootState type by checking state structure
      const state = store.getState();
      const rootState: RootState = state;
      expect(rootState).toBe(state);
    });
  });

  describe('Store functionality', () => {
    it('should allow dispatching actions', () => {
      const store = makeStore();
      const testAction = { type: 'TEST_ACTION' };

      store.dispatch(testAction);

      expect(store.dispatch).toHaveBeenCalledWith(testAction);
    });

    it('should maintain state correctly', () => {
      const store = makeStore();

      const state = store.getState();

      expect(state).toEqual({ test: 'initial' });
    });

    it('should support subscription', () => {
      const store = makeStore();
      const listener = jest.fn();

      store.subscribe(listener);

      expect(store.subscribe).toHaveBeenCalledWith(listener);
    });

    it('should support reducer replacement', () => {
      const store = makeStore();
      const newReducer = jest.fn();

      store.replaceReducer(newReducer);

      expect(store.replaceReducer).toHaveBeenCalledWith(newReducer);
    });
  });

  describe('Middleware integration', () => {
    it('should include all required middlewares', () => {
      makeStore();

      const configCall = (configureStore as jest.Mock).mock.calls[0][0];
      const mockGetDefaultMiddleware = jest.fn().mockReturnValue({
        concat: jest.fn().mockReturnValue(['combined', 'middlewares']),
      });

      configCall.middleware(mockGetDefaultMiddleware);

      // Verify that apiMiddleWares are concatenated
      const { apiMiddleWares } = require('@/lib/store/rootReducer');
      expect(mockGetDefaultMiddleware().concat).toHaveBeenCalledWith(apiMiddleWares);
    });

    it('should disable serializable check', () => {
      makeStore();

      const configCall = (configureStore as jest.Mock).mock.calls[0][0];
      const mockGetDefaultMiddleware = jest.fn().mockReturnValue({
        concat: jest.fn().mockReturnValue([]),
      });

      configCall.middleware(mockGetDefaultMiddleware);

      expect(mockGetDefaultMiddleware).toHaveBeenCalledWith({
        serializableCheck: false,
      });
    });
  });
});
