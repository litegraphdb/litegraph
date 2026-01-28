import '@testing-library/jest-dom';
import { getNodeNameByGUID, transformEdgeDataForTable } from '@/page/edges/utils';
import { EdgeType, NodeType } from '@/types/types';

describe('Edge Utils', () => {
  const mockNodes: NodeType[] = [
    {
      GUID: 'node-1',
      Name: 'Node One',
      Type: 'circle',
      X: 100,
      Y: 100,
      Labels: ['label1'],
      Tags: { tag1: 'value1' },
      Data: {},
      Vectors: [],
    },
    {
      GUID: 'node-2',
      Name: 'Node Two',
      Type: 'square',
      X: 200,
      Y: 200,
      Labels: ['label2'],
      Tags: { tag2: 'value2' },
      Data: {},
      Vectors: [],
    },
    {
      GUID: 'node-3',
      Name: 'Node Three',
      Type: 'triangle',
      X: 300,
      Y: 300,
      Labels: ['label3'],
      Tags: { tag3: 'value3' },
      Data: {},
      Vectors: [],
    },
  ];

  const mockEdges: EdgeType[] = [
    {
      GUID: 'edge-1',
      Name: 'Edge One',
      From: 'node-1',
      To: 'node-2',
      Cost: 5,
      Labels: ['edgeLabel1'],
      Tags: { edgeTag1: 'edgeValue1' },
      Data: { edgeData1: 'value1' },
      Vectors: [{ id: 'vec1', values: [1, 2, 3] }],
      CreatedUtc: '2023-01-01T00:00:00Z',
    },
    {
      GUID: 'edge-2',
      Name: 'Edge Two',
      From: 'node-2',
      To: 'node-3',
      Cost: 10,
      Labels: ['edgeLabel2'],
      Tags: { edgeTag2: 'edgeValue2' },
      Data: { edgeData2: 'value2' },
      Vectors: [{ id: 'vec2', values: [4, 5, 6] }],
      CreatedUtc: '2023-01-02T00:00:00Z',
    },
  ];

  describe('getNodeNameByGUID', () => {
    it('returns node name when GUID exists', () => {
      const result = getNodeNameByGUID('node-1', mockNodes);
      expect(result).toBe('Node One');
    });

    it('returns node name for different GUID', () => {
      const result = getNodeNameByGUID('node-2', mockNodes);
      expect(result).toBe('Node Two');
    });

    it('returns GUID when node is not found', () => {
      const result = getNodeNameByGUID('non-existent-node', mockNodes);
      expect(result).toBe('non-existent-node');
    });

    it('returns GUID when nodes list is empty', () => {
      const result = getNodeNameByGUID('node-1', []);
      expect(result).toBe('node-1');
    });

    it('returns GUID when nodes list is undefined', () => {
      const result = getNodeNameByGUID('node-1', undefined as any);
      expect(result).toBe('node-1');
    });

    it('returns GUID when nodes list is null', () => {
      const result = getNodeNameByGUID('node-1', null as any);
      expect(result).toBe('node-1');
    });

    it('returns N/A when GUID is empty string', () => {
      const result = getNodeNameByGUID('', mockNodes);
      expect(result).toBe('N/A');
    });

    it('returns N/A when GUID is undefined', () => {
      const result = getNodeNameByGUID(undefined as any, mockNodes);
      expect(result).toBe('N/A');
    });

    it('returns N/A when GUID is null', () => {
      const result = getNodeNameByGUID(null as any, mockNodes);
      expect(result).toBe('N/A');
    });

    it('handles case-sensitive GUID matching', () => {
      const result = getNodeNameByGUID('NODE-1', mockNodes);
      expect(result).toBe('NODE-1'); // Should not find match due to case sensitivity
    });

    it('handles GUID with special characters', () => {
      const result = getNodeNameByGUID('node-1@#$%', mockNodes);
      expect(result).toBe('node-1@#$%');
    });

    it('handles GUID with spaces', () => {
      const result = getNodeNameByGUID('node 1', mockNodes);
      expect(result).toBe('node 1');
    });

    it('handles very long GUID', () => {
      const longGuid = 'a'.repeat(1000);
      const result = getNodeNameByGUID(longGuid, mockNodes);
      expect(result).toBe(longGuid);
    });

    it('handles numeric GUID as string', () => {
      const numericGuid = '12345';
      const result = getNodeNameByGUID(numericGuid, mockNodes);
      expect(result).toBe(numericGuid);
    });
  });

  describe('transformEdgeDataForTable', () => {
    it('transforms edges with valid nodes correctly', () => {
      const result = transformEdgeDataForTable(mockEdges, mockNodes);

      expect(result).toHaveLength(2);
      expect(result[0].FromName).toBe('Node One');
      expect(result[0].ToName).toBe('Node Two');
      expect(result[0].key).toBe('edge-1');
      expect(result[1].FromName).toBe('Node Two');
      expect(result[1].ToName).toBe('Node Three');
      expect(result[1].key).toBe('edge-2');
    });

    it('preserves all original edge properties', () => {
      const result = transformEdgeDataForTable(mockEdges, mockNodes);

      expect(result[0]).toMatchObject({
        GUID: 'edge-1',
        Name: 'Edge One',
        From: 'node-1',
        To: 'node-2',
        Cost: 5,
        Labels: ['edgeLabel1'],
        Tags: { edgeTag1: 'edgeValue1' },
        Data: { edgeData1: 'value1' },
        Vectors: [{ id: 'vec1', values: [1, 2, 3] }],
        CreatedUtc: '2023-01-01T00:00:00Z',
      });
    });

    it('handles edges with missing source nodes', () => {
      const edgesWithMissingSource = [
        {
          ...mockEdges[0],
          From: 'missing-node',
        },
      ];

      const result = transformEdgeDataForTable(edgesWithMissingSource, mockNodes);

      expect(result).toHaveLength(1);
      expect(result[0].FromName).toBe('missing-node');
      expect(result[0].ToName).toBe('Node Two');
    });

    it('handles edges with missing target nodes', () => {
      const edgesWithMissingTarget = [
        {
          ...mockEdges[0],
          To: 'missing-node',
        },
      ];

      const result = transformEdgeDataForTable(edgesWithMissingTarget, mockNodes);

      expect(result).toHaveLength(1);
      expect(result[0].FromName).toBe('Node One');
      expect(result[0].ToName).toBe('missing-node');
    });

    it('handles edges with both missing source and target nodes', () => {
      const edgesWithMissingNodes = [
        {
          ...mockEdges[0],
          From: 'missing-source',
          To: 'missing-target',
        },
      ];

      const result = transformEdgeDataForTable(edgesWithMissingNodes, mockNodes);

      expect(result).toHaveLength(1);
      expect(result[0].FromName).toBe('missing-source');
      expect(result[0].ToName).toBe('missing-target');
    });

    it('handles empty edges array', () => {
      const result = transformEdgeDataForTable([], mockNodes);

      expect(result).toHaveLength(0);
      expect(Array.isArray(result)).toBe(true);
    });

    it('handles undefined edges array', () => {
      const result = transformEdgeDataForTable(undefined as any, mockNodes);

      expect(result).toHaveLength(0);
      expect(Array.isArray(result)).toBe(true);
    });

    it('handles null edges array', () => {
      const result = transformEdgeDataForTable(null as any, mockNodes);

      expect(result).toHaveLength(0);
      expect(Array.isArray(result)).toBe(true);
    });

    it('handles empty nodes array', () => {
      const result = transformEdgeDataForTable(mockEdges, []);

      expect(result).toHaveLength(2);
      expect(result[0].FromName).toBe('node-1');
      expect(result[0].ToName).toBe('node-2');
      expect(result[1].FromName).toBe('node-2');
      expect(result[1].ToName).toBe('node-3');
    });

    it('handles undefined nodes array', () => {
      const result = transformEdgeDataForTable(mockEdges, undefined as any);

      expect(result).toHaveLength(2);
      expect(result[0].FromName).toBe('node-1');
      expect(result[0].ToName).toBe('node-2');
      expect(result[1].FromName).toBe('node-2');
      expect(result[1].ToName).toBe('node-3');
    });

    it('handles null nodes array', () => {
      const result = transformEdgeDataForTable(mockEdges, null as any);

      expect(result).toHaveLength(2);
      expect(result[0].FromName).toBe('node-1');
      expect(result[0].ToName).toBe('node-2');
      expect(result[1].FromName).toBe('node-2');
      expect(result[1].ToName).toBe('node-3');
    });

    it('handles edges with missing properties', () => {
      const incompleteEdges = [
        {
          GUID: 'edge-3',
          Name: 'Incomplete Edge',
          From: 'node-1',
          To: 'node-2',
        } as EdgeType,
      ];

      const result = transformEdgeDataForTable(incompleteEdges, mockNodes);

      expect(result).toHaveLength(1);
      expect(result[0].FromName).toBe('Node One');
      expect(result[0].ToName).toBe('Node Two');
      expect(result[0].key).toBe('edge-3');
    });

    it('handles edges with null/undefined values', () => {
      const edgesWithNulls = [
        {
          ...mockEdges[0],
          From: null as any,
          To: undefined as any,
        },
      ];

      const result = transformEdgeDataForTable(edgesWithNulls, mockNodes);

      expect(result).toHaveLength(1);
      expect(result[0].FromName).toBe('N/A');
      expect(result[0].ToName).toBe('N/A');
    });

    it('handles edges with empty string values', () => {
      const edgesWithEmptyStrings = [
        {
          ...mockEdges[0],
          From: '',
          To: '',
        },
      ];

      const result = transformEdgeDataForTable(edgesWithEmptyStrings, mockNodes);

      expect(result).toHaveLength(1);
      expect(result[0].FromName).toBe('N/A');
      expect(result[0].ToName).toBe('N/A');
    });

    it('handles edges with numeric string GUIDs', () => {
      const edgesWithNumericStrings = [
        {
          ...mockEdges[0],
          From: '123',
          To: '456',
        },
      ];

      const result = transformEdgeDataForTable(edgesWithNumericStrings, mockNodes);

      expect(result).toHaveLength(1);
      expect(result[0].FromName).toBe('123');
      expect(result[0].ToName).toBe('456');
    });

    it('handles edges with special characters in GUIDs', () => {
      const edgesWithSpecialChars = [
        {
          ...mockEdges[0],
          From: 'node-1@#$%',
          To: 'node-2!@#',
        },
      ];

      const result = transformEdgeDataForTable(edgesWithSpecialChars, mockNodes);

      expect(result).toHaveLength(1);
      expect(result[0].FromName).toBe('node-1@#$%');
      expect(result[0].ToName).toBe('node-2!@#');
    });

    it('handles edges with very long GUIDs', () => {
      const longGuid = 'a'.repeat(1000);
      const edgesWithLongGuids = [
        {
          ...mockEdges[0],
          From: longGuid,
          To: longGuid,
        },
      ];

      const result = transformEdgeDataForTable(edgesWithLongGuids, mockNodes);

      expect(result).toHaveLength(1);
      expect(result[0].FromName).toBe(longGuid);
      expect(result[0].ToName).toBe(longGuid);
    });

    it('handles mixed case scenarios', () => {
      const mixedCaseEdges = [
        {
          ...mockEdges[0],
          From: 'NODE-1',
          To: 'Node-2',
        },
      ];

      const result = transformEdgeDataForTable(mixedCaseEdges, mockNodes);

      expect(result).toHaveLength(1);
      expect(result[0].FromName).toBe('NODE-1');
      expect(result[0].ToName).toBe('Node-2');
    });

    it('handles edges with whitespace in GUIDs', () => {
      const edgesWithWhitespace = [
        {
          ...mockEdges[0],
          From: ' node-1 ',
          To: ' node-2 ',
        },
      ];

      const result = transformEdgeDataForTable(edgesWithWhitespace, mockNodes);

      expect(result).toHaveLength(1);
      expect(result[0].FromName).toBe(' node-1 ');
      expect(result[0].ToName).toBe(' node-2 ');
    });
  });
});
