import { ActionReducerMapBuilder, createReducer } from '@reduxjs/toolkit';
import {
  storeToken,
  storeSelectedGraph,
  storeTenant,
  storeUser,
  storeAdminAccessKey,
  storeLocale,
} from './actions';
import { TenantMetaData, Token, UserMetadata } from 'litegraphdb/dist/types/types';
import { AppLocale, DEFAULT_LOCALE } from '@/i18n/locales';

export type LiteGraphStore = {
  selectedGraph: string;
  tenant: TenantMetaData | null;
  token: Token | null;
  adminAccessKey: string | null;
  user: UserMetadata | null;
  locale: AppLocale;
};

export const initialState: LiteGraphStore = {
  selectedGraph: '',
  tenant: null,
  token: null,
  adminAccessKey: null,
  user: null,
  locale: DEFAULT_LOCALE,
};

const liteGraphReducer = createReducer(
  initialState,
  (builder: ActionReducerMapBuilder<typeof initialState>) => {
    // Store selected graph
    builder.addCase(
      storeSelectedGraph,
      (state: typeof initialState, action: ReturnType<typeof storeSelectedGraph>) => {
        state.selectedGraph = action.payload.graph;
      }
    );
    // Store tenant
    builder.addCase(
      storeTenant,
      (state: typeof initialState, action: ReturnType<typeof storeTenant>) => {
        state.tenant = action.payload;
      }
    );
    // Store credentials
    builder.addCase(
      storeToken,
      (state: typeof initialState, action: ReturnType<typeof storeToken>) => {
        state.token = action.payload;
      }
    );
    // Store user
    builder.addCase(
      storeUser,
      (state: typeof initialState, action: ReturnType<typeof storeUser>) => {
        state.user = action.payload;
      }
    );
    // Store admin token
    builder.addCase(
      storeAdminAccessKey,
      (state: typeof initialState, action: ReturnType<typeof storeAdminAccessKey>) => {
        state.adminAccessKey = action.payload;
      }
    );
    // Store active UI locale
    builder.addCase(
      storeLocale,
      (state: typeof initialState, action: ReturnType<typeof storeLocale>) => {
        state.locale = action.payload;
      }
    );
  }
);

export default liteGraphReducer;
