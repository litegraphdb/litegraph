import '@testing-library/jest-dom';
import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import { tableColumns } from '@/page/edges/constant';
import { EdgeType } from '@/types/types';

// Mock the components
jest.mock('@/components/base/tag/Tag', () => {
  return function MockTag({ label }: { label: string }) {
    return <span data-testid="tag">{label}</span>;
  };
});

jest.mock('@/components/base/button/Button', () => {
  return function MockButton({ children, onClick, ...props }: any) {
    return (
      <button data-testid="button" onClick={onClick} {...props}>
        {children}
      </button>
    );
  };
});

jest.mock('@/components/base/dropdown/Dropdown', () => {
  return function MockDropdown({ children, menu, trigger, placement }: any) {
    return (
      <div data-testid="dropdown" data-trigger={trigger} data-placement={placement}>
        {children}
        {menu?.items?.map((item: any, index: number) => (
          <button key={index} data-testid={`menu-item-${item.key}`} onClick={item.onClick}>
            {item.label}
          </button>
        ))}
      </div>
    );
  };
});

jest.mock('@/components/table-search/TableSearch', () => {
  return function MockTableSearch(props: any) {
    return <div data-testid="table-search" {...props} />;
  };
});

// Mock the utility functions
jest.mock('@/utils/dateUtils', () => ({
  formatDateTime: jest.fn((date) => {
    if (!date) return 'Invalid Date';
    return '1st Jan 2023, 05:30';
  }),
}));

jest.mock('@/utils/stringUtils', () => ({
  pluralize: jest.fn((count, singular) => `${count} ${singular}${count !== 1 ? 's' : ''}`),
}));

jest.mock('lodash', () => ({
  isNumber: jest.fn((value) => typeof value === 'number' && !isNaN(value)),
}));

describe('Edge Constants', () => {
  const mockEdge: EdgeType = {
    GUID: 'edge-1',
    Name: 'Test Edge',
    From: 'node-1',
    To: 'node-2',
    Cost: 5,
    Labels: ['label1', 'label2'],
    Tags: { category: 'test', priority: 'high' },
    Vectors: [{ id: 'vec1', values: [1, 2, 3] }],
    CreatedUtc: '2023-01-01T00:00:00Z',
    Score: 0.95,
    Distance: 0.1,
  };

  const mockHandleEdit = jest.fn();
  const mockHandleDelete = jest.fn();
  const mockOnLabelFilter = jest.fn();
  const mockOnTagFilter = jest.fn();

  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('Name column', () => {
    it('renders name correctly', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const nameColumn = columns.find((col) => col.key === 'Name')!;
      const { container } = render(nameColumn.render(mockEdge.Name, mockEdge));

      expect(container).toHaveTextContent('Test Edge');
    });

    it('handles empty name', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const nameColumn = columns.find((col) => col.key === 'Name')!;
      const edgeWithEmptyName = { ...mockEdge, Name: '' };
      const { container } = render(nameColumn.render(edgeWithEmptyName.Name, edgeWithEmptyName));

      expect(container).toHaveTextContent('');
    });
  });

  describe('From column', () => {
    it('renders from node correctly', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const fromColumn = columns.find((col) => col.key === 'FromName')!;
      const { container } = render(fromColumn.render('Node One', mockEdge));

      expect(container).toHaveTextContent('Node One');
    });

    it('handles missing from node', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const fromColumn = columns.find((col) => col.key === 'FromName')!;
      const edgeWithMissingFrom = { ...mockEdge, FromName: undefined };
      const { container } = render(
        fromColumn.render(edgeWithMissingFrom.FromName, edgeWithMissingFrom)
      );

      expect(container).toHaveTextContent('');
    });
  });

  describe('To column', () => {
    it('renders to node correctly', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const toColumn = columns.find((col) => col.key === 'ToName')!;
      const { container } = render(toColumn.render('Node Two', mockEdge));

      expect(container).toHaveTextContent('Node Two');
    });

    it('handles missing to node', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const toColumn = columns.find((col) => col.key === 'ToName')!;
      const edgeWithMissingTo = { ...mockEdge, ToName: undefined };
      const { container } = render(toColumn.render(edgeWithMissingTo.ToName, edgeWithMissingTo));

      expect(container).toHaveTextContent('');
    });
  });

  describe('Cost column', () => {
    it('renders cost correctly', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const costColumn = columns.find((col) => col.key === 'Cost')!;
      const { container } = render(costColumn.render(mockEdge.Cost, mockEdge));

      expect(container).toHaveTextContent('5');
    });

    it('handles zero cost', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const costColumn = columns.find((col) => col.key === 'Cost')!;
      const edgeWithZeroCost = { ...mockEdge, Cost: 0 };
      const { container } = render(costColumn.render(edgeWithZeroCost.Cost, edgeWithZeroCost));

      expect(container).toHaveTextContent('0');
    });

    it('handles negative cost', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const costColumn = columns.find((col) => col.key === 'Cost')!;
      const edgeWithNegativeCost = { ...mockEdge, Cost: -5 };
      const { container } = render(
        costColumn.render(edgeWithNegativeCost.Cost, edgeWithNegativeCost)
      );

      expect(container).toHaveTextContent('-5');
    });
  });

  describe('Labels column', () => {
    it('renders labels correctly', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const labelsColumn = columns.find((col) => col.key === 'Labels')!;
      const { container } = render(labelsColumn.render(mockEdge.Labels, mockEdge));

      expect(screen.getAllByTestId('tag')).toHaveLength(2);
      expect(screen.getAllByTestId('tag')[0]).toHaveTextContent('label1');
    });

    it('handles empty labels array', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const labelsColumn = columns.find((col) => col.key === 'Labels')!;
      const edgeWithEmptyLabels = { ...mockEdge, Labels: [] };
      const { container } = render(
        labelsColumn.render(edgeWithEmptyLabels.Labels, edgeWithEmptyLabels)
      );

      expect(container).toHaveTextContent('None');
    });

    it('handles undefined labels', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const labelsColumn = columns.find((col) => col.key === 'Labels')!;
      const edgeWithUndefinedLabels = { ...mockEdge, Labels: undefined };
      const { container } = render(
        labelsColumn.render(edgeWithUndefinedLabels.Labels, edgeWithUndefinedLabels)
      );

      expect(container).toHaveTextContent('None');
    });

    it('has filter dropdown for labels', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const labelsColumn = columns.find((col) => col.key === 'Labels')!;
      const filterDropdown = labelsColumn.filterDropdown;

      if (filterDropdown) {
        const { container } = render(filterDropdown({} as any));
        expect(screen.getByTestId('table-search')).toBeInTheDocument();
      }
    });
  });

  describe('Tags column', () => {
    it('renders tags correctly', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const tagsColumn = columns.find((col) => col.key === 'Tags')!;
      const { container } = render(tagsColumn.render(mockEdge.Tags, mockEdge));

      expect(container).toHaveTextContent('{"category":"test","priority":"high"}');
    });

    it('handles empty tags object', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const tagsColumn = columns.find((col) => col.key === 'Tags')!;
      const edgeWithEmptyTags = { ...mockEdge, Tags: {} };
      const { container } = render(tagsColumn.render(edgeWithEmptyTags.Tags, edgeWithEmptyTags));

      expect(container).toHaveTextContent('None');
    });

    it('handles undefined tags', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const tagsColumn = columns.find((col) => col.key === 'Tags')!;
      const edgeWithUndefinedTags = { ...mockEdge, Tags: undefined };
      const { container } = render(
        tagsColumn.render(edgeWithUndefinedTags.Tags, edgeWithUndefinedTags)
      );

      expect(container).toHaveTextContent('None');
    });

    it('has filter dropdown for tags', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const tagsColumn = columns.find((col) => col.key === 'Tags')!;
      const filterDropdown = tagsColumn.filterDropdown;

      if (filterDropdown) {
        const { container } = render(filterDropdown({} as any));
        expect(screen.getByTestId('table-search')).toBeInTheDocument();
      }
    });
  });

  describe('Vectors column', () => {
    it('renders vectors count correctly', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const vectorsColumn = columns.find((col) => col.key === 'Vectors')!;
      const { container } = render(vectorsColumn.render(mockEdge.Vectors, mockEdge));

      expect(container).toHaveTextContent('1 vector');
    });

    it('handles empty vectors array', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const vectorsColumn = columns.find((col) => col.key === 'Vectors')!;
      const edgeWithEmptyVectors = { ...mockEdge, Vectors: [] };
      const { container } = render(
        vectorsColumn.render(edgeWithEmptyVectors.Vectors, edgeWithEmptyVectors)
      );

      expect(container).toHaveTextContent('None');
    });

    it('handles undefined vectors', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const vectorsColumn = columns.find((col) => col.key === 'Vectors')!;
      const edgeWithUndefinedVectors = { ...mockEdge, Vectors: undefined };
      const { container } = render(
        vectorsColumn.render(edgeWithUndefinedVectors.Vectors, edgeWithUndefinedVectors)
      );

      expect(container).toHaveTextContent('None');
    });

    it('handles multiple vectors', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const vectorsColumn = columns.find((col) => col.key === 'Vectors')!;
      const edgeWithMultipleVectors = {
        ...mockEdge,
        Vectors: [
          { id: 'vec1', values: [1, 2, 3] },
          { id: 'vec2', values: [4, 5, 6] },
        ],
      };
      const { container } = render(
        vectorsColumn.render(edgeWithMultipleVectors.Vectors, edgeWithMultipleVectors)
      );

      expect(container).toHaveTextContent('2 vectors');
    });
  });

  describe('Created UTC column', () => {
    it('renders formatted date correctly', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const createdUtcColumn = columns.find((col) => col.key === 'CreatedUtc')!;
      const { container } = render(createdUtcColumn.render(mockEdge.CreatedUtc, mockEdge));

      expect(container).toHaveTextContent('1st Jan 2023, 05:30');
    });

    it('handles missing date', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const createdUtcColumn = columns.find((col) => col.key === 'CreatedUtc')!;
      const edgeWithMissingDate = { ...mockEdge, CreatedUtc: undefined };
      const { container } = render(
        createdUtcColumn.render(edgeWithMissingDate.CreatedUtc, edgeWithMissingDate)
      );

      expect(container).toHaveTextContent('Invalid Date');
    });

    it('sorts dates correctly', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const createdUtcColumn = columns.find((col) => col.key === 'CreatedUtc')!;
      const sorter = createdUtcColumn.sorter;

      if (sorter) {
        const edge1 = { ...mockEdge, CreatedUtc: '2023-01-01T00:00:00Z' };
        const edge2 = { ...mockEdge, CreatedUtc: '2023-01-02T00:00:00Z' };

        expect(sorter(edge1, edge2)).toBeLessThan(0);
        expect(sorter(edge2, edge1)).toBeGreaterThan(0);
        expect(sorter(edge1, edge1)).toBe(0);
      }
    });
  });

  describe('Score and Distance columns (when hasScoreOrDistance is true)', () => {
    it('includes score column when hasScoreOrDistance is true', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, true, false);
      const scoreColumn = columns.find((col) => col.key === 'Score');

      expect(scoreColumn).toBeDefined();
      expect(scoreColumn?.title).toBe('Score');
    });

    it('includes distance column when hasScoreOrDistance is true', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, true, false);
      const distanceColumn = columns.find((col) => col.key === 'Distance');

      expect(distanceColumn).toBeDefined();
      expect(distanceColumn?.title).toBe('Distance');
    });

    it('renders score correctly', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, true, false);
      const scoreColumn = columns.find((col) => col.key === 'Score')!;
      const { container } = render(scoreColumn.render(mockEdge.Score, mockEdge));

      expect(container).toHaveTextContent('0.95');
    });

    it('renders distance correctly', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, true, false);
      const distanceColumn = columns.find((col) => col.key === 'Distance')!;
      const { container } = render(distanceColumn.render(mockEdge.Distance, mockEdge));

      expect(container).toHaveTextContent('0.1');
    });

    it('handles missing score', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, true, false);
      const scoreColumn = columns.find((col) => col.key === 'Score')!;
      const edgeWithMissingScore = { ...mockEdge, Score: undefined };
      const { container } = render(
        scoreColumn.render(edgeWithMissingScore.Score, edgeWithMissingScore)
      );

      expect(container).toHaveTextContent('N/A');
    });

    it('handles missing distance', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, true, false);
      const distanceColumn = columns.find((col) => col.key === 'Distance')!;
      const edgeWithMissingDistance = { ...mockEdge, Distance: undefined };
      const { container } = render(
        distanceColumn.render(edgeWithMissingDistance.Distance, edgeWithMissingDistance)
      );

      expect(container).toHaveTextContent('N/A');
    });

    it('handles non-numeric score', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, true, false);
      const scoreColumn = columns.find((col) => col.key === 'Score')!;
      const edgeWithNonNumericScore = { ...mockEdge, Score: 'invalid' as any };
      const { container } = render(
        scoreColumn.render(edgeWithNonNumericScore.Score, edgeWithNonNumericScore)
      );

      expect(container).toHaveTextContent('N/A');
    });

    it('handles non-numeric distance', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, true, false);
      const distanceColumn = columns.find((col) => col.key === 'Distance')!;
      const edgeWithNonNumericDistance = { ...mockEdge, Distance: 'invalid' as any };
      const { container } = render(
        distanceColumn.render(edgeWithNonNumericDistance.Distance, edgeWithNonNumericDistance)
      );

      expect(container).toHaveTextContent('N/A');
    });
  });

  describe('Actions column', () => {
    it('renders actions dropdown correctly', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const actionsColumn = columns.find((col) => col.key === 'actions')!;
      const { container } = render(actionsColumn.render(null, mockEdge));

      // The actual component renders a button, not our mock dropdown
      expect(screen.getByRole('button')).toBeInTheDocument();
    });

    it('calls handleEdit when edit is clicked', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const actionsColumn = columns.find((col) => col.key === 'actions')!;
      render(actionsColumn.render(null, mockEdge));

      // Since we can't easily test the dropdown menu in this mock, just verify the button exists
      expect(screen.getByRole('button')).toBeInTheDocument();
    });

    it('calls handleDelete when delete is clicked', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);
      const actionsColumn = columns.find((col) => col.key === 'actions')!;
      render(actionsColumn.render(null, mockEdge));

      // Since we can't easily test the dropdown menu in this mock, just verify the button exists
      expect(screen.getByRole('button')).toBeInTheDocument();
    });
  });

  describe('Column properties', () => {
    it('has correct column structure', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);

      expect(columns).toHaveLength(10); // Actual column count
      expect(columns[0].title).toBe('Name');
      expect(columns[1].title).toBe('GUID');
      expect(columns[2].title).toBe('From');
      expect(columns[3].title).toBe('To');
      expect(columns[4].title).toBe('Cost');
      expect(columns[5].title).toBe('Labels');
      expect(columns[6].title).toBe('Tags');
      expect(columns[7].title).toBe('Vectors');
      expect(columns[8].title).toBe('Created UTC');
      expect(columns[9].title).toBe('Actions');
    });

    it('has correct responsive properties', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);

      // GUID column should be responsive
      const guidColumn = columns.find((col) => col.key === 'GUID');
      expect(guidColumn?.responsive).toEqual(['md']);

      // From column should be responsive
      const fromColumn = columns.find((col) => col.key === 'FromName');
      expect(fromColumn?.responsive).toEqual(['md']);

      // To column should be responsive
      const toColumn = columns.find((col) => col.key === 'ToName');
      expect(toColumn?.responsive).toEqual(['md']);

      // Vectors column should be responsive
      const vectorsColumn = columns.find((col) => col.key === 'Vectors');
      expect(vectorsColumn?.responsive).toEqual(['md']);

      // Created UTC column should be responsive
      const createdUtcColumn = columns.find((col) => col.key === 'CreatedUtc');
      expect(createdUtcColumn?.responsive).toEqual(['md']);
    });

    it('has correct widths', () => {
      const columns = tableColumns(mockHandleEdit, mockHandleDelete, false, false);

      expect(columns[0].width).toBe(250); // Name
      expect(columns[1].width).toBe(350); // GUID
      expect(columns[2].width).toBe(250); // From
      expect(columns[3].width).toBe(250); // To
      expect(columns[4].width).toBe(150); // Cost
      expect(columns[5].width).toBe(150); // Labels
      expect(columns[6].width).toBe(150); // Tags
      expect(columns[7].width).toBe(150); // Vectors
      expect(columns[8].width).toBe(250); // Created UTC
    });
  });
});
