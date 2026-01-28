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

export const handlers = [
  // Get edges from a node
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes/${mockNodeGuid}/edges/from`, (req, res, ctx) => {
    return HttpResponse.json(edgeMockApiResponse);
  }),

  // Get edges to a node
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes/${mockNodeGuid}/edges/to`, (req, res, ctx) => {
    return HttpResponse.json(edgeMockApiResponse);
  }),

  // Get edges between node
  http.get(
    `${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/edges/between?from= ${fromNodeGuid} &to= ${toNodeGuid}`,
    (req, res, ctx) => {
      return HttpResponse.json(edgeMockApiResponse);
    }
  ),

  // Get all edges for a node
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes/${mockNodeGuid}/edges`, (req, res, ctx) => {
    return HttpResponse.json(edgeMockApiResponse);
  }),

  // Get child nodes from a node
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes/${mockNodeGuid}/children`, (req, res, ctx) => {
    return HttpResponse.json(nodeMockApiResponse);
  }),

  // Get parent nodes from a node
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes/${mockNodeGuid}/parents`, (req, res, ctx) => {
    return HttpResponse.json(nodeMockApiResponse);
  }),

  // Get neighboring nodes
  http.get(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/nodes/${mockNodeGuid}/neighbors`, (req, res, ctx) => {
    return HttpResponse.json(nodeMockApiResponse);
  }),

  // Get routes between nodes
  http.post(`${mockEndpoint}v1.0/tenants/${mockTenantId}/graphs/${mockGraphGuid}/routes`, (req, res, ctx) => {
    return HttpResponse.json(routesData[mockNodeGuid]);
  }),
];
