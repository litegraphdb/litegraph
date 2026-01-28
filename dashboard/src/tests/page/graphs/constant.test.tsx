import '@testing-library/jest-dom';
import React from 'react';
import { render, screen } from '@testing-library/react';
import { tableColumns } from '@/page/graphs/constant';
import { GraphData } from '@/types/types';

// Mock dependencies
jest.mock('@/utils/dateUtils', () => ({
  formatDateTime: jest.fn((date) => `formatted-${date}`),
}));

jest.mock('@/utils/stringUtils', () => ({
  pluralize: jest.fn((count, singular) => `${count} ${singular}${count !== 1 ? 's' : ''}`),
}));

jest.mock('lodash', () => ({
  isNumber: jest.fn((value) => typeof value === 'number' && !isNaN(value)),
}));

jest.mock('@/components/table-search/TableSearch', () => {
  return function MockTableSearch({ placeholder }: { placeholder: string }) {
    return <div data-testid="table-search">{placeholder}</div>;
  };
});

describe('Graphs Constants', () => {
  const mockHandleEdit = jest.fn();
  const mockHandleDelete = jest.fn();
  const mockHandleExportGexf = jest.fn();
  const mockHandleEnableVectorIndex = jest.fn();
  const mockHandleReadVectorIndexConfig = jest.fn();
  const mockHandleReadVectorIndexStats = jest.fn();
  const mockHandleRebuildVectorIndex = jest.fn();
  const mockHandleDeleteVectorIndex = jest.fn();

  const mockGraphData: GraphData = {
    GUID: 'graph-123',
    Name: 'Test Graph',
    Description: 'Test Description',
    Tags: { tag1: 'value1', tag2: 'value2' },
    Vectors: ['vector1', 'vector2'],
    CreatedUtc: '2024-01-01T00:00:00Z',
    LastUpdateUtc: '2024-01-01T12:00:00Z',
    Score: 0.95,
    Distance: 0.1,
  };

  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('tableColumns function', () => {
    it('returns correct number of columns', () => {
      const columns = tableColumns(
        mockHandleEdit,
        mockHandleDelete,
        mockHandleExportGexf,
        mockHandleEnableVectorIndex,
        mockHandleReadVectorIndexConfig,
        mockHandleReadVectorIndexStats,
        mockHandleRebuildVectorIndex,
        mockHandleDeleteVectorIndex,
        false
      );

      expect(columns).toHaveLength(7); // Basic columns without score/distance
    });

    it('includes score and distance columns when hasScoreOrDistance is true', () => {
      const columns = tableColumns(
        mockHandleEdit,
        mockHandleDelete,
        mockHandleExportGexf,
        mockHandleEnableVectorIndex,
        mockHandleReadVectorIndexConfig,
        mockHandleReadVectorIndexStats,
        mockHandleRebuildVectorIndex,
        mockHandleDeleteVectorIndex,
        true
      );

      expect(columns).toHaveLength(9); // Basic columns + score + distance

      // Check that score and distance columns are present
      const scoreColumn = columns.find((col) => col.key === 'Score');
      const distanceColumn = columns.find((col) => col.key === 'Distance');

      expect(scoreColumn).toBeDefined();
      expect(distanceColumn).toBeDefined();
    });

    it('renders name column correctly', () => {
      const columns = tableColumns(
        mockHandleEdit,
        mockHandleDelete,
        mockHandleExportGexf,
        mockHandleEnableVectorIndex,
        mockHandleReadVectorIndexConfig,
        mockHandleReadVectorIndexStats,
        mockHandleRebuildVectorIndex,
        mockHandleDeleteVectorIndex,
        false
      );

      const nameColumn = columns.find((col) => col.key === 'name');
      expect(nameColumn).toBeDefined();
      expect(nameColumn?.title).toBe('Name');
      expect(nameColumn?.dataIndex).toBe('Name');
    });

    it('renders GUID column correctly', () => {
      const columns = tableColumns(
        mockHandleEdit,
        mockHandleDelete,
        mockHandleExportGexf,
        mockHandleEnableVectorIndex,
        mockHandleReadVectorIndexConfig,
        mockHandleReadVectorIndexStats,
        mockHandleRebuildVectorIndex,
        mockHandleDeleteVectorIndex,
        false
      );

      const guidColumn = columns.find((col) => col.key === 'GUID');
      expect(guidColumn).toBeDefined();
      expect(guidColumn?.title).toBe('GUID');
      expect(guidColumn?.dataIndex).toBe('GUID');
    });

    it('renders labels column with filter', () => {
      const columns = tableColumns(
        mockHandleEdit,
        mockHandleDelete,
        mockHandleExportGexf,
        mockHandleEnableVectorIndex,
        mockHandleReadVectorIndexConfig,
        mockHandleReadVectorIndexStats,
        mockHandleRebuildVectorIndex,
        mockHandleDeleteVectorIndex,
        false
      );

      const labelsColumn = columns.find((col) => col.key === 'labels');
      expect(labelsColumn).toBeDefined();
      expect(labelsColumn?.title).toBe('Labels');
      expect(labelsColumn?.dataIndex).toBe('Labels');
      expect(labelsColumn?.filterDropdown).toBeDefined();
      expect(labelsColumn?.onFilter).toBeDefined();
    });

    it('renders tags column with filter', () => {
      const columns = tableColumns(
        mockHandleEdit,
        mockHandleDelete,
        mockHandleExportGexf,
        mockHandleEnableVectorIndex,
        mockHandleReadVectorIndexConfig,
        mockHandleReadVectorIndexStats,
        mockHandleRebuildVectorIndex,
        mockHandleDeleteVectorIndex,
        false
      );

      const tagsColumn = columns.find((col) => col.key === 'tags');
      expect(tagsColumn).toBeDefined();
      expect(tagsColumn?.title).toBe('Tags');
      expect(tagsColumn?.dataIndex).toBe('Tags');
      expect(tagsColumn?.filterDropdown).toBeDefined();
      expect(tagsColumn?.onFilter).toBeDefined();
    });

    it('renders vectors column correctly', () => {
      const columns = tableColumns(
        mockHandleEdit,
        mockHandleDelete,
        mockHandleExportGexf,
        mockHandleEnableVectorIndex,
        mockHandleReadVectorIndexConfig,
        mockHandleReadVectorIndexStats,
        mockHandleRebuildVectorIndex,
        mockHandleDeleteVectorIndex,
        false
      );

      const vectorsColumn = columns.find((col) => col.key === 'Vectors');
      expect(vectorsColumn).toBeDefined();
      expect(vectorsColumn?.title).toBe('Vectors');
      expect(vectorsColumn?.dataIndex).toBe('Vectors');
    });

    it('renders created UTC column with sorter', () => {
      const columns = tableColumns(
        mockHandleEdit,
        mockHandleDelete,
        mockHandleExportGexf,
        mockHandleEnableVectorIndex,
        mockHandleReadVectorIndexConfig,
        mockHandleReadVectorIndexStats,
        mockHandleRebuildVectorIndex,
        mockHandleDeleteVectorIndex,
        false
      );

      const createdColumn = columns.find((col) => col.key === 'CreatedUtc');
      expect(createdColumn).toBeDefined();
      expect(createdColumn?.title).toBe('Created UTC');
      expect(createdColumn?.dataIndex).toBe('CreatedUtc');
      expect(createdColumn?.sorter).toBeDefined();
    });

    it('renders actions column with all action items', () => {
      const columns = tableColumns(
        mockHandleEdit,
        mockHandleDelete,
        mockHandleExportGexf,
        mockHandleEnableVectorIndex,
        mockHandleReadVectorIndexConfig,
        mockHandleReadVectorIndexStats,
        mockHandleRebuildVectorIndex,
        mockHandleDeleteVectorIndex,
        false
      );

      const actionsColumn = columns.find((col) => col.key === 'actions');
      expect(actionsColumn).toBeDefined();
      expect(actionsColumn?.title).toBe('Actions');
      expect(actionsColumn?.render).toBeDefined();
    });

    it('handles score column rendering correctly', () => {
      const columns = tableColumns(
        mockHandleEdit,
        mockHandleDelete,
        mockHandleExportGexf,
        mockHandleEnableVectorIndex,
        mockHandleReadVectorIndexConfig,
        mockHandleReadVectorIndexStats,
        mockHandleRebuildVectorIndex,
        mockHandleDeleteVectorIndex,
        true
      );

      const scoreColumn = columns.find((col) => col.key === 'Score');
      expect(scoreColumn).toBeDefined();
      expect(scoreColumn?.title).toBe('Score');
      expect(scoreColumn?.dataIndex).toBe('Score');
      expect(scoreColumn?.render).toBeDefined();
    });

    it('handles distance column rendering correctly', () => {
      const columns = tableColumns(
        mockHandleEdit,
        mockHandleDelete,
        mockHandleExportGexf,
        mockHandleEnableVectorIndex,
        mockHandleReadVectorIndexConfig,
        mockHandleReadVectorIndexStats,
        mockHandleRebuildVectorIndex,
        mockHandleDeleteVectorIndex,
        true
      );

      const distanceColumn = columns.find((col) => col.key === 'Distance');
      expect(distanceColumn).toBeDefined();
      expect(distanceColumn?.title).toBe('Distance');
      expect(distanceColumn?.dataIndex).toBe('Distance');
      expect(distanceColumn?.render).toBeDefined();
    });

    it('handles responsive properties correctly', () => {
      const columns = tableColumns(
        mockHandleEdit,
        mockHandleDelete,
        mockHandleExportGexf,
        mockHandleEnableVectorIndex,
        mockHandleReadVectorIndexConfig,
        mockHandleReadVectorIndexStats,
        mockHandleRebuildVectorIndex,
        mockHandleDeleteVectorIndex,
        false
      );

      // Check that columns have responsive properties
      const nameColumn = columns.find((col) => col.key === 'name');
      const guidColumn = columns.find((col) => col.key === 'GUID');
      const labelsColumn = columns.find((col) => col.key === 'labels');
      const tagsColumn = columns.find((col) => col.key === 'tags');
      const vectorsColumn = columns.find((col) => col.key === 'Vectors');
      const createdColumn = columns.find((col) => col.key === 'CreatedUtc');
      const actionsColumn = columns.find((col) => col.key === 'actions');

      expect(nameColumn?.responsive).toEqual(['sm']);
      expect(guidColumn?.responsive).toEqual(['sm']);
      expect(labelsColumn?.responsive).toEqual(['sm']);
      expect(tagsColumn?.responsive).toEqual(['sm']);
      expect(vectorsColumn?.responsive).toEqual(['sm']);
      expect(createdColumn?.responsive).toEqual(['sm']);
      expect(actionsColumn?.responsive).toEqual(['sm']);
    });
  });
});
