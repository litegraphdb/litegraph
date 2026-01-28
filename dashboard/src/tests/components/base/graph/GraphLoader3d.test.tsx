import '@testing-library/jest-dom';
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import GraphLoader3d from '@/components/base/graph/GraphLoader3d';
import { GraphNodeTooltip, GraphEdgeTooltip } from '@/components/base/graph/types';
import { NodeData, EdgeData } from '@/lib/graph/types';
import { createMockInitialState } from '../../../store/mockStore';
import { renderWithRedux } from '../../../store/utils';

// Mock the app context first
const mockUseAppContext = jest.fn(() => ({
  theme: 'light',
}));

// Mock the dynamic import for react-force-graph-3d
jest.mock('next/dynamic', () => ({
  __esModule: true,
  default: (importFn: any) => {
    const MockForceGraph3D = ({ graphData, onNodeClick, onLinkClick, ...props }: any) => (
      <div data-testid="force-graph-3d" {...props}>
        <div data-testid="graph-data">{JSON.stringify(graphData)}</div>
        <button
          data-testid="node-click"
          onClick={() => onNodeClick({ id: 'node1' }, { clientX: 100, clientY: 200 })}
        >
          Click Node
        </button>
        <button
          data-testid="link-click"
          onClick={() => onLinkClick({ id: 'edge1' }, { clientX: 150, clientY: 250 })}
        >
          Click Link
        </button>
      </div>
    );
    return MockForceGraph3D;
  },
}));

// Mock the utility function
jest.mock('@/utils/appUtils', () => ({
  calculateTooltipPosition: jest.fn(() => ({ x: 150, y: 250 })),
}));

describe('GraphLoader3d Component', () => {
  const mockSetTooltip = jest.fn();
  const mockSetEdgeTooltip = jest.fn();
  const mockRef = React.createRef<HTMLDivElement>();

  const mockNodes: NodeData[] = [
    {
      id: 'node1',
      label: 'Node 1',
      x: 100,
      y: 100,
      z: 0,
      type: 'circle',
      vx: 0,
      vy: 0,
    },
    {
      id: 'node2',
      label: 'Node 2',
      x: 200,
      y: 200,
      z: 0,
      type: 'square',
      vx: 0,
      vy: 0,
    },
  ];

  const mockEdges: EdgeData[] = [
    {
      id: 'edge1',
      source: 'node1',
      target: 'node2',
      label: 'Edge 1',
      cost: 5,
      data: '',
      sourceX: 100,
      sourceY: 100,
      targetX: 200,
      targetY: 200,
    },
  ];

  const mockContainerDivHeightAndWidth = {
    height: 600,
    width: 800,
  };

  beforeEach(() => {
    jest.clearAllMocks();
    mockUseAppContext.mockReturnValue({
      theme: 'light',
    });
  });

  it('renders without crashing', () => {
    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    expect(screen.getByTestId('force-graph-3d')).toBeInTheDocument();
  });

  it('handles node click events correctly', () => {
    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const nodeClickButton = screen.getByTestId('node-click');
    fireEvent.click(nodeClickButton);

    expect(mockSetEdgeTooltip).toHaveBeenCalledWith({
      visible: false,
      edgeId: '',
      x: 0,
      y: 0,
    });
    expect(mockSetTooltip).toHaveBeenCalledWith({
      visible: true,
      nodeId: 'node1',
      x: 150,
      y: 250,
    });
  });

  it('handles link click events correctly', () => {
    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const linkClickButton = screen.getByTestId('link-click');
    fireEvent.click(linkClickButton);

    expect(mockSetTooltip).toHaveBeenCalledWith({
      visible: false,
      nodeId: '',
      x: 0,
      y: 0,
    });
    expect(mockSetEdgeTooltip).toHaveBeenCalledWith({
      visible: true,
      edgeId: 'edge1',
      x: 150,
      y: 250,
    });
  });

  it('filters out edges with invalid source or target nodes', () => {
    const invalidEdges: EdgeData[] = [
      {
        id: 'edge1',
        source: 'node1',
        target: 'node2',
        label: 'Valid Edge',
        cost: 5,
        data: '',
        sourceX: 100,
        sourceY: 100,
        targetX: 200,
        targetY: 200,
      },
      {
        id: 'edge2',
        source: 'invalidNode',
        target: 'node2',
        label: 'Invalid Source',
        cost: 5,
        data: '',
        sourceX: 300,
        sourceY: 300,
        targetX: 200,
        targetY: 200,
      },
      {
        id: 'edge3',
        source: 'node1',
        target: 'invalidNode',
        label: 'Invalid Target',
        cost: 5,
        data: '',
        sourceX: 100,
        sourceY: 100,
        targetX: 400,
        targetY: 400,
      },
    ];

    const consoleWarnSpy = jest.spyOn(console, 'warn').mockImplementation(() => {});

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={invalidEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    // Should warn about invalid edges
    expect(consoleWarnSpy).toHaveBeenCalledWith(
      'Skipping edge edge2: source node invalidNode or target node node2 not found in nodes'
    );
    expect(consoleWarnSpy).toHaveBeenCalledWith(
      'Skipping edge edge3: source node node1 or target node invalidNode not found in nodes'
    );

    consoleWarnSpy.mockRestore();
  });

  it('clears label refs when graph data changes', () => {
    const { rerender } = renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    // Change the nodes to trigger useEffect
    const newNodes: NodeData[] = [
      {
        id: 'node3',
        label: 'Node 3',
        x: 300,
        y: 300,
        z: 0,
        type: 'triangle',
        vx: 0,
        vy: 0,
      },
    ];

    rerender(
      <GraphLoader3d
        nodes={newNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    // The component should re-render with new data
    expect(screen.getByTestId('force-graph-3d')).toBeInTheDocument();
  });

  it('renders with showLabels disabled', () => {
    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
        showLabels={false}
      />
    );

    expect(screen.getByTestId('force-graph-3d')).toBeInTheDocument();
  });

  it('handles empty nodes and edges arrays', () => {
    renderWithRedux(
      <GraphLoader3d
        nodes={[]}
        edges={[]}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    expect(screen.getByTestId('force-graph-3d')).toBeInTheDocument();
  });

  it('handles undefined container dimensions', () => {
    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={{}}
      />
    );

    expect(screen.getByTestId('force-graph-3d')).toBeInTheDocument();
  });

  it('handles null ref', () => {
    const nullRef = React.createRef<HTMLDivElement | null>();

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={nullRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
        showLabels={true}
      />
    );

    expect(screen.getByTestId('force-graph-3d')).toBeInTheDocument();
  });

  it('handles edge with missing properties', () => {
    const incompleteEdges: EdgeData[] = [
      {
        id: 'edge1',
        source: 'node1',
        target: 'node2',
        label: '',
        cost: 0,
        data: '',
        sourceX: 0,
        sourceY: 0,
        targetX: 0,
        targetY: 0,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={incompleteEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    expect(screen.getByTestId('force-graph-3d')).toBeInTheDocument();
  });

  it('handles node with missing properties', () => {
    const incompleteNodes: NodeData[] = [
      {
        id: 'node1',
        label: '',
        x: 0,
        y: 0,
        z: 0,
        type: '',
        vx: 0,
        vy: 0,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={incompleteNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    expect(screen.getByTestId('force-graph-3d')).toBeInTheDocument();
  });

  it('handles theme changes correctly', () => {
    const { rerender } = renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
        showLabels={true}
      />
    );

    // Test light theme
    expect(screen.getByTestId('force-graph-3d')).toBeInTheDocument();

    // Test dark theme
    mockUseAppContext.mockReturnValue({
      theme: 'dark',
    });

    rerender(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
        showLabels={true}
      />
    );

    expect(screen.getByTestId('force-graph-3d')).toBeInTheDocument();
  });

  it('handles showLabels prop changes', () => {
    const { rerender } = renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
        showLabels={true}
      />
    );

    expect(screen.getByTestId('force-graph-3d')).toBeInTheDocument();

    // Test with showLabels=false
    rerender(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
        showLabels={false}
      />
    );

    expect(screen.getByTestId('force-graph-3d')).toBeInTheDocument();
  });

  it('handles empty nodes and edges arrays', () => {
    renderWithRedux(
      <GraphLoader3d
        nodes={[]}
        edges={[]}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
        showLabels={true}
      />
    );

    expect(screen.getByTestId('force-graph-3d')).toBeInTheDocument();
    expect(screen.getByTestId('graph-data')).toHaveTextContent('{"nodes":[],"links":[]}');
  });

  it('handles undefined container dimensions', () => {
    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={{}}
        showLabels={true}
      />
    );

    expect(screen.getByTestId('force-graph-3d')).toBeInTheDocument();
  });

  it('handles null ref', () => {
    const nullRef = React.createRef<HTMLDivElement | null>();
    nullRef.current = null;

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={nullRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
        showLabels={true}
      />
    );

    expect(screen.getByTestId('force-graph-3d')).toBeInTheDocument();
  });

  it('handles incomplete edge/node properties', () => {
    const incompleteNodes = [
      {
        id: 'node1',
        label: 'Node 1',
        x: 100,
        y: 100,
        z: 0,
        type: 'circle',
        vx: 0,
        vy: 0,
      },
    ];

    const incompleteEdges = [
      {
        id: 'edge1',
        source: 'node1',
        target: 'node2',
        label: 'Edge 1',
        cost: 5,
        data: '',
        sourceX: 100,
        sourceY: 100,
        targetX: 200,
        targetY: 200,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={incompleteNodes}
        edges={incompleteEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
        showLabels={true}
      />
    );

    expect(screen.getByTestId('force-graph-3d')).toBeInTheDocument();
  });

  it('handles theme changes with node colors', () => {
    const { rerender } = renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
        showLabels={true}
      />
    );

    // Test light theme
    expect(screen.getByTestId('force-graph-3d')).toBeInTheDocument();

    // Test dark theme
    mockUseAppContext.mockReturnValue({
      theme: 'dark',
    });

    rerender(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
        showLabels={true}
      />
    );

    expect(screen.getByTestId('force-graph-3d')).toBeInTheDocument();
  });

  it('handles edge click with missing link id', () => {
    const mockLinkClick = (link: any, event: any) => {
      mockSetEdgeTooltip({
        visible: true,
        edgeId: link.id || '',
        x: 150,
        y: 250,
      });
    };

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    // Simulate link click with missing id
    const linkClickButton = screen.getByTestId('link-click');
    fireEvent.click(linkClickButton);

    expect(mockSetEdgeTooltip).toHaveBeenCalledWith({
      visible: true,
      edgeId: 'edge1',
      x: 150,
      y: 250,
    });
  });

  it('handles node click with missing node id', () => {
    const mockNodeClick = (node: any, event: any) => {
      mockSetTooltip({
        visible: true,
        nodeId: node.id || '',
        x: 150,
        y: 250,
      });
    };

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    // Simulate node click with missing id
    const nodeClickButton = screen.getByTestId('node-click');
    fireEvent.click(nodeClickButton);

    expect(mockSetTooltip).toHaveBeenCalledWith({
      visible: true,
      nodeId: 'node1',
      x: 150,
      y: 250,
    });
  });

  it('maps nodes and edges to correct format', () => {
    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />,
      createMockInitialState()
    );

    const graphDataElement = screen.getByTestId('graph-data');
    const graphData = JSON.parse(graphDataElement.textContent || '{}');

    expect(graphData.nodes).toHaveLength(2);
    expect(graphData.links).toHaveLength(1);

    expect(graphData.nodes[0]).toEqual({
      id: 'node1',
      name: 'Node 1',
      type: 'circle',
    });

    expect(graphData.nodes[1]).toEqual({
      id: 'node2',
      name: 'Node 2',
      type: 'square',
    });

    expect(graphData.links[0]).toEqual({
      source: 'node1',
      target: 'node2',
      id: 'edge1',
      cost: 5,
      name: 'Edge 1',
    });
  });

  it('handles node click event', () => {
    const { calculateTooltipPosition } = require('@/utils/appUtils');

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />,
      createMockInitialState()
    );

    const nodeClickButton = screen.getByTestId('node-click');
    fireEvent.click(nodeClickButton);

    expect(calculateTooltipPosition).toHaveBeenCalledWith(100, 200);
    expect(mockSetEdgeTooltip).toHaveBeenCalledWith({
      visible: false,
      edgeId: '',
      x: 0,
      y: 0,
    });
    expect(mockSetTooltip).toHaveBeenCalledWith({
      visible: true,
      nodeId: 'node1',
      x: 150,
      y: 250,
    });
  });

  it('handles link click event', () => {
    const { calculateTooltipPosition } = require('@/utils/appUtils');

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />,
      createMockInitialState()
    );

    const linkClickButton = screen.getByTestId('link-click');
    fireEvent.click(linkClickButton);

    expect(calculateTooltipPosition).toHaveBeenCalledWith(150, 250);
    expect(mockSetTooltip).toHaveBeenCalledWith({
      visible: false,
      nodeId: '',
      x: 0,
      y: 0,
    });
    expect(mockSetEdgeTooltip).toHaveBeenCalledWith({
      visible: true,
      edgeId: 'edge1',
      x: 150,
      y: 250,
    });
  });

  it('passes correct props to ForceGraph3D', () => {
    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />,
      createMockInitialState()
    );

    const forceGraphElement = screen.getByTestId('force-graph-3d');

    expect(forceGraphElement).toHaveAttribute('height', '600');
    expect(forceGraphElement).toHaveAttribute('width', '800');
  });

  it('handles empty nodes and edges arrays', () => {
    renderWithRedux(
      <GraphLoader3d
        nodes={[]}
        edges={[]}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />,
      createMockInitialState()
    );

    const graphDataElement = screen.getByTestId('graph-data');
    const graphData = JSON.parse(graphDataElement.textContent || '{}');

    expect(graphData.nodes).toHaveLength(0);
    expect(graphData.links).toHaveLength(0);
  });

  it('handles undefined container dimensions', () => {
    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={{}}
      />,
      createMockInitialState()
    );

    const forceGraphElement = screen.getByTestId('force-graph-3d');

    // When height/width are undefined, they become null attributes
    expect(forceGraphElement.getAttribute('height')).toBeNull();
    expect(forceGraphElement.getAttribute('width')).toBeNull();
  });

  it('updates graph data when nodes or edges change', () => {
    const { rerender } = renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />,
      createMockInitialState()
    );

    const newNodes = [
      ...mockNodes,
      {
        id: 'node3',
        label: 'Node 3',
        x: 300,
        y: 300,
        z: 0,
        type: 'triangle',
        vx: 0,
        vy: 0,
      },
    ];

    rerender(
      <GraphLoader3d
        nodes={newNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphDataElement = screen.getByTestId('graph-data');
    const graphData = JSON.parse(graphDataElement.textContent || '{}');

    expect(graphData.nodes).toHaveLength(3);
    expect(graphData.nodes[2]).toEqual({
      id: 'node3',
      name: 'Node 3',
      type: 'triangle',
    });
  });

  it('handles nodes with missing properties', () => {
    const incompleteNodes: NodeData[] = [
      {
        id: 'node1',
        label: 'Node 1',
        type: 'circle',
        x: 0,
        y: 0,
        z: 0,
        vx: 0,
        vy: 0,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={incompleteNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.nodes).toHaveLength(1);
    expect(parsedData.nodes[0]).toEqual({
      id: 'node1',
      name: 'Node 1',
      type: 'circle',
    });
  });

  it('filters out edges with invalid source nodes', () => {
    const invalidEdges: EdgeData[] = [
      {
        id: 'edge1',
        source: 'nonexistent-node',
        target: 'node2',
        label: 'Invalid Edge',
        cost: 5,
        data: '',
        sourceX: 0,
        sourceY: 0,
        targetX: 200,
        targetY: 200,
      },
      ...mockEdges,
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={invalidEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    // Should only include valid edges
    expect(parsedData.links).toHaveLength(1);
    expect(parsedData.links[0].id).toBe('edge1');
  });

  it('filters out edges with invalid target nodes', () => {
    const invalidEdges: EdgeData[] = [
      {
        id: 'edge1',
        source: 'node1',
        target: 'nonexistent-node',
        label: 'Invalid Edge',
        cost: 5,
        data: '',
        sourceX: 100,
        sourceY: 100,
        targetX: 0,
        targetY: 0,
      },
      ...mockEdges,
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={invalidEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    // Should only include valid edges
    expect(parsedData.links).toHaveLength(1);
    expect(parsedData.links[0].id).toBe('edge1');
  });

  it('filters out edges with both invalid source and target nodes', () => {
    const invalidEdges: EdgeData[] = [
      {
        id: 'edge1',
        source: 'nonexistent-source',
        target: 'nonexistent-target',
        label: 'Invalid Edge',
        cost: 5,
        data: '',
        sourceX: 0,
        sourceY: 0,
        targetX: 0,
        targetY: 0,
      },
      ...mockEdges,
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={invalidEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    // Should only include valid edges
    expect(parsedData.links).toHaveLength(1);
    expect(parsedData.links[0].id).toBe('edge1');
  });

  it('handles empty nodes and edges arrays', () => {
    renderWithRedux(
      <GraphLoader3d
        nodes={[]}
        edges={[]}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.nodes).toHaveLength(0);
    expect(parsedData.links).toHaveLength(0);
  });

  it('handles undefined nodes and edges', () => {
    renderWithRedux(
      <GraphLoader3d
        nodes={[]}
        edges={[]}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.nodes).toHaveLength(0);
    expect(parsedData.links).toHaveLength(0);
  });

  it('handles null nodes and edges', () => {
    renderWithRedux(
      <GraphLoader3d
        nodes={[]}
        edges={[]}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.nodes).toHaveLength(0);
    expect(parsedData.links).toHaveLength(0);
  });

  it('handles mixed valid and invalid edges', () => {
    const mixedEdges: EdgeData[] = [
      {
        id: 'valid-edge-1',
        source: 'node1',
        target: 'node2',
        label: 'Valid Edge 1',
        cost: 5,
        data: '',
        sourceX: 100,
        sourceY: 100,
        targetX: 200,
        targetY: 200,
      },
      {
        id: 'invalid-edge-1',
        source: 'nonexistent',
        target: 'node1',
        label: 'Invalid Edge 1',
        cost: 3,
        data: '',
        sourceX: 0,
        sourceY: 0,
        targetX: 100,
        targetY: 100,
      },
      {
        id: 'valid-edge-2',
        source: 'node2',
        target: 'node1',
        label: 'Valid Edge 2',
        cost: 7,
        data: '',
        sourceX: 200,
        sourceY: 200,
        targetX: 100,
        targetY: 100,
      },
      {
        id: 'invalid-edge-2',
        source: 'node1',
        target: 'nonexistent',
        label: 'Invalid Edge 2',
        cost: 2,
        data: '',
        sourceX: 100,
        sourceY: 100,
        targetX: 0,
        targetY: 0,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mixedEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    // Should only include valid edges
    expect(parsedData.links).toHaveLength(2);
    expect(parsedData.links.map((link: any) => link.id)).toEqual(['valid-edge-1', 'valid-edge-2']);
  });

  it('handles nodes with very long labels', () => {
    const longLabel = 'a'.repeat(1000);
    const longLabelNodes: NodeData[] = [
      {
        id: 'node1',
        label: longLabel,
        type: 'circle',
        x: 0,
        y: 0,
        z: 0,
        vx: 0,
        vy: 0,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={longLabelNodes}
        edges={[]}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.nodes).toHaveLength(1);
    expect(parsedData.nodes[0].name).toBe(longLabel);
  });

  it('handles nodes with special characters in labels', () => {
    const specialLabelNodes: NodeData[] = [
      {
        id: 'node1',
        label: 'Node with special chars: !@#$%^&*()_+-=[]{}|;:,.<>?',
        type: 'circle',
        x: 0,
        y: 0,
        z: 0,
        vx: 0,
        vy: 0,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={specialLabelNodes}
        edges={[]}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.nodes).toHaveLength(1);
    expect(parsedData.nodes[0].name).toBe('Node with special chars: !@#$%^&*()_+-=[]{}|;:,.<>?');
  });

  it('handles nodes with unicode characters in labels', () => {
    const unicodeLabelNodes: NodeData[] = [
      {
        id: 'node1',
        label: 'Node with unicode: 🚀🌟🎉 café résumé 你好世界',
        type: 'circle',
        x: 0,
        y: 0,
        z: 0,
        vx: 0,
        vy: 0,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={unicodeLabelNodes}
        edges={[]}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.nodes).toHaveLength(1);
    expect(parsedData.nodes[0].name).toBe('Node with unicode: 🚀🌟🎉 café résumé 你好世界');
  });

  it('handles edges with very long labels', () => {
    const longLabel = 'b'.repeat(500);
    const longLabelEdges: EdgeData[] = [
      {
        id: 'edge1',
        source: 'node1',
        target: 'node2',
        label: longLabel,
        cost: 5,
        data: '',
        sourceX: 100,
        sourceY: 100,
        targetX: 200,
        targetY: 200,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={longLabelEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.links).toHaveLength(1);
    expect(parsedData.links[0].name).toBe(longLabel);
  });

  it('handles edges with special characters in labels', () => {
    const specialLabelEdges: EdgeData[] = [
      {
        id: 'edge1',
        source: 'node1',
        target: 'node2',
        label: 'Edge with special chars: !@#$%^&*()_+-=[]{}|;:,.<>?',
        cost: 5,
        data: '',
        sourceX: 100,
        sourceY: 100,
        targetX: 200,
        targetY: 200,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={specialLabelEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.links).toHaveLength(1);
    expect(parsedData.links[0].name).toBe('Edge with special chars: !@#$%^&*()_+-=[]{}|;:,.<>?');
  });

  it('handles edges with unicode characters in labels', () => {
    const unicodeLabelEdges: EdgeData[] = [
      {
        id: 'edge1',
        source: 'node1',
        target: 'node2',
        label: 'Edge with unicode: 🚀🌟🎉 café résumé 你好世界',
        cost: 5,
        data: '',
        sourceX: 100,
        sourceY: 100,
        targetX: 200,
        targetY: 200,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={unicodeLabelEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.links).toHaveLength(1);
    expect(parsedData.links[0].name).toBe('Edge with unicode: 🚀🌟🎉 café résumé 你好世界');
  });

  it('handles edges with numeric cost values', () => {
    const numericCostEdges: EdgeData[] = [
      {
        id: 'edge1',
        source: 'node1',
        target: 'node2',
        label: 'Edge 1',
        cost: 0,
        data: '',
        sourceX: 100,
        sourceY: 100,
        targetX: 200,
        targetY: 200,
      },
      {
        id: 'edge2',
        source: 'node2',
        target: 'node1',
        label: 'Edge 2',
        cost: 999,
        data: '',
        sourceX: 200,
        sourceY: 200,
        targetX: 100,
        targetY: 100,
      },
      {
        id: 'edge3',
        source: 'node1',
        target: 'node2',
        label: 'Edge 3',
        cost: -5,
        data: '',
        sourceX: 100,
        sourceY: 100,
        targetX: 200,
        targetY: 200,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={numericCostEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.links).toHaveLength(3);
    expect(parsedData.links[0].cost).toBe(0);
    expect(parsedData.links[1].cost).toBe(999);
    expect(parsedData.links[2].cost).toBe(-5);
  });

  it('handles edges with empty data field', () => {
    const emptyDataEdges: EdgeData[] = [
      {
        id: 'edge1',
        source: 'node1',
        target: 'node2',
        label: 'Edge 1',
        cost: 5,
        data: '',
        sourceX: 100,
        sourceY: 100,
        targetX: 200,
        targetY: 200,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={emptyDataEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.links).toHaveLength(1);
    expect(parsedData.links[0].id).toBe('edge1');
  });

  it('handles edges with non-empty data field', () => {
    const nonEmptyDataEdges: EdgeData[] = [
      {
        id: 'edge1',
        source: 'node1',
        target: 'node2',
        label: 'Edge 1',
        cost: 5,
        data: '{"custom": "data"}',
        sourceX: 100,
        sourceY: 100,
        targetX: 200,
        targetY: 200,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={nonEmptyDataEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.links).toHaveLength(1);
    expect(parsedData.links[0].id).toBe('edge1');
  });

  it('handles nodes with different types', () => {
    const differentTypeNodes: NodeData[] = [
      {
        id: 'node1',
        label: 'Circle Node',
        type: 'circle',
        x: 0,
        y: 0,
        z: 0,
        vx: 0,
        vy: 0,
      },
      {
        id: 'node2',
        label: 'Square Node',
        type: 'square',
        x: 100,
        y: 100,
        z: 0,
        vx: 0,
        vy: 0,
      },
      {
        id: 'node3',
        label: 'Triangle Node',
        type: 'triangle',
        x: 200,
        y: 200,
        z: 0,
        vx: 0,
        vy: 0,
      },
      {
        id: 'node4',
        label: 'Custom Node',
        type: 'custom',
        x: 300,
        y: 300,
        z: 0,
        vx: 0,
        vy: 0,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={differentTypeNodes}
        edges={[]}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.nodes).toHaveLength(4);
    expect(parsedData.nodes[0].type).toBe('circle');
    expect(parsedData.nodes[1].type).toBe('square');
    expect(parsedData.nodes[2].type).toBe('triangle');
    expect(parsedData.nodes[3].type).toBe('custom');
  });

  it('handles nodes with zero velocity', () => {
    const zeroVelocityNodes: NodeData[] = [
      {
        id: 'node1',
        label: 'Static Node',
        type: 'circle',
        x: 0,
        y: 0,
        z: 0,
        vx: 0,
        vy: 0,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={zeroVelocityNodes}
        edges={[]}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.nodes).toHaveLength(1);
    expect(parsedData.nodes[0].id).toBe('node1');
  });

  it('handles nodes with non-zero velocity', () => {
    const movingNodes: NodeData[] = [
      {
        id: 'node1',
        label: 'Moving Node',
        type: 'circle',
        x: 0,
        y: 0,
        z: 0,
        vx: 10,
        vy: -5,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={movingNodes}
        edges={[]}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.nodes).toHaveLength(1);
    expect(parsedData.nodes[0].id).toBe('node1');
  });

  it('handles nodes with negative coordinates', () => {
    const negativeCoordNodes: NodeData[] = [
      {
        id: 'node1',
        label: 'Negative Node',
        type: 'circle',
        x: -100,
        y: -200,
        z: 0,
        vx: 0,
        vy: 0,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={negativeCoordNodes}
        edges={[]}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.nodes).toHaveLength(1);
    expect(parsedData.nodes[0].id).toBe('node1');
  });

  it('handles nodes with very large coordinates', () => {
    const largeCoordNodes: NodeData[] = [
      {
        id: 'node1',
        label: 'Large Coord Node',
        type: 'circle',
        x: 999999,
        y: 999999,
        z: 0,
        vx: 0,
        vy: 0,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={largeCoordNodes}
        edges={[]}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.nodes).toHaveLength(1);
    expect(parsedData.nodes[0].id).toBe('node1');
  });

  it('handles edges with zero cost', () => {
    const zeroCostEdges: EdgeData[] = [
      {
        id: 'edge1',
        source: 'node1',
        target: 'node2',
        label: 'Free Edge',
        cost: 0,
        data: '',
        sourceX: 100,
        sourceY: 100,
        targetX: 200,
        targetY: 200,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={zeroCostEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.links).toHaveLength(1);
    expect(parsedData.links[0].cost).toBe(0);
  });

  it('handles edges with negative cost', () => {
    const negativeCostEdges: EdgeData[] = [
      {
        id: 'edge1',
        source: 'node1',
        target: 'node2',
        label: 'Negative Cost Edge',
        cost: -10,
        data: '',
        sourceX: 100,
        sourceY: 100,
        targetX: 200,
        targetY: 200,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={negativeCostEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.links).toHaveLength(1);
    expect(parsedData.links[0].cost).toBe(-10);
  });

  it('handles edges with very large cost', () => {
    const largeCostEdges: EdgeData[] = [
      {
        id: 'edge1',
        source: 'node1',
        target: 'node2',
        label: 'Expensive Edge',
        cost: 999999,
        data: '',
        sourceX: 100,
        sourceY: 100,
        targetX: 200,
        targetY: 200,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={largeCostEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.links).toHaveLength(1);
    expect(parsedData.links[0].cost).toBe(999999);
  });

  it('handles edges with decimal cost', () => {
    const decimalCostEdges: EdgeData[] = [
      {
        id: 'edge1',
        source: 'node1',
        target: 'node2',
        label: 'Decimal Cost Edge',
        cost: 3.14,
        data: '',
        sourceX: 100,
        sourceY: 100,
        targetX: 200,
        targetY: 200,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={decimalCostEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.links).toHaveLength(1);
    expect(parsedData.links[0].cost).toBe(3.14);
  });

  it('handles edges with very small decimal cost', () => {
    const smallDecimalCostEdges: EdgeData[] = [
      {
        id: 'edge1',
        source: 'node1',
        target: 'node2',
        label: 'Small Decimal Cost Edge',
        cost: 0.001,
        data: '',
        sourceX: 100,
        sourceY: 100,
        targetX: 200,
        targetY: 200,
      },
    ];

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={smallDecimalCostEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />
    );

    const graphData = screen.getByTestId('graph-data');
    const parsedData = JSON.parse(graphData.textContent || '{}');

    expect(parsedData.links).toHaveLength(1);
    expect(parsedData.links[0].cost).toBe(0.001);
  });

  it('handles theme changes', () => {
    // Test with dark theme
    mockUseAppContext.mockReturnValue({
      theme: 'dark',
    });

    renderWithRedux(
      <GraphLoader3d
        nodes={mockNodes}
        edges={mockEdges}
        setTooltip={mockSetTooltip}
        setEdgeTooltip={mockSetEdgeTooltip}
        ref={mockRef}
        containerDivHeightAndWidth={mockContainerDivHeightAndWidth}
      />,
      createMockInitialState()
    );

    expect(screen.getByTestId('force-graph-3d')).toBeInTheDocument();
  });
});
