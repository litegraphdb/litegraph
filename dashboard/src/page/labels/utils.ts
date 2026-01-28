import { NodeType } from '@/types/types';
import { getNodeNameByGUID } from '../edges/utils';

import { getEdgeNameByGUID } from '../tags/utils';
import { LabelMetadata } from 'litegraphdb/dist/types/types';
import { EdgeType } from '@/types/types';
import { LabelMetadataForTable } from './types';

export const transformLabelsDataForTable = (
  labelsList: LabelMetadata[],
  nodesList: NodeType[],
  edgesList: EdgeType[]
) => {
  return (
    labelsList?.map((data: LabelMetadata) => {
      return {
        ...data,
        NodeName: getNodeNameByGUID(data.NodeGUID, nodesList),
        EdgeName: getEdgeNameByGUID(data.EdgeGUID, edgesList),
        key: data.GUID,
      } as LabelMetadataForTable;
    }) || []
  );
};
