import { api } from '../setupTest'; // Adjust paths as needed
import { handlers } from './handlers';
import { getServer } from '../server';
import { mockGraphGuid, mockNodeGuid, fromNodeGuid, toNodeGuid, edgeData, nodeData, routesData, edgeMockApiResponse, nodeMockApiResponse, routesMockApiResponse } from './mockData';
import Edge from '../../src/models/Edge';
import Node from '../../src/models/Node';
import RouteResult from '../../src/models/RouteResult';

const server = getServer(handlers);

beforeAll(() => server.listen());
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

describe('Node Routes and Traversal', () => {

    it('should get edges from a node (success)', async () => {
        const response = await api.getEdgesFromNode(mockGraphGuid, mockNodeGuid);
        // expect(response).toEqual(edgeMockApiResponse);
        response.map((edge) => {
            expect(JSON.stringify(edge)).toBe(JSON.stringify(new Edge(edgeData[edge.GUID])));
        });
    });

    it('should get edges to a node (success)', async () => {
        const response = await api.getEdgesToNode(mockGraphGuid, mockNodeGuid);
        // expect(response).toEqual(edgeMockApiResponse);
        response.map((edge) => {
            expect(JSON.stringify(edge)).toBe(JSON.stringify(new Edge(edgeData[edge.GUID])));
        });
    });

    it('should get edges between nodes (success)', async () => {
        const response = await api.getEdgesBetween(mockGraphGuid, fromNodeGuid, toNodeGuid);
        // expect(response).toEqual(edgeMockApiResponse);
        response.map((edge) => {
            expect(JSON.stringify(edge)).toBe(JSON.stringify(new Edge(edgeData[edge.GUID])));
        });
    });

    it('should get all edges for a node (success)', async () => {
        const response = await api.getAllNodeEdges(mockGraphGuid, mockNodeGuid);
        // expect(response).toEqual(edgeMockApiResponse);
        response.map((edge) => {
            expect(JSON.stringify(edge)).toBe(JSON.stringify(new Edge(edgeData[edge.GUID])));
        });
    });

    it('should get child nodes from a node (success)', async () => {
        const response = await api.getChildrenFromNode(mockGraphGuid, mockNodeGuid);
        // expect(response).toEqual(nodeMockApiResponse);
        response.map((node) => {
            expect(JSON.stringify(node)).toBe(JSON.stringify(new Node(nodeData[node.GUID])));
        });

    });

    it('should get parent nodes from a node (success)', async () => {
        const response = await api.getParentsFromNode(mockGraphGuid, mockNodeGuid);
        // expect(response).toEqual(nodeMockApiResponse);
        response.map((node) => {
            expect(JSON.stringify(node)).toBe(JSON.stringify(new Node(nodeData[node.GUID])));
        });
    });

    it('should get neighboring nodes (success)', async () => {
        const response = await api.getNodeNeighbors(mockGraphGuid, mockNodeGuid);
        // expect(response).toEqual(nodeMockApiResponse);
        response.map((node) => {
            expect(JSON.stringify(node)).toBe(JSON.stringify(new Node(nodeData[node.GUID])));
        });
    });

    it('should get routes between nodes (success)', async () => {
        const response = await api.getRoutes(mockGraphGuid, fromNodeGuid, toNodeGuid);
        expect(JSON.stringify(response)).toEqual(JSON.stringify(new RouteResult(routesData[mockNodeGuid])));
    });
});
