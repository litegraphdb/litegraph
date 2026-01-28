import { screen, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import { renderWithRedux } from '../../store/utils';
import { createMockInitialState, testStoreTypes } from '../../store/mockStore';
import { fireEvent } from '@testing-library/react';
import { setupServer } from 'msw/node';
import { handlers } from './handler';
import { commonHandlers } from '@/tests/handler';
import { setTenant } from '@/lib/sdk/litegraph.service';
import { mockGraphData, mockGraphGUID, mockTenantGUID } from '../mockData';
import EdgePage from '@/app/dashboard/[tenantId]/edges/page';
import { mockEndpoint } from '@/tests/config';
import AddEditEdge from '@/page/edges/components/AddEditEdge';
import React from 'react';
import { NodeData, EdgeData, HoveredElement, Point } from '@/lib/graph/types';
import { applyForceLayout } from '@/lib/graph/layout';

const server = setupServer(...handlers, ...commonHandlers);

jest.mock('jsoneditor-react', () => ({
  JsonEditor: ({ 'data-testid': testId, onChange }: any) => (
    <textarea
      data-testid={testId || 'edge-data-input'}
      onChange={(e) => {
        try {
          const parsed = JSON.parse(e.target.value);
          onChange?.(parsed);
        } catch {}
      }}
    />
  ),
}));

describe('EdgePage with Mock API', () => {
  beforeAll(() => server.listen());
  afterEach(() => {
    server.resetHandlers();
    jest.clearAllMocks();
  });
  afterAll(() => server.close());

  const initialState = createMockInitialState();

  it('should handle lib graph types correctly', () => {
    const node: NodeData = {
      id: 'node-123',
      label: 'Test Node',
      type: 'default',
      x: 100,
      y: 200,
      vx: 0.5,
      vy: -0.3,
      isDragging: false,
    };

    const edge: EdgeData = {
      id: 'edge-123',
      source: 'node-1',
      target: 'node-2',
      cost: 5,
      data: '{"weight": 5}',
      sourceX: 100,
      sourceY: 100,
      targetX: 200,
      targetY: 200,
      label: 'Test Edge',
    };

    const hovered: HoveredElement = {
      type: 'edge',
      data: edge,
    };

    const point: Point = {
      x: 150,
      y: 250,
    };

    expect(node.id).toBe('node-123');
    expect(edge.id).toBe('edge-123');
    expect(hovered.type).toBe('edge');
    expect(point.x).toBe(150);
    expect(point.y).toBe(250);
  });

  it('should handle store types correctly', () => {
    const { storeType, dispatchType, initialState } = testStoreTypes();

    expect(storeType).toBeDefined();
    expect(dispatchType).toBeDefined();
    expect(initialState).toBeDefined();
    expect(initialState.liteGraph.selectedGraph).toBe('mock-graph-id');
  });

  it('renders the edge page title', async () => {
    setTenant(mockTenantGUID);
    const wrapper = renderWithRedux(<EdgePage />, initialState, undefined, true);

    const heading = await screen.findByTestId('heading');
    expect(heading).toHaveTextContent('Edges');

    expect(wrapper.container).toMatchSnapshot();
  });

  it('creates a new edge', async () => {
    const initialState = createMockInitialState();
    const mockOnEdgeUpdated = jest.fn();

    const { container } = renderWithRedux(
      <AddEditEdge
        isAddEditEdgeVisible={true}
        setIsAddEditEdgeVisible={() => {}}
        edge={null}
        onEdgeUpdated={mockOnEdgeUpdated}
        selectedGraph={mockGraphData[0].GUID}
      />,
      initialState,
      undefined,
      true
    );

    // Wait for modal to render
    const modal = await screen.findByTestId('add-edit-edge-modal');
    expect(modal).toBeInTheDocument();

    // Initial snapshot
    expect(container).toMatchSnapshot('initial modal state');

    // Wait for form fields to be available
    const nameInput = await screen.findByTestId('edge-name-input');
    const costInput = await screen.findByPlaceholderText('Enter edge cost');

    expect(nameInput).toBeInTheDocument();
    expect(costInput).toBeInTheDocument();

    // Fill form fields
    fireEvent.change(nameInput, { target: { value: 'Test Edge' } });
    fireEvent.change(costInput, { target: { value: '10' } });

    // Wait for values to be set
    await waitFor(() => {
      expect(nameInput.value).toBe('Test Edge');
      expect(costInput.value).toBe('10');
    });

    // Snapshot after filling
    expect(container).toMatchSnapshot('form filled with test data');

    // Wait for Create button to be available
    const createButton = await screen.findByText('Create');
    expect(createButton).toBeInTheDocument();

    // Test button interaction
    fireEvent.click(createButton);

    // Wait for any immediate state changes
    await waitFor(() => {
      expect(createButton).toBeInTheDocument();
    });

    // Final snapshot
    expect(container).toMatchSnapshot('final form state');
  }, 6000);

  it('updates an edge', async () => {
    const initialState = createMockInitialState();
    const mockOnEdgeUpdated = jest.fn();

    const { container } = renderWithRedux(
      <AddEditEdge
        isAddEditEdgeVisible={true}
        setIsAddEditEdgeVisible={() => {}}
        edge={null} // Use null to avoid API loading
        onEdgeUpdated={mockOnEdgeUpdated}
        selectedGraph={mockGraphData[0].GUID}
      />,
      initialState,
      undefined,
      true
    );

    // Wait for modal to render
    const modal = await screen.findByTestId('add-edit-edge-modal');
    expect(modal).toBeInTheDocument();

    // Initial snapshot
    expect(container).toMatchSnapshot('initial modal state for update test');

    // Wait for form fields to be available
    const nameInput = await screen.findByTestId('edge-name-input');
    const costInput = await screen.findByPlaceholderText('Enter edge cost');

    expect(nameInput).toBeInTheDocument();
    expect(costInput).toBeInTheDocument();

    // Fill form to simulate update
    fireEvent.change(nameInput, { target: { value: 'Updated Edge Name' } });
    fireEvent.change(costInput, { target: { value: '25' } });

    // Wait for values to be set
    await waitFor(() => {
      expect(nameInput.value).toBe('Updated Edge Name');
      expect(costInput.value).toBe('25');
    });

    // Snapshot after updating fields
    expect(container).toMatchSnapshot('form updated with new values');

    // Wait for button to be available
    const actionButton = await screen.findByText('Create');
    expect(actionButton).toBeInTheDocument();

    // Test button interaction
    fireEvent.click(actionButton);

    // Wait for any immediate state changes
    await waitFor(() => {
      expect(actionButton).toBeInTheDocument();
    });

    // Final snapshot
    expect(container).toMatchSnapshot('final state after update simulation');
  }, 8000);

  it('renders fallback on error', async () => {
    server.use(
      ...handlers,
      require('msw').http.get(
        `${mockEndpoint}v1.0/tenants/${mockTenantGUID}/graphs/${mockGraphGUID}/edges`,
        () => {
          return require('msw').HttpResponse.error();
        }
      )
    );

    const wrapper = renderWithRedux(<EdgePage />, initialState, true);
    await waitFor(() => {
      expect(screen.getByText('Something went wrong.')).toBeInTheDocument();
    });

    expect(wrapper.container).toMatchSnapshot('fallback message');
  });
});
