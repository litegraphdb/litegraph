import { mockEndpoint } from '@/tests/config';
import { http, HttpResponse } from 'msw';
import {
  getMockEnumerateQueryData,
  mockEdgeData,
  mockGraphGUID,
  mockTenantGUID,
} from '../mockData';

export const handlers = [
  //update user
  http.post(`${mockEndpoint}v2.0/tenants/${mockTenantGUID}/graphs/${mockGraphGUID}/edges`, () => {
    return HttpResponse.json(getMockEnumerateQueryData(mockEdgeData));
  }),
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/graphs/${mockGraphGUID}/edges`, () => {
    return HttpResponse.json(mockEdgeData);
  }),
  http.put(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/graphs/${mockGraphGUID}/edges`, () => {
    return HttpResponse.json(mockEdgeData);
  }),
];
