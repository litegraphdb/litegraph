import '@testing-library/jest-dom';
import React from 'react';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import TagPage from '@/app/dashboard/[tenantId]/tags/page';
import { createMockInitialState } from '../../store/mockStore';
import { mockTagData, mockGraphGUID } from '../mockData';
import { setupServer } from 'msw/node';
import { handlers } from './handler';
import { commonHandlers } from '@/tests/handler';
import { renderWithRedux } from '@/tests/store/utils';
import AddEditTag from '@/page/tags/components/AddEditTag';
import DeleteTag from '@/page/tags/components/DeleteTag';

const server = setupServer(...handlers, ...commonHandlers);

describe('TagsPage', () => {
  beforeAll(() => server.listen());
  afterEach(() => {
    server.resetHandlers();
  });
  afterAll(() => server.close());

  it('renders the tags page', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<TagPage />, initialState, undefined, true);

    await waitFor(() => {
      expect(screen.getByText(/tags/i)).toBeVisible();
      expect(screen.getByRole('button', { name: /create tag/i })).toBeVisible();
    });
    expect(container).toMatchSnapshot('initial table state');
  });

  it('should create a tag and should be visible in the table', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<TagPage />, initialState, undefined, true);

    const createButton = screen.getByRole('button', { name: /create tag/i });
    expect(createButton).toBeVisible();
    expect(createButton).toMatchSnapshot();
  });

  it('should create a tag and should be visible in the table', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(
      <AddEditTag
        isAddEditTagVisible={true}
        setIsAddEditTagVisible={() => {}}
        tag={null}
        selectedGraph={mockGraphGUID}
      />,
      initialState,
      undefined,
      true
    );

    const modal = await screen.findByTestId('add-edit-tag-modal');
    expect(modal).toBeInTheDocument();

    await waitFor(
      () => {
        expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
      },
      { timeout: 10000 }
    );

    const keyInput = screen.getByPlaceholderText(/enter tag key/i);
    fireEvent.change(keyInput, { target: { value: mockTagData.allTags[0].Key } });

    const valueInput = screen.getByPlaceholderText(/enter tag value/i);
    fireEvent.change(valueInput, { target: { value: mockTagData.allTags[0].Value } });

    const submitButton = screen.getByRole('button', { name: /ok/i });
    fireEvent.click(submitButton);

    expect(container).toMatchSnapshot('final table state after creation');
  });

  it('should update a tag successfully', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(
      <AddEditTag
        isAddEditTagVisible={true}
        setIsAddEditTagVisible={() => {}}
        tag={mockTagData.allTags[0]}
        selectedGraph={mockGraphGUID}
      />,
      initialState,
      undefined,
      true
    );

    const modal = await screen.findByTestId('add-edit-tag-modal');
    expect(modal).toBeInTheDocument();

    await waitFor(
      () => {
        expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
      },
      { timeout: 10000 }
    );

    const keyInput = screen.getByPlaceholderText(/enter tag key/i);
    fireEvent.change(keyInput, { target: { value: 'My updated test tag' } });

    const valueInput = screen.getByPlaceholderText(/enter tag value/i);
    fireEvent.change(valueInput, { target: { value: 'My updated test tag' } });

    const submitButton = screen.getByRole('button', { name: /ok/i });
    fireEvent.click(submitButton);

    expect(container).toMatchSnapshot('final table state after update');
  });

  it('should delete a tag successfully', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(
      <DeleteTag
        isDeleteModelVisible={true}
        setIsDeleteModelVisible={() => {}}
        selectedTag={mockTagData.allTags[0]}
        setSelectedTag={() => {}}
        title={`Are you sure you want to delete "${mockTagData.allTags[0].Key}" tag?`}
        paragraphText={'This action will delete tag.'}
      />,
      initialState,
      undefined,
      true
    );

    const modal = await screen.findByTestId('delete-tag-modal');
    expect(modal).toBeInTheDocument();

    const deleteButton = screen.getByRole('button', { name: /delete/i });
    fireEvent.click(deleteButton);

    expect(container).toMatchSnapshot('final table state after deletion');
  });
});
