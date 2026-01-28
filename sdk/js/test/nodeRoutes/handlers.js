import { http, HttpResponse } from 'msw';
import { mockGraphGuid, mockNodeGuid, nodeData, searchNodeData, nodeMockApiResponse, nodeMockSearchApiResponse } from './mockData';
import { mockEndpoint, mockTenantId } from '../setupTest';

export const handlers = [
    // Check if a node exists by GUID
    http.head(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes/${mockNodeGuid}`, ({ request, params, cookies }) => {
        return HttpResponse.text('true'); // Simulating node exists
    }),

    // Create a node
    http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes`, ({ request, params, cookies }) => {
        // // Return the created node, using `mockNodeGuid` and nodeData for consistency
        return HttpResponse.json(nodeData[mockNodeGuid]);
    }),

    // Create multiple node
    http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes/multiple`, ({ request, params, cookies }) => {
        // // Return the created node, using `mockNodeGuid` and nodeData for consistency
        return HttpResponse.json(nodeData[mockNodeGuid]);
    }),

    // Read all nodes for a specific graph
    http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes`, ({ request, params, cookies }) => {
        // Return an array of nodes related to the graph
        return HttpResponse.json(nodeMockApiResponse);
    }),

    // Search nodes
    http.post(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes/search`, ({ request, params, cookies }) => {
        const searchRequest = request.body;
        // Simulate search by returning the mock search result
        return HttpResponse.json(searchNodeData[mockNodeGuid]);
    }),

    // Read a specific node by GUID
    http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes/${mockNodeGuid}`, ({ request, params, cookies }) => {
        return HttpResponse.json(nodeData[mockNodeGuid]);
    }),

    // Update a node
    http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes/${mockNodeGuid}`, ({ request, params, cookies }) => {
        // // Update the node data in `nodeData`
        return HttpResponse.json(nodeData[mockNodeGuid]);
    }),

    // Delete a node
    http.delete(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes/${mockNodeGuid}`, ({ request, params, cookies }) => {
        // Simulate node deletion
        return HttpResponse.json(nodeData[mockNodeGuid]);
    }),

    // Delete all nodes within graph
    http.delete(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes/all`, ({ request, params, cookies }) => {
        // Simulate node deletion
        return HttpResponse.json(nodeData[mockNodeGuid]);
    }),

    // Delete multiple node
    http.delete(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes/multiple`, ({ request, params, cookies }) => {
        // Simulate node deletion
        return HttpResponse.json(nodeData[mockNodeGuid]);
    }),

    // If node does not exist (for negative test cases)
    http.head(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes/wrongNodeGuid`, ({ request, params, cookies }) => {
        return HttpResponse.text('false').status(404);
    })
];
