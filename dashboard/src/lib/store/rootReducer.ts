import { UnknownAction, combineReducers } from '@reduxjs/toolkit';
import liteGraphReducer from './litegraph/reducer';
import { rtkQueryErrorLogger } from './rtkApiMiddlewear';
import { localStorageKeys, paths } from '@/constants/constant';
import { LitegraphAction } from './litegraph/actions';
import sdkSlice from './rtk/rtkSdkInstance';

const rootReducer = combineReducers({
  [sdkSlice.reducerPath]: sdkSlice.reducer,
  liteGraph: liteGraphReducer,
});

export const apiMiddleWares = [rtkQueryErrorLogger, sdkSlice.middleware];

export const handleLogout = (path?: string) => {
  localStorage.removeItem(localStorageKeys.token);
  localStorage.removeItem(localStorageKeys.tenant);
  localStorage.removeItem(localStorageKeys.user);
  localStorage.removeItem(localStorageKeys.adminAccessKey);
  localStorage.removeItem(localStorageKeys.serverUrl);
  window.location.href = path ? path : paths.login;
};

const resettableRootReducer = (
  state: ReturnType<typeof rootReducer> | undefined,
  action: UnknownAction
) => {
  if (action.type === LitegraphAction.LOG_OUT) {
    handleLogout(action.payload as any);
  }
  return rootReducer(state, action);
};

export default resettableRootReducer;
