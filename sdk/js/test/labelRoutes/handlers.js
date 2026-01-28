import { http, HttpResponse } from 'msw';
import { mockLabelGuid, labelMockApiResponse, labelData } from './mockData';
import { mockEndpoint, mockTenantId } from '../setupTest';

export const handlers = [
    // Check if a label exists by GUID
    http.head(`${mockEndpoint}v1.0/tenants/${mockTenantId}/labels/${mockLabelGuid}`, ({ request, params, cookies }) => {
        return HttpResponse.text('true'); // Simulating label exists
    }),

    // Create a label
    http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}/labels`, ({ request, params, cookies }) => {
        // // Return the created edge, using `mockEdgeGuid` and edgeData for consistency
        return HttpResponse.json(labelData);
    }),

    // Read all tenants
    http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/labels`, ({ request, params, cookies }) => {
        // Return an array of tenants
        return HttpResponse.json(labelMockApiResponse);
    }),

    // Read a specific label by GUID
    http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/labels/${mockLabelGuid}`, ({ request, params, cookies }) => {
        return HttpResponse.json(labelData);
    }),

    // Update a label
    http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}/labels/${mockLabelGuid}`, ({ request, params, cookies }) => {
        // // Update the edge data in `edgeData`
        return HttpResponse.json(labelData);
    }),

    // Delete a label
    http.delete(`${mockEndpoint}v1.0/tenants/${mockTenantId}/labels/${mockLabelGuid}`, ({ request, params, cookies }) => {
        // Simulate edge deletion
        return HttpResponse.json(labelData);
    }),
];
