import React from 'react';
import { screen } from '@testing-library/react';
import AddEditNode from '@/page/nodes/components/AddEditNode';
import { renderWithRedux } from '../../../store/utils';

const mockCreateNode = jest.fn();
const mockUpdateNode = jest.fn();
const mockGetNodeByIdQuery = jest.fn();
const mockGetGraphByIdQuery = jest.fn();

jest.mock('@/lib/store/slice/slice', () => ({
  useCreateNodeMutation: () => [mockCreateNode, { isLoading: false }],
  useUpdateNodeMutation: () => [mockUpdateNode, { isLoading: false }],
  useGetNodeByIdQuery: (...args: any[]) => mockGetNodeByIdQuery(...args),
  useGetGraphByIdQuery: (...args: any[]) => mockGetGraphByIdQuery(...args),
}));

jest.mock('react-hot-toast', () => ({
  success: jest.fn(),
  error: jest.fn(),
}));

jest.mock('uuid', () => ({
  v4: () => 'test-uuid',
}));

jest.mock('jsoneditor-react', () => ({
  JsonEditor: ({ value, onChange }: any) => (
    <input
      data-testid="json-editor-textarea"
      value={JSON.stringify(value)}
      onChange={(e) => onChange && onChange(JSON.parse(e.target.value))}
    />
  ),
}));

jest.mock('@/components/inputs/label-input/LabelInput', () => ({
  __esModule: true,
  default: ({ name, readonly }: any) => (
    <div data-testid="label-input" name={name}>
      <input data-testid="labels-input" readOnly={readonly} />
    </div>
  ),
}));

jest.mock('@/components/inputs/tags-input/TagsInput', () => ({
  __esModule: true,
  default: ({ name, readonly }: any) => (
    <div data-testid="tags-input" name={name}>
      <input data-testid="tags-input-field" readOnly={readonly} />
    </div>
  ),
}));

jest.mock('@/components/inputs/vectors-input.tsx/VectorsInput', () => ({
  __esModule: true,
  default: ({ name, readonly }: any) => (
    <div data-testid="vectors-input" name={name}>
      <input data-testid="vectors-input-field" readOnly={readonly} />
    </div>
  ),
}));

jest.mock('@/components/inputs/tags-input/utils', () => ({
  convertTagsToRecord: jest.fn(() => ({})),
}));

jest.mock('@/components/inputs/vectors-input.tsx/utils', () => ({
  convertVectorsToAPIRecord: jest.fn(() => []),
}));

jest.mock('@/utils/appUtils', () => ({
  getCreateEditViewModelTitle: jest.fn((type, loading, isNew, isEdit, isReadonly) => {
    if (loading) return 'Loading...';
    if (isReadonly) return 'View Node';
    if (isEdit) return 'Edit Node';
    return 'Create Node';
  }),
}));

const defaultProps = {
  isAddEditNodeVisible: true,
  setIsAddEditNodeVisible: jest.fn(),
  node: null,
  selectedGraph: 'graph1',
  onNodeUpdated: jest.fn(),
  readonly: false,
};

describe('AddEditNode', () => {
  beforeEach(() => {
    jest.clearAllMocks();
    mockGetNodeByIdQuery.mockReturnValue({
      data: null,
      isLoading: false,
      isFetching: false,
      refetch: jest.fn(),
    });
    mockGetGraphByIdQuery.mockReturnValue({ data: { Name: 'Test Graph' } });
  });

  it('renders readonly layout as always maximized with guid', () => {
    const readonlyNode = {
      GUID: 'readonly-node-id',
      Name: 'Readonly Node',
      GraphGUID: 'graph1',
      Data: {},
      Labels: ['entity'],
      Tags: {},
      Vectors: [],
    };
    mockGetNodeByIdQuery.mockReturnValue({
      data: readonlyNode,
      isLoading: false,
      isFetching: false,
      refetch: jest.fn(),
    });

    renderWithRedux(<AddEditNode {...defaultProps} node={readonlyNode as any} readonly={true} />);

    expect(screen.getByDisplayValue('readonly-node-id')).toBeInTheDocument();
    expect(screen.getByText('Close')).toBeInTheDocument();
    expect(screen.queryByRole('button', { name: 'Update' })).not.toBeInTheDocument();
    expect(screen.queryByText('Maximize')).not.toBeInTheDocument();
    expect(screen.queryByText('Restore')).not.toBeInTheDocument();
    expect(document.querySelector('.ant-modal')?.getAttribute('style')).toContain('95vw');
    expect(screen.queryByTestId('label-input')).not.toBeInTheDocument();
    expect(screen.getByTestId('node-label-badges')).toBeInTheDocument();
    expect(screen.getByText('entity')).toBeInTheDocument();

    const summaryGrid = screen.getByTestId('node-view-summary-grid');
    expect(summaryGrid.className).toContain('summaryGridExpanded');
  });
});
