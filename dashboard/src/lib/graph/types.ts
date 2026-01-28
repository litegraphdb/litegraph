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
