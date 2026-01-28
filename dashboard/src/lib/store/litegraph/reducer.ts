import { ActionReducerMapBuilder, createReducer } from '@reduxjs/toolkit';
import {
  storeToken,
  storeSelectedGraph,
  storeTenant,
  storeUser,
  storeAdminAccessKey,
} from './actions';
import { TenantMetaData, Token, UserMetadata } from 'litegraphdb/dist/types/types';

export type LiteGraphStore = {
  selectedGraph: string;
  tenant: TenantMetaData | null;
  token: Token | null;
  adminAccessKey: string | null;
  user: UserMetadata | null;
};

export const initialState: LiteGraphStore = {
  selectedGraph: '',
  tenant: null,
  token: null,
  adminAccessKey: null,
  user: null,
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
  }
);

export default liteGraphReducer;
