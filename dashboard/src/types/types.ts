import { CredentialMetadata } from 'litegraphdb/dist/types/types';
import { Edge } from 'litegraphdb/dist/types/types';
import { Graph } from 'litegraphdb/dist/types/types';
import { Node } from 'litegraphdb/dist/types/types';
import { TagMetaData } from 'litegraphdb/dist/types/types';
import { VectorMetadata } from 'litegraphdb/dist/types/types';

export type VectorType = VectorMetadata & {
  NodeName?: string;
  EdgeName?: string;
  key?: string;
};

export type TagType = TagMetaData & {
  NodeName?: string;
  EdgeName?: string;
  key?: string;
};

export type NodeType = Node & {
  Score?: number;
  Distance?: number;
};

export interface GraphData extends Graph {
  gexfContent?: string;
}

export type EdgeType = Edge & {
  Distance?: number;
  Score?: number;
  FromName?: string;
  ToName?: string;
};

export type CredentialType = CredentialMetadata & {
  userName?: string;
};

export enum ThemeEnum {
  LIGHT = 'light',
  DARK = 'dark',
}
