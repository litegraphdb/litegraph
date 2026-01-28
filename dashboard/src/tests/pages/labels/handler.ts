import { mockEndpoint } from '@/tests/config';
import { http, HttpResponse } from 'msw';
import {
  getMockEnumerateQueryData,
  mockGraphGUID,
  mockLabelData,
  mockNodeData,
  mockTenantGUID,
} from '../mockData';

export const handlers = [
  //update user
  http.post(`${mockEndpoint}v2.0/tenants/${mockTenantGUID}/labels`, () => {
    return HttpResponse.json(getMockEnumerateQueryData(mockLabelData));
  }),
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/labels`, () => {
    return HttpResponse.json(mockNodeData);
  }),
  http.put(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/labels`, () => {
    return HttpResponse.json(mockLabelData);
  }),
  http.delete(
    `${mockEndpoint}v1.0/tenants/${mockTenantGUID}/labels/${mockLabelData[0].GUID}`,
    () => {
      return HttpResponse.json(mockLabelData);
    }
  ),
  http.put(`${mockEndpoint}v1.0/tenants/${mockTenantGUID}/labels/${mockLabelData[0].GUID}`, () => {
    return HttpResponse.json(mockLabelData[0]);
  }),
];
