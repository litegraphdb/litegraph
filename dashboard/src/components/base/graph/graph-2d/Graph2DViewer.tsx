import { SigmaContainer } from '@react-sigma/core';
import { MultiDirectedGraph } from 'graphology';
import React, { Dispatch, SetStateAction, useEffect, useRef, useState } from 'react';
import SigmaGraphLoader from './SigmaGraphLoader';
import { GraphEdgeTooltip, GraphNodeTooltip } from '../types';
import { EdgeData, NodeData } from '@/lib/graph/types';
import { uuid } from '@/utils/stringUtils';
import { useAppContext } from '@/hooks/appHooks';
import { ThemeEnum } from '@/types/types';
import { LightGraphTheme } from '@/theme/theme';

interface Graph2DViewerProps {
  show3d: boolean;
  selectedGraphRedux: string;
  nodes: NodeData[];
  edges: EdgeData[];
  gexfContent: string;
  setTooltip: Dispatch<SetStateAction<GraphNodeTooltip>>;
  setEdgeTooltip: Dispatch<SetStateAction<GraphEdgeTooltip>>;
  nodeTooltip: GraphNodeTooltip;
  edgeTooltip: GraphEdgeTooltip;
  showGraphHorizontal: boolean;
  topologicalSortNodes: boolean;
  showLabel: boolean;
  groupDragging: boolean;
  legends: Record<string, { legend: string; color: string }>;
}
const Graph2DViewer = ({
  show3d,
  selectedGraphRedux,
  nodes,
  edges,
  gexfContent,
  setTooltip,
  setEdgeTooltip,
  nodeTooltip,
  edgeTooltip,
  showGraphHorizontal,
  topologicalSortNodes,
  showLabel,
  groupDragging,
  legends,
}: Graph2DViewerProps) => {
  const [refId, setRefId] = useState<string>(uuid());
  const { theme } = useAppContext();

  useEffect(() => {
    setRefId(uuid());
    // console.log('nodes on graph 2d viewer', { nodes, edges });
  }, [nodes, edges, showGraphHorizontal, theme, topologicalSortNodes, showLabel]);

  return (
    <SigmaContainer
      className={show3d ? 'd-none' : ''}
      key={refId} // Force re-render when the context changes
      style={{ height: '100%' }}
      settings={{
        enableEdgeHoverEvents: true, // Explicitly enable edge hover events
        enableEdgeClickEvents: true, // Enable click events for edges
        defaultNodeColor: '#999',
        defaultEdgeColor: '#999',
        labelSize: 14,
        labelWeight: 'bold',
        renderEdgeLabels: showLabel,
        labelRenderer: (context: CanvasRenderingContext2D, data: any, settings: any) => {
          const size = settings.labelSize || 14;
          const font = `bold ${size}px ${settings.labelFont || '"Inter", "serif"'}`;

          context.font = font;
          context.fillStyle = theme === ThemeEnum.LIGHT ? '#666' : '#aaa';
          context.textAlign = 'center';
          context.textBaseline = 'bottom'; // Positions label above the node
          context.fillText(data.label, data.x, data.y - (data.size || 10) - 4); // shift above node
        },
        hoverRenderer: (context: CanvasRenderingContext2D, data: any, settings: any) => {
          const size = settings.labelSize || 14;
          const font = `bold ${size}px ${settings.labelFont || '"Inter", "serif"'}`;

          context.font = font;
          context.fillStyle = theme === ThemeEnum.LIGHT ? '#222' : '#ddd';
          context.textAlign = 'center';
          context.textBaseline = 'bottom'; // Positions label above the node
          context.fillText(data.label, data.x, data.y - (data.size || 10) - 4); // shift above node
        },
        renderLabels: showLabel,
        edgeLabelSize: 12,
        minCameraRatio: 0.1,
        maxCameraRatio: 10,
        nodeReducer: (node, data) => ({
          ...data,
          highlighted: data.highlighted,
          size: data.highlighted ? 12 : 7,
          color: data.highlighted ? '#ff9900' : data.color,
        }),
        edgeReducer: (edge, data) => ({
          ...data,
          size: data.size * (data.highlighted ? 1.2 : 0.7),
          color: data.highlighted ? '#ff9900' : data.color,
          label: data.highlighted ? data.label : undefined,
        }),
      }}
      graph={MultiDirectedGraph}
    >
      <SigmaGraphLoader
        gexfContent={''}
        setTooltip={setTooltip}
        setEdgeTooltip={setEdgeTooltip}
        nodeTooltip={nodeTooltip}
        edgeTooltip={edgeTooltip}
        nodes={nodes}
        edges={edges}
        groupDragging={groupDragging}
        legends={legends}
      />
    </SigmaContainer>
  );
};

export default Graph2DViewer;
