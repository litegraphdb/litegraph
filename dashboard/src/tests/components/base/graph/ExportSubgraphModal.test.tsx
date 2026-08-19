import '@testing-library/jest-dom';
import React from 'react';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import ExportSubgraphModal from '@/components/base/graph/ExportSubgraphModal';
import { renderWithRedux } from '../../../store/utils';
import { createMockInitialState } from '../../../store/mockStore';
import { mockGraphGUID } from '../../../pages/mockData';

jest.mock('react-hot-toast', () => ({
  __esModule: true,
  default: { success: jest.fn(), error: jest.fn() },
  success: jest.fn(),
  error: jest.fn(),
}));

jest.mock('file-saver', () => ({ saveAs: jest.fn() }));

jest.mock('@/lib/sdk/importExport', () => ({
  __esModule: true,
  exportGraphJsonl: jest.fn(),
  exportSubgraphJsonl: jest.fn(),
  importGraphJsonl: jest.fn(),
  importGraphAsNewJsonl: jest.fn(),
}));

const importExport = require('@/lib/sdk/importExport');
const { saveAs } = require('file-saver');
const toast = require('react-hot-toast').default;

const START_NODE = 'a1b2c3d4-0000-0000-0000-000000000001';

describe('ExportSubgraphModal', () => {
  beforeEach(() => jest.clearAllMocks());

  it('renders the modal with the start node and fields', () => {
    const initialState = createMockInitialState();
    renderWithRedux(
      <ExportSubgraphModal
        isVisible
        setIsVisible={() => {}}
        graphGuid={mockGraphGUID}
        startNodeGuid={START_NODE}
      />,
      initialState,
      undefined,
      true
    );

    expect(screen.getByTestId('export-subgraph-modal')).toBeInTheDocument();
    expect(screen.getByText(START_NODE)).toBeInTheDocument();
    expect(screen.getByText('Export subgraph')).toBeInTheDocument();
  });

  it('submits and calls the export subgraph mutation, then saves the file', async () => {
    importExport.exportSubgraphJsonl.mockResolvedValue('subgraph-jsonl');
    const setIsVisible = jest.fn();
    const initialState = createMockInitialState();
    renderWithRedux(
      <ExportSubgraphModal
        isVisible
        setIsVisible={setIsVisible}
        graphGuid={mockGraphGUID}
        startNodeGuid={START_NODE}
      />,
      initialState,
      undefined,
      true
    );

    fireEvent.click(screen.getByRole('button', { name: 'Export' }));

    await waitFor(() => expect(importExport.exportSubgraphJsonl).toHaveBeenCalled());
    const callArgs = importExport.exportSubgraphJsonl.mock.calls[0];
    expect(callArgs[0]).toBe(initialState.liteGraph.tenant?.GUID);
    expect(callArgs[1]).toBe(mockGraphGUID);
    expect(callArgs[2].StartNodeGUIDs).toEqual([START_NODE]);
    expect(callArgs[2].Direction).toBe('Outbound');

    await waitFor(() => expect(saveAs).toHaveBeenCalled());
    expect(toast.success).toHaveBeenCalled();
    expect(setIsVisible).toHaveBeenCalledWith(false);
  });
});
