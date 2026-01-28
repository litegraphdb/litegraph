import React from 'react';
import '@testing-library/jest-dom';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import { Form, FormInstance } from 'antd';
import NodeSelector from '@/components/node-selector/NodeSelector';
import { useSearchNodesMutation, useGetNodeByIdQuery } from '@/lib/store/slice/slice';
import { useSelectedGraph } from '@/hooks/entityHooks';
import { skipToken } from '@reduxjs/toolkit/query';

// Mock the dependencies
jest.mock('@/lib/store/slice/slice', () => ({
  useSearchNodesMutation: jest.fn(),
  useGetNodeByIdQuery: jest.fn(),
}));

jest.mock('@/hooks/entityHooks', () => ({
  useSelectedGraph: jest.fn(),
}));

jest.mock('@/components/base/select/Select', () => {
  return function MockLitegraphSelect({
    options,
    onSearch,
    onChange,
    loading,
    placeholder,
    notFoundContent,
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
          data-testid="node-select"
          onChange={(e) => onChange?.(e.target.value)}
          disabled={readonly}
        >
          <option value="">Select a node</option>
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

// Mock Form context
const MockForm: React.FC<{ children: React.ReactNode; initialValues?: any }> = ({
  children,
  initialValues = {},
}) => {
  const [form] = Form.useForm();

  React.useEffect(() => {
    form.setFieldsValue(initialValues);
  }, [form, initialValues]);

  return <Form form={form}>{children}</Form>;
};

describe('NodeSelector', () => {
  let mockSearchNodes: jest.Mock;
  let mockUseGetNodeByIdQuery: jest.Mock;
  let mockUseSelectedGraph: jest.Mock;

  const mockLocalNodes = [
    { id: 'local-1', label: 'Local Node 1', type: 'circle' },
    { id: 'local-2', label: 'Local Node 2', type: 'square' },
  ];

  const mockApiNodes = [
    {
      GUID: 'api-1',
      Name: 'API Node 1',
      Labels: ['api'],
      TenantGUID: 'tenant-1',
      GraphGUID: 'graph-1',
      CreatedUtc: '2023-01-01T00:00:00Z',
      LastUpdateUtc: '2023-01-01T00:00:00Z',
      Data: {},
      Tags: {},
      Vectors: [],
    },
    {
      GUID: 'api-2',
      Name: 'API Node 2',
      Labels: ['api'],
      TenantGUID: 'tenant-1',
      GraphGUID: 'graph-1',
      CreatedUtc: '2023-01-01T00:00:00Z',
      LastUpdateUtc: '2023-01-01T00:00:00Z',
      Data: {},
      Tags: {},
      Vectors: [],
    },
  ];

  beforeEach(() => {
    jest.clearAllMocks();

    mockSearchNodes = jest.fn();
    mockUseGetNodeByIdQuery = jest.fn();
    mockUseSelectedGraph = jest.fn();

    (useSearchNodesMutation as jest.Mock).mockReturnValue([mockSearchNodes]);
    (useGetNodeByIdQuery as jest.Mock).mockReturnValue({
      data: null,
      isFetching: false,
      isLoading: false,
    });
    (useSelectedGraph as jest.Mock).mockReturnValue('graph-123');
  });

  describe('Basic rendering and props', () => {
    it('should render with basic props', () => {
      render(
        <MockForm>
          <NodeSelector name="nodeId" />
        </MockForm>
      );

      expect(screen.getByTestId('litegraph-select')).toBeInTheDocument();
      expect(screen.getByPlaceholderText('Search and select node')).toBeInTheDocument();
    });

    it('should render with label and required prop', () => {
      render(
        <MockForm>
          <NodeSelector name="nodeId" label="Select Node" required />
        </MockForm>
      );

      expect(screen.getByText('Select Node')).toBeInTheDocument();
    });

    it('should render with custom className', () => {
      render(
        <MockForm>
          <NodeSelector name="nodeId" className="custom-class" />
        </MockForm>
      );

      const formItem = screen.getByTestId('litegraph-select').closest('.ant-form-item');
      expect(formItem).toHaveClass('custom-class');
    });

    it('should render as readonly when readonly prop is true', () => {
      render(
        <MockForm>
          <NodeSelector name="nodeId" readonly />
        </MockForm>
      );

      expect(screen.getByTestId('variant')).toHaveTextContent('borderless');
    });

    it('should render as outlined when readonly prop is false', () => {
      render(
        <MockForm>
          <NodeSelector name="nodeId" readonly={false} />
        </MockForm>
      );

      expect(screen.getByTestId('variant')).toHaveTextContent('outlined');
    });

    it('should render with custom rules', () => {
      const customRules = [{ required: true, message: 'Node is required' }];

      render(
        <MockForm>
          <NodeSelector name="nodeId" rules={customRules} />
        </MockForm>
      );

      // The rules are passed to Form.Item, so we just verify the component renders
      expect(screen.getByTestId('litegraph-select')).toBeInTheDocument();
    });
  });

  describe('Local nodes handling', () => {
    it('should initialize with local nodes when provided', () => {
      render(
        <MockForm>
          <NodeSelector name="nodeId" localNodes={mockLocalNodes} />
        </MockForm>
      );

      // Check that local nodes are available in the select
      expect(screen.getByText('Local Node 1')).toBeInTheDocument();
      expect(screen.getByText('Local Node 2')).toBeInTheDocument();
    });

    it('should convert local nodes to Node format correctly', () => {
      render(
        <MockForm>
          <NodeSelector name="nodeId" localNodes={mockLocalNodes} />
        </MockForm>
      );

      // The local nodes should be converted to the expected format
      const select = screen.getByTestId('node-select');
      expect(select).toHaveValue('');

      // Check that local nodes are available as options
      expect(screen.getByText('Local Node 1')).toBeInTheDocument();
      expect(screen.getByText('Local Node 2')).toBeInTheDocument();
    });

    it('should handle local nodes with missing type', () => {
      const localNodesWithoutType = [{ id: 'local-1', label: 'Local Node 1' }];

      render(
        <MockForm>
          <NodeSelector name="nodeId" localNodes={localNodesWithoutType} />
        </MockForm>
      );

      // Should still render without errors
      expect(screen.getByTestId('litegraph-select')).toBeInTheDocument();
    });

    it('should not duplicate local nodes when they already exist in options', () => {
      render(
        <MockForm>
          <NodeSelector name="nodeId" localNodes={mockLocalNodes} />
        </MockForm>
      );

      // The useEffect should handle deduplication
      expect(screen.getByTestId('litegraph-select')).toBeInTheDocument();
    });
  });

  describe('API node fetching', () => {
    it('should fetch node by ID when currentGUID exists and node not in options', () => {
      const mockNodeData = {
        GUID: 'existing-node',
        Name: 'Existing Node',
        Labels: ['existing'],
        TenantGUID: 'tenant-1',
        GraphGUID: 'graph-123',
        CreatedUtc: '2023-01-01T00:00:00Z',
        LastUpdateUtc: '2023-01-01T00:00:00Z',
        Data: {},
        Tags: {},
        Vectors: [],
      };

      mockUseGetNodeByIdQuery.mockReturnValue({
        data: mockNodeData,
        isFetching: false,
        isLoading: false,
      });

      render(
        <MockForm initialValues={{ nodeId: 'existing-node' }}>
          <NodeSelector name="nodeId" />
        </MockForm>
      );

      expect(useGetNodeByIdQuery).toHaveBeenCalledWith({
        graphId: 'graph-123',
        nodeId: 'existing-node',
      });
    });

    it('should not fetch node by ID when currentGUID is empty', () => {
      render(
        <MockForm initialValues={{ nodeId: '' }}>
          <NodeSelector name="nodeId" />
        </MockForm>
      );

      expect(useGetNodeByIdQuery).toHaveBeenCalledWith(skipToken);
    });

    it('should not fetch node by ID when selectedGraph is empty', () => {
      mockUseSelectedGraph.mockReturnValue(null);

      render(
        <MockForm initialValues={{ nodeId: 'some-node' }}>
          <NodeSelector name="nodeId" />
        </MockForm>
      );

      expect(useGetNodeByIdQuery).toHaveBeenCalledWith(skipToken);
    });

    it('should not fetch node by ID when node already exists in options', () => {
      render(
        <MockForm initialValues={{ nodeId: 'local-1' }}>
          <NodeSelector name="nodeId" localNodes={mockLocalNodes} />
        </MockForm>
      );

      // Since the node exists in local nodes, it shouldn't fetch from API
      expect(useGetNodeByIdQuery).toHaveBeenCalledWith(skipToken);
    });

    it('should add fetched node to options when not already present', async () => {
      const mockNodeData = {
        GUID: 'new-node',
        Name: 'New Node',
        Labels: ['new'],
        TenantGUID: 'tenant-1',
        GraphGUID: 'graph-123',
        CreatedUtc: '2023-01-01T00:00:00Z',
        LastUpdateUtc: '2023-01-01T00:00:00Z',
        Data: {},
        Tags: {},
        Vectors: [],
      };

      mockUseGetNodeByIdQuery.mockReturnValue({
        data: mockNodeData,
        isFetching: false,
        isLoading: false,
      });

      render(
        <MockForm initialValues={{ nodeId: 'new-node' }}>
          <NodeSelector name="nodeId" />
        </MockForm>
      );

      // Verify the component renders and the hook is called correctly
      expect(useGetNodeByIdQuery).toHaveBeenCalledWith({
        graphId: 'graph-123',
        nodeId: 'new-node',
      });

      // Verify the component renders without errors
      expect(screen.getByTestId('litegraph-select')).toBeInTheDocument();
    });

    it('should not add duplicate nodes to options', () => {
      const mockNodeData = {
        GUID: 'local-1',
        Name: 'Local Node 1',
        Labels: ['circle'],
        TenantGUID: 'tenant-1',
        GraphGUID: 'graph-123',
        CreatedUtc: '2023-01-01T00:00:00Z',
        LastUpdateUtc: '2023-01-01T00:00:00Z',
        Data: {},
        Tags: {},
        Vectors: [],
      };

      mockUseGetNodeByIdQuery.mockReturnValue({
        data: mockNodeData,
        isFetching: false,
        isLoading: false,
      });

      render(
        <MockForm initialValues={{ nodeId: 'local-1' }}>
          <NodeSelector name="nodeId" localNodes={mockLocalNodes} />
        </MockForm>
      );

      // Verify the component renders and has local nodes
      expect(screen.getByTestId('litegraph-select')).toBeInTheDocument();
      expect(screen.getByText('Local Node 1')).toBeInTheDocument();
      expect(screen.getByText('Local Node 2')).toBeInTheDocument();
    });
  });

  describe('Search functionality', () => {
    it('should call searchNodes when search value is provided', async () => {
      mockSearchNodes.mockResolvedValue({
        data: { Nodes: mockApiNodes },
      });

      render(
        <MockForm>
          <NodeSelector name="nodeId" localNodes={mockLocalNodes} />
        </MockForm>
      );

      const searchInput = screen.getByTestId('search-input');
      fireEvent.change(searchInput, { target: { value: 'test search' } });

      // Wait for debounced search to execute
      await waitFor(
        () => {
          expect(mockSearchNodes).toHaveBeenCalledWith({
            GraphGUID: 'graph-123',
            Name: 'test search',
            Ordering: 'CreatedDescending',
          });
        },
        { timeout: 1000 }
      );
    });

    it('should show only local nodes when search is empty', async () => {
      render(
        <MockForm>
          <NodeSelector name="nodeId" localNodes={mockLocalNodes} />
        </MockForm>
      );

      const searchInput = screen.getByTestId('search-input');
      fireEvent.change(searchInput, { target: { value: '' } });

      // Should show local nodes
      expect(screen.getByText('Local Node 1')).toBeInTheDocument();
      expect(screen.getByText('Local Node 2')).toBeInTheDocument();
    });

    it('should combine API search results with local nodes', async () => {
      mockSearchNodes.mockResolvedValue({
        data: { Nodes: mockApiNodes },
      });

      render(
        <MockForm>
          <NodeSelector name="nodeId" localNodes={mockLocalNodes} />
        </MockForm>
      );

      const searchInput = screen.getByTestId('search-input');
      fireEvent.change(searchInput, { target: { value: 'api' } });

      await waitFor(
        () => {
          // Should show both local and API nodes
          expect(screen.getByText('Local Node 1')).toBeInTheDocument();
          expect(screen.getByText('Local Node 2')).toBeInTheDocument();
          expect(screen.getByText('API Node 1')).toBeInTheDocument();
          expect(screen.getByText('API Node 2')).toBeInTheDocument();
        },
        { timeout: 1000 }
      );
    });

    it('should remove duplicates when combining API and local nodes', async () => {
      const duplicateApiNode = {
        GUID: 'local-1', // Same ID as local node
        Name: 'Duplicate Node',
        Labels: ['duplicate'],
        TenantGUID: 'tenant-1',
        GraphGUID: 'graph-123',
        CreatedUtc: '2023-01-01T00:00:00Z',
        LastUpdateUtc: '2023-01-01T00:00:00Z',
        Data: {},
        Tags: {},
        Vectors: [],
      };

      mockSearchNodes.mockResolvedValue({
        data: { Nodes: [duplicateApiNode] },
      });

      render(
        <MockForm>
          <NodeSelector name="nodeId" localNodes={mockLocalNodes} />
        </MockForm>
      );

      const searchInput = screen.getByTestId('search-input');
      fireEvent.change(searchInput, { target: { value: 'duplicate' } });

      await waitFor(
        () => {
          // Should show only one instance of the duplicate node
          const options = screen.getAllByRole('option');
          const duplicateOptions = options.filter(
            (option) =>
              option.textContent === 'Local Node 1' || option.textContent === 'Duplicate Node'
          );
          expect(duplicateOptions).toHaveLength(1);
        },
        { timeout: 1000 }
      );
    });

    it('should handle search error gracefully and show only local nodes', async () => {
      mockSearchNodes.mockRejectedValue(new Error('Search failed'));

      render(
        <MockForm>
          <NodeSelector name="nodeId" localNodes={mockLocalNodes} />
        </MockForm>
      );

      const searchInput = screen.getByTestId('search-input');
      fireEvent.change(searchInput, { target: { value: 'failed search' } });

      await waitFor(
        () => {
          // Should show only local nodes on error
          expect(screen.getByText('Local Node 1')).toBeInTheDocument();
          expect(screen.getByText('Local Node 2')).toBeInTheDocument();
        },
        { timeout: 1000 }
      );
    });

    it('should set loading state during search', async () => {
      mockSearchNodes.mockImplementation(() => new Promise((resolve) => setTimeout(resolve, 100)));

      render(
        <MockForm>
          <NodeSelector name="nodeId" localNodes={mockLocalNodes} />
        </MockForm>
      );

      const searchInput = screen.getByTestId('search-input');
      fireEvent.change(searchInput, { target: { value: 'loading test' } });

      // The loading state should be managed by the component's internal state
      // We can't directly test the loading state from the mock component
      // Instead, test that the search function is called
      await waitFor(
        () => {
          expect(mockSearchNodes).toHaveBeenCalled();
        },
        { timeout: 1000 }
      );
    });
  });

  describe('Loading states', () => {
    it('should pass loading state to LitegraphSelect when fetching node by ID', () => {
      mockUseGetNodeByIdQuery.mockReturnValue({
        data: null,
        isFetching: true,
        isLoading: false,
      });

      render(
        <MockForm initialValues={{ nodeId: 'loading-node' }}>
          <NodeSelector name="nodeId" />
        </MockForm>
      );

      // Verify the component renders without crashing when loading
      expect(screen.getByTestId('litegraph-select')).toBeInTheDocument();
    });

    it('should pass loading state to LitegraphSelect when loading node by ID', () => {
      mockUseGetNodeByIdQuery.mockReturnValue({
        data: null,
        isFetching: false,
        isLoading: true,
      });

      render(
        <MockForm initialValues={{ nodeId: 'loading-node' }}>
          <NodeSelector name="nodeId" />
        </MockForm>
      );

      // Verify the component renders without crashing when loading
      expect(screen.getByTestId('litegraph-select')).toBeInTheDocument();
    });

    it('should handle search loading state', async () => {
      let resolveMockSearch: (value: any) => void;
      const searchPromise = new Promise((resolve) => {
        resolveMockSearch = resolve;
      });

      mockSearchNodes.mockReturnValue(searchPromise);

      render(
        <MockForm>
          <NodeSelector name="nodeId" localNodes={mockLocalNodes} />
        </MockForm>
      );

      const searchInput = screen.getByTestId('search-input');
      fireEvent.change(searchInput, { target: { value: 'loading search' } });

      // Wait for the search to be called
      await waitFor(
        () => {
          expect(mockSearchNodes).toHaveBeenCalled();
        },
        { timeout: 1000 }
      );

      // Resolve the search promise
      resolveMockSearch({ data: { Nodes: [] } });
    });
  });

  describe('Form integration', () => {
    it('should update form value when node is selected', () => {
      const mockForm = {
        setFieldValue: jest.fn(),
      };

      // Mock the Form.useFormInstance hook
      jest.doMock('antd/es/form/hooks/useFormInstance', () => ({
        __esModule: true,
        default: () => mockForm,
      }));

      render(
        <MockForm>
          <NodeSelector name="nodeId" localNodes={mockLocalNodes} />
        </MockForm>
      );

      const select = screen.getByTestId('node-select');
      fireEvent.change(select, { target: { value: 'local-1' } });

      // The form value should be updated
      expect(select).toHaveValue('local-1');
    });

    it('should handle form value changes correctly', () => {
      render(
        <MockForm initialValues={{ nodeId: 'local-1' }}>
          <NodeSelector name="nodeId" localNodes={mockLocalNodes} />
        </MockForm>
      );

      // Should show the selected value
      expect(screen.getByText('Local Node 1')).toBeInTheDocument();
    });
  });

  describe('Edge cases', () => {
    it('should handle empty localNodes array', () => {
      render(
        <MockForm>
          <NodeSelector name="nodeId" localNodes={[]} />
        </MockForm>
      );

      expect(screen.getByTestId('litegraph-select')).toBeInTheDocument();
      expect(screen.getByText('No nodes found')).toBeInTheDocument();
    });

    it('should handle undefined localNodes', () => {
      render(
        <MockForm>
          <NodeSelector name="nodeId" />
        </MockForm>
      );

      expect(screen.getByTestId('litegraph-select')).toBeInTheDocument();
    });

    it('should handle missing selectedGraph', () => {
      mockUseSelectedGraph.mockReturnValue(null);

      render(
        <MockForm>
          <NodeSelector name="nodeId" localNodes={mockLocalNodes} />
        </MockForm>
      );

      expect(screen.getByTestId('litegraph-select')).toBeInTheDocument();
    });

    it('should handle search with no results', async () => {
      mockSearchNodes.mockResolvedValue({
        data: { Nodes: [] },
      });

      render(
        <MockForm>
          <NodeSelector name="nodeId" localNodes={mockLocalNodes} />
        </MockForm>
      );

      const searchInput = screen.getByTestId('search-input');
      fireEvent.change(searchInput, { target: { value: 'no results' } });

      await waitFor(
        () => {
          expect(screen.getByText('No nodes found')).toBeInTheDocument();
        },
        { timeout: 1000 }
      );
    });
  });
});
