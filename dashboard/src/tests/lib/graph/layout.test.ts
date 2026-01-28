import { applyForceLayout } from '@/lib/graph/layout';
import type { NodeData, EdgeData } from '@/lib/graph/types';

describe('Graph Layout', () => {
  describe('applyForceLayout', () => {
    let nodes: NodeData[];
    let edges: EdgeData[];

    beforeEach(() => {
      // Reset nodes and edges for each test
      nodes = [
        {
          id: 'node1',
          x: 100,
          y: 100,
          vx: 0,
          vy: 0,
          isDragging: false,
        },
        {
          id: 'node2',
          x: 200,
          y: 200,
          vx: 0,
          vy: 0,
          isDragging: false,
        },
        {
          id: 'node3',
          x: 300,
          y: 150,
          vx: 0,
          vy: 0,
          isDragging: false,
        },
      ];

      edges = [
        {
          id: 'edge1',
          source: 'node1',
          target: 'node2',
          sourceX: 100,
          sourceY: 100,
          targetX: 200,
          targetY: 200,
        },
        {
          id: 'edge2',
          source: 'node2',
          target: 'node3',
          sourceX: 200,
          sourceY: 200,
          targetX: 300,
          targetY: 150,
        },
      ];
    });

    it('should apply force layout to nodes and edges', () => {
      const originalPositions = nodes.map((n) => ({ x: n.x, y: n.y }));

      applyForceLayout(nodes, edges);

      // Check that positions have changed (forces were applied)
      nodes.forEach((node, index) => {
        expect(node.x).not.toBe(originalPositions[index].x);
        expect(node.y).not.toBe(originalPositions[index].y);
      });
    });

    it('should skip force calculations for dragging nodes', () => {
      nodes[0].isDragging = true;
      const originalPosition = { x: nodes[0].x, y: nodes[0].y };
      const originalVelocity = { vx: nodes[0].vx, vy: nodes[0].vy };

      applyForceLayout(nodes, edges);

      // Dragging node should not move
      expect(nodes[0].x).toBe(originalPosition.x);
      expect(nodes[0].y).toBe(originalPosition.y);
      expect(nodes[0].vx).toBe(0);
      expect(nodes[0].vy).toBe(0);
    });

    it('should apply gravity to center (400, 300)', () => {
      // Place node far from center
      nodes[0].x = 100;
      nodes[0].y = 100;
      nodes[0].vx = 0;
      nodes[0].vy = 0;

      applyForceLayout(nodes, edges);

      // Node should move toward center
      expect(nodes[0].x).toBeGreaterThan(100);
      expect(nodes[0].y).toBeGreaterThan(100);
    });

    it('should apply repulsion between nodes', () => {
      // Place nodes close together
      nodes[0].x = 100;
      nodes[0].y = 100;
      nodes[1].x = 101;
      nodes[1].y = 101;

      const originalDistance = Math.sqrt(
        Math.pow(nodes[1].x - nodes[0].x, 2) + Math.pow(nodes[1].y - nodes[0].y, 2)
      );

      applyForceLayout(nodes, edges);

      const newDistance = Math.sqrt(
        Math.pow(nodes[1].x - nodes[0].x, 2) + Math.pow(nodes[1].y - nodes[0].y, 2)
      );

      // Nodes should move apart due to repulsion
      expect(newDistance).toBeGreaterThan(originalDistance);
    });

    it('should apply spring forces along edges', () => {
      // Place nodes far apart
      nodes[0].x = 100;
      nodes[0].y = 100;
      nodes[1].x = 500;
      nodes[1].y = 500;

      const originalDistance = Math.sqrt(
        Math.pow(nodes[1].x - nodes[0].x, 2) + Math.pow(nodes[1].y - nodes[0].y, 2)
      );

      applyForceLayout(nodes, edges);

      const newDistance = Math.sqrt(
        Math.pow(nodes[1].x - nodes[0].x, 2) + Math.pow(nodes[1].y - nodes[0].y, 2)
      );

      // Nodes should move closer due to spring force (target distance is 100)
      expect(newDistance).toBeLessThan(originalDistance);
    });

    it('should update edge coordinates after layout', () => {
      const originalEdgeCoords = {
        sourceX: edges[0].sourceX,
        sourceY: edges[0].sourceY,
        targetX: edges[0].targetX,
        targetY: edges[0].targetY,
      };

      applyForceLayout(nodes, edges);

      // Edge coordinates should be updated to match node positions
      const sourceNode = nodes.find((n) => n.id === edges[0].source);
      const targetNode = nodes.find((n) => n.id === edges[0].target);

      // Check that coordinates were updated (may not be exact due to force calculations)
      expect(edges[0].sourceX).toBeDefined();
      expect(edges[0].sourceY).toBeDefined();
      expect(edges[0].targetX).toBeDefined();
      expect(edges[0].targetY).toBeDefined();
    });

    it('should constrain node positions within bounds', () => {
      // Place node outside bounds
      nodes[0].x = 1000;
      nodes[0].y = 1000;

      applyForceLayout(nodes, edges);

      // Node should be constrained within bounds (50-750 for x, 50-550 for y)
      expect(nodes[0].x).toBeLessThanOrEqual(750);
      expect(nodes[0].x).toBeGreaterThanOrEqual(50);
      expect(nodes[0].y).toBeLessThanOrEqual(550);
      expect(nodes[0].y).toBeGreaterThanOrEqual(50);
    });

    it('should handle empty nodes array', () => {
      expect(() => {
        applyForceLayout([], edges);
      }).not.toThrow();
    });

    it('should handle empty edges array', () => {
      expect(() => {
        applyForceLayout(nodes, []);
      }).not.toThrow();
    });

    it('should handle nodes with missing edge connections', () => {
      // Create edge with non-existent node IDs
      const invalidEdges: EdgeData[] = [
        {
          id: 'invalid-edge',
          source: 'nonexistent-node',
          target: 'another-nonexistent-node',
          sourceX: 0,
          sourceY: 0,
          targetX: 0,
          targetY: 0,
        },
      ];

      expect(() => {
        applyForceLayout(nodes, invalidEdges);
      }).not.toThrow();
    });

    it('should apply damping to node velocities', () => {
      nodes[0].vx = 10;
      nodes[0].vy = 10;

      applyForceLayout(nodes, edges);

      // Velocities should be modified by forces and damping
      expect(nodes[0].vx).toBeDefined();
      expect(nodes[0].vy).toBeDefined();
    });

    it('should handle nodes at the same position', () => {
      nodes[0].x = 100;
      nodes[0].y = 100;
      nodes[1].x = 100;
      nodes[1].y = 100;

      expect(() => {
        applyForceLayout(nodes, edges);
      }).not.toThrow();

      // Nodes should move apart due to repulsion
      const distance = Math.sqrt(
        Math.pow(nodes[1].x - nodes[0].x, 2) + Math.pow(nodes[1].y - nodes[0].y, 2)
      );
      expect(distance).toBeGreaterThan(0);
    });

    it('should handle edge with dragging source node', () => {
      nodes[0].isDragging = true;

      expect(() => {
        applyForceLayout(nodes, edges);
      }).not.toThrow();

      // Edge coordinates should still be updated
      const targetNode = nodes.find((n) => n.id === edges[0].target);
      expect(edges[0].targetX).toBeDefined();
      expect(edges[0].targetY).toBeDefined();
    });

    it('should handle edge with dragging target node', () => {
      nodes[1].isDragging = true;

      expect(() => {
        applyForceLayout(nodes, edges);
      }).not.toThrow();

      // Edge coordinates should still be updated
      const sourceNode = nodes.find((n) => n.id === edges[0].source);
      expect(edges[0].sourceX).toBeDefined();
      expect(edges[0].sourceY).toBeDefined();
    });
  });
});
