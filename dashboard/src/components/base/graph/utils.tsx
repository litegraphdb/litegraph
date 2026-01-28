import { NodeData } from '@/lib/graph/types';
import { defaultNodeColor, nodeLightColorByType, universalNodeColors } from './constant';

const maxLegends = 16;

export const getLegendsForNodes = (nodes: NodeData[]) => {
  const priorityLegends = new Map<string, { legend: string; color: string }>();
  const customLegends = new Map<string, { legend: string; color: string }>();
  let hasUnknownNodeType = false;
  // Separate legends into priority (nodeLightColorByType) and custom
  nodes.forEach((node) => {
    if (!node.type) {
      hasUnknownNodeType = true;
    }
    if (Object.keys(nodeLightColorByType).includes(node.type)) {
      priorityLegends.set(node.type, { legend: node.type, color: nodeLightColorByType[node.type] });
    } else {
      // Only add if not already present to avoid changing colors for same legend
      if (node.type && !customLegends.has(node.type)) {
        customLegends.set(node.type, {
          legend: node.type,
          color: universalNodeColors[customLegends.size % universalNodeColors.length],
        });
      }
    }
  });

  const priorityLegendsSize = priorityLegends.size;
  // If there are nodes with unknown types, add "Unknown" to priority legends
  const includeUnknownNodeType =
    customLegends.size + priorityLegendsSize > maxLegends || hasUnknownNodeType;

  // Convert to arrays for easier manipulation
  const priorityArray = Array.from(priorityLegends.values());
  const customArray = Array.from(customLegends.values()).slice(
    0,
    maxLegends - (priorityArray.length + (includeUnknownNodeType ? 1 : 0))
  );

  // Combine priority legends first, then custom legends
  const legends = [
    ...priorityArray,
    ...customArray,
    ...(includeUnknownNodeType ? [{ legend: 'Unknown', color: defaultNodeColor }] : []),
  ];
  const mapLegends: Record<string, { legend: string; color: string }> = {};
  legends.forEach((legend) => {
    mapLegends[legend.legend] = legend;
  });
  return mapLegends;
};
