import { mockUserId, tenantData } from './mockData';
import { api } from '../setupTest'; // Adjust paths as needed
import { handlers } from './handlers';
import { getServer } from '../server';
import Tenant from '../../src/models/TenantMetaData';
import TenantMetaData from '../../src/models/TenantMetaData';
import UserMetadata from '../../src/models/UserMetadata';

const server = getServer(handlers);

describe('userRoute Tests', () => {
  beforeAll(() => {
    server.listen();
  });

  afterEach(() => {
    server.resetHandlers();
  });

  afterAll(() => {
    server.close();
  });

  describe('User Route', () => {
    test('should check if user exists by GUID', async () => {
      const response = await api.existsUser(mockUserId);
      expect(response).toBe(true); // Assuming the mock returns true
    });

    test('should create a user', async () => {
      const newUser = {
        "GUID": "00000000-0000-0000-0000-000000000000",
        "FirstName": "Another",
        "LastName": "User",
        "Email": "another@user.com",
        "Password": "password",
        "Active": true
      };
      const response = await api.createUser(newUser);
      expect(true).toBe(response instanceof UserMetadata);
    });

    it('throws error when creating a user', async () => {
      try {
        await api.createUser();
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: user is null or empty');
      }
    });

    test('should read all users', async () => {
      const response = await api.readAllUsers();
      response.forEach((user) => {
        expect(user instanceof UserMetadata).toBe(true);
      });
    });

    test('should read a specific user by GUID', async () => {
      const response = await api.readUser(mockUserId);
      expect(true).toBe(response instanceof UserMetadata);
      expect(response.GUID).toBe(mockUserId);
    });

    test('should update a user', async () => {
      const updateUser = {
        "FirstName": "Again Updated",
        "LastName": "User",
        "Email": "anotherbbb@user.com",
        "Password": "password",
        "Active": true
      };
      const response = await api.updateUser(updateUser, mockUserId);
      expect(response instanceof UserMetadata).toBe(true);
    });

    it('throws error when if missed user data while updating a User', async () => {
      try {
        await api.updateUser(null, mockUserId);
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: user is null or empty');
      }
    });

    it('throws error when if missed user guid while updating a User', async () => {
      try {
        const updateUser = {
          "FirstName": "Again Updated",
          "LastName": "User",
          "Email": "anotherbbb@user.com",
          "Password": "password",
          "Active": true
        };
        await api.updateUser(updateUser, null);
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: guid is null or empty');
      }
    });

    test('should delete a user', async () => {
      const response = await api.deleteUser(mockUserId);
      expect(response).toBeUndefined(); // Assuming delete operation returns nothing
    });

    test('should delete a user with abort', async () => {
      const cancellationToken = {};
      await api.deleteUser(mockUserId, cancellationToken);
      cancellationToken.abort();
    });
  });
});
