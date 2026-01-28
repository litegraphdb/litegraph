import { createAction } from '@reduxjs/toolkit';
import { TenantMetaData, Token, UserMetadata } from 'litegraphdb/dist/types/types';

export const LitegraphAction = {
  STORE_SELECTED_GRAPH: 'STORE_SELECTED_GRAPH',
  STORE_TENANT: 'STORE_TENANT',
  STORE_CREDENTIALS: 'STORE_CREDENTIALS',
  STORE_USER: 'STORE_USER',
  LOG_OUT: 'LOG_OUT',
  STORE_ADMIN_ACCESS_KEY: 'STORE_ADMIN_ACCESS_KEY',
};

export const storeSelectedGraph = createAction<{ graph: string }>(
  LitegraphAction.STORE_SELECTED_GRAPH
);
export const storeTenant = createAction<TenantMetaData | null>(LitegraphAction.STORE_TENANT);
export const storeToken = createAction<Token | null>(LitegraphAction.STORE_CREDENTIALS);
export const storeUser = createAction<UserMetadata | null>(LitegraphAction.STORE_USER);
export const logOut = createAction<string | undefined>(LitegraphAction.LOG_OUT);
export const storeAdminAccessKey = createAction<string | null>(
  LitegraphAction.STORE_ADMIN_ACCESS_KEY
);
