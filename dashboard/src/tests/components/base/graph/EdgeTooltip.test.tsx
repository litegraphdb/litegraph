import '@testing-library/jest-dom';
import React from 'react';
import { fireEvent, screen } from '@testing-library/react';
import EdgeToolTip from '@/components/base/graph/EdgeTooltip';
import { defaultEdgeTooltip } from '@/components/base/graph/constant';
import { GraphEdgeTooltip } from '@/components/base/graph/types';
import { createMockInitialState } from '../../../store/mockStore';
import { renderWithRedux } from '../../../store/utils';

// Mock the API hooks
jest.mock('@/lib/store/slice/slice', () => ({
  useGetEdgeByIdQuery: jest.fn(),
  useGetManyNodesQuery: jest.fn(),
  useGetGraphGexfContentQuery: jest.fn(),
  useDeleteEdgeMutation: jest.fn(),
  useCreateEdgeMutation: jest.fn(),
  useUpdateEdgeMutation: jest.fn(),
  useGetGraphByIdQuery: jest.fn(),
  useGetNodeByIdQuery: jest.fn(),
  useSearchNodesMutation: jest.fn(),
}));

jest.mock('@/page/edges/components/AddEditEdge', () => {
  return function MockAddEditEdge() {
    return null;
  };
});

jest.mock('@/page/edges/components/DeleteEdge', () => {
  return function MockDeleteEdge() {
    return null;
  };
});

// Mock the JsonEditor component
jest.mock('jsoneditor-react', () => ({
  JsonEditor: ({ value }: { value: any }) => (
    <div data-testid="json-editor">{JSON.stringify(value)}</div>
  ),
}));

describe('EdgeTooltip Component', () => {
  const mockSetTooltip = jest.fn();
  const mockTooltip: GraphEdgeTooltip = {
    visible: true,
    edgeId: 'test-edge-id',
    x: 100,
    y: 200,
  };

  const mockEdge = {
    GUID: 'test-edge-id',
    Name: 'Test Edge',
    From: 'node-1',
    To: 'node-2',
    Cost: 5,
    Labels: ['label1', 'label2'],
    Tags: { category: 'test', priority: 'high' },
    Data: { key: 'value' },
  };

  const mockNodes = [
    { GUID: 'node-1', Name: 'Node 1' },
    { GUID: 'node-2', Name: 'Node 2' },
  ];

  beforeEach(() => {
    jest.clearAllMocks();

    // Mock all the RTK Query hooks used by child components
    const {
      useDeleteEdgeMutation,
      useCreateEdgeMutation,
      useUpdateEdgeMutation,
      useGetGraphByIdQuery,
      useGetNodeByIdQuery,
      useSearchNodesMutation,
    } = require('@/lib/store/slice/slice');

    useDeleteEdgeMutation.mockReturnValue([jest.fn(), { isLoading: false }]);

    useCreateEdgeMutation.mockReturnValue([jest.fn(), { isLoading: false }]);

    useUpdateEdgeMutation.mockReturnValue([jest.fn(), { isLoading: false }]);

    useGetGraphByIdQuery.mockReturnValue({
      data: { Name: 'Test Graph' },
      isLoading: false,
      isFetching: false,
      error: null,
    });

    useGetNodeByIdQuery.mockReturnValue({
      data: null,
      isLoading: false,
      isFetching: false,
      error: null,
      refetch: jest.fn(),
    });

    useSearchNodesMutation.mockReturnValue([jest.fn(), { isLoading: false }]);
  });

  it('renders error state when API fails', () => {
    const { useGetEdgeByIdQuery, useGetManyNodesQuery } = require('@/lib/store/slice/slice');
    useGetEdgeByIdQuery.mockReturnValue({
      data: null,
      isLoading: false,
      isFetching: false,
      error: { message: 'API Error' },
      refetch: jest.fn(),
    });
    useGetManyNodesQuery.mockReturnValue({
      data: null,
      isLoading: false,
      isFetching: false,
      refetch: jest.fn(),
    });

    renderWithRedux(
      <EdgeToolTip tooltip={mockTooltip} setTooltip={mockSetTooltip} graphId="test-graph" />,
      createMockInitialState()
    );

    expect(screen.getByText('Something went wrong.')).toBeInTheDocument();
  });

  it('displays tags in JsonEditor when tags exist', () => {
    const { useGetEdgeByIdQuery, useGetManyNodesQuery } = require('@/lib/store/slice/slice');
    useGetEdgeByIdQuery.mockReturnValue({
      data: mockEdge,
      isLoading: false,
      isFetching: false,
      error: null,
      refetch: jest.fn(),
    });
    useGetManyNodesQuery.mockReturnValue({
      data: mockNodes,
      isLoading: false,
      isFetching: false,
      refetch: jest.fn(),
    });

    renderWithRedux(
      <EdgeToolTip tooltip={mockTooltip} setTooltip={mockSetTooltip} graphId="test-graph" />,
      createMockInitialState()
    );

    expect(screen.getByTestId('json-editor')).toBeInTheDocument();
    expect(screen.getByText('{"category":"test","priority":"high"}')).toBeInTheDocument();
  });

  it('displays edge GUID value in a fixed-width font', () => {
    const { useGetEdgeByIdQuery, useGetManyNodesQuery } = require('@/lib/store/slice/slice');
    useGetEdgeByIdQuery.mockReturnValue({
      data: mockEdge,
      isLoading: false,
      isFetching: false,
      error: null,
      refetch: jest.fn(),
    });
    useGetManyNodesQuery.mockReturnValue({
      data: mockNodes,
      isLoading: false,
      isFetching: false,
      refetch: jest.fn(),
    });

    renderWithRedux(
      <EdgeToolTip tooltip={mockTooltip} setTooltip={mockSetTooltip} graphId="test-graph" />,
      createMockInitialState()
    );

    expect(screen.getByTestId('edge-guid')).toBeInTheDocument();
    const guidValue = screen.getByTestId('edge-guid-value');
    expect(guidValue).toHaveTextContent('test-edge-id');
    expect(guidValue.style.fontFamily).toBe('monospace');
  });

  it('does not close when clicking inside the edge information modal', () => {
    const { useGetEdgeByIdQuery, useGetManyNodesQuery } = require('@/lib/store/slice/slice');
    useGetEdgeByIdQuery.mockReturnValue({
      data: mockEdge,
      isLoading: false,
      isFetching: false,
      error: null,
      refetch: jest.fn(),
    });
    useGetManyNodesQuery.mockReturnValue({
      data: mockNodes,
      isLoading: false,
      isFetching: false,
      refetch: jest.fn(),
    });

    renderWithRedux(
      <EdgeToolTip tooltip={mockTooltip} setTooltip={mockSetTooltip} graphId="test-graph" />,
      createMockInitialState()
    );

    fireEvent.mouseDown(screen.getByText('Edge Information'));

    expect(mockSetTooltip).not.toHaveBeenCalled();
  });

  it('closes when clicking outside the edge information modal', () => {
    const { useGetEdgeByIdQuery, useGetManyNodesQuery } = require('@/lib/store/slice/slice');
    useGetEdgeByIdQuery.mockReturnValue({
      data: mockEdge,
      isLoading: false,
      isFetching: false,
      error: null,
      refetch: jest.fn(),
    });
    useGetManyNodesQuery.mockReturnValue({
      data: mockNodes,
      isLoading: false,
      isFetching: false,
      refetch: jest.fn(),
    });

    renderWithRedux(
      <EdgeToolTip tooltip={mockTooltip} setTooltip={mockSetTooltip} graphId="test-graph" />,
      createMockInitialState()
    );

    fireEvent.mouseDown(document.body);

    expect(mockSetTooltip).toHaveBeenCalledWith(defaultEdgeTooltip);
  });
});
