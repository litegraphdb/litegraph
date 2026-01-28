import '@testing-library/jest-dom';
import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import EnableVectorIndexModal from '@/page/graphs/components/EnableVectorIndexModal';
import { VectorIndexType } from '@/page/graphs/components/types';
import {
  useEnableVectorIndexMutation,
  useReadVectorIndexConfigurationQuery,
} from '@/lib/store/slice/slice';
import { renderWithRedux } from '@/tests/store/utils';

// Mock the API hooks
jest.mock('@/lib/store/slice/slice', () => ({
  useEnableVectorIndexMutation: jest.fn(),
  useReadVectorIndexConfigurationQuery: jest.fn(),
}));

// Mock the validation function
jest.mock('@/page/graphs/components/constant', () => ({
  validateVectorIndexFile: jest.fn(),
}));

// Mock the message module
jest.mock('antd', () => ({
  ...jest.requireActual('antd'),
  message: {
    success: jest.fn(),
    error: jest.fn(),
  },
}));

const mockUseEnableVectorIndexMutation = useEnableVectorIndexMutation as jest.MockedFunction<
  typeof useEnableVectorIndexMutation
>;
const mockUseReadVectorIndexConfigurationQuery =
  useReadVectorIndexConfigurationQuery as jest.MockedFunction<
    typeof useReadVectorIndexConfigurationQuery
  >;

describe('EnableVectorIndexModal Component', () => {
  const defaultProps = {
    isEnableVectorIndexModalVisible: true,
    setIsEnableVectorIndexModalVisible: jest.fn(),
    graphId: 'test-graph-id',
    onSuccess: jest.fn(),
    viewMode: false,
  };

  const mockEnableVectorIndex = jest.fn();
  const mockSetModalVisible = jest.fn();
  const mockOnSuccess = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();

    mockUseEnableVectorIndexMutation.mockReturnValue([
      mockEnableVectorIndex,
      { isLoading: false },
    ] as any);

    mockUseReadVectorIndexConfigurationQuery.mockReturnValue({
      data: null,
      isLoading: false,
      isFetching: false,
      error: null,
      isError: false,
    } as any);
  });

  describe('Create Mode (viewMode = false)', () => {
    it('renders create mode modal with correct title', () => {
      renderWithRedux(<EnableVectorIndexModal {...defaultProps} />);

      expect(screen.getByText('Enable Vector Index')).toBeInTheDocument();
    });

    it('renders form fields in create mode', () => {
      renderWithRedux(<EnableVectorIndexModal {...defaultProps} />);

      expect(screen.getByText('Vector Index Type')).toBeInTheDocument();
      expect(screen.getByText('Vector Index File')).toBeInTheDocument();
      expect(screen.getByText('Vector Index Threshold')).toBeInTheDocument();
      expect(screen.getByText('Vector Dimensionality')).toBeInTheDocument();
      expect(screen.getByText('Vector Index M')).toBeInTheDocument();
      expect(screen.getByText('Vector Index Ef')).toBeInTheDocument();
      expect(screen.getByText('Vector Index Ef Construction')).toBeInTheDocument();
    });

    it('closes modal on cancel', () => {
      renderWithRedux(
        <EnableVectorIndexModal
          {...defaultProps}
          setIsEnableVectorIndexModalVisible={mockSetModalVisible}
        />
      );

      const cancelButton = screen.getByText('Cancel');
      fireEvent.click(cancelButton);

      expect(mockSetModalVisible).toHaveBeenCalledWith(false);
    });
  });

  describe('View Mode (viewMode = true)', () => {
    const viewModeProps = {
      ...defaultProps,
      viewMode: true,
    };

    it('renders view mode modal with correct title', () => {
      renderWithRedux(<EnableVectorIndexModal {...viewModeProps} />);

      expect(screen.getByText('Vector Index Configuration')).toBeInTheDocument();
    });

    it('shows loading state while fetching configuration', () => {
      mockUseReadVectorIndexConfigurationQuery.mockReturnValue({
        data: null,
        isLoading: true,
        isFetching: false,
        error: null,
        isError: false,
      } as any);

      renderWithRedux(<EnableVectorIndexModal {...viewModeProps} />);

      expect(screen.getByTestId('loading-message')).toBeInTheDocument();
      expect(screen.getByText('Loading...')).toBeInTheDocument();
    });

    it('shows error state when configuration fetch fails', () => {
      mockUseReadVectorIndexConfigurationQuery.mockReturnValue({
        data: null,
        isLoading: false,
        isFetching: false,
        error: { data: { Description: 'API Error' } },
        isError: true,
      } as any);

      renderWithRedux(<EnableVectorIndexModal {...viewModeProps} />);

      expect(screen.getByText('Failed to load vector index configuration')).toBeInTheDocument();
      expect(screen.getByText('API Error')).toBeInTheDocument();
    });

    it('shows error state with fallback error message', () => {
      mockUseReadVectorIndexConfigurationQuery.mockReturnValue({
        data: null,
        isLoading: false,
        isFetching: false,
        error: { Description: 'Direct Error' },
        isError: true,
      } as any);

      renderWithRedux(<EnableVectorIndexModal {...viewModeProps} />);

      expect(screen.getByText('Direct Error')).toBeInTheDocument();
    });

    it('shows error state with generic error message', () => {
      mockUseReadVectorIndexConfigurationQuery.mockReturnValue({
        data: null,
        isLoading: false,
        isFetching: false,
        error: {},
        isError: true,
      } as any);

      renderWithRedux(<EnableVectorIndexModal {...viewModeProps} />);

      expect(screen.getByText('Unable to retrieve configuration details')).toBeInTheDocument();
    });

    it('displays configuration data in read-only fields', () => {
      const mockConfig = {
        VectorIndexType: VectorIndexType.HnswRam,
        VectorIndexFile: 'test.db',
        VectorIndexThreshold: 0.5,
        VectorDimensionality: 768,
        VectorIndexM: 32,
        VectorIndexEf: 150,
        VectorIndexEfConstruction: 300,
      };

      mockUseReadVectorIndexConfigurationQuery.mockReturnValue({
        data: mockConfig,
        isLoading: false,
        isFetching: false,
        error: null,
        isError: false,
      } as any);

      renderWithRedux(<EnableVectorIndexModal {...viewModeProps} />);

      // Check that fields display the configuration values
      expect(screen.getByDisplayValue(VectorIndexType.HnswRam)).toBeInTheDocument();
      expect(screen.getByDisplayValue('test.db')).toBeInTheDocument();
      expect(screen.getByDisplayValue('0.5')).toBeInTheDocument();
      expect(screen.getByDisplayValue('768')).toBeInTheDocument();
      expect(screen.getByDisplayValue('32')).toBeInTheDocument();
      expect(screen.getByDisplayValue('150')).toBeInTheDocument();
      expect(screen.getByDisplayValue('300')).toBeInTheDocument();
    });

    it('has OK button that acts as close button in view mode', () => {
      renderWithRedux(
        <EnableVectorIndexModal
          {...viewModeProps}
          setIsEnableVectorIndexModalVisible={mockSetModalVisible}
        />
      );

      const okButton = screen.getByText('OK');
      expect(okButton).toBeInTheDocument();

      fireEvent.click(okButton);
      expect(mockSetModalVisible).toHaveBeenCalledWith(false);
    });

    it('does not show loading state on OK button in view mode', () => {
      renderWithRedux(<EnableVectorIndexModal {...viewModeProps} />);

      const okButton = screen.getByText('OK');
      expect(okButton).not.toHaveAttribute('loading');
    });
  });

  describe('Edge Cases', () => {
    it('handles graphId changes correctly', () => {
      const { rerender } = renderWithRedux(<EnableVectorIndexModal {...defaultProps} />);

      // Change graphId
      rerender(<EnableVectorIndexModal {...defaultProps} graphId="new-graph-id" />);

      // Form should still be rendered
      expect(screen.getByText('Enable Vector Index')).toBeInTheDocument();
    });
  });
});
