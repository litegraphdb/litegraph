import type { NodeData, Point } from './types';

export function isPointInNode(point: Point, node: NodeData, radius: number = 10): boolean {
  const dx = point.x - node.x;
  const dy = point.y - node.y;
  return dx * dx + dy * dy <= radius * radius;
}

export function getNodeAtPosition(nodes: NodeData[], point: Point): NodeData | null {
  return nodes.find((node) => isPointInNode(point, node)) || null;
}

export function transformToOptions<T extends { GUID: string; name?: string; Name?: string }>(
  data?: T[] | null,
  labelField: keyof T = 'name' // Field to use for label in options
) {
  return data
    ? data?.map((item) => ({
        value: item.GUID,
        label: (item[labelField] as string) || item.Name || item.GUID,
      }))
    : [];
}
