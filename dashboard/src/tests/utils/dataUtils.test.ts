import {
  hasScoreOrDistanceInData,
  getNodeAndEdgeGUIDsByEntityList,
  getPercentage,
  humanizeNumber,
} from '@/utils/dataUtils';

// Mock humanize-number
jest.mock('humanize-number', () => jest.fn((value) => `humanized-${value}`));

describe('Data Utils', () => {
  beforeEach(() => {
    jest.clearAllMocks();
  });

  describe('hasScoreOrDistanceInData', () => {
    it('should return true when data contains Score property', () => {
      const data = [
        { id: 1, name: 'Item 1', Score: 0.95 },
        { id: 2, name: 'Item 2' },
      ];

      expect(hasScoreOrDistanceInData(data)).toBe(true);
    });

    it('should return true when data contains Distance property', () => {
      const data = [
        { id: 1, name: 'Item 1' },
        { id: 2, name: 'Item 2', Distance: 0.5 },
      ];

      expect(hasScoreOrDistanceInData(data)).toBe(true);
    });

    it('should return true when data contains both Score and Distance properties', () => {
      const data = [
        { id: 1, name: 'Item 1', Score: 0.95, Distance: 0.3 },
        { id: 2, name: 'Item 2' },
      ];

      expect(hasScoreOrDistanceInData(data)).toBe(true);
    });

    it('should return false when data contains neither Score nor Distance properties', () => {
      const data = [
        { id: 1, name: 'Item 1' },
        { id: 2, name: 'Item 2' },
      ];

      expect(hasScoreOrDistanceInData(data)).toBe(false);
    });

    it('should return false for empty array', () => {
      expect(hasScoreOrDistanceInData([])).toBe(false);
    });

    it('should handle case-sensitive property names', () => {
      const data = [
        { id: 1, name: 'Item 1', score: 0.95 }, // lowercase
        { id: 2, name: 'Item 2', distance: 0.5 }, // lowercase
      ];

      expect(hasScoreOrDistanceInData(data)).toBe(false);
    });
  });

  describe('getNodeAndEdgeGUIDsByEntityList', () => {
    it('should extract node and edge GUIDs when keys are provided', () => {
      const entityList = [
        { id: 1, nodeId: 'node-1', edgeId: 'edge-1' },
        { id: 2, nodeId: 'node-2', edgeId: 'edge-2' },
        { id: 3, nodeId: 'node-1', edgeId: 'edge-3' }, // duplicate nodeId
      ];

      const result = getNodeAndEdgeGUIDsByEntityList(
        entityList,
        'nodeId' as keyof (typeof entityList)[0],
        'edgeId' as keyof (typeof entityList)[0]
      );

      expect(result.nodeGUIDs).toEqual(['node-1', 'node-2']);
      expect(result.edgeGUIDs).toEqual(['edge-1', 'edge-2', 'edge-3']);
    });

    it('should return empty arrays when no keys are provided', () => {
      const entityList = [{ id: 1, nodeId: 'node-1', edgeId: 'edge-1' }];

      const result = getNodeAndEdgeGUIDsByEntityList(entityList);

      expect(result.nodeGUIDs).toEqual([]);
      expect(result.edgeGUIDs).toEqual([]);
    });

    it('should return only node GUIDs when only nodeGUIDKey is provided', () => {
      const entityList = [
        { id: 1, nodeId: 'node-1', edgeId: 'edge-1' },
        { id: 2, nodeId: 'node-2', edgeId: 'edge-2' },
      ];

      const result = getNodeAndEdgeGUIDsByEntityList(
        entityList,
        'nodeId' as keyof (typeof entityList)[0]
      );

      expect(result.nodeGUIDs).toEqual(['node-1', 'node-2']);
      expect(result.edgeGUIDs).toEqual([]);
    });

    it('should return only edge GUIDs when only edgeGUIDKey is provided', () => {
      const entityList = [
        { id: 1, nodeId: 'node-1', edgeId: 'edge-1' },
        { id: 2, nodeId: 'node-2', edgeId: 'edge-2' },
      ];

      const result = getNodeAndEdgeGUIDsByEntityList(
        entityList,
        undefined,
        'edgeId' as keyof (typeof entityList)[0]
      );

      expect(result.nodeGUIDs).toEqual([]);
      expect(result.edgeGUIDs).toEqual(['edge-1', 'edge-2']);
    });

    it('should filter out falsy GUIDs', () => {
      const entityList = [
        { id: 1, nodeId: 'node-1', edgeId: 'edge-1' },
        { id: 2, nodeId: '', edgeId: null as any },
        { id: 3, nodeId: undefined as any, edgeId: 'edge-3' },
        { id: 4, nodeId: 'node-4', edgeId: 'edge-4' },
      ];

      const result = getNodeAndEdgeGUIDsByEntityList(
        entityList,
        'nodeId' as keyof (typeof entityList)[0],
        'edgeId' as keyof (typeof entityList)[0]
      );

      expect(result.nodeGUIDs).toEqual(['node-1', 'node-4']);
      expect(result.edgeGUIDs).toEqual(['edge-1', 'edge-3', 'edge-4']);
    });

    it('should handle empty entity list', () => {
      const result = getNodeAndEdgeGUIDsByEntityList([]);

      expect(result.nodeGUIDs).toEqual([]);
      expect(result.edgeGUIDs).toEqual([]);
    });

    it('should remove duplicates using Set', () => {
      const entityList = [
        { id: 1, nodeId: 'node-1', edgeId: 'edge-1' },
        { id: 2, nodeId: 'node-1', edgeId: 'edge-1' }, // duplicate
        { id: 3, nodeId: 'node-2', edgeId: 'edge-2' },
      ];

      const result = getNodeAndEdgeGUIDsByEntityList(
        entityList,
        'nodeId' as keyof (typeof entityList)[0],
        'edgeId' as keyof (typeof entityList)[0]
      );

      expect(result.nodeGUIDs).toEqual(['node-1', 'node-2']);
      expect(result.edgeGUIDs).toEqual(['edge-1', 'edge-2']);
    });
  });

  describe('getPercentage', () => {
    it('should calculate percentage correctly', () => {
      expect(getPercentage(25, 100)).toBe(25);
    });

    it('should round percentage to nearest integer', () => {
      expect(getPercentage(33, 100)).toBe(33);
    });

    it('should handle zero values', () => {
      expect(getPercentage(0, 100)).toBe(0);
      expect(getPercentage(50, 0)).toBe(Infinity); // Division by zero returns Infinity
    });

    it('should handle decimal values', () => {
      expect(getPercentage(0.5, 1)).toBe(50);
    });

    it('should handle large numbers', () => {
      expect(getPercentage(999999, 1000000)).toBe(100);
    });

    it('should handle negative values', () => {
      expect(getPercentage(-25, 100)).toBe(-25);
    });
  });

  describe('humanizeNumber', () => {
    it('should call humanize function with the provided value', () => {
      const result = humanizeNumber(1000);

      expect(require('humanize-number')).toHaveBeenCalledWith(1000);
      expect(result).toBe('humanized-1000');
    });

    it('should handle zero', () => {
      const result = humanizeNumber(0);

      expect(require('humanize-number')).toHaveBeenCalledWith(0);
      expect(result).toBe('humanized-0');
    });

    it('should handle negative numbers', () => {
      const result = humanizeNumber(-1000);

      expect(require('humanize-number')).toHaveBeenCalledWith(-1000);
      expect(result).toBe('humanized--1000');
    });

    it('should handle decimal numbers', () => {
      const result = humanizeNumber(3.14);

      expect(require('humanize-number')).toHaveBeenCalledWith(3.14);
      expect(result).toBe('humanized-3.14');
    });
  });
});
