import { CredentialMetadata } from 'litegraphdb/dist/types/types';
import { Edge } from 'litegraphdb/dist/types/types';
import { Graph } from 'litegraphdb/dist/types/types';
import { Node } from 'litegraphdb/dist/types/types';
import { TagMetaData } from 'litegraphdb/dist/types/types';
import { UserMetadata } from 'litegraphdb/dist/types/types';
import { VectorMetadata } from 'litegraphdb/dist/types/types';

/**
 * User record extended with the v8.0 capability flags. The packaged SDK type
 * does not yet declare these, but the server round-trips them, so the dashboard
 * layers them on locally. Both default to false when absent.
 */
export type FlaggedUser = UserMetadata & {
  IsSystemAdmin?: boolean;
  IsTenantAdmin?: boolean;
};

/** User create/update payload carrying the capability flags. */
export type FlaggedUserWriteRequest = {
  FirstName: string;
  LastName: string;
  Email: string;
  Password?: string;
  Active: boolean;
  IsSystemAdmin?: boolean;
  IsTenantAdmin?: boolean;
};

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
