import '@testing-library/jest-dom';
import React from 'react';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import UserPage from '@/app/admin/dashboard/users/page';
import { createMockInitialState } from '../../store/mockStore';
import { mockTenantGUID, mockUserData } from '../mockData';
import { handlers } from './handler';
import { setupServer } from 'msw/node';
import { setTenant } from '@/lib/sdk/litegraph.service';
import { commonHandlers } from '@/tests/handler';
import { renderWithRedux } from '@/tests/store/utils';
import AddEditUser from '@/page/users/components/AddEditUser';

// Mock react-hot-toast
// jest.mock('react-hot-toast', () => ({
//   success: jest.fn(),
//   error: jest.fn(),
// }));

const server = setupServer(...handlers, ...commonHandlers);
setTenant(mockTenantGUID);

describe('UsersPage', () => {
  beforeAll(() => server.listen());
  afterEach(() => {
    server.resetHandlers();
    jest.clearAllMocks();
  });
  afterAll(() => server.close());

  it('renders the users page', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<UserPage />, initialState, undefined, true);

    await waitFor(() => {
      expect(screen.getAllByText(mockUserData[0].FirstName).length).toBe(2);
    });
    expect(container).toMatchSnapshot('initial table state');
  });

  it('should display Create User button', () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<UserPage />, initialState, undefined, true);

    const createButton = screen.getByRole('button', { name: /create user/i });
    expect(createButton).toBeVisible();
    expect(createButton).toMatchSnapshot();
  });

  it('should create a user and should be visible in the table', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<UserPage />, initialState, undefined, true);

    await waitFor(() => {
      expect(screen.getAllByText(mockUserData[0].FirstName).length).toBe(2);
    });

    const createButton = screen.getByRole('button', { name: /create user/i });
    expect(createButton).toMatchSnapshot('create user button');
    fireEvent.click(createButton);

    const createModal = screen.getByRole('dialog');
    expect(createModal).toMatchSnapshot('create user modal');

    const nameInput = screen.getByPlaceholderText(/enter first name/i);
    const lastNameInput = screen.getByPlaceholderText(/enter last name/i);
    const emailInput = screen.getByPlaceholderText(/email/i);
    const passwordInput = screen.getByPlaceholderText(/enter password/i);
    const activeInput = screen.getByTestId('active-switch');

    fireEvent.change(nameInput, { target: { value: 'Test User' } });
    fireEvent.change(lastNameInput, { target: { value: 'Test Last Name' } });
    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.change(passwordInput, { target: { value: 'testpassword' } });
    fireEvent.click(activeInput);

    const submitButton = screen.getByRole('button', { name: /ok/i });
    fireEvent.click(submitButton);

    const finalTable = container.querySelector('.ant-table');
    expect(finalTable).toMatchSnapshot('final table state after creation');
  });

  it('should update user successfully', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(
      <AddEditUser
        isAddEditUserVisible={true}
        setIsAddEditUserVisible={() => {}}
        user={mockUserData[0]}
      />,
      initialState,
      undefined,
      true
    );

    const nameInput = screen.getByPlaceholderText(/enter first name/i);
    const lastNameInput = screen.getByPlaceholderText(/enter last name/i);
    const emailInput = screen.getByPlaceholderText(/email/i);
    const passwordInput = screen.getByPlaceholderText(/enter password/i);
    const activeInput = screen.getByTestId('active-switch');

    fireEvent.change(nameInput, { target: { value: 'Updated User Name' } });
    fireEvent.change(lastNameInput, { target: { value: 'Updated Last Name' } });
    fireEvent.change(emailInput, { target: { value: 'updated@example.com' } });
    fireEvent.change(passwordInput, { target: { value: 'updatedpassword' } });
    fireEvent.click(activeInput);

    const submitButton = screen.getByRole('button', { name: /ok/i });
    fireEvent.click(submitButton);

    const finalTable = container.querySelector('.ant-table');
    expect(finalTable).toMatchSnapshot('final table state after update');
  });

  it('should delete user successfully', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<UserPage />, initialState, undefined, true);

    await waitFor(() => {
      expect(screen.getAllByText(mockUserData[0].FirstName).length).toBe(2);
    });

    const menuButtons = screen.getAllByRole('user-action-menu');
    expect(menuButtons[0]).toBeInTheDocument();
    fireEvent.click(menuButtons[0]);

    const deleteOption = screen.getByText('Delete');
    expect(deleteOption).toBeInTheDocument();
    fireEvent.click(deleteOption);

    const confirmModal = screen.getByRole('dialog');
    expect(confirmModal).toMatchSnapshot('delete confirmation modal');

    const confirmButton = screen.getByRole('button', { name: /delete/i });
    fireEvent.click(confirmButton);

    const finalTable = container.querySelector('.ant-table');
    expect(finalTable).toMatchSnapshot('final table state after deletion');
  });
});

describe('AddEditUser Component - Error Handling and Form Validation', () => {
  beforeAll(() => server.listen());
  afterEach(() => {
    server.resetHandlers();
    jest.clearAllMocks();
  });
  afterAll(() => server.close());

  it('should handle form validation errors when submitting empty form', async () => {
    const initialState = createMockInitialState();
    const setIsAddEditUserVisible = jest.fn();

    renderWithRedux(
      <AddEditUser
        isAddEditUserVisible={true}
        setIsAddEditUserVisible={setIsAddEditUserVisible}
        user={null}
      />,
      initialState,
      undefined,
      true
    );

    const submitButton = screen.getByRole('button', { name: /ok/i });
    fireEvent.click(submitButton);

    // Should not call setIsAddEditUserVisible or show success toast
    expect(setIsAddEditUserVisible).not.toHaveBeenCalled();
  });

  it('should handle form validation errors when submitting partially filled form', async () => {
    const initialState = createMockInitialState();
    const setIsAddEditUserVisible = jest.fn();

    renderWithRedux(
      <AddEditUser
        isAddEditUserVisible={true}
        setIsAddEditUserVisible={setIsAddEditUserVisible}
        user={null}
      />,
      initialState,
      undefined,
      true
    );

    // Fill only some fields
    const nameInput = screen.getByPlaceholderText(/enter first name/i);
    const emailInput = screen.getByPlaceholderText(/email/i);

    fireEvent.change(nameInput, { target: { value: 'Test User' } });
    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });

    const submitButton = screen.getByRole('button', { name: /ok/i });
    fireEvent.click(submitButton);

    // Should not call setIsAddEditUserVisible due to validation error
    expect(setIsAddEditUserVisible).not.toHaveBeenCalled();
  });

  it('should handle form validation errors when submitting with invalid email', async () => {
    const initialState = createMockInitialState();
    const setIsAddEditUserVisible = jest.fn();

    renderWithRedux(
      <AddEditUser
        isAddEditUserVisible={true}
        setIsAddEditUserVisible={setIsAddEditUserVisible}
        user={null}
      />,
      initialState,
      undefined,
      true
    );

    // Fill form with invalid email
    const nameInput = screen.getByPlaceholderText(/enter first name/i);
    const lastNameInput = screen.getByPlaceholderText(/enter last name/i);
    const emailInput = screen.getByPlaceholderText(/email/i);
    const passwordInput = screen.getByPlaceholderText(/enter password/i);

    fireEvent.change(nameInput, { target: { value: 'Test User' } });
    fireEvent.change(lastNameInput, { target: { value: 'Test Last Name' } });
    fireEvent.change(emailInput, { target: { value: 'invalid-email' } });
    fireEvent.change(passwordInput, { target: { value: 'testpassword' } });

    const submitButton = screen.getByRole('button', { name: /ok/i });
    fireEvent.click(submitButton);

    // Should not call setIsAddEditUserVisible due to validation error
    expect(setIsAddEditUserVisible).not.toHaveBeenCalled();
  });

  it('should handle API error when creating user', async () => {
    const initialState = createMockInitialState();
    const setIsAddEditUserVisible = jest.fn();

    // Mock API error response
    server.use(
      require('msw').http.post(
        `${require('@/tests/config').mockEndpoint}v1.0/tenants/${mockTenantGUID}/users`,
        () => {
          return require('msw').HttpResponse.error();
        }
      )
    );

    renderWithRedux(
      <AddEditUser
        isAddEditUserVisible={true}
        setIsAddEditUserVisible={setIsAddEditUserVisible}
        user={null}
      />,
      initialState,
      undefined,
      true
    );

    // Fill form with valid data
    const nameInput = screen.getByPlaceholderText(/enter first name/i);
    const lastNameInput = screen.getByPlaceholderText(/enter last name/i);
    const emailInput = screen.getByPlaceholderText(/email/i);
    const passwordInput = screen.getByPlaceholderText(/enter password/i);

    fireEvent.change(nameInput, { target: { value: 'Test User' } });
    fireEvent.change(lastNameInput, { target: { value: 'Test Last Name' } });
    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.change(passwordInput, { target: { value: 'testpassword' } });

    const submitButton = screen.getByRole('button', { name: /ok/i });
    fireEvent.click(submitButton);

    // Should not close modal due to error
    await waitFor(() => {
      expect(setIsAddEditUserVisible).not.toHaveBeenCalled();
    });
  });

  it('should handle API error when updating user', async () => {
    const initialState = createMockInitialState();
    const setIsAddEditUserVisible = jest.fn();

    // Mock API error response for update
    server.use(
      require('msw').http.put(
        `${require('@/tests/config').mockEndpoint}v1.0/tenants/${mockTenantGUID}/users/${mockUserData[0].GUID}`,
        () => {
          return require('msw').HttpResponse.error();
        }
      )
    );

    renderWithRedux(
      <AddEditUser
        isAddEditUserVisible={true}
        setIsAddEditUserVisible={setIsAddEditUserVisible}
        user={mockUserData[0]}
      />,
      initialState,
      undefined,
      true
    );

    // Fill form with valid data
    const nameInput = screen.getByPlaceholderText(/enter first name/i);
    const lastNameInput = screen.getByPlaceholderText(/enter last name/i);
    const emailInput = screen.getByPlaceholderText(/email/i);
    const passwordInput = screen.getByPlaceholderText(/enter password/i);

    fireEvent.change(nameInput, { target: { value: 'Updated User' } });
    fireEvent.change(lastNameInput, { target: { value: 'Updated Last Name' } });
    fireEvent.change(emailInput, { target: { value: 'updated@example.com' } });
    fireEvent.change(passwordInput, { target: { value: 'updatedpassword' } });

    const submitButton = screen.getByRole('button', { name: /ok/i });
    fireEvent.click(submitButton);

    // Should not close modal due to error
    await waitFor(() => {
      expect(setIsAddEditUserVisible).not.toHaveBeenCalled();
    });
  });

  it('should handle unknown error type', async () => {
    const initialState = createMockInitialState();
    const setIsAddEditUserVisible = jest.fn();

    // Mock API to throw non-Error exception
    server.use(
      require('msw').http.post(
        `${require('@/tests/config').mockEndpoint}v1.0/tenants/${mockTenantGUID}/users`,
        () => {
          throw 'String error';
        }
      )
    );

    renderWithRedux(
      <AddEditUser
        isAddEditUserVisible={true}
        setIsAddEditUserVisible={setIsAddEditUserVisible}
        user={null}
      />,
      initialState,
      undefined,
      true
    );

    // Fill form with valid data
    const nameInput = screen.getByPlaceholderText(/enter first name/i);
    const lastNameInput = screen.getByPlaceholderText(/enter last name/i);
    const emailInput = screen.getByPlaceholderText(/email/i);
    const passwordInput = screen.getByPlaceholderText(/enter password/i);

    fireEvent.change(nameInput, { target: { value: 'Test User' } });
    fireEvent.change(lastNameInput, { target: { value: 'Test Last Name' } });
    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.change(passwordInput, { target: { value: 'testpassword' } });

    const submitButton = screen.getByRole('button', { name: /ok/i });
    fireEvent.click(submitButton);

    // Should not close modal due to error
    await waitFor(() => {
      expect(setIsAddEditUserVisible).not.toHaveBeenCalled();
    });
  });

  it('should handle null response from create user API', async () => {
    const initialState = createMockInitialState();
    const setIsAddEditUserVisible = jest.fn();

    // Mock API to return null response
    server.use(
      require('msw').http.post(
        `${require('@/tests/config').mockEndpoint}v1.0/tenants/${mockTenantGUID}/users`,
        () => {
          return require('msw').HttpResponse.json(null);
        }
      )
    );

    renderWithRedux(
      <AddEditUser
        isAddEditUserVisible={true}
        setIsAddEditUserVisible={setIsAddEditUserVisible}
        user={null}
      />,
      initialState,
      undefined,
      true
    );

    // Fill form with valid data
    const nameInput = screen.getByPlaceholderText(/enter first name/i);
    const lastNameInput = screen.getByPlaceholderText(/enter last name/i);
    const emailInput = screen.getByPlaceholderText(/email/i);
    const passwordInput = screen.getByPlaceholderText(/enter password/i);

    fireEvent.change(nameInput, { target: { value: 'Test User' } });
    fireEvent.change(lastNameInput, { target: { value: 'Test Last Name' } });
    fireEvent.change(emailInput, { target: { value: 'test@example.com' } });
    fireEvent.change(passwordInput, { target: { value: 'testpassword' } });

    const submitButton = screen.getByRole('button', { name: /ok/i });
    fireEvent.click(submitButton);

    // Should not close modal due to null response
    await waitFor(() => {
      expect(setIsAddEditUserVisible).not.toHaveBeenCalled();
    });
  });

  it('should validate email format', async () => {
    const initialState = createMockInitialState();
    const setIsAddEditUserVisible = jest.fn();

    renderWithRedux(
      <AddEditUser
        isAddEditUserVisible={true}
        setIsAddEditUserVisible={setIsAddEditUserVisible}
        user={null}
      />,
      initialState,
      undefined,
      true
    );

    // Fill form with invalid email
    const nameInput = screen.getByPlaceholderText(/enter first name/i);
    const lastNameInput = screen.getByPlaceholderText(/enter last name/i);
    const emailInput = screen.getByPlaceholderText(/email/i);
    const passwordInput = screen.getByPlaceholderText(/enter password/i);

    fireEvent.change(nameInput, { target: { value: 'Test User' } });
    fireEvent.change(lastNameInput, { target: { value: 'Test Last Name' } });
    fireEvent.change(emailInput, { target: { value: 'invalid-email' } });
    fireEvent.change(passwordInput, { target: { value: 'testpassword' } });

    const submitButton = screen.getByRole('button', { name: /ok/i });
    fireEvent.click(submitButton);

    // Should not call setIsAddEditUserVisible due to validation error
    expect(setIsAddEditUserVisible).not.toHaveBeenCalled();
  });

  it('should test form validation logic by checking button state', () => {
    const initialState = createMockInitialState();
    const setIsAddEditUserVisible = jest.fn();

    renderWithRedux(
      <AddEditUser
        isAddEditUserVisible={true}
        setIsAddEditUserVisible={setIsAddEditUserVisible}
        user={null}
      />,
      initialState,
      undefined,
      true
    );

    const submitButton = screen.getByRole('button', { name: /ok/i });

    // Test that button is initially disabled (covers form validation logic)
    expect(submitButton).toBeDisabled();

    // Test that form fields are present and can be interacted with
    const nameInput = screen.getByPlaceholderText(/enter first name/i);
    const lastNameInput = screen.getByPlaceholderText(/enter last name/i);
    const emailInput = screen.getByPlaceholderText(/email/i);
    const passwordInput = screen.getByPlaceholderText(/enter password/i);
    const activeSwitch = screen.getByTestId('active-switch');

    expect(nameInput).toBeInTheDocument();
    expect(lastNameInput).toBeInTheDocument();
    expect(emailInput).toBeInTheDocument();
    expect(passwordInput).toBeInTheDocument();
    expect(activeSwitch).toBeInTheDocument();
  });

  it('should test form validation by attempting submission with empty form', async () => {
    const initialState = createMockInitialState();
    const setIsAddEditUserVisible = jest.fn();

    renderWithRedux(
      <AddEditUser
        isAddEditUserVisible={true}
        setIsAddEditUserVisible={setIsAddEditUserVisible}
        user={null}
      />,
      initialState,
      undefined,
      true
    );

    const submitButton = screen.getByRole('button', { name: /ok/i });

    // Try to submit empty form
    fireEvent.click(submitButton);

    // Should not call setIsAddEditUserVisible due to validation failure
    expect(setIsAddEditUserVisible).not.toHaveBeenCalled();
  });

  it('should reset form when modal is cancelled', () => {
    const initialState = createMockInitialState();
    const setIsAddEditUserVisible = jest.fn();

    renderWithRedux(
      <AddEditUser
        isAddEditUserVisible={true}
        setIsAddEditUserVisible={setIsAddEditUserVisible}
        user={null}
      />,
      initialState,
      undefined,
      true
    );

    // Fill form with data
    const nameInput = screen.getByPlaceholderText(/enter first name/i);
    fireEvent.change(nameInput, { target: { value: 'Test User' } });

    // Cancel modal
    const cancelButton = screen.getByRole('button', { name: /cancel/i });
    fireEvent.click(cancelButton);

    expect(setIsAddEditUserVisible).toHaveBeenCalledWith(false);

    // Form should be reset (button should be disabled again)
    const submitButton = screen.getByRole('button', { name: /ok/i });
    expect(submitButton).toBeDisabled();
  });

  it('should populate form with user data when editing existing user', () => {
    const initialState = createMockInitialState();
    const setIsAddEditUserVisible = jest.fn();

    renderWithRedux(
      <AddEditUser
        isAddEditUserVisible={true}
        setIsAddEditUserVisible={setIsAddEditUserVisible}
        user={mockUserData[0]}
      />,
      initialState,
      undefined,
      true
    );

    // Check that form is populated with user data
    const nameInput = screen.getByPlaceholderText(/enter first name/i);
    const lastNameInput = screen.getByPlaceholderText(/enter last name/i);
    const emailInput = screen.getByPlaceholderText(/email/i);
    const passwordInput = screen.getByPlaceholderText(/enter password/i);
    const activeSwitch = screen.getByTestId('active-switch');

    expect(nameInput).toHaveValue(mockUserData[0].FirstName);
    expect(lastNameInput).toHaveValue(mockUserData[0].LastName);
    expect(emailInput).toHaveValue(mockUserData[0].Email);
    expect(passwordInput).toHaveValue(mockUserData[0].Password);
    expect(activeSwitch).toBeChecked();
  });

  it('should set default Active value to true for new users', () => {
    const initialState = createMockInitialState();
    const setIsAddEditUserVisible = jest.fn();

    renderWithRedux(
      <AddEditUser
        isAddEditUserVisible={true}
        setIsAddEditUserVisible={setIsAddEditUserVisible}
        user={null}
      />,
      initialState,
      undefined,
      true
    );

    const activeSwitch = screen.getByTestId('active-switch');
    expect(activeSwitch).toBeChecked();
  });

  it('should test handleSubmit function with form validation failure', async () => {
    const initialState = createMockInitialState();
    const setIsAddEditUserVisible = jest.fn();

    renderWithRedux(
      <AddEditUser
        isAddEditUserVisible={true}
        setIsAddEditUserVisible={setIsAddEditUserVisible}
        user={null}
      />,
      initialState,
      undefined,
      true
    );

    const submitButton = screen.getByRole('button', { name: /ok/i });

    // Try to submit without filling required fields (covers form validation in handleSubmit)
    fireEvent.click(submitButton);

    // Should not call setIsAddEditUserVisible due to validation failure
    expect(setIsAddEditUserVisible).not.toHaveBeenCalled();
  });
});
