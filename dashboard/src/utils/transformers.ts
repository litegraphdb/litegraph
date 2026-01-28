import { EdgeType } from '@/types/types';
import { GraphData } from '@/types/types';
import { NodeType } from '@/types/types';
import { VectorSearchResult } from 'litegraphdb/dist/types/types';

export const transformToGraphData = (data: VectorSearchResult[]) => {
  const graphData: GraphData[] = [];
  data.forEach((item: VectorSearchResult) => {
    if (item.Graph) {
      graphData.push(item.Graph);
    }
  });
  return graphData;
};

export const transformToNodeData = (data: VectorSearchResult[]) => {
  const nodeData: NodeType[] = [];
  data.forEach((item: VectorSearchResult) => {
    if (item.Node) {
      nodeData.push({
        ...item.Node,
        Score: item.Score,
        Distance: item.Distance,
      });
    }
  });
  return nodeData;
};

export const transformToEdgeData = (data: VectorSearchResult[]) => {
  const edgeData: EdgeType[] = [];
  data.forEach((item: VectorSearchResult) => {
    if (item.Edge) {
      edgeData.push(item.Edge);
    }
  });
  return edgeData;
};
