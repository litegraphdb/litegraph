export const mockVectorGuid = '00000000-0000-0000-0000-000000000000';

export const vectorData = {
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

export const vectorMockApiResponse = [
    vectorData
];