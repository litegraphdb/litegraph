import { configureStore } from '@reduxjs/toolkit';
import rootReducer, { apiMiddleWares } from './rootReducer';

export const makeStore = () => {
  return configureStore({
    reducer: rootReducer,
    middleware: (gDM: any) =>
      gDM({
        serializableCheck: false,
      }).concat(apiMiddleWares),
  });
};

export type AppStore = ReturnType<typeof makeStore>;

// Infer the `RootState` and `AppDispatch` types from the store itself
export type RootState = ReturnType<typeof rootReducer>;
export type AppDispatch = AppStore['dispatch'];
