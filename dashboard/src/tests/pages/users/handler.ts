import { mockEndpoint } from '@/tests/config';
import { http, HttpResponse } from 'msw';
import { getMockEnumerateQueryData, mockTenantGUID, mockUserData } from '../mockData';

export const handlers = [
  //update user
  http.get(`${mockEndpoint}v2.0/tenants/${mockTenantGUID}/users`, () => {
    return HttpResponse.json(getMockEnumerateQueryData(mockUserData));
  }),
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/users`, () => {
    return HttpResponse.json(mockUserData);
  }),
  http.delete(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/users/${mockUserData[0].GUID}`, () => {
    return HttpResponse.json(mockUserData[0]);
  }),
  http.put(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/users/${mockUserData[0].GUID}`, () => {
    return HttpResponse.json(mockUserData[0]);
  }),
  http.put(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/users`, () => {
    return HttpResponse.json(mockUserData[0]);
  }),
  // Create user endpoint
  http.post(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/users`, () => {
    return HttpResponse.json(mockUserData[0]);
  }),
];
