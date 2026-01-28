// import { RootState } from '@/lib/store/store';
// import { configureStore } from '@reduxjs/toolkit';
// import sdkSlice from '@/lib/store/rtk/rtkSdkInstance';
// import { mockTenantData } from '../pages/mockData';

// export const mockInitialState = {
//   [sdkSlice.reducerPath]: sdkSlice.reducer as any,
//   liteGraph: {
//     selectedGraph: 'mock-graph-id',
//     tenant: mockTenantData[0],
//     token: null,
//     adminAccessKey: 'adminAccessKey',
//     user: null,
//   },
// } as RootState;
// export const createMockStore = () => {
//   return configureStore({
//     reducer: (state = mockInitialState) => state,
//     preloadedState: mockInitialState,

//     middleware: (gDM: any) =>
//       gDM({
//         serializableCheck: false,
//       }).concat([sdkSlice.middleware]),
//   });
// };

// tests/store/mockStore.ts
import { RootState, AppStore, AppDispatch } from '@/lib/store/store';
import { configureStore } from '@reduxjs/toolkit';
import sdkSlice from '@/lib/store/rtk/rtkSdkInstance';
import { mockTenantData } from '../pages/mockData';

// ✅ Create a fresh mock state per test
export const createMockInitialState = (): RootState => ({
  [sdkSlice.reducerPath]: undefined as any, // Let RTK initialize the API slice
  liteGraph: {
    selectedGraph: 'mock-graph-id',
    tenant: { ...mockTenantData[0] }, // Deep copy to prevent mutation
    token: null,
    adminAccessKey: 'adminAccessKey',
    user: null,
  },
});

// ✅ Create a mock store with dynamic state
export const createMockStore = (initialState: RootState) => {
  return configureStore({
    reducer: () => initialState,
    preloadedState: initialState,
    middleware: (gDM: any) =>
      gDM({
        serializableCheck: false,
      }).concat([sdkSlice.middleware]),
  });
};

// Test function to ensure store types are used
export const testStoreTypes = () => {
  const initialState = createMockInitialState();
  const store = createMockStore(initialState);

  // This ensures the types are used and covered
  const storeType: AppStore = store;
  const dispatchType: AppDispatch = store.dispatch;

  return { storeType, dispatchType, initialState };
};
