import { GraphData } from '@/types/types';
import {
  BackupMetaData,
  CredentialMetadata,
  Edge,
  LabelMetadata,
  Node,
  EnumerateResponse,
  TenantMetaData,
  UserMetadata,
} from 'litegraphdb/dist/types/types';

export const mockGraphGUID = 'e6d4294e-6f49-4d67-8260-5e44c2b077a6';

export const mockGraphData: GraphData[] = [
  {
    TenantGUID: '00000000-0000-0000-0000-000000000000',
    LastUpdateUtc: '2024-12-23T15:36:07.816289Z' as any,
    GUID: mockGraphGUID,
    Name: 'Test Demo Graph',
    CreatedUtc: '2024-12-23T15:36:07.816289Z' as any,
    Data: {
      name: 'value',
    },
    gexfContent: `<?xml version="1.0" encoding="utf-8"?> <gexf xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema" xsi:schemaLocation="http://www.gexf.net/1.3 http://www.gexf.net/1.3/gexf.xsd" version="1.3" xmlns="http://www.gexf.net/1.3"> <meta lastmodifieddate="2025-01-02T07:37:34.9191749Z"> <creator>LiteGraph</creator> <description>Graph from LiteGraph https://github.com/jchristn/litegraph</description> </meta> <graph defaultedgetype="directed"> <attributes class="node"> <attribute id="0" title="props" type="string" /> </attributes> <nodes> <node id="3897b459-a197-48e0-bb8d-e54255675713" label="new one 2"> <attvalues> <attvalue for="Name" value="new one 2" /> </attvalues> </node> <node id="0be10847-040f-4ce5-8694-72da08824f1e" label="new one"> <attvalues> <attvalue for="Name" value="new one" /> </attvalues> </node> <node id="ada1be94-0272-45bb-84fd-75e14f56ffe8" label="test"> <attvalues> <attvalue for="Name" value="test" /> </attvalues> </node> <node id="976e0552-2462-494b-82bc-fd113f89409c" label="My test node 121"> <attvalues> <attvalue for="Name" value="My test node 121" /> </attvalues> </node> </nodes> <edges> <edge id="7b665c02-249a-4029-b782-92526f50d357" source="0be10847-040f-4ce5-8694-72da08824f1e" target="ada1be94-0272-45bb-84fd-75e14f56ffe8"> <attvalues> <attvalue for="Name" value="new test 23" /> <attvalue for="Cost" value="2" /> </attvalues> </edge> <edge id="d35bea13-f548-43be-b005-c79191117334" source="976e0552-2462-494b-82bc-fd113f89409c" target="ada1be94-0272-45bb-84fd-75e14f56ffe8"> <attvalues> <attvalue for="Name" value="test edge" /> <attvalue for="Cost" value="5" /> </attvalues> </edge> </edges> </graph> </gexf>`,
    Labels: [],
    Tags: {},
    Vectors: [],
  },
  {
    TenantGUID: '00000000-0000-0000-0000-000000000000',
    LastUpdateUtc: '2024-12-23T15:36:07.816289Z' as any,
    GUID: 'd52aeab4-4de7-4076-98dd-461d4a61ac88',
    Name: 'testttt 2',
    CreatedUtc: '2024-12-18T07:28:53.965316Z' as any,
    Data: {},
    Labels: [],
    Tags: {},
    Vectors: [],
  },
];

export const mockEdgeData: Edge[] = [
  {
    TenantGUID: '00000000-0000-0000-0000-000000000000',
    GUID: '7b665c02-249a-4029-b782-92526f50d357',
    GraphGUID: 'e6d4294e-6f49-4d67-8260-5e44c2b077a6',
    Name: 'new test 23',
    From: '3fc519b1-5f78-4fc7-810b-c18fade549db',
    To: '6fe5823b-31ad-4431-9d3d-53d8b6f1ed4c',
    Cost: 2,
    CreatedUtc: '2024-12-24T07:34:47.695616Z' as any,
    Data: {},
    LastUpdateUtc: '2024-12-24T07:34:47.695616Z' as any,
    Labels: [],
    Tags: {},
    Vectors: [],
  },
  {
    TenantGUID: '00000000-0000-0000-0000-000000000000',
    GUID: 'd35bea13-f548-43be-b005-c79191117334',
    GraphGUID: 'd52e9587-f9a8-4348-9cfe-b860ab13ba54',
    Name: 'test edge',
    From: '976e0552-2462-494b-82bc-fd113f89409c',
    To: 'ada1be94-0272-45bb-84fd-75e14f56ffe8',
    Cost: 5,
    CreatedUtc: '2024-12-17T09:24:39.806284Z' as any,
    Data: {},
    LastUpdateUtc: '2024-12-17T09:24:39.806284Z' as any,
    Labels: [],
    Tags: {},
    Vectors: [],
  },
];

export const mockNodeData: Node[] = [
  {
    TenantGUID: '00000000-0000-0000-0000-000000000000',
    GUID: '3fc519b1-5f78-4fc7-810b-c18fade549db',
    GraphGUID: '01010101-0101-0101-0101-010101010101',
    Name: 'My updated test node',
    Data: {},
    CreatedUtc: '2024-12-18T07:27:58.206132Z' as any,
    LastUpdateUtc: '2024-12-18T07:27:58.206132Z' as any,
    Labels: [],
    Tags: {},
    Vectors: [],
  },
  {
    TenantGUID: '00000000-0000-0000-0000-000000000000',
    GUID: '6fe5823b-31ad-4431-9d3d-53d8b6f1ed4c',
    GraphGUID: '01010101-0101-0101-0101-010101010101',
    Name: 'test sas',
    Data: {
      Hello: 'world',
    },
    CreatedUtc: '2024-12-16T08:27:17.639147Z' as any,
    LastUpdateUtc: '2024-12-16T08:27:17.639147Z' as any,
    Labels: [],
    Tags: {},
    Vectors: [],
  },
];

export const graphListingData = {
  count: Object.values(mockGraphData).length,
  results: Object.values(mockGraphData),
};

export const nodeListingData = {
  count: Object.values(mockNodeData).length,
  results: Object.values(mockNodeData),
};

export const mockTagData = {
  allTags: [
    {
      GUID: 'tag-1',
      TenantGUID: '00000000-0000-0000-0000-000000000000',
      GraphGUID: 'd52aeab4-4de7-4076-98dd-461d4a61ac88',
      NodeGUID: '3fc519b1-5f78-4fc7-810b-c18fade549db',
      EdgeGUID: '',
      Key: 'Category',
      Value: 'Important',
      CreatedUtc: '2024-01-30T10:00:00.000Z',
      LastUpdateUtc: '2024-01-30T10:00:00.000Z',
    },
    {
      GUID: 'tag-2',
      TenantGUID: '00000000-0000-0000-0000-000000000000',
      GraphGUID: 'd52aeab4-4de7-4076-98dd-461d4a61ac88',
      NodeGUID: '6fe5823b-31ad-4431-9d3d-53d8b6f1ed4c',
      EdgeGUID: '',
      Key: 'Priority',
      Value: 'High',
      CreatedUtc: '2024-01-30T10:00:00.000Z',
      LastUpdateUtc: '2024-01-30T10:00:00.000Z',
    },
  ],
  tags: [
    {
      'd52aeab4-4de7-4076-98dd-461d4a61ac88': [
        {
          GUID: 'tag-1',
          TenantGUID: '00000000-0000-0000-0000-000000000000',
          GraphGUID: 'd52aeab4-4de7-4076-98dd-461d4a61ac88',
          NodeGUID: '3fc519b1-5f78-4fc7-810b-c18fade549db',
          EdgeGUID: '',
          Key: 'Category',
          Value: 'Important',
          CreatedUtc: '2024-01-30T10:00:00.000Z',
          LastUpdateUtc: '2024-01-30T10:00:00.000Z',
        },
        {
          GUID: 'tag-2',
          TenantGUID: '00000000-0000-0000-0000-000000000000',
          GraphGUID: 'd52aeab4-4de7-4076-98dd-461d4a61ac88',
          NodeGUID: '6fe5823b-31ad-4431-9d3d-53d8b6f1ed4c',
          EdgeGUID: '',
          Key: 'Priority',
          Value: 'High',
          CreatedUtc: '2024-01-30T10:00:00.000Z',
          LastUpdateUtc: '2024-01-30T10:00:00.000Z',
        },
      ],
    },
  ],
};

//tenant data
export const tagListingData = {
  count: mockTagData.allTags.length,
  results: mockTagData.allTags,
};

export const mockTenantGUID = '00000000-0000-0000-0000-000000000000';

export const mockTenantData: TenantMetaData[] = [
  {
    GUID: mockTenantGUID,
    Name: 'Test Tenant',
    Active: true,
    CreatedUtc: '2024-01-01T00:00:00Z' as any,
    LastUpdateUtc: '2024-01-01T00:00:00Z' as any,
  },
  {
    GUID: '00000000-0000-0000-0000-000000000002',
    Name: 'Test Tenant 2',
    Active: true,
    CreatedUtc: '2024-01-02T00:00:00Z' as any,
    LastUpdateUtc: '2024-01-02T00:00:00Z' as any,
  },
];

//user data
export const mockUserData: UserMetadata[] = [
  {
    GUID: '00000000-0000-0000-0000-000000000001',
    TenantGUID: '00000000-0000-0000-0000-000000000000',
    FirstName: 'Test',
    LastName: 'User',
    Email: 'test@example.com',
    Password: 'password',
    Active: true,
    CreatedUtc: '2024-01-01T00:00:00Z' as any,
    LastUpdateUtc: '2024-01-01T00:00:00Z' as any,
  },
  {
    GUID: '00000000-0000-0000-0000-000000000002',
    TenantGUID: '00000000-0000-0000-0000-000000000000',
    FirstName: 'Test',
    LastName: 'User 2',
    Email: 'test2@example.com',
    Password: 'password',
    Active: true,
    CreatedUtc: '2024-01-02T00:00:00Z' as any,
    LastUpdateUtc: '2024-01-02T00:00:00Z' as any,
  },
];

//credential data
export const mockCredentialData: CredentialMetadata[] = [
  {
    GUID: '00000000-0000-0000-0000-000000000003',
    UserGUID: mockUserData[0].GUID,
    Name: 'Test Credential',
    BearerToken: 'test-bearer-token',
    Active: true,
    CreatedUtc: '2024-01-01T00:00:00Z' as any,
    TenantGUID: '00000000-0000-0000-0000-000000000000',
    LastUpdateUtc: '2024-01-01T00:00:00Z' as any,
  },
  {
    GUID: '00000000-0000-0000-0000-000000000004',
    UserGUID: mockUserData[1].GUID,
    Name: 'Test Credential 2',
    BearerToken: 'test-bearer-token-2',
    Active: true,
    CreatedUtc: '2024-01-02T00:00:00Z' as any,
    TenantGUID: '00000000-0000-0000-0000-000000000000',
    LastUpdateUtc: '2024-01-02T00:00:00Z' as any,
  },
];

export const userListingData = {
  count: mockUserData.length,
  results: mockUserData.map((user) => ({
    ...user,
    fullName: `${user.FirstName} ${user.LastName}`,
  })),
};

export const credentialListingData = {
  count: mockCredentialData.length,
  results: mockCredentialData,
};

//label data
export const mockLabelData: LabelMetadata[] = [
  {
    GUID: '00000000-0000-0000-0000-000000000005',
    TenantGUID: '00000000-0000-0000-0000-000000000000',
    GraphGUID: mockGraphData[0].GUID,
    NodeGUID: mockNodeData[0].GUID,
    EdgeGUID: mockEdgeData[0].GUID,
    Label: 'Test Label',
    CreatedUtc: '2024-01-01T00:00:00Z' as any,
    LastUpdateUtc: '2024-01-01T00:00:00Z' as any,
  },
];

//vector data
export const mockVectorData = [
  {
    GUID: '00000000-0000-0000-0000-000000000005',
    TenantGUID: '00000000-0000-0000-0000-000000000000',
    GraphGUID: mockGraphData[0].GUID,
    NodeGUID: mockNodeData[0].GUID,
    EdgeGUID: mockEdgeData[0].GUID,
    Model: 'test-model',
    Dimensionality: 3,
    Content: 'Test vector content',
    Vectors: [1.0, 2.0, 3.0],
    CreatedUtc: '2024-01-01T00:00:00Z',
    LastUpdateUtc: '2024-01-01T00:00:00Z',
  },
  {
    GUID: '00000000-0000-0000-0000-000000000006',
    TenantGUID: '00000000-0000-0000-0000-000000000000',
    GraphGUID: mockGraphData[0].GUID,
    NodeGUID: mockNodeData[1].GUID,
    EdgeGUID: mockEdgeData[1].GUID,
    Model: 'test-model-2',
    Dimensionality: 2,
    Content: 'Another test vector',
    Vectors: [4.0, 5.0],
    CreatedUtc: '2024-01-02T00:00:00Z',
  },
];

export const vectorListingData = {
  count: mockVectorData.length,
  results: mockVectorData,
};

export const mockBackupData: BackupMetaData[] = [
  {
    Filename: 'test-backup-1',
    Length: 438272,
    MD5Hash: 'test-md5-hash',
    SHA1Hash: 'test-sha1-hash',
    SHA256Hash: '0123456789abcdef0123456789abcdef0123456789abcdef0123456789abcdef',
    CreatedUtc: '2025-01-01T00:00:00Z' as any,
    LastUpdateUtc: '2025-01-01T00:00:00Z' as any,
    LastAccessUtc: '2025-01-01T00:00:00Z' as any,
  },
  {
    Filename: 'test-backup-2',
    Length: 438272,
    MD5Hash: 'test-md5-hash-2',
    SHA1Hash: 'test-sha1-hash-2',
    SHA256Hash: 'abcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcdefabcd',
    CreatedUtc: '2025-01-02T00:00:00Z' as any,
    LastUpdateUtc: '2025-01-02T00:00:00Z' as any,
    LastAccessUtc: '2025-01-02T00:00:00Z' as any,
  },
];

export function getMockEnumerateQueryData<T>(data: T[]): EnumerateResponse<T> {
  return {
    Success: true,
    Timestamp: {
      Start: '2025-01-01T00:00:00Z',
      End: '2025-01-01T00:00:00Z',
      TotalMs: 0,
      Messages: {},
    },
    ContinuationToken: undefined,
    MaxResults: 10,
    EndOfResults: false,
    TotalRecords: data.length,
    RecordsRemaining: 0,
    Objects: data,
  } as EnumerateResponse<T>;
}

// mockData.ts

export const mockToken = {
  AccessToken: 'mocked-access-token',
  ExpiresIn: 3600,
  RefreshToken: 'mocked-refresh-token',
  TokenType: 'Bearer',
  GUID: 'mock-token-guid',
  CreatedAt: new Date().toISOString(),
};

export const mockUser = {
  id: 'user-123-456',
  email: 'test@example.com',
  name: 'Test User',
  role: 'user',
  permissions: ['read', 'write'],
  avatar: 'https://example.com/avatar.jpg',
  createdAt: '2024-01-01T00:00:00Z',
  lastLogin: '2024-01-15T10:30:00Z',
};

export const mockAdminUser = {
  id: 'admin-123-456',
  email: 'admin@example.com',
  name: 'Admin User',
  role: 'admin',
  permissions: ['read', 'write', 'delete', 'admin'],
  avatar: 'https://example.com/admin-avatar.jpg',
  createdAt: '2024-01-01T00:00:00Z',
  lastLogin: '2024-01-15T09:00:00Z',
};
export const mockTenant = {
  GUID: 'tenant-123-456-789',
  Name: 'Test Tenant',
  Description: 'A test tenant for development',
  Status: 'Active',
  CreatedAt: '2024-01-01T00:00:00Z',
  UpdatedAt: '2024-01-01T00:00:00Z',
  DatabaseUrl: 'postgresql://localhost:5432/test_tenant',
  SchemaVersion: '1.0.0',
};
