import resettableRootReducer from '@/lib/store/rootReducer';
import sdkSlice from '@/lib/store/rtk/rtkSdkInstance';
import { RootState } from '@/lib/store/store';
import { configureStore } from '@reduxjs/toolkit';
import { render } from '@testing-library/react';
import { Provider } from 'react-redux';

export const renderWithRedux = (
  ui: React.ReactNode,
  reduxState?: RootState,
  useGraphsSelector = false,
  useTenantSelector = false
) => {
  const store = configureStore({
    reducer: resettableRootReducer,
    preloadedState: reduxState,
    middleware: (gDM: any) =>
      gDM({
        serializableCheck: false,
      }).concat([sdkSlice.middleware]),
  });

  const utils = render(<Provider store={store}>{ui}</Provider>);

  return {
    ...utils,
    store, // ⬅️ expose store for dispatching or inspection if needed
  };
};
