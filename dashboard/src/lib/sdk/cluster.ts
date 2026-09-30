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
