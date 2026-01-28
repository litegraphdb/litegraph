import { mockEdgeGuid, mockGraphGuid, edgeData, searchEdgeData, mockEdgeGuids, mockEmptyEdgeGuids, edgeMockApiResponse, edgeMockSearchApiResponse } from './mockData';
import { api } from '../setupTest'; // Adjust paths as needed
import { handlers } from './handlers';
import { getServer } from '../server';
import Edge from '../../src/models/Edge';
import SearchResult from '../../src/models/SearchResult';

const server = getServer(handlers);

describe('EdgeRoute Tests', () => {
  beforeAll(() => {
    server.listen();
  });

  afterEach(() => {
    server.resetHandlers();
  });

  afterAll(() => {
    server.close();
  });

  describe('EdgeRoute', () => {
    test('should check if edge exists by GUID', async () => {
      const response = await api.edgeExists(mockGraphGuid, mockEdgeGuid);
      expect(response).toBe(true); // Assuming the mock returns true
    });

    test('should create a edge', async () => {
      const newEdge = {
        GUID: '01010101-0101-0101-0101-010101010101',
        GraphGUID: '01010101-0101-0101-0101-010101010101',
        Name: 'My test edge',
        From: '2b1520be-d285-4f22-8c74-f296047162b9',
        To: '784cfa37-fb06-4f81-b10d-f1167dfe2b22',
        Cost: 10,
        Data: {
          Hello: 'World',
        },
        CreatedUtc: '2024-07-01 15:43:06.991834',
      };
      const response = await api.createEdge(newEdge);
      expect(JSON.stringify(response)).toBe(JSON.stringify(new Edge(edgeData[mockEdgeGuid])));
    });

    it('throws error when creating a Edge', async () => {
      try {
        await api.createEdge();
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: edge is null or empty');
      }
    });

    test('should read all edges of a graph', async () => {
      const response = await api.readEdges(mockGraphGuid);
      response.map((edge) => {
        expect(JSON.stringify(edge)).toBe(JSON.stringify(new Edge(edgeData[edge.GUID])));
      });
    });

    test('should search edges', async () => {
      const searchRequest = {
        Ordering: 'CreatedDescending',
        Expr: {
          Left: 'Hello',
          Operator: 'Equals',
          Right: 'World',
        },
      };
      const response = await api.searchEdges(mockGraphGuid, searchRequest);
      expect(JSON.stringify(response)).toEqual(JSON.stringify(new SearchResult(searchEdgeData[mockGraphGuid])));
    });

    test('should read a specific edge by GUID', async () => {
      const response = await api.readEdge(mockGraphGuid, mockEdgeGuid);
      expect(response.GUID).toEqual(mockEdgeGuid);
    });

    test('should update a edge', async () => {
      const updatedEdgeData = {
        GUID: '01010101-0101-0101-0101-010101010101',
        GraphGUID: '01010101-0101-0101-0101-010101010101',
        Name: 'My test edge',
        From: '2b1520be-d285-4f22-8c74-f296047162b9',
        To: '784cfa37-fb06-4f81-b10d-f1167dfe2b22',
        Cost: 10,
        Data: {
          Hello: 'World',
        },
        CreatedUtc: '2024-07-01 15:43:06.991834',
      };
      const response = await api.updateEdge(updatedEdgeData);
      expect(response instanceof Edge).toBe(true);
      expect(JSON.stringify(response)).toBe(JSON.stringify(new Edge(edgeData[mockEdgeGuid])));
    });

    it('throws error when if missed edge data while updating a Edge', async () => {
      try {
        await api.updateEdge();
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: Edge is null or empty');
      }
    });

    test('should delete a edge', async () => {
      const response = await api.deleteEdge(mockEdgeGuid, mockEdgeGuid);
      expect(response).toBeUndefined(); // Assuming delete operation returns nothing
    });

    test('should delete all edges within a graph', async () => {
      const response = await api.deleteEdges(mockGraphGuid);
      expect(response).toBeUndefined(); // Assuming delete operation returns nothing
    });

    test('should delete all edges within a graph with abort', async () => {
      const cancellationToken = {};
      await api.deleteEdges(mockGraphGuid, cancellationToken);
      cancellationToken.abort();
    });

    test('should delete multiple edges within a graph', async () => {
      const response = await api.deleteMultipleEdges(mockGraphGuid, mockEdgeGuids);
      expect(response).toBeUndefined(); // Assuming delete operation returns nothing
    });

    test('should throw an error when edgeGuids is null', async () => {
      try {
        await api.deleteMultipleEdges(mockGraphGuid, null);
      } catch (err) {
        expect(err.toString()).toBe('Error: ArgumentNullException: edgeGuids is null or empty');
      }
    });

    test('should return an empty array if edgeGuids is an empty array', async () => {
      const response = await api.deleteMultipleEdges(mockGraphGuid, mockEmptyEdgeGuids);
      expect(response).toEqual([]);
    });

    test('should delete multiple edges within a graph with abort', async () => {
      const cancellationToken = {};
      await api.deleteMultipleEdges(mockGraphGuid, mockEdgeGuids, cancellationToken);
      cancellationToken.abort();
    });

    test('should create multiple edges', async () => {
      const graphGuid = '01010101-0101-0101-0101-010101010101';
      const edges = [
        {
          GUID: '01010101-0101-0101-0101-010101010101',
          GraphGUID: '01010101-0101-0101-0101-010101010101',
          Name: 'My test edge',
          From: '2b1520be-d285-4f22-8c74-f296047162b9',
          To: '784cfa37-fb06-4f81-b10d-f1167dfe2b22',
          Cost: 10,
          Data: {
            Hello: 'World',
          },
          CreatedUtc: '2024-07-01 15:43:06.991834',
        },
        {
          GUID: '02020202-0202-0202-0202-020202020202',
          GraphGUID: '02020202-0202-0202-0202-020202020202',
          Name: "My test edge",
          From: "2b1520be-d285-4f22-8c74-f296047162b9",
          To: "784cfa37-fb06-4f81-b10d-f1167dfe2b22",
          Cost: 10,
          Data: {
            Hello: "World 1"
          },
          CreatedUtc: "2024-07-01 15:43:06.991834"
        },
      ];

      const response = await api.createEdges(graphGuid, edges);
      // expect(response).toEqual(expect.arrayContaining(edges));
      expect(JSON.stringify(response)).toBe(JSON.stringify(new Edge(edgeData[mockEdgeGuid])));
    });

    test('should create multiple edges with abort', async () => {
      const graphGuid = '01010101-0101-0101-0101-010101010101';
      const edges = [
        {
          GUID: '01010101-0101-0101-0101-010101010101',
          GraphGUID: graphGuid,
          Name: 'Sample Edge 1',
          Data: { key1: 'value1' },
          CreatedUtc: '2024-10-19T14:35:20.351Z',
        },
        {
          GUID: '02020202-0202-0202-0202-020202020202',
          GraphGUID: graphGuid,
          Name: 'Sample Edge 2',
          Data: { key2: 'value2' },
          CreatedUtc: '2024-10-19T14:35:20.351Z',
        },
      ];

      const cancellationToken = new AbortController();
      await api.createEdges(graphGuid, edges, cancellationToken);
      cancellationToken.abort();
    });

    test('should return an empty array when creating an empty list of edges', async () => {
      const graphGuid = '01010101-0101-0101-0101-010101010101';
      const edges = [];

      const response = await api.createEdges(graphGuid, edges);
      expect(response).toEqual([]);
    });

    test('throws error when edges parameter is missing', async () => {
      const graphGuid = '01010101-0101-0101-0101-010101010101';
      try {
        await api.createEdges(graphGuid);
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: Edges is null or empty');
      }
    });



  });
});
