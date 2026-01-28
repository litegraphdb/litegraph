import '@testing-library/jest-dom';
import React from 'react';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import TenantPage from '@/app/admin/dashboard/tenants/page';
import { createMockInitialState } from '../../store/mockStore';
import { mockTenantData } from '../mockData';
import { handlers } from './handler';
import { setupServer } from 'msw/node';
import { setTenant } from '@/lib/sdk/litegraph.service';
import { mockTenantGUID } from '../mockData';
import { commonHandlers } from '@/tests/handler';
import { renderWithRedux } from '@/tests/store/utils';
import AddEditTenant from '@/page/tenants/components/AddEditTenant';
import DeleteTenant from '@/page/tenants/components/DeleteTenant';

const server = setupServer(...handlers, ...commonHandlers);

describe('TenantsPage', () => {
  beforeAll(() => server.listen());
  beforeEach(() => {
    setTenant(mockTenantGUID);
  });
  afterEach(() => {
    server.resetHandlers();
    jest.clearAllMocks();
    jest.clearAllTimers();
  });
  afterAll(() => server.close());

  it('renders the tenants page', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<TenantPage />, initialState, undefined, true);

    await waitFor(() => {
      expect(screen.getAllByText(mockTenantData[0].Name).length).toBe(1);
    });
    expect(container).toMatchSnapshot('initial table state');
  });

  it('should display Create Tenant button', () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<TenantPage />, initialState, undefined, true);

    const createButton = screen.getByRole('button', { name: /create tenant/i });
    expect(createButton).toBeVisible();
    expect(createButton).toMatchSnapshot();
  });

  it('should create a tenant and should be visible in the table', async () => {
    const initialState = createMockInitialState();
    jest.setTimeout(15000);

    const { container } = renderWithRedux(<TenantPage />, initialState, undefined, true);

    const createButton = screen.getByRole('button', { name: /create tenant/i });
    expect(createButton).toMatchSnapshot('create tenant button');
    fireEvent.click(createButton);

    const nameInput = screen.getByPlaceholderText(/enter name/i);
    const activeInput = screen.getByRole('switch');

    fireEvent.change(nameInput, { target: { value: mockTenantData[0].Name } });
    fireEvent.click(activeInput);

    const submitButton = screen.getByRole('button', { name: /ok/i });
    fireEvent.click(submitButton);

    await waitFor(() => {
      expect(screen.getAllByText(mockTenantData[0].Name).length).toBe(1);
    });

    const finalTable = container.querySelector('.ant-table');
    expect(finalTable).toMatchSnapshot('final table state');
  }, 15000);

  it('should update tenant successfully', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(
      <AddEditTenant
        isAddEditTenantVisible={true}
        setIsAddEditTenantVisible={() => {}}
        tenant={mockTenantData[0]}
      />,
      initialState,
      undefined,
      true
    );

    const nameInput = screen.getByPlaceholderText(/enter name/i);
    const activeInput = screen.getByRole('switch');

    const updatedName = 'Updated Tenant Name';
    const updatedActive = false;

    fireEvent.change(nameInput, { target: { value: updatedName } });
    fireEvent.click(activeInput);

    const submitButton = screen.getByRole('button', { name: /ok/i });
    fireEvent.click(submitButton);

    const finalTable = container.querySelector('.ant-table');
    expect(finalTable).toMatchSnapshot('final table state after update');
  });

  it('should delete tenant successfully', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(
      <DeleteTenant
        isDeleteModelVisible={true}
        setIsDeleteModelVisible={() => {}}
        selectedTenant={mockTenantData[0]}
        setSelectedTenant={() => {}}
        title="Delete Tenant"
        paragraphText="Are you sure you want to delete this tenant?"
        onTenantDeleted={async () => {}}
      />,
      initialState,
      undefined,
      true
    );
    const confirmModal = await screen.findByTestId('delete-tenant-modal');
    expect(confirmModal).toBeVisible();
    expect(confirmModal).toMatchSnapshot('delete confirmation modal');

    const confirmButton = screen.getByRole('button', { name: /delete/i });
    fireEvent.click(confirmButton);

    const finalTable = container.querySelector('.ant-table');
    expect(finalTable).toMatchSnapshot('final table state after deletion');
  });
});
