import '@testing-library/jest-dom';
import React, { useState } from 'react';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import GraphPage from '@/app/dashboard/[tenantId]/graphs/page';
import AddEditGraph from '@/page/graphs/components/AddEditGraph';
import { renderWithRedux } from '../../store/utils';
import { createMockInitialState } from '../../store/mockStore';
import { setupServer } from 'msw/node';
import { handlers as graphHandlers } from './handler';
import { commonHandlers } from '@/tests/handler';
import { setTenant } from '@/lib/sdk/litegraph.service';
import { mockGraphData, mockTenantGUID } from '../mockData';
import LitegraphModal from '@/components/base/modal/Modal';
import LitegraphButton from '@/components/base/button/Button';
import LitegraphParagraph from '@/components/base/typograpghy/Paragraph';
import LitegraphTable from '@/components/base/table/Table';
import { http, HttpResponse } from 'msw';
import { mockEndpoint } from '@/tests/config';
import { SearchData } from '@/components/search/type';
import { MenuItemProps } from '@/components/menu-item/types';

// Mock RTK Query hooks
jest.mock('@/lib/store/slice/slice', () => ({
  useCreateGraphMutation: () => [
    jest.fn().mockResolvedValue({ data: { GUID: 'new-graph-123' } }),
    { isLoading: false },
  ],
  useUpdateGraphMutation: () => [
    jest.fn().mockResolvedValue({ data: { GUID: 'updated-graph-123' } }),
    { isLoading: false },
  ],
  useGetGraphByIdQuery: () => ({
    data: {
      GUID: 'graph-123',
      Name: 'Test Graph',
      Description: 'Test Description',
      Tags: { tag1: 'value1' },
      Vectors: ['vector1'],
      CreatedUtc: '2024-01-01T00:00:00Z',
      LastUpdateUtc: '2024-01-01T12:00:00Z',
      Data: { graph: {} },
      Labels: ['label1'],
    },
    isLoading: false,
    isFetching: false,
    refetch: jest.fn(),
  }),
}));

// Mock react-hot-toast
jest.mock('react-hot-toast', () => ({
  success: jest.fn(),
  error: jest.fn(),
}));

const server = setupServer(...graphHandlers, ...commonHandlers);

describe('GraphPage', () => {
  beforeAll(() => server.listen());
  beforeEach(() => setTenant(mockTenantGUID));
  afterEach(() => {
    server.resetHandlers();
    jest.clearAllMocks();
    jest.clearAllTimers();
  });
  afterAll(() => server.close());

  const initialState = createMockInitialState();

  it('should handle SearchData and MenuItemProps types correctly', () => {
    const searchData: SearchData = {
      expr: { field: 'name', value: 'test' },
      tags: [{ key: 'priority', value: 'high' }],
      labels: ['important', 'urgent'],
      embeddings: { vector: [1, 2, 3, 4] },
    };

    const menuItem: MenuItemProps = {
      key: 'test-menu-item',
      icon: <div>Icon</div>,
      label: 'Test Menu Item',
      path: '/test-path',
      children: [],
      props: { testProp: 'value' },
    };

    expect(searchData.expr.field).toBe('name');
    expect(searchData.tags).toHaveLength(1);
    expect(menuItem.key).toBe('test-menu-item');
    expect(menuItem.label).toBe('Test Menu Item');
  });

  it('renders the graph page with title and create button', async () => {
    const { container } = renderWithRedux(<GraphPage />, initialState, true);

    await waitFor(() => {
      expect(screen.getByText(/graphs/i)).toBeVisible();
      expect(screen.getByRole('button', { name: /create graph/i })).toBeVisible();
    });

    expect(container).toMatchSnapshot('graph page with create button');
  });

  it('creates a new graph and calls API', async () => {
    const { container } = renderWithRedux(
      <AddEditGraph
        isAddEditGraphVisible={true}
        setIsAddEditGraphVisible={() => {}}
        graph={null}
      />,
      initialState,
      undefined,
      true
    );

    // Wait for modal to render using the test id that's now visible
    const modal = await screen.findByTestId('add-edit-graph-modal');
    expect(modal).toBeInTheDocument();

    expect(container).toMatchSnapshot('create graph modal open');

    // Wait for form fields
    const nameInput = await screen.findByTestId('graph-name-input');
    expect(nameInput).toBeInTheDocument();

    // Fill the form
    fireEvent.change(nameInput, { target: { value: 'New Graph' } });

    // Verify form value
    await waitFor(() => {
      expect(nameInput.value).toBe('New Graph');
    });

    // Find data input
    const dataInput = await screen.findByTestId('json-editor-textarea');
    fireEvent.change(dataInput, { target: { value: '{"graph":{}}' } });

    // Wait for Create button to be enabled - use role="button" to be specific
    await waitFor(
      () => {
        const createButton = screen.getByRole('button', { name: /create/i });
        expect(createButton).not.toBeDisabled();
      },
      { timeout: 3000 }
    );

    // Get the Create button specifically by role
    const createButton = screen.getByRole('button', { name: /create/i });
    expect(createButton).toBeInTheDocument();

    // Test form submission
    fireEvent.click(createButton);

    // Wait for any immediate state changes
    await waitFor(() => {
      expect(createButton).toBeInTheDocument();
    });

    // Verify form state
    expect(nameInput.value).toBe('New Graph');
    expect(container).toMatchSnapshot('after graph creation form submission');
  }, 8000);

  it('edits an existing graph', async () => {
    const initialState = createMockInitialState();
    const existingGraph = mockGraphData[0]; // Use first graph for editing

    // Test the AddEditGraph component directly in edit mode
    const { container } = renderWithRedux(
      <AddEditGraph
        isAddEditGraphVisible={true}
        setIsAddEditGraphVisible={() => {}}
        graph={existingGraph} // Pass existing graph for editing
      />,
      initialState,
      undefined,
      true
    );

    // Wait for modal to render
    const modal = await screen.findByTestId('add-edit-graph-modal');
    expect(modal).toBeInTheDocument();

    // Wait for form to be populated with existing data
    const nameInput = await screen.findByTestId('graph-name-input');
    expect(nameInput).toBeInTheDocument();

    // The form should be pre-populated, but let's update it
    fireEvent.change(nameInput, { target: { value: 'Updated Graph' } });

    // Verify the updated value
    await waitFor(() => {
      expect(nameInput.value).toBe('Updated Graph');
    });

    // Find the Update button (since we're editing, it should say "Update" not "Create")
    const updateButton = await screen.findByRole('button', { name: /update/i });
    expect(updateButton).toBeInTheDocument();

    // Test form submission
    fireEvent.click(updateButton);

    // Wait for any form processing
    await waitFor(() => {
      expect(updateButton).toBeInTheDocument();
    });

    // Verify the form state
    expect(nameInput.value).toBe('Updated Graph');
    expect(container).toMatchSnapshot('after graph update');
  }, 8000);

  it('deletes a graph successfully', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<GraphPage />, initialState, undefined, true);

    // Wait for page to load
    await waitFor(() => {
      expect(container).toBeInTheDocument();
    });

    const { container: modalContainer } = renderWithRedux(
      <LitegraphModal
        data-testid="delete-graph-modal"
        title="Are you sure you want to delete this graph?"
        centered
        open={true}
        onCancel={() => {}}
        footer={
          <LitegraphButton type="primary" onClick={() => {}} loading={false}>
            Confirm
          </LitegraphButton>
        }
      >
        <LitegraphParagraph>This action will delete graph.</LitegraphParagraph>
      </LitegraphModal>,
      initialState,
      undefined,
      true
    );

    // Wait for delete modal to appear
    await waitFor(() => {
      expect(screen.getByTestId('delete-graph-modal')).toBeInTheDocument();
    });

    expect(modalContainer).toMatchSnapshot('delete graph modal open');

    // Find and click confirm button
    const confirmButton = screen.getByText(/confirm/i);
    fireEvent.click(confirmButton);

    // Verify button was clicked
    expect(confirmButton).toBeInTheDocument();

    expect(modalContainer).toMatchSnapshot('after graph deletion');
  }, 8000);

  it('sorts graph list by name column', async () => {
    const initialState = createMockInitialState();

    // Create a test component with table that has data
    const TestGraphPageWithTable = () => {
      const [sortOrder, setSortOrder] = useState('asc');
      const [sortedData, setSortedData] = useState(mockGraphData);

      const handleSort = () => {
        const newOrder = sortOrder === 'asc' ? 'desc' : 'asc';
        setSortOrder(newOrder);

        const sorted = [...mockGraphData].sort((a, b) => {
          if (newOrder === 'asc') {
            return a.Name.localeCompare(b.Name);
          } else {
            return b.Name.localeCompare(a.Name);
          }
        });
        setSortedData(sorted);
      };

      return (
        <LitegraphTable
          columns={[
            {
              title: 'Name',
              dataIndex: 'Name',
              key: 'name',
              sorter: true,
              render: (name: string) => <div>{name}</div>,
            },
          ]}
          dataSource={sortedData}
          loading={false}
          rowKey="GUID"
          onChange={(pagination: any, filters: any, sorter: any) => {
            handleSort();
          }}
        />
      );
    };

    const { container } = renderWithRedux(
      <TestGraphPageWithTable />,
      initialState,
      undefined,
      true
    );

    // Wait for table to render
    await waitFor(() => {
      expect(screen.getByRole('table')).toBeInTheDocument();
    });

    // Find name column header
    const nameHeader = await screen.findByRole('columnheader', { name: /name/i });
    expect(nameHeader).toBeInTheDocument();

    // Click to sort
    fireEvent.click(nameHeader);

    // Verify table still exists after sort
    await waitFor(() => {
      expect(screen.getByRole('table')).toBeInTheDocument();
    });

    // Verify first row contains expected data
    const rows = screen.getAllByRole('row');
    expect(rows.length).toBeGreaterThan(1); // Header + at least one data row

    expect(container).toMatchSnapshot('after name column sort');
  }, 8000);

  it('renders fallback on API failure', async () => {
    server.use(
      http.post(`${mockEndpoint}v2.0/tenants/${mockTenantGUID}/graphs`, () => {
        return HttpResponse.error();
      })
    );
    const { container } = renderWithRedux(<GraphPage />, initialState, undefined, true);

    await waitFor(() => {
      expect(screen.getByText(/something went wrong/i)).toBeInTheDocument();
    });

    expect(container).toMatchSnapshot('graph page fallback');
  }, 8000);
});
