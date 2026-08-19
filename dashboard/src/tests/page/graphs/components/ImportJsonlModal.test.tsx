import '@testing-library/jest-dom';
import React from 'react';
import { screen, fireEvent, waitFor, within } from '@testing-library/react';
import ImportJsonlModal from '@/page/graphs/components/ImportJsonlModal';
import { renderWithRedux } from '../../../store/utils';
import { createMockInitialState } from '../../../store/mockStore';
import { mockGraphGUID } from '../../../pages/mockData';

jest.mock('react-hot-toast', () => ({
  __esModule: true,
  default: { success: jest.fn(), error: jest.fn() },
  success: jest.fn(),
  error: jest.fn(),
}));

jest.mock('@/lib/sdk/importExport', () => ({
  __esModule: true,
  exportGraphJsonl: jest.fn(),
  exportSubgraphJsonl: jest.fn(),
  importGraphJsonl: jest.fn(),
  importGraphAsNewJsonl: jest.fn(),
}));

const importExport = require('@/lib/sdk/importExport');
const toast = require('react-hot-toast').default;

const SAMPLE_JSONL = [
  '# LiteGraph subgraph export',
  '{"Type":"Node","GUID":"n1"}',
  '{"Type":"Node","GUID":"n2"}',
  '{"Type":"Edge","GUID":"e1"}',
].join('\n');

const mockResult = {
  Success: true,
  TenantGUID: 'tenant',
  GraphGUID: mockGraphGUID,
  GraphsCreated: 0,
  NodesCreated: 2,
  NodesUpdated: 0,
  NodesSkipped: 0,
  EdgesCreated: 1,
  EdgesUpdated: 0,
  EdgesSkipped: 0,
  LinesRead: 4,
  LinesIgnored: 0,
  Warnings: [],
  GuidMap: {},
};

const uploadFile = () => {
  const input = document.querySelector('input[type="file"]') as HTMLInputElement;
  const file = new File([SAMPLE_JSONL], 'graph.jsonl', { type: 'application/x-ndjson' });
  // jsdom's File does not implement Blob.text(); browsers do. Polyfill for tests.
  (file as unknown as { text: () => Promise<string> }).text = () => Promise.resolve(SAMPLE_JSONL);
  fireEvent.change(input, { target: { files: [file] } });
};

describe('ImportJsonlModal', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders the merge import modal with title', () => {
    const initialState = createMockInitialState();
    renderWithRedux(
      <ImportJsonlModal
        isVisible
        setIsVisible={() => {}}
        mode="merge"
        targetGraphGuid={mockGraphGUID}
      />,
      initialState,
      undefined,
      true
    );

    expect(screen.getByTestId('import-jsonl-modal')).toBeInTheDocument();
    expect(screen.getByText('Import into graph')).toBeInTheDocument();
  });

  it('reads an uploaded file and shows preview counts', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(
      <ImportJsonlModal
        isVisible
        setIsVisible={() => {}}
        mode="merge"
        targetGraphGuid={mockGraphGUID}
      />,
      initialState,
      undefined,
      true
    );

    uploadFile();

    const preview = await screen.findByTestId('import-jsonl-preview');
    expect(within(preview).getByText('graph.jsonl')).toBeInTheDocument();
    // 2 nodes, 1 edge, 1 header line.
    expect(within(preview).getByText('2')).toBeInTheDocument();
    expect(within(preview).getAllByText('1').length).toBeGreaterThanOrEqual(2);
  });

  it('submits and renders the import result', async () => {
    importExport.importGraphJsonl.mockResolvedValue(mockResult);
    const onSuccess = jest.fn();
    const initialState = createMockInitialState();
    renderWithRedux(
      <ImportJsonlModal
        isVisible
        setIsVisible={() => {}}
        mode="merge"
        targetGraphGuid={mockGraphGUID}
        onSuccess={onSuccess}
      />,
      initialState,
      undefined,
      true
    );

    uploadFile();
    await screen.findByTestId('import-jsonl-preview');

    const importButton = screen.getByRole('button', { name: 'Import' });
    fireEvent.click(importButton);

    const result = await screen.findByTestId('import-jsonl-result');
    expect(within(result).getByText('2')).toBeInTheDocument();
    expect(importExport.importGraphJsonl).toHaveBeenCalledWith(
      initialState.liteGraph.tenant?.GUID,
      mockGraphGUID,
      SAMPLE_JSONL,
      { guidStrategy: 'regenerate', onError: 'abort' }
    );
    await waitFor(() => expect(toast.success).toHaveBeenCalled());
    expect(onSuccess).toHaveBeenCalled();
  });

  it('shows an error toast when the import fails', async () => {
    importExport.importGraphJsonl.mockRejectedValue(new Error('server exploded'));
    const initialState = createMockInitialState();
    renderWithRedux(
      <ImportJsonlModal
        isVisible
        setIsVisible={() => {}}
        mode="merge"
        targetGraphGuid={mockGraphGUID}
      />,
      initialState,
      undefined,
      true
    );

    uploadFile();
    await screen.findByTestId('import-jsonl-preview');

    fireEvent.click(screen.getByRole('button', { name: 'Import' }));

    await waitFor(() => expect(toast.error).toHaveBeenCalled());
    expect(screen.queryByTestId('import-jsonl-result')).not.toBeInTheDocument();
  });
});
