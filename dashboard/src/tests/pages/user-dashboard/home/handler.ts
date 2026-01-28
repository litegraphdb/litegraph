import { mockEndpoint } from '@/tests/config';
import { http, HttpResponse } from 'msw';
import { mockNodeData, mockEdgeData } from '../../mockData'; // Import mock data for nodes and edges

export const graphHandlers = [
  // Simulate fetching selected graph details
  http.get(`${mockEndpoint}v1.0/graphs/selected`, () => {
    return HttpResponse.json({
      selectedGraph: {
        id: '1',
        name: 'Sample Graph',
      },
    });
  }),

  // Simulate fetching nodes for the graph
  http.get(`${mockEndpoint}v1.0/graphs/nodes`, () => {
    return HttpResponse.json({
      nodes: mockNodeData,
      total: mockNodeData.length,
    });
  }),

  // Simulate fetching edges for the graph
  http.get(`${mockEndpoint}v1.0/graphs/edges`, () => {
    return HttpResponse.json({
      edges: mockEdgeData,
      total: mockEdgeData.length,
    });
  }),

  // Simulate error fetching nodes or edges
  http.get(`${mockEndpoint}v1.0/graphs/nodes`, () => {
    return HttpResponse.json({ message: 'Failed to load nodes' }, { status: 500 });
  }),

  http.get(`${mockEndpoint}v1.0/graphs/edges`, () => {
    return HttpResponse.json({ message: 'Failed to load edges' }, { status: 500 });
  }),

  // Optional: Retry loading graph data
  http.get(`${mockEndpoint}v1.0/graphs/retry`, () => {
    return HttpResponse.json({
      nodes: mockNodeData,
      edges: mockEdgeData,
    });
  }),
];
