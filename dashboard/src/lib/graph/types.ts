export type GraphNodeKind = 'node' | 'group' | 'group-anchor';

export interface NodeData {
  id: string;
  label: string;
  type: string;
  x: number;
  y: number;
  z: number;
  vx: number;
  vy: number;
  isDragging?: boolean;
  size?: number;
  labels?: string[];
  tags?: Record<string, any>;
  tagKeys?: string[];
  depth?: number;
  componentId?: string;
  clusterId?: string;
  clusterLabel?: string;
  nodeKind?: GraphNodeKind;
  groupTargetId?: string;
  memberNodeIds?: string[];
  alwaysShowLabel?: boolean;
}

export interface EdgeData {
  id: string;
  source: string;
  target: string;
  cost: number;
  data: string;
  sourceX: number;
  sourceY: number;
  targetX: number;
  targetY: number;
  label?: string;
  labels?: string[];
  tags?: Record<string, any>;
  relationType?: string;
  isSynthetic?: boolean;
  rawEdgeIds?: string[];
  edgeCount?: number;
}

export type HoveredElement =
  | {
      type: 'edge';
      data: EdgeData;
    }
  | {
      type: 'node';
      data: NodeData;
    }
  | null;

export interface Point {
  x: number;
  y: number;
}
