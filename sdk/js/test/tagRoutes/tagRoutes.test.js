import { mockCredentialGuid, mockTagGuid } from './mockData';
import { api } from '../setupTest'; // Adjust paths as needed
import { handlers } from './handlers';
import { getServer } from '../server';
import TagMetaData from '../../src/models/TagMetaData';

const server = getServer(handlers);

describe('tagRoute Tests', () => {
  beforeAll(() => {
    server.listen();
  });

  afterEach(() => {
    server.resetHandlers();
  });

  afterAll(() => {
    server.close();
  });

  describe('Tag Route', () => {
    test('should check if tag exists by GUID', async () => {
      const response = await api.existsTag(mockTagGuid);
      expect(response).toBe(true); // Assuming the mock returns true
    });

    test('should create a tag', async () => {
      const newTag = {
        "GUID": mockTagGuid,
        "GraphGUID": "00000000-0000-0000-0000-000000000000",
        "NodeGUID": "158c634b-53d2-4a60-be87-61c39c990451",
        "EdgeGUID": "cdf28e4f-9a28-4a70-b246-e3ae9ccb35d6",
        "Key": "mykey",
        "Value": "myvalue"
      };
      const response = await api.createTag(newTag);
      expect(true).toBe(response instanceof TagMetaData);
    });

    it('throws error when creating a tag', async () => {
      try {
        await api.createTag();
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: tag is null or empty');
      }
    });

    test('should read all tags', async () => {
      const response = await api.readAllTags();
      response.forEach((tag) => {
        expect(tag instanceof TagMetaData).toBe(true);
      });
    });

    test('should read a specific tag by GUID', async () => {
      const response = await api.readTag(mockTagGuid);
      expect(true).toBe(response instanceof TagMetaData);
      expect(response.GUID).toBe(mockTagGuid);
    });

    test('should update a tag', async () => {
      const updateTag = {
        "Key": "updatedkey",
        "Value": "myvalue",
        "CreatedUtc": "2024-12-27T18:12:38.653402Z",
        "LastUpdateUtc": "2024-12-27T18:12:38.653402Z"
      };
      const response = await api.updateTag(updateTag, mockTagGuid);
      expect(response instanceof TagMetaData).toBe(true);
      expect(response.GUID).toBe(mockTagGuid);
    });

    it('throws error when if missed tag data while updating a tag', async () => {
      try {
        await api.updateTag(null, mockTagGuid);
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: tag is null or empty');
      }
    });

    it('throws error when if missed tag guid while updating a tag', async () => {
      try {
        const updateTag = {
          "Key": "updatedkey",
          "Value": "myvalue",
          "CreatedUtc": "2024-12-27T18:12:38.653402Z",
          "LastUpdateUtc": "2024-12-27T18:12:38.653402Z"
        };
        await api.updateTag(updateTag, null);
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: guid is null or empty');
      }
    });

    test('should delete a tag', async () => {
      const response = await api.deleteTag(mockTagGuid);
      expect(response).toBeUndefined(); // Assuming delete operation returns nothing
    });

    test('should delete a tag with abort', async () => {
      const cancellationToken = {};
      await api.deleteTag(mockTagGuid, cancellationToken);
      cancellationToken.abort();
    });
  });
});
