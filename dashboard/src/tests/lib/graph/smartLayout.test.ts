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

  it('infers a deeper hierarchy when inverse edges would otherwise flatten the graph', () => {
    const nodes = [
      createNode('workspace', 'Workspace', ['Workspace'], { workspaceId: '1' }),
      createNode('topic-1', 'Topic 1', ['Topic'], { topicId: '1' }),
      createNode('topic-2', 'Topic 2', ['Topic'], { topicId: '2' }),
      createNode('topic-3', 'Topic 3', ['Topic'], { topicId: '3' }),
      createNode('assertion-1', 'Assertion 1', ['Assertion'], { assertionId: '1' }),
      createNode('assertion-2', 'Assertion 2', ['Assertion'], { assertionId: '2' }),
      createNode('assertion-3', 'Assertion 3', ['Assertion'], { assertionId: '3' }),
      createNode('assertion-4', 'Assertion 4', ['Assertion'], { assertionId: '4' }),
      createNode('chunk-1', 'Chunk 1', ['Chunk', 'Text'], { chunkId: '1' }),
      createNode('chunk-2', 'Chunk 2', ['Chunk', 'Text'], { chunkId: '2' }),
      createNode('chunk-3', 'Chunk 3', ['Chunk', 'Text'], { chunkId: '3' }),
      createNode('chunk-4', 'Chunk 4', ['Chunk', 'Text'], { chunkId: '4' }),
    ];
    const edges = [
      createEdge('edge-wt1', 'workspace', 'topic-1', ['HasTopic']),
      createEdge('edge-wt2', 'workspace', 'topic-2', ['HasTopic']),
      createEdge('edge-wt3', 'workspace', 'topic-3', ['HasTopic']),
      createEdge('edge-t1a1', 'topic-1', 'assertion-1', ['Contains']),
      createEdge('edge-a1t1', 'assertion-1', 'topic-1', ['BelongsTo']),
      createEdge('edge-t1a2', 'topic-1', 'assertion-2', ['Contains']),
      createEdge('edge-a2t1', 'assertion-2', 'topic-1', ['BelongsTo']),
      createEdge('edge-t2a3', 'topic-2', 'assertion-3', ['Contains']),
      createEdge('edge-a3t2', 'assertion-3', 'topic-2', ['BelongsTo']),
      createEdge('edge-t3a4', 'topic-3', 'assertion-4', ['Contains']),
      createEdge('edge-a4t3', 'assertion-4', 'topic-3', ['BelongsTo']),
      createEdge('edge-c1a1', 'chunk-1', 'assertion-1', ['Supports']),
      createEdge('edge-a1c1', 'assertion-1', 'chunk-1', ['ExtractedFrom']),
      createEdge('edge-c2a2', 'chunk-2', 'assertion-2', ['Supports']),
      createEdge('edge-a2c2', 'assertion-2', 'chunk-2', ['ExtractedFrom']),
      createEdge('edge-c3a3', 'chunk-3', 'assertion-3', ['Supports']),
      createEdge('edge-a3c3', 'assertion-3', 'chunk-3', ['ExtractedFrom']),
      createEdge('edge-c4a4', 'chunk-4', 'assertion-4', ['Supports']),
      createEdge('edge-a4c4', 'assertion-4', 'chunk-4', ['ExtractedFrom']),
    ];

    const result = buildSmartDepthLayout(nodes, edges, true);
    const workspaceNode = result.nodes.find((node) => node.id === 'workspace');
    const topicNode = result.nodes.find((node) => node.id === 'topic-1');
    const assertionNode = result.nodes.find((node) => node.id === 'assertion-1');
    const chunkNode = result.nodes.find((node) => node.id === 'chunk-1');
    const uniqueDepths = new Set(result.nodes.map((node) => node.depth));

    expect(result.isCyclic).toBe(true);
    expect(uniqueDepths.size).toBeGreaterThanOrEqual(4);
    expect(workspaceNode?.depth || 0).toBeLessThan(topicNode?.depth || 0);
    expect(topicNode?.depth || 0).toBeLessThan(assertionNode?.depth || 0);
    expect(assertionNode?.depth || 0).toBeLessThan(chunkNode?.depth || 0);
  });

  it('ignores one-off tag keys when grouping otherwise similar nodes', () => {
    const nodes = [
      createNode('node-1', 'Node 1', ['Chunk'], { alphaKey: '1' }),
      createNode('node-2', 'Node 2', ['Chunk'], { betaKey: '1' }),
      createNode('node-3', 'Node 3', ['Chunk'], { gammaKey: '1' }),
      createNode('node-4', 'Node 4', ['Chunk'], { deltaKey: '1' }),
      createNode('node-5', 'Node 5', ['Document'], { docId: '1' }),
    ];
    const edges = [
      createEdge('edge-15', 'node-1', 'node-5'),
      createEdge('edge-25', 'node-2', 'node-5'),
      createEdge('edge-35', 'node-3', 'node-5'),
      createEdge('edge-45', 'node-4', 'node-5'),
    ];

    const result = buildSmartDepthLayout(nodes, edges, true);
    const chunkClusterIds = result.nodes
      .filter((node) => node.id !== 'node-5')
      .map((node) => node.clusterId);

    expect(new Set(chunkClusterIds).size).toBe(1);
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
