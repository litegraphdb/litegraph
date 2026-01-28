import { http, HttpResponse } from 'msw';
import { mockVectorGuid, vectorMockApiResponse, vectorData } from './mockData';
import { mockEndpoint, mockTenantId } from '../setupTest';

export const handlers = [
    // Check if a vector exists by GUID
    http.head(`${mockEndpoint}v1.0/tenants/${mockTenantId}/vectors/${mockVectorGuid}`, ({ request, params, cookies }) => {
        return HttpResponse.text('true'); // Simulating vector exists
    }),

    // Create a vector
    http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}/vectors`, ({ request, params, cookies }) => {
        // // Return the created vector, using `mockVectorGuid` and vectorData for consistency
        return HttpResponse.json(vectorData);
    }),

    // Read all vectors
    http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/vectors`, ({ request, params, cookies }) => {
        // Return an array of vectors
        return HttpResponse.json(vectorMockApiResponse);
    }),

    // Read a specific vector by GUID
    http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/vectors/${mockVectorGuid}`, ({ request, params, cookies }) => {
        return HttpResponse.json(vectorData);
    }),

    // Update a vector
    http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}/vectors/${mockVectorGuid}`, ({ request, params, cookies }) => {
        // // Update the vector data in `vectorData`
        return HttpResponse.json(vectorData);
    }),

    // Delete a vector
    http.delete(`${mockEndpoint}v1.0/tenants/${mockTenantId}/vectors/${mockVectorGuid}`, ({ request, params, cookies }) => {
        // Simulate vector deletion
        return HttpResponse.json(vectorData);
    }),
];
