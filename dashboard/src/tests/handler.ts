import { mockEndpoint } from '@/tests/config';
import { http, HttpResponse } from 'msw';
import { mockGraphData, mockTenantData, mockTenantGUID } from './pages/mockData';

export const commonHandlers = [
  //get graphs
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/graphs`, () => {
    return HttpResponse.json(mockGraphData);
  }),
  //get tenants
  http.get(`${mockEndpoint}v1.0/tenants`, () => {
    return HttpResponse.json(mockTenantData);
  }),
];
