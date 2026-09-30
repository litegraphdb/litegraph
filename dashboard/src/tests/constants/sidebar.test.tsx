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
const resourcesOf = (p: Principal, key: string) =>
  buildNavForPrincipal(p)
    .flatMap((s) => s.items)
    .find((i) => i.key === key)?.resources;

describe('dashboardNavSections (consolidated nav)', () => {
  it('declares two groups with seven hub entries', () => {
    expect(dashboardNavSections.map((s) => s.id)).toEqual(['workspace', 'administration']);
    expect(dashboardNavSections[0].items.map((i) => i.key)).toEqual(['/home', '/graphs', '/chat']);
    expect(dashboardNavSections[1].items.map((i) => i.key)).toEqual([
      '/access',
      '/system',
      '/developer',
    ]);
  });

  it('uses nav.section.* header keys and nav.hub.* item keys', () => {
    for (const section of dashboardNavSections) {
      expect(section.labelKey).toMatch(/^nav\.section\./);
      for (const item of section.items) {
        expect(item.labelKey).toMatch(/^nav\.hub\./);
        expect(item.titleKey).toMatch(/^nav\.hub\..*Title$/);
      }
    }
  });

  it('links each hub to its bare path and highlights hubs on every tab', () => {
    const items = dashboardNavSections.flatMap((s) => s.items);
    expect(items.find((i) => i.key === '/home')).toMatchObject({
      path: '/dashboard/:tenantId',
      matchPrefix: false,
    });
    expect(items.find((i) => i.key === '/graphs')).toMatchObject({
      path: '/dashboard/:tenantId/graphs',
      matchPrefix: true,
    });
    expect(items.find((i) => i.key === '/access')?.path).toBe('/dashboard/access');
    expect(items.find((i) => i.key === '/system')?.path).toBe('/dashboard/system');
  });
});

describe('buildNavForPrincipal — role-aware nav', () => {
  it('SystemAdmin sees every hub with every tab', () => {
    expect(sectionIds(systemAdmin)).toEqual(['workspace', 'administration']);
    expect(itemKeys(systemAdmin)).toEqual([
      '/home',
      '/graphs',
      '/chat',
      '/access',
      '/system',
      '/developer',
    ]);
    expect(resourcesOf(systemAdmin, '/system')).toEqual(['settings', 'cluster', 'backups']);
    expect(resourcesOf(systemAdmin, '/chat')).toEqual([
      'aiChat',
      'aiHistory',
      'aiFeedback',
      'aiEndpoints',
      'aiSettings',
    ]);
  });

  it('TenantAdmin sees Access with Authorization but not System', () => {
    expect(itemKeys(tenantAdmin)).toEqual(['/home', '/graphs', '/chat', '/access', '/developer']);
    expect(resourcesOf(tenantAdmin, '/access')).toContain('authorization');
  });

  it('Regular user sees Chat with only the Chat tab and Access without Authorization', () => {
    expect(itemKeys(regular)).toEqual(['/home', '/graphs', '/chat', '/access', '/developer']);
    expect(resourcesOf(regular, '/chat')).toEqual(['aiChat']);
    expect(resourcesOf(regular, '/access')).toEqual(['tenants', 'users', 'credentials']);
  });

  it('returns nothing for an anonymous principal', () => {
    expect(buildNavForPrincipal(null)).toEqual([]);
  });
});

describe('navSectionsToMenuItems', () => {
  it('emits antd group items with children', () => {
    const items = navSectionsToMenuItems(buildNavForPrincipal(systemAdmin));
    expect(items.every((i) => i.type === 'group')).toBe(true);
    expect(items.map((i) => i.key)).toEqual(['section:workspace', 'section:administration']);
    expect(items[0].children && items[0].children.length).toBe(3);
  });
});
