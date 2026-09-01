import {
  buildNavForPrincipal,
  dashboardNavSections,
  navSectionsToMenuItems,
} from '@/constants/sidebar';
import { Principal } from '@/lib/authz/capabilities';

const systemAdmin: Principal = {
  isSystemAdmin: true,
  isTenantAdmin: false,
  isBreakGlass: false,
  userGuid: 'sa',
  tenantGuid: 't',
};
const tenantAdmin: Principal = {
  isSystemAdmin: false,
  isTenantAdmin: true,
  isBreakGlass: false,
  userGuid: 'ta',
  tenantGuid: 't',
};
const regular: Principal = {
  isSystemAdmin: false,
  isTenantAdmin: false,
  isBreakGlass: false,
  userGuid: 'ru',
  tenantGuid: 't',
};

const sectionIds = (p: Principal) => buildNavForPrincipal(p).map((s) => s.id);
const itemKeys = (p: Principal) =>
  buildNavForPrincipal(p).flatMap((s) => s.items.map((i) => i.key));

describe('dashboardNavSections (consolidated grouped nav)', () => {
  it('declares the seven workflow sections in order (AI between METADATA and MANAGE)', () => {
    expect(dashboardNavSections.map((s) => s.id)).toEqual([
      'home',
      'data',
      'metadata',
      'ai',
      'manage',
      'secure',
      'administer',
    ]);
  });

  it('uses nav.section.* header keys', () => {
    for (const section of dashboardNavSections) {
      expect(section.labelKey).toMatch(/^nav\.section\./);
    }
  });
});

describe('buildNavForPrincipal — role-aware nav', () => {
  it('SystemAdmin sees every section including ADMINISTER', () => {
    expect(sectionIds(systemAdmin)).toEqual([
      'home',
      'data',
      'metadata',
      'ai',
      'manage',
      'secure',
      'administer',
    ]);
    expect(itemKeys(systemAdmin)).toEqual(
      expect.arrayContaining(['/tenants', '/users', '/credentials', '/authorization', '/backups', '/settings'])
    );
  });

  it('TenantAdmin sees SECURE but not ADMINISTER', () => {
    const ids = sectionIds(tenantAdmin);
    expect(ids).toContain('secure');
    expect(ids).not.toContain('administer');
    expect(itemKeys(tenantAdmin)).toContain('/authorization');
    expect(itemKeys(tenantAdmin)).not.toContain('/settings');
    expect(itemKeys(tenantAdmin)).not.toContain('/backups');
  });

  it('Regular user sees SECURE users/credentials but not authorization or ADMINISTER', () => {
    const ids = sectionIds(regular);
    expect(ids).toContain('data');
    expect(ids).toContain('secure');
    expect(ids).not.toContain('administer');
    const keys = itemKeys(regular);
    expect(keys).toContain('/users');
    expect(keys).toContain('/credentials');
    expect(keys).toContain('/tenants');
    expect(keys).not.toContain('/authorization');
    expect(keys).not.toContain('/settings');
    expect(keys).not.toContain('/backups');
  });

  it('returns nothing for an anonymous principal', () => {
    expect(buildNavForPrincipal(null)).toEqual([]);
  });
});

describe('buildNavForPrincipal — AI section', () => {
  const aiSection = (p: Principal) => buildNavForPrincipal(p).find((s) => s.id === 'ai');

  it('shows all five AI items to admins', () => {
    for (const admin of [systemAdmin, tenantAdmin]) {
      const section = aiSection(admin);
      expect(section).toBeDefined();
      expect(section!.items.map((i) => i.key)).toEqual([
        '/ai/chat',
        '/ai/endpoints',
        '/ai/history',
        '/ai/feedback',
        '/ai/settings',
      ]);
    }
  });

  it('shows only Chat to a regular tenant user', () => {
    const section = aiSection(regular);
    expect(section).toBeDefined();
    expect(section!.items.map((i) => i.key)).toEqual(['/ai/chat']);
  });
});

describe('navSectionsToMenuItems', () => {
  it('emits antd group items with children', () => {
    const items = navSectionsToMenuItems(buildNavForPrincipal(systemAdmin));
    expect(items.every((i) => i.type === 'group')).toBe(true);
    expect(items[0].children && items[0].children.length).toBeGreaterThan(0);
  });
});
