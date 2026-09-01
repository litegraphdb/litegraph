import { can, canViewSection, Principal } from '@/lib/authz/capabilities';

const TENANT_A = 'tenant-a';
const TENANT_B = 'tenant-b';
const USER_SELF = 'user-self';
const USER_OTHER = 'user-other';

const systemAdmin: Principal = {
  isSystemAdmin: true,
  isTenantAdmin: false,
  isBreakGlass: false,
  userGuid: 'sa',
  tenantGuid: TENANT_A,
};
const tenantAdmin: Principal = {
  isSystemAdmin: false,
  isTenantAdmin: true,
  isBreakGlass: false,
  userGuid: 'ta',
  tenantGuid: TENANT_A,
};
const regular: Principal = {
  isSystemAdmin: false,
  isTenantAdmin: false,
  isBreakGlass: false,
  userGuid: USER_SELF,
  tenantGuid: TENANT_A,
};
const breakGlass: Principal = {
  isSystemAdmin: false,
  isTenantAdmin: false,
  isBreakGlass: true,
  userGuid: null,
  tenantGuid: null,
};

describe('capability map — can()', () => {
  it('returns false with no principal', () => {
    expect(can(null, 'view', 'graphs')).toBe(false);
  });

  describe('SystemAdmin', () => {
    it('may view and edit everything', () => {
      for (const resource of [
        'graphs',
        'tenants',
        'users',
        'credentials',
        'authorization',
        'backups',
        'settings',
      ] as const) {
        expect(can(systemAdmin, 'view', resource)).toBe(true);
        expect(can(systemAdmin, 'edit', resource)).toBe(true);
      }
    });
    it('may edit users in any tenant', () => {
      expect(can(systemAdmin, 'edit', 'users', { tenantGuid: TENANT_B })).toBe(true);
    });
  });

  describe('break-glass', () => {
    it('is superuser-equivalent', () => {
      expect(can(breakGlass, 'edit', 'settings')).toBe(true);
      expect(can(breakGlass, 'edit', 'tenants', { tenantGuid: TENANT_B })).toBe(true);
    });
  });

  describe('TenantAdmin', () => {
    it('edits data and secure within own tenant only', () => {
      expect(can(tenantAdmin, 'edit', 'graphs', { tenantGuid: TENANT_A })).toBe(true);
      expect(can(tenantAdmin, 'edit', 'users', { tenantGuid: TENANT_A })).toBe(true);
      expect(can(tenantAdmin, 'edit', 'tenants', { tenantGuid: TENANT_A })).toBe(true);
      expect(can(tenantAdmin, 'view', 'authorization', { tenantGuid: TENANT_A })).toBe(true);
    });
    it('cannot touch another tenant', () => {
      expect(can(tenantAdmin, 'edit', 'users', { tenantGuid: TENANT_B })).toBe(false);
      expect(can(tenantAdmin, 'edit', 'tenants', { tenantGuid: TENANT_B })).toBe(false);
    });
    it('cannot see ADMINISTER', () => {
      expect(can(tenantAdmin, 'view', 'backups')).toBe(false);
      expect(can(tenantAdmin, 'view', 'settings')).toBe(false);
    });
  });

  describe('Regular user', () => {
    it('views own tenant data read-only', () => {
      expect(can(regular, 'view', 'graphs')).toBe(true);
      expect(can(regular, 'edit', 'graphs')).toBe(false);
    });
    it('views tenant read-only, cannot edit the tenant', () => {
      expect(can(regular, 'view', 'tenants')).toBe(true);
      expect(can(regular, 'edit', 'tenants', { tenantGuid: TENANT_A })).toBe(false);
    });
    it('edits only their own user record and credentials', () => {
      expect(can(regular, 'edit', 'users', { ownerUserGuid: USER_SELF })).toBe(true);
      expect(can(regular, 'edit', 'users', { ownerUserGuid: USER_OTHER })).toBe(false);
      expect(can(regular, 'view', 'users', { ownerUserGuid: USER_OTHER })).toBe(false);
      expect(can(regular, 'edit', 'credentials', { ownerUserGuid: USER_SELF })).toBe(true);
      expect(can(regular, 'edit', 'credentials', { ownerUserGuid: USER_OTHER })).toBe(false);
    });
    it('cannot see authorization or ADMINISTER', () => {
      expect(can(regular, 'view', 'authorization')).toBe(false);
      expect(can(regular, 'view', 'backups')).toBe(false);
      expect(can(regular, 'view', 'settings')).toBe(false);
    });
  });
});

describe('capability map — canViewSection()', () => {
  it('SystemAdmin sees every section', () => {
    for (const s of ['home', 'data', 'metadata', 'manage', 'secure', 'administer'] as const) {
      expect(canViewSection(systemAdmin, s)).toBe(true);
    }
  });
  it('TenantAdmin sees everything except ADMINISTER', () => {
    expect(canViewSection(tenantAdmin, 'secure')).toBe(true);
    expect(canViewSection(tenantAdmin, 'administer')).toBe(false);
  });
  it('Regular user sees data/secure but not administer', () => {
    expect(canViewSection(regular, 'data')).toBe(true);
    expect(canViewSection(regular, 'secure')).toBe(true);
    expect(canViewSection(regular, 'administer')).toBe(false);
  });
});

describe('capability map — AI resources', () => {
  it('chat is open to every tenant principal', () => {
    expect(can(regular, 'view', 'aiChat')).toBe(true);
    expect(can(tenantAdmin, 'view', 'aiChat')).toBe(true);
    expect(can(systemAdmin, 'view', 'aiChat')).toBe(true);
    expect(can(breakGlass, 'view', 'aiChat')).toBe(true);
  });

  it('admin AI surfaces are hidden from regular users', () => {
    for (const resource of ['aiEndpoints', 'aiHistory', 'aiFeedback', 'aiSettings'] as const) {
      expect(can(regular, 'view', resource)).toBe(false);
      expect(can(regular, 'edit', resource)).toBe(false);
      expect(can(tenantAdmin, 'view', resource)).toBe(true);
      expect(can(systemAdmin, 'view', resource)).toBe(true);
    }
  });

  it('tenant admins are scoped to their own tenant for AI admin surfaces', () => {
    expect(can(tenantAdmin, 'edit', 'aiEndpoints', { tenantGuid: TENANT_A })).toBe(true);
    expect(can(tenantAdmin, 'edit', 'aiEndpoints', { tenantGuid: TENANT_B })).toBe(false);
  });

  it('the AI section is visible to everyone (chat is always viewable)', () => {
    expect(canViewSection(regular, 'ai')).toBe(true);
    expect(canViewSection(tenantAdmin, 'ai')).toBe(true);
    expect(canViewSection(systemAdmin, 'ai')).toBe(true);
    expect(canViewSection(null, 'ai')).toBe(false);
  });
});
