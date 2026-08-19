import { EdgeType } from '@/types/types';
import { NodeType } from '@/types/types';

type Translator = (key: string, values?: Record<string, string | number>) => string;

// Utility to find the name by GUID
export const getNodeNameByGUID = (
  guid: string,
  nodesList: NodeType[] | null | undefined,
  t?: Translator
) => {
  if (!guid) return t ? t('states.notAvailable') : 'N/A';
  if (!nodesList || !Array.isArray(nodesList)) return guid;
  const node = nodesList.find((n: NodeType) => n.GUID === guid);
  return node ? node.Name : guid;
};

export const transformEdgeDataForTable = (
  edgesList: EdgeType[] | null | undefined,
  nodesList: NodeType[] | null | undefined,
  t?: Translator
) => {
  if (!edgesList || !Array.isArray(edgesList)) return [];

  return edgesList.map((data: EdgeType) => {
    return {
      ...data,
      FromName: getNodeNameByGUID(data.From, nodesList, t),
      ToName: getNodeNameByGUID(data.To, nodesList, t),
      key: data.GUID,
    } as EdgeType;
  });
};
