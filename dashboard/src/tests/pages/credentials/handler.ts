import { mockEndpoint } from '@/tests/config';
import { http, HttpResponse } from 'msw';
import { getMockEnumerateQueryData, mockCredentialData, mockTenantGUID } from '../mockData';

export const handlers = [
  //update user
  http.get(`${mockEndpoint}v2.0/tenants/${mockTenantGUID}/credentials`, () => {
    return HttpResponse.json(getMockEnumerateQueryData(mockCredentialData));
  }),

  //create credential
  http.put(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/credentials`, () => {
    return HttpResponse.json(mockCredentialData);
  }),

  //delete credential
  http.delete(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/credentials`, () => {
    return HttpResponse.json(mockCredentialData);
  }),

  //update credential
  http.put(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/credentials`, () => {
    return HttpResponse.json(mockCredentialData);
  }),
];
