import { localStorageKeys, dynamicSlugs, paths, keepUnusedDataFor } from '@/constants/constant';

describe('Constants', () => {
  describe('localStorageKeys', () => {
    it('should have all required keys', () => {
      expect(localStorageKeys).toEqual({
        tenant: 'tenant',
        token: 'token',
        adminAccessKey: 'adminAccessKey',
        user: 'user',
        serverUrl: 'serverUrl',
        theme: 'theme',
        locale: 'locale',
      });
    });

    it('should have string values for all keys', () => {
      Object.values(localStorageKeys).forEach((value) => {
        expect(typeof value).toBe('string');
      });
    });
  });

  describe('dynamicSlugs', () => {
    it('should have tenantId slug', () => {
      expect(dynamicSlugs.tenantId).toBe(':tenantId');
    });
  });

  describe('paths', () => {
    it('should have all required paths', () => {
      expect(paths).toHaveProperty('login');
      expect(paths).toHaveProperty('dashboardHome');
      expect(paths).toHaveProperty('graphs');
      expect(paths).toHaveProperty('nodes');
      expect(paths).toHaveProperty('edges');
      expect(paths).toHaveProperty('tags');
      expect(paths).toHaveProperty('vectors');
      expect(paths).toHaveProperty('labels');
      expect(paths).toHaveProperty('credentials');
      expect(paths).toHaveProperty('users');
      expect(paths).toHaveProperty('tenants');
      expect(paths).toHaveProperty('backups');
      expect(paths).toHaveProperty('settings');
    });

    it('should use dynamic slug in dashboard paths', () => {
      expect(paths.dashboardHome).toContain(dynamicSlugs.tenantId);
      expect(paths.graphs).toContain(dynamicSlugs.tenantId);
      expect(paths.nodes).toContain(dynamicSlugs.tenantId);
      expect(paths.edges).toContain(dynamicSlugs.tenantId);
      expect(paths.tags).toContain(dynamicSlugs.tenantId);
      expect(paths.vectors).toContain(dynamicSlugs.tenantId);
      expect(paths.labels).toContain(dynamicSlugs.tenantId);
    });

    it('should have correct path values', () => {
      expect(paths.login).toBe('/login');
      expect(paths.tenants).toBe('/dashboard/access/tenants');
      expect(paths.users).toBe('/dashboard/access/users');
      expect(paths.credentials).toBe('/dashboard/access/credentials');
      expect(paths.authorization).toBe('/dashboard/access/authorization');
      expect(paths.settings).toBe('/dashboard/system/settings');
      expect(paths.cluster).toBe('/dashboard/system/cluster');
      expect(paths.backups).toBe('/dashboard/system/backups');
      expect(paths.nodes).toBe(`/dashboard/${dynamicSlugs.tenantId}/graphs/nodes`);
      expect(paths.aiChat).toBe(`/dashboard/${dynamicSlugs.tenantId}/chat/chat`);
      expect(paths.requestHistory).toBe(`/dashboard/${dynamicSlugs.tenantId}/developer/requests`);
    });
  });

  describe('keepUnusedDataFor', () => {
    it('should be 900 seconds (15 minutes)', () => {
      expect(keepUnusedDataFor).toBe(900);
    });
  });
});
