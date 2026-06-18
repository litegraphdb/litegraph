const graphTransactionTemplate = {
  MaxOperations: 1000,
  TimeoutSeconds: 60,
  IsolationLevel: 'Default',
  Operations: [
    {
      OperationType: 'Create',
      ObjectType: 'Node',
      Payload: {
        GUID: '00000000-0000-0000-0000-000000000001',
        Name: 'Ada Lovelace',
        Data: {
          role: 'engineer',
          active: true,
        },
      },
    },
    {
      OperationType: 'Attach',
      ObjectType: 'Tag',
      Payload: {
        GUID: '00000000-0000-0000-0000-000000000002',
        NodeGUID: '00000000-0000-0000-0000-000000000001',
        Key: 'source',
        Value: 'api-explorer',
      },
    },
  ],
};

const graphQueryTemplate = {
  Query: 'MATCH (a:Person)-[path:LINKS*1..3]->(c:Person) WHERE a.guid = $start AND c.guid = $end RETURN a, path, c LIMIT 10',
  Parameters: {
    start: '00000000-0000-0000-0000-000000000001',
    end: '00000000-0000-0000-0000-000000000002',
  },
  MaxResults: 100,
  TimeoutSeconds: 30,
  IncludeProfile: true,
};

export const getRequestBodyTemplate = (path: string, method: string): string | undefined => {
  if (
    method.toUpperCase() === 'POST' &&
    /\/v1\.0\/tenants\/\{tenantGuid\}\/graphs\/\{graphGuid\}\/query$/i.test(path)
  ) {
    return JSON.stringify(graphQueryTemplate, null, 2);
  }

  if (
    method.toUpperCase() === 'POST' &&
    /\/v1\.0\/tenants\/\{tenantGuid\}\/graphs\/\{graphGuid\}\/transaction$/i.test(path)
  ) {
    return JSON.stringify(graphTransactionTemplate, null, 2);
  }

  return undefined;
};
