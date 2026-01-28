import { GraphEdgeTooltip, GraphNodeTooltip } from './types';

export const defaultNodeTooltip: GraphNodeTooltip = {
  visible: false,
  nodeId: '',
  x: 0,
  y: 0,
};

export const defaultEdgeTooltip: GraphEdgeTooltip = {
  visible: false,
  edgeId: '',
  x: 0,
  y: 0,
};

export const defaultNodeColor = '#aaaaaa';

export const nodeLightColorByType: Record<string, string> = {
  StoragePool: '#008f7a',
  BucketMetadata: '#008e9b',
  TenantMetadata: '#ff8066',
  VectorRepository: '#0081cf',
  Collection: '#c34a36',
  ObjectMetadata: '#845ec2',
  EmbeddingsDocument: '#d65db1',
  SourceDocument: '#ff6f91',
  Unknown: defaultNodeColor,
};
export const universalNodeColors = [
  '#5C7AEA', // soft blue
  '#E0A458', // warm gold
  '#50B748', // fresh green
  '#C97FD3', // lavender magenta
  '#F2C94C', // bright yellow
  '#2D9CDB', // sky blue
  '#EB5757', // coral red
  '#6FCF97', // mint green
  '#9B51E0', // vivid violet
  '#F2994A', // orange
  '#56CCF2', // cyan blue
  '#219653', // forest green
  '#BB6BD9', // purple
  '#F2D388', // sand
  '#27AE60', // jade
  '#E8A87C', // light brown
];
