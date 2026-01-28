import { mockCredentialGuid, credentialMockApiResponse, credentialData } from './mockData';
import { api } from '../setupTest'; // Adjust paths as needed
import { handlers } from './handlers';
import { getServer } from '../server';
import CredentialMetaData from '../../src/models/CredentialMetadata';
import CredentialMetadata from '../../src/models/CredentialMetadata';

const server = getServer(handlers);

describe('credentialRoute Tests', () => {
  beforeAll(() => {
    server.listen();
  });

  afterEach(() => {
    server.resetHandlers();
  });

  afterAll(() => {
    server.close();
  });

  describe('Credential Route', () => {
    test('should check if credential exists by GUID', async () => {
      const response = await api.existsCredential(mockCredentialGuid);
      expect(response).toBe(true); // Assuming the mock returns true
    });

    test('should create a credential', async () => {
      const newCredential = {
        "UserGUID": "00000000-0000-0000-0000-000000000000",
        "Name": "New credential",
        "BearerToken": "foobar",
        "Active": true
      };
      const response = await api.createCredential(newCredential);
      expect(true).toBe(response instanceof CredentialMetaData);
    });

    it('throws error when creating a credential', async () => {
      try {
        await api.createCredential();
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: credential is null or empty');
      }
    });

    test('should read all credentials', async () => {
      const response = await api.readAllCredentials();
      response.forEach((credential) => {
        expect(credential instanceof CredentialMetaData).toBe(true);
      });
    });

    test('should read a specific credential by GUID', async () => {
      const response = await api.readCredential(mockCredentialGuid);
      expect(true).toBe(response instanceof CredentialMetaData);
      expect(response.GUID).toBe(mockCredentialGuid);
    });

    test('should update a credential', async () => {
      const updateCredential = {
        "UserGUID": mockCredentialGuid,
        "Name": "Updated credential",
        "BearerToken": "default",
        "Active": true
      };
      const response = await api.updateCredential(updateCredential, mockCredentialGuid);
      expect(response instanceof CredentialMetadata).toBe(true);
      expect(response.GUID).toBe(mockCredentialGuid);
    });

    it('throws error when if missed credential data while updating a Credential', async () => {
      try {
        await api.updateCredential(null, mockCredentialGuid);
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: credential is null or empty');
      }
    });

    it('throws error when if missed credential guid while updating a Credential', async () => {
      try {
        const updateCredential = {
          "UserGUID": mockCredentialGuid,
          "Name": "Updated credential",
          "BearerToken": "default",
          "Active": true
        };
        await api.updateCredential(updateCredential, null);
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: guid is null or empty');
      }
    });

    test('should delete a credential', async () => {
      const response = await api.deleteCredential(mockCredentialGuid);
      expect(response).toBeUndefined(); // Assuming delete operation returns nothing
    });

    test('should delete a credential with abort', async () => {
      const cancellationToken = {};
      await api.deleteCredential(mockCredentialGuid, cancellationToken);
      cancellationToken.abort();
    });
  });
});
