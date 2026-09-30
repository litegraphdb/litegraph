import { http, HttpResponse } from 'msw';
import { handlers } from './handlers';
import { getServer } from './server';
import { api, mockEndpoint, sdk, mockTenantId, mockAccessToken } from './setupTest';

const server = getServer(handlers);

describe('LiteGraph SDK', () => {
  beforeAll(() => {
    server.listen();
  });

  afterEach(() => {
    server.resetHandlers();
  });

  afterAll(() => {
    server.close();
  });
  describe('test Base SDK methods', () => {
    it('intialze sdk with default endpoint and tty updating headers', async () => {
      const api2 = new sdk.LiteGraphSdk(mockEndpoint, mockTenantId, mockAccessToken);
      expect(api2.header).toBe('[LiteGraphSdk] ');
      api2.header = '[test] ';
      expect(api2.header).toBe('[test] ');
      expect(api2._header).toBe('[test] ');
      expect(api2.timeoutMs).toBe(300000);
    });

    it('should set endpoint and throw error for invalid endpoint', async () => {
      try {
        api.endpoint = mockEndpoint;
        api.endpoint = '';
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: ArgumentNullException: Endpoint is null or empty');
      }
    });

    it('should set timeout and throw error for invalid timeout', async () => {
      try {
        api.timeoutMs = 30000;
        api.timeoutMs = 0;
      } catch (err) {
        expect(err instanceof Error).toBe(true);
        expect(err.toString()).toBe('Error: TimeoutMs must be greater than 0.');
      }
    });

    it('should validate connectivity', async () => {
      const response = await api.validateConnectivity();
      expect(response).toBe(true);
    });

    it('should validate connectivity with abort', async () => {
      const cancellationToken = {};
      await api.validateConnectivity(cancellationToken);
      cancellationToken.abort();
    });

    //   it('calls retrieve methods without url param', async () => {
    //     try {
    //       await api.retrieve();
    //     } catch (err) {
    //       expect(err instanceof Error).toBe(true);
    //       expect(err.toString()).toBe('Error: ArgumentNullException: url is null or empty');
    //     }
    //   });

    //   it('calls retrieve methods without Modal param', async () => {
    //     try {
    //       await api.retrieve('path');
    //     } catch (err) {
    //       expect(err instanceof Error).toBe(true);
    //       expect(err.toString()).toBe('Error: ArgumentNullException: Modal Class is null or empty');
    //     }
    //   });
  });

  describe('v8 settings admin methods', () => {
    it('reads server settings', async () => {
      server.use(
        http.get(`${mockEndpoint}v1.0/settings`, () =>
          HttpResponse.json({ RequestTimeoutSeconds: 60 })
        )
      );
      const settings = await api.readSettings();
      expect(settings.RequestTimeoutSeconds).toBe(60);
    });

    it('updates server settings and returns the update result', async () => {
      server.use(
        http.put(`${mockEndpoint}v1.0/settings`, async ({ request }) => {
          const body = await request.json();
          expect(body.RequestTimeoutSeconds).toBe(30);
          return HttpResponse.json({
            Success: true,
            AppliedLive: ['RequestTimeoutSeconds'],
            RestartRequired: [],
          });
        })
      );
      const result = await api.updateSettings({ RequestTimeoutSeconds: 30 });
      expect(result.Success).toBe(true);
      expect(result.AppliedLive).toContain('RequestTimeoutSeconds');
    });

    it('rejects updating settings without a body', async () => {
      await expect(api.updateSettings()).rejects.toBeDefined();
    });

    it('requests a server restart', async () => {
      server.use(
        http.post(`${mockEndpoint}v1.0/settings/restart`, () =>
          HttpResponse.json({ Restarting: true, Rolling: false })
        )
      );
      const result = await api.restartServer();
      expect(result).toBeDefined();
      expect(result.Restarting).toBe(true);
    });

    it('reads the cluster nodes', async () => {
      server.use(
        http.get(`${mockEndpoint}v1.0/cluster/nodes`, () =>
          HttpResponse.json({
            ClusterEnabled: true,
            ClusterName: 'litegraph',
            AnsweredBy: 'litegraph-1',
            RegistryAvailable: true,
            SettingsVersion: 2,
            RestartVersion: 1,
            Nodes: [
              { NodeId: 'litegraph-1', State: 'Healthy', RestartPending: false },
              { NodeId: 'litegraph-2', State: 'Restarting', RestartPending: true },
            ],
          })
        )
      );
      const status = await api.readClusterNodes();
      expect(status.ClusterEnabled).toBe(true);
      expect(status.Nodes).toHaveLength(2);
      expect(status.Nodes[1].State).toBe('Restarting');
    });

    it('requests a rolling cluster restart', async () => {
      server.use(
        http.post(`${mockEndpoint}v1.0/cluster/restart`, () =>
          HttpResponse.json({ Restarting: true, Rolling: true, RestartVersion: 3 })
        )
      );
      const result = await api.restartCluster();
      expect(result.Rolling).toBe(true);
      expect(result.RestartVersion).toBe(3);
    });
  });
});
