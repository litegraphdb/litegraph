import type { NodeData, EdgeData } from './types';

export function applyForceLayout(nodes: NodeData[], edges: EdgeData[]) {
  const k = 0.1; // Spring constant
  const gravity = 0.1;
  const damping = 0.8;
  const repulsion = 1000;

  nodes.forEach((node) => {
    // Skip force calculations for nodes being dragged
    if (node.isDragging) {
      node.vx = 0;
      node.vy = 0;
      return;
    }

    node.vx *= damping;
    node.vy *= damping;

    // Apply gravity to center
    node.vx += (400 - node.x) * gravity;
    node.vy += (300 - node.y) * gravity;

    // Apply repulsion between nodes
    nodes.forEach((other) => {
      if (node === other || other.isDragging) return;
      const dx = node.x - other.x;
      const dy = node.y - other.y;
      const dist = Math.sqrt(dx * dx + dy * dy) || 1;
      const force = repulsion / (dist * dist);
      node.vx += (dx / dist) * force;
      node.vy += (dy / dist) * force;
    });
  });

  // Apply spring forces along edges
  edges.forEach((edge) => {
    const source = nodes.find((n) => n.id === edge.source);
    const target = nodes.find((n) => n.id === edge.target);
    if (!source || !target || source.isDragging || target.isDragging) return;

    const dx = target.x - source.x;
    const dy = target.y - source.y;
    const dist = Math.sqrt(dx * dx + dy * dy) || 1;
    const force = (dist - 100) * k;

    const fx = (dx / dist) * force;
    const fy = (dy / dist) * force;

    source.vx += fx;
    source.vy += fy;
    target.vx -= fx;
    target.vy -= fy;

    // Update edge coordinates
    edge.sourceX = source.x;
    edge.sourceY = source.y;
    edge.targetX = target.x;
    edge.targetY = target.y;
  });

  // Update positions for non-dragged nodes
  nodes.forEach((node) => {
    if (node.isDragging) return;

    node.x += node.vx;
    node.y += node.vy;
    node.x = Math.max(50, Math.min(750, node.x));
    node.y = Math.max(50, Math.min(550, node.y));
  });
}
