import { http, HttpResponse } from 'msw';
import { mockCredentialGuid, credentialMockApiResponse, credentialData } from './mockData';
import { mockEndpoint, mockTenantId } from '../setupTest';

export const handlers = [
    // Check if a credential exists by GUID
    http.head(`${mockEndpoint}v1.0/tenants/${mockTenantId}/credentials/${mockCredentialGuid}`, ({ request, params, cookies }) => {
        return HttpResponse.text('true'); // Simulating credential exists
    }),

    // Create a credential
    http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}/credentials`, ({ request, params, cookies }) => {
        // // Return the created credential, using `mockCredentialGuid` and credentialData for consistency
        return HttpResponse.json(credentialData);
    }),

    // Read all credentials
    http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/credentials`, ({ request, params, cookies }) => {
        // Return an array of credentials
        return HttpResponse.json(credentialMockApiResponse);
    }),

    // Read a specific credential by GUID
    http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/credentials/${mockCredentialGuid}`, ({ request, params, cookies }) => {
        return HttpResponse.json(credentialData);
    }),

    // Update a credential
    http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}/credentials/${mockCredentialGuid}`, ({ request, params, cookies }) => {
        // // Update the credential data in `credentialData`
        return HttpResponse.json(credentialData);
    }),

    // Delete a credential
    http.delete(`${mockEndpoint}v1.0/tenants/${mockTenantId}/credentials/${mockCredentialGuid}`, ({ request, params, cookies }) => {
        // Simulate credential deletion
        return HttpResponse.json(credentialData);
    }),
];
