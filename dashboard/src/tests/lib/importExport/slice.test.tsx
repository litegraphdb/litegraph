import '@testing-library/jest-dom';
import React from 'react';
import { renderHook, act } from '@testing-library/react';
import { Provider } from 'react-redux';
import { configureStore } from '@reduxjs/toolkit';
import resettableRootReducer from '@/lib/store/rootReducer';
import sdkSlice from '@/lib/store/rtk/rtkSdkInstance';
import {
  useExportGraphJsonlMutation,
  useExportSubgraphJsonlMutation,
  useImportGraphJsonlMutation,
  useImportGraphAsNewJsonlMutation,
} from '@/lib/store/slice/slice';

jest.mock('@/lib/sdk/importExport', () => ({
  __esModule: true,
  exportGraphJsonl: jest.fn(),
  exportSubgraphJsonl: jest.fn(),
  importGraphJsonl: jest.fn(),
  importGraphAsNewJsonl: jest.fn(),
}));

const importExport = require('@/lib/sdk/importExport');

const createWrapper = () => {
  const store = configureStore({
    reducer: resettableRootReducer,
    middleware: (gDM: any) => gDM({ serializableCheck: false }).concat([sdkSlice.middleware]),
  });
  // eslint-disable-next-line react/display-name
  return ({ children }: { children: React.ReactNode }) => (
    <Provider store={store}>{children}</Provider>
  );
};

const TENANT = '00000000-0000-0000-0000-000000000000';
const GRAPH = 'e6d4294e-6f49-4d67-8260-5e44c2b077a6';

describe('import/export JSONL RTK endpoints', () => {
  beforeEach(() => jest.clearAllMocks());

  it('exportGraphJsonl calls the helper and returns text', async () => {
    importExport.exportGraphJsonl.mockResolvedValue('jsonl-text');
    const { result } = renderHook(() => useExportGraphJsonlMutation(), { wrapper: createWrapper() });

    let response: any;
    await act(async () => {
      response = await result.current[0]({
        tenantGuid: TENANT,
        graphGuid: GRAPH,
        options: { includeData: true, includeSubordinates: true },
      }).unwrap();
    });

    expect(importExport.exportGraphJsonl).toHaveBeenCalledWith(TENANT, GRAPH, {
      includeData: true,
      includeSubordinates: true,
    });
    expect(response).toBe('jsonl-text');
  });

  it('exportSubgraphJsonl calls the helper with the extraction request', async () => {
    importExport.exportSubgraphJsonl.mockResolvedValue('subgraph-text');
    const { result } = renderHook(() => useExportSubgraphJsonlMutation(), {
      wrapper: createWrapper(),
    });

    const request = { StartNodeGUIDs: ['n1'], MaxDepth: 2, Direction: 'Outbound' as const };
    let response: any;
    await act(async () => {
      response = await result.current[0]({
        tenantGuid: TENANT,
        graphGuid: GRAPH,
        request,
      }).unwrap();
    });

    expect(importExport.exportSubgraphJsonl).toHaveBeenCalledWith(TENANT, GRAPH, request);
    expect(response).toBe('subgraph-text');
  });

  it('importGraphJsonl calls the helper and returns the import result', async () => {
    const importResult = { Success: true, NodesCreated: 3 };
    importExport.importGraphJsonl.mockResolvedValue(importResult);
    const { result } = renderHook(() => useImportGraphJsonlMutation(), { wrapper: createWrapper() });

    let response: any;
    await act(async () => {
      response = await result.current[0]({
        tenantGuid: TENANT,
        graphGuid: GRAPH,
        jsonl: 'line',
        options: { guidStrategy: 'regenerate', onError: 'abort' },
      }).unwrap();
    });

    expect(importExport.importGraphJsonl).toHaveBeenCalledWith(TENANT, GRAPH, 'line', {
      guidStrategy: 'regenerate',
      onError: 'abort',
    });
    expect(response).toEqual(importResult);
  });

  it('importGraphAsNewJsonl calls the helper without a graph GUID', async () => {
    const importResult = { Success: true, GraphsCreated: 1 };
    importExport.importGraphAsNewJsonl.mockResolvedValue(importResult);
    const { result } = renderHook(() => useImportGraphAsNewJsonlMutation(), {
      wrapper: createWrapper(),
    });

    let response: any;
    await act(async () => {
      response = await result.current[0]({
        tenantGuid: TENANT,
        jsonl: 'line',
        options: { guidStrategy: 'preserve' },
      }).unwrap();
    });

    expect(importExport.importGraphAsNewJsonl).toHaveBeenCalledWith(TENANT, 'line', {
      guidStrategy: 'preserve',
    });
    expect(response).toEqual(importResult);
  });
});
