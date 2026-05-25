import {
  buildCollapsibleDisplayGraph,
  buildSmartDepthLayout,
  DEFAULT_COLLAPSE_THRESHOLD,
} from '@/lib/graph/smartLayout';
import type { Edge, Node } from 'litegraphdb/dist/types/types';

function createNode(
  id: string,
  name: string,
  labels: string[] = ['Entity'],
  tags: Record<string, any> = {}
): Node {
  return {
    TenantGUID: 'tenant-1',
    GUID: id,
    GraphGUID: 'graph-1',
    Name: name,
    Data: {},
    CreatedUtc: '2026-01-01T00:00:00Z',
    LastUpdateUtc: '2026-01-01T00:00:00Z',
    Labels: labels,
    Tags: tags,
    Vectors: [],
  };
}

function createEdge(id: string, from: string, to: string, labels: string[] = ['CONNECTS']): Edge {
  return {
    GUID: id,
    TenantGUID: 'tenant-1',
    GraphGUID: 'graph-1',
    Name: labels[0],
    From: from,
    To: to,
    Cost: 1,
    CreatedUtc: '2026-01-01T00:00:00Z',
    Data: {},
    LastUpdateUtc: '2026-01-01T00:00:00Z',
    Labels: labels,
    Tags: {},
    Vectors: [],
  };
}

describe('smartLayout', () => {
  it('builds a depth-aware layout that condenses cycles', () => {
    const nodes = [
      createNode('node-a', 'Node A', ['Document'], { docId: '1' }),
      createNode('node-b', 'Node B', ['Document'], { docId: '2' }),
      createNode('node-c', 'Node C', ['Chunk'], { chunkId: '1' }),
    ];
    const edges = [
      createEdge('edge-ab', 'node-a', 'node-b'),
      createEdge('edge-ba', 'node-b', 'node-a'),
      createEdge('edge-bc', 'node-b', 'node-c'),
    ];

    const result = buildSmartDepthLayout(nodes, edges, true);
    const nodeA = result.nodes.find((node) => node.id === 'node-a');
    const nodeB = result.nodes.find((node) => node.id === 'node-b');
    const nodeC = result.nodes.find((node) => node.id === 'node-c');

    expect(result.isCyclic).toBe(true);
    expect(nodeA?.componentId).toBe(nodeB?.componentId);
    expect(nodeA?.depth).toBe(nodeB?.depth);
    expect(nodeC?.depth || 0).toBeGreaterThan(nodeA?.depth || 0);
    expect(result.nodes.every((node) => node.clusterId)).toBe(true);
  });

  it('collapses related clusters into a visible group node and aggregates edges', () => {
    const nodes = [
      createNode('node-1', 'Node 1', ['Chunk'], { docId: '1', page: 1 }),
      createNode('node-2', 'Node 2', ['Chunk'], { docId: '2', page: 2 }),
      createNode('node-3', 'Node 3', ['Chunk'], { docId: '3', page: 3 }),
      createNode('node-4', 'Node 4', ['Chunk'], { docId: '4', page: 4 }),
      createNode('node-5', 'Node 5', ['Document'], { docId: '5' }),
    ];
    const edges = [
      createEdge('edge-15', 'node-1', 'node-5'),
      createEdge('edge-25', 'node-2', 'node-5'),
    ];
    const smartLayout = buildSmartDepthLayout(nodes, edges, true);

    const clusterId = smartLayout.nodes.find((node) => node.id === 'node-1')?.clusterId;
    expect(clusterId).toBeDefined();

    const clusterNodes = smartLayout.nodes.filter((node) =>
      ['node-1', 'node-2', 'node-3', 'node-4'].includes(node.id)
    );
    clusterNodes.forEach((node) => {
      node.clusterId = clusterId;
      node.clusterLabel = 'Chunk';
    });

    const collapsed = buildCollapsibleDisplayGraph(smartLayout.nodes, smartLayout.edges, {
      collapseRelatedNodes: true,
      collapseThreshold: DEFAULT_COLLAPSE_THRESHOLD,
    });

    expect(collapsed.collapsibleClusterIds).toContain(clusterId);
    expect(collapsed.nodes.some((node) => node.nodeKind === 'group')).toBe(true);
    expect(collapsed.edges).toHaveLength(1);
    expect(collapsed.edges[0].isSynthetic).toBe(true);
    expect(collapsed.edges[0].edgeCount).toBe(2);
  });

  it('shows an anchor and original nodes when a collapsed cluster is expanded', () => {
    const nodes = [
      createNode('node-1', 'Node 1', ['Chunk'], { docId: '1', page: 1 }),
      createNode('node-2', 'Node 2', ['Chunk'], { docId: '2', page: 2 }),
      createNode('node-3', 'Node 3', ['Chunk'], { docId: '3', page: 3 }),
      createNode('node-4', 'Node 4', ['Chunk'], { docId: '4', page: 4 }),
    ];
    const smartLayout = buildSmartDepthLayout(nodes, [], true);
    const clusterId = smartLayout.nodes[0].clusterId;

    smartLayout.nodes.forEach((node) => {
      node.clusterId = clusterId;
      node.clusterLabel = 'Chunk';
    });

    const expanded = buildCollapsibleDisplayGraph(smartLayout.nodes, smartLayout.edges, {
      collapseRelatedNodes: true,
      collapseThreshold: DEFAULT_COLLAPSE_THRESHOLD,
      expandedClusterIds: [clusterId || ''],
    });

    expect(expanded.nodes.some((node) => node.nodeKind === 'group-anchor')).toBe(true);
    expect(expanded.nodes.some((node) => node.nodeKind === 'group')).toBe(false);
    expect(expanded.nodes.filter((node) => node.nodeKind === 'node')).toHaveLength(4);
  });
});
