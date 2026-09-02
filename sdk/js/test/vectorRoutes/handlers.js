import { http, HttpResponse } from 'msw';
import { mockVectorGuid, vectorMockApiResponse, vectorData, vectorSearchResultData } from './mockData';
import { mockEndpoint, mockTenantId } from '../setupTest';
import { toEnumerationEnvelope } from '../enumerationEnvelope';

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

    // Create multiple vectors
    http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}/vectors/bulk`, ({ request, params, cookies }) => {
        return HttpResponse.json(vectorMockApiResponse);
    }),

    // Read all vectors (enumeration envelope)
    http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/vectors`, ({ request, params, cookies }) => {
        // Return an enumeration envelope of vectors
        return HttpResponse.json(toEnumerationEnvelope(vectorMockApiResponse));
    }),

    // Vector search (enumeration envelope of VectorSearchResult objects)
    http.post(`${mockEndpoint}v1.0/tenants/${mockTenantId}/vectors`, ({ request, params, cookies }) => {
        return HttpResponse.json(toEnumerationEnvelope(vectorSearchResultData));
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
