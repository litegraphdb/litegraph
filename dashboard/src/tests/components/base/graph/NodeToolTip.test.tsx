import '@testing-library/jest-dom';
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import NodeToolTip from '@/components/base/graph/NodeToolTip';
import { GraphNodeTooltip } from '@/components/base/graph/types';
import { defaultNodeTooltip } from '@/components/base/graph/constant';
import { createMockInitialState } from '../../../store/mockStore';
import { renderWithRedux } from '../../../store/utils';

// Mock the API hooks
jest.mock('@/lib/store/slice/slice', () => ({
  useGetNodeByIdQuery: jest.fn(),
  useGetGraphGexfContentQuery: jest.fn(),
  useDeleteNodeMutation: jest.fn(),
  useCreateNodeMutation: jest.fn(),
  useUpdateNodeMutation: jest.fn(),
  useGetGraphByIdQuery: jest.fn(),
  useCreateEdgeMutation: jest.fn(),
  useUpdateEdgeMutation: jest.fn(),
  useGetEdgeByIdQuery: jest.fn(),
  useSearchNodesMutation: jest.fn(),
  useEnumerateAndSearchTagQuery: jest.fn(),
}));

// Mock the JsonEditor component
jest.mock('jsoneditor-react', () => ({
  JsonEditor: ({ value }: { value: any }) => (
    <div data-testid="json-editor">{JSON.stringify(value)}</div>
  ),
}));

describe('NodeToolTip Component', () => {
  const mockSetTooltip = jest.fn();
  const mockTooltip: GraphNodeTooltip = {
    visible: true,
    nodeId: 'test-node-id',
    x: 100,
    y: 200,
  };

  const mockNode = {
    GUID: 'test-node-id',
    Name: 'Test Node',
    Labels: ['label1', 'label2'],
    Tags: { category: 'test', priority: 'high' },
    Data: { key: 'value' },
  };

  beforeEach(() => {
    jest.clearAllMocks();

    // Mock all the RTK Query hooks used by child components
    const {
      useDeleteNodeMutation,
      useCreateNodeMutation,
      useUpdateNodeMutation,
      useGetGraphByIdQuery,
      useCreateEdgeMutation,
      useUpdateEdgeMutation,
      useGetEdgeByIdQuery,
      useSearchNodesMutation,
      useEnumerateAndSearchTagQuery,
    } = require('@/lib/store/slice/slice');

    useDeleteNodeMutation.mockReturnValue([jest.fn(), { isLoading: false }]);

    useCreateNodeMutation.mockReturnValue([jest.fn(), { isLoading: false }]);

    useUpdateNodeMutation.mockReturnValue([jest.fn(), { isLoading: false }]);

    useGetGraphByIdQuery.mockReturnValue({
      data: { Name: 'Test Graph' },
      isLoading: false,
      isFetching: false,
      error: null,
    });

    useCreateEdgeMutation.mockReturnValue([jest.fn(), { isLoading: false }]);

    useUpdateEdgeMutation.mockReturnValue([jest.fn(), { isLoading: false }]);

    useGetEdgeByIdQuery.mockReturnValue({
      data: null,
      isLoading: false,
      isFetching: false,
      error: null,
    });

    useSearchNodesMutation.mockReturnValue([jest.fn(), { isLoading: false }]);

    useEnumerateAndSearchTagQuery.mockReturnValue({
      data: { Objects: [] },
      isLoading: false,
      isFetching: false,
      error: null,
      refetch: jest.fn(),
    });
  });

  it('renders error state when API fails', () => {
    const { useGetNodeByIdQuery } = require('@/lib/store/slice/slice');
    useGetNodeByIdQuery.mockReturnValue({
      data: null,
      isLoading: false,
      isFetching: false,
      error: { message: 'API Error' },
      refetch: jest.fn(),
    });

    renderWithRedux(
      <NodeToolTip tooltip={mockTooltip} setTooltip={mockSetTooltip} graphId="test-graph" />,
      createMockInitialState()
    );

    expect(screen.getByText('Something went wrong.')).toBeInTheDocument();
  });

  it('displays tags in JsonEditor when tags exist', () => {
    const {
      useGetNodeByIdQuery,
      useEnumerateAndSearchTagQuery,
    } = require('@/lib/store/slice/slice');
    useGetNodeByIdQuery.mockReturnValue({
      data: mockNode,
      isLoading: false,
      isFetching: false,
      error: null,
      refetch: jest.fn(),
    });

    useEnumerateAndSearchTagQuery.mockReturnValue({
      data: {
        Objects: [
          { NodeGUID: 'test-node-id', Key: 'category', Value: 'test' },
          { NodeGUID: 'test-node-id', Key: 'priority', Value: 'high' },
        ],
      },
      isLoading: false,
      isFetching: false,
      error: null,
      refetch: jest.fn(),
    });

    renderWithRedux(
      <NodeToolTip tooltip={mockTooltip} setTooltip={mockSetTooltip} graphId="test-graph" />,
      createMockInitialState()
    );

    expect(screen.getByTestId('json-editor')).toBeInTheDocument();
    expect(screen.getByText('{"category":"test","priority":"high"}')).toBeInTheDocument();
  });

  it('displays node GUID in gray text', () => {
    const {
      useGetNodeByIdQuery,
      useEnumerateAndSearchTagQuery,
    } = require('@/lib/store/slice/slice');
    useGetNodeByIdQuery.mockReturnValue({
      data: mockNode,
      isLoading: false,
      isFetching: false,
      error: null,
      refetch: jest.fn(),
    });

    useEnumerateAndSearchTagQuery.mockReturnValue({
      data: {
        Objects: [
          { NodeGUID: 'test-node-id', Key: 'category', Value: 'test' },
          { NodeGUID: 'test-node-id', Key: 'priority', Value: 'high' },
        ],
      },
      isLoading: false,
      isFetching: false,
      error: null,
      refetch: jest.fn(),
    });

    renderWithRedux(
      <NodeToolTip tooltip={mockTooltip} setTooltip={mockSetTooltip} graphId="test-graph" />,
      createMockInitialState()
    );

    expect(screen.getByTestId('node-guid')).toBeInTheDocument();
  });

  it('handles complex tag objects', () => {
    const nodeWithComplexTags = {
      ...mockNode,
      Tags: {
        simple: 'value',
        nested: { inner: 'data' },
        array: [1, 2, 3],
        boolean: true,
        number: 42,
      },
    };

    const {
      useGetNodeByIdQuery,
      useEnumerateAndSearchTagQuery,
    } = require('@/lib/store/slice/slice');
    useGetNodeByIdQuery.mockReturnValue({
      data: nodeWithComplexTags,
      isLoading: false,
      isFetching: false,
      error: null,
      refetch: jest.fn(),
    });

    useEnumerateAndSearchTagQuery.mockReturnValue({
      data: {
        Objects: [
          { NodeGUID: 'test-node-id', Key: 'simple', Value: 'value' },
          { NodeGUID: 'test-node-id', Key: 'nested', Value: JSON.stringify({ inner: 'data' }) },
          { NodeGUID: 'test-node-id', Key: 'array', Value: JSON.stringify([1, 2, 3]) },
          { NodeGUID: 'test-node-id', Key: 'boolean', Value: 'true' },
          { NodeGUID: 'test-node-id', Key: 'number', Value: '42' },
        ],
      },
      isLoading: false,
      isFetching: false,
      error: null,
      refetch: jest.fn(),
    });

    renderWithRedux(
      <NodeToolTip tooltip={mockTooltip} setTooltip={mockSetTooltip} graphId="test-graph" />,
      createMockInitialState()
    );

    expect(screen.getByTestId('json-editor')).toBeInTheDocument();
    const jsonEditor = screen.getByTestId('json-editor');
    const jsonText = jsonEditor.textContent || '';
    expect(jsonText).toContain('"simple":"value"');
    expect(jsonText).toContain('"nested"');
    expect(jsonText).toContain('"array":"[1,2,3]"');
    expect(jsonText).toContain('"boolean":"true"');
    expect(jsonText).toContain('"number":"42"');
  });
});
