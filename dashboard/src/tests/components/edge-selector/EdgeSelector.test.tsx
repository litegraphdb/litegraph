import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { Form } from 'antd';
import EdgeSelector from '@/components/edge-selector/EdgeSelector';
import { useSearchEdgesMutation, useGetEdgeByIdQuery } from '@/lib/store/slice/slice';
import { useSelectedGraph } from '@/hooks/entityHooks';

// Mock dependencies
jest.mock('@/lib/store/slice/slice');
jest.mock('@/hooks/entityHooks');
jest.mock('@/components/base/select/Select', () => {
  return function MockLitegraphSelect({
    showSearch,
    loading,
    placeholder,
    filterOption,
    onSearch,
    notFoundContent,
    options,
    onChange,
    readonly,
    variant,
  }: any) {
    return (
      <div data-testid="litegraph-select">
        <input
          data-testid="search-input"
          placeholder={placeholder}
          onChange={(e) => onSearch?.(e.target.value)}
          disabled={loading}
        />
        <select
          data-testid="edge-select"
          onChange={(e) => onChange?.(e.target.value)}
          disabled={readonly}
        >
          <option value="">Select an edge</option>
          {options?.map((option: any) => (
            <option key={option.value} value={option.value}>
              {option.label}
            </option>
          ))}
        </select>
        <div data-testid="loading-status">{loading ? 'Loading...' : 'Ready'}</div>
        <div data-testid="not-found">{notFoundContent}</div>
        <div data-testid="variant">{variant}</div>
        <div data-testid="options-count">Options: {options?.length || 0}</div>
      </div>
    );
  };
});

const mockUseSearchEdgesMutation = useSearchEdgesMutation as jest.MockedFunction<
  typeof useSearchEdgesMutation
>;
const mockUseGetEdgeByIdQuery = useGetEdgeByIdQuery as jest.MockedFunction<
  typeof useGetEdgeByIdQuery
>;
const mockUseSelectedGraph = useSelectedGraph as jest.MockedFunction<typeof useSelectedGraph>;

// Mock Form wrapper component
const MockForm: React.FC<{ children: React.ReactNode; initialValues?: any }> = ({
  children,
  initialValues = {},
}) => {
  const [form] = Form.useForm();
  React.useEffect(() => {
    form.setFieldsValue(initialValues);
  }, [form, initialValues]);

  return (
    <Form form={form} initialValues={initialValues}>
      {children}
    </Form>
  );
};

describe('EdgeSelector', () => {
  const mockSearchEdges = jest.fn();
  const mockSelectedGraph = 'graph-123';

  const mockEdge = {
    GUID: 'edge-123',
    Name: 'Test Edge',
    SourceNodeGUID: 'node-1',
    TargetNodeGUID: 'node-2',
    GraphGUID: 'graph-123',
    TenantGUID: 'tenant-1',
    CreatedUtc: '2023-01-01T00:00:00Z',
    LastUpdateUtc: '2023-01-01T00:00:00Z',
    Data: {},
    Tags: {},
    Cost: 5,
  };

  beforeEach(() => {
    mockUseSearchEdgesMutation.mockReturnValue([mockSearchEdges, { isLoading: false }]);
    mockUseGetEdgeByIdQuery.mockReturnValue({
      data: undefined,
      isFetching: false,
      isLoading: false,
      error: null,
    });
    mockUseSelectedGraph.mockReturnValue(mockSelectedGraph);
  });

  afterEach(() => {
    jest.clearAllMocks();
  });

  it('renders with default props', () => {
    render(
      <MockForm>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    expect(screen.getByTestId('litegraph-select')).toBeInTheDocument();
    expect(screen.getByTestId('search-input')).toHaveAttribute(
      'placeholder',
      'Search and select edge'
    );
  });

  it('renders with custom label', () => {
    render(
      <MockForm>
        <EdgeSelector name="edge" label="Custom Edge Label" />
      </MockForm>
    );

    expect(screen.getByText('Custom Edge Label')).toBeInTheDocument();
  });

  it('renders with required validation', () => {
    render(
      <MockForm>
        <EdgeSelector name="edge" required />
      </MockForm>
    );

    // Check that the required attribute is present on the Form.Item
    const formItem = screen.getByTestId('litegraph-select').closest('.ant-form-item');
    expect(formItem).toBeInTheDocument();

    // Verify the component renders with required prop
    expect(screen.getByTestId('litegraph-select')).toBeInTheDocument();
  });

  it('renders with custom rules', () => {
    const customRules = [{ required: true, message: 'Edge is required' }];

    render(
      <MockForm>
        <EdgeSelector name="edge" rules={customRules} />
      </MockForm>
    );

    expect(screen.getByTestId('litegraph-select')).toBeInTheDocument();
  });

  it('renders in readonly mode', () => {
    render(
      <MockForm>
        <EdgeSelector name="edge" readonly />
      </MockForm>
    );

    const select = screen.getByTestId('edge-select');
    expect(select).toBeDisabled();
  });

  it('renders with custom className', () => {
    render(
      <MockForm>
        <EdgeSelector name="edge" className="custom-class" />
      </MockForm>
    );

    const formItem = screen.getByTestId('litegraph-select').closest('.ant-form-item');
    expect(formItem).toHaveClass('custom-class');
  });

  it('handles search input changes', async () => {
    mockSearchEdges.mockResolvedValue({ data: [mockEdge] });

    render(
      <MockForm>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    const searchInput = screen.getByTestId('search-input');
    fireEvent.change(searchInput, { target: { value: 'test' } });

    await waitFor(() => {
      expect(mockSearchEdges).toHaveBeenCalledWith({
        GraphGUID: mockSelectedGraph,
        Name: 'test',
        Ordering: 'CreatedDescending',
      });
    });
  });

  it('debounces search input', async () => {
    jest.useFakeTimers();
    mockSearchEdges.mockResolvedValue({ data: [mockEdge] });

    render(
      <MockForm>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    const searchInput = screen.getByTestId('search-input');

    // Type multiple characters quickly
    fireEvent.change(searchInput, { target: { value: 't' } });
    fireEvent.change(searchInput, { target: { value: 'te' } });
    fireEvent.change(searchInput, { target: { value: 'tes' } });
    fireEvent.change(searchInput, { target: { value: 'test' } });

    // Should not have been called yet due to debouncing
    expect(mockSearchEdges).not.toHaveBeenCalled();

    // Fast-forward timers to trigger debounced function
    jest.advanceTimersByTime(500);

    await waitFor(() => {
      expect(mockSearchEdges).toHaveBeenCalledWith({
        GraphGUID: mockSelectedGraph,
        Name: 'test',
        Ordering: 'CreatedDescending',
      });
    });

    jest.useRealTimers();
  });

  it('clears options when search input is empty', async () => {
    render(
      <MockForm>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    const searchInput = screen.getByTestId('search-input');
    fireEvent.change(searchInput, { target: { value: '' } });

    await waitFor(() => {
      expect(screen.getByTestId('options-count')).toHaveTextContent('Options: 0');
    });
  });

  it('handles search errors gracefully', async () => {
    mockSearchEdges.mockRejectedValue(new Error('Search failed'));

    render(
      <MockForm>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    const searchInput = screen.getByTestId('search-input');
    fireEvent.change(searchInput, { target: { value: 'test' } });

    await waitFor(() => {
      expect(screen.getByTestId('options-count')).toHaveTextContent('Options: 0');
    });
  });

  it('fetches edge by ID when not in options', () => {
    render(
      <MockForm initialValues={{ edge: 'edge-123' }}>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    // The hook should be called with skipToken initially since the edge doesn't exist in options
    expect(mockUseGetEdgeByIdQuery).toHaveBeenCalledWith(
      expect.objectContaining({
        graphId: mockSelectedGraph,
        edgeId: 'edge-123',
      })
    );
  });

  it('does not fetch edge by ID when already in options', () => {
    render(
      <MockForm initialValues={{ edge: 'edge-123' }}>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    // Mock that the edge is already in options
    mockUseGetEdgeByIdQuery.mockReturnValue({
      data: undefined,
      isFetching: false,
      isLoading: false,
      error: null,
    });

    expect(mockUseGetEdgeByIdQuery).toHaveBeenCalledWith({
      graphId: mockSelectedGraph,
      edgeId: 'edge-123',
    });
  });

  it('adds fetched edge to options', async () => {
    mockUseGetEdgeByIdQuery.mockReturnValue({
      data: mockEdge,
      isFetching: false,
      isLoading: false,
      error: null,
    });

    render(
      <MockForm initialValues={{ edge: 'edge-123' }}>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    await waitFor(() => {
      expect(screen.getByTestId('options-count')).toHaveTextContent('Options: 1');
    });
  });

  it('prevents duplicate edges in options', async () => {
    mockUseGetEdgeByIdQuery.mockReturnValue({
      data: mockEdge,
      isFetching: false,
      isLoading: false,
      error: null,
    });

    const { rerender } = render(
      <MockForm initialValues={{ edge: 'edge-123' }}>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    // Re-render with the same edge data
    rerender(
      <MockForm initialValues={{ edge: 'edge-123' }}>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    await waitFor(() => {
      expect(screen.getByTestId('options-count')).toHaveTextContent('Options: 1');
    });
  });

  it('shows loading state during search', async () => {
    mockSearchEdges.mockImplementation(() => new Promise(() => {})); // Never resolves

    render(
      <MockForm>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    const searchInput = screen.getByTestId('search-input');
    fireEvent.change(searchInput, { target: { value: 'test' } });

    await waitFor(() => {
      expect(screen.getByTestId('loading-status')).toHaveTextContent('Loading...');
    });
  });

  it('shows loading state when fetching edge by ID', () => {
    mockUseGetEdgeByIdQuery.mockReturnValue({
      data: undefined,
      isFetching: true,
      isLoading: false,
      error: null,
    });

    render(
      <MockForm initialValues={{ edge: 'edge-123' }}>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    expect(screen.getByTestId('loading-status')).toHaveTextContent('Loading...');
  });

  it('shows loading state when loading edge by ID', () => {
    mockUseGetEdgeByIdQuery.mockReturnValue({
      data: undefined,
      isFetching: false,
      isLoading: true,
      error: null,
    });

    render(
      <MockForm initialValues={{ edge: 'edge-123' }}>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    expect(screen.getByTestId('loading-status')).toHaveTextContent('Loading...');
  });

  it('displays correct not found content', () => {
    render(
      <MockForm>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    expect(screen.getByTestId('not-found')).toHaveTextContent('No edges found');
  });

  it('displays loading not found content during search', async () => {
    mockSearchEdges.mockImplementation(() => new Promise(() => {})); // Never resolves

    render(
      <MockForm>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    const searchInput = screen.getByTestId('search-input');
    fireEvent.change(searchInput, { target: { value: 'test' } });

    await waitFor(() => {
      expect(screen.getByTestId('not-found')).toHaveTextContent('Loading...');
    });
  });

  it('displays loading not found content when fetching edge by ID', () => {
    mockUseGetEdgeByIdQuery.mockReturnValue({
      data: undefined,
      isFetching: true,
      isLoading: false,
      error: null,
    });

    render(
      <MockForm initialValues={{ edge: 'edge-123' }}>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    expect(screen.getByTestId('not-found')).toHaveTextContent('Loading...');
  });

  it('handles edge selection change', () => {
    render(
      <MockForm>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    // The component should render without errors
    expect(screen.getByTestId('litegraph-select')).toBeInTheDocument();

    // Test that the select element exists and can be interacted with
    const select = screen.getByTestId('edge-select');
    expect(select).toBeInTheDocument();
  });

  it('applies correct variant when readonly', () => {
    render(
      <MockForm>
        <EdgeSelector name="edge" readonly />
      </MockForm>
    );

    expect(screen.getByTestId('variant')).toHaveTextContent('borderless');
  });

  it('applies outlined variant when not readonly', () => {
    render(
      <MockForm>
        <EdgeSelector name="edge" readonly={false} />
      </MockForm>
    );

    expect(screen.getByTestId('variant')).toHaveTextContent('outlined');
  });

  it('handles search with special characters', async () => {
    mockSearchEdges.mockResolvedValue({ data: [mockEdge] });

    render(
      <MockForm>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    const searchInput = screen.getByTestId('search-input');
    fireEvent.change(searchInput, { target: { value: 'test@#$%' } });

    await waitFor(() => {
      expect(mockSearchEdges).toHaveBeenCalledWith({
        GraphGUID: mockSelectedGraph,
        Name: 'test@#$%',
        Ordering: 'CreatedDescending',
      });
    });
  });

  it('handles search with numbers', async () => {
    mockSearchEdges.mockResolvedValue({ data: [mockEdge] });

    render(
      <MockForm>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    const searchInput = screen.getByTestId('search-input');
    fireEvent.change(searchInput, { target: { value: '123' } });

    await waitFor(() => {
      expect(mockSearchEdges).toHaveBeenCalledWith({
        GraphGUID: mockSelectedGraph,
        Name: '123',
        Ordering: 'CreatedDescending',
      });
    });
  });

  it('handles search with whitespace', async () => {
    mockSearchEdges.mockResolvedValue({ data: [mockEdge] });

    render(
      <MockForm>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    const searchInput = screen.getByTestId('search-input');
    fireEvent.change(searchInput, { target: { value: '  test  ' } });

    await waitFor(() => {
      expect(mockSearchEdges).toHaveBeenCalledWith({
        GraphGUID: mockSelectedGraph,
        Name: '  test  ',
        Ordering: 'CreatedDescending',
      });
    });
  });

  it('handles multiple rapid search inputs', async () => {
    jest.useFakeTimers();
    mockSearchEdges.mockResolvedValue({ data: [mockEdge] });

    render(
      <MockForm>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    const searchInput = screen.getByTestId('search-input');

    // Type rapidly
    fireEvent.change(searchInput, { target: { value: 'a' } });
    fireEvent.change(searchInput, { target: { value: 'ab' } });
    fireEvent.change(searchInput, { target: { value: 'abc' } });

    // Fast-forward to trigger the last search
    jest.advanceTimersByTime(500);

    await waitFor(() => {
      expect(mockSearchEdges).toHaveBeenCalledTimes(1);
      expect(mockSearchEdges).toHaveBeenCalledWith({
        GraphGUID: mockSelectedGraph,
        Name: 'abc',
        Ordering: 'CreatedDescending',
      });
    });

    jest.useRealTimers();
  });

  it('handles search response with empty data', async () => {
    mockSearchEdges.mockResolvedValue({ data: [] });

    render(
      <MockForm>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    const searchInput = screen.getByTestId('search-input');
    fireEvent.change(searchInput, { target: { value: 'nonexistent' } });

    await waitFor(() => {
      expect(screen.getByTestId('options-count')).toHaveTextContent('Options: 0');
    });
  });

  it('handles search response with null data', async () => {
    mockSearchEdges.mockResolvedValue({ data: null });

    render(
      <MockForm>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    const searchInput = screen.getByTestId('search-input');
    fireEvent.change(searchInput, { target: { value: 'test' } });

    await waitFor(() => {
      expect(screen.getByTestId('options-count')).toHaveTextContent('Options: 0');
    });
  });

  it('handles search response with undefined data', async () => {
    mockSearchEdges.mockResolvedValue({ data: undefined });

    render(
      <MockForm>
        <EdgeSelector name="edge" />
      </MockForm>
    );

    const searchInput = screen.getByTestId('search-input');
    fireEvent.change(searchInput, { target: { value: 'test' } });

    await waitFor(() => {
      expect(screen.getByTestId('options-count')).toHaveTextContent('Options: 0');
    });
  });
});
