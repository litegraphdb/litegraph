// tests/components/layout/DashboardLayout/handler.ts
import { mockEndpoint } from '@/tests/config';
import {
  mockAdminUser,
  mockGraphData,
  mockTenant,
  mockTenantData,
  mockToken,
  mockUser,
} from '@/tests/pages/mockData';
import { http, HttpResponse } from 'msw';

export const handlers = [
  // Get all graphs
  http.get(`${mockEndpoint}v1.0/graphs`, () => {
    return HttpResponse.json(mockGraphData);
  }),

  // Get all tenants
  http.get(`${mockEndpoint}v1.0/tenants`, () => {
    return HttpResponse.json(mockTenantData);
  }),

  // Get graphs with error
  http.get(`${mockEndpoint}v1.0/graphs-error`, () => {
    return HttpResponse.json(
      { success: false, message: 'Failed to fetch graphs' },
      { status: 500 }
    );
  }),

  // Get tenants with error
  http.get(`${mockEndpoint}v1.0/tenants-error`, () => {
    return HttpResponse.json(
      { success: false, message: 'Failed to fetch tenants' },
      { status: 500 }
    );
  }),

  // Logout endpoint
  http.post(`${mockEndpoint}v1.0/auth/logout`, () => {
    return HttpResponse.json({ success: true, message: 'Logged out successfully' });
  }),

  // Set tenant
  http.post(`${mockEndpoint}v1.0/tenant/set`, async ({ request }) => {
    const body = await request.json();
    const { tenantId } = body as { tenantId: string };

    return HttpResponse.json({
      success: true,
      message: 'Tenant set successfully',
      tenantId,
    });
  }),
];

export const authHandlers = [
  // Token validation endpoint
  http.post(`${mockEndpoint}v1.0/auth/validate-token`, async ({ request }) => {
    const authHeader = request.headers.get('Authorization');

    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      return HttpResponse.json(
        { success: false, message: 'Invalid or missing token' },
        { status: 401 }
      );
    }

    const token = authHeader.replace('Bearer ', '');

    if (token === 'mock-jwt-token-12345') {
      return HttpResponse.json({
        success: true,
        valid: true,
        user: mockUser,
        expiresAt: mockToken.ExpiresIn,
      });
    }

    return HttpResponse.json(
      { success: false, message: 'Invalid token', valid: false },
      { status: 401 }
    );
  }),

  // Refresh token endpoint
  http.post(`${mockEndpoint}v1.0/auth/refresh`, async ({ request }) => {
    const body = await request.json();
    const { refreshToken } = body as { refreshToken: string };

    if (refreshToken === 'mock-refresh-token-67890') {
      return HttpResponse.json({
        success: true,
        token: {
          ...mockToken,
          Token: 'new-mock-jwt-token-12345',
          RefreshToken: 'new-mock-refresh-token-67890',
          IssuedAt: new Date().toISOString(),
        },
      });
    }

    return HttpResponse.json({ success: false, message: 'Invalid refresh token' }, { status: 401 });
  }),

  // Get current user endpoint
  http.get(`${mockEndpoint}v1.0/auth/me`, ({ request }) => {
    const authHeader = request.headers.get('Authorization');

    if (!authHeader || !authHeader.startsWith('Bearer ')) {
      return HttpResponse.json(
        { success: false, message: 'Authentication required' },
        { status: 401 }
      );
    }

    return HttpResponse.json({
      success: true,
      user: mockUser,
    });
  }),

  // Admin authentication validation
  http.post(`${mockEndpoint}v1.0/auth/admin/validate`, async ({ request }) => {
    const body = await request.json();
    const { accessKey } = body as { accessKey: string };

    if (accessKey === 'admin-access-key-12345') {
      return HttpResponse.json({
        success: true,
        valid: true,
        user: mockAdminUser,
        permissions: mockAdminUser.permissions,
      });
    }

    return HttpResponse.json(
      { success: false, message: 'Invalid admin access key', valid: false },
      { status: 403 }
    );
  }),

  // Tenant validation endpoint
  http.get(`${mockEndpoint}v1.0/tenants/:tenantId/validate`, ({ params }) => {
    const { tenantId } = params;

    if (tenantId === 'tenant-123-456-789') {
      return HttpResponse.json({
        success: true,
        valid: true,
        tenant: mockTenant,
      });
    }

    return HttpResponse.json(
      { success: false, message: 'Tenant not found', valid: false },
      { status: 404 }
    );
  }),

  // Server endpoint validation
  http.get(`${mockEndpoint}v1.0/health`, () => {
    return HttpResponse.json({
      success: true,
      status: 'healthy',
      timestamp: new Date().toISOString(),
      version: '1.0.0',
    });
  }),

  // Logout endpoint
  http.post(`${mockEndpoint}v1.0/auth/logout`, async ({ request }) => {
    const authHeader = request.headers.get('Authorization');

    if (!authHeader) {
      return HttpResponse.json({ success: false, message: 'No token provided' }, { status: 400 });
    }

    return HttpResponse.json({
      success: true,
      message: 'Successfully logged out',
    });
  }),

  // Admin logout endpoint
  http.post(`${mockEndpoint}v1.0/auth/admin/logout`, () => {
    return HttpResponse.json({
      success: true,
      message: 'Admin successfully logged out',
    });
  }),

  // Get user preferences/settings
  http.get(`${mockEndpoint}v1.0/user/preferences`, ({ request }) => {
    const authHeader = request.headers.get('Authorization');

    if (!authHeader) {
      return HttpResponse.json(
        { success: false, message: 'Authentication required' },
        { status: 401 }
      );
    }

    return HttpResponse.json({
      success: true,
      preferences: {
        theme: 'light',
        language: 'en',
        notifications: true,
        autoSave: true,
      },
    });
  }),

  // Error scenarios for testing
  http.get(`${mockEndpoint}v1.0/auth/error-test`, () => {
    return HttpResponse.json({ success: false, message: 'Internal server error' }, { status: 500 });
  }),

  // Network timeout simulation
  http.get(`${mockEndpoint}v1.0/auth/timeout-test`, async () => {
    // Simulate a timeout
    await new Promise((resolve) => setTimeout(resolve, 30000));
    return HttpResponse.json({ success: true });
  }),

  // Session expiry endpoint
  http.get(`${mockEndpoint}v1.0/auth/session-expired`, () => {
    return HttpResponse.json(
      {
        success: false,
        message: 'Session expired',
        code: 'SESSION_EXPIRED',
      },
      { status: 401 }
    );
  }),

  // Multiple tenants endpoint (for testing tenant switching)
  http.get(`${mockEndpoint}v1.0/user/tenants`, ({ request }) => {
    const authHeader = request.headers.get('Authorization');

    if (!authHeader) {
      return HttpResponse.json(
        { success: false, message: 'Authentication required' },
        { status: 401 }
      );
    }

    return HttpResponse.json({
      success: true,
      tenants: [
        mockTenant,
        {
          ...mockTenant,
          GUID: 'tenant-987-654-321',
          Name: 'Another Test Tenant',
          Description: 'Another test tenant',
        },
      ],
    });
  }),

  // Initialize session endpoint
  http.post(`${mockEndpoint}v1.0/auth/initialize`, async ({ request }) => {
    const body = await request.json();
    const { token, tenantId, adminAccessKey } = body as {
      token?: string;
      tenantId?: string;
      adminAccessKey?: string;
    };

    const response: any = {
      success: true,
      initialized: true,
      timestamp: new Date().toISOString(),
    };

    if (token) {
      response.tokenValid = token === 'mock-jwt-token-12345';
      response.user = response.tokenValid ? mockUser : null;
    }

    if (tenantId) {
      response.tenantValid = tenantId === 'tenant-123-456-789';
      response.tenant = response.tenantValid ? mockTenant : null;
    }

    if (adminAccessKey) {
      response.adminValid = adminAccessKey === 'admin-access-key-12345';
      response.adminUser = response.adminValid ? mockAdminUser : null;
    }

    return HttpResponse.json(response);
  }),
];
