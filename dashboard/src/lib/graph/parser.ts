import { DOMParser } from '@xmldom/xmldom';
import type { NodeData, EdgeData } from './types';
import { Edge, Node } from 'litegraphdb/dist/types/types';

// export function parseGexf(gexfContent: string) {
//   const parser = new DOMParser();
//   const xmlDoc = parser.parseFromString(gexfContent, 'text/xml');

//   const parsedNodes: NodeData[] = [];
//   const parsedEdges: EdgeData[] = [];

//   // Parse nodes
//   const nodeElements = xmlDoc.getElementsByTagName('node');
//   for (let i = 0; i < nodeElements.length; i++) {
//     const node = nodeElements[i];
//     const nodeId = node.getAttribute('id') || '';
//     const label = node.getAttribute('label') || nodeId;

//     let type = 'server';
//     const attvalues = node.getElementsByTagName('attvalue');
//     for (let j = 0; j < attvalues.length; j++) {
//       const attvalue = attvalues[j];
//       if (attvalue.getAttribute('for') === 'type') {
//         type = attvalue.getAttribute('value') || type;
//         break;
//       }
//     }

//     parsedNodes.push({
//       id: nodeId,
//       label,
//       type,
//       x: Math.random() * 800,
//       y: Math.random() * 600,
//       z: 0,
//       vx: 0,
//       vy: 0,
//       isDragging: false,
//     });
//   }

//   // Parse edges
//   const edgeElements = xmlDoc.getElementsByTagName('edge');
//   for (let i = 0; i < edgeElements.length; i++) {
//     const edge = edgeElements[i];
//     const edgeId = edge.getAttribute('id') || `e${i}`;
//     const source = edge.getAttribute('source') || '';
//     const target = edge.getAttribute('target') || '';

//     let cost = 1;
//     let data = '';
//     const attvalues = edge.getElementsByTagName('attvalue');
//     for (let j = 0; j < attvalues.length; j++) {
//       const attvalue = attvalues[j];
//       if (attvalue.getAttribute('for') === 'cost') {
//         cost = Number(attvalue.getAttribute('value')) || cost;
//       } else if (attvalue.getAttribute('for') === 'data') {
//         data = attvalue.getAttribute('value') || '';
//       }
//     }

//     const sourceNode = parsedNodes.find((n) => n.id === source);
//     const targetNode = parsedNodes.find((n) => n.id === target);

//     parsedEdges.push({
//       id: edgeId,
//       source,
//       target,
//       cost,
//       data,
//       sourceX: sourceNode?.x || 0,
//       sourceY: sourceNode?.y || 0,
//       targetX: targetNode?.x || 0,
//       targetY: targetNode?.y || 0,
//     });
//   }

//   return { nodes: parsedNodes, edges: parsedEdges };
// }

// Function to build the adjacency list based on dependencies
export const buildAdjacencyList = (nodes: Node[], dependencies: { from: string; to: string }[]) => {
  const adjList: Record<string, string[]> = {};

  // Initialize adjacency list with empty arrays for each node
  nodes.forEach((node) => {
    adjList[node.GUID] = [];
  });

  // Add dependencies to the adjacency list
  dependencies.forEach((dependency) => {
    adjList[dependency.from]?.push(dependency.to);
  });

  return adjList;
};

// Topological Sort using Kahn's Algorithm
export const topologicalSortKahn = (
  adjList: Record<string, string[]>
): { topologicalOrder: { id: string; x: number; y: number; z: number }[]; isCyclic: boolean } => {
  const inDegree: Record<string, number> = {}; // In-degree for each node
  const queue: { id: string; x: number; y: number; z: number }[] = []; // Queue for nodes with in-degree 0
  const topologicalOrder: { id: string; x: number; y: number; z: number }[] = [];
  const level = 0;
  // Initialize in-degree of all nodes to 0
  Object.keys(adjList).forEach((node) => {
    inDegree[node] = 0; // Initially, no incoming edges
  });

  // Calculate the in-degree for each node based on adjList
  Object.keys(adjList).forEach((node) => {
    adjList[node].forEach((neighbor) => {
      inDegree[neighbor]++;
    });
  });

  // Add nodes with in-degree 0 to the queue
  Object.keys(inDegree).forEach((node) => {
    if (inDegree[node] === 0) {
      queue.push({ id: node, x: 0, y: 0, z: 0 });
    }
  });

  // Process nodes in the queue
  // console.log(queue, 'chk queue');
  // console.log({ ...inDegree }, 'chk inDegree');
  function processNode(node: { id: string; x: number; y: number; z: number }) {
    // console.log(node, 'chk node to process');
    topologicalOrder.push(node);

    adjList[node.id].forEach((neighbor) => {
      inDegree[neighbor]--;
      if (inDegree[neighbor] === 0) {
        queue.push({ id: neighbor, x: 0, y: node.y + 1, z: 0 });
      }
    });
    if (queue.length > 0) {
      processNode(queue.shift()!);
    }
  }
  const node = queue.shift();
  if (node) {
    processNode(node);
  }
  let isCyclic = false;
  // Check if there was a cycle (not all nodes were processed)
  if (topologicalOrder.length !== Object.keys(adjList).length) {
    isCyclic = true;
  }

  return { topologicalOrder, isCyclic };
};

// Parse nodes and assign positions based on topological sort and calculated depths
export function parseNode(
  nodes: Node[],
  totalNodes: number, // current graph instance
  adjList: Record<string, string[]>, // adjacency list (node dependencies)
  topologicalOrder: { id: string; x: number; y: number; z: number }[], // topological sort order of node GUIDs
  showGraphHorizontal = false,
  centerX = 0, // Center X position (where the topological order starts)
  centerY = 0, // Base Y position (used for vertical stacking)
  centerZ = 0, // Base Z position (used for depth stacking)
  horizontalSpacing = 100, // Spacing between nodes in the x-direction
  verticalSpacing = 200, // Base spacing between layers in the y-direction
  depthSpacing = 1000 // Spacing between layers in the z-direction
): NodeData[] {
  const existingNodeCount = totalNodes;

  // Create a map of node GUID to its index in the topological order

  // 1. Initialize depth for each node and calculate the depths
  const nodeDepthMap: Record<string, number> = {};
  const visited = new Set<string>(); // To prevent cycles

  // Function to calculate the depth of each node (how far from the root)
  function calculateDepth(node: string): number {
    if (visited.has(node)) return nodeDepthMap[node]; // Return pre-calculated depth

    visited.add(node);

    let maxDepth = 0;
    adjList[node]?.forEach((dependency) => {
      maxDepth = Math.max(maxDepth, calculateDepth(dependency));
    });

    // The depth of this node is 1 more than its deepest dependency
    nodeDepthMap[node] = maxDepth + 1;
    return nodeDepthMap[node];
  }

  // Calculate depths for all nodes
  topologicalOrder.forEach((node) => {
    if (!nodeDepthMap[node.id]) {
      calculateDepth(node.id);
    }
  });
  const nodesInEachDepth = Object.entries(nodeDepthMap).reduce(
    (acc, [nodeId, depth]) => {
      acc[depth] = [...(acc[depth] || []), nodeId];
      return acc;
    },
    {} as Record<number, string[]>
  );
  // 2. Assign x, y, z positions based on topological order and calculated depths
  return nodes.map((node, i) => {
    // Get the topological position of the node in the sorted list
    const depth = nodeDepthMap[node.GUID] ?? 0; // Depth of the current
    const nodesInDepth = nodesInEachDepth[depth] ?? [];
    const nodeIndexMap = nodesInDepth?.reduce(
      (acc, nodeId, index) => {
        acc[nodeId] = index;
        return acc;
      },
      {} as Record<string, number>
    );
    const sortedIndex = nodeIndexMap[node.GUID] ?? i; // Fallback to 'i' if the node is not in the topological ordernode

    // Position nodes along the x-axis based on the sorted order
    const x =
      centerX -
      Math.abs(((nodesInDepth.length - 1) * horizontalSpacing) / 2) +
      sortedIndex * horizontalSpacing;
    // Calculate y-position using node depth and vertical spacing
    const y = centerY - depth * verticalSpacing;

    // Calculate z-position based on depth and a new spacing
    const z = centerZ - depth * depthSpacing;

    return {
      id: node.GUID,
      label: node.Name,
      type: node.Labels[0],
      x: showGraphHorizontal ? y : x,
      y: showGraphHorizontal ? x : y,
      z, // Add the z-axis position for 3D visualization
      vx: 0,
      vy: 0,
      isDragging: false,
    };
  });
}

export function parseEdge(edges: Edge[]): EdgeData[] {
  return edges.map((edge) => ({
    id: edge.GUID,
    source: edge.From,
    target: edge.To,
    cost: edge.Cost || 0,
    label: edge.Name,
    data: '',
    sourceX: 0,
    sourceY: 0,
    targetX: 0,
    targetY: 0,
  }));
}

// New deterministic circular layout that doesn't shuffle during lazy loading
export function parseCircularNodeDeterministic(
  nodes: Node[],
  radiusStep = 200,
  centerX = 5000,
  centerY = 5000
): NodeData[] {
  return nodes.map((node) => {
    // Use a hash of the node ID to create deterministic positions
    const hash = node.GUID.split('').reduce((a, b) => {
      a = (a << 5) - a + b.charCodeAt(0);
      return a & a;
    }, 0);

    // Use hash to determine circle index
    const circleIndex = Math.abs(hash) % 5; // Fixed number of circles (0-4)

    // Band radius range
    const minRadius = circleIndex * radiusStep;
    const maxRadius = (circleIndex + 1) * radiusStep;

    // Deterministic radius within band
    const radius = minRadius + (Math.abs(hash) % (maxRadius - minRadius));

    // Deterministic angle based on hash
    const angle = (Math.abs(hash) % 360) * (Math.PI / 180);

    const x = centerX + radius * Math.cos(angle);
    const y = centerY + radius * Math.sin(angle);

    return {
      id: node.GUID,
      label: node.Name,
      type: node.Labels[0],
      x,
      y,
      z: 0,
      vx: 0,
      vy: 0,
      isDragging: false,
    };
  });
}

export function parseNodeGroupedByLabel(
  nodes: Node[],
  showGraphHorizontal = false,
  horizontalSpacing = 200,
  verticalSpacing = 200,
  depthSpacing = 1000
): NodeData[] {
  const fixedLevels = {
    StoragePool: 0,
    BucketMetadata: 1,
    TenantMetadata: 1,
    VectorRepository: 2,
    Collection: 2,
    ObjectMetadata: 3,
    EmbeddingsDocument: 4,
    SourceDocument: 5,
  };

  const labelLevelMap: Record<string, number> = {};
  let dynamicLevel = 3;

  // Assign levels per label
  for (const node of nodes) {
    const label = node.Labels[0]; // Assume first label
    if (fixedLevels[label as keyof typeof fixedLevels] !== undefined) {
      labelLevelMap[label] = fixedLevels[label as keyof typeof fixedLevels];
    } else if (!(label in labelLevelMap)) {
      labelLevelMap[label] = dynamicLevel++;
    }
  }

  // Group nodes by level
  const levelGroups: Record<number, Node[]> = {};
  for (const node of nodes) {
    const label = node.Labels[0];
    const level = labelLevelMap[label];
    if (!levelGroups[level]) levelGroups[level] = [];
    levelGroups[level].push(node);
  }

  const result: NodeData[] = [];

  Object.entries(levelGroups).forEach(([levelStr, group]) => {
    const level = parseInt(levelStr);
    const groupSize = group.length;

    group.forEach((node, i) => {
      const offset = (groupSize - 1) / 2; // center align
      const x = showGraphHorizontal
        ? level * horizontalSpacing // Root on left, children go right
        : (i - offset) * horizontalSpacing;

      const y = showGraphHorizontal
        ? (i - offset) * verticalSpacing // Stack vertically
        : -level * verticalSpacing; // Root on top, children below

      result.push({
        id: node.GUID,
        label: node.Name,
        type: node.Labels[0],
        x,
        y,
        z: level * depthSpacing,
        vx: 0,
        vy: 0,
        isDragging: false,
      });
    });
  });

  return result;
}
