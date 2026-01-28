import { http, HttpResponse } from 'msw';
import { mockGraphGuid, mockEdgeGuid, edgeData, searchEdgeData, edgeMockApiResponse, edgeMockSearchApiResponse } from './mockData';
import { mockEndpoint, mockTenantId } from '../setupTest';

export const handlers = [
    // Check if a edge exists by GUID
    http.head(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/edges/${mockEdgeGuid}`, ({ request, params, cookies }) => {
        return HttpResponse.text('true'); // Simulating edge exists
    }),

    // Create a edge
    http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/edges`, ({ request, params, cookies }) => {
        // // Return the created edge, using `mockEdgeGuid` and edgeData for consistency
        return HttpResponse.json(edgeData[mockEdgeGuid]);
    }),

    // Create multiple edges
    http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/edges/multiple`, ({ request, params, cookies }) => {
        // // Return the created edge, using `mockEdgeGuid` and edgeData for consistency
        return HttpResponse.json(edgeData[mockEdgeGuid]);
    }),

    // Read all edges for a specific graph
    http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/edges`, ({ request, params, cookies }) => {
        // Return an array of edges related to the graph
        return HttpResponse.json(edgeMockApiResponse);
    }),

    // Search edges
    http.post(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/edges/search`, ({ request, params, cookies }) => {
        const searchRequest = request.body;
        // Simulate search by returning the mock search result
        return HttpResponse.json(searchEdgeData[mockEdgeGuid]);
    }),

    // Read a specific edge by GUID
    http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/edges/${mockEdgeGuid}`, ({ request, params, cookies }) => {
        return HttpResponse.json(edgeData[mockEdgeGuid]);
    }),

    // Update a edge
    http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/edges/${mockEdgeGuid}`, ({ request, params, cookies }) => {
        // // Update the edge data in `edgeData`
        return HttpResponse.json(edgeData[mockEdgeGuid]);
    }),

    // Delete a edge
    http.delete(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/edges/${mockEdgeGuid}`, ({ request, params, cookies }) => {
        // Simulate edge deletion
        return HttpResponse.json(edgeData[mockEdgeGuid]);
    }),

    // Delete all edges within graph
    http.delete(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/edges/all`, ({ request, params, cookies }) => {
        // Simulate edge deletion
        return HttpResponse.json(edgeData[mockEdgeGuid]);
    }),

    // Delete multiple edge
    http.delete(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/edges/multiple`, ({ request, params, cookies }) => {
        // Simulate edge deletion
        return HttpResponse.json(edgeData[mockEdgeGuid]);
    }),

    // If edge does not exist (for negative test cases)
    http.head(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/edges/wrongEdgeGuid`, ({ request, params, cookies }) => {
        return HttpResponse.text('false').status(404);
    })
];
