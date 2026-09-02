import {
  mockGraphGuid,
  graphData,
  searchGraphData,
  graphMockApiResponse,
  graphMockSearchApiResponse,
  subgraphExtractionRequest,
} from './mockData';
import { api } from '../setupTest'; // Adjust paths as needed
import { handlers } from './handlers';
import { getServer } from '../server';
import Graph from '../../src/models/Graph';
import SearchResult from '../../src/models/SearchResult';

const server = getServer(handlers);

describe('GraphRoute Tests', () => {
  beforeAll(() => {
    server.listen();
  });
  afterEach(() => {
    server.resetHandlers();
  });
  afterAll(() => server.close());

  describe('GraphRoute', () => {
    test('should check if graph exists by GUID', async () => {
      const response = await api.graphExists(mockGraphGuid);
      expect(response).toBe(true); // Assuming the mock returns true
    });

    test('should create a graph', async () => {
      const response = await api.createGraph({
        GUID: '01010101-0101-0101-0101-010101010101',
        GraphGUID: '01010101-0101-0101-0101-010101010101',
        Name: 'Sample Node',
        Data: {
          key1: 'value1',
        },
        CreatedUtc: '2024-10-19T14:35:20.351Z',
      });
      expect(response.GUID).toEqual(mockGraphGuid);
      expect(true).toBe(response instanceof Graph);
      expect(JSON.stringify(response)).toBe(JSON.stringify(new Graph(graphData[mockGraphGuid])));
    });

    it('throws error when creating a Graph', async () => {
      try {
        await api.createGraph();
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: Graph is null or empty');
      }
    });

    test('should read all graphs', async () => {
      const response = await api.readGraphs();
      expect(response.TotalRecords).toBe(2);
      expect(Array.isArray(response.Objects)).toBe(true);
      response.Objects.map((graph) => {
        expect(JSON.stringify(graph)).toBe(JSON.stringify(new Graph(graphData[graph.GUID])));
      });
    });

    test('should search graphs', async () => {
      const searchRequest = {
        Ordering: 'CreatedDescending',
        Expr: {
          Left: 'Hello',
          Operator: 'Equals',
          Right: 'World',
        },
      };
      const response = await api.searchGraphs(searchRequest);
      expect(JSON.stringify(response)).toEqual(JSON.stringify(new SearchResult(searchGraphData[mockGraphGuid])));
    });

    test('should read a specific graph by GUID', async () => {
      const response = await api.readGraph(mockGraphGuid);
      expect(response.GUID).toEqual(mockGraphGuid);
    });

    test('should update a graph', async () => {
      const updatedGraphData = {
        GUID: '01010101-0101-0101-0101-010101010101',
        GraphGUID: '01010101-0101-0101-0101-010101010101',
        Name: 'Sample Node',
        Data: {
          key1: 'value1',
        },
        CreatedUtc: '2024-10-19T14:35:20.351Z',
      };
      const response = await api.updateGraph(updatedGraphData);
      expect(true).toBe(response instanceof Graph);
      expect(JSON.stringify(response)).toBe(JSON.stringify(new Graph(graphData[mockGraphGuid])));
    });

    it('throws error when if missed graph data while updating a Graph', async () => {
      try {
        await api.updateGraph();
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: Graph is null or empty');
      }
    });

    test('should delete a graph', async () => {
      const response = await api.deleteGraph(mockGraphGuid);
      expect(response).toBeUndefined(); // Assuming delete operation returns nothing
    });

    test('should export a graph to GEXF format', async () => {
      const response = await api.exportGraphToGexf(mockGraphGuid);
      expect(response).toContain('<?xml'); // Checking for GEXF format
    });

    test('should export a graph to GEXF format with abort', async () => {
      const cancellationToken = {};
      await api.exportGraphToGexf(mockGraphGuid, cancellationToken);
      cancellationToken.abort();
    });

    test('should export a graph to JSONL format', async () => {
      const response = await api.exportGraphToJsonl(mockGraphGuid, {
        includeData: true,
        includeSubordinates: true,
      });
      expect(response).toContain('# litegraph-jsonl');
      expect(response).toContain('"Type":"Node"');
    });

    it('throws error when exporting to JSONL without a graph GUID', async () => {
      try {
        await api.exportGraphToJsonl();
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: GraphGuid is null or empty');
      }
    });

    test('should export a subgraph to JSONL format', async () => {
      const response = await api.exportSubgraphToJsonl(mockGraphGuid, subgraphExtractionRequest);
      expect(response).toContain('# litegraph-jsonl');
      expect(response).toContain('"Type":"Edge"');
    });

    it('throws error when exporting a subgraph without a request', async () => {
      try {
        await api.exportSubgraphToJsonl(mockGraphGuid);
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: SubgraphExtractionRequest is null or empty');
      }
    });

    test('should import JSONL into an existing graph', async () => {
      const jsonl = await api.exportGraphToJsonl(mockGraphGuid);
      const response = await api.importGraphFromJsonl(mockGraphGuid, jsonl, {
        guidStrategy: 'preserve',
        onError: 'skip',
        batchSize: 100,
      });
      expect(response.Success).toBe(true);
      expect(response.GraphGUID).toBe(mockGraphGuid);
      expect(response.NodesCreated).toBe(2);
      expect(response.EdgesCreated).toBe(1);
    });

    test('should import JSONL as a new graph', async () => {
      const jsonl = await api.exportGraphToJsonl(mockGraphGuid);
      const response = await api.importGraphAsNewFromJsonl(jsonl, {
        guidStrategy: 'regenerate',
      });
      expect(response.Success).toBe(true);
      expect(response.GraphsCreated).toBe(1);
    });

    it('throws error when importing JSONL without data', async () => {
      try {
        await api.importGraphFromJsonl(mockGraphGuid);
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: JsonlString is null or empty');
      }
    });
  });
});
