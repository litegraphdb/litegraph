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

const node = (id, state = 'Healthy') => ({
  NodeId: id,
  State: state,
  Checks: { Database: true, Clutch: true, Redis: true, Draining: false },
  SettingsVersion: 1,
  RestartPending: false,
  RestartVersion: 0,
});

describe('Cluster, node header, and retry policy', () => {
  beforeAll(() => server.listen({ onUnhandledRequest: 'error' }));
  afterEach(() => server.resetHandlers());
  afterAll(() => server.close());

  describe('retry settings', () => {
    it('defaults to two retries, 200 ms, and no POST retries', () => {
      const client = new sdk.LiteGraphSdk(endpoint, 'default', 'default');
      expect(client.maxRetries).toBe(2);
      expect(client.retryBaseDelayMs).toBe(200);
      expect(client.retryPost).toBe(false);
      expect(client.lastNodeId).toBeNull();
    });

    it('rejects out-of-range values', () => {
      const client = newApi();
      expect(() => {
        client.maxRetries = 11;
      }).toThrow();
      expect(() => {
        client.maxRetries = -1;
      }).toThrow();
      expect(() => {
        client.retryBaseDelayMs = 6000;
      }).toThrow();
    });
  });

  describe('retries', () => {
    it('retries a GET that returns 503 and then succeeds', async () => {
      let calls = 0;
      server.use(
        http.get(`${endpoint}v1.0/cluster/nodes`, () => {
          calls++;
          if (calls < 3) return new HttpResponse(null, { status: 503 });
          return HttpResponse.json({ ClusterEnabled: true, Nodes: [node('litegraph-2')] }, { headers: { 'x-litegraph-node': 'litegraph-2' } });
        })
      );
      const client = newApi();
      const status = await client.readClusterNodes();
      expect(calls).toBe(3);
      expect(status.Nodes[0].NodeId).toBe('litegraph-2');
    });

    it('gives up after maxRetries', async () => {
      let calls = 0;
      server.use(
        http.get(`${endpoint}v1.0/cluster/nodes`, () => {
          calls++;
          return new HttpResponse(null, { status: 502 });
        })
      );
      const client = newApi();
      client.maxRetries = 1;
      await expect(client.readClusterNodes()).rejects.toBeDefined();
      expect(calls).toBe(2);
    });

    it('retries a GET after a connection failure', async () => {
      let calls = 0;
      server.use(
        http.get(`${endpoint}v1.0/cluster/nodes`, () => {
          calls++;
          if (calls === 1) return HttpResponse.error();
          return HttpResponse.json({ ClusterEnabled: false, Nodes: [node('host-1')] });
        })
      );
      const client = newApi();
      const status = await client.readClusterNodes();
      expect(calls).toBe(2);
      expect(status.Nodes).toHaveLength(1);
    });

    it('does not retry a 500', async () => {
      let calls = 0;
      server.use(
        http.get(`${endpoint}v1.0/cluster/nodes`, () => {
          calls++;
          return HttpResponse.json({ Error: 'InternalError' }, { status: 500 });
        })
      );
      const client = newApi();
      await expect(client.readClusterNodes()).rejects.toBeDefined();
      expect(calls).toBe(1);
    });

    it('does not retry a POST by default', async () => {
      let calls = 0;
      server.use(
        http.post(`${endpoint}v1.0/cluster/restart`, () => {
          calls++;
          return new HttpResponse(null, { status: 503 });
        })
      );
      const client = newApi();
      const result = await client.restartCluster();
      expect(result).toBeUndefined();
      expect(calls).toBe(1);
    });

    it('retries a POST when retryPost is enabled', async () => {
      let calls = 0;
      server.use(
        http.post(`${endpoint}v1.0/cluster/restart`, () => {
          calls++;
          if (calls === 1) return new HttpResponse(null, { status: 504 });
          return HttpResponse.json({ Restarting: true, Rolling: true, RestartVersion: 5 });
        })
      );
      const client = newApi();
      client.retryPost = true;
      const result = await client.restartCluster();
      expect(calls).toBe(2);
      expect(result.RestartVersion).toBe(5);
    });

    it('does not retry when maxRetries is 0', async () => {
      let calls = 0;
      server.use(
        http.get(`${endpoint}v1.0/cluster/nodes`, () => {
          calls++;
          return new HttpResponse(null, { status: 503 });
        })
      );
      const client = newApi();
      client.maxRetries = 0;
      await expect(client.readClusterNodes()).rejects.toBeDefined();
      expect(calls).toBe(1);
    });
  });

  describe('node header', () => {
    it('records the node that answered', async () => {
      server.use(
        http.get(`${endpoint}v1.0/cluster/nodes`, () =>
          HttpResponse.json({ ClusterEnabled: true, Nodes: [] }, { headers: { 'x-litegraph-node': 'litegraph-3' } })
        )
      );
      const client = newApi();
      await client.readClusterNodes();
      expect(client.lastNodeId).toBe('litegraph-3');
    });

    it('carries the node on API errors', async () => {
      server.use(
        http.get(`${endpoint}v1.0/cluster/nodes/missing`, () =>
          HttpResponse.json(
            { Error: 'NotFound', Description: 'Node not found' },
            { status: 404, headers: { 'x-litegraph-node': 'litegraph-1' } }
          )
        )
      );
      const client = newApi();
      let caught = null;
      try {
        await client.readClusterNode('missing');
      } catch (err) {
        caught = err;
      }
      expect(caught).not.toBeNull();
      expect(caught.error).toBe('NotFound');
      expect(caught.nodeId).toBe('litegraph-1');
      expect(client.lastNodeId).toBe('litegraph-1');
    });
  });

  describe('cluster node methods', () => {
    it('reads one node', async () => {
      server.use(http.get(`${endpoint}v1.0/cluster/nodes/litegraph-2`, () => HttpResponse.json(node('litegraph-2'))));
      const result = await newApi().readClusterNode('litegraph-2');
      expect(result.NodeId).toBe('litegraph-2');
      expect(result.State).toBe('Healthy');
    });

    it('requests a restart of one node', async () => {
      let path = null;
      server.use(
        http.post(`${endpoint}v1.0/cluster/nodes/:nodeId/restart`, ({ params }) => {
          path = params.nodeId;
          return HttpResponse.json({ Restarting: true, Rolling: false });
        })
      );
      const result = await newApi().restartClusterNode('litegraph-3');
      expect(path).toBe('litegraph-3');
      expect(result.Restarting).toBe(true);
      expect(result.Rolling).toBe(false);
    });

    it('rethrows a refused node restart', async () => {
      server.use(
        http.post(`${endpoint}v1.0/cluster/nodes/:nodeId/restart`, () =>
          HttpResponse.json({ Error: 'Conflict', Description: 'Node is Offline' }, { status: 409 })
        )
      );
      await expect(newApi().restartClusterNode('litegraph-9')).rejects.toMatchObject({ error: 'Conflict' });
    });

    it('removes a node', async () => {
      let deleted = null;
      server.use(
        http.delete(`${endpoint}v1.0/cluster/nodes/:nodeId`, ({ params }) => {
          deleted = params.nodeId;
          return new HttpResponse(null, { status: 200 });
        })
      );
      await newApi().deleteClusterNode('old-node');
      expect(deleted).toBe('old-node');
    });

    it('requires a node identifier', async () => {
      await expect(newApi().readClusterNode('')).rejects.toThrow();
    });
  });

  describe('health', () => {
    it('reads liveness', async () => {
      server.use(http.get(`${endpoint}v1.0/health/live`, () => HttpResponse.json({ Status: 'Healthy', NodeId: 'litegraph-1' })));
      const live = await newApi().healthLive();
      expect(live.Status).toBe('Healthy');
    });

    it('returns a 503 readiness body instead of throwing, without retrying', async () => {
      let calls = 0;
      server.use(
        http.get(`${endpoint}v1.0/health/ready`, () => {
          calls++;
          return HttpResponse.json(
            { Status: 'Unavailable', Checks: { Database: false, Draining: false } },
            { status: 503, headers: { 'x-litegraph-node': 'litegraph-2' } }
          );
        })
      );
      const client = newApi();
      const ready = await client.healthReady();
      expect(calls).toBe(1);
      expect(ready.Status).toBe('Unavailable');
      expect(ready.Checks.Database).toBe(false);
      expect(client.lastNodeId).toBe('litegraph-2');
    });
  });
});
