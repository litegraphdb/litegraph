import { http, HttpResponse } from 'msw';
import { mockTagGuid, tagData, tagMockApiResponse } from './mockData';
import { mockEndpoint, mockTenantId } from '../setupTest';

export const handlers = [
    // Check if a tag exists by GUID
    http.head(`${mockEndpoint}v1.0/tenants/${mockTenantId}/tags/${mockTagGuid}`, ({ request, params, cookies }) => {
        return HttpResponse.text('true'); // Simulating tag exists
    }),

    // Create a tag
    http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}/tags`, ({ request, params, cookies }) => {
        // // Return the created tag, using tagData for consistency
        return HttpResponse.json(tagData);
    }),

    // Read all tags
    http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/tags`, ({ request, params, cookies }) => {
        // Return an array of tags
        return HttpResponse.json(tagMockApiResponse);
    }),

    // Read a specific tag by GUID
    http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/tags/${mockTagGuid}`, ({ request, params, cookies }) => {
        return HttpResponse.json(tagData);
    }),

    // Update a tag
    http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}/tags/${mockTagGuid}`, ({ request, params, cookies }) => {
        // // Update the tag data in `tagData`
        return HttpResponse.json(tagData);
    }),

    // Delete a tag
    http.delete(`${mockEndpoint}v1.0/tenants/${mockTenantId}/tags/${mockTagGuid}`, ({ request, params, cookies }) => {
        // Simulate tag deletion
        return HttpResponse.json(tagData);
    }),
];
