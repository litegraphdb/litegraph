import {
  ClusterNode,
  ClusterRestartResult,
  ClusterStatus,
  getBaseUrl,
  getClusterNodes,
  request,
} from './settings';

export type { ClusterNode, ClusterRestartResult, ClusterStatus, NodeHealthChecks } from './settings';
export { getClusterNodes };

/** Read one node's registry entry (404 when the node is not registered). */
export const getClusterNode = (nodeId: string): Promise<ClusterNode> =>
  request<ClusterNode>('GET', `${getBaseUrl()}/v1.0/cluster/nodes/${encodeURIComponent(nodeId)}`);

/** Request a rolling restart: every node restarts, one at a time (503 when Redis is unreachable). */
export const restartCluster = (): Promise<ClusterRestartResult> =>
  request<ClusterRestartResult>('POST', `${getBaseUrl()}/v1.0/cluster/restart`, { confirm: true });

/** Restart one node (409 when the node is Offline or Stopped). */
export const restartClusterNode = (nodeId: string): Promise<ClusterRestartResult> =>
  request<ClusterRestartResult>(
    'POST',
    `${getBaseUrl()}/v1.0/cluster/nodes/${encodeURIComponent(nodeId)}/restart`,
    { confirm: true }
  );

/** Remove an Offline or Stopped node from the node registry (409 for a running node). */
export const deleteClusterNode = (nodeId: string): Promise<void> =>
  request<void>('DELETE', `${getBaseUrl()}/v1.0/cluster/nodes/${encodeURIComponent(nodeId)}`);

/** A distributed lock this cluster holds in Clutch (v10.0 `GET /v1.0/cluster/locks`). */
export interface ClusterLock {
  Key: string;
  KeyClass: string;
  Mode: string;
  /** LiteGraph node holding the lock, or null when its Clutch session is not in the node registry. */
  NodeId?: string | null;
  ClutchNodeId?: string | null;
  FencingToken: number;
  AcquiredUtc?: string | null;
  LeaseExpiresUtc?: string | null;
}

/** Locks held by this cluster; empty on a single node. */
export interface ClusterLockList {
  ClusterEnabled: boolean;
  LockServiceAvailable?: boolean | null;
  Locks: ClusterLock[];
  Utc: string;
}

/** The most recent run of a cluster singleton job (v10.0 `GET /v1.0/cluster/jobs`). */
export interface ClusterJobRun {
  Job: string;
  NodeId?: string | null;
  StartedUtc: string;
  CompletedUtc: string;
  DurationMs: number;
  Success: boolean;
  Message?: string | null;
}

/** Most recent run of each cluster singleton job; empty on a single node. */
export interface ClusterJobList {
  ClusterEnabled: boolean;
  RegistryAvailable?: boolean | null;
  Jobs: ClusterJobRun[];
  Utc: string;
}

/** Liveness body (anonymous `GET /v1.0/health/live`), including the storage and vector index providers. */
export interface ServerHealth {
  Status: string;
  NodeId?: string | null;
  ClusterName?: string | null;
  Version?: string | null;
  StorageProvider?: 'Sqlite' | 'Postgresql' | string | null;
  VectorIndexProvider?: 'HnswLite' | 'pgvector' | string | null;
  StartedUtc?: string;
  Utc?: string;
}

/** List the distributed locks this cluster currently holds. */
export const getClusterLocks = (): Promise<ClusterLockList> =>
  request<ClusterLockList>('GET', `${getBaseUrl()}/v1.0/cluster/locks`);

/** List the most recent run of each cluster singleton job. */
export const getClusterJobs = (): Promise<ClusterJobList> =>
  request<ClusterJobList>('GET', `${getBaseUrl()}/v1.0/cluster/jobs`);

/** Read the server's liveness body, which names the storage and vector index providers. */
export const getServerHealth = (): Promise<ServerHealth> =>
  request<ServerHealth>('GET', `${getBaseUrl()}/v1.0/health/live`);
