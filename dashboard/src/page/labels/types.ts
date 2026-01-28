import { LabelMetadata } from 'litegraphdb/dist/types/types';

export type LabelMetadataForTable = LabelMetadata & {
  NodeName: string;
  EdgeName: string;
  key: string;
};
