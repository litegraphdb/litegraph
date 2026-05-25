import type { Edge, Node } from 'litegraphdb/dist/types/types';
import type { EdgeData, NodeData } from './types';

export const DEFAULT_COLLAPSE_THRESHOLD = 4;

const DEPTH_BAND_GAP = 360;
const DEPTH_BAND_WIDTH = 260;
const NODE_GAP_X = 88;
const NODE_GAP_Y = 90;
const CLUSTER_GAP_Y = 82;
const CLUSTER_HEADER_SPACE = 58;
const MAX_CLUSTER_COLUMNS = 4;

type ComponentInfo = {
  id: string;
  nodeIds: string[];
};

type ClusterInfo = {
  id: string;
  depth: number;
  nodeIds: string[];
  signature: string;
  label: string;
  dominantType: string;
  predecessorClusterIds: Set<string>;
};

type Position = {
  x: number;
  y: number;
};

function toSortedUnique(values: string[] | undefined, limit = 4): string[] {
  return Array.from(new Set((values || []).filter(Boolean)))
    .sort()
    .slice(0, limit);
}

function getPrimaryType(labels?: string[]): string {
  return labels?.[0] || 'Unknown';
}

function getRelationType(
  edge: Pick<Edge, 'Labels' | 'Name'> | Pick<EdgeData, 'labels' | 'label'>
): string {
  if ('Labels' in edge) {
    return edge.Labels?.[0] || edge.Name || 'related';
  }

  return edge.labels?.[0] || edge.label || 'related';
}

function buildBaseNode(node: Node): NodeData {
  const labels = [...(node.Labels || [])];
  const tags = { ...(node.Tags || {}) };

  return {
    id: node.GUID,
    label: node.Name,
    type: getPrimaryType(labels),
    x: 0,
    y: 0,
    z: 0,
    vx: 0,
    vy: 0,
    size: 15,
    labels,
    tags,
    tagKeys: Object.keys(tags).sort(),
    nodeKind: 'node',
  };
}

function buildBaseEdge(edge: Edge): EdgeData {
  const labels = [...(edge.Labels || [])];
  const tags = { ...(edge.Tags || {}) };

  return {
    id: edge.GUID,
    source: edge.From,
    target: edge.To,
    cost: edge.Cost || 0,
    data: '',
    sourceX: 0,
    sourceY: 0,
    targetX: 0,
    targetY: 0,
    label: edge.Name,
    labels,
    tags,
    relationType: getRelationType(edge),
    rawEdgeIds: [edge.GUID],
    edgeCount: 1,
  };
}

function rotatePoint(point: Position): Position {
  return {
    x: point.y,
    y: -point.x,
  };
}

function buildAdjacency(nodeIds: string[], edges: EdgeData[]) {
  const outgoing = new Map<string, string[]>();
  const incoming = new Map<string, string[]>();

  nodeIds.forEach((nodeId) => {
    outgoing.set(nodeId, []);
    incoming.set(nodeId, []);
  });

  edges.forEach((edge) => {
    if (!outgoing.has(edge.source) || !incoming.has(edge.target)) return;
    outgoing.get(edge.source)?.push(edge.target);
    incoming.get(edge.target)?.push(edge.source);
  });

  return { outgoing, incoming };
}

function computeStronglyConnectedComponents(nodeIds: string[], edges: EdgeData[]) {
  const adjacency = buildAdjacency(nodeIds, edges).outgoing;
  const indexByNode = new Map<string, number>();
  const lowLinkByNode = new Map<string, number>();
  const stack: string[] = [];
  const onStack = new Set<string>();
  const components: ComponentInfo[] = [];
  let index = 0;

  const selfLoops = new Set(
    edges.filter((edge) => edge.source === edge.target).map((edge) => edge.source)
  );

  function strongConnect(nodeId: string) {
    indexByNode.set(nodeId, index);
    lowLinkByNode.set(nodeId, index);
    index += 1;
    stack.push(nodeId);
    onStack.add(nodeId);

    for (const neighborId of adjacency.get(nodeId) || []) {
      if (!indexByNode.has(neighborId)) {
        strongConnect(neighborId);
        lowLinkByNode.set(
          nodeId,
          Math.min(lowLinkByNode.get(nodeId) || 0, lowLinkByNode.get(neighborId) || 0)
        );
      } else if (onStack.has(neighborId)) {
        lowLinkByNode.set(
          nodeId,
          Math.min(lowLinkByNode.get(nodeId) || 0, indexByNode.get(neighborId) || 0)
        );
      }
    }

    if ((lowLinkByNode.get(nodeId) || 0) === (indexByNode.get(nodeId) || 0)) {
      const componentNodeIds: string[] = [];
      let currentNodeId = stack.pop();
      while (currentNodeId) {
        onStack.delete(currentNodeId);
        componentNodeIds.push(currentNodeId);
        if (currentNodeId === nodeId) break;
        currentNodeId = stack.pop();
      }

      components.push({
        id: `component:${components.length}`,
        nodeIds: componentNodeIds.sort(),
      });
    }
  }

  nodeIds.forEach((nodeId) => {
    if (!indexByNode.has(nodeId)) {
      strongConnect(nodeId);
    }
  });

  const componentByNodeId = new Map<string, string>();
  components.forEach((component) => {
    component.nodeIds.forEach((nodeId) => {
      componentByNodeId.set(nodeId, component.id);
    });
  });

  const hasCycles =
    components.some((component) => component.nodeIds.length > 1) ||
    components.some((component) => component.nodeIds.some((nodeId) => selfLoops.has(nodeId)));

  return {
    components,
    componentByNodeId,
    hasCycles,
  };
}

function computeComponentDepths(
  components: ComponentInfo[],
  componentByNodeId: Map<string, string>,
  edges: EdgeData[]
) {
  const componentEdges = new Map<string, Set<string>>();
  const indegree = new Map<string, number>();
  const depthByComponentId = new Map<string, number>();

  components.forEach((component) => {
    componentEdges.set(component.id, new Set<string>());
    indegree.set(component.id, 0);
    depthByComponentId.set(component.id, 0);
  });

  edges.forEach((edge) => {
    const sourceComponentId = componentByNodeId.get(edge.source);
    const targetComponentId = componentByNodeId.get(edge.target);
    if (!sourceComponentId || !targetComponentId || sourceComponentId === targetComponentId) return;

    const neighbors = componentEdges.get(sourceComponentId);
    if (!neighbors?.has(targetComponentId)) {
      neighbors?.add(targetComponentId);
      indegree.set(targetComponentId, (indegree.get(targetComponentId) || 0) + 1);
    }
  });

  const queue = components
    .filter((component) => (indegree.get(component.id) || 0) === 0)
    .map((component) => component.id)
    .sort();

  while (queue.length > 0) {
    const componentId = queue.shift();
    if (!componentId) continue;

    const currentDepth = depthByComponentId.get(componentId) || 0;
    for (const neighborComponentId of Array.from(componentEdges.get(componentId) || [])) {
      depthByComponentId.set(
        neighborComponentId,
        Math.max(depthByComponentId.get(neighborComponentId) || 0, currentDepth + 1)
      );
      indegree.set(neighborComponentId, (indegree.get(neighborComponentId) || 0) - 1);
      if ((indegree.get(neighborComponentId) || 0) === 0) {
        queue.push(neighborComponentId);
      }
    }

    queue.sort();
  }

  return depthByComponentId;
}

function buildNodeSignature(
  node: NodeData,
  nodeById: Map<string, NodeData>,
  incomingEdgesByNodeId: Map<string, EdgeData[]>,
  outgoingEdgesByNodeId: Map<string, EdgeData[]>
) {
  const incomingEdges = incomingEdgesByNodeId.get(node.id) || [];
  const outgoingEdges = outgoingEdgesByNodeId.get(node.id) || [];

  const incomingTypes = toSortedUnique(
    incomingEdges.map((edge) => nodeById.get(edge.source)?.type || 'Unknown')
  );
  const outgoingTypes = toSortedUnique(
    outgoingEdges.map((edge) => nodeById.get(edge.target)?.type || 'Unknown')
  );
  const incomingRelations = toSortedUnique(
    incomingEdges.map((edge) => edge.relationType || edge.label || 'related')
  );
  const outgoingRelations = toSortedUnique(
    outgoingEdges.map((edge) => edge.relationType || edge.label || 'related')
  );
  const labelsKey = toSortedUnique(node.labels).join('|') || 'Unknown';
  const tagKeysKey = toSortedUnique(node.tagKeys).join('|') || '-';

  return [
    labelsKey,
    tagKeysKey,
    incomingTypes.join('|') || '-',
    outgoingTypes.join('|') || '-',
    incomingRelations.join('|') || '-',
    outgoingRelations.join('|') || '-',
  ].join('::');
}

function buildClusterLabel(nodes: NodeData[]) {
  const firstNode = nodes[0];
  const baseLabel = firstNode?.type || 'Unknown';
  const tagKeys = toSortedUnique(firstNode?.tagKeys, 2);
  return tagKeys.length > 0 ? `${baseLabel} · ${tagKeys.join(', ')}` : baseLabel;
}

function buildClusterInfos(nodes: NodeData[], edges: EdgeData[]) {
  const nodeById = new Map(nodes.map((node) => [node.id, node]));
  const incomingEdgesByNodeId = new Map<string, EdgeData[]>();
  const outgoingEdgesByNodeId = new Map<string, EdgeData[]>();

  nodes.forEach((node) => {
    incomingEdgesByNodeId.set(node.id, []);
    outgoingEdgesByNodeId.set(node.id, []);
  });

  edges.forEach((edge) => {
    outgoingEdgesByNodeId.get(edge.source)?.push(edge);
    incomingEdgesByNodeId.get(edge.target)?.push(edge);
  });

  const clustersByKey = new Map<string, ClusterInfo>();

  nodes.forEach((node) => {
    const depth = node.depth || 0;
    const signature = buildNodeSignature(
      node,
      nodeById,
      incomingEdgesByNodeId,
      outgoingEdgesByNodeId
    );
    const clusterKey = `depth:${depth}|${signature}`;
    const existingCluster = clustersByKey.get(clusterKey);

    if (existingCluster) {
      existingCluster.nodeIds.push(node.id);
      return;
    }

    clustersByKey.set(clusterKey, {
      id: `cluster:${clustersByKey.size}`,
      depth,
      nodeIds: [node.id],
      signature,
      label: buildClusterLabel([node]),
      dominantType: node.type || 'Unknown',
      predecessorClusterIds: new Set<string>(),
    });
  });

  const clusters = Array.from(clustersByKey.values());
  const clusterByNodeId = new Map<string, ClusterInfo>();
  clusters.forEach((cluster) => {
    const clusterNodes = cluster.nodeIds
      .map((nodeId) => nodeById.get(nodeId))
      .filter(Boolean) as NodeData[];
    cluster.label = buildClusterLabel(clusterNodes);
    cluster.dominantType = clusterNodes[0]?.type || 'Unknown';
    cluster.nodeIds.sort((leftNodeId, rightNodeId) => {
      const leftNode = nodeById.get(leftNodeId);
      const rightNode = nodeById.get(rightNodeId);
      return (leftNode?.label || '').localeCompare(rightNode?.label || '');
    });
    cluster.nodeIds.forEach((nodeId) => {
      clusterByNodeId.set(nodeId, cluster);
    });
  });

  edges.forEach((edge) => {
    const sourceCluster = clusterByNodeId.get(edge.source);
    const targetCluster = clusterByNodeId.get(edge.target);
    if (!sourceCluster || !targetCluster || sourceCluster.id === targetCluster.id) return;
    targetCluster.predecessorClusterIds.add(sourceCluster.id);
  });

  return {
    clusters,
    clusterByNodeId,
  };
}

function sortClustersByDepth(clusters: ClusterInfo[]) {
  const clustersByDepth = new Map<number, ClusterInfo[]>();
  const orderByClusterId = new Map<string, number>();

  clusters.forEach((cluster) => {
    const depthClusters = clustersByDepth.get(cluster.depth) || [];
    depthClusters.push(cluster);
    clustersByDepth.set(cluster.depth, depthClusters);
  });

  Array.from(clustersByDepth.entries())
    .sort(([leftDepth], [rightDepth]) => leftDepth - rightDepth)
    .forEach(([, depthClusters]) => {
      depthClusters.sort((leftCluster, rightCluster) => {
        const leftScore = getClusterBarycenter(leftCluster, orderByClusterId);
        const rightScore = getClusterBarycenter(rightCluster, orderByClusterId);

        if (leftScore !== rightScore) return leftScore - rightScore;
        if (leftCluster.nodeIds.length !== rightCluster.nodeIds.length) {
          return rightCluster.nodeIds.length - leftCluster.nodeIds.length;
        }

        return leftCluster.label.localeCompare(rightCluster.label);
      });

      depthClusters.forEach((cluster, index) => {
        orderByClusterId.set(cluster.id, index);
      });
    });

  return clustersByDepth;
}

function getClusterBarycenter(cluster: ClusterInfo, orderByClusterId: Map<string, number>) {
  if (cluster.predecessorClusterIds.size < 1) return Number.MAX_SAFE_INTEGER;

  const predecessorOrders = Array.from(cluster.predecessorClusterIds)
    .map((clusterId) => orderByClusterId.get(clusterId))
    .filter((value): value is number => Number.isFinite(value));

  if (predecessorOrders.length < 1) return Number.MAX_SAFE_INTEGER;

  return predecessorOrders.reduce((total, value) => total + value, 0) / predecessorOrders.length;
}

function layoutClusterNodes(nodes: NodeData[], cluster: ClusterInfo, showGraphHorizontal: boolean) {
  const columns =
    nodes.length > 1
      ? Math.min(MAX_CLUSTER_COLUMNS, Math.max(2, Math.ceil(Math.sqrt(nodes.length))))
      : 1;
  const rows = Math.max(1, Math.ceil(nodes.length / columns));
  const blockWidth = (columns - 1) * NODE_GAP_X;
  const blockHeight = (rows - 1) * NODE_GAP_Y;
  const headerSpace = nodes.length >= DEFAULT_COLLAPSE_THRESHOLD ? CLUSTER_HEADER_SPACE : 0;
  const startX = cluster.depth * DEPTH_BAND_GAP + Math.max(0, (DEPTH_BAND_WIDTH - blockWidth) / 2);

  return {
    columns,
    rows,
    blockWidth,
    blockHeight,
    headerSpace,
    startX,
    apply(nodeIndex: number, currentY: number) {
      const row = nodeIndex % rows;
      const column = Math.floor(nodeIndex / rows);
      const horizontalPoint = {
        x: startX + column * NODE_GAP_X,
        y: currentY + headerSpace + row * NODE_GAP_Y,
      };

      return showGraphHorizontal ? horizontalPoint : rotatePoint(horizontalPoint);
    },
  };
}

export function buildSmartDepthLayout(
  rawNodes: Node[],
  rawEdges: Edge[],
  showGraphHorizontal: boolean
) {
  const baseNodes = rawNodes.map((node) => buildBaseNode(node));
  const nodeIds = baseNodes.map((node) => node.id);
  const nodeIdSet = new Set(nodeIds);
  const baseEdges = rawEdges
    .map((edge) => buildBaseEdge(edge))
    .filter((edge) => nodeIdSet.has(edge.source) && nodeIdSet.has(edge.target));
  const nodeById = new Map(baseNodes.map((node) => [node.id, node]));

  const { components, componentByNodeId, hasCycles } = computeStronglyConnectedComponents(
    nodeIds,
    baseEdges
  );
  const depthByComponentId = computeComponentDepths(components, componentByNodeId, baseEdges);

  baseNodes.forEach((node) => {
    const componentId = componentByNodeId.get(node.id);
    node.componentId = componentId;
    node.depth = componentId ? depthByComponentId.get(componentId) || 0 : 0;
  });

  const { clusters, clusterByNodeId } = buildClusterInfos(baseNodes, baseEdges);
  const clustersByDepth = sortClustersByDepth(clusters);
  const positionedNodes: NodeData[] = [];

  Array.from(clustersByDepth.entries())
    .sort(([leftDepth], [rightDepth]) => leftDepth - rightDepth)
    .forEach(([, depthClusters]) => {
      let currentY = 0;

      depthClusters.forEach((cluster) => {
        const clusterNodes = cluster.nodeIds
          .map((nodeId) => nodeById.get(nodeId))
          .filter(Boolean) as NodeData[];
        const layout = layoutClusterNodes(clusterNodes, cluster, showGraphHorizontal);

        clusterNodes.forEach((node, index) => {
          const position = layout.apply(index, currentY);
          positionedNodes.push({
            ...node,
            ...position,
            clusterId: cluster.id,
            clusterLabel: cluster.label,
            depth: cluster.depth,
            size: 15,
          });
        });

        currentY += layout.headerSpace + layout.blockHeight + CLUSTER_GAP_Y;
      });
    });

  const positionedNodeById = new Map(positionedNodes.map((node) => [node.id, node]));
  const positionedEdges = baseEdges.filter((edge) => {
    const sourceNode = positionedNodeById.get(edge.source);
    const targetNode = positionedNodeById.get(edge.target);
    if (!sourceNode || !targetNode) return false;
    edge.sourceX = sourceNode.x;
    edge.sourceY = sourceNode.y;
    edge.targetX = targetNode.x;
    edge.targetY = targetNode.y;
    return true;
  });

  positionedNodes.forEach((node) => {
    const cluster = clusterByNodeId.get(node.id);
    if (!cluster) return;
    node.clusterId = cluster.id;
    node.clusterLabel = cluster.label;
  });

  return {
    nodes: positionedNodes,
    edges: positionedEdges,
    isCyclic: hasCycles,
  };
}

function buildCollapsedNode(clusterId: string, clusterNodes: NodeData[]) {
  const centerX =
    clusterNodes.reduce((total, node) => total + node.x, 0) / Math.max(1, clusterNodes.length);
  const centerY =
    clusterNodes.reduce((total, node) => total + node.y, 0) / Math.max(1, clusterNodes.length);
  const label = clusterNodes[0]?.clusterLabel || clusterNodes[0]?.type || 'Grouped';

  return {
    id: `group:${clusterId}`,
    label: `[+] ${label} x${clusterNodes.length}`,
    type: clusterNodes[0]?.type || 'Unknown',
    x: centerX,
    y: centerY,
    z: 0,
    vx: 0,
    vy: 0,
    size: 16 + Math.min(14, clusterNodes.length),
    labels: clusterNodes[0]?.labels || [],
    tags: clusterNodes[0]?.tags || {},
    tagKeys: clusterNodes[0]?.tagKeys || [],
    depth: clusterNodes[0]?.depth,
    componentId: clusterNodes[0]?.componentId,
    clusterId,
    clusterLabel: label,
    nodeKind: 'group' as const,
    groupTargetId: clusterId,
    memberNodeIds: clusterNodes.map((node) => node.id),
    alwaysShowLabel: true,
  };
}

function buildGroupAnchor(clusterId: string, clusterNodes: NodeData[]) {
  const minY = Math.min(...clusterNodes.map((node) => node.y));
  const centerX =
    clusterNodes.reduce((total, node) => total + node.x, 0) / Math.max(1, clusterNodes.length);
  const label = clusterNodes[0]?.clusterLabel || clusterNodes[0]?.type || 'Grouped';

  return {
    id: `group-anchor:${clusterId}`,
    label: `[-] ${label} x${clusterNodes.length}`,
    type: clusterNodes[0]?.type || 'Unknown',
    x: centerX,
    y: minY - 36,
    z: 0,
    vx: 0,
    vy: 0,
    size: 14,
    labels: clusterNodes[0]?.labels || [],
    tags: clusterNodes[0]?.tags || {},
    tagKeys: clusterNodes[0]?.tagKeys || [],
    depth: clusterNodes[0]?.depth,
    componentId: clusterNodes[0]?.componentId,
    clusterId,
    clusterLabel: label,
    nodeKind: 'group-anchor' as const,
    groupTargetId: clusterId,
    memberNodeIds: clusterNodes.map((node) => node.id),
    alwaysShowLabel: true,
  };
}

function buildSyntheticEdgeLabel(edge: EdgeData, edgeCount: number) {
  const label = edge.relationType || edge.label || 'related';
  return edgeCount > 1 ? `${label} x${edgeCount}` : label;
}

export function buildCollapsibleDisplayGraph(
  nodes: NodeData[],
  edges: EdgeData[],
  options?: {
    collapseThreshold?: number;
    collapseRelatedNodes?: boolean;
    expandedClusterIds?: Iterable<string>;
  }
) {
  const collapseThreshold = options?.collapseThreshold || DEFAULT_COLLAPSE_THRESHOLD;
  const collapseRelatedNodes = options?.collapseRelatedNodes ?? false;
  const expandedClusterIds = new Set(options?.expandedClusterIds || []);

  if (!collapseRelatedNodes) {
    return {
      nodes,
      edges,
      collapsibleClusterIds: [] as string[],
      collapsedClusterIds: [] as string[],
      expandedClusterIds: [] as string[],
    };
  }

  const clusterNodesById = new Map<string, NodeData[]>();
  nodes.forEach((node) => {
    if (!node.clusterId) return;
    const clusterNodes = clusterNodesById.get(node.clusterId) || [];
    clusterNodes.push(node);
    clusterNodesById.set(node.clusterId, clusterNodes);
  });

  const collapsibleClusterIds = Array.from(clusterNodesById.entries())
    .filter(([, clusterNodes]) => clusterNodes.length >= collapseThreshold)
    .map(([clusterId]) => clusterId);

  if (collapsibleClusterIds.length < 1) {
    return {
      nodes,
      edges,
      collapsibleClusterIds: [] as string[],
      collapsedClusterIds: [] as string[],
      expandedClusterIds: [] as string[],
    };
  }

  const collapsibleClusterIdSet = new Set(collapsibleClusterIds);
  const visibleNodes: NodeData[] = [];
  const ownerNodeIdByNodeId = new Map<string, string>();
  const collapsedClusterResultIds: string[] = [];
  const expandedClusterResultIds: string[] = [];

  clusterNodesById.forEach((clusterNodes, clusterId) => {
    const sortedClusterNodes = [...clusterNodes].sort((leftNode, rightNode) => {
      if (leftNode.y !== rightNode.y) return leftNode.y - rightNode.y;
      return leftNode.x - rightNode.x;
    });

    if (!collapsibleClusterIdSet.has(clusterId)) {
      visibleNodes.push(...sortedClusterNodes);
      sortedClusterNodes.forEach((node) => ownerNodeIdByNodeId.set(node.id, node.id));
      return;
    }

    if (expandedClusterIds.has(clusterId)) {
      visibleNodes.push(buildGroupAnchor(clusterId, sortedClusterNodes));
      visibleNodes.push(...sortedClusterNodes);
      sortedClusterNodes.forEach((node) => ownerNodeIdByNodeId.set(node.id, node.id));
      expandedClusterResultIds.push(clusterId);
      return;
    }

    const collapsedNode = buildCollapsedNode(clusterId, sortedClusterNodes);
    visibleNodes.push(collapsedNode);
    sortedClusterNodes.forEach((node) => ownerNodeIdByNodeId.set(node.id, collapsedNode.id));
    collapsedClusterResultIds.push(clusterId);
  });

  nodes
    .filter((node) => !node.clusterId)
    .forEach((node) => {
      visibleNodes.push(node);
      ownerNodeIdByNodeId.set(node.id, node.id);
    });

  const displayEdgesByKey = new Map<string, EdgeData>();
  edges.forEach((edge) => {
    const sourceOwnerNodeId = ownerNodeIdByNodeId.get(edge.source);
    const targetOwnerNodeId = ownerNodeIdByNodeId.get(edge.target);

    if (!sourceOwnerNodeId || !targetOwnerNodeId || sourceOwnerNodeId === targetOwnerNodeId) return;

    const syntheticKey = `${sourceOwnerNodeId}|${targetOwnerNodeId}|${edge.relationType || edge.label || 'related'}`;
    const isSynthetic = sourceOwnerNodeId !== edge.source || targetOwnerNodeId !== edge.target;
    const key = isSynthetic ? syntheticKey : edge.id;
    const existingEdge = displayEdgesByKey.get(key);

    if (!existingEdge) {
      displayEdgesByKey.set(key, {
        ...edge,
        id: isSynthetic ? `group-edge:${syntheticKey}` : edge.id,
        source: sourceOwnerNodeId,
        target: targetOwnerNodeId,
        isSynthetic,
        rawEdgeIds: [...(edge.rawEdgeIds || [edge.id])],
        edgeCount: 1,
        label: isSynthetic ? buildSyntheticEdgeLabel(edge, 1) : edge.label,
      });
      return;
    }

    existingEdge.edgeCount = (existingEdge.edgeCount || 1) + 1;
    existingEdge.rawEdgeIds = [...(existingEdge.rawEdgeIds || []), edge.id];
    existingEdge.label = buildSyntheticEdgeLabel(existingEdge, existingEdge.edgeCount);
    existingEdge.isSynthetic = true;
  });

  return {
    nodes: visibleNodes.sort((leftNode, rightNode) => {
      if (leftNode.x !== rightNode.x) return leftNode.x - rightNode.x;
      return leftNode.y - rightNode.y;
    }),
    edges: Array.from(displayEdgesByKey.values()),
    collapsibleClusterIds,
    collapsedClusterIds: collapsedClusterResultIds,
    expandedClusterIds: expandedClusterResultIds,
  };
}
