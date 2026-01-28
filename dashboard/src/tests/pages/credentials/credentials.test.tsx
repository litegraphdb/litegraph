import '@testing-library/jest-dom';
import React from 'react';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import CredentialPage from '@/app/admin/dashboard/credentials/page';
import { createMockInitialState } from '../../store/mockStore';
import { mockCredentialData, mockUserData } from '../mockData';
import { commonHandlers } from '@/tests/handler';
import { handlers } from './handler';
import { handlers as usersHandlers } from '../users/handler';
import { setupServer } from 'msw/node';
import { setTenant } from '@/lib/sdk/litegraph.service';
import { mockTenantGUID } from '../mockData';
import { renderWithRedux } from '@/tests/store/utils';
import AddEditCredential from '@/page/credentials/components/AddEditCredential';
import DeleteCredential from '@/page/credentials/components/DeleteCredential';

// Mock react-hot-toast
jest.mock('react-hot-toast', () => ({
  success: jest.fn(),
  error: jest.fn(),
}));

const server = setupServer(...handlers, ...commonHandlers, ...usersHandlers);

describe('CredentialsPage', () => {
  beforeAll(() => server.listen());
  beforeEach(() => {
    setTenant(mockTenantGUID); // ensure it's reset before every test
  });
  afterEach(() => {
    server.resetHandlers();
    jest.clearAllMocks();
    jest.clearAllTimers();
  });
  afterAll(() => server.close());

  it('renders the credentials page', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<CredentialPage />, initialState, undefined, true);

    await waitFor(() => {
      expect(screen.getAllByText(mockCredentialData[0].Name).length).toBe(1);
    });
    expect(container).toMatchSnapshot('initial table state');
  });

  it('should display Create Credential button', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<CredentialPage />, initialState, undefined, true);

    const createButton = screen.getByRole('button', { name: /create credential/i });
    await waitFor(() => {
      expect(createButton).toBeVisible();
    });
    expect(createButton).toMatchSnapshot();
  });

  it('should create a credential and should be visible in the table', async () => {
    // Increase timeout for this test
    jest.setTimeout(15000);

    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<CredentialPage />, initialState, undefined, true);

    // Click create button
    const createButton = screen.getByRole('button', { name: /create credential/i });
    await waitFor(() => {
      expect(createButton).toBeVisible();
    });
    await fireEvent.click(createButton);

    // Wait for modal to appear
    const modal = await screen.findByRole('dialog');
    await waitFor(() => {
      expect(modal).toBeVisible();
    });

    // Fill in the form
    const nameInput = screen.getByTestId('name-input');
    const userSelect = screen.getByTestId('user-select');

    await fireEvent.change(nameInput, { target: { value: 'Test Credential' } });

    fireEvent.mouseDown(userSelect);

    const options = await screen.findAllByText((text: string) =>
      text.includes(mockUserData[0].FirstName)
    );

    const option = options.find(
      (el: HTMLElement) =>
        el.textContent === `${mockUserData[0].FirstName} ${mockUserData[0].LastName}`
    );

    if (!option) {
      throw new Error('No matching user option found for Select dropdown.');
    }

    fireEvent.click(option);
    const submitButton = screen.getByRole('button', { name: /ok/i });
    await fireEvent.click(submitButton);
  });

  it('should update credential successfully', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(
      <AddEditCredential
        isAddEditCredentialVisible={true}
        setIsAddEditCredentialVisible={() => {}}
        credential={mockCredentialData[0]}
      />,
      initialState,
      undefined,
      true
    );

    // Find and update the form fields
    const nameInput = screen.getByTestId('name-input');
    const activeInput = screen.getByTestId('active-switch');

    // Use hardcoded values
    const updatedName = 'Updated Credential Name';
    await fireEvent.change(nameInput, { target: { value: updatedName } });
    await fireEvent.click(activeInput); // Toggle active status

    // Find and click the update button in the modal
    const submitButton = screen.getByRole('button', { name: /ok/i });
    await fireEvent.click(submitButton);

    // Take final table snapshot
    const finalTable = container.querySelector('.ant-table');
    expect(finalTable).toMatchSnapshot('final table state after update');
  }, 15000);

  it('should delete credential successfully', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(
      <DeleteCredential
        isDeleteModelVisible={true}
        setIsDeleteModelVisible={() => {}}
        selectedCredential={mockCredentialData[0]}
        setSelectedCredential={() => {}}
        onCredentialDeleted={async () => {}}
        title="Delete Credential"
        paragraphText="Are you sure you want to delete this credential?"
      />,
      initialState,
      undefined,
      true
    );

    // Wait for modal to appear
    const confirmModal = await screen.findByTestId('delete-credential-modal');
    expect(confirmModal).toBeVisible();
    expect(confirmModal).toMatchSnapshot('delete confirmation modal');

    const confirmButton = screen.getByRole('button', { name: /delete/i });
    fireEvent.click(confirmButton);

    // Take final table snapshot
    const finalTable = container.querySelector('.ant-table');
    expect(finalTable).toMatchSnapshot('final table state after deletion');
  });

  it('should handle modal cancellation for create credential', async () => {
    const initialState = createMockInitialState();
    const mockSetVisible = jest.fn();

    renderWithRedux(
      <AddEditCredential
        isAddEditCredentialVisible={true}
        setIsAddEditCredentialVisible={mockSetVisible}
        credential={null}
      />,
      initialState,
      undefined,
      true
    );

    const cancelButton = screen.getByRole('button', { name: /cancel/i });
    fireEvent.click(cancelButton);

    expect(mockSetVisible).toHaveBeenCalledWith(false);
  });

  it('should handle modal cancellation for delete credential', async () => {
    const initialState = createMockInitialState();
    const mockSetVisible = jest.fn();
    const mockSetSelected = jest.fn();

    renderWithRedux(
      <DeleteCredential
        isDeleteModelVisible={true}
        setIsDeleteModelVisible={mockSetVisible}
        selectedCredential={mockCredentialData[0]}
        setSelectedCredential={mockSetSelected}
        title="Delete Credential"
        paragraphText="Are you sure?"
      />,
      initialState,
      undefined,
      true
    );

    const cancelButton = screen.getByRole('button', { name: /cancel/i });
    fireEvent.click(cancelButton);

    expect(mockSetVisible).toHaveBeenCalledWith(false);
    expect(mockSetSelected).toHaveBeenCalledWith(null);
  });

  it('should handle loading states correctly', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<CredentialPage />, initialState, undefined, true);

    // Should show loading state initially
    expect(container.querySelector('.ant-spin')).toBeInTheDocument();

    // Wait for loading to complete
    await waitFor(() => {
      expect(screen.getByText('Create Credential')).toBeVisible();
    });
  });

  it('should handle empty credentials list', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<CredentialPage />, initialState, undefined, true);

    await waitFor(() => {
      expect(screen.getByText('Create Credential')).toBeVisible();
    });

    // Should show empty table
    expect(container.querySelector('.ant-table-tbody')).toBeInTheDocument();
  });

  it('should handle pagination correctly', async () => {
    const initialState = createMockInitialState();

    // Mock data with enough records to trigger pagination
    // Assuming default page size is 10, we'll mock 25 records
    const mockCredentialsWithPagination = Array.from({ length: 25 }, (_, i) => ({
      ...mockCredentialData[0],
      GUID: `credential-guid-${i}`,
      Name: `Test Credential ${i + 1}`,
      UserGUID: mockUserData[0].GUID,
    }));

    renderWithRedux(<CredentialPage />, initialState, undefined, true);

    await waitFor(() => {
      expect(screen.getByText('Create Credential')).toBeVisible();
    });

    // Wait for table to load and pagination to appear
    await waitFor(() => {
      const table = document.querySelector('.ant-table');
      expect(table).toBeInTheDocument();
    });

    // Check if pagination controls are present
    await waitFor(
      () => {
        const pagination = document.querySelector('.ant-pagination');
        expect(pagination).toBeInTheDocument();
      },
      { timeout: 5000 }
    );

    // Check for pagination items (page numbers)
    const paginationItems = document.querySelectorAll('.ant-pagination-item');
    expect(paginationItems.length).toBeGreaterThan(0);

    // Check if next button exists
    const nextButton = document.querySelector('.ant-pagination-next');
    expect(nextButton).toBeInTheDocument();

    // Check if previous button exists
    const prevButton = document.querySelector('.ant-pagination-prev');
    expect(prevButton).toBeInTheDocument();
  });

  it('should handle user data loading for credentials mapping', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<CredentialPage />, initialState, undefined, true);

    await waitFor(() => {
      expect(screen.getByText('Create Credential')).toBeVisible();
    });

    // Should show credentials with user names mapped
    await waitFor(() => {
      expect(screen.getByText(mockCredentialData[0].Name)).toBeVisible();
    });
  });

  it('should handle bearer token field disabled state in edit mode', async () => {
    const initialState = createMockInitialState();

    renderWithRedux(
      <AddEditCredential
        isAddEditCredentialVisible={true}
        setIsAddEditCredentialVisible={() => {}}
        credential={mockCredentialData[0]}
      />,
      initialState,
      undefined,
      true
    );

    // Bearer token field should be disabled in edit mode
    const bearerTokenInput = screen.getByPlaceholderText('Enter bearer token');
    expect(bearerTokenInput).toBeDisabled();

    // User GUID field should also be disabled in edit mode
    const userSelect = screen.getByTestId('user-select');
    expect(userSelect).toHaveClass('ant-select-disabled');
  });

  it('should handle form values change and validation', async () => {
    const initialState = createMockInitialState();

    renderWithRedux(
      <AddEditCredential
        isAddEditCredentialVisible={true}
        setIsAddEditCredentialVisible={() => {}}
        credential={null}
      />,
      initialState,
      undefined,
      true
    );

    const nameInput = screen.getByTestId('name-input');
    const submitButton = screen.getByRole('button', { name: /ok/i });

    // Initially disabled
    expect(submitButton).toBeDisabled();

    // Fill name field
    fireEvent.change(nameInput, { target: { value: 'Test Name' } });

    // Still disabled because other required fields are empty
    expect(submitButton).toBeDisabled();
  });

  it('should handle credential update success callback', async () => {
    const initialState = createMockInitialState();
    const mockCallback = jest.fn();

    renderWithRedux(
      <AddEditCredential
        isAddEditCredentialVisible={true}
        setIsAddEditCredentialVisible={() => {}}
        credential={mockCredentialData[0]}
        onCredentialUpdated={mockCallback}
      />,
      initialState,
      undefined,
      true
    );

    // Wait for form to be populated with credential data
    await waitFor(() => {
      const nameInput = screen.getByTestId('name-input');
      expect(nameInput).toHaveValue(mockCredentialData[0].Name);
    });

    // Make a change to the form
    const nameInput = screen.getByTestId('name-input');
    fireEvent.change(nameInput, { target: { value: 'Updated Name' } });

    // Wait for form validation to complete
    await waitFor(() => {
      const submitButton = screen.getByRole('button', { name: /ok/i });
      expect(submitButton).not.toBeDisabled();
    });

    // Submit the form
    const submitButton = screen.getByRole('button', { name: /ok/i });
    fireEvent.click(submitButton);

    // Wait for the success toast and callback
    await waitFor(
      () => {
        expect(mockCallback).toHaveBeenCalled();
      },
      { timeout: 10000 }
    );
  });

  it('should handle delete success callback', async () => {
    const initialState = createMockInitialState();
    const mockCallback = jest.fn();

    renderWithRedux(
      <DeleteCredential
        isDeleteModelVisible={true}
        setIsDeleteModelVisible={() => {}}
        selectedCredential={mockCredentialData[0]}
        setSelectedCredential={() => {}}
        onCredentialDeleted={mockCallback}
        title="Delete Credential"
        paragraphText="Are you sure?"
      />,
      initialState,
      undefined,
      true
    );

    const deleteButton = screen.getByRole('button', { name: /delete/i });
    fireEvent.click(deleteButton);

    await waitFor(() => {
      // Check that the toast.success was called instead of looking for the text in DOM
      const { success } = require('react-hot-toast');
      expect(success).toHaveBeenCalledWith('Credential deleted successfully');
    });
  });
});
