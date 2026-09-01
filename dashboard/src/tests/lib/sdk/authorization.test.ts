import {
  createAuthorizationRole,
  deleteCredentialScopeAssignment,
  getUserEffectivePermissions,
  listAuthorizationRoles,
} from '@/lib/sdk/authorization';
import { sdk, setAccessKey, setEndpoint } from '@/lib/sdk/litegraph.service';
import { mockEndpoint } from '@/tests/config';
import { mockTenantGUID } from '@/tests/pages/mockData';

describe('authorization sdk helpers', () => {
  const originalFetch = global.fetch;

  beforeEach(() => {
    setEndpoint(mockEndpoint);
    setAccessKey('test-access-key');
    // The real SdkConfiguration's accessKey setter maintains defaultHeaders; the jest-mocked
    // litegraphdb has no such side effect, so seed the header the same way the SDK would.
    (sdk.config as unknown as { defaultHeaders: Record<string, string> }).defaultHeaders = {
      Authorization: 'Bearer test-access-key',
    };
    global.fetch = jest.fn().mockImplementation(() =>
      Promise.resolve({
        ok: true,
        status: 200,
        statusText: 'OK',
        text: jest
          .fn()
          .mockResolvedValue(JSON.stringify({ Objects: [], Page: 0, PageSize: 1000, TotalCount: 0 })),
      })
    );
  });

  afterEach(() => {
    global.fetch = originalFetch;
    jest.clearAllMocks();
  });

  it('lists authorization roles with built-ins included by default', async () => {
    await listAuthorizationRoles(mockTenantGUID);

    expect(global.fetch).toHaveBeenCalledWith(
      `${mockEndpoint}v1.0/tenants/${mockTenantGUID}/roles?page=0&pageSize=1000&includeBuiltIns=true`,
      expect.objectContaining({
        method: 'GET',
        headers: expect.objectContaining({
          Authorization: 'Bearer test-access-key',
        }),
      })
    );
  });

  it('creates custom roles through the tenant authorization endpoint', async () => {
    await createAuthorizationRole(mockTenantGUID, {
      Name: 'GraphAuditor',
      ResourceScope: 'Graph',
      Permissions: ['Read'],
      ResourceTypes: ['Graph'],
      InheritsToGraphs: false,
    });

    expect(global.fetch).toHaveBeenCalledWith(
      `${mockEndpoint}v1.0/tenants/${mockTenantGUID}/roles`,
      expect.objectContaining({
        method: 'PUT',
        body: expect.stringContaining('GraphAuditor'),
      })
    );
  });

  it('builds subject-scoped permission and credential scope routes', async () => {
    await getUserEffectivePermissions(mockTenantGUID, 'user-guid', 'graph-guid');
    await deleteCredentialScopeAssignment(mockTenantGUID, 'credential-guid', 'scope-guid');

    expect(global.fetch).toHaveBeenNthCalledWith(
      1,
      `${mockEndpoint}v1.0/tenants/${mockTenantGUID}/users/user-guid/permissions?graphGuid=graph-guid`,
      expect.objectContaining({ method: 'GET' })
    );
    expect(global.fetch).toHaveBeenNthCalledWith(
      2,
      `${mockEndpoint}v1.0/tenants/${mockTenantGUID}/credentials/credential-guid/scopes/scope-guid`,
      expect.objectContaining({ method: 'DELETE' })
    );
  });
});
