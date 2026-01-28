import React from 'react';
import { render, screen } from '@testing-library/react';
import Graph2DViewer from '@/components/base/graph/graph-2d/Graph2DViewer';
import { useAppContext } from '@/hooks/appHooks';
import { ThemeEnum } from '@/types/types';

// Mock dependencies
jest.mock('@/hooks/appHooks');
jest.mock('@react-sigma/core', () => ({
  SigmaContainer: ({ children, className, key, style, settings, graph }: any) => {
    // Create a serializable version of settings for testing
    const serializableSettings = {
      ...settings,
      nodeReducer: typeof settings.nodeReducer === 'function' ? 'function' : settings.nodeReducer,
      edgeReducer: typeof settings.edgeReducer === 'function' ? 'function' : settings.edgeReducer,
      labelRenderer:
        typeof settings.labelRenderer === 'function' ? 'function' : settings.labelRenderer,
      hoverRenderer:
        typeof settings.hoverRenderer === 'function' ? 'function' : settings.hoverRenderer,
    };

    return (
      <div data-testid="sigma-container" className={className} style={style}>
        <div data-testid="sigma-settings">{JSON.stringify(serializableSettings)}</div>
        <div data-testid="sigma-graph-type">{graph.name}</div>
        {children}
      </div>
    );
  },
}));

jest.mock('@/components/base/graph/graph-2d/SigmaGraphLoader', () => {
  return function MockSigmaGraphLoader({
    gexfContent,
    setTooltip,
    setEdgeTooltip,
    nodeTooltip,
    edgeTooltip,
    nodes,
    edges,
  }: any) {
    return (
      <div data-testid="sigma-graph-loader">
        <div data-testid="gexf-content">{gexfContent}</div>
        <div data-testid="nodes-count">Nodes: {nodes?.length || 0}</div>
        <div data-testid="edges-count">Edges: {edges?.length || 0}</div>
        <div data-testid="node-tooltip">{JSON.stringify(nodeTooltip)}</div>
        <div data-testid="edge-tooltip">{JSON.stringify(edgeTooltip)}</div>
      </div>
    );
  };
});

jest.mock('@/utils/stringUtils', () => ({
  uuid: jest.fn(() => 'test-uuid-123'),
}));

const mockUseAppContext = useAppContext as jest.MockedFunction<typeof useAppContext>;

describe('Graph2DViewer', () => {
  const defaultProps = {
    show3d: false,
    selectedGraphRedux: 'graph-123',
    nodes: [
      {
        id: 'node1',
        label: 'Node 1',
        type: 'circle',
        x: 0,
        y: 0,
        vx: 0,
        vy: 0,
      },
      {
        id: 'node2',
        label: 'Node 2',
        type: 'square',
        x: 100,
        y: 100,
        vx: 0,
        vy: 0,
      },
    ],
    edges: [
      {
        id: 'edge1',
        source: 'node1',
        target: 'node2',
        label: 'Edge 1',
        cost: 5,
        data: '',
        sourceX: 0,
        sourceY: 0,
        targetX: 100,
        targetY: 100,
      },
    ],
    gexfContent: '<gexf>test content</gexf>',
    setTooltip: jest.fn(),
    setEdgeTooltip: jest.fn(),
    nodeTooltip: { visible: false, nodeId: '', x: 0, y: 0 },
    edgeTooltip: { visible: false, edgeId: '', x: 0, y: 0 },
    showGraphHorizontal: false,
    topologicalSortNodes: false,
    showLabel: true,
  };

  beforeEach(() => {
    mockUseAppContext.mockReturnValue({
      theme: ThemeEnum.LIGHT,
      setTheme: jest.fn(),
    });
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  it('renders with default props', () => {
    render(<Graph2DViewer {...defaultProps} />);

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();
    expect(screen.getByTestId('sigma-graph-loader')).toBeInTheDocument();
    expect(screen.getByTestId('nodes-count')).toHaveTextContent('Nodes: 2');
    expect(screen.getByTestId('edges-count')).toHaveTextContent('Edges: 1');
  });

  it('applies correct CSS class when show3d is true', () => {
    render(<Graph2DViewer {...defaultProps} show3d={true} />);

    const container = screen.getByTestId('sigma-container');
    expect(container).toHaveClass('d-none');
  });

  it('does not apply d-none class when show3d is false', () => {
    render(<Graph2DViewer {...defaultProps} show3d={false} />);

    const container = screen.getByTestId('sigma-container');
    expect(container).not.toHaveClass('d-none');
  });

  it('passes correct props to SigmaGraphLoader', () => {
    render(<Graph2DViewer {...defaultProps} />);

    // Note: The component hardcodes gexfContent to empty string
    expect(screen.getByTestId('gexf-content')).toHaveTextContent('');
    expect(screen.getByTestId('node-tooltip')).toHaveTextContent(
      JSON.stringify(defaultProps.nodeTooltip)
    );
    expect(screen.getByTestId('edge-tooltip')).toHaveTextContent(
      JSON.stringify(defaultProps.edgeTooltip)
    );
  });

  it('renders with light theme settings', () => {
    mockUseAppContext.mockReturnValue({
      theme: ThemeEnum.LIGHT,
      setTheme: jest.fn(),
    });

    render(<Graph2DViewer {...defaultProps} />);

    const settingsElement = screen.getByTestId('sigma-settings');
    const settings = JSON.parse(settingsElement.textContent || '{}');

    expect(settings.defaultNodeColor).toBe('#999');
    expect(settings.defaultEdgeColor).toBe('#999');
  });

  it('renders with dark theme settings', () => {
    mockUseAppContext.mockReturnValue({
      theme: ThemeEnum.DARK,
      setTheme: jest.fn(),
    });

    render(<Graph2DViewer {...defaultProps} />);

    const settingsElement = screen.getByTestId('sigma-settings');
    const settings = JSON.parse(settingsElement.textContent || '{}');

    expect(settings.defaultNodeColor).toBe('#999');
    expect(settings.defaultEdgeColor).toBe('#999');
  });

  it('configures label rendering when showLabel is true', () => {
    render(<Graph2DViewer {...defaultProps} showLabel={true} />);

    const settingsElement = screen.getByTestId('sigma-settings');
    const settings = JSON.parse(settingsElement.textContent || '{}');

    expect(settings.renderLabels).toBe(true);
    expect(settings.renderEdgeLabels).toBe(true);
  });

  it('configures label rendering when showLabel is false', () => {
    render(<Graph2DViewer {...defaultProps} showLabel={false} />);

    const settingsElement = screen.getByTestId('sigma-settings');
    const settings = JSON.parse(settingsElement.textContent || '{}');

    expect(settings.renderLabels).toBe(false);
    expect(settings.renderEdgeLabels).toBe(false);
  });

  it('configures edge events correctly', () => {
    render(<Graph2DViewer {...defaultProps} />);

    const settingsElement = screen.getByTestId('sigma-settings');
    const settings = JSON.parse(settingsElement.textContent || '{}');

    expect(settings.enableEdgeHoverEvents).toBe(true);
    expect(settings.enableEdgeClickEvents).toBe(true);
  });

  it('configures camera ratios correctly', () => {
    render(<Graph2DViewer {...defaultProps} />);

    const settingsElement = screen.getByTestId('sigma-settings');
    const settings = JSON.parse(settingsElement.textContent || '{}');

    expect(settings.minCameraRatio).toBe(0.1);
    expect(settings.maxCameraRatio).toBe(10);
  });

  it('configures label sizes correctly', () => {
    render(<Graph2DViewer {...defaultProps} />);

    const settingsElement = screen.getByTestId('sigma-settings');
    const settings = JSON.parse(settingsElement.textContent || '{}');

    expect(settings.labelSize).toBe(14);
    expect(settings.labelWeight).toBe('bold');
    expect(settings.edgeLabelSize).toBe(12);
  });

  it('applies node reducer function', () => {
    render(<Graph2DViewer {...defaultProps} />);

    const settingsElement = screen.getByTestId('sigma-settings');
    const settings = JSON.parse(settingsElement.textContent || '{}');

    expect(settings.nodeReducer).toBe('function');
  });

  it('applies edge reducer function', () => {
    render(<Graph2DViewer {...defaultProps} />);

    const settingsElement = screen.getByTestId('sigma-settings');
    const settings = JSON.parse(settingsElement.textContent || '{}');

    expect(settings.edgeReducer).toBe('function');
  });

  it('applies label renderer function', () => {
    render(<Graph2DViewer {...defaultProps} />);

    const settingsElement = screen.getByTestId('sigma-settings');
    const settings = JSON.parse(settingsElement.textContent || '{}');

    expect(settings.labelRenderer).toBe('function');
  });

  it('applies hover renderer function', () => {
    render(<Graph2DViewer {...defaultProps} />);

    const settingsElement = screen.getByTestId('sigma-settings');
    const settings = JSON.parse(settingsElement.textContent || '{}');

    expect(settings.hoverRenderer).toBe('function');
  });

  it('uses MultiDirectedGraph as graph type', () => {
    render(<Graph2DViewer {...defaultProps} />);

    expect(screen.getByTestId('sigma-graph-type')).toHaveTextContent('MultiDirectedGraph');
  });

  it('renders with empty nodes and edges', () => {
    render(<Graph2DViewer {...defaultProps} nodes={[]} edges={[]} />);

    expect(screen.getByTestId('nodes-count')).toHaveTextContent('Nodes: 0');
    expect(screen.getByTestId('edges-count')).toHaveTextContent('Edges: 0');
  });

  it('renders with large number of nodes and edges', () => {
    const manyNodes = Array.from({ length: 100 }, (_, i) => ({
      id: `node${i}`,
      label: `Node ${i}`,
      type: 'circle',
      x: i * 10,
      y: i * 10,
      vx: 0,
      vy: 0,
    }));

    const manyEdges = Array.from({ length: 50 }, (_, i) => ({
      id: `edge${i}`,
      source: `node${i}`,
      target: `node${i + 1}`,
      label: `Edge ${i}`,
      cost: i,
      data: '',
      sourceX: i * 10,
      sourceY: i * 10,
      targetX: (i + 1) * 10,
      targetY: (i + 1) * 10,
    }));

    render(<Graph2DViewer {...defaultProps} nodes={manyNodes} edges={manyEdges} />);

    expect(screen.getByTestId('nodes-count')).toHaveTextContent('Nodes: 100');
    expect(screen.getByTestId('edges-count')).toHaveTextContent('Edges: 50');
  });

  it('handles undefined gexfContent', () => {
    render(<Graph2DViewer {...defaultProps} gexfContent={undefined as any} />);

    // Component always passes empty string to SigmaGraphLoader
    expect(screen.getByTestId('gexf-content')).toHaveTextContent('');
  });

  it('handles empty string gexfContent', () => {
    render(<Graph2DViewer {...defaultProps} gexfContent="" />);

    // Component always passes empty string to SigmaGraphLoader
    expect(screen.getByTestId('gexf-content')).toHaveTextContent('');
  });

  it('passes correct tooltip state', () => {
    const visibleNodeTooltip = { visible: true, nodeId: 'node1', x: 100, y: 200 };
    const visibleEdgeTooltip = { visible: true, edgeId: 'edge1', x: 150, y: 250 };

    render(
      <Graph2DViewer
        {...defaultProps}
        nodeTooltip={visibleNodeTooltip}
        edgeTooltip={visibleEdgeTooltip}
      />
    );

    // Note: gexfContent is always passed as empty string by the component
    expect(screen.getByTestId('gexf-content')).toHaveTextContent('');
    expect(screen.getByTestId('node-tooltip')).toHaveTextContent(
      JSON.stringify(visibleNodeTooltip)
    );
    expect(screen.getByTestId('edge-tooltip')).toHaveTextContent(
      JSON.stringify(visibleEdgeTooltip)
    );
  });

  it('handles different showGraphHorizontal values', () => {
    const { rerender } = render(<Graph2DViewer {...defaultProps} showGraphHorizontal={false} />);

    rerender(<Graph2DViewer {...defaultProps} showGraphHorizontal={true} />);

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();
  });

  it('handles different topologicalSortNodes values', () => {
    const { rerender } = render(<Graph2DViewer {...defaultProps} topologicalSortNodes={false} />);

    rerender(<Graph2DViewer {...defaultProps} topologicalSortNodes={true} />);

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();
  });

  it('handles different showLabel values', () => {
    const { rerender } = render(<Graph2DViewer {...defaultProps} showLabel={false} />);

    rerender(<Graph2DViewer {...defaultProps} showLabel={true} />);

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();
  });

  it('handles theme changes', () => {
    const { rerender } = render(<Graph2DViewer {...defaultProps} />);

    mockUseAppContext.mockReturnValue({
      theme: ThemeEnum.DARK,
      setTheme: jest.fn(),
    });

    rerender(<Graph2DViewer {...defaultProps} />);

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();
  });

  it('handles nodes and edges changes', () => {
    const { rerender } = render(<Graph2DViewer {...defaultProps} />);

    const newNodes = [
      { id: 'node3', label: 'Node 3', type: 'triangle', x: 200, y: 200, vx: 0, vy: 0 },
    ];
    const newEdges = [
      {
        id: 'edge2',
        source: 'node2',
        target: 'node3',
        label: 'Edge 2',
        cost: 10,
        data: '',
        sourceX: 100,
        sourceY: 100,
        targetX: 200,
        targetY: 200,
      },
    ];

    rerender(<Graph2DViewer {...defaultProps} nodes={newNodes} edges={newEdges} />);

    expect(screen.getByTestId('nodes-count')).toHaveTextContent('Nodes: 1');
    expect(screen.getByTestId('edges-count')).toHaveTextContent('Edges: 1');
  });

  it('renders with showLabel enabled', () => {
    render(<Graph2DViewer {...defaultProps} showLabel={true} />);

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();
  });

  it('renders with showLabel disabled', () => {
    render(<Graph2DViewer {...defaultProps} showLabel={false} />);

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();
  });

  it('handles topological sort changes', () => {
    const { rerender } = render(<Graph2DViewer {...defaultProps} topologicalSortNodes={false} />);

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();

    // Change topological sort setting
    rerender(<Graph2DViewer {...defaultProps} topologicalSortNodes={true} />);

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();
  });

  it('handles showGraphHorizontal changes', () => {
    const { rerender } = render(<Graph2DViewer {...defaultProps} showGraphHorizontal={false} />);

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();

    // Change horizontal setting
    rerender(<Graph2DViewer {...defaultProps} showGraphHorizontal={true} />);

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();
  });

  it('handles theme changes', () => {
    const { rerender } = render(<Graph2DViewer {...defaultProps} />);

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();

    // Change theme (this would trigger useEffect)
    rerender(<Graph2DViewer {...defaultProps} />);

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();
  });

  it('handles nodes and edges changes', () => {
    const { rerender } = render(<Graph2DViewer {...defaultProps} />);

    const newNodes = [...defaultProps.nodes, { id: 'node3', label: 'Node 3', x: 300, y: 300 }];
    const newEdges = [
      ...defaultProps.edges,
      { id: 'edge2', source: 'node2', target: 'node3', label: 'Edge 2' },
    ];

    rerender(<Graph2DViewer {...defaultProps} nodes={newNodes} edges={newEdges} />);

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();
  });

  it('handles empty nodes and edges', () => {
    render(<Graph2DViewer {...defaultProps} nodes={[]} edges={[]} />);

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();
  });

  it('handles undefined nodes and edges', () => {
    render(<Graph2DViewer {...defaultProps} nodes={undefined as any} edges={undefined as any} />);

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();
  });

  it('handles missing gexfContent', () => {
    render(<Graph2DViewer {...defaultProps} gexfContent="" />);

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();
  });

  it('handles missing selectedGraphRedux', () => {
    render(<Graph2DViewer {...defaultProps} selectedGraphRedux={undefined as any} />);

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();
  });

  it('handles missing setTooltip and setEdgeTooltip', () => {
    render(
      <Graph2DViewer
        {...defaultProps}
        setTooltip={undefined as any}
        setEdgeTooltip={undefined as any}
      />
    );

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();
  });

  it('handles missing nodeTooltip and edgeTooltip', () => {
    render(
      <Graph2DViewer
        {...defaultProps}
        nodeTooltip={undefined as any}
        edgeTooltip={undefined as any}
      />
    );

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();
  });

  it('handles show3d prop changes', () => {
    const { rerender } = render(<Graph2DViewer {...defaultProps} show3d={false} />);

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();

    // Change show3d
    rerender(<Graph2DViewer {...defaultProps} show3d={true} />);

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();
  });

  it('handles all props changing simultaneously', () => {
    const { rerender } = render(
      <Graph2DViewer
        {...defaultProps}
        showLabel={false}
        topologicalSortNodes={false}
        showGraphHorizontal={false}
      />
    );

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();

    // Change all props
    rerender(
      <Graph2DViewer
        {...defaultProps}
        showLabel={true}
        topologicalSortNodes={true}
        showGraphHorizontal={true}
      />
    );

    expect(screen.getByTestId('sigma-container')).toBeInTheDocument();
  });
});
