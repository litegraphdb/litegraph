import '@testing-library/jest-dom';
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import GraphViewer from '@/components/base/graph/GraphViewer';
import { GraphNodeTooltip, GraphEdgeTooltip } from '@/components/base/graph/types';
import { createMockInitialState } from '../../../store/mockStore';
import { renderWithRedux } from '../../../store/utils';

// Mock the Sigma container
jest.mock('@react-sigma/core', () => ({
  SigmaContainer: ({ children, ...props }: any) => (
    <div data-testid="sigma-container" {...props}>
      {children}
    </div>
  ),
}));

// Mock the GraphLoader component
jest.mock('@/components/base/graph/graph-2d/SigmaGraphLoader', () => {
  return function MockGraphLoader(props: any) {
    return (
      <div
        data-testid="graph-loader"
        data-gexf-content={props.gexfContent}
        data-nodes={JSON.stringify(props.nodes)}
        data-edges={JSON.stringify(props.edges)}
        data-node-tooltip={JSON.stringify(props.nodeTooltip)}
        data-edge-tooltip={JSON.stringify(props.edgeTooltip)}
      />
    );
  };
});

// Mock the GraphLoader3d component
jest.mock('@/components/base/graph/GraphLoader3d', () => {
  return function MockGraphLoader3d(props: any) {
    return (
      <div
        data-testid="graph-loader-3d"
        data-nodes={JSON.stringify(props.nodes)}
        data-edges={JSON.stringify(props.edges)}
      />
    );
  };
});

// Mock the NodeToolTip component
jest.mock('@/components/base/graph/NodeToolTip', () => {
  return function MockNodeToolTip(props: any) {
    return <div data-testid="node-tooltip" {...props} />;
  };
});

// Mock the EdgeToolTip component
jest.mock('@/components/base/graph/EdgeTooltip', () => {
  return function MockEdgeToolTip(props: any) {
    return <div data-testid="edge-tooltip" {...props} />;
  };
});

// Mock the AddEditNode component
jest.mock('@/page/nodes/components/AddEditNode', () => {
  return function MockAddEditNode(props: any) {
    return <div data-testid="add-edit-node" {...props} />;
  };
});

// Mock the AddEditEdge component
jest.mock('@/page/edges/components/AddEditEdge', () => {
  return function MockAddEditEdge(props: any) {
    return <div data-testid="add-edit-edge" {...props} />;
  };
});

// Mock the ProgressBar component
jest.mock('@/components/base/graph/ProgressBar', () => {
  return function MockProgressBar(props: any) {
    return <div data-testid="progress-bar" {...props} />;
  };
});

// Mock the entity hooks
jest.mock('@/hooks/entityHooks', () => ({
  useLazyLoadEdgesAndNodes: jest.fn(),
}));

// Mock the Redux hooks
jest.mock('@/lib/store/hooks', () => ({
  useAppSelector: jest.fn(),
  useAppDispatch: jest.fn(),
}));

describe('GraphViewer Component', () => {
  const mockSetNodeTooltip = jest.fn();
  const mockSetEdgeTooltip = jest.fn();
  const mockSetIsAddEditNodeVisible = jest.fn();
  const mockSetIsAddEditEdgeVisible = jest.fn();

  const mockNodeTooltip: GraphNodeTooltip = {
    visible: false,
    nodeId: '',
    x: 0,
    y: 0,
  };

  const mockEdgeTooltip: GraphEdgeTooltip = {
    visible: false,
    edgeId: '',
    x: 0,
    y: 0,
  };

  const mockNodes = [
    { id: 'node1', label: 'Node 1', x: 100, y: 100, type: 'circle' },
    { id: 'node2', label: 'Node 2', x: 200, y: 200, type: 'square' },
  ];

  const mockEdges = [{ id: 'edge1', source: 'node1', target: 'node2', label: 'Edge 1', cost: 5 }];

  beforeEach(() => {
    jest.clearAllMocks();

    // Mock Redux hooks
    const { useAppSelector, useAppDispatch } = require('@/lib/store/hooks');
    useAppSelector.mockReturnValue('test-graph-id');
    useAppDispatch.mockReturnValue(jest.fn());

    // Mock entity hooks
    const { useLazyLoadEdgesAndNodes } = require('@/hooks/entityHooks');
    useLazyLoadEdgesAndNodes.mockReturnValue({
      nodes: mockNodes,
      edges: mockEdges,
      rawEdges: mockEdges,
      refetch: jest.fn(),
      isError: false,
      nodesFirstResult: { TotalRecords: 2 },
      edgesFirstResult: { TotalRecords: 1 },
      isLoading: false,
      isNodesLoading: false,
      isEdgesLoading: false,
      updateLocalNode: jest.fn(),
      addLocalNode: jest.fn(),
      removeLocalNode: jest.fn(),
      updateLocalEdge: jest.fn(),
      addLocalEdge: jest.fn(),
      removeLocalEdge: jest.fn(),
    });
  });

  it('renders without crashing', () => {
    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();
  });

  it('renders AddEditNode when selectedGraph exists', () => {
    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={true}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    expect(screen.getByTestId('add-edit-node')).toBeInTheDocument();
  });

  it('renders AddEditEdge when selectedGraph exists', () => {
    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={true}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    expect(screen.getByTestId('add-edit-edge')).toBeInTheDocument();
  });

  it('shows progress bar when loading nodes', () => {
    const { useLazyLoadEdgesAndNodes } = require('@/hooks/entityHooks');
    useLazyLoadEdgesAndNodes.mockReturnValue({
      nodes: [],
      edges: [],
      rawEdges: [],
      refetch: jest.fn(),
      isError: false,
      nodesFirstResult: { TotalRecords: 2 },
      edgesFirstResult: { TotalRecords: 1 },
      isLoading: false,
      isNodesLoading: true,
      isEdgesLoading: false,
    });

    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    expect(screen.getByTestId('progress-bar')).toBeInTheDocument();
  });

  it('shows progress bar when loading edges', () => {
    const { useLazyLoadEdgesAndNodes } = require('@/hooks/entityHooks');
    useLazyLoadEdgesAndNodes.mockReturnValue({
      nodes: mockNodes,
      edges: [],
      rawEdges: [],
      refetch: jest.fn(),
      isError: false,
      nodesFirstResult: { TotalRecords: 2 },
      edgesFirstResult: { TotalRecords: 1 },
      isLoading: false,
      isNodesLoading: false,
      isEdgesLoading: true,
    });

    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    expect(screen.getByTestId('progress-bar')).toBeInTheDocument();
  });

  it('shows error fallback when there is an error', () => {
    const { useLazyLoadEdgesAndNodes } = require('@/hooks/entityHooks');
    const mockRefetch = jest.fn();
    useLazyLoadEdgesAndNodes.mockReturnValue({
      nodes: [],
      edges: [],
      refetch: mockRefetch,
      isError: true,
      nodesFirstResult: null,
      edgesFirstResult: null,
      isLoading: false,
      isNodesLoading: false,
      isEdgesLoading: false,
    });

    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    expect(screen.getByText('Error loading graph')).toBeInTheDocument();
  });

  it('shows loading state when initially loading', () => {
    const { useLazyLoadEdgesAndNodes } = require('@/hooks/entityHooks');
    useLazyLoadEdgesAndNodes.mockReturnValue({
      nodes: [],
      edges: [],
      refetch: jest.fn(),
      isError: false,
      nodesFirstResult: null,
      edgesFirstResult: null,
      isLoading: true,
      isNodesLoading: false,
      isEdgesLoading: false,
    });

    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    expect(screen.getByTestId('loading-message')).toBeInTheDocument();
  });

  it('shows warning when no nodes exist', () => {
    const { useLazyLoadEdgesAndNodes } = require('@/hooks/entityHooks');
    useLazyLoadEdgesAndNodes.mockReturnValue({
      nodes: [],
      edges: [],
      refetch: jest.fn(),
      isError: false,
      nodesFirstResult: null,
      edgesFirstResult: null,
      isLoading: false,
      isNodesLoading: false,
      isEdgesLoading: false,
    });

    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    expect(screen.getByText('This graph has no nodes.')).toBeInTheDocument();
  });

  it('toggles 3D view when switch is clicked', () => {
    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    const switchElement = screen.getByTestId('3d-switch');
    fireEvent.click(switchElement);

    expect(screen.getByTestId('graph-loader-3d')).toBeInTheDocument();
    expect(mockSetNodeTooltip).toHaveBeenCalledWith({
      visible: false,
      nodeId: '',
      x: 0,
      y: 0,
    });
    expect(mockSetEdgeTooltip).toHaveBeenCalledWith({
      visible: false,
      edgeId: '',
      x: 0,
      y: 0,
    });
  });

  it('disables 3D switch when loading', () => {
    const { useLazyLoadEdgesAndNodes } = require('@/hooks/entityHooks');
    useLazyLoadEdgesAndNodes.mockReturnValue({
      nodes: [],
      edges: [],
      refetch: jest.fn(),
      isError: false,
      nodesFirstResult: null,
      edgesFirstResult: null,
      isLoading: false,
      isNodesLoading: true,
      isEdgesLoading: false,
    });

    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    const switchElement = screen.getByTestId('3d-switch');
    expect(switchElement).toBeDisabled();
  });

  it('shows node tooltip when visible', () => {
    const visibleNodeTooltip = {
      ...mockNodeTooltip,
      visible: true,
      nodeId: 'node1',
      x: 100,
      y: 200,
    };

    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={visibleNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    expect(screen.getByTestId('node-tooltip')).toBeInTheDocument();
  });

  it('shows edge tooltip when visible', () => {
    const visibleEdgeTooltip = {
      ...mockEdgeTooltip,
      visible: true,
      edgeId: 'edge1',
      x: 150,
      y: 250,
    };

    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={visibleEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    expect(screen.getByTestId('edge-tooltip')).toBeInTheDocument();
  });

  it('resets 3D view when selectedGraph changes', () => {
    const { rerender } = renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    // Enable 3D view
    const switchElement = screen.getByTestId('3d-switch');
    fireEvent.click(switchElement);
    expect(screen.getByTestId('graph-loader-3d')).toBeInTheDocument();

    // Change selectedGraph (this would normally happen through Redux)
    const { useAppSelector } = require('@/lib/store/hooks');
    useAppSelector.mockReturnValue('new-graph-id');

    rerender(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />
    );

    // 3D view should be reset to false
    expect(screen.queryByTestId('graph-loader-3d')).not.toBeInTheDocument();
  });

  it('handles window resize events', () => {
    const addEventListenerSpy = jest.spyOn(window, 'addEventListener');
    const removeEventListenerSpy = jest.spyOn(window, 'removeEventListener');

    const { unmount } = renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    expect(addEventListenerSpy).toHaveBeenCalledWith('resize', expect.any(Function));

    unmount();

    expect(removeEventListenerSpy).toHaveBeenCalledWith('resize', expect.any(Function));

    addEventListenerSpy.mockRestore();
    removeEventListenerSpy.mockRestore();
  });

  it('passes correct props to GraphLoader', () => {
    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    const graphLoader = screen.getByTestId('graph-loader');
    expect(graphLoader).toHaveAttribute('data-gexf-content', '');
    expect(graphLoader).toHaveAttribute('data-nodes', JSON.stringify(mockNodes));
    expect(graphLoader).toHaveAttribute('data-edges', JSON.stringify(mockEdges));
  });

  it('passes correct props to GraphLoader3d', () => {
    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    // Enable 3D view
    const switchElement = screen.getByTestId('3d-switch');
    fireEvent.click(switchElement);

    const graphLoader3d = screen.getByTestId('graph-loader-3d');
    expect(graphLoader3d).toHaveAttribute('data-nodes', JSON.stringify(mockNodes));
    expect(graphLoader3d).toHaveAttribute('data-edges', JSON.stringify(mockEdges));
  });

  it('handles retry functionality in error state', () => {
    const { useLazyLoadEdgesAndNodes } = require('@/hooks/entityHooks');
    const mockRefetch = jest.fn();
    useLazyLoadEdgesAndNodes.mockReturnValue({
      nodes: [],
      edges: [],
      refetch: mockRefetch,
      isError: true,
      nodesFirstResult: null,
      edgesFirstResult: null,
      isLoading: false,
      isNodesLoading: false,
      isEdgesLoading: false,
    });

    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    // Find and click the retry link in the fallback
    const retryLink = screen.getByText('Retry');
    fireEvent.click(retryLink);

    expect(mockRefetch).toHaveBeenCalled();
  });

  it('handles error state with different error message', () => {
    const { useLazyLoadEdgesAndNodes } = require('@/hooks/entityHooks');
    useLazyLoadEdgesAndNodes.mockReturnValue({
      nodes: [],
      edges: [],
      refetch: jest.fn(),
      isError: true,
      nodesFirstResult: null,
      edgesFirstResult: null,
      isLoading: false,
      isNodesLoading: false,
      isEdgesLoading: false,
    });

    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    expect(screen.getByText('Error loading graph')).toBeInTheDocument();
  });

  it('handles error state with null error', () => {
    const { useLazyLoadEdgesAndNodes } = require('@/hooks/entityHooks');
    useLazyLoadEdgesAndNodes.mockReturnValue({
      nodes: [],
      edges: [],
      refetch: jest.fn(),
      isError: true,
      nodesFirstResult: null,
      edgesFirstResult: null,
      isLoading: false,
      isNodesLoading: false,
      isEdgesLoading: false,
    });

    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    // The component should still show the error message even with null error
    expect(screen.getByText('Error loading graph')).toBeInTheDocument();
  });

  it('handles loading state with null results', () => {
    const { useLazyLoadEdgesAndNodes } = require('@/hooks/entityHooks');
    useLazyLoadEdgesAndNodes.mockReturnValue({
      nodes: [],
      edges: [],
      refetch: jest.fn(),
      isError: false,
      nodesFirstResult: null,
      edgesFirstResult: null,
      isLoading: true,
      isNodesLoading: false,
      isEdgesLoading: false,
    });

    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    expect(screen.getByTestId('loading-message')).toBeInTheDocument();
  });

  it('handles empty state with null results', () => {
    const { useLazyLoadEdgesAndNodes } = require('@/hooks/entityHooks');
    useLazyLoadEdgesAndNodes.mockReturnValue({
      nodes: [],
      edges: [],
      refetch: jest.fn(),
      isError: false,
      nodesFirstResult: null,
      edgesFirstResult: null,
      isLoading: false,
      isNodesLoading: false,
      isEdgesLoading: false,
    });

    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    expect(screen.getByText('This graph has no nodes.')).toBeInTheDocument();
  });

  // it('handles edge case with undefined nodes', () => {
  //   const { useLazyLoadEdgesAndNodes } = require('@/hooks/entityHooks');
  //   useLazyLoadEdgesAndNodes.mockReturnValue({
  //     nodes: undefined,
  //     edges: [],
  //     refetch: jest.fn(),
  //     isError: false,
  //     nodesFirstResult: { TotalRecords: 0 },
  //     edgesFirstResult: { TotalRecords: 0 },
  //     isLoading: false,
  //     isNodesLoading: false,
  //     isEdgesLoading: false,
  //   });

  //   renderWithRedux(
  //     <GraphViewer
  //       isAddEditNodeVisible={false}
  //       setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
  //       nodeTooltip={mockNodeTooltip}
  //       edgeTooltip={mockEdgeTooltip}
  //       setNodeTooltip={mockSetNodeTooltip}
  //       setEdgeTooltip={mockSetEdgeTooltip}
  //       isAddEditEdgeVisible={false}
  //       setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
  //     />,
  //     createMockInitialState()
  //   );

  //   expect(screen.getByText('This graph has no nodes.')).toBeInTheDocument();
  // });

  // it('handles edge case with null nodes', () => {
  //   const { useLazyLoadEdgesAndNodes } = require('@/hooks/entityHooks');
  //   useLazyLoadEdgesAndNodes.mockReturnValue({
  //     nodes: null,
  //     edges: [],
  //     refetch: jest.fn(),
  //     isError: false,
  //     nodesFirstResult: { TotalRecords: 0 },
  //     edgesFirstResult: { TotalRecords: 0 },
  //     isLoading: false,
  //     isNodesLoading: false,
  //     isEdgesLoading: false,
  //   });

  //   renderWithRedux(
  //     <GraphViewer
  //       isAddEditNodeVisible={false}
  //       setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
  //       nodeTooltip={mockNodeTooltip}
  //       edgeTooltip={mockEdgeTooltip}
  //       setNodeTooltip={mockSetNodeTooltip}
  //       setEdgeTooltip={mockSetEdgeTooltip}
  //       isAddEditEdgeVisible={false}
  //       setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
  //     />,
  //     createMockInitialState()
  //   );

  //   expect(screen.getByText('This graph has no nodes.')).toBeInTheDocument();
  // });

  it('handles edge case with undefined edges', () => {
    const { useLazyLoadEdgesAndNodes } = require('@/hooks/entityHooks');
    useLazyLoadEdgesAndNodes.mockReturnValue({
      nodes: mockNodes,
      edges: undefined,
      refetch: jest.fn(),
      isError: false,
      nodesFirstResult: { TotalRecords: 2 },
      edgesFirstResult: { TotalRecords: 0 },
      isLoading: false,
      isNodesLoading: false,
      isEdgesLoading: false,
    });

    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    // Should still render the graph with nodes but no edges
    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();
  });

  it('handles edge case with null edges', () => {
    const { useLazyLoadEdgesAndNodes } = require('@/hooks/entityHooks');
    useLazyLoadEdgesAndNodes.mockReturnValue({
      nodes: mockNodes,
      edges: null,
      refetch: jest.fn(),
      isError: false,
      nodesFirstResult: { TotalRecords: 2 },
      edgesFirstResult: { TotalRecords: 0 },
      isLoading: false,
      isNodesLoading: false,
      isEdgesLoading: false,
    });

    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    // Should still render the graph with nodes but no edges
    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();
  });

  it('handles error state with retry functionality', () => {
    const { useLazyLoadEdgesAndNodes } = require('@/hooks/entityHooks');
    const mockRefetch = jest.fn();
    useLazyLoadEdgesAndNodes.mockReturnValue({
      nodes: [],
      edges: [],
      refetch: mockRefetch,
      isError: true,
      nodesFirstResult: null,
      edgesFirstResult: null,
      isLoading: false,
      isNodesLoading: false,
      isEdgesLoading: false,
    });

    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    // Find and click the retry link in the fallback
    const retryLink = screen.getByText('Retry');
    fireEvent.click(retryLink);

    expect(mockRefetch).toHaveBeenCalled();
  });

  it('handles loading state with partial data', () => {
    const { useLazyLoadEdgesAndNodes } = require('@/hooks/entityHooks');
    useLazyLoadEdgesAndNodes.mockReturnValue({
      nodes: mockNodes,
      edges: [],
      rawEdges: [],
      refetch: jest.fn(),
      isError: false,
      nodesFirstResult: { TotalRecords: 2 },
      edgesFirstResult: { TotalRecords: 1 },
      isLoading: false,
      isNodesLoading: false,
      isEdgesLoading: true,
    });

    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    expect(screen.getByTestId('progress-bar')).toBeInTheDocument();
  });

  it('handles loading state with no data', () => {
    const { useLazyLoadEdgesAndNodes } = require('@/hooks/entityHooks');
    useLazyLoadEdgesAndNodes.mockReturnValue({
      nodes: [],
      edges: [],
      refetch: jest.fn(),
      isError: false,
      nodesFirstResult: null,
      edgesFirstResult: null,
      isLoading: true,
      isNodesLoading: false,
      isEdgesLoading: false,
    });

    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    expect(screen.getByTestId('loading-message')).toBeInTheDocument();
  });

  it('handles empty state with zero total records', () => {
    const { useLazyLoadEdgesAndNodes } = require('@/hooks/entityHooks');
    useLazyLoadEdgesAndNodes.mockReturnValue({
      nodes: [],
      edges: [],
      refetch: jest.fn(),
      isError: false,
      nodesFirstResult: { TotalRecords: 0 },
      edgesFirstResult: { TotalRecords: 0 },
      isLoading: false,
      isNodesLoading: false,
      isEdgesLoading: false,
    });

    renderWithRedux(
      <GraphViewer
        isAddEditNodeVisible={false}
        setIsAddEditNodeVisible={mockSetIsAddEditNodeVisible}
        nodeTooltip={mockNodeTooltip}
        edgeTooltip={mockEdgeTooltip}
        setNodeTooltip={mockSetNodeTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        isAddEditEdgeVisible={false}
        setIsAddEditEdgeVisible={mockSetIsAddEditEdgeVisible}
      />,
      createMockInitialState()
    );

    expect(screen.getByText('This graph has no nodes.')).toBeInTheDocument();
  });
});
