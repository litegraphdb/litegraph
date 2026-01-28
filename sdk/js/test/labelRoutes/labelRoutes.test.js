import { mockCredentialGuid, credentialMockApiResponse, credentialData, mockLabelGuid } from './mockData';
import { api } from '../setupTest'; // Adjust paths as needed
import { handlers } from './handlers';
import { getServer } from '../server';
import LabelMetaData from '../../src/models/LabelMetadata';

const server = getServer(handlers);

describe('labelRoute Tests', () => {
  beforeAll(() => {
    server.listen();
  });

  afterEach(() => {
    server.resetHandlers();
  });

  afterAll(() => {
    server.close();
  });

  describe('Label Route', () => {
    test('should check if label exists by GUID', async () => {
      const response = await api.existsLabel(mockLabelGuid);
      expect(response).toBe(true); // Assuming the mock returns true
    });

    test('should create a label', async () => {
      const newLabel = {
        "TenantGUID": "00000000-0000-0000-0000-000000000000",
        "GraphGUID": "00000000-0000-0000-0000-000000000000",
        "NodeGUID": "00000000-0000-0000-0000-000000000000",
        "Label": "test",
        "CreatedUtc": "2025-01-16T07:23:26.752372Z",
        "LastUpdateUtc": "2025-01-16T07:23:26.752417Z"
      };
      const response = await api.createLabel(newLabel);
      expect(true).toBe(response instanceof LabelMetaData);
    });

    it('throws error when creating a label', async () => {
      try {
        await api.createLabel();
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: label is null or empty');
      }
    });

    test('should read all labels', async () => {
      const response = await api.readAllLabels();
      response.forEach((label) => {
        expect(label instanceof LabelMetaData).toBe(true);
      });
    });

    test('should read a specific label by GUID', async () => {
      const response = await api.readLabel(mockLabelGuid);
      expect(true).toBe(response instanceof LabelMetaData);
      expect(response.GUID).toBe(mockLabelGuid);
    });

    test('should update a label', async () => {
      const updateLabel = {
        "Key": "updatedkey",
        "Value": "myvalue",
        "CreatedUtc": "2024-12-27T18:12:38.653402Z",
        "LastUpdateUtc": "2024-12-27T18:12:38.653402Z"
      };
      const response = await api.updateLabel(updateLabel, mockLabelGuid);
      expect(response instanceof LabelMetaData).toBe(true);
      expect(response.GUID).toBe(mockLabelGuid);
    });

    it('throws error when if missed label guid while updating a label', async () => {
      try {
        await api.updateLabel(null, mockLabelGuid);
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: label is null or empty');
      }
    });

    it('throws error when if missed label guid while updating a label', async () => {
      try {
        const updateLabel = {
          "Key": "updatedkey",
          "Value": "myvalue",
          "CreatedUtc": "2024-12-27T18:12:38.653402Z",
          "LastUpdateUtc": "2024-12-27T18:12:38.653402Z"
        };
        await api.updateLabel(updateLabel, null);
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: guid is null or empty');
      }
    });

    test('should delete a label', async () => {
      const response = await api.deleteLabel(mockLabelGuid);
      expect(response).toBeUndefined(); // Assuming delete operation returns nothing
    });

    test('should delete a label with abort', async () => {
      const cancellationToken = {};
      await api.deleteLabel(mockLabelGuid, cancellationToken);
      cancellationToken.abort();
    });
  });
});
