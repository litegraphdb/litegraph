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
  AUTHORIZATION = 'authorization',
  SETTINGS = 'settings',
  RESET = 'reset',
  CHAT_ENDPOINT = 'chatEndpoint',
  CHAT_THREAD = 'chatThread',
  CHAT_FEEDBACK = 'chatFeedback',
  CHAT_SETTINGS = 'chatSettings',
}
