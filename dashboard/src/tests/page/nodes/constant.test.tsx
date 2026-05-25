import '@testing-library/jest-dom';
import React from 'react';
import { render, screen } from '@testing-library/react';
import { tableColumns } from '@/page/nodes/constant';
import { NodeType } from '@/types/types';

jest.mock('@/components/base/tag/Tag', () => {
  return function MockTag({ label }: { label: string }) {
    return <span data-testid="tag">{label}</span>;
  };
});

describe('Node Constants', () => {
  const mockHandleEdit = jest.fn();
  const mockHandleDelete = jest.fn();

  const mockNode: NodeType = {
    GUID: 'node-1',
    Name: 'Test Node',
    Labels: ['label1', 'label2'],
    Tags: { category: 'test', priority: 'high' },
    Vectors: [],
    CreatedUtc: '2023-01-01T00:00:00Z',
  } as NodeType;

  beforeEach(() => {
    jest.clearAllMocks();
  });

  it('renders labels column as a count badge', () => {
    const columns = tableColumns(mockHandleEdit, mockHandleDelete, false);
    const labelsColumn = columns.find((col) => col.key === 'Labels')!;

    render(labelsColumn.render?.(mockNode.Labels, mockNode) as React.ReactElement);

    expect(screen.getByTestId('tag')).toHaveTextContent('2 labels');
  });

  it('renders tags column as a count badge', () => {
    const columns = tableColumns(mockHandleEdit, mockHandleDelete, false);
    const tagsColumn = columns.find((col) => col.key === 'Tags')!;

    render(tagsColumn.render?.(mockNode.Tags, mockNode) as React.ReactElement);

    expect(screen.getByTestId('tag')).toHaveTextContent('2 tags');
  });
});
