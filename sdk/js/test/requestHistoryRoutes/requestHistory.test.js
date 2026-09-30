import { http, HttpResponse } from 'msw';
import { getServer } from '../server';
import { sdk } from '../setupTest';

const endpoint = 'http://127.0.0.1:8701/';
const server = getServer([]);

const newApi = () => {
  const client = new sdk.LiteGraphSdk(endpoint, 'default', 'default');
  client.retryBaseDelayMs = 0;
  return client;
};

const entry = (guid, nodeId, path = '/v1.0/tenants') => ({
  GUID: guid,
  Method: 'GET',
  Path: path,
  Url: 'http://127.0.0.1:8701' + path,
  SourceIp: '127.0.0.1',
  NodeId: nodeId,
  StatusCode: 200,
  Success: true,
  ProcessingTimeMs: 3.2,
});

const envelope = (objects, remaining = 0) => ({
  Success: true,
  MaxResults: 100,
  EndOfResults: remaining === 0,
  TotalRecords: objects.length + remaining,
  RecordsRemaining: remaining,
  Objects: objects,
});

const rawQuery = (request) => {
  const url = request.url;
  const idx = url.indexOf('?');
  return idx < 0 ? '' : url.substring(idx + 1);
};

describe('Request history', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  it('lists entries and sends paging and filters, including nodeId', async () => {
    let query = null;
    server.use(
      http.get(`${endpoint}v1.0/requesthistory`, ({ request }) => {
        query = rawQuery(request);
        return HttpResponse.json(envelope([entry('11111111-1111-1111-1111-111111111111', 'litegraph-2')]));
      })
    );
    const result = await newApi().listRequestHistory({
      nodeId: 'litegraph-2',
      method: 'GET',
      statusCode: 200,
      success: true,
      maxKeys: 50,
      skip: 0,
    });
    expect(result.Objects).toHaveLength(1);
    expect(result.Objects[0].NodeId).toBe('litegraph-2');
    expect(query).toContain('nodeId=litegraph-2');
    expect(query).toContain('method=GET');
    expect(query).toContain('statusCode=200');
    expect(query).toContain('success=true');
    expect(query).toContain('max-keys=50');
    expect(query).toContain('skip=0');
  });

  it('sends paths and timestamps unencoded, because the server matches query values as sent', async () => {
    let query = null;
    server.use(
      http.get(`${endpoint}v1.0/requesthistory`, ({ request }) => {
        query = rawQuery(request);
        return HttpResponse.json(envelope([]));
      })
    );
    await newApi().listRequestHistory({ path: '/v1.0/tenants', fromUtc: new Date('2026-09-30T10:00:00Z') });
    expect(query).toContain('path=/v1.0/tenants');
    expect(query).toContain('fromUtc=2026-09-30T10:00:00.000Z');
  });

  it('escapes characters that would break the query string', async () => {
    let query = null;
    server.use(
      http.get(`${endpoint}v1.0/requesthistory`, ({ request }) => {
        query = rawQuery(request);
        return HttpResponse.json(envelope([]));
      })
    );
    await newApi().listRequestHistory({ path: 'a b&c=d' });
    expect(query).toContain('path=a%20b%26c%3Dd');
  });

  it('reads an entry and its detail', async () => {
    const guid = '22222222-2222-2222-2222-222222222222';
    server.use(
      http.get(`${endpoint}v1.0/requesthistory/${guid}`, () => HttpResponse.json(entry(guid, 'litegraph-1'))),
      http.get(`${endpoint}v1.0/requesthistory/${guid}/detail`, () =>
        HttpResponse.json({ ...entry(guid, 'litegraph-1'), RequestHeaders: { Accept: '*/*' }, ResponseBody: '[]' })
      )
    );
    const api = newApi();
    const read = await api.readRequestHistory(guid);
    expect(read.GUID).toBe(guid);
    expect(read.NodeId).toBe('litegraph-1');
    const detail = await api.readRequestHistoryDetail(guid);
    expect(detail.RequestHeaders.Accept).toBe('*/*');
  });

  it('rejects when an entry does not exist', async () => {
    const guid = '33333333-3333-3333-3333-333333333333';
    server.use(
      http.get(`${endpoint}v1.0/requesthistory/${guid}`, () =>
        HttpResponse.json({ Error: 'NotFound', Description: 'The requested resource was not found.' }, { status: 404 })
      )
    );
    await expect(newApi().readRequestHistory(guid)).rejects.toBeDefined();
  });

  it('reads a summary with interval and range', async () => {
    let query = null;
    server.use(
      http.get(`${endpoint}v1.0/requesthistory/summary`, ({ request }) => {
        query = rawQuery(request);
        return HttpResponse.json({ Interval: 'hour', TotalRequests: 5, TotalSuccess: 4, TotalFailure: 1, Data: [] });
      })
    );
    const summary = await newApi().readRequestHistorySummary({ interval: 'hour', startUtc: '2026-09-30T00:00:00Z' });
    expect(summary.TotalRequests).toBe(5);
    expect(query).toContain('interval=hour');
    expect(query).toContain('startUtc=2026-09-30T00:00:00Z');
  });

  it('deletes one entry and many entries by filter', async () => {
    const guid = '44444444-4444-4444-4444-444444444444';
    let bulkQuery = null;
    server.use(
      http.delete(`${endpoint}v1.0/requesthistory/bulk`, ({ request }) => {
        bulkQuery = rawQuery(request);
        return HttpResponse.json({ Deleted: 3 });
      }),
      http.delete(`${endpoint}v1.0/requesthistory/${guid}`, () => new HttpResponse(null, { status: 204 }))
    );
    const api = newApi();
    await expect(api.deleteRequestHistory(guid)).resolves.toBeUndefined();
    const result = await api.deleteRequestHistoryMany({ nodeId: 'litegraph-3', maxKeys: 10 });
    expect(result.Deleted).toBe(3);
    expect(bulkQuery).toContain('nodeId=litegraph-3');
    expect(bulkQuery).not.toContain('max-keys');
  });

  it('requires arguments', async () => {
    const api = newApi();
    await expect(api.readRequestHistory()).rejects.toBeDefined();
    await expect(api.deleteRequestHistoryMany()).rejects.toBeDefined();
  });
});
