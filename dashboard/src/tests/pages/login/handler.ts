// handler.ts for LoginPage
import { http, HttpResponse } from 'msw';
import { mockEndpoint } from '@/tests/config';
import { mockToken, mockTenantData } from '../mockData';

export const handlers = [
  http.get(`${mockEndpoint}v1.0/validate`, () => {
    return HttpResponse.json(true);
  }),

  http.post(`${mockEndpoint}v1.0/token`, async ({ request }) => {
    const body = (await request.json()) as { email?: string; password?: string; tenantId?: string };
    if (body.email && body.password && body.tenantId) {
      return HttpResponse.json(mockToken);
    }
    return HttpResponse.error();
  }),

  http.post(`${mockEndpoint}v1.0/tenants/email`, async ({ request }) => {
    const body = (await request.json()) as { email?: string };
    if (body.email) {
      return HttpResponse.json(mockTenantData);
    }
    return HttpResponse.json([]);
  }),
];
