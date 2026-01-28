import { mockEndpoint } from '@/tests/config';
import { http, HttpResponse } from 'msw';
import { getMockEnumerateQueryData, mockTenantData, mockTenantGUID } from '../mockData';

export const handlers = [
  //update user
  http.get(`${mockEndpoint}v2.0/tenants`, () => {
    return HttpResponse.json(getMockEnumerateQueryData(mockTenantData));
  }),
  http.get(`${mockEndpoint}v1.0/tenants`, () => {
    return HttpResponse.json(mockTenantData);
  }),
  http.put(`${mockEndpoint}v1.0/tenants`, () => {
    return HttpResponse.json(mockTenantData);
  }),
  http.put(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}`, () => {
    return HttpResponse.json(mockTenantData[0]);
  }),
  http.delete(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}`, () => {
    return HttpResponse.json(mockTenantData);
  }),
];
