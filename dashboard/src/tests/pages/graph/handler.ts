import { mockEndpoint } from '@/tests/config';
import { http, HttpResponse } from 'msw';
import { mockTenantGUID, mockGraphData } from '../mockData';
import { getMockEnumerateQueryData } from '../mockData';

export const handlers = [
  http.post(`${mockEndpoint}v2.0/tenants/${mockTenantGUID}/graphs`, async (request) => {
    return HttpResponse.json(getMockEnumerateQueryData(mockGraphData));
  }),

  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/graphs`, () =>
    HttpResponse.json(mockGraphData)
  ),
  http.put(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/graphs`, () =>
    HttpResponse.json(mockGraphData[0])
  ),
  http.delete(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/graphs`, () =>
    HttpResponse.json(mockGraphData[0])
  ),
];
