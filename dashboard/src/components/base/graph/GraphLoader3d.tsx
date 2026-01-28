'use client';

import React, { Dispatch, SetStateAction, useEffect, useState, useRef } from 'react';
import dynamic from 'next/dynamic';
import { GraphData, LinkObject, NodeObject } from 'react-force-graph-3d';
import { GraphEdgeTooltip, GraphNodeTooltip } from './types';
import { NodeType, ThemeEnum } from '@/types/types';
import { EdgeData, NodeData } from '@/lib/graph/types';
import { darkTheme } from '@/theme/theme';
import { calculateTooltipPosition } from '@/utils/appUtils';
import { useAppContext } from '@/hooks/appHooks';
import * as THREE from 'three';
import { defaultNodeColor, nodeLightColorByType } from './constant';
import useToken from 'antd/es/theme/useToken';
import { GlobalToken } from 'antd';

const ForceGraph3D = dynamic(() => import('react-force-graph-3d'), {
  ssr: false,
});

// --- HELPER: Create label canvas with word wrap ---
function createTextCanvas(text: string, token: GlobalToken): HTMLCanvasElement | null {
  const maxLineChars = 30;
  const fontSize = 30;
  const padding = 20;

  const words = text.split(' ');
  const lines: string[] = [];
  let currentLine = '';

  for (const word of words) {
    if ((currentLine + word).length > maxLineChars) {
      lines.push(currentLine.trim());
      currentLine = word + ' ';
    } else {
      currentLine += word + ' ';
    }
  }
  if (currentLine) lines.push(currentLine.trim());

  const tempCanvas = document.createElement('canvas');
  const tempCtx = tempCanvas.getContext('2d')!;
  tempCtx.font = `bold ${fontSize}px "Inter", "serif"`;
  const maxLineWidthPx = Math.max(...lines.map((line) => tempCtx.measureText(line).width));

  const canvas = document.createElement('canvas');
  canvas.width = Math.pow(2, Math.ceil(Math.log2(maxLineWidthPx + padding * 2)));
  canvas.height = Math.pow(2, Math.ceil(Math.log2((fontSize + 10) * lines.length + padding * 2)));

  const ctx = canvas.getContext('2d');
  if (!ctx) return null;

  ctx.font = `bold ${fontSize}px "Inter", "serif"`;
  ctx.fillStyle = token.colorTextSecondary || 'white';
  ctx.textAlign = 'center';
  ctx.textBaseline = 'middle';

  lines.forEach((line, i) => {
    ctx.fillText(line, canvas.width / 2, padding + (i + 0.5) * (fontSize + 10));
  });

  return canvas;
}

// --- MAIN COMPONENT ---
export default function GraphLoader3d({
  nodes,
  edges,
  setTooltip,
  setEdgeTooltip,
  className,
  ref,
  containerDivHeightAndWidth,
  showLabels = true,
  legends,
}: {
  nodes: NodeData[];
  edges: EdgeData[];
  className?: string;
  setTooltip: Dispatch<SetStateAction<GraphNodeTooltip>>;
  setEdgeTooltip: Dispatch<SetStateAction<GraphEdgeTooltip>>;
  ref: React.RefObject<HTMLDivElement | null>;
  containerDivHeightAndWidth: { height?: number; width?: number };
  showLabels?: boolean;
  legends: Record<string, { legend: string; color: string }>;
}) {
  const { theme } = useAppContext();
  const [_, token] = useToken();
  const [graphData, setGraphData] = useState<GraphData>({ nodes: [], links: [] });
  const labelRefs = useRef(new Map<string, THREE.Sprite>());
  const fgRef = useRef<any>(null);

  // --- Map raw data to force-graph format ---
  useEffect(() => {
    const mappedNodes = nodes.map((n) => ({
      id: n.id,
      name: n.label,
      type: n.type,
    }));

    // Filter edges to only include those with valid source and target nodes
    const validEdges = edges.filter((e) => {
      const hasValidSource = nodes.some((n) => n.id === e.source);
      const hasValidTarget = nodes.some((n) => n.id === e.target);
      if (!hasValidSource || !hasValidTarget) {
        console.warn(
          `Skipping edge ${e.id}: source node ${e.source} or target node ${e.target} not found in nodes`
        );
        return false;
      }
      return true;
    });

    const mappedLinks = validEdges.map((e) => ({
      source: e.source,
      target: e.target,
      id: e.id,
      cost: e.cost,
      name: e.label,
    }));

    labelRefs.current.clear(); // clear old labels

    setGraphData({
      nodes: mappedNodes,
      links: mappedLinks,
    });
  }, [nodes, edges]);

  const handleNodeClick = (node: NodeObject<NodeType>, event: any) => {
    setEdgeTooltip({ visible: false, edgeId: '', x: 0, y: 0 });
    const { x, y } = calculateTooltipPosition(event.clientX, event.clientY);
    setTooltip({
      visible: true,
      nodeId: node.id as string,
      x,
      y,
    });
  };

  const handleLinkClick = (link: LinkObject<NodeData, EdgeData>, event: any) => {
    setTooltip({ visible: false, nodeId: '', x: 0, y: 0 });
    const { x, y } = calculateTooltipPosition(event.clientX, event.clientY);
    setEdgeTooltip({
      visible: true,
      edgeId: link.id,
      x,
      y,
    });
  };

  return (
    <ForceGraph3D
      ref={fgRef}
      height={containerDivHeightAndWidth.height}
      width={containerDivHeightAndWidth.width}
      graphData={graphData}
      backgroundColor={theme === ThemeEnum.LIGHT ? '#fff' : darkTheme.token?.colorBgBase}
      nodeOpacity={0.8}
      nodeAutoColorBy="type"
      nodeLabel="name"
      linkLabel="name"
      linkColor={() => (theme === ThemeEnum.LIGHT ? '#444' : '#fff')}
      linkWidth={1}
      showNavInfo
      enableNodeDrag
      onNodeClick={(node, event) => handleNodeClick(node as NodeObject<NodeType>, event)}
      onLinkClick={(link, event) => handleLinkClick(link as LinkObject<NodeData, EdgeData>, event)}
      nodeColor={(node) => legends[node.type]?.color || defaultNodeColor}
      nodeThreeObject={
        showLabels
          ? (node: any) => {
              const canvas = createTextCanvas(node.name as string, token);
              if (!canvas) return new THREE.Object3D();

              const texture = new THREE.CanvasTexture(canvas);
              texture.minFilter = THREE.LinearFilter;
              texture.magFilter = THREE.LinearFilter;
              texture.generateMipmaps = false;

              const sprite = new THREE.Sprite(
                new THREE.SpriteMaterial({ map: texture, depthWrite: false })
              );

              const scaleY = 12;
              const scaleX = (canvas.width / canvas.height) * scaleY;
              sprite.scale.set(scaleX, scaleY, 1);
              sprite.position.set(0, 5, 0);

              labelRefs.current.set(node.id as string, sprite);
              return sprite;
            }
          : undefined
      }
      nodeThreeObjectExtend={showLabels}
    />
  );
}
