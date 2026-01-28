import '@testing-library/jest-dom';
import React from 'react';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import NodePage from '@/app/dashboard/[tenantId]/nodes/page';
import { createMockInitialState } from '../../store/mockStore';
import { mockGraphGUID, mockNodeData } from '../mockData';
import { setupServer } from 'msw/node';
import { handlers } from './handler';
import { commonHandlers } from '@/tests/handler';
import { renderWithRedux } from '@/tests/store/utils';
import AddEditNode from '@/page/nodes/components/AddEditNode';
import DeleteNode from '@/page/nodes/components/DeleteNode';

const server = setupServer(...handlers, ...commonHandlers);

describe('NodesPage', () => {
  beforeAll(() => server.listen());
  afterEach(() => {
    server.resetHandlers();
  });
  afterAll(() => server.close());

  it('renders the nodes page', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<NodePage />, initialState, undefined, true);

    await waitFor(() => {
      expect(screen.getByText(/nodes/i)).toBeVisible();
      expect(screen.getByRole('button', { name: /create node/i })).toBeVisible();
    });
    expect(container).toMatchSnapshot('initial table state');
  });

  it('should display Create Node button', () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<NodePage />, initialState, undefined, true);

    const createButton = screen.getByRole('button', { name: /create node/i });
    expect(createButton).toBeVisible();
    expect(createButton).toMatchSnapshot();
  });

  it('should create a node and should be visible in the table', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(
      <AddEditNode
        isAddEditNodeVisible={true}
        setIsAddEditNodeVisible={() => {}}
        node={null}
        selectedGraph={mockGraphGUID}
      />,
      initialState,
      undefined,
      true
    );

    const modal = await screen.findByTestId('add-edit-node-modal');
    expect(modal).toBeInTheDocument();

    await waitFor(
      () => {
        expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
      },
      { timeout: 10000 }
    );

    const nameInput = screen.getByPlaceholderText(/enter node name/i);
    fireEvent.change(nameInput, { target: { value: mockNodeData[0].Name } });

    await waitFor(() => {
      expect(nameInput.value).toBe(mockNodeData[0].Name);
    });

    const createButton = screen.getByRole('button', { name: /create/i });
    fireEvent.click(createButton);

    expect(container).toMatchSnapshot('final table state after creation');
  });

  it('should update a node successfully', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(
      <AddEditNode
        isAddEditNodeVisible={true}
        setIsAddEditNodeVisible={() => {}}
        node={mockNodeData[0]}
        selectedGraph={mockGraphGUID}
        readonly={false}
      />,
      initialState,
      undefined,
      true
    );

    const modal = await screen.findByTestId('add-edit-node-modal');
    expect(modal).toBeInTheDocument();

    await waitFor(
      () => {
        expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
      },
      { timeout: 10000 }
    );

    const nameInput = screen.getByPlaceholderText(/enter node name/i);
    fireEvent.change(nameInput, { target: { value: 'My updated test node' } });

    await waitFor(() => {
      expect(nameInput.value).toBe('My updated test node');
    });

    const createButton = screen.getByRole('button', { name: /update/i });
    fireEvent.click(createButton);

    expect(container).toMatchSnapshot('final table state after update');
  });

  it('should delete a node successfully', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(
      <DeleteNode
        isDeleteModelVisible={true}
        setIsDeleteModelVisible={() => {}}
        selectedNode={mockNodeData[0]}
        setSelectedNode={() => {}}
        title={`Are you sure you want to delete "${mockNodeData[0].Name}" node?`}
        paragraphText={'This action will delete node.'}
      />,
      initialState,
      undefined,
      true
    );

    const modal = await screen.findByTestId('delete-node-modal');
    expect(modal).toBeInTheDocument();

    const deleteButton = screen.getByRole('button', { name: /confirm/i });
    fireEvent.click(deleteButton);

    expect(container).toMatchSnapshot('final table state after deletion');
  });
});
