import humanize from 'humanize-number';

export function hasScoreOrDistanceInData<T extends Record<string, any>>(data: T[]): boolean {
  return data.some((item) => 'Score' in item || 'Distance' in item);
}

export function getNodeAndEdgeGUIDsByEntityList<T>(
  entityList: T[],
  nodeGUIDKey?: keyof T,
  edgeGUIDKey?: keyof T
) {
  const nodeGUIDs = nodeGUIDKey ? entityList.map((entity) => entity[nodeGUIDKey] as string) : [];
  const edgeGUIDs = edgeGUIDKey ? entityList.map((entity) => entity[edgeGUIDKey] as string) : [];
  return {
    nodeGUIDs: Array.from(new Set(nodeGUIDs?.filter((guid) => guid))),
    edgeGUIDs: Array.from(new Set(edgeGUIDs?.filter((guid) => guid))),
  };
}

export const getPercentage = (value: number, total: number) => {
  return Math.round((value / total) * 100);
};

export const humanizeNumber = (value: number) => {
  return humanize(value);
};
