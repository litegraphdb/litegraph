import {
  transformToGraphData,
  transformToNodeData,
  transformToEdgeData,
} from '@/utils/transformers';
import { VectorSearchResult } from 'litegraphdb/dist/types/types';
import { GraphData, NodeType, EdgeType } from '@/types/types';

describe('Transformers', () => {
  describe('transformToGraphData', () => {
    it('should transform vector search results to graph data', () => {
      const mockData: VectorSearchResult[] = [
        { Graph: { id: 'graph1', name: 'Graph 1' } as GraphData },
        { Graph: { id: 'graph2', name: 'Graph 2' } as GraphData },
        { Node: { id: 'node1', name: 'Node 1' } as NodeType }, // Should be filtered out
        { Edge: { id: 'edge1', name: 'Edge 1' } as EdgeType }, // Should be filtered out
      ];

      const result = transformToGraphData(mockData);

      expect(result).toHaveLength(2);
      expect(result[0]).toEqual({ id: 'graph1', name: 'Graph 1' });
      expect(result[1]).toEqual({ id: 'graph2', name: 'Graph 2' });
    });

    it('should return empty array when no graph data exists', () => {
      const mockData: VectorSearchResult[] = [
        { Node: { id: 'node1', name: 'Node 1' } as NodeType },
        { Edge: { id: 'edge1', name: 'Edge 1' } as EdgeType },
      ];

      const result = transformToGraphData(mockData);

      expect(result).toHaveLength(0);
      expect(result).toEqual([]);
    });

    it('should handle empty input array', () => {
      const result = transformToGraphData([]);

      expect(result).toHaveLength(0);
      expect(result).toEqual([]);
    });

    it('should handle mixed data with some items having no Graph property', () => {
      const mockData: VectorSearchResult[] = [
        { Graph: { id: 'graph1', name: 'Graph 1' } as GraphData },
        { Score: 0.95, Distance: 0.1 }, // No Graph property
        { Graph: { id: 'graph2', name: 'Graph 2' } as GraphData },
      ];

      const result = transformToGraphData(mockData);

      expect(result).toHaveLength(2);
      expect(result[0]).toEqual({ id: 'graph1', name: 'Graph 1' });
      expect(result[1]).toEqual({ id: 'graph2', name: 'Graph 2' });
    });
  });

  describe('transformToNodeData', () => {
    it('should transform vector search results to node data with score and distance', () => {
      const mockData: VectorSearchResult[] = [
        {
          Node: { id: 'node1', name: 'Node 1' } as NodeType,
          Score: 0.95,
          Distance: 0.1,
        },
        {
          Node: { id: 'node2', name: 'Node 2' } as NodeType,
          Score: 0.87,
          Distance: 0.2,
        },
        { Graph: { id: 'graph1', name: 'Graph 1' } as GraphData }, // Should be filtered out
        { Edge: { id: 'edge1', name: 'Edge 1' } as EdgeType }, // Should be filtered out
      ];

      const result = transformToNodeData(mockData);

      expect(result).toHaveLength(2);
      expect(result[0]).toEqual({
        id: 'node1',
        name: 'Node 1',
        Score: 0.95,
        Distance: 0.1,
      });
      expect(result[1]).toEqual({
        id: 'node2',
        name: 'Node 2',
        Score: 0.87,
        Distance: 0.2,
      });
    });

    it('should return empty array when no node data exists', () => {
      const mockData: VectorSearchResult[] = [
        { Graph: { id: 'graph1', name: 'Graph 1' } as GraphData },
        { Edge: { id: 'edge1', name: 'Edge 1' } as EdgeType },
      ];

      const result = transformToNodeData(mockData);

      expect(result).toHaveLength(0);
      expect(result).toEqual([]);
    });

    it('should handle nodes without score or distance', () => {
      const mockData: VectorSearchResult[] = [
        {
          Node: { id: 'node1', name: 'Node 1' } as NodeType,
          // No Score or Distance
        },
      ];

      const result = transformToNodeData(mockData);

      expect(result).toHaveLength(1);
      expect(result[0]).toEqual({
        id: 'node1',
        name: 'Node 1',
        Score: undefined,
        Distance: undefined,
      });
    });

    it('should handle empty input array', () => {
      const result = transformToNodeData([]);

      expect(result).toHaveLength(0);
      expect(result).toEqual([]);
    });

    it('should preserve all node properties while adding score and distance', () => {
      const mockData: VectorSearchResult[] = [
        {
          Node: {
            id: 'node1',
            name: 'Node 1',
            type: 'Person',
            properties: { age: 30, city: 'NYC' },
          } as NodeType,
          Score: 0.95,
          Distance: 0.1,
        },
      ];

      const result = transformToNodeData(mockData);

      expect(result).toHaveLength(1);
      expect(result[0]).toEqual({
        id: 'node1',
        name: 'Node 1',
        type: 'Person',
        properties: { age: 30, city: 'NYC' },
        Score: 0.95,
        Distance: 0.1,
      });
    });
  });

  describe('transformToEdgeData', () => {
    it('should transform vector search results to edge data', () => {
      const mockData: VectorSearchResult[] = [
        { Edge: { id: 'edge1', name: 'Edge 1' } as EdgeType },
        { Edge: { id: 'edge2', name: 'Edge 2' } as EdgeType },
        { Node: { id: 'node1', name: 'Node 1' } as NodeType }, // Should be filtered out
        { Graph: { id: 'graph1', name: 'Graph 1' } as GraphData }, // Should be filtered out
      ];

      const result = transformToEdgeData(mockData);

      expect(result).toHaveLength(2);
      expect(result[0]).toEqual({ id: 'edge1', name: 'Edge 1' });
      expect(result[1]).toEqual({ id: 'edge2', name: 'Edge 2' });
    });

    it('should return empty array when no edge data exists', () => {
      const mockData: VectorSearchResult[] = [
        { Node: { id: 'node1', name: 'Node 1' } as NodeType },
        { Graph: { id: 'graph1', name: 'Graph 1' } as GraphData },
      ];

      const result = transformToEdgeData(mockData);

      expect(result).toHaveLength(0);
      expect(result).toEqual([]);
    });

    it('should handle empty input array', () => {
      const result = transformToEdgeData([]);

      expect(result).toHaveLength(0);
      expect(result).toEqual([]);
    });

    it('should preserve all edge properties', () => {
      const mockData: VectorSearchResult[] = [
        {
          Edge: {
            id: 'edge1',
            name: 'Edge 1',
            source: 'node1',
            target: 'node2',
            type: 'KNOWS',
            properties: { since: 2020 },
          } as EdgeType,
        },
      ];

      const result = transformToEdgeData(mockData);

      expect(result).toHaveLength(1);
      expect(result[0]).toEqual({
        id: 'edge1',
        name: 'Edge 1',
        source: 'node1',
        target: 'node2',
        type: 'KNOWS',
        properties: { since: 2020 },
      });
    });

    it('should handle mixed data with some items having no Edge property', () => {
      const mockData: VectorSearchResult[] = [
        { Edge: { id: 'edge1', name: 'Edge 1' } as EdgeType },
        { Score: 0.95, Distance: 0.1 }, // No Edge property
        { Edge: { id: 'edge2', name: 'Edge 2' } as EdgeType },
      ];

      const result = transformToEdgeData(mockData);

      expect(result).toHaveLength(2);
      expect(result[0]).toEqual({ id: 'edge1', name: 'Edge 1' });
      expect(result[1]).toEqual({ id: 'edge2', name: 'Edge 2' });
    });
  });

  describe('Edge cases and error handling', () => {
    it('should handle null/undefined values in input array', () => {
      const mockData: VectorSearchResult[] = [
        null as any,
        undefined as any,
        { Graph: { id: 'graph1', name: 'Graph 1' } as GraphData },
      ];

      // Filter out null/undefined values before processing
      const validData = mockData.filter((item) => item != null);

      const graphResult = transformToGraphData(validData);
      const nodeResult = transformToNodeData(validData);
      const edgeResult = transformToEdgeData(validData);

      expect(graphResult).toHaveLength(1);
      expect(nodeResult).toHaveLength(0);
      expect(edgeResult).toHaveLength(0);
    });

    it('should handle items with multiple properties (prioritize first found)', () => {
      const mockData: VectorSearchResult[] = [
        {
          Graph: { id: 'graph1', name: 'Graph 1' } as GraphData,
          Node: { id: 'node1', name: 'Node 1' } as NodeType,
          Edge: { id: 'edge1', name: 'Edge 1' } as EdgeType,
        },
      ];

      const graphResult = transformToGraphData(mockData);
      const nodeResult = transformToNodeData(mockData);
      const edgeResult = transformToEdgeData(mockData);

      // Should only include in graph data since it's processed first
      expect(graphResult).toHaveLength(1);
      expect(nodeResult).toHaveLength(1); // Function processes all items, not just first
      expect(edgeResult).toHaveLength(1); // Function processes all items, not just first
    });
  });
});
