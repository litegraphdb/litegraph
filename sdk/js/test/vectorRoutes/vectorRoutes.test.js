import { mockVectorGuid, vectorMockApiResponse, vectorData } from './mockData';
import { api } from '../setupTest'; // Adjust paths as needed
import { handlers } from './handlers';
import { getServer } from '../server';
import { VectorMetadata } from '../../src/models/VectorMetadata';

const server = getServer(handlers);

describe('vectorRoute Tests', () => {
  beforeAll(() => {
    server.listen();
  });

  afterEach(() => {
    server.resetHandlers();
  });

  afterAll(() => {
    server.close();
  });

  describe('Vector Route', () => {
    test('should check if vector exists by GUID', async () => {
      const response = await api.existsVector(mockVectorGuid);
      expect(response).toBe(true); // Assuming the mock returns true
    });

    test('should create a vector', async () => {
      const newVector = {
        "GUID": mockVectorGuid,
        "TenantGUID": "987e6543-a21b-45c3-b678-789012345678",
        "GraphGUID": "321e6543-a21b-45c3-b678-789012345679",
        "NodeGUID": "123e4567-e89b-12d3-a456-426614174001",
        "EdgeGUID": "123e4567-e89b-12d3-a456-426614174002",
        "Model": "SampleModel",
        "Dimensionality": 128,
        "Content": "Sample vector content",
        "Vectors": [0.1, 0.2, 0.3, 0.4, 0.5],
        "CreatedUtc": "2025-01-10T10:00:00.000Z",
        "LastUpdateUtc": "2025-01-10T12:00:00.000Z"
      };
      const response = await api.createVector(newVector);
      expect(true).toBe(response instanceof VectorMetadata);
    });

    it('throws error when creating a vector', async () => {
      try {
        await api.createVector();
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: vector is null or empty');
      }
    });

    test('should read all vectors', async () => {
      const response = await api.readAllVectors();
      response.forEach((vector) => {
        expect(vector instanceof VectorMetadata).toBe(true);
      });
    });

    test('should read a specific vector by GUID', async () => {
      const response = await api.readVector(mockVectorGuid);
      expect(true).toBe(response instanceof VectorMetadata);
      expect(response.GUID).toBe(mockVectorGuid);
    });

    test('should update a vector', async () => {
      const updateVector = {
        "TenantGUID": "987e6543-a21b-45c3-b678-789012345678",
        "GraphGUID": "321e6543-a21b-45c3-b678-789012345679",
        "NodeGUID": "123e4567-e89b-12d3-a456-426614174001",
        "EdgeGUID": "123e4567-e89b-12d3-a456-426614174002",
        "Model": "SampleModel",
        "Dimensionality": 128,
        "Content": "Sample vector content",
        "Vectors": [0.1, 0.2, 0.3, 0.4, 0.5],
        "CreatedUtc": "2025-01-10T10:00:00.000Z",
        "LastUpdateUtc": "2025-01-10T12:00:00.000Z"
      };
      const response = await api.updateVector(updateVector, mockVectorGuid);
      expect(response instanceof VectorMetadata).toBe(true);
      expect(response.GUID).toBe(mockVectorGuid);
    });

    it('throws error when if missed vector guid while updating a vector', async () => {
      try {
        await api.updateVector(null, mockVectorGuid);
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: vector is null or empty');
      }
    });

    it('throws error when if missed vector guid while updating a vector', async () => {
      try {
        const updateVector = {
          "TenantGUID": "987e6543-a21b-45c3-b678-789012345678",
          "GraphGUID": "321e6543-a21b-45c3-b678-789012345679",
          "NodeGUID": "123e4567-e89b-12d3-a456-426614174001",
          "EdgeGUID": "123e4567-e89b-12d3-a456-426614174002",
          "Model": "SampleModel",
          "Dimensionality": 128,
          "Content": "Sample vector content",
          "Vectors": [0.1, 0.2, 0.3, 0.4, 0.5],
          "CreatedUtc": "2025-01-10T10:00:00.000Z",
          "LastUpdateUtc": "2025-01-10T12:00:00.000Z"
        };
        await api.updateVector(updateVector, null);
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: guid is null or empty');
      }
    });

    test('should delete a vector', async () => {
      const response = await api.deleteVector(mockVectorGuid);
      expect(response).toBeUndefined(); // Assuming delete operation returns nothing
    });

    test('should delete a vector with abort', async () => {
      const cancellationToken = {};
      await api.deleteVector(mockVectorGuid, cancellationToken);
      cancellationToken.abort();
    });
  });
});
