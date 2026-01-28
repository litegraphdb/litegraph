import { mockEndpoint } from '@/tests/config';
import { http, HttpResponse } from 'msw';
import { mockTenantData } from '../mockData';

export const handlers = [
  // Validate connectivity
  http.get(`${mockEndpoint}v1.0/validate-connectivity`, () => {
    return HttpResponse.json({ success: true, message: 'Connection successful' });
  }),

  // Get tenants
  http.get(`${mockEndpoint}v1.0/tenants`, () => {
    return HttpResponse.json(mockTenantData);
  }),

  // Login endpoint (if you have a specific login endpoint)
  http.post(`${mockEndpoint}v1.0/auth/admin/login`, async ({ request }) => {
    const body = await request.json();
    const { accessKey, url } = body as { accessKey: string; url: string };

    if (accessKey === 'valid-access-key') {
      return HttpResponse.json({
        success: true,
        token: 'mock-admin-token',
        tenants: mockTenantData,
      });
    }

    return HttpResponse.json({ success: false, message: 'Invalid access key' }, { status: 401 });
  }),

  // Error scenario for validate connectivity
  http.get(`${mockEndpoint}v1.0/validate-connectivity-error`, () => {
    return HttpResponse.json({ success: false, message: 'Server not reachable' }, { status: 500 });
  }),

  // Error scenario for get tenants
  http.get(`${mockEndpoint}v1.0/tenants-error`, () => {
    return HttpResponse.json({ success: false, message: 'Access denied' }, { status: 403 });
  }),
];
