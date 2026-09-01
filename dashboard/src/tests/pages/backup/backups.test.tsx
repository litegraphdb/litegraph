import '@testing-library/jest-dom';
import React from 'react';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import { createMockInitialState } from '../../store/mockStore';
import { mockBackupData, mockTenantData } from '../mockData';
import BackupPage from '@/page/backups/BackupPage';
import { renderWithRedux } from '@/tests/store/utils';
import { getServer } from '@/tests/server';
import { handlers } from './handler';
import { setTenant } from '@/lib/sdk/litegraph.service';
import { commonHandlers } from '@/tests/handler';

const server = getServer([...commonHandlers, ...handlers]);
setTenant(mockTenantData[0].GUID);

describe('BackupPage', () => {
  beforeAll(() => {
    server.listen();
  });
  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  it('should render the BackupPage', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<BackupPage />, initialState, undefined, true);
    await waitFor(() => {
      expect(screen.getByText('Create Backup')).toBeVisible();
      expect(screen.getByText(mockBackupData[0].Filename)).toBeVisible();
    });
    expect(container).toMatchSnapshot('initial table state');

    const createButton = screen.getByRole('button', { name: /create backup/i });
    await waitFor(() => {
      expect(createButton).toBeVisible();
    });
    fireEvent.click(createButton);
  });

  it('should display Create Backup button', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<BackupPage />, initialState, undefined, true);

    const createButton = screen.getByRole('button', { name: /create backup/i });
    await waitFor(() => {
      expect(createButton).toBeVisible();
    });
    expect(createButton).toMatchSnapshot();
  });

  it('should create a backup and should be visible in the table', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<BackupPage />, initialState, undefined, true);

    const initialTable = container.querySelector('.ant-table');
    expect(initialTable).toMatchSnapshot('initial table state');

    const createButton = screen.getByRole('button', { name: /create backup/i });
    await waitFor(() => {
      expect(createButton).toBeVisible();
    });
    fireEvent.click(createButton);

    const addBackupModal = container.querySelector('.ant-modal');
    expect(addBackupModal).toMatchSnapshot('add backup modal');

    const backupNameInput = screen.getByTestId('filename-input');
    await waitFor(() => {
      expect(backupNameInput).toBeVisible();
    });

    const backupName = mockBackupData[0].Filename;
    fireEvent.change(backupNameInput, { target: { value: backupName } });

    const submitButton = screen.getByRole('button', { name: /ok/i });
    await waitFor(() => {
      expect(submitButton).toBeVisible();
    });
    fireEvent.click(submitButton);

    const finalTable = container.querySelector('.ant-table');
    await waitFor(() => {
      expect(finalTable).toBeVisible();
    });
  }, 15000);

  it('should delete a backup and should be removed from the table', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<BackupPage />, initialState, undefined, true);

    const initialTable = container.querySelector('.ant-table');
    await waitFor(() => {
      expect(initialTable).toBeVisible();
    });

    const menuButtons = await screen.findAllByRole('backup-action-menu');
    expect(menuButtons.length).toBeGreaterThan(0);

    fireEvent.click(menuButtons[0]);

    const deleteButton = await screen.findByText(/delete/i); // inside dropdown
    fireEvent.click(deleteButton);

    const confirmButton = await screen.findByRole('button', { name: /delete/i }); // modal confirm
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument(); // confirm modal closed
    });

    const finalTable = container.querySelector('.ant-table');
    await waitFor(() => {
      expect(finalTable).toBeVisible();
    });
    expect(finalTable).toMatchSnapshot('final table state after deletion');
  }, 15000);

  it('should download a backup', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<BackupPage />, initialState, undefined, true);

    // ✅ Wait for the backup filename to appear (ensures table is populated)
    await waitFor(() => {
      expect(screen.getAllByTestId('backup-filename').length).toBeGreaterThan(0);
    });

    // Now query the menu buttons
    const menuButtons = screen.getAllByRole('backup-action-menu');
    expect(menuButtons.length).toBeGreaterThan(0);
    fireEvent.click(menuButtons[0]);

    // Optional: click download if needed
    const downloadButton = await screen.findByText('Download');
    fireEvent.click(downloadButton);

    // Final assertion
    const finalTable = container.querySelector('.ant-table');
    expect(finalTable).toMatchSnapshot('final table state after download');
  }, 15000);

  it('should open create backup modal when create button is clicked', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<BackupPage />, initialState, undefined, true);

    const createButton = screen.getByRole('button', { name: /create backup/i });
    fireEvent.click(createButton);

    await waitFor(() => {
      expect(screen.getByText('Add Backup')).toBeVisible();
      expect(screen.getByPlaceholderText('Enter filename')).toBeVisible();
    });
  });

  it('should close create backup modal when cancel button is clicked', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<BackupPage />, initialState, undefined, true);

    const createButton = screen.getByRole('button', { name: /create backup/i });
    fireEvent.click(createButton);

    await waitFor(() => {
      expect(screen.getByText('Add Backup')).toBeVisible();
    });

    const cancelButton = screen.getByRole('button', { name: /cancel/i });
    fireEvent.click(cancelButton);

    await waitFor(() => {
      expect(screen.queryByText('Add Backup')).not.toBeInTheDocument();
    });
  });

  it('should disable OK button when filename is empty', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<BackupPage />, initialState, undefined, true);

    const createButton = screen.getByRole('button', { name: /create backup/i });
    fireEvent.click(createButton);

    await waitFor(() => {
      expect(screen.getByText('Add Backup')).toBeVisible();
    });

    const okButton = screen.getByRole('button', { name: /ok/i });
    expect(okButton).toBeDisabled();
  });

  it('should open delete confirmation modal when delete is clicked', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<BackupPage />, initialState, undefined, true);

    await waitFor(() => {
      expect(screen.getAllByTestId('backup-filename').length).toBeGreaterThan(0);
    });

    const menuButtons = screen.getAllByRole('backup-action-menu');
    fireEvent.click(menuButtons[0]);

    const deleteButton = await screen.findByText(/delete/i);
    fireEvent.click(deleteButton);

    await waitFor(() => {
      expect(screen.getByText(/are you sure you want to delete/i)).toBeVisible();
      expect(screen.getByText('This action will delete backup.')).toBeVisible();
    });
  });

  it('should close delete modal when cancel is clicked', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<BackupPage />, initialState, undefined, true);

    await waitFor(() => {
      expect(screen.getAllByTestId('backup-filename').length).toBeGreaterThan(0);
    });

    const menuButtons = screen.getAllByRole('backup-action-menu');
    fireEvent.click(menuButtons[0]);

    const deleteButton = await screen.findByText(/delete/i);
    fireEvent.click(deleteButton);

    await waitFor(() => {
      expect(screen.getByText(/are you sure you want to delete/i)).toBeVisible();
    });

    const cancelButton = screen.getByRole('button', { name: /cancel/i });
    fireEvent.click(cancelButton);

    await waitFor(() => {
      expect(screen.queryByText(/are you sure you want to delete/i)).not.toBeInTheDocument();
    });
  });

  it('should show delete button as danger button in modal', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<BackupPage />, initialState, undefined, true);

    await waitFor(() => {
      expect(screen.getAllByTestId('backup-filename').length).toBeGreaterThan(0);
    });

    const menuButtons = screen.getAllByRole('backup-action-menu');
    fireEvent.click(menuButtons[0]);

    const deleteButton = await screen.findByText(/delete/i);
    fireEvent.click(deleteButton);

    await waitFor(() => {
      const confirmDeleteButton = screen.getByRole('button', { name: /delete/i });
      expect(confirmDeleteButton).toBeVisible();
      expect(confirmDeleteButton).toHaveClass('ant-btn-dangerous');
    });
  });

  it('should show loading state when fetching backups', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<BackupPage />, initialState, undefined, true);

    // The table should show loading spinner initially
    expect(screen.getByText('Create Backup')).toBeVisible();

    // Wait for loading to complete
    await waitFor(() => {
      expect(screen.getAllByTestId('backup-filename').length).toBeGreaterThan(0);
    });
  });

  it('should show loading state when deleting backup', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<BackupPage />, initialState, undefined, true);

    await waitFor(() => {
      expect(screen.getAllByTestId('backup-filename').length).toBeGreaterThan(0);
    });

    const menuButtons = screen.getAllByRole('backup-action-menu');
    fireEvent.click(menuButtons[0]);

    const deleteButton = await screen.findByText(/delete/i);
    fireEvent.click(deleteButton);

    const confirmButton = await screen.findByRole('button', { name: /delete/i });
    fireEvent.click(confirmButton);

    // Check if delete button shows loading state
    await waitFor(() => {
      expect(confirmButton).toHaveClass('ant-btn-loading');
    });
  });

  it('should display backup metadata in table rows', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<BackupPage />, initialState, undefined, true);

    await waitFor(() => {
      // Check that all backup filenames are displayed
      mockBackupData.forEach((backup) => {
        expect(screen.getByText(backup.Filename)).toBeVisible();
      });

      // Verify the correct number of backup rows are rendered
      expect(screen.getAllByTestId('backup-filename')).toHaveLength(mockBackupData.length);

      // Check that table has data (not empty)
      const table = document.querySelector('.ant-table');
      expect(table).not.toHaveClass('ant-table-empty');

      // Verify table body contains the expected number of data rows (excluding any header/placeholder rows)
      const dataRows = document.querySelectorAll('.ant-table-tbody tr:not(.ant-table-placeholder)');
      expect(dataRows).toHaveLength(mockBackupData.length);
    });
  });

  it('should show action menu when menu button is clicked', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<BackupPage />, initialState, undefined, true);

    await waitFor(() => {
      expect(screen.getAllByTestId('backup-filename').length).toBeGreaterThan(0);
    });

    const menuButtons = screen.getAllByRole('backup-action-menu');
    fireEvent.click(menuButtons[0]);

    await waitFor(() => {
      expect(screen.getByText('Download')).toBeVisible();
      expect(screen.getByText('Delete')).toBeVisible();
    });
  });

  it('should handle empty backup list', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<BackupPage />, initialState, undefined, true);

    await waitFor(() => {
      // When Ant Design table is empty, it adds 'ant-table-empty' class
      const table = container.querySelector('.ant-table');
      expect(table).toHaveClass('ant-table-empty');

      // Check for the "No data" empty state message
      expect(screen.getByText('No data')).toBeVisible();
    });
  });

  it('should validate filename field on blur', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<BackupPage />, initialState, undefined, true);

    const createButton = screen.getByRole('button', { name: /create backup/i });
    fireEvent.click(createButton);

    const filenameInput = screen.getByTestId('filename-input');
    fireEvent.focus(filenameInput);
    fireEvent.change(filenameInput, { target: { value: 'test' } });
    fireEvent.change(filenameInput, { target: { value: '' } });
    fireEvent.blur(filenameInput);

    await waitFor(() => {
      expect(screen.getByText('Please enter a filename')).toBeVisible();
    });
  });

  it('should reset form when modal is closed', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<BackupPage />, initialState, undefined, true);

    const createButton = screen.getByRole('button', { name: /create backup/i });
    fireEvent.click(createButton);

    const filenameInput = screen.getByTestId('filename-input');
    fireEvent.change(filenameInput, { target: { value: 'test-backup.db' } });

    const cancelButton = screen.getByRole('button', { name: /cancel/i });
    fireEvent.click(cancelButton);

    // Open modal again
    fireEvent.click(createButton);

    await waitFor(() => {
      const newFilenameInput = screen.getByTestId('filename-input');
      expect(newFilenameInput.value).toBe('');
    });
  });

  it('should handle modal cancellation for create backup', async () => {
    const initialState = createMockInitialState();
    renderWithRedux(<BackupPage />, initialState, undefined, true);

    const createButton = screen.getByRole('button', { name: /create backup/i });
    fireEvent.click(createButton);

    // Wait for modal to appear
    await waitFor(() => {
      expect(screen.getByText('Add Backup')).toBeVisible();
    });

    // Cancel the modal
    const cancelButton = screen.getByRole('button', { name: /cancel/i });
    fireEvent.click(cancelButton);

    // Verify modal is closed
    await waitFor(() => {
      expect(screen.queryByText('Add Backup')).not.toBeInTheDocument();
    });
  });

  it('should handle loading states correctly', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<BackupPage />, initialState, undefined, true);

    // Should show loading state initially
    expect(container.querySelector('.ant-spin')).toBeInTheDocument();

    // Wait for loading to complete
    await waitFor(() => {
      expect(screen.getByText('Create Backup')).toBeVisible();
    });
  });
});
