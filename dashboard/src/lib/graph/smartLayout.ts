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
const MIN_ADAPTIVE_LAYERING_NODES = 12;
const MAX_SIGNATURE_TAG_KEYS = 2;
const MAX_SIGNATURE_TYPES = 1;
const MAX_SIGNATURE_RELATIONS = 2;
const COMMON_TAG_KEY_RATIO = 0.55;

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

type LayoutSummary = {
  largestComponentSize: number;
  largestDepthBandSize: number;
  maxDepth: number;
};

function toSortedUnique(values: string[] | undefined, limit = 4): string[] {
  return Array.from(new Set((values || []).filter(Boolean)))
    .sort()
    .slice(0, limit);
}

function countValues(values: string[] | undefined): Map<string, number> {
  const counts = new Map<string, number>();

  (values || []).filter(Boolean).forEach((value) => {
    counts.set(value, (counts.get(value) || 0) + 1);
  });

  return counts;
}

function getDominantValues(values: string[] | undefined, limit = 2): string[] {
  return Array.from(countValues(values).entries())
    .sort((leftEntry, rightEntry) => {
      if (leftEntry[1] !== rightEntry[1]) {
        return rightEntry[1] - leftEntry[1];
      }

      return leftEntry[0].localeCompare(rightEntry[0]);
    })
    .slice(0, limit)
    .map(([value]) => value);
}

function bucketCount(count: number) {
  if (count <= 0) return '0';
  if (count === 1) return '1';
  if (count <= 3) return '2-3';
  if (count <= 6) return '4-6';
  return '7+';
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

function buildTagKeyFrequency(nodes: NodeData[]) {
  const counts = new Map<string, number>();

  nodes.forEach((node) => {
    Array.from(new Set(node.tagKeys || [])).forEach((tagKey) => {
      counts.set(tagKey, (counts.get(tagKey) || 0) + 1);
    });
  });

  return counts;
}

function getInformativeTagKeys(
  node: NodeData | undefined,
  tagKeyFrequency: Map<string, number>,
  totalNodeCount: number,
  limit = MAX_SIGNATURE_TAG_KEYS
) {
  if (!node?.tagKeys?.length) return [];

  const maxCommonCount = Math.max(3, Math.ceil(totalNodeCount * COMMON_TAG_KEY_RATIO));

  return Array.from(new Set(node.tagKeys))
    .map((tagKey) => ({
      tagKey,
      count: tagKeyFrequency.get(tagKey) || 0,
    }))
    .filter(({ count }) => count > 1 && count < maxCommonCount)
    .sort((leftTagKey, rightTagKey) => {
      if (leftTagKey.count !== rightTagKey.count) {
        return leftTagKey.count - rightTagKey.count;
      }

      return leftTagKey.tagKey.localeCompare(rightTagKey.tagKey);
    })
    .slice(0, limit)
    .map(({ tagKey }) => tagKey);
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

function summarizeLayout(
  components: ComponentInfo[],
  depthByComponentId: Map<string, number>
): LayoutSummary {
  const nodeCountByDepth = new Map<number, number>();
  let largestComponentSize = 0;

  components.forEach((component) => {
    const depth = depthByComponentId.get(component.id) || 0;
    largestComponentSize = Math.max(largestComponentSize, component.nodeIds.length);
    nodeCountByDepth.set(depth, (nodeCountByDepth.get(depth) || 0) + component.nodeIds.length);
  });

  return {
    largestComponentSize,
    largestDepthBandSize: Math.max(0, ...Array.from(nodeCountByDepth.values())),
    maxDepth: Math.max(0, ...Array.from(nodeCountByDepth.keys())),
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

function shouldUseAdaptiveLayering(nodeCount: number, hasCycles: boolean, summary: LayoutSummary) {
  if (nodeCount < MIN_ADAPTIVE_LAYERING_NODES) return false;

  const largestComponentRatio = summary.largestComponentSize / Math.max(1, nodeCount);
  const largestDepthBandRatio = summary.largestDepthBandSize / Math.max(1, nodeCount);

  if (hasCycles && largestComponentRatio >= 0.35) return true;
  if (hasCycles && summary.maxDepth <= 2 && largestDepthBandRatio >= 0.5) return true;
  if (summary.maxDepth <= 1 && largestDepthBandRatio >= 0.7) return true;

  return false;
}

function buildDegreeByNodeId(nodeIds: string[], edges: EdgeData[]) {
  const degreeByNodeId = new Map(nodeIds.map((nodeId) => [nodeId, 0]));

  edges.forEach((edge) => {
    degreeByNodeId.set(edge.source, (degreeByNodeId.get(edge.source) || 0) + 1);
    degreeByNodeId.set(edge.target, (degreeByNodeId.get(edge.target) || 0) + 1);
  });

  return degreeByNodeId;
}

function getNodeSize(nodeId: string, degreeByNodeId: Map<string, number>, maxDegree: number) {
  if (maxDegree <= 1) return 15;

  const degree = degreeByNodeId.get(nodeId) || 0;
  const normalizedDegree = Math.sqrt(degree / maxDegree);

  return 13 + normalizedDegree * 10;
}

function computeLocalComponentDepths(
  componentNodeIds: string[],
  componentEdges: EdgeData[],
  seedNodeIds: string[],
  nodeById: Map<string, NodeData>
) {
  const nodeIdSet = new Set(componentNodeIds);
  const outgoingNodeIds = new Map(componentNodeIds.map((nodeId) => [nodeId, [] as string[]]));
  const incomingNodeIds = new Map(componentNodeIds.map((nodeId) => [nodeId, [] as string[]]));

  componentEdges.forEach((edge) => {
    if (!nodeIdSet.has(edge.source) || !nodeIdSet.has(edge.target)) return;
    outgoingNodeIds.get(edge.source)?.push(edge.target);
    incomingNodeIds.get(edge.target)?.push(edge.source);
  });

  const localDepthByNodeId = new Map<string, number>();
  const traversalQueue: string[] = [];

  const compareNodeIds = (leftNodeId: string, rightNodeId: string) => {
    const leftLabel = nodeById.get(leftNodeId)?.label || leftNodeId;
    const rightLabel = nodeById.get(rightNodeId)?.label || rightNodeId;
    const labelComparison = leftLabel.localeCompare(rightLabel);
    if (labelComparison !== 0) return labelComparison;
    return leftNodeId.localeCompare(rightNodeId);
  };

  const chooseSeedNodeId = (availableNodeIds: string[]) =>
    [...availableNodeIds].sort((leftNodeId, rightNodeId) => {
      const leftOutgoingCount = outgoingNodeIds.get(leftNodeId)?.length || 0;
      const leftIncomingCount = incomingNodeIds.get(leftNodeId)?.length || 0;
      const rightOutgoingCount = outgoingNodeIds.get(rightNodeId)?.length || 0;
      const rightIncomingCount = incomingNodeIds.get(rightNodeId)?.length || 0;
      const leftScore = leftOutgoingCount - leftIncomingCount;
      const rightScore = rightOutgoingCount - rightIncomingCount;

      if (leftScore !== rightScore) {
        return rightScore - leftScore;
      }

      const leftTotalDegree = leftOutgoingCount + leftIncomingCount;
      const rightTotalDegree = rightOutgoingCount + rightIncomingCount;
      if (leftTotalDegree !== rightTotalDegree) {
        return rightTotalDegree - leftTotalDegree;
      }

      return compareNodeIds(leftNodeId, rightNodeId);
    })[0];

  const enqueueSeeds = (candidateNodeIds: string[], depth: number) => {
    candidateNodeIds
      .filter((nodeId) => nodeIdSet.has(nodeId))
      .sort(compareNodeIds)
      .forEach((nodeId) => {
        if (localDepthByNodeId.has(nodeId)) return;
        localDepthByNodeId.set(nodeId, depth);
        traversalQueue.push(nodeId);
      });
  };

  enqueueSeeds(seedNodeIds, 0);

  if (traversalQueue.length < 1 && componentNodeIds.length > 0) {
    const seedNodeId = chooseSeedNodeId(componentNodeIds);
    if (seedNodeId) {
      enqueueSeeds([seedNodeId], 0);
    }
  }

  let currentMaxDepth = Math.max(0, ...Array.from(localDepthByNodeId.values()));

  while (traversalQueue.length > 0 || localDepthByNodeId.size < componentNodeIds.length) {
    while (traversalQueue.length > 0) {
      const nodeId = traversalQueue.shift();
      if (!nodeId) continue;

      const currentDepth = localDepthByNodeId.get(nodeId) || 0;
      currentMaxDepth = Math.max(currentMaxDepth, currentDepth);

      Array.from(new Set(outgoingNodeIds.get(nodeId) || []))
        .sort(compareNodeIds)
        .forEach((targetNodeId) => {
          if (localDepthByNodeId.has(targetNodeId)) return;
          localDepthByNodeId.set(targetNodeId, currentDepth + 1);
          traversalQueue.push(targetNodeId);
        });
    }

    const remainingNodeIds = componentNodeIds.filter((nodeId) => !localDepthByNodeId.has(nodeId));
    if (remainingNodeIds.length < 1) break;

    const seedNodeId = chooseSeedNodeId(remainingNodeIds);
    if (!seedNodeId) break;

    currentMaxDepth += 1;
    localDepthByNodeId.set(seedNodeId, currentMaxDepth);
    traversalQueue.push(seedNodeId);
  }

  return localDepthByNodeId;
}

function computeAdaptiveDepthByNodeId(
  components: ComponentInfo[],
  componentByNodeId: Map<string, string>,
  depthByComponentId: Map<string, number>,
  edges: EdgeData[],
  nodeById: Map<string, NodeData>
) {
  const componentEdges = new Map<string, EdgeData[]>();
  const entryNodeIdsByComponentId = new Map<string, Set<string>>();

  components.forEach((component) => {
    componentEdges.set(component.id, []);
    entryNodeIdsByComponentId.set(component.id, new Set<string>());
  });

  edges.forEach((edge) => {
    const sourceComponentId = componentByNodeId.get(edge.source);
    const targetComponentId = componentByNodeId.get(edge.target);
    if (!sourceComponentId || !targetComponentId) return;

    if (sourceComponentId === targetComponentId) {
      componentEdges.get(sourceComponentId)?.push(edge);
      return;
    }

    entryNodeIdsByComponentId.get(targetComponentId)?.add(edge.target);
  });

  const localDepthByNodeId = new Map<string, number>();
  let maxLocalDepth = 0;

  components.forEach((component) => {
    const componentLocalDepths =
      component.nodeIds.length > 1
        ? computeLocalComponentDepths(
            component.nodeIds,
            componentEdges.get(component.id) || [],
            Array.from(entryNodeIdsByComponentId.get(component.id) || []),
            nodeById
          )
        : new Map([[component.nodeIds[0], 0]]);

    componentLocalDepths.forEach((localDepth, nodeId) => {
      localDepthByNodeId.set(nodeId, localDepth);
      maxLocalDepth = Math.max(maxLocalDepth, localDepth);
    });
  });

  const depthStride = Math.max(2, maxLocalDepth + 1);
  const adaptiveDepthByNodeId = new Map<string, number>();

  components.forEach((component) => {
    const componentDepth = depthByComponentId.get(component.id) || 0;
    component.nodeIds.forEach((nodeId) => {
      adaptiveDepthByNodeId.set(
        nodeId,
        componentDepth * depthStride + (localDepthByNodeId.get(nodeId) || 0)
      );
    });
  });

  return adaptiveDepthByNodeId;
}

function buildNodeSignature(
  node: NodeData,
  nodeById: Map<string, NodeData>,
  incomingEdgesByNodeId: Map<string, EdgeData[]>,
  outgoingEdgesByNodeId: Map<string, EdgeData[]>,
  tagKeyFrequency: Map<string, number>,
  totalNodeCount: number
) {
  const incomingEdges = incomingEdgesByNodeId.get(node.id) || [];
  const outgoingEdges = outgoingEdgesByNodeId.get(node.id) || [];

  const incomingTypes = getDominantValues(
    incomingEdges.map((edge) => nodeById.get(edge.source)?.type || 'Unknown'),
    MAX_SIGNATURE_TYPES
  );
  const outgoingTypes = getDominantValues(
    outgoingEdges.map((edge) => nodeById.get(edge.target)?.type || 'Unknown'),
    MAX_SIGNATURE_TYPES
  );
  const incomingRelations = getDominantValues(
    incomingEdges.map((edge) => edge.relationType || edge.label || 'related'),
    MAX_SIGNATURE_RELATIONS
  );
  const outgoingRelations = getDominantValues(
    outgoingEdges.map((edge) => edge.relationType || edge.label || 'related'),
    MAX_SIGNATURE_RELATIONS
  );
  const labelsKey = toSortedUnique(node.labels, 2).join('|') || 'Unknown';
  const tagKeysKey = getInformativeTagKeys(node, tagKeyFrequency, totalNodeCount).join('|') || '-';
  const degreeProfile = `${bucketCount(incomingEdges.length)}>${bucketCount(outgoingEdges.length)}`;

  return [
    labelsKey,
    tagKeysKey,
    incomingTypes.join('|') || '-',
    outgoingTypes.join('|') || '-',
    incomingRelations.join('|') || '-',
    outgoingRelations.join('|') || '-',
    degreeProfile,
  ].join('::');
}

function buildClusterLabel(
  nodes: NodeData[],
  tagKeyFrequency: Map<string, number>,
  totalNodeCount: number
) {
  const firstNode = nodes[0];
  const baseLabel = firstNode?.type || 'Unknown';
  const tagKeys = getInformativeTagKeys(firstNode, tagKeyFrequency, totalNodeCount);
  return tagKeys.length > 0 ? `${baseLabel} - ${tagKeys.join(', ')}` : baseLabel;
}

function buildClusterInfos(nodes: NodeData[], edges: EdgeData[]) {
  const nodeById = new Map(nodes.map((node) => [node.id, node]));
  const incomingEdgesByNodeId = new Map<string, EdgeData[]>();
  const outgoingEdgesByNodeId = new Map<string, EdgeData[]>();
  const tagKeyFrequency = buildTagKeyFrequency(nodes);
  const totalNodeCount = nodes.length;

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
      outgoingEdgesByNodeId,
      tagKeyFrequency,
      totalNodeCount
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
      label: buildClusterLabel([node], tagKeyFrequency, totalNodeCount),
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
    cluster.label = buildClusterLabel(clusterNodes, tagKeyFrequency, totalNodeCount);
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
  const degreeByNodeId = buildDegreeByNodeId(nodeIds, baseEdges);
  const maxDegree = Math.max(0, ...Array.from(degreeByNodeId.values()));

  const {
    components: baseComponents,
    componentByNodeId: baseComponentByNodeId,
    hasCycles,
  } = computeStronglyConnectedComponents(nodeIds, baseEdges);
  const baseDepthByComponentId = computeComponentDepths(
    baseComponents,
    baseComponentByNodeId,
    baseEdges
  );
  const baseLayoutSummary = summarizeLayout(baseComponents, baseDepthByComponentId);

  let layoutEdges = baseEdges;
  let depthByNodeId = new Map<string, number>();

  if (shouldUseAdaptiveLayering(nodeIds.length, hasCycles, baseLayoutSummary)) {
    depthByNodeId = computeAdaptiveDepthByNodeId(
      baseComponents,
      baseComponentByNodeId,
      baseDepthByComponentId,
      baseEdges,
      nodeById
    );
    layoutEdges = baseEdges.filter((edge) => {
      const sourceDepth = depthByNodeId.get(edge.source) || 0;
      const targetDepth = depthByNodeId.get(edge.target) || 0;
      return sourceDepth <= targetDepth;
    });
    if (layoutEdges.length < 1) {
      layoutEdges = baseEdges;
    }
  } else {
    baseNodes.forEach((node) => {
      const componentId = baseComponentByNodeId.get(node.id);
      depthByNodeId.set(node.id, componentId ? baseDepthByComponentId.get(componentId) || 0 : 0);
    });
  }

  baseNodes.forEach((node) => {
    node.componentId = baseComponentByNodeId.get(node.id);
    node.depth = depthByNodeId.get(node.id) || 0;
  });

  const { clusters, clusterByNodeId } = buildClusterInfos(baseNodes, layoutEdges);
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
            size: getNodeSize(node.id, degreeByNodeId, maxDegree),
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
