import { http, HttpResponse } from 'msw';
import {
  mockGraphGuid,
  mockNodeGuid,
  fromNodeGuid,
  toNodeGuid,
  edgeMockApiResponse,
  routesData,
  nodeMockApiResponse,
  routeMockResponse,
  routesMockApiResponse,
} from './mockData';
import { mockEndpoint, mockTenantId } from '../setupTest';
import { toEnumerationEnvelope } from '../enumerationEnvelope';

export const handlers = [
  // Get edges from a node (enumeration envelope)
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes/${mockNodeGuid}/edges/from`, (req, res, ctx) => {
    return HttpResponse.json(toEnumerationEnvelope(edgeMockApiResponse));
  }),

  // Get edges to a node (enumeration envelope)
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes/${mockNodeGuid}/edges/to`, (req, res, ctx) => {
    return HttpResponse.json(toEnumerationEnvelope(edgeMockApiResponse));
  }),

  // Get edges between node (enumeration envelope)
  http.get(
    `${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/edges/between`,
    (req, res, ctx) => {
      return HttpResponse.json(toEnumerationEnvelope(edgeMockApiResponse));
    }
  ),

  // Get all edges for a node (enumeration envelope)
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes/${mockNodeGuid}/edges`, (req, res, ctx) => {
    return HttpResponse.json(toEnumerationEnvelope(edgeMockApiResponse));
  }),

  // Get child nodes from a node (enumeration envelope)
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes/${mockNodeGuid}/children`, (req, res, ctx) => {
    return HttpResponse.json(toEnumerationEnvelope(nodeMockApiResponse));
  }),

  // Get parent nodes from a node (enumeration envelope)
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes/${mockNodeGuid}/parents`, (req, res, ctx) => {
    return HttpResponse.json(toEnumerationEnvelope(nodeMockApiResponse));
  }),

  // Get neighboring nodes (enumeration envelope)
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes/${mockNodeGuid}/neighbors`, (req, res, ctx) => {
    return HttpResponse.json(toEnumerationEnvelope(nodeMockApiResponse));
  }),

  // Get routes between nodes
  http.post(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/routes`, (req, res, ctx) => {
    return HttpResponse.json(routesData[mockNodeGuid]);
  }),
];
