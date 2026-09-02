import '@testing-library/jest-dom';
import {
  getMetricsEndpointUrl,
  listRecentRequestErrors,
  listRequestHistory,
} from '@/lib/sdk/requestHistory';
import { setEndpoint } from '@/lib/sdk/litegraph.service';

const mockSearchResult = {
  Success: true,
  Timestamp: { Start: '2026-01-01T00:00:00Z', End: '2026-01-01T00:00:00Z', TotalMs: 1, Messages: {} },
  MaxResults: 25,
  EndOfResults: true,
  TotalRecords: 0,
  RecordsRemaining: 0,
  Objects: [],
};

describe('requestHistory sdk', () => {
  beforeEach(() => {
    setEndpoint('http://localhost:8701/');
    jest.clearAllMocks();
    global.fetch = jest.fn().mockResolvedValue({
      ok: true,
      status: 200,
      statusText: 'OK',
      text: jest.fn().mockResolvedValue(JSON.stringify(mockSearchResult)),
    }) as jest.Mock;
  });

  it('passes success filters to request history search', async () => {
    await listRequestHistory({
      page: 0,
      pageSize: 25,
      success: false,
      tenantGuid: 'tenant-1',
      hasTransactionDiagnostics: true,
      transactionId: '11111111',
    });

    const [url] = (global.fetch as jest.Mock).mock.calls[0];
    expect(url).toContain('/v1.0/requesthistory');
    expect(url).toContain('max-keys=25');
    expect(url).not.toContain('skip=');
    expect(url).toContain('success=false');
    expect(url).toContain('tenantGuid=tenant-1');
    expect(url).toContain('hasTransactionDiagnostics=true');
    expect(url).toContain('transactionId=11111111');
  });

  it('lists recent request errors with success=false', async () => {
    await listRecentRequestErrors({
      pageSize: 10,
      path: '/v1.0/tenants',
    });

    const [url] = (global.fetch as jest.Mock).mock.calls[0];
    expect(url).toContain('/v1.0/requesthistory');
    expect(url).toContain('max-keys=10');
    expect(url).toContain('path=%2Fv1.0%2Ftenants');
    expect(url).toContain('success=false');
  });

  it('builds the Prometheus metrics endpoint URL from the configured endpoint', () => {
    setEndpoint('http://localhost:8701/');

    expect(getMetricsEndpointUrl()).toBe('http://localhost:8701/metrics');
  });
});
