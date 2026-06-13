import { flattenOpenApi } from '@/page/api-explorer/openApi';

describe('API Explorer OpenAPI helpers', () => {
  it('uses a transaction request template for graph transaction endpoints', () => {
    const operations = flattenOpenApi({
      paths: {
        '/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/transaction': {
          post: {
            tags: ['Transactions'],
            requestBody: {
              content: {
                'application/json': {
                  schema: {
                    type: 'object',
                    properties: {
                      Operations: { type: 'array', items: { type: 'object' } },
                    },
                  },
                },
              },
            },
          },
        },
      },
    });

    expect(operations).toHaveLength(1);
    const body = JSON.parse(operations[0].requestBodyExample || '{}');
    expect(body.TimeoutSeconds).toBe(60);
    expect(body.Operations[0]).toMatchObject({
      OperationType: 'Create',
      ObjectType: 'Node',
    });
    expect(body.Operations[1]).toMatchObject({
      OperationType: 'Attach',
      ObjectType: 'Tag',
    });
  });

  it('uses a query request template for graph query endpoints', () => {
    const operations = flattenOpenApi({
      paths: {
        '/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/query': {
          post: {
            tags: ['Graphs'],
            requestBody: {
              content: {
                'application/json': {
                  schema: {
                    type: 'object',
                    properties: {
                      Query: { type: 'string' },
                    },
                  },
                },
              },
            },
          },
        },
      },
    });

    expect(operations).toHaveLength(1);
    const body = JSON.parse(operations[0].requestBodyExample || '{}');
    expect(body.Query).toContain('MATCH');
    expect(body.Query).toContain('*1..3');
    expect(body.Parameters).toMatchObject({
      start: expect.any(String),
      end: expect.any(String),
    });
    expect(body.IncludeProfile).toBe(true);
  });

  it('adds return mode query parameter for bulk create endpoints', () => {
    const operations = flattenOpenApi({
      paths: {
        '/v1.0/tenants/{tenantGuid}/graphs/{graphGuid}/nodes/bulk': {
          put: {
            tags: ['Nodes'],
            requestBody: {
              content: {
                'application/json': {
                  schema: { type: 'array', items: { type: 'object' } },
                },
              },
            },
          },
        },
      },
    });

    expect(operations).toHaveLength(1);
    expect(operations[0].parameters).toEqual(expect.arrayContaining([
      expect.objectContaining({
        name: 'return',
        in: 'query',
        required: false,
      }),
    ]));
  });
});
