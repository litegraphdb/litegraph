import { http, HttpResponse } from 'msw';
import { mockTenantId, tenantData, tenantMockApiResponse } from './mockData';
import { mockEndpoint } from '../setupTest';

export const handlers = [
    // Check if a tenant exists by GUID
    http.head(`${mockEndpoint}v1.0/tenants/${mockTenantId}`, ({ request, params, cookies }) => {
        return HttpResponse.text('true'); // Simulating tenant exists
    }),

    // Create a tenant
    http.put(`${mockEndpoint}v1.0/tenants`, ({ request, params, cookies }) => {
        // // Return the created edge, using `mockEdgeGuid` and edgeData for consistency
        return HttpResponse.json(tenantData);
    }),

    // Read all tenants
    http.get(`${mockEndpoint}v1.0/tenants`, ({ request, params, cookies }) => {
        // Return an array of tenants
        return HttpResponse.json(tenantMockApiResponse);
    }),

    // Read a specific tenant by GUID
    http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}`, ({ request, params, cookies }) => {
        return HttpResponse.json(tenantData);
    }),

    // Update a tenant
    http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}`, ({ request, params, cookies }) => {
        // // Update the edge data in `edgeData`
        return HttpResponse.json(tenantData);
    }),

    // Delete a tenant
    http.delete(`${mockEndpoint}v1.0/tenants/${mockTenantId}`, ({ request, params, cookies }) => {
        // Simulate edge deletion
        return HttpResponse.json(tenantData);
    }),

    // Delete force tenants
    http.delete(`${mockEndpoint}v1.0/tenants/${mockTenantId}/force`, ({ request, params, cookies }) => {
        // Simulate edge deletion
        return HttpResponse.json(tenantData);
    })
];
