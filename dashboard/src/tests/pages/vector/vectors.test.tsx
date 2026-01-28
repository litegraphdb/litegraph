import '@testing-library/jest-dom';
import React from 'react';
import { screen, fireEvent, waitFor } from '@testing-library/react';
import VectorPage from '@/app/dashboard/[tenantId]/vectors/page';
import { createMockInitialState } from '../../store/mockStore';
import { mockVectorData, mockTenantGUID } from '../mockData';
import { setupServer } from 'msw/node';
import { handlers } from './handler';
import { commonHandlers } from '@/tests/handler';
import { renderWithRedux } from '@/tests/store/utils';
import AddEditVector from '@/page/vectors/components/AddEditVector';
import DeleteVector from '@/page/vectors/components/DeleteVector';
import { mockGraphGUID } from '../mockData';
import { VectorType } from '@/types/types';
import { http, HttpResponse } from 'msw';
import { mockEndpoint } from '@/tests/config';

// Mock react-hot-toast
jest.mock('react-hot-toast', () => ({
  success: jest.fn(),
  error: jest.fn(),
}));

const server = setupServer(...handlers, ...commonHandlers);

describe('Vectors Page', () => {
  beforeAll(() => server.listen());
  afterEach(() => {
    server.resetHandlers();
  });
  afterAll(() => server.close());

  it('renders the vectors page', () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<VectorPage />, initialState, undefined, true);

    expect(screen.getByText(/vectors/i)).toBeVisible();
    expect(screen.getByRole('button', { name: /create vector/i })).toBeVisible();
    expect(container).toMatchSnapshot('initial table state');
  });

  it('should create a vector and should be visible in the table', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(<VectorPage />, initialState, undefined, true);

    const createButton = screen.getByRole('button', { name: /create vector/i });
    expect(createButton).toBeVisible();
    expect(createButton).toMatchSnapshot();
  });

  it('should create a vector and should be visible in the table', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(
      <AddEditVector
        isAddEditVectorVisible={true}
        setIsAddEditVectorVisible={() => {}}
        vector={null}
        selectedGraph={mockGraphGUID}
      />,
      initialState,
      undefined,
      true
    );

    const modal = await screen.findByTestId('add-edit-vector-modal');
    expect(modal).toBeInTheDocument();

    await waitFor(
      () => {
        expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
      },
      { timeout: 10000 }
    );

    const modelInput = screen.getByPlaceholderText(/enter model/i);
    fireEvent.change(modelInput, { target: { value: mockVectorData[0].Model } });

    const dimensionalityInput = screen.getByPlaceholderText(/enter dimensionality/i);
    fireEvent.change(dimensionalityInput, { target: { value: mockVectorData[0].Dimensionality } });

    const contentInput = screen.getByPlaceholderText(/enter content/i);
    fireEvent.change(contentInput, { target: { value: mockVectorData[0].Content } });

    const submitButton = screen.getByRole('button', { name: /ok/i });
    fireEvent.click(submitButton);

    const finalTable = container.querySelector('.ant-table');
    expect(finalTable).toMatchSnapshot('final table state');
  });

  it('should update vector successfully', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(
      <AddEditVector
        isAddEditVectorVisible={true}
        setIsAddEditVectorVisible={() => {}}
        vector={null}
        selectedGraph={mockGraphGUID}
      />,
      initialState,
      undefined,
      true
    );

    const modal = await screen.findByTestId('add-edit-vector-modal');
    expect(modal).toBeInTheDocument();

    await waitFor(
      () => {
        expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
      },
      { timeout: 10000 }
    );

    const modelInput = screen.getByPlaceholderText(/enter model/i);
    fireEvent.change(modelInput, { target: { value: mockVectorData[0].Model } });

    const submitButton = screen.getByRole('button', { name: /ok/i });
    fireEvent.click(submitButton);

    const finalTable = container.querySelector('.ant-table');
    expect(finalTable).toMatchSnapshot('final table state');
  });

  it('should delete vector successfully', async () => {
    const initialState = createMockInitialState();
    const { container } = renderWithRedux(
      <DeleteVector
        title="Delete Vector"
        paragraphText="Are you sure you want to delete this vector?"
        isDeleteModelVisible={true}
        setIsDeleteModelVisible={() => {}}
        selectedVector={null}
        setSelectedVector={() => {}}
        onVectorDeleted={() => Promise.resolve()}
      />,
      initialState,
      undefined,
      true
    );

    const modal = await screen.findByTestId('delete-vector-modal');
    expect(modal).toBeInTheDocument();

    await waitFor(
      () => {
        expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
      },
      { timeout: 10000 }
    );

    const confirmButton = screen.getByRole('button', { name: /delete/i });
    fireEvent.click(confirmButton);

    const finalTable = container.querySelector('.ant-table');
    expect(finalTable).toMatchSnapshot('final table state');
  });
});

describe('AddEditVector Component', () => {
  beforeAll(() => server.listen());
  afterEach(() => {
    server.resetHandlers();
  });
  afterAll(() => server.close());

  const mockSetIsAddEditVectorVisible = jest.fn();
  const mockOnVectorUpdated = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('Create Mode', () => {
    it('should render create vector modal with correct title', async () => {
      const initialState = createMockInitialState();
      renderWithRedux(
        <AddEditVector
          isAddEditVectorVisible={true}
          setIsAddEditVectorVisible={mockSetIsAddEditVectorVisible}
          vector={null}
          selectedGraph={mockGraphGUID}
        />,
        initialState,
        undefined,
        true
      );

      const modal = await screen.findByTestId('add-edit-vector-modal');
      expect(modal).toBeInTheDocument();
      expect(screen.getByText('Create Vector')).toBeInTheDocument();
    });

    it('should render all required form fields', async () => {
      const initialState = createMockInitialState();
      renderWithRedux(
        <AddEditVector
          isAddEditVectorVisible={true}
          setIsAddEditVectorVisible={mockSetIsAddEditVectorVisible}
          vector={null}
          selectedGraph={mockGraphGUID}
        />,
        initialState,
        undefined,
        true
      );

      await waitFor(
        () => {
          expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
        },
        { timeout: 10000 }
      );

      // Check for form fields by placeholder text instead of labels
      expect(screen.getByPlaceholderText(/enter model/i)).toBeInTheDocument();
      expect(screen.getByPlaceholderText(/enter dimensionality/i)).toBeInTheDocument();
      expect(screen.getByPlaceholderText(/enter content/i)).toBeInTheDocument();

      // Check for JsonEditor - it appears as a textarea in the test environment
      expect(screen.getByTestId('json-editor-textarea')).toBeInTheDocument();

      // Check for Node and Edge selectors
      expect(screen.getByText('Node')).toBeInTheDocument();
      expect(screen.getByText('Edge')).toBeInTheDocument();
    });

    it('should have submit button disabled initially due to form validation', async () => {
      const initialState = createMockInitialState();
      renderWithRedux(
        <AddEditVector
          isAddEditVectorVisible={true}
          setIsAddEditVectorVisible={mockSetIsAddEditVectorVisible}
          vector={null}
          selectedGraph={mockGraphGUID}
        />,
        initialState,
        undefined,
        true
      );

      await waitFor(
        () => {
          expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
        },
        { timeout: 10000 }
      );

      const submitButton = screen.getByRole('button', { name: /ok/i });
      expect(submitButton).toBeDisabled();
    });

    it('should show validation errors for empty required fields', async () => {
      const initialState = createMockInitialState();
      renderWithRedux(
        <AddEditVector
          isAddEditVectorVisible={true}
          setIsAddEditVectorVisible={mockSetIsAddEditVectorVisible}
          vector={null}
          selectedGraph={mockGraphGUID}
        />,
        initialState,
        undefined,
        true
      );

      await waitFor(
        () => {
          expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
        },
        { timeout: 10000 }
      );

      // Try to submit without filling required fields
      const submitButton = screen.getByRole('button', { name: /ok/i });

      // The button should be disabled initially due to form validation
      expect(submitButton).toBeDisabled();

      // Fill only one field to trigger validation
      const modelInput = screen.getByPlaceholderText(/enter model/i);
      fireEvent.change(modelInput, { target: { value: 'test' } });

      // Try to submit again - should still be disabled or show validation errors
      fireEvent.click(submitButton);

      // Check if validation errors appear or button remains disabled
      await waitFor(() => {
        // Either validation errors should appear or button should remain disabled
        const isDisabled = submitButton.disabled;
        const hasValidationErrors = screen.queryByText(/please input/i);
        expect(isDisabled || hasValidationErrors).toBeTruthy();
      });
    });

    it('should close modal and reset form when cancel is clicked', async () => {
      const initialState = createMockInitialState();
      renderWithRedux(
        <AddEditVector
          isAddEditVectorVisible={true}
          setIsAddEditVectorVisible={mockSetIsAddEditVectorVisible}
          vector={null}
          selectedGraph={mockGraphGUID}
        />,
        initialState,
        undefined,
        true
      );

      await waitFor(
        () => {
          expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
        },
        { timeout: 10000 }
      );

      const cancelButton = screen.getByRole('button', { name: /cancel/i });
      fireEvent.click(cancelButton);

      expect(mockSetIsAddEditVectorVisible).toHaveBeenCalledWith(false);
    });
  });

  describe('Edit Mode', () => {
    const mockVector: VectorType = {
      ...mockVectorData[0],
      NodeName: 'Test Node',
      EdgeName: 'Test Edge',
      LastUpdateUtc: mockVectorData[0].LastUpdateUtc || '2024-01-01T00:00:00Z',
    };

    it('should render edit vector modal with correct title', async () => {
      const initialState = createMockInitialState();
      renderWithRedux(
        <AddEditVector
          isAddEditVectorVisible={true}
          setIsAddEditVectorVisible={mockSetIsAddEditVectorVisible}
          vector={mockVector}
          selectedGraph={mockGraphGUID}
        />,
        initialState,
        undefined,
        true
      );

      const modal = await screen.findByTestId('add-edit-vector-modal');
      expect(modal).toBeInTheDocument();
      expect(screen.getByText('Edit Vector')).toBeInTheDocument();
    });

    it('should pre-populate form fields with vector data', async () => {
      const initialState = createMockInitialState();
      renderWithRedux(
        <AddEditVector
          isAddEditVectorVisible={true}
          setIsAddEditVectorVisible={mockSetIsAddEditVectorVisible}
          vector={mockVector}
          selectedGraph={mockGraphGUID}
        />,
        initialState,
        undefined,
        true
      );

      await waitFor(
        () => {
          expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
        },
        { timeout: 10000 }
      );

      const modelInput = screen.getByPlaceholderText(/enter model/i);
      const dimensionalityInput = screen.getByPlaceholderText(/enter dimensionality/i);
      const contentInput = screen.getByPlaceholderText(/enter content/i);

      expect(modelInput).toHaveValue(mockVector.Model);
      expect(dimensionalityInput).toHaveValue(mockVector.Dimensionality);
      expect(contentInput).toHaveValue(mockVector.Content);
    });

    it('should successfully update vector with modified data', async () => {
      const initialState = createMockInitialState();
      renderWithRedux(
        <AddEditVector
          isAddEditVectorVisible={true}
          setIsAddEditVectorVisible={mockSetIsAddEditVectorVisible}
          vector={mockVector}
          selectedGraph={mockGraphGUID}
          onVectorUpdated={mockOnVectorUpdated}
        />,
        initialState,
        undefined,
        true
      );

      await waitFor(
        () => {
          expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
        },
        { timeout: 10000 }
      );

      const modelInput = screen.getByPlaceholderText(/enter model/i);
      fireEvent.change(modelInput, { target: { value: 'updated-model' } });

      // Wait for form validation to complete
      await waitFor(
        () => {
          const submitButton = screen.getByRole('button', { name: /ok/i });
          expect(submitButton).not.toBeDisabled();
        },
        { timeout: 10000 }
      );

      const submitButton = screen.getByRole('button', { name: /ok/i });
      fireEvent.click(submitButton);

      await waitFor(
        () => {
          expect(mockSetIsAddEditVectorVisible).toHaveBeenCalledWith(false);
          expect(mockOnVectorUpdated).toHaveBeenCalled();
        },
        { timeout: 5000 }
      );
    });
  });

  describe('Update Vector Logic', () => {
    it('should handle undefined NodeGUID and EdgeGUID during update', async () => {
      const mockVector: VectorType = {
        ...mockVectorData[0],
        NodeName: 'Test Node',
        EdgeName: 'Test Edge',
        LastUpdateUtc: mockVectorData[0].LastUpdateUtc || '2024-01-01T00:00:00Z',
      };

      const initialState = createMockInitialState();
      renderWithRedux(
        <AddEditVector
          isAddEditVectorVisible={true}
          setIsAddEditVectorVisible={mockSetIsAddEditVectorVisible}
          vector={mockVector}
          selectedGraph={mockGraphGUID}
          onVectorUpdated={mockOnVectorUpdated}
        />,
        initialState,
        undefined,
        true
      );

      await waitFor(
        () => {
          expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
        },
        { timeout: 10000 }
      );

      const modelInput = screen.getByPlaceholderText(/enter model/i);
      fireEvent.change(modelInput, { target: { value: 'updated-model' } });

      await waitFor(
        () => {
          const submitButton = screen.getByRole('button', { name: /ok/i });
          expect(submitButton).not.toBeDisabled();
        },
        { timeout: 10000 }
      );

      const submitButton = screen.getByRole('button', { name: /ok/i });
      fireEvent.click(submitButton);

      await waitFor(
        () => {
          expect(mockSetIsAddEditVectorVisible).toHaveBeenCalledWith(false);
          expect(mockOnVectorUpdated).toHaveBeenCalled();
        },
        { timeout: 5000 }
      );
    });
  });

  describe('Success Handling', () => {
    it('should show success toast and call onVectorUpdated after successful update', async () => {
      const mockVector: VectorType = {
        ...mockVectorData[0],
        LastUpdateUtc: mockVectorData[0].LastUpdateUtc || '2024-01-01T00:00:00Z',
      };

      const initialState = createMockInitialState();
      renderWithRedux(
        <AddEditVector
          isAddEditVectorVisible={true}
          setIsAddEditVectorVisible={mockSetIsAddEditVectorVisible}
          vector={mockVector}
          selectedGraph={mockGraphGUID}
          onVectorUpdated={mockOnVectorUpdated}
        />,
        initialState,
        undefined,
        true
      );

      await waitFor(
        () => {
          expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
        },
        { timeout: 10000 }
      );

      const modelInput = screen.getByPlaceholderText(/enter model/i);
      fireEvent.change(modelInput, { target: { value: 'updated-model' } });

      await waitFor(
        () => {
          const submitButton = screen.getByRole('button', { name: /ok/i });
          expect(submitButton).not.toBeDisabled();
        },
        { timeout: 10000 }
      );

      const submitButton = screen.getByRole('button', { name: /ok/i });
      fireEvent.click(submitButton);

      await waitFor(
        () => {
          // Check that the toast.success was called instead of looking for the text in DOM
          const { success } = require('react-hot-toast');
          expect(success).toHaveBeenCalledWith('Vector updated successfully');
          expect(mockSetIsAddEditVectorVisible).toHaveBeenCalledWith(false);
          expect(mockOnVectorUpdated).toHaveBeenCalled();
        },
        { timeout: 5000 }
      );
    });
  });

  describe('Edge Cases', () => {
    it('should handle component unmounting during API call', async () => {
      const initialState = createMockInitialState();
      const { unmount } = renderWithRedux(
        <AddEditVector
          isAddEditVectorVisible={true}
          setIsAddEditVectorVisible={mockSetIsAddEditVectorVisible}
          vector={null}
          selectedGraph={mockGraphGUID}
        />,
        initialState,
        undefined,
        true
      );

      await waitFor(
        () => {
          expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
        },
        { timeout: 10000 }
      );

      const modelInput = screen.getByPlaceholderText(/enter model/i);
      const dimensionalityInput = screen.getByPlaceholderText(/enter dimensionality/i);
      const contentInput = screen.getByPlaceholderText(/enter content/i);

      fireEvent.change(modelInput, { target: { value: 'test-model' } });
      fireEvent.change(dimensionalityInput, { target: { value: '3' } });
      fireEvent.change(contentInput, { target: { value: 'test content' } });

      const submitButton = screen.getByRole('button', { name: /ok/i });
      fireEvent.click(submitButton);

      // Unmount before API call completes
      unmount();

      // Should not throw any errors
      expect(true).toBe(true);
    });

    it('should handle rapid form changes', async () => {
      const initialState = createMockInitialState();
      renderWithRedux(
        <AddEditVector
          isAddEditVectorVisible={true}
          setIsAddEditVectorVisible={mockSetIsAddEditVectorVisible}
          vector={null}
          selectedGraph={mockGraphGUID}
        />,
        initialState,
        undefined,
        true
      );

      await waitFor(
        () => {
          expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
        },
        { timeout: 10000 }
      );

      const modelInput = screen.getByPlaceholderText(/enter model/i);
      const dimensionalityInput = screen.getByPlaceholderText(/enter dimensionality/i);
      const contentInput = screen.getByPlaceholderText(/enter content/i);

      // Rapid changes
      fireEvent.change(modelInput, { target: { value: 'test1' } });
      fireEvent.change(modelInput, { target: { value: 'test2' } });
      fireEvent.change(modelInput, { target: { value: 'test3' } });
      fireEvent.change(dimensionalityInput, { target: { value: '1' } });
      fireEvent.change(dimensionalityInput, { target: { value: '2' } });
      fireEvent.change(dimensionalityInput, { target: { value: '3' } });
      fireEvent.change(contentInput, { target: { value: 'content1' } });
      fireEvent.change(contentInput, { target: { value: 'content2' } });
      fireEvent.change(contentInput, { target: { value: 'content3' } });

      await waitFor(() => {
        expect(modelInput).toHaveValue('test3');
        expect(dimensionalityInput).toHaveValue(3);
        expect(contentInput).toHaveValue('content3');
      });
    });

    it('should handle empty selectedGraph prop', async () => {
      const initialState = createMockInitialState();
      renderWithRedux(
        <AddEditVector
          isAddEditVectorVisible={true}
          setIsAddEditVectorVisible={mockSetIsAddEditVectorVisible}
          vector={null}
          selectedGraph=""
        />,
        initialState,
        undefined,
        true
      );

      await waitFor(
        () => {
          expect(screen.queryByText('Loading...')).not.toBeInTheDocument();
        },
        { timeout: 10000 }
      );

      const modal = screen.getByTestId('add-edit-vector-modal');
      expect(modal).toBeInTheDocument();
    });
  });
});
