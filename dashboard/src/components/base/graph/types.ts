export interface GraphNodeTooltip {
  visible: boolean;
  nodeId: string;
  x: number;
  y: number;
}

export interface GraphEdgeTooltip {
  visible: boolean;
  edgeId: string;
  x: number;
  y: number;
}

export interface GraphEnumerateRequest {
  token?: string;
  maxKeys?: number;
}
