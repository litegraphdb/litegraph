import { mockTenantId, tenantData } from './mockData';
import { api } from '../setupTest'; // Adjust paths as needed
import { handlers } from './handlers';
import { getServer } from '../server';
import Tenant from '../../src/models/TenantMetaData';
import TenantMetaData from '../../src/models/TenantMetaData';

const server = getServer(handlers);

describe('TenantRoute Tests', () => {
  beforeAll(() => {
    server.listen();
  });

  afterEach(() => {
    server.resetHandlers();
  });

  afterAll(() => {
    server.close();
  });

  describe('Tenant Route', () => {
    test('should check if tenant exists by GUID', async () => {
      const response = await api.tenantExists(mockTenantId);
      expect(response).toBe(true); // Assuming the mock returns true
    });

    test('should create a tenant', async () => {
      const newTenant = {
        "GUID": mockTenantId,
        "Name": "Another tenant",
        "Active": true
      };
      const response = await api.createTenant(newTenant);
      expect(true).toBe(response instanceof TenantMetaData);
    });

    it('throws error when creating a tenant', async () => {
      try {
        await api.createTenant();
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: tenant is null or empty');
      }
    });

    test('should read all tenants', async () => {
      const response = await api.readTenants();
      response.forEach((tenant) => {
        expect(tenant instanceof TenantMetaData).toBe(true);
      });
    });

    test('should read a specific tenant by GUID', async () => {
      const response = await api.readTenant(mockTenantId);
      expect(true).toBe(response instanceof TenantMetaData);
      expect(response.GUID).toBe(mockTenantId);
    });

    test('should update a tenant', async () => {
      const updateTenant = {
        "Name": "Updated tenant",
        "Active": true
      };
      const response = await api.updateTenant(updateTenant, mockTenantId);
      expect(response instanceof Tenant).toBe(true);
      expect(response.GUID).toBe(mockTenantId);
    });

    it('throws error when if missed tenant data while updating a Tenant', async () => {
      try {
        await api.updateTenant(null, mockTenantId);
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: tenant is null or empty');
      }
    });

    it('throws error when if missed tenant guid while updating a Tenant', async () => {
      try {
        const updateTenant = {
          "Name": "Updated tenant",
          "Active": true
        };
        await api.updateTenant(updateTenant, null);
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: guid is null or empty');
      }
    });

    test('should delete a tenant', async () => {
      const response = await api.deleteTenant(mockTenantId);
      expect(response).toBeUndefined(); // Assuming delete operation returns nothing
    });

    test('should delete a tenant with abort', async () => {
      const cancellationToken = {};
      await api.deleteTenant(mockTenantId, cancellationToken);
      cancellationToken.abort();
    });
  });
});
