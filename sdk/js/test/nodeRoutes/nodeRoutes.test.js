import {
  mockNodeGuid,
  mockGraphGuid,
  nodeData,
  searchNodeData,
  mockNodeGuids,
  mockEmptyNodeGuids,
  nodeMockApiResponse,
  nodeMockSearchApiResponse,
} from './mockData';
import { api } from '../setupTest'; // Adjust paths as needed
import { handlers } from './handlers';
import { getServer } from '../server';
import Node from '../../src/models/Node';
import SearchResult from '../../src/models/SearchResult';

const server = getServer(handlers);

describe('NodeRoute Tests', () => {
  beforeAll(() => {
    server.listen();
  });

  afterEach(() => {
    server.resetHandlers();
  });

  afterAll(() => {
    server.close();
  });

  describe('NodeRoute', () => {
    test('should check if node exists by GUID', async () => {
      const response = await api.nodeExists(mockGraphGuid, mockNodeGuid);
      expect(response).toBe(true); // Assuming the mock returns true
    });
    test('should check if node exists by GUID with abort', async () => {
      const cancellationToken = {};
      await api.nodeExists(mockGraphGuid, mockNodeGuid, cancellationToken);
      cancellationToken.abort();
    });

    test('should create a node', async () => {
      const newNode = {
        GUID: '01010101-0101-0101-0101-010101010101',
        GraphGUID: '01010101-0101-0101-0101-010101010101',
        Name: 'Sample Node',
        Data: {
          key1: 'value1',
        },
        CreatedUtc: '2024-10-19T14:35:20.351Z',
      };
      const response = await api.createNode(newNode);
      expect(JSON.stringify(response)).toBe(JSON.stringify(new Node(nodeData[mockNodeGuid])));
    });
    test('should create a node with abort', async () => {
      const newNode = {
        GUID: '01010101-0101-0101-0101-010101010101',
        GraphGUID: '01010101-0101-0101-0101-010101010101',
        Name: 'Sample Node',
        Data: {
          key1: 'value1',
        },
        CreatedUtc: '2024-10-19T14:35:20.351Z',
      };
      const cancellationToken = {};
      await api.createNode(newNode, cancellationToken);
      cancellationToken.abort();
    });

    it('throws error when creating a Node', async () => {
      try {
        await api.createNode();
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: Node is null or empty');
      }
    });

    test('should read all nodes of a graph', async () => {
      const response = await api.readNodes(mockGraphGuid);
      response.map((node) => {
        expect(JSON.stringify(node)).toBe(JSON.stringify(new Node(nodeData[node.GUID])));
      });
    });
    test('should read all nodes of a graph with abort', async () => {
      const cancellationToken = {};
      await api.readNodes(mockGraphGuid, cancellationToken);
      cancellationToken.abort();
    });

    test('should search nodes', async () => {
      const searchRequest = {
        GraphGUID: mockNodeGuid,
        Ordering: 'CreatedDescending',
        Expr: {
          Left: 'Hello',
          Operator: 'Equals',
          Right: 'World',
        },
      };
      const response = await api.searchNodes(mockGraphGuid, searchRequest);
      expect(JSON.stringify(response)).toEqual(JSON.stringify(new SearchResult(searchNodeData[mockNodeGuid])));
    });

    test('should search nodes with abort', async () => {
      const cancellationToken = {};
      const searchRequest = {
        GraphGUID: mockNodeGuid,
        Ordering: 'CreatedDescending',
        Expr: {
          Left: 'Hello',
          Operator: 'Equals',
          Right: 'World',
        },
      };
      await api.searchNodes(mockGraphGuid, searchRequest, cancellationToken);
      cancellationToken.abort();
    });

    test('should read a specific node by GUID', async () => {
      const response = await api.readNode(mockGraphGuid, mockNodeGuid);
      expect(response.GUID).toEqual(mockNodeGuid);
    });

    test('should read a specific node by GUID with abort', async () => {
      const cancellationToken = {};
      await api.readNode(mockGraphGuid, mockNodeGuid, cancellationToken);
      cancellationToken.abort();
    });

    test('should update a node', async () => {
      const updatedNodeData = {
        GUID: '01010101-0101-0101-0101-010101010101',
        GraphGUID: '01010101-0101-0101-0101-010101010101',
        Name: 'Updated Node',
        Data: {
          key1: 'updatedValue',
        },
        CreatedUtc: '2024-10-19T14:35:20.351Z',
      };
      const response = await api.updateNode(updatedNodeData);
      expect(response instanceof Node).toBe(true);
      expect(JSON.stringify(response)).toBe(JSON.stringify(new Node(nodeData[mockNodeGuid])));
    });

    test('should update a node with abort', async () => {
      const cancellationToken = {};
      const updatedNodeData = {
        GUID: '01010101-0101-0101-0101-010101010101',
        GraphGUID: '01010101-0101-0101-0101-010101010101',
        Name: 'Updated Node',
        Data: {
          key1: 'updatedValue',
        },
        CreatedUtc: '2024-10-19T14:35:20.351Z',
      };
      await api.updateNode(updatedNodeData, cancellationToken);
      cancellationToken.abort();
    });

    it('throws error when if missed node data while updating a Node', async () => {
      try {
        await api.updateNode();
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: Node is null or empty');
      }
    });

    test('should delete a node', async () => {
      const response = await api.deleteNode(mockNodeGuid, mockNodeGuid);
      expect(response).toBeUndefined(); // Assuming delete operation returns nothing
    });

    test('should delete a node with abort', async () => {
      const cancellationToken = {};
      await api.deleteNode(mockNodeGuid, mockNodeGuid, cancellationToken);
      cancellationToken.abort();
    });

    test('should delete all nodes within a graph', async () => {
      const response = await api.deleteNodes(mockGraphGuid);
      expect(response).toBeUndefined(); // Assuming delete operation returns nothing
    });

    test('should delete all nodes within a graph with abort', async () => {
      const cancellationToken = {};
      await api.deleteNodes(mockGraphGuid, cancellationToken);
      cancellationToken.abort();
    });

    test('should delete multiple nodes within a graph', async () => {
      const response = await api.deleteMultipleNodes(mockGraphGuid, mockNodeGuids);
      expect(response).toBeUndefined(); // Assuming delete operation returns nothing
    });

    test('should throw an error when nodeGuids is null', async () => {
      try {
        await api.deleteMultipleNodes(mockGraphGuid, null);
      } catch (err) {
        expect(err.toString()).toBe('Error: ArgumentNullException: nodeGuids is null or empty');
      }
    });

    test('should return an empty array if nodeGuids is an empty array', async () => {
      const response = await api.deleteMultipleNodes(mockGraphGuid, mockEmptyNodeGuids);
      expect(response).toEqual([]);
    });

    test('should delete multiple nodes within a graph with abort', async () => {
      const cancellationToken = {};
      await api.deleteMultipleNodes(mockGraphGuid, mockNodeGuids, cancellationToken);
      cancellationToken.abort();
    });

    test('should create multiple nodes', async () => {
      const graphGuid = '01010101-0101-0101-0101-010101010101';
      const nodes = [
        {
          GUID: '01010101-0101-0101-0101-010101010101',
          GraphGUID: '01010101-0101-0101-0101-010101010101',
          Name: 'Sample Node',
          Data: {
            key1: 'value1',
          },
          CreatedUtc: '2024-10-19T14:35:20.351Z',
        },
        {
          GUID: '02020202-0202-0202-0202-020202020202',
          GraphGUID: '02020202-0202-0202-0202-020202020202',
          Name: 'Custom GUID',
          Data: {
            Hello: "World 1"
          },
          CreatedUtc: '2024-10-19T14:35:20.351Z'
        },
      ];

      const response = await api.createNodes(graphGuid, nodes);
      // expect(response).toEqual(expect.arrayContaining(nodes));
      expect(JSON.stringify(response)).toBe(JSON.stringify(new Node(nodeData[mockNodeGuid])));
    });

    test('should create multiple nodes with abort', async () => {
      const graphGuid = '01010101-0101-0101-0101-010101010101';
      const nodes = [
        {
          GUID: '01010101-0101-0101-0101-010101010101',
          GraphGUID: graphGuid,
          Name: 'Sample Node 1',
          Data: { key1: 'value1' },
          CreatedUtc: '2024-10-19T14:35:20.351Z',
        },
        {
          GUID: '02020202-0202-0202-0202-020202020202',
          GraphGUID: graphGuid,
          Name: 'Sample Node 2',
          Data: { key2: 'value2' },
          CreatedUtc: '2024-10-19T14:35:20.351Z',
        },
      ];

      const cancellationToken = new AbortController();
      await api.createNodes(graphGuid, nodes, cancellationToken);
      cancellationToken.abort();
    });

    test('should return an empty array when creating an empty list of nodes', async () => {
      const graphGuid = '01010101-0101-0101-0101-010101010101';
      const nodes = [];

      const response = await api.createNodes(graphGuid, nodes);
      expect(response).toEqual([]);
    });

    test('throws error when nodes parameter is missing', async () => {
      const graphGuid = '01010101-0101-0101-0101-010101010101';
      try {
        await api.createNodes(graphGuid);
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: Nodes is null or empty');
      }
    });

  });
});
