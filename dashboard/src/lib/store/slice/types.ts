import { Graph } from 'litegraphdb/dist/types/types';

export interface GraphData extends Graph {
  gexfContent?: string;
}

export enum SliceTags {
  GRAPH = 'graph',
  NODE = 'node',
  EDGE = 'edge',
  TAG = 'tag',
  LABEL = 'label',
  VECTOR = 'vector',
  CREDENTIAL = 'credential',
  USER = 'user',
  TENANT = 'tenant',
  BACKUP = 'backup',
  RESET = 'reset',
}
