import { http, HttpResponse } from 'msw';
import { mockGraphGuid, graphData, searchGraphData, graphGEXFData, graphMockApiResponse, graphMockSearchApiResponse } from './mockData';
import { mockEndpoint, mockTenantId } from '../setupTest';

export const handlers = [
    http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs`, ({ request, params, cookies }) => {
        return HttpResponse.json(graphMockApiResponse);
    }),
    http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}`, ({ request, params, cookies }) => {
        return HttpResponse.json(graphData[mockGraphGuid]);
    }),
    http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs`, ({ request, params, cookies }) => {
        return HttpResponse.json(graphData[mockGraphGuid]);
    }),
    http.put(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}`, ({ request, params, cookies }) => {
        return HttpResponse.json(graphData[mockGraphGuid]);
    }),
    http.delete(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}`, ({ request, params, cookies }) => {
        return HttpResponse.json(graphData[mockGraphGuid]);
    }),
    http.head(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}`, ({ request, params, cookies }) => {
        return HttpResponse.text('true');
    }),
    http.head(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${'wrongID'}`, ({ request, params, cookies }) => {
        return HttpResponse.text('false').status(404);
    }),
    http.post(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/search`, ({ request, params, cookies }) => {
        return HttpResponse.json(searchGraphData[mockGraphGuid]);
    }),
    http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/export/gexf`, ({ request, params, cookies }) => {
        return HttpResponse.json(graphGEXFData[mockGraphGuid]);
    }),
];
