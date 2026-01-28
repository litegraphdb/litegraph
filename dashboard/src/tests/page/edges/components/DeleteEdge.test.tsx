import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import DeleteEdge from '@/page/edges/components/DeleteEdge';
import { useDeleteEdgeMutation } from '@/lib/store/slice/slice';
import { EdgeType } from '@/types/types';
import toast from 'react-hot-toast';

// Mock dependencies
jest.mock('@/lib/store/slice/slice');
jest.mock('react-hot-toast');
jest.mock('@/components/base/modal/Modal', () => {
  return function MockLitegraphModal({ children, title, open, onCancel, footer, ...props }: any) {
    if (!open) return null;
    return (
      <div data-testid="delete-edge-modal" {...props}>
        <div data-testid="modal-title">{title}</div>
        <div data-testid="modal-content">{children}</div>
        <div data-testid="modal-footer">{footer}</div>
        <button data-testid="cancel-button" onClick={onCancel}>
          Cancel
        </button>
      </div>
    );
  };
});

jest.mock('@/components/base/typograpghy/Paragraph', () => {
  return function MockLitegraphParagraph({ children }: any) {
    return <p data-testid="paragraph">{children}</p>;
  };
});

jest.mock('@/components/base/button/Button', () => {
  return function MockLitegraphButton({ children, onClick, loading, ...props }: any) {
    return (
      <button data-testid="confirm-button" onClick={onClick} disabled={loading} {...props}>
        {children}
      </button>
    );
  };
});

const mockUseDeleteEdgeMutation = useDeleteEdgeMutation as jest.MockedFunction<
  typeof useDeleteEdgeMutation
>;
const mockToast = toast as jest.Mocked<typeof toast>;

describe('DeleteEdge', () => {
  const defaultProps = {
    title: 'Delete Edge',
    paragraphText: 'Are you sure you want to delete this edge?',
    isDeleteModelVisisble: true,
    setIsDeleteModelVisisble: jest.fn(),
    selectedEdge: {
      GUID: 'edge-123',
      Name: 'Test Edge',
      GraphGUID: 'graph-123',
      TenantGUID: 'tenant-123',
      CreatedUtc: '2023-01-01T00:00:00Z',
      LastUpdateUtc: '2023-01-01T00:00:00Z',
      Data: {},
      Tags: {},
      Labels: [],
      Vectors: [],
      Cost: 5,
      From: 'node-1',
      To: 'node-2',
    } as EdgeType,
    setSelectedEdge: jest.fn(),
    onEdgeDeleted: jest.fn(),
    removeLocalEdge: jest.fn(),
  };

  const mockDeleteEdgeById = jest.fn();

  beforeEach(() => {
    mockUseDeleteEdgeMutation.mockReturnValue([mockDeleteEdgeById, { isLoading: false }]);
    jest.clearAllMocks();
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  it('renders without crashing', () => {
    render(<DeleteEdge {...defaultProps} />);

    expect(screen.getByTestId('delete-edge-modal')).toBeInTheDocument();
    expect(screen.getByTestId('modal-title')).toHaveTextContent('Delete Edge');
    expect(screen.getByTestId('paragraph')).toHaveTextContent(
      'Are you sure you want to delete this edge?'
    );
    expect(screen.getByTestId('confirm-button')).toHaveTextContent('Confirm');
  });

  it('does not render when modal is not visible', () => {
    render(<DeleteEdge {...defaultProps} isDeleteModelVisisble={false} />);

    expect(screen.queryByTestId('delete-edge-modal')).not.toBeInTheDocument();
  });

  it('displays correct title and paragraph text', () => {
    const customProps = {
      ...defaultProps,
      title: 'Custom Delete Title',
      paragraphText: 'Custom delete message',
    };

    render(<DeleteEdge {...customProps} />);

    expect(screen.getByTestId('modal-title')).toHaveTextContent('Custom Delete Title');
    expect(screen.getByTestId('paragraph')).toHaveTextContent('Custom delete message');
  });

  it('calls onCancel when cancel button is clicked', () => {
    render(<DeleteEdge {...defaultProps} />);

    const cancelButton = screen.getByTestId('cancel-button');
    fireEvent.click(cancelButton);

    expect(defaultProps.setIsDeleteModelVisisble).toHaveBeenCalledWith(false);
  });

  it('handles local edge deletion when removeLocalEdge is provided', async () => {
    const mockRemoveLocalEdge = jest.fn();
    const props = {
      ...defaultProps,
      removeLocalEdge: mockRemoveLocalEdge,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockRemoveLocalEdge).toHaveBeenCalledWith('edge-123');
      expect(mockToast.success).toHaveBeenCalledWith('Delete Edge successfully');
      expect(props.setIsDeleteModelVisisble).toHaveBeenCalledWith(false);
      expect(props.setSelectedEdge).toHaveBeenCalledWith(null);
      expect(props.onEdgeDeleted).toHaveBeenCalled();
    });
  });

  it('handles API edge deletion when removeLocalEdge is not provided', async () => {
    const mockResponse = { data: { success: true } };
    mockDeleteEdgeById.mockResolvedValue(mockResponse);

    const props = {
      ...defaultProps,
      removeLocalEdge: undefined,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockDeleteEdgeById).toHaveBeenCalledWith({
        graphId: 'graph-123',
        edgeId: 'edge-123',
      });
      expect(mockToast.success).toHaveBeenCalledWith('Delete Edge successfully');
      expect(props.setIsDeleteModelVisisble).toHaveBeenCalledWith(false);
      expect(props.setSelectedEdge).toHaveBeenCalledWith(null);
      expect(props.onEdgeDeleted).toHaveBeenCalled();
    });
  });

  it('handles edge deletion with different edge properties', async () => {
    const mockRemoveLocalEdge = jest.fn();
    const customEdge: EdgeType = {
      GUID: 'custom-edge-456',
      Name: 'Custom Edge',
      GraphGUID: 'custom-graph-456',
      TenantGUID: 'custom-tenant-456',
      CreatedUtc: '2023-02-01T00:00:00Z',
      LastUpdateUtc: '2023-02-01T00:00:00Z',
      Data: { custom: 'data' },
      Tags: { custom: 'tag' },
      Labels: ['custom-label'],
      Vectors: ['custom-vector'],
      Cost: 10,
      From: 'custom-node-1',
      To: 'custom-node-2',
    } as EdgeType;

    const props = {
      ...defaultProps,
      selectedEdge: customEdge,
      removeLocalEdge: mockRemoveLocalEdge,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockRemoveLocalEdge).toHaveBeenCalledWith('custom-edge-456');
      expect(mockToast.success).toHaveBeenCalledWith('Delete Edge successfully');
    });
  });

  it('handles edge deletion with minimal edge properties', async () => {
    const mockRemoveLocalEdge = jest.fn();
    const minimalEdge: EdgeType = {
      GUID: 'minimal-edge',
      GraphGUID: 'minimal-graph',
    } as EdgeType;

    const props = {
      ...defaultProps,
      selectedEdge: minimalEdge,
      removeLocalEdge: mockRemoveLocalEdge,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockRemoveLocalEdge).toHaveBeenCalledWith('minimal-edge');
      expect(mockToast.success).toHaveBeenCalledWith('Delete Edge successfully');
    });
  });

  it('handles edge deletion with null selectedEdge', async () => {
    const mockRemoveLocalEdge = jest.fn();
    const props = {
      ...defaultProps,
      selectedEdge: null,
      removeLocalEdge: mockRemoveLocalEdge,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    // Should not call removeLocalEdge when selectedEdge is null
    expect(mockRemoveLocalEdge).not.toHaveBeenCalled();
    expect(mockToast.success).not.toHaveBeenCalled();
  });

  it('handles edge deletion with undefined selectedEdge', async () => {
    const mockRemoveLocalEdge = jest.fn();
    const props = {
      ...defaultProps,
      selectedEdge: undefined,
      removeLocalEdge: mockRemoveLocalEdge,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    // Should not call removeLocalEdge when selectedEdge is undefined
    expect(mockRemoveLocalEdge).not.toHaveBeenCalled();
    expect(mockToast.success).not.toHaveBeenCalled();
  });

  it('handles edge deletion without onEdgeDeleted callback', async () => {
    const mockRemoveLocalEdge = jest.fn();
    const props = {
      ...defaultProps,
      onEdgeDeleted: undefined,
      removeLocalEdge: mockRemoveLocalEdge,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockRemoveLocalEdge).toHaveBeenCalledWith('edge-123');
      expect(mockToast.success).toHaveBeenCalledWith('Delete Edge successfully');
      expect(props.setIsDeleteModelVisisble).toHaveBeenCalledWith(false);
      expect(props.setSelectedEdge).toHaveBeenCalledWith(null);
      // onEdgeDeleted should not be called when undefined
    });
  });

  it('handles edge deletion without removeLocalEdge callback', async () => {
    const mockResponse = { data: { success: true } };
    mockDeleteEdgeById.mockResolvedValue(mockResponse);

    const props = {
      ...defaultProps,
      removeLocalEdge: undefined,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockDeleteEdgeById).toHaveBeenCalledWith({
        graphId: 'graph-123',
        edgeId: 'edge-123',
      });
      expect(mockToast.success).toHaveBeenCalledWith('Delete Edge successfully');
    });
  });

  it('handles edge deletion with local edge flag', async () => {
    const mockRemoveLocalEdge = jest.fn();
    const localEdge: EdgeType = {
      ...defaultProps.selectedEdge,
      isLocal: true,
    } as any;

    const props = {
      ...defaultProps,
      selectedEdge: localEdge,
      removeLocalEdge: mockRemoveLocalEdge,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockRemoveLocalEdge).toHaveBeenCalledWith('edge-123');
      expect(mockToast.success).toHaveBeenCalledWith('Delete Edge successfully');
    });
  });

  it('handles edge deletion with API edge flag', async () => {
    const mockResponse = { data: { success: true } };
    mockDeleteEdgeById.mockResolvedValue(mockResponse);

    const apiEdge: EdgeType = {
      ...defaultProps.selectedEdge,
      isLocal: false,
    } as any;

    const props = {
      ...defaultProps,
      selectedEdge: apiEdge,
      removeLocalEdge: undefined,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockDeleteEdgeById).toHaveBeenCalledWith({
        graphId: 'graph-123',
        edgeId: 'edge-123',
      });
      expect(mockToast.success).toHaveBeenCalledWith('Delete Edge successfully');
    });
  });

  it('handles edge deletion with different GUID formats', async () => {
    const mockRemoveLocalEdge = jest.fn();
    const edgeWithDifferentGuid: EdgeType = {
      ...defaultProps.selectedEdge,
      GUID: 'different-guid-format-789',
    } as EdgeType;

    const props = {
      ...defaultProps,
      selectedEdge: edgeWithDifferentGuid,
      removeLocalEdge: mockRemoveLocalEdge,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockRemoveLocalEdge).toHaveBeenCalledWith('different-guid-format-789');
      expect(mockToast.success).toHaveBeenCalledWith('Delete Edge successfully');
    });
  });

  it('handles edge deletion with different GraphGUID formats', async () => {
    const mockResponse = { data: { success: true } };
    mockDeleteEdgeById.mockResolvedValue(mockResponse);

    const edgeWithDifferentGraphGuid: EdgeType = {
      ...defaultProps.selectedEdge,
      GraphGUID: 'different-graph-guid-456',
    } as EdgeType;

    const props = {
      ...defaultProps,
      selectedEdge: edgeWithDifferentGraphGuid,
      removeLocalEdge: undefined,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockDeleteEdgeById).toHaveBeenCalledWith({
        graphId: 'different-graph-guid-456',
        edgeId: 'edge-123',
      });
      expect(mockToast.success).toHaveBeenCalledWith('Delete Edge successfully');
    });
  });

  it('handles edge deletion with loading state', async () => {
    mockUseDeleteEdgeMutation.mockReturnValue([mockDeleteEdgeById, { isLoading: true }]);

    render(<DeleteEdge {...defaultProps} />);

    const confirmButton = screen.getByTestId('confirm-button');
    expect(confirmButton).toBeDisabled();
  });

  it('handles edge deletion with non-loading state', async () => {
    mockUseDeleteEdgeMutation.mockReturnValue([mockDeleteEdgeById, { isLoading: false }]);

    render(<DeleteEdge {...defaultProps} />);

    const confirmButton = screen.getByTestId('confirm-button');
    expect(confirmButton).not.toBeDisabled();
  });

  it('handles edge deletion with complex edge data', async () => {
    const mockRemoveLocalEdge = jest.fn();
    const complexEdge: EdgeType = {
      GUID: 'complex-edge',
      Name: 'Complex Edge with Special Characters: !@#$%^&*()',
      GraphGUID: 'complex-graph',
      TenantGUID: 'complex-tenant',
      CreatedUtc: '2023-03-01T00:00:00Z',
      LastUpdateUtc: '2023-03-01T00:00:00Z',
      Data: {
        complex: 'data',
        nested: {
          array: [1, 2, 3],
          boolean: true,
          null: null,
        },
      },
      Tags: {
        special: '!@#$%^&*()',
        unicode: '🚀🌟🎉',
        chinese: '你好世界',
      },
      Labels: ['label1', 'label2', 'label3'],
      Vectors: ['vector1', 'vector2', 'vector3'],
      Cost: 999.99,
      From: 'complex-node-1',
      To: 'complex-node-2',
    } as EdgeType;

    const props = {
      ...defaultProps,
      selectedEdge: complexEdge,
      removeLocalEdge: mockRemoveLocalEdge,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockRemoveLocalEdge).toHaveBeenCalledWith('complex-edge');
      expect(mockToast.success).toHaveBeenCalledWith('Delete Edge successfully');
    });
  });

  it('handles edge deletion with edge having only required properties', async () => {
    const mockRemoveLocalEdge = jest.fn();
    const minimalRequiredEdge: EdgeType = {
      GUID: 'minimal-required-edge',
      GraphGUID: 'minimal-required-graph',
    } as EdgeType;

    const props = {
      ...defaultProps,
      selectedEdge: minimalRequiredEdge,
      removeLocalEdge: mockRemoveLocalEdge,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockRemoveLocalEdge).toHaveBeenCalledWith('minimal-required-edge');
      expect(mockToast.success).toHaveBeenCalledWith('Delete Edge successfully');
    });
  });

  it('handles edge deletion with edge having all optional properties', async () => {
    const mockRemoveLocalEdge = jest.fn();
    const fullEdge: EdgeType = {
      GUID: 'full-edge',
      Name: 'Full Edge',
      GraphGUID: 'full-graph',
      TenantGUID: 'full-tenant',
      CreatedUtc: '2023-04-01T00:00:00Z',
      LastUpdateUtc: '2023-04-01T00:00:00Z',
      Data: {},
      Tags: {},
      Labels: [],
      Vectors: [],
      Cost: 0,
      From: '',
      To: '',
    } as EdgeType;

    const props = {
      ...defaultProps,
      selectedEdge: fullEdge,
      removeLocalEdge: mockRemoveLocalEdge,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockRemoveLocalEdge).toHaveBeenCalledWith('full-edge');
      expect(mockToast.success).toHaveBeenCalledWith('Delete Edge successfully');
    });
  });

  // Tests to cover uncovered lines 34-57
  it('handles edge deletion when removeLocalEdge is provided', async () => {
    const mockRemoveLocalEdge = jest.fn();
    const mockOnEdgeDeleted = jest.fn().mockResolvedValue(undefined);

    const props = {
      ...defaultProps,
      removeLocalEdge: mockRemoveLocalEdge,
      onEdgeDeleted: mockOnEdgeDeleted,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockRemoveLocalEdge).toHaveBeenCalledWith('edge-123');
      expect(mockToast.success).toHaveBeenCalledWith('Delete Edge successfully');
      expect(defaultProps.setIsDeleteModelVisisble).toHaveBeenCalledWith(false);
      expect(defaultProps.setSelectedEdge).toHaveBeenCalledWith(null);
      expect(mockOnEdgeDeleted).toHaveBeenCalled();
    });
  });

  it('handles edge deletion when removeLocalEdge is not provided', async () => {
    const mockOnEdgeDeleted = jest.fn().mockResolvedValue(undefined);
    mockDeleteEdgeById.mockResolvedValue({ success: true });

    const props = {
      ...defaultProps,
      removeLocalEdge: undefined,
      onEdgeDeleted: mockOnEdgeDeleted,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockDeleteEdgeById).toHaveBeenCalledWith({
        graphId: 'graph-123',
        edgeId: 'edge-123',
      });
      expect(mockToast.success).toHaveBeenCalledWith('Delete Edge successfully');
      expect(defaultProps.setIsDeleteModelVisisble).toHaveBeenCalledWith(false);
      expect(defaultProps.setSelectedEdge).toHaveBeenCalledWith(null);
      expect(mockOnEdgeDeleted).toHaveBeenCalled();
    });
  });

  it('handles edge deletion with API failure', async () => {
    mockDeleteEdgeById.mockResolvedValue(null);

    const props = {
      ...defaultProps,
      removeLocalEdge: undefined,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockDeleteEdgeById).toHaveBeenCalledWith({
        graphId: 'graph-123',
        edgeId: 'edge-123',
      });
      // Should not show success message or close modal on failure
      expect(mockToast.success).not.toHaveBeenCalled();
      expect(defaultProps.setIsDeleteModelVisisble).not.toHaveBeenCalled();
      expect(defaultProps.setSelectedEdge).not.toHaveBeenCalled();
    });
  });

  it('handles edge deletion with edge having different GUID formats', async () => {
    const mockRemoveLocalEdge = jest.fn();
    const edgeWithDifferentGUID: EdgeType = {
      ...defaultProps.selectedEdge!,
      GUID: 'different-guid-format-12345',
    };

    const props = {
      ...defaultProps,
      selectedEdge: edgeWithDifferentGUID,
      removeLocalEdge: mockRemoveLocalEdge,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockRemoveLocalEdge).toHaveBeenCalledWith('different-guid-format-12345');
      expect(mockToast.success).toHaveBeenCalledWith('Delete Edge successfully');
    });
  });

  it('handles edge deletion with edge having special characters in GUID', async () => {
    const mockRemoveLocalEdge = jest.fn();
    const edgeWithSpecialGUID: EdgeType = {
      ...defaultProps.selectedEdge!,
      GUID: 'edge-with-special-chars!@#$%^&*()',
    };

    const props = {
      ...defaultProps,
      selectedEdge: edgeWithSpecialGUID,
      removeLocalEdge: mockRemoveLocalEdge,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockRemoveLocalEdge).toHaveBeenCalledWith('edge-with-special-chars!@#$%^&*()');
      expect(mockToast.success).toHaveBeenCalledWith('Delete Edge successfully');
    });
  });

  it('handles edge deletion with edge having numeric GUID', async () => {
    const mockRemoveLocalEdge = jest.fn();
    const edgeWithNumericGUID: EdgeType = {
      ...defaultProps.selectedEdge!,
      GUID: '12345',
    };

    const props = {
      ...defaultProps,
      selectedEdge: edgeWithNumericGUID,
      removeLocalEdge: mockRemoveLocalEdge,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockRemoveLocalEdge).toHaveBeenCalledWith('12345');
      expect(mockToast.success).toHaveBeenCalledWith('Delete Edge successfully');
    });
  });

  it('handles edge deletion with edge having empty GUID', async () => {
    const mockRemoveLocalEdge = jest.fn();
    const edgeWithEmptyGUID: EdgeType = {
      ...defaultProps.selectedEdge!,
      GUID: '',
    };

    const props = {
      ...defaultProps,
      selectedEdge: edgeWithEmptyGUID,
      removeLocalEdge: mockRemoveLocalEdge,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockRemoveLocalEdge).toHaveBeenCalledWith('');
      expect(mockToast.success).toHaveBeenCalledWith('Delete Edge successfully');
    });
  });

  it('handles edge deletion with edge having whitespace-only GUID', async () => {
    const mockRemoveLocalEdge = jest.fn();
    const edgeWithWhitespaceGUID: EdgeType = {
      ...defaultProps.selectedEdge!,
      GUID: '   ',
    };

    const props = {
      ...defaultProps,
      selectedEdge: edgeWithWhitespaceGUID,
      removeLocalEdge: mockRemoveLocalEdge,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockRemoveLocalEdge).toHaveBeenCalledWith('   ');
      expect(mockToast.success).toHaveBeenCalledWith('Delete Edge successfully');
    });
  });

  it('handles edge deletion with edge having very long GUID', async () => {
    const mockRemoveLocalEdge = jest.fn();
    const longGUID = 'a'.repeat(1000);
    const edgeWithLongGUID: EdgeType = {
      ...defaultProps.selectedEdge!,
      GUID: longGUID,
    };

    const props = {
      ...defaultProps,
      selectedEdge: edgeWithLongGUID,
      removeLocalEdge: mockRemoveLocalEdge,
    };

    render(<DeleteEdge {...props} />);

    const confirmButton = screen.getByTestId('confirm-button');
    fireEvent.click(confirmButton);

    await waitFor(() => {
      expect(mockRemoveLocalEdge).toHaveBeenCalledWith(longGUID);
      expect(mockToast.success).toHaveBeenCalledWith('Delete Edge successfully');
    });
  });
});
